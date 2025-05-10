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
            Debug.Log($"[ClientGameObjectPool] Destroying duplicate instance on {gameObject.name}");
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
            Debug.Log($"[ClientGameObjectPool] Initializing pool for ID: '{poolConfig.prefabId}' with initial size: {poolConfig.initialSize}");
            poolConfig.activeObjectsInPool.Clear(); // Ensure list is empty on init
            for (int i = 0; i < poolConfig.initialSize; i++)
            {
                GameObject obj = Instantiate(poolConfig.prefab, transform); // Parent to the pool manager for organization
                obj.SetActive(false);
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
            Debug.Log($"[ClientGameObjectPool] Pool for '{poolConfig.prefabId}' initialized. Queue count: {poolConfig.objectQueue.Count}");
        }
    }

    public GameObject GetObject(string prefabId)
    {
        if (!poolDictionary.TryGetValue(prefabId, out Pool pool))
        {
            Debug.LogWarning($"[ClientGameObjectPool] GetObject: Pool with ID '{prefabId}' doesn't exist.");
            return null;
        }

        if (prefabId == "Spirit")
        {
            Debug.Log($"[ClientGameObjectPool] GetObject attempting for ID 'Spirit'. Current queue count: {pool.objectQueue.Count}");
        }

        if (pool.objectQueue.Count > 0)
        {
            GameObject obj = pool.objectQueue.Dequeue();
            pool.activeObjectsInPool.Add(obj); // NEW: Add to active list
            if (prefabId == "Spirit")
            {
                Debug.Log($"[ClientGameObjectPool] GetObject DEQUEUED for ID 'Spirit'. New queue count: {pool.objectQueue.Count}. Object: {obj.name}", obj);
            }
            return obj;
        }
        else
        {
            Debug.LogWarning($"[ClientGameObjectPool] GetObject: Pool with ID '{prefabId}' is empty and expansion is not implemented.");
            return null;
        }
    }

    public void ReturnObject(GameObject objInstance)
    {
        if (objInstance == null)
        {
            Debug.LogWarning("[ClientGameObjectPool] ReturnObject: objInstance is null.");
            return;
        }

        PooledObjectInfo poi = objInstance.GetComponent<PooledObjectInfo>();
        if (poi == null || string.IsNullOrEmpty(poi.PrefabID))
        {
            Debug.LogError("[ClientGameObjectPool] ReturnObject: Returned object is missing PooledObjectInfo or PrefabID. Destroying instead.", objInstance);
            Destroy(objInstance);
            return;
        }

        string prefabIdForReturn = poi.PrefabID;
        if (prefabIdForReturn == "Spirit")
        {
            Debug.Log($"[ClientGameObjectPool] ReturnObject attempting for ID 'Spirit'. Object: {objInstance.name}. Current active state: {objInstance.activeSelf}", objInstance);
        }

        if (poolDictionary.TryGetValue(prefabIdForReturn, out Pool pool))
        {
            if (prefabIdForReturn == "Spirit")
            {
                 Debug.Log($"[ClientGameObjectPool] ReturnObject for 'Spirit': Found pool. Current queue count BEFORE SetActive/Enqueue: {pool.objectQueue.Count}");
            }

            objInstance.SetActive(false);
            objInstance.transform.SetParent(transform, true); // Re-parent to pool manager, ensure world position stays for a moment if it matters
            
            // Check if already in queue to prevent duplicates, though this hides the symptom not the cause of double return
            if (pool.objectQueue.Contains(objInstance))
            {
                Debug.LogWarning($"[ClientGameObjectPool] ReturnObject for '{prefabIdForReturn}': Object {objInstance.name} is already in the queue. This might indicate a double return.", objInstance);
            }
            else
            {
            pool.objectQueue.Enqueue(objInstance);
            }
            
            pool.activeObjectsInPool.Remove(objInstance);
            if (prefabIdForReturn == "Spirit")
            {
                Debug.Log($"[ClientGameObjectPool] ReturnObject for 'Spirit': ENQUEUED {objInstance.name}. New queue count: {pool.objectQueue.Count}. Active list count: {pool.activeObjectsInPool.Count}");
            }
        }
        else
        {
            Debug.LogError($"[ClientGameObjectPool] ReturnObject: Attempted to return object with unknown PrefabID: '{prefabIdForReturn}'. Destroying {objInstance.name} instead.", objInstance);
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