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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkTransform = GetComponent<NetworkTransform>();
        playerDataManager = PlayerDataManager.Instance; // Cache instance

        // Determine which bounds to use
        if (playerDataManager != null)
        {
            PlayerDataManager.PlayerData? p1Data = playerDataManager.GetPlayer1Data();
            // PlayerDataManager.PlayerData? p2Data = playerDataManager.GetPlayer2Data(); // Not strictly needed here

            if (p1Data.HasValue && OwnerClientId == p1Data.Value.ClientId)
            {
                currentBounds = player1Bounds;
                Debug.Log($"Owner {OwnerClientId} is Player 1. Applying P1 bounds: {currentBounds}");
            }
            else
            {
                // Assume if not P1, must be P2 (or potentially spectator/error if >2 players)
                currentBounds = player2Bounds;
                 Debug.Log($"Owner {OwnerClientId} is Player 2. Applying P2 bounds: {currentBounds}");
            }
        }
        else
        {
             Debug.LogError("PlayerDataManager not found! Cannot determine player bounds.");
             // Default to some bounds or disable movement?
             currentBounds = new Rect(); // Prevent null ref, but player will be stuck at 0,0
        }

        if (IsOwner)
        {
            // Disable NetworkTransform for the owner
            // The owner will move itself locally in Update() and send RPCs
            // Non-owners will keep NetworkTransform enabled to sync from server state
            if (networkTransform != null)
            {
                networkTransform.enabled = false;
                Debug.Log($"Disabled NetworkTransform for owner {OwnerClientId}");
            }
            else
            {
                Debug.LogError("NetworkTransform component not found on player prefab!");
            }
        }
        // Optional: Log if NetworkTransform is enabled for non-owners
        // else if (networkTransform != null)
        // {
        //     Debug.Log($"NetworkTransform remains enabled for non-owner view of {OwnerClientId}");
        // }
    }

    void Update()
    {
        // Only the owner of this object should process input and move
        if (!IsOwner) return;

        // Add log to check position on owner client
        Debug.Log($"[Client Owner {OwnerClientId}] {gameObject.name} position is {transform.position} on frame {Time.frameCount}");

        // --- Client-Side Movement Logic ---
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

        // --- Send Position to Server at Intervals ---
        timeSinceLastSend += Time.deltaTime;

        if (moveAmount != Vector3.zero && timeSinceLastSend >= networkSendInterval)
        {
            // Send the *clamped* position
            SubmitPositionRequestServerRpc(transform.position); // Send current (clamped) position
            timeSinceLastSend = 0f; // Reset timer
        }
        // Consider sending one final update when stopping
        else if (moveAmount == Vector3.zero && timeSinceLastSend > 0f) // Check if we were moving and stopped
        { 
            // Send the *clamped* position
            SubmitPositionRequestServerRpc(transform.position); 
            timeSinceLastSend = 0f; // Reset timer (or set to negative to prevent immediate resend)
        }
    }

    // Helper function to clamp position within a Rect
    private Vector3 ClampPositionToBounds(Vector3 position, Rect bounds)
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
        // Determine bounds for the sender on the server
        Rect boundsForClient = player2Bounds; // Default assumption
        if (playerDataManager != null)
        {
             PlayerDataManager.PlayerData? p1Data = playerDataManager.GetPlayer1Data();
             if (p1Data.HasValue && rpcParams.Receive.SenderClientId == p1Data.Value.ClientId)
             {
                 boundsForClient = player1Bounds;
             }
        }
        
        // Clamp the received position on the server before applying
        Vector3 serverClampedPosition = ClampPositionToBounds(clientPosition, boundsForClient);

        // The server receives the position from the owning client
        // Update the position on the server using the server-clamped value.
        // NetworkTransform should handle replication.
        transform.position = serverClampedPosition;

        // Optional: Add server-side validation here if needed (e.g., check bounds)
        // Debug.Log($"Server received position {clientPosition} from client {rpcParams.Receive.SenderClientId}, clamped to {serverClampedPosition}");
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