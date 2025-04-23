using UnityEngine;

/// <summary>
/// Controls the character's Animator component based on horizontal movement input.
/// Receives input updates from <see cref="PlayerMovement"/> via <see cref="SetHorizontalInput"/>
/// and updates Animator parameters ("isMovingLeft", "isMovingRight") accordingly.
/// Assumes directionality is handled within the animations themselves.
/// </summary>
[RequireComponent(typeof(Animator))]
public class CharacterAnimation : MonoBehaviour
{
    /// <summary>Cached reference to the Animator component.</summary>
    private Animator animator;

    // --- Animator Parameter Hashes (for performance) ---
    /// <summary>Hash ID for the "isMovingLeft" boolean parameter in the Animator.</summary>
    private static readonly int IsMovingLeftHash = Animator.StringToHash("isMovingLeft");
    /// <summary>Hash ID for the "isMovingRight" boolean parameter in the Animator.</summary>
    private static readonly int IsMovingRightHash = Animator.StringToHash("isMovingRight");

    /// <summary>Input values below this magnitude (absolute) are treated as no movement for animation purposes.</summary>
    private const float InputThreshold = 0.1f;

    /// <summary>Stores the most recent horizontal input value received from PlayerMovement.</summary>
    private float currentHorizontalInput = 0f;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Caches the Animator component.
    /// </summary>
    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogError("CharacterAnimation: Animator not found!");
    }

    /// <summary>
    /// Public method called by external scripts (like <see cref="PlayerMovement"/>)
    /// to update the horizontal input value used for determining animation states.
    /// </summary>
    /// <param name="horizontalInput">The current horizontal input value (typically -1 to 1).</param>
    public void SetHorizontalInput(float horizontalInput)
    {
        currentHorizontalInput = horizontalInput;
    }

    /// <summary>
    /// Called every frame.
    /// Updates the Animator's "isMovingLeft" and "isMovingRight" boolean parameters
    /// based on the stored <see cref="currentHorizontalInput"/> value.
    /// </summary>
    void Update()
    {
        if (animator == null) return; // Basic check

        // Determine state based on stored input
        bool movingLeft = currentHorizontalInput < -InputThreshold;
        bool movingRight = currentHorizontalInput > InputThreshold;

        // Update the Animator parameters
        animator.SetBool(IsMovingLeftHash, movingLeft);
        animator.SetBool(IsMovingRightHash, movingRight);

        // --- Sprite flipping removed as animations handle direction ---
    }
} 