using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MainMenuNavigator : MonoBehaviour
{
    [Tooltip("List of buttons that can be navigated with Up/Down arrows.")]
    [SerializeField] private List<Button> navigableButtons = new List<Button>();

    [Tooltip("Key used to confirm selection.")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Z;

    [Tooltip("Optional: AudioSource for UI sounds.")]
    [SerializeField] private AudioSource uiAudioSource;

    [Tooltip("Optional: Sound effect for navigating between options.")]
    [SerializeField] private AudioClip navigateSound;

    [Tooltip("Optional: Sound effect for confirming an option.")]
    [SerializeField] private AudioClip confirmSound;


    private int selectedIndex = 0;
    private bool selectionActive = true; // To prevent navigation while typing in InputField

    // Start is called before the first frame update
    void Start()
    {
        if (navigableButtons.Count > 0)
        {
            // Ensure the first button is selected visually on start
            UpdateSelectionVisuals();
        }
        else
        {
            Debug.LogWarning("MainMenuNavigator: No navigable buttons assigned.", this);
            selectionActive = false; // Disable navigation if no buttons
        }

        // Listen for InputField selection events
        // This requires adding an EventTrigger component to your InputField
        // and configuring it to call OnInputFieldSelected and OnInputFieldDeselected
        // Alternatively, check EventSystem.current.currentSelectedGameObject in Update.
        // Simple check in Update is often sufficient:
    }

    // Update is called once per frame
    void Update()
    {
        // Simple check to disable navigation if an InputField is focused
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null)
        {
            selectionActive = false;
        }
        else
        {
            selectionActive = true;
        }


        if (!selectionActive || navigableButtons.Count == 0)
        {
            // If input field is selected or no buttons, do nothing
            return;
        }

        // --- Navigation Input ---
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex++;
            if (selectedIndex >= navigableButtons.Count)
            {
                selectedIndex = 0; // Wrap around to top
            }
            UpdateSelectionVisuals();
            PlaySound(navigateSound);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex--;
            if (selectedIndex < 0)
            {
                selectedIndex = navigableButtons.Count - 1; // Wrap around to bottom
            }
            UpdateSelectionVisuals();
            PlaySound(navigateSound);
        }

        // --- Confirmation Input ---
        if (Input.GetKeyDown(confirmKey))
        {
            if (selectedIndex >= 0 && selectedIndex < navigableButtons.Count && navigableButtons[selectedIndex] != null)
            {
                // Ensure the button is interactable before clicking
                if (navigableButtons[selectedIndex].interactable)
                {
                    PlaySound(confirmSound);
                    // Invoke the button's onClick event
                    navigableButtons[selectedIndex].onClick.Invoke();
                }
            }
        }
    }

    private void UpdateSelectionVisuals()
    {
        if (EventSystem.current != null && navigableButtons.Count > 0 && selectedIndex >= 0 && selectedIndex < navigableButtons.Count)
        {
            // Tell the EventSystem which GameObject should be selected.
            // The Button component will handle its visual state change (highlighting).
            EventSystem.current.SetSelectedGameObject(navigableButtons[selectedIndex].gameObject);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
        // else: Optionally Debug.Log("Tried to play sound but source or clip missing");
    }

    // --- Optional: Methods to be called by InputField's EventTrigger ---
    // Add EventTrigger component to InputField -> Add Pointer Down event -> Call this method
    // Add EventTrigger component to InputField -> Add Deselect event -> Call this method
    /*
    public void OnInputFieldSelected()
    {
        selectionActive = false;
        // Optionally clear EventSystem selection so no button appears highlighted
        // EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnInputFieldDeselected()
    {
        selectionActive = true;
        // Optionally re-select the button that was selected before field was clicked
        // UpdateSelectionVisuals();
    }
    */
}
