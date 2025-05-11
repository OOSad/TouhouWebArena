using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;

namespace TouhouWebArena.DevTools
{
    public class DebugMenuController : NetworkBehaviour
    {
        [SerializeField] private GameObject debugPanel;

        private bool isPanelVisible = false;

        // --- Spawner Toggles ---
        private SpiritSpawner spiritSpawnerInstance;
        private List<FairySpawner> fairySpawnerInstances = new List<FairySpawner>();

        void Start()
        {
            // Ensure the panel is hidden initially on clients, and shown/hidden based on F11 on server
            if (debugPanel != null)
            {
                isPanelVisible = NetworkManager.Singleton.IsServer && debugPanel.activeSelf; // Initial state if server
                debugPanel.SetActive(isPanelVisible); 
            }
            else
            {
                UnityEngine.Debug.LogError("DebugPanel is not assigned in the DebugMenuController inspector!");
            }

            // Find spawners on the server
            if (NetworkManager.Singleton.IsServer)
            {
                FindSpawners();

                // TEMP TEST: Call ClientRpc directly from server's Start
                // UnityEngine.Debug.Log($"[Server DebugMenuController.Start()] Attempting to call TogglePlayerHitboxClientRpc for Player1, Enabled: False");
                // TogglePlayerHitboxClientRpc(PlayerRole.Player1, false); // Test P1 off // REMOVED - Was for testing
            }
        }

        // Find spawner instances (server only)
        private void FindSpawners()
        {
            // Find the single SpiritSpawner instance in the scene
            spiritSpawnerInstance = FindFirstObjectByType<SpiritSpawner>();
            if (spiritSpawnerInstance == null)
            {
                 UnityEngine.Debug.LogWarning("DebugMenuController could not find the SpiritSpawner instance.");
            }

            // Find all active FairySpawner instances in the scene
            fairySpawnerInstances.Clear();
            fairySpawnerInstances.AddRange(FindObjectsByType<FairySpawner>(FindObjectsSortMode.None));
            if (fairySpawnerInstances.Count == 0)
            {
                UnityEngine.Debug.LogWarning("DebugMenuController could not find any FairySpawner instances.");
            }
            // Optional: Add delay or subscribe to an event if spawners are created later
        }

        public void ToggleSpiritSpawner(bool value)
        {
             if (!NetworkManager.Singleton.IsServer) return;
            
            // Enable/Disable the Spawner script itself
            if (spiritSpawnerInstance != null)
            {
                spiritSpawnerInstance.SetSpawningEnabledServer(value);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Cannot toggle Spirit Spawner: Instance not found.");
                // Attempt to find it again? 
                FindSpawners(); 
                spiritSpawnerInstance?.SetSpawningEnabledServer(value);
            }
        }

        public void ToggleFairySpawners(bool value)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (fairySpawnerInstances.Count == 0)
            {
                 UnityEngine.Debug.LogWarning("Cannot toggle Fairy Spawners: No instances found.");
                 // Attempt to find them again?
                 FindSpawners();
            }

            foreach (FairySpawner spawner in fairySpawnerInstances)
            {
                if (spawner != null) // Check in case an instance was destroyed
                {
                     spawner.SetSpawningEnabledServer(value);
                }
            }
        }

        void Update()
        {
            // ADDED DIAGNOSTIC LOG - Appears on server and ALL clients if script is active
            // if (Time.frameCount % 300 == 0) // Log every 300 frames (approx every 5 seconds) to avoid spam
            // {
            //     UnityEngine.Debug.Log($"[DebugMenuController UPDATE on {gameObject.name}] IsServer: {NetworkManager.Singleton?.IsServer}, IsClient: {NetworkManager.Singleton?.IsClient}, LocalClientId: {NetworkManager.Singleton?.LocalClientId}");
            // }

            if (debugPanel == null) return;

            // Only server can toggle the debug panel visibility
            if (NetworkManager.Singleton.IsServer)
            {
                if (Input.GetKeyDown(KeyCode.F11))
                {
                    isPanelVisible = !isPanelVisible;
                    debugPanel.SetActive(isPanelVisible);
                    UnityEngine.Debug.Log($"[Server DebugMenuController UPDATE on {gameObject.name}] Toggled panel visibility to {isPanelVisible}");
                    // Potentially add cursor locking/unlocking logic here later
                }
            }
            // Clients do not process F11 or other server-side logic
        }

        // --- Placeholder Methods for UI Elements ---

        public void TogglePlayer1Hitbox(bool value)
        {
            UnityEngine.Debug.Log($"Debug Toggle Player 1 Hitbox: {value}");
            TogglePlayerHitbox(PlayerRole.Player1, value);
        }

