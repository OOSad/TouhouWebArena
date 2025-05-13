using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components; // Added for NetworkTransform
using TouhouWebArena; // For PoolableObjectIdentity, PlayerRole etc.
using TouhouWebArena.Spellcards.Behaviors; // For NetworkBulletLifetime
using TouhouWebArena.Spellcards; // Added for IllusionHealth
using TouhouWebArena.Helpers; // Added
using TouhouWebArena.Client; // Added for ClientEntityCleanupHandler
using TMPro; // Added for potential UI references if needed later

namespace TouhouWebArena.Managers
{
    /// <summary>
    /// [Server Only] Manages the game rounds, scoring, and transitions between rounds/matches.
    /// Listens for player deaths to update scores and trigger round resets or match end.
    /// Depends on PlayerDataManager, SpellBarManager, ServerSpawnerManager, ServerDisconnectHandler.
    /// </summary>
    public class RoundManager : NetworkBehaviour
    {
        // --- Score Tracking ---
        private const int WinningScore = 2; // First to 2 points wins

        /// <summary>NetworkVariable tracking Player 1's score.</summary>
        public NetworkVariable<int> Player1Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        /// <summary>NetworkVariable tracking Player 2's score.</summary>
        public NetworkVariable<int> Player2Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        /// <summary>NetworkVariable indicating if a round is currently in active gameplay (true) or transition (false).</summary>
        public NetworkVariable<bool> IsRoundActive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        /// <summary>NetworkVariable tracking the elapsed time in the current round. Server authoritative.</summary>
        public NetworkVariable<float> RoundTime = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Header("Round Settings")]
        [SerializeField]
        [Tooltip("Delay in seconds after a round ends before the next round starts.")]
        private float roundResetDelay = 3.0f;

        [Header("Spawn Points")]
        [Tooltip("Reference to the Transform defining Player 1's spawn location.")]
        [SerializeField] private Transform player1SpawnPoint;
        [Tooltip("Reference to the Transform defining Player 2's spawn location.")]
        [SerializeField] private Transform player2SpawnPoint;

        // --- Rematch State (Server Only) ---
        private bool player1WantsRematch = false;
        private bool player2WantsRematch = false;
        private bool matchHasEnded = false; // Track if the match conclusion has been reached

        // --- Cached reference for ClientEntityCleanupHandler ---
        private ClientEntityCleanupHandler clientEntityCleanupHandlerCache;

