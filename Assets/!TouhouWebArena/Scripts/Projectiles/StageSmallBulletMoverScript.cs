using UnityEngine;
using TouhouWebArena; // For PlayerRole and IClearable
// Removed: using Unity.Netcode;
// Removed: using TouhouWebArena; // No longer using IClearable or PlayerRole directly from here in the client-side version

// Consider renaming this script to ClientStageBulletMovement or similar for clarity.
// Removed: [RequireComponent(typeof(PoolableObjectIdentity))] // Will use PooledObjectInfo from prefab directly
/// <summary>
/// [Client-Side] Controls the movement and behavior of small and large stage bullets.
/// Handles movement based on an initial velocity, lifetime management, and returning to ClientGameObjectPool.
/// Implements IClearable for server-side spellcard clears and potentially other clearing mechanisms.
/// Designed to be pooled.
/// </summary>
public class StageSmallBulletMoverScript : MonoBehaviour, IClearable
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
    private PlayerRole _owningPlayerRole = PlayerRole.None; // Added field

    // For color reset
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // --- Public getters for properties that might be needed by the spawner ---
    public float DefaultSpeed => defaultSpeed;
    public float MaxLifetime => maxLifetime;
    public PlayerRole OwningPlayerRole => _owningPlayerRole; // Added getter
    // -----------------------------------------------------------------------

    void Awake() // Added Awake to get SpriteRenderer and originalColor
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            Debug.LogWarning($"[StageSmallBulletMoverScript] SpriteRenderer component not found on {gameObject.name}. Color reset will not work.", this);
        }
    }

    void OnEnable()
    {
        // Reset state when enabled from pool
        _isReturningToPool = false;
        // _owningPlayerRole = PlayerRole.None; // REMOVED: Role should be set by Initialize/InitializeOwnerRole, not reset here.
        // _currentLifetimeRemaining will be set by Initialize
        // _currentVelocity will be set by Initialize
    }

    void OnDisable()
    {
        _isReturningToPool = false; // Reset flag when disabled

        // Reset color to original
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// Initializes the bullet's movement direction, speed, lifetime, and owning role.
    /// Called by the spawning system (e.g., PlayerAttackRelay) after getting from pool.
    /// </summary>
    /// <param name="initialDirection">The normalized direction the bullet should travel.</param>
    /// <param name="speed">The speed of the bullet. If 0 or less, uses DefaultSpeed.</param>
    /// <param name="lifetime">The duration the bullet should exist. If 0 or less, uses MaxLifetime.</param>
    /// <param name="ownerRole">The PlayerRole this bullet belongs to.</param>
    public void Initialize(Vector3 initialDirection, float speed, float lifetime, PlayerRole ownerRole)
    {
        float actualSpeed = speed > 0 ? speed : defaultSpeed;
        _currentVelocity = initialDirection.normalized * actualSpeed;
        _currentLifetimeRemaining = lifetime > 0 ? lifetime : maxLifetime;
        _owningPlayerRole = ownerRole; // Store role
        _isReturningToPool = false; // Ensure it's ready to go
    }

    /// <summary>
    /// Initializes only the owning player role for this bullet.
    /// Used when other components (like specific spellcard behaviors) handle movement and lifetime initialization.
    /// </summary>
    /// <param name="ownerRole">The PlayerRole this bullet belongs to.</param>
    public void InitializeOwnerRole(PlayerRole ownerRole)
    {
        _owningPlayerRole = ownerRole;
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

        // Check for collision with StageWalls
        if (other.gameObject.layer == LayerMask.NameToLayer("StageWalls"))
        {
            Debug.Log($"[StageSmallBulletMoverScript] Hit StageWalls, returning {gameObject.name} to pool.");
            ReturnToClientPool();
            return; // Exit after hitting a wall
        }

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

    // --- IClearable Implementation ---
    /// <summary>
    /// Handles requests to clear this bullet, typically from server-side effects like spellcard activation
    /// or client-side effects like shockwaves (though client-side might use OnTriggerEnter2D directly).
    /// </summary>
    /// <param name="force">If true, the bullet is cleared regardless of isNormallyClearable.</param>
    /// <param name="clearingPlayerRole">The role of the player initiating the clear (used by server checks).</param>
    /// <returns>True if the bullet was cleared, false otherwise.</returns>
    public bool Clear(bool force, PlayerRole clearingPlayerRole)
    {
        if (_isReturningToPool) return false; // Already being returned

        // DEBUG: Log parameters and outcome
        bool cleared = false;
        if (force)
        {
            // Debug.Log($"{gameObject.name} force-cleared by role {clearingPlayerRole}.");
            ReturnToClientPool();
            cleared = true;
        }
        else if (isNormallyClearable)
        {
            // Debug.Log($"{gameObject.name} normally cleared by role {clearingPlayerRole}.");
            ReturnToClientPool();
            cleared = true;
        }
        else
        {
            // Debug.Log($"{gameObject.name} NOT cleared by role {clearingPlayerRole} (not normally clearable).");
            // cleared remains false
        }
        Debug.Log($"[StageSmallBulletMoverScript.Clear] Bullet: {gameObject.name}, Role: {OwningPlayerRole}, CasterRoleForClear: {clearingPlayerRole}, Force: {force}, isNormallyClearable: {isNormallyClearable}, Cleared: {cleared}", gameObject);
        return cleared;
    }
    // ----------------------------------
} 