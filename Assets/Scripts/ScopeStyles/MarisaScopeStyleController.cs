using UnityEngine;
using Unity.Netcode;

// Implementation of the Scope Style for Marisa.
// Handles the horizontally expanding cone visual.
public class MarisaScopeStyleController : BaseScopeStyleController
{
    [Header("Marisa Scope Settings")]
    [SerializeField] private Transform scopeVisualTransform;    // Assign the cone SpriteRenderer's Transform
    [SerializeField] private SpriteRenderer scopeSpriteRenderer; // Assign the cone SpriteRenderer component
    [SerializeField] private float initialScopeWidth = 0.2f;     // Initial localScale.x
    [SerializeField] private float maxScopeWidth = 8.0f;         // Maximum localScale.x
    [SerializeField] private float widthExpansionSpeed = 6.0f;   // Width units per second

    // NetworkVariable to sync the current width across clients.
    private NetworkVariable<float> NetworkedCurrentScopeWidth = new NetworkVariable<float>(0.2f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Tracks whether the scope should currently be active (expanding/visible)
    private bool isCurrentlyFocused = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to width changes for visual updates
        NetworkedCurrentScopeWidth.OnValueChanged += OnScopeWidthChanged;

        // Initialize width and visuals
        if (IsOwner)
        {
            // Owner sets the initial width
            NetworkedCurrentScopeWidth.Value = initialScopeWidth;
        }
        // All clients apply the initial width visually
        ApplyScopeWidthVisual(NetworkedCurrentScopeWidth.Value);

        // Validation
        if (scopeVisualTransform == null)
        {
            Debug.LogError($"MarisaScopeStyleController ({OwnerClientId}): Scope Visual Transform not set!", this);
            enabled = false;
            return;
        }
        if (scopeSpriteRenderer == null)
        {
            scopeSpriteRenderer = scopeVisualTransform.GetComponent<SpriteRenderer>();
            if (scopeSpriteRenderer == null)
            {
                Debug.LogError($"MarisaScopeStyleController ({OwnerClientId}): Scope Sprite Renderer not set and couldn't find one on Visual Transform!", this);
                enabled = false;
                return;
            }
            else
            {
                Debug.LogWarning($"MarisaScopeStyleController ({OwnerClientId}): Scope Sprite Renderer was not assigned, but found one on Visual Transform. Assigning automatically.", this);
            }
        }

        // Ensure visuals start inactive
        SetVisualActive(false);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkedCurrentScopeWidth.OnValueChanged -= OnScopeWidthChanged;
    }

    // This is called by PlayerFocusController
    public override void SetFocusState(bool isFocusing)
    {
        isCurrentlyFocused = isFocusing;

        // If focus stopped, owner should immediately reset the width
        if (!isCurrentlyFocused && IsOwner)
        {
            NetworkedCurrentScopeWidth.Value = initialScopeWidth;
        }
    }

    // Called by PlayerFocusController to control visibility
    public override void SetVisualActive(bool isActive)
    {
        if (scopeSpriteRenderer != null)
        {
            scopeSpriteRenderer.enabled = isActive;
        }
        else if (isActive)
        {
            Debug.LogError($"MarisaScopeStyleController ({OwnerClientId}): Cannot activate visual, Scope Sprite Renderer is missing!");
        }
    }

    void Update()
    {
        // Only the owner calculates and updates the width NetworkVariable
        if (!IsOwner) return;

        if (isCurrentlyFocused)
        {
            // Expand scope width while focusing
            float targetWidth = Mathf.MoveTowards(NetworkedCurrentScopeWidth.Value, maxScopeWidth, widthExpansionSpeed * Time.deltaTime);
            float clampedWidth = Mathf.Clamp(targetWidth, initialScopeWidth, maxScopeWidth);

            // Only update the network variable if the value actually changes
            if (!Mathf.Approximately(NetworkedCurrentScopeWidth.Value, clampedWidth))
            {
                NetworkedCurrentScopeWidth.Value = clampedWidth;
            }
        }
        // Resetting happens instantly in SetFocusState when isFocusing becomes false
    }

    // Runs on all clients when NetworkedCurrentScopeWidth changes
    private void OnScopeWidthChanged(float previousValue, float newValue)
    {
        ApplyScopeWidthVisual(newValue);
    }

    // Applies the scale width to the visual Transform
    private void ApplyScopeWidthVisual(float width)
    {
        if (scopeVisualTransform != null)
        {
            // Keep current Y scale, only change X
            Vector3 currentScale = scopeVisualTransform.localScale;
            scopeVisualTransform.localScale = new Vector3(width, currentScale.y, currentScale.z);
        }
    }

    void OnDisable()
    {
        // Ensure focus state is considered false if disabled
        isCurrentlyFocused = false;

        // If owner is disabled, ensure the width resets on the network
        if (IsOwner && NetworkedCurrentScopeWidth.Value != initialScopeWidth)
        {
            NetworkedCurrentScopeWidth.Value = initialScopeWidth;
        }

        // Ensure visuals are off if component is disabled
        SetVisualActive(false);
    }
} 