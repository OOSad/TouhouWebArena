using UnityEngine;

/// <summary>
/// Attach this component to prefabs that will be pooled.
/// It helps the NetworkObjectPool identify which pool an instance belongs to when returned,
/// using a string ID instead of a direct GameObject reference.
/// </summary>
public class PoolableObjectIdentity : MonoBehaviour
{
    [Tooltip("Unique string identifier for this prefab type (e.g., 'ReimuBullet', 'FairyTypeA'). Must be set on the prefab asset.")]
    public string PrefabID;

    // We keep the OriginalPrefab reference for potential debugging or other uses, but the pool will primarily use PrefabID.
    [Tooltip("Reference to the original prefab asset. Should still be assigned correctly on the prefab itself for potential validation/debugging.")]
    public GameObject OriginalPrefab;

    void Awake()
    {
        // Validation: Ensure PrefabID is set.
        if (string.IsNullOrEmpty(PrefabID))
        {
            Debug.LogError($"PoolableObjectIdentity on '{gameObject.name}' is missing its PrefabID! Please assign a unique ID on the prefab asset.", this.gameObject);
        }

        // Optional validation for OriginalPrefab can remain if desired
        if (OriginalPrefab == null)
        {
             Debug.LogWarning("PoolableObjectIdentity is missing its OriginalPrefab reference. While the pool uses PrefabID, this might be useful for debugging.", this.gameObject);
        }
    }
} 