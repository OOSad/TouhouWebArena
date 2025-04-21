using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

/// <summary>
/// A NetworkObject pooling system using string IDs to manage multiple prefab types,
/// configured via a list in the Inspector.
/// Manages the lifecycle of networked objects to improve performance.
/// Designed to be used as a Singleton.
/// Pooled prefabs must have PoolableObjectIdentity component with a unique PrefabID set.
/// </summary>
public class NetworkObjectPool : NetworkBehaviour
{
    // Serializable struct to hold config data in the Inspector
    [System.Serializable]
    public struct PoolConfig
    {
        public GameObject Prefab;
        public int InitialSize;
    }

    public static NetworkObjectPool Instance { get; private set; }

    [Header("Pool Configuration")]
    [Tooltip("List of prefabs to pool. Assign Prefab and Initial Size.")]
    [SerializeField] private List<PoolConfig> poolsToCreate = new List<PoolConfig>();

    [Header("Runtime Settings")]
    [SerializeField] private bool allowPoolExpansion = true;

    // --- Runtime Dictionaries (Private) ---
    private Dictionary<string, Queue<NetworkObject>> prefabPools = new Dictionary<string, Queue<NetworkObject>>();
    private Dictionary<string, GameObject> prefabIdToReference = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); } else { Instance = this; }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Initialize pools on the server
        if (IsServer)
        {
            InitializePools();
        }
    }

    /// <summary>
    /// Initializes all pools based on the poolsToCreate list configured in the Inspector.
    /// Should only run on the server.
    /// </summary>
    private void InitializePools()
    {
        if (!IsServer) return;

        foreach (PoolConfig config in poolsToCreate)
        {
            if (config.Prefab == null)
            {
                 Debug.LogWarning("Null Prefab found in NetworkObjectPool configuration. Skipping.", this);
                 continue;
            }

            // --- Validation --- 
             PoolableObjectIdentity identity = config.Prefab.GetComponent<PoolableObjectIdentity>();
            if (identity == null) { Debug.LogError($"Prefab '{config.Prefab.name}' missing PoolableObjectIdentity. Cannot pool.", config.Prefab); continue; }
            if (string.IsNullOrEmpty(identity.PrefabID)) { Debug.LogError($"Prefab '{config.Prefab.name}' has missing/empty PrefabID in PoolableObjectIdentity. Cannot pool.", config.Prefab); continue; }
            if (config.Prefab.GetComponent<NetworkObject>() == null) { Debug.LogError($"Prefab '{config.Prefab.name}' missing NetworkObject. Cannot pool.", config.Prefab); continue; }

            string prefabID = identity.PrefabID;

            // --- Registration --- 
            if (prefabPools.ContainsKey(prefabID))
            {
                Debug.LogWarning($"Prefab ID '{prefabID}' (from {config.Prefab.name}) is already registered or duplicated in config list. Skipping duplicate.", this);
                continue;
            }
            prefabIdToReference.Add(prefabID, config.Prefab);
            Queue<NetworkObject> objectQueue = new Queue<NetworkObject>();
            prefabPools.Add(prefabID, objectQueue);

            // --- Pre-warming --- 
            int initialSize = config.InitialSize > 0 ? config.InitialSize : 1; // Ensure at least 1 if size is 0 or less
            for (int i = 0; i < initialSize; i++)
            {
                CreateAndPoolObject(prefabID, objectQueue);
            }
        }
    }

    /// <summary>
    /// Instantiates a new object for a specific prefab ID.
    /// </summary>
    private NetworkObject CreateAndPoolObject(string prefabID, Queue<NetworkObject> queue)
    {
        if (!IsServer) return null;
        if (!prefabIdToReference.TryGetValue(prefabID, out GameObject prefabToInstantiate))
        {
            Debug.LogError($"CreateAndPoolObject: Cannot find prefab reference for ID '{prefabID}'. Cannot instantiate.", this);
            return null;
        }

        GameObject newObj = Instantiate(prefabToInstantiate);
        NetworkObject netObj = newObj.GetComponent<NetworkObject>();

        // Cannot set parent here - NetworkObjects must be spawned first.
        // newObj.transform.SetParent(this.transform, worldPositionStays: false);

        newObj.SetActive(false);
        queue.Enqueue(netObj);
        return netObj;
    }

    /// <summary>
    /// Gets an object from the pool for the specified Prefab ID.
    /// </summary>
    public NetworkObject GetNetworkObject(string prefabID)
    {
        if (!IsServer) { Debug.LogError("GetNetworkObject: Server only."); return null; }
        if (string.IsNullOrEmpty(prefabID)) { Debug.LogError("GetNetworkObject: Null or empty prefabID provided."); return null; }

        if (!prefabPools.TryGetValue(prefabID, out Queue<NetworkObject> objectQueue))
        {
            // Important: Check if the ID is even known before logging 'not registered'
            if (prefabIdToReference.ContainsKey(prefabID))
                 Debug.LogWarning($"GetNetworkObject: Pool for ID '{prefabID}' exists but queue is missing? This shouldn't happen.", this);
            else
                Debug.LogError($"GetNetworkObject: Prefab ID '{prefabID}' has not been registered via Inspector list.", this);
            return null;
        }

        if (objectQueue.Count > 0)
        {
            return objectQueue.Dequeue();
        }
        else if (allowPoolExpansion)
        {
            Debug.LogWarning($"Pool for ID '{prefabID}' was empty. Expanding.", this);
            NetworkObject newObj = CreateAndPoolObject(prefabID, objectQueue);
            if (newObj != null && objectQueue.Count > 0) return objectQueue.Dequeue();
            else if (newObj != null) return newObj; // Fallback
            else { Debug.LogError($"Failed to expand pool for ID '{prefabID}'.", this); return null; }
        }
        else
        {
            Debug.LogWarning($"Pool for ID '{prefabID}' is empty and expansion is disabled!", this);
            return null;
        }
    }

    /// <summary>
    /// Returns a NetworkObject to its appropriate pool using its PrefabID.
    /// </summary>
    public void ReturnNetworkObject(NetworkObject networkObject)
    {
        if (!IsServer) { Debug.LogError("ReturnNetworkObject: Server only."); return; }
        if (networkObject == null) { Debug.LogError("ReturnNetworkObject: Null object.", this); return; }

        PoolableObjectIdentity identity = networkObject.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
            Debug.LogError($"ReturnNetworkObject: Returned object '{networkObject.name}' is missing PoolableObjectIdentity or PrefabID. Destroying.", networkObject.gameObject);
            if (networkObject.IsSpawned) networkObject.Despawn(true); else if (networkObject.gameObject != null) Destroy(networkObject.gameObject);
            return;
        }

        string prefabID = identity.PrefabID;

        if (prefabPools.TryGetValue(prefabID, out Queue<NetworkObject> objectQueue))
        {
            networkObject.gameObject.SetActive(false);
            objectQueue.Enqueue(networkObject);
        }
        else
        {
            string registeredKeys = string.Join(", ", prefabPools.Keys);
            Debug.LogError($"ReturnNetworkObject FAIL: Returned object '{networkObject.name}' has PrefabID '{prefabID}', which was NOT found in the pool dictionary. Registered keys: [{registeredKeys}]. Destroying instead.", networkObject.gameObject);
            if (networkObject.IsSpawned) networkObject.Despawn(true); else if (networkObject.gameObject != null) Destroy(networkObject.gameObject);
        }
    }

    // Cleanup logic remains the same
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
        {
            foreach (var kvp in prefabPools)
            {
                Queue<NetworkObject> queue = kvp.Value;
                while (queue.Count > 0)
                {
                    NetworkObject obj = queue.Dequeue();
                    if (obj != null && obj.gameObject != null)
                    {
                        if(obj.IsSpawned) obj.Despawn(true);
                        else Destroy(obj.gameObject);
                    }
                }
            }
            prefabPools.Clear();
            prefabIdToReference.Clear();
        }
        if (Instance == this) { Instance = null; }
    }

    // Override OnDestroy to ensure base class cleanup happens
    public override void OnDestroy()
    {
        // Custom cleanup (clearing dictionaries, etc.)
        if (Instance == this) { Instance = null; }
        // Server-side cleanup of actual GameObjects is handled in OnNetworkDespawn
        // but we clear the dictionaries here as a fallback
        if (IsServer) // Check if this instance *was* the server
        {
             // Destroy any remaining objects if OnNetworkDespawn didn't run
             foreach (var kvp in prefabPools)
            {
                Queue<NetworkObject> queue = kvp.Value;
                while (queue.Count > 0)
                {
                    NetworkObject obj = queue.Dequeue();
                    if (obj != null && obj.gameObject != null)
                    {
                         // Check IsSpawned before Despawn
                         if (obj.IsSpawned) obj.Despawn(true);
                         // Destroy regardless if not null
                         else Destroy(obj.gameObject); 
                    }
                }
            }
        }
        prefabPools.Clear();
        prefabIdToReference.Clear();

        // IMPORTANT: Call the base class method
        base.OnDestroy();
    }
}

// Interface for pooled objects to implement reset logic remains the same (optional)
// public interface IPoolable
// {
//     void ResetState();
// }