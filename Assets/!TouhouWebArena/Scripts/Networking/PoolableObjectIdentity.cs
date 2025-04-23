using UnityEngine;

/// <summary>
/// A component required on all networked prefabs managed by the <see cref="NetworkObjectPool"/>.
/// Provides a unique string identifier (<see cref="PrefabID"/>) used by the pool to categorize
/// and retrieve instances, avoiding direct prefab references at runtime for returning objects.
/// </summary>
public class PoolableObjectIdentity : MonoBehaviour
{
    /// <summary>
    /// A unique string identifier for this specific prefab type (e.g., "ReimuBullet", "FairyTypeA"). 
    /// This ID **must** be manually assigned in the Inspector on the prefab asset itself and must match
    /// the ID used when requesting objects from the <see cref="NetworkObjectPool"/>.
    /// </summary>
    [Tooltip("Unique string identifier for this prefab type (e.g., 'ReimuBullet', 'FairyTypeA'). Must be set on the prefab asset.")]
    public string PrefabID;

    /// <summary>
    /// A reference to the original prefab asset this instance was created from.
    /// Primarily used for potential validation or debugging purposes; the <see cref="NetworkObjectPool"/>
    /// mainly relies on the <see cref="PrefabID"/> for runtime operations.
    /// </summary>
    [Tooltip("Reference to the original prefab asset. Should still be assigned correctly on the prefab itself for potential validation/debugging.")]
    public GameObject OriginalPrefab;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Validates that the required <see cref="PrefabID"/> has been assigned in the Inspector.
    /// Also provides a warning if the <see cref="OriginalPrefab"/> reference is missing.
    /// </summary>
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