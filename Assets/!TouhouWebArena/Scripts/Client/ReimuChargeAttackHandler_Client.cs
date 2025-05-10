using UnityEngine;
using Unity.Netcode;

// Potentially: namespace TouhouWebArena.Client {
public class ReimuChargeAttackHandler_Client : NetworkBehaviour
{
    // --- Reimu Homing Talisman ---
    [Header("Reimu Charge Attack (Client)")]
    [SerializeField] private string reimuTalismanPrefabId = "ReimuChargeTalisman_Client"; // Client-side prefab ID
    [SerializeField] private int reimuTalismanCount = 4;
    [SerializeField] private float reimuTalismanSpawnRadius = 0.5f;
    [SerializeField] private float reimuTalismanInitialDelay = 0.1f; // Stagger spawn slightly

    // Note: No OnNetworkSpawn/Despawn needed if it's not a singleton

    // --- RPC for Reimu's Homing Talismans ---
    [ClientRpc]
    public void SpawnChargeAttackClientRpc(Vector3 spawnCenter, PlayerRole ownerRole, ClientRpcParams clientRpcParams = default)
    {
        // Debug.Log($"[{GetType().Name}] Received RPC to spawn Reimu's Charge Attack for owner: {ownerRole}");
        if (ClientGameObjectPool.Instance == null)
        {
            Debug.LogError($"[{GetType().Name}] ClientGameObjectPool.Instance is null. Cannot spawn Reimu talismans.");
            return;
        }

        for (int i = 0; i < reimuTalismanCount; i++)
        {
            GameObject talismanGO = ClientGameObjectPool.Instance.GetObject(reimuTalismanPrefabId);
            if (talismanGO != null)
            {
                float angle = i * (360f / reimuTalismanCount);
                Vector3 offset = Quaternion.Euler(0, 0, angle) * Vector3.up * reimuTalismanSpawnRadius;
                talismanGO.transform.position = spawnCenter + offset;
                talismanGO.transform.rotation = Quaternion.identity; 

                talismanGO.SetActive(true);

                HomingTalisman_Client talismanScript = talismanGO.GetComponent<HomingTalisman_Client>();
                if (talismanScript != null)
                {
                    talismanScript.Initialize(ownerRole, reimuTalismanInitialDelay * i); 
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] Spawned talisman '{reimuTalismanPrefabId}' is missing HomingTalisman_Client script.", talismanGO);
                }
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] Failed to get talisman '{reimuTalismanPrefabId}' from pool.");
            }
        }
    }
}
// } // End namespace 