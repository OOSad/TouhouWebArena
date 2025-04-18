using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // Added for NetworkTransform

[RequireComponent(typeof(NetworkObject))] // Ensure NetworkObject is present
[RequireComponent(typeof(Rigidbody2D))] // Keep this if other logic relies on it
[RequireComponent(typeof(CharacterAnimation))] // Add this back
[RequireComponent(typeof(PlayerPositionSynchronizer))]
[RequireComponent(typeof(CharacterStats))] // Added
public class PlayerMovement : NetworkBehaviour
{
    // Make bounds public static so other scripts (like PlayerDeathBomb) can access them
    public static readonly Rect player1Bounds = new Rect(-8f, -4f, 7f, 8f); // Public Hardcoded Left Bounds
    public static readonly Rect player2Bounds = new Rect(1f, -4f, 7f, 8f); // Public Hardcoded Right Bounds

    // Private variables
    private NetworkTransform networkTransform;
    private Rect currentBounds; // Which bounds apply to this player instance
    private PlayerDataManager playerDataManager; // To check P1/P2
    private PlayerHealth playerHealth;
    private CharacterAnimation characterAnimation; // Add this reference back
    private CharacterStats characterStats; // Added reference
    private PlayerAIController playerAIController; // Added: Reference to AI controller

    // --- Added: AI Control State ---
    private float aiHorizontalInput = 0f;
    // ----------------------------

    // Public property to access the current bounds (read-only from outside)
    public Rect CurrentBounds => currentBounds;

    // Public property to let other scripts control the focus state
    public bool IsFocused { get; set; } = false;

