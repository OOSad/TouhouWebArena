using UnityEngine;
using Unity.Netcode;

public class PlayerExtraAttackRelay : NetworkBehaviour
{
    public static PlayerExtraAttackRelay LocalInstance { get; private set; }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            LocalInstance = this;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner && LocalInstance == this)
        {
            LocalInstance = null;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void InformServerOfExtraAttackTriggerServerRpc(string characterName, PlayerRole attackerPlayerRole, ulong originalAttackerClientId, 
                                                        float pReimuSpawnX, float pReimuSpawnY, float pReimuSidewaysForce,
                                                        float pMarisaSpawnXOffset, float pMarisaTiltAngle,
                                                        ServerRpcParams rpcParams = default)
    {
        if (ClientExtraAttackManager.Instance != null)
        {
            Debug.Log($"[Server PlayerExtraAttackRelay] Received InformServerOfExtraAttackTriggerServerRpc from ClientId {originalAttackerClientId} for {characterName} ({attackerPlayerRole}). Relaying to clients with sync params.");
            ClientExtraAttackManager.Instance.RelayExtraAttackToClientsClientRpc(characterName, attackerPlayerRole, originalAttackerClientId,
                                                                               pReimuSpawnX, pReimuSpawnY, pReimuSidewaysForce,
                                                                               pMarisaSpawnXOffset, pMarisaTiltAngle);
        }
        else
        {
            Debug.LogError("[Server PlayerExtraAttackRelay] ClientExtraAttackManager.Instance is null on the server. Cannot relay ClientRpc.");
        }
    }

    [ServerRpc(RequireOwnership = true)] // Requires ownership because the client calls this on its own relay instance
    public void ReportExtraAttackPlayerHitServerRpc(ulong victimOwnerClientId, int damageAmount, ulong extraAttackOwnerClientId, ServerRpcParams rpcParams = default)
    {
        // This code executes on the server.
        NetworkObject victimNetworkObject = null;
        // Iterate through all spawned NetworkObjects to find the one owned by victimOwnerClientId
        foreach (NetworkObject networkObject in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (networkObject.OwnerClientId == victimOwnerClientId)
            {
                // We are looking for the player character, which should have PlayerHealth
                if (networkObject.GetComponent<PlayerHealth>() != null)
                {
                    victimNetworkObject = networkObject;
                    break;
                }
            }
        }

        if (victimNetworkObject != null)
        {
            PlayerHealth victimPlayerHealth = victimNetworkObject.GetComponent<PlayerHealth>();
            if (victimPlayerHealth != null) // This check should be redundant if the loop found it, but good for safety
            {
                Debug.Log($"[Server PlayerExtraAttackRelay] Player {extraAttackOwnerClientId} (Client {rpcParams.Receive.SenderClientId})'s extra attack hit Player {victimOwnerClientId}. Applying {damageAmount} damage.");
                victimPlayerHealth.TakeDamage(damageAmount); 
            }
            else
            {
                // This case should ideally not be reached if the loop logic is correct
                Debug.LogError($"[Server PlayerExtraAttackRelay] Found NetworkObject for victim {victimOwnerClientId} but it has no PlayerHealth component!");
            }
        }
        else
        {
            Debug.LogError($"[Server PlayerExtraAttackRelay] Could not find a Player NetworkObject owned by victim ClientId: {victimOwnerClientId} to apply extra attack damage.");
        }
    }
} 