using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic; // Needed for List
using TouhouWebArena; // Add namespace for IClearable and PlayerRole

// Final set of required components -- Temporarily Commented Out
// [RequireComponent(typeof(SplineWalker))]
// [RequireComponent(typeof(Collider2D))]
// [RequireComponent(typeof(NetworkObject))]
// [RequireComponent(typeof(FairyCollisionHandler))]
// [RequireComponent(typeof(FairyDeathEffects))]
// [RequireComponent(typeof(FairyPathInitializer))]
// [RequireComponent(typeof(FairyExtraAttackTrigger))]
// [RequireComponent(typeof(FairyChainReactionHandler))]
// [RequireComponent(typeof(FairyHealth))] // Added health component
/// <summary>
/// Represents a Fairy enemy unit. Acts as the central coordinator for various fairy components,
/// managing path following initialization, line/owner information, pooling setup, and death sequence coordination.
/// Relies on several sibling components for specific functionalities:
/// <see cref="SplineWalker"/>, <see cref="Collider2D"/>, <see cref="NetworkObject"/>, <see cref="FairyCollisionHandler"/>,
/// <see cref="FairyDeathEffects"/>, <see cref="FairyPathInitializer"/>, <see cref="FairyExtraAttackTrigger"/>, 
/// <see cref="FairyChainReactionHandler"/>, <see cref="FairyHealth"/>.
/// Implements <see cref="IClearable"/>, delegating the action to <see cref="FairyHealth"/>.
/// Designed to be pooled and reused.
/// </summary>
public class FairyController : NetworkBehaviour, IClearable
{
    // --- REMOVED Health Stats ---
    // [Header("Stats")]
    // private NetworkVariable<int> currentHealth = ...
    // [SerializeField] private int initialMaxHealth = 1;
    // [SerializeField] private bool isGreatFairy = false;
    // ----------------------------

    // --- Flag to prevent HandleDeath() running multiple times ---
    private bool isDying = false;
    // ----------------------------------------------------------

    // --- Line Info ---
    private System.Guid lineId = System.Guid.Empty;
    private int indexInLine = -1;
    // ---------------------

    // --- Owner Role --- 
    private PlayerRole ownerRole = PlayerRole.None;
    // -----------------------

    // --- Component References --- 
    private FairyDeathEffects deathEffectsHandler;
    private FairyPathInitializer pathInitializer;
    private FairyExtraAttackTrigger extraAttackTriggerHandler;
    private FairyChainReactionHandler chainReactionHandler;
    private FairyHealth fairyHealth; // Added health reference
    private SplineWalker splineWalker;
    private Collider2D fairyCollider;
    // --------------------------

    void Awake()
    {
        // Get all required components
        splineWalker = GetComponent<SplineWalker>();
        fairyCollider = GetComponent<Collider2D>();
        deathEffectsHandler = GetComponent<FairyDeathEffects>();
        pathInitializer = GetComponent<FairyPathInitializer>();
        extraAttackTriggerHandler = GetComponent<FairyExtraAttackTrigger>(); 
        chainReactionHandler = GetComponent<FairyChainReactionHandler>();
        fairyHealth = GetComponent<FairyHealth>(); // Get health component
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Health initialization is handled by FairyHealth.OnNetworkSpawn

        if (IsServer)
        {
            // Subscribe to the death event from the health component
            if (fairyHealth != null) 
            {
                fairyHealth.OnDeath += HandleDeath;
            }
            else
            {
                Debug.LogError($"Fairy {NetworkObjectId} missing FairyHealth component in OnNetworkSpawn!", this);
            }
        }
        
        if (FairyRegistry.Instance != null)
        {   
            FairyRegistry.Instance.Register(this);
        }
        else if (IsServer) // Only log error on server to avoid client spam
        {
            Debug.LogError("FairyRegistry instance is null during Fairy OnNetworkSpawn!", this);
        }
        
        // Reset dying flag on spawn/respawn
        isDying = false; 
    }

    public override void OnNetworkDespawn()
    {
        // --- Deregister from Registry ---
        if (FairyRegistry.Instance != null)
        {
            FairyRegistry.Instance.Deregister(this);
        }
        // -----------------------------------

        // --- Unsubscribe from Death Event --- 
        if (IsServer && fairyHealth != null)
        {
            fairyHealth.OnDeath -= HandleDeath;
        }
        // ------------------------------------

        base.OnNetworkDespawn();
    }

