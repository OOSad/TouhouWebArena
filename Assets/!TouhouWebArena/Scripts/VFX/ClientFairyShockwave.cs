using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for HashSet
using TouhouWebArena; // For PlayerRole

// Requires ClientShockwaveVisuals
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(ClientShockwaveVisuals))] 
[RequireComponent(typeof(PooledObjectInfo))]
public class ClientFairyShockwave : MonoBehaviour
{
    // Expansion Settings 
    private float _visualMaxRadius = 2f; 
    private float _effectiveMaxRadius = 2f; // Added for decoupled radius
    private float _expansionDuration = 0.5f; 
    private AnimationCurve _expansionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); 
    private int _damageToDeal = 5;
    private ulong _ownerIdForChainedDamage; 
    private PlayerRole _ownerPlayerRole = PlayerRole.None; // Role of the player whose side this shockwave belongs to
    private bool _canSpawnCounterBullets = true; // NEW: Flag to control counter bullet spawning

    [Header("Damage Settings")]
    [Tooltip("How many times per second the shockwave can damage/clear the SAME object.")]
    [SerializeField] private float damageTickRate = 10f; // e.g., 10 times per second max per object
    private float _damageCooldownDuration; // Calculated from tick rate

    [Header("Opponent Bullet Spawn Settings")]
    [Tooltip("Prefab ID of the bullet to spawn on the opponent's side when this shockwave clears a bullet.")]
    [SerializeField] private string opponentBulletPrefabId = "StageSmallBullet";
    [Tooltip("Speed of the bullet spawned on the opponent's side.")]
    [SerializeField] private float opponentBulletSpeed = 2.5f;
    [Tooltip("Lifetime of the bullet spawned on the opponent's side.")]
    [SerializeField] private float opponentBulletLifetime = 7f;

    // Components
    private CircleCollider2D _circleCollider;
    private ClientShockwaveVisuals _shockwaveVisuals;
    private PooledObjectInfo _pooledObjectInfo;

    // State
    private float _currentExpansionTime = 0f;
    private bool _isExpanding = false;
    private Color trueInitialColor; // Stores the full alpha color
    private Coroutine _despawnCoroutine;
    private Dictionary<Collider2D, float> _lastHitTime = new Dictionary<Collider2D, float>();

    void Awake()
    {
        _circleCollider = GetComponent<CircleCollider2D>();
        _shockwaveVisuals = GetComponent<ClientShockwaveVisuals>();
        _pooledObjectInfo = GetComponent<PooledObjectInfo>();

        _circleCollider.isTrigger = true; 
        _circleCollider.radius = 0; 
        _circleCollider.enabled = false; // Start disabled, enable in Initialize

        if (_shockwaveVisuals == null)
        {
            Debug.LogError("[ClientFairyShockwave] Missing ClientShockwaveVisuals!", this);
            enabled = false; 
        }
        if (_pooledObjectInfo == null)
        {
            Debug.LogError("[ClientFairyShockwave] Missing PooledObjectInfo!", this);
            enabled = false;
        }

        _damageCooldownDuration = (damageTickRate > 0) ? (1f / damageTickRate) : float.MaxValue; 
    }
    
    public float GetInitialColliderRadiusForVisuals() => 0f; // Visuals start from 0 radius effectively

    public void Initialize(float startRadius, float visualMaxRadius, float effectiveMaxRadius, float duration, AnimationCurve curve, int damage, ulong ownerId, PlayerRole ownerRole, bool canSpawnCounterBullets = true)
    {
        transform.localScale = Vector3.one; 
        _visualMaxRadius = visualMaxRadius;
        _effectiveMaxRadius = effectiveMaxRadius;
        _expansionDuration = duration;
        _expansionCurve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1); 
        _damageToDeal = damage;
        _ownerIdForChainedDamage = ownerId;
        _ownerPlayerRole = ownerRole;
        _canSpawnCounterBullets = canSpawnCounterBullets; // STORE THE FLAG

        _currentExpansionTime = 0f;
        _isExpanding = true;
        _lastHitTime.Clear(); // Clear hit history for this new expansion
        _circleCollider.radius = 0f;
        _circleCollider.enabled = true; // Enable collider for expansion

        if (_shockwaveVisuals != null) 
        {
             _shockwaveVisuals.ResetVisuals();
             _shockwaveVisuals.UpdateVisuals(0f, 0f); // Start visuals at 0 radius
        }

        if (_despawnCoroutine != null)
        {
            StopCoroutine(_despawnCoroutine);
        }
        _despawnCoroutine = StartCoroutine(DespawnAfterDuration(_expansionDuration));
    }

    void OnEnable()
    {
        _circleCollider.radius = 0f;
        _circleCollider.enabled = false; // Ensure disabled when taken from pool
        _isExpanding = false; 
        _lastHitTime.Clear();
        _ownerPlayerRole = PlayerRole.None;
        if (_shockwaveVisuals != null)
        {
            _shockwaveVisuals.ResetVisuals();
        }
    }
    
    void Update()
    {
        if (!_isExpanding) return;

        _currentExpansionTime += Time.deltaTime;
        float progress = Mathf.Clamp01(_currentExpansionTime / _expansionDuration);
        float curveValue = _expansionCurve.Evaluate(progress); 
        
        // Update Collider Radius based on effective max radius
        float currentEffectiveRadius = Mathf.Lerp(0, _effectiveMaxRadius, curveValue); 
        _circleCollider.radius = currentEffectiveRadius;

        if (_shockwaveVisuals != null)
        {
            // Update visuals based on visual max radius
            float currentVisualRadius = Mathf.Lerp(0, _visualMaxRadius, curveValue); 
            _shockwaveVisuals.UpdateVisuals(progress, currentVisualRadius); 
        }

        if (_currentExpansionTime >= _expansionDuration)
        {
            _isExpanding = false; 
            _circleCollider.enabled = false; // Disable collider at end of life
            // Despawn is handled by coroutine
        }
    }

    private IEnumerator DespawnAfterDuration(float delay)
    {
        yield return new WaitForSeconds(delay);
        _isExpanding = false; // Ensure stopped
        _circleCollider.enabled = false;
        if (ClientGameObjectPool.Instance != null && _pooledObjectInfo != null && gameObject != null)
        {
            ClientGameObjectPool.Instance.ReturnObject(gameObject);
        }
        else if (gameObject != null)
        {
            Destroy(gameObject);
        }
        _despawnCoroutine = null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // We use OnTriggerStay2D for continuous check with cooldown
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!_isExpanding || _ownerPlayerRole == PlayerRole.None) return; // Don't do checks if not expanding or owner role is invalid

        // Check cooldown for this specific collider
        if (_lastHitTime.TryGetValue(other, out float lastHit) && Time.time < lastHit + _damageCooldownDuration)
        {
            return; // Still in cooldown for this object
        }

        // --- Apply effect and update cooldown ---
        bool processed = false;
        PlayerRole targetRole = PlayerRole.None;

        // 1. Check Fairies
        ClientFairyHealth fairyHealth = other.GetComponent<ClientFairyHealth>();
        if (fairyHealth != null && fairyHealth.IsAlive)
        {
            targetRole = fairyHealth.OwningPlayerRole;
            if (targetRole == _ownerPlayerRole) // Check if target role matches shockwave owner role
        {
            fairyHealth.TakeDamage(_damageToDeal, _ownerIdForChainedDamage);
            processed = true;
        }
        }
        
        // 2. Check Spirits (only if not already processed as a fairy)
        if (!processed)
        {
            ClientSpiritHealth spiritHealth = other.GetComponent<ClientSpiritHealth>();
            if (spiritHealth != null)
            {
                 // Get role via controller
                 ClientSpiritController spiritController = other.GetComponent<ClientSpiritController>();
                 if (spiritController != null) 
                 {
                    targetRole = spiritController.OwningPlayerRole;
                    if (targetRole == _ownerPlayerRole) // Check if target role matches shockwave owner role
                    {
                        spiritHealth.TakeDamage(_damageToDeal, _ownerIdForChainedDamage);
                        processed = true;
                    }
                 }
            }
        }

        // 3. Clear Bullets (only if not already processed as damage)
        if (!processed)
        {
            StageSmallBulletMoverScript stageMover = other.GetComponent<StageSmallBulletMoverScript>();
            if (stageMover != null)
            {
                PlayerRole bulletOwnerRole = stageMover.OwningPlayerRole;
                if (bulletOwnerRole == _ownerPlayerRole) // Only clear bullets on the same side as the shockwave
                {
                    Debug.Log($"[ClientFairyShockwave] Attempting to clear stage bullet {other.gameObject.name} owned by {bulletOwnerRole} (Shockwave Owner: {_ownerPlayerRole})");
                stageMover.ForceReturnToPoolByBomb(); 
                    _lastHitTime[other] = Time.time + _damageCooldownDuration; 

                    if (PlayerAttackRelay.LocalInstance != null)
                    {
                        PlayerRole opponentRole = (_ownerPlayerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
                        if (_ownerPlayerRole == PlayerRole.None) opponentRole = PlayerRole.None; 

                        if (opponentRole != PlayerRole.None)
                        {
                            if (_canSpawnCounterBullets) // CHECK THE FLAG
                            {
                                Debug.Log($"[ClientFairyShockwave (Counter Allowed)] Requesting opponent bullet spawn. Opponent: {opponentRole}, Prefab: {opponentBulletPrefabId}, Speed: {opponentBulletSpeed}, Lifetime: {opponentBulletLifetime}");
                                PlayerAttackRelay.LocalInstance.RequestOpponentStageBulletSpawnServerRpc(
                                    opponentRole,
                                    opponentBulletPrefabId,
                                    opponentBulletSpeed,
                                    opponentBulletLifetime
                                );
            }
                            else
                            {
                                Debug.Log($"[ClientFairyShockwave (Counter NOT Allowed)] Shockwave cleared bullet but will not spawn counter bullet.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[ClientFairyShockwave] Shockwave owner role is None, cannot determine opponent role for bullet spawn.", this);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[ClientFairyShockwave] PlayerAttackRelay.LocalInstance is null. Cannot request opponent bullet spawn.", this);
                    }
                }
                return; // Processed, exit
            }

            // Potential check for player bullets (BulletMovement) if they also need role-based clearing
            // BulletMovement playerBulletMover = other.GetComponent<BulletMovement>();
            // if (playerBulletMover != null) { /* Check role, Potentially clear */ processed = true; }
        }

        // If we damaged or cleared something, record the hit time
        if (processed)
        {
            _lastHitTime[other] = Time.time;
        }
    }

    void OnDisable()
    {
        if (_despawnCoroutine != null)
        {
            StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = null;
        }
        _isExpanding = false; 
        _circleCollider.enabled = false;
        _lastHitTime.Clear(); // Clear hit history
    }
} 