using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Manages the visual representation for the player's spell bar.
/// Holds networked state updated authoritatively by the server via ServerCalculateState.
/// </summary>
public class SpellBarController : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField]
    [Tooltip("The Image component representing the passive fill level.")]
    private Image passiveFillImage;

    [SerializeField]
    [Tooltip("The Image component representing the active charge level overlay.")]
    private Image activeFillImage;

    [Header("Target Player")]
    [SerializeField]
    [Tooltip("Which player ID (OwnerClientId) this bar is intended for (e.g., 1 or 2).")]
    private int targetPlayerId = 1; // Default to 1, should be set in Inspector

    // Network Variables for synchronized state
    // Written by Server (Owner of this object), Everyone reads for UI
    public NetworkVariable<float> currentPassiveFill = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> currentActiveFill = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public const float MaxFillAmount = 4f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Initialize the bar state only on the server
        if (IsServer)
        {
            // Start with the first quadrant filled
            currentPassiveFill.Value = 1.0f; 
            // Active fill defaults to 0 from constructor, which is correct
            currentActiveFill.Value = 0f; 
        }
    }

    void Update()
    {
        // Update visuals on all clients based on networked values ONLY
        UpdateFillImages();
    }

    /// <summary>
    /// Calculates and updates the spell bar state. MUST ONLY BE CALLED ON THE SERVER.
    /// Handles ACTIVE charge based on player input. Passive fill is handled elsewhere.
    /// </summary>
    /// <param name="isCharging">Whether the targeted player is currently charging.</param>
    /// <param name="activeRate">Active charge rate for this character.</param>
    public void ServerCalculateState(bool isCharging, float activeRate)
    {
        // Ensure this only runs on the server (which owns these scene objects)
        if (!IsServer)
        {
             return;
        }

        // --- Passive Fill Logic (Server) ---
        // REMOVED - This is now handled in PlayerShooting server Update loop

        // --- Active Charge Logic (Server) ---
        float newActiveFill = currentActiveFill.Value; // Start with current value
        if (isCharging)
        {
            newActiveFill += activeRate * Time.deltaTime; // Use Time.deltaTime here for active charging
            // Active fill cannot exceed passive fill
            newActiveFill = Mathf.Clamp(newActiveFill, 0f, currentPassiveFill.Value);
        }
        else
        {
            // Reset active fill when not charging
            newActiveFill = 0f;
        }
        currentActiveFill.Value = newActiveFill; // Write potentially changed value
    }

    private void UpdateFillImages()
    {
        if (passiveFillImage != null)
        {
            passiveFillImage.fillAmount = currentPassiveFill.Value / MaxFillAmount;
        }

        if (activeFillImage != null)
        {
            activeFillImage.fillAmount = currentActiveFill.Value / MaxFillAmount;
        }
    }

    /// <summary>
    /// Gets the intended player ID for this spell bar.
    /// </summary>
    public int GetTargetPlayerId()
    {
        return targetPlayerId;
    }
} 