using UnityEngine;
using Unity.Netcode;

public class GameInitializer : NetworkBehaviour
{
    [Header("Spawner Prefabs")]
    [SerializeField] private GameObject player1FairySpawnerPrefab;
    [SerializeField] private GameObject player2FairySpawnerPrefab;

    private bool spawnersInitialized = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Ensure this only runs once on the server
        if (!IsServer || spawnersInitialized) return; 

        Debug.Log("[Server] Initializing Game - Spawning Fairy Spawners...");

        SpawnSpawnerPrefab(player1FairySpawnerPrefab, "Player 1");
        SpawnSpawnerPrefab(player2FairySpawnerPrefab, "Player 2");

        spawnersInitialized = true; // Mark as initialized
    }

    private void SpawnSpawnerPrefab(GameObject prefab, string playerIdentifier)
    {
        if (prefab == null)
        {
            Debug.LogError($"[Server] Fairy Spawner Prefab for {playerIdentifier} is not assigned in GameInitializer!", this);
            return;
        }

        try
        {
            // Instantiate the prefab
            GameObject spawnerInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity); // Position doesn't matter much if it only contains logic
            
            // Get the NetworkObject and spawn it
            NetworkObject networkObject = spawnerInstance.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn(true); // Spawn and make active
                Debug.Log($"[Server] Spawned Fairy Spawner for {playerIdentifier}.");
            }
            else
            {
                Debug.LogError($"[Server] Fairy Spawner Prefab for {playerIdentifier} is missing NetworkObject component! Destroying instance.", spawnerInstance);
                Destroy(spawnerInstance);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Server] Failed to instantiate or spawn spawner for {playerIdentifier}. Prefab assigned correctly in NetworkManager? Error: {e.Message}", this);
        }
    }
} 