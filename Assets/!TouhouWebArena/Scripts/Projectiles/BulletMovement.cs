using UnityEngine;
// using Unity.Netcode; // REMOVED
// using TouhouWebArena.Spellcards; // REMOVED if IllusionHealth is not used client-side directly by this script

// [RequireComponent(typeof(NetworkObject))] // REMOVED
// [RequireComponent(typeof(PoolableObjectIdentity))] // REMOVED - Will use PooledObjectInfo for ClientGameObjectPool

/// <summary>
/// Handles client-side movement, lifetime, and collision for basic player projectiles.
/// </summary>
public class BulletMovement : MonoBehaviour // CHANGED from NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 10f; 
    // bulletLifetime will now be handled by ClientProjectileLifetime component

    // public NetworkVariable<PlayerRole> OwnerRole { get; private set; } = ... // REMOVED

    // private bool isDespawning = false; // REMOVED - Lifetime component will handle this

    private ClientProjectileLifetime _projectileLifetime;

    void Awake()
    {
        _projectileLifetime = GetComponent<ClientProjectileLifetime>();
        if (_projectileLifetime == null)
        {
            Debug.LogError("[BulletMovement] ClientProjectileLifetime component not found! Bullet will not despawn correctly.", gameObject);
        }
    }

    // OnNetworkSpawn, OnNetworkDespawn, OnDisable related to server invoke and IsServer checks are REMOVED
    // OnEnable can be used to reset state if needed when taken from pool.
    void OnEnable()
    {
        // Ensure Initialize is called after this to set speed if it can vary.
        // If moveSpeed is always constant from the prefab, this is less critical.
    }

    public void Initialize(float speed, float lifetime) // Added lifetime parameter
    {
        this.moveSpeed = speed;
        if (_projectileLifetime != null)
        {
            _projectileLifetime.Initialize(lifetime);
        }
    }

    void Update()
    {
        // Client-side movement
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.Self);
    }

    // Client-side collision detection
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check for collision with Fairy or Spirit tags
        if (other.CompareTag("Fairy") || other.CompareTag("Spirit")) 
        {
            // Debug.Log($"[BulletMovement] Client-side collision with {other.name} (Tag: {other.tag})");
            if (_projectileLifetime != null)
            {
                _projectileLifetime.ForceReturnToPool(); // Return bullet to pool on hit
            }
            else
            {
                // Fallback if lifetime component is missing for some reason
                gameObject.SetActive(false); 
            }
            // Note: Actual damage dealing or enemy destruction would happen elsewhere, 
            // or the client would send an RPC to the server if hits need to be validated/processed server-side.
            // For now, the bullet just disappears on hit.
        }
        // Removed checks for "Enemy", "OpponentHitbox", "WorldBoundary"
    }

    // ReturnToPool method is REMOVED - ClientProjectileLifetime handles returning to ClientGameObjectPool
    // DespawnBullet method is REMOVED
} 