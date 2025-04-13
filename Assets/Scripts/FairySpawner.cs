using UnityEngine;
using Unity.Netcode; // Import Netcode namespace
using System.Collections;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for LINQ

// Inherit from NetworkBehaviour
public class FairySpawner : NetworkBehaviour
{
    [Header("Network Config")]
    [SerializeField] private int playerIndex; // 0 for P1, 1 for P2

    [Header("Prefabs")]
    [SerializeField] private GameObject normalFairyPrefab;
    [SerializeField] private GameObject greatFairyPrefab;

    [Header("Spawning Configuration")]
    [SerializeField] private float spawnInterval = 5f; // Time between spawning lines
    [SerializeField] private int minFairiesPerLine = 6;
    [SerializeField] private int maxFairiesPerLine = 10;
    [SerializeField] [Range(0f, 1f)] private float greatFairyChance = 0.2f; // Chance for first/last fairy to be great
    [SerializeField] private float delayBetweenFairies = 0.3f; // Delay spawning fairies in a line for spacing
    [SerializeField] private bool allowReverseSpawning = true; // Allow fairies to spawn from the end of the path

    private Coroutine spawnCoroutine;

    // Called when the NetworkObject is spawned (on server and clients)
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // No longer need to find paths here, PathManager handles it
        // Debug.Log($"[{NetworkManager.Singleton.LocalClientId}] Spawner {playerIndex} OnNetworkSpawn.");
        // FindAndAssignPaths(); 

        // Basic validation
        if (normalFairyPrefab == null || greatFairyPrefab == null)
        {
            Debug.LogError($"Spawner P{playerIndex} prefab config invalid on {gameObject.name}! Disabling.", this);
            this.enabled = false;
            return;
        }
        // Path validation might happen in the loop now, checking PathManager

        if (!IsServer) return;
        spawnCoroutine = StartCoroutine(ServerSpawnLoop());
    }

    private IEnumerator ServerSpawnLoop()
    {
        // Get path list from PathManager ONCE (assuming paths don't change)
        List<BezierSpline> paths = PathManager.Instance?.GetPathsForPlayer(playerIndex);
        if (paths == null || paths.Count == 0)
        {
            Debug.LogError($"Server Spawner {playerIndex} could not get paths from PathManager, stopping loop.");
            yield break;
        }
        Debug.Log($"Server Spawner {playerIndex} found {paths.Count} paths via PathManager.");

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (paths.Count == 0) continue; // Should have been caught above, but safety

            int pathIndex = Random.Range(0, paths.Count);
            int fairyCount = Random.Range(minFairiesPerLine, maxFairiesPerLine + 1);
            bool spawnAtBeginning = allowReverseSpawning ? (Random.value < 0.5f) : true;
            bool firstIsGreat = (fairyCount > 0) && (Random.value < greatFairyChance);
            bool lastIsGreat = (fairyCount > 1) && (Random.value < greatFairyChance);

            // Get the specific path from the list retrieved earlier
            BezierSpline chosenPath = paths[pathIndex]; 
            if (chosenPath == null)
            {
                 Debug.LogError($"Server Spawner {playerIndex} selected null path at index {pathIndex}!");
                 continue; // Skip this iteration
            }

            for (int i = 0; i < fairyCount; i++)
            {
                bool makeGreat = false;
                if (i == 0 && firstIsGreat) { makeGreat = true; }
                else if (i == fairyCount - 1 && i != 0 && lastIsGreat) { makeGreat = true; }
                GameObject prefabToSpawn = makeGreat ? greatFairyPrefab : normalFairyPrefab;

                Vector3 spawnPos = spawnAtBeginning ? chosenPath.GetPoint(0f) : chosenPath.GetPoint(1f);
                GameObject fairyInstance = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                NetworkObject fairyNetworkObject = fairyInstance.GetComponent<NetworkObject>();

                if (fairyNetworkObject != null)
                {
                    fairyNetworkObject.Spawn(true);

                    // --- Wait 1 second before sending RPC --- NO LONGER NEEDED
                    // yield return new WaitForSeconds(1.0f); 
                    // -----------------------------------------

                    // Get the SplineWalker component on the server instance
                    SplineWalker walker = fairyInstance.GetComponent<SplineWalker>();
                    if (walker != null)
                    {
                        // --- SERVER-SIDE INITIALIZATION ---
                        // Server needs the actual path object to initialize its own walker
                        BezierSpline chosenPathForServer = paths[pathIndex]; 
                        if(chosenPathForServer != null)
                        {
                           walker.InitializeOnServer(chosenPathForServer, spawnAtBeginning);
                        }
                        else
                        {
                            Debug.LogError($"[Server Spawner {playerIndex}] Failed to find path index {pathIndex} for server-side initialization!", this);
                            // Handle error - maybe destroy fairy?
                        }
                        // ---------------------------------

                        // Call the ClientRpc directly on the fairy's SplineWalker to initialize clients
                        walker.InitializePathClientRpc(this.playerIndex, pathIndex, spawnAtBeginning);
                    }
                    else
                    {
                        Debug.LogError($"Spawned Fairy Prefab missing SplineWalker component!", fairyInstance);
                        // Optionally destroy the fairy if the walker is missing
                        // Destroy(fairyInstance); 
                        // continue;
                    }
                }
                else
                {
                    Debug.LogError("Spawned Fairy Prefab missing NetworkObject!", fairyInstance);
                    Destroy(fairyInstance);
                    continue;
                }

                if (i < fairyCount - 1)
                {
                    yield return new WaitForSeconds(delayBetweenFairies);
                }
            }
        }
    }

    // OnDestroy might be useful if the server needs to clean up anything when the spawner is destroyed
    // but the coroutine stopping logic might not be needed if relying on object destruction.
    // public override void OnNetworkDespawn()
    // {
    //     if (IsServer && spawnCoroutine != null)
    //     {
    //         StopCoroutine(spawnCoroutine);
    //     }
    //     base.OnNetworkDespawn();
    // }

    // TODO: Add logic to associate spawner/fairies with a specific player area if needed (using layers, tags, or parent transforms)
} 