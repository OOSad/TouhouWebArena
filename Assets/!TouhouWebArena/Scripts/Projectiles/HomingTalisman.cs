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

    private Transform currentTarget; // Renamed for clarity
    private bool canSeek = false;
    private float timeSinceLastRetargetCheck = 0f; // Timer for periodic retargeting if needed
    private const float RETARGET_CHECK_INTERVAL = 0.1f; // Check for new target every 0.1 seconds if current is null

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // Server controls the talisman's logic

        StartCoroutine(InitialDelayCoroutine());
        // Start lifetime countdown
        StartCoroutine(LifetimeCoroutine());
    }

    // Changed to FixedUpdate for physics-based movement consistency
    private void FixedUpdate()
    {
        if (!IsServer || !canSeek) return;

        // --- Target Validity Check ---
        if (currentTarget != null)
        {
            // Check if target GameObject is inactive (destroyed or disabled) OR outside boundaries
            if (!currentTarget.gameObject.activeInHierarchy || IsTargetOutOfBounds(currentTarget.position))
            {
                // Optionally check health component here if needed
                currentTarget = null; // Invalidate target
            }
        }
        // ---------------------------

        // --- Find Target if Necessary ---
        if (currentTarget == null)
        {
            // Optional: Limit how often we search to avoid constant checks if no targets exist
            timeSinceLastRetargetCheck += Time.fixedDeltaTime;
            if (timeSinceLastRetargetCheck >= RETARGET_CHECK_INTERVAL)
            {
                 FindTarget(); // Try to find a NEW target
                 timeSinceLastRetargetCheck = 0f; // Reset timer
            }

            // If still no target after trying to find one, maybe just fly straight or despawn?
            // For now, it will just stop moving until a target is found or lifetime ends.
            // Consider adding behavior here if needed (e.g., continue straight)
            // if(currentTarget == null) { /* Fly straight? */ }
        }
        // -----------------------------


        // --- Move Towards Valid Target ---
        if (currentTarget != null)
        {
            Vector2 direction = ((Vector3)currentTarget.position - transform.position).normalized; // Cast target position to Vector3
            // Using transform.position directly is fine for kinematic movement
            transform.position += (Vector3)direction * speed * Time.fixedDeltaTime;

            // Optional rotation (ensure sprite is oriented correctly)
        // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward); // Adjust -90 based on sprite orientation
        }
        // ----------------------------
    }

    private IEnumerator InitialDelayCoroutine()
    {
        yield return new WaitForSeconds(initialDelay);
        // Don't find target immediately, let FixedUpdate handle it
        // FindTarget(); // Removed from here
        canSeek = true;
        timeSinceLastRetargetCheck = RETARGET_CHECK_INTERVAL; // Allow immediate check on first seek frame
    }

     private IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(lifetime);
        // If the talisman hasn't hit anything by now, despawn it directly.
        if (this != null && IsSpawned && IsServer) // Check IsServer as only server should despawn
        {
           NetworkObject netObj = GetComponent<NetworkObject>();
           if (netObj != null) // Safety check
           {
               netObj.Despawn(true);
           }
        }
    }

    // Helper function to check bounds
    private bool IsTargetOutOfBounds(Vector3 position)
    {
        return position.x < minX || position.x > maxX || position.y < minY || position.y > maxY;
    }

    // Server finds the closest **VALID** enemy (owned by self) based on tags
    private void FindTarget()
    {
        Transform foundTarget = null;

        if (!IsServer || targetTags == null || targetTags.Count == 0)
        {
            currentTarget = null;
            return;
        }

        // --- Get Talisman Owner Role --- 
        PlayerRole talismanOwnerRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null)
        {
             PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
             if (ownerData.HasValue) { talismanOwnerRole = ownerData.Value.Role; }
        }
        if (talismanOwnerRole == PlayerRole.None) { currentTarget = null; return; } // Cannot target if owner role is unknown
        // ------------------------------


        float closestDistSqr = float.MaxValue;

        foreach (string tag in targetTags)
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in taggedObjects)
            {
                 if (!obj.activeInHierarchy) continue;

                // --- Check Ownership: Target MUST belong to the SAME player who fired the talisman ---
                bool isOwnedByCorrectPlayer = false;
                PlayerRole enemyOwnerRole = PlayerRole.None;

                if (obj.TryGetComponent<SpiritController>(out SpiritController spirit))
                {
                    enemyOwnerRole = spirit.GetOwnerRole(); // Assuming GetOwnerRole exists
                }
                else if (obj.TryGetComponent<FairyController>(out FairyController fairy))
                {
                    enemyOwnerRole = fairy.GetOwnerRole(); // Assuming GetOwnerRole exists
                }

                // Check if the found enemy belongs to the SAME player as the talisman owner
                if (enemyOwnerRole != PlayerRole.None && enemyOwnerRole == talismanOwnerRole)
                    {
                        isOwnedByCorrectPlayer = true; 
                    }
                // *****************************

                if (isOwnedByCorrectPlayer)
                {
                    Vector3 targetPos = obj.transform.position;
                    if (!IsTargetOutOfBounds(targetPos)) // Check boundaries
                    {
                        float distSqr = (targetPos - transform.position).sqrMagnitude;
                        if (distSqr < closestDistSqr)
                    {
                            closestDistSqr = distSqr;
                            foundTarget = obj.transform;
                    }
                    }
                }
            }
        }

        currentTarget = foundTarget; // Assign the closest valid target found (null if none)
    }

    // --- ALSO NEED TO FIX COLLISION ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (targetTags.Contains(other.gameObject.tag))
        {
            // --- Determine Owner Role (who fired this) ---
            PlayerRole ownerRole = PlayerRole.None; // Renamed for clarity
            if (PlayerDataManager.Instance != null) 
            {
                PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
                if (ownerData.HasValue) { ownerRole = ownerData.Value.Role; }
            }
            // -------------------------------------------

            // --- Check if the hit target belongs to the SAME player ---
            PlayerRole hitTargetRole = PlayerRole.None;
            bool damageApplied = false;

            if (other.TryGetComponent<FairyController>(out FairyController fairy))
            {
                hitTargetRole = fairy.GetOwnerRole();
                // Check if SAME owner
                if (hitTargetRole != PlayerRole.None && hitTargetRole == ownerRole) // Check if SAME owner
                {
                    fairy.ApplyDamageServer(damage, ownerRole); // Damage attributed to owner
                damageApplied = true;
                }
            }
            else if (other.TryGetComponent<SpiritController>(out SpiritController spirit))
            {
                 hitTargetRole = spirit.GetOwnerRole();
                 // Check if SAME owner
                 if (hitTargetRole != PlayerRole.None && hitTargetRole == ownerRole) // Check if SAME owner
                 {
                    spirit.ApplyDamageServer(damage, ownerRole); // Damage attributed to owner
                damageApplied = true;
            }
            }

            // Despawn only if we successfully damaged a valid target (owned by self)
            if (damageApplied)
            {
                DespawnInternal();
            }
             // Note: If it hits an enemy belonging to the opponent, it passes through harmlessly
        }
    }

    // Helper for despawning
    private void DespawnInternal()
    {
         if (this != null && IsSpawned && IsServer)
         {
             canSeek = false; // Stop seeking immediately
             NetworkObject netObj = GetComponent<NetworkObject>();
             if (netObj != null)
             {
                 netObj.Despawn(true);
             }
        }
    }
} 