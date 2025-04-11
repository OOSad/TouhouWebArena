using UnityEngine;
using Unity.Netcode;
using TMPro; // Use TextMeshPro for UI text
using UnityEngine.SceneManagement; // Needed for LoadSceneMode
using Unity.Collections; // Required for FixedString
using UnityEngine.UI; // Required for Button
using System.Collections.Generic; // Required for List
using System.Collections; // Required for Coroutine

public class CharacterSelector : NetworkBehaviour
{
    [System.Serializable]
    public struct CharacterButtonMapping
    {
        public Button button;
        public string characterName;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text player1SelectionText; // Assign Player 1's text element in Inspector
    [SerializeField] private TMP_Text player2SelectionText; // Assign Player 2's text element in Inspector
    [SerializeField] private List<CharacterButtonMapping> characterButtons; // Assign buttons and their character names in Inspector

    [Header("Scene Management")]
    [SerializeField] private string gameplaySceneName = "GameplayScene"; // Name of the scene to load next

    // --- Private Variables ---
    private PlayerDataManager playerDataManager;

    public override void OnNetworkSpawn()
    {
        // Find the PlayerDataManager instance
        playerDataManager = PlayerDataManager.Instance;
        if (playerDataManager == null)
        {
            Debug.LogError("PlayerDataManager instance not found!");
            this.enabled = false; // Disable script if manager is missing
            return;
        }

        // Setup button listeners
        SetupButtonListeners();

        // Initial UI update is now triggered by the event subscription
        // UpdateUI();

        // Subscribe to PlayerDataManager updates
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerDataUpdated += HandlePlayerDataUpdated;
            // Trigger an initial update in case the event fired before we subscribed
            HandlePlayerDataUpdated();
        }
        else
        {
            Debug.LogError("Cannot subscribe to PlayerDataManager events: Instance is null.");
        }
    }

    private void SetupButtonListeners()
    {
        foreach (var mapping in characterButtons)
        {
            if (mapping.button != null && !string.IsNullOrEmpty(mapping.characterName))
            {
                // Capture the character name for the listener
                string name = mapping.characterName;
                mapping.button.onClick.AddListener(() => OnCharacterButtonClicked(name));
            }
            else
            {
                Debug.LogWarning("Character button mapping is incomplete in the Inspector.");
            }
        }
    }

    private void OnCharacterButtonClicked(string characterName)
    {
        Debug.Log($"Button clicked via listener: SelectCharacter({characterName})");
        // Send the selection to the server
        RequestSetCharacterServerRpc(characterName);
    }

    // Method called by UI Buttons is removed as listeners are now added programmatically
    // public void SelectCharacter(string characterName) { ... }

    [ServerRpc(RequireOwnership = false)] // Allow any client to call this RPC
    private void RequestSetCharacterServerRpc(string characterName, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Server received character selection '{characterName}' from client {clientId}");

        if (playerDataManager != null)
        {
            playerDataManager.SetPlayerCharacter(clientId, characterName);

            // Tell all clients to refresh their UI -- THIS IS NO LONGER NEEDED
            // UpdateClientUIsClientRpc();

            // Check if both players are ready AFTER updating
            if (playerDataManager.AreBothPlayersReady())
            {
                // Don't load immediately, start the delayed load coroutine
                // LoadGameplayScene(); 
                StartCoroutine(DelayedSceneLoad());
            }
        }
        else
        {
             Debug.LogError("PlayerDataManager is null on server during RPC.");
        }
    }

    // Renamed UpdateUI to HandlePlayerDataUpdated to clarify it's an event handler
    private void HandlePlayerDataUpdated()
    {
        // Debug.Log("HandlePlayerDataUpdated called.");
        if (PlayerDataManager.Instance == null)
        {
            // Debug.LogWarning("HandlePlayerDataUpdated called but PlayerDataManager is null.");
            return;
        }

        // Get data for Player 1 (usually index 0)
        PlayerDataManager.PlayerData? p1Data = playerDataManager.GetPlayer1Data();
        if (p1Data.HasValue)
        {
            string p1Character = p1Data.Value.SelectedCharacter.ToString();
            string p1Name = p1Data.Value.PlayerName.ToString();
            string p1Text = string.IsNullOrEmpty(p1Character) ? "Choosing..." : p1Character;
            // Corrected string formatting
            player1SelectionText.text = $"Player 1 ({p1Name}):\n{p1Text}";
        }
        else
        {
            player1SelectionText.text = "Player 1: Waiting...";
        }

        // Get data for Player 2 (usually index 1)
        PlayerDataManager.PlayerData? p2Data = playerDataManager.GetPlayer2Data();
         if (p2Data.HasValue)
        {
            string p2Character = p2Data.Value.SelectedCharacter.ToString();
            string p2Name = p2Data.Value.PlayerName.ToString();
            string p2Text = string.IsNullOrEmpty(p2Character) ? "Choosing..." : p2Character;
            // Corrected string formatting
            player2SelectionText.text = $"Player 2 ({p2Name}):\n{p2Text}";
        }
        else
        {
            player2SelectionText.text = "Player 2: Waiting...";
        }
    }

    private IEnumerator DelayedSceneLoad()
    {
        // Ensure this only runs on the server
        if (!IsServer) yield break;

        // Wait a fraction of a second to allow client UI updates
        yield return new WaitForSeconds(0.1f); 

        Debug.Log("Both players ready. Loading Gameplay Scene after delay...");
        // Ensure NetworkManager is still valid before loading
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
             NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("NetworkManager or SceneManager became invalid before delayed scene load.");
        }
    }

    private void LoadGameplayScene()
    {
         if (!IsServer) return; // Only server should trigger scene change

        Debug.Log("Both players ready. Loading Gameplay Scene...");
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public override void OnNetworkDespawn()
    {
        // Clean up listeners
        foreach (var mapping in characterButtons)
        {
            if (mapping.button != null)
            {
                mapping.button.onClick.RemoveAllListeners(); // Simple cleanup
            }
        }

        // Unsubscribe from PlayerDataManager events
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerDataUpdated -= HandlePlayerDataUpdated;
        }

        base.OnNetworkDespawn();
    }
} 