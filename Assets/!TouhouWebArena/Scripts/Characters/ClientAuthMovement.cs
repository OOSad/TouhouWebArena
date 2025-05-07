using UnityEngine;
using Unity.Netcode;
// using Unity.Netcode.Components; // NetworkTransform is no longer strictly required by this script

/// <summary>
/// Handles client-authoritative movement for the player character.
/// Reads local input, applies movement directly. Uses NetworkVariables for position sync.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(PlayerFocusController))]
[RequireComponent(typeof(CharacterAnimation))]
// [RequireComponent(typeof(NetworkTransform))] // REMOVED - NetworkTransform is no longer a direct dependency of this script's logic
public class ClientAuthMovement : NetworkBehaviour
{
    // Movement Bounds (consider moving to a shared GameConstants or similar if used elsewhere)
    public static readonly Rect player1Bounds = new Rect(-8f, -4f, 7f, 8f);
    public static readonly Rect player2Bounds = new Rect(1f, -4f, 7f, 8f);
    private Rect currentBounds;

    // Component References
    private CharacterStats characterStats;
    private PlayerFocusController playerFocusController;
    private CharacterAnimation characterAnimation;
    private Rigidbody2D rb;

    // Input State
    private float horizontalInput;
    private float verticalInput;
    private bool isFocusing;

    // NetworkVariable for position synchronization
    private NetworkVariable<Vector2> NetworkedPosition = new NetworkVariable<Vector2>(
        Vector2.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );

    // [SerializeField] private float interpolationSpeed = 15f; // REMOVED - No interpolation for now

    /// <summary>
    /// If true, player movement input will be ignored. Controlled by other scripts (e.g., during invincibility).
    /// </summary>
    public bool IsMovementLocked { get; set; } = false;

    void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
        playerFocusController = GetComponent<PlayerFocusController>();
        characterAnimation = GetComponent<CharacterAnimation>();
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // We're controlling movement directly
        }
        else
        {
            Debug.LogError("ClientAuthMovement: Rigidbody2D not found!", this);
        }

        if (characterStats == null) Debug.LogError("ClientAuthMovement: CharacterStats not found!", this);
        if (playerFocusController == null) Debug.LogError("ClientAuthMovement: PlayerFocusController not found!", this);
        if (characterAnimation == null) Debug.LogError("ClientAuthMovement: CharacterAnimation not found!", this);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[ClientAuthMovement] OnNetworkSpawn for {gameObject.name}. IsOwner: {IsOwner}, IsClient: {IsClient}, IsServer: {IsServer}, OwnerClientId: {OwnerClientId}");

        PlayerDataManager playerDataManager = PlayerDataManager.Instance;
        if (IsOwner)
        {
            if (playerDataManager != null)
            {
                PlayerData? myData = playerDataManager.GetPlayerData(OwnerClientId);
                if (myData.HasValue)
                {
                    if (myData.Value.Role == PlayerRole.Player1) currentBounds = player1Bounds;
                    else if (myData.Value.Role == PlayerRole.Player2) currentBounds = player2Bounds;
                    else { currentBounds = new Rect(); Debug.LogWarning($"Owner {OwnerClientId} has unexpected Role {myData.Value.Role}. Movement may be unbounded."); }
                }
                else { currentBounds = new Rect(); Debug.LogWarning($"Could not retrieve PlayerData for Owner {OwnerClientId}. Movement may be unbounded."); }
            }
            else { Debug.LogError("PlayerDataManager not found! Cannot determine player bounds for Owner."); currentBounds = new Rect(); }
            
            // NetworkTransform logging removed as it's no longer a primary concern for this script's functionality.
        }
        // For non-owners, their movement will be driven by NetworkedPosition interpolation in Update.
        // We no longer disable the component for non-owners here.
    }

    void Update()
    {
        if (IsOwner)
        {
            // If movement is locked, clear any existing input and do not process new input.
            if (IsMovementLocked)
            {
                horizontalInput = 0f;
                verticalInput = 0f;
                // Optionally, ensure animation reflects no movement
                if (characterAnimation != null) characterAnimation.SetHorizontalInput(0f);
                return; // Skip reading new input
            }

            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            isFocusing = playerFocusController.IsFocusingNetworked;

            if (characterAnimation != null)
            {
                characterAnimation.SetHorizontalInput(horizontalInput);
            }
        }
        else
        {
            // Non-owner: Directly set position to NetworkedPosition (no interpolation)
            if (rb != null) 
            {
                // rb.position = Vector2.Lerp(rb.position, NetworkedPosition.Value, Time.deltaTime * interpolationSpeed); // REMOVED LERP
                rb.position = NetworkedPosition.Value; // DIRECT SET
            }
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return; // Only owner calculates and applies direct movement & updates NetworkVariable

        // If movement is locked, do not apply any movement logic.
        // The character will remain still, but NetworkedPosition will still reflect its last valid position.
        if (IsMovementLocked)
        {
            // It might be beneficial to still send the current (locked) position to ensure no stale values are kept by NetworkedPosition
            // if the lock starts/stops between FixedUpdate cycles. However, if input is zeroed, movement will be zero.
            // For absolute stillness, ensure movement vector is zero if lock is active.
            // NetworkedPosition.Value = rb.position; // Keep NetworkedPosition updated even if locked still.
            return; // Skip applying movement
        }

        float baseMoveSpeed = characterStats.GetMoveSpeed();
        float focusModifier = characterStats.GetFocusSpeedModifier();
        float currentSpeed = isFocusing ? baseMoveSpeed * focusModifier : baseMoveSpeed;

        Vector2 movement = new Vector2(horizontalInput, verticalInput).normalized * currentSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = rb.position + movement; // Vector3 for Clamp, rb.position is Vector2

        if (currentBounds != Rect.zero) 
        {
            newPosition.x = Mathf.Clamp(newPosition.x, currentBounds.xMin, currentBounds.xMax);
            newPosition.y = Mathf.Clamp(newPosition.y, currentBounds.yMin, currentBounds.yMax);
        }
        
        rb.MovePosition((Vector2)newPosition); // Cast back to Vector2 for MovePosition
        NetworkedPosition.Value = rb.position; // Update NetworkVariable with the new authoritative position
    }

    // Optional: If other scripts need to know the bounds
    public Rect GetCurrentBounds()
    {
        return currentBounds;
    }
} 