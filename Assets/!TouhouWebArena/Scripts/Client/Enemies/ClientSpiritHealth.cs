using UnityEngine;
using Unity.Netcode;

// namespace TouhouWebArena.Client.Enemies // Global for now
// {
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
        [SerializeField] private float normalSpiritShockwaveMaxRadius = 1.5f;
        [SerializeField] private float activatedSpiritShockwaveMaxRadius = 3.0f;
        [SerializeField] private float shockwaveDuration = 0.5f;
        [SerializeField] private int shockwaveDamage = 3;
        [SerializeField] private float shockwaveInitialRadius = 0.1f;

        // private ClientSpiritController _spiritController; // Optional, if health needs to tell controller something on death
        private PooledObjectInfo _pooledObjectInfo; // To get PrefabID for logging or other purposes

        void Awake()
        {
            // _spiritController = GetComponent<ClientSpiritController>();
            _pooledObjectInfo = GetComponent<PooledObjectInfo>();
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

        public void TakeDamage(int amount, ulong attackerOwnerClientId)
        {
            if (!_isInitialized || _currentHealth <= 0) return;

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

            GameObject shockwaveInstance = ClientGameObjectPool.Instance.GetObject(shockwavePrefabId);
            if (shockwaveInstance != null)
            {
                shockwaveInstance.transform.position = transform.position;
                shockwaveInstance.SetActive(true);

                ClientFairyShockwave shockwaveScript = shockwaveInstance.GetComponent<ClientFairyShockwave>();
                if (shockwaveScript != null)
                {
                    float maxRadius = _isActivated ? activatedSpiritShockwaveMaxRadius : normalSpiritShockwaveMaxRadius;
                    shockwaveScript.Initialize(
                        shockwaveInitialRadius, 
                        maxRadius,
                        shockwaveDuration,
                        null,
                        shockwaveDamage,
                        killerClientId 
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
        }
    }
// } 