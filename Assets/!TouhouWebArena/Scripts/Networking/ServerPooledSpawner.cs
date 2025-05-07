using UnityEngine;
using Unity.Netcode;
using TouhouWebArena; // For PlayerData, PlayerRole

/// <summary>
/// **[Server Only]** Static helper class responsible for spawning pooled NetworkObjects
/// using the <see cref="NetworkObjectPool"/>.
/// </summary>
public static class ServerPooledSpawner
{
    /// <summary>
    /// **[Server Only]** Core helper method to spawn a single projectile **using the NetworkObjectPool**.
    /// Gets an object from the pool via <see cref="PoolableObjectIdentity.PrefabID"/>, positions it,
    /// spawns it with ownership, assigns owner role via <see cref="PlayerDataManager"/> **after spawning**,
    /// and parents it to the pool.
    /// </summary>
    /// <param name="prefab">The pooled prefab to spawn.</param>
    /// <param name="position">World position for spawning.</param>
    /// <param name="rotation">World rotation for spawning.</param>
    /// <param name="ownerId">The ClientId of the player owning this bullet.</param>
    /// <returns>The spawned NetworkObject, or null if spawning failed.</returns>
    public static NetworkObject SpawnSinglePooledBullet(GameObject prefab, Vector3 position, Quaternion rotation, ulong ownerId)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("[ServerPooledSpawner] Attempted to spawn pooled bullet from client!");
            return null;
        }
        if (prefab == null)
        {
            Debug.LogError("[ServerPooledSpawner.SpawnSinglePooledBullet] Attempted to spawn a null prefab.");
            return null;
        }
        if (NetworkObjectPool.Instance == null)
        {
            Debug.LogError("[ServerPooledSpawner.SpawnSinglePooledBullet] NetworkObjectPool instance not found!");
            return null;
        }

        // Ensure prefab has the identity component for pooling
        PoolableObjectIdentity identity = prefab.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
             Debug.LogError($"[ServerPooledSpawner.SpawnSinglePooledBullet] Prefab '{prefab.name}' is missing PoolableObjectIdentity or PrefabID.");
             return null;
        }
        string prefabID = identity.PrefabID;

        // Get from pool
        NetworkObject bulletNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);
        if (bulletNetworkObject == null)
        {
            // Pool likely returned null (e.g., pool empty and cannot grow)
            Debug.LogError($"[ServerPooledSpawner.SpawnSinglePooledBullet] Failed to get NetworkObject from pool for PrefabID: {prefabID}. Pool might be exhausted.");
            return null;
        }

        // Position, Rotate, Activate
        bulletNetworkObject.transform.position = position;
        bulletNetworkObject.transform.rotation = rotation;
        bulletNetworkObject.gameObject.SetActive(true);

        // --- Spawn FIRST ---
        bulletNetworkObject.SpawnWithOwnership(ownerId);

        // --- Assign Owner Role AFTER Spawning ---
        BulletMovement bulletMovement = bulletNetworkObject.GetComponent<BulletMovement>();
        if (bulletMovement != null)
        {
            // OwnerRole NetworkVariable has been removed from the client-side BulletMovement.cs
            // If this spawner is used for server-side bullets that still have OwnerRole,
            // those bullets would need a different component or version of BulletMovement.
            /* // Temporarily removed to allow compilation
            if (PlayerDataManager.Instance != null)
            {
                PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(ownerId);
                // Now it's safe to set the NetworkVariable
                bulletMovement.OwnerRole.Value = ownerData.HasValue ? ownerData.Value.Role : PlayerRole.None;
            }
            else
            {
                 Debug.LogError("[ServerPooledSpawner.SpawnSinglePooledBullet] PlayerDataManager instance not found! Cannot set bullet owner role.");
                 bulletMovement.OwnerRole.Value = PlayerRole.None; // Assign default
            }
            */
        }
        else
        {
            // Optional: Log warning if expected component is missing, but don't stop spawning.
            // Debug.LogWarning($"[ServerPooledSpawner.SpawnSinglePooledBullet] Spawned object '{prefabID}' is missing BulletMovement component.");
        }

        // Parent AFTER setting position/rotation and spawning
        bulletNetworkObject.transform.SetParent(NetworkObjectPool.Instance.transform, worldPositionStays: true);

        return bulletNetworkObject;
    }
} 