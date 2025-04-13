using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // Added for NetworkTransform

[RequireComponent(typeof(NetworkObject))] // Ensure NetworkObject is present
[RequireComponent(typeof(Rigidbody2D))] // Keep this if other logic relies on it
[RequireComponent(typeof(CharacterAnimation))] // Add this back
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f; // Speed of the character
    [SerializeField] private float focusSpeedModifier = 0.5f; // Speed multiplier when focused
    [SerializeField] private float networkSendInterval = 0.05f; // Send position updates 20 times per second (Adjusted based on user feedback)

    // [Header("Boundaries")] // Removed Header
    // [SerializeField] private Rect player1Bounds = new Rect(-8f, -4.5f, 7.5f, 9f); // Example Left Bounds (X, Y, Width, Height)
    // [SerializeField] private Rect player2Bounds = new Rect(0.5f, -4.5f, 7.5f, 9f); // Example Right Bounds
    private static readonly Rect player1Bounds = new Rect(-8f, -4f, 7f, 8f); // Hardcoded Left Bounds
    private static readonly Rect player2Bounds = new Rect(1f, -4f, 7f, 8f); // Hardcoded Right Bounds

    // Private variables
    private float timeSinceLastSend = 0f;
    private NetworkTransform networkTransform;
    private Rect currentBounds; // Which bounds apply to this player instance
    private PlayerDataManager playerDataManager; // To check P1/P2
    private PlayerHealth playerHealth;
    private CharacterAnimation characterAnimation; // Add this reference back

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

        // Error checking
        if (networkTransform == null) Debug.LogError("PlayerMovement: NetworkTransform not found!", this);
        if (playerHealth == null) Debug.LogError("PlayerMovement: PlayerHealth not found!", this);
        if (characterAnimation == null) Debug.LogError("PlayerMovement: CharacterAnimation not found!", this);
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
            PlayerDataManager.PlayerData? myData = playerDataManager.GetPlayerData(OwnerClientId);
            if (myData.HasValue)
            {
                if (myData.Value.Role == PlayerRole.Player1)
                {
                    currentBounds = player1Bounds;
                    Debug.Log($"[PlayerMovement OnNetworkSpawn] Owner {OwnerClientId} assigned Player 1 bounds.");
                }
                else if (myData.Value.Role == PlayerRole.Player2)
                {
                    currentBounds = player2Bounds;
                    Debug.Log($"[PlayerMovement OnNetworkSpawn] Owner {OwnerClientId} assigned Player 2 bounds.");
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
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

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
        float currentSpeed = moveSpeed;
        if (IsFocused) // Check the property set by PlayerFocusController
        {
            currentSpeed *= focusSpeedModifier;
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

        // --- Send Position to Server at Intervals (only if client has control) ---
        timeSinceLastSend += Time.deltaTime;
        if (moveAmount != Vector3.zero && timeSinceLastSend >= networkSendInterval)
        {
            SubmitPositionRequestServerRpc(transform.position); 
            timeSinceLastSend = 0f;
        }
        else if (moveAmount == Vector3.zero && timeSinceLastSend > 0f) 
        {
            SubmitPositionRequestServerRpc(transform.position);
            timeSinceLastSend = 0f; 
        }
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

        PlayerDataManager.PlayerData? myData = playerDataManager.GetPlayerData(OwnerClientId);
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

    [ServerRpc]
    private void SubmitPositionRequestServerRpc(Vector3 clientPosition, ServerRpcParams rpcParams = default)
    {
        // Removed check for server knockback state
        // if (playerHealth != null && playerHealth.IsServerKnockingBack)
        // {
        //    return; 
        // }
        
        // Determine bounds for the sender on the server based on Role
        Rect boundsForClient = new Rect(); // Default to empty
        if (playerDataManager != null)
        {
             PlayerDataManager.PlayerData? senderData = playerDataManager.GetPlayerData(rpcParams.Receive.SenderClientId);
             if (senderData.HasValue)
             {
                 if (senderData.Value.Role == PlayerRole.Player1)
                 {
                     boundsForClient = player1Bounds;
                 }
                 else if (senderData.Value.Role == PlayerRole.Player2)
                 {
                     boundsForClient = player2Bounds;
                 }
                 else
                 {
                     Debug.LogWarning($"[ServerRPC] Sender {rpcParams.Receive.SenderClientId} has unexpected Role {senderData.Value.Role}. Using default bounds.");
                 }
             }
             else
             {
                 Debug.LogError($"[ServerRPC] Could not retrieve PlayerData for Sender {rpcParams.Receive.SenderClientId}. Using default bounds.");
             }
        }
        else
        {
            Debug.LogError("[ServerRPC] PlayerDataManager not found! Using default bounds.");
        }
        
        // Clamp and apply position on server
        Vector3 serverClampedPosition = ClampPositionToBounds(clientPosition, boundsForClient);
        transform.position = serverClampedPosition;
    }

    /* // Removed Gizmo code
    // Draw boundary gizmos in the Scene view when the object is selected
    private void OnDrawGizmosSelected()
    {
        // Draw Player 1 Bounds (e.g., Red)
        Gizmos.color = Color.red;
        Vector3 p1Center = new Vector3(player1Bounds.center.x, player1Bounds.center.y, transform.position.z);
        Vector3 p1Size = new Vector3(player1Bounds.size.x, player1Bounds.size.y, 0.1f); // Add small depth for visibility
        Gizmos.DrawWireCube(p1Center, p1Size);

        // Draw Player 2 Bounds (e.g., Blue)
        Gizmos.color = Color.blue;
        Vector3 p2Center = new Vector3(player2Bounds.center.x, player2Bounds.center.y, transform.position.z);
        Vector3 p2Size = new Vector3(player2Bounds.size.x, player2Bounds.size.y, 0.1f); // Add small depth for visibility
        Gizmos.DrawWireCube(p2Center, p2Size);
    }
    */
} 