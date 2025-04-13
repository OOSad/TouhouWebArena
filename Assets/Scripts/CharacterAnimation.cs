using UnityEngine;

// Assuming this script is on the same GameObject as the Animator
[RequireComponent(typeof(Animator))]
public class CharacterAnimation : MonoBehaviour
{
    private Animator animator;

    // Use Hash IDs for performance
    private static readonly int IsMovingLeftHash = Animator.StringToHash("isMovingLeft");
    private static readonly int IsMovingRightHash = Animator.StringToHash("isMovingRight");

    // Threshold for input instead of velocity (can be simpler, e.g., 0.1 or just checking != 0)
    private const float InputThreshold = 0.1f;

    // Store the input received from PlayerMovement
    private float currentHorizontalInput = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogError("CharacterAnimation: Animator not found!");
    }

    // Public method for PlayerMovement to call
    public void SetHorizontalInput(float horizontalInput)
    {
        currentHorizontalInput = horizontalInput;
    }

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
        // if (movingLeft)
        // {
        //     transform.localScale = new Vector3(-1f, 1f, 1f);
        // }
        // else if (movingRight)
        // {
        //     transform.localScale = new Vector3(1f, 1f, 1f);
        // }
    }
} 