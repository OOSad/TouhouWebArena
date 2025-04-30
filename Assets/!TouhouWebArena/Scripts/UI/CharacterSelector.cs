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
/// Manages the Character Selection UI screen.
/// Handles:
/// - Displaying available character buttons.
/// - Processing player input (mouse clicks, keyboard navigation, confirmation).
/// - Updating the local player's selected character via PlayerDataManager.
/// - Displaying character synopsis panels based on current selections and button highlights.
/// - Listening for PlayerData updates to reflect selections from both players.
/// - Handling application focus changes to maintain keyboard navigation usability.
/// </summary>
public class CharacterSelector : NetworkBehaviour
{
    /// <summary>
    /// Maps a UI Button to a character's internal identifier name.
    /// Used to link button clicks/selections to specific character data.
    /// Must be Serializable to be configured in the Inspector.
    /// </summary>
    [System.Serializable]
    public struct CharacterButtonMapping
    {
        [Tooltip("The UI Button component for this character.")]
        public Button button;
        [Tooltip("The internal identifier for the character (e.g., HakureiReimu). Must match CharacterSynopsisData.internalName.")]
        public string characterInternalName;
    }

    [Header("UI References")]
    [Tooltip("List mapping UI buttons to their corresponding character internal names.")]
    [SerializeField] private List<CharacterButtonMapping> characterButtons;
    [Tooltip("Reference to the Synopsis Panel controller for Player 1.")]
    [SerializeField] private SynopsisPanelController player1SynopsisPanel;
    [Tooltip("Reference to the Synopsis Panel controller for Player 2.")]
    [SerializeField] private SynopsisPanelController player2SynopsisPanel;

    [Header("Data References")]
    [Tooltip("List of all available CharacterSynopsisData ScriptableObjects. Used to populate the synopsis panels.")]
    [SerializeField] private List<CharacterSynopsisData> allSynopsisData;

    [Header("Navigation Settings")]
    [Tooltip("Key used to confirm the character selection.")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Z;
    [Tooltip("Sound effect played when navigating between buttons.")]
    [SerializeField] private AudioClip navigationSound;
    [Tooltip("Sound effect played when confirming a selection.")]
    [SerializeField] private AudioClip confirmSound;
    [Tooltip("AudioSource component used to play UI sound effects.")]
    [SerializeField] private AudioSource uiAudioSource;

    // --- Private Variables ---
    private PlayerDataManager playerDataManager;
    private int selectedIndex = 0; // Index of the currently visually selected button (driven by EventSystem)
    private bool navigationActive = true; // Prevent navigation while scene transition is happening
    private GameObject lastSelectedObject; // Track the previously selected UI element for sound playback logic

    /// <summary>
    /// Lookup dictionary for quick access to CharacterSynopsisData by internal name.
    /// Populated in Start().
    /// </summary>
    private Dictionary<string, CharacterSynopsisData> synopsisLookup = new Dictionary<string, CharacterSynopsisData>();

    /// <summary>
    /// Reference to the synopsis panel controlled by the local player.
    /// Determined in OnNetworkSpawn based on the player's role.
    /// </summary>
    private SynopsisPanelController localSynopsisPanel;
    /// <summary>
    /// Reference to the synopsis panel representing the remote player.
    /// Determined in OnNetworkSpawn based on the player's role.
    /// </summary>
    private SynopsisPanelController remoteSynopsisPanel;
    /// <summary>
    /// The PlayerRole (Player1 or Player2) of the local client.
    /// </summary>
    private PlayerRole localRole = PlayerRole.None;


    // --- Unity Lifecycle Methods ---

    /// <summary>
    /// Called when the NetworkObject is spawned. Subscribes to events and initializes UI state.
    /// Determines local/remote player roles and panels.
    /// Sets up button listeners and initial selection state.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!TryGetComponent(out uiAudioSource))
        {
            Debug.LogWarning("[CharacterSelector] AudioSource component not found on this GameObject. UI sounds will not play.", this);
        }

        // Ensure PlayerDataManager is ready
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[CharacterSelector] PlayerDataManager instance not found! Cannot function.", this);
            enabled = false; // Disable script
            return;
        }
        playerDataManager = PlayerDataManager.Instance;

