using UnityEngine;

/// <summary>
/// Attach this to prefabs that will be pooled by the ClientGameObjectPool.
/// Stores the PrefabID used by the pool to identify and manage this object type.
/// </summary>
public class PooledObjectInfo : MonoBehaviour
{
    [Tooltip("Unique ID for this prefab type. Must match an ID configured in ClientGameObjectPool.")]
    public string PrefabID;
} 