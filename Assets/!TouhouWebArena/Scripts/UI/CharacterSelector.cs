using UnityEngine;
using Unity.Netcode;
using TMPro; // Use TextMeshPro for UI text
using UnityEngine.SceneManagement; // Needed for LoadSceneMode
using Unity.Collections; // Required for FixedString
using UnityEngine.UI; // Required for Button
using System.Collections.Generic; // Required for List
using System.Collections; // Required for Coroutine
using UnityEngine.EventSystems; // Required for EventSystem
using System.Linq; // Used for FirstOrDefault

/// <summary>
/// Manages the Character Selection screen UI.
/// Handles:
/// - Displaying character buttons.
/// - Listening for player selections (click or keyboard/controller).
/// - Displaying character synopsis panels based on selections (local and opponent).
/// - Sending selection requests to the server.
/// - Handling keyboard/controller navigation and focus.
/// - Triggering the transition to the gameplay scene when both players are ready.
/// </summary>
public class CharacterSelector : NetworkBehaviour
{
    /// <summary>
    /// Maps a UI Button to a character's internal identifier name.
    /// </summary>
    [System.Serializable]
    public struct CharacterButtonMapping
    {
        [Tooltip("The UI Button component for this character.")]
        public Button button;
        [Tooltip("The internal string identifier for the character (e.g., HakureiReimu). Must match CharacterStats and CharacterSynopsisData.")]
        public string characterInternalName;
    }

    [Header("UI References")]
    [Tooltip("List mapping UI buttons to character internal names. Order determines navigation sequence.")]
    [SerializeField] private List<CharacterButtonMapping> characterButtons;
    [Tooltip("Reference to the SynopsisPanelController for Player 1's display area.")]
    [SerializeField] private SynopsisPanelController player1SynopsisPanel;
    [Tooltip("Reference to the SynopsisPanelController for Player 2's display area.")]
    [SerializeField] private SynopsisPanelController player2SynopsisPanel;

    [Header("Input Controller")]
    [Tooltip("Reference to the component handling keyboard/controller navigation.")]
    [SerializeField] private CharacterSelectInputController inputController;

    [Header("Data References")]
    [Tooltip("List of all available CharacterSynopsisData ScriptableObjects. Used to populate the lookup dictionary.")]
    [SerializeField] private List<CharacterSynopsisData> allSynopsisData;

    [Header("Scene Management")]
    [Tooltip("The exact name of the gameplay scene to load after selection is complete.")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    // --- Private Variables ---
    /// <summary>Cached reference to the PlayerDataManager singleton.</summary>
    private PlayerDataManager playerDataManager;
    /// <summary>Fast lookup dictionary mapping character internal names (string) to their CharacterSynopsisData.</summary>
    private Dictionary<string, CharacterSynopsisData> synopsisLookup = new Dictionary<string, CharacterSynopsisData>();
    /// <summary>Cached reference to the SynopsisPanelController associated with the *local* player.</summary>
    private SynopsisPanelController localSynopsisPanel;
    /// <summary>Cached reference to the SynopsisPanelController associated with the *opponent* player.</summary>
    private SynopsisPanelController opponentSynopsisPanel;

    // --- Add reference to the audio component --- 
    [Header("Component References (Optional)")]
    [Tooltip("Optional reference to the component that handles audio feedback.")]
    [SerializeField] private CharacterSelectAudio characterSelectAudio;
    // -----------------------------------------

    /// <summary>
    /// Initializes the synopsisLookup dictionary from the allSynopsisData list.
    /// </summary>
    private void Awake()
    {
        // --- Populate Synopsis Lookup Dictionary ---
        synopsisLookup.Clear();
        foreach (var synopsisData in allSynopsisData)
        {
            if (synopsisData != null && !string.IsNullOrEmpty(synopsisData.internalName) && !synopsisLookup.ContainsKey(synopsisData.internalName))
            {
                synopsisLookup.Add(synopsisData.internalName, synopsisData);
            }
            else
            {
                Debug.LogWarning($"[CharacterSelector] Found duplicate or invalid synopsis data for internal name: {synopsisData?.internalName ?? "NULL"}", this);
            }
        }
    }

    /// <summary>
    /// Called when the NetworkObject is spawned. Handles essential setup:
    /// - Caches PlayerDataManager instance.
    /// - Subscribes to PlayerDataManager.OnPlayerDataUpdated.
    /// - Determines which synopsis panel belongs to the local player vs. the opponent.
    /// - Sets up button listeners.
    /// - Sets initial UI state (selected button, initial synopsis highlight).
    /// - Calls HandlePlayerDataUpdated to display any pre-existing selections.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Ensure PlayerDataManager singleton is ready
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[CharacterSelector] OnNetworkSpawn: PlayerDataManager instance is NULL! Disabling component.", this);
            enabled = false;
            return;
        }
        playerDataManager = PlayerDataManager.Instance; // Assign local cache
        // Debug.Log("[CharacterSelector] OnNetworkSpawn: PlayerDataManager instance found via Singleton.", this);

