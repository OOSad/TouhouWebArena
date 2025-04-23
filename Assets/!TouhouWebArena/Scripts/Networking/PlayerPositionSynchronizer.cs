using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Works alongside client-authoritative <see cref="PlayerMovement"/> to synchronize
/// the player's position to the server periodically for validation.
/// The owning client sends its position at a fixed interval.
/// The server receives this position, clamps it within the correct player bounds
/// (determined via <see cref="PlayerDataManager"/>), and updates its own transform.
/// This server position is then replicated to other clients via <see cref="NetworkTransform"/>.
/// </summary>
[RequireComponent(typeof(PlayerMovement))] // Needs access to PlayerMovement data and methods
public class PlayerPositionSynchronizer : NetworkBehaviour
{
    [Header("Sync Settings")]
    /// <summary>
    /// The interval in seconds at which the client sends position updates to the server.
    /// </summary>
    [Tooltip("The interval in seconds at which the client sends position updates to the server.")]
    [SerializeField] private float networkSendInterval = 0.05f; // e.g., 0.05f = 20 updates per second

    /// <summary>Time elapsed since the last position update was sent.</summary>
    private float timeSinceLastSend = 0f;
    /// <summary>Cached reference to the PlayerMovement script on the same GameObject.</summary>
    private PlayerMovement playerMovement;
    /// <summary>Cached reference to the PlayerDataManager singleton (used only on server).</summary>
    private PlayerDataManager playerDataManager;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Caches the required <see cref="PlayerMovement"/> component.
    /// </summary>
    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerPositionSynchronizer requires a PlayerMovement component on the same GameObject.", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Called when the network object is spawned.
    /// Caches the <see cref="PlayerDataManager"/> instance (primarily needed on the server).
    /// </summary>
     public override void OnNetworkSpawn()
     {
        base.OnNetworkSpawn();
        // Get PlayerDataManager instance, primarily needed on server
        playerDataManager = PlayerDataManager.Instance;
        if (IsServer && playerDataManager == null)
        {
            Debug.LogWarning("PlayerPositionSynchronizer: PlayerDataManager instance not found on server. Cannot validate client positions accurately.", this);
        }
     }

    /// <summary>
    /// Called every frame.
    /// If this client is the owner, it increments the timer and calls <see cref="SubmitPositionRequestServerRpc"/>
    /// if the <see cref="networkSendInterval"/> has elapsed.
    /// </summary>
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

    /// <summary>
    /// [ServerRpc] Receives a position update from an owning client.
    /// Validates the position by clamping it within the appropriate bounds for that client's role
    /// (obtained via <see cref="GetBoundsForClient"/>).
    /// Sets the server's authoritative transform position to the clamped value.
    /// </summary>
    /// <param name="clientPosition">The position sent by the client.</param>
    /// <param name="rpcParams">Contains metadata about the RPC call, including the sender's ClientId.</param>
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

    /// <summary>
    /// [Server Only] Helper method to retrieve the correct movement bounds (<see cref="PlayerMovement.player1Bounds"/> or <see cref="PlayerMovement.player2Bounds"/>)
    /// for a given client based on their assigned <see cref="PlayerRole"/> stored in the <see cref="PlayerDataManager"/>.
    /// </summary>
    /// <param name="clientId">The ClientId of the player whose bounds are needed.</param>
    /// <returns>The Rect defining the movement bounds for the client, or an empty Rect if data is unavailable.</returns>
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
            else
            {
                Debug.LogWarning($"PlayerPositionSynchronizer: Client {clientId} has unexpected Role {senderData.Value.Role} in PlayerDataManager. Using default bounds.", this);
            }
        }
        else
        {
            Debug.LogWarning($"PlayerPositionSynchronizer: Could not find PlayerData for client {clientId}. Using default bounds.", this);
        }

        return new Rect(); // Default empty bounds
    }
}