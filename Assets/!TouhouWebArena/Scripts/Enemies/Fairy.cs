using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic; // Needed for List
using TouhouWebArena; // Add namespace for IClearable and PlayerRole

// Final set of required components
[RequireComponent(typeof(SplineWalker))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(FairyCollisionHandler))]
[RequireComponent(typeof(FairyDeathEffects))]
[RequireComponent(typeof(FairyPathInitializer))] // Added path initializer
/// <summary>
/// Represents a Fairy enemy unit. Handles health, path following initialization, line formation tracking,
/// damage taking, death effects triggering, and interactions with clearing effects via IClearable.
/// Relies on several other components (SplineWalker, Collider2D, NetworkObject, FairyCollisionHandler,
/// FairyDeathEffects, FairyPathInitializer) for its functionality.
/// Designed to be pooled and reused.
/// </summary>
public class Fairy : NetworkBehaviour, IClearable
{
    [Header("Stats")]
    // Use NetworkVariable for synchronized health
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [SerializeField] private int initialMaxHealth = 1; // Used for initialization
    [SerializeField] private bool isGreatFairy = false; // Determines health and potentially score later

    [Header("Chain Reaction")] // New Header
    [SerializeField] 
    [Tooltip("Delay in seconds before the next fairy in line is destroyed.")]
    private float chainReactionDelay = 0.08f; // Example delay, adjust as needed
    [SerializeField] 
    [Tooltip("The prefab for the DelayedActionProcessor utility.")]
    private GameObject delayedActionProcessorPrefab;
    [SerializeField] 
    [Tooltip("The shockwave effect prefab triggered on death (passed to DelayedActionProcessor).")]
    private GameObject deathShockwavePrefab; 

    // --- NEW: Flag for Extra Attack Trigger ---
    private bool isExtraAttackTrigger = false;
    // ------------------------------------------

    // --- RE-ADD: Flag to prevent Die() running multiple times ---
    private bool isDying = false;
    // ----------------------------------------------------------

    // --- NEW: Line Info ---
    private System.Guid lineId = System.Guid.Empty;
    private int indexInLine = -1;
    // ---------------------

    // --- NEW: Owner Role --- 
    private PlayerRole ownerRole = PlayerRole.None;
    // -----------------------

    // --- Reference Cleanup ---
    [SerializeField] private FairyDeathEffects deathEffectsHandler;
    [SerializeField] private FairyPathInitializer pathInitializer; // Added reference
    // ---------------------------------------------

    // References (no longer need playerToDamage here)
    private SplineWalker splineWalker;
    private Collider2D fairyCollider; // Reference to this fairy's collider

    void Awake()
    {
        splineWalker = GetComponent<SplineWalker>();
        fairyCollider = GetComponent<Collider2D>(); // Get reference to own collider
        if (deathEffectsHandler == null)
        {
             deathEffectsHandler = GetComponent<FairyDeathEffects>();
        }
        if (pathInitializer == null) pathInitializer = GetComponent<FairyPathInitializer>();
    }

