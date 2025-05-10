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
        if (!IsClient) return; // Should only execute on clients

        if (_clientObjectPool == null || _pathManager == null)
        {
            Debug.LogError($"[FairySpawnNetworkHandler Client {NetworkManager.Singleton.LocalClientId}]: Dependencies (Pool or PathManager) not met. Cannot spawn wave.", this);
            return;
        }

        // Determine owning side based on PlayerAreaIdentifier
        PlayerRole owningSide = (waveData.PlayerAreaIdentifier == 0) ? PlayerRole.Player1 : PlayerRole.Player2;
        // Assuming PlayerAreaIdentifier 0 is P1, 1 is P2. Adjust if this mapping is different.
        // If PlayerAreaIdentifier could be other values, add more robust mapping or error handling.

        // Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} received SpawnFairyWaveClientRpc for player area {waveData.PlayerAreaIdentifier}, path {waveData.PathId}, count {waveData.FairyCount}");
        StartCoroutine(SpawnFairiesRoutine(waveData, owningSide));
    }

    private IEnumerator SpawnFairiesRoutine(FairyWaveData waveData, PlayerRole owningSide)
    {
        // Get the list of paths for the specified player area from PathManager
        List<BezierSpline> pathsForArea = _pathManager.GetPathsForPlayer(waveData.PlayerAreaIdentifier); 

        // Define health values locally for clarity
        const int normalFairyHealth = 1;
        const int greatFairyHealth = 3;

        if (pathsForArea == null || waveData.PathId < 0 || waveData.PathId >= pathsForArea.Count)
        {
            Debug.LogError($"[FairySpawnNetworkHandler] Client could not find/resolve path. Area: {waveData.PlayerAreaIdentifier}, PathID (index): {waveData.PathId}. Path list for area was null or index out of bounds.", this);
            yield break;
        }

        BezierSpline chosenPath = pathsForArea[waveData.PathId];
        
        if (chosenPath == null) // Should ideally be caught by the above check if list indexing works as expected
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

            fairyInstance.transform.rotation = Quaternion.identity; // Reset rotation
            fairyInstance.SetActive(true);

            SplineWalker splineWalker = fairyInstance.GetComponent<SplineWalker>();
            if (splineWalker != null)
            {
                splineWalker.InitializePath(chosenPath, waveData.SpawnAtBeginning);
                // Optionally, if speed can vary per wave:
                // if (waveData.CustomSpeed > 0) splineWalker.moveSpeed = waveData.CustomSpeed;
            }
            else
            {
                Debug.LogError($"[FairySpawnNetworkHandler] Spawned fairy '{prefabIDToUse}' is missing SplineWalker component!", fairyInstance);
            }
            
            // Optional: Initialize ClientFairyController or ClientFairyHealth if they need specific data from the wave
            ClientFairyController fairyController = fairyInstance.GetComponent<ClientFairyController>();
            if (fairyController != null)
            {
                fairyController.SetOwningPlayerRole(owningSide);
                // bool isTrigger = (i == waveData.TriggerFairyIndex); // Old way of just passing to controller
                // Example: if ClientFairyController has an Init method:
                // fairyController.InitializeWaveData(isTrigger /*, other relevant data from waveData */);
            }

            ClientFairyHealth fairyHealth = fairyInstance.GetComponent<ClientFairyHealth>();
            if (fairyHealth != null) 
            {
                bool isTriggerFairy = (i == waveData.TriggerFairyIndex && waveData.TriggerFairyIndex != -1); // Ensure index is valid
                int healthToSet = isGreat ? greatFairyHealth : normalFairyHealth; 
                fairyHealth.Initialize(healthToSet, isTriggerFairy);
            }

            if (i < waveData.FairyCount - 1 && waveData.DelayBetweenFairies > 0.0f)
            {
                yield return new WaitForSeconds(waveData.DelayBetweenFairies);
            }
        }
    }
} 