    // --- Consolidated Initialization for Pooling ---
    /// <summary>
    /// [Server Only] Initializes or re-initializes the Fairy's state and components after being obtained from a pool.
    /// Resets the dying flag, assigns line/owner info, initializes the extra attack trigger,
    /// resets and sets up the path initializer, and ensures components like colliders/walkers are enabled.
    /// Note: Health initialization is handled by <see cref="FairyHealth.OnNetworkSpawn"/>.
    /// </summary>
    /// <param name="ownerIdx">The player index (0 or 1) for path selection.</param>
    /// <param name="pIdx">The specific path index.</param>
    /// <param name="startAtBegin">Whether to start at the path beginning or end.</param>
    /// <param name="lineGuid">The Guid identifying the fairy's line.</param>
    /// <param name="index">The fairy's index within its line.</param>
    /// <param name="isTrigger">Whether this fairy triggers an extra attack.</param>
    /// <param name="owner">The <see cref="PlayerRole"/> owning this fairy.</param>
    public void InitializeForPooling(int ownerIdx, int pIdx, bool startAtBegin, 
                                       System.Guid lineGuid, int index, 
                                       bool isTrigger, PlayerRole owner)
    {
        if (!IsServer) return;
        
        // --- Reset State --- 
        isDying = false;
        // Health is reset in FairyHealth.OnNetworkSpawn or InitializeHealth()
        
        // --- Assign Info --- 
        lineId = lineGuid;
        indexInLine = index;
        ownerRole = owner;
        
        // --- Initialize Handlers/Components --- 
        extraAttackTriggerHandler?.Initialize(isTrigger, owner);
        // FairyHealth is initialized via its own OnNetworkSpawn
        
        // --- Path --- 
        pathInitializer?.ResetInitializationFlag(); // Ensure path can be re-initialized
        pathInitializer?.SetPathInfoOnServer(ownerIdx, pIdx, startAtBegin);
        
        // --- Component States --- 
        if (splineWalker != null) splineWalker.enabled = true; // Re-enable walker
        if (fairyCollider != null) fairyCollider.enabled = true; // Re-enable collider
    }
    // -------------------------------------------------

    // --- Getters for line info and owner role ---
    /// <summary>Gets the unique identifier for the line of fairies this instance belongs to.</summary>
    public System.Guid GetLineId() { return lineId; }
    /// <summary>Gets the index of this fairy within its line.</summary>
    public int GetIndexInLine() { return indexInLine; }
    /// <summary>Gets the <see cref="PlayerRole"/> that owns this fairy.</summary>
    public PlayerRole GetOwnerRole() { return ownerRole; }
    // --------------------------------------------

    // --- Damage Request Handling --- 

    /// <summary>
    /// [Client/Server] Requests that this fairy takes damage via ServerRpc.
    /// Invokes <see cref="TakeDamageServerRpc"/> to forward the request to the server.
    /// Includes a client-side check for <see cref="isDying"/> to prevent spamming requests.
    /// </summary>
    /// <param name="amount">The amount of damage to request.</param>
    /// <param name="killerRole">The role potentially credited with the kill.</param>
    public void RequestDamage(int amount, PlayerRole killerRole)
    {   
        // Prevent requests if already dying on the client
        if (isDying) return; 
        TakeDamageServerRpc(amount, killerRole);
    }
    
    /// <summary>
    /// [Client Only] Convenience overload for <see cref="RequestDamage(int, PlayerRole)"/>, 
    /// requesting damage with <see cref="PlayerRole.None"/> as the killer.
    /// </summary>
    /// <param name="amount">The amount of damage to request.</param>
    public void RequestDamage(int amount)
    {
        RequestDamage(amount, PlayerRole.None); 
    }

    // ServerRpc is called by a client, executed on the server
    /// <summary>
    /// [ServerRpc] Receives a damage request from a client and delegates it to <see cref="FairyHealth.ApplyDamageFromRpc"/>.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(int amount, PlayerRole killerRole, ServerRpcParams rpcParams = default)
    {   
        // Delegate damage application to the health component
        fairyHealth?.ApplyDamageFromRpc(amount, killerRole);
    }

    /// <summary>
    /// [Server Only] Directly applies damage to the fairy by delegating to <see cref="FairyHealth.ApplyDamageFromServer"/>.
    /// Use this for server-authoritative damage sources (e.g., direct collision checks on server).
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    /// <param name="killerRole">The role potentially credited with the kill.</param>
    public void ApplyDamageServer(int amount, PlayerRole killerRole)
    {   
        // Delegate damage application to the health component
        fairyHealth?.ApplyDamageFromServer(amount, killerRole);
    }
    // ------------------------------------

