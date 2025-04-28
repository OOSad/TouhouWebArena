using UnityEngine;
using Unity.Netcode; // Re-added for NetworkObject interaction
using System.Collections;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for LINQ

/// <summary>
/// [Server Only] Responsible for spawning waves (lines) of Fairy enemies along predefined paths for a specific player.
/// This component is expected to be instantiated and initialized by <see cref="GameInitializer"/> on the server.
/// It retrieves paths from <see cref="PathManager"/>, uses the <see cref="NetworkObjectPool"/> for fairy instances,
/// and configures each spawned <see cref="Fairy"/> via its Initialize method.
/// Includes logic for randomizing wave size, path selection, great fairy chance, and optional extra attack triggers.
/// </summary>
public class FairySpawner : MonoBehaviour
{
    [Tooltip("Player index this spawner belongs to (0 for Player 1, 1 for Player 2). Determines paths and ownership.")]
    [SerializeField] private int playerIndex; // 0 for P1, 1 for P2

    [Header("Prefabs")]
    [Tooltip("The prefab used for spawning standard fairies.")]
    [SerializeField] private GameObject normalFairyPrefab;
    [Tooltip("The prefab used for spawning 'great' fairies (typically first/last in a line).")]
    [SerializeField] private GameObject greatFairyPrefab;

    [Header("Spawning Configuration")]
    [Tooltip("Time in seconds between the start of each new line of fairies.")]
    [SerializeField] private float spawnInterval = 5f; // Time between spawning lines
    [Tooltip("Minimum number of fairies to spawn in a single line.")]
    [SerializeField] private int minFairiesPerLine = 6;
    [Tooltip("Maximum number of fairies to spawn in a single line.")]
    [SerializeField] private int maxFairiesPerLine = 10;
    [Tooltip("Probability (0-1) that the first and/or last fairy in a line will be a 'great' fairy.")]
    [SerializeField] [Range(0f, 1f)] private float greatFairyChance = 0.2f; // Chance for first/last fairy to be great
    [Tooltip("Delay in seconds between spawning individual fairies within the same line.")]
    [SerializeField] private float delayBetweenFairies = 0.3f; // Delay spawning fairies in a line for spacing
    [Tooltip("If true, lines have a 50% chance to spawn from the end of the path instead of the beginning.")]
    [SerializeField] private bool allowReverseSpawning = true; // Allow fairies to spawn from the end of the path

    [Header("Extra Attack Trigger (Server Only)")]
    [Tooltip("If enabled, every N waves, one fairy will be marked as an extra attack trigger.")]
    [SerializeField] private int extraAttackTriggerWaveInterval = 4; // Every N waves, one fairy becomes a trigger
    [Tooltip("Master toggle for the extra attack trigger functionality.")]
    [SerializeField] private bool enableExtraAttackTrigger = true; // Toggle for this feature

    /// <summary>Reference to the main spawning coroutine.</summary>
    private Coroutine spawnCoroutine;
    /// <summary>Counter tracking the number of waves (lines) spawned.</summary>
    private int waveCounter = 0; // Counter for waves spawned
    /// <summary>If false, the spawning coroutine will pause.</summary>
    public bool isDebugSpawningEnabled = true;

    /// <summary>
    /// [Server Only] Initializes the spawner and starts the spawning loop.
    /// Called externally (e.g., by <see cref="GameInitializer"/>) after instantiation.
    /// Validates prefabs and starts the <see cref="ServerSpawnLoop"/> coroutine.
    /// </summary>
    public void InitializeAndStartSpawning()
    {
        // Basic validation
        if (normalFairyPrefab == null || greatFairyPrefab == null)
        {
            Debug.LogError($"Spawner P{playerIndex} prefab config invalid on {gameObject.name}! Disabling.", this);
            this.enabled = false;
            return;
        }

        // Since this is only called on the server now, no need for IsServer check
        if (spawnCoroutine == null) // Prevent starting multiple times
        {
            spawnCoroutine = StartCoroutine(ServerSpawnLoop());
        }
    }

