using UnityEngine;
// Removed: using Unity.Netcode;
// Removed: using TouhouWebArena; // No longer using IClearable or PlayerRole directly from here in the client-side version

// Consider renaming this script to ClientStageBulletMovement or similar for clarity.
// Removed: [RequireComponent(typeof(PoolableObjectIdentity))] // Will use PooledObjectInfo from prefab directly
/// <summary>
/// [Client-Side] Controls the movement and behavior of small and large stage bullets.
/// Handles movement based on an initial velocity, lifetime management, and returning to ClientGameObjectPool.
/// Designed to be pooled.
/// </summary>
public class StageSmallBulletMoverScript : MonoBehaviour // Changed from NetworkBehaviour, Removed IClearable for now
{
    [Header("Movement & Lifetime")]
    [SerializeField] private float defaultSpeed = 3f; // Used if no specific speed is provided during initialization
    [SerializeField] private float maxLifetime = 15f;

    [Header("Behavior")]
    [Tooltip("Can this bullet be cleared by standard shockwaves?")]
    [SerializeField] private bool isNormallyClearable = true;

    private Vector3 _currentVelocity;
    private float _currentLifetimeRemaining;
    private bool _isReturningToPool = false;

    // --- Public getters for properties that might be needed by the spawner ---
    public float DefaultSpeed => defaultSpeed;
    public float MaxLifetime => maxLifetime;
    // -----------------------------------------------------------------------

    void OnEnable()
    {
        // Reset state when enabled from pool
        _isReturningToPool = false;
        // _currentLifetimeRemaining will be set by Initialize
        // _currentVelocity will be set by Initialize
    }

    void OnDisable()
    {
        _isReturningToPool = false; // Reset flag when disabled
    }

    /// <summary>
    /// Initializes the bullet's movement direction, speed, and lifetime.
    /// Called by the spawning system (e.g., PlayerAttackRelay) after getting from pool.
    /// </summary>
    /// <param name="initialDirection">The normalized direction the bullet should travel.</param>
    /// <param name="speed">The speed of the bullet. If 0 or less, uses DefaultSpeed.</param>
    /// <param name="lifetime">The duration the bullet should exist. If 0 or less, uses MaxLifetime.</param>
    public void Initialize(Vector3 initialDirection, float speed, float lifetime)
    {
        float actualSpeed = speed > 0 ? speed : defaultSpeed;
        _currentVelocity = initialDirection.normalized * actualSpeed;
        _currentLifetimeRemaining = lifetime > 0 ? lifetime : maxLifetime;
        _isReturningToPool = false; // Ensure it's ready to go
    }

    private void Update()
    {
        if (_isReturningToPool) return;

        transform.Translate(_currentVelocity * Time.deltaTime, Space.World);

        _currentLifetimeRemaining -= Time.deltaTime;
        if (_currentLifetimeRemaining <= 0f)
        {
            ReturnToClientPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isReturningToPool) return;

        // Example: Client-side shockwave clearing
        if (other.CompareTag("FairyShockwave")) // Assuming shockwaves are client-side and tagged
        {
            if (isNormallyClearable)
            {
                // TODO: Maybe play a clear visual/sound effect
                ReturnToClientPool();
            }
        }
        // Example: Collision with player (if these bullets can hit the player)
        // if (other.CompareTag("Player")) 
        // {
        //     // TODO: Handle player hit logic (e.g., notify player health script)
        //     ReturnToClientPool(); // Return bullet after hitting player
        // }
    }

    private void ReturnToClientPool()
    {
        if (_isReturningToPool) return;
        _isReturningToPool = true;

        if (ClientGameObjectPool.Instance != null)
        {
            ClientGameObjectPool.Instance.ReturnObject(this.gameObject);
        }
        else
        {
            Debug.LogWarning($"[StageSmallBulletMoverScript] ClientGameObjectPool instance missing. Destroying {gameObject.name} instead.", this);
            Destroy(gameObject); // Fallback
        }
    }

    /// <summary>
    /// Public method to allow external systems (like a bomb) to force this bullet back to the pool.
    /// </summary>
    public void ForceReturnToPoolByBomb()
    {
        // TODO: Optionally play a specific "cleared by bomb" visual/audio effect here first
        // Debug.Log($"{gameObject.name} being force returned by bomb.");
        ReturnToClientPool();
    }

    // --- IClearable implementation removed for this client-side version ---
    // The old Clear() method was server-authoritative.
    // Client-side clearing will be simpler, based on direct collision or specific client events.
} 