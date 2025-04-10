using UnityEngine;
using DanmakU;

// This script manages a single DanmakuSet using DanmakU's intended lifecycle.
[AddComponentMenu("DanmakU/Danmaku Set Controller")]
[DisallowMultipleComponent]
public class DanmakuSetController : DanmakuBehaviour
{
    [Tooltip("The DanmakuPrefab to create a set for.")]
    [SerializeField]
    private DanmakuPrefab bulletPrefab;

    // Public property to access the managed set
    public DanmakuSet ManagedSet { get; private set; }

    void Start()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet Prefab is not assigned to DanmakuSetController!", this);
            enabled = false;
            return;
        }

        // Use the base class method to create and manage the set
        ManagedSet = CreateSet(bulletPrefab);

        if (ManagedSet == null)
        {
            Debug.LogError("Failed to create DanmakuSet. Is DanmakuManager present?", this);
            enabled = false;
        }
    }

    // DanmakuBehaviour's OnDestroy will automatically handle cleanup of the ManagedSet
}
