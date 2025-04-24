using UnityEngine;
using Unity.Netcode;
using TouhouWebArena; // Add namespace for IClearable and PlayerRole

[RequireComponent(typeof(PoolableObjectIdentity))] // Ensure identity component exists
/// <summary>
/// Controls the movement and behavior of small and large stage bullets.
/// Handles random velocity calculation (or uses a set initial velocity),
/// network synchronization of velocity, lifetime management, pooling,
/// and interaction with clearing effects via the IClearable interface.
/// Designed to be pooled.
/// </summary>
public class StageSmallBulletMoverScript : NetworkBehaviour, IClearable // Implement IClearable
{
    [Header("Movement & Lifetime")] // Added header for clarity
    /// <summary>
    /// The minimum speed for randomly generated velocity.
    /// </summary>
    [SerializeField] private float minSpeed = 2f;
    /// <summary>
    /// The maximum speed for randomly generated velocity.
    /// </summary>
    [SerializeField] private float maxSpeed = 5f;
    /// <summary>
    /// Maximum deviation angle from straight down (in degrees) for random velocity generation.
    /// </summary>
    [SerializeField] private float maxAngleDeviation = 15f; 
    /// <summary>
    /// Maximum time in seconds before the bullet is automatically despawned by the server.
    /// </summary>
    [SerializeField] private float maxLifetime = 15f; // Seconds before the bullet despawns

    [Header("Behavior")] // Added header
    /// <summary>
    /// If true, this bullet will not be destroyed upon collision with a standard shockwave (e.g., for Large Stage Bullets).
    /// Used by the IClearable implementation.
    /// </summary>
    [SerializeField] 
    [Tooltip("Can this bullet be cleared by standard shockwaves (non-forced clears)?")]
    private bool isNormallyClearable = true; // Add field, default to true?
    // --- REMOVED isImmuneToShockwave, replaced by isNormallyClearable for the interface logic ---
    // [SerializeField] private bool isImmuneToShockwave = false;

