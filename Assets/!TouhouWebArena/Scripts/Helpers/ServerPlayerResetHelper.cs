using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using TouhouWebArena; // For PlayerData, PlayerRole, CharacterStats, PlayerHealth
using TouhouWebArena.Managers; // For PlayerDataManager, SpellBarManager

namespace TouhouWebArena.Helpers
{
    /// <summary>
    /// [Server Only] Provides static helper methods for resetting player states (health, position, spell bars) during round resets.
    /// </summary>
    public static class ServerPlayerResetHelper
    {
        /// <summary>
        /// Finds and resets the health, spell bars, and positions of both players.
        /// Should only be called on the server.
        /// </summary>
        /// <param name="player1Spawn">The spawn transform for Player 1.</param>
        /// <param name="player2Spawn">The spawn transform for Player 2.</param>
        public static void ResetPlayersServer(Transform player1Spawn, Transform player2Spawn)
        {
            Debug.Log("[ServerPlayerResetHelper] Resetting player states...");

            // Ensure NetworkManager is available before proceeding
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                 Debug.LogError("[ServerPlayerResetHelper] Cannot reset players: Not on server or NetworkManager unavailable.");
                 return;
            }

            // Find player NetworkObjects using helper
            NetworkObject player1NetObj = GetPlayerNetworkObject(PlayerRole.Player1);
            NetworkObject player2NetObj = GetPlayerNetworkObject(PlayerRole.Player2);

            // --- Reset Health ---
            Debug.Log("[ServerPlayerResetHelper] Attempting to reset Player 1 health...");
            ResetPlayerHealth(player1NetObj, PlayerRole.Player1);

            Debug.Log("[ServerPlayerResetHelper] Attempting to reset Player 2 health...");
            ResetPlayerHealth(player2NetObj, PlayerRole.Player2);

            // --- Reset Spell Bars (COMMENTED OUT) ---
            // Debug.Log("[ServerPlayerResetHelper] Attempting to reset spell bars...");
            // if (SpellBarManager.Instance != null)
            // {
            //     PlayerData? p1Data = PlayerDataManager.Instance?.GetPlayerDataByRole(PlayerRole.Player1);
            //     PlayerData? p2Data = PlayerDataManager.Instance?.GetPlayerDataByRole(PlayerRole.Player2);
            // 
            //     if (p1Data.HasValue) SpellBarManager.Instance.ResetSpellBarServer(p1Data.Value.ClientId);
            //     else Debug.LogWarning("[ServerPlayerResetHelper] Could not find P1 data to reset spell bar.");
            // 
            //     if (p2Data.HasValue) SpellBarManager.Instance.ResetSpellBarServer(p2Data.Value.ClientId);
            //     else Debug.LogWarning("[ServerPlayerResetHelper] Could not find P2 data to reset spell bar.");
            // }
            // else { Debug.LogWarning("[ServerPlayerResetHelper] SpellBarManager instance not found, cannot reset spell bars."); }
            // -------------------------------------

            // --- Reset Position ---
            Debug.Log("[ServerPlayerResetHelper] Attempting to reset Player 1 position...");
            ResetPlayerPosition(player1NetObj, player1Spawn, PlayerRole.Player1);

            Debug.Log("[ServerPlayerResetHelper] Attempting to reset Player 2 position...");
            ResetPlayerPosition(player2NetObj, player2Spawn, PlayerRole.Player2);
            
            Debug.Log("[ServerPlayerResetHelper] Player reset finished.");
        }

        /// <summary>
        /// Helper to reset a single player's health.
        /// </summary>
        private static void ResetPlayerHealth(NetworkObject playerNetObj, PlayerRole role)
        {
             if (playerNetObj != null) 
            {
                if (playerNetObj.TryGetComponent<PlayerHealth>(out var playerHealth)) 
                {
                    CharacterStats playerStats = playerNetObj.GetComponent<CharacterStats>(); 
                    if (playerStats != null)
                    {
                        playerHealth.SetHealthDirectlyServer(playerStats.GetStartingHealth());
                        Debug.Log($"[ServerPlayerResetHelper] Reset {role} health to {playerStats.GetStartingHealth()}.");
                    }
                    else { Debug.LogError($"[ServerPlayerResetHelper] Cannot reset {role} health directly: CharacterStats not found."); }
                } 
                else { Debug.LogWarning($"[ServerPlayerResetHelper] {role} NetworkObject found, but PlayerHealth component missing."); }
            }
            else { Debug.LogWarning($"[ServerPlayerResetHelper] Could not find {role} NetworkObject to reset health."); }
        }

        /// <summary>
        /// Helper to reset a single player's position.
        /// </summary>
        private static void ResetPlayerPosition(NetworkObject playerNetObj, Transform spawnPoint, PlayerRole role)
        {
            if (spawnPoint != null && playerNetObj != null) 
            {
                playerNetObj.transform.position = spawnPoint.position;
                playerNetObj.transform.rotation = spawnPoint.rotation;
                
                Debug.Log($"[ServerPlayerResetHelper] Directly set {role} server position to {spawnPoint.position} and rotation to {spawnPoint.rotation.eulerAngles}.");

                // Inform the client to update its state
                if (playerNetObj.TryGetComponent<ClientAuthMovement>(out var clientAuthMovement))
                {
                    // Always send to the owner of the playerNetObj.
                    // The IsOwner check within ServerForceTeleport on the client side handles ensuring only the owner processes it.
                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { playerNetObj.OwnerClientId }
                        }
                    };
                    clientAuthMovement.TeleportClientRpc(spawnPoint.position, spawnPoint.rotation, clientRpcParams);
                    Debug.Log($"[ServerPlayerResetHelper] Sent TeleportClientRpc to {role} (Client ID: {playerNetObj.OwnerClientId}).");
                }
                else
                {
                    Debug.LogWarning($"[ServerPlayerResetHelper] Could not find ClientAuthMovement component on {role} to send TeleportClientRpc.");
                }
            }
            else 
            {
                string reason = "";
                if (spawnPoint == null) reason += "spawnPoint is null. ";
                if (playerNetObj == null) reason += "playerNetObj is null. ";
                // If NetworkTransform was required, we'd check it here too.
                Debug.LogWarning($"[ServerPlayerResetHelper] Could not reset {role} position. Reason: {reason}");
            }
        }

        /// <summary>
        /// Helper method to get a player's NetworkObject based on their role.
        /// Server only.
        /// </summary>
        /// <returns>The NetworkObject, or null if not found or NetworkManager/PlayerDataManager are unavailable.</returns>
        public static NetworkObject GetPlayerNetworkObject(PlayerRole role)
        {
            // Ensure NetworkManager and PlayerDataManager are available
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || PlayerDataManager.Instance == null)
            {
                Debug.LogError("[ServerPlayerResetHelper] Cannot get player NetworkObject: NetworkManager/PlayerDataManager not ready or not on server.");
                return null;
            }

            PlayerData? playerData = PlayerDataManager.Instance.GetPlayerDataByRole(role);
            if (!playerData.HasValue)
            { 
                Debug.LogWarning($"[ServerPlayerResetHelper] PlayerData not found for role {role}.");
                return null; 
            }
           
            if (NetworkManager.Singleton.SpawnManager == null)
            {
                Debug.LogError("[ServerPlayerResetHelper] SpawnManager not ready!");
                return null;
            }
            return NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
        }
    }
} 