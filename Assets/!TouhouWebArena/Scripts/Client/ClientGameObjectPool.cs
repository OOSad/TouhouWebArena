using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Added for LINQ operations like SelectMany

/// <summary>
/// A simple client-side object pool for GameObjects.
/// </summary>
public class ClientGameObjectPool : MonoBehaviour
{
    public static ClientGameObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string prefabId;
        public GameObject prefab;
        public int initialSize;
        [HideInInspector]
        public Queue<GameObject> objectQueue = new Queue<GameObject>();
        [HideInInspector]
        public List<GameObject> activeObjectsInPool = new List<GameObject>(); // NEW: To track active objects
    }

    [SerializeField]
    private List<Pool> poolsToCreate = new List<Pool>();
    private Dictionary<string, Pool> poolDictionary = new Dictionary<string, Pool>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Optional: if this pool needs to persist across scenes

        InitializePools();
    }

    private void InitializePools()
    {
        foreach (Pool poolConfig in poolsToCreate)
        {
            if (poolConfig.prefab == null)
            {
                Debug.LogError($"[ClientGameObjectPool] Prefab is null for ID: {poolConfig.prefabId}. Skipping this pool.");
                continue;
            }

            poolConfig.activeObjectsInPool.Clear(); // Ensure list is empty on init
            for (int i = 0; i < poolConfig.initialSize; i++)
            {
                GameObject obj = Instantiate(poolConfig.prefab, transform); // Parent to the pool manager for organization
                obj.SetActive(false);
                // Add PooledObjectInfo if it doesn't exist, or ensure it has the correct ID
                PooledObjectInfo poi = obj.GetComponent<PooledObjectInfo>();
                if (poi == null) poi = obj.AddComponent<PooledObjectInfo>();
                poi.PrefabID = poolConfig.prefabId;
                poolConfig.objectQueue.Enqueue(obj);
            }
            if (!poolDictionary.ContainsKey(poolConfig.prefabId))
            {
                poolDictionary.Add(poolConfig.prefabId, poolConfig);
            }
            else
            {
                Debug.LogWarning($"[ClientGameObjectPool] Duplicate PrefabID found: {poolConfig.prefabId}. Check configuration.");
            }
        }
    }

    public GameObject GetObject(string prefabId)
    {
        if (!poolDictionary.TryGetValue(prefabId, out Pool pool))
        {
            Debug.LogWarning($"[ClientGameObjectPool] Pool with ID '{prefabId}' doesn't exist.");
            return null;
        }

        if (pool.objectQueue.Count > 0)
        {
            GameObject obj = pool.objectQueue.Dequeue();
            pool.activeObjectsInPool.Add(obj); // NEW: Add to active list
            // obj.SetActive(true); // Activation will be handled by the requesting script
            return obj;
        }
        else
        {
            // No expansion for now, as per previous design. Could be added here.
            Debug.LogWarning($"[ClientGameObjectPool] Pool with ID '{prefabId}' is empty and expansion is not implemented.");
            return null;
        }
    }

    public void ReturnObject(GameObject objInstance)
    {
        if (objInstance == null) return;

        PooledObjectInfo poi = objInstance.GetComponent<PooledObjectInfo>();
        if (poi == null || string.IsNullOrEmpty(poi.PrefabID))
        {
            Debug.LogError("[ClientGameObjectPool] Returned object is missing PooledObjectInfo or PrefabID. Destroying instead.", objInstance);
            Destroy(objInstance);
            return;
        }

        if (poolDictionary.TryGetValue(poi.PrefabID, out Pool pool))
        {
            objInstance.SetActive(false);
            objInstance.transform.SetParent(transform); // Re-parent to pool manager
            pool.objectQueue.Enqueue(objInstance);
            pool.activeObjectsInPool.Remove(objInstance); // NEW: Remove from active list
        }
        else
        {
            Debug.LogError($"[ClientGameObjectPool] Attempted to return object with unknown PrefabID: {poi.PrefabID}. Destroying instead.", objInstance);
            Destroy(objInstance);
        }
    }

    /// <summary>
    /// Gets a list of all GameObjects currently considered active (i.e., taken from the pool and not yet returned).
    /// </summary>
    /// <returns>A new list containing all active GameObjects across all pools.</returns>
    public List<GameObject> GetAllActiveObjects()
    {
        // Consolidate all active objects from all pools into one list
        // Using LINQ's SelectMany for conciseness
        return poolDictionary.Values.SelectMany(pool => pool.activeObjectsInPool).ToList();
    }

    // Optional: A more performant way if you only care about a specific type or tag
    // public List<GameObject> GetActiveObjectsWithMover<T>() where T : MonoBehaviour
    // {
    //     List<GameObject> result = new List<GameObject>();
    //     foreach (Pool pool in poolDictionary.Values)
    //     {
    //         foreach (GameObject activeObj in pool.activeObjectsInPool)
    //         {
    //             if (activeObj.GetComponent<T>() != null) result.Add(activeObj);
    //         }
    //     }
    //     return result;
    // }
} 