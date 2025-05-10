using UnityEngine;
using Unity.Netcode;

// Potentially: namespace TouhouWebArena.Client {
public class MarisaChargeAttackHandler_Client : NetworkBehaviour
{
    // --- Marisa Laser ---
    [Header("Marisa Charge Attack (Client)")]
    [SerializeField] private string marisaLaserPrefabId = "MarisaChargeLaser_Client"; // Client-side prefab ID
    // [SerializeField] private Vector2 marisaLaserOffset = new Vector2(0f, 0.5f); // Offset from player center - REPLACED by spawn point
    [SerializeField] private Transform marisaLaserSpawnPoint; // Assign this in the Inspector
    // Laser duration and other properties will be on the laser script itself (IllusionLaser_Client).

    // Note: No OnNetworkSpawn/Despawn needed if it's not a singleton

    // --- RPC for Marisa's Laser ---
    [ClientRpc]
    public void SpawnChargeAttackClientRpc(Vector3 playerPositionAtRpcCall, PlayerRole ownerRole, ulong ownerNetworkObjectId, ClientRpcParams clientRpcParams = default)
    {
        // Debug.Log($"[{GetType().Name}] Received RPC to spawn Marisa's Charge Attack for owner: {ownerRole}, NetObjId: {ownerNetworkObjectId}");
        if (ClientGameObjectPool.Instance == null)
        {
            Debug.LogError($"[{GetType().Name}] ClientGameObjectPool.Instance is null. Cannot spawn Marisa laser.");
            return;
        }

        if (marisaLaserSpawnPoint == null)
        {
            Debug.LogError($"[{GetType().Name}] MarisaLaserSpawnPoint is not assigned in the Inspector for {gameObject.name}. Cannot spawn laser.");
            return;
        }

        GameObject laserGO = ClientGameObjectPool.Instance.GetObject(marisaLaserPrefabId);
        if (laserGO != null)
        {
            laserGO.transform.position = marisaLaserSpawnPoint.position; // Use the spawn point's world position
            laserGO.transform.rotation = marisaLaserSpawnPoint.rotation; // Optional: also use spawn point's rotation if laser should be oriented by it
            // If the laser should always fire straight up or based on other logic, Quaternion.identity might still be correct for rotation.

            laserGO.SetActive(true);

            IllusionLaser_Client laserScript = laserGO.GetComponent<IllusionLaser_Client>();
            if (laserScript != null)
            {
                Transform transformToFollow = null;
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null && 
                    NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ownerNetworkObjectId, out NetworkObject ownerNetworkObject))
                {
                    if (ownerNetworkObject != null)
                    {
                        transformToFollow = ownerNetworkObject.transform;
                    }
                    else
                    {
                        Debug.LogWarning($"[{GetType().Name}] Found null NetworkObject for ID {ownerNetworkObjectId} when spawning Marisa laser.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] Could not find NetworkObject for ID {ownerNetworkObjectId} to follow for Marisa laser.");
                }

                // The playerPositionAtRpcCall is the player's general position when the RPC was called.
                // For initializing the laser logic (e.g., if it needs to know where the player was, separate from where it spawns or follows),
                // we still pass it. The visual spawn is now handled by marisaLaserSpawnPoint.position.
                laserScript.Initialize(ownerRole, playerPositionAtRpcCall, transformToFollow); 
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] Spawned laser '{marisaLaserPrefabId}' is missing IllusionLaser_Client script.", laserGO);
            }
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] Failed to get laser '{marisaLaserPrefabId}' from pool.");
        }
    }
}
// } // End namespace 