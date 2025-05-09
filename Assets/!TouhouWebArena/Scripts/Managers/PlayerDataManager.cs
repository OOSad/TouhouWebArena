using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System;
using System.Collections;

/// <summary>
/// Defines the possible roles a player can have in the game.
/// </summary>
public enum PlayerRole
{
    /// <summary>No role assigned or role is unknown.</summary>
    None,
    /// <summary>Represents Player 1 (typically left side).</summary>
    Player1,
    /// <summary>Represents Player 2 (typically right side).</summary>
    Player2
}

/// <summary>
/// Structure holding the synchronized data for a single player.
/// Includes identification, selected character, and assigned role.
/// Implements <see cref="INetworkSerializable"/> for network transport
/// and <see cref="IEquatable{T}"/> for efficient comparisons based on ClientId.
/// </summary>
[System.Serializable]
public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
{
    /// <summary>The unique network identifier for the player's client.</summary>
    public ulong ClientId;
    /// <summary>The player's chosen display name (fixed size for network serialization).</summary>
    public FixedString64Bytes PlayerName;
    /// <summary>The name identifier of the character selected by the player (fixed size for network serialization).</summary>
    public FixedString32Bytes SelectedCharacter;
    /// <summary>The assigned role (<see cref="PlayerRole"/>) determining the player's side and potentially other gameplay aspects.</summary>
    public PlayerRole Role;

    /// <summary>
    /// Serializes/deserializes the struct's fields for network transmission.
    /// </summary>
    /// <typeparam name="T">The type of the buffer serializer.</typeparam>
    /// <param name="serializer">The serializer instance used for reading or writing.</param>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref SelectedCharacter);
        serializer.SerializeValue(ref Role);
    }

    /// <summary>
    /// Determines equality based solely on the <see cref="ClientId"/>.
    /// </summary>
    /// <param name="other">The other PlayerData instance to compare against.</param>
    /// <returns>True if the ClientIds match, false otherwise.</returns>
    public bool Equals(PlayerData other)
    {
        return ClientId == other.ClientId;
    }

    /// <summary>
    /// Provides a string representation of the player data for debugging purposes.
    /// </summary>
    /// <returns>A formatted string containing player details.</returns>
    public override string ToString()
    {
        return $"Player: {PlayerName} (ID: {ClientId}, Character: {SelectedCharacter}, Role: {Role})";
    }
}

/// <summary>
/// Manages player data across the network using a singleton pattern.
/// Acts as a central repository for <see cref="PlayerData"/> for all connected clients.
/// Uses a <see cref="NetworkList{T}"/> to synchronize player data automatically.
/// Provides methods for registering, updating, and retrieving player information, primarily intended for server use.
/// Includes integration with <see cref="NetworkManager"/> for disconnect handling and <see cref="Matchmaker"/> for player queuing.
/// </summary>
public class PlayerDataManager : NetworkBehaviour
{
    /// <summary>
    /// Singleton instance of the PlayerDataManager.
    /// </summary>
    public static PlayerDataManager Instance { get; private set; }
    
    /// <summary>
    /// Event triggered whenever the list of player data (<see cref="players"/>) changes (add, remove, update).
    /// UI elements or other systems can subscribe to this to react to player list modifications.
    /// </summary>
    public event Action OnPlayerDataUpdated;
    
    /// <summary>
    /// The synchronized list containing <see cref="PlayerData"/> for all connected players.
    /// Automatically replicated from server to clients.
    /// </summary>
    private NetworkList<PlayerData> players;
    
    /// <summary>
    /// Called when the script instance is being loaded.
    /// Implements the singleton pattern and initializes the <see cref="players"/> NetworkList.
    /// </summary>
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // RESTORED: Persistent manager
        
