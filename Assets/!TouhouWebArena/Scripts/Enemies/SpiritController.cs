using UnityEngine;
using Unity.Netcode;
using TouhouWebArena; // Add namespace for IClearable and PlayerRole

/// <summary>
/// Controls the behavior of a Spirit collectible/enemy.
/// Spirits have two states: normal (drifting) and activated (slowly moving up, vulnerable).
/// Handles health, state transitions, movement, damage taking, death effects, timed despawning,
/// and interactions with player scopes and clearing effects via IClearable.
/// Designed to be pooled and requires several external references and prefabs.
/// </summary>
public class SpiritController : NetworkBehaviour, IClearable
{
    [Header("References")]
    /// <summary>Reference to the Rigidbody2D component for physics-based movement.</summary>
    [SerializeField] private Rigidbody2D rb;
    /// <summary>Reference to the main collider for the spirit body.</summary>
    [SerializeField] private CircleCollider2D bodyCollider; // Or other collider type
    /// <summary>Reference to the Spirit prefab asset itself, used for revenge spawns.</summary>
    [SerializeField] private GameObject spiritPrefabRef; // Assign Spirit prefab itself here
    // Potential reference to a bullet clearing component
    // [SerializeField] private BulletClearer bulletClearer;

    [Header("Visuals (Assign Children)")]
    /// <summary>The child GameObject holding visuals for the normal (unactivated) state.</summary>
    [SerializeField]
    [Tooltip("The child GameObject holding visuals for the normal state.")]
    private GameObject normalVisualObject;
    /// <summary>The child GameObject holding visuals for the activated state.</summary>
    [SerializeField]
    [Tooltip("The child GameObject holding visuals for the activated state.")]
    private GameObject activatedVisualObject;

    [Header("Movement")]
    /// <summary>The movement speed when in the normal (unactivated) state.</summary>
    [SerializeField] private float normalMoveSpeed = 2f;
    /// <summary>The upward movement speed when in the activated state.</summary>
    [SerializeField] private float activatedMoveSpeed = 1f;
    /// <summary>[Server Only] Flag set by the spawner indicating if the spirit should initially aim towards the player.</summary>
    private bool aimAtPlayerOnSpawn = false; // Set by spawner (only needed on server)

    [Header("Health")]
    /// <summary>The maximum health points when in the normal (unactivated) state.</summary>
    [SerializeField] private int normalMaxHp = 5;
    /// <summary>The maximum health points when in the activated state.</summary>
    [SerializeField] private int activatedMaxHp = 1;

    // --- Networked State ---
    /// <summary>[Server Write, Client Read] The current health points of the spirit.</summary>
    private NetworkVariable<int> currentHp = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>[Server Write, Client Read] Indicates if the spirit is currently in the activated state (true) or normal state (false).</summary>
    private NetworkVariable<bool> isActivated = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // -----------------------

    [Header("Death Effects")]
    /// <summary>Prefab for the visual effect spawned when the spirit dies in the normal state.</summary>
    [SerializeField] private GameObject normalDeathEffectPrefab;
    /// <summary>Prefab for the visual effect spawned when the spirit dies in the activated state.</summary>
    [SerializeField] private GameObject activatedDeathEffectPrefab;

    [Header("Lifetime")]
    /// <summary>Maximum duration in seconds the spirit can exist before being automatically despawned by the server.</summary>
    [SerializeField] private float maxLifetime = 15f; // Time in seconds before auto-despawn

    [Header("Timeout Behavior (Server Only)")]
    /// <summary>Prefab for the large bullet spawned when the spirit times out while activated.</summary>
    [SerializeField] private GameObject spiritLargeBulletPrefab; // Prefab to spawn on timeout (should have StageSmallBulletMoverScript)
    /// <summary>Duration in seconds the spirit stays activated before timing out and firing bullets.</summary>
    [SerializeField] private float activatedTimeoutDuration = 3.0f;
    /// <summary>Spread angle (degrees) for the side bullets fired during timeout.</summary>
    [SerializeField] private float bulletSpreadAngle = 15f; // Angle for side bullets

