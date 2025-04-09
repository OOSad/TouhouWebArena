using UnityEngine;
using UnityEngine.UI; // Required for UI Image
using System.Collections.Generic; // Required for List
using Unity.Netcode; // Required for NetworkBehaviour (optional, depending on how reference is found)
using System.Collections; // Required for Coroutine

// Manages the visual display of player health (e.g., 5 orbs)
// This script should be placed on a UI GameObject in your Canvas.
public class PlayerHealthUI : MonoBehaviour // Doesn't strictly need NetworkBehaviour unless finding players relies on it
{
    public enum TargetPlayer { Player1, Player2 }

    [Header("UI References")]
    [Tooltip("Assign the 5 UI Image components for the health orbs here, in order (0=first health point).")]
    [SerializeField] private List<Image> healthOrbs;

    [Header("Targeting")]
    [Tooltip("Which player's health should this UI display?")]
    [SerializeField] private TargetPlayer targetPlayer = TargetPlayer.Player1;

    private PlayerHealth _targetPlayerHealth;

    // Use a coroutine to wait for PlayerDataManager to be ready and players to spawn
    IEnumerator Start()
    {
        if (healthOrbs == null || healthOrbs.Count != PlayerHealth.MaxHealth)
        {
            Debug.LogError($"PlayerHealthUI ({targetPlayer}): Health Orbs list is not assigned or doesn't have exactly {PlayerHealth.MaxHealth} elements!", this);
            yield break; // Stop if UI isn't set up correctly
        }

        // Wait until PlayerDataManager is initialized
        // Adjust this wait condition if PlayerDataManager has a different readiness flag/event
        while (PlayerDataManager.Instance == null)
        {
             // Debug.Log($"PlayerHealthUI ({targetPlayer}) waiting for PlayerDataManager...");
            yield return null; // Wait for the next frame
        }
        // Debug.Log($"PlayerHealthUI ({targetPlayer}) found PlayerDataManager.");

        // Find the target PlayerHealth component
        yield return StartCoroutine(FindTargetPlayerHealth());

        // Subscribe and do initial update if found
        if (_targetPlayerHealth != null)
        {
             // Debug.Log($"PlayerHealthUI ({targetPlayer}) found target PlayerHealth. Subscribing.");
            _targetPlayerHealth.OnHealthChanged += UpdateHealthUI;
            // Initial UI state based on current health
            UpdateHealthUI(_targetPlayerHealth.CurrentHealth.Value);
        }
        else
        {
             Debug.LogError($"PlayerHealthUI ({targetPlayer}) failed to find the target PlayerHealth component after waiting!", this);
        }
    }

    IEnumerator FindTargetPlayerHealth()
    {
        // Continuously check until the target player data is available
        PlayerDataManager.PlayerData? targetData = null;
        while (!targetData.HasValue)
        {
            if (targetPlayer == TargetPlayer.Player1)
            {
                targetData = PlayerDataManager.Instance.GetPlayer1Data();
            }
            else // TargetPlayer.Player2
            {
                targetData = PlayerDataManager.Instance.GetPlayer2Data();
            }

            if (!targetData.HasValue)
            {
                 // Debug.Log($"PlayerHealthUI ({targetPlayer}) waiting for player data...");
                yield return new WaitForSeconds(0.5f); // Wait a bit before checking again
            }
        }

        // Once we have the player data, find the corresponding NetworkObject/PlayerHealth
        ulong targetClientId = targetData.Value.ClientId;
        NetworkObject targetNetworkObject = null;

        // Wait until the NetworkObject for the target client exists locally
        int safetyCounter = 0; // Prevent infinite loops
        const int maxAttempts = 20; // Try for ~10 seconds

        while (targetNetworkObject == null && safetyCounter < maxAttempts)
        {
            // Iterate through locally spawned objects to find the one owned by the target client
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                foreach (NetworkObject networkObject in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
                {
                    // Check if it's a player object owned by the target client
                    if (networkObject.IsPlayerObject && networkObject.OwnerClientId == targetClientId)
                    {
                        targetNetworkObject = networkObject;
                        // Debug.Log($"PlayerHealthUI ({targetPlayer}) found NetworkObject for Client ID {targetClientId} by iterating.");
                        break; // Found it
                    }
                }
            }

            if (targetNetworkObject == null)
            {
                // Debug.Log($"PlayerHealthUI ({targetPlayer}) waiting for NetworkObject of Client ID {targetClientId} to spawn locally (Attempt {safetyCounter + 1})...");
                safetyCounter++;
                yield return new WaitForSeconds(0.5f); // Wait before checking again
            }
        }

        if (targetNetworkObject == null)
        {
             Debug.LogWarning($"PlayerHealthUI ({targetPlayer}) timed out waiting for NetworkObject of Client ID {targetClientId}.");
             yield break; // Exit if not found after multiple attempts
        }

        // Now get the PlayerHealth component from the found NetworkObject
         _targetPlayerHealth = targetNetworkObject.GetComponent<PlayerHealth>();

         // Debug.Log($"PlayerHealthUI ({targetPlayer}) finished search. Found NetworkObject: {targetNetworkObject != null}, Found PlayerHealth: {_targetPlayerHealth != null}");
    }


    private void OnDestroy()
    {
        // Unsubscribe when the UI object is destroyed
        if (_targetPlayerHealth != null)
        {
            _targetPlayerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }

    private void UpdateHealthUI(int currentHealth)
    {
        // Loop through the orb images and enable/disable them based on current health
        for (int i = 0; i < healthOrbs.Count; i++)
        {
            if (healthOrbs[i] != null)
            {
                // Orb index 'i' corresponds to health point 'i+1'
                // Enable the orb if currentHealth is greater than the index
                healthOrbs[i].enabled = (currentHealth > i);
            }
        }
        // Debug.Log($"PlayerHealthUI ({targetPlayer}) updated. Current Health: {currentHealth}");
    }

    // Helper to initially hide all orbs if no target is found
    private void SetAllOrbsActive(bool isActive)
    {
         if (healthOrbs == null) return;
         foreach(var orb in healthOrbs)
         {
             if (orb != null) orb.enabled = isActive;
         }
    }

    // --- Optional: Dynamic Player Finding ---
    // If assigning via inspector isn't feasible, you might need logic like this:
    // [SerializeField] private bool trackPlayer1 = true; // Set this in inspector
    //
    // IEnumerator Start() {
    //     // ... wait for PlayerDataManager or similar to be ready ...
    //     yield return new WaitUntil(() => PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsReady);
    //
    //     PlayerDataManager.PlayerData? targetData = trackPlayer1 ? PlayerDataManager.Instance.GetPlayer1Data() : PlayerDataManager.Instance.GetPlayer2Data();
    //     if (targetData.HasValue)
    //     {
    //         NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetData.Value.ClientId);
    //         if (playerObj != null) {
    //              targetPlayerHealth = playerObj.GetComponent<PlayerHealth>();
    //         }
    //     }
    //     // ... rest of Start logic including null check and subscription ...
    // }
    // -----------------------------------------
}
