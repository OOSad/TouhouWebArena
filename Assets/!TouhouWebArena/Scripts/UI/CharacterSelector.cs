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
        Debug.Log("[CharacterSelector] OnNetworkSpawn: PlayerDataManager instance found via Singleton.", this);

        // Subscribe to updates AFTER confirming PlayerDataManager exists
        playerDataManager.OnPlayerDataUpdated += HandlePlayerDataUpdated;
        Debug.Log("[CharacterSelector] OnNetworkSpawn: Subscribed to OnPlayerDataUpdated.", this);

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
            Debug.Log("[CharacterSelector] Local player is Player 1.", this);
        }
        else if (localRole == PlayerRole.Player2)
        {
            localSynopsisPanel = player2SynopsisPanel;
            opponentSynopsisPanel = player1SynopsisPanel;
             Debug.Log("[CharacterSelector] Local player is Player 2.", this);
        }
        else // Observer or Undefined
        {
             localSynopsisPanel = null; 
             opponentSynopsisPanel = null;
             Debug.LogWarning("[CharacterSelector] Local player role is not P1 or P2. Local synopsis updates disabled.", this);
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
             Debug.LogWarning("[CharacterSelector] Initial button selection skipped (Navigation disabled or Local Panel invalid).", this);
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
        Debug.Log($"[CharacterSelector] Button clicked for character: {characterInternalName}");
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
        Debug.Log("[CharacterSelector] HandlePlayerDataUpdated called.", this);
        if (playerDataManager == null) { /* Error logged in OnNetworkSpawn */ return; }
        if (player1SynopsisPanel == null || player2SynopsisPanel == null) { /* Error logged in OnNetworkSpawn */ return; } 

        Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: Updating Synopsis Panels...", this);

        // --- Update Player 1 --- 
        PlayerData? p1Data = playerDataManager.GetPlayer1Data();
        CharacterSynopsisData p1Synopsis = null;
        string p1SelectedChar = ""; // Initialize
        if (p1Data.HasValue)
        {
            p1SelectedChar = p1Data.Value.SelectedCharacter.ToString();
            Debug.Log($"[CharacterSelector] HandlePlayerDataUpdated: P1 Data found. SelectedCharacter = '{p1SelectedChar}'", this); // LOG Character Name
            if (!string.IsNullOrEmpty(p1SelectedChar))
            {
                bool foundP1 = synopsisLookup.TryGetValue(p1SelectedChar, out p1Synopsis);
                Debug.Log($"[CharacterSelector] HandlePlayerDataUpdated: Synopsis lookup for P1 ('{p1SelectedChar}') result: Found={foundP1}, Data={(p1Synopsis != null ? p1Synopsis.name : "NULL")}", this); // LOG Lookup Result
            }
            else {
                 Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: P1 SelectedCharacter is null or empty.", this); // LOG Empty Selection
            }
        } else {
             Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: P1 Data is NULL.", this); // LOG No P1 Data
        }
        if (player1SynopsisPanel)
        {
             player1SynopsisPanel.gameObject.SetActive(p1Synopsis != null);
             Debug.Log($"[CharacterSelector] Set P1 Panel Active={player1SynopsisPanel.gameObject.activeSelf}. Hier={player1SynopsisPanel.gameObject.activeInHierarchy}", this);
             if (player1SynopsisPanel.gameObject.activeSelf)
             {
                 player1SynopsisPanel.UpdateDisplay(p1Synopsis);
             }
        } else { Debug.LogError("[CharacterSelector] player1SynopsisPanel reference is NULL!", this); }
        
        // --- Update Player 2 --- 
        PlayerData? p2Data = playerDataManager.GetPlayer2Data();
        CharacterSynopsisData p2Synopsis = null;
        string p2SelectedChar = ""; // Initialize
         if (p2Data.HasValue)
        {
            p2SelectedChar = p2Data.Value.SelectedCharacter.ToString();
             Debug.Log($"[CharacterSelector] HandlePlayerDataUpdated: P2 Data found. SelectedCharacter = '{p2SelectedChar}'", this); // LOG Character Name
             if (!string.IsNullOrEmpty(p2SelectedChar))
            {
                 bool foundP2 = synopsisLookup.TryGetValue(p2SelectedChar, out p2Synopsis);
                 Debug.Log($"[CharacterSelector] HandlePlayerDataUpdated: Synopsis lookup for P2 ('{p2SelectedChar}') result: Found={foundP2}, Data={(p2Synopsis != null ? p2Synopsis.name : "NULL")}", this); // LOG Lookup Result
            }
             else {
                 Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: P2 SelectedCharacter is null or empty.", this); // LOG Empty Selection
             }
        } else {
             Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: P2 Data is NULL.", this); // LOG No P2 Data
        }
        if (player2SynopsisPanel)
        {
            player2SynopsisPanel.gameObject.SetActive(p2Synopsis != null);
            Debug.Log($"[CharacterSelector] Set P2 Panel Active={player2SynopsisPanel.gameObject.activeSelf}. Hier={player2SynopsisPanel.gameObject.activeInHierarchy}", this);
            if (player2SynopsisPanel.gameObject.activeSelf)
            {
                player2SynopsisPanel.UpdateDisplay(p2Synopsis);
        }
        } else { Debug.LogError("[CharacterSelector] player2SynopsisPanel reference is NULL!", this); }
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
            Debug.Log("[CharacterSelector] OnNetworkDespawn: Unsubscribed from OnPlayerDataUpdated.");
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
        // Navigation active check is handled by the InputController before invoking the event
        if (localSynopsisPanel == null) 
        { 
            // Debug.LogWarning("[CharacterSelector] UpdateLocalHighlightSynopsis: Local panel is null, cannot update highlight.");
            return; // Cannot update if local panel isn't determined or assigned
        }
        if (index < 0 || index >= characterButtons.Count)
        {
            Debug.LogError($"[CharacterSelector] UpdateLocalHighlightSynopsis: Index {index} out of range.", this);
            return;
        }

        string internalName = characterButtons[index].characterInternalName;
        if (synopsisLookup.TryGetValue(internalName, out CharacterSynopsisData synopsisData))
        {    
            // Only show if not null
            localSynopsisPanel.gameObject.SetActive(synopsisData != null);
            if (localSynopsisPanel.gameObject.activeSelf)
            {
                localSynopsisPanel.UpdateDisplay(synopsisData);
                 Debug.Log($"[CharacterSelector] Updated local highlight synopsis to: {internalName}", this);
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
        // Get the currently selected button index from the EventSystem directly
        // This assumes the InputController keeps the EventSystem selection up-to-date.
        GameObject currentSelectedObj = EventSystem.current?.currentSelectedGameObject;
        if (currentSelectedObj == null)
        {
            Debug.LogWarning("[CharacterSelector] ExecuteConfirmSelection called but nothing is selected.");
            return;
        }

        int currentSelectedIndex = -1;
        for (int i = 0; i < characterButtons.Count; i++)
        {
            if (characterButtons[i].button != null && characterButtons[i].button.gameObject == currentSelectedObj)
            {   
                currentSelectedIndex = i;
                break;
            }
        }

        if (currentSelectedIndex >= 0)
        {
            string selectedName = characterButtons[currentSelectedIndex].characterInternalName;
            Debug.Log($"[CharacterSelector] Executing confirmation for: {selectedName}");
            RequestSetCharacterServerRpc(selectedName);
        }
        else
        {
            Debug.LogWarning($"[CharacterSelector] ExecuteConfirmSelection: Currently selected object '{currentSelectedObj.name}' not found in buttons.");
        }
    }
} 