    // Initialize health on the server when spawned
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            int maxHealth = isGreatFairy ? 3 : initialMaxHealth;
            currentHealth.Value = maxHealth;
        }
        // Optionally, disable collider briefly on clients to prevent spawn collisions?

        // --- NEW: Register with Registry ---
        if (FairyRegistry.Instance != null)
        {
            FairyRegistry.Instance.Register(this);
        }
        else if (IsServer) // Only log error on server to avoid client spam
        {
             
        }
        // ---------------------------------
    }

    public override void OnNetworkDespawn()
    {
        // --- NEW: Deregister from Registry ---
        if (FairyRegistry.Instance != null)
        {
            FairyRegistry.Instance.Deregister(this);
        }
        // -----------------------------------

        base.OnNetworkDespawn();
    }

    // --- NEW: Consolidated Initialization for Pooling ---
    // Called by the Spawner AFTER NetworkObject.Spawn()
    /// <summary>
    /// [Server Only] Initializes or re-initializes the Fairy after being obtained from a pool.
    /// Resets state, assigns line/owner/trigger info, and sets up the path using <see cref="FairyPathInitializer"/>.
    /// </summary>
    /// <param name="ownerIdx">The player index (0 or 1) associated with this fairy's path.</param>
    /// <param name="pIdx">The specific path index within the owner's path list.</param>
    /// <param name="startAtBegin">True if the fairy should start at the beginning of the path, false to start at the end.</param>
    /// <param name="lineGuid">The unique ID for the line of fairies this instance belongs to.</param>
    /// <param name="index">The index of this fairy within its line.</param>
    /// <param name="isTrigger">True if this fairy should trigger an extra attack on death.</param>
    /// <param name="owner">The <see cref="PlayerRole"/> who owns this fairy (whose side it spawns bullets on).</param>
    public void InitializeForPooling(int ownerIdx, int pIdx, bool startAtBegin, 
                                       System.Guid lineGuid, int index, 
                                       bool isTrigger, PlayerRole owner)
    {
        // --- Reset State --- 
        isDying = false;
        // Health is reset in OnNetworkSpawn based on isGreatFairy
        // Flags:
        isExtraAttackTrigger = isTrigger;
        // isGreatFairy is part of the prefab, not reset here.
        
        // --- Assign Info --- 
        lineId = lineGuid;
        indexInLine = index;
        ownerRole = owner;
        
        // --- Path --- 
        if (pathInitializer != null)
        {
            // PathInitializer now needs to handle being called potentially multiple times
            // or we assume it's safe to call SetPathInfoOnServer repeatedly.
            // For now, we assume it's safe. If issues arise, PathInitializer needs adjustment.
            pathInitializer.ResetInitializationFlag();
            pathInitializer.SetPathInfoOnServer(ownerIdx, pIdx, startAtBegin); 
        }
        else if(IsServer)
        {
            Debug.LogError($"Fairy {NetworkObjectId} missing Path Initializer during pooled init!", this);
        }

        // --- Component States --- 
        // Ensure components are enabled (they might be disabled by Die)
        // if (splineWalker != null) splineWalker.enabled = true;
        if (fairyCollider != null) fairyCollider.enabled = true;
    }
    // -------------------------------------------------

    // Public method to set path info (delegates to initializer)
    // --- OBSOLETE: Logic moved to InitializeForPooling ---
    /*
    public void SetPathInfo(int ownerIndex, int pIndex, bool startAtBegin)
    {
        // ... existing code ...
    }
    */
    // -----------------------------------------------------

    // --- NEW: Method to assign line info (called by spawner)
    // --- OBSOLETE: Logic moved to InitializeForPooling ---
    /*
    public void AssignLineInfo(System.Guid lineGuid, int index)
    {
        // ... existing code ...
    }
    */
    // -----------------------------------------------------------

    // --- NEW: Getters for line info ---
    /// <summary>
    /// Gets the unique identifier for the line of fairies this instance belongs to.
    /// </summary>
    /// <returns>The line's Guid.</returns>
    public System.Guid GetLineId() { return lineId; }
    /// <summary>
    /// Gets the index of this fairy within its line.
    /// </summary>
    /// <returns>The zero-based index in the line.</returns>
    public int GetIndexInLine() { return indexInLine; }
    // ---------------------------------

    // --- NEW: Getter for Owner Role --- 
    /// <summary>
    /// Gets the <see cref="PlayerRole"/> that owns this fairy.
    /// This determines which player's side bullets spawn on when this fairy is destroyed.
    /// </summary>
    /// <returns>The owning PlayerRole.</returns>
    public PlayerRole GetOwnerRole() 
    {
        return ownerRole;
    }
    // ----------------------------------

    // --- NEW: Method to mark this fairy as the trigger --- 
    // --- OBSOLETE: Logic moved to InitializeForPooling ---
    /*
    public void MarkAsExtraAttackTrigger()
    {
       // ... existing code ...
    }
    */
    // ------------------------------------------------------

    // --- NEW: Method to assign owner role (called by spawner) --- 
    // --- OBSOLETE: Logic moved to InitializeForPooling ---
    /*
    public void AssignOwnerRole(PlayerRole role)
    {
        // ... existing code ...
    }
    */
    // ----------------------------------------------------------

    void Update()
    {
        // Movement is now handled by SplineWalker attached to this GameObject

        // TODO: Add logic to destroy fairy if it goes off-screen (SplineWalker might handle this if destroyOnComplete is true)
    }

    // Public method called locally (e.g., by shockwave) to request damage
    /// <summary>
    /// [Client/Server] Requests that this fairy takes damage.
    /// This is typically called by local effects (like a shockwave) where the specific killer isn't known.
    /// Invokes <see cref="TakeDamageServerRpc"/> with <see cref="PlayerRole.None"/> as the killer.
    /// </summary>
    /// <param name="amount">The amount of damage to request.</param>
    public void RequestDamage(int amount)
    {
        // Default killer role if called without specific attribution (e.g., shockwave, collision with player)
        RequestDamage(amount, PlayerRole.None); 
    }
    
    /// <summary>
    /// [Client/Server] Requests that this fairy takes damage, attributing it to a specific player.
    /// Invokes <see cref="TakeDamageServerRpc"/>.
    /// </summary>
    /// <param name="amount">The amount of damage to request.</param>
    /// <param name="killerRole">The <see cref="PlayerRole"/> credited with the kill if the damage is lethal.</param>
    public void RequestDamage(int amount, PlayerRole killerRole)
    {
        TakeDamageServerRpc(amount, killerRole);
    }

    // ServerRpc is called by a client, executed on the server
    [ServerRpc(RequireOwnership = false)] // Allow any client to request damage
    private void TakeDamageServerRpc(int amount, PlayerRole killerRole, ServerRpcParams rpcParams = default) 
    {
        // This RPC now simply calls the internal logic method
        ApplyDamageInternal(amount, killerRole);
    }

    // --- NEW: Internal method containing the actual damage logic --- 
    // Can be called by ServerRpc (from client) or ApplyDamageServer (from server)
    private void ApplyDamageInternal(int amount, PlayerRole killerRole)
    {
         // --- DIAGNOSTIC LOG: Internal Logic Entry --- 
        

        // Check isDying flag FIRST
        if (isDying) 
        {   
             
             return; 
        }
        
        // Ensure health is positive before applying damage
        if (currentHealth.Value <= 0) 
        {
            
            return;
        }

        int previousHealth = currentHealth.Value;
        currentHealth.Value -= amount;
        

        // Check if health dropped to 0 or below
        if (currentHealth.Value <= 0)
        { 
            
            Die(killerRole); 
        }
    }
    // --------------------------------------------------------------

    // --- NEW: Public method for SERVER-SIDE damage application --- 
    /// <summary>
    /// [Server Only] Directly applies damage to the fairy on the server.
    /// Use this for server-authoritative damage sources (e.g., direct bullet hits processed on the server).
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    /// <param name="killerRole">The <see cref="PlayerRole"/> credited with the kill if the damage is lethal.</param>
    public void ApplyDamageServer(int amount, PlayerRole killerRole)
    {
        // Ensure this is only called on the server
        if (!IsServer) 
        {   
            
            return;
        }
        // Directly call the internal logic
        ApplyDamageInternal(amount, killerRole);
    }
    // -----------------------------------------------------------

    // --- New method called by SplineWalker --- 
    /// <summary>
    /// [Client/Server] Called by <see cref="SplineWalker"/> when the end of the path is reached.
    /// If on the server, calls <see cref="HandleEndOfPathServer"/>. If on a client, does nothing.
    /// </summary>
    public void ReportEndOfPath()
    {
        // Since SplineWalker Update runs only on server, this method is only called on server.
        // We can directly call the server-side handling logic.
        HandleEndOfPathServer();
    }

    /// <summary>
    /// [Server Only] Handles the logic when a fairy reaches the end of its path.
    /// Calls Die without triggering chain reaction effects.
    /// </summary>
    public void HandleEndOfPathServer()
    {
        bool alive = IsAlive();
        if (alive) 
        {
            Die(PlayerRole.None, triggerChainReaction: false); // Set flag to false
        }
    }
    // -------------------------------------------

    // --- NEW: Helper to check if alive (used by Chain Reaction) ---
    /// <summary>
    /// [Server/Client] Checks if the fairy is currently considered alive (health > 0 and not in the process of dying).
    /// </summary>
    /// <returns>True if the fairy is alive, false otherwise.</returns>
    public bool IsAlive()
    {
        // Consider network readiness if necessary, but health is a good indicator server-side
        return currentHealth.Value > 0;
    }
    // -------------------------------------------------------------

    /// <summary>
    /// [Server Only] Handles the death sequence of the fairy.
    /// Disables components, optionally triggers chain reaction effects (shockwave, next fairy kill, bullet spawns, extra attacks)
    /// based on the triggerChainReaction flag, and returns the object to the pool.
    /// </summary>
    /// <param name="killerRole">The role of the player who caused the death, or PlayerRole.None if no specific killer.</param>
    /// <param name="triggerChainReaction">If true (default), triggers shockwaves, kills the next fairy, spawns opponent bullets, and checks for extra attacks. If false, only despawns the fairy.</param>
    private void Die(PlayerRole killerRole = PlayerRole.None, bool triggerChainReaction = true)
    {
         // --- RE-ADD: Early exit if already dying --- 
        if (isDying || !IsServer) 
        {
            return;
        }
        isDying = true;
        // --------------------------------------------

        // Disable components immediately
        if (splineWalker != null) splineWalker.enabled = false;
        if (fairyCollider != null) fairyCollider.enabled = false;

        // --- CHAIN REACTION EFFECTS (Conditional) --- 
        if (triggerChainReaction)
        {
            // --- Create DelayedActionProcessor --- 
            if (delayedActionProcessorPrefab != null)
            {
                GameObject processorGO = Instantiate(delayedActionProcessorPrefab, transform.position, Quaternion.identity);
                DelayedActionProcessor processor = processorGO.GetComponent<DelayedActionProcessor>();
                if (processor != null)
                {
                    processor.InitializeAndRun(
                        transform.position, 
                        killerRole, 
                        chainReactionDelay, 
                        deathShockwavePrefab, 
                        lineId, 
                        indexInLine
                    );
                }
                else
                {
                    Debug.LogError($"DelayedActionProcessor prefab is missing the DelayedActionProcessor script!", delayedActionProcessorPrefab);
                    Destroy(processorGO); 
                }
            }
            else
            {
                Debug.LogError("DelayedActionProcessor prefab is not assigned on Fairy! Cannot run delayed actions.", this);
            }
            // --- End DelayedActionProcessor Creation ---

            // Effects below should only happen if killed BY A PLAYER during a chain reaction scenario
            if (killerRole != PlayerRole.None)
            {
                // --- Spawn Regular Bullet on Opponent Side --- 
                if (this.ownerRole != PlayerRole.None) // Check owner is valid
                {
                    if (StageSmallBulletSpawner.Instance != null)
                    {
                        StageSmallBulletSpawner.Instance.SpawnBulletForOpponent(this.ownerRole); 
                    }
                    else if (IsServer) 
                    {
                        Debug.LogWarning("StageSmallBulletSpawner instance is null, cannot spawn bullet.", this);
                    }
                }

                // Extra Attack Trigger Logic --- 
                if (isExtraAttackTrigger) 
                {
                    if (IsServer)
                    {
                        ExtraAttackManager attackManager = ExtraAttackManager.Instance;
                        PlayerDataManager dataManager = PlayerDataManager.Instance;

                        if (attackManager != null && dataManager != null)
                        {
                            PlayerData? attackerData = dataManager.GetPlayerDataByRole(killerRole);

                            if (attackerData.HasValue)
                            {
                                PlayerRole opponentRole = (killerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
                                attackManager.TriggerExtraAttackInternal(attackerData.Value, opponentRole); 
                            }
                        }
                    }
                }
            } // End if (killerRole != PlayerRole.None)
        } // --- END CHAIN REACTION EFFECTS ---
        
        // Return to Pool AFTER handling effects/triggers --- 
        if (NetworkObject != null && NetworkObjectPool.Instance != null)
        {
            NetworkObjectPool.Instance.ReturnNetworkObject(this.NetworkObject);
        }
        else // Fallback if NetworkObject or Pool somehow null
        {
             Debug.LogError($"Cannot return Fairy {NetworkObjectId} to pool. NetworkObject Null: {NetworkObject == null}, Pool Instance Null: {NetworkObjectPool.Instance == null}", this);
             if (gameObject != null) 
             {
                 Destroy(gameObject);
             }
        }
    }

    // --- NEW: Server-side direct damage application method --- 
    /// <summary>
    /// [Server Only] Applies lethal damage, bypassing normal health checks, and triggers the Die sequence.
    /// Useful for effects that should instantly kill fairies (e.g., bomb clearing, chain reactions via DelayedActionProcessor).
    /// </summary>
    /// <param name="killerRole">The <see cref="PlayerRole"/> attributed to the kill (used for bomb effect attribution or chain reaction propagation).</param>
    public void ApplyLethalDamage(PlayerRole killerRole)
    {
        // Ensure this is only called on the server
        if (!IsServer) 
        {   
            
            return;
        }

        // Check isDying flag
        if (isDying) 
        {
            
            return; 
        }

        // Directly call Die(), bypassing normal health checks
        Die(killerRole);
    }
    // --------------------------------------------------------

    // Common method for destruction logic, run ONLY on server
    // Made public so Chain Reaction handler can call it
    /// <summary>
    /// [Server Only] Handles the final destruction/despawning of the fairy GameObject.
    /// Calls Die without triggering chain reaction effects.
    /// </summary>
    public void DestroySelf()
    {
        if (!IsServer || isDying) return; 
        Die(PlayerRole.None, triggerChainReaction: false); // Set flag to false
    }

    // We might need a method to assign a path later
    // public void SetPath(Path pathToFollow) { ... }

    // --- Implementation of IClearable ---
    /// <summary>
    /// Called by effects like PlayerDeathBomb or Shockwave to clear this fairy.
    /// On the server, triggers the fairy's death sequence.
    /// </summary>
    /// <param name="forceClear">Ignored by fairies, as they are always clearable.</param>
    /// <param name="sourceRole">The role of the player causing the clear (used for kill attribution).</param>
    public void Clear(bool forceClear, PlayerRole sourceRole)
    {
        // Clearing logic only runs on the server
        if (!IsServer) return;

        // Fairies are always cleared, regardless of forceClear.
        // Call the existing Die method, passing the sourceRole for attribution.
        Die(sourceRole);
    }
    // ------------------------------------
} 