    [Header("Revenge Spawn (Server Only)")]
    /// <summary>Maximum number of spirits allowed per player side (checked during revenge spawns).</summary>
    [SerializeField] private int maxSpiritsPerSide = 10; // Max spirits allowed per side
    /// <summary>The size of the zone used for placing revenge-spawned spirits.</summary>
    [SerializeField] private Vector2 revengeSpawnZoneSize = new Vector2(7f, 1f); // How large is the spawn zone

    // --- Server-Side State ---
    /// <summary>[Server Only] Cached transform of the target player (if aiming).</summary>
    private Transform playerTransform; // Set by spawner (only needed on server)
    /// <summary>[Server Only] The role of the player whose side this spirit belongs to.</summary>
    private PlayerRole ownerRole = PlayerRole.None; // Which side this spirit belongs to
    /// <summary>[Server Only] Flag to prevent Die() logic from executing multiple times.</summary>
    private bool isDying = false; // Server-side flag to prevent multiple deaths
    /// <summary>[Server Only] Timer tracking the remaining lifetime of the spirit.</summary>
    private float currentLifetime; // Server-side timer
    /// <summary>[Server Only] Timer tracking how long the spirit has been in the activated state.</summary>
    private float activatedTimer = 0f; // Server-side timer for timeout
    /// <summary>[Server Only] Cached reference to Player 1's spirit spawn zone transform.</summary>
    private Transform player1SpawnZoneRef; // Passed in Initialize
    /// <summary>[Server Only] Cached reference to Player 2's spirit spawn zone transform.</summary>
    private Transform player2SpawnZoneRef; // Passed in Initialize
    // ------------------------

    // --- NEW: Public Getter for Owner Role ---
    /// <summary>
    /// Gets the <see cref="PlayerRole"/> indicating which player's side this spirit belongs to.
    /// </summary>
    /// <returns>The owning PlayerRole.</returns>
    public PlayerRole GetOwnerRole() 
    {
        return ownerRole;
    }
    // ----------------------------------------

    #region Initialization and Spawning

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to state changes to update visuals
        isActivated.OnValueChanged += OnActivationStateChanged;

        // Immediately update visuals on spawn based on the synchronized initial state
        UpdateVisuals(isActivated.Value);
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe to prevent errors
        isActivated.OnValueChanged -= OnActivationStateChanged;

        // --- Deregister from Registry (Server Only) --- 
        // This is a safety net; primary deregistration happens before despawn calls
        if (IsServer && SpiritRegistry.Instance != null)
        {
            SpiritRegistry.Instance.Deregister(this, ownerRole);
        }
        // -------------------------------------------

        base.OnNetworkDespawn();
    }

    // Called by the spawner ONLY ON SERVER after instantiation but before Spawn()
    /// <summary>
    /// [Server Only] Initializes the Spirit after being retrieved from a pool or instantiated.
    /// Sets initial state, health, owner, target player (optional), spawn zone references, and initial velocity.
    /// Registers the spirit with the <see cref="SpiritRegistry"/>.
    /// </summary>
    /// <param name="targetPlayer">The Transform of the player to initially aim at (can be null).</param>
    /// <param name="owner">The <see cref="PlayerRole"/> owning this spirit.</param>
    /// <param name="shouldAim">If true and targetPlayer is not null, the spirit will initially move towards the target player.</param>
    /// <param name="p1Zone">Reference to Player 1's spawn zone Transform (used for revenge spawns).</param>
    /// <param name="p2Zone">Reference to Player 2's spawn zone Transform (used for revenge spawns).</param>
    public void Initialize(Transform targetPlayer, PlayerRole owner, bool shouldAim, 
                         Transform p1Zone, Transform p2Zone) // Added spawn zone refs
    {
        if (!IsServer) return; // Should only be called on Server

        playerTransform = targetPlayer; // Can be null if not aiming
        ownerRole = owner; // Store the owner role
        aimAtPlayerOnSpawn = shouldAim;
        player1SpawnZoneRef = p1Zone;
        player2SpawnZoneRef = p2Zone;

        // Validate essential references passed in
        if (ownerRole == PlayerRole.None)
        {
            Debug.LogWarning($"[SpiritController] Initialized with PlayerRole.None!", this);
            // Consider warning or error
        }
        if (player1SpawnZoneRef == null || player2SpawnZoneRef == null)
        {
            Debug.LogError($"[SpiritController] Initialized with null spawn zone references!", this);
            // Consider warning or error
        }
        if (spiritPrefabRef == null) // Check prefab needed for revenge spawn
        {
             Debug.LogError($"[SpiritController] Initialized without Spirit Prefab Reference!", this);
            // Consider warning or error
        }

        // --- Reset State for Pooling --- 
        isDying = false; // CRITICAL: Reset dying flag
        currentHp.Value = normalMaxHp;
        isActivated.Value = false; // Explicitly set initial state
        activatedTimer = 0f; // Reset timer on init
        currentLifetime = maxLifetime; // Reset lifetime timer
        // -------------------------------        

        // Set initial velocity (server-side)
        SetInitialVelocity();

        // --- Register with Registry (Server Only) --- 
        if (IsServer) // Check IsServer here, although Initialize should only be called on server
        {
            if (SpiritRegistry.Instance != null)
            {
                SpiritRegistry.Instance.Register(this, ownerRole);
            }
            else
            {
                Debug.LogWarning($"[SpiritController] SpiritRegistry instance not found during initialization.", this);
                // Consider warning
            }
        }
        // ------------------------------------------
    }

    #endregion

    #region State Management and Visuals

    /// <summary>
    /// [Server Only] Changes the activation state of the spirit.
    /// Updates the <see cref="isActivated"/> NetworkVariable, adjusts health, and sets velocity.
    /// </summary>
    /// <param name="activate">True to activate the spirit, false to deactivate (currently unused).</param>
    private void ServerSetActivationState(bool activate)
    {
        if (!IsServer) return;
        if (isActivated.Value == activate) return; // Already in this state

        isActivated.Value = activate;

        if (activate)
        {
            // Update HP, capping at new max (server-side)
            currentHp.Value = Mathf.Min(currentHp.Value, activatedMaxHp);
            rb.velocity = Vector2.up * activatedMoveSpeed; // Move upwards (server-side)
            activatedTimer = 0f; // Reset timer when activated
        }
        else
        {
            // This case currently shouldn't happen as we don't deactivate
            currentHp.Value = normalMaxHp; // Reset HP?
            SetInitialVelocity(); // Reset velocity (server-side)
        }
    }

    /// <summary>
    /// Callback function triggered when the <see cref="isActivated"/> NetworkVariable changes value.
    /// Calls <see cref="UpdateVisuals"/> on all clients and the server.
    /// </summary>
    /// <param name="previousValue">The previous activation state.</param>
    /// <param name="newValue">The new activation state.</param>
    private void OnActivationStateChanged(bool previousValue, bool newValue)
    {
        UpdateVisuals(newValue);
    }

    /// <summary>
    /// Updates the visibility of the child GameObjects (<see cref="normalVisualObject"/>, <see cref="activatedVisualObject"/>)
    /// based on the current activation state.
    /// </summary>
    /// <param name="activated">The current activation state.</param>
    private void UpdateVisuals(bool activated)
    {
        if (normalVisualObject == null || activatedVisualObject == null)
        {
            Debug.LogError($"[SpiritController] Visual objects not assigned!", this);
            // Consider warning
            return;
        }

        // Activate/Deactivate based on state
        normalVisualObject.SetActive(!activated); 
        activatedVisualObject.SetActive(activated);
    }

    /// <summary>
    /// [Server Only] Sets the initial velocity of the spirit based on the <see cref="aimAtPlayerOnSpawn"/> flag.
    /// Either aims towards the cached <see cref="playerTransform"/> or defaults to moving straight down.
    /// </summary>
    private void SetInitialVelocity()
    {
        if (!IsServer) return;

        Vector2 initialVelocity;
        if (aimAtPlayerOnSpawn && playerTransform != null)
        {
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            initialVelocity = directionToPlayer * normalMoveSpeed;
        }
        else
        {
            initialVelocity = Vector2.down * normalMoveSpeed; // Default: move straight down
        }
        rb.velocity = initialVelocity;
    }

    #endregion

    #region Update Logic (Server Only)

    void FixedUpdate()
    {
        // Physics movement is handled by velocity set on Rigidbody2D (server only)
        // Client-side movement will be interpolated by NetworkTransform or similar
        if (!IsServer || isDying) return; 
        
        // --- Lifetime Check ---
        currentLifetime -= Time.fixedDeltaTime;
        if (currentLifetime <= 0f)
        {
            Die(PlayerRole.None); // Timeout counts as death without killer
            return; 
        }

        // --- Activated Timeout Check ---
        if (isActivated.Value)
        {
            activatedTimer += Time.fixedDeltaTime;
            if (activatedTimer >= activatedTimeoutDuration)
            {
                 HandleActivatedTimeout(); // Handle timeout logic
                return; // Prevent further processing after handling timeout (which calls Die)
            }
        }
    }

    #endregion

    #region Collision and Damage

    // Called when player scope enters the spirit's trigger collider
    /// <summary>
    /// Handles trigger enter events, specifically looking for Player Scope collisions.
    /// If a "PlayerScope" trigger enters and the spirit is not yet activated,
    /// calls <see cref="HandlePlayerScopeCollisionServerRpc"/> to initiate activation on the server.
    /// </summary>
    /// <param name="other">The Collider2D that entered the trigger.</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if it's the Player Scope and the spirit is NOT already activated
        if (!isActivated.Value && other.CompareTag("ScopeStyleZone"))
        {
            // Send an RPC to the server to handle the activation logic
            HandlePlayerScopeCollisionServerRpc();
        }
    }

    // ServerRpc called by a client, executed on the server
    [ServerRpc(RequireOwnership = false)] // Allow any client to trigger this
    private void HandlePlayerScopeCollisionServerRpc(ServerRpcParams rpcParams = default)
    {
        // Activate the spirit on the server
        if (!isActivated.Value) // Double check state on server
        {
            ServerSetActivationState(true);
        }
    }

    // Apply damage (called by Player Bullet or maybe other sources)
    /// <summary>
    /// [Server Only] Applies damage to the spirit.
    /// If damage reduces health to 0 or below, calls <see cref="Die"/>.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    /// <param name="killerRole">The <see cref="PlayerRole"/> credited with the kill if the damage is lethal.</param>
    public void ApplyDamageServer(int amount, PlayerRole killerRole)
    {
        if (!IsServer || isDying || currentHp.Value <= 0) return;

        currentHp.Value -= amount;

        if (currentHp.Value <= 0)
        {
            Die(killerRole);
        }
    }

    #endregion

    #region Death and Cleanup

    /// <summary>
    /// [Server Only] Handles the death sequence of the spirit.
    /// Sets the dying flag, spawns death effects, handles potential revenge spirit spawns (if killed by owner in normal state),
    /// deregisters from the <see cref="SpiritRegistry"/>, and returns the object to the pool via <see cref="NetworkObjectPool"/>.
    /// </summary>
    /// <param name="killerRole">The <see cref="PlayerRole"/> who caused the death, or <see cref="PlayerRole.None"/> if no specific killer (e.g., timeout).</param>
    private void Die(PlayerRole killerRole)
    {
        if (!IsServer || isDying) return;
        isDying = true; // Prevent multiple calls

        // --- Handle Death Effect Spawning --- 
        GameObject effectPrefab = isActivated.Value ? activatedDeathEffectPrefab : normalDeathEffectPrefab;
        if (effectPrefab != null)
        {
            // Instantiate and spawn the effect
            GameObject deathEffectInstance = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            NetworkObject effectNetworkObject = deathEffectInstance.GetComponent<NetworkObject>();
            if (effectNetworkObject != null)
            {
                effectNetworkObject.Spawn(true); // Spawn server-owned, destroy with scene
            }
            else
            {
                Debug.LogWarning($"[SpiritController] Death effect prefab '{effectPrefab.name}' is missing a NetworkObject component.", this);
                Destroy(deathEffectInstance); // Cleanup unspawnable effect
            }
        }
        else
        {
             Debug.LogWarning($"[SpiritController] Death effect prefab is null for state (Activated: {isActivated.Value}).", this);
        }


        // --- Simplified Spawn Logic: Always spawn a spirit on the opponent's side on death ---
        if (ownerRole != PlayerRole.None) // Ensure the dying spirit has a valid owner
        {
            PlayerRole opponentRole = (ownerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
            SpawnSpiritOnOpponentSide(opponentRole); // Pass OPPONENT's role
        }
        else
        {
            Debug.LogWarning($"[SpiritController] Spirit died with OwnerRole.None, cannot determine opponent to spawn spirit for.", this);
        }
        // -------------------------------------------------------------------------------------

        // --- Deregister from Registry --- 
        if (SpiritRegistry.Instance != null)
        {
            SpiritRegistry.Instance.Deregister(this, ownerRole); 
        }
        else
        {
            Debug.LogWarning("[SpiritController] SpiritRegistry instance not found during death.", this);
        }
        // --------------------------------

        // --- Despawn/Return to Pool --- 
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            // Despawn the object without destroying it
            NetworkObject.Despawn(false); 

            // Return the object to the pool
            if (NetworkObjectPool.Instance != null)
            {
                NetworkObjectPool.Instance.ReturnNetworkObject(NetworkObject);
            }
            else
            {
                Debug.LogWarning("[SpiritController] NetworkObjectPool instance not found when trying to return spirit. Destroying instead.", this);
                Destroy(gameObject); 
            }
        }
        else
        {
            // If not spawned or null, maybe just destroy?
            // Or maybe it was already being handled?
            // This case might indicate an issue if reached frequently.
            // If the object is already inactive/pooled, this is fine.
        }
        // -----------------------------
    }

    #endregion

    // --- Implementation of IClearable ---
    /// <summary>
    /// [Server Only] Handles the spirit being cleared by effects like PlayerDeathBomb or Shockwave.
    /// Always triggers the Die sequence regardless of the forceClear flag.
    /// </summary>
    /// <param name="forceClear">Flag indicating if the clear is forced (e.g., by a bomb). Ignored by this implementation; spirits always die when cleared.</param>
    /// <param name="sourceRole">The role of the player causing the clear (used for kill attribution).</param>
    public void Clear(bool forceClear, PlayerRole sourceRole)
    {
        if (!IsServer) return;
        Die(sourceRole); // Pass the clearer's role as the killer
    }
    // ------------------------------------

    #region Timeout and Revenge Logic (Server Only)

    /// <summary>
    /// [Server Only] Handles the logic when an activated spirit times out.
    /// Spawns three large bullets (one straight down, two angled) and then calls <see cref="Die"/>.
    /// </summary>
    private void HandleActivatedTimeout()
    {
        if (!IsServer) return;

        // Spawn 3 bullets: one down, two angled
        Vector3 spawnPos = transform.position; // Use spirit's current position

        // 1. Straight down
        SpawnTimeoutBullet(Vector3.down, spawnPos);

        // 2. Angled left
        Quaternion leftRotation = Quaternion.Euler(0, 0, bulletSpreadAngle);
        Vector3 leftDirection = leftRotation * Vector3.down;
        SpawnTimeoutBullet(leftDirection, spawnPos);

        // 3. Angled right
        Quaternion rightRotation = Quaternion.Euler(0, 0, -bulletSpreadAngle);
        Vector3 rightDirection = rightRotation * Vector3.down;
        SpawnTimeoutBullet(rightDirection, spawnPos);

        // After firing, the spirit dies (without a killer)
        Die(PlayerRole.None);
    }

    // Helper to spawn a bullet from the pool (assuming StageSmallBulletMoverScript is used)
    // --- REPLACED: Updated SpawnTimeoutBullet to use Instantiate directly ---
    /*
    private void SpawnTimeoutBulletFromPool(Vector3 direction, Vector3 spawnPosition)
    {
        // ... (Pool logic) ...
    }
    */
    // ----------------------------------------------------------------------

    // --- RENAMED and Updated for Clarity ---
    /// <summary>
    /// [Server Only] Instantiates and spawns a single timeout bullet (typically a large stage bullet)
    /// with a specified initial velocity and position. This method now directly instantiates the prefab.
    /// </summary>
    /// <param name="direction">The direction the bullet should travel.</param>
    /// <param name="spawnPosition">The world position where the bullet should spawn.</param>
    private void SpawnTimeoutBullet(Vector3 direction, Vector3 spawnPosition)
    {
        if (!IsServer) return;

        if (spiritLargeBulletPrefab == null)
        {
            Debug.LogError("[SpiritController] spiritLargeBulletPrefab is not assigned!", this);
            return;
        }

        // Calculate rotation to face the direction
        Quaternion bulletRotation = Quaternion.LookRotation(Vector3.forward, direction); // Use LookRotation for 2D up direction

        // Instantiate the bullet prefab
        GameObject bulletInstance = Instantiate(spiritLargeBulletPrefab, spawnPosition, bulletRotation);

        // Get the NetworkObject
        NetworkObject bulletNetworkObject = bulletInstance.GetComponent<NetworkObject>();
        if (bulletNetworkObject == null)
        {
            Debug.LogError($"[SpiritController] Timeout bullet prefab '{spiritLargeBulletPrefab.name}' is missing a NetworkObject component.", this);
            Destroy(bulletInstance);
            return;
        }

        // --- Configure Bullet (If needed) ---
        // Get the bullet script (assuming StageSmallBulletMoverScript)
        StageSmallBulletMoverScript bulletMover = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
        if (bulletMover != null)
        {
            // Spawn the bullet on the network FIRST
            bulletNetworkObject.Spawn(true); // Spawn server-owned

            // THEN set NetworkVariables *after* spawning
            bulletMover.UseInitialVelocity.Value = true;
            bulletMover.InitialVelocity.Value = direction * bulletMover.GetMinSpeed(); // Use min speed for consistency? Or add a field?
            bulletMover.TargetPlayerRole.Value = PlayerRole.None; // Timeout bullets aren't targeted? Or should they target owner? Check design.
        }
        else
        {
             Debug.LogWarning($"[SpiritController] Timeout bullet prefab '{spiritLargeBulletPrefab.name}' is missing StageSmallBulletMoverScript.", this);
            // Still spawn the object if it has a NetworkObject, but warn about missing script
            bulletNetworkObject.Spawn(true);
        }
        // --------------------------------------
    }
    // -------------------------------------------------

    // --- RENAMED from SpawnRevengeSpirit         ---
    /// <summary>
    /// [Server Only] Handles the spawning of a spirit on the opponent's side.
    /// Checks if the maximum spirit count for that side has been reached using <see cref="SpiritRegistry"/>.
    /// Calculates a random spawn position within the opponent's designated zone (using cached references)
    /// and obtains/initializes a spirit from the <see cref="NetworkObjectPool"/>.
    /// </summary>
    /// <param name="opponentRole">The role of the player who will own the newly spawned spirit.</param>
    private void SpawnSpiritOnOpponentSide(PlayerRole opponentRole) 
    {
        if (!IsServer || opponentRole == PlayerRole.None) return;

        if (spiritPrefabRef == null)
        {
            Debug.LogError("[SpiritController] SpawnOpponentSpirit: Spirit prefab reference is missing!", this);
            return;
        }
        if (SpiritRegistry.Instance == null)
        {
             Debug.LogWarning("[SpiritController] SpawnOpponentSpirit: SpiritRegistry instance not found.", this);
             return;
        }
        if (NetworkObjectPool.Instance == null)
        {
            Debug.LogError("[SpiritController] SpawnOpponentSpirit: NetworkObjectPool instance not found!", this);
            return;
        }


        // The new spirit is owned by the OPPONENT (passed in as opponentRole).
        PlayerRole newSpiritOwnerRole = opponentRole; 

        // Check max spirit count for the OPPONENT's side.
        int opponentSpiritCount = SpiritRegistry.Instance.GetSpiritCount(newSpiritOwnerRole);
        if (opponentSpiritCount >= maxSpiritsPerSide)
        {
             // Debug.Log($"[SpiritController] SpawnOpponentSpirit: Opponent {newSpiritOwnerRole} at max spirit capacity ({opponentSpiritCount}/{maxSpiritsPerSide}).");
             return; // Opponent is at max capacity
        }

        // Determine the OPPONENT's spawn zone.
        Transform opponentSpawnZone = (newSpiritOwnerRole == PlayerRole.Player1) ? player1SpawnZoneRef : player2SpawnZoneRef;
        if (opponentSpawnZone == null)
        {
            Debug.LogError($"[SpiritController] SpawnOpponentSpirit: Opponent ({newSpiritOwnerRole}) spawn zone reference is null!", this);
            return;
        }
        
        // Calculate random position within the OPPONENT's spawn zone.
        float spawnX = Random.Range(-revengeSpawnZoneSize.x / 2f, revengeSpawnZoneSize.x / 2f);
        float spawnY = Random.Range(-revengeSpawnZoneSize.y / 2f, revengeSpawnZoneSize.y / 2f);
        Vector3 spawnPosition = opponentSpawnZone.position + new Vector3(spawnX, spawnY, 0);

        // --- Pool Integration --- 
        PoolableObjectIdentity identity = spiritPrefabRef.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
            Debug.LogError($"[SpiritController] SpawnOpponentSpirit: Spirit prefab ref '{spiritPrefabRef.name}' missing identity/ID!", this);
            return;
        }
        string prefabID = identity.PrefabID;
        NetworkObject pooledNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);
        if (pooledNetworkObject == null)
        {
            Debug.LogError($"[SpiritController] SpawnOpponentSpirit: Failed to get Spirit '{prefabID}' from pool.", this);
            return;
        }
        // ----------------------

        // Get required components from pooled object
        SpiritController newSpiritController = pooledNetworkObject.GetComponent<SpiritController>();
        // NetworkObject newNetworkObject = pooledNetworkObject; // Already have reference

        if (newSpiritController == null)
        {
            Debug.LogError("[SpiritController] SpawnOpponentSpirit: Pooled object missing SpiritController! Returning to pool.", this);
            NetworkObjectPool.Instance.ReturnNetworkObject(pooledNetworkObject); // Return broken object
            return;
        }

        // Position and Activate pooled object
        pooledNetworkObject.transform.position = spawnPosition;
        pooledNetworkObject.transform.rotation = Quaternion.identity;
        pooledNetworkObject.gameObject.SetActive(true);

        // Find OPPONENT player transform (to potentially aim at, though we don't aim these spirits)
        Transform opponentPlayerTransform = null; // We don't aim these spirits
        
        // Spawn the new spirit on the network FIRST
        pooledNetworkObject.Spawn(false); // Spawn client-owned or server-owned based on pool config? Assume false for now.
        
        // Initialize the new spirit AFTER spawning
        newSpiritController.Initialize(opponentPlayerTransform, newSpiritOwnerRole, false, player1SpawnZoneRef, player2SpawnZoneRef); // Don't aim initially
    }
    // -------------------------------------------------
    
    #endregion
} 