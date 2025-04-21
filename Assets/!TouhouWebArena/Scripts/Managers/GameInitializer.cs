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
                Destroy(spawnerInstance);
            }
        }
        catch (System.Exception)
        {
            // Empty catch block - Silences the warning definitively if discard '_' doesn't work
        }
    }
} 