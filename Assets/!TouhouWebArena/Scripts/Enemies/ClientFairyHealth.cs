using UnityEngine;
using System;
using Unity.Netcode; // Required for NetworkManager
using TouhouWebArena; // Needed for PlayerRole
using System.Collections; // ADDED: For IEnumerator

// Basic client-side health management for a fairy.
public class ClientFairyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 1;
    [Header("Death Effects")][Tooltip("The Pool ID for the shockwave prefab spawned on death.")]
    [SerializeField] private string deathShockwavePrefabId = "FairyShockwave";
    [Tooltip("The maximum radius this fairy's death shockwave reaches visually.")]
    [SerializeField] private float deathShockwaveMaxRadius = 2f;
    [Tooltip("The maximum EFFECTIVE radius (for damage/clearing) of the death shockwave.")]
    [SerializeField] private float deathShockwaveEffectiveMaxRadius = 2f; // Default to visual radius
    [Tooltip("The duration of this fairy's death shockwave expansion.")]
    [SerializeField] private float deathShockwaveDuration = 0.5f;
    [Tooltip("The damage dealt by this fairy's death shockwave.")]
    [SerializeField] private int deathShockwaveDamage = 5;

    [Header("Damage Flash")][Tooltip("The color the sprite flashes when taking damage.")]
    [SerializeField] private Color _flashColor = Color.red;
    [Tooltip("How long the flash color lasts in seconds.")]
    [SerializeField] private float _flashDuration = 0.1f;
    [Tooltip("How strong the flash color tint is (0=no tint, 1=full color).")]
    [Range(0f, 1f)] [SerializeField] private float _flashIntensity = 0.5f;

    private SpriteRenderer _spriteRenderer;
    private Coroutine _flashCoroutine;

    private int _currentHealth;
    private bool _isExtraAttackTrigger = false; // Added for extra attack
    private PlayerRole _owningPlayerRole = PlayerRole.None; // Added field

    public int CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0;
    public PlayerRole OwningPlayerRole => _owningPlayerRole; // Added getter

    public event Action<GameObject> OnClientDeath; // Event to signal death, passing the fairy GameObject

    private void Awake()
    {
        // Cache the SpriteRenderer
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // Get it from children in case it's nested
        if (_spriteRenderer == null)
        {
            Debug.LogWarning($"[ClientFairyHealth on {gameObject.name}] SpriteRenderer not found in children.");
        }
    }

    void OnEnable()
    {
        // Reset health when enabled (e.g., when taken from pool)
        _currentHealth = maxHealth;
        _isExtraAttackTrigger = false; // Reset trigger status on enable
        _owningPlayerRole = PlayerRole.None; // Reset role on enable
        // Reset color in case it was flashing when disabled
        if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
        if (_flashCoroutine != null) 
        { 
            StopCoroutine(_flashCoroutine); 
            _flashCoroutine = null; 
        }
    }

    public void Initialize(int startingHealth, bool isTrigger, PlayerRole ownerRole)
    {
        maxHealth = startingHealth > 0 ? startingHealth : 1;
        _currentHealth = maxHealth;
        _isExtraAttackTrigger = isTrigger;
        _owningPlayerRole = ownerRole; // Store the role
        // Debug.Log($"{gameObject.name} initialized. Health: {_currentHealth}, IsTrigger: {_isExtraAttackTrigger}, Role: {_owningPlayerRole}");
    }

    public void SetMaxHealth(int newMaxHealth, bool setCurrentToMax = true)
    {
        maxHealth = newMaxHealth > 0 ? newMaxHealth : 1;
        if (setCurrentToMax)
        {
            _currentHealth = maxHealth;
        }
    }

    public void TakeDamage(int amount, ulong attackerOwnerClientId)
    {
        if (!IsAlive) return;

        // Trigger flash effect
        FlashRed();

        _currentHealth -= amount;
        // Debug.Log($"{gameObject.name} took {amount} damage from Client {attackerOwnerClientId}, health is now {_currentHealth}");

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;

            SpawnDeathShockwave(attackerOwnerClientId); // Spawn effect first

            // Conditional Kill Reporting
            if (_isExtraAttackTrigger)
            {
                // If it's a trigger fairy, let ClientExtraAttackManager handle it.
                // It will decide if a server report is needed for relaying.
                if (ClientExtraAttackManager.Instance != null)
                {
                    ClientExtraAttackManager.Instance.OnTriggerFairyKilled(attackerOwnerClientId);
                }
                else
                {
                    Debug.LogError($"[ClientFairyHealth on {gameObject.name}] ClientExtraAttackManager.Instance is null. Cannot report trigger fairy kill by {attackerOwnerClientId}.");
                }
            }
            else if (attackerOwnerClientId == NetworkManager.Singleton.LocalClientId) // Only report normal fairy kill if not a trigger and killed by local player
            {
                if (PlayerAttackRelay.LocalInstance != null)
                {
                    PlayerAttackRelay.LocalInstance.ReportFairyKillServerRpc();
                }
                else
                {
                    Debug.LogError($"[ClientFairyHealth on {gameObject.name}] PlayerAttackRelay.LocalInstance is null. Cannot report kill by local player {attackerOwnerClientId}.");
                }
            }
            
            // Trigger death event AFTER effects/reporting
            OnClientDeath?.Invoke(this.gameObject);
        }
    }

    public void ApplyLethalDamage(ulong instigatorClientId)
    {
        if (!IsAlive) return;
        _currentHealth = 0;

        SpawnDeathShockwave(instigatorClientId); // Spawn effect first

        // Conditional Kill Reporting for bombs/special
        // For lethal damage (e.g. bombs), we might not want extra attacks to trigger,
        // or it needs specific rules. For now, extra attacks only trigger from normal TakeDamage kills.
        // If bombs should trigger them, the logic from TakeDamage's _isExtraAttackTrigger block would be duplicated here.
        // For now, only standard kill reporting for lethal damage (if not handled by extra attack logic)
        if (!_isExtraAttackTrigger && instigatorClientId == NetworkManager.Singleton.LocalClientId)
        {
            if (PlayerAttackRelay.LocalInstance != null)
            {
                PlayerAttackRelay.LocalInstance.ReportFairyKillServerRpc(); // Or a specific bomb RPC if needed
            }
            else
            {
                Debug.LogError($"[ClientFairyHealth on {gameObject.name}] PlayerAttackRelay.LocalInstance is null. Cannot report lethal damage kill by local player {instigatorClientId}.");
            }
        }
        
        // Trigger death event AFTER effects/reporting
        OnClientDeath?.Invoke(this.gameObject); 
    }

    private void SpawnDeathShockwave(ulong killerClientId)
    {
        if (string.IsNullOrEmpty(deathShockwavePrefabId) || ClientGameObjectPool.Instance == null)
        {
            Debug.LogWarning($"[ClientFairyHealth on {gameObject.name}] Cannot spawn death shockwave. PrefabID: {deathShockwavePrefabId}, Pool: {ClientGameObjectPool.Instance?.name}");
            return;
        }

        GameObject shockwaveInstance = ClientGameObjectPool.Instance.GetObject(deathShockwavePrefabId);
        if (shockwaveInstance != null)
        {
            shockwaveInstance.transform.position = transform.position;
            shockwaveInstance.SetActive(true); // Activate before getting component?

            ClientFairyShockwave shockwaveScript = shockwaveInstance.GetComponent<ClientFairyShockwave>();
            if (shockwaveScript != null)
            {
                shockwaveScript.Initialize(
                    0.1f, 
                    deathShockwaveMaxRadius, // Visual radius
                    deathShockwaveEffectiveMaxRadius, // Effective radius
                    deathShockwaveDuration,
                    null, 
                    deathShockwaveDamage,
                    killerClientId, 
                    this.OwningPlayerRole,
                    true // canSpawnCounterBullets
                );
                // shockwaveInstance.SetActive(true); // Already set active?
            }
            else
            {
                Debug.LogError($"[ClientFairyHealth on {gameObject.name}] Spawned shockwave prefab '{deathShockwavePrefabId}' is missing the ClientFairyShockwave script! Returning to pool.");
                ClientGameObjectPool.Instance.ReturnObject(shockwaveInstance);
            }
        }
        else
        {
            Debug.LogWarning($"[ClientFairyHealth on {gameObject.name}] Failed to get shockwave '{deathShockwavePrefabId}' from pool.");
        }
    }

    /// <summary>
    /// Public method to allow external systems (like the spellcard clear RPC) 
    /// to force this fairy back to the pool.
    /// </summary>
    public void ForceReturnToPool()
    {
        // Currently, just directly calls the private return method.
        // Could add specific effects here later if needed.
        ReturnToPool();
    }

    private IEnumerator DelayedReturnToPool(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // Implementation of ReturnToPool method
        // This method should be implemented to return the fairy to the pool
        // and handle any necessary cleanup or effects.
        // Destroy(gameObject); // Fallback if pool is missing

        // ADDED: Actual pooling logic
        if (ClientGameObjectPool.Instance != null)
        {
            ClientGameObjectPool.Instance.ReturnObject(this.gameObject);
        }
        else
        {
            Debug.LogWarning($"[ClientFairyHealth] ClientGameObjectPool instance missing. Destroying {gameObject.name} instead.", this);
            Destroy(gameObject); // Fallback
        }
    }

    // --- Damage Flash Logic ---

    private void FlashRed()
    {
        if (_spriteRenderer == null || !gameObject.activeInHierarchy) return; // Don't flash if no renderer or disabled

        // Stop any existing flash coroutine
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            // Ensure color is reset if interrupted
             _spriteRenderer.color = Color.white; 
        }

        // Start a new flash coroutine
        _flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        Color originalColor = Color.white; // Assuming base color is white
        Color targetFlashColor = Color.Lerp(originalColor, _flashColor, _flashIntensity);

        // Set to tinted flash color instantly
        _spriteRenderer.color = targetFlashColor;

        // Smoothly fade back to white over the flash duration
        float elapsedTime = 0f;

        while (elapsedTime < _flashDuration)
        {
            if (_spriteRenderer == null) // Object might have been destroyed/disabled
            {
                _flashCoroutine = null;
                yield break; 
            }
            // Lerp from the tinted color back to original
            _spriteRenderer.color = Color.Lerp(targetFlashColor, originalColor, elapsedTime / _flashDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure it ends exactly on the original color
        if (_spriteRenderer != null) 
        {
            _spriteRenderer.color = originalColor;
        }

        // Coroutine finished
        _flashCoroutine = null;
    }
} 