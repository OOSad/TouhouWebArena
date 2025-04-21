using UnityEngine;
using System.Collections; // Keep for potential future use, but not needed for current logic
using Unity.Netcode; // Added Netcode namespace

// Inherit from NetworkBehaviour
public class StageSmallBulletSpawner : NetworkBehaviour
{
    // --- Singleton Pattern --- 
    public static StageSmallBulletSpawner Instance { get; private set; }
    // -----------------------

    [Header("Spawn Zone Setup")]
    [SerializeField] private Transform spawnZone1; // Target zone for Player 1
    [SerializeField] private Transform spawnZone2; // Target zone for Player 2
    [SerializeField] private Vector2 spawnZoneSize = new Vector2(7f, 1f); // Size of the area around the zone transform

    [Header("Bullet Prefabs")]
    [SerializeField] private GameObject smallBulletPrefab;
    [SerializeField] private GameObject stageLargeBulletPrefab; // Assign the large bullet prefab here

    [Header("Spawn Logic")]
    [SerializeField] [Range(0f, 1f)] private float largeBulletSpawnChance = 0.1f; // 10% chance default

    private void Awake()
    {
        // --- Singleton Setup ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // ---------------------
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Validate setup only on the server
        if (!IsServer) return;

        if (spawnZone1 == null || spawnZone2 == null)
        {
            enabled = false; // Disable the script if setup is incorrect
            return;
        }

        if (smallBulletPrefab == null || stageLargeBulletPrefab == null)
        {
            enabled = false;
            return;
        }

        if (smallBulletPrefab.GetComponent<NetworkObject>() == null || stageLargeBulletPrefab.GetComponent<NetworkObject>() == null)
        {
            enabled = false;
            return;
        }
        
        if (smallBulletPrefab.GetComponent<StageSmallBulletMoverScript>() == null || stageLargeBulletPrefab.GetComponent<StageSmallBulletMoverScript>() == null)
        {
            enabled = false;
            return;
        }
    }

    // Use override and call base method
    public override void OnDestroy() // Changed signature to public override
    {
        // Custom Singleton Cleanup first
        if (Instance == this)
        {
            Instance = null;
        }

        // Call the base NetworkBehaviour cleanup
        base.OnDestroy(); 
    }

    /// <summary>
    /// Called by a Fairy when it's destroyed by a specific player (killerRole).
    /// Spawns a bullet (small or large) in the opponent's spawn zone.
    /// This method MUST only be called on the server.
    /// </summary>
    /// <param name="killerRole">The role of the player who defeated the fairy.</param>
    public void SpawnBulletForOpponent(PlayerRole killerRole)
    {
        // --- SERVER CHECK & Killer Validation --- 
        if (!IsServer)
        {
            return;
        }
        if (killerRole == PlayerRole.None)
        {
            // Don't spawn bullets if the killer is unknown (e.g., cleared by bomb)
            return; 
        }
        // ----------------------------------------

        // --- Determine Target --- 
        PlayerRole targetRole = (killerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
        Transform targetZone = (targetRole == PlayerRole.Player1) ? spawnZone1 : spawnZone2;

        if (targetZone == null) // Safety check
        {
            return;
        }
        // ----------------------

        // --- Choose Bullet Prefab --- 
        GameObject prefabToSpawn;
        if (Random.value < largeBulletSpawnChance)
        {
            prefabToSpawn = stageLargeBulletPrefab;
        }
        else
        {
            prefabToSpawn = smallBulletPrefab;
        }
        // --------------------------

        // --- Get Prefab ID --- 
        PoolableObjectIdentity identity = prefabToSpawn.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
             Debug.LogError($"Stage bullet prefab '{prefabToSpawn.name}' is missing PoolableObjectIdentity or PrefabID. Cannot get from pool.", prefabToSpawn);
             return;
        }
        string prefabID = identity.PrefabID;

        // --- Calculate Spawn Position --- 
        Vector3 center = targetZone.position;
        float randomX = Random.Range(-spawnZoneSize.x / 2f, spawnZoneSize.x / 2f);
        float randomY = Random.Range(-spawnZoneSize.y / 2f, spawnZoneSize.y / 2f);
        Vector3 spawnPosition = new Vector3(center.x + randomX, center.y + randomY, center.z);
        // ------------------------------

        // --- Get from Pool, Position, Activate --- 
        NetworkObject networkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);

        if (networkObject == null)
        {
             Debug.LogError($"Failed to get stage bullet object with ID '{prefabID}' from NetworkObjectPool.", this);
             return;
        }

        networkObject.transform.position = spawnPosition;
        networkObject.transform.rotation = Quaternion.identity; // Assuming stage bullets don't need specific rotation
        networkObject.gameObject.SetActive(true);
        // -------------------------------------------

        // Get components needed AFTER activation/positioning
        StageSmallBulletMoverScript bulletMover = networkObject.GetComponent<StageSmallBulletMoverScript>();

        // Error checking before spawn
        if (bulletMover == null)
        {
            Debug.LogError($"Pooled stage bullet '{networkObject.name}' is missing StageSmallBulletMoverScript! Returning to pool.", networkObject.gameObject);
             NetworkObjectPool.Instance.ReturnNetworkObject(networkObject); // Return immediately
            return;
        }

        // Spawn the instance across the network first
        networkObject.Spawn(true); // true = despawn with server

         // Set parent AFTER spawning
        if (NetworkObjectPool.Instance != null)
        {
            networkObject.transform.SetParent(NetworkObjectPool.Instance.transform, worldPositionStays: false);
        }

        // Set Target Player Role AFTER SPAWN 
        bulletMover.TargetPlayerRole.Value = targetRole;
        // ------------------------------
    }

    // Draw visual aids in the editor to see the spawn zones
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; // Changed color for clarity
        if (spawnZone1 != null)
        {
            Gizmos.DrawWireCube(spawnZone1.position, new Vector3(spawnZoneSize.x, spawnZoneSize.y, 0f));
        }
        if (spawnZone2 != null)
        {
            Gizmos.DrawWireCube(spawnZone2.position, new Vector3(spawnZoneSize.x, spawnZoneSize.y, 0f));
        }
    }
} 