using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic; // Needed for List

// Now inherits from NetworkBehaviour
[RequireComponent(typeof(SplineWalker), typeof(Collider2D), typeof(NetworkObject))] // Restored
public class Fairy : NetworkBehaviour
{
    [Header("Stats")]
    // Use NetworkVariable for synchronized health
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [SerializeField] private int initialMaxHealth = 1; // Used for initialization
    [SerializeField] private bool isGreatFairy = false; // Determines health and potentially score later

    [Header("Effects")]
    [SerializeField] private GameObject shockwavePrefab; // Assign the FairyShockwave prefab here

    // --- NEW: Chain Reaction Settings (Server Only) ---
    [Header("Chain Reaction (Server)")]
    [SerializeField] private float chainRadius = 2.0f; // Radius to check for nearby fairies
    [SerializeField] private float chainDelay = 0.1f;  // Delay between chained explosions
    [SerializeField] private LayerMask fairyLayer;     // Layer mask to specifically hit fairies
    // --- NEW: Bullet Clearing Settings (Server Only) ---
    // [Header("Bullet Clearing (Server)")]
    // [SerializeField] private float bulletClearRadius = 2.0f; // Radius to check for nearby bullets
    // [SerializeField] private LayerMask bulletLayer;      // Layer mask to specifically hit bullets
    // ---------------------------------------------------

    // References (no longer need playerToDamage here)
    private SplineWalker splineWalker;
    private Collider2D fairyCollider; // Reference to this fairy's collider

    void Awake()
    {
        splineWalker = GetComponent<SplineWalker>();
        fairyCollider = GetComponent<Collider2D>(); // Get reference to own collider
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
    }

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
        if (currentHealth.Value <= 0) return;

        int previousHealth = currentHealth.Value;
        currentHealth.Value -= amount;

