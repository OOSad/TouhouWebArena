using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TouhouWebArena;
using TouhouWebArena.Spellcards;
using TouhouWebArena.Spellcards.Behaviors;
using System.Collections.Generic;
using Unity.Collections; // For FixedString

/// <summary>
/// **[Server Only]** Server-authoritative singleton service responsible for spawning all player-related projectiles
/// (basic shots, charge attacks, spellcards) based on requests received from <see cref="PlayerShootingController"/> RPCs.
/// Interacts with helper classes like <see cref="ServerBasicShotSpawner"/>, <see cref="ServerChargeAttackSpawner"/>,
/// <see cref="ServerIllusionManager"/>, and <see cref="ServerBulletConfigurer"/>.
/// For Level 4 spellcards, it handles the initial spawning of illusion prefabs, which are then managed by `ServerIllusionOrchestrator`.
/// </summary>
[RequireComponent(typeof(ServerSpellcardActionRunner))]
public class ServerAttackSpawner : NetworkBehaviour
{
    /// <summary>Singleton instance of the ServerAttackSpawner.</summary>
    public static ServerAttackSpawner Instance { get; private set; }

    // --- Spawner Instances ---
    private ServerChargeAttackSpawner _chargeAttackSpawner;
    // -----------------------

    // Constant vertical offset from player center for spawning basic shot pairs.

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
            _chargeAttackSpawner = new ServerChargeAttackSpawner();
            
        }
    }

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

    // --- Public Methods Called by PlayerShootingController RPCs ---

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

        if (spellLevel == 4)
        {
            // --- NEW LOGIC FOR LEVEL 4 --- 
            string lv4ResourcePath = $"Spellcards/{senderCharacterName}Level{spellLevel}Spellcard"; 
            Level4SpellcardData lv4Data = Resources.Load<Level4SpellcardData>(lv4ResourcePath);

            if (lv4Data == null)
            {
                Debug.LogError($"[ServerAttackSpawner] Failed to load Level4SpellcardData from path: {lv4ResourcePath} for character {senderCharacterName}");
                return;
            }

            if (lv4Data.IllusionPrefab == null)
            {
                Debug.LogError($"[ServerAttackSpawner] IllusionPrefab is null in Level4SpellcardData: {lv4ResourcePath}");
                return;
            }

            // --- Determine Spawn Position for Illusion ---
            Vector3 illusionSpawnPosition = Vector3.zero; 
            Quaternion illusionSpawnRotation = Quaternion.identity; 

            // TODO: Replace this with your actual logic to get the opponent's side spawn point.
            // This likely involves your SpawnAreaManager and the opponentClientId.
            // Example (you'll need to adapt this to your SpawnAreaManager's API):
            /*
            if (PlayerDataManager.Instance != null && SpawnAreaManager.Instance != null) // Ensure SpawnAreaManager.Instance exists and is accessible
            {
                PlayerData? opponentData = PlayerDataManager.Instance.GetPlayerData(opponentClientId);
                if (opponentData.HasValue)
                {
                    // Assuming SpawnAreaManager has a method that takes a PlayerRole 
                    // and returns a suitable spawn point (e.g., top-center of their field)
                    // illusionSpawnPosition = SpawnAreaManager.Instance.GetIllusionSpawnPoint(opponentData.Value.Role); 
                }
                else
                {
                    Debug.LogError($"[ServerAttackSpawner] Could not get PlayerData for opponent {opponentClientId} to determine illusion spawn point.");
                }
            }
            else
            {
                Debug.LogWarning("[ServerAttackSpawner] PlayerDataManager or SpawnAreaManager instance not available for illusion spawn positioning.");
            }
            */
            // As a TEMPORARY placeholder if SpawnAreaManager logic isn't ready:
            // This simplistic example assumes player fields are centered around x=0 and tries to place on left/right.
            // You WILL need to replace this with proper logic from SpawnAreaManager.
            float spawnX = 5.0f; // Default for one side
            if (PlayerDataManager.Instance != null) {
                PlayerData? casterPlayerData = PlayerDataManager.Instance.GetPlayerData(senderClientId);
                // If caster is P1 (typically on left), spawn illusion on P2's side (right).
                if (casterPlayerData.HasValue && casterPlayerData.Value.Role == PlayerRole.Player1) {
                    spawnX = 5.0f; // Example X for Player 2's side
                } 
                // If caster is P2 (typically on right), spawn illusion on P1's side (left).
                else if (casterPlayerData.HasValue && casterPlayerData.Value.Role == PlayerRole.Player2) {
                    spawnX = -5.0f; // Example X for Player 1's side
                }
            }
            illusionSpawnPosition = new Vector3(spawnX, 3.0f, 0); // Example Y and Z. Adjust Y based on your field layout.
            // --- End TEMPORARY Placeholder ---

            // LEVEL 4 SPELLCARD: SPAWN ILLUSION
            if (lv4Data.IllusionPrefab == null)
            {
                Debug.LogError($"[ServerAttackSpawner] Level 4 Spellcard '{lv4ResourcePath}' has no IllusionPrefab assigned.");
                return;
            }

            // --- Logic to Despawn Existing Enemy Illusion --- 
            ServerIllusionOrchestrator[] allIllusions = FindObjectsByType<ServerIllusionOrchestrator>(FindObjectsSortMode.None);
            foreach (ServerIllusionOrchestrator existingIllusionOrchestrator in allIllusions)
            {
                if (existingIllusionOrchestrator != null && existingIllusionOrchestrator.IsSpawned && existingIllusionOrchestrator.TargetPlayerId == senderClientId)
                {
                    if (existingIllusionOrchestrator.NetworkObject.OwnerClientId != senderClientId)
                    {
                        Debug.Log($"[ServerAttackSpawner] Player {senderClientId} is casting a Level 4. Despawning enemy illusion (Owner: {existingIllusionOrchestrator.NetworkObject.OwnerClientId}, Target: {existingIllusionOrchestrator.TargetPlayerId}) that was targeting them.");
                        existingIllusionOrchestrator.DespawnIllusion();
                        break; 
                    }
                }
            }
            // --- End Despawn Logic ---

            GameObject illusionInstance = Instantiate(lv4Data.IllusionPrefab, illusionSpawnPosition, illusionSpawnRotation); 
            
            NetworkObject illusionNetworkObject = illusionInstance.GetComponent<NetworkObject>();
            if (illusionNetworkObject == null)
            {
                Debug.LogError($"[ServerAttackSpawner] IllusionPrefab '{lv4Data.IllusionPrefab.name}' is missing a NetworkObject component.");
                Destroy(illusionInstance);
                return;
            }

            // MODIFIED: Spawn with ownership assigned to the casting player
            illusionNetworkObject.SpawnWithOwnership(senderClientId);
            // The ServerIllusionOrchestrator on the prefab will then manage the illusion's lifecycle and attacks.
            // --- END MODIFICATION ---

            ServerIllusionOrchestrator orchestrator = illusionInstance.GetComponent<ServerIllusionOrchestrator>();
            if (orchestrator == null)
            {
                Debug.LogError($"[ServerAttackSpawner] IllusionPrefab '{lv4Data.IllusionPrefab.name}' is missing ServerIllusionOrchestrator component.");
                // Optional: Despawn if critical, though client view might still exist if orchestrator is missing.
                // illusionNetworkObject.Despawn(); 
                // Destroy(illusionInstance);
                return; // Return or log, initialization will fail on orchestrator anyway
            }

            orchestrator.Initialize(lv4ResourcePath, opponentClientId); // Pass opponentId as the target
            Debug.Log($"[ServerAttackSpawner] Initialized Level 4 Illusion ({lv4ResourcePath}) for {senderCharacterName} targeting player {opponentClientId}.");
        }
        else if (spellLevel == 2 || spellLevel == 3)
        {
            // --- EXISTING LOGIC FOR LEVEL 2/3 --- 
            string resourcePath = $"Spellcards/{senderCharacterName}Level{spellLevel}Spellcard";
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
            }
            else
            {
                Debug.LogWarning($"[ServerAttackSpawner] Failed to load SpellcardData from {resourcePath} for Lv{spellLevel}. Random offset will be zero.");
            }

            if (_spellcardNetworkHandler == null)
            {
                Debug.LogError($"[ServerAttackSpawner] SpellcardNetworkHandler Instance is null! Cannot send ExecuteSpellcardClientRpc for Lv{spellLevel}.");
                return;
            }

            ClientRpcParams clientRpcParams = new ClientRpcParams
                { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds } };
            // Send the RPC
            // Assuming targetPlayerId is needed by the client to know who is being attacked
            _spellcardNetworkHandler.ExecuteSpellcardClientRpc(
                senderClientId, // Caster
                opponentClientId, // Target
                // SWAPPED spellLevel and resourcePath to fix argument mismatch error
                resourcePath, // Argument 3: Path (string, likely converted to FixedString internally or receiver handles string)
                spellLevel, // Argument 4: Level (int)
                calculatedOffset, // The random offset
                clientRpcParams
            );
            Debug.Log($"[ServerAttackSpawner] Sent ExecuteSpellcardClientRpc for Lv{spellLevel} from {senderClientId} targeting {opponentClientId}. Path: {resourcePath}, Offset: {calculatedOffset}");
        }
        else
        {
            Debug.LogWarning($"[ServerAttackSpawner] ExecuteSpellcard called with unhandled spell level: {spellLevel}");
        }
    }

    /// <summary>
    /// Server-side logic to trigger a positional clear effect around the caster.
    /// Calculates the clear radius based on the spell level and sends an RPC 
    /// to all clients (<see cref="ClientSpellcardExecutor.TriggerLocalClearEffectClientRpc"/>) 
    /// to execute the clear locally.
    /// </summary>
    /// <param name="castingPlayerClientId">The ClientId of the player who cast the spellcard.</param>
    /// <param name="spellLevel">The level (2, 3, or 4) of the spellcard cast.</param>
    public void TriggerSpellcardClear(ulong castingPlayerClientId, int spellLevel)
    {
        if (!IsServer) return;

        PlayerData? casterData = PlayerDataManager.Instance?.GetPlayerData(castingPlayerClientId);
        if (!casterData.HasValue)
        {
            Debug.LogError($"[ServerAttackSpawner.TriggerSpellcardClear] Could not get PlayerData for caster {castingPlayerClientId}. Cannot determine clear center or role.");
            return;
        }
        PlayerRole casterRole = casterData.Value.Role;

        NetworkObject casterNO = NetworkManager.Singleton.ConnectedClients[castingPlayerClientId].PlayerObject;
        if (casterNO == null)
        {
            Debug.LogError($"[ServerAttackSpawner.TriggerSpellcardClear] Could not find NetworkObject for caster {castingPlayerClientId}. Cannot determine clear center.");
                return;
        }
        Vector3 clearCenter = casterNO.transform.position; 

        // --- MODIFIED: Radius based on spell level ---
        float clearRadius;
        switch (spellLevel)
        {
            case 2:
                clearRadius = 2.5f; // Example radius for Level 2
                break;
            case 3:
                clearRadius = 5.0f; // Example radius for Level 3
                break;
            case 4:
                clearRadius = 10.0f; // Example radius for Level 4 (large screen clear)
                break;
            default:
                Debug.LogWarning($"[ServerAttackSpawner.TriggerSpellcardClear] Unknown spell level {spellLevel}. Defaulting radius to 2.5f.");
                clearRadius = 2.5f;
                break;
        }
        // --- END MODIFICATION ---

        bool forceClear = (spellLevel == 4); 
        // Currently, the client-side clear for bullets doesn't use 'forceClear' specifically, 
        // but fairies/spirits are always force-cleared.
        // This 'forceClear' flag could be used more extensively on the client if needed.

        // Debug.Log($"[ServerAttackSpawner.TriggerSpellcardClear] Triggering clear for Caster: {casterRole}, Center: {clearCenter}, Radius: {clearRadius}, Level: {spellLevel}, Force: {forceClear}");

        if (ClientSpellcardExecutor.Instance != null)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds } };
            ClientSpellcardExecutor.Instance.TriggerLocalClearEffectClientRpc(clearCenter, clearRadius, casterRole);
            // Debug.Log("[ServerAttackSpawner.TriggerSpellcardClear] Sent TriggerLocalClearEffectClientRpc to all clients.");
        }
        else
        {
            Debug.LogError("[ServerAttackSpawner.TriggerSpellcardClear] ClientSpellcardExecutor.Instance is null! Cannot send RPC.");
                }
            }

    // Method to get the spawn center for the OPPONENT of the caster
} 