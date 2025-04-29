using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Collections;
using System;
using System.Collections;

/// <summary>
/// Holds basic information about a player relevant for matchmaking.
/// Implements <see cref="INetworkSerializable"/> for synchronization and <see cref="IEquatable{T}"/>.
/// </summary>
public struct PlayerInfo : INetworkSerializable, IEquatable<PlayerInfo>
{
    /// <summary>The unique network identifier of the player's client.</summary>
    public ulong ClientId;
    /// <summary>The player's chosen display name (fixed size for network serialization).</summary>
    public FixedString32Bytes PlayerName;
    
    /// <summary>
    /// Serializes/deserializes the struct's fields for network transmission.
    /// </summary>
    /// <typeparam name="T">The type of the buffer serializer.</typeparam>
    /// <param name="serializer">The serializer instance used for reading or writing.</param>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
    }
    
    /// <summary>
    /// Determines equality based on ClientId and PlayerName.
    /// </summary>
    /// <param name="other">The other PlayerInfo instance to compare against.</param>
    /// <returns>True if both ClientId and PlayerName match, false otherwise.</returns>
    public bool Equals(PlayerInfo other)
    {
        return ClientId == other.ClientId && PlayerName.Equals(other.PlayerName);
    }

    /// <summary>
    /// Overrides object.Equals for correct comparison.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the object is a PlayerInfo and is equal, false otherwise.</returns>
    public override bool Equals(object obj)
    {
        return obj is PlayerInfo info && Equals(info);
    }

    /// <summary>
    /// Provides a hash code based on ClientId and PlayerName.
    /// </summary>
    /// <returns>A hash code for the struct.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(ClientId, PlayerName);
    }
}

/// <summary>
/// Implements a simple matchmaking system using a synchronized queue.
/// Manages a <see cref="NetworkList{T}"/> of <see cref="PlayerInfo"/>.
/// Clients request to join/leave the queue via ServerRPCs.
/// The server checks for matches when the queue changes.
/// Fires server-side events (<see cref="OnPlayerQueuedServer"/>, <see cref="OnMatchFoundServer"/>)
/// for other systems (like <see cref="PlayerDataManager"/> and <see cref="PlayerSetupManager"/>) to react.
/// Interacts with <see cref="MatchmakerUI"/> to display queue information to clients.
/// </summary>
public class Matchmaker : NetworkBehaviour
{
    [Header("Scene Management")]
    [Tooltip("The name of the character select scene to load when a match is found.")]
    [SerializeField] private string characterSelectSceneName = "CharacterSelectScene"; // Ensure this name is correct

    [Header("UI Handler Reference")]
    /// <summary>
    /// Reference to the MatchmakerUI component that displays queue status and messages.
    /// </summary>
    [Tooltip("Reference to the MatchmakerUI component that displays queue status and messages.")]
    [SerializeField] private MatchmakerUI matchmakerUI;

    /// <summary>The synchronized list of players currently waiting in the matchmaking queue.</summary>
    private NetworkList<PlayerInfo> queuedPlayers;
    /// <summary>Cached ClientId of the local player.</summary>
    private ulong localClientId = ulong.MaxValue;
    /// <summary>Flag to prevent UI updates after a match is found but before scene load.</summary>
    private bool matchInProgress = false;

    /// <summary>Singleton instance of the Matchmaker.</summary>
    public static Matchmaker Instance { get; private set; }

