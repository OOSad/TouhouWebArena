using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles keyboard/controller input navigation and focus management 
/// for the Character Selection screen.
/// Interacts with the EventSystem and notifies CharacterSelector via UnityEvents.
/// </summary>
public class CharacterSelectInputController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Optional reference to the component that handles audio feedback.")]
    [SerializeField] private CharacterSelectAudio characterSelectAudio;

    [Header("Settings")]
    [Tooltip("The key used to confirm the currently highlighted character selection.")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Z;

    // --- Events --- 
    [Header("Events")]
    [Tooltip("Fired when navigation changes the highlighted button index.")]
    public UnityEvent<int> OnNavigate = new UnityEvent<int>();
    [Tooltip("Fired when the confirm key is pressed while a button is highlighted.")]
    public UnityEvent OnConfirm = new UnityEvent();

    // --- State --- 
    private List<CharacterSelector.CharacterButtonMapping> characterButtons;
    private int selectedIndex = 0;
    private bool navigationActive = true;
    private GameObject lastSelectedObject;

    /// <summary>
    /// Initializes the controller with the list of character buttons.
    /// Should be called by CharacterSelector after its buttons are set up.
    /// </summary>
    /// <param name="buttons">The list of button mappings from CharacterSelector.</param>
    public void Initialize(List<CharacterSelector.CharacterButtonMapping> buttons)
    {
        this.characterButtons = buttons;
        // Initial selection logic remains in CharacterSelector's OnNetworkSpawn
        if (buttons != null && buttons.Count > 0 && buttons[0].button != null)
        {
            // Store the initial selected object if provided by CharacterSelector
            // This assumes CharacterSelector sets the initial EventSystem selection.
            // lastSelectedObject = buttons[0].button.gameObject;
        } else {
            Debug.LogWarning("[InputController] Initialized with null or empty button list.", this);
            navigationActive = false; // Disable if no buttons
        }
        // Set initial selectedIndex based on EventSystem? Or assume 0?
        // Let's rely on CharacterSelector setting the EventSystem and LateUpdate catching it.
    }

    /// <summary>
    /// Enables or disables input navigation.
    /// </summary>
    public void SetNavigationActive(bool active)
    {
        navigationActive = active;
        if (!active && EventSystem.current != null)
        {
            // Deselect UI when navigation becomes inactive
            EventSystem.current.SetSelectedGameObject(null);
        }
        Debug.Log($"[InputController] Navigation set to: {active}", this);
    }

    void Update()
    {
        if (!navigationActive) return;

        // Check for confirmation input
        if (Input.GetKeyDown(confirmKey))
        {
            // Confirmation action is handled by the listener (CharacterSelector)
            OnConfirm?.Invoke();
        }
    }

    void LateUpdate()
    {
        if (!navigationActive) return;

        if (EventSystem.current == null)
        {   
            Debug.LogError("[InputController.LateUpdate] EventSystem.current is NULL!", this);
            return; 
        }
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (characterButtons == null)
        {   
            // This shouldn't happen if Initialize was called correctly
            Debug.LogError("[InputController.LateUpdate] characterButtons list is NULL!", this);
            return; 
        }

        // Check if selection changed to a non-null object that is different from the last
        if (currentSelected != null && currentSelected != lastSelectedObject)
        {   
            int newIndex = -1;
            for (int i = 0; i < characterButtons.Count; i++)
            {   
                // Check both button and mapping validity
                if (characterButtons[i].button != null && characterButtons[i].button.gameObject == currentSelected)
                {   
                    newIndex = i;
                    break;
                }
            }

            if (newIndex != -1)
            {   
                // Selection changed to a valid button
                selectedIndex = newIndex;
                characterSelectAudio?.PlayNavigateSound();
                OnNavigate?.Invoke(selectedIndex); // Notify listener (CharacterSelector)
                Debug.Log($"[InputController.LateUpdate] Navigated to index {selectedIndex}", this);
            }
            // else: Selection changed, but not to a mapped button (ignore?)
            
            lastSelectedObject = currentSelected; // Update tracker regardless
        }
        // Handle deselection
        else if (currentSelected == null && lastSelectedObject != null)
        {   
            // Selection lost
            lastSelectedObject = null;
             Debug.Log($"[InputController.LateUpdate] Selection became NULL.", this);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && navigationActive)
        {
            StartCoroutine(ReselectAfterFocusRoutine());
        }
    }

    private IEnumerator ReselectAfterFocusRoutine()
    {
        yield return null; // Wait one frame

        // Re-check conditions
        if (navigationActive && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {   
            // Check if lastSelectedObject is still valid and part of the current button list
            bool foundLastSelected = false;
             if (lastSelectedObject != null && characterButtons != null)
             {
                 for (int i = 0; i < characterButtons.Count; i++)
                 {
                     if (characterButtons[i].button != null && characterButtons[i].button.gameObject == lastSelectedObject)
                     {
                         selectedIndex = i; // Update index based on last selected
                         foundLastSelected = true;
                         break;
                     }
                 }
             }

            if (foundLastSelected)
            {
                EventSystem.current.SetSelectedGameObject(lastSelectedObject);
                 Debug.Log($"[InputController] Re-selected {lastSelectedObject.name} on regaining focus.", this);
            }
            else if (characterButtons != null && characterButtons.Count > 0 && characterButtons[0].button != null)
            {   // Fallback: select the first button if lastSelected is invalid/gone
                 selectedIndex = 0;
                 lastSelectedObject = characterButtons[0].button.gameObject;
                EventSystem.current.SetSelectedGameObject(lastSelectedObject);
                Debug.LogWarning("[InputController] ReselectAfterFocusRoutine: Last selected object invalid, selecting first button.", this);
            }
             else
            {   // No valid button to select
                 Debug.LogWarning("[InputController] ReselectAfterFocusRoutine: No valid button found to re-select.", this);
                 lastSelectedObject = null; // Clear tracker
            }
        }
        // else: No need to reselect (already selected, navigation inactive, etc.)
    }
} 