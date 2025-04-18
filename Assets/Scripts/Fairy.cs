using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic; // Needed for List

// Final set of required components
[RequireComponent(typeof(SplineWalker))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(FairyCollisionHandler))]
[RequireComponent(typeof(FairyDeathEffects))]
[RequireComponent(typeof(FairyPathInitializer))] // Added path initializer
public class Fairy : NetworkBehaviour, IClearableByBomb
{
    [Header("Stats")]
    // Use NetworkVariable for synchronized health
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [SerializeField] private int initialMaxHealth = 1; // Used for initialization
    [SerializeField] private bool isGreatFairy = false; // Determines health and potentially score later

    // --- NEW: Flag for Extra Attack Trigger ---
    private bool isExtraAttackTrigger = false;
    // ------------------------------------------

    // --- RE-ADD: Flag to prevent Die() running multiple times ---
    private bool isDying = false;
    // ----------------------------------------------------------

    // --- NEW: Delay for visual effects/chaining ---
    [SerializeField] private float deathEffectDelay = 0.1f; // Delay before showing effects/triggering next chain
    // --------------------------------------------

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
             Debug.LogError("FairyRegistry instance not found during OnNetworkSpawn!");
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

    // Public method to set path info (delegates to initializer)
    public void SetPathInfo(int ownerIndex, int pIndex, bool startAtBegin)
    {
        // This method should only be called on the server by the spawner
        if (!IsServer)
        {
             Debug.LogWarning("[Fairy] SetPathInfo called on a non-server instance.");
             return;
        }
        if (pathInitializer != null)
        {
            pathInitializer.SetPathInfoOnServer(ownerIndex, pIndex, startAtBegin);
        }
        else
        {
            Debug.LogError($"[Fairy NetId:{NetworkObjectId}] PathInitializer component is missing! Cannot set path info.");
        }
    }

    // --- NEW: Method to assign line info (called by spawner) ---
    public void AssignLineInfo(System.Guid lineGuid, int index)
    {
        if (!IsServer) 
        {
            Debug.LogWarning("AssignLineInfo called on non-server instance.", this);
            return;
        }
        lineId = lineGuid;
        indexInLine = index;
        // Can't register here, might happen before OnNetworkSpawn
    }
    // -----------------------------------------------------------

    // --- NEW: Getters for line info ---
    public System.Guid GetLineId() { return lineId; }
    public int GetIndexInLine() { return indexInLine; }
    // ---------------------------------

    // --- NEW: Getter for Owner Role --- 
    public PlayerRole GetOwnerRole() 
    {
        return ownerRole;
    }
    // ----------------------------------

    // --- NEW: Method to mark this fairy as the trigger ---
    // Called directly by the FairySpawner on the server after instantiation
    public void MarkAsExtraAttackTrigger()
    {
        // Basic check - should only be callable on server by spawner
        if (!IsServer) 
        {
            Debug.LogWarning("MarkAsExtraAttackTrigger called on non-server instance.", this);
            return;
        }
        isExtraAttackTrigger = true;
        // TODO: Add visual indicator change here if desired (e.g., change sprite color, add particle effect)
    }
    // ------------------------------------------------------

    // --- NEW: Method to assign owner role (called by spawner) --- 
    public void AssignOwnerRole(PlayerRole role)
    {
        if (!IsServer) 
        {   
            Debug.LogWarning("AssignOwnerRole called on non-server instance.", this);
            return;
        }
        ownerRole = role;
    }
    // ----------------------------------------------------------

    void Update()
    {
        // Movement is now handled by SplineWalker attached to this GameObject

        // TODO: Add logic to destroy fairy if it goes off-screen (SplineWalker might handle this if destroyOnComplete is true)
    }

    // Public method called locally (e.g., by shockwave) to request damage
    public void RequestDamage(int amount)
    {
        // Default killer role if called without specific attribution (e.g., shockwave, collision with player)
        RequestDamage(amount, PlayerRole.None); 
    }
    
    // Overload to specify the killer
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
        Debug.Log($"[Server Fairy {NetworkObjectId}] ApplyDamageInternal executing. Amount: {amount}, Killer: {killerRole}, Current HP: {currentHealth.Value}, isDying: {isDying}");

        // Check isDying flag FIRST
        if (isDying) 
        {   
             Debug.Log($"[Server Fairy {NetworkObjectId}] Ignoring damage because isDying is true.");
             return; 
        }
        
        // Ensure health is positive before applying damage
        if (currentHealth.Value <= 0) 
        {
            Debug.Log($"[Server Fairy {NetworkObjectId}] Ignoring damage because currentHealth is already {currentHealth.Value}.");
            return;
        }