        // Subscribe to updates AFTER confirming PlayerDataManager exists
        playerDataManager.OnPlayerDataUpdated += HandlePlayerDataUpdated;
        // Debug.Log("[CharacterSelector] OnNetworkSpawn: Subscribed to OnPlayerDataUpdated.", this);

        // --- Determine Local/Opponent Panels ---
        // Moved here from Start - NetworkManager and PlayerData should be more reliable
        if (NetworkManager.Singleton == null)
        {
             Debug.LogError("[CharacterSelector] OnNetworkSpawn: NetworkManager Singleton is NULL! Cannot determine roles.", this);
             enabled = false; 
             return;
        }
        
        PlayerData? localPlayerData = playerDataManager.GetPlayerData(NetworkManager.Singleton.LocalClientId);
        PlayerRole localRole = localPlayerData.HasValue ? localPlayerData.Value.Role : PlayerRole.None;

        if (localRole == PlayerRole.Player1)
        {
            localSynopsisPanel = player1SynopsisPanel;
            opponentSynopsisPanel = player2SynopsisPanel;
            // Debug.Log("[CharacterSelector] Local player is Player 1.", this);
        }
        else if (localRole == PlayerRole.Player2)
        {
            localSynopsisPanel = player2SynopsisPanel;
            opponentSynopsisPanel = player1SynopsisPanel;
             // Debug.Log("[CharacterSelector] Local player is Player 2.", this);
        }
        else // Observer or Undefined
        {
             localSynopsisPanel = null; 
             opponentSynopsisPanel = null;
             // Debug.LogWarning("[CharacterSelector] Local player role is not P1 or P2. Local synopsis updates disabled.", this);
        }

        // Check if panels are assigned before trying to use them
        bool canNavigate = true; // Assume true unless panels are missing
        if (player1SynopsisPanel == null || player2SynopsisPanel == null)
        {
            Debug.LogError("[CharacterSelector] OnNetworkSpawn: Player 1 or Player 2 Synopsis Panel is not assigned in the Inspector! Disabling interaction.", this);
            // Maybe disable buttons or the whole component?
            canNavigate = false;
            // Don't return entirely, still might need to listen for opponent updates if observer?
            // For now, just disable local nav
        }

        // --- Initial UI State & Button Setup ---
        SetupButtonListeners(); // Setup listeners before potentially selecting

        // Ensure the first button is selected visually and update local highlight ONLY if panel is valid
        // Ensure EventSystem.current is not null
        if (EventSystem.current == null)
        {
             Debug.LogError("[CharacterSelector] OnNetworkSpawn: EventSystem.current is NULL! Cannot set initial selection.", this);
        }
        else if (canNavigate && localSynopsisPanel != null && characterButtons.Count > 0 && characterButtons[0].button != null) 
        {
            // Set initial EventSystem selection
            EventSystem.current.SetSelectedGameObject(characterButtons[0].button.gameObject);
            UpdateLocalHighlightSynopsis(0); // Show initial highlight
            // inputController will pick up this selection in its LateUpdate
        }
        else if (characterButtons.Count == 0 || characterButtons[0].button == null)
        {
            Debug.LogWarning("[CharacterSelector] No character buttons assigned or first button is null. Navigation might not work.", this);
        }
        else if (!canNavigate || localSynopsisPanel == null)
        {
             // Debug.LogWarning("[CharacterSelector] Initial button selection skipped (Navigation disabled or Local Panel invalid).", this);
        }

