using UnityEngine;
using Unity.Netcode;

// Handles visual/audio effects upon Fairy death 
// [RequireComponent(typeof(FairyController))] // Requires the main Fairy script -- Temporarily Commented Out
/// <summary>
/// [Server Only] Handles spawning visual/audio effects, specifically a shockwave,
/// when the associated <see cref="FairyController"/> dies.
/// Relies on the <see cref="NetworkObjectPool"/> to instantiate and manage the shockwave effect prefab.
/// </summary>
public class FairyDeathEffects : NetworkBehaviour // Needs NetworkBehaviour for IsServer check 
{
    [Header("Effects")]
    [SerializeField] private GameObject shockwavePrefab; // Assign the FairyShockwave prefab here

    // Reference to the PoolableObjectIdentity component on the prefab
    private PoolableObjectIdentity shockwaveIdentity; 

    void Awake()
    {
        // Debug.Log($"[FairyDeathEffects] Awake called on {(IsServer ? "Server" : "Client")}", this);
        // Get the identity component from the prefab
        if (shockwavePrefab != null)
        {
            // Debug.Log($"[FairyDeathEffects] Awake: shockwavePrefab assigned ('{shockwavePrefab.name}')", this);
            shockwaveIdentity = shockwavePrefab.GetComponent<PoolableObjectIdentity>();
            if (shockwaveIdentity == null)
            {
                Debug.LogError($"[FairyDeathEffects] Awake ERROR: Shockwave prefab '{shockwavePrefab.name}' is missing the required PoolableObjectIdentity component!", this);
            }
            else if (string.IsNullOrEmpty(shockwaveIdentity.PrefabID))
            {
                 Debug.LogError($"[FairyDeathEffects] Awake ERROR: Shockwave prefab '{shockwavePrefab.name}' has a missing or empty PrefabID in its PoolableObjectIdentity component!", this);
            }
            // else
            // {
            //      Debug.Log($"[FairyDeathEffects] Awake: Found valid PoolableObjectIdentity with PrefabID: '{shockwaveIdentity.PrefabID}'", this);
            // }
        }
        else
        {
             Debug.LogError("[FairyDeathEffects] Awake ERROR: Shockwave prefab is not assigned!", this);
        }
    }


    // --- Public method to trigger effects immediately --- 
    /// <summary>
    /// [Server Only] Spawns the configured shockwave effect prefab at the specified position.
    /// Retrieves the shockwave object from the <see cref="NetworkObjectPool"/> using the prefab's
    /// <see cref="PoolableObjectIdentity"/>, positions it, activates it, and spawns it.
    /// </summary>
    /// <param name="position">The world position where the death occurred and the effect should spawn.</param>
    public void TriggerEffects(Vector3 position)
    {
         // Debug.Log($"[FairyDeathEffects] TriggerEffects called on {(IsServer ? "Server" : "Client")}", this);

        // Ensure prefab and identity are valid, and we are the server
        if (!IsServer || shockwavePrefab == null || shockwaveIdentity == null || string.IsNullOrEmpty(shockwaveIdentity.PrefabID)) 
        {
            // --- DEBUG LOG --- 
            if (IsServer) 
            {
                 string reason = "Unknown";
                 if (shockwavePrefab == null) reason = "shockwavePrefab is NULL";
                 else if (shockwaveIdentity == null) reason = "shockwaveIdentity is NULL (check Awake logs)";
                 else if (string.IsNullOrEmpty(shockwaveIdentity.PrefabID)) reason = "shockwaveIdentity.PrefabID is NULL or Empty";
                 Debug.LogError($"[FairyDeathEffects] TriggerEffects returning early on Server. Reason: {reason}", this);
            }
            // else
            // {
            //      Debug.LogWarning("[FairyDeathEffects] TriggerEffects called on Client, returning.", this);
            // }
            // -----------------
            return; 
        }

        // Get the PrefabID from the identity component
        string prefabID = shockwaveIdentity.PrefabID;

        // --- DEBUG LOG ---
        // Debug.Log($"[FairyDeathEffects {GetInstanceID()}] Triggering shockwave (PrefabID: {prefabID}) at position {position}", this);
        // -----------------

        // Get the shockwave instance from the pool using the PrefabID
        NetworkObject pooledNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);

        if (pooledNetworkObject != null)
        {
            Shockwave shockwaveComponent = pooledNetworkObject.GetComponent<Shockwave>();
            if (shockwaveComponent == null)
            {
                Debug.LogError($"[FairyDeathEffects] Pooled object for PrefabID '{prefabID}' is missing the Shockwave component! Returning to pool.", pooledNetworkObject);
                NetworkObjectPool.Instance.ReturnNetworkObject(pooledNetworkObject); 
                return;
            }

            // Position and activate the object (server controls state)
            pooledNetworkObject.transform.position = position;
            pooledNetworkObject.transform.rotation = Quaternion.identity; 
            pooledNetworkObject.gameObject.SetActive(true); // Ensure it's active before spawning

            // --- ALWAYS SPAWN --- 
            // Since ReturnNetworkObject now Despawns, IsSpawned will be false.
            // OnNetworkSpawn in Shockwave.cs will handle calling ResetAndStartExpansion.
            pooledNetworkObject.Spawn(false); // false = Pool handles destruction/lifetime
            // ------------------
        }
        else
        {
             // Log error if GetNetworkObject returned null (pool might be empty & non-expandable, or ID wrong)
             Debug.LogError($"[FairyDeathEffects] Failed to get object with PrefabID '{prefabID}' from NetworkObjectPool.", this);
        }
    }

    /// <summary>
    /// Gets the GameObject prefab configured for the shockwave effect.
    /// </summary>
    /// <returns>The shockwave effect GameObject prefab.</returns>
    public GameObject GetShockwavePrefab() 
    {
        return shockwavePrefab;
    }
} 