using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Manages the player's focus state, typically activated by holding Left Shift.
/// The owning client detects input and updates a <see cref="NetworkVariable{T}"/> (<see cref="NetworkedIsFocusing"/>).
/// All clients react to changes in this variable to update visuals (hitbox, scope style)
/// and the owner updates the <see cref="PlayerMovement.IsFocused"/> property to affect movement speed.
/// Requires references to <see cref="PlayerMovement"/>, the hitbox <see cref="SpriteRenderer"/>,
/// and a character-specific <see cref="BaseScopeStyleController"/>.
/// </summary>
public class PlayerFocusController : NetworkBehaviour
{
    [Header("Core References")]
    [Tooltip("Reference to the PlayerMovement script to control the IsFocused state.")]
    [SerializeField] private PlayerMovement playerMovement;
    [Tooltip("Reference to the SpriteRenderer component for the player's hitbox visual.")]
    [SerializeField] private SpriteRenderer hitboxSpriteRenderer;
    [Tooltip("Reference to the character-specific scope style controller for focus-related effects.")]
    [SerializeField] private BaseScopeStyleController scopeStyleController;

    [Header("Focus Settings")]
    /// <summary>
    /// NetworkVariable synchronizing the focus state across all clients.
    /// Written by the owner, read by everyone.
    /// </summary>
    private NetworkVariable<bool> NetworkedIsFocusing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>Public getter for the networked focus state.</summary>
    public bool IsFocusingNetworked => NetworkedIsFocusing.Value;

    /// <summary>Local tracking of the focus input state, only used by the owning client.</summary>
    private bool localIsFocusing = false;

    /// <summary>
    /// Called when the network object is spawned.
    /// Subscribes to the <see cref="NetworkedIsFocusing"/> value change event.
    /// Performs initial validation of component references.
    /// Applies the initial visual state based on the current network value (<see cref="ApplyFocusVisualState"/>).
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // --- Network Variable Callback Subscription ---
        NetworkedIsFocusing.OnValueChanged += OnFocusStateChanged;
        
        // --- Basic Validation --- 
        // Note: Validation happens *before* initial state sync might occur.
        if (playerMovement == null)
        {
            Debug.LogError("PlayerFocusController: PlayerMovement reference not set.", this);
            enabled = false;
        }
        if (hitboxSpriteRenderer == null)
        {
             Debug.LogWarning("PlayerFocusController: Hitbox SpriteRenderer reference not set. Hitbox visual will not function.", this);
             ToggleHitboxVisual(false); // Keep hitbox initially off
        }
        if (scopeStyleController == null)
        {
            Debug.LogWarning("PlayerFocusController: BaseScopeStyleController reference not set. Scope style effects will not function.", this);
            // Don't toggle active state here, just log the error.
        }
        
        // --- Initial State Application --- 
        // Apply initial state based on the *current* value of the NetworkVariable.
        // This ensures correct visuals for late joiners or if state changes before this runs.
        // We only need to do this once here, the subscription handles subsequent changes.
        ApplyFocusVisualState(NetworkedIsFocusing.Value);

    }

    /// <summary>
    /// Called when the network object is despawned.
    /// Unsubscribes from the <see cref="NetworkedIsFocusing"/> value change event.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkedIsFocusing.OnValueChanged -= OnFocusStateChanged;
    }

    /// <summary>
    /// Called every frame.
    /// If this is the owning client, checks for the focus key (Left Shift) press/release.
    /// If the state changes, updates <see cref="localIsFocusing"/> and calls <see cref="UpdateFocusState"/>.
    /// </summary>
    void Update()
    {
        if (!IsOwner) return;

        bool focusKeyPressed = Input.GetKey(KeyCode.LeftShift);

        if (focusKeyPressed != localIsFocusing)
        {
            localIsFocusing = focusKeyPressed;
            UpdateFocusState(localIsFocusing);
        }
    }

    /// <summary>
    /// [Owner Only] Updates the player's focus state locally and on the network.
    /// Sets the <see cref="PlayerMovement.IsFocused"/> property and updates the <see cref="NetworkedIsFocusing"/> value.
    /// </summary>
    /// <param name="isFocusingNow">The new focus state.</param>
    private void UpdateFocusState(bool isFocusingNow)
    {
        NetworkedIsFocusing.Value = isFocusingNow;
    }

    /// <summary>
    /// Callback handler for the <see cref="NetworkedIsFocusing.OnValueChanged"/> event.
    /// Runs on all clients when the focus state changes.
    /// Calls <see cref="ApplyFocusVisualState"/> to update visuals based on the new state.
    /// </summary>
    /// <param name="previousValue">The previous focus state.</param>
    /// <param name="newValue">The new (current) focus state.</param>
    private void OnFocusStateChanged(bool previousValue, bool newValue)
    {
        ApplyFocusVisualState(newValue);
    }

    /// <summary>
    /// Applies all visual changes associated with the focus state.
    /// Toggles the hitbox visibility and informs/updates the scope style controller.
    /// </summary>
    /// <param name="isFocused">The current focus state.</param>
    private void ApplyFocusVisualState(bool isFocused)
    {
        ToggleHitboxVisual(isFocused);
        // Inform the controller *before* changing its visual state
        InformScopeStyleController(isFocused); 
        // Now tell the controller to toggle its visuals
        SetScopeStyleVisualActive(isFocused); // New way: activating/deactivating visuals via controller method
    }

    /// <summary>
    /// Toggles the enabled state of the <see cref="hitboxSpriteRenderer"/>.
    /// </summary>
    /// <param name="show">True to enable the renderer, false to disable.</param>
    private void ToggleHitboxVisual(bool show)
    {
        if (hitboxSpriteRenderer != null)
        {
            hitboxSpriteRenderer.enabled = show;
        }
    }
    
    /// <summary>
    /// Calls the <see cref="BaseScopeStyleController.SetVisualActive"/> method on the assigned controller.
    /// Used to activate/deactivate the scope style's visual elements.
    /// </summary>
    /// <param name="isActive">True to activate visuals, false to deactivate.</param>
    private void SetScopeStyleVisualActive(bool isActive)
    {
        if (scopeStyleController != null)
        {
            scopeStyleController.SetVisualActive(isActive);
        }
    }

    /// <summary>
    /// Informs the assigned <see cref="BaseScopeStyleController"/> about the current focus state
    /// by calling its <see cref="BaseScopeStyleController.SetFocusState"/> method.
    /// </summary>
    /// <param name="isFocusing">The current focus state.</param>
    private void InformScopeStyleController(bool isFocusing)
    {
        // Inform the controller regardless of its current activeSelf state
        // The controller's SetFocusState should handle its internal logic appropriately.
        // if (scopeStyleController != null && scopeStyleController.gameObject.activeSelf) // Removed activeSelf check
        if (scopeStyleController != null)
        {
            scopeStyleController.SetFocusState(isFocusing);
        }
    }

    /// <summary>
    /// Called when the component becomes disabled or inactive.
    /// If running on the owner and currently focusing, ensures the focus state is reset
    /// (<see cref="UpdateFocusState"/>) and visuals are turned off.
    /// </summary>
    void OnDisable()
    {
        if (IsOwner)
        {
            if (localIsFocusing)
            {
                UpdateFocusState(false); 
            }
            ToggleHitboxVisual(false);
            // Ensure scope controller is informed and its visuals are turned off
            InformScopeStyleController(false);
            SetScopeStyleVisualActive(false); 
        }
    }
} 