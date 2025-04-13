using UnityEngine;
using System.Collections;
using Unity.Netcode; // Added Netcode namespace

// Inherit from NetworkBehaviour
public class StageSmallBulletSpawner : NetworkBehaviour
{
    [SerializeField] private Transform spawnZone1;
    [SerializeField] private Transform spawnZone2;
    [SerializeField] private Vector2 spawnZoneSize = new Vector2(2f, 1f); // Default size, adjust in editor
    [SerializeField] private GameObject smallBulletPrefab;
    [SerializeField] private float spawnInterval = 0.5f; // Time between spawns

    private void Start()
    {
        if (spawnZone1 == null || spawnZone2 == null)
        {
            Debug.LogError("Spawn zones not assigned in StageSmallBulletSpawner.", this);
            enabled = false; // Disable the script if setup is incorrect
            return;
        }

        if (smallBulletPrefab == null)
        {
            Debug.LogError("Small bullet prefab not assigned in StageSmallBulletSpawner.", this);
            enabled = false; // Disable the script if setup is incorrect
            return;
        }

        // Ensure the prefab has a NetworkObject component
        if (smallBulletPrefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("Small bullet prefab is missing a NetworkObject component.", this);
            enabled = false; // Disable the script if setup is incorrect
            return;
        }

        StartCoroutine(SpawnBullets());
    }

    private IEnumerator SpawnBullets()
    {
        // --- SERVER CHECK --- Only the server should spawn bullets
        if (!IsServer) yield break;

        // Wait a small initial delay before starting the loop
        yield return new WaitForSeconds(Random.Range(0f, spawnInterval)); 
        
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Spawn one bullet in each zone (on the server)
            SpawnBulletInZone(spawnZone1);
            SpawnBulletInZone(spawnZone2);
        }
    }

    // This method now only runs on the server because SpawnBullets checks IsServer
    private void SpawnBulletInZone(Transform zoneCenter)
    {
        // Calculate random position within the specified zone
        Vector3 center = zoneCenter.position;
        float randomX = Random.Range(-spawnZoneSize.x / 2f, spawnZoneSize.x / 2f);
        float randomY = Random.Range(-spawnZoneSize.y / 2f, spawnZoneSize.y / 2f);
        // Use the zone's z position for the bullet's z position
        Vector3 spawnPosition = new Vector3(center.x + randomX, center.y + randomY, center.z); 

        // Instantiate the bullet locally on the server first
        GameObject bulletInstance = Instantiate(smallBulletPrefab, spawnPosition, Quaternion.identity);
        
        // Get components needed before spawn
        StageSmallBulletMoverScript bulletMover = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
        NetworkObject networkObject = bulletInstance.GetComponent<NetworkObject>();

        // Error checking before spawn
        if (bulletMover == null)
        {
            Debug.LogError("Instantiated bullet is missing StageSmallBulletMoverScript!", bulletInstance);
            Destroy(bulletInstance);
            return;
        }
        if (networkObject == null)
        {
            Debug.LogError("Instantiated bullet is missing NetworkObject!", bulletInstance);
            Destroy(bulletInstance);
            return;
        }
        
        // --- NETWORK SPAWN --- Spawn the instance across the network first
        networkObject.Spawn(true); // true = despawn with server

        // --- Set Target Player Role AFTER SPAWN --- 
        if (zoneCenter == spawnZone1)
        {
            bulletMover.TargetPlayerRole.Value = PlayerRole.Player1;
        }
        else if (zoneCenter == spawnZone2)
        {
            bulletMover.TargetPlayerRole.Value = PlayerRole.Player2;
        }
        else
        {
            Debug.LogWarning("Spawn zone not recognized, setting bullet TargetPlayerRole to None.");
            bulletMover.TargetPlayerRole.Value = PlayerRole.None; 
        }
        // --- End Set Target Player Role ---
    }

    // Draw visual aids in the editor to see the spawn zones
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
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