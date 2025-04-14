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
    [SerializeField] private int extraAttackThreshold = 10; // Fairies needed for Extra Attack
    [SerializeField] private GameObject reimuExtraAttackPrefab; // Assign Reimu's Yin-Yang Orb prefab
    [SerializeField] private GameObject marisaExtraAttackPrefab; // Assign Marisa's Earthlight Ray prefab

    // Removed cached spawner references
    // private ReimuExtraAttackOrbSpawner _reimuSpawner;
    // private MarisaExtraAttackSpawner _marisaSpawner;

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

        // Removed GetComponent calls for spawners from Awake
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

            Debug.Log($"Player {killerRole} kill count: {updatedData.FairyKillCount}/{extraAttackThreshold}"); // Debug Log

            // Check if threshold reached
            if (updatedData.FairyKillCount >= extraAttackThreshold)
            {
                Debug.Log($"Player {killerRole} reached Extra Attack threshold!"); // Debug Log
                // Reset kill count
                updatedData.FairyKillCount = 0;
                players[playerIndex] = updatedData;

                // Trigger Extra Attack against the *opponent*
                PlayerRole opponentRole = (killerRole == PlayerRole.Player1) ? PlayerRole.Player2 : PlayerRole.Player1;
                // Pass the attacker's data to TriggerExtraAttack
                TriggerExtraAttack(updatedData, opponentRole);
            }
        }
        else
        {
            Debug.LogWarning($"IncrementFairyKillCount called for role {killerRole}, but no player found with that role.");
        }
    }
    // -------------------------------------------------------------------------

    // --- MODIFIED: Trigger the appropriate Extra Attack (Server Only) ---
    private void TriggerExtraAttack(PlayerData attackerData, PlayerRole targetRole)
    {
        string attackerCharacter = attackerData.SelectedCharacter.ToString();
        Debug.Log($"[TriggerExtraAttack] Triggering for {attackerCharacter} (Role: {attackerData.Role}) against {targetRole}");

        GameObject prefabToSpawn = null;
        Action<GameObject, Transform> spawnLogic = null;
        Transform targetSpawnArea = null;

        switch (attackerCharacter)
        {
            case "Hakurei Reimu":
                prefabToSpawn = reimuExtraAttackPrefab;
                // Find the spawner component in the scene NOW
                ReimuExtraAttackOrbSpawner reimuSpawner = FindObjectOfType<ReimuExtraAttackOrbSpawner>();
                if (reimuSpawner == null)
                {
                    Debug.LogError("Cannot trigger Reimu Extra Attack: ReimuExtraAttackOrbSpawner component not found in the active scene.", this);
                    return;
                }
                targetSpawnArea = (targetRole == PlayerRole.Player1) ? reimuSpawner.GetSpawnZone1() : reimuSpawner.GetSpawnZone2();

                spawnLogic = (prefab, spawnArea) => {
                    if(spawnArea != null)
                    {
                         GameObject instance = Instantiate(prefab, spawnArea.position, Quaternion.identity);
                         NetworkObject nob = instance.GetComponent<NetworkObject>();
                         if (nob != null) nob.Spawn(true);

                         ReimuExtraAttackOrb orbScript = instance.GetComponent<ReimuExtraAttackOrb>();
                         if(orbScript != null)
                         {
                             orbScript.TargetPlayerRole.Value = targetRole;
                         }
                         else
                         {
                            Debug.LogError("Failed to get ReimuExtraAttackOrb script from instantiated prefab!");
                         }
                    } else {
                         Debug.LogError($"Spawn Area Transform for Role {targetRole} was null when attempting Reimu's attack.");
                    }
                };
                break;

            case "Kirisame Marisa":
                prefabToSpawn = marisaExtraAttackPrefab;
                // Find the spawner component in the scene NOW
                MarisaExtraAttackSpawner marisaSpawner = FindObjectOfType<MarisaExtraAttackSpawner>();
                 if (marisaSpawner == null)
                {
                    Debug.LogError("Cannot trigger Marisa Extra Attack: MarisaExtraAttackSpawner component not found in the active scene.", this);
                    return;
                }
                targetSpawnArea = (targetRole == PlayerRole.Player1) ? marisaSpawner.GetPlayer1TargetArea() : marisaSpawner.GetPlayer2TargetArea();
                float spawnWidth = marisaSpawner.GetSpawnWidth();

                spawnLogic = (prefab, spawnArea) => {
                    if (spawnArea != null)
                    {
                        float randomOffsetX = UnityEngine.Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
                        Vector3 spawnPosition = spawnArea.position + new Vector3(randomOffsetX, 0, 0);

                        GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity);
                        NetworkObject nob = instance.GetComponent<NetworkObject>();
                        if (nob != null) nob.Spawn(true);

                        EarthlightRay rayScript = instance.GetComponent<EarthlightRay>();
                        if (rayScript != null)
                        {
                            rayScript.AttackerRole.Value = attackerData.Role;
                            Debug.Log($"Set Earthlight Ray AttackerRole to {attackerData.Role}");
                        }
                        else
                        {
                            Debug.LogError("Failed to get EarthlightRay script from instantiated prefab!");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Spawn Area Transform for Role {targetRole} was null when attempting Marisa's attack.");
                    }
                };
                break;

            default:
                 Debug.LogError($"No Extra Attack defined for character: {attackerCharacter}");
                 return;
        }

        // --- Common Checks and Execution ---
        if (prefabToSpawn == null)
        {
            Debug.LogError($"Extra Attack Prefab is null for character: {attackerCharacter}. Make sure it's assigned in the PlayerDataManager component.");
            return;
        }

        if (targetSpawnArea == null)
        {
             Debug.LogError($"Target Extra Attack Spawn Area for Role {targetRole} (Character: {attackerCharacter}) is not assigned in the corresponding Spawner component (Reimu or Marisa).");
             return;
        }

        // Execute the specific spawn logic
        if (spawnLogic != null)
        {
            spawnLogic(prefabToSpawn, targetSpawnArea);
        }
        else
        {
             // Should not happen if switch statement is correct
             Debug.LogError($"Internal Error: Spawn logic not defined for {attackerCharacter}.");
        }
    }
    // -------------------------------------------------------------

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