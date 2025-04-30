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

public class CharacterSelector : NetworkBehaviour
{
    [System.Serializable]
    public struct CharacterButtonMapping
    {
        public Button button;
        public string characterInternalName;
    }

    [Header("UI References")]
    [SerializeField] private List<CharacterButtonMapping> characterButtons;
    [SerializeField] private SynopsisPanelController player1SynopsisPanel;
    [SerializeField] private SynopsisPanelController player2SynopsisPanel;

    [Header("Data References")]
    [SerializeField] private List<CharacterSynopsisData> allSynopsisData;

    [Header("Navigation Settings")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Z;

    [Header("Audio Feedback (Optional)")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip navigateSound;
    [SerializeField] private AudioClip confirmSound;

    [Header("Scene Management")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    // --- Private Variables ---
    private PlayerDataManager playerDataManager;
    private int selectedIndex = 0;
    private bool navigationActive = true;
    private GameObject lastSelectedObject;
    private Dictionary<string, CharacterSynopsisData> synopsisLookup = new Dictionary<string, CharacterSynopsisData>();
    private SynopsisPanelController localSynopsisPanel;
    private SynopsisPanelController opponentSynopsisPanel;

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

    private void OnCharacterButtonClicked(string characterInternalName)
    {
        Debug.Log($"[CharacterSelector] Button clicked for character: {characterInternalName}");
        RequestSetCharacterServerRpc(characterInternalName);
    }

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

    private IEnumerator DelayedSceneLoad()
    {
        if (!IsServer) yield break;
        yield return new WaitForSeconds(0.1f); 
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
             NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe PlayerDataManager event
        if (playerDataManager != null)
        {
            playerDataManager.OnPlayerDataUpdated -= HandlePlayerDataUpdated;
        }
        
        // Remove button listeners
        foreach (var mapping in characterButtons)
        {
            if (mapping.button != null)
            {
                mapping.button.onClick.RemoveAllListeners();
            }
        }

        base.OnNetworkDespawn();
    }

    private void Start()
    {
        // Start is now mostly empty - initial setup moved to OnNetworkSpawn or Awake
        
        // Null check for audio source (can stay here)
        if (uiAudioSource == null)
        {
            Debug.LogWarning("[CharacterSelector] UI AudioSource not assigned. No sound effects will play.", this);
        }
    }

    private void Update()
    {
        // Only allow input if navigation is active and we are not transitioning
        if (!navigationActive || characterButtons.Count == 0)
        {
            return;
        }

        // Check for confirmation input - Keep this
        if (Input.GetKeyDown(confirmKey))
        {
            ConfirmSelection();
        }

        // REMOVED: Explicit left/right navigation checks. EventSystem handles this.
    }

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
        if (currentSelected != lastSelectedObject && currentSelected != null)
        {
            bool foundButton = false;
            int newlySelectedIndex = -1;
            for (int i = 0; i < characterButtons.Count; i++)
            {
                // --- Add Inner Null Check ---
                if (characterButtons[i].button == null)
                {
                    Debug.LogWarning($"[CharacterSelector.LateUpdate] Button at index {i} in characterButtons list is NULL! Check Inspector assignment.", this);
                    continue; // Skip this null entry
                }
                // ---------------------------

                if (characterButtons[i].button.gameObject == currentSelected) 
                {
                    newlySelectedIndex = i; 
                    foundButton = true;
                    break;
                }
            }

            if (foundButton)
            {
                selectedIndex = newlySelectedIndex; 
                UpdateLocalHighlightSynopsis(selectedIndex); 
                PlaySound(navigateSound);
                // Reduced log spam, uncomment if needed
                // Debug.Log($"[CharacterSelector] Selection changed via EventSystem to index: {selectedIndex}, Button: {currentSelected.name}");
            }
            else
            {
                 // Selection changed to something else
            }
        }

        lastSelectedObject = currentSelected;
    }

    /// <summary>
    /// Updates the local player's synopsis panel based on the currently highlighted button index.
    /// </summary>
    private void UpdateLocalHighlightSynopsis(int index)
    {
        if (localSynopsisPanel == null) return; 

        if (index >= 0 && index < characterButtons.Count)
        {
            string internalName = characterButtons[index].characterInternalName;
            if (synopsisLookup.TryGetValue(internalName, out CharacterSynopsisData data))
            {
                localSynopsisPanel.gameObject.SetActive(true);
                localSynopsisPanel.UpdateDisplay(data);
            }
            else
            {
                Debug.LogWarning($"[CharacterSelector] Could not find synopsis data for highlighted button: {internalName}");
                localSynopsisPanel.gameObject.SetActive(false);
            }
        }
        else
        {
            localSynopsisPanel.gameObject.SetActive(false);
        }
    }

    private void ConfirmSelection()
    {
        if (selectedIndex >= 0 && selectedIndex < characterButtons.Count)
        {
            Button selectedButton = characterButtons[selectedIndex].button;
            if (selectedButton != null && selectedButton.interactable)
            {
                Debug.Log($"[CharacterSelector] Confirming selection: {selectedButton.name}");
                PlaySound(confirmSound);
                selectedButton.onClick.Invoke(); // Trigger the button's existing click event
            }
            else
            {
                Debug.LogWarning("[CharacterSelector] Cannot confirm: Selected button is null or not interactable.");
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // If the application regained focus AND navigation is supposed to be active
        if (hasFocus && navigationActive)
        {
            // Start a coroutine to handle re-selection slightly delayed
            StartCoroutine(ReselectAfterFocusRoutine());
        }
    }

    private IEnumerator ReselectAfterFocusRoutine()
    {
        // Wait one frame to allow the click event (that caused focus) to potentially deselect
        yield return null;

        // Re-check conditions AFTER the frame delay
        if (navigationActive && EventSystem.current.currentSelectedGameObject == null)
        {
             // Reselect the button based on the last known selected index
            if (selectedIndex >= 0 && selectedIndex < characterButtons.Count && characterButtons[selectedIndex].button != null)
            {
                EventSystem.current.SetSelectedGameObject(characterButtons[selectedIndex].button.gameObject);
                Debug.Log($"[CharacterSelector] Re-selected button at index {selectedIndex} after focus regain delay.");
            }
            else if (characterButtons.Count > 0 && characterButtons[0].button != null) // Fallback to first button if index invalid
            {
                EventSystem.current.SetSelectedGameObject(characterButtons[0].button.gameObject);
                selectedIndex = 0;
                lastSelectedObject = characterButtons[0].button.gameObject;
                Debug.LogWarning("[CharacterSelector] Last selected index was invalid after focus delay. Re-selected first button.");
            }
            // Update lastSelectedObject after re-selection
            lastSelectedObject = EventSystem.current.currentSelectedGameObject;
        }
    }
} 