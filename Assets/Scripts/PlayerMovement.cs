using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // Added for NetworkTransform

[RequireComponent(typeof(NetworkObject))] // Ensure NetworkObject is present
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f; // Speed of the character
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

    // Public property to access the current bounds (read-only from outside)
    public Rect CurrentBounds => currentBounds; 

    // Reference to the health component
    private PlayerHealth playerHealth;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkTransform = GetComponent<NetworkTransform>();
        playerDataManager = PlayerDataManager.Instance; // Cache instance

        // Get the health component
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerMovement could not find PlayerHealth component!", this);
            // Decide if movement should be disabled entirely
            // enabled = false; 
        }

        // Determine which bounds to use
        if (playerDataManager != null)
        {
            PlayerDataManager.PlayerData? p1Data = playerDataManager.GetPlayer1Data();
            // PlayerDataManager.PlayerData? p2Data = playerDataManager.GetPlayer2Data(); // Not strictly needed here

            if (OwnerClientId == 1) // Assuming Player 1 is Owner Client ID 1
            {
                currentBounds = player1Bounds;
                // Debug.Log($"Owner {OwnerClientId} is Player 1. Applying P1 bounds: {currentBounds}");
            }
            else if (OwnerClientId == 2) // Assuming Player 2 is Owner Client ID 2
            {
                currentBounds = player2Bounds;
                // Debug.Log($"Owner {OwnerClientId} is Player 2. Applying P2 bounds: {currentBounds}");
            }
            else
            {
                // Assume if not P1 or P2, must be spectator/error if >2 players
                currentBounds = new Rect(); // Prevent null ref, but player will be stuck at 0,0
                Debug.LogError($"Owner {OwnerClientId} is not recognized as Player 1 or Player 2. Applying default bounds: {currentBounds}");
            }
        }
        else
        {
             Debug.LogError("PlayerDataManager not found! Cannot determine player bounds.");
             // Default to some bounds or disable movement?
             currentBounds = new Rect(); // Prevent null ref, but player will be stuck at 0,0
        }

        // Reset NetworkTransform state based purely on ownership
        if (networkTransform != null)
        {
            if (IsOwner)
            {
                networkTransform.enabled = false;
                // Debug.Log($"Owner {OwnerClientId} disabled NetworkTransform.");
            }
            else
            { 
                networkTransform.enabled = true; 
                // Debug.Log($"Non-owner {OwnerClientId} enabled NetworkTransform.");
            }
        }
        else
        {
            Debug.LogError("NetworkTransform component not found!");
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

        // Create input vector
        Vector2 currentInput = new Vector2(horizontalInput, verticalInput);

        // Normalize if moving diagonally to prevent faster speed
        if (currentInput.magnitude > 1.0f)
        {
            currentInput.Normalize();
        }

        // Calculate movement
        Vector3 moveDirection = new Vector3(currentInput.x, currentInput.y, 0);
        Vector3 moveAmount = moveDirection * moveSpeed * Time.deltaTime;

        // Calculate potential next position
        Vector3 targetPosition = transform.position + moveAmount;

        // Clamp the position before applying it locally
        Vector3 clampedPosition = ClampPositionToBounds(targetPosition, currentBounds);

        // Apply movement locally using the clamped position
        // transform.position += moveAmount; // Old way
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

    [ServerRpc]
    private void SubmitPositionRequestServerRpc(Vector3 clientPosition, ServerRpcParams rpcParams = default)
    {
        // Removed check for server knockback state
        // if (playerHealth != null && playerHealth.IsServerKnockingBack)
        // {
        //    return; 
        // }
        
        // Determine bounds for the sender on the server
        Rect boundsForClient = player2Bounds; 
        if (playerDataManager != null)
        {
             PlayerDataManager.PlayerData? p1Data = playerDataManager.GetPlayer1Data();
             if (p1Data.HasValue && rpcParams.Receive.SenderClientId == p1Data.Value.ClientId)
             {
                 boundsForClient = player1Bounds;
             }
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