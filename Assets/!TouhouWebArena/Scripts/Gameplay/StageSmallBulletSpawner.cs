using UnityEngine;
using System.Collections; // Keep for potential future use, but not needed for current logic
using Unity.Netcode; // Added Netcode namespace

/// <summary>
/// [Server Only Singleton] Manages the spawning of basic stage bullets (small and large).
/// Primarily triggered when a Fairy is defeated (<see cref="SpawnBulletForOpponent"/>),
/// spawning a bullet in the opponent's designated zone.
/// Uses the <see cref="NetworkObjectPool"/> for bullet instances.
/// </summary>
public class StageSmallBulletSpawner : NetworkBehaviour
{
    /// <summary>Singleton instance of the StageSmallBulletSpawner.</summary>
    public static StageSmallBulletSpawner Instance { get; private set; }

    [Header("Spawn Zone Setup")]
    [Tooltip("Transform defining the center of the bullet spawn zone for Player 1's side.")]
    [SerializeField] private Transform spawnZone1;
    [Tooltip("Transform defining the center of the bullet spawn zone for Player 2's side.")]
    [SerializeField] private Transform spawnZone2;
    [Tooltip("The dimensions (Width, Height) of the rectangular spawn zones.")]
    [SerializeField] private Vector2 spawnZoneSize = new Vector2(7f, 1f);

    [Header("Bullet Prefabs")]
    [Tooltip("Prefab for the standard small stage bullet. Requires NetworkObject, PoolableObjectIdentity, StageSmallBulletMoverScript.")]
    [SerializeField] private GameObject smallBulletPrefab;
    [Tooltip("Prefab for the larger stage bullet. Requires NetworkObject, PoolableObjectIdentity, StageSmallBulletMoverScript.")]
    [SerializeField] private GameObject stageLargeBulletPrefab;

    [Header("Spawn Logic")]
    [Tooltip("Probability (0-1) that a large bullet will spawn instead of a small one.")]
    [SerializeField] [Range(0f, 1f)] private float largeBulletSpawnChance = 0.1f;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Sets up the singleton instance.
    /// </summary>
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

    /// <summary>
    /// Called when the network object is spawned.
    /// Validates required references and prefab components on the server.
    /// Disables the script if validation fails.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Validate setup only on the server
        if (!IsServer) return;

        if (spawnZone1 == null || spawnZone2 == null)
        {
            Debug.LogError("StageSmallBulletSpawner: Spawn Zone 1 or Spawn Zone 2 not assigned.", this);
            enabled = false; // Disable the script if setup is incorrect
            return;
        }

        if (smallBulletPrefab == null || stageLargeBulletPrefab == null)
        {
            Debug.LogError("StageSmallBulletSpawner: Small or Large Bullet Prefab not assigned.", this);
            enabled = false;
            return;
        }

        if (smallBulletPrefab.GetComponent<NetworkObject>() == null || stageLargeBulletPrefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("StageSmallBulletSpawner: Bullet Prefabs must have NetworkObject component.", this);
            enabled = false;
            return;
        }
        
        if (smallBulletPrefab.GetComponent<StageSmallBulletMoverScript>() == null || stageLargeBulletPrefab.GetComponent<StageSmallBulletMoverScript>() == null)
        {
            Debug.LogError("StageSmallBulletSpawner: Bullet Prefabs must have StageSmallBulletMoverScript component.", this);
            enabled = false;
            return;
        }

        if (smallBulletPrefab.GetComponent<PoolableObjectIdentity>() == null || string.IsNullOrEmpty(smallBulletPrefab.GetComponent<PoolableObjectIdentity>().PrefabID) ||
            stageLargeBulletPrefab.GetComponent<PoolableObjectIdentity>() == null || string.IsNullOrEmpty(stageLargeBulletPrefab.GetComponent<PoolableObjectIdentity>().PrefabID))
        {
            Debug.LogError("StageSmallBulletSpawner: Bullet Prefabs must have PoolableObjectIdentity component with a valid PrefabID.", this);
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Cleans up the singleton instance and calls the base NetworkBehaviour OnDestroy.
    /// </summary>
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
    /// [Server Only] Spawns a stage bullet (small or large based on chance) in the opponent's spawn zone.
    /// Called externally (e.g., by a <see cref="Fairy"/> script) when an enemy is defeated.
    /// Determines target zone, selects prefab, gets instance from <see cref="NetworkObjectPool"/>,
    /// positions it randomly within the zone, spawns the <see cref="NetworkObject"/>,
    /// and sets the target role on the bullet's <see cref="StageSmallBulletMoverScript"/>.
    /// </summary>
    /// <param name="killerRole">The <see cref="PlayerRole"/> of the player who defeated the enemy triggering the spawn.</param>
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
            Debug.LogWarning($"StageSmallBulletSpawner: Target zone for role {targetRole} is null. Cannot spawn bullet.", this);
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
            networkObject.transform.SetParent(NetworkObjectPool.Instance.transform, worldPositionStays: true); // Use worldPositionStays = true after setting position
        }

        // Set Target Player Role AFTER SPAWN 
        bulletMover.TargetPlayerRole.Value = targetRole;
        // ------------------------------
    }

    /// <summary>
    /// Draws wireframe cubes in the editor scene view to visualize the spawn zones.
    /// </summary>
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