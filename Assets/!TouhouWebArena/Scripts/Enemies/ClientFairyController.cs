using UnityEngine;

// Assuming PooledObjectInfo, SplineWalker, ClientGameObjectPool, ClientFairyHealth are accessible.
// Add using statements for their namespaces if necessary.
// e.g., using TouhouWebArena.ObjectPooling;
// e.g., using TouhouWebArena.Utilities;

[RequireComponent(typeof(PooledObjectInfo))]
[RequireComponent(typeof(SplineWalker))]
[RequireComponent(typeof(ClientFairyHealth))] // Added health component dependency
[RequireComponent(typeof(Collider2D))] // For OnTriggerEnter2D
public class ClientFairyController : MonoBehaviour
{
    private PooledObjectInfo _pooledObjectInfo;
    private SplineWalker _splineWalker;
    private ClientFairyHealth _clientFairyHealth;
    private Collider2D _collider;

    // To identify player shots. Could be a tag, a layer, or a specific component.
    private const string PLAYER_SHOT_TAG = "PlayerShot"; // Example tag

    void Awake()
    {
        _pooledObjectInfo = GetComponent<PooledObjectInfo>();
        _splineWalker = GetComponent<SplineWalker>();
        _clientFairyHealth = GetComponent<ClientFairyHealth>();
        _collider = GetComponent<Collider2D>();

        if (_pooledObjectInfo == null) Debug.LogError("[ClientFairyController] Missing PooledObjectInfo!", this);
        if (_splineWalker == null) Debug.LogError("[ClientFairyController] Missing SplineWalker!", this);
        if (_clientFairyHealth == null) Debug.LogError("[ClientFairyController] Missing ClientFairyHealth!", this);
        if (_collider == null)
        {
            Debug.LogError("[ClientFairyController] Missing Collider2D! Cannot detect collisions.", this);
        }
        else
        {
            // Ensure collider is set to trigger if we're using OnTriggerEnter2D
            if (!_collider.isTrigger)
            {
                Debug.LogWarning($"[ClientFairyController] Collider2D on {gameObject.name} is not set to IsTrigger. Setting it now. Please check prefab.", this);
                _collider.isTrigger = true;
            }
        }
    }

    void OnEnable()
    {
        if (_splineWalker != null)
        {
            _splineWalker.OnPathCompleted.AddListener(HandlePathCompleted);
        }
        if (_clientFairyHealth != null)
        {
            _clientFairyHealth.OnClientDeath += HandleFairyDeath;
        }
        // Reset any other state if necessary when re-enabled from pool
        if (_collider != null) _collider.enabled = true; // Ensure collider is active
    }

    void OnDisable()
    {
        if (_splineWalker != null)
        {
            _splineWalker.OnPathCompleted.RemoveListener(HandlePathCompleted);
        }
        if (_clientFairyHealth != null)
        {
            _clientFairyHealth.OnClientDeath -= HandleFairyDeath;
        }
    }

    private void HandlePathCompleted()
    {
        ReturnToPool(false); // Pass false for playerKill
    }

    private void HandleFairyDeath(GameObject मृतFairy)
    {
        // This event is now just signalling death occurred. 
        // The actual trigger check and RPC call happens when TakeDamage leads to death after a collision.
        // We still need to return the object to the pool.
        
        // We need a slightly different approach. TakeDamage doesn't know *why* it was called.
        // Let's move the 'kill report' logic into OnTriggerEnter2D

        // TODO: Play local death visual/audio effects
        // Debug.Log($"Fairy {gameObject.name} death event received. PrefabID: {_pooledObjectInfo?.PrefabID}");

        // Just return to pool when the health component signals death.
        // The RPC is sent *before* this, during the collision.
        ReturnToPool(false); // Mark as false, RPC handled separately
    }

    private void ReturnToPool(bool playerKill)
    {
        // Disable collider immediately to prevent further interactions
        if (_collider != null) _collider.enabled = false;

        // Optionally play different effects based on playerKill?
        // if (playerKill) { /* Play player kill effect */ } else { /* Play path end effect */ }

        if (ClientGameObjectPool.Instance != null)
        {
            ClientGameObjectPool.Instance.ReturnObject(this.gameObject);
        }
        else
        {
            Debug.LogWarning("[ClientFairyController] Pool null. Destroying.", this);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_clientFairyHealth.IsAlive) return; 

        if (other.CompareTag(PLAYER_SHOT_TAG))
        {
            BulletMovement bullet = other.GetComponent<BulletMovement>();
            if (bullet != null)
            {
                // Damage the fairy, passing the bullet owner's ID
                // ClientFairyHealth will handle the conditional kill reporting internally.
                _clientFairyHealth.TakeDamage(1, bullet.FiredByOwnerClientId); // Assuming 1 damage

                // Deactivate the bullet locally since it hit.
                // The bullet's own lifetime/collision logic might also handle this, 
                // but doing it here ensures it disappears immediately from this fairy's perspective.
                // BulletMovement's OnTriggerEnter2D already deactivates itself and returns to pool.
                // So, we might not strictly need to do other.gameObject.SetActive(false) here IF
                // BulletMovement's collision runs first or reliably for this interaction.
                // However, for safety and immediate visual feedback, explicit deactivation can be kept.
                // other.gameObject.SetActive(false); // This is likely redundant if BulletMovement handles it.
            }
            else
            {
                Debug.LogWarning($"[ClientFairyController] Collided with {PLAYER_SHOT_TAG} but it had no BulletMovement component.", other.gameObject);
            }
        }
    }

    // Example: Basic initialization method that could be called by ClientFairySpawner
    // public void Initialize(bool isGreatFairy, int health, float moveSpeed)
    // {
    //     if (_clientFairyHealth != null) _clientFairyHealth.SetMaxHealth(health, true);
    //     if (_splineWalker != null) _splineWalker.moveSpeed = moveSpeed; 
    // }
} 