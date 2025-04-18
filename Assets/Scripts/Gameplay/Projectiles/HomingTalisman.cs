using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HomingTalisman : NetworkBehaviour
{
    [Header("Stats")] // Added header for clarity
    [SerializeField] private int damage = 2;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float lifetime = 5f; // Failsafe despawn time
    [SerializeField] private List<string> targetTags = new List<string>() { "Fairy", "Spirit" }; // Default target tags, editable in Inspector

    [Header("Targeting Boundaries (World Space)")] // Added header
    [SerializeField] private float minX = -4f; // Default, adjust in Inspector
    [SerializeField] private float maxX = 4f;  // Default, adjust in Inspector
    [SerializeField] private float minY = -5f; // Default, adjust in Inspector
    [SerializeField] private float maxY = 5f;  // Default, adjust in Inspector

    private Transform target;
    private bool canSeek = false;
    private float spawnTime;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // Server controls the talisman's logic

        spawnTime = Time.time;
        StartCoroutine(InitialDelayCoroutine());
        // Start lifetime countdown
        StartCoroutine(LifetimeCoroutine());
    }

    private void Update()
    {
        if (!IsServer || !canSeek) return;

        if (target == null)
        {
            FindTarget(); // Try to find target if null (e.g., opponent just spawned)
            if (target == null) {
                 // If still no target, maybe just destroy self or fly straight?
                 // For now, let's destroy it to prevent errors.
                 DespawnServerRpc();
                 return;
            }
        }

        // Move towards the target
        Vector2 direction = (target.position - transform.position).normalized;
        transform.position += (Vector3)direction * speed * Time.deltaTime;

        // Optional: Rotate to face the target
        // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // transform.rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward); // Assuming sprite faces upwards
    }

    private IEnumerator InitialDelayCoroutine()
    {
        yield return new WaitForSeconds(initialDelay);
        FindTarget(); // Find target after delay
        canSeek = true;
    }

     private IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(lifetime);
        // If the talisman hasn't hit anything by now, despawn it directly.
        if (IsSpawned && IsServer) // Check IsServer as only server should despawn
        {
           Debug.Log($"[Server Talisman {NetworkObjectId}] Lifetime expired. Despawning directly.");
           NetworkObject netObj = GetComponent<NetworkObject>();
           if (netObj != null) // Safety check
           {
               netObj.Despawn(true);
           }
           else { Debug.LogError($"[Server Talisman {NetworkObjectId}] NetworkObject missing during lifetime despawn!"); }
        }
    }

    // Server finds the closest opponent player based on tags AND owner
    private void FindTarget()
    {
        if (targetTags == null || targetTags.Count == 0)
        {
            Debug.LogWarning($"[Server Talisman {NetworkObjectId}] has no target tags assigned.");
            return;
        }

        // --- Get Talisman Owner Role --- 
        PlayerRole talismanOwnerRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null)
        {
             PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
             if (ownerData.HasValue)
             {
                 talismanOwnerRole = ownerData.Value.Role;
             }
        }
        if (talismanOwnerRole == PlayerRole.None)
        {
            Debug.LogWarning($"[Server Talisman {NetworkObjectId}] Could not determine valid owner role (OwnerClientId: {OwnerClientId}). Aborting target search.");
            return; // Cannot target if owner role is unknown
        }
        // ------------------------------

        List<Transform> potentialTargets = new List<Transform>();

        foreach (string tag in targetTags)
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in taggedObjects)
            {
                // --- Check Ownership --- 
                bool isOwnedByCorrectPlayer = false;
                if (obj.TryGetComponent<SpiritController>(out SpiritController spirit))
                {
                    // Spirit stores ownerRole internally, need a getter or public field
                    // Assuming a public getter GetOwnerRole() exists for now
                    if (spirit.GetOwnerRole() == talismanOwnerRole) 
                    { 
                        isOwnedByCorrectPlayer = true; 
                    }
                }
                else if (obj.TryGetComponent<Fairy>(out Fairy fairy))
                {
                    // Fairy needs a way to expose owner role
                    // Assuming a public getter GetOwnerRole() exists for now
                    if (fairy.GetOwnerRole() == talismanOwnerRole) 
                    {
                        isOwnedByCorrectPlayer = true; 
                    }
                }
                else
                {
                     Debug.LogWarning($"[Server Talisman {NetworkObjectId}] Found object {obj.name} with tag {tag} but no recognized Enemy script (SpiritController/Fairy).");
                }

                // --- Add if Ownership Matches AND Within Boundaries --- 
                if (isOwnedByCorrectPlayer)
                {
                    // --- Boundary Check --- 
                    Vector3 targetPos = obj.transform.position;
                    bool isInBounds = targetPos.x >= minX && targetPos.x <= maxX &&
                                       targetPos.y >= minY && targetPos.y <= maxY;

                    if (isInBounds)
                    {
                        potentialTargets.Add(obj.transform);
                    }
                    // ---------------------
                }
                // ---------------------------------------------------
            }
        }

        if (potentialTargets.Count == 0)
        {
            // Log adjusted to reflect ownership check
            Debug.Log($"[Server Talisman {NetworkObjectId}] Could not find any valid targets for owner {talismanOwnerRole} with tags: {string.Join(", ", targetTags)}");
            target = null;
            return;
        }

        // Find the closest target among all VALID potential targets
        target = potentialTargets
            .OrderBy(t => Vector3.Distance(transform.position, t.position))
            .FirstOrDefault();

        if (target != null)
        {
            Debug.Log($"[Server Talisman {NetworkObjectId}] Targeting closest valid enemy for {talismanOwnerRole}: {target.name} with tag {target.tag}");
        }
        else
        {
            Debug.LogWarning($"[Server Talisman {NetworkObjectId}] Failed to assign the closest target despite finding potential targets.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // --- DIAGNOSTIC LOG 1: Method Entry --- 
        Debug.Log($"[Talisman {NetworkObjectId}] OnTriggerEnter2D entered. Collided with: {other.name} (Tag: {other.tag}). IsServer: {IsServer}, canSeek: {canSeek}");

        // --- MODIFIED: Only check IsServer, allow collisions even if !canSeek --- 
        if (!IsServer) return;
        // --- END MODIFIED ---

        // --- DIAGNOSTIC LOG 2: Server Check Passed --- 
        // Log updated slightly to reflect change
        Debug.Log($"[Server Talisman {NetworkObjectId}] Passed server check. Processing collision...");

        if (targetTags.Contains(other.gameObject.tag))
        {
            // --- DIAGNOSTIC LOG 3: Tag Match --- 
            Debug.Log($"[Server Talisman {NetworkObjectId}] Tag '{other.gameObject.tag}' found in targetTags.");

            // --- Determine Killer Role --- 
            PlayerRole killerRole = PlayerRole.None;
            if (PlayerDataManager.Instance != null) 
            {
                PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
                if (ownerData.HasValue)
                {
                    killerRole = ownerData.Value.Role;
                }
                else
                {
                    Debug.LogWarning($"HomingTalisman {NetworkObjectId}: Could not find PlayerData for OwnerClientId {OwnerClientId}.");
                }
            }
            else
            {
                Debug.LogWarning($"HomingTalisman {NetworkObjectId}: PlayerDataManager.Instance is null! Cannot determine killer role.");
            }
            // ---------------------------

            bool damageApplied = false;
            // Debug.Log removed, redundant with logs below

            // --- Try applying damage to Fairy --- 
            if (other.TryGetComponent<Fairy>(out Fairy fairy))
            {
                // --- DIAGNOSTIC LOG 4: Fairy Component Found --- 
                Debug.Log($"[Server Talisman {NetworkObjectId}] Found Fairy component on {other.name}. Calling ApplyDamageServer. Damage: {damage}, Killer: {killerRole}");
                fairy.ApplyDamageServer(damage, killerRole); 
                damageApplied = true;
            }
            // --- Try applying damage to Spirit --- 
            else if (other.TryGetComponent<SpiritController>(out SpiritController spirit))
            {
                // --- DIAGNOSTIC LOG 5: Spirit Component Found --- 
                 Debug.Log($"[Server Talisman {NetworkObjectId}] Found SpiritController component on {other.name}. Requesting {damage} damage. Killer: {killerRole}");
                spirit.TakeDamage(damage, killerRole); 
                damageApplied = true;
            }
            // ---------------------------------
            
            if (!damageApplied)
            {
                 // --- DIAGNOSTIC LOG 6: No Component Found --- 
                Debug.LogWarning($"[Server Talisman {NetworkObjectId}] Collided object {other.name} with tag {other.gameObject.tag} does not have a recognized health component (Fairy or SpiritController).");
            }

            // --- DIAGNOSTIC LOG 7: Despawning --- 
            Debug.Log($"[Server Talisman {NetworkObjectId}] Despawning self directly after collision processing.");
            NetworkObject netObj = GetComponent<NetworkObject>(); // Get NetworkObject
            if (netObj != null && netObj.IsSpawned) // Check if it exists and is spawned
            {
                netObj.Despawn(true); // Despawn directly on the server
            }
            else
            {
                 Debug.LogWarning($"[Server Talisman {NetworkObjectId}] Could not despawn directly. NetworkObject is null or not spawned.");
            }
            canSeek = false; // Immediately stop seeking/moving after despawn initiated
        }
        else
        {
            // --- DIAGNOSTIC LOG 8: Tag Mismatch --- 
             Debug.Log($"[Server Talisman {NetworkObjectId}] Tag '{other.gameObject.tag}' NOT found in targetTags. Ignoring collision.");
        }
    }

    // ServerRpc to despawn the object across the network
    [ServerRpc(RequireOwnership = false)] // Allow server to call this even if it doesn't own it (though it should)
    private void DespawnServerRpc()
    {
        // --- DIAGNOSTIC LOG 9: RPC Executing --- 
        Debug.Log($"[Server Talisman {NetworkObjectId}] DespawnServerRpc executing. IsSpawned: {IsSpawned}");
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
} 