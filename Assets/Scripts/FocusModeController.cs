using UnityEngine;
using Unity.Netcode; // Assuming player might need this later

// Handles the player's Focus Mode state
public class FocusModeController : NetworkBehaviour // Or MonoBehaviour if state is purely local
{
    [Header("Input")]
    [SerializeField] private KeyCode focusKey = KeyCode.LeftShift;

    [Header("Visuals")]
    [Tooltip("The parent GameObject containing all hitbox visuals (sprite, rotating graphic, etc.)")]
    [SerializeField] private GameObject hitboxVisualsRoot; // Assign the root object in inspector

    // Public property to let other scripts know if focus is active
    public bool IsFocused { get; private set; }

    void Start()
    {
        // Ensure hitbox visuals are hidden initially
        if (hitboxVisualsRoot != null)
        {
            hitboxVisualsRoot.SetActive(false); 
        }
        else
        {
            Debug.LogWarning("FocusModeController: Hitbox Visuals Root is not assigned!", this);
        }
    }

    // We only want the local player controlling this
    void Update()
    {
        // Only process input for the owner of this player object
        if (!IsOwner) return;

        CheckFocusInput();
    }

    private void CheckFocusInput()
    {
        // Check if the focus key is being held down
        IsFocused = Input.GetKey(focusKey);

        // Toggle hitbox visibility based on focus state
        if (hitboxVisualsRoot != null)
        {
            // Only activate/deactivate if the state actually changed to avoid unnecessary calls
            if (hitboxVisualsRoot.activeSelf != IsFocused)
            {
                hitboxVisualsRoot.SetActive(IsFocused);
            }
        }

        // Optional: Add logic here later for hitbox display, scope style, etc.
        // based on the IsFocused state.
        // Example:
        // if (hitboxVisual != null) hitboxVisual.SetActive(IsFocused);
        // if (scopeStyleController != null) scopeStyleController.SetActive(IsFocused);
    }
}
