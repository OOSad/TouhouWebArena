using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components; // Added for NetworkTransform
using TouhouWebArena; // For PoolableObjectIdentity, PlayerRole etc.
using TouhouWebArena.Spellcards.Behaviors; // For NetworkBulletLifetime
using TouhouWebArena.Spellcards; // Added for IllusionHealth

namespace TouhouWebArena.Managers
{
    /// <summary>
    /// [Server Only] Manages the game rounds, scoring, and transitions between rounds/matches.
    /// Listens for player deaths to update scores and trigger round resets or match end.
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

        [Header("Round Settings")]
        [SerializeField]
        [Tooltip("Delay in seconds after a round ends before the next round starts.")]
        private float roundResetDelay = 3.0f;

        [Header("Spawn Points")]
        [Tooltip("Reference to the Transform defining Player 1's spawn location.")]
        [SerializeField] private Transform player1SpawnPoint;
        [Tooltip("Reference to the Transform defining Player 2's spawn location.")]
        [SerializeField] private Transform player2SpawnPoint;

        // Cached references (Server only)
        private SpiritSpawner spiritSpawnerInstance;
        private List<FairySpawner> fairySpawnerInstances = new List<FairySpawner>();

        // --- Rematch State (Server Only) ---
        private bool player1WantsRematch = false;
        private bool player2WantsRematch = false;
        private bool matchHasEnded = false; // Track if the match conclusion has been reached

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer) 
            {
                enabled = false; // This script is server-only
                return;
            }

            // Cache Spawner references on Server
            CacheSpawnerReferences();

            // Subscribe to events
            PlayerHealth.OnPlayerDeathServer += HandlePlayerDeathServer;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnectServer;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                // Unsubscribe from events
                PlayerHealth.OnPlayerDeathServer -= HandlePlayerDeathServer;
                if (NetworkManager.Singleton != null) 
                {
                    NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnectServer;
                }
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

        /// <summary>
        /// Finds and caches references to the Spirit and Fairy spawners in the scene.
        /// Server only.
        /// </summary>
        private void CacheSpawnerReferences()
        {
            spiritSpawnerInstance = FindObjectOfType<SpiritSpawner>();
            if (spiritSpawnerInstance == null)
            {
                Debug.LogWarning("[RoundManager] Could not find SpiritSpawner instance to cache.");
            }

            fairySpawnerInstances.Clear();
            fairySpawnerInstances.AddRange(FindObjectsOfType<FairySpawner>());
            if (fairySpawnerInstances.Count == 0)
            {
                Debug.LogWarning("[RoundManager] Could not find any FairySpawner instances to cache.");
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
            // Disable Spawners
            spiritSpawnerInstance?.SetSpawningEnabledServer(false);
            foreach(var fs in fairySpawnerInstances) { fs?.SetSpawningEnabledServer(false); }
            // TODO: Disable Player Input?

            // --- 2. Clear Entities --- 
            // Delay slightly to allow in-flight projectiles to finish spawning/registering?
            yield return new WaitForSeconds(0.2f); 

            // Clear Projectiles (Multiple types)
            List<NetworkObject> projectilesToClear = new List<NetworkObject>();

            // Find Spellcard Bullets
            NetworkBulletLifetime[] spellcardBullets = FindObjectsOfType<NetworkBulletLifetime>();
            foreach (var bullet in spellcardBullets) { projectilesToClear.Add(bullet.GetComponent<NetworkObject>()); }

            // Find Stage Bullets
            StageSmallBulletMoverScript[] stageBullets = FindObjectsOfType<StageSmallBulletMoverScript>();
            foreach (var bullet in stageBullets) { projectilesToClear.Add(bullet.GetComponent<NetworkObject>()); }

            // TODO: Find Player Shots?
            // TODO: Find Extra Attack projectiles?

            Debug.Log($"[RoundManager] Clearing {projectilesToClear.Count} projectiles (Spellcard/Stage).");
            foreach(var netObj in projectilesToClear)
            {
                if (netObj != null) { 
                    // Use TryGetComponent for safety
                    if (NetworkObjectPool.Instance != null && netObj.TryGetComponent<PoolableObjectIdentity>(out _))
                    {
                        NetworkObjectPool.Instance.ReturnNetworkObject(netObj); 
                    }
                }
            }

            // Clear Fairies
            Fairy[] activeFairies = FindObjectsOfType<Fairy>();
            Debug.Log($"[RoundManager] Clearing {activeFairies.Length} fairies.");
            foreach(var fairy in activeFairies)
            {
                NetworkObject netObj = fairy.GetComponent<NetworkObject>();
                if (netObj != null && NetworkObjectPool.Instance != null)
                {
                    NetworkObjectPool.Instance.ReturnNetworkObject(netObj); 
                }
                else if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
            }

            // Clear Spirits
            SpiritController[] activeSpirits = FindObjectsOfType<SpiritController>();
            Debug.Log($"[RoundManager] Clearing {activeSpirits.Length} spirits.");
            foreach(var spirit in activeSpirits)
            {
                NetworkObject netObj = spirit.GetComponent<NetworkObject>();
                if (netObj != null && NetworkObjectPool.Instance != null)
                {
                    NetworkObjectPool.Instance.ReturnNetworkObject(netObj); 
                }
                else if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
            }

            // Clear Illusions (Assuming IllusionHealth component)
            IllusionHealth[] activeIllusions = FindObjectsOfType<IllusionHealth>();
            Debug.Log($"[RoundManager] Clearing {activeIllusions.Length} illusions.");
            foreach(var illusion in activeIllusions)
            {
                NetworkObject netObj = illusion.GetComponent<NetworkObject>();
                if (netObj != null)
                { 
                    // Check if pooled first
                    if (NetworkObjectPool.Instance != null && netObj.TryGetComponent<PoolableObjectIdentity>(out _))
                    {
                        NetworkObjectPool.Instance.ReturnNetworkObject(netObj);
                    }
                    else if (netObj.IsSpawned) // Fallback to despawn if not pooled or pool unavailable
                    {
                        netObj.Despawn(true);
                    }
                }
            }

            // --- ADDED: Clear Extra Attacks --- 
            GameObject[] extraAttacks = GameObject.FindGameObjectsWithTag("ExtraAttack"); // Use the tag
            Debug.Log($"[RoundManager] Clearing {extraAttacks.Length} Extra Attack objects.");
            foreach (GameObject extraAttack in extraAttacks)
            {
                if (extraAttack.TryGetComponent<NetworkObject>(out var netObj))
                {
                    if (netObj.IsSpawned)
                    {
                        netObj.Despawn(true);
                    }
                }
                else
                {
                    // If it has the tag but no NetworkObject, destroy it directly (shouldn't happen ideally)
                    Debug.LogWarning($"[RoundManager] Found ExtraAttack tagged object '{extraAttack.name}' without a NetworkObject. Destroying directly.");
                    Destroy(extraAttack);
                }
            }
            // ---------------------------------

            // --- 3. Reset Players ---
            Debug.Log("[RoundManager] Resetting player states...");

            // Find player NetworkObjects (helper function already exists)
            NetworkObject player1NetObj = GetPlayerNetworkObject(PlayerRole.Player1);
            NetworkObject player2NetObj = GetPlayerNetworkObject(PlayerRole.Player2);

            // Reset Health
            Debug.Log("[RoundManager] Attempting to reset Player 1 health...");
            if (player1NetObj != null) 
            {
                if (player1NetObj.TryGetComponent<PlayerHealth>(out var p1Health)) 
                {
                    Debug.Log("[RoundManager] Found P1 Health, calling SetHealthDirectlyServer...");
                    CharacterStats p1Stats = player1NetObj.GetComponent<CharacterStats>(); // Get stats for max HP
                    if (p1Stats != null)
                    {
                        p1Health.SetHealthDirectlyServer(p1Stats.GetStartingHealth());
                    }
                    else { Debug.LogError("[RoundManager] Cannot reset P1 health directly: CharacterStats not found."); }
                } 
                else 
                { 
                    Debug.LogWarning("[RoundManager] Player 1 NetworkObject found, but PlayerHealth component missing."); 
                }
            }
            else 
            { 
                Debug.LogWarning("[RoundManager] Could not find Player 1 NetworkObject to reset health."); 
            }

            Debug.Log("[RoundManager] Attempting to reset Player 2 health...");
            if (player2NetObj != null)
            {
                if (player2NetObj.TryGetComponent<PlayerHealth>(out var p2Health)) 
                {
                    Debug.Log("[RoundManager] Found P2 Health, calling SetHealthDirectlyServer...");
                    CharacterStats p2Stats = player2NetObj.GetComponent<CharacterStats>(); // Get stats for max HP
                    if (p2Stats != null)
                    {
                        p2Health.SetHealthDirectlyServer(p2Stats.GetStartingHealth());
                    }
                    else { Debug.LogError("[RoundManager] Cannot reset P2 health directly: CharacterStats not found."); }
                }
                else 
                { 
                    Debug.LogWarning("[RoundManager] Player 2 NetworkObject found, but PlayerHealth component missing."); 
                }
            }
            else 
            { 
                Debug.LogWarning("[RoundManager] Could not find Player 2 NetworkObject to reset health."); 
            }

            // Reset Spell Bars (by setting charge to 0)
            if (SpellBarManager.Instance != null)
            {
                // Get PlayerData for both roles to reset their specific bars
                PlayerData? p1Data = PlayerDataManager.Instance?.GetPlayerDataByRole(PlayerRole.Player1);
                PlayerData? p2Data = PlayerDataManager.Instance?.GetPlayerDataByRole(PlayerRole.Player2);

                if (p1Data.HasValue) SpellBarManager.Instance.ResetSpellBarServer(p1Data.Value.ClientId);
                else Debug.LogWarning("[RoundManager][RoundReset] Could not find Player 1 data to reset spell bar.");

                if (p2Data.HasValue) SpellBarManager.Instance.ResetSpellBarServer(p2Data.Value.ClientId);
                else Debug.LogWarning("[RoundManager][RoundReset] Could not find Player 2 data to reset spell bar.");
                Debug.Log("[RoundManager] Attempted to reset player spell bars using ResetSpellBarServer.");
            }
            else { Debug.LogWarning("[RoundManager] SpellBarManager instance not found, cannot reset spell bars."); }

            // Reset Position
            if (player1SpawnPoint != null && player1NetObj != null && player1NetObj.TryGetComponent<NetworkTransform>(out var p1Transform)) { 
                Vector3 currentScaleP1 = p1Transform.transform.localScale; // Get current scale
                p1Transform.Teleport(player1SpawnPoint.position, player1SpawnPoint.rotation, currentScaleP1); // Use current scale
            }
            else { Debug.LogWarning("[RoundManager] Could not reset Player 1 position (missing spawn point or NetworkTransform?)."); }

            if (player2SpawnPoint != null && player2NetObj != null && player2NetObj.TryGetComponent<NetworkTransform>(out var p2Transform)) { 
                Vector3 currentScaleP2 = p2Transform.transform.localScale; // Get current scale
                p2Transform.Teleport(player2SpawnPoint.position, player2SpawnPoint.rotation, currentScaleP2); // Use current scale
            }
            else { Debug.LogWarning("[RoundManager] Could not reset Player 2 position (missing spawn point or NetworkTransform?)."); }

            // --- 4. Wait --- 
            Debug.Log($"[RoundManager] Waiting for {roundResetDelay} seconds...");
            yield return new WaitForSeconds(roundResetDelay);

            // --- 5. Resume Systems ---
            Debug.Log("[RoundManager] Resuming spawners.");
            spiritSpawnerInstance?.SetSpawningEnabledServer(true);
            foreach(var fs in fairySpawnerInstances) { fs?.SetSpawningEnabledServer(true); }
            // TODO: Re-enable player input? (Ensure input is disabled when match ends / round resets)

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

        // --- Helper to get NetworkObject --- 
        private NetworkObject GetPlayerNetworkObject(PlayerRole role)
        {
            PlayerData? playerData = PlayerDataManager.Instance?.GetPlayerDataByRole(role);
            if (!playerData.HasValue)
            { 
                return null; // PlayerData not found for role
            }
            // Ensure NetworkManager and SpawnManager are available
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
            {
                Debug.LogError("[RoundManager] NetworkManager or SpawnManager not ready!");
                return null;
            }
            return NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
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

        /// <summary>
        /// [Server Only] Handles client disconnections.
        /// If a client disconnects during the post-match/rematch phase, force remaining clients back to menu.
        /// </summary>
        private void HandleClientDisconnectServer(ulong disconnectedClientId)
        {
            if (!IsServer) return;

            Debug.Log($"[RoundManager] Client disconnected: {disconnectedClientId}");

            // Check if the disconnection happened after the match ended but before a rematch started
            if (matchHasEnded || player1WantsRematch || player2WantsRematch)
            {
                Debug.Log("[RoundManager] Client disconnected during post-match phase. Resetting rematch state and returning remaining players to menu.");
                
                // Reset rematch state immediately
                player1WantsRematch = false;
                player2WantsRematch = false;

                // Stop any ongoing round reset if applicable (though unlikely here)
                StopAllCoroutines(); 

                // Find remaining clients (excluding the one that just disconnected)
                List<ulong> remainingClientIds = new List<ulong>();
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.ClientId != disconnectedClientId)
                    {
                        remainingClientIds.Add(client.ClientId);
                    }
                }

                if (remainingClientIds.Count > 0)
                {
                    ClientRpcParams remainingClientsParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {   
                            TargetClientIds = remainingClientIds.ToArray()
                        }
                    };
                    ForceReturnToMenuClientRpc(remainingClientsParams);
                }
                else
                {
                    Debug.Log("[RoundManager] Last client disconnected during post-match. Shutting down server and returning to menu.");
                    
                    // Store NetworkManager instance before potential nullification
                    var networkManager = NetworkManager.Singleton;

                    // Reset rematch state first
                    player1WantsRematch = false;
                    player2WantsRematch = false;
                    // DO NOT reset matchHasEnded here
                    
                    // Shut down the server/host instance.
                    if (networkManager != null)
                    {
                        Debug.Log("[RoundManager] Initiating NetworkManager Shutdown...");
                        networkManager.Shutdown();
                        Debug.Log("[RoundManager] NetworkManager Shutdown called.");
                    }
                    else
                    {
                        Debug.LogWarning("[RoundManager] NetworkManager was already null before shutdown.");
                    }

                    // --- ADDED: Despawn session-specific PlayerDataManager ---
                    PlayerDataManager pdmInstance = FindObjectOfType<PlayerDataManager>();
                    if (pdmInstance != null && pdmInstance.TryGetComponent<NetworkObject>(out var pdmNetObj))
                    {
                        if (pdmNetObj.IsSpawned)
                        {
                             Debug.Log("[RoundManager] Despawning PlayerDataManager instance...");
                             pdmNetObj.Despawn(true); // true = destroy object after despawn
                        }
                        else { Debug.LogWarning("[RoundManager] Found PlayerDataManager but it wasn't spawned?"); }
                    }
                    else { Debug.LogWarning("[RoundManager] Could not find PlayerDataManager instance to despawn."); }
                    // --------------------------------------------------------

                    // --- ADD BACK: Load the main menu scene on the server instance. ---
                    Debug.Log("[RoundManager] Loading MainMenuScene...");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
                    // --------------------------------------------------------------------
                }
            }
            else
            {
                Debug.Log("[RoundManager] Client disconnected during active gameplay or before match end. Normal disconnect process handled by NetworkManager.");
                // Handle mid-game disconnects if necessary (e.g., grant win to opponent)
                // For now, we assume NetworkManager handles the basics.
            }
        }

        /// <summary>
        /// [ClientRpc] Forces the targeted clients to shut down networking and return to the main menu.
        /// Used when a player quits during the post-match phase.
        /// </summary>
        [ClientRpc]
        private void ForceReturnToMenuClientRpc(ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"[RoundManager - Client {NetworkManager.Singleton.LocalClientId}] Received ForceReturnToMenu RPC.");

            // Use the same logic as the Quit button
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // TODO: Ensure this scene name is correct and in Build Settings!
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene"); 
        }
    }
} 