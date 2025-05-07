using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TouhouWebArena.Managers;

public class RoundIndicatorDisplay : MonoBehaviour
{
    [Header("Player 1 Indicators")]
    [SerializeField] private Image player1Petal1;
    [SerializeField] private Image player1Petal2;

    [Header("Player 2 Indicators")]
    [SerializeField] private Image player2Petal1;
    [SerializeField] private Image player2Petal2;

    private RoundManager roundManager;
    private bool subscribedP1 = false;
    private bool subscribedP2 = false;

    void Start()
    {
        // Basic validation
        if (player1Petal1 == null || player1Petal2 == null || player2Petal1 == null || player2Petal2 == null)
        {
            Debug.LogError("One or more Player Petal Image components are not assigned in the inspector.", this);
            enabled = false;
            return;
        }

        // Initialize all petals as inactive
        UpdatePetals(player1Petal1, player1Petal2, 0);
        UpdatePetals(player2Petal1, player2Petal2, 0);

        // Attempt to find RoundManager and subscribe
        FindAndSubscribe();
    }

    void Update()
    {
        // Keep trying to find RoundManager if not found yet
        if (roundManager == null)
        {
            FindAndSubscribe();
        }
    }

    void OnEnable()
    {
        // Re-subscribe if re-enabled
        if (roundManager != null)
        {
            if (!subscribedP1)
            {
                roundManager.Player1Score.OnValueChanged += HandleP1ScoreChanged;
                subscribedP1 = true;
                HandleP1ScoreChanged(0, roundManager.Player1Score.Value); // Initial update
            }
            if (!subscribedP2)
            {
                roundManager.Player2Score.OnValueChanged += HandleP2ScoreChanged;
                subscribedP2 = true;
                HandleP2ScoreChanged(0, roundManager.Player2Score.Value); // Initial update
            }
        }
         else
        {
             FindAndSubscribe(); // Attempt if re-enabled before Update finds it
        }
    }

    void OnDisable()
    {
        // Unsubscribe
        if (roundManager != null)
        {
            if (subscribedP1)
            {
                roundManager.Player1Score.OnValueChanged -= HandleP1ScoreChanged;
                subscribedP1 = false;
            }
            if (subscribedP2)
            {
                roundManager.Player2Score.OnValueChanged -= HandleP2ScoreChanged;
                subscribedP2 = false;
            }
        }
    }

    private void FindAndSubscribe()
    {
        roundManager = FindFirstObjectByType<RoundManager>();

        if (roundManager != null)
        {
            if (!subscribedP1)
            {
                Debug.Log("Found RoundManager, subscribing to Player1Score changes.", this);
                roundManager.Player1Score.OnValueChanged += HandleP1ScoreChanged;
                subscribedP1 = true;
                HandleP1ScoreChanged(0, roundManager.Player1Score.Value); // Initial update
            }
            if (!subscribedP2)
            {
                Debug.Log("Found RoundManager, subscribing to Player2Score changes.", this);
                roundManager.Player2Score.OnValueChanged += HandleP2ScoreChanged;
                subscribedP2 = true;
                HandleP2ScoreChanged(0, roundManager.Player2Score.Value); // Initial update
            }
        }
        // else { Debug.LogWarning("RoundManager not found. Indicator display inactive."); // Optional log }
    }

    private void HandleP1ScoreChanged(int previousValue, int newValue)
    {
        UpdatePetals(player1Petal1, player1Petal2, newValue);
    }

    private void HandleP2ScoreChanged(int previousValue, int newValue)
    {
        UpdatePetals(player2Petal1, player2Petal2, newValue);
    }

    // Helper method to update the state of two petal images based on score
    private void UpdatePetals(Image petal1, Image petal2, int score)
    {
        if (petal1 != null) petal1.enabled = (score >= 1);
        if (petal2 != null) petal2.enabled = (score >= 2);
        // Score > 2 shouldn't happen with WinningScore = 2, but this handles it.
    }
} 