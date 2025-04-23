using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles the server-side initialization of essential gameplay prefabs, specifically the fairy spawners for each player.
/// This script ensures that the required spawners are instantiated and activated once when the network session starts.
/// It should be placed on a GameObject that exists only on the server or is enabled only on the server.
/// </summary>
public class GameInitializer : NetworkBehaviour
{
    [Header("Spawner Prefabs")]
    [Tooltip("The prefab containing the FairySpawner component for Player 1.")]
    [SerializeField] private GameObject player1FairySpawnerPrefab;
    [Tooltip("The prefab containing the FairySpawner component for Player 2.")]
    [SerializeField] private GameObject player2FairySpawnerPrefab;

    /// <summary>Flag to ensure spawner initialization logic runs only once.</summary>
    private bool spawnersInitialized = false;

    /// <summary>
    /// Called when the network object is spawned.
    /// If this instance is the server and initialization hasn't occurred yet,
    /// it proceeds to spawn and initialize the fairy spawner prefabs for both players
    /// using <see cref="SpawnSpawnerPrefab"/>.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Ensure this only runs once on the server
        if (!IsServer || spawnersInitialized) return; 

        SpawnSpawnerPrefab(player1FairySpawnerPrefab, "Player 1");
        SpawnSpawnerPrefab(player2FairySpawnerPrefab, "Player 2");

        spawnersInitialized = true; // Mark as initialized
    }

    /// <summary>
    /// [Server Only] Instantiates a given spawner prefab, retrieves its <see cref="FairySpawner"/> component,
    /// and calls its initialization method.
    /// Includes error handling for null prefabs and missing components.
    /// </summary>
    /// <param name="prefab">The spawner GameObject prefab to instantiate.</param>
    /// <param name="playerIdentifier">A string identifier for logging purposes (e.g., "Player 1").</param>
    private void SpawnSpawnerPrefab(GameObject prefab, string playerIdentifier)
    {
        if (prefab == null)
        {
            Debug.LogError($"GameInitializer: Spawner prefab for {playerIdentifier} is not assigned.", this);
            return;
        }

        GameObject spawnerInstance = null; // Declare outside try block
        try
        {
            // Instantiate the prefab (server-side only)
            spawnerInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            
            // Get the FairySpawner component
            FairySpawner fairySpawner = spawnerInstance.GetComponent<FairySpawner>();
            if (fairySpawner != null)
            {
                // Call the initialization method directly
                fairySpawner.InitializeAndStartSpawning();
            }
            else
            {
                Debug.LogError($"GameInitializer: Prefab for {playerIdentifier} ({prefab.name}) is missing the required FairySpawner component. Destroying instance.", this);
                Destroy(spawnerInstance);
            }
        }
        catch (System.Exception ex) // Catch specific exception
        {
            Debug.LogError($"GameInitializer: Exception occurred while spawning or initializing spawner for {playerIdentifier}. Prefab: {prefab.name}\nException: {ex}", this);
            // Consider destroying the instance if it exists and exception occurred after instantiation
            if(spawnerInstance != null) Destroy(spawnerInstance); // Now it's safe to check/destroy
        }
    }
} 