        // Subscribe to PlayerData updates
        playerDataManager.OnPlayerDataUpdated += HandlePlayerDataUpdated;

        // Determine local player role and assign synopsis panels
        PlayerData? localPlayerData = PlayerDataManager.Instance.GetPlayerData(NetworkManager.Singleton.LocalClientId);
        localRole = localPlayerData.HasValue ? localPlayerData.Value.Role : PlayerRole.None;

        if (localRole == PlayerRole.Player1)
        {
            localSynopsisPanel = player1SynopsisPanel;
            remoteSynopsisPanel = player2SynopsisPanel;
            Debug.Log("[CharacterSelector] Local player is Player 1.", this);
        }
        else if (localRole == PlayerRole.Player2)
        {
            localSynopsisPanel = player2SynopsisPanel;
            remoteSynopsisPanel = player1SynopsisPanel;
            Debug.Log("[CharacterSelector] Local player is Player 2.", this);
        }
        else
        {
            Debug.LogError($"[CharacterSelector] Could not determine local player role (Role: {localRole}). Synopsis panels might not work correctly.", this);
            // Assign P1 as default for local if needed, though this indicates a setup issue
            localSynopsisPanel = player1SynopsisPanel;
            remoteSynopsisPanel = player2SynopsisPanel;
        }


        SetupButtonListeners(); // Setup listeners before potentially selecting

        // Ensure the first button is selected visually and update local highlight ONLY if panel is valid
        if (localSynopsisPanel != null && characterButtons.Count > 0 && characterButtons[0].button != null)
        {
            EventSystem.current.SetSelectedGameObject(characterButtons[0].button.gameObject);
            lastSelectedObject = characterButtons[0].button.gameObject; // Initialize last selected
            selectedIndex = 0;
            UpdateSynopsisPanelForSelection(characterButtons[0].characterInternalName, localSynopsisPanel, localRole == PlayerRole.Player2); // Mirror P2's panel
            Debug.Log($"[CharacterSelector] Initial button selected and local panel updated for {characterButtons[0].characterInternalName}.", this);
        }
        else
        {
             Debug.LogWarning("[CharacterSelector] Could not set initial button selection. Check characterButtons list, button assignments, and localSynopsisPanel assignment.", this);
        }


        // Initial update in case data already exists (e.g., reconnecting)
        HandlePlayerDataUpdated();

