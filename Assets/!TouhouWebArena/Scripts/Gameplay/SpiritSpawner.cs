using UnityEngine;
using System.Collections;
using Unity.Netcode;

// Spawns Spirit enemies in the designated zones
public class SpiritSpawner : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spawnZone1; // Same as StageSmallBulletSpawner zones
    [SerializeField] private Transform spawnZone2;
    [SerializeField] private GameObject spiritPrefab;

    [Header("Spawning Configuration")]
    [SerializeField] private Vector2 spawnZoneSize = new Vector2(2f, 1f); // Match bullet spawner or customize
    [SerializeField] private float spawnInterval = 2.0f; // Time between spirit spawns
    [SerializeField, Range(0f, 1f)] private float aimAtPlayerChance = 0.25f; // 25% chance to aim at player

    private void Start()
    {
        if (!ValidateReferences()) return;
        StartCoroutine(SpawnSpirits());
    }

    private bool ValidateReferences()
    {
        if (spawnZone1 == null || spawnZone2 == null)
        {
            enabled = false;
            return false;
        }

        if (spiritPrefab == null)
        {
            enabled = false;
            return false;
        }

        if (spiritPrefab.GetComponent<NetworkObject>() == null)
        {
            enabled = false;
            return false;
        }
        if (spiritPrefab.GetComponent<SpiritController>() == null)
        {
            enabled = false;
            return false;
        }
        return true;
    }

    private IEnumerator SpawnSpirits()
    {
        if (!IsServer) yield break; // Only server spawns

        // Initial delay
        yield return new WaitForSeconds(Random.Range(0f, spawnInterval));

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnSpiritInZone(spawnZone1);
            SpawnSpiritInZone(spawnZone2);
        }
    }

    private void SpawnSpiritInZone(Transform zoneCenter)
    {
        // Calculate spawn position
        Vector3 center = zoneCenter.position;
        float randomX = Random.Range(-spawnZoneSize.x / 2f, spawnZoneSize.x / 2f);
        float randomY = Random.Range(-spawnZoneSize.y / 2f, spawnZoneSize.y / 2f);
        Vector3 spawnPosition = new Vector3(center.x + randomX, center.y + randomY, center.z);

        // Instantiate locally on server
        GameObject spiritInstance = Instantiate(spiritPrefab, spawnPosition, Quaternion.identity);

        // Get components
        SpiritController spiritController = spiritInstance.GetComponent<SpiritController>();
        NetworkObject networkObject = spiritInstance.GetComponent<NetworkObject>();

        // Determine target player and aim chance
        PlayerRole targetRole = (zoneCenter == spawnZone1) ? PlayerRole.Player1 : PlayerRole.Player2;
        bool shouldAim = Random.value < aimAtPlayerChance;
        Transform targetPlayerTransform = null;

        // Get player transform if aiming
        if (shouldAim)
        {
            // --- Access Player Transform (Requires PlayerDataManager or similar) ---
            if (PlayerDataManager.Instance != null && NetworkManager.Singleton != null)
            {
                PlayerData? playerData = PlayerDataManager.Instance.GetPlayerDataByRole(targetRole);
                if (playerData.HasValue)
                {
                    NetworkObject playerNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
                    if (playerNetObj != null)
                    {
                        targetPlayerTransform = playerNetObj.transform;
                    }
                    else
                    {
                        shouldAim = false; // Fallback to not aiming
                    }
                }
                else
                {
                    shouldAim = false; // Fallback to not aiming
                }
            }
            // -----------------------------------------------------------------------
        }

        // Spawn network object
        networkObject.Spawn(true);

        // Initialize the spirit AFTER spawning
        // Pass the targetRole (owner), player transform, aim status, and zone references
        spiritController.Initialize(targetPlayerTransform, targetRole, shouldAim, spawnZone1, spawnZone2);

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; // Use a different color for spirit zones
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