        // Initialize the input controller AFTER buttons are set up and initial selection might be done
        if (inputController != null)
        {
            inputController.Initialize(characterButtons);
            inputController.SetNavigationActive(canNavigate); // Pass initial navigation state
        }
        else
        {
            Debug.LogError("[CharacterSelector] CharacterSelectInputController reference is missing! Keyboard/Controller navigation will not work.", this);
        }

        // Trigger an initial update based on potentially already selected characters
        HandlePlayerDataUpdated(); 
    }

    /// <summary>
    /// Adds listeners to the onClick event of each button in the characterButtons list.
    /// Each listener calls OnCharacterButtonClicked with the corresponding character's internal name.
    /// </summary>
    private void SetupButtonListeners()
    {
        foreach (var mapping in characterButtons)
        {
            if (mapping.button != null && !string.IsNullOrEmpty(mapping.characterInternalName))
            {
                string internalName = mapping.characterInternalName;
                mapping.button.onClick.AddListener(() => OnCharacterButtonClicked(internalName));
            }
        }
    }

    /// <summary>
    /// Called when a character button is clicked. Invokes the ServerRpc to set the character.
    /// </summary>
    /// <param name="characterInternalName">The internal name of the character associated with the clicked button.</param>
    private void OnCharacterButtonClicked(string characterInternalName)
    {
        // Debug.Log($"[CharacterSelector] Button clicked for character: {characterInternalName}");
        RequestSetCharacterServerRpc(characterInternalName);
    }

    /// <summary>
    /// ServerRpc called by a client to request setting their selected character.
    /// Updates the character in PlayerDataManager and checks if both players are now ready.
    /// If both are ready, starts the transition to the gameplay scene.
    /// </summary>
    /// <param name="characterInternalName">The internal name of the character selected by the client.</param>
    /// <param name="rpcParams">Provides information about the sender (client ID).</param>
    [ServerRpc(RequireOwnership = false)]
    private void RequestSetCharacterServerRpc(string characterInternalName, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        // Debug.Log($"[CharacterSelector] ServerRPC RequestSetCharacter: Client {clientId} selected {characterInternalName}", this);

        // Use the locally cached reference
        if (playerDataManager != null)
        {
            playerDataManager.SetPlayerCharacter(clientId, characterInternalName);
            // Check if both players are ready AFTER updating
            if (playerDataManager.AreBothPlayersReady())
            {
                // Disable input navigation via the controller
                inputController?.SetNavigationActive(false);

                // Call the centralized SceneTransitionManager
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.LoadNetworkScene(gameplaySceneName, 1.0f); // Use 1.0f delay as before
                }
                else
                {
                    Debug.LogError("[CharacterSelector] SceneTransitionManager Instance is null! Cannot load gameplay scene.", this);
                    // Fallback? Maybe try direct load if server? Or just log error.
                    // if (IsServer) NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
                }
            }
            else
            {
                // Debug.Log("[CharacterSelector] Server: Not all players ready yet.");
            }
        }
        else 
        {
            Debug.LogError("[CharacterSelector] ServerRPC: PlayerDataManager is NULL! Cannot set character or check readiness.", this);
        }
    }

    /// <summary>
    /// Callback executed when PlayerDataManager signals that player data has changed.
    /// Updates the Player 1 and Player 2 synopsis panels based on the current 
    /// SelectedCharacter values in PlayerDataManager.
    /// Handles cases where data or synopsis lookup might fail.
    /// </summary>
    private void HandlePlayerDataUpdated()
    {
        // Debug.Log("[CharacterSelector] HandlePlayerDataUpdated called.", this);
        if (playerDataManager == null) { /* Error logged in OnNetworkSpawn */ return; }
        if (player1SynopsisPanel == null || player2SynopsisPanel == null) { /* Error logged in OnNetworkSpawn */ return; } 

        Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: Updating Synopsis Panels...", this);

        // Loop through all connected clients (usually 2 players)
        foreach (var clientData in NetworkManager.Singleton.ConnectedClients.Values)
        {
            // Debug.Log($"[CharacterSelector] Processing client ID: {clientData.ClientId}", this);
            PlayerData? playerData = playerDataManager.GetPlayerData(clientData.ClientId);

            if (playerData.HasValue)
            {
                // Debug.Log($"[CharacterSelector] PlayerData found for client {clientData.ClientId}. Character: '{playerData.Value.SelectedCharacter}', Role: {playerData.Value.Role}", this);
                SynopsisPanelController targetPanel = null;

                // Determine which panel to update based on the player's role
                if (playerData.Value.Role == PlayerRole.Player1)
                {
                    targetPanel = player1SynopsisPanel;
                    // Debug.Log($"[CharacterSelector] Updating Player 1 panel.", this);
                }
                else if (playerData.Value.Role == PlayerRole.Player2)
                {
                    targetPanel = player2SynopsisPanel;
                    // Debug.Log($"[CharacterSelector] Updating Player 2 panel.", this);
                }
                // else: Handle Observers or unassigned roles if necessary

                if (targetPanel != null)
                {
                    if (!string.IsNullOrEmpty(playerData.Value.SelectedCharacter.ToString()))
                    {
                        if (synopsisLookup.TryGetValue(playerData.Value.SelectedCharacter.ToString(), out CharacterSynopsisData dataToDisplay))
                        {
                            targetPanel.UpdateDisplay(dataToDisplay, playerData.Value.Role);
                            // Debug.Log($"[CharacterSelector] Synopsis lookup for P{(playerData.Value.Role == PlayerRole.Player1 ? 1:2)} ('{playerData.Value.SelectedCharacter}') result: Found=True, Data={dataToDisplay.characterName}", this);
                        }
                        else
                        {
                            // Character selected but no synopsis found (should not happen if data is set up correctly)
                            targetPanel.gameObject.SetActive(false);
                            Debug.LogWarning($"[CharacterSelector] Synopsis data not found for character: {playerData.Value.SelectedCharacter}. Hiding panel for P{(playerData.Value.Role == PlayerRole.Player1 ? 1:2)}.", this);
                        }
                    }
                    else
                    {
                        // No character selected yet for this player, ensure panel is hidden
                        targetPanel.gameObject.SetActive(false);
                        // Debug.Log($"[CharacterSelector] P{(playerData.Value.Role == PlayerRole.Player1 ? 1:2)} SelectedCharacter is null or empty. Set Panel Active=False. Hier={targetPanel.gameObject.activeInHierarchy}", this);
                    }
                }
                else
                {
                    // Debug.LogWarning($"[CharacterSelector] Target panel is null for player role: {playerData.Value.Role}. Cannot update synopsis.", this);
                }
            }
            else
            {
                // Debug.LogWarning($"[CharacterSelector] PlayerData not found for client ID: {clientData.ClientId}. Cannot update synopsis.", this);
            }
        }
    }

    /// <summary>
    /// Called when the NetworkObject is despawned. Unsubscribes from events.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (playerDataManager != null)
        {
            playerDataManager.OnPlayerDataUpdated -= HandlePlayerDataUpdated;
            // Debug.Log("[CharacterSelector] OnNetworkDespawn: Unsubscribed from OnPlayerDataUpdated.");
        }
        // Reset cached references if needed
        playerDataManager = null;
        localSynopsisPanel = null;
        opponentSynopsisPanel = null;
        synopsisLookup.Clear(); // Clear dictionary on despawn
    }

    /// <summary>
    /// Standard Unity Start method. Currently empty as most initialization is in OnNetworkSpawn.
    /// </summary>
    private void Start()
    {
        // Initialization moved to OnNetworkSpawn for network readiness
        // Null check for audio source moved here as Awake/OnNetworkSpawn might be too early for components on other objects
        if (characterSelectAudio == null)
        {
            // Debug.LogWarning("[CharacterSelector] CharacterSelectAudio not assigned. No sound effects will play.", this); // Less noisy
        }
    }

    /// <summary>
    /// Updates the *local* player's synopsis panel to show the character 
    /// corresponding to the given button index.
    /// Called when navigating with keyboard/controller before confirming.
    /// Invoked by CharacterSelectInputController.OnNavigate event.
    /// </summary>
    /// <param name="index">The index in the characterButtons list to display.</param>
    public void UpdateLocalHighlightSynopsis(int index)
    {
        if (localSynopsisPanel == null) 
        {
            // Debug.LogWarning("[CharacterSelector] UpdateLocalHighlightSynopsis: Local panel is null, cannot update.", this);
            return;
        }
        if (index < 0 || index >= characterButtons.Count)
        {
            Debug.LogError($"[CharacterSelector] UpdateLocalHighlightSynopsis: Index {index} out of range.", this);
            return;
        }

        string internalName = characterButtons[index].characterInternalName;
        if (synopsisLookup.TryGetValue(internalName, out CharacterSynopsisData synopsisData))
        {    
            if (characterButtons[index].button != null && !string.IsNullOrEmpty(characterButtons[index].characterInternalName))
            {
                if (synopsisLookup.TryGetValue(internalName, out CharacterSynopsisData dataToDisplay))
                {
                    // Debug.Log($"[CharacterSelector] Highlighting character: {dataToDisplay.characterName}", this);
                    localSynopsisPanel.UpdateDisplay(dataToDisplay, PlayerRole.None); // PlayerRole.None signifies a highlight, not a lock-in
                    localSynopsisPanel.gameObject.SetActive(true); // CS1061 Fix: Ensure it's visible for highlighting
                }
                else
                {
                    Debug.LogWarning($"[CharacterSelector] Synopsis data not found for highlight: {internalName}", this);
                    localSynopsisPanel.gameObject.SetActive(false); // CS1061 Fix: Hide if no data
                }
            }
            else
            {
                Debug.LogWarning($"[CharacterSelector] Invalid button or internal name at index {index} for highlight.", this);
                localSynopsisPanel.gameObject.SetActive(false); // CS1061 Fix: Hide if button/data is invalid
            }
        }
        else
        {
             Debug.LogWarning($"[CharacterSelector] UpdateLocalHighlightSynopsis: Synopsis data not found for internal name '{internalName}'. Hiding local panel.", this);
            localSynopsisPanel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Public method called by the CharacterSelectInputController.OnConfirm event.
    /// Plays the confirm sound and executes the selection logic.
    /// </summary>
    public void HandleConfirmInput()
    {
        characterSelectAudio?.PlayConfirmSound();
        ExecuteConfirmSelection();
    }

    /// <summary>
    /// Performs the actual logic for confirming a selection based on the 
    /// current state tracked by the input controller (implicitly via EventSystem). 
    /// Calls the ServerRpc to set the character choice for the local player.
    /// </summary>
    private void ExecuteConfirmSelection()
    {
        GameObject currentSelectedButton = EventSystem.current.currentSelectedGameObject;
        if (currentSelectedButton != null)
        {
            CharacterButtonMapping? mapping = characterButtons.FirstOrDefault(cb => cb.button != null && cb.button.gameObject == currentSelectedButton);
            if (mapping.HasValue && !string.IsNullOrEmpty(mapping.Value.characterInternalName))
            {
                string selectedCharName = mapping.Value.characterInternalName;
                // Debug.Log($"[CharacterSelector] Confirming selection: {selectedCharName}", this);
                RequestSetCharacterServerRpc(selectedCharName);
            }
            else
            {
                Debug.LogWarning("[CharacterSelector] ExecuteConfirmSelection: Current selected UI element is not a valid character button or mapping not found.", this);
            }
        }
        else
        {
            Debug.LogWarning("[CharacterSelector] ExecuteConfirmSelection: No UI element currently selected by EventSystem.", this);
        }
    }
} 