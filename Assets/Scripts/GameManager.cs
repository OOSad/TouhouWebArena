using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System;

// Define PlayerRole enum
public enum PlayerRole
{
    None,
    Player1,
    Player2
}

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
        public PlayerRole Role; // Add Role field
        public int FairyKillCount; // Track kills towards extra attack
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref SelectedCharacter);
            serializer.SerializeValue(ref Role); // Serialize Role
            serializer.SerializeValue(ref FairyKillCount); // Serialize kill count
        }
        
        public bool Equals(PlayerData other)
        {
            return ClientId == other.ClientId;
        }
        
        public override string ToString()
        {
            return $"Player: {PlayerName} (ID: {ClientId}, Character: {SelectedCharacter}, Role: {Role}, Kills: {FairyKillCount})"; // Update ToString
        }
    }
    
    private NetworkList<PlayerData> players;
    
    // --- Extra Attack Settings ---
    [Header("Extra Attack Settings")]
    [SerializeField] private int extraAttackThreshold = 7; // Fairies needed for Extra Attack
    [SerializeField] private GameObject reimuExtraAttackPrefab; // Assign Reimu's Yin-Yang Orb prefab
    // Add prefabs for other characters here
    // [SerializeField] private GameObject marisaExtraAttackPrefab; 
    // ---------------------------
    
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
            SelectedCharacter = new FixedString32Bytes(""),
            Role = PlayerRole.None, // Initialize Role to None
            FairyKillCount = 0 // FairyKillCount defaults to 0
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
                Debug.Log($"Assigned Role {role} to player {clientId}");
                return;
            }
        }
        Debug.LogWarning($"Attempted to assign role for unregistered player {clientId}");
    }
    
    // --- NEW: Increment kill count and check for Extra Attack (Server Only) ---
    public void IncrementFairyKillCount(PlayerRole killerRole)
    {
        if (!IsServer) return;
        // *** DEBUG LOG ***
        Debug.Log($"[PlayerDataManager Server] IncrementFairyKillCount called for role {killerRole}.");
        if (killerRole == PlayerRole.None) return; // Don't count unattributed kills

        int playerIndex = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].Role == killerRole)
            {
                playerIndex = i;
                break;
            }
        }

        if (playerIndex != -1)
        {
            PlayerData updatedData = players[playerIndex];
            updatedData.FairyKillCount++;
            players[playerIndex] = updatedData; // Update the list

            Debug.Log($"Player role {killerRole} kill count is now {updatedData.FairyKillCount}");

            // Check if threshold reached
            // *** DEBUG LOG ***
            Debug.Log($"[PlayerDataManager Server] Checking threshold: {updatedData.FairyKillCount} >= {extraAttackThreshold} ?");
            if (updatedData.FairyKillCount >= extraAttackThreshold)
            {
                // *** DEBUG LOG ***
                Debug.Log($"[PlayerDataManager Server] Threshold MET! Resetting count and triggering Extra Attack.");
                // Reset kill count
                updatedData.FairyKillCount = 0;
                players[playerIndex] = updatedData;

                // Trigger Extra Attack against the *opponent*
                PlayerRole opponentRole = (killerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
                TriggerExtraAttack(updatedData.SelectedCharacter.ToString(), opponentRole); 
            }
        }
        else
        {
            Debug.LogWarning($"IncrementFairyKillCount called for role {killerRole}, but no player found with that role.");
        }
    }
    // -------------------------------------------------------------------------

    // --- NEW: Trigger the appropriate Extra Attack (Server Only) ---
    private void TriggerExtraAttack(string attackerCharacter, PlayerRole targetRole)
    { 
        Debug.Log($"Triggering Extra Attack from {attackerCharacter} against {targetRole}");

        GameObject prefabToSpawn = null;
        switch (attackerCharacter)
        {
            case "Hakurei Reimu": // Match the character name string used during selection
                prefabToSpawn = reimuExtraAttackPrefab;
                break;
            // case "Kirisame Marisa":
            //     prefabToSpawn = marisaExtraAttackPrefab;
            //     break;
            default:
                 Debug.LogError($"No Extra Attack prefab defined for character: {attackerCharacter}");
                 return;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError($"Extra Attack prefab for {attackerCharacter} is not assigned in PlayerDataManager!");
            return;
        }

        // --- Get Spawn Position from ReimuExtraAttackOrbSpawner ---
        ReimuExtraAttackOrbSpawner orbSpawner = FindObjectOfType<ReimuExtraAttackOrbSpawner>();
        if (orbSpawner == null)
        {
            Debug.LogError("Cannot find ReimuExtraAttackOrbSpawner in the scene to determine spawn position!");
            return; // Cannot spawn without position
        }

        Transform targetZone = (targetRole == PlayerRole.Player1) ? orbSpawner.GetSpawnZone1() : orbSpawner.GetSpawnZone2();
        if (targetZone == null)
        {
             Debug.LogError($"Target spawn zone {(targetRole == PlayerRole.Player1 ? 1: 2)} is not assigned in the ReimuExtraAttackOrbSpawner!");
             return;
        }

        Vector2 zoneSize = orbSpawner.GetSpawnZoneSize();
        Vector3 zoneCenter = targetZone.position;

        // Calculate random position within the target zone
        float randomX = UnityEngine.Random.Range(-zoneSize.x / 2f, zoneSize.x / 2f);
        float randomY = UnityEngine.Random.Range(-zoneSize.y / 2f, zoneSize.y / 2f);
        Vector3 spawnPos = new Vector3(zoneCenter.x + randomX, zoneCenter.y + randomY, zoneCenter.z);
        // --- End Get Spawn Position ---

        // Instantiate and spawn the Extra Attack object
        GameObject attackInstance = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        NetworkObject attackNetworkObject = attackInstance.GetComponent<NetworkObject>();

        if (attackNetworkObject == null)
        {
            Debug.LogError($"Extra Attack Prefab for {attackerCharacter} is missing NetworkObject!");
            Destroy(attackInstance);
            return;
        }
        
        // --- Assign Target Role --- 
        // Assuming the extra attack prefab has a script like ReimuExtraAttackOrb.cs
        // with a NetworkVariable<PlayerRole> TargetPlayerRole
        var attackScript = attackInstance.GetComponent<ReimuExtraAttackOrb>(); // Get specific script
        if(attackScript != null)
        {
            attackScript.TargetPlayerRole.Value = targetRole;
        }
        else
        {
             Debug.LogError($"Spawned Extra Attack for {attackerCharacter} is missing expected script (e.g., ReimuExtraAttackOrb)!");
             // Consider destroying if script is missing?
             // Destroy(attackInstance);
             // return;
        }
        // --------------------------

        attackNetworkObject.Spawn(true);
        Debug.Log($"Spawned {attackerCharacter}'s Extra Attack targeting {targetRole}.");
    }
    // --------------------------------------------------------------

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
    
    #endregion
} 