using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Collections;
using System;
using System.Collections;

// PlayerInfo struct definition (now outside)
public struct PlayerInfo : INetworkSerializable, IEquatable<PlayerInfo>
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

public class Matchmaker : NetworkBehaviour
{
    [Header("UI Handler Reference")]
    [SerializeField] private MatchmakerUI matchmakerUI;

    private NetworkList<PlayerInfo> queuedPlayers;
    private ulong localClientId = ulong.MaxValue;

    public static Matchmaker Instance { get; private set; }

    // --- NEW: Server-Side Events ---
    public event Action<ulong, string> OnPlayerQueuedServer;       // Fired when a player is added to the queue
    public event Action<ulong, ulong> OnMatchFoundServer;  // Fired when two players are matched
    // --------------------------------

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        queuedPlayers = new NetworkList<PlayerInfo>();
        if (matchmakerUI == null) matchmakerUI = FindObjectOfType<MatchmakerUI>();
        if (matchmakerUI == null) Debug.LogWarning("Matchmaker could not find MatchmakerUI instance.", this);
    }

    private void OnEnable()
    {
        queuedPlayers.OnListChanged += HandleQueueChanged;
    }

    private void OnDisable()
    {
        queuedPlayers.OnListChanged -= HandleQueueChanged;
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        HandleQueueChanged(default);
    }

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

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            localClientId = clientId;
            HandleQueueChanged(default);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer && queuedPlayers != null)
        {
            for (int i = queuedPlayers.Count - 1; i >= 0; i--)
            {
                if (queuedPlayers[i].ClientId == clientId)
                {
                    Debug.Log($"[Server] Player {clientId} disconnected, removing from queue.");
                    queuedPlayers.RemoveAt(i);
                }
            }
        }
         if (clientId == localClientId) localClientId = ulong.MaxValue;
    }

    public void RequestJoinQueue(string playerName)
    {
         if (NetworkManager.Singleton.IsConnectedClient)
         {
            int uniqueId = UnityEngine.Random.Range(1000, 10000);
            string uniquePlayerName = $"{playerName}#{uniqueId}";
            JoinQueueServerRpc(uniquePlayerName, NetworkManager.Singleton.LocalClientId);
         }
         else Debug.LogWarning("RequestJoinQueue called but client is not connected.");
    }
     public void RequestLeaveQueue()
     {
         if (NetworkManager.Singleton.IsConnectedClient)
         {
             LeaveQueueServerRpc(NetworkManager.Singleton.LocalClientId);
         }
         else Debug.LogWarning("RequestLeaveQueue called but client is not connected.");
     }


    [ServerRpc(RequireOwnership = false)]
    private void JoinQueueServerRpc(string playerName, ulong clientId)
    {
        if (!IsServer) return;

        // Check if player already in queue
        for (int i = 0; i < queuedPlayers.Count; i++) if (queuedPlayers[i].ClientId == clientId) return;

        queuedPlayers.Add(new PlayerInfo { PlayerName = new FixedString32Bytes(playerName), ClientId = clientId });
        Debug.Log($"[Server] Added {playerName} ({clientId}) to queue.");

        // --- NEW: Invoke Queued Event --- Pass name
        OnPlayerQueuedServer?.Invoke(clientId, playerName); // Pass name
        // -----------------------------

        // Update specific client about their queue status
        ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[]{ clientId } } };
        SetPlayerQueueStatusClientRpc(true, clientRpcParams);

        CheckForMatch();
    }


    [ServerRpc(RequireOwnership = false)]
    private void LeaveQueueServerRpc(ulong clientId)
    {
        if (!IsServer) return;

        bool removed = false;
        for (int i = 0; i < queuedPlayers.Count; i++)
        {
            if (queuedPlayers[i].ClientId == clientId)
            {
                queuedPlayers.RemoveAt(i);
                removed = true;
                Debug.Log($"[Server] Removed Client {clientId} from queue.");
                break;
            }
        }

        if(removed)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[]{ clientId } } };
            SetPlayerQueueStatusClientRpc(false, clientRpcParams);
        }
    }

    private void CheckForMatch()
    {
        if (!IsServer || queuedPlayers.Count < 2) return;

        Debug.Log("[Server] Checking for match...");
        ulong player1ClientId = queuedPlayers[0].ClientId;
        ulong player2ClientId = queuedPlayers[1].ClientId;

        // --- NEW: Invoke Match Found Event ---
        OnMatchFoundServer?.Invoke(player1ClientId, player2ClientId);
        // -----------------------------------

        // Notify clients match is starting (UI update)
        StartMatchClientRpc(player1ClientId, player2ClientId);

        // Remove matched players AFTER invoking event and notifying clients
        queuedPlayers.RemoveAt(1); // P2
        queuedPlayers.RemoveAt(0); // P1
        Debug.Log("[Server] Removed matched players from queue.");
    }

    [ClientRpc] private void SetPlayerQueueStatusClientRpc(bool inQueue, ClientRpcParams clientRpcParams = default)
    {
        if (matchmakerUI != null) matchmakerUI.UpdateQueueStatus(inQueue);
        else Debug.LogWarning("SetPlayerQueueStatusClientRpc: MatchmakerUI reference is null.");
    }

    [ClientRpc] private void StartMatchClientRpc(ulong player1Id, ulong player2Id)
    {
        if (NetworkManager.Singleton.LocalClientId == player1Id || NetworkManager.Singleton.LocalClientId == player2Id)
        {
            Debug.Log("Match is starting for client: " + NetworkManager.Singleton.LocalClientId);
            if(matchmakerUI != null)
            {
                matchmakerUI.UpdateQueueDisplayText("Match found! Loading game...");
                matchmakerUI.UpdateQueueStatus(false);
            }
        }
    }

    private void HandleQueueChanged(NetworkListEvent<PlayerInfo> changeEvent)
    {
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
    }
} 