using UnityEngine;
using Unity.Netcode;
using TMPro;
using System;
using TouhouWebArena.Managers; // Required for RoundManager

public class RoundTimerDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private RoundManager roundManager;
    private bool isSubscribed = false;

    void Start()
    {
        if (timerText == null)
        {
            Debug.LogError("Timer TextMeshProUGUI component is not assigned in the inspector.", this);
            enabled = false;
            return;
        }

        timerText.text = "00:00"; // Initial display

        // Attempt to find RoundManager and subscribe
        FindAndSubscribe();
    }

    void Update()
    {
        // Periodically try to find RoundManager if not found initially 
        // (e.g., if it spawns later)
        if (roundManager == null)
        {
            FindAndSubscribe();
        }
    }

    void OnEnable()
    {
        // Re-subscribe if the object gets re-enabled and we had previously subscribed
        if (roundManager != null && !isSubscribed)
        {
            roundManager.RoundTime.OnValueChanged += HandleRoundTimeChanged;
            isSubscribed = true;
            // Update immediately with current value
            HandleRoundTimeChanged(0, roundManager.RoundTime.Value);
        }
        else if (roundManager == null)
        {
             // Attempt to find it again if re-enabled before Update found it
             FindAndSubscribe();
        }
    }

    void OnDisable()
    {
        // Unsubscribe when disabled or destroyed
        if (roundManager != null && isSubscribed)
        {
            roundManager.RoundTime.OnValueChanged -= HandleRoundTimeChanged;
            isSubscribed = false;
        }
    }

    private void FindAndSubscribe()
    {
        // Find the RoundManager instance in the scene
        // Using FindObjectOfType can be slow, consider a Singleton or direct reference if performance matters
        roundManager = FindFirstObjectByType<RoundManager>();

        if (roundManager != null && !isSubscribed)
        {
            Debug.Log("Found RoundManager, subscribing to RoundTime changes.", this);
            roundManager.RoundTime.OnValueChanged += HandleRoundTimeChanged;
            isSubscribed = true;
            // Update immediately with current value upon finding
            HandleRoundTimeChanged(0, roundManager.RoundTime.Value);
        }
        else if (roundManager == null)
        {
             // Log periodically in Update might be too noisy, perhaps only log once
             // Debug.LogWarning("RoundManager not found. Timer display inactive.", this);
             // --- Add Log ---
             // if (Time.frameCount % 120 == 0) // Log less frequently
             // {
             //    Debug.LogWarning("[RoundTimerDisplay Client] Still searching for RoundManager...", this);
             // }
             // ---------------
        }
    }

    private void HandleRoundTimeChanged(float previousValue, float newValue)
    {
        // --- Add Log ---
        // Debug.Log($"[RoundTimerDisplay Client] HandleRoundTimeChanged: Received new value {newValue}"); // Keep this commented unless debugging
        // ---------------
        if (timerText != null)
        {
            try
            {
                // Ensure value is non-negative
                if (newValue < 0) newValue = 0;

                // Format the time as MM:SS manually
                TimeSpan timeSpan = TimeSpan.FromSeconds(newValue);
                int minutes = (int)timeSpan.TotalMinutes; // Use TotalMinutes for safety
                int seconds = timeSpan.Seconds;

                // Manual formatting with padding
                string formattedTime = $"{minutes:D2}:{seconds:D2}"; // D2 ensures two digits with leading zero

                timerText.text = formattedTime;
            }
            catch (Exception ex) // Catch broader Exception just in case
            {
                Debug.LogError($"[RoundTimerDisplay Client] Error updating timer text: {ex.Message} for value {newValue}", this);
                timerText.text = "##:##"; // Different error display
            }
        }
    }
} 