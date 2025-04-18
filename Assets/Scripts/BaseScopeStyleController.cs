using UnityEngine;
using Unity.Netcode;

// Abstract base class for all character-specific Scope Style controllers.
// Ensures they all have a way to be activated/deactivated by the PlayerFocusController.
public abstract class BaseScopeStyleController : NetworkBehaviour
{
    // Called by PlayerFocusController to tell the specific scope style
    // whether it should be active and performing its logic.
    // The GameObject's active state itself will also be toggled by PlayerFocusController.
    public abstract void SetFocusState(bool isFocusing);

    // Called by PlayerFocusController to show/hide the scope visuals.
    public abstract void SetVisualActive(bool isActive);

    // Optional: Common initialization or helper methods could go here if needed.
    // For example, validating a required visual transform reference.
} 