using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for LINQ
using Unity.Collections; // Required for FixedString used in FairyWaveData

/// <summary>
/// [Server Only] Responsible for calculating parameters for waves of Fairy enemies and 
/// sending commands via ClientRpc to <see cref="FairySpawnNetworkHandler"/> for clients to spawn them.
/// Retrieves paths from <see cref="PathManager"/>.
/// Includes logic for randomizing wave size, path selection, great fairy chance, and optional extra attack triggers.
/// </summary>
public class FairySpawner : MonoBehaviour
{
    [Tooltip("Player index this spawner creates waves for (0 for Player 1, 1 for Player 2).")]
    [SerializeField] private int playerIndex; // 0 for P1, 1 for P2

    // Removed Prefab references - we now only need the IDs clients expect
    // [Header("Prefabs")]
    // [SerializeField] private GameObject normalFairyPrefab;
    // [SerializeField] private GameObject greatFairyPrefab;

    [Header("Client Prefab IDs")]
    [Tooltip("The Prefab ID the client pool uses for normal fairies (e.g., NormalFairyClient).")]
    [SerializeField] private string normalFairyClientPrefabID = "NormalFairyClient";
    [Tooltip("The Prefab ID the client pool uses for great fairies (e.g., GreatFairyClient).")]
    [SerializeField] private string greatFairyClientPrefabID = "GreatFairyClient";

    [Header("Spawning Configuration")]
    [Tooltip("Time in seconds between sending each wave spawn command.")]
    [SerializeField] private float spawnInterval = 5f;
    [Tooltip("Minimum number of fairies per wave.")]
    [SerializeField] private int minFairiesPerLine = 6;
    [Tooltip("Maximum number of fairies per wave.")]
    [SerializeField] private int maxFairiesPerLine = 10;
    [Tooltip("Probability (0-1) that the first and/or last fairy in a wave will be a 'great' fairy.")]
    [SerializeField] [Range(0f, 1f)] private float greatFairyChance = 0.2f;
    [Tooltip("Delay in seconds between spawning individual fairies within the wave (sent to client).")]
    [SerializeField] private float delayBetweenFairies = 0.3f;
    [Tooltip("If true, waves have a 50% chance to spawn from the end of the path instead of the beginning.")]
    [SerializeField] private bool allowReverseSpawning = true;

    [Header("Extra Attack Trigger (Server Only Calculation)")]
    [Tooltip("If enabled, every N waves, one fairy index will be marked as an extra attack trigger.")]
    [SerializeField] private int extraAttackTriggerWaveInterval = 4;
    [Tooltip("Master toggle for calculating the extra attack trigger index.")]
    [SerializeField] private bool enableExtraAttackTrigger = true;

    private Coroutine spawnCoroutine;
    private int waveCounter = 0;
    public bool isDebugSpawningEnabled = true;

    void Start()
    {
        // Simple validation for IDs
        if (string.IsNullOrEmpty(normalFairyClientPrefabID) || string.IsNullOrEmpty(greatFairyClientPrefabID))
        {
            Debug.LogError($"Server FairySpawner P{playerIndex} has missing client Prefab IDs! Disabling.", this);
            this.enabled = false;
            return;
        }

        // Only start spawning on the server
        if (NetworkManager.Singleton.IsServer)
        {
            InitializeAndStartSpawning();
        }
        else
        {
            this.enabled = false; // Disable component on clients
        }
    }

    /// <summary>
    /// [Server Only] Initializes the spawner and starts the spawning loop.
    /// </summary>
    public void InitializeAndStartSpawning()
    {
        if (!NetworkManager.Singleton.IsServer) return; // Extra safety

        if (spawnCoroutine == null) 
        {
            spawnCoroutine = StartCoroutine(ServerSpawnLoop());
        }
    }

