using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// **[Server Only]** Server-authoritative singleton service responsible for managing the state
/// of all players' <see cref="SpellBarController"/> instances. 
/// Handles passive fill in its Update loop, active charging based
/// on client input state received via <see cref="PlayerShootingController.UpdateChargeStateServerRpc"/>,
/// and spell cost consumption requests from <see cref="PlayerShootingController.RequestSpellcardServerRpc"/>.
/// </summary>
public class SpellBarManager : NetworkBehaviour
{
    /// <summary>Singleton instance of the SpellBarManager.</summary>
    public static SpellBarManager Instance { get; private set; }

    // Cache keyed by PlayerRole now
    private Dictionary<PlayerRole, SpellBarController> playerSpellBars = new Dictionary<PlayerRole, SpellBarController>();

    /// <summary>
    /// Unity Awake method. Implements the singleton pattern.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SpellBarManager] Duplicate instance detected. Destroying self.", gameObject);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Consider DontDestroyOnLoad(gameObject) if needed across scenes.
        }
    }

    /// <summary>
    /// Called when the NetworkObject spawns. Only executes on the server.
    /// Initializes the spell bar cache by finding all <see cref="SpellBarController"/> instances in the scene.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            InitializeSpellBarCache();

            // TODO: Consider subscribing to NetworkManager connect/disconnect events
            // to dynamically update the playerSpellBars cache if players can join/leave mid-game
            // and SpellBarControllers are added/removed dynamically.
            // NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnect;
            // NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }
    }

    /// <summary>
    /// Called when the NetworkObject despawns. Only executes on the server.
    /// Placeholder for unsubscribing from NetworkManager events if they were added.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            // Unsubscribe from NetworkManager events if subscribed in OnNetworkSpawn
            // if (NetworkManager.Singleton != null)
            // {
            //     NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnect;
            //     NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            // }
        }
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Unity OnDestroy method. Clears the singleton instance if this is the active instance.
    /// </summary>
    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnDestroy();
    }

    /// <summary>
    /// **[Server Only]** Finds all <see cref="SpellBarController"/> instances in the scene
    /// and populates the <see cref="playerSpellBars"/> dictionary cache.
    /// Called by the server during <see cref="OnNetworkSpawn"/>.
    /// </summary>
    private void InitializeSpellBarCache()
    {
        if (!IsServer) return;

        playerSpellBars.Clear();
        SpellBarController[] allSpellBars = FindObjectsOfType<SpellBarController>();
        foreach (SpellBarController bar in allSpellBars)
        {
            // Use the TargetPlayerRole configured on the SpellBarController as the key.
            PlayerRole targetRole = bar.TargetPlayerRole;
            if (targetRole != PlayerRole.None) // Only add bars with a valid role assigned
            {
                if (!playerSpellBars.ContainsKey(targetRole))
                {
                    playerSpellBars.Add(targetRole, bar);
                }
                else
                {
                    Debug.LogWarning($"[SpellBarManager] Duplicate SpellBarController found targeting Role {targetRole}. Using the first instance found.", bar.gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"[SpellBarManager] Found SpellBarController with TargetPlayerRole set to None. It will be ignored.", bar.gameObject);
            }
        }
        // Removed Debug.Log from here
        // // Debug.Log($"[SpellBarManager] Initialized cache with {playerSpellBars.Count} spell bars.");
    }

    // --- Server-Side Update Loop --- 

    /// <summary>
    /// Unity Update method. Only executes on the server.
    /// Calls helper method to apply passive spell bar fill.
    /// </summary>
    void Update()
    {
        // Only run updates on the server
        if (!IsServer) return;

        // Passive Fill Update
        UpdatePassiveFillForAllPlayers();
    }

    /// <summary>
    /// **[Server Only]** Iterates through all currently connected clients,
    /// finds their corresponding <see cref="SpellBarController"/> in the cache,
    /// gets their <see cref="CharacterStats"/>, and applies the passive fill rate to the bar's <see cref="SpellBarController.currentPassiveFill"/> NetworkVariable.
    /// </summary>
    private void UpdatePassiveFillForAllPlayers()
    {
         foreach (var kvp in NetworkManager.Singleton.ConnectedClients) 
         {
             ulong clientId = kvp.Key;
             NetworkClient networkClient = kvp.Value;

             // --- Get PlayerRole from ClientId --- 
             PlayerRole clientRole = PlayerRole.None;
             if (PlayerDataManager.Instance != null)
             {
                 PlayerData? data = PlayerDataManager.Instance.GetPlayerData(clientId);
                 if (data.HasValue) 
                 {
                     clientRole = data.Value.Role;
                 }
             }
             if (clientRole == PlayerRole.None) continue; // Skip if no role found for this client
             // --------------------------------------

             // Find the bar associated with this player's ROLE in our cache
             if (playerSpellBars.TryGetValue(clientRole, out SpellBarController bar))
             {
                 // Get the player's stats for the fill rate
                 if (networkClient.PlayerObject != null)
                 {
                     CharacterStats stats = networkClient.PlayerObject.GetComponent<CharacterStats>();
                     if (stats != null)
                     {
                         // Apply passive fill rate based on character stats
                         float passiveRate = stats.GetPassiveFillRate();
                         float newFill = bar.currentPassiveFill.Value + passiveRate * Time.deltaTime;
                         // Update the NetworkVariable; Netcode handles synchronization to clients.
                         bar.currentPassiveFill.Value = Mathf.Clamp(newFill, 0f, SpellBarController.MaxFillAmount);
                     }
                     else { /* Log warning only if player object exists but lacks stats */ 
                          if (!loggedMissingStatsWarning.Contains(clientId))
                          {
                                Debug.LogWarning($"[SpellBarManager] Client {clientId} PlayerObject exists but is missing CharacterStats. Cannot apply passive fill.");
                                loggedMissingStatsWarning.Add(clientId); // Log once per client
                          }
                     }
                 }
                 // else: Player object not found/spawned yet? Normal during startup or if player disconnected.
             }
             // else: No spell bar found for this client? This might happen if InitializeSpellBarCache hasn't run or failed.
         }
    }
    // Helper set to prevent spamming logs for missing stats
    private HashSet<ulong> loggedMissingStatsWarning = new HashSet<ulong>();


    // --- Public Methods Called by PlayerShootingController RPCs ---

    /// <summary>
    /// **[Server Only]** Updates the active charge state for a specific player based on their input state.
    /// Called via <see cref="PlayerShootingController.UpdateChargeStateServerRpc"/>.
    /// Finds the player's <see cref="SpellBarController"/> and calls its <see cref="SpellBarController.ServerCalculateState"/> method.
    /// </summary>
    /// <param name="clientId">The ClientId of the player whose charge state to update.</param>
    /// <param name="isCharging">Whether the player client reported holding the charge key.</param>
    public void UpdatePlayerActiveCharge(ulong clientId, bool isCharging)
    {
        if (!IsServer) return;

        // --- Get PlayerRole from ClientId --- 
        PlayerRole clientRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? data = PlayerDataManager.Instance.GetPlayerData(clientId);
            if (data.HasValue) 
            {
                clientRole = data.Value.Role;
            }
        }
        if (clientRole == PlayerRole.None)
        {
            Debug.LogWarning($"[SpellBarManager] Could not determine PlayerRole for ClientId {clientId} in UpdatePlayerActiveCharge.");
            return; 
        }
        // --------------------------------------

        // Find the target spell bar using PlayerRole
        if (playerSpellBars.TryGetValue(clientRole, out SpellBarController targetBar))
        {
            // Get Player Stats required for the active charge rate
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient networkClient) && networkClient.PlayerObject != null)
            {
                 CharacterStats stats = networkClient.PlayerObject.GetComponent<CharacterStats>();
                 if (stats != null)
                 {
                    float activeRate = stats.GetActiveChargeRate();
                    // Delegate the state calculation to the SpellBarController instance itself.
                    // It will update its own currentActiveFill NetworkVariable.
                    targetBar.ServerCalculateState(isCharging, activeRate);
                 }
                 else { /* Log warning only if player object exists but lacks stats */ 
                    Debug.LogWarning($"[SpellBarManager] Client {clientId} has PlayerObject but no CharacterStats. Cannot update active charge rate.");
                 }
            }
             else { /* Client or PlayerObject not found, might be disconnecting */
                 // Debug.LogWarning($"[SpellBarManager] Could not find PlayerObject for client {clientId} to get active charge rate.");
             }
        }
        else
        {
            // This might happen briefly during connection/disconnection, or if cache initialization failed.
             Debug.LogWarning($"[SpellBarManager] Could not find SpellBarController for Role {clientRole} to update active charge.");
        }
    }

    /// <summary>
    /// **[Server Only]** Attempts to consume the spell cost from a player's spell bar when a spellcard is requested.
    /// Called by <see cref="PlayerShootingController.RequestSpellcardServerRpc"/>.
    /// Checks if the player has enough <see cref="SpellBarController.currentPassiveFill"/> and deducts the cost if possible.
    /// Also resets the <see cref="SpellBarController.currentActiveFill"/> upon successful cost payment.
    /// </summary>
    /// <param name="clientId">The ClientId of the player attempting to use the spellcard.</param>
    /// <param name="spellLevel">The level of the spellcard being requested (2, 3, or 4).</param>
    /// <returns>True if the cost was successfully deducted, false otherwise.</returns>
    public bool ConsumeSpellCost(ulong clientId, int spellLevel)
    {
        if (!IsServer) return false;

        // --- Get PlayerRole from ClientId --- 
        PlayerRole clientRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? data = PlayerDataManager.Instance.GetPlayerData(clientId);
            if (data.HasValue) 
            {
                clientRole = data.Value.Role;
            }
        }
        if (clientRole == PlayerRole.None)
        {
             Debug.LogWarning($"[SpellBarManager] Could not determine PlayerRole for ClientId {clientId} in ConsumeSpellCost.");
             return false; 
        }
        // --------------------------------------

        // Find the sender's spell bar using PlayerRole
        if (playerSpellBars.TryGetValue(clientRole, out SpellBarController senderBar))
        {
            // Calculate cost based on spell level (Level 2 costs 1, Level 3 costs 2, Level 4 costs 3 segments)
            float cost = (spellLevel - 1) * 1.0f; // Cost is 1.0 per level above 1 on the 0-4 scale

            // Authoritative check: Does the player have enough PASSIVE charge?
            if (senderBar.currentPassiveFill.Value >= cost)
            {
                // Deduct cost from passive fill
                senderBar.currentPassiveFill.Value = Mathf.Max(0f, senderBar.currentPassiveFill.Value - cost);
                // Reset active fill when a spellcard is successfully used
                senderBar.currentActiveFill.Value = 0f;
                return true; // Cost paid successfully
            }
            else
            {
                // Not enough charge - log the failure reason
                // Debug.Log($"[SpellBarManager] Client {clientId} failed spellcard level {spellLevel}: Insufficient passive charge (Needs {cost}, Has {senderBar.currentPassiveFill.Value}).");
                return false; // Failed: Not enough charge
            }
        }
        else
        {
             Debug.LogWarning($"[SpellBarManager] Could not find SpellBarController for Role {clientRole} to consume spell cost.");
             return false;
        }
    }

    // --- Debug Method ---
    /// <summary>
    /// [Server Only] Debug method to instantly set a player's spell bar charge to maximum.
    /// Finds the player's bar by role and sets both passive and active fill NetworkVariables to max.
    /// </summary>
    /// <param name="role">The PlayerRole of the player whose bar to maximize.</param>
    public void SetPlayerChargeToMaxServer(PlayerRole role)
    {
        if (!IsServer) return;

        if (playerSpellBars.TryGetValue(role, out SpellBarController targetBar))
        {
            // Assuming SpellBarController has a constant or property for the max value.
            // If not, replace SpellBarController.MaxFillAmount with 4.0f or the correct max value.
            targetBar.currentPassiveFill.Value = SpellBarController.MaxFillAmount;
            targetBar.currentActiveFill.Value = SpellBarController.MaxFillAmount; // Setting both ensures max regardless of current state
            UnityEngine.Debug.Log($"[SpellBarManager] Debug: Set Player {role} spell bar charge to MAX.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[SpellBarManager] Could not find SpellBarController for Role {role} to set charge to max.");
        }
    }
}