    // --- Path Completion Handling ---
    /// <summary>
    /// [Server Only] Called by <see cref="SplineWalker"/> when the end of the path is reached.
    /// Silently returns the fairy to the object pool without triggering death effects or chain reactions.
    /// </summary>
    public void ReportEndOfPath()
    {   
        if (!IsServer) return; // Ensure this only runs on server

        // Log the event
        Debug.Log($"[FairyController] Fairy {NetworkObjectId} reached end of path. Returning to pool silently.");

        // --- Silent Despawn Logic ---
        // 1. Disable components that might interfere (optional but good practice)
        if (splineWalker != null) splineWalker.enabled = false;
        if (fairyCollider != null) fairyCollider.enabled = false;
        gameObject.SetActive(false); // Deactivate the object visually immediately

        // 2. Return to pool (assuming NetworkObjectPool handles despawning)
        if (NetworkObjectPool.Instance != null)
        {
            NetworkObjectPool.Instance.ReturnNetworkObject(this.NetworkObject); 
        }
        else
        {
            Debug.LogError($"[FairyController] NetworkObjectPool instance is null! Cannot return {NetworkObjectId} to pool on path end.", this);
            // Fallback: Destroy if pooling fails?
            // NetworkObject.Despawn(true); // Or just despawn
        }
        // -----------------------------

        // OLD LOGIC: Triggered full death sequence
        // fairyHealth?.ApplyLethalDamage(PlayerRole.None); 
    }
    // -------------------------------

    // --- REMOVED: HandleEndOfPathServer() --- 
    // Logic moved into ReportEndOfPath calling ApplyLethalDamage
    // ----------------------------------------
    
    // --- REMOVED: IsAlive() --- 
    // Now handled by FairyHealth.IsAlive()
    // --------------------------
    
    // --- Death Handling --- 

    /// <summary>
    /// [Server Only] Handles the death sequence triggered by the <see cref="FairyHealth.OnDeath"/> event.
    /// Sets the <see cref="isDying"/> flag, disables movement/collision components,
    /// triggers effects via <see cref="FairyDeathEffects"/>, triggers potential extra attacks via <see cref="FairyExtraAttackTrigger"/>,
    /// triggers potential chain reactions via <see cref="FairyChainReactionHandler"/>,
    /// and returns the object to the pool.
    /// </summary>
    /// <param name="killerRole">The role of the player who caused the death, passed from the OnDeath event.</param>
    private void HandleDeath(PlayerRole killerRole)
    {   
        // Early exit if already processing death or not on server
        if (isDying || !IsServer) 
        {
            return;
        }
        isDying = true;
        
        // Disable components immediately
        if (splineWalker != null) splineWalker.enabled = false;
        if (fairyCollider != null) fairyCollider.enabled = false;

        // Trigger visual/audio death effects
        deathEffectsHandler?.TriggerEffects(transform.position);

        // Trigger functional death effects (chain reaction, extra attack)
        // These components handle their own logic checks (e.g., isTrigger)
        extraAttackTriggerHandler?.TriggerExtraAttackIfApplicable(killerRole);
        chainReactionHandler?.ProcessChainReaction(killerRole, lineId, indexInLine, ownerRole);
        
        // Return to Pool AFTER handling effects/triggers
        if (NetworkObject != null && NetworkObjectPool.Instance != null)
        {
            NetworkObjectPool.Instance.ReturnNetworkObject(this.NetworkObject);
        }
        else // Fallback
        {
             Debug.LogError($"Cannot return Fairy {NetworkObjectId} to pool. NetworkObject Null: {NetworkObject == null}, Pool Instance Null: {NetworkObjectPool.Instance == null}", this);
             if (gameObject != null) Destroy(gameObject);
        }
    }
    
    // --- REMOVED: ApplyLethalDamage() --- 
    // Now handled by FairyHealth.ApplyLethalDamage()
    // ------------------------------------
    
    // --- REMOVED: DestroySelf() --- 
    // Functionality replaced by calling ApplyLethalDamage on FairyHealth
    // ------------------------------
    
    // --- Implementation of IClearable --- 
    /// <summary>
    /// [Server Only] Called by effects like bombs to clear this fairy.
    /// Delegates the action to <see cref="FairyHealth.ApplyLethalDamage"/>.
    /// </summary>
    /// <param name="forceClear">Ignored by fairies, as they are always clearable by bomb-like effects.</param>
    /// <param name="sourceRole">The role of the player causing the clear (used for kill attribution).</param>
    public void Clear(bool forceClear, PlayerRole sourceRole)
    {   
        // Clearing logic only runs on the server
        if (!IsServer) return;

        // Fairies are always cleared. Apply lethal damage via health component.
        fairyHealth?.ApplyLethalDamage(sourceRole);
    }
    // ------------------------------------
} 