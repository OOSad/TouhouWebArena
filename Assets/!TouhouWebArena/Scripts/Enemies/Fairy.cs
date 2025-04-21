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
             
             return;
        }
        if (pathInitializer != null)
        {
            pathInitializer.SetPathInfoOnServer(ownerIndex, pIndex, startAtBegin);
        }
        else
        {
            
        }
    }

    // --- NEW: Method to assign line info (called by spawner) ---
    public void AssignLineInfo(System.Guid lineGuid, int index)
    {
        if (!IsServer) 
        {
            
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
    public void ReportEndOfPath()
    {
        // This might be called on client or server, send RPC to server to handle destruction
        RequestDestroyAtEndOfPathServerRpc();
    }

    [ServerRpc(RequireOwnership = false)] // Needs to be callable by client
    private void RequestDestroyAtEndOfPathServerRpc()
    {
        // --- DIAGNOSTIC LOG: Server Destroy Request --- 
        

        // This method is now executed on the server
        if (IsAlive()) // Only destroy if still alive
        {
            Die(PlayerRole.None); // Dies from reaching end, no specific killer
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
         // --- RE-ADD: Early exit if already dying ---
        if (isDying || !IsServer) return;
        isDying = true;
        // --------------------------------------------

        // --- DIAGNOSTIC LOG: Die Method Entry --- 
        

        // Disable components immediately
        if (splineWalker != null) splineWalker.enabled = false;
        if (fairyCollider != null) fairyCollider.enabled = false;
        // No need to explicitly disable path initializer, it runs once.

        // --- Create the delayed action processor --- 
        
        GameObject processorGO = new GameObject($"Fairy_{NetworkObjectId}_DelayedProcessor");
        DelayedActionProcessor processor = processorGO.AddComponent<DelayedActionProcessor>();
        
        // Pass necessary data for the processor to work after this fairy is destroyed
        processor.InitializeAndRun(
            transform.position,
            killerRole, 
            deathEffectDelay,
            deathEffectsHandler != null ? deathEffectsHandler.GetShockwavePrefab() : null,
            lineId, 
            indexInLine
        );
        // -------------------------------------------

        // --- Spawn Regular Bullet on Opponent Side (Only if killed by player) --- 
        if (this.ownerRole != PlayerRole.None && killerRole != PlayerRole.None)
        {
            if (StageSmallBulletSpawner.Instance != null)
            {
                StageSmallBulletSpawner.Instance.SpawnBulletForOpponent(this.ownerRole); 
            }
            else if (IsServer) // Log error only on server
            {
                // Optional: Log error if spawner instance is missing
                
            }
        }
        // ------------------------------------------------------------------

        // --- Extra Attack Trigger Logic --- 
        if (isExtraAttackTrigger && killerRole != PlayerRole.None) 
        {
            // --- DIAGNOSTIC LOG: Triggering Extra Attack --- 
            

            // Only the server should find and call the manager
            if (IsServer)
            {
                ExtraAttackManager attackManager = ExtraAttackManager.Instance; // Use Singleton
                PlayerDataManager dataManager = PlayerDataManager.Instance; // Use Singleton

                if (attackManager != null && dataManager != null)
                {
                    // Get the attacker's data
                    PlayerData? attackerData = dataManager.GetPlayerDataByRole(killerRole);

                    if (attackerData.HasValue)
                    {
                        // Determine the opponent role
                        PlayerRole opponentRole = (killerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
                        // Call the correct method with correct arguments
                        attackManager.TriggerExtraAttackInternal(attackerData.Value, opponentRole); 
                    }
                    else
                    {
                        // Log if attacker data couldn't be found (optional)
                        
                    }
                }
                else
                {
                    // Log if managers couldn't be found (optional)
                    
                }
            }
        }
        // ---------------------------------

        // --- Deregister from Registry --- 
        // Done in OnNetworkDespawn or just before despawn
        if (FairyRegistry.Instance != null)
        {
            FairyRegistry.Instance.Deregister(this); 
        }
        // ---------------------------------

        // --- DIAGNOSTIC LOG: Despawning --- 
        

        // Despawn the NetworkObject
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); // true = destroy GameObject
        }
        else if (gameObject != null) // Fallback for local destruction if not networked/spawned
        {
            Destroy(gameObject);
        }
    }

    // --- NEW: Server-side direct damage application method ---
    // This bypasses the RPC for server-side calls like chain reactions
    // Needs to remain public for the Chain Reaction handler to call it on other fairies
    public void ApplyLethalDamage(PlayerRole killerRole)
    {
        // Ensure this is only called on the server
        if (!IsServer) 
        {   
            
            return;
        }

        // --- DIAGNOSTIC LOG: Applying Lethal Damage --- 
        

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
    public void DestroySelf()
    {
        // --- DIAGNOSTIC LOG: Destroy Self Called --- 
        

        // Ensure this runs on the server
        if (!IsServer) 
        { 
            
            return; 
        }
        
        // Check isDying flag
        if (isDying) 
        { 
            
            return; 
        }
        
        Die(PlayerRole.None); // Killer is None when self-destructing
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
        // Only execute on the server and if the spirit is not already dying
        if (!IsServer || isDying)
        {
             return;
        }

        // Call Die, passing the bombingPlayer as the killer.
        // This ensures the bullet spawn check (killer != None) passes,
        // and the revenge spawn check (killedByOwner) works correctly.
        Die(bombingPlayer);
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