        // Store the initial selection for focus regain logic
        if(EventSystem.current != null)
        {
            lastSelectedObject = EventSystem.current.currentSelectedGameObject;
            Debug.Log($"[CharacterSelector] Initial lastSelectedObject set to: {(lastSelectedObject != null ? lastSelectedObject.name : "NULL")}", this);
        }
    }

    /// <summary>
    /// Called when the NetworkObject is despawned. Unsubscribes from events.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (playerDataManager != null)
        {
            playerDataManager.OnPlayerDataUpdated -= HandlePlayerDataUpdated;
        }
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Initializes the synopsis lookup dictionary for fast access.
    /// </summary>
    void Start()
    {
        // Populate the synopsis lookup dictionary
        synopsisLookup.Clear();
        foreach (var synopsis in allSynopsisData)
        {
            if (synopsis != null && !string.IsNullOrEmpty(synopsis.internalName))
            {
                if (!synopsisLookup.ContainsKey(synopsis.internalName))
                {
                    synopsisLookup.Add(synopsis.internalName, synopsis);
                }
                else
                {
                    Debug.LogWarning($"[CharacterSelector] Duplicate internalName '{synopsis.internalName}' found in allSynopsisData. Only the first one will be used.", this);
                }
            }
            else
            {
                 Debug.LogWarning($"[CharacterSelector] Found null or invalid entry in allSynopsisData list.", this);
            }
        }
         Debug.Log($"[CharacterSelector] Populated synopsisLookup with {synopsisLookup.Count} entries.", this);

        // Check panel assignments (helpful for debugging)
        if (player1SynopsisPanel == null) Debug.LogError("[CharacterSelector] Player 1 Synopsis Panel is not assigned in the Inspector!", this);
        if (player2SynopsisPanel == null) Debug.LogError("[CharacterSelector] Player 2 Synopsis Panel is not assigned in the Inspector!", this);
    }


    /// <summary>
    /// Handles player input for confirming selection.
    /// Navigation logic is handled in LateUpdate based on EventSystem selection changes.
    /// </summary>
    void Update()
    {
        // Only allow input if navigation is active
        if (!navigationActive)
        {
            return;
        }

        // Check for confirmation input
        if (Input.GetKeyDown(confirmKey))
        {
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            if (currentSelected != null)
            {
                // Find the button mapping for the selected button
                foreach (var mapping in characterButtons)
                {
                    if (mapping.button != null && mapping.button.gameObject == currentSelected)
                    {
                        Debug.Log($"[CharacterSelector] Confirm key pressed. Attempting to select character: {mapping.characterInternalName}", this);
                        HandleConfirmSelection(mapping.characterInternalName);
                        break; // Found the button, no need to check others
                    }
                }
            }
             else
            {
                Debug.LogWarning("[CharacterSelector] Confirm key pressed, but no UI element is currently selected by the EventSystem.", this);
            }
        }
    }

    /// <summary>
    /// Executes after the regular Update loop, primarily used here to detect changes
    /// in the EventSystem's selected GameObject for keyboard/gamepad navigation feedback.
    /// Updates the local player's synopsis panel based on the highlighted button and plays navigation sounds.
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

        // Check if selection changed to a non-null object
        if (currentSelected != null && currentSelected != lastSelectedObject)
        {
            Debug.Log($"[CharacterSelector.LateUpdate] Selection changed. New: {(currentSelected ? currentSelected.name : "NULL")}, Old: {(lastSelectedObject ? lastSelectedObject.name : "NULL")}", this);

            // Find the corresponding character data for the newly selected button
            string selectedCharInternalName = null;
            Button selectedButtonComponent = currentSelected.GetComponent<Button>(); // Ensure it's a button we care about

            if (selectedButtonComponent != null)
            {
                for (int i = 0; i < characterButtons.Count; i++)
                {
                    if (characterButtons[i].button == selectedButtonComponent)
                    {
                        selectedCharInternalName = characterButtons[i].characterInternalName;
                        selectedIndex = i; // Update selected index based on EventSystem
                         Debug.Log($"[CharacterSelector.LateUpdate] Found matching button mapping. InternalName: {selectedCharInternalName}, Index: {selectedIndex}", this);
                        break;
                    }
                }
            }


            // Update the local player's synopsis panel based on the new selection
            if (localSynopsisPanel != null && !string.IsNullOrEmpty(selectedCharInternalName))
            {
                 UpdateSynopsisPanelForSelection(selectedCharInternalName, localSynopsisPanel, localRole == PlayerRole.Player2); // Mirror P2's panel
            }
            else if(localSynopsisPanel == null)
            {
                 Debug.LogWarning("[CharacterSelector.LateUpdate] Cannot update local synopsis panel because it is null.", this);
            }
            else
            {
                 Debug.LogWarning($"[CharacterSelector.LateUpdate] Could not find character internal name for selected button: {currentSelected.name}", this);
            }


            // Play navigation sound
            PlaySound(navigationSound);

            // Update the last selected object
            lastSelectedObject = currentSelected;
        }
         // Handle case where selection becomes null (e.g., clicking off buttons)
        else if (currentSelected == null && lastSelectedObject != null)
        {
            Debug.Log("[CharacterSelector.LateUpdate] Selection became NULL.", this);
             // Optional: Clear local synopsis panel or revert to confirmed selection? Currently does nothing.
            lastSelectedObject = null; // Update the last selected object
        }
    }


    /// <summary>
    /// Called by Unity when the application gains or loses focus.
    /// Used here to re-select the last known selected button when focus is regained,
    /// ensuring keyboard navigation remains functional after alt-tabbing or clicking outside the window.
    /// </summary>
    /// <param name="hasFocus">True if the application gained focus, false if it lost focus.</param>
    private void OnApplicationFocus(bool hasFocus)
    {
        // If the application regained focus AND navigation is supposed to be active
        if (hasFocus && navigationActive)
        {
             Debug.Log("[CharacterSelector] Application gained focus.", this);
            // Start a coroutine to handle re-selection slightly delayed
            StartCoroutine(ReselectAfterFocusRoutine());
        }
         else if (!hasFocus)
        {
             Debug.Log("[CharacterSelector] Application lost focus.", this);
            // Optional: Clear selection immediately? Current logic relies on EventSystem potentially doing this.
        }
    }

    /// <summary>
    /// Coroutine to re-select the appropriate button after a frame delay.
    /// This delay prevents the click event (that often causes focus gain) from immediately
    /// deselecting the button we are trying to re-select.
    /// </summary>
    private IEnumerator ReselectAfterFocusRoutine()
    {
        // Wait one frame to allow the click event (that caused focus) to potentially deselect
        yield return null;

         Debug.Log("[CharacterSelector] ReselectAfterFocusRoutine executing after 1 frame delay.", this);

        // Re-check conditions AFTER the frame delay
        if (navigationActive && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
             Debug.Log("[CharacterSelector] EventSystem has no selected object after focus regain + delay. Attempting re-selection.", this);
             // Reselect the button based on the last known selected index tracked via EventSystem changes
            if (selectedIndex >= 0 && selectedIndex < characterButtons.Count && characterButtons[selectedIndex].button != null)
            {
                EventSystem.current.SetSelectedGameObject(characterButtons[selectedIndex].button.gameObject);
                lastSelectedObject = characterButtons[selectedIndex].button.gameObject; // Also update lastSelectedObject tracking
                Debug.Log($"[CharacterSelector] Re-selected button '{characterButtons[selectedIndex].button.name}' at index {selectedIndex} on regain focus.", this);

                 // Also re-update the local panel display to be sure
                string internalName = characterButtons[selectedIndex].characterInternalName;
                if(localSynopsisPanel != null && !string.IsNullOrEmpty(internalName))
                {
                    UpdateSynopsisPanelForSelection(internalName, localSynopsisPanel, localRole == PlayerRole.Player2);
                }

            }
             else
            {
                Debug.LogWarning($"[CharacterSelector] Could not re-select button on regain focus. Index {selectedIndex} might be out of bounds or button is null.", this);
            }
        }
         else if (navigationActive && EventSystem.current != null)
        {
             Debug.Log($"[CharacterSelector] EventSystem already has an object selected ({EventSystem.current.currentSelectedGameObject.name}) after focus regain + delay. No re-selection needed.", this);
        }
         else if (!navigationActive)
        {
             Debug.Log("[CharacterSelector] Navigation not active during ReselectAfterFocusRoutine. No re-selection needed.", this);
        }
        else if (EventSystem.current == null)
        {
             Debug.LogError("[CharacterSelector] EventSystem is null during ReselectAfterFocusRoutine!", this);
        }

    }


    // --- Private Helper Methods ---

    /// <summary>
    /// Sets up onClick listeners for all configured character buttons.
    /// </summary>
    private void SetupButtonListeners()
    {
        Debug.Log("[CharacterSelector] Setting up button listeners...", this);
        if (characterButtons == null || characterButtons.Count == 0)
        {
             Debug.LogError("[CharacterSelector] CharacterButtons list is null or empty! Cannot set up listeners. Check Inspector assignment.", this);
             return;
        }

        foreach (var mapping in characterButtons)
        {
            // Need to capture the variable locally for the lambda closure
            string currentCharacterName = mapping.characterInternalName;
            if (mapping.button != null)
            {
                mapping.button.onClick.AddListener(() => HandleButtonClicked(currentCharacterName));
                 Debug.Log($"  Added listener for button '{mapping.button.name}' -> '{currentCharacterName}'", this);
            }
            else
            {
                 Debug.LogWarning($"[CharacterSelector] Found null button in CharacterButtonMapping for internal name '{mapping.characterInternalName}'. Listener not added.", this);
            }
        }
         Debug.Log("[CharacterSelector] Button listeners setup complete.", this);
    }

    /// <summary>
    /// Called when a character button is clicked via its onClick listener.
    /// Attempts to select the character for the local player.
    /// </summary>
    /// <param name="characterInternalName">The internal name of the character associated with the clicked button.</param>
    private void HandleButtonClicked(string characterInternalName)
    {
        Debug.Log($"[CharacterSelector] Button clicked for character: {characterInternalName}", this);
        // Logic is the same as confirming with a key
        HandleConfirmSelection(characterInternalName);
    }

    /// <summary>
    /// Handles the confirmation logic when a character is selected either by click or key press.
    /// Updates the PlayerData for the local player.
    /// </summary>
    /// <param name="characterInternalName">The internal name of the character being confirmed.</param>
    private void HandleConfirmSelection(string characterInternalName)
    {
        if (playerDataManager == null)
        {
            Debug.LogError("[CharacterSelector] Cannot confirm selection, PlayerDataManager is null.", this);
            return;
        }
        if (string.IsNullOrEmpty(characterInternalName))
        {
             Debug.LogWarning("[CharacterSelector] Cannot confirm selection, characterInternalName is null or empty.", this);
             return;
        }

        // Update PlayerData - this will trigger OnPlayerDataChanged event
        playerDataManager.SetPlayerCharacter(NetworkManager.Singleton.LocalClientId, characterInternalName);

        // Play confirmation sound
        PlaySound(confirmSound);

        Debug.Log($"[CharacterSelector] Selection confirmed for: {characterInternalName}. Called PlayerDataManager.SetPlayerCharacter.", this);

        // Consider adding visual feedback here (e.g., brief button flash, "Selected!" text)
        // Potentially disable navigation temporarily?
    }

    /// <summary>
    /// Callback method executed when the PlayerDataManager detects changes in any PlayerData.
    /// Updates both synopsis panels based on the latest selections for Player 1 and Player 2.
    /// </summary>
    private void HandlePlayerDataUpdated()
    {
        if (playerDataManager == null)
        {
            Debug.LogError("[CharacterSelector] HandlePlayerDataUpdated called, but PlayerDataManager is null!", this);
            return;
        }
         if (player1SynopsisPanel == null || player2SynopsisPanel == null)
        {
             Debug.LogError("[CharacterSelector] HandlePlayerDataUpdated called, but one or both Synopsis Panels are null! Check Inspector assignments.", this);
             return;
        }
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
        }
         else
        {
            Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: P1 Data not found (null).", this);
        }

        if (player1SynopsisPanel)
        {
            player1SynopsisPanel.UpdateDisplay(p1Synopsis, false); // P1 panel is never mirrored
            player1SynopsisPanel.gameObject.SetActive(p1Synopsis != null); // Show panel only if data is valid
            Debug.Log($"[CharacterSelector] HandlePlayerDataUpdated: Updated P1 Panel Display (Data: {(p1Synopsis != null ? p1Synopsis.displayName : "NULL")}, Active: {player1SynopsisPanel.gameObject.activeInHierarchy})", this);
        }


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
        }
         else
        {
            Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: P2 Data not found (null).", this);
        }

        if (player2SynopsisPanel)
        {
            player2SynopsisPanel.UpdateDisplay(p2Synopsis, true); // Mirror P2's panel illustration
            player2SynopsisPanel.gameObject.SetActive(p2Synopsis != null); // Show panel only if data is valid
             Debug.Log($"[CharacterSelector] HandlePlayerDataUpdated: Updated P2 Panel Display (Data: {(p2Synopsis != null ? p2Synopsis.displayName : "NULL")}, Active: {player2SynopsisPanel.gameObject.activeInHierarchy})", this);
        }

         Debug.Log("[CharacterSelector] HandlePlayerDataUpdated: Finished updating panels.", this);

    }

    /// <summary>
    /// Updates a specific synopsis panel based on the character selected by keyboard/gamepad navigation.
    /// This primarily affects the *local* player's view before confirmation.
    /// </summary>
    /// <param name="characterInternalName">The internal name of the character whose synopsis should be shown.</param>
    /// <param name="panelToUpdate">The specific panel controller to update.</param>
    /// <param name="mirrorIllustration">Whether to mirror the illustration (usually true for P2 panel).</param>
    private void UpdateSynopsisPanelForSelection(string characterInternalName, SynopsisPanelController panelToUpdate, bool mirrorIllustration)
    {
        if (panelToUpdate == null)
        {
            Debug.LogWarning($"[CharacterSelector] UpdateSynopsisPanelForSelection called but panelToUpdate is null (Char: {characterInternalName}).", this);
            return;
        }
         if (string.IsNullOrEmpty(characterInternalName))
        {
             Debug.LogWarning("[CharacterSelector] UpdateSynopsisPanelForSelection called with null or empty characterInternalName.", this);
             panelToUpdate.UpdateDisplay(null, mirrorIllustration); // Clear the panel
             // Don't necessarily hide it here, as it might be showing the *other* player's confirmed choice
             // panelToUpdate.gameObject.SetActive(false);
             return;
        }

        CharacterSynopsisData synopsis = FindSynopsisData(characterInternalName);
        if (synopsis != null)
        {
            panelToUpdate.UpdateDisplay(synopsis, mirrorIllustration);
            // We don't SetActive here because the panel might already be active showing the *confirmed* selection
            // for this player slot. We only update the content based on highlight.
             Debug.Log($"[CharacterSelector] UpdateSynopsisPanelForSelection updated panel {panelToUpdate.name} for highlighted character {characterInternalName}.", this);
        }
        else
        {
            Debug.LogWarning($"[CharacterSelector] Could not find CharacterSynopsisData for internal name: {characterInternalName}. Cannot update panel {panelToUpdate.name}.", this);
            panelToUpdate.UpdateDisplay(null, mirrorIllustration); // Clear the panel if data not found
            // panelToUpdate.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Finds the CharacterSynopsisData ScriptableObject corresponding to the given internal name.
    /// Uses the pre-populated lookup dictionary.
    /// </summary>
    /// <param name="internalName">The character's internal identifier (e.g., HakureiReimu).</param>
    /// <returns>The found CharacterSynopsisData, or null if not found.</returns>
    private CharacterSynopsisData FindSynopsisData(string internalName)
    {
        if (synopsisLookup.TryGetValue(internalName, out CharacterSynopsisData data))
        {
            return data;
        }
        // Log warning moved to UpdateSynopsisPanelForSelection where it's called
        // Debug.LogWarning($"[CharacterSelector] Could not find CharacterSynopsisData for internal name: {internalName}");
        return null;
    }

    /// <summary>
    /// Plays the specified AudioClip using the assigned AudioSource, if available.
    /// </summary>
    /// <param name="clipToPlay">The AudioClip to play.</param>
    private void PlaySound(AudioClip clipToPlay)
    {
        if (uiAudioSource != null && clipToPlay != null)
        {
            uiAudioSource.PlayOneShot(clipToPlay);
        }
         else if(uiAudioSource == null)
        {
             // Warning issued in OnNetworkSpawn
        }
         else
        {
            // Don't warn every time for null clip, might be intentional
            // Debug.LogWarning($"[CharacterSelector] Tried to play a null audio clip.");
        }
    }

} 