    // --- Server-Side Events ---
    /// <summary>
    /// [Server Only] Event fired when a player is successfully added to the queue via <see cref="JoinQueueServerRpc"/>.
    /// Provides the ClientId and chosen PlayerName.
    /// </summary>
    public event Action<ulong, string> OnPlayerQueuedServer;
    /// <summary>
    /// [Server Only] Event fired by <see cref="CheckForMatch"/> when exactly two players are in the queue.
    /// Provides the ClientIds of the two matched players.
    /// </summary>
    public event Action<ulong, ulong> OnMatchFoundServer;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Sets up the singleton instance, initializes the <see cref="queuedPlayers"/> list,
    /// and attempts to find the <see cref="matchmakerUI"/> if not assigned.
    /// </summary>
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        queuedPlayers = new NetworkList<PlayerInfo>();
        if (matchmakerUI == null) matchmakerUI = FindObjectOfType<MatchmakerUI>();
        if (matchmakerUI == null) Debug.LogError("MatchmakerUI reference not set and not found in scene!", this);
    }

    /// <summary>
    /// Called when the component becomes enabled and active.
    /// Subscribes to the <see cref="queuedPlayers"/> list change event.
    /// </summary>
    private void OnEnable()
    {
        queuedPlayers.OnListChanged += HandleQueueChanged;
    }

    /// <summary>
    /// Called when the component becomes disabled or inactive.
    /// Unsubscribes from the <see cref="queuedPlayers"/> list change event.
    /// </summary>
    private void OnDisable()
    {
        queuedPlayers.OnListChanged -= HandleQueueChanged;
    }

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// Subscribes to NetworkManager connection events and triggers an initial UI update.
    /// </summary>
    private void Start()
    {
        matchInProgress = false;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        else
        {
             Debug.LogError("NetworkManager Singleton not available in Start!", this);
        }
        HandleQueueChanged(default);
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Cleans up the singleton instance, unsubscribes from NetworkManager events,
    /// and disposes the <see cref="queuedPlayers"/> list.
    /// </summary>
    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        if (queuedPlayers != null) queuedPlayers.Dispose();
        base.OnDestroy();
    }

    /// <summary>
    /// Callback handler for <see cref="NetworkManager.OnClientConnectedCallback"/>.
    /// Caches the <see cref="localClientId"/> if the connected client is the local one.
    /// Triggers a queue UI update.
    /// </summary>
    /// <param name="clientId">The ClientId of the client that connected.</param>
    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            localClientId = clientId;
        }
        // Always update queue display on any connection for consistency
        HandleQueueChanged(default);
    }

    /// <summary>
    /// Callback handler for <see cref="NetworkManager.OnClientDisconnectCallback"/>.
    /// If running on the server, removes the disconnected client from the <see cref="queuedPlayers"/> list.
    /// Resets <see cref="localClientId"/> if the disconnected client was the local one.
    /// </summary>
    /// <param name="clientId">The ClientId of the client that disconnected.</param>
    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer && queuedPlayers != null)
        {
            for (int i = queuedPlayers.Count - 1; i >= 0; i--)
            {
                if (queuedPlayers[i].ClientId == clientId)
                {
                    queuedPlayers.RemoveAt(i);
                }
            }
        }
         if (clientId == localClientId) {
            localClientId = ulong.MaxValue;
            // If local client disconnects, also ensure match flag is reset
            matchInProgress = false;
         }
        // Update queue display after potential removal or if local client left
        HandleQueueChanged(default);
    }

    /// <summary>
    /// Client-side method to initiate joining the matchmaking queue.
    /// Takes the player name provided by the UI and invokes the <see cref="JoinQueueServerRpc"/>.
    /// </summary>
    /// <param name="playerName">The name the player wants to use (potentially already made unique by UI).</param>
    public void RequestJoinQueue(string playerName)
    {
         if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
         {
            // Directly use the name provided by the UI.
            JoinQueueServerRpc(playerName, NetworkManager.Singleton.LocalClientId);
         }
         else
         {
              Debug.LogWarning("RequestJoinQueue called but client is not connected.");
         }
    }
     /// <summary>
     /// Client-side method to initiate leaving the matchmaking queue.
     /// Invokes the <see cref="LeaveQueueServerRpc"/>.
     /// </summary>
     public void RequestLeaveQueue()
     {
         if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
         {
             LeaveQueueServerRpc(NetworkManager.Singleton.LocalClientId);
         }
         else
         {
              Debug.LogWarning("RequestLeaveQueue called but client is not connected.");
         }
     }


    /// <summary>
    /// [ServerRpc] Handles a client's request to join the queue.
    /// Checks if the player is already queued. If not, adds them to the <see cref="queuedPlayers"/> list,
    /// invokes the <see cref="OnPlayerQueuedServer"/> event, updates the requesting client's UI via ClientRpc,
    /// and calls <see cref="CheckForMatch"/>.
    /// </summary>
    /// <param name="playerName">The (potentially unique) name provided by the client.</param>
    /// <param name="clientId">The ClientId of the requesting client.</param>
    [ServerRpc(RequireOwnership = false)]
    private void JoinQueueServerRpc(string playerName, ulong clientId)
    {
        if (!IsServer) return;

        // Check if player already in queue
        for (int i = 0; i < queuedPlayers.Count; i++) if (queuedPlayers[i].ClientId == clientId) {
            Debug.LogWarning($"Client {clientId} ({playerName}) tried to join queue but is already in it.");
            return;
        }

        Debug.Log($"Client {clientId} ({playerName}) joining queue.");
        queuedPlayers.Add(new PlayerInfo { PlayerName = new FixedString32Bytes(playerName), ClientId = clientId });

        OnPlayerQueuedServer?.Invoke(clientId, playerName);

        // Update specific client about their queue status
        ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[]{ clientId } } };
        SetPlayerQueueStatusClientRpc(true, clientRpcParams);

        CheckForMatch();
    }


    /// <summary>
    /// [ServerRpc] Handles a client's request to leave the queue.
    /// Removes the player from the <see cref="queuedPlayers"/> list if found.
    /// If successful, updates the requesting client's UI via ClientRpc.
    /// </summary>
    /// <param name="clientId">The ClientId of the requesting client.</param>
    [ServerRpc(RequireOwnership = false)]
    private void LeaveQueueServerRpc(ulong clientId)
    {
        if (!IsServer) return;

        bool removed = false;
        for (int i = queuedPlayers.Count - 1; i >= 0; i--) // Iterate backwards when removing
        {
            if (queuedPlayers[i].ClientId == clientId)
            {
                Debug.Log($"Client {clientId} ({queuedPlayers[i].PlayerName}) leaving queue.");
                queuedPlayers.RemoveAt(i);
                removed = true;
                break; // Assuming player can only be in queue once
            }
        }

        if(removed)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[]{ clientId } } };
            SetPlayerQueueStatusClientRpc(false, clientRpcParams);
        }
        else
        {
             Debug.LogWarning($"Client {clientId} tried to leave queue but was not found.");
        }
    }

    /// <summary>
    /// [Server Only] Checks if a match can be made (exactly 2 players in queue).
    /// If a match is found, it invokes the <see cref="OnMatchFoundServer"/> event,
    /// notifies the involved clients via <see cref="StartMatchClientRpc"/>, and removes them from the queue.
    /// </summary>
    private void CheckForMatch()
    {
        if (!IsServer || queuedPlayers.Count < 2) return;

        // Prevent processing match if already loading scene
        if (matchInProgress) {
            Debug.LogWarning("CheckForMatch called but matchInProgress is already true.");
            return;
        }

        ulong player1ClientId = queuedPlayers[0].ClientId;
        ulong player2ClientId = queuedPlayers[1].ClientId;

        OnMatchFoundServer?.Invoke(player1ClientId, player2ClientId);
        Debug.Log($"[Matchmaker] Match found! Invoked OnMatchFoundServer for clients {player1ClientId} & {player2ClientId}");

        StartMatchClientRpc(player1ClientId, player2ClientId);

        matchInProgress = true; // Set flag *before* removing players

        // Remove matched players AFTER invoking event and notifying clients
        // This order is important for the matchInProgress flag logic in HandleQueueChanged
        queuedPlayers.RemoveAt(1); // Remove P2 first (index 1)
        queuedPlayers.RemoveAt(0); // Then remove P1 (now at index 0)

        // Load the character select scene for the matched players.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            Debug.Log($"[Matchmaker] Loading scene: {characterSelectSceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(characterSelectSceneName, LoadSceneMode.Single);
            // Note: matchInProgress will remain true until the Matchmaker script is destroyed/reloaded
            // Or needs to be reset explicitly if scene loading fails or is cancelled
        }
         else
         {
             Debug.LogError("[Matchmaker] Cannot load scene - NetworkManager or SceneManager is null!");
             matchInProgress = false; // Reset flag if scene load fails critically
         }
    }

    /// <summary>
    /// [ClientRpc] Updates the queue status UI on a specific target client.
    /// Called by the server after join/leave operations.
    /// </summary>
    /// <param name="inQueue">True if the client is now in the queue, false otherwise.</param>
    /// <param name="clientRpcParams">Parameters specifying the target client.</param>
    [ClientRpc]
    private void SetPlayerQueueStatusClientRpc(bool inQueue, ClientRpcParams clientRpcParams = default)
    {
        // Only update UI if this is the target client
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        if (matchmakerUI != null)
        {
            matchmakerUI.UpdateQueueStatus(inQueue);
        }
        else if(IsOwner) // Only log error on the actual client it was meant for
        {
             Debug.LogError("SetPlayerQueueStatusClientRpc: MatchmakerUI reference is null!", this);
        }
    }

    /// <summary>
    /// [ClientRpc] Notifies the two matched clients that a match has been found and the game will load.
    /// Updates the UI on the target clients.
    /// </summary>
    /// <param name="player1Id">ClientId of Player 1.</param>
    /// <param name="player2Id">ClientId of Player 2.</param>
    [ClientRpc]
    private void StartMatchClientRpc(ulong player1Id, ulong player2Id)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        // Only update UI if this client is one of the matched players
        if (NetworkManager.Singleton.LocalClientId == player1Id || NetworkManager.Singleton.LocalClientId == player2Id)
        {
            if(matchmakerUI != null)
            {
                matchmakerUI.UpdateQueueDisplayText("Match found! Loading game...");
                // The UI might also want to disable buttons here again
                matchmakerUI.UpdateQueueStatus(false); // Treat as leaving queue UI-wise
            }
            else
            {
                 Debug.LogError("StartMatchClientRpc: MatchmakerUI reference is null!", this);
            }
        }
    }

    /// <summary>
    /// Callback handler for the <see cref="queuedPlayers"/> <see cref="NetworkList{T}.OnListChanged"/> event.
    /// Updates the <see cref="matchmakerUI"/> display text to show the current list of queued players.
    /// Runs on all clients due to NetworkList synchronization.
    /// </summary>
    /// <param name="changeEvent">Details about the change in the NetworkList (can be ignored for simple redraw).</param>
    private void HandleQueueChanged(NetworkListEvent<PlayerInfo> changeEvent)
    {
        // Prevent UI update if a match is being loaded
        if (matchInProgress)
        {
            // Debug.Log("HandleQueueChanged: matchInProgress is true, skipping UI update.");
            return;
        }

        if (matchmakerUI != null)
        {
            if (queuedPlayers.Count == 0)
            {
                 matchmakerUI.UpdateQueueDisplayText("No players in queue");
            }
            else
            {
                string queueDisplay = "Queued players:\n";
                foreach (var player in queuedPlayers) queueDisplay += player.PlayerName.ToString() + "\n";
                 matchmakerUI.UpdateQueueDisplayText(queueDisplay);
            }
        }
         // Don't log error if matchmakerUI is null here, as it might be null legitimately
         // on server or non-UI clients.
    }
} 