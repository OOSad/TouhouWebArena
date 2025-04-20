using UnityEngine;
using Unity.Netcode; // Re-added for NetworkObject interaction
using System.Collections;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for LINQ

// Changed from NetworkBehaviour to MonoBehaviour
public class FairySpawner : MonoBehaviour
{
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

    [Header("Extra Attack Trigger (Server Only)")]
    [SerializeField] private int extraAttackTriggerWaveInterval = 4; // Every N waves, one fairy becomes a trigger
    [SerializeField] private bool enableExtraAttackTrigger = true; // Toggle for this feature

    private Coroutine spawnCoroutine;
    private int waveCounter = 0; // Counter for waves spawned

    // Changed from OnNetworkSpawn, called by GameInitializer on the server
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

    // This method MUST only be called on the server
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
                bool makeGreat = false;
                if (i == 0 && firstIsGreat) { makeGreat = true; }
                else if (i == fairyCount - 1 && i != 0 && lastIsGreat) { makeGreat = true; }
                GameObject prefabToSpawn = makeGreat ? greatFairyPrefab : normalFairyPrefab;

                Vector3 spawnPos = spawnAtBeginning ? chosenPath.GetPoint(0f) : chosenPath.GetPoint(1f);
                GameObject fairyInstance = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                NetworkObject fairyNetworkObject = fairyInstance.GetComponent<NetworkObject>();

                if (fairyNetworkObject != null)
                {
                    // Spawn the fairy NetworkObject FIRST
                    fairyNetworkObject.Spawn(true);

                    Fairy fairyScript = fairyInstance.GetComponent<Fairy>();
                    if (fairyScript != null)
                    {
                        // Set NetworkVariables on the Fairy script immediately after spawning
                        fairyScript.SetPathInfo(this.playerIndex, pathIndex, spawnAtBeginning);
                        // --- NEW: Assign Line ID and Index ---
                        fairyScript.AssignLineInfo(currentLineId, i);
                        // -------------------------------------

                        // --- NEW: Mark trigger fairy ---
                        if (i == triggerFairyIndex) // Check if this is the designated trigger fairy
                        {
                            fairyScript.MarkAsExtraAttackTrigger(); 
                            // Optional: Add visual indication here if needed
                        }
                        // ------------------------------

                        // --- Assign Owner Role --- 
                        PlayerRole ownerRole = (this.playerIndex == 0) ? PlayerRole.Player1 : PlayerRole.Player2;
                        fairyScript.AssignOwnerRole(ownerRole);
                        // --------------------------
                    }
                    else
                    {
                         Debug.LogError("Spawned fairy is missing Fairy script! Cannot set path info. Destroying.", fairyInstance);
                         Destroy(fairyInstance);
                         continue; // Skip to next fairy
                    }
                }
                else
                {
                    Debug.LogError("Spawned fairy is missing NetworkObject! Destroying.", fairyInstance);
                    Destroy(fairyInstance);
                }

                // Only delay if there are more fairies to spawn in this line
                if (i < fairyCount - 1)
                {
                     yield return new WaitForSeconds(delayBetweenFairies);
                }
            }
        }
    }

    // Optional: Add OnDestroy to stop coroutine if the spawner GO is destroyed
    void OnDestroy()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    // TODO: Add logic to associate spawner/fairies with a specific player area if needed (using layers, tags, or parent transforms)
} 