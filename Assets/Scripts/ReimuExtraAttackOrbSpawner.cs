using UnityEngine;
using System.Collections;
using Unity.Netcode;

// Spawner specifically for Reimu's Extra Attack Orbs
// This script now primarily holds the configuration for spawn zones used by PlayerDataManager.
public class ReimuExtraAttackOrbSpawner : NetworkBehaviour
{
    [Header("Spawn Zones")]
    [SerializeField] private Transform spawnZone1; // Assign the same Transform as StageSmallBulletSpawner
    [SerializeField] private Transform spawnZone2; // Assign the same Transform as StageSmallBulletSpawner
    [SerializeField] private Vector2 spawnZoneSize = new Vector2(2f, 1f); // Should match StageSmallBulletSpawner if using same zones

    // Note: The actual spawning is triggered by PlayerDataManager based on kill count.
    // The timer-based spawning logic has been removed.
    // [Header("Orb Settings")] // Removed header
    // [SerializeField] private GameObject reimuOrbPrefab; // Removed - Prefab is assigned in PlayerDataManager
    // [SerializeField] private float minSpawnInterval = 5.0f; // Removed
    // [SerializeField] private float maxSpawnInterval = 15.0f; // Removed

    // private Coroutine spawnCoroutine; // Removed

    private void Start()
    {
        // Basic validation for assigned zones
        if (spawnZone1 == null || spawnZone2 == null)
        {
            Debug.LogError("Spawn zones not assigned in ReimuExtraAttackOrbSpawner.", this);
            enabled = false;
            return;
        }

        // Removed prefab validation as PlayerDataManager handles the prefab assignment.
        /*
        if (reimuOrbPrefab == null)
        {
            Debug.LogError("Reimu Orb prefab not assigned in ReimuExtraAttackOrbSpawner.", this);
            enabled = false;
            return;
        }

        if (reimuOrbPrefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("Reimu Orb prefab is missing a NetworkObject component.", this);
            enabled = false;
            return;
        }

        if (reimuOrbPrefab.GetComponent<ReimuExtraAttackOrb>() == null)
        {
            Debug.LogError("Reimu Orb prefab is missing the ReimuExtraAttackOrb script.", this);
            enabled = false;
            return;
        }
        */
    }

    // Removed OnNetworkSpawn as coroutine start is no longer needed
    /*
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Start spawning only on the server after the network object is ready
        if (IsServer)
        {
            if (spawnCoroutine == null) // Prevent multiple coroutines if respawned
            {
                 spawnCoroutine = StartCoroutine(SpawnOrbsRoutine());
            }
        }
    }
    */

    // Removed OnNetworkDespawn as coroutine stop is no longer needed
    /*
    public override void OnNetworkDespawn()
    {
        // Stop the coroutine if the spawner is despawned or the server shuts down
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        base.OnNetworkDespawn();
    }
    */

    // Removed SpawnOrbsRoutine as spawning is triggered externally
    /*
    private IEnumerator SpawnOrbsRoutine()
    {
        // Server-only loop
        while (true)
        {
            // Wait for a random interval before spawning the next orb
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // Spawn one orb in a randomly chosen zone
            SpawnOrbInRandomZone();
        }
    }
    */

    // Removed SpawnOrbInRandomZone as spawning is triggered externally
    /*
    // Server-only method to spawn a single orb
    private void SpawnOrbInRandomZone()
    {
         // Choose zone randomly
         Transform targetZone = (Random.value < 0.5f) ? spawnZone1 : spawnZone2;
         PlayerRole targetRole = (targetZone == spawnZone1) ? PlayerRole.Player1 : PlayerRole.Player2;

         // Calculate random position within the chosen zone
         Vector3 center = targetZone.position;
         float randomX = Random.Range(-spawnZoneSize.x / 2f, spawnZoneSize.x / 2f);
         float randomY = Random.Range(-spawnZoneSize.y / 2f, spawnZoneSize.y / 2f);
         Vector3 spawnPosition = new Vector3(center.x + randomX, center.y + randomY, center.z);

         // Instantiate the orb locally on the server
         GameObject orbInstance = Instantiate(reimuOrbPrefab, spawnPosition, Quaternion.identity);

         // Get components
         ReimuExtraAttackOrb orbScript = orbInstance.GetComponent<ReimuExtraAttackOrb>();
         NetworkObject networkObject = orbInstance.GetComponent<NetworkObject>();

         // Basic error checking (more thorough checks are in Start)
         if (networkObject == null || orbScript == null)
         {
             Debug.LogError("Instantiated Reimu Orb is missing required components!", orbInstance);
             Destroy(orbInstance);
             return;
         }

         // Spawn the network object
         networkObject.Spawn(true); // true = despawn with server

         // Set the target player role on the orb script AFTER spawning
         orbScript.TargetPlayerRole.Value = targetRole;
         Debug.Log($"[Server OrbSpawner] Spawned Reimu Orb targeting {targetRole} at {spawnPosition}");
    }
    */

    // --- Public Getters for Spawn Zone Info ---
    public Transform GetSpawnZone1() => spawnZone1;
    public Transform GetSpawnZone2() => spawnZone2;
    public Vector2 GetSpawnZoneSize() => spawnZoneSize;
    // --- End Public Getters ---

    // Draw visual aids in the editor to see the spawn zones
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // Use a different color to distinguish from bullet spawner
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