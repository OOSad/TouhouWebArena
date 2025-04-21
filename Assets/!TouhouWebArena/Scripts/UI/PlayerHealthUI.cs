using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // For List
using Unity.Netcode;
using System.Collections; // Added explicitly for IEnumerator

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private GameObject healthIconPrefab; // Prefab for a single health icon (e.g., a heart or yin-yang)
    [SerializeField] private Transform iconContainer;   // Parent transform where icons will be instantiated
    [SerializeField] private int targetPlayerId = 1;    // 1 for Player1, 2 for Player2 - SET IN INSPECTOR

    [Header("Search Settings")] // Added Header
    [SerializeField] private int maxSearchAttempts = 20; // How many times to retry search
    [SerializeField] private float searchRetryDelay = 0.5f; // Delay between retries

    private List<GameObject> healthIcons = new List<GameObject>();
    private PlayerHealth _targetPlayerHealth; // Keep reference to PlayerHealth
    private CharacterStats _characterStats; // Added reference to CharacterStats

    void Start()
    {
        // Find the correct PlayerHealth component in the scene
        StartCoroutine(FindAndSubscribeToPlayerHealth());
    }

    private System.Collections.IEnumerator FindAndSubscribeToPlayerHealth()
    {
        // Wait until NetworkManager is ready
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient);

        int attempts = 0;
        while (_targetPlayerHealth == null && attempts < maxSearchAttempts)
        {
            attempts++;

            PlayerHealth[] allPlayerHealths = FindObjectsOfType<PlayerHealth>();

            foreach (PlayerHealth ph in allPlayerHealths)
            {
                // Check the OwnerClientId 
                if (ph.OwnerClientId == (ulong)targetPlayerId)
                {
                    _targetPlayerHealth = ph;
                    _characterStats = ph.GetComponent<CharacterStats>(); 
                    if (_characterStats == null)
                    {
                        yield break; 
                    }

                    InitializeUI(_characterStats.GetStartingHealth()); 
                    _targetPlayerHealth.OnHealthChanged += UpdateUI;
                    UpdateUI(_targetPlayerHealth.CurrentHealth.Value);
                    yield break; // Exit coroutine once found
                }
            }

            // If not found after checking all, wait before retrying
            if (_targetPlayerHealth == null)
            {
                yield return new WaitForSeconds(searchRetryDelay); 
            }
        }

        // Only log warning if loop finished without finding the target
        if (_targetPlayerHealth == null)
        {
            
        }
    }


    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (_targetPlayerHealth != null)
        {
            _targetPlayerHealth.OnHealthChanged -= UpdateUI;
        }
    }

    // Initialize the UI with the maximum number of health icons
    private void InitializeUI(int maxHealth)
    {
        // Clear existing icons first (important for initialization)
        foreach (GameObject icon in healthIcons)
        {
            Destroy(icon);
        }
        healthIcons.Clear();

        // Ensure container and prefab are assigned
        if (iconContainer == null || healthIconPrefab == null)
        {
            return;
        }

        // Instantiate icons based on maxHealth
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject iconInstance = Instantiate(healthIconPrefab, iconContainer);
            healthIcons.Add(iconInstance);
            // Optionally disable them initially if UpdateUI handles enabling
            // iconInstance.SetActive(false); 
        }
    }

    // Update the UI based on the current health value
    private void UpdateUI(int currentHealth)
    {
        // Check if stats are available (needed for max health comparison)
        if (_characterStats == null) return; 

        int maxHealth = _characterStats.GetStartingHealth(); // Get max health from stats

        // Ensure we don't try to access icons out of bounds
        if (healthIcons.Count != maxHealth)
        {
             // If the icon count doesn't match max health (e.g., after a stats change or initialization issue)
             // Re-initialize based on the correct max health. This is a fallback.
             
             InitializeUI(maxHealth);
        }

        // Activate/deactivate icons based on current health
        for (int i = 0; i < healthIcons.Count; i++)
        {
            // Activate icon if index is less than current health
            healthIcons[i].SetActive(i < currentHealth); 
        }
    }
} 