        // --- Add Update method for timer ---
        void Update()
        {
            if (!IsServer) return; // Only server updates the time

            if (IsRoundActive.Value)
            {
                RoundTime.Value += Time.deltaTime;
                // --- Add Log ---
                // Log roughly once per second to avoid spam
                // if (Time.frameCount % 60 == 0) 
                // {
                //     Debug.Log($"[RoundManager Server] IsRoundActive: {IsRoundActive.Value}, RoundTime: {RoundTime.Value}");
                // }
                // ---------------
            }
        }
        // ----------------------------------

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer) 
            {
                enabled = false; // This script is server-only
                return;
            }

            // Subscribe to events
            PlayerHealth.OnPlayerDeathServer += HandlePlayerDeathServer;

            // Cache the ClientEntityCleanupHandler instance if we are the server
            if (IsServer)
            {
                clientEntityCleanupHandlerCache = FindFirstObjectByType<ClientEntityCleanupHandler>();
                if (clientEntityCleanupHandlerCache == null)
                {
                    Debug.LogError("[RoundManager] Could not find ClientEntityCleanupHandler in the scene! Client-side cleanup will not work.");
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                // Unsubscribe from events
                PlayerHealth.OnPlayerDeathServer -= HandlePlayerDeathServer;
            }
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// [Server Only] Handler for the PlayerHealth.OnPlayerDeathServer event.
        /// Determines the winner, updates score, and checks for round/match end.
        /// </summary>
        /// <param name="deadPlayerClientId">The ClientId of the player whose health reached zero.</param>
        private void HandlePlayerDeathServer(ulong deadPlayerClientId)
        {
            if (!IsServer) return;

            Debug.Log($"[RoundManager] Player death detected for ClientId: {deadPlayerClientId}");

            // Determine winner based on deadPlayerClientId and PlayerDataManager
            PlayerData? deadPlayerData = PlayerDataManager.Instance?.GetPlayerData(deadPlayerClientId);
            if (!deadPlayerData.HasValue)
            {
                Debug.LogError($"[RoundManager] Could not find PlayerData for dead client {deadPlayerClientId}! Cannot process round end.");
                return;
            }

            PlayerRole deadPlayerRole = deadPlayerData.Value.Role;
            PlayerRole winnerRole = PlayerRole.None;
            int newScore = -1;

            // Increment winner's score NetworkVariable
            if (deadPlayerRole == PlayerRole.Player1)
            {
                winnerRole = PlayerRole.Player2;
                Player2Score.Value++;
                newScore = Player2Score.Value;
                Debug.Log($"[RoundManager] Player 2 wins the round! New Score -> P1: {Player1Score.Value}, P2: {Player2Score.Value}");
            }
            else if (deadPlayerRole == PlayerRole.Player2)
            {
                winnerRole = PlayerRole.Player1;
                Player1Score.Value++;
                newScore = Player1Score.Value;
                Debug.Log($"[RoundManager] Player 1 wins the round! New Score -> P1: {Player1Score.Value}, P2: {Player2Score.Value}");
            }
            else
            {
                Debug.LogError($"[RoundManager] Dead player ({deadPlayerClientId}) had role 'None'! Cannot assign score.");
                return;
            }

            // Check if winner's score >= WinningScore
            if (newScore >= WinningScore)
            {
                Debug.Log($"[RoundManager] MATCH END! Player {winnerRole} reached {newScore} points. Notifying clients...");

                // --- Notify Clients via ClientRpc ---
                // Get the ClientId of the winner
                PlayerData? winnerData = PlayerDataManager.Instance?.GetPlayerDataByRole(winnerRole);
                if (!winnerData.HasValue)
                {
                    Debug.LogError($"[RoundManager] Could not find PlayerData for winner role {winnerRole}! Cannot send MatchEnded RPC.");
                    return; // Or handle differently?
                }
                ulong winnerClientId = winnerData.Value.ClientId;

                // Prepare ClientRpc parameters to target only the two players involved
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { deadPlayerClientId, winnerClientId }
                    }
                };

                // Send the RPC to the specific clients
                MatchEndedClientRpc(winnerRole, clientRpcParams);
                matchHasEnded = true; // Mark match as ended on the server
                // -------------------------------------
            }
            else
            {   
                // Start Round Reset coroutine
                Debug.Log("[RoundManager] Round finished, starting round reset...");
                StartCoroutine(RoundResetCoroutine());
            }
        }

        // TODO: Implement Round Reset Coroutine
        private System.Collections.IEnumerator RoundResetCoroutine()
        {
            matchHasEnded = false; // Reset the flag now that a new round/match is starting

            // --- Tell Clients to Hide Match End UI --- 
            // Send RPC to *all* clients who might have the panel open
            // Note: This assumes MaxPlayers or similar is defined or accessible
            // If not, we might need to track clients involved in the last match.
            // For simplicity, let's send to all connected clients for now.
            ClientRpcParams allClientsParams = new ClientRpcParams { 
                Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds }
            };
            HideMatchEndPanelClientRpc(allClientsParams);
            // ---------------------------------------

            if (!IsServer) yield break;

            IsRoundActive.Value = false;
            Debug.Log("[RoundManager] Round Reset: Deactivating spawners and clearing entities.");

            // --- 1. Pause Systems ---
            // Disable Spawners using ServerSpawnerManager
            if (ServerSpawnerManager.Instance != null)
            {
                 ServerSpawnerManager.Instance.PauseAllSpawners();
            } else {
                 Debug.LogError("[RoundManager] ServerSpawnerManager instance not found! Cannot pause spawners.");
            }
           
            // TODO: Disable Player Input?

            // --- 2. Clear Entities --- 
            // Delay slightly to allow in-flight projectiles to finish spawning/registering?
            yield return new WaitForSeconds(0.2f); 

            // Call the static helper method to clear entities
            ServerEntityCleanupHelper.CleanupAllEntitiesServer();

            // --- ADDED: Tell clients to clear their specific visuals ---
            if (clientEntityCleanupHandlerCache != null) // Changed from ClientEntityCleanupHandler.Instance
            {
                ClientRpcParams allClientsParamsForCleanup = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds }
                };
                clientEntityCleanupHandlerCache.ClearAllClientSideVisualsClientRpc(allClientsParamsForCleanup); // Changed from ClientEntityCleanupHandler.Instance
                Debug.Log("[RoundManager] Sent ClearAllClientSideVisualsClientRpc to all clients via cached reference.");
            }
            else
            {
                Debug.LogWarning("[RoundManager] Cached ClientEntityCleanupHandler is null. Cannot send RPC to clear client visuals.");
            }
            // ---------------------------------------------------------

            // --- 3. Reset Players ---
            // Call the static helper method to reset players
            ServerPlayerResetHelper.ResetPlayersServer(player1SpawnPoint, player2SpawnPoint);

            // --- 4. Wait --- 
            Debug.Log($"[RoundManager] Waiting for {roundResetDelay} seconds...");
            yield return new WaitForSeconds(roundResetDelay);

            // --- 5. Resume Systems ---
            Debug.Log("[RoundManager] Resuming systems.");
            // Resume Spawners using ServerSpawnerManager
             if (ServerSpawnerManager.Instance != null)
            {
                 ServerSpawnerManager.Instance.ResumeAllSpawners();
            } else {
                 Debug.LogError("[RoundManager] ServerSpawnerManager instance not found! Cannot resume spawners.");
            }

            // TODO: Re-enable player input? (Ensure input is disabled when match ends / round resets)

            // Reset timer before activating round
            RoundTime.Value = 0f;

            IsRoundActive.Value = true;
            Debug.Log("[RoundManager] Round Reset Complete. New round active.");
        }

        /// <summary>
        /// [ClientRpc] Sent by the server to specific clients when the match has concluded.
        /// </summary>
        /// <param name="winnerRole">The PlayerRole of the player who won the match.</param>
        /// <param name="clientRpcParams">Parameters targeting the specific clients involved.</param>
        [ClientRpc]
        private void MatchEndedClientRpc(PlayerRole winnerRole, ClientRpcParams clientRpcParams = default)
        {
            // This code runs on the clients specified in clientRpcParams
            Debug.Log($"[RoundManager - Client {NetworkManager.Singleton.LocalClientId}] Received MatchEnded RPC. Winner: {winnerRole}. Activating UI...");

            // Find the local MatchEndUIController instance and show the screen
            // Need to include inactive objects since the panel starts disabled
            if (MatchEndUIController.Instance != null)
            {
                MatchEndUIController.Instance.ShowMatchEndScreen(winnerRole);
            }
            else
            {
                Debug.LogError($"[RoundManager - Client {NetworkManager.Singleton.LocalClientId}] Could not find MatchEndUIController singleton instance!");
            }
        }

        /// <summary>
        /// [ClientRpc] Tells clients to hide the Match End UI panel.
        /// </summary>
        [ClientRpc]
        private void HideMatchEndPanelClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (MatchEndUIController.Instance != null)
            {
                MatchEndUIController.Instance.HideMatchEndScreen();
            }
            // No error log needed if not found, maybe it wasn't shown
        }

        /// <summary>
        /// [ServerRpc] Called by a client when they click the Rematch button.
        /// </summary>
        [ServerRpc(RequireOwnership = false)] // Called by clients, need RequireOwnership = false
        public void RequestRematchServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"[RoundManager] Received Rematch request from ClientId: {senderClientId}");

            // Determine which player sent the request
            PlayerData? senderData = PlayerDataManager.Instance?.GetPlayerData(senderClientId);
            if (!senderData.HasValue)
            { 
                Debug.LogError($"[RoundManager] Cannot process rematch request: PlayerData not found for sender {senderClientId}.");
                return; 
            }

            PlayerRole senderRole = senderData.Value.Role;
            bool bothReady = false;

            if (senderRole == PlayerRole.Player1)
            {
                player1WantsRematch = true;
                Debug.Log("[RoundManager] Player 1 wants rematch.");
                if (player2WantsRematch) bothReady = true;
            }
            else if (senderRole == PlayerRole.Player2)
            {
                player2WantsRematch = true;
                Debug.Log("[RoundManager] Player 2 wants rematch.");
                if (player1WantsRematch) bothReady = true;
            }
            else
            {
                Debug.LogWarning($"[RoundManager] Rematch request from client {senderClientId} with invalid role {senderRole}.");
                return;
            }

            // Check if both players are ready
            if (bothReady)
            {
                Debug.Log("[RoundManager] Both players agreed to rematch! Resetting match...");

                // Reset state for new match
                Player1Score.Value = 0;
                Player2Score.Value = 0;
                player1WantsRematch = false;
                player2WantsRematch = false;
                matchHasEnded = false; // Reset flag before starting the new match sequence

                // --- ADDED: Reset Spell Bars ---
                SpellBarManager spellBarManager = SpellBarManager.Instance; // Assuming Singleton pattern
                if (spellBarManager != null)
                {
                    Debug.Log("[RoundManager] Resetting spell bars for rematch...");
                    // Get PlayerData for both roles to reset their specific bars
                    PlayerData? p1Data = PlayerDataManager.Instance?.GetPlayerDataByRole(PlayerRole.Player1);
                    PlayerData? p2Data = PlayerDataManager.Instance?.GetPlayerDataByRole(PlayerRole.Player2);

                    if (p1Data.HasValue) spellBarManager.ResetSpellBarServer(p1Data.Value.ClientId);
                    else Debug.LogWarning("[RoundManager] Could not find Player 1 data to reset spell bar.");

                    if (p2Data.HasValue) spellBarManager.ResetSpellBarServer(p2Data.Value.ClientId);
                    else Debug.LogWarning("[RoundManager] Could not find Player 2 data to reset spell bar.");
                }
                else
                {
                    Debug.LogError("[RoundManager] SpellBarManager instance not found! Cannot reset spell bars.");
                }
                // -----------------------------

                // TODO: Reset any other persistent match stats if necessary

                // Start the first round of the new match
                // The RoundResetCoroutine will handle hiding the MatchEndPanel implicitly
                // by resetting player states and enabling gameplay.
                StartCoroutine(RoundResetCoroutine()); 
            }
            else
            {
                 Debug.Log("[RoundManager] Waiting for other player to confirm rematch...");
                 // TODO: Potentially send a ClientRpc back to the requester? Or just let them wait?
                 // TODO: Implement a timeout? 
            }
        }

        // --- ADDED Helper methods for ServerDisconnectHandler ---
        
        /// <summary>
        /// [Server Only] Checks if the game is currently in a post-match state 
        /// (match ended, waiting for rematch decisions).
        /// </summary>
        /// <returns>True if the match has ended or a player wants a rematch, false otherwise.</returns>
        public bool IsInPostMatchPhase()
        {
            if (!IsServer) return false;
            return matchHasEnded || player1WantsRematch || player2WantsRematch;
        }

        /// <summary>
        /// [Server Only] Resets the rematch request flags.
        /// </summary>
        public void ResetRematchFlags()
        {
            if (!IsServer) return;
            player1WantsRematch = false;
            player2WantsRematch = false;
            Debug.Log("[RoundManager] Rematch flags reset.");
        }

        /// <summary>
        /// [Server Only] Stops coroutines that might interfere with disconnect handling.
        /// Currently stops all coroutines, might need refinement later.
        /// </summary>
        public void StopDisconnectHandlerCoroutines()
        {
            if (!IsServer) return;
            Debug.Log("[RoundManager] Stopping coroutines for disconnect handling.");
            StopAllCoroutines(); // Simple for now, might need specific coroutine stopping later
        }
        // ----------------------------------------------------
    }
} 