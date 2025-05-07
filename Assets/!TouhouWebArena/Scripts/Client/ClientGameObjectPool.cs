using UnityEngine;
using System.Collections.Generic;

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
        }
        else
        {
            Debug.LogError($"[ClientGameObjectPool] Attempted to return object with unknown PrefabID: {poi.PrefabID}. Destroying instead.", objInstance);
            Destroy(objInstance);
        }
    }
} 