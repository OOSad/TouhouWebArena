using UnityEngine;
using Unity.Netcode; // Assuming player might need this later

// Handles the player's Focus Mode state and synchronizes it
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

    // Synchronized state variable. Owner writes, everyone reads.
    public NetworkVariable<bool> IsFocused = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );

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

        // Owner updates the NetworkVariable based on input
        bool currentlyFocused = Input.GetKey(focusKey);
        if (currentlyFocused != IsFocused.Value) // Only write if changed
        {
            IsFocused.Value = currentlyFocused;
        }
    }

    // --- Network Variable Synchronization ---

    public override void OnNetworkSpawn()
    {
        // Subscribe to changes for the IsFocused variable
        IsFocused.OnValueChanged += HandleFocusChanged;

        // Apply initial state immediately in case we missed the first change
        HandleFocusChanged(false, IsFocused.Value); 
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe to prevent errors after object is destroyed
        IsFocused.OnValueChanged -= HandleFocusChanged;
    }

    // This method runs on ALL clients when IsFocused changes
    private void HandleFocusChanged(bool previousValue, bool newValue)
    {
        // Toggle Hitbox Visuals
        if (hitboxVisualsRoot != null)
        {
            hitboxVisualsRoot.SetActive(newValue);
        }

        // Activate/Deactivate Scope Style
        if (newValue)
        {
            // Activate the appropriate scope style, if found
            circleScope?.Activate(); 
            coneScope?.Activate();
        }
        else
        {
            // Deactivate the appropriate scope style, if found
            circleScope?.Deactivate();
            coneScope?.Deactivate();
        }
    }
}
