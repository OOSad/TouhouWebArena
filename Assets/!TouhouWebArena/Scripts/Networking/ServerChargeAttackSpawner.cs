using UnityEngine;
using Unity.Netcode;
using TouhouWebArena; // CharacterStats etc.

/// <summary>
/// **[Server Only]** Handles the logic for spawning character-specific charge attacks.
/// Instantiated and used by <see cref="ServerAttackSpawner"/>.
/// Charge attacks are client-simulated, triggered by RPCs to character-specific handlers
/// (e.g., <c>ReimuChargeAttackHandler_Client</c>, <c>MarisaChargeAttackHandler_Client</c>) on the client's player object.
/// </summary>
public class ServerChargeAttackSpawner
{
    /// <summary>
    /// **[Server Only]** Triggers the appropriate client-side charge attack for the requesting player's character.
    /// </summary>
    /// <param name="requesterClientId">The ClientId of the player who requested the attack.</param>
    public void SpawnChargeAttack(ulong requesterClientId)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[ServerChargeAttackSpawner] SpawnChargeAttack called on client? Aborting.");
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out NetworkClient networkClient) || networkClient.PlayerObject == null)
        {
            Debug.LogError($"[ServerChargeAttackSpawner.SpawnChargeAttack] Could not find player object for client {requesterClientId}");
            return;
        }
        Transform playerTransform = networkClient.PlayerObject.transform;
        NetworkObject playerNetworkObject = networkClient.PlayerObject;
        CharacterStats stats = playerNetworkObject.GetComponent<CharacterStats>();
        if (stats == null)
        {
             Debug.LogError($"[ServerChargeAttackSpawner.SpawnChargeAttack] Player object for client {requesterClientId} missing CharacterStats.");
             return;
        }

        PlayerRole ownerRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? playerData = PlayerDataManager.Instance.GetPlayerData(requesterClientId);
            if (playerData.HasValue)
            {
                ownerRole = playerData.Value.Role;
            }
        }
        if (ownerRole == PlayerRole.None)
        {
            Debug.LogError($"[ServerChargeAttackSpawner.SpawnChargeAttack] Could not determine PlayerRole for client {requesterClientId}. Aborting charge attack.");
            return;
        }

        string characterName = stats.GetCharacterName();

        if (characterName == "HakureiReimu")
        {
            var reimuHandler = playerNetworkObject.GetComponent<ReimuChargeAttackHandler_Client>();
            if (reimuHandler != null)
            {
                reimuHandler.SpawnChargeAttackClientRpc(playerTransform.position, ownerRole);
                // Debug.Log($"[ServerChargeAttackSpawner] Triggered Reimu Charge Attack RPC for Client: {requesterClientId}, Role: {ownerRole}");
            }
            else
            {
                Debug.LogError($"[ServerChargeAttackSpawner.SpawnChargeAttack] Player object for client {requesterClientId} (Reimu) missing ReimuChargeAttackHandler_Client component.");
            }
        }
        else if (characterName == "KirisameMarisa")
        {
            var marisaHandler = playerNetworkObject.GetComponent<MarisaChargeAttackHandler_Client>();
            if (marisaHandler != null)
            {
                marisaHandler.SpawnChargeAttackClientRpc(playerTransform.position, ownerRole, playerNetworkObject.NetworkObjectId);
                // Debug.Log($"[ServerChargeAttackSpawner] Triggered Marisa Charge Attack RPC for Client: {requesterClientId}, Role: {ownerRole}");
            }
            else
            {
                Debug.LogError($"[ServerChargeAttackSpawner.SpawnChargeAttack] Player object for client {requesterClientId} (Marisa) missing MarisaChargeAttackHandler_Client component.");
            }
        }
        else
        {
            Debug.LogWarning($"[ServerChargeAttackSpawner.SpawnChargeAttack] Unknown character '{characterName}' attempting charge attack for client {requesterClientId}. No attack defined.");
        }
    }

    // Removed old SpawnReimuChargeAttack and SpawnMarisaChargeAttack methods as logic is now in SpawnChargeAttack directly.
} 