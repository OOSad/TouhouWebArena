using UnityEngine;
using Unity.Netcode;

/// <summary>
/// [Client-Only] Handles ClientRPCs from the server to spawn and initialize spirits locally.
/// Uses ClientGameObjectPool to manage spirit instances.
/// </summary>
public class ClientSpiritSpawnHandler : NetworkBehaviour
{
    public static ClientSpiritSpawnHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnNetworkDespawn();
    }

    // This RPC will be called by the server's SpiritSpawner
    [ClientRpc]
    public void SpawnSpiritClientRpc(PlayerRole owningSide,
                                   string spiritPrefabID, Vector3 position, 
                                   bool shouldAim, ulong targetNetworkObjId,
                                   bool isRevengeSpawn, float initialVelocity, 
                                   int spiritType /* For future variations */)
    {
        if (!IsClient) return; // Should only execute on clients

        // Debug.Log($"[ClientSpiritSpawnHandler] Received SpawnSpiritClientRpc: PrefabID={spiritPrefabID}, Pos={position}, Aim={shouldAim}, TargetNetObjID={targetNetworkObjId}, Revenge={isRevengeSpawn}, Vel={initialVelocity}, Type={spiritType}, OwningSide={owningSide}");

        GameObject spiritInstance = ClientGameObjectPool.Instance.GetObject(spiritPrefabID);
        if (spiritInstance == null)
        {
            Debug.LogError($"[ClientSpiritSpawnHandler] Failed to get spirit prefab '{spiritPrefabID}' from pool.", this);
            return;
        }

        spiritInstance.transform.position = position;
        spiritInstance.transform.rotation = Quaternion.identity; // Default rotation

        // ACTIVATE THE GAMEOBJECT **BEFORE** GETTING/INITIALIZING COMPONENTS
        spiritInstance.SetActive(true);
        // Debug.Log($"[ClientSpiritSpawnHandler] Activated spirit '{spiritPrefabID}' from pool at {position}. Now initializing components.", spiritInstance);

        // --- Get and Initialize Client-Side Spirit Components ---
        ClientSpiritController controller = spiritInstance.GetComponent<ClientSpiritController>();
        if (controller != null)
        {
            // Pass spiritInstance.transform for the originTransform parameter
            controller.Initialize(owningSide, shouldAim, targetNetworkObjId, isRevengeSpawn, initialVelocity, spiritType, spiritInstance.transform);
        }
        else
        {
            Debug.LogError($"[ClientSpiritSpawnHandler] Spirit prefab '{spiritPrefabID}' is missing ClientSpiritController component.", spiritInstance);
        }

        ClientSpiritHealth health = spiritInstance.GetComponent<ClientSpiritHealth>();
        if (health != null)
        {
            health.Initialize(spiritType); // Pass spiritType to determine starting HP
        }
        else
        {
            Debug.LogError($"[ClientSpiritSpawnHandler] Spirit prefab '{spiritPrefabID}' is missing ClientSpiritHealth component.", spiritInstance);
        }
        
        ClientSpiritTimeoutAttack timeoutAttack = spiritInstance.GetComponent<ClientSpiritTimeoutAttack>();
        if (timeoutAttack == null)
        {
            // This might be acceptable if not all spirits are intended to have a timeout attack by default.
            // However, based on the requirements, all activated spirits should have one.
            Debug.LogWarning($"[ClientSpiritSpawnHandler] Spirit prefab '{spiritPrefabID}' is missing ClientSpiritTimeoutAttack component. Activated spirits may not function correctly.", spiritInstance);
        }

        // spiritInstance.SetActive(true); // MOVED UP
        // Debug.Log($"[ClientSpiritSpawnHandler] Successfully initialized spirit '{spiritPrefabID}' at {position}", spiritInstance);
    }
} 