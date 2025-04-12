using UnityEngine;
using Unity.Netcode;

// Implementation of the Scope Style for Reimu.
// Handles the expanding circular visual.
public class ReimuScopeStyleController : BaseScopeStyleController
{
    [Header("Reimu Scope Settings")]
    [SerializeField] private Transform scopeVisualTransform; // Assign the circular SpriteRenderer's Transform
    [SerializeField] private SpriteRenderer scopeSpriteRenderer; // Assign the SpriteRenderer component
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

        // --- Network Object Verification Log ---
        // if (NetworkObject != null && NetworkManager.Singleton != null)
        // {
        //      Debug.Log($"[{GetType().Name} {OwnerClientId}] OnNetworkSpawn: NetworkObject ID={NetworkObject.NetworkObjectId}, IsSpawned={NetworkObject.IsSpawned}. NetworkManager knows {NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count} spawned objects.");
        // }
        // else
        // {
        //      Debug.LogError($"[{GetType().Name} {OwnerClientId}] OnNetworkSpawn: NetworkObject (null? {!NetworkObject}) or NetworkManager (null? {!NetworkManager.Singleton}) is missing!");
        // }
        // -------------------------------------

        // Subscribe to scale changes for visual updates
        NetworkedCurrentScopeScale.OnValueChanged += OnScopeScaleChanged;

        // Initialize scale and visuals
        if (IsOwner)
        {
            // Owner sets the initial scale
            NetworkedCurrentScopeScale.Value = initialScopeScale;
            // Debug.Log($"[{GetType().Name} {OwnerClientId} ON_SPAWN_OWNER] Setting initial NetworkedCurrentScopeScale to: {initialScopeScale}");
        }
        
        // All clients apply the initial scale visually - this happens before the object might be disabled by PlayerFocusController's initial state sync.
        // Debug.Log($"[{GetType().Name} {OwnerClientId} ON_SPAWN_ALL] Applying initial visual scale based on NetworkVar value: {NetworkedCurrentScopeScale.Value}");
        ApplyScopeScaleVisual(NetworkedCurrentScopeScale.Value);

        // Validation
        if (scopeVisualTransform == null)
        {
            Debug.LogError($"ReimuScopeStyleController ({OwnerClientId}): Scope Visual Transform not set!", this);
            enabled = false;
            return; // Exit early if transform is missing
        }
        if (scopeSpriteRenderer == null)
        {
            // Attempt to get it from the visual transform if not assigned
            scopeSpriteRenderer = scopeVisualTransform.GetComponent<SpriteRenderer>();
            if (scopeSpriteRenderer == null)
            {
                Debug.LogError($"ReimuScopeStyleController ({OwnerClientId}): Scope Sprite Renderer not set and couldn't find one on Visual Transform!", this);
                enabled = false;
                return; // Exit early if renderer is missing
            }
            else
            {
                 Debug.LogWarning($"ReimuScopeStyleController ({OwnerClientId}): Scope Sprite Renderer was not assigned, but found one on Visual Transform. Assigning automatically.", this);
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
             Debug.LogError($"ReimuScopeStyleController ({OwnerClientId}): Cannot activate visual, Scope Sprite Renderer is missing!");
        }
    }

    void Update()
    {
        // Only the owner calculates and updates the scale NetworkVariable
        if (!IsOwner) return;

        // TEMPORARY DEBUG: Verify runtime values
        // Debug.Log($"[Owner {OwnerClientId} Update] isFocusing={isCurrentlyFocused}, currentNVScale={NetworkedCurrentScopeScale.Value}, maxScale={maxScopeScale}, expansionSpeed={scopeExpansionSpeed}");

        if (isCurrentlyFocused)
        {
            // Expand scope while focusing
            float targetScale = Mathf.MoveTowards(NetworkedCurrentScopeScale.Value, maxScopeScale, scopeExpansionSpeed * Time.deltaTime);
            float clampedScale = Mathf.Clamp(targetScale, initialScopeScale, maxScopeScale);
            
            // Only update the network variable if the value actually changes
            if (!Mathf.Approximately(NetworkedCurrentScopeScale.Value, clampedScale))
            {
                 // TEMPORARY DEBUG: Log network variable update attempt
                 // Debug.Log($"[Owner {OwnerClientId} Update] Attempting to set NetworkedCurrentScopeScale from {NetworkedCurrentScopeScale.Value} to {clampedScale}");
                 NetworkedCurrentScopeScale.Value = clampedScale;
            }
        }
        // Resetting happens instantly in SetFocusState when isFocusing becomes false
    }

    // Runs on all clients when NetworkedCurrentScopeScale changes
    private void OnScopeScaleChanged(float previousValue, float newValue)
    {
        // TEMPORARY DEBUG: Log value received by callback
        // Debug.Log($"[!!! CLIENT {NetworkManager.Singleton?.LocalClientId ?? 0} for Player {OwnerClientId} !!!] OnScopeScaleChanged received: {newValue} (was {previousValue}). CALLING ApplyScopeScaleVisual.");
        ApplyScopeScaleVisual(newValue);
    }

    // Applies the scale to the visual Transform
    private void ApplyScopeScaleVisual(float scale)
    {
        if (scopeVisualTransform != null)
        {   
            // Debug.Log($"[Client {NetworkManager.Singleton?.LocalClientId ?? 0} for Player {OwnerClientId}] ApplyScopeScaleVisual: Applying scale {scale} to {scopeVisualTransform.name}");
            scopeVisualTransform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
             // Debug.LogError($"[Client {NetworkManager.Singleton?.LocalClientId ?? 0} for Player {OwnerClientId}] ApplyScopeScaleVisual: scopeVisualTransform is NULL!");
        }
    }
    
    // Optional: Reset scale visually if the component is disabled unexpectedly
    void OnDisable()
    {
        // Reset visual scale to initial state if disabled
        // ApplyScopeScaleVisual(initialScopeScale); // Let OnScopeScaleChanged handle visual reset if NetworkVar changes

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
} 