        int previousHealth = currentHealth.Value;
        currentHealth.Value -= amount;
        Debug.Log($"[Server Fairy {NetworkObjectId}] Applied damage. New HP: {currentHealth.Value} (was {previousHealth})");

        // Check if health dropped to 0 or below
        if (currentHealth.Value <= 0)
        { 
            Debug.Log($"[Server Fairy {NetworkObjectId}] Health dropped to {currentHealth.Value}. Calling Die().");
            Die(killerRole); 
        }
    }
    // --------------------------------------------------------------

    // --- NEW: Public method for SERVER-SIDE damage application --- 
    public void ApplyDamageServer(int amount, PlayerRole killerRole)
    {
        // Ensure this is only called on the server
        if (!IsServer) 
        {   
            Debug.LogWarning($"ApplyDamageServer called on non-server instance for Fairy {NetworkObjectId}. Ignoring.");
            return;
        }
        // Directly call the internal logic
        ApplyDamageInternal(amount, killerRole);
    }
    // -----------------------------------------------------------

    // --- New method called by SplineWalker --- 
    public void ReportEndOfPath()
    {
        // This might be called on client or server, send RPC to server to handle destruction
        RequestDestroyAtEndOfPathServerRpc();
    }

    [ServerRpc(RequireOwnership = false)] // Needs to be callable by client
    private void RequestDestroyAtEndOfPathServerRpc()
    {
        // Only destroy if still alive (hasn't been killed by something else)
        if (currentHealth.Value > 0) 
        {
            DestroySelf(); // Use a common destroy method
        }
    }
    // -------------------------------------------

    // --- NEW: Helper to check if alive (used by Chain Reaction) ---
    public bool IsAlive()
    {
        // Consider network readiness if necessary, but health is a good indicator server-side
        return currentHealth.Value > 0;
    }
    // -------------------------------------------------------------

    // This function now ONLY runs on the server
    private void Die(PlayerRole killerRole = PlayerRole.None)
    {
        // This function now ONLY runs on the server
        if (!IsServer) return; // Extra safety check

        // --- DIAGNOSTIC LOG: Die() Entry --- 
        Debug.Log($"[Server Fairy {NetworkObjectId}] Die() method entered. Killer: {killerRole}, isDying: {isDying}");

        // --- RE-ADD: Prevent re-entry if already dying --- 
        if (isDying) 
        {   
            Debug.Log($"[Server Fairy {NetworkObjectId}] Die() aborted: isDying flag already true.");
            return;
        }
        isDying = true; // Set flag immediately
        Debug.Log($"[Server Fairy {NetworkObjectId}] Set isDying = true."); // Log flag change
        // --------------------------------------------------

        // --- NOTIFY PLAYER DATA MANAGER OR GRANT ATTACK ---
        if (killerRole != PlayerRole.None && PlayerDataManager.Instance != null)
        {
            // Get killer's data for potential Extra Attack trigger
            PlayerData? killerData = PlayerDataManager.Instance.GetPlayerDataByRole(killerRole);

            if (isExtraAttackTrigger && killerData.HasValue)
            {
                // Trigger Extra Attack directly via ExtraAttackManager
                if (ExtraAttackManager.Instance != null)
                {
                    PlayerRole opponentRole = (killerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
                    ExtraAttackManager.Instance.TriggerExtraAttackInternal(killerData.Value, opponentRole);
                }
                else
                {
                    Debug.LogError("ExtraAttackManager.Instance is null! Cannot trigger Extra Attack.");
                }
                // NOTE: We are NOT calling IncrementFairyKillCount here - trigger replaces kill count point.
            }
            else
            {
                // Increment regular kill count (now directly notifies ExtraAttackManager)
                // This call remains necessary for non-trigger fairies.
                // PlayerDataManager.Instance.IncrementFairyKillCount(killerRole); // This now calls ExtraAttackManager.NotifyFairyKilled
                // Let's keep the original call structure via PlayerDataManager for consistency
                PlayerDataManager.Instance.IncrementFairyKillCount(killerRole); 
            }
        }
        else if (PlayerDataManager.Instance == null && killerRole != PlayerRole.None) // Only log error if manager missing when expected
        {
             Debug.LogError($"[Server Fairy {NetworkObjectId}] Died. Killer role {killerRole} valid, but PlayerDataManager.Instance is null!");
        }
        // --- END NOTIFY ---

        // --- NEW: Trigger Stage Bullet Spawn on Opponent ---
        if (StageSmallBulletSpawner.Instance != null)
        {
            StageSmallBulletSpawner.Instance.SpawnBulletForOpponent(killerRole);
        }
        else if (killerRole != PlayerRole.None) // Only warn if spawner missing when we expected to use it
        {
            Debug.LogWarning($"[Server Fairy {NetworkObjectId}] Died. StageSmallBulletSpawner instance is null, cannot spawn bullet for opponent.");
        }
        // -------------------------------------------------

        // --- Store data needed for delayed actions --- 
        Vector3 positionAtDeath = transform.position;
        System.Guid currentLineId = this.lineId; 
        int currentIndex = this.indexInLine;
        GameObject shockwavePrefab = (deathEffectsHandler != null) ? deathEffectsHandler.GetShockwavePrefab() : null;
        // ---------------------------------------------

        // --- Start Delayed Action Processor BEFORE destroying self ---
        GameObject processorObject = new GameObject($"DelayedActionProcessor_Fairy_{NetworkObjectId}");
        DelayedActionProcessor processor = processorObject.AddComponent<DelayedActionProcessor>();
        processor.InitializeAndRun(positionAtDeath, killerRole, deathEffectDelay, 
                                 shockwavePrefab, currentLineId, currentIndex);
        // ---------------------------------------------------------

        // --- Destroy Self AFTER processor is initialized ---
        DestroySelf(); 
        // -------------------------------------------------
    }

    // --- NEW: Server-side direct damage application method ---
    // This bypasses the RPC for server-side calls like chain reactions
    // Needs to remain public for the Chain Reaction handler to call it on other fairies
    public void ApplyLethalDamage(PlayerRole killerRole)
    {
        if (!IsServer) return;
        // --- Check isDying flag to prevent re-entry ---
        if (isDying) return; 
        // ---------------------------------------------

        // Apply enough damage to ensure death (health value itself might be redundant now)
        currentHealth.Value = 0; 
        Die(killerRole);
    }
    // --------------------------------------------------------

    // Common method for destruction logic, run ONLY on server
    // Made public so Chain Reaction handler can call it
    public void DestroySelf()
    {
        if (!IsServer) return; // Safety check

        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            // Disable the SplineWalker component if it exists
            if (splineWalker != null) 
            {
                splineWalker.enabled = false;
            }

            // Check if the object is spawned before trying to despawn
            if (networkObject.IsSpawned)
            {
                networkObject.Despawn(true); // true to destroy the object as well
            }
             else
            {
                 // If not spawned, destroy the GameObject directly on the server
                 Destroy(gameObject);
            }
        }
        else
        {
             // If no NetworkObject, just destroy locally
             Destroy(gameObject);
        }
    }

    // We might need a method to assign a path later
    // public void SetPath(Path pathToFollow) { ... }

    #region IClearableByBomb Implementation

    /// <summary>
    /// Called when the player's death bomb effect should clear this fairy.
    /// Sends an RPC to the server to handle the destruction.
    /// </summary>
    /// <param name="bombingPlayer">The role of the player who activated the bomb.</param>
    public void ClearByBomb(PlayerRole bombingPlayer)
    {
        // Since PlayerDeathBomb runs on server, this is called on the server instance.
        // We can directly perform the server-side checks and destroy.
        if (!IsServer || !IsAlive() || isDying)
        {
            // Keep this warning
            Debug.LogWarning($"[Server Fairy {NetworkObjectId}] ClearByBomb called, but ignoring. IsServer={IsServer}, IsAlive={IsAlive()}, isDying={isDying}");
            return;
        }
        
        // Debug.Log($"[Server Fairy {NetworkObjectId}] Clearing self via Die() due to bomb."); // <-- REMOVE LOG
        Die(bombingPlayer); // <-- Pass the bombingPlayer role to Die()
        // DestroySelf(); // Call the existing server-side destroy method
    }

    // ServerRpc called by ClearByBomb() - NO LONGER NEEDED
    /*
    [ServerRpc(RequireOwnership = false)] // Allow any client (or server) to trigger this
    private void RequestClearByBombServerRpc(ServerRpcParams rpcParams = default)
    {
        // Debug.Log($"[Server Fairy {NetworkObjectId}] RPC RequestClearByBombServerRpc received from client {rpcParams.Receive.SenderClientId}."); // <-- REMOVE LOG
        // Only destroy if still alive (hasn't been killed by something else)
        // Use the IsAlive check for consistency
        if (IsAlive() && !isDying) // Added !isDying check
        {
             // Debug.Log($"[Server Fairy {NetworkObjectId}] Attempting to DestroySelf due to bomb request."); // <-- REMOVE LOG
             DestroySelf();
        }
        else
        {
            // Debug.LogWarning($"[Server Fairy {NetworkObjectId}] Ignoring ClearByBomb RPC. IsAlive={IsAlive()}, isDying={isDying}"); // <-- REMOVE LOG
            // Debug.Log($"[Server Fairy {NetworkObjectId}] Received ClearByBomb request, but fairy is already dead/dying or not alive. Ignoring."); // Original log
        }
    }
    */
    #endregion
} 