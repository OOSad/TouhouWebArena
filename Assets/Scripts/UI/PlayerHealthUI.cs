using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq; // Added for Linq

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Is this UI for Player 1?")]
    [SerializeField] private bool isPlayer1UI = true;
    [Tooltip("Drag the 5 pre-placed health orb GameObjects here in the desired order.")]
    [SerializeField] private List<GameObject> healthOrbSlots = new List<GameObject>(PlayerHealth.MaxHealth);

    private PlayerHealth targetPlayerHealth; 
    private ulong targetClientId => isPlayer1UI ? 1UL : 2UL; // Assuming P1=ClientID 1, P2=ClientID 2

    void Start()
    {
        // Validate the orb slots list
        if (healthOrbSlots == null || healthOrbSlots.Count < PlayerHealth.MaxHealth || healthOrbSlots.Any(slot => slot == null))
        {
            Debug.LogError($"PlayerHealthUI requires exactly {PlayerHealth.MaxHealth} health orb GameObjects assigned to the 'Health Orb Slots' list! Found {healthOrbSlots?.Count ?? 0} assigned.", this);
            enabled = false;
            return;
        }

        // Initially deactivate all orbs
        foreach (var orbSlot in healthOrbSlots)
        {
            orbSlot.SetActive(false);
        }

        // Delay slightly to find player health
        Invoke(nameof(FindAndSubscribePlayerHealth), 0.5f); 
    }

    void OnDestroy()
    {
        // Unsubscribe when the UI object is destroyed
        if (targetPlayerHealth != null)
        {
            targetPlayerHealth.OnHealthChanged -= UpdateHealthDisplay;
        }
    }

    private void FindAndSubscribePlayerHealth()
    {
        if (targetPlayerHealth != null) return; // Already found

        // Find all PlayerHealth components in the scene
        PlayerHealth[] allPlayerHealths = FindObjectsOfType<PlayerHealth>();

        foreach (PlayerHealth healthComponent in allPlayerHealths)
        {
            // Check if this health component belongs to the target player
            if (healthComponent.OwnerClientId == targetClientId)
            {
                targetPlayerHealth = healthComponent;
                // Debug.Log($"PlayerHealthUI ({ (isPlayer1UI ? \"P1\" : \"P2\") }) found target PlayerHealth for Client ID {targetClientId}");
                break; // Found the target
            }
        }

        if (targetPlayerHealth != null)
        {
            // Subscribe to the health changed event
            targetPlayerHealth.OnHealthChanged += UpdateHealthDisplay;
            // Update display with initial health
            UpdateHealthDisplay(targetPlayerHealth.CurrentHealth.Value);
        }
        else
        {
            // Debug.LogWarning($"PlayerHealthUI ({ (isPlayer1UI ? \"P1\" : \"P2\") }) could not find PlayerHealth for Client ID {targetClientId}. Retrying soon...");
            // Retry finding the player health component after a delay
            Invoke(nameof(FindAndSubscribePlayerHealth), 1.0f); 
        }
    }

    private void UpdateHealthDisplay(int currentHealth)
    {
        if (healthOrbSlots == null || healthOrbSlots.Count == 0) return; // Safety check

        // Activate/Deactivate orbs based on current health
        for (int i = 0; i < healthOrbSlots.Count; i++)
        {
            // Activate the orb if its index is less than the current health
            bool shouldBeActive = i < currentHealth;
            if (healthOrbSlots[i] != null && healthOrbSlots[i].activeSelf != shouldBeActive)
            {
                healthOrbSlots[i].SetActive(shouldBeActive);
            }
            else if (healthOrbSlots[i] == null)
            {
                 // Log error if a slot became null unexpectedly (shouldn't happen after Start check)
                 Debug.LogError($"Health Orb Slot at index {i} is null unexpectedly.", this); 
            }
        }
    }
} 