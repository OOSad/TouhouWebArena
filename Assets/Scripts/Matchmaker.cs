using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Collections;
using System;
using System.Collections;
using System.Threading.Tasks;

public class Matchmaker : NetworkBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private Button queueButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI queueText;
    [SerializeField] private float sceneTransitionDelay = 1.0f;
    
    // Networked list to store queued players
    private NetworkList<PlayerInfo> queuedPlayers;
    
    // Currently connected client info
    private string localPlayerName = "";
    private ulong localClientId;
    private bool isInQueue = false;
    
    private void Awake()
    {
        // Initialize the networked list
        queuedPlayers = new NetworkList<PlayerInfo>();
    }
    
    private void OnEnable()
    {
        // Listen for queue changes
        queuedPlayers.OnListChanged += HandleQueueChanged;
    }
    
    private void OnDisable()
    {
        // Remove listener
        queuedPlayers.OnListChanged -= HandleQueueChanged;
    }
    
    private void Start()
    {
        // Set up button listeners
        if (queueButton != null)
            queueButton.onClick.AddListener(OnQueueButtonClicked);
            
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
            
        // Disable queue functionality until connected
        SetQueueButtonsInteractable(false);
        
        // Set up network connection callbacks
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }
    
    public override void OnDestroy()
    {
        // Clean up callbacks
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        // Dispose the NetworkList to prevent memory leaks
        if (queuedPlayers != null)
        {
            queuedPlayers.Dispose();
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        // Store local client info
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            localClientId = clientId;
            SetQueueButtonsInteractable(true);
            
            // Clear the queue text for newly connected clients
            UpdateQueueText();
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer && queuedPlayers != null)
        {
            // Remove disconnected players from queue
            for (int i = 0; i < queuedPlayers.Count; i++)
            {
                if (queuedPlayers[i].ClientId == clientId)
                {
                    queuedPlayers.RemoveAt(i);
                    break;
                }
            }
        }
        
        // If we're disconnecting, disable queue buttons
        if (clientId == localClientId)
        {
            SetQueueButtonsInteractable(false);
            isInQueue = false;
        }
    }
    
    private void SetQueueButtonsInteractable(bool interactable)
    {
        if (queueButton != null)
            queueButton.interactable = interactable;
            
        if (cancelButton != null)
            cancelButton.interactable = false; // Initially disabled until in queue
    }
    
    private void OnQueueButtonClicked()
    {
        // Get username from input field, use "Anonymous" if empty
        string username = "Anonymous";
        
        if (usernameInput != null && !string.IsNullOrWhiteSpace(usernameInput.text))
        {
            username = usernameInput.text;
        }
        
        // Add a random identifier between 1000-9999
        int uniqueId = UnityEngine.Random.Range(1000, 10000);
        username = $"{username}#{uniqueId}";
        
        localPlayerName = username;
        JoinQueueServerRpc(localPlayerName, NetworkManager.Singleton.LocalClientId);
    }
    
    private void OnCancelButtonClicked()
    {
        if (isInQueue)
        {
            LeaveQueueServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void JoinQueueServerRpc(string playerName, ulong clientId)
    {
        if (!IsServer) return;
        
        // Register player with PlayerDataManager
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.RegisterPlayer(clientId, playerName);
        }
        
        // Check if player is already in queue
        foreach (var player in queuedPlayers)
        {
            if (player.ClientId == clientId)
            {
                // Already in queue
                return;
            }
        }
        
        // Add player to queue
        queuedPlayers.Add(new PlayerInfo { 
            PlayerName = new FixedString32Bytes(playerName), 
            ClientId = clientId 
        });
        
        // Update clients about this player's queue status
        SetPlayerQueueStatusClientRpc(clientId, true);
        
        // Check if we have two players ready
        if (queuedPlayers.Count >= 2)
        {
            ulong player1ClientId = queuedPlayers[0].ClientId;
            ulong player2ClientId = queuedPlayers[1].ClientId;
            
            // Assign roles before starting the match
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.AssignPlayerRole(player1ClientId, PlayerRole.Player1);
                PlayerDataManager.Instance.AssignPlayerRole(player2ClientId, PlayerRole.Player2);
            }
            else
            {
                Debug.LogError("[Matchmaker] PlayerDataManager Instance is null! Cannot assign roles.");
                // Handle this error appropriately, perhaps by not starting the match
                return; 
            }
            
            // Start match with the assigned players
            StartMatchClientRpc(player1ClientId, player2ClientId);
            
            // Start delayed scene load for server as well
            if (IsServer)
            {
                StartCoroutine(LoadGameplaySceneDelayed());
            }
            
            // Remove matched players from queue
            queuedPlayers.RemoveAt(0);
            queuedPlayers.RemoveAt(0);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void LeaveQueueServerRpc(ulong clientId)
    {
        if (!IsServer) return;
        
        // Remove player from queue
        for (int i = 0; i < queuedPlayers.Count; i++)
        {
            if (queuedPlayers[i].ClientId == clientId)
            {
                queuedPlayers.RemoveAt(i);
                break;
            }
        }
        
        // Update client about their queue status
        SetPlayerQueueStatusClientRpc(clientId, false);
    }
    
    [ClientRpc]
    private void SetPlayerQueueStatusClientRpc(ulong clientId, bool inQueue)
    {
        // Update local queue status if this is for us
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            isInQueue = inQueue;
            
            // Enable/disable buttons based on queue status
            if (queueButton != null)
                queueButton.interactable = !inQueue;
                
            if (cancelButton != null)
                cancelButton.interactable = inQueue;
        }
    }
    
    [ClientRpc]
    private void StartMatchClientRpc(ulong player1Id, ulong player2Id)
    {
        // Check if this client is one of the players in the match
        if (NetworkManager.Singleton.LocalClientId == player1Id || 
            NetworkManager.Singleton.LocalClientId == player2Id)
        {
            Debug.Log("Match is starting for client: " + NetworkManager.Singleton.LocalClientId);
            
            // Start coroutine for delayed scene loading
            StartCoroutine(DelayedSceneLoad(1.0f));
            
            // Update queue text to show match is starting
            if (queueText != null)
            {
                queueText.text = "Match found! Loading game...";
            }
        }
    }
    
    private IEnumerator DelayedSceneLoad(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Note: We don't need to load the scene on clients now, as the server will handle it
        // and network scene manager will automatically sync to clients
        if (!IsServer)
        {
            Debug.Log("Client waiting for server to change scene...");
        }
    }
    
    private IEnumerator LoadGameplaySceneDelayed()
    {
        Debug.Log("Server preparing to load character select scene...");
        
        // Wait for the delay
        yield return new WaitForSeconds(sceneTransitionDelay);
        
        // Load the character select scene (this will sync to all connected clients)
        Debug.Log("Server loading character select scene...");
        NetworkManager.Singleton.SceneManager.LoadScene("CharacterSelectScene", LoadSceneMode.Single);
    }
    
    private void HandleQueueChanged(NetworkListEvent<PlayerInfo> changeEvent)
    {
        // Update UI when queue changes
        UpdateQueueText();
    }
    
    private void UpdateQueueText()
    {
        if (queueText == null) return;
        
        if (queuedPlayers.Count == 0)
        {
            queueText.text = "No players in queue";
            return;
        }
        
        // Build queue display text
        string queueDisplay = "Queued players:\n";
        foreach (var player in queuedPlayers)
        {
            queueDisplay += player.PlayerName.ToString() + "\n";
        }
        
        queueText.text = queueDisplay;
    }

    private async Task LoadCharacterSelectSceneAsync()
    {
        if (!IsServer) return;

        // Debug.Log("Server preparing to load character select scene...");

        // Ensure all clients are ready before loading
        await Task.Delay(1000); // Give a moment for clients to connect/sync

        // Debug.Log("Server loading character select scene...");
        NetworkManager.SceneManager.LoadScene("CharacterSelectScene", LoadSceneMode.Single);
    }
}

// Custom struct to hold player info in the networked list
struct PlayerInfo : INetworkSerializable, IEquatable<PlayerInfo>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
    }
    
    public bool Equals(PlayerInfo other)
    {
        return ClientId == other.ClientId && PlayerName.Equals(other.PlayerName);
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerInfo info && Equals(info);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClientId, PlayerName);
    }
} 