    /// <summary>
    /// [Server Only] The main coroutine loop responsible for spawning waves of fairies.
    /// Runs indefinitely, waiting <see cref="spawnInterval"/> seconds between waves.
    /// Retrieves paths, selects path/count/direction, determines great/trigger fairies,
    /// gets instances from the <see cref="NetworkObjectPool"/>, spawns them, and initializes them.
    /// </summary>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator ServerSpawnLoop()
    {
        // Get path list from PathManager ONCE
        // Ensure PathManager.Instance is ready before calling this
        List<BezierSpline> paths = PathManager.Instance?.GetPathsForPlayer(playerIndex);
        if (paths == null || paths.Count == 0)
        {
            Debug.LogError($"Server Spawner {playerIndex} could not get paths from PathManager, stopping loop.");
            yield break;
        }

        while (true)
        {
            // Pause spawning if debug flag is false
            while (!isDebugSpawningEnabled)
            {
                yield return null; 
            }

            yield return new WaitForSeconds(spawnInterval);
            if (paths.Count == 0) continue;

            // Increment wave counter
            waveCounter++;

            // Determine if this is a trigger wave and select trigger index
            bool isTriggerWave = enableExtraAttackTrigger && (waveCounter % extraAttackTriggerWaveInterval == 0);
            int triggerFairyIndex = -1; // -1 means no trigger fairy this wave

            int pathIndex = Random.Range(0, paths.Count);
            int fairyCount = Random.Range(minFairiesPerLine, maxFairiesPerLine + 1);

            if (isTriggerWave && fairyCount > 0)
            {
                triggerFairyIndex = Random.Range(0, fairyCount);
            }

            bool spawnAtBeginning = allowReverseSpawning ? (Random.value < 0.5f) : true;
            bool firstIsGreat = (fairyCount > 0) && (Random.value < greatFairyChance);
            bool lastIsGreat = (fairyCount > 1) && (Random.value < greatFairyChance);

            BezierSpline chosenPath = paths[pathIndex];
            if (chosenPath == null)
            {
                 Debug.LogError($"Server Spawner {playerIndex} selected null path at index {pathIndex}!");
                 continue;
            }

            // Generate a unique ID for this line of fairies
            System.Guid currentLineId = System.Guid.NewGuid();

            for (int i = 0; i < fairyCount; i++)
            {
                // ADDED CHECK: Stop spawning line immediately if disabled mid-spawn
                if (!isDebugSpawningEnabled) break;

                bool makeGreat = false;
                if (i == 0 && firstIsGreat) { makeGreat = true; }
                else if (i == fairyCount - 1 && i != 0 && lastIsGreat) { makeGreat = true; }
                
                // Determine prefab and ID BEFORE getting from pool
                GameObject prefabToUse = makeGreat ? greatFairyPrefab : normalFairyPrefab;
                PoolableObjectIdentity identity = prefabToUse.GetComponent<PoolableObjectIdentity>();
                if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
                {
                     Debug.LogError($"[FairySpawner] Prefab '{prefabToUse.name}' is missing PoolableObjectIdentity or PrefabID! Skipping spawn.", this);
                     continue;
                }
                string prefabID = identity.PrefabID;

                // Get object from pool
                NetworkObject pooledNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);
                if (pooledNetworkObject == null)
                {
                     Debug.LogError($"[FairySpawner] Failed to get Fairy '{prefabID}' from pool. Skipping spawn.", this);
                     continue;
                }
                
                // Position and Activate
                Vector3 spawnPos = spawnAtBeginning ? chosenPath.GetPoint(0f) : chosenPath.GetPoint(1f);
                pooledNetworkObject.transform.position = spawnPos;
                pooledNetworkObject.transform.rotation = Quaternion.identity;
                pooledNetworkObject.gameObject.SetActive(true);

                // Spawn FIRST
                pooledNetworkObject.Spawn(false);

                // Get script and Initialize AFTER spawning
                Fairy fairyScript = pooledNetworkObject.GetComponent<Fairy>();
                if (fairyScript != null)
                {
                    // Determine necessary parameters for initialization
                    PlayerRole ownerRole = (this.playerIndex == 0) ? PlayerRole.Player1 : PlayerRole.Player2;
                    bool isTrigger = (i == triggerFairyIndex);

                    // Call the consolidated Initialize method
                    fairyScript.InitializeForPooling(this.playerIndex, pathIndex, spawnAtBeginning, 
                                                     currentLineId, i, 
                                                     isTrigger, ownerRole);
                }
                else
                {
                     Debug.LogError($"[FairySpawner P{playerIndex}] Pooled fairy is missing Fairy script! Returning to pool.", pooledNetworkObject);
                     NetworkObjectPool.Instance.ReturnNetworkObject(pooledNetworkObject); // Return broken obj
                     continue; // Skip to next fairy
                }

                // Only delay if there are more fairies to spawn in this line
                if (i < fairyCount - 1)
                {
                     yield return new WaitForSeconds(delayBetweenFairies);
                }
            }
        }
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Stops the active <see cref="spawnCoroutine"/> if it exists.
    /// </summary>
    void OnDestroy()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    /// <summary>
    /// [Server Only] Sets the debug flag to enable/disable fairy spawning.
    /// </summary>
    /// <param name="enabled">True to enable spawning, false to disable.</param>
    public void SetSpawningEnabledServer(bool enabled)
    {
        // Although this script should only exist on server, check anyway
        if (!NetworkManager.Singleton.IsServer) return;
        isDebugSpawningEnabled = enabled;
        UnityEngine.Debug.Log($"Fairy Spawner (Player {playerIndex}) spawning set to: {enabled}");
    }

    // TODO: Add logic to associate spawner/fairies with a specific player area if needed (using layers, tags, or parent transforms)
} 