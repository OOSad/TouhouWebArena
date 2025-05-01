using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TouhouWebArena;
using TouhouWebArena.Spellcards;
using TouhouWebArena.Spellcards.Behaviors;
using System.Collections.Generic;

/// <summary>
/// **[Server Only]** Server-authoritative singleton service responsible for spawning all player-related projectiles
/// (basic shots, charge attacks, spellcards) based on requests received from <see cref="PlayerShootingController"/> RPCs.
/// Interacts with helper classes like <see cref="ServerBasicShotSpawner"/>, <see cref="ServerChargeAttackSpawner"/>,
/// <see cref="ServerIllusionManager"/>, and <see cref="ServerBulletConfigurer"/>.
/// </summary>
[RequireComponent(typeof(ServerSpellcardActionRunner))] // Add dependency for runner
public class ServerAttackSpawner : NetworkBehaviour
{
    /// <summary>Singleton instance of the ServerAttackSpawner.</summary>
    public static ServerAttackSpawner Instance { get; private set; }

    // --- Spawner Instances ---
    private ServerBasicShotSpawner _basicShotSpawner;
    private ServerChargeAttackSpawner _chargeAttackSpawner;
    private ServerSpellcardExecutor _spellcardExecutor; // Add executor
    private ServerSpellcardActionRunner _actionRunner; // Add runner
    // Add fields for other spawners/executors later
    // -----------------------

    // --- Active Illusion Tracking (Server Only) --- Removed, handled by ServerIllusionManager
    // private Dictionary<ulong, NetworkObject> _activeIllusionsTargetingPlayer = new Dictionary<ulong, NetworkObject>();
    // private Dictionary<ulong, NetworkObject> _activeIllusionsCastByPlayer = new Dictionary<ulong, NetworkObject>();
    // ---------------------------------------------

    // Constant vertical offset from player center for spawning basic shot pairs.
    // private const float firePointVerticalOffset = 0.5f; // Moved to ServerBasicShotSpawner

    /// <summary>
    /// Unity Awake method. Implements the singleton pattern.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ServerAttackSpawner] Duplicate instance detected. Destroying self.", gameObject);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Consider DontDestroyOnLoad(gameObject) if this manager needs to persist across scenes independently.

            // Create spawner instances
            _basicShotSpawner = new ServerBasicShotSpawner();
            _chargeAttackSpawner = new ServerChargeAttackSpawner();
            
            // Get/Add runner component
            _actionRunner = GetComponent<ServerSpellcardActionRunner>();
            if (_actionRunner == null) // Add component if missing
            {
                 Debug.LogWarning("[ServerAttackSpawner] ServerSpellcardActionRunner component not found, adding it.");
                _actionRunner = gameObject.AddComponent<ServerSpellcardActionRunner>();
            }

            // Create executor, passing the runner
            _spellcardExecutor = new ServerSpellcardExecutor(_actionRunner);
        }
    }

    /// <summary>
    /// Unity OnDestroy method. Clears the singleton instance if this is the active instance.
    /// </summary>
    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }
        }
        base.OnDestroy();
    }

    // --- Network Spawn/Disconnect Handling ---
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            if (NetworkManager.Singleton != null)
            {
                 NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            }
        }
    }

    private void HandleClientDisconnect(ulong disconnectedClientId)
    {
        if (!IsServer) return;
        
        // Delegate illusion cleanup to the manager instance
        ServerIllusionManager.Instance?.HandleClientDisconnect(disconnectedClientId);
    }
    // -----------------------------------------

    // --- Illusion Tracking Management (Server Only) --- Removed, handled by ServerIllusionManager
    // public void ServerNotifyIllusionDespawned(NetworkObject illusionNO)
    // {
        // [METHOD CONTENT REMOVED]
    // }

    // private void ServerForceDespawnIllusion(NetworkObject illusionNO)
    // {
       // [METHOD CONTENT REMOVED]
    // }
    // ---------------------------------------------------

    // --- Public Methods Called by PlayerShootingController RPCs ---

    /// <summary>
    /// **[Server Only]** Spawns a pair of basic shot bullets for the requesting player.
    /// Called via <see cref="PlayerShootingController.RequestFireServerRpc"/>.
    /// </summary>
    /// <param name="requesterClientId">The ClientId of the player who requested the shot.</param>
    public void SpawnBasicShot(ulong requesterClientId)
    {
        // Delegate to the specialized spawner
        _basicShotSpawner?.SpawnBasicShot(requesterClientId); 
    }

    /// <summary>
    /// **[Server Only]** Spawns the appropriate (non-pooled) charge attack for the requesting player's character.
    /// Called via <see cref="PlayerShootingController.RequestChargeAttackServerRpc"/>.
    /// </summary>
    /// <param name="requesterClientId">The ClientId of the player who requested the attack.</param>
    public void SpawnChargeAttack(ulong requesterClientId)
    {
        // Delegate to the specialized spawner
        _chargeAttackSpawner?.SpawnChargeAttack(requesterClientId);
    }

     /// <summary>
    /// **[Server Only]** Initiates the execution of a spellcard pattern against the opponent.
    /// Called by <see cref="PlayerShootingController.RequestSpellcardServerRpc"/> after cost is confirmed by <see cref="SpellBarManager"/>.
    /// Loads <see cref="SpellcardData"/> and starts the <see cref="ServerExecuteSpellcardActions"/> coroutine.
    /// </summary>
    /// <param name="senderClientId">The ClientId of the player declaring the spellcard.</param>
    /// <param name="spellLevel">The level of the spellcard (2, 3, or 4).</param>
    public void ExecuteSpellcard(ulong senderClientId, int spellLevel)
    {
        if (!IsServer) return;

        // --- Get Sender Info ---
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out NetworkClient senderClient) || senderClient.PlayerObject == null)
        {
            Debug.LogError($"[ServerAttackSpawner.ExecuteSpellcard] Could not find sender player object for client {senderClientId}");
            return;
        }
        CharacterStats senderStats = senderClient.PlayerObject.GetComponent<CharacterStats>();
         if (senderStats == null)
        {
            Debug.LogError($"[ServerAttackSpawner.ExecuteSpellcard] Sender player object for client {senderClientId} missing CharacterStats.");
            return;
        }
        string senderCharacterName = senderStats.GetCharacterName();

        // --- Find Opponent ---
        ulong opponentClientId = ulong.MaxValue;
        NetworkObject opponentPlayerObject = null;
        PlayerRole opponentRole = PlayerRole.None;
        foreach (var connectedClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (connectedClient.ClientId != senderClientId)
            {
                opponentClientId = connectedClient.ClientId;
                opponentPlayerObject = connectedClient.PlayerObject;
                break;
            }
        }
        if (opponentPlayerObject == null)
        {
            Debug.LogWarning($"[ServerAttackSpawner.ExecuteSpellcard] Could not find opponent for client {senderClientId} to execute spellcard.");
            return; // Cannot execute spellcard without an opponent
        }
        // --- Determine Opponent Role ---
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? opponentData = PlayerDataManager.Instance.GetPlayerData(opponentClientId);
            if (opponentData.HasValue)
            {
                opponentRole = opponentData.Value.Role;
            }
            else { Debug.LogError($"[ServerAttackSpawner] Could not get PlayerData for opponent {opponentClientId} for Lv4 spell."); return; }
        }
        else { Debug.LogError("[ServerAttackSpawner] PlayerDataManager instance missing for Lv4 spell."); return; }
        // -------------------------------------------
        
        // --- Load Spellcard Resource based on Level ---
        string resourcePath = $"Spellcards/{senderCharacterName}Level{spellLevel}Spellcard";

        // --- Handle Level 4 --- 
        if (spellLevel == 4)
        {
            // Cancellation logic moved to ServerIllusionManager.ServerSpawnLevel4Illusion
            
            Level4SpellcardData level4Data = Resources.Load<Level4SpellcardData>(resourcePath);
            if (level4Data == null)
            {
                Debug.LogError($"[ServerAttackSpawner.ExecuteSpellcard] Failed to load Level4SpellcardData at path: {resourcePath}.");
                return;
            }
            // Delegate spawning to the manager instance
            ServerIllusionManager.Instance?.ServerSpawnLevel4Illusion(level4Data, senderClientId, opponentClientId, opponentPlayerObject, opponentRole);
            return; // Level 4 handled
        }

        // --- Handle Levels 2 & 3 (Delegate to Executor) ---
        _spellcardExecutor?.ExecuteLevel2or3Spellcard(senderClientId, senderCharacterName, spellLevel);
    }

    /// <summary>
    /// **[Server Only]** Triggers a bullet-clearing effect around the player who activated a spellcard.
    /// The radius scales with the spell level.
    /// </summary>
    /// <param name="castingPlayerClientId">The ClientId of the player who cast the spellcard.</param>
    /// <param name="spellLevel">The level (2, 3, or 4) of the spellcard cast.</param>
    public void TriggerSpellcardClear(ulong castingPlayerClientId, int spellLevel)
    {
        if (!IsServer) return;

        // Get caster's player object
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(castingPlayerClientId, out NetworkClient networkClient) || networkClient.PlayerObject == null)
        {
            Debug.LogError($"[ServerAttackSpawner.TriggerSpellcardClear] Could not find player object for client {castingPlayerClientId}");
            return;
        }
        Transform playerTransform = networkClient.PlayerObject.transform;
        Vector3 playerPosition = playerTransform.position;

        // Determine radius based on spell level
        float clearRadius = 0f;
        switch (spellLevel)
        {
            case 2: clearRadius = 3.0f; break; // Tune these values
            case 3: clearRadius = 5.0f; break;
            case 4: clearRadius = 10.0f; break; // Large radius for Lv 4
            default: 
                Debug.LogWarning($"[ServerAttackSpawner.TriggerSpellcardClear] Invalid spell level {spellLevel} for clear effect.");
                return;
        }

        // Determine the role of the player casting the spell
        PlayerRole castingPlayerRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? data = PlayerDataManager.Instance.GetPlayerData(castingPlayerClientId);
            if (data.HasValue) { castingPlayerRole = data.Value.Role; }
        }
        if (castingPlayerRole == PlayerRole.None)
        {
            Debug.LogWarning($"[ServerAttackSpawner.TriggerSpellcardClear] Could not determine PlayerRole for ClientId {castingPlayerClientId}.");
            // Decide if we should proceed with PlayerRole.None or return
        }

        // Find colliders in radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(playerPosition, clearRadius);
        // Debug.Log($"[ServerAttackSpawner.TriggerSpellcardClear] Level {spellLevel} clear triggered by {castingPlayerClientId}. Found {colliders.Length} colliders in radius {clearRadius}.");

        foreach (Collider2D col in colliders)
        {
            // Check if the collider belongs to a NetworkObject with IClearable
            NetworkObject netObj = col.GetComponentInParent<NetworkObject>(); // Use GetComponentInParent for flexibility
            if (netObj != null)
            {
                IClearable[] clearables = netObj.GetComponentsInChildren<IClearable>(true); // Include inactive components
                foreach (IClearable clearable in clearables)
                {
                    // Use forced = true for spellcard clear, similar to deathbomb
                    clearable.Clear(true, castingPlayerRole); 
                    // Debug.Log($"    Cleared {netObj.name} via IClearable component.");
                }
            }
        }
        // TODO: Trigger visual effect via ClientRpc?
    }
} 