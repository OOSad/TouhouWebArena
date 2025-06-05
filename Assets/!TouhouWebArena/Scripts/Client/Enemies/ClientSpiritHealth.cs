using UnityEngine;
using Unity.Netcode;
using System.Collections; // ADDED for coroutine

// namespace TouhouWebArena.Client.Enemies // Global for now
// {
    [RequireComponent(typeof(AudioSource))] // Ensure AudioSource is present for death sounds
    public class ClientSpiritHealth : MonoBehaviour
    {
        private int _currentHealth;
        private bool _isInitialized = false;
        private bool _isActivated = false;

        // Constants for HP values
        private const int NORMAL_SPIRIT_HP = 5;
        private const int ACTIVATED_SPIRIT_HP = 1;

        [Header("Shockwave Settings")]
        [SerializeField] private string shockwavePrefabId = "FairyShockwave";
        [Tooltip("VISUAL max radius for normal spirit death shockwave.")]
        [SerializeField] private float normalSpiritShockwaveMaxRadius = 1.5f;
        [Tooltip("VISUAL max radius for activated spirit death shockwave.")]
        [SerializeField] private float activatedSpiritShockwaveMaxRadius = 3.0f;
        [Tooltip("EFFECTIVE max radius (damage/clear) for normal spirit death shockwave.")]
        [SerializeField] private float normalSpiritShockwaveEffectiveMaxRadius = 1.5f; // Default to visual
        [Tooltip("EFFECTIVE max radius (damage/clear) for activated spirit death shockwave.")]
        [SerializeField] private float activatedSpiritShockwaveEffectiveMaxRadius = 3.0f; // Default to visual
        [SerializeField] private float shockwaveDuration = 0.5f;
        [SerializeField] private int shockwaveDamage = 3;
        [SerializeField] private float shockwaveInitialRadius = 0.1f;

        [Header("Damage Flash")][Tooltip("The color the sprite flashes when taking damage.")]
        [SerializeField] private Color _flashColor = Color.red;
        [Tooltip("How long the flash color lasts in seconds.")]
        [SerializeField] private float _flashDuration = 0.1f;
        [Tooltip("How strong the flash color tint is (0=no tint, 1=full color).")]
        [Range(0f, 1f)] [SerializeField] private float _flashIntensity = 0.5f;

        [Header("Sound Settings")] // New header for sound fields
        public AudioClip enemyDefeatedSound;

        private ClientSpiritController _spiritController; // ADDED: Reference to controller for visuals
        private PooledObjectInfo _pooledObjectInfo; // To get PrefabID for logging or other purposes
        private Coroutine _flashCoroutine;
        private AudioSource audioSource; // Cached AudioSource component

        void Awake()
        {
            _spiritController = GetComponent<ClientSpiritController>(); // ADDED: Cache controller
            _pooledObjectInfo = GetComponent<PooledObjectInfo>();

            if (_spiritController == null)
            {
                Debug.LogError($"[ClientSpiritHealth] ClientSpiritController component not found on {gameObject.name}! Flash effect will not work.");
            }

            // Get and configure AudioSource for death sounds
            audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 1.0f; // Make it a 3D sound
            }
            else
            {
                Debug.LogError($"[ClientSpiritHealth on {gameObject.name}] AudioSource component not found despite RequireComponent attribute!");
            }
        }

        public void Initialize(int spiritType) // spiritType can determine initial HP
        {
            // For now, assume spiritType 0 is normal, others might be special
            // Or, just use a fixed HP and let ActivateSpirit change it.
            _currentHealth = NORMAL_SPIRIT_HP; 
            _isActivated = false;
            _isInitialized = true;
            // Debug.Log($"[ClientSpiritHealth] Initialized with {_currentHealth} HP for type {spiritType}", this);
        }

        public bool IsAlive()
        {
            return _isInitialized && _currentHealth > 0;
        }

        public void TakeDamage(int amount, ulong attackerOwnerClientId)
        {
            if (!_isInitialized || _currentHealth <= 0) return;

            // ADDED: Trigger flash effect
            FlashRed();

            _currentHealth -= amount;
            // Debug.Log($"[ClientSpiritHealth] Took {amount} damage, remaining HP: {_currentHealth}", this);

            if (_currentHealth <= 0)
            {
                Die(attackerOwnerClientId);
            }
        }

        public void OnActivated()
        {
            if (!_isInitialized) return;
            _currentHealth = ACTIVATED_SPIRIT_HP;
            _isActivated = true;
            Debug.Log($"[ClientSpiritHealth] Spirit Activated. HP set to {ACTIVATED_SPIRIT_HP}", this);
            // Potentially add a check: if HP was already below ACTIVATED_SPIRIT_HP, no change or handle as needed.
        }

        private void Die(ulong attackerOwnerClientId)
        {
            // Debug.Log($"[ClientSpiritHealth] Spirit died. Killed by client: {attackerOwnerClientId}", this);

            // Play defeat sound only if the local client was the attacker
            if (enemyDefeatedSound != null && attackerOwnerClientId == NetworkManager.Singleton.LocalClientId)
            {
                // Use PlayClipAtPoint to play sound independently of this GameObject's lifecycle
                float volume = (audioSource != null) ? audioSource.volume : 1.0f; // Use existing volume or default
                AudioSource.PlayClipAtPoint(enemyDefeatedSound, transform.position, volume);
            }

            SpawnDeathShockwave(attackerOwnerClientId);

            // Report kill to server if local player made the kill, for revenge spawn
            if (attackerOwnerClientId == NetworkManager.Singleton.LocalClientId)
            {
                // We need a way to report this. PlayerAttackRelay is for fairies.
                // We might need a new relay or add to existing.
                // For now, let's assume a new ServerRpc on PlayerAttackRelay or a dedicated SpiritAttackRelay.
                // Example: PlayerAttackRelay.Instance.ReportSpiritKillServerRpc(); // This needs to specify which player's spirit was killed to target opponent
                // Debug.Log($"[ClientSpiritHealth] Local player killed spirit. Reporting to server...", this);
                 if (PlayerAttackRelay.LocalInstance != null)
                 {
                    PlayerAttackRelay.LocalInstance.ReportSpiritKillServerRpc();
                 }
                 else
                 {
                    Debug.LogWarning("[ClientSpiritHealth] PlayerAttackRelay.LocalInstance is null. Cannot report spirit kill.", this);
                 }
            }

            // Return to pool
            if (_pooledObjectInfo != null) // Sanity check
            {
                ClientGameObjectPool.Instance.ReturnObject(this.gameObject);
            }
            else
            {
                Debug.LogError("[ClientSpiritHealth] PooledObjectInfo is null, cannot return to pool! Destroying instead.", this);
                Destroy(gameObject); // Fallback
            }
        }

        private void SpawnDeathShockwave(ulong killerClientId)
        {
            if (string.IsNullOrEmpty(shockwavePrefabId) || ClientGameObjectPool.Instance == null)
            {
                Debug.LogWarning($"[ClientSpiritHealth on {gameObject.name}] Cannot spawn death shockwave. PrefabID: {shockwavePrefabId}, Pool: {ClientGameObjectPool.Instance?.name}");
                return;
            }

            // Get OwningPlayerRole from ClientSpiritController
            PlayerRole spiritOwnerRole = PlayerRole.None;
            ClientSpiritController spiritController = GetComponent<ClientSpiritController>();
            if (spiritController != null) 
            {
                spiritOwnerRole = spiritController.OwningPlayerRole;
            }
            else
            {
                Debug.LogError($"[ClientSpiritHealth on {gameObject.name}] Cannot find ClientSpiritController to determine owner role for shockwave!");
                // Potentially default to a role or don't spawn, depending on desired behavior for this edge case
            }

            GameObject shockwaveInstance = ClientGameObjectPool.Instance.GetObject(shockwavePrefabId);
            if (shockwaveInstance != null)
            {
                shockwaveInstance.transform.position = transform.position;
                shockwaveInstance.SetActive(true);

                ClientFairyShockwave shockwaveScript = shockwaveInstance.GetComponent<ClientFairyShockwave>();
                if (shockwaveScript != null)
                {
                    float visualMaxRadius = _isActivated ? activatedSpiritShockwaveMaxRadius : normalSpiritShockwaveMaxRadius;
                    float effectiveMaxRadius = _isActivated ? activatedSpiritShockwaveEffectiveMaxRadius : normalSpiritShockwaveEffectiveMaxRadius;
                    shockwaveScript.Initialize(
                        shockwaveInitialRadius, 
                        visualMaxRadius,
                        effectiveMaxRadius,
                        shockwaveDuration,
                        null,
                        shockwaveDamage,
                        killerClientId,
                        spiritOwnerRole, // Pass the spirit's owner role
                        true // canSpawnCounterBullets
                    );
                }
                else
                {
                    Debug.LogError($"[ClientSpiritHealth on {gameObject.name}] Spawned shockwave prefab '{shockwavePrefabId}' is missing the ClientFairyShockwave script! Returning to pool.");
                    ClientGameObjectPool.Instance.ReturnObject(shockwaveInstance);
                }
            }
            else
            {
                Debug.LogWarning($"[ClientSpiritHealth on {gameObject.name}] Failed to get shockwave '{shockwavePrefabId}' from pool.");
            }
        }

        // Call this before returning to pool if not through Die() (e.g. timeout)
        public void ForceReturnToPool()
        {
            if (_pooledObjectInfo != null)
            {
                ClientGameObjectPool.Instance.ReturnObject(this.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnDisable()
        {
            // Reset state when returned to pool
            _isInitialized = false;
            _isActivated = false;
            _currentHealth = NORMAL_SPIRIT_HP; // Reset to default

            // ADDED: Ensure flash is stopped and color is reset
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            ResetVisualColor();
        }

        // --- Damage Flash Logic ---

        private void FlashRed()
        {
            if (_spiritController == null || !gameObject.activeInHierarchy) return;

            // Stop any existing flash coroutine
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                // Reset color before starting new flash
                ResetVisualColor(); 
            }

            // Start a new flash coroutine
            _flashCoroutine = StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            SpriteRenderer activeRenderer = GetActiveSpriteRenderer();
            if (activeRenderer != null)
            {
                Color originalColor = Color.white; // Assuming base color is white
                Color targetFlashColor = Color.Lerp(originalColor, _flashColor, _flashIntensity);
                
                // Set to tinted flash color instantly
                activeRenderer.color = targetFlashColor;

                // Smoothly fade back to white over the flash duration
                float elapsedTime = 0f;

                while (elapsedTime < _flashDuration)
                {
                    // Re-check renderer validity inside the loop
                    activeRenderer = GetActiveSpriteRenderer(); 
                    if (activeRenderer == null || !gameObject.activeInHierarchy) // Object might have been destroyed/disabled/switched visuals
                    {
                        _flashCoroutine = null;
                        yield break; 
                    }
                    // Lerp from the tinted color back to original
                    activeRenderer.color = Color.Lerp(targetFlashColor, originalColor, elapsedTime / _flashDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null; // Wait for the next frame
                }

                // Ensure it ends exactly on the original color
                ResetVisualColor(); // Use existing helper which checks renderer again
            }

            // Coroutine finished
            _flashCoroutine = null;
        }

        private SpriteRenderer GetActiveSpriteRenderer()
        {
            if (_spiritController == null) return null;

            GameObject activeVisual = _isActivated ? _spiritController.activatedSpiritVisual : _spiritController.normalSpiritVisual;
            if (activeVisual != null)
            {
                return activeVisual.GetComponentInChildren<SpriteRenderer>(); // Find renderer in the active visual's hierarchy
            }
            return null;
        }

        private void ResetVisualColor()
        {
             if (_spiritController == null || !gameObject.activeInHierarchy) return;
             SpriteRenderer activeRenderer = GetActiveSpriteRenderer();
             if (activeRenderer != null)
             {
                activeRenderer.color = Color.white; // Assuming default is white
             }
        }
    }
// } 