using UnityEngine;

/// <summary>
/// Manages the visual representation of a Spirit based on its activation state.
/// Toggles between the normal and activated visual GameObjects.
/// Should be attached to the same GameObject as the SpiritController.
/// </summary>
public class SpiritVisualController : MonoBehaviour
{
    [Header("Visual Objects (Assign Children)")]
    [Tooltip("The child GameObject holding visuals for the normal (unactivated) state.")]
    [SerializeField] private GameObject normalVisualObject;

    [Tooltip("The child GameObject holding visuals for the activated state.")]
    [SerializeField] private GameObject activatedVisualObject;

    /// <summary>
    /// Sets the active visual representation based on the spirit's activation state.
    /// </summary>
    /// <param name="isActivated">True if the spirit is activated, false otherwise.</param>
    public void SetVisualState(bool isActivated)
    {
        if (normalVisualObject == null || activatedVisualObject == null)
        {
            Debug.LogWarning("[SpiritVisualController] Visual objects not assigned! Cannot update visuals.", this);
            return;
        }

        // Activate/Deactivate based on state
        normalVisualObject.SetActive(!isActivated);
        activatedVisualObject.SetActive(isActivated);
    }

    // Optional: Could add an Initialize method if needed later, 
    // e.g., to ensure visuals are correct on Awake/Start before network sync.
    // void Start()
    // {
    //     // Set a default state initially?
    //     SetVisualState(false); 
    // }
} 