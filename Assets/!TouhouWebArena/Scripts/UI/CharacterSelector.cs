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
        base.OnNetworkSpawn();

        // Initialization logic MOVED BACK to OnNetworkSpawn AGAIN.
        // This ensures it runs on clients after the scene and NetworkObjects are loaded.
        
        Debug.Log("[CharacterSelector] OnNetworkSpawn: Finding PlayerDataManager...");
        // Directly find the spawned instance in the scene.
        // REVERTED: Use Singleton Instance for persistent manager
        // playerDataManager = FindObjectOfType<PlayerDataManager>();
        playerDataManager = PlayerDataManager.Instance;

        if (playerDataManager == null)
        {
            Debug.LogError("[CharacterSelector] OnNetworkSpawn: Could not find PlayerDataManager instance via Singleton! Disabling.");
            this.enabled = false; 
            return;
        }

        Debug.Log("[CharacterSelector] OnNetworkSpawn: PlayerDataManager instance found via Singleton. Completing initialization.");

        // Setup button listeners
        SetupButtonListeners();

        // Subscribe to PlayerDataManager updates
        Debug.Log("[CharacterSelector] OnNetworkSpawn: Subscribing to OnPlayerDataUpdated.");
        playerDataManager.OnPlayerDataUpdated += HandlePlayerDataUpdated;
        
        // Trigger an initial update now that we know the manager exists
        // This is important for clients joining/loading the scene.
        HandlePlayerDataUpdated(); 
    }

    private void SetupButtonListeners()
    {
        foreach (var mapping in characterButtons)
        {
            if (mapping.button != null && !string.IsNullOrEmpty(mapping.characterName))
            {
                string name = mapping.characterName;
                mapping.button.onClick.AddListener(() => OnCharacterButtonClicked(name));
            }
        }
    }

    private void OnCharacterButtonClicked(string characterName)
    {
        Debug.Log($"[CharacterSelector] Button clicked for character: {characterName}");
        RequestSetCharacterServerRpc(characterName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSetCharacterServerRpc(string characterName, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        // Use the locally cached reference
        if (playerDataManager != null)
        {
            playerDataManager.SetPlayerCharacter(clientId, characterName);
            // Check if both players are ready AFTER updating
            if (playerDataManager.AreBothPlayersReady())
            {
                StartCoroutine(DelayedSceneLoad());
            }
        }
    }

    private void HandlePlayerDataUpdated()
    {
        Debug.Log("[CharacterSelector] HandlePlayerDataUpdated called.");
        // Use the locally cached reference
        if (playerDataManager == null)
        {
            Debug.LogError("[CharacterSelector] HandlePlayerDataUpdated: PlayerDataManager reference is NULL!");
            return;
        }

        if (player1SelectionText == null || player2SelectionText == null)
        {
            Debug.LogError("[CharacterSelector] HandlePlayerDataUpdated: UI Text references are NULL!");
            return;
        }
        Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: Updating UI text...");

        PlayerData? p1Data = playerDataManager.GetPlayer1Data();
        if (p1Data.HasValue)
        {
            string p1Character = p1Data.Value.SelectedCharacter.ToString();
            string p1Name = p1Data.Value.PlayerName.ToString();
            string p1Text = string.IsNullOrEmpty(p1Character) ? "Choosing..." : p1Character;
            player1SelectionText.text = $"Player 1 ({p1Name}):\n{p1Text}";
        }
        else
        {
            player1SelectionText.text = "Player 1: Waiting...";
        }

        PlayerData? p2Data = playerDataManager.GetPlayer2Data();
         if (p2Data.HasValue)
        {
            string p2Character = p2Data.Value.SelectedCharacter.ToString();
            string p2Name = p2Data.Value.PlayerName.ToString();
            string p2Text = string.IsNullOrEmpty(p2Character) ? "Choosing..." : p2Character;
            player2SelectionText.text = $"Player 2 ({p2Name}):\n{p2Text}";
        }
        else
        {
            player2SelectionText.text = "Player 2: Waiting...";
        }
    }

    private IEnumerator DelayedSceneLoad()
    {
        if (!IsServer) yield break;
        yield return new WaitForSeconds(0.1f); 
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
             NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }
    }

    public override void OnNetworkDespawn()
    {
        foreach (var mapping in characterButtons)
        {
            if (mapping.button != null)
            {
                mapping.button.onClick.RemoveAllListeners();
            }
        }

        if (playerDataManager != null)
        {
            playerDataManager.OnPlayerDataUpdated -= HandlePlayerDataUpdated;
        }

        base.OnNetworkDespawn();
    }
} 