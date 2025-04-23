using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // Added for NetworkTransform

/// <summary>
/// Handles client-authoritative movement for player characters.
/// Reads input locally on the owning client, calculates movement based on current speed (normal or focused),
/// clamps the position within defined bounds, and applies the movement directly to the transform.
/// The position is synchronized to other clients via the <see cref="NetworkTransform"/> component,
/// which is disabled on the owning client to prevent conflicts.
/// Also interacts with <see cref="CharacterStats"/>, <see cref="CharacterAnimation"/>, <see cref="PlayerHealth"/>,
/// and optionally <see cref="PlayerAIController"/>.
/// </summary>
[RequireComponent(typeof(NetworkObject))] // Ensure NetworkObject is present
[RequireComponent(typeof(Rigidbody2D))] // Keep this if other logic relies on it
[RequireComponent(typeof(CharacterAnimation))] // Add this back
[RequireComponent(typeof(PlayerPositionSynchronizer))]
[RequireComponent(typeof(CharacterStats))] // Added
public class PlayerMovement : NetworkBehaviour
{
    /// <summary>
    /// Hardcoded movement bounds for Player 1 (left side of the screen).
    /// Accessed publicly by other scripts (e.g., PlayerDeathBomb).
    /// </summary>
    public static readonly Rect player1Bounds = new Rect(-8f, -4f, 7f, 8f); // Public Hardcoded Left Bounds
    /// <summary>
    /// Hardcoded movement bounds for Player 2 (right side of the screen).
    /// Accessed publicly by other scripts.
    /// </summary>
    public static readonly Rect player2Bounds = new Rect(1f, -4f, 7f, 8f); // Public Hardcoded Right Bounds

    // --- Component References ---
    /// <summary>Reference to the NetworkTransform component for state synchronization (disabled for owner).</summary>
    private NetworkTransform networkTransform;
    /// <summary>Reference to the PlayerDataManager singleton to determine player role and bounds.</summary>
    private PlayerDataManager playerDataManager;
    /// <summary>Reference to the PlayerHealth component to check for invincibility.</summary>
    private PlayerHealth playerHealth;
    /// <summary>Reference to the CharacterAnimation component to update visual state.</summary>
    private CharacterAnimation characterAnimation;
    /// <summary>Reference to the CharacterStats component to get movement speeds.</summary>
    private CharacterStats characterStats;
    /// <summary>Optional reference to the PlayerAIController for AI-driven movement.</summary>
    private PlayerAIController playerAIController;

    // --- State Variables ---
    /// <summary>The movement bounds currently applied to this player instance (determined in OnNetworkSpawn).</summary>
    private Rect currentBounds;
    /// <summary>Stores the horizontal movement input provided by the AI controller, if active.</summary>
    private float aiHorizontalInput = 0f;

    /// <summary>
    /// Gets the movement bounds currently applied to this player instance.
    /// </summary>
    public Rect CurrentBounds => currentBounds;

    /// <summary>
    /// Gets or sets whether the player is currently in focus mode (slowed movement).
    /// This is typically controlled by an external script like PlayerFocusController.
    /// </summary>
    public bool IsFocused { get; set; } = false;

    /// <summary>
    /// Called once when the script instance is first loaded.
    /// Used to cache required component references reliably before network spawning.
    /// </summary>
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

    /// <summary>
    /// Called when the NetworkObject associated with this script is spawned across the network.
    /// Fetches the PlayerDataManager instance, disables the NetworkTransform for the owner,
    /// and determines the appropriate movement <see cref="currentBounds"/> based on the player's role.
    /// </summary>
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

    /// <summary>
    /// Called every frame.
    /// If this client is the owner and the player is not invincible, it calls <see cref="HandleInputAndMove"/>
    /// to process input and update the player's position locally.
    /// Ensures the NetworkTransform remains disabled for the owner.
    /// </summary>
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

    /// <summary>
    /// Handles reading input (either from player or AI), calculating the movement vector based on speed and focus state,
    /// clamping the target position within bounds, and applying the final position to the transform.
    /// Also updates the <see cref="CharacterAnimation"/> component.
    /// This method is called only on the owning client.
    /// </summary>
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

    /// <summary>
    /// Clamps the given position vector within the provided rectangular bounds.
    /// </summary>
    /// <param name="position">The position to clamp.</param>
    /// <param name="bounds">The Rect defining the minimum and maximum X and Y coordinates.</param>
    /// <returns>The clamped position vector.</returns>
    public Vector3 ClampPositionToBounds(Vector3 position, Rect bounds)
    {
        return new Vector3(
            Mathf.Clamp(position.x, bounds.xMin, bounds.xMax),
            Mathf.Clamp(position.y, bounds.yMin, bounds.yMax),
            position.z // Keep original Z position
        );
    }

    /// <summary>
    /// Retrieves the <see cref="PlayerRole"/> (Player1 or Player2) for this player instance.
    /// Relies on the <see cref="PlayerDataManager"/> being available.
    /// </summary>
    /// <returns>The <see cref="PlayerRole"/>, or <see cref="PlayerRole.None"/> if data cannot be retrieved.</returns>
    public PlayerRole GetPlayerRole()
    {
        if (playerDataManager == null)
        {
            // Moved the GetInstance call here
            playerDataManager = PlayerDataManager.Instance;
            if (playerDataManager == null)
            {
                // Still null after trying to get instance
                
                return PlayerRole.None;
            }
        }

        PlayerData? myData = playerDataManager.GetPlayerData(OwnerClientId);
        if (myData.HasValue)
        {
            return myData.Value.Role;
        }
        else
        {
            // Removed the warning log
            return PlayerRole.None;
        }
    }

    // --- AI Control Method ---
    /// <summary>
    /// Allows the <see cref="PlayerAIController"/> (or other scripts) to set the horizontal movement input externally.
    /// The input value is clamped between -1 and 1.
    /// </summary>
    /// <param name="input">Horizontal input value (-1 for left, 1 for right, 0 for none).</param>
    public void SetAIHorizontalInput(float input)
    {
        aiHorizontalInput = Mathf.Clamp(input, -1f, 1f);
    }
    // --------------------------------------------------
} 