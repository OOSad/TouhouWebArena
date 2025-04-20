using UnityEngine;
using Unity.Netcode;

// Handles synchronizing the player's position from client to server periodically
[RequireComponent(typeof(PlayerMovement))] // Needs access to PlayerMovement data
public class PlayerPositionSynchronizer : NetworkBehaviour
{
    [Header("Sync Settings")]
    [SerializeField] private float networkSendInterval = 0.05f; // Send position updates 20 times per second

    // Private variables
    private float timeSinceLastSend = 0f;
    private PlayerMovement playerMovement; // Reference to the core movement script
    private PlayerDataManager playerDataManager; // Needed for server-side role check

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerPositionSynchronizer requires a PlayerMovement component!", this);
            enabled = false;
        }
    }

     public override void OnNetworkSpawn()
     {
        base.OnNetworkSpawn();
        // Get PlayerDataManager instance, primarily needed on server
        playerDataManager = PlayerDataManager.Instance;
        if (IsServer && playerDataManager == null)
        {
            Debug.LogError("[Server PlayerPositionSynchronizer] PlayerDataManager instance not found!", this);
            // Decide if this is critical enough to disable server RPC logic?
        }
     }

    void Update()
    {
        // Only the owner sends position updates
        if (!IsOwner || playerMovement == null) return;

        // Basic timer logic - send if enough time passed
        // Note: Original logic also sent if movement stopped. We can simplify or replicate.
        // Let's simplify: only send on timer interval. NetworkTransform handles interpolation.
        timeSinceLastSend += Time.deltaTime;
        if (timeSinceLastSend >= networkSendInterval)
        {
            SubmitPositionRequestServerRpc(playerMovement.transform.position);
            timeSinceLastSend = 0f;
        }
    }

    [ServerRpc]
    private void SubmitPositionRequestServerRpc(Vector3 clientPosition, ServerRpcParams rpcParams = default)
    {
        if (playerMovement == null) return; // Safety check

        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // Determine bounds for the sender on the server based on Role
        Rect boundsForClient = GetBoundsForClient(senderClientId);

        // Clamp the received position on the server
        Vector3 clampedPosition = playerMovement.ClampPositionToBounds(clientPosition, boundsForClient);

        // Set the server's authoritative position
        // This assumes PlayerMovement and this script are on the same GameObject
        transform.position = clampedPosition;
    }

    // Helper to get bounds on the server based on client ID
    private Rect GetBoundsForClient(ulong clientId)
    {
        if (!IsServer || playerDataManager == null) return new Rect(); // Safety checks

        PlayerData? senderData = playerDataManager.GetPlayerData(clientId);
        if (senderData.HasValue)
        {
            if (senderData.Value.Role == PlayerRole.Player1)
            {
                return PlayerMovement.player1Bounds; // Access static bounds
            }
            else if (senderData.Value.Role == PlayerRole.Player2)
            {
                return PlayerMovement.player2Bounds; // Access static bounds
            }
            else Debug.LogWarning($"[ServerSync] Sender {clientId} has unexpected Role {senderData.Value.Role}. Using default bounds.");
        }
        else Debug.LogError($"[ServerSync] Could not retrieve PlayerData for Sender {clientId}. Using default bounds.");

        return new Rect(); // Default empty bounds
    }
} 