using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

/// <summary>
/// A NetworkBehaviour Singleton responsible for relaying specific effect triggers 
/// from the server to the correct client(s), like spawning retaliation bullets.
/// Requires a NetworkObject component.
/// </summary>
public class EffectNetworkHandler : NetworkBehaviour
{
    public static EffectNetworkHandler Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optional: DontDestroyOnLoad(gameObject);
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    // Note: ClientRpcParams are passed by the caller (the server)
    [ClientRpc]
    public void SpawnStageBulletClientRpc(ulong explicitTargetClientId, FixedString64Bytes bulletPrefabID, Vector2 normalizedSpawnPosition, float actualSpeed, Vector2 direction, float bulletLifetime, bool isFromShockwaveClear, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[EffectNetworkHandler Client {NetworkManager.Singleton.LocalClientId}] Received SpawnStageBulletClientRpc. Target: P{explicitTargetClientId}, Prefab: {bulletPrefabID}, NormPos: {normalizedSpawnPosition}, Speed: {actualSpeed}, Dir: {direction}, Lifetime: {bulletLifetime}, IsFromShockwaveClear: {isFromShockwaveClear}");

        PlayerData? targetedPlayerData = PlayerDataManager.Instance?.GetPlayerData(explicitTargetClientId);
        if (targetedPlayerData == null || !targetedPlayerData.HasValue)
        {
            Debug.LogError($"[EffectNetworkHandler Client {NetworkManager.Singleton.LocalClientId}] Could not find PlayerData for explicitTargetClientId {explicitTargetClientId}. Cannot spawn bullet {bulletPrefabID}.");
            return;
        }
        PlayerRole targetedPlayerRole = targetedPlayerData.Value.Role;

        Transform spawnZoneCenterTransform = SpawnAreaManager.Instance?.GetSpawnCenterForTargetedPlayer(targetedPlayerRole);
        if (spawnZoneCenterTransform == null)
        {
            Debug.LogError($"[EffectNetworkHandler Client {NetworkManager.Singleton.LocalClientId}] Could not find spawn center for role {targetedPlayerRole} (Player {explicitTargetClientId}). Cannot spawn bullet {bulletPrefabID}.");
            return;
        }
        Vector2 spawnZoneDimensions = SpawnAreaManager.Instance.GetSpawnZoneDimensions(); // Assuming this is universal for now

        if (ClientGameObjectPool.Instance == null)
        {
            Debug.LogError($"[EffectNetworkHandler Client {NetworkManager.Singleton.LocalClientId}] ClientGameObjectPool.Instance is null. Cannot spawn bullet {bulletPrefabID}.");
            return;
        }

        GameObject bulletInstance = ClientGameObjectPool.Instance.GetObject(bulletPrefabID.ToString());
        if (bulletInstance == null) 
        {
            Debug.LogWarning($"[EffectNetworkHandler Client {NetworkManager.Singleton.LocalClientId}] Failed to get bullet '{bulletPrefabID}' from pool.");
            return;
        }

        Vector3 spawnCenterPos = spawnZoneCenterTransform.position;
        float offsetX = (normalizedSpawnPosition.x - 0.5f) * spawnZoneDimensions.x;
        float offsetY = (normalizedSpawnPosition.y - 0.5f) * spawnZoneDimensions.y;
        Vector3 worldSpawnPos = new Vector3(spawnCenterPos.x + offsetX, spawnCenterPos.y + offsetY, spawnCenterPos.z);

        bulletInstance.transform.position = worldSpawnPos;
        bulletInstance.transform.rotation = Quaternion.identity; // Or some default rotation for stage bullets
        
        StageSmallBulletMoverScript mover = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
        if (mover != null)
        {
            mover.Initialize(direction, actualSpeed, bulletLifetime, targetedPlayerRole);
        }
        else
        {
            Debug.LogError($"[EffectNetworkHandler Client {NetworkManager.Singleton.LocalClientId}] Spawned bullet {bulletPrefabID} is missing StageSmallBulletMoverScript!", bulletInstance);
        }

        bulletInstance.SetActive(true);
        // Debug.Log($"[EffectNetworkHandler Client {NetworkManager.Singleton.LocalClientId}] Successfully spawned stage bullet {bulletPrefabID} at world pos {worldSpawnPos} in area for role {targetedPlayerRole} (Player {explicitTargetClientId}). ShockwaveClear: {isFromShockwaveClear}");
    }
} 