    // Add Awake back to get components reliably before OnNetworkSpawn
    void Awake()
    {
        networkTransform = GetComponent<NetworkTransform>(); // Good practice to get components early
        playerHealth = GetComponent<PlayerHealth>();
        characterAnimation = GetComponent<CharacterAnimation>();
        characterStats = GetComponent<CharacterStats>(); // Get stats component
        playerAIController = GetComponent<PlayerAIController>(); // Get AI controller

        // Error checking
        if (networkTransform == null) Debug.LogError("PlayerMovement: NetworkTransform not found!", this);
        if (playerHealth == null) Debug.LogError("PlayerMovement: PlayerHealth not found!", this);
        if (characterAnimation == null) Debug.LogError("PlayerMovement: CharacterAnimation not found!", this);
        if (characterStats == null) Debug.LogError("PlayerMovement: CharacterStats not found!", this); // Check stats
        // AI Controller is optional, so no error check here
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Components should already be fetched in Awake
        playerDataManager = PlayerDataManager.Instance;

        // We still need the owner-specific logic for NetworkTransform
        if (networkTransform != null)
        {
            networkTransform.enabled = !IsOwner; // Simplified: enable if not owner, disable if owner
        }

        // Determine which bounds to use based on PlayerRole
        if (playerDataManager != null)
        {
            // Use top-level PlayerData
            PlayerData? myData = playerDataManager.GetPlayerData(OwnerClientId);
            if (myData.HasValue)
            {
                if (myData.Value.Role == PlayerRole.Player1)
                {
                    currentBounds = player1Bounds;
                }
                else if (myData.Value.Role == PlayerRole.Player2)
                {
                    currentBounds = player2Bounds;
                }
                else
                {
                    currentBounds = new Rect(); // Default empty bounds if role is None or unexpected
                    Debug.LogError($"Owner {OwnerClientId} has unexpected Role {myData.Value.Role}. Applying default bounds.");
                }
            }
            else
            {
                 currentBounds = new Rect();
                 Debug.LogError($"Could not retrieve PlayerData for Owner {OwnerClientId}. Applying default bounds.");
            }
        }
        else
        {
             Debug.LogError("PlayerDataManager not found! Cannot determine player bounds.");
             currentBounds = new Rect();
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // Check ONLY invincibility to block player input
        if (playerHealth != null && playerHealth.IsInvincible.Value)
        {
            return; 
        }
        
        // If not invincible, client has control
        // Ensure NetworkTransform is disabled (might have been enabled temporarily before)
        if (networkTransform != null && networkTransform.enabled)
        {
             // This shouldn't strictly happen anymore with this logic, but keep as warning
             Debug.LogWarning($"[Client Owner {OwnerClientId}] NetworkTransform was enabled, disabling as client takes control.");
             networkTransform.enabled = false; 
        }

        // Proceed with normal client-side input and movement
        HandleInputAndMove();
    }

    private void HandleInputAndMove()
    {
        // Get raw input (returns -1, 0, or 1)
        // --- Modified: Check for AI control ---
        float horizontalInput = 0f;
        float verticalInput = 0f; // AI doesn't control vertical yet

        // Check if AI component exists and is active
        bool isAIControlled = playerAIController != null && playerAIController.IsAIActive();

        if (isAIControlled)
        {
            horizontalInput = aiHorizontalInput; // Use AI input
            // verticalInput remains 0
        }
        else
        {
            // Read from player input only if AI is not active/present
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
        }
        // --- End Modification ---

        // --- Tell CharacterAnimation about the input ---
        if (characterAnimation != null) // Check just in case
        {
            characterAnimation.SetHorizontalInput(horizontalInput); // Call the method again
        }
        // --- End Animation Input Call ---

        // Create input vector
        Vector2 currentInput = new Vector2(horizontalInput, verticalInput);

        // Normalize if moving diagonally to prevent faster speed
        if (currentInput.magnitude > 1.0f)
        {
            currentInput.Normalize();
        }

        // Determine current speed based on focus state (set externally)
        // Read base speed from CharacterStats
        float currentSpeed = characterStats.GetMoveSpeed(); 
        if (IsFocused) // Check the property set by PlayerFocusController
        {
            // Read modifier from CharacterStats
            currentSpeed *= characterStats.GetFocusSpeedModifier(); 
        }

        // Calculate movement
        Vector3 moveDirection = new Vector3(currentInput.x, currentInput.y, 0);
        Vector3 moveAmount = moveDirection * currentSpeed * Time.deltaTime;

        // Calculate potential next position
        Vector3 targetPosition = transform.position + moveAmount;

        // Clamp the position before applying it locally
        Vector3 clampedPosition = ClampPositionToBounds(targetPosition, currentBounds);

        // Apply movement locally using the clamped position
        transform.position = clampedPosition;
    }

    // Helper function to clamp position within a Rect - Make public
    public Vector3 ClampPositionToBounds(Vector3 position, Rect bounds)
    {
        return new Vector3(
            Mathf.Clamp(position.x, bounds.xMin, bounds.xMax),
            Mathf.Clamp(position.y, bounds.yMin, bounds.yMax),
            position.z // Keep original Z position
        );
    }

    /// <summary>
    /// Retrieves the PlayerRole for this player instance.
    /// Relies on PlayerDataManager being available.
    /// </summary>
    /// <returns>The PlayerRole (Player1, Player2) or PlayerRole.None if data is not found.</returns>
    public PlayerRole GetPlayerRole()
    {
        if (playerDataManager == null)
        {
            // Attempt to get the instance if it wasn't set in OnNetworkSpawn (e.g., called before spawn)
            playerDataManager = PlayerDataManager.Instance;
            if (playerDataManager == null)
            {
                 Debug.LogError($"[PlayerMovement GetPlayerRole] PlayerDataManager instance is null! Cannot determine role for {OwnerClientId}.");
                 return PlayerRole.None;
            }
        }

        // Use top-level PlayerData
        PlayerData? myData = playerDataManager.GetPlayerData(OwnerClientId);
        if (myData.HasValue)
        {
            return myData.Value.Role;
        }
        else
        {
             Debug.LogWarning($"[PlayerMovement GetPlayerRole] Could not retrieve PlayerData for Owner {OwnerClientId}. Returning None.");
             return PlayerRole.None;
        }
    }

    // --- Added: Method for AI to set horizontal input ---
    /// <summary>
    /// Allows the PlayerAIController to set the horizontal movement input.
    /// </summary>
    /// <param name="input">Horizontal input value (-1 to 1).</param>
    public void SetAIHorizontalInput(float input)
    {
        aiHorizontalInput = Mathf.Clamp(input, -1f, 1f);
    }
    // --------------------------------------------------
} 