    // --- Networked State --- 
    /// <summary>
    /// [Server Write, Client Read] The authoritative velocity vector calculated or set by the server.
    /// Used by the server to move the bullet and implicitly synced for client-side prediction/movement.
    /// </summary>
    private NetworkVariable<Vector3> SyncedVelocity = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);
    // NetworkVariable to store which player this bullet belongs to
    /// <summary>
    /// [Server Write, Client Read] The <see cref="PlayerRole"/> this bullet is targeting or associated with.
    /// Used for logic like bomb clearing.
    /// </summary>
    public NetworkVariable<PlayerRole> TargetPlayerRole { get; private set; } = new NetworkVariable<PlayerRole>(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>
    /// [Server Write, Client Read] If true, the bullet will use the <see cref="InitialVelocity"/> instead of calculating a random one.
    /// </summary>
    public NetworkVariable<bool> UseInitialVelocity { get; private set; } = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>
    /// [Server Write, Client Read] The specific velocity vector to use if <see cref="UseInitialVelocity"/> is true.
    /// </summary>
    public NetworkVariable<Vector3> InitialVelocity { get; private set; } = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // -----------------------

    /// <summary>Remaining time before the bullet automatically despawns (server-side timer).</summary>
    private float currentLifetime;
    /// <summary>Server-side flag to prevent ReturnToPool from being called multiple times in quick succession.</summary>
    private bool isReturning = false; // Flag to prevent double returns

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isReturning = false; // Reset flag

        // Movement logic and velocity calculation should only run on the server
        if (!IsServer) return;

        // --- Server-side Velocity Calculation/Setting --- 
        Vector3 calculatedVelocity;
        if (UseInitialVelocity.Value)
        {
            // Use the velocity provided by the spawner
            calculatedVelocity = InitialVelocity.Value;
        }
        else
        {
            // Calculate random speed
            float speed = Random.Range(minSpeed, maxSpeed);
            // Calculate random angle deviation
            float randomAngle = Random.Range(-maxAngleDeviation, maxAngleDeviation);
            // Calculate direction based on the angle
            Vector3 direction = Quaternion.Euler(0, 0, randomAngle) * Vector3.down;
            // Calculate the final velocity vector
            calculatedVelocity = direction.normalized * speed;
        }

        // Store the final velocity in the NetworkVariable for movement
        SyncedVelocity.Value = calculatedVelocity;
        // TargetPlayerRole should be set by the spawner *after* OnNetworkSpawn
        // --- End Server-side Calculation ---

        // Initialize lifetime timer on the server
        currentLifetime = maxLifetime;
    }

    // --- Add OnNetworkDespawn --- 
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isReturning = false; // Reset flag
    }
    // --------------------------

    // --- Add OnDisable --- 
    void OnDisable()
    {
        // Also reset flag when disabled (e.g., returned to pool)
        isReturning = false;
    }
    // ---------------------

    // --- Public getter for speed (used by SpiritController) ---
    /// <summary>
    /// Gets the minimum speed configured for this bullet type.
    /// </summary>
    /// <returns>The minimum speed.</returns>
    public float GetMinSpeed() { return minSpeed; }
    // ---------------------------------------------------------

    private void Update()
    {
        if (!IsServer || isReturning) return; // Ignore if not server or already returning

        // Move using the velocity stored in the NetworkVariable
        transform.Translate(SyncedVelocity.Value * Time.deltaTime, Space.World);

        // --- Lifetime Check (Server) ---
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0f)
        {
            ReturnToPool(); // Use the new method
            return; 
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || isReturning) return; // Ignore if not server or already returning

        // Check if the bullet collided with a shockwave
        if (other.CompareTag("FairyShockwave"))
        {
            // --- REMOVED Immunity Check Here - Now handled by IClearable --- 
            // if (isImmuneToShockwave) return;
            
            // Instead of directly returning, the shockwave will now call IClearable.Clear
            // on this object, which will handle the logic based on isNormallyClearable.
            // So, we remove the ReturnToPool call from here.
            // ReturnToPool(); 
        }
        // Potentially add other collision checks here that should return the bullet to the pool
        // (but clearing is now primarily handled via the interface)
    }

    // --- New ReturnToPool Method --- 
    private void ReturnToPool()
    {
        if (!IsServer || isReturning) return; // Should only run on server, prevent double calls

        isReturning = true;

        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn(false); // Despawn WITHOUT destroying

            // Return to the pool manager
            if (NetworkObjectPool.Instance != null)
            {
                NetworkObjectPool.Instance.ReturnNetworkObject(networkObject);
            }
            else
            {
                // Fallback if pool manager is gone
                Debug.LogWarning($"NetworkObjectPool instance missing when trying to return {gameObject.name}. Destroying instead.", gameObject);
                Destroy(gameObject);
            }
        }
        else
        {
            // If not spawned or null, just ensure flag is reset if somehow reached here
            isReturning = false; 
        }
    }
    // -----------------------------

    // --- Implementation of IClearable ---
    /// <summary>
    /// Called by effects like PlayerDeathBomb or Shockwave to clear this bullet.
    /// On the server, checks if the clear should happen based on forceClear and isNormallyClearable flags,
    /// then returns the bullet to the object pool if applicable.
    /// </summary>
    /// <param name="forceClear">If true, the bullet is cleared regardless of isNormallyClearable.</param>
    /// <param name="sourceRole">The role of the player causing the clear (ignored by this implementation).</param>
    public void Clear(bool forceClear, PlayerRole sourceRole)
    {
        // Clearing logic only runs on the server
        if (!IsServer || isReturning) return;

        // If it's a forced clear (player bomb) OR this bullet is normally clearable
        if (forceClear || isNormallyClearable)
        {
            // Reuse the existing pooling logic
            ReturnToPool();
        }
        // Else: Normal clear attempt on a bullet that is not normally clearable - do nothing.
    }
    // ------------------------------------
} 