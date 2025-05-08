using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TouhouWebArena;
using TouhouWebArena.Spellcards;
using System.Collections.Generic;
using Unity.Collections; // For FixedString

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
    // private ServerBasicShotSpawner _basicShotSpawner; // REMOVED
    private ServerChargeAttackSpawner _chargeAttackSpawner;
    // private ServerSpellcardExecutor _spellcardExecutor; // REMOVED - Logic moved to client
    // private ServerSpellcardActionRunner _actionRunner; // REMOVED - Logic moved to client
    // Add fields for other spawners/executors later
    // -----------------------

    // --- Active Illusion Tracking (Server Only) --- Removed, handled by ServerIllusionManager
    // private Dictionary<ulong, NetworkObject> _activeIllusionsTargetingPlayer = new Dictionary<ulong, NetworkObject>();
    // private Dictionary<ulong, NetworkObject> _activeIllusionsCastByPlayer = new Dictionary<ulong, NetworkObject>();
    // ---------------------------------------------

    // Constant vertical offset from player center for spawning basic shot pairs.
    // private const float firePointVerticalOffset = 0.5f; // Moved to ServerBasicShotSpawner

    [Header("Dependencies")]
    [Tooltip("Drag the GameObject with the SpellcardNetworkHandler component here.")]
    public SpellcardNetworkHandler SpellcardNetworkHandlerInstance; // MODIFIED: Public field

    private SpellcardNetworkHandler _spellcardNetworkHandler; // ADDED

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
            // _basicShotSpawner = new ServerBasicShotSpawner(); // REMOVED
            _chargeAttackSpawner = new ServerChargeAttackSpawner();
            
            // REMOVED: Get SpellcardNetworkHandler instance from Awake
            // _spellcardNetworkHandler = SpellcardNetworkHandler.Instance;
            // if (_spellcardNetworkHandler == null)
            // {
            //     Debug.LogError("[ServerAttackSpawner] Could not find SpellcardNetworkHandler Instance! Spellcards might not execute on clients.", gameObject);
            // }
        }
    }

    // ADDED Start() method to initialize _spellcardNetworkHandler
    private void Start()
    {
        if (IsServer) // Only server needs to interact with these server-side handlers
        {
            // MODIFIED: Prioritize Inspector-assigned instance
            if (SpellcardNetworkHandlerInstance != null)
            {
                _spellcardNetworkHandler = SpellcardNetworkHandlerInstance;
            }
            else
            {
                Debug.LogWarning("[ServerAttackSpawner] SpellcardNetworkHandlerInstance not assigned in Inspector. Attempting to use Singleton.Instance.", gameObject);
                _spellcardNetworkHandler = SpellcardNetworkHandler.Instance;
            }
            
            if (_spellcardNetworkHandler == null)
            {
                Debug.LogError("[ServerAttackSpawner] Could not find SpellcardNetworkHandler Instance in Start! Spellcards might not execute on clients.", gameObject);
            }
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

    // REMOVED SpawnBasicShot method
    // /// <summary>
    // /// **[Server Only]** Spawns a pair of basic shot bullets for the requesting player.
    // /// Called via <see cref="PlayerShootingController.RequestFireServerRpc"/>.
    // /// </summary>
    // /// <param name="requesterClientId">The ClientId of the player who requested the shot.</param>
    // public void SpawnBasicShot(ulong requesterClientId)
    // {
    //     // Delegate to the specialized spawner
    //     _basicShotSpawner?.SpawnBasicShot(requesterClientId); 
    // }

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

        // --- Trigger Caster Clear Effect (Server-Side) & Banner (Client-Side) --- 
        // It's important to clear around the caster *before* potentially spawning new bullets.
        TriggerSpellcardClear(senderClientId, spellLevel); 
        
        PlayerData? casterData = PlayerDataManager.Instance?.GetPlayerData(senderClientId);
        if (casterData.HasValue && !string.IsNullOrEmpty(casterData.Value.SelectedCharacter.ToString()))
        {
            if (SpellcardBannerDisplay.Instance != null)
            {
                 ClientRpcParams bannerParams = new ClientRpcParams
                 { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds } };
                SpellcardBannerDisplay.Instance.ShowBannerClientRpc(casterData.Value.Role, casterData.Value.SelectedCharacter.ToString(), bannerParams);
                Debug.Log($"[ServerAttackSpawner] Sent ShowBannerClientRpc for {casterData.Value.Role} ({casterData.Value.SelectedCharacter})");
            }
            else { Debug.LogWarning("[ServerAttackSpawner] SpellcardBannerDisplay Instance is null. Cannot show banner."); }
        }
        else { Debug.LogWarning($"[ServerAttackSpawner] Could not get valid caster data or character name for ClientId {senderClientId}. Cannot show banner."); }
        // ----------------------------------------

        // --- Find Opponent ---
        ulong opponentClientId = ulong.MaxValue;
        // NetworkObject opponentPlayerObject = null; // May not be needed here anymore if only ID is passed
        foreach (var connectedClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (connectedClient.ClientId != senderClientId)
            {
                opponentClientId = connectedClient.ClientId;
                // opponentPlayerObject = connectedClient.PlayerObject; // Store if needed for future server logic
                break;
            }
        }
        if (opponentClientId == ulong.MaxValue)
        {
            Debug.LogWarning($"[ServerAttackSpawner.ExecuteSpellcard] Could not find opponent for client {senderClientId}. Cannot execute spellcard.");
            return; // Still need an opponent to target
        }
        // --- Determine Opponent Role ---
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? opponentData = PlayerDataManager.Instance.GetPlayerData(opponentClientId);
            if (opponentData.HasValue)
            {
                // opponentRole = opponentData.Value.Role;
            }
            else { Debug.LogError($"[ServerAttackSpawner] Could not get PlayerData for opponent {opponentClientId} for Lv4 spell."); return; }
        }
        else { Debug.LogError("[ServerAttackSpawner] PlayerDataManager instance missing for Lv4 spell."); return; }
        // -------------------------------------------

        // --- Load Spellcard Resource Path ---
        string resourcePath = $"Spellcards/{senderCharacterName}Level{spellLevel}Spellcard";

        // --- LOAD SPELLCARD DATA ON SERVER --- (Needed for random offset calculation)
        Vector2 calculatedOffset = Vector2.zero;
        SpellcardData spellDataForOffset = Resources.Load<SpellcardData>(resourcePath);
        if (spellDataForOffset != null)
        {
            if (spellDataForOffset.actions != null && spellDataForOffset.actions.Count > 0 && spellDataForOffset.actions[0].applyRandomSpawnOffset)
            {
                SpellcardAction firstAction = spellDataForOffset.actions[0];
                float randomX = (firstAction.randomOffsetMin.x == firstAction.randomOffsetMax.x) ? firstAction.randomOffsetMin.x : Random.Range(firstAction.randomOffsetMin.x, firstAction.randomOffsetMax.x);
                float randomY = (firstAction.randomOffsetMin.y == firstAction.randomOffsetMax.y) ? firstAction.randomOffsetMin.y : Random.Range(firstAction.randomOffsetMin.y, firstAction.randomOffsetMax.y);
                calculatedOffset = new Vector2(randomX, randomY); 
            }
            // Note: Currently assumes only SpellcardData. Add check for Level4SpellcardData if needed.
        }
        else
        { 
            // Attempt to load as Level4SpellcardData if the first load failed or if level is 4
            // (We might need Level 4 data for other reasons later too)
             Level4SpellcardData level4DataForOffset = Resources.Load<Level4SpellcardData>(resourcePath);
            if (level4DataForOffset != null)
            {
                 // Decide if/how Level 4 spellcards use random offset. 
                 // Maybe they have an offset defined directly, or use their first embedded action?
                 // For now, assume Level 4 doesn't use this shared offset mechanism unless explicitly designed.
                 // calculatedOffset = ... logic based on level4DataForOffset if needed ...
            }
            else
            {   
                 Debug.LogWarning($"[ServerAttackSpawner] Failed to load any spellcard data from {resourcePath} on server. Cannot determine random offset.");
            }
        }
        // ------------------------------------

        if (_spellcardNetworkHandler == null)
        {
            Debug.LogError("[ServerAttackSpawner] SpellcardNetworkHandler is null (neither Inspector-assigned nor Singleton.Instance found). Cannot send ExecuteSpellcardClientRpc.");
            return;
        }

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds } 
        };
        
        _spellcardNetworkHandler.ExecuteSpellcardClientRpc(
            senderClientId, 
            opponentClientId, 
            new FixedString512Bytes(resourcePath),
            spellLevel,
            calculatedOffset, // Pass the calculated offset
            clientRpcParams
        );
        Debug.Log($"[ServerAttackSpawner] Sent ExecuteSpellcardClientRpc for Lv{spellLevel} from {senderClientId} targeting {opponentClientId}. Path: {resourcePath}, Offset: {calculatedOffset}"); // Log the offset
        
        // --- REMOVED OLD EXECUTION LOGIC --- 
        // REMOVED: Level 4 handling (ServerIllusionManager call)
        // REMOVED: Level 2/3 handling (_spellcardExecutor call)
        // -----------------------------------
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