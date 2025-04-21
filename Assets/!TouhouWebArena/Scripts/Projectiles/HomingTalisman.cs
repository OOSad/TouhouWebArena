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
           NetworkObject netObj = GetComponent<NetworkObject>();
           if (netObj != null) // Safety check
           {
               netObj.Despawn(true);
           }
        }
    }

    // Server finds the closest opponent player based on tags AND owner
    private void FindTarget()
    {
        if (targetTags == null || targetTags.Count == 0)
        {
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
            
            target = null;
            return;
        }

        // Find the closest target among all VALID potential targets
        target = potentialTargets
            .OrderBy(t => Vector3.Distance(transform.position, t.position))
            .FirstOrDefault();

        if (target != null)
        {
            
        }
        else
        {
            
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // --- DIAGNOSTIC LOG 1: Method Entry --- 
        

        // --- MODIFIED: Only check IsServer, allow collisions even if !canSeek --- 
        if (!IsServer) return;
        // --- END MODIFIED ---

        // --- DIAGNOSTIC LOG 2: Server Check Passed --- 
        // Log updated slightly to reflect change
        

        if (targetTags.Contains(other.gameObject.tag))
        {
            // --- DIAGNOSTIC LOG 3: Tag Match --- 
            

            // --- Determine Killer Role --- 
            PlayerRole killerRole = PlayerRole.None;
            if (PlayerDataManager.Instance != null) 
            {
                PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
                if (ownerData.HasValue)
                {
                    killerRole = ownerData.Value.Role;
                }
            }
            // ---------------------------

            bool damageApplied = false;

            // --- Try applying damage to Fairy --- 
            if (other.TryGetComponent<Fairy>(out Fairy fairy))
            {
                // --- DIAGNOSTIC LOG 4: Fairy Component Found --- 
                
                fairy.ApplyDamageServer(damage, killerRole); 
                damageApplied = true;
            }
            // --- Try applying damage to Spirit --- 
            else if (other.TryGetComponent<SpiritController>(out SpiritController spirit))
            {
                // --- DIAGNOSTIC LOG 5: Spirit Component Found --- 
                 
                spirit.TakeDamage(damage, killerRole); 
                damageApplied = true;
            }
            // ---------------------------------
            
            if (!damageApplied)
            {
                 // --- DIAGNOSTIC LOG 6: No Component Found --- 
                
            }

            // --- DIAGNOSTIC LOG 7: Despawning --- 
            
            NetworkObject netObj = GetComponent<NetworkObject>(); // Get NetworkObject
            if (netObj != null && netObj.IsSpawned) // Check if it exists and is spawned
            {
                netObj.Despawn(true); // Despawn using the reference
            }
            else
            {
                // --- DIAGNOSTIC LOG 8: NetworkObject Missing on Despawn --- 
                
            }
            canSeek = false; // Immediately stop seeking/moving after despawn initiated
        }
        else
        {
             // --- DIAGNOSTIC LOG 9: Tag Mismatch --- 
             
        }
    }

    // ServerRpc to despawn the object across the network
    [ServerRpc(RequireOwnership = false)] // Allow server to call this even if it doesn't own it (though it should)
    private void DespawnServerRpc()
    {
        // --- DIAGNOSTIC LOG 9: RPC Executing --- 
        if (IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
} 