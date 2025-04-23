using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Manages the UI display for a player's spell bar, showing passive and active charge levels.
/// The spell bar's state (fill levels) is controlled by networked variables, updated authoritatively by the server.
/// Each instance targets a specific player defined by <see cref="targetPlayerId"/>.
/// </summary>
public class SpellBarController : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField]
    [Tooltip("The Image component representing the passive fill level (the background fill).")]
    private Image passiveFillImage;

    [SerializeField]
    [Tooltip("The Image component representing the active charge level (an overlay on the passive fill).")]
    private Image activeFillImage;

    [Header("Target Player")]
    [SerializeField]
    [Tooltip("The OwnerClientId of the player this spell bar belongs to (e.g., 1 for Player 1, 2 for Player 2). Must be set in the Inspector.")]
    private int targetPlayerId = 1; // Default to 1, should be set in Inspector

    /// <summary>
    /// The current passive charge level of the spell bar.
    /// Range: 0 to <see cref="MaxFillAmount"/>.
    /// Updated authoritatively by the server (via PlayerShooting), read by all clients for UI updates.
    /// </summary>
    public NetworkVariable<float> currentPassiveFill = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    /// <summary>
    /// The current active charge level being built up by the player holding the charge button.
    /// Range: 0 to <see cref="currentPassiveFill"/>. Cannot exceed the passive level.
    /// Updated authoritatively by the server (in <see cref="ServerCalculateState"/>), read by all clients for UI updates.
    /// </summary>
    public NetworkVariable<float> currentActiveFill = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// The maximum value for both passive and active fill, representing a full bar (e.g., 4 quadrants).
    /// </summary>
    public const float MaxFillAmount = 4f;

    /// <summary>
    /// Called when the NetworkObject is spawned. Initializes the spell bar state on the server.
    /// Sets the initial passive fill level.
    /// </summary>
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

    /// <summary>
    /// Called every frame. Updates the visual representation of the fill images based on the current networked values.
    /// This runs on all clients to keep the UI synchronized.
    /// </summary>
    void Update()
    {
        // Update visuals on all clients based on networked values ONLY
        UpdateFillImages();
    }

    /// <summary>
    /// Calculates and updates the spell bar's active charge state based on player input.
    /// Should only be called on the server, which has authority over the <see cref="currentActiveFill"/> NetworkVariable.
    /// Passive fill updates are handled externally (e.g., in PlayerShooting).
    /// </summary>
    /// <param name="isCharging">Whether the targeted player is currently holding the charge input.</param>
    /// <param name="activeRate">The rate at which the active charge accumulates for the player's character.</param>
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

    /// <summary>
    /// Updates the fillAmount property of the UI Image components based on the current networked fill values.
    /// Normalizes the fill values by dividing by <see cref="MaxFillAmount"/>.
    /// </summary>
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
    /// Gets the target player's OwnerClientId assigned to this spell bar instance.
    /// Used to associate this UI element with the correct player data.
    /// </summary>
    /// <returns>The target player's OwnerClientId.</returns>
    public int GetTargetPlayerId()
    {
        return targetPlayerId;
    }
} 