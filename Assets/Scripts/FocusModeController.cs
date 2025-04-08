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

    // --- Scope Style Components (Optional) ---
    // These are found automatically on Start, no need to assign in Inspector
    private CircleScope circleScope;
    private ConeScope coneScope;
    // -----------------------------------------

    // Public property to let other scripts know if focus is active
    public bool IsFocused { get; private set; }
    private bool wasFocusedLastFrame = false; // To detect changes

    void Awake()
    {
        // Try to find scope components on the same GameObject or children
        circleScope = GetComponentInChildren<CircleScope>();
        coneScope = GetComponentInChildren<ConeScope>();
        // It's expected that only one of these will be found per character prefab
    }

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
        bool currentlyFocused = Input.GetKey(focusKey);
        IsFocused = currentlyFocused; // Update public property

        // --- Handle State Changes ---
        if (currentlyFocused && !wasFocusedLastFrame)
        {
            // Focus Started
            ActivateFocusEffects();
        }
        else if (!currentlyFocused && wasFocusedLastFrame)
        {
            // Focus Ended
            DeactivateFocusEffects();
        }
        // --------------------------

        // Store state for next frame's comparison
        wasFocusedLastFrame = currentlyFocused;

        // --- Hitbox visual is handled separately from scope styles ---
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

    private void ActivateFocusEffects()
    {
        // Activate the appropriate scope style, if found
        circleScope?.Activate(); // Null-conditional operator ?. 
        coneScope?.Activate();
    }

    private void DeactivateFocusEffects()
    {
        // Deactivate the appropriate scope style, if found
        circleScope?.Deactivate();
        coneScope?.Deactivate();
    }
}
