using UnityEngine;
using Unity.Netcode;

// Implementation of the Scope Style for Reimu.
// Handles the expanding circular visual.
public class ReimuScopeStyleController : BaseScopeStyleController
{
    [Header("Reimu Scope Settings")]
    [SerializeField] private Transform scopeVisualTransform; // Assign the circular SpriteRenderer's Transform
    [SerializeField] private SpriteRenderer scopeSpriteRenderer; // Assign the SpriteRenderer component
    [SerializeField] private CircleCollider2D scopeCollider; // <<< ADDED REFERENCE FOR COLLIDER
    [SerializeField] private float initialScopeScale = 0.1f;
    [SerializeField] private float maxScopeScale = 5.0f;
    [SerializeField] private float scopeExpansionSpeed = 4.0f; // Scale units per second

    // NetworkVariable to sync the current scale across clients.
    private NetworkVariable<float> NetworkedCurrentScopeScale = new NetworkVariable<float>(0.1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Tracks whether the scope should currently be active (expanding/visible)
    private bool isCurrentlyFocused = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to scale changes for visual updates
        NetworkedCurrentScopeScale.OnValueChanged += OnScopeScaleChanged;

        // Initialize scale and visuals
        if (IsOwner)
        {
            // Owner sets the initial scale
            NetworkedCurrentScopeScale.Value = initialScopeScale;
        }
        
        // All clients apply the initial scale visually - this happens before the object might be disabled by PlayerFocusController's initial state sync.
        ApplyScopeScaleVisual(NetworkedCurrentScopeScale.Value);

        // Validation
        if (scopeVisualTransform == null)
        {
            
            enabled = false;
            return; // Exit early if transform is missing
        }
        if (scopeSpriteRenderer == null)
        {
            // Attempt to get it from the visual transform if not assigned
            scopeSpriteRenderer = scopeVisualTransform.GetComponent<SpriteRenderer>();
            if (scopeSpriteRenderer == null)
            {
                
                enabled = false;
                return; // Exit early if renderer is missing
            }
            else
            {
                 
            }
        }

        // Ensure visuals start inactive
        SetVisualActive(false);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkedCurrentScopeScale.OnValueChanged -= OnScopeScaleChanged;
    }

    // This is called by PlayerFocusController
    public override void SetFocusState(bool isFocusing)
    {
        isCurrentlyFocused = isFocusing;

        // If focus stopped, owner should immediately reset the scale
        if (!isCurrentlyFocused && IsOwner)
        {
            NetworkedCurrentScopeScale.Value = initialScopeScale;
        }
        
        // Visibility (active state) of this GameObject is handled by PlayerFocusController
    }

    // Called by PlayerFocusController to control visibility
    public override void SetVisualActive(bool isActive)
    {
        if (scopeSpriteRenderer != null)
        {
            scopeSpriteRenderer.enabled = isActive;
        }
        else if(isActive)
        {
             // Log error only if trying to activate a missing renderer
             
        }

        // --- ADDED: Enable/disable collider along with visual ---
        if (scopeCollider != null)
        {
            scopeCollider.enabled = isActive;
        }
        else if (isActive) // Only log error if trying to activate missing collider
        {
            
        }
        // ------------------------------------------------------
    }

    void Update()
    {
        // Only the owner calculates and updates the scale NetworkVariable
        if (!IsOwner) return;

        if (isCurrentlyFocused)
        {
            // Expand scope while focusing
            float targetScale = Mathf.MoveTowards(NetworkedCurrentScopeScale.Value, maxScopeScale, scopeExpansionSpeed * Time.deltaTime);
            float clampedScale = Mathf.Clamp(targetScale, initialScopeScale, maxScopeScale);
            
            // Only update the network variable if the value actually changes
            if (!Mathf.Approximately(NetworkedCurrentScopeScale.Value, clampedScale))
            {
                 NetworkedCurrentScopeScale.Value = clampedScale;
            }
        }
        // Resetting happens instantly in SetFocusState when isFocusing becomes false
    }

    // Runs on all clients when NetworkedCurrentScopeScale changes
    private void OnScopeScaleChanged(float previousValue, float newValue)
    {
        ApplyScopeScaleVisual(newValue);
    }

    // Applies the scale to the visual Transform
    private void ApplyScopeScaleVisual(float scale)
    {
        if (scopeVisualTransform != null)
        {   
            scopeVisualTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
    
    // Optional: Reset scale visually if the component is disabled unexpectedly
    void OnDisable()
    {
        // Ensure focus state is considered false if disabled
        isCurrentlyFocused = false; 

         // If owner is disabled, ensure the scale resets on the network
         // This might be redundant if PlayerFocusController already sets NetworkedIsFocusing to false on disable,
         // which should trigger SetFocusState(false) -> scale reset.
         // Keeping it as a fallback isn't harmful.
         if(IsOwner && NetworkedCurrentScopeScale.Value != initialScopeScale)
         {
            NetworkedCurrentScopeScale.Value = initialScopeScale;
         }

        // Ensure visuals are off if component is disabled
        SetVisualActive(false); 
    }

    // --- ADDED: OnTriggerEnter2D for Spirit Activation ---
    void OnTriggerEnter2D(Collider2D other)
    {
        // This controller is on the player, which is a NetworkObject.
        // The scope style zone is a child, its triggers will be handled by this script on the parent player object if the collider is on the same object or a child
        // and this script is the one unity finds for the event.
        // However, it is generally better to have the script that handles collisions directly on the GameObject that has the Collider2D.
        // Assuming this script IS on the GameObject with the CircleCollider2D for the scope.

        if (!IsClient) return; // Only clients should handle this visual/local activation logic

        // Debug.Log($"[{this.GetType().Name} Owner: {OwnerClientId}] OnTriggerEnter2D triggered by: {other.name} with tag: {other.tag}", gameObject);

        ClientSpiritController spiritController = other.GetComponent<ClientSpiritController>();
        if (spiritController != null)
        {
            Debug.Log($"[{this.GetType().Name} Owner: {OwnerClientId}] Scope style activating spirit: {other.name}", gameObject);
            spiritController.ActivateSpirit();
        }
        // else
        // {
        //     Debug.Log($"[{this.GetType().Name} Owner: {OwnerClientId}] Collided with {other.name}, but it's not a spirit (no ClientSpiritController).", gameObject);
        // }
    }
    // --- END ADDED --- 
} 