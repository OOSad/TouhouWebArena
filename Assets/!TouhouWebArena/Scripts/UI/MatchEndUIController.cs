using UnityEngine;
using UnityEngine.UI; // For Button
using TMPro; // For TextMeshProUGUI
using Unity.Netcode;
using TouhouWebArena.Managers; // For PlayerRole maybe?
using UnityEngine.SceneManagement; // For loading main menu

/// <summary>
/// Controls the UI panel displayed at the end of a match, showing the winner
/// and providing options for rematch or quitting.
/// This script runs on the clients.
/// </summary>
public class MatchEndUIController : MonoBehaviour
{
    // --- Singleton Instance ---
    public static MatchEndUIController Instance { get; private set; }
    // -------------------------

    [Header("UI References")]
    [SerializeField] private GameObject matchEndPanel; // The root panel GameObject
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Button rematchButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        // --- Singleton Setup ---
        // Assign the instance *before* the duplicate check.
        // If this is the first instance, Instance will be null, so assign it.
        if (Instance == null)
        {
            Instance = this;
        }
        // If Instance is *not* null (meaning another instance already ran Awake and set it),
        // check if the existing Instance is different from this one.
        else if (Instance != this)
        {
            // If it's different, this is a duplicate.
            Debug.LogWarning("[MatchEndUIController] Multiple instances detected. Destroying duplicate.", gameObject);
            Destroy(gameObject);
            return; // Stop execution for this duplicate instance.
        }
        // If Instance was already set *and* it's this instance, we don't need to do anything.
        // ----------------------

        // Ensure panel is hidden initially
        if (matchEndPanel != null)
        {
            matchEndPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[MatchEndUIController] Match End Panel reference is not set!", gameObject);
        }

        // Add button listeners
        rematchButton?.onClick.AddListener(OnRematchButtonClicked);
        quitButton?.onClick.AddListener(OnQuitButtonClicked);
    }

    private void OnDestroy()
    {
         // Remove listeners to prevent errors
        rematchButton?.onClick.RemoveListener(OnRematchButtonClicked);
        quitButton?.onClick.RemoveListener(OnQuitButtonClicked);
    }

    /// <summary>
    /// Called by the RoundManager's ClientRpc to display the match end screen.
    /// </summary>
    /// <param name="winnerRole">The role of the player who won the match.</param>
    public void ShowMatchEndScreen(PlayerRole winnerRole)
    {
        Debug.Log($"[MatchEndUIController] Showing Match End Screen. Winner: {winnerRole}");
        if (matchEndPanel != null)
        {
            winnerText.text = $"{winnerRole} Wins!"; // Basic winner text
            matchEndPanel.SetActive(true);
            // ADDED LOG: Check state immediately after setting active
            Debug.Log($"[MatchEndUIController] After SetActive(true), panel activeSelf: {matchEndPanel.activeSelf}, activeInHierarchy: {matchEndPanel.activeInHierarchy}");
            // TODO: Disable player input?
        }
    }

    /// <summary>
    /// Hides the match end screen panel and re-enables the rematch button.
    /// </summary>
    public void HideMatchEndScreen()
    {
        if (matchEndPanel != null)
        {
            matchEndPanel.SetActive(false);
        }
        // Re-enable rematch button for the next time
        if (rematchButton != null) 
        { 
            rematchButton.interactable = true; 
        }
        Debug.Log("[MatchEndUIController] Hiding Match End Screen.");
    }

    private void OnRematchButtonClicked()
    {
        Debug.Log("[MatchEndUIController] Rematch button clicked.");
        // Find RoundManager and send RPC
        RoundManager roundManager = FindFirstObjectByType<RoundManager>(); // Find the server-side manager
        if (roundManager != null)
        {
            roundManager.RequestRematchServerRpc();
            Debug.Log("[MatchEndUIController] Sent RequestRematchServerRpc.");
        }
        else
        {
             Debug.LogError("[MatchEndUIController] Could not find RoundManager to send rematch request!");
        }

        // Optional: Give visual feedback (e.g., disable button, show "Waiting...")
        rematchButton.interactable = false; 
    }

    private void OnQuitButtonClicked()
    {
        Debug.Log("[MatchEndUIController] Quit button clicked.");
        // TODO: Consider informing the server? (May not be necessary if handled by disconnect)
        
        // Shutdown network connection
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Load Main Menu Scene (Ensure scene name is correct)
        SceneManager.LoadScene("MainMenuScene"); // Replace with your actual main menu scene name
    }
} 