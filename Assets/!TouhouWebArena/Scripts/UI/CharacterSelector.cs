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

        // --- Fallback if Instance is null (Timing issue during scene load?) ---
        // REVERTED: Fallback no longer needed with persistent manager + execution order.
        /* 
        if (playerDataManager == null)
        {
            Debug.LogWarning("[CharacterSelector] PlayerDataManager.Instance was NULL on NetworkSpawn. Attempting FindObjectOfType...");
            playerDataManager = FindObjectOfType<PlayerDataManager>();
        }
        */
        // ------------------------------------------------------------------

        if (playerDataManager == null)
        {
            Debug.LogError("[CharacterSelector] PlayerDataManager could NOT be found via Instance!");
            this.enabled = false; // Disable script if manager is missing
            return;
        }
        Debug.Log("[CharacterSelector] PlayerDataManager instance acquired.");

        // Setup button listeners
        SetupButtonListeners();

        // Initial UI update is now triggered by the event subscription
        // UpdateUI();

        // Subscribe to PlayerDataManager updates
        if (PlayerDataManager.Instance != null)
        {
            Debug.Log("[CharacterSelector] Subscribing to OnPlayerDataUpdated.");
            PlayerDataManager.Instance.OnPlayerDataUpdated += HandlePlayerDataUpdated;
            // Trigger an initial update in case the event fired before we subscribed
            // HandlePlayerDataUpdated(); // REMOVED: Let the event handle the initial update.
        }
        else
        {
            Debug.LogError("[CharacterSelector] PlayerDataManager.Instance was NULL when trying to subscribe to event!");
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
                
            }
        }
    }

    private void OnCharacterButtonClicked(string characterName)
    {
        Debug.Log($"[CharacterSelector] Button clicked for character: {characterName}");
        // Send the selection to the server
        RequestSetCharacterServerRpc(characterName);
    }

    // Method called by UI Buttons is removed as listeners are now added programmatically
    // public void SelectCharacter(string characterName) { ... }

    [ServerRpc(RequireOwnership = false)] // Allow any client to call this RPC
    private void RequestSetCharacterServerRpc(string characterName, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        

        if (playerDataManager != null)
        {
            playerDataManager.SetPlayerCharacter(clientId, characterName);

            // Tell all clients to refresh their UI -- THIS IS NO LONGER NEEDED
            // UpdateClientUIsClientRpc();

            // Check if both players are ready AFTER updating
            if (playerDataManager.AreBothPlayersReady())
            {
                // Don't load immediately, start the delayed load coroutine
                StartCoroutine(DelayedSceneLoad());
            }
        }
        else
        {
             
        }
    }

    // Renamed UpdateUI to HandlePlayerDataUpdated to clarify it's an event handler
    private void HandlePlayerDataUpdated()
    {
        Debug.Log("[CharacterSelector] HandlePlayerDataUpdated called.");
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[CharacterSelector] HandlePlayerDataUpdated: PlayerDataManager.Instance is NULL!");
            return;
        }

        if (player1SelectionText == null || player2SelectionText == null)
        {
            Debug.LogError("[CharacterSelector] HandlePlayerDataUpdated: UI Text references are NULL!");
            return;
        }
        Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: Updating UI text...");

        // Get data for Player 1 (usually index 0)
        // Use top-level PlayerData
        PlayerData? p1Data = playerDataManager.GetPlayer1Data();
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
        // Use top-level PlayerData
        PlayerData? p2Data = playerDataManager.GetPlayer2Data();
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

        
        // Ensure NetworkManager is still valid before loading
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
             NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }
        else
        {
            
        }
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