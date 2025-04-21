using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System;
using System.Collections;

// Define PlayerRole enum
public enum PlayerRole
{
    None,
    Player1,
    Player2
}

// --- MOVED PlayerData struct outside the class ---
[System.Serializable]
public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
{
    public ulong ClientId;
    public FixedString64Bytes PlayerName;
    public FixedString32Bytes SelectedCharacter;
    public PlayerRole Role;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref SelectedCharacter);
        serializer.SerializeValue(ref Role);
    }

    public bool Equals(PlayerData other)
    {
        return ClientId == other.ClientId;
    }

    public override string ToString()
    {
        return $"Player: {PlayerName} (ID: {ClientId}, Character: {SelectedCharacter}, Role: {Role})";
    }
}
// --------------------------------------------------

public class PlayerDataManager : NetworkBehaviour
{
    // Singleton pattern
    public static PlayerDataManager Instance { get; private set; }
    
    // Event triggered when the player list changes
    public event Action OnPlayerDataUpdated;
    
    private NetworkList<PlayerData> players;
    
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize network list
        players = new NetworkList<PlayerData>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        players.OnListChanged += HandlePlayerDataListChanged;
        
        // --- Subscribe to NetworkManager events on Server ---
        if (IsServer)
        {
            // Subscribe to disconnect event to handle cleanup
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            }

            // Subscribe to Matchmaker event (existing logic)
            StartCoroutine(SubscribeToMatchmakerEventsDelayed()); 
        }
        // ----------------------------------------------------
        
        OnPlayerDataUpdated?.Invoke(); // Trigger initial UI update
    }
    
    public override void OnNetworkDespawn()
    {
        if (players != null)
        {
            players.OnListChanged -= HandlePlayerDataListChanged; // Unsubscribe
        }
        
        // --- Unsubscribe from NetworkManager/Matchmaker events on Server ---
        // Check IsServer AND Instance existence because this might be called during shutdown
        if (IsServer)
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }
            if (Matchmaker.Instance != null) 
            { 
                Matchmaker.Instance.OnPlayerQueuedServer -= HandlePlayerQueued;
            }
        }
        // -----------------------------------------------------
        
        base.OnNetworkDespawn();
    }
    
    private void HandlePlayerDataListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        OnPlayerDataUpdated?.Invoke();
    }
    
    public override void OnDestroy()
    {
        if (players != null)
        {
            players.Dispose();
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
        
        base.OnDestroy();
    }
    
    #region Public Methods
    
    // --- NEW: Handler for the Matchmaker event --- 
    private void HandlePlayerQueued(ulong clientId, string playerName)
    {
        RegisterPlayer(clientId, playerName);
    }
    // ------------------------------------------

    // Register a player with the player data manager
    public void RegisterPlayer(ulong clientId, string playerName)
    {
        if (!IsServer) return;
        
        // Check if player already exists
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                return;
            }
        }
        
        // Create new player data
        PlayerData newPlayer = new PlayerData
        {
            ClientId = clientId,
            PlayerName = new FixedString64Bytes(playerName),
            SelectedCharacter = new FixedString32Bytes(""),
            Role = PlayerRole.None
        };
        
        // Add to list
        players.Add(newPlayer);
    }
    
    // Update player character selection
    public void SetPlayerCharacter(ulong clientId, string characterName)
    {
        if (!IsServer) return;
        
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                PlayerData updatedData = players[i];
                updatedData.SelectedCharacter = new FixedString32Bytes(characterName);
                players[i] = updatedData;
                
                return;
            }
        }
    }
    
    // Add method to assign player roles (Server only)
    public void AssignPlayerRole(ulong clientId, PlayerRole role)
    {
        if (!IsServer) return;
        
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                PlayerData updatedData = players[i];
                updatedData.Role = role;
                players[i] = updatedData;
                return;
            }
        }
    }
    
    // --- MODIFIED: Increment kill count - now delegates to ExtraAttackManager ---
    public void IncrementFairyKillCount(PlayerRole killerRole)
    {
        if (!IsServer || killerRole == PlayerRole.None) return;

        // Find the player data corresponding to the killer role
        PlayerData? killerData = null;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].Role == killerRole)
            {
                killerData = players[i];
                break;
            }
        }

        // If player found, notify the ExtraAttackManager
        if (killerData.HasValue)
        {
            // TODO: Add score increment or other logic here if needed for regular kills
        }
    }
    // ------------------------------------------------------------------------

    // Remove a player from the player data manager
    public void UnregisterPlayer(ulong clientId)
    {
        if (!IsServer) return;
        
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                players.RemoveAt(i);
                return;
            }
        }
    }
    
    // Get player data (can be called from any client)
    public PlayerData? GetPlayerData(ulong clientId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                return players[i];
            }
        }
        
        return null;
    }
    
    // Get player 1 data based on Role
    public PlayerData? GetPlayer1Data()
    {
        foreach (var player in players)
        {
            if (player.Role == PlayerRole.Player1)
            {
                return player;
            }
        }
        return null;
    }
    
    // Get player 2 data based on Role
    public PlayerData? GetPlayer2Data()
    {
        foreach (var player in players)
        {
            if (player.Role == PlayerRole.Player2)
            {
                return player;
            }
        }
        return null;
    }
    
    // Check if both players have selected characters and have roles assigned
    public bool AreBothPlayersReady()
    {
        // Check if Player1 and Player2 roles are assigned
        PlayerData? p1Data = GetPlayer1Data();
        PlayerData? p2Data = GetPlayer2Data();
        
        if (!p1Data.HasValue || !p2Data.HasValue) return false;

        // Check if both assigned players have selected characters
        return !string.IsNullOrEmpty(p1Data.Value.SelectedCharacter.ToString()) &&
               !string.IsNullOrEmpty(p2Data.Value.SelectedCharacter.ToString());
    }
    
    // --- NEW HELPER: Get PlayerData by Role ---
    public PlayerData? GetPlayerDataByRole(PlayerRole role)
    {
        if (role == PlayerRole.None) return null;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].Role == role)
            {
                return players[i];
            }
        }
        return null; // Role not found among registered players
    }
    // ---------------------------------------

    #endregion

    // --- NEW: Coroutine to delay subscription slightly ---
    private IEnumerator SubscribeToMatchmakerEventsDelayed()
    {
        yield return new WaitForSeconds(0.1f); // Short delay

        if (Matchmaker.Instance != null)
        {
            Matchmaker.Instance.OnPlayerQueuedServer += HandlePlayerQueued;
        }
    }
    // ----------------------------------------------------

    // Server-side handler for client disconnection
    private void HandleClientDisconnect(ulong clientId)
    {
        UnregisterPlayer(clientId);
    }
}