        // NetworkList initialization moved to network event handlers
        DisposeNetworkList(); // Safety check
        players = new NetworkList<PlayerData>(); // Initialize immediately
        Debug.Log("[PlayerDataManager] NetworkList initialized in Awake.");
    }
    
    /// <summary>
    /// Called when the network object is spawned.
    /// Subscribes to list changes and network events.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // NetworkList initialization happens in HandleServerStarted
        players.OnListChanged += HandlePlayerDataListChanged;
        
        // --- Subscribe to NetworkManager session events --- 
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.OnServerStopped += HandleServerStopped; 
            if (IsServer) // Only server needs disconnect for player removal
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            }

            // If server is already running when this spawns, initialize immediately
            if (NetworkManager.Singleton.IsServer)
            {
                 // HandleServerStarted(); // Initialize list if starting as server // REMOVED - Handled by Awake
            }
        }
        else { Debug.LogError("[PlayerDataManager] NetworkManager Singleton is null in OnNetworkSpawn!"); }
        // -------------------------------------------------
        
        // --- Subscribe to Matchmaker event on Server --- 
        if (IsServer)
        {
            StartCoroutine(SubscribeToMatchmakerEventsDelayed()); 
        }
        // --------------------------------------------
        
        // Initial UI update triggered by InitializeNetworkList
        // OnPlayerDataUpdated?.Invoke(); 
    }
    
    /// <summary>
    /// Called when the network object is despawned.
    /// Unsubscribes from events.
    /// </summary>
    public override void OnNetworkDespawn()
    { 
        // --- Unsubscribe from NetworkManager/Matchmaker events --- 
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnServerStopped -= HandleServerStopped;
             if (IsServer) // Only server subscribed
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }
        }
        if (IsServer && Matchmaker.Instance != null) 
        { 
             Matchmaker.Instance.OnPlayerQueuedServer -= HandlePlayerQueued;
        }
        // ---------------------------------------------------------
        
        // List is disposed by HandleServerStopped or OnDestroy
        // DisposeNetworkList(); 

        base.OnNetworkDespawn();
    }
    
    /// <summary>
    /// Callback handler for the <see cref="players"/> <see cref="NetworkList{T}.OnListChanged"/> event.
    /// Invokes the <see cref="OnPlayerDataUpdated"/> event to notify subscribers.
    /// </summary>
    /// <param name="changeEvent">Details about the change in the NetworkList.</param>
    private void HandlePlayerDataListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        // Debug.Log($"[PlayerDataManager] NetworkList changed: Type={changeEvent.Type}, Index={changeEvent.Index}, Value={changeEvent.Value}"); // Optional: Keep if needed
        OnPlayerDataUpdated?.Invoke();
    }
    
    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Cleans up instance reference. List disposal is not strictly needed for DDOL objects
    /// unless explicitly shutting down the application.
    /// </summary>
    public override void OnDestroy()
    {
        // Ensure list is disposed if object is destroyed
        DisposeNetworkList(); 
        
        if (Instance == this)
        {
            Instance = null;
        }
        
        base.OnDestroy();
    }

    // --- NetworkList Lifecycle Management ---

    private void HandleServerStarted()
    {
        if (!IsServer) return; // Should not happen if subscribed correctly, but safety check
        // Debug.Log("[PlayerDataManager] Server Started event received. Initializing NetworkList.");
        // InitializeNetworkList(); // REMOVED - Initialization moved back to Awake
    }

    private void HandleServerStopped(bool wasServer)
    {
        // wasServer is true if NetworkManager was running as server when Shutdown was called
        // We want to dispose if we *were* the server
        if (wasServer) 
        {
             // Debug.Log("[PlayerDataManager] Server Stopped event received. Disposing NetworkList.");
             DisposeNetworkList();
        }
    }

    private void DisposeNetworkList()
    {
        if (players != null)
        {
            // Debug.Log("[PlayerDataManager] Disposing existing NetworkList.");
            players.OnListChanged -= HandlePlayerDataListChanged;
            players.Dispose();
            players = null;
        }
    }

    // --------------------------------------

    #region Public Methods
    
    /// <summary>
    /// Server-side handler for the <see cref="Matchmaker.OnPlayerQueuedServer"/> event.
    /// Automatically calls <see cref="RegisterPlayer"/> when the Matchmaker signals a player has queued.
    /// </summary>
    /// <param name="clientId">The ClientId of the player who queued.</param>
    /// <param name="playerName">The name of the player who queued.</param>
    private void HandlePlayerQueued(ulong clientId, string playerName)
    {
        if (!IsServer) return;
        // Debug.Log($"[PlayerDataManager] Player Queued: ClientId={clientId}, Name={playerName}");
        RegisterPlayer(clientId, playerName);
    }

    /// <summary>
    /// [Server Only] Registers a new player in the manager.
    /// Checks if the player already exists. If not, creates a new <see cref="PlayerData"/> entry
    /// with the provided ClientId and name, default character/role, and adds it to the <see cref="players"/> list.
    /// </summary>
    /// <param name="clientId">The ClientId of the player to register.</param>
    /// <param name="playerName">The display name of the player.</param>
    public void RegisterPlayer(ulong clientId, string playerName)
    {
        if (!IsServer) return;
        // Debug.Log($"[PlayerDataManager] Registering player: ClientId={clientId}, Name={playerName}");
        
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

        // Debug.Log($"[PlayerDataManager] Player {playerName} (Client {clientId}) registered. Total players: {players.Count}");
        // OnPlayerDataUpdated?.Invoke(); // Let NetworkList event handle this
    }
    
    /// <summary>
    /// [Server Only] Sets the selected character for a specific player.
    /// Finds the player by <see cref="clientId"/> and updates their <see cref="PlayerData.SelectedCharacter"/>.
    /// </summary>
    /// <param name="clientId">The ClientId of the player whose character to set.</param>
    /// <param name="characterName">The name identifier of the selected character.</param>
    public void SetPlayerCharacter(ulong clientId, string characterName)
    {
        if (!IsServer) return;
        // Debug.Log($"[PlayerDataManager] Setting character for Client {clientId} to {characterName}");
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
            {
                PlayerData updatedData = players[i];
                updatedData.SelectedCharacter = new FixedString32Bytes(characterName);
                players[i] = updatedData;
                // Debug.Log($"[PlayerDataManager] Updated PlayerData: {players[i]}");
                return;
            }
        }
    }
    
    /// <summary>
    /// [Server Only] Assigns a <see cref="PlayerRole"/> (Player1 or Player2) to a specific player.
    /// Finds the player by <see cref="clientId"/> and updates their <see cref="PlayerData.Role"/>.
    /// </summary>
    /// <param name="clientId">The ClientId of the player whose role to assign.</param>
    /// <param name="role">The <see cref="PlayerRole"/> to assign.</param>
    public void AssignPlayerRole(ulong clientId, PlayerRole role)
    {
        if (!IsServer) return;
        // Debug.Log($"[PlayerDataManager] Assigning role {role} to Client {clientId}");
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
    
    /// <summary>
    /// [Server Only] Placeholder method potentially related to tracking kills.
    /// Currently finds player data by role but does not modify state directly.
    /// Intended to delegate to other systems like <see cref="NetworkExtraAttackManager"/>.
    /// </summary>
    /// <param name="killerRole">The role of the player who got the kill.</param>
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
            // TODO: Call NetworkExtraAttackManager or scoring system here.
            // Example: NetworkExtraAttackManager.Instance?.RecordKill(killerData.Value.ClientId);
        }
    }

    /// <summary>
    /// [Server Only] Removes a player's data from the manager.
    /// Finds the player by <see cref="clientId"/> and removes their entry from the <see cref="players"/> list.
    /// Typically called when a client disconnects.
    /// </summary>
    /// <param name="clientId">The ClientId of the player to unregister.</param>
    public void UnregisterPlayer(ulong clientId)
    {
        if (!IsServer) return;
        // Debug.Log($"[PlayerDataManager] Attempting to unregister Client {clientId}");
        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i].ClientId == clientId)
            {
                // Debug.Log($"[PlayerDataManager] Unregistering player: {players[i].PlayerName} (Client {clientId})");
                players.RemoveAt(i);
                // OnPlayerDataUpdated?.Invoke(); // Let NetworkList event handle this
                return; // Player found and removed
            }
        }
        // Debug.LogWarning($"[PlayerDataManager] Could not unregister Client {clientId}: Player not found.");
    }
    
    /// <summary>
    /// Gets the <see cref="PlayerData"/> for a specific client.
    /// Can be called by any client or server.
    /// Iterates through the synchronized <see cref="players"/> list.
    /// </summary>
    /// <param name="clientId">The ClientId of the player whose data to retrieve.</param>
    /// <returns>The <see cref="PlayerData"/> struct if found, otherwise null.</returns>
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
    
    /// <summary>
    /// Gets the <see cref="PlayerData"/> for the client currently assigned the <see cref="PlayerRole.Player1"/> role.
    /// </summary>
    /// <returns>The PlayerData for Player 1 if assigned, otherwise null.</returns>
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
    
    /// <summary>
    /// Gets the <see cref="PlayerData"/> for the client currently assigned the <see cref="PlayerRole.Player2"/> role.
    /// </summary>
    /// <returns>The PlayerData for Player 2 if assigned, otherwise null.</returns>
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
    
    /// <summary>
    /// Checks if both Player 1 and Player 2 roles have been assigned and have selected a character.
    /// Useful for determining game readiness.
    /// </summary>
    /// <returns>True if both players are assigned a role and have a non-empty selected character, false otherwise.</returns>
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
    
    /// <summary>
    /// Gets the <see cref="PlayerData"/> for a specific <see cref="PlayerRole"/>.
    /// Searches the list for a player matching the given role.
    /// </summary>
    /// <param name="role">The <see cref="PlayerRole"/> to search for.</param>
    /// <returns>The <see cref="PlayerData"/> if a player with that role exists, otherwise null.</returns>
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
    #endregion

    #region Private Helpers / Coroutines

    /// <summary>
    /// Coroutine that waits briefly before subscribing to Matchmaker events.
    /// This delay helps ensure the Matchmaker singleton instance is available.
    /// Only runs on the server.
    /// </summary>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator SubscribeToMatchmakerEventsDelayed()
    {
        yield return new WaitForSeconds(0.1f); // Short delay

        if (Matchmaker.Instance != null)
        {
            Matchmaker.Instance.OnPlayerQueuedServer += HandlePlayerQueued;
        }
    }

    /// <summary>
    /// Server-side handler for the <see cref="NetworkManager.OnClientDisconnectCallback"/> event.
    /// Automatically calls <see cref="UnregisterPlayer"/> when a client disconnects from the server.
    /// </summary>
    /// <param name="clientId">The ClientId of the player who disconnected.</param>
    private void HandleClientDisconnect(ulong clientId)
    {
        if (!IsServer) return;
        // Debug.Log($"[PlayerDataManager] Client {clientId} disconnected. Unregistering.");
        UnregisterPlayer(clientId);
    }
    #endregion
}