    /// <summary>
    /// [Server Only] The main coroutine loop responsible for calculating fairy wave parameters 
    /// and sending spawn commands to clients via RPC.
    /// </summary>
    private IEnumerator ServerSpawnLoop()
    {
        List<BezierSpline> paths = PathManager.Instance?.GetPathsForPlayer(playerIndex);
        if (paths == null || paths.Count == 0)
        {
            Debug.LogError($"Server Spawner {playerIndex} could not get paths from PathManager, stopping loop.");
            yield break;
        }

        // Wait until the NetworkHandler instance is ready (clients might connect later)
        yield return new WaitUntil(() => FairySpawnNetworkHandler.Instance != null);

        while (true)
        {
            while (!isDebugSpawningEnabled)
            {
                yield return null; 
            }

            yield return new WaitForSeconds(spawnInterval);

            // ADDED: Re-check flag after spawnInterval before proceeding
            if (!isDebugSpawningEnabled) 
            {
                // Debug.Log($"[FairySpawner P{playerIndex}] Spawning PAUSED after interval, skipping wave."); // Optional log
                continue; // Skip this iteration if spawning was disabled during the interval
            }

            if (paths.Count == 0) continue;

            waveCounter++;

            // --- Calculate all wave parameters --- 
            int pathIndex = Random.Range(0, paths.Count);
            // Ensure chosen path is valid before proceeding (though list check should suffice)
            if (paths[pathIndex] == null)
            {
                 Debug.LogWarning($"Server Spawner {playerIndex} selected null path at index {pathIndex}! Skipping wave.");
                 continue;
            }
            
            int fairyCount = Random.Range(minFairiesPerLine, maxFairiesPerLine + 1);
            bool spawnAtBeginning = allowReverseSpawning ? (Random.value < 0.5f) : true;
            bool firstIsGreat = (fairyCount > 0) && (Random.value < greatFairyChance);
            bool lastIsGreat = (fairyCount > 1) && (Random.value < greatFairyChance);
            
            int triggerFairyIndex = -1; 
            bool isTriggerWave = enableExtraAttackTrigger && (waveCounter % extraAttackTriggerWaveInterval == 0);
            if (isTriggerWave && fairyCount > 0)
            {
                triggerFairyIndex = Random.Range(0, fairyCount);
            }
            // --- End Parameter Calculation --- 

            // --- Create and Populate FairyWaveData --- 
            FairyWaveData waveToSend = new FairyWaveData
            {
                PlayerAreaIdentifier = this.playerIndex,
                PathId = pathIndex, // Send the index, client will resolve using PathManager
                FairyCount = fairyCount,
                SpawnAtBeginning = spawnAtBeginning,
                DelayBetweenFairies = this.delayBetweenFairies,
                FirstIsGreat = firstIsGreat,
                LastIsGreat = lastIsGreat,
                TriggerFairyIndex = triggerFairyIndex,
                NormalFairyPrefabID = this.normalFairyClientPrefabID, // Assign configured ID
                GreatFairyPrefabID = this.greatFairyClientPrefabID   // Assign configured ID
            };
            // --- End Populate Data ---

            // --- Send ClientRpc --- 
            // Check instance just in case it becomes null mid-game (unlikely but safe)
            if (FairySpawnNetworkHandler.Instance != null)
            {
                 FairySpawnNetworkHandler.Instance.SpawnFairyWaveClientRpc(waveToSend);
                 // Debug.Log($"Server Spawner {playerIndex} sent wave command: Path {pathIndex}, Count {fairyCount}");
            }
            else
            {
                Debug.LogError("[Server FairySpawner] FairySpawnNetworkHandler.Instance became null! Cannot send wave command.");
                 // Maybe try to wait again? Or stop the loop?
                 yield return new WaitUntil(() => FairySpawnNetworkHandler.Instance != null);
            }
            // --- End Send ClientRpc ---

            // Removed the old per-fairy spawning loop entirely
        }
    }

    void OnDestroy()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    /// <summary>
    /// [Server Only] Sets the debug flag to enable/disable calculating and sending spawn commands.
    /// </summary>
    public void SetSpawningEnabledServer(bool enabled)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        isDebugSpawningEnabled = enabled;
        Debug.Log($"Server Fairy Spawner (Player {playerIndex}) command sending set to: {enabled}");
    }
} 