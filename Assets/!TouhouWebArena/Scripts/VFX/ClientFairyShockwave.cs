using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for HashSet

// Requires ClientShockwaveVisuals
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(ClientShockwaveVisuals))] 
[RequireComponent(typeof(PooledObjectInfo))]
public class ClientFairyShockwave : MonoBehaviour
{
    // Expansion Settings 
    private float _maxRadius = 2f; 
    private float _expansionDuration = 0.5f; 
    private AnimationCurve _expansionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); 
    private int _damageToDeal = 5;
    private ulong _ownerIdForChainedDamage; 

    [Header("Damage Settings")]
    [Tooltip("How many times per second the shockwave can damage/clear the SAME object.")]
    [SerializeField] private float damageTickRate = 10f; // e.g., 10 times per second max per object
    private float _damageCooldownDuration; // Calculated from tick rate

    // Components
    private CircleCollider2D _circleCollider;
    private ClientShockwaveVisuals _shockwaveVisuals;
    private PooledObjectInfo _pooledObjectInfo;

    // State
    private float _currentExpansionTime = 0f;
    private bool _isExpanding = false;
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

    public void Initialize(float startRadius, float targetMaxRadius, float duration, AnimationCurve curve, int damage, ulong ownerId)
    {
        transform.localScale = Vector3.one; 
        // _circleCollider.radius = startRadius; // Collider starts at 0 and expands
        _maxRadius = targetMaxRadius;
        _expansionDuration = duration;
        _expansionCurve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1); 
        _damageToDeal = damage;
        _ownerIdForChainedDamage = ownerId;

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
        float currentRadius = Mathf.Lerp(0, _maxRadius, curveValue); 
        _circleCollider.radius = currentRadius; // Expand the actual collider

        if (_shockwaveVisuals != null)
        {
            _shockwaveVisuals.UpdateVisuals(progress, currentRadius); // Update visuals based on current radius
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
        if (!_isExpanding) return;

        // Check cooldown for this specific collider
        if (_lastHitTime.TryGetValue(other, out float lastHit) && Time.time < lastHit + _damageCooldownDuration)
        {
            return; // Still in cooldown for this object
        }

        // --- Apply effect and update cooldown ---
        bool processed = false;

        // 1. Damage Fairies/Spirits
        ClientFairyHealth fairyHealth = other.GetComponent<ClientFairyHealth>();
        if (fairyHealth != null && fairyHealth.IsAlive)
        {
            fairyHealth.TakeDamage(_damageToDeal, _ownerIdForChainedDamage);
            processed = true;
        }
        // TODO: Add ClientSpiritHealth check

        // 2. Clear Bullets (only if not already processed as damage)
        if (!processed)
        {
            StageSmallBulletMoverScript stageMover = other.GetComponent<StageSmallBulletMoverScript>();
            if (stageMover != null)
            {
                stageMover.ForceReturnToPoolByBomb(); 
                processed = true;
            }

            // Check for player bullets if needed
            // BulletMovement playerBulletMover = other.GetComponent<BulletMovement>();
            // if (playerBulletMover != null) { /* Potentially clear */ processed = true; }
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