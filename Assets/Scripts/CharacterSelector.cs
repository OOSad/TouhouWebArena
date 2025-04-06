using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;
using Unity.Collections;
using System.Collections.Generic;

public class CharacterSelector : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button reimuButton;
    [SerializeField] private Button marisaButton;
    [SerializeField] private TextMeshProUGUI player1SelectionText;
    [SerializeField] private TextMeshProUGUI player2SelectionText;
    
    [Header("Configuration")]
    [SerializeField] private float sceneTransitionDelay = 1.5f;
    [SerializeField] private string gameplaySceneName = "GameplayScene";
    
    // Network variables to track player selections
    private NetworkVariable<FixedString32Bytes> player1Character = new NetworkVariable<FixedString32Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    private NetworkVariable<FixedString32Bytes> player2Character = new NetworkVariable<FixedString32Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // Network variable to track player assignments
    private NetworkVariable<ulong> player1ClientId = new NetworkVariable<ulong>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    private NetworkVariable<ulong> player2ClientId = new NetworkVariable<ulong>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // Keep track of local player assignment
    private bool isPlayer1;
    private bool hasSelectedCharacter = false;
    private string localSelection = "";
    
    // Connected clients list (server-side only)
    private List<ulong> connectedClients = new List<ulong>();
    
    public override void OnNetworkSpawn()
    {
        // If we're the server, assign player roles when clients connect
        if (IsServer)
        {
            // Get currently connected clients
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (!connectedClients.Contains(clientId))
                {
                    connectedClients.Add(clientId);
                }
            }
            
            // Assign roles based on connection order
            AssignPlayerRoles();
            
            // Subscribe to client connected event for late joiners
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        }
        
        // Determine local player role
        DeterminePlayerAssignment();
        
        base.OnNetworkSpawn();
    }
    
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        }
        
        base.OnNetworkDespawn();
    }
    
    private void OnClientConnectedCallback(ulong clientId)
    {
        if (!IsServer) return;
        
        if (!connectedClients.Contains(clientId))
        {
            connectedClients.Add(clientId);
            AssignPlayerRoles();
        }
    }
    
    private void AssignPlayerRoles()
    {
        if (!IsServer || connectedClients.Count == 0) return;
        
        // Assign first client as Player 1
        if (connectedClients.Count >= 1)
        {
            player1ClientId.Value = connectedClients[0];
            Debug.Log($"Server assigned Player 1 to client ID: {player1ClientId.Value}");
        }
        
        // Assign second client as Player 2
        if (connectedClients.Count >= 2)
        {
            player2ClientId.Value = connectedClients[1];
            Debug.Log($"Server assigned Player 2 to client ID: {player2ClientId.Value}");
        }
    }
    
    private void Start()
    {
        // Listen for character selection changes
        player1Character.OnValueChanged += OnPlayer1CharacterChanged;
        player2Character.OnValueChanged += OnPlayer2CharacterChanged;
        
        // Set up button click events
        if (reimuButton != null)
            reimuButton.onClick.AddListener(() => SelectCharacter("Hakurei Reimu"));
            
        if (marisaButton != null)
            marisaButton.onClick.AddListener(() => SelectCharacter("Kirisame Marisa"));
        
        // Initialize UI
        UpdateSelectionTexts();
    }

    public override void OnDestroy()
    {
        // Clean up listeners
        player1Character.OnValueChanged -= OnPlayer1CharacterChanged;
        player2Character.OnValueChanged -= OnPlayer2CharacterChanged;
        
        base.OnDestroy();
    }
    
    private void DeterminePlayerAssignment()
    {
        if (!NetworkManager.Singleton.IsClient)
            return;
        
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        
        // Check if we're Player 1 based on assigned client ID
        isPlayer1 = (localClientId == player1ClientId.Value);
        
        Debug.Log($"Client {localClientId} determined to be Player {(isPlayer1 ? "1" : "2")}");
        Debug.Log($"Player1 ID: {player1ClientId.Value}, Player2 ID: {player2ClientId.Value}");
    }
    
    private void SelectCharacter(string characterName)
    {
        if (hasSelectedCharacter)
            return;
        
        localSelection = characterName;
            
        // Call server RPC to register selection
        SelectCharacterServerRpc(characterName, isPlayer1);
        
        // Disable character buttons after selection
        hasSelectedCharacter = true;
        DisableCharacterButtons();
        
        Debug.Log($"Selected character: {characterName} as Player {(isPlayer1 ? "1" : "2")}");
    }
    
    private void DisableCharacterButtons()
    {
        if (reimuButton != null)
            reimuButton.interactable = false;
            
        if (marisaButton != null)
            marisaButton.interactable = false;
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SelectCharacterServerRpc(string characterName, bool isFirstPlayer)
    {
        if (isFirstPlayer)
        {
            player1Character.Value = new FixedString32Bytes(characterName);
            Debug.Log($"Server received Player 1 selection: {characterName}");
        }
        else
        {
            player2Character.Value = new FixedString32Bytes(characterName);
            Debug.Log($"Server received Player 2 selection: {characterName}");
        }
        
        // Check if both players have selected characters
        if (!string.IsNullOrEmpty(player1Character.Value.ToString()) && 
            !string.IsNullOrEmpty(player2Character.Value.ToString()))
        {
            // Both players have selected, start countdown to next scene
            StartCoroutine(LoadGameplaySceneDelayed());
        }
    }
    
    private IEnumerator LoadGameplaySceneDelayed()
    {
        // Notify all clients that game is about to start
        NotifyGameStartingClientRpc(player1Character.Value.ToString(), player2Character.Value.ToString());
        
        // Wait for the delay
        yield return new WaitForSeconds(sceneTransitionDelay);
        
        // Load the gameplay scene
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
    
    [ClientRpc]
    private void NotifyGameStartingClientRpc(string player1Selection, string player2Selection)
    {
        // Show a message that game is starting
        Debug.Log($"Match starting: {player1Selection} vs {player2Selection}");
    }
    
    private void OnPlayer1CharacterChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        UpdateSelectionTexts();
    }
    
    private void OnPlayer2CharacterChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        UpdateSelectionTexts();
    }
    
    private void UpdateSelectionTexts()
    {
        if (player1SelectionText != null)
        {
            string p1Selection = player1Character.Value.ToString();
            player1SelectionText.text = string.IsNullOrEmpty(p1Selection) ? "Waiting..." : p1Selection;
        }
        
        if (player2SelectionText != null)
        {
            string p2Selection = player2Character.Value.ToString();
            player2SelectionText.text = string.IsNullOrEmpty(p2Selection) ? "Waiting..." : p2Selection;
        }
    }
} 