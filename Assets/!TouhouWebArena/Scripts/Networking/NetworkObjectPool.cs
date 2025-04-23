using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

/// <summary>
/// A singleton <see cref="NetworkBehaviour"/> that manages object pooling for networked prefabs.
/// Improves performance by reusing NetworkObject instances instead of instantiating and destroying them frequently.
/// Prefabs are configured in the Inspector using <see cref="PoolConfig"/> and identified at runtime by a unique <see cref="PoolableObjectIdentity.PrefabID"/> string.
/// All pool operations (initialization, getting, returning) are server-authoritative.
/// </summary>
public class NetworkObjectPool : NetworkBehaviour
{
    /// <summary>
    /// Defines the configuration for a single prefab type within the object pool.
    /// Used in the Inspector list <see cref="NetworkObjectPool.poolsToCreate"/>.
    /// </summary>
    [System.Serializable]
    public struct PoolConfig
    {
        /// <summary>
        /// The prefab GameObject to be pooled. Must have <see cref="NetworkObject"/> and <see cref="PoolableObjectIdentity"/> components.
        /// </summary>
        [Tooltip("The prefab to pool. Must have NetworkObject and PoolableObjectIdentity components.")]
        public GameObject Prefab;
        /// <summary>
        /// The initial number of instances of this prefab to create when the pool initializes on the server.
        /// </summary>
        [Tooltip("Number of instances to pre-warm the pool with.")]
        public int InitialSize;
    }

    /// <summary>
    /// Singleton instance of the NetworkObjectPool.
    /// </summary>
    [Tooltip("Singleton instance.")]
    public static NetworkObjectPool Instance { get; private set; }

    [Header("Pool Configuration")]
    /// <summary>
    /// Inspector-configurable list defining the prefabs to be pooled and their initial quantities.
    /// </summary>
    [Tooltip("List of prefabs to pool. Assign Prefab and Initial Size.")]
    [SerializeField] private List<PoolConfig> poolsToCreate = new List<PoolConfig>();

    [Header("Runtime Settings")]
    /// <summary>
    /// If true, the pool will instantiate new objects if a requested prefab type runs out.
    /// If false, <see cref="GetNetworkObject"/> will return null when the specific pool is empty.
    /// </summary>
    [Tooltip("If true, the pool will instantiate new objects if a requested prefab type runs out. If false, GetNetworkObject will return null when the pool is empty.")]
    [SerializeField] private bool allowPoolExpansion = true;

    // --- Runtime Dictionaries (Private) ---
    // Stores the actual pooled objects, keyed by PrefabID.
    private Dictionary<string, Queue<NetworkObject>> prefabPools = new Dictionary<string, Queue<NetworkObject>>();
    // Maps PrefabID to the actual prefab GameObject for instantiation.
    private Dictionary<string, GameObject> prefabIdToReference = new Dictionary<string, GameObject>();

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the Singleton pattern for the pool.
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            // Debug.LogWarning($"[POOL INSTANCE] Duplicate NetworkObjectPool detected (Instance ID: {GetInstanceID()}). Destroying self.", this);
            Destroy(gameObject); 
        }
        else 
        { 
            Instance = this; 
            // Debug.Log($"[POOL INSTANCE] Awake - Instance ID: {GetInstanceID()} assigned as Singleton.", this);
        }
    }

    /// <summary>
    /// Called when the NetworkObject associated with this script is spawned.
    /// Initializes the pools on the server (<see cref="InitializePools"/>).
    /// </summary>
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
    /// Initializes all pools based on the <see cref="poolsToCreate"/> list configured in the Inspector.
    /// Performs validation checks on each prefab configuration.
    /// Populates the internal dictionaries (<see cref="prefabIdToReference"/>, <see cref="prefabPools"/>)
    /// and pre-warms each pool by instantiating the specified initial number of objects using <see cref="CreateAndPoolObject"/>.
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
    /// Server-only method. Instantiates a new instance of the specified prefab, deactivates it,
    /// and adds its NetworkObject component to the corresponding pool queue.
    /// </summary>
    /// <param name="prefabID">The <see cref="PoolableObjectIdentity.PrefabID"/> of the prefab to instantiate.</param>
    /// <param name="queue">The specific queue for this prefab ID.</param>
    /// <returns>The created NetworkObject, or null if instantiation failed or not on server.</returns>
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
    /// Server-only method. Retrieves an inactive NetworkObject from the pool for the specified prefab ID.
    /// If the pool is empty and <see cref="allowPoolExpansion"/> is true, it attempts to create a new object.
    /// The retrieved object will be inactive; the caller is responsible for setting its position/rotation,
    /// activating it (<c>gameObject.SetActive(true)</c>), and spawning it (<c>NetworkObject.Spawn(true)</c>).
    /// </summary>
    /// <param name="prefabID">The <see cref="PoolableObjectIdentity.PrefabID"/> of the desired prefab.</param>
    /// <returns>An inactive NetworkObject instance ready to be spawned, or null if unavailable/error.</returns>
    public NetworkObject GetNetworkObject(string prefabID)
    {
        if (!IsServer) { Debug.LogError("GetNetworkObject: Must be called on Server!"); return null; }
        if (string.IsNullOrEmpty(prefabID)) { Debug.LogError("GetNetworkObject: Null or empty prefabID provided.", this); return null; }

        if (!prefabPools.TryGetValue(prefabID, out Queue<NetworkObject> objectQueue))
        {
            // Important: Check if the ID is even known before logging 'not registered'
            if (prefabIdToReference.ContainsKey(prefabID))
                 Debug.LogWarning($"[POOL] Pool for ID '{prefabID}' exists but queue is missing? This shouldn't happen.", this);
            else
                Debug.LogError($"[POOL] Prefab ID '{prefabID}' has not been registered via Inspector list.", this);
            return null;
        }

        if (objectQueue.Count > 0)
        {
            NetworkObject obj = objectQueue.Dequeue();
            return obj;
        }
        else if (allowPoolExpansion)
        {
            NetworkObject newObj = CreateAndPoolObject(prefabID, objectQueue); 
            if (newObj != null && objectQueue.Count > 0) 
            { 
                NetworkObject obj = objectQueue.Dequeue();
                return obj;
            }
            else if (newObj != null) 
            { 
                return newObj;
            }
            else { Debug.LogError($"[POOL] Failed to expand pool for ID '{prefabID}'.", this); return null; }
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Server-only method. Returns a previously spawned NetworkObject to the pool.
    /// It despawns the object across the network (without destroying it) and deactivates the GameObject,
    /// placing it back into the appropriate queue based on its <see cref="PoolableObjectIdentity.PrefabID"/>.
    /// If the object doesn't have a valid identity or the pool doesn't recognize it, it will be destroyed instead.
    /// </summary>
    /// <param name="networkObject">The NetworkObject instance to return to the pool.</param>
    public void ReturnNetworkObject(NetworkObject networkObject)
    {
        if (!IsServer) { Debug.LogError("ReturnNetworkObject: Must be called on Server!"); return; }
        if (networkObject == null) { Debug.LogError("ReturnNetworkObject: Null object provided.", this); return; }

        PoolableObjectIdentity identity = networkObject.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
            if (networkObject.IsSpawned) networkObject.Despawn(true); else if (networkObject.gameObject != null) Destroy(networkObject.gameObject);
            return;
        }

        string prefabID = identity.PrefabID;

        if (prefabPools.TryGetValue(prefabID, out Queue<NetworkObject> objectQueue))
        {
            if (networkObject.IsSpawned) 
            {
                networkObject.Despawn(false); // false = Do not destroy the GameObject
            }
            networkObject.gameObject.SetActive(false); // Deactivate GameObject
            objectQueue.Enqueue(networkObject); // Add to queue
        }
        else
        {
            // If it wasn't found in the pool, despawn/destroy it properly
            if (networkObject.IsSpawned) networkObject.Despawn(true); // true = destroy
            else if (networkObject.gameObject != null) Destroy(networkObject.gameObject);
        }
    }

    /// <summary>
    /// Called when the NetworkObject is despawned (e.g., server shutdown).
    /// Cleans up pooled objects on the server by despawning and destroying them.
    /// </summary>
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

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Ensures the Singleton instance is cleared.
    /// </summary>
    public override void OnDestroy()
    {
        if (Instance == this) 
        {
             Instance = null;
            // Debug.Log($"[POOL INSTANCE] OnDestroy - Instance ID: {GetInstanceID()} cleared Singleton.", this);
        }
        base.OnDestroy();
    }
}

// Interface for pooled objects to implement reset logic remains the same (optional)
// public interface IPoolable
// {
//     void ResetState();
// }