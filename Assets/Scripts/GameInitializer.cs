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
            // Instantiate the prefab (server-side only)
            GameObject spawnerInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            
            // Get the FairySpawner component
            FairySpawner fairySpawner = spawnerInstance.GetComponent<FairySpawner>();
            if (fairySpawner != null)
            {
                // Call the initialization method directly
                fairySpawner.InitializeAndStartSpawning();
            }
            else
            {
                // Prefab is missing the required script
                Debug.LogError($"[Server] Fairy Spawner Prefab for {playerIdentifier} is missing the FairySpawner script! Destroying instance.", spawnerInstance);
                Destroy(spawnerInstance);
            }
        }
        catch (System.Exception e)
        {
            // Updated error message slightly
            Debug.LogError($"[Server] Failed to instantiate or initialize spawner for {playerIdentifier}. Error: {e.Message}", this);
        }
    }
} 