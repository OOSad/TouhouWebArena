using UnityEngine;
using Unity.Netcode;
using TouhouWebArena; // Add namespace for IClearable and PlayerRole

/// <summary>
/// Acts as the main coordinator for a Spirit entity, handling its core state (health, activation), 
/// basic movement, lifetime tracking, damage processing, and interactions with player scopes and clearing effects via IClearable.
/// Delegates visual updates (<see cref="SpiritVisualController"/>), death effects (<see cref="SpiritDeathEffects"/>), 
/// and timeout attacks (<see cref="SpiritTimeoutAttack"/>) to specialized components.
/// Spirits have two states: normal (drifting) and activated (slowly moving up, vulnerable).
/// Handles health, state transitions, movement, damage taking, death effects, timed despawning,
/// and interactions with player scopes and clearing effects via IClearable.
/// Designed to be pooled. Requires references to sibling components for delegated behaviors.
/// </summary>
public class SpiritController : NetworkBehaviour, IClearable
{
    [Header("References")]
    /// <summary>Reference to the Rigidbody2D component for physics-based movement.</summary>
    [SerializeField] private Rigidbody2D rb;
    /// <summary>Reference to the main collider for the spirit body.</summary>
    [SerializeField] private CircleCollider2D bodyCollider; // Or other collider type
    /// <summary>Reference to the component responsible for spawning death visual effects.</summary>
    [Tooltip("Reference to the component responsible for spawning death visual effects.")]
    [SerializeField] private SpiritDeathEffects spiritDeathEffects;
    // Potential reference to a bullet clearing component
    // [SerializeField] private BulletClearer bulletClearer;
    // --- Add reference to the visual controller ---
    [Tooltip("Reference to the component that manages visual state changes.")]
    [SerializeField] private SpiritVisualController visualController;
    // ---------------------------------------------

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

    [Header("Lifetime")]
    /// <summary>Maximum duration in seconds the spirit can exist before being automatically despawned by the server.</summary>
    [SerializeField] private float maxLifetime = 15f; // Time in seconds before auto-despawn

    /// <summary>Duration in seconds the spirit stays activated before timing out and firing bullets.</summary>
    [SerializeField] private float activatedTimeoutDuration = 3.0f;

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
    // ------------------------

    // --- Add reference to the timeout attack component ---
    [Header("Component References")] // Add a header for component refs if desired
    [Tooltip("Reference to the component that executes the timeout attack pattern.")]
    [SerializeField] private SpiritTimeoutAttack spiritTimeoutAttack;
    // ---------------------------------------------------

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

        // Immediately update visuals via the controller based on the synchronized initial state
        if (visualController != null)
        {
            visualController.SetVisualState(isActivated.Value);
        }
        else
        {
            Debug.LogError("[SpiritController] VisualController reference is missing on NetworkSpawn!", this);
        }
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
    /// Sets initial state (health, owner, timers), target player for aiming (optional), and initial velocity.
    /// Registers the spirit with the <see cref="SpiritRegistry"/>.
    /// </summary>
    /// <param name="targetPlayer">The Transform of the player to initially aim at (can be null).</param>
    /// <param name="owner">The <see cref="PlayerRole"/> owning this spirit.</param>
    /// <param name="shouldAim">If true and targetPlayer is not null, the spirit will initially move towards the target player.</param>
    public void Initialize(Transform targetPlayer, PlayerRole owner, bool shouldAim)
    {
        if (!IsServer) return; // Should only be called on Server

        playerTransform = targetPlayer; // Can be null if not aiming
        ownerRole = owner; // Store the owner role
        aimAtPlayerOnSpawn = shouldAim;

        // Validate essential references passed in
        if (ownerRole == PlayerRole.None)
        {
            Debug.LogWarning($"[SpiritController] Initialized with PlayerRole.None!", this);
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
            rb.linearVelocity = Vector2.up * activatedMoveSpeed; // Move upwards (server-side)
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
        // Delegate visual update to the visual controller
        if (visualController != null)
        {
            visualController.SetVisualState(newValue);
        }
        // No else log here to prevent spam on state change if reference is missing
        // The error on spawn should be sufficient.
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
        rb.linearVelocity = initialVelocity;
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
                // --- Delegate timeout attack execution --- 
                if (spiritTimeoutAttack != null)
                {
                    spiritTimeoutAttack.ExecuteAttack(transform.position);
                }
                else
                {
                    Debug.LogError("[SpiritController] SpiritTimeoutAttack component reference is missing! Cannot execute timeout attack.", this);
                }
                // ---------------------------------------
                
                // After the attack executes, the spirit dies.
                Die(PlayerRole.None); 
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
    /// Sets the dying flag, calls the <see cref="SpiritDeathEffects"/> component for visuals, 
    /// triggers a potential revenge spawn via <see cref="SpiritSpawner"/> if killed by a player,
    /// deregisters from the <see cref="SpiritRegistry"/>, and returns the object to the pool via <see cref="NetworkObjectPool"/>.
    /// </summary>
    /// <param name="killerRole">The <see cref="PlayerRole"/> who caused the death, or <see cref="PlayerRole.None"/> if no specific killer (e.g., timeout).</param>
    private void Die(PlayerRole killerRole)
    {
        if (!IsServer || isDying) return;
        isDying = true;

        // --- Deregister from Registry FIRST ---
        // Ensure it's removed before potential reuse via revenge spawn
        if (SpiritRegistry.Instance != null)
        {
            SpiritRegistry.Instance.Deregister(this, ownerRole);
        }
        // -------------------------------------

        // Trigger visual effects
        spiritDeathEffects?.PlayDeathEffect(isActivated.Value, transform.position);

        // --- Revenge Spawn Logic (Corrected based on Rule) ---
        // Rule: If killed by *any* player, spawn one on the side OPPOSITE the spirit's owner.
        if (killerRole != PlayerRole.None)
        {
            // Determine the opponent of the OWNER
            PlayerRole opponentRole = (ownerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
            
            if (SpiritSpawner.Instance != null)
            {
                // Spawn FOR the opponent (meaning on their side)
                SpiritSpawner.Instance.SpawnRevengeSpirit(opponentRole); 
            }
            else
            {
                Debug.LogError("[SpiritController] SpiritSpawner instance is null! Cannot spawn revenge spirit.", this);
            }
        }
        // ---------------------------------------------------

        // --- Return to Pool --- 
        // Call the existing method that handles despawn and pooling
        ReturnToPool(); 
        // ----------------------
    }

    /// <summary>
    /// [Server Only] Handles timed despawning and returning the object to the pool.
    /// Cancels pending invokes, deregisters, despawns the network object, and returns it to the pool.
    /// </summary>
    private void ReturnToPool()
    {
        // Existing check adjusted slightly for clarity 
        if (!IsServer) return;
        if (isDying && !gameObject.GetComponent<NetworkObject>().IsSpawned) 
        {
             // If Die() already called ReturnToPool implicitly via Despawn, 
             // and the object is no longer spawned, we might be called by Invoke later.
             // Avoid processing again if already dying AND despawned.
             return;
        }
        // It's possible Die() calls this, then Invoke calls it later after Die() set isDying=true.
        // If called by Invoke and isDying is already true, log it? For now, just proceed.
        // Or, just rely on NetworkObjectPool handling duplicate returns gracefully.
        if (isDying) {
             // Already processing death, likely called by Die(). 
             // The pool return happens below, let it proceed.
             // We could potentially skip the CancelInvoke and Deregister here if called by Die,
             // but doing them again is harmless.
        }
        
        isDying = true; // Ensure flag is set if called by Invoke

        // Cancel invoke in case this was called manually before timer expired
        CancelInvoke(nameof(ReturnToPool));

        // --- Deregister from Registry ---
        if (SpiritRegistry.Instance != null)
        {
            SpiritRegistry.Instance.Deregister(this, ownerRole);
        }
        // ------------------------------

        // Despawn the NetworkObject (this handles disabling/returning to pool if applicable)
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
             NetworkObject.Despawn(false); // Don't destroy, allow pool reuse
        }

        // Return the GameObject's NetworkObject to the pool
        if (NetworkObjectPool.Instance != null && NetworkObject != null) // Check NetworkObject again
        {
            NetworkObjectPool.Instance.ReturnNetworkObject(this.NetworkObject);
        }
        else if (NetworkObjectPool.Instance == null)
        {
            Debug.LogError("[SpiritController] NetworkObjectPool instance is null! Cannot return spirit to pool. Destroying instead.", this);
            if (gameObject != null) Destroy(gameObject); // Fallback: destroy if pool missing
        }
    }

    #endregion

    // --- Implementation of IClearable ---
    /// <summary>
    /// Called by effects like PlayerDeathBomb or Shockwave to clear this spirit.
    /// Triggers the spirit's death sequence on the server.
    /// </summary>
    /// <param name="forceClear">If true, bypasses normal conditions and forces clearance (currently ignored by spirits).</param>
    /// <param name="sourceRole">The role of the player causing the clear (used for kill attribution).</param>
    public void Clear(bool forceClear, PlayerRole sourceRole)
    {
        // Clearing logic only runs on the server
        if (!IsServer) return;

        // Spirits are always cleared. Call Die, passing the sourceRole for attribution.
        Die(sourceRole);
    }
    // ------------------------------------
} 