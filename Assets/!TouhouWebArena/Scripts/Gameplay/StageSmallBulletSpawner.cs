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

        // --- Calculate Spawn Position --- 
        Vector3 center = targetZone.position;
        float randomX = Random.Range(-spawnZoneSize.x / 2f, spawnZoneSize.x / 2f);
        float randomY = Random.Range(-spawnZoneSize.y / 2f, spawnZoneSize.y / 2f);
        // Use the zone's z position for the bullet's z position
        Vector3 spawnPosition = new Vector3(center.x + randomX, center.y + randomY, center.z);
        // ------------------------------

        // --- Instantiate and Spawn --- 
        // Instantiate the bullet locally on the server first
        GameObject bulletInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        // Get components needed before spawn
        StageSmallBulletMoverScript bulletMover = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
        NetworkObject networkObject = bulletInstance.GetComponent<NetworkObject>();

        // Error checking before spawn
        if (bulletMover == null)
        {
            Destroy(bulletInstance);
            return;
        }
        if (networkObject == null)
        {
            Destroy(bulletInstance);
            return;
        }

        // Spawn the instance across the network first
        networkObject.Spawn(true); // true = despawn with server

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