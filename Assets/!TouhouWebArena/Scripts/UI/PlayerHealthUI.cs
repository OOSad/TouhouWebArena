using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // For List
using Unity.Netcode;
using System.Collections; // Added explicitly for IEnumerator

/// <summary>
/// Manages the UI display of a player's health using individual icons (e.g., hearts).
/// Finds the target player's <see cref="PlayerHealth"/> component based on the assigned <see cref="targetPlayerRole"/> (Player1 or Player2),
/// by querying the <see cref="PlayerDataManager"/> to match the role with the correct client ID.
/// Subscribes to the found player's health changes and updates the number of active icons accordingly.
/// Requires references to the icon prefab and the container where icons will be placed.
/// Uses a coroutine with retry logic to find the target player component, accommodating network initialization delays.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI Configuration")] // Grouped UI elements
    [Tooltip("Prefab representing a single unit of health (e.g., heart, life icon).")]
    [SerializeField] private GameObject healthIconPrefab;
    [Tooltip("The UI Transform (e.g., Horizontal Layout Group) where health icons will be instantiated.")]
    [SerializeField] private Transform iconContainer;

    [Header("Target Player")] // Grouped target settings
    [Tooltip("The role (Player1 or Player2) this health UI represents. Must be set correctly in the Inspector.")]
    [SerializeField] private PlayerRole targetPlayerRole = PlayerRole.Player1;

    [Header("Target Search Settings")]
    [Tooltip("Maximum number of attempts the script will make to find the target PlayerHealth component on Start.")]
    [SerializeField] private int maxSearchAttempts = 20;
    [Tooltip("Delay in seconds between each attempt to find the target PlayerHealth component.")]
    [SerializeField] private float searchRetryDelay = 0.5f;

    /// <summary>
    /// List holding the instantiated health icon GameObjects managed by this UI.
    /// </summary>
    private List<GameObject> healthIcons = new List<GameObject>();
    /// <summary>
    /// Cached reference to the target player's <see cref="PlayerHealth"/> component. Found via <see cref="FindAndSubscribeToPlayerHealth"/>.
    /// </summary>
    private PlayerHealth _targetPlayerHealth;
    /// <summary>
    /// Cached reference to the target player's <see cref="CharacterStats"/> component. Used to retrieve max health for UI initialization. Found via <see cref="FindAndSubscribeToPlayerHealth"/>.
    /// </summary>
    private CharacterStats _characterStats;

    /// <summary>
    /// Called once when the script instance is enabled.
    /// Starts the <see cref="FindAndSubscribeToPlayerHealth"/> coroutine to locate and link to the target player's health data.
    /// </summary>
    void Start()
    {
        // Find the correct PlayerHealth component in the scene
        StartCoroutine(FindAndSubscribeToPlayerHealth());
    }

    /// <summary>
    /// Coroutine that repeatedly attempts to find the <see cref="PlayerHealth"/> component associated with the specified <see cref="targetPlayerRole"/>.
    /// Waits until the <see cref="NetworkManager"/> and <see cref="PlayerDataManager"/> are available, then searches through all <see cref="PlayerHealth"/> instances.
    /// For each instance, it retrieves the owner's <see cref="PlayerData"/> from the <see cref="PlayerDataManager"/> and checks if the <see cref="PlayerData.Role"/> matches the <see cref="targetPlayerRole"/>.
    /// Upon finding the correct component, it caches references to <see cref="PlayerHealth"/> and <see cref="CharacterStats"/>,
    /// calls <see cref="InitializeUI"/>, subscribes to <see cref="PlayerHealth.OnHealthChanged"/>, and updates the UI immediately.
    /// Includes retry logic with delays (<see cref="searchRetryDelay"/>) up to <see cref="maxSearchAttempts"/>.
    /// </summary>
    /// <returns>An IEnumerator for the coroutine.</returns>
    private IEnumerator FindAndSubscribeToPlayerHealth()
    {
        // Wait until NetworkManager is ready
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient);

        int attempts = 0;
        while (_targetPlayerHealth == null && attempts < maxSearchAttempts)
        {
            attempts++;

            PlayerHealth[] allPlayerHealths = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

            foreach (PlayerHealth ph in allPlayerHealths)
            {
                // Check the PlayerRole via PlayerDataManager
                if (PlayerDataManager.Instance == null)
                {
                    // Wait if PlayerDataManager isn't ready yet
                    Debug.LogWarning($"[PlayerHealthUI-{targetPlayerRole}] Waiting for PlayerDataManager..."); // Added waiting log
                    break; // Break inner loop, wait for next attempt
                }

                PlayerData? playerData = PlayerDataManager.Instance.GetPlayerData(ph.OwnerClientId);
                if (playerData.HasValue && playerData.Value.Role == targetPlayerRole)
                {
                    // Check the OwnerClientId // REMOVED - Check Role instead
                    // if (ph.OwnerClientId == (ulong)targetPlayerId) // REMOVED
                    // {
                    _targetPlayerHealth = ph;
                    _characterStats = ph.GetComponent<CharacterStats>(); // Get associated CharacterStats
                    if (_characterStats == null)
                    {
                        Debug.LogError($"PlayerHealthUI for Target Role {targetPlayerRole}: Found PlayerHealth but CharacterStats component is missing on GameObject {ph.gameObject.name}!", this);
                        _targetPlayerHealth = null; // Reset target if stats are missing
                        yield break; // Stop searching, critical setup error
                    }

                    // Successfully found and validated
                    InitializeUI(_characterStats.GetStartingHealth()); // Use stats for max health
                    _targetPlayerHealth.OnHealthChanged += UpdateUI;
                    UpdateUI(_targetPlayerHealth.CurrentHealth.Value);
                    yield break; // Exit coroutine once found
                    // }
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
            Debug.LogWarning($"PlayerHealthUI for Target Role {targetPlayerRole}: Failed to find PlayerHealth component after {maxSearchAttempts} attempts.", this);
        }
    }

    /// <summary>
    /// Called when the GameObject this component is attached to is destroyed.
    /// Ensures cleanup by unsubscribing from the <see cref="PlayerHealth.OnHealthChanged"/> event if previously subscribed, preventing potential memory leaks.
    /// </summary>
    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (_targetPlayerHealth != null)
        {
            _targetPlayerHealth.OnHealthChanged -= UpdateUI;
        }
    }

    /// <summary>
    /// Initializes or re-initializes the health icons display based on the player's maximum health.
    /// Clears any existing icons currently managed by this UI, then instantiates the correct number of
    /// <see cref="healthIconPrefab"/> instances as children of the <see cref="iconContainer"/>.
    /// Assumes <see cref="_characterStats"/> is available to provide the maximum health value.
    /// </summary>
    /// <param name="maxHealth">The maximum health value determining the total number of icons to display.</param>
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
            Debug.LogError($"PlayerHealthUI for Target Role {targetPlayerRole}: Icon Container or Health Icon Prefab is not assigned in the Inspector.", this); // Updated log
            return;
        }

        // Instantiate icons based on maxHealth
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject iconInstance = Instantiate(healthIconPrefab, iconContainer);
            healthIcons.Add(iconInstance);
            // Icons are activated/deactivated in UpdateUI, so no SetActive(false) needed here.
        }
    }

    /// <summary>
    /// Callback method triggered by the <see cref="PlayerHealth.OnHealthChanged"/> event.
    /// Updates the visual state of the health icons to reflect the player's current health.
    /// First, it ensures the number of instantiated icons matches the player's maximum health (retrieved from <see cref="_characterStats"/>),
    /// calling <see cref="InitializeUI"/> if there's a mismatch.
    /// Then, it activates/deactivates the icons in the <see cref="healthIcons"/> list based on the <paramref name="currentHealth"/>.
    /// </summary>
    /// <param name="currentHealth">The player's current health value received from the event.</param>
    private void UpdateUI(int currentHealth)
    {
        // Check if stats are available (needed for max health comparison)
        if (_characterStats == null)
        {
             // Add a log to indicate potential issue if stats become null after initialization
             Debug.LogWarning($"PlayerHealthUI for Target Role {targetPlayerRole}: CharacterStats reference lost. Cannot update UI correctly.", this); // Updated log
             return;
        }

        int maxHealth = _characterStats.GetStartingHealth(); // Get max health from stats

        // Ensure we don't try to access icons out of bounds
        if (healthIcons.Count != maxHealth)
        {
             // If the icon count doesn't match max health (e.g., after a stats change or initialization issue)
             // Re-initialize based on the correct max health. This is a fallback.
             Debug.LogWarning($"PlayerHealthUI for Target Role {targetPlayerRole}: Icon count ({healthIcons.Count}) mismatch with Max Health ({maxHealth}). Re-initializing UI.", this); // Updated log
             InitializeUI(maxHealth);
             // Re-check count after re-initialization to avoid errors in the loop below if InitializeUI failed
             if(healthIcons.Count != maxHealth) return;
        }

        // Activate/deactivate icons based on current health
        for (int i = 0; i < healthIcons.Count; i++)
        {
            // Activate icon if index is less than current health
            healthIcons[i].SetActive(i < currentHealth); 
        }
    }
} 