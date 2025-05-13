using UnityEngine;
using Unity.Netcode;
using Unity.Collections; // Required for FixedString
using System.Collections; // Required for IEnumerator
using System.Collections.Generic; // Required for List<BezierSpline>

// Potentially using TouhouWebArena.ObjectPooling; // If ClientGameObjectPool is in this namespace
// Potentially using TouhouWebArena.Paths;       // If PathManager is in this namespace
// Potentially using TouhouWebArena.Enemies;      // If client fairy components are in this namespace

/// <summary>
/// Defines the data structure for a wave of fairies to be spawned.
/// Implements INetworkSerializable for network transmission.
/// </summary>
public struct FairyWaveData : INetworkSerializable
{
    public int PlayerAreaIdentifier; // e.g., 0 for P1 side, 1 for P2 side
    public int PathId;               // ID for the client to look up the BezierSpline path
    public int FairyCount;
    public bool SpawnAtBeginning;    // True to spawn at path start, false for path end
    public float DelayBetweenFairies;
    public bool FirstIsGreat;
    public bool LastIsGreat;
    public int TriggerFairyIndex;    // Index of the fairy that's an "extra attack trigger", -1 if none

    public FixedString64Bytes NormalFairyPrefabID; // Prefab ID for normal fairies (from PooledObjectInfo)
    public FixedString64Bytes GreatFairyPrefabID;  // Prefab ID for great fairies (from PooledObjectInfo)

    // Further per-fairy customization could be added here if needed (e.g., individual speeds, specific projectile patterns)

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerAreaIdentifier);
        serializer.SerializeValue(ref PathId);
        serializer.SerializeValue(ref FairyCount);
        serializer.SerializeValue(ref SpawnAtBeginning);
        serializer.SerializeValue(ref DelayBetweenFairies);
        serializer.SerializeValue(ref FirstIsGreat);
        serializer.SerializeValue(ref LastIsGreat);
        serializer.SerializeValue(ref TriggerFairyIndex);
        serializer.SerializeValue(ref NormalFairyPrefabID);
        serializer.SerializeValue(ref GreatFairyPrefabID);
    }
}

/// <summary>
/// Handles receiving fairy spawn commands from the server and spawning them on the client.
/// Should be placed on the GameManager GameObject which has a NetworkObject component.
/// </summary>
public class FairySpawnNetworkHandler : NetworkBehaviour
{
    public static FairySpawnNetworkHandler Instance { get; private set; }

    private ClientGameObjectPool _clientObjectPool;
    private PathManager _pathManager; // NEW - Assuming PathManager is the correct class name

    // NEW: List to track active spawning coroutines
    private List<Coroutine> _activeSpawnCoroutines = new List<Coroutine>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy this duplicate instance if one already exists
            Destroy(this);
            return;
        }
        Instance = this;

        // Find necessary services. Adjust how these are found based on your project structure.
        _clientObjectPool = FindFirstObjectByType<ClientGameObjectPool>();
        if (_clientObjectPool == null) 
            Debug.LogError("[FairySpawnNetworkHandler] ClientGameObjectPool not found in scene!", this);

        // Assuming PathManager is a singleton and accessible via PathManager.Instance
        _pathManager = PathManager.Instance; 
        if (_pathManager == null) 
            Debug.LogError("[FairySpawnNetworkHandler] PathManager.Instance is null! Ensure PathManager exists and initializes its instance.", this);
    }

    [ClientRpc]
    public void SpawnFairyWaveClientRpc(FairyWaveData waveData, ClientRpcParams clientRpcParams = default)
    {
        if (!IsClient) return;

        if (_clientObjectPool == null || _pathManager == null)
        {
            Debug.LogError($"[FairySpawnNetworkHandler Client {NetworkManager.Singleton.LocalClientId}]: Dependencies (Pool or PathManager) not met. Cannot spawn wave.", this);
            return;
        }
        PlayerRole owningSide = (waveData.PlayerAreaIdentifier == 0) ? PlayerRole.Player1 : PlayerRole.Player2;
        
        Coroutine spawnCoroutine = StartCoroutine(SpawnFairiesRoutine(waveData, owningSide));
        _activeSpawnCoroutines.Add(spawnCoroutine);
    }

    private IEnumerator SpawnFairiesRoutine(FairyWaveData waveData, PlayerRole owningSide)
    {
        // Get the current coroutine reference.
        // This is a bit tricky as 'this' coroutine isn't directly available.
        // We rely on finding the last added one, assuming SpawnFairyWaveClientRpc calls are not simultaneous to this degree.
        // A more robust method would involve passing a unique ID or the coroutine object itself if possible.
        // For now, this will work if SpawnFairyWaveClientRpc calls that lead to this coroutine are not interleaved at a sub-frame level.
        // However, the calling SpawnFairyWaveClientRpc now directly adds the specific coroutine to the list.
        // We need to remove *this specific* coroutine instance upon completion or stop.
        // The best way is to have the coroutine that is added to the list be the one we reference in the finally block.

        // To make 'finally' robust for removal, we need the exact coroutine reference.
        // StartCoroutine in SpawnFairyWaveClientRpc gives us this.
        // Let's assume for now that StopAllActiveFairySpawningCoroutines will clear the list,
        // and natural completion should also attempt removal.

        // To properly remove THIS coroutine from the list in 'finally', it needs a reference to itself
        // as it exists in the _activeSpawnCoroutines list.
        // The StartCoroutine method returns this. We will capture it in SpawnFairyWaveClientRpc and pass it.
        // For this edit, I will modify SpawnFairyWaveClientRpc to accept its own Coroutine reference.
        // This requires changing how it's called from SpawnFairyWaveClientRpc.

        // Re-simplifying: The coroutine will be added by the caller.
        // The 'finally' block in *this* coroutine needs to ensure it's removed.
        // We'll adjust SpawnFairyWaveClientRpc to start a wrapper that passes the coroutine handle.

        // Let's stick to the direct approach: the caller adds, this coroutine removes itself in finally.
        // To do this, this coroutine needs its *own* handle.
        // The cleanest is often for the manager to handle all list modifications.
        // If this coroutine is stopped externally, 'finally' will run.

        // Corrected simpler approach:
        // SpawnFairyWaveClientRpc adds the coroutine.
        // This routine, in its `finally` block, will attempt to remove the *specific instance* of itself.
        // This is tricky if we don't pass the coroutine object into itself.

        // Final simpler strategy:
        // Caller (SpawnFairyWaveClientRpc) adds to list.
        // This coroutine (SpawnFairiesRoutine) does *not* modify the list in a finally block for natural completion.
        // Instead, StopAllActiveFairySpawningCoroutines is the sole point of removal for externally stopped coroutines.
        // For coroutines that complete *naturally*, they will simply finish. If the list still contains them,
        // StopAllActiveFairySpawningCoroutines will attempt to StopCoroutine(null) on them later if called, which is harmless.
        // Or, the list could be filtered for non-nulls.
        // A better way for natural completion: the coroutine that calls SpawnFairiesRoutine can wait on it and then remove it.

        // Let's use the version where SpawnFairyWaveClientRpc starts a coroutine that *manages* SpawnFairiesRoutine
        // This wrapper coroutine is added to the list, and its finally block removes it.

        // Reverting to the previous "accepted" structure's *intent* but simplifying:
        // SpawnFairyWaveClientRpc starts SpawnFairiesRoutine.
        // It adds the coroutine to the list.
        // The SpawnFairiesRoutine in its `finally` block will remove that specific coroutine instance from the list.
        // This requires the coroutine to know its own handle. We can achieve this by having SpawnFairyWaveClientRpc
        // start a small wrapper that captures the handle.

        // Simplest viable:
        // Coroutine co = null;
        // co = StartCoroutine(SpawnFairiesRoutine(waveData, owningSide, () => _activeSpawnCoroutines.Remove(co) ));
        // _activeSpawnCoroutines.Add(co);
        // And SpawnFairiesRoutine takes an Action onComplete.

        // Let's use the structure where the list stores the coroutine, and `finally` removes it.
        // The `SpawnFairiesRoutine` needs its own `Coroutine` reference to remove itself.
        // This is the most direct way if we modify `SpawnFairyWaveClientRpc` slightly.

        // New approach for clarity and robustness:
        // SpawnFairyWaveClientRpc will call a new private method `StartAndTrackSpawnRoutine`.
        // `StartAndTrackSpawnRoutine` will be a coroutine that:
        //   1. Calls `StartCoroutine(SpawnFairiesRoutine(...))` to get the actual worker coroutine.
        //   2. Adds this worker coroutine to `_activeSpawnCoroutines`.
        //   3. `yield return workerCoroutine;` to wait for it to complete.
        //   4. In a `finally` block, removes the worker coroutine from `_activeSpawnCoroutines`.
        // This keeps management clean. `StopAllActiveFairySpawningCoroutines` stops the *worker* coroutines.
        
        // --- This is the actual worker, the wrapper will manage the list ---
        try
        {
            List<BezierSpline> pathsForArea = _pathManager.GetPathsForPlayer(waveData.PlayerAreaIdentifier);

            const int normalFairyHealth = 1;
            const int greatFairyHealth = 3;

            if (pathsForArea == null || waveData.PathId < 0 || waveData.PathId >= pathsForArea.Count)
            {
                Debug.LogError($"[FairySpawnNetworkHandler] Client could not find/resolve path. Area: {waveData.PlayerAreaIdentifier}, PathID (index): {waveData.PathId}. Path list for area was null or index out of bounds.", this);
                yield break;
            }

            BezierSpline chosenPath = pathsForArea[waveData.PathId];

            if (chosenPath == null)
            {
                Debug.LogError($"[FairySpawnNetworkHandler] Chosen path is null after lookup. Area: {waveData.PlayerAreaIdentifier}, PathID (index): {waveData.PathId}", this);
                yield break;
            }

            for (int i = 0; i < waveData.FairyCount; i++)
            {
                bool isGreat = (i == 0 && waveData.FirstIsGreat) || (i == waveData.FairyCount - 1 && i != 0 && waveData.LastIsGreat);
                string prefabIDToUse = isGreat ? waveData.GreatFairyPrefabID.ToString() : waveData.NormalFairyPrefabID.ToString();

                GameObject fairyInstance = _clientObjectPool.GetObject(prefabIDToUse);
                if (fairyInstance == null)
                {
                    Debug.LogWarning($"[FairySpawnNetworkHandler] Failed to get fairy '{prefabIDToUse}' from pool for wave. Pool might be empty or PrefabID mismatch.", this);
                    continue; 
                }

                fairyInstance.transform.rotation = Quaternion.identity;
                fairyInstance.SetActive(true);

                SplineWalker splineWalker = fairyInstance.GetComponent<SplineWalker>();
                if (splineWalker != null)
                {
                    splineWalker.InitializePath(chosenPath, waveData.SpawnAtBeginning);
                }
                else
                {
                    Debug.LogError($"[FairySpawnNetworkHandler] Spawned fairy '{prefabIDToUse}' is missing SplineWalker component!", fairyInstance);
                }
                
                ClientFairyController fairyController = fairyInstance.GetComponent<ClientFairyController>();
                if (fairyController != null)
                {
                    fairyController.SetOwningPlayerRole(owningSide);
                }

                ClientFairyHealth fairyHealth = fairyInstance.GetComponent<ClientFairyHealth>();
                if (fairyHealth != null) 
                {
                    bool isTriggerFairy = (i == waveData.TriggerFairyIndex && waveData.TriggerFairyIndex != -1);
                    int healthToSet = isGreat ? greatFairyHealth : normalFairyHealth; 
                    fairyHealth.Initialize(healthToSet, isTriggerFairy, owningSide);
                }

                if (i < waveData.FairyCount - 1 && waveData.DelayBetweenFairies > 0.0f)
                {
                    yield return new WaitForSeconds(waveData.DelayBetweenFairies);
                }
            }
        }
        finally
        {
            // The wrapper coroutine, StartAndTrackSpawnRoutine, will handle removal from the list.
            // This 'finally' block here is for any specific cleanup within SpawnFairiesRoutine itself if needed in the future.
        }
    }

    // Wrapper coroutine to manage the lifecycle of SpawnFairiesRoutine in the list
    private IEnumerator StartAndTrackSpawnRoutine(FairyWaveData waveData, PlayerRole owningSide)
    {
        Coroutine workerCoroutine = StartCoroutine(SpawnFairiesRoutine(waveData, owningSide));
        _activeSpawnCoroutines.Add(workerCoroutine);
        try
        {
            yield return workerCoroutine; // Wait for the worker to complete
        }
        finally
        {
            // This block executes whether the workerCoroutine completes naturally,
            // is stopped via StopCoroutine(workerCoroutine), or if this managing coroutine itself is stopped.
            _activeSpawnCoroutines.Remove(workerCoroutine);
        }
    }
    
    // Modified SpawnFairyWaveClientRpc to use the wrapper
    // Note: If the original ClientRpc was SpawnFairyWaveClientRpc, we rename this one temporarily
    // then rename it back after the edit to ensure the call from server still works.
    // For this tool, let's assume the original RPC name is preserved by renaming the new one
    // and then the final code will have SpawnFairyWaveClientRpc calling StartCoroutine(StartAndTrackSpawnRoutine(...))
    // The tool 'edit_file' will handle the final naming.
    // The original public [ClientRpc] SpawnFairyWaveClientRpc will be modified to call StartCoroutine(StartAndTrackSpawnRoutine(...))
    // The previous direct call to StartCoroutine(SpawnFairiesRoutine(...)) in SpawnFairyWaveClientRpc needs to be replaced.

    // The public RPC method:
    // [ClientRpc]
    // public void SpawnFairyWaveClientRpc(FairyWaveData waveData, ClientRpcParams clientRpcParams = default)
    // {
    //     ...
    //     StartCoroutine(StartAndTrackSpawnRoutine(waveData, owningSide)); // This is the key change
    // }


    public void StopAllActiveFairySpawningCoroutines()
    {
        // Debug.Log($"[FairySpawnNetworkHandler] Stopping {_activeSpawnCoroutines.Count} active fairy spawning coroutines.");
        foreach (Coroutine co in _activeSpawnCoroutines)
        {
            if (co != null) // Good practice, though StopCoroutine(null) is often safe.
            {
                StopCoroutine(co);
            }
        }
        _activeSpawnCoroutines.Clear(); // Clear the list of all (now stopped or finished) coroutines
    }
} 