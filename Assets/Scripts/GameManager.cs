using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System;

public class PlayerDataManager : NetworkBehaviour
{
    // Singleton pattern
    public static PlayerDataManager Instance { get; private set; }
    
    // Event triggered when the player list changes
    public event Action OnPlayerDataUpdated;
    
    [System.Serializable]
    public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
    {
        public ulong ClientId;
        public FixedString64Bytes PlayerName;
        public FixedString32Bytes SelectedCharacter;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref SelectedCharacter);
        }
        
        public bool Equals(PlayerData other)
        {
            return ClientId == other.ClientId;
        }
        
        public override string ToString()
        {
            return $"Player: {PlayerName} (ID: {ClientId}, Character: {SelectedCharacter})";
        }
    }
    
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
        // Subscribe to list changes AFTER the list is initialized and network is ready
        players.OnListChanged += HandlePlayerDataListChanged;
        // Trigger initial update for late joiners or if list populated before spawn
        OnPlayerDataUpdated?.Invoke();
    }
    
    public override void OnNetworkDespawn()
    {
        if (players != null)
        {
            players.OnListChanged -= HandlePlayerDataListChanged; // Unsubscribe
        }
        base.OnNetworkDespawn();
    }
    
    private void HandlePlayerDataListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        // Debug.Log($"PlayerDataManager: NetworkList changed (Type: {changeEvent.Type}, Index: {changeEvent.Index})");
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
    
    // Register a player with the player data manager
    public void RegisterPlayer(ulong clientId, string playerName)
    {
        if (!IsServer) return;
        
        // Check if player already exists
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                Debug.Log($"Player {clientId} already registered");
                return;
            }
        }
        
        // Create new player data
        PlayerData newPlayer = new PlayerData
        {
            ClientId = clientId,
            PlayerName = new FixedString64Bytes(playerName),
            SelectedCharacter = new FixedString32Bytes("")
        };
        
        // Add to list
        players.Add(newPlayer);
        Debug.Log($"Registered new player: {newPlayer}");
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
                
                Debug.Log($"Updated player {clientId} character to {characterName}");
                return;
            }
        }
        
        Debug.LogWarning($"Attempted to set character for unregistered player {clientId}");
    }
    
    // Remove a player from the player data manager
    public void UnregisterPlayer(ulong clientId)
    {
        if (!IsServer) return;
        
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                players.RemoveAt(i);
                Debug.Log($"Unregistered player {clientId}");
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
    
    // Get player 1 data
    public PlayerData? GetPlayer1Data()
    {
        if (players.Count >= 1)
        {
            return players[0];
        }
        return null;
    }
    
    // Get player 2 data
    public PlayerData? GetPlayer2Data()
    {
        if (players.Count >= 2)
        {
            return players[1];
        }
        return null;
    }
    
    // Check if both players have selected characters
    public bool AreBothPlayersReady()
    {
        if (players.Count < 2) return false;
        
        return !string.IsNullOrEmpty(players[0].SelectedCharacter.ToString()) &&
               !string.IsNullOrEmpty(players[1].SelectedCharacter.ToString());
    }
    
    #endregion
} 