        // Check if health dropped to 0 or below in this call
        if (previousHealth > 0 && currentHealth.Value <= 0)
        {
            // Pass the killerRole down to Die()
            Die(killerRole); 
        }
    }

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

    // This function now ONLY runs on the server
    private void Die(PlayerRole killerRole = PlayerRole.None)
    {
        // This function now ONLY runs on the server
        if (!IsServer) return; // Extra safety check

        // --- NOTIFY PLAYER DATA MANAGER (MOVED HERE) ---
        if (killerRole != PlayerRole.None && PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.IncrementFairyKillCount(killerRole);
        }
        else if (PlayerDataManager.Instance == null && killerRole != PlayerRole.None) // Only log error if manager missing when expected
        {
             Debug.LogError($"[Server Fairy {NetworkObjectId}] Died. Killer role {killerRole} valid, but PlayerDataManager.Instance is null!");
        }
        // --- END NOTIFY ---

        // Log position on server before sending RPC - REMOVED
        // Debug.Log($"[Server Fairy NetId:{NetworkObjectId}] Die() called. Position: {transform.position}");

        // --- SERVER-SIDE CHAIN REACTION --- 
        List<Fairy> fairiesToChain = FindFairiesInChainRadius();
        bool isStartingChain = false; // Flag to check if we need to delay destruction
        if (fairiesToChain.Count > 0)
        {
            isStartingChain = true;
            StartCoroutine(ProcessChainReaction(fairiesToChain, killerRole));
        }
        // -----------------------------------

        // --- SERVER-SIDE BULLET CLEARING - REMOVED ---
        // ClearBulletsInRadius();
        // -----------------------------------

        // 1. Trigger the shockwave effect visually on all clients
        if (shockwavePrefab != null)
        {
            // Tell clients to spawn the non-networked effect
            SpawnShockwaveClientRpc(transform.position); 
        }

        // TODO: Add server-side logic for score/sending attacks later?

        // 2. Despawn and destroy the networked Fairy object 
        //    ONLY if it's NOT starting a chain reaction (the coroutine will handle destruction later)
        if (!isStartingChain)
        {
            DestroySelf(); 
        }
    }

    // --- NEW: Server-side method to find nearby fairies ---
    private List<Fairy> FindFairiesInChainRadius()
    {
        List<Fairy> foundFairies = new List<Fairy>();
        // --- Logging Start - REMOVED ---
        // Vector3 checkPosition = transform.position;
        // Debug.Log($"[Server FindFairies] Checking radius {chainRadius} at position {checkPosition} on layer {LayerMask.LayerToName(gameObject.layer)} (Mask: {fairyLayer.value})");
        // --- Logging End ---

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, chainRadius, fairyLayer);

        // --- Logging Start - REMOVED ---
        // Debug.Log($"[Server FindFairies] OverlapCircleAll hit {hits.Length} colliders.");
        // --- Logging End ---

        foreach (Collider2D hit in hits)
        {
            // Ignore self
            if (hit.gameObject == gameObject) continue;

            Fairy otherFairy = hit.GetComponent<Fairy>();
            // --- Logging Start - REMOVED ---
            // string hitInfo = $"  - Hit: {hit.gameObject.name} (Layer: {LayerMask.LayerToName(hit.gameObject.layer)}, Tag: {hit.tag}, HasFairy: {otherFairy != null})";
            // --- Logging End ---
            
            if (otherFairy != null && otherFairy.currentHealth.Value > 0)
            {
                 // --- Logging Start - REMOVED ---
                 // hitInfo += " -> Adding to list.";
                 // --- Logging End ---
                foundFairies.Add(otherFairy);
            }
            // else
            // {
            //     // --- Logging Start - REMOVED ---
            //     // hitInfo += " -> Skipping (Self, No Fairy, or Dead).";
            //     // --- Logging End ---
            // }
            // Debug.Log(hitInfo); // REMOVED
        }
        // Debug.Log($"[Server FindFairies] Found {foundFairies.Count} valid fairies to chain."); // REMOVED
        return foundFairies;
    }
    // ------------------------------------------------------

    // --- NEW: Server-side method to clear nearby bullets - REMOVED ---
    // private void ClearBulletsInRadius()
    // {
    //     // Use OverlapCircleAll with LayerMask for bullets
    //     Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, bulletClearRadius, bulletLayer);
    //     Debug.Log($"[Server ClearBullets] Checking radius {bulletClearRadius}. Found {hits.Length} colliders on bullet layer.");
    //
    //     foreach (Collider2D hit in hits)
    //     {
    //         NetworkObject bulletNetworkObject = hit.GetComponent<NetworkObject>();
    //         // Check if it's a networked object and actually spawned
    //         if (bulletNetworkObject != null) // Check for component first
    //         {
    //              // Log IsSpawned status
    //              Debug.Log($"[Server ClearBullets] Found bullet {hit.gameObject.name} with NetId:{bulletNetworkObject.NetworkObjectId}. IsSpawned = {bulletNetworkObject.IsSpawned}");
    //              if (bulletNetworkObject.IsSpawned) // Then check if spawned
    //              {
    //                 Debug.Log($"[Server ClearBullets] Requesting destruction for spawned bullet NetId:{bulletNetworkObject.NetworkObjectId}.");
    //                 // Call the existing RPC to destroy the object
    //                 RequestDestroyObjectServerRpc(bulletNetworkObject.NetworkObjectId);
    //              }
    //              else
    //              {
    //                  Debug.LogWarning($"[Server ClearBullets] Found bullet {hit.gameObject.name} with NetId:{bulletNetworkObject.NetworkObjectId} but it was NOT spawned. Skipping destruction request.");
    //              }
    //         }
    //         else
    //         {
    //              Debug.LogWarning($"[Server ClearBullets] Found collider {hit.gameObject.name} on bullet layer, but it has no NetworkObject component.");
    //         }
    //     }
    // }
    // -------------------------------------------------------

    // --- NEW: Server-side coroutine for delayed chain reaction ---
    private IEnumerator ProcessChainReaction(List<Fairy> chainedFairies, PlayerRole killerRole)
    {
        ulong sourceNetId = NetworkObjectId;
        string sourceName = gameObject.name; // Store name in case GO is destroyed prematurely
        // Debug.Log($"[Server Chain Coroutine {sourceNetId}] Starting with {chainedFairies.Count} fairies."); // REMOVED
        // Wait one frame to allow the current fairy to be fully destroyed/marked by other systems if needed
        yield return null; 

        int chainIndex = 0;
        foreach (Fairy targetFairy in chainedFairies)
        {
            chainIndex++;
            ulong targetNetId = targetFairy?.NetworkObjectId ?? 0; 
            // Debug.Log($"[Server Chain Coroutine {sourceNetId}] Processing fairy {chainIndex}/{chainedFairies.Count} (NetId: {targetNetId})"); // REMOVED

            // Check if the target is still valid before waiting
            if (targetFairy != null && targetFairy.gameObject != null && targetFairy.currentHealth.Value > 0)
            {
                // Debug.Log($"[Server Chain Coroutine {sourceNetId}] Target {targetNetId} is valid, waiting for delay {chainDelay}s..."); // REMOVED
                yield return new WaitForSeconds(chainDelay);
                // Debug.Log($"[Server Chain Coroutine {sourceNetId}] Finished wait for target {targetNetId}"); // REMOVED

                // Check again after the delay, in case it died from another source
                if (targetFairy != null && targetFairy.gameObject != null && targetFairy.currentHealth.Value > 0)
                {
                    // Debug.Log($"[Server Chain Coroutine {sourceNetId}] >>> Triggering ApplyLethalDamage for Fairy NetId:{targetNetId}"); // REMOVED
                    targetFairy.ApplyLethalDamage(killerRole);
                }
                 else
                {
                    // Debug.LogWarning($"[Server Chain Coroutine {sourceNetId}] Target Fairy NetId:{targetNetId} became invalid AFTER delay."); // REMOVED
                }
            }
            else
            {
                // Debug.LogWarning($"[Server Chain Coroutine {sourceNetId}] Target Fairy NetId:{targetNetId} was invalid BEFORE delay."); // REMOVED
            }
        }
        // Debug.Log($"[Server Chain Coroutine {sourceNetId}] Finished processing chain."); // REMOVED

        // --- NOW destroy the original fairy that started this chain ---
        // Debug.Log($"[Server Chain Coroutine {sourceNetId}] Chain complete. Destroying original fairy ({sourceName})."); // REMOVED
        // Make sure the original fairy hasn't been destroyed by something else already
        if (this != null && this.gameObject != null)
        {
           DestroySelf();
        }
        // ----------------------------------------------------------------
    }
    // -----------------------------------------------------------

    // --- NEW: Server-side direct damage application method ---
    // This bypasses the RPC for server-side calls like chain reactions
    public void ApplyLethalDamage(PlayerRole killerRole)
    {
        if (!IsServer) return;
        if (currentHealth.Value <= 0) return;

        // Apply enough damage to ensure death
        currentHealth.Value = 0;
        Die(killerRole);
    }
    // --------------------------------------------------------

    // Common method for destruction logic, run ONLY on server
    private void DestroySelf()
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
                // Debug.Log($"[Server DestroySelf] Despawning Fairy NetId:{networkObject.NetworkObjectId}"); // REMOVED
                networkObject.Despawn(true); // true to destroy the object as well
            }
             else
            {
                 // Debug.LogWarning($"[Server DestroySelf] Tried to despawn Fairy NetId:{networkObject.NetworkObjectId}, but it was already despawned or never spawned. Destroying locally."); // REMOVED
                 // If not spawned, destroy the GameObject directly on the server
                 Destroy(gameObject);
            }
        }
        else
        {
             // Debug.LogError("[Server DestroySelf] Cannot find NetworkObject on Fairy. Destroying locally."); // REMOVED
             // If no NetworkObject, just destroy locally
             Destroy(gameObject);
        }
    }

    // ClientRpc is called by the server, executed on all clients
    [ClientRpc]
    private void SpawnShockwaveClientRpc(Vector3 spawnPosition)
    {
        // Log position received on client
        Debug.Log($"[Client Fairy NetId:{NetworkObjectId}] SpawnShockwaveClientRpc received. Position: {spawnPosition}");

        // Clients instantiate the purely visual shockwave effect
        if (shockwavePrefab != null)
        {
           GameObject shockwaveInstance = Instantiate(shockwavePrefab, spawnPosition, Quaternion.identity);
           
           // Prevent shockwave hitting the (already destroyed on server, soon to be destroyed locally) source
           // Getting the collider might be unreliable here as the object is being destroyed.
           // Consider adding a brief delay before enabling the shockwave collider, 
           // or make the shockwave ignore the Fairy layer for a fraction of a second.
           // Shockwave shockwaveScript = shockwaveInstance.GetComponent<Shockwave>();
           // if (shockwaveScript != null && fairyCollider != null && fairyCollider.enabled)
           // {
           //     shockwaveScript.SetSourceCollider(fairyCollider);
           // }
        }
    }

    // Collision runs locally on clients, but requests actions from the server
    void OnTriggerEnter2D(Collider2D other)
    {
        // --- LOGGING START - REMOVED ---
        // Debug.Log($"[Fairy {NetworkObjectId} OnTriggerEnter2D] Collision detected with {other.gameObject.name} (Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        // --- LOGGING END ---

        if (!IsOwner && !IsServer) 
        {
             // Basic collision detection can run on clients, but actions requiring
             // state changes (damage, destruction) must be requested from the server.
        }

        // Player Shot collision:
        if (other.CompareTag("PlayerShot"))
        {
            NetworkObject shotNetworkObject = other.GetComponent<NetworkObject>();
            PlayerRole killer = PlayerRole.None; // Default
            BulletMovement bulletScript = other.GetComponent<BulletMovement>(); // Assuming script name
            if(bulletScript != null)
            {
                killer = bulletScript.OwnerRole.Value;
            }

            if (shotNetworkObject != null)
            {
                RequestDestroyObjectServerRpc(shotNetworkObject.NetworkObjectId);
            }
            else
            {
                Destroy(other.gameObject); 
            }
            
            // Request server apply damage to this fairy, attributing the kill
            RequestDamage(1, killer);
        }
        // Player collision:
        else if (other.CompareTag("Player"))
        {
            NetworkObject playerNetworkObject = other.GetComponent<NetworkObject>();
            if(playerNetworkObject != null)
            {
                // Call the new ServerRpc to handle damaging the player
                DamagePlayerServerRpc(playerNetworkObject.NetworkObjectId, 1);
            }

            // Request server kill this fairy (deal lethal damage)
            // Attributing kill to player might be complex here, maybe PlayerRole.None is fine?
            int damageToKill = currentHealth.Value > 0 ? currentHealth.Value : 999; 
            RequestDamage(damageToKill, PlayerRole.None); // Or figure out player role if needed
        }
    }

    // Generic ServerRpc to request destruction of any networked object by ID
    [ServerRpc(RequireOwnership = false)]
    void RequestDestroyObjectServerRpc(ulong networkObjectId)
    {
        // Debug.Log($"[Server RequestDestroy] Received request for NetId:{networkObjectId}"); // REMOVED
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
        {
            if (networkObject != null && networkObject.IsSpawned)
            {
                // Debug.Log($"[Server RequestDestroy] Found NetId:{networkObjectId}. Calling Despawn(true)."); // REMOVED
                networkObject.Despawn(true); // Server despawns/destroys the object
                // Debug.Log($"[Server RequestDestroy] Despawn(true) called for NetId:{networkObjectId}."); // REMOVED
            }
            else
            {
                 // Keep this warning as it indicates a potential issue
                 Debug.LogWarning($"[Server RequestDestroy] Tried to destroy NetId:{networkObjectId} but it was null or not spawned.");
            }
        }
        else
        {
             // Keep this warning as it indicates a potential issue
             Debug.LogWarning($"[Server RequestDestroy] Could not find NetId:{networkObjectId} in SpawnedObjects.");
        }
    }

    // --- New ServerRpc to handle damaging the player ---
    [ServerRpc(RequireOwnership = false)]
    void DamagePlayerServerRpc(ulong playerNetworkObjectId, int damageAmount)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerNetworkObject))
        {
            if (playerNetworkObject != null)
            {
                PlayerHealth playerHealth = playerNetworkObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    // Now call the correct server-authoritative method on PlayerHealth
                    playerHealth.TakeDamage(damageAmount);
                }
                else
                {
                     Debug.LogWarning($"Fairy tried to damage object {playerNetworkObjectId}, but it has no PlayerHealth component.");
                }
            }
        }
        else
        {
            Debug.LogWarning($"Server could not find Player NetworkObject with ID {playerNetworkObjectId} to damage.");
        }
    }

    // We might need a method to assign a path later
    // public void SetPath(Path pathToFollow) { ... }
} 