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

    [Header("Data References")]
    [Tooltip("List of all available CharacterSynopsisData ScriptableObjects. Used to populate the lookup dictionary.")]
    [SerializeField] private List<CharacterSynopsisData> allSynopsisData;

    [Header("Navigation Settings")]
    [Tooltip("The key used to confirm the currently highlighted character selection.")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Z;

    [Header("Audio Feedback (Optional)")]
    [Tooltip("AudioSource component to play UI sounds.")]
    [SerializeField] private AudioSource uiAudioSource;
    [Tooltip("Sound played when navigating between character buttons.")]
    [SerializeField] private AudioClip navigateSound;
    [Tooltip("Sound played when confirming a character selection.")]
    [SerializeField] private AudioClip confirmSound;

    [Header("Scene Management")]
    [Tooltip("The exact name of the gameplay scene to load after selection is complete.")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    // --- Private Variables ---
    /// <summary>Cached reference to the PlayerDataManager singleton.</summary>
    private PlayerDataManager playerDataManager;
    /// <summary>The index of the currently highlighted character button in the characterButtons list (used for local navigation).</summary>
    private int selectedIndex = 0;
    /// <summary>Flag to enable/disable keyboard/controller navigation (e.g., during scene transitions).</summary>
    private bool navigationActive = true;
    /// <summary>Tracks the last GameObject selected by the EventSystem, used for focus handling.</summary>
    private GameObject lastSelectedObject;
    /// <summary>Fast lookup dictionary mapping character internal names (string) to their CharacterSynopsisData.</summary>
    private Dictionary<string, CharacterSynopsisData> synopsisLookup = new Dictionary<string, CharacterSynopsisData>();
    /// <summary>Cached reference to the SynopsisPanelController associated with the *local* player.</summary>
    private SynopsisPanelController localSynopsisPanel;
    /// <summary>Cached reference to the SynopsisPanelController associated with the *opponent* player.</summary>
    private SynopsisPanelController opponentSynopsisPanel;

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
        if (player1SynopsisPanel == null || player2SynopsisPanel == null)
        {
            Debug.LogError("[CharacterSelector] OnNetworkSpawn: Player 1 or Player 2 Synopsis Panel is not assigned in the Inspector! Disabling interaction.", this);
            // Maybe disable buttons or the whole component?
            navigationActive = false;
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
        else if (navigationActive && localSynopsisPanel != null && characterButtons.Count > 0 && characterButtons[0].button != null) 
        {
            EventSystem.current.SetSelectedGameObject(characterButtons[0].button.gameObject);
            selectedIndex = 0;
            UpdateLocalHighlightSynopsis(selectedIndex); // Show initial highlight
            lastSelectedObject = characterButtons[0].button.gameObject; // Store initial selection for focus handling
        }
        else if (characterButtons.Count == 0 || characterButtons[0].button == null)
        {
            Debug.LogWarning("[CharacterSelector] No character buttons assigned or first button is null. Navigation might not work.", this);
        }
        else if (!navigationActive || localSynopsisPanel == null)
        {
             Debug.LogWarning("[CharacterSelector] Initial button selection skipped (Navigation disabled or Local Panel invalid).", this);
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
                navigationActive = false; // Disable navigation during transition
                EventSystem.current.SetSelectedGameObject(null); // Deselect buttons
                StartCoroutine(DelayedSceneLoad());
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
    /// Coroutine initiated by the server when both players are ready.
    /// Waits for a short delay before loading the gameplay scene.
    /// Ensures scene loading happens on the server (if this object is server-owned) 
    /// or relies on the host/server initiating the network scene transition.
    /// </summary>
    private IEnumerator DelayedSceneLoad()
    {
        Debug.Log("[CharacterSelector] Both players ready! Starting scene load countdown...");
        yield return new WaitForSeconds(1.0f); // Brief delay
        Debug.Log("[CharacterSelector] Loading Gameplay Scene...");
        // Scene loading should ideally be managed centrally, possibly triggered by the server/host.
        // Assuming NetworkManager handles the transition.
        if (IsServer) // Only server should initiate scene change for all clients
        {
             NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
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
        if (uiAudioSource == null)
        {
            Debug.LogWarning("[CharacterSelector] UI AudioSource not assigned. No sound effects will play.", this);
        }
    }

    /// <summary>
    /// Standard Unity Update method. Checks for confirmation key press.
    /// </summary>
    private void Update()
    {
        // Only allow input if navigation is active
        if (!navigationActive) return;

        // Check for confirmation input
        if (Input.GetKeyDown(confirmKey))
        {
            ConfirmSelection();
        }
    }

    /// <summary>
    /// Called after Update. Used to detect changes in the EventSystem's selected GameObject.
    /// If the selection changed to a valid character button, updates the local synopsis highlight
    /// and plays a navigation sound.
    /// </summary>
    private void LateUpdate()
    {
        if (!navigationActive) return; 

        // --- Add Null Checks ---
        if (EventSystem.current == null)
            {
            Debug.LogError("[CharacterSelector.LateUpdate] EventSystem.current is NULL!", this);
            return; // Cannot proceed without EventSystem
        }
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        
        if (characterButtons == null)
        {
             Debug.LogError("[CharacterSelector.LateUpdate] characterButtons list is NULL! Check Inspector assignment.", this);
             return; // Cannot proceed without the button list
        }
        // ------------------------

        // Check if selection changed to a non-null object that is different from the last
        if (currentSelected != null && currentSelected != lastSelectedObject)
        {
            // Find the index of the newly selected button
            int newIndex = -1;
            for (int i = 0; i < characterButtons.Count; i++)
            {
                if (characterButtons[i].button != null && characterButtons[i].button.gameObject == currentSelected)
                {
                    newIndex = i;
                    break;
                }
            }

            // If a valid button was selected, update highlight and sound
            if (newIndex != -1)
            {
                 Debug.Log($"[CharacterSelector.LateUpdate] Selection changed to index {newIndex} ('{characterButtons[newIndex].characterInternalName}').", this);
                selectedIndex = newIndex;
                UpdateLocalHighlightSynopsis(selectedIndex);
                PlaySound(navigateSound);
            }
            else {
                 Debug.LogWarning($"[CharacterSelector.LateUpdate] Selected object '{currentSelected.name}' not found in characterButtons mapping.", this);
            }
            
            lastSelectedObject = currentSelected; // Update the last selected object
        }
        // Handle deselection (e.g., clicking off buttons)
        else if (currentSelected == null && lastSelectedObject != null)
        {
            // Potentially clear local synopsis or handle as needed.
            // For now, just update the tracked object.
             Debug.Log($"[CharacterSelector.LateUpdate] Selection became NULL.", this);
            lastSelectedObject = null;
            }
        }

    /// <summary>
    /// Updates the *local* player's synopsis panel to show the character 
    /// corresponding to the given button index.
    /// Called when navigating with keyboard/controller before confirming.
    /// </summary>
    /// <param name="index">The index in the characterButtons list to display.</param>
    private void UpdateLocalHighlightSynopsis(int index)
    {
        if (!navigationActive) return;
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
    /// Called when the confirmation key is pressed. 
    /// If a valid button is selected, plays the confirm sound and calls the ServerRpc 
    /// to set the character choice for the local player.
    /// </summary>
    private void ConfirmSelection()
    {
        if (!navigationActive) return;
        if (selectedIndex >= 0 && selectedIndex < characterButtons.Count)
        {
            string selectedName = characterButtons[selectedIndex].characterInternalName;
            Debug.Log($"[CharacterSelector] Confirming selection: {selectedName}");
            PlaySound(confirmSound);
            RequestSetCharacterServerRpc(selectedName);
        }
        else
        {
            Debug.LogWarning("[CharacterSelector] ConfirmSelection called with invalid selectedIndex: " + selectedIndex);
        }
    }

    /// <summary>
    /// Plays the provided AudioClip using the assigned UI AudioSource, if available.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    private void PlaySound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Called by Unity when the application gains or loses focus.
    /// Used to re-select the last highlighted button when focus is regained,
    /// preventing loss of keyboard navigation control.
    /// </summary>
    /// <param name="hasFocus">True if the application gained focus, false if lost.</param>
    private void OnApplicationFocus(bool hasFocus)
    {
        // If the application regained focus AND navigation is supposed to be active
        if (hasFocus && navigationActive)
        {
            // Start a coroutine to handle re-selection slightly delayed
            StartCoroutine(ReselectAfterFocusRoutine());
        }
    }

    /// <summary>
    /// Coroutine that waits one frame after regaining focus before attempting
    /// to re-select the last highlighted character button.
    /// This delay prevents the click event (that caused focus) from immediately 
    /// deselecting the re-selected button.
    /// </summary>
    private IEnumerator ReselectAfterFocusRoutine()
    {
        // Wait one frame to allow the click event (that caused focus) to potentially deselect
        yield return null;

        // Re-check conditions AFTER the frame delay
        if (navigationActive && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
             // Reselect the button based on the last known selected index
            if (selectedIndex >= 0 && selectedIndex < characterButtons.Count && characterButtons[selectedIndex].button != null)
            {
                GameObject objectToSelect = characterButtons[selectedIndex].button.gameObject;
                EventSystem.current.SetSelectedGameObject(objectToSelect);
                lastSelectedObject = objectToSelect; // Update tracker
                Debug.Log($"[CharacterSelector] Re-selected button at index {selectedIndex} ('{objectToSelect.name}') on regaining focus.", this);
            }
             else {
                  Debug.LogWarning($"[CharacterSelector] ReselectAfterFocusRoutine: Could not re-select button at index {selectedIndex} (Invalid index or button is null).", this);
             }
        }
        else if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            Debug.Log($"[CharacterSelector] ReselectAfterFocusRoutine: Something was already selected ('{EventSystem.current.currentSelectedGameObject.name}'), no need to re-select.", this);
        }
        else if (!navigationActive)
        {
            Debug.Log("[CharacterSelector] ReselectAfterFocusRoutine: Navigation became inactive, skipping re-selection.", this);
        }
        else if(EventSystem.current == null)
        {
            Debug.LogError("[CharacterSelector] ReselectAfterFocusRoutine: EventSystem.current is NULL! Cannot re-select.", this);
        }
    }
} 