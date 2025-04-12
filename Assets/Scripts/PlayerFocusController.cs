using UnityEngine;
using Unity.Netcode;

public class PlayerFocusController : NetworkBehaviour
{
    [Header("Core References")]
    [SerializeField] private PlayerMovement playerMovement; // Assign in inspector
    [SerializeField] private SpriteRenderer hitboxSpriteRenderer; // Assign the hitbox SpriteRenderer in inspector
    [SerializeField] private BaseScopeStyleController scopeStyleController; // Assign the character-specific Scope Style Controller

    [Header("Focus Settings")]
    // NetworkVariable to sync the focus state across all clients.
    private NetworkVariable<bool> NetworkedIsFocusing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Local tracking for input changes, only relevant for the owner
    private bool localIsFocusing = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // --- Network Variable Callback Subscription ---
        NetworkedIsFocusing.OnValueChanged += OnFocusStateChanged;
        
        // --- Basic Validation --- 
        // Note: Validation happens *before* initial state sync might occur.
        if (playerMovement == null)
        {
            Debug.LogError($"PlayerFocusController ({OwnerClientId}): PlayerMovement reference not set!", this);
            enabled = false;
        }
        if (hitboxSpriteRenderer == null)
        {
             Debug.LogWarning($"PlayerFocusController ({OwnerClientId}): Hitbox SpriteRenderer reference not set! Hitbox visual disabled.", this);
             ToggleHitboxVisual(false); // Keep hitbox initially off
        }
        if (scopeStyleController == null)
        {
            Debug.LogError($"PlayerFocusController ({OwnerClientId}): Scope Style Controller reference not set! Scope Style disabled.", this);
            // Don't toggle active state here, just log the error.
        }
        
        // --- Initial State Application --- 
        // Apply initial state based on the *current* value of the NetworkVariable.
        // This ensures correct visuals for late joiners or if state changes before this runs.
        // We only need to do this once here, the subscription handles subsequent changes.
        ApplyFocusVisualState(NetworkedIsFocusing.Value);

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkedIsFocusing.OnValueChanged -= OnFocusStateChanged;
    }

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

    private void UpdateFocusState(bool isFocusingNow)
    {
        if (playerMovement != null)
        {   
            playerMovement.IsFocused = isFocusingNow;
        }
        NetworkedIsFocusing.Value = isFocusingNow;
    }

    // --- Network Variable Callbacks (Run on ALL clients) --- 

    // This is now the single point where visuals/state are updated based on network changes
    private void OnFocusStateChanged(bool previousValue, bool newValue)
    {
        ApplyFocusVisualState(newValue);
    }

    // Helper method to apply the visual state based on focus
    private void ApplyFocusVisualState(bool isFocused)
    {
        ToggleHitboxVisual(isFocused);
        // Inform the controller *before* changing its visual state
        InformScopeStyleController(isFocused); 
        // Now tell the controller to toggle its visuals
        // ToggleScopeStyleActiveState(isFocused); // Old way: activating/deactivating GameObject
        SetScopeStyleVisualActive(isFocused); // New way: activating/deactivating visuals via controller method
    }

    // --- Visual/State Update Methods --- 

    private void ToggleHitboxVisual(bool show)
    {
        if (hitboxSpriteRenderer != null)
        {
            hitboxSpriteRenderer.enabled = show;
        }
    }
    
    // Calls the method on the controller to handle its own visuals
    private void SetScopeStyleVisualActive(bool isActive)
    {
        if (scopeStyleController != null)
        {
            scopeStyleController.SetVisualActive(isActive);
        }
    }

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

    // --- Cleanup --- 

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
            // ToggleScopeStyleActiveState(false);
            SetScopeStyleVisualActive(false); 
        }
    }
} 