        public void TogglePlayer2Hitbox(bool value)
        {
            UnityEngine.Debug.Log($"Debug Toggle Player 2 Hitbox: {value}");
            TogglePlayerHitbox(PlayerRole.Player2, value);
        }

        private void TogglePlayerHitbox(PlayerRole role, bool enabled)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            PlayerData? playerData = PlayerDataManager.Instance?.GetPlayerDataByRole(role);
            if (!playerData.HasValue)
            {
                UnityEngine.Debug.LogWarning($"Could not find PlayerData for role {role} to toggle hitbox.");
                return;
            }

            NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
            if (playerNetworkObject == null)
            {
                UnityEngine.Debug.LogWarning($"Could not find NetworkObject for player {playerData.Value.ClientId} (Role: {role}) to toggle hitbox.");
                return;
            }

            GameObject playerObject = playerNetworkObject.gameObject;
            Transform hitboxTransform = playerObject.transform.Find("Hitbox"); // Find the child object named "Hitbox"

            if (hitboxTransform != null)
            {
                // 1. Server sets its local state
                hitboxTransform.gameObject.SetActive(enabled);
                UnityEngine.Debug.Log($"[Server] Set Player {role} Hitbox GameObject active state to: {enabled}");

                // 2. Server tells all clients to do the same
                TogglePlayerHitboxClientRpc(role, enabled);
            }
            else
            {
                UnityEngine.Debug.LogError($"[Server] Could not find 'Hitbox' child GameObject on player {playerData.Value.ClientId} (Role: {role}).");
            }
        }

        [ClientRpc]
        private void TogglePlayerHitboxClientRpc(PlayerRole role, bool enabled)
        {
            UnityEngine.Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Entered TogglePlayerHitboxClientRpc for Role: {role}, Enabled: {enabled}. IsClient: {NetworkManager.Singleton.IsClient}");

            // This code runs on all clients
            // UnityEngine.Debug.Log($"[Client] Received TogglePlayerHitboxClientRpc for Role: {role}, Enabled: {enabled}"); // LOG A - Redundant with above

            // Find the correct player object locally based on role
            ClientAuthMovement targetPlayerComponent = null;
            var playerObjects = FindObjectsByType<ClientAuthMovement>(FindObjectsSortMode.None);
            foreach (var pNet in playerObjects)
            {
                PlayerData? pd = PlayerDataManager.Instance?.GetPlayerData(pNet.OwnerClientId);
                if (pd != null && pd.Value.Role == role)
                {
                    targetPlayerComponent = pNet;
                    break;
                }
            }

            if (targetPlayerComponent != null)
            {
                Transform hitboxGameObjectTransform = targetPlayerComponent.transform.Find("Hitbox");
                if (hitboxGameObjectTransform != null)
                {
                    // PlayerHitbox playerHitboxScript = hitboxGameObjectTransform.GetComponent<PlayerHitbox>();
                    // if (playerHitboxScript != null)
                    // {
                    //     playerHitboxScript.enabled = enabled;
                    //     UnityEngine.Debug.Log($"[Client] Set Player {role} (Owner: {targetPlayerComponent.OwnerClientId}) PlayerHitbox SCRIPT enabled state to: {enabled}");
                    // }
                    // else
                    // {
                    //     UnityEngine.Debug.LogWarning($"[Client] Could not find 'PlayerHitbox' script on 'Hitbox' child for player with role {role}.");
                    // }
                    hitboxGameObjectTransform.gameObject.SetActive(enabled);
                    UnityEngine.Debug.Log($"[Client] Set Player {role} (Owner: {targetPlayerComponent.OwnerClientId}) Hitbox GameObject active state to: {enabled}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[Client] Could not find 'Hitbox' child GameObject on player with role {role}.");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[Client] Could not find player with role {role} to toggle hitbox.");
            }
        }

        public void InstakillPlayer1()
        {
            UnityEngine.Debug.Log("Debug Instakill Player 1");
            InstakillPlayer(PlayerRole.Player1);
        }

        public void InstakillPlayer2()
        {
            UnityEngine.Debug.Log("Debug Instakill Player 2");
            InstakillPlayer(PlayerRole.Player2);
        }

        private void InstakillPlayer(PlayerRole role)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            PlayerData? playerData = PlayerDataManager.Instance?.GetPlayerDataByRole(role);
            if (!playerData.HasValue)
            {
                UnityEngine.Debug.LogWarning($"Could not find PlayerData for role {role} to instakill.");
                return;
            }

            NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
            if (playerNetworkObject == null)
            {
                UnityEngine.Debug.LogWarning($"Could not find NetworkObject for player {playerData.Value.ClientId} (Role: {role}) to instakill.");
                return;
            }

            PlayerHealth playerHealth = playerNetworkObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                int currentHp = playerHealth.CurrentHealth.Value; // Get current health
                if (currentHp > 0)
                {
                    playerHealth.TakeDamage(currentHp); // Deal exact lethal damage
                    UnityEngine.Debug.Log($"Applied {currentHp} damage to instakill Player {role}.");
                }
                else
                {
                     UnityEngine.Debug.Log($"Player {role} already has 0 or less health.");
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"Could not find PlayerHealth component on player {playerData.Value.ClientId} (Role: {role}).");
            }
        }

        // --- Add methods for other controls later ---

        // --- Player HP Lock ---
        public void ToggleLockPlayer1Hp(bool value)
        {
             SetPlayerHpLockState(PlayerRole.Player1, value);
        }

        public void ToggleLockPlayer2Hp(bool value)
        {
             SetPlayerHpLockState(PlayerRole.Player2, value);
        }

        private void SetPlayerHpLockState(PlayerRole role, bool locked)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            PlayerHealth playerHealth = GetPlayerHealthComponent(role);
            if (playerHealth != null)
            {
                playerHealth.SetHpLockStateServer(locked);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Could not find PlayerHealth for role {role} to set lock state.");
            }
        }

        // --- Player HP Set ---
        public void SetPlayer1Hp(string value)
        {
            SetPlayerHp(PlayerRole.Player1, value);
        }

        public void SetPlayer2Hp(string value)
        {
            SetPlayerHp(PlayerRole.Player2, value);
        }

        private void SetPlayerHp(PlayerRole role, string valueString)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (int.TryParse(valueString, out int targetHp))
            {
                PlayerHealth playerHealth = GetPlayerHealthComponent(role);
                if (playerHealth != null)
                {
                    playerHealth.SetHealthDirectlyServer(targetHp);
                }
                 else
                {
                    UnityEngine.Debug.LogWarning($"Could not find PlayerHealth for role {role} to set HP value.");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Invalid HP value entered: {valueString}");
            }
        }

        // --- AI Toggle ---
        public void TogglePlayer1AI(bool value)
        {
            SetPlayerAIState(PlayerRole.Player1, value);
        }

        public void TogglePlayer2AI(bool value)
        {
            SetPlayerAIState(PlayerRole.Player2, value);
        }

        private void SetPlayerAIState(PlayerRole role, bool enabled)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            NetworkObject playerNetworkObject = GetPlayerNetworkObject(role);
            if (playerNetworkObject != null)
            {
                PlayerAIController aiController = playerNetworkObject.GetComponent<PlayerAIController>();
                if (aiController != null)
                {
                    aiController.SetAIEnabledServer(enabled);
                }
                else
                {
                     UnityEngine.Debug.LogWarning($"Could not find PlayerAIController on player for role {role}.");
                }
            }
            else
            {
                 UnityEngine.Debug.LogWarning($"Could not find NetworkObject for role {role} to toggle AI.");
            }
        }

        // --- Helper to get PlayerHealth ---
        private PlayerHealth GetPlayerHealthComponent(PlayerRole role)
        {
            PlayerData? playerData = PlayerDataManager.Instance?.GetPlayerDataByRole(role);
            if (!playerData.HasValue)
            {
                return null; // PlayerData not found for role
            }

            NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
            if (playerNetworkObject == null)
            {
                return null; // NetworkObject not found for client
            }

            return playerNetworkObject.GetComponent<PlayerHealth>(); // Return component or null if not found
        }

        // --- Helper to get NetworkObject (Refactored from others) ---
        private NetworkObject GetPlayerNetworkObject(PlayerRole role)
        {
             PlayerData? playerData = PlayerDataManager.Instance?.GetPlayerDataByRole(role);
            if (!playerData.HasValue)
            { 
                return null; // PlayerData not found for role
            }
            return NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
        }

        // --- Max Spell Bar --- 
        public void GiveMaxSpellBars()
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (SpellBarManager.Instance != null)
            {
                UnityEngine.Debug.Log("Debug: Setting Player 1 and Player 2 spell bars to max.");
                SpellBarManager.Instance.SetPlayerChargeToMaxServer(PlayerRole.Player1);
                SpellBarManager.Instance.SetPlayerChargeToMaxServer(PlayerRole.Player2);
            }
            else
            {
                UnityEngine.Debug.LogError("SpellBarManager instance not found!");
            }
        }
    }
} 