using UnityEngine;
using Unity.Netcode;

public class StageSmallBulletMoverScript : NetworkBehaviour, IClearableByBomb
{
    [Header("Movement & Lifetime")] // Added header for clarity
    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 5f;
    // Max deviation angle from straight down (in degrees)
    [SerializeField] private float maxAngleDeviation = 15f; 
    [SerializeField] private float maxLifetime = 15f; // Seconds before the bullet despawns

    [Header("Behavior")] // Added header
    [SerializeField] private bool isImmuneToShockwave = false; // Set true for Large Bullets

    // --- Networked State --- 
    // Store the calculated/set velocity, writeable only by the server.
    private NetworkVariable<Vector3> SyncedVelocity = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);
    // NetworkVariable to store which player this bullet belongs to
    public NetworkVariable<PlayerRole> TargetPlayerRole { get; private set; } = new NetworkVariable<PlayerRole>(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Flag to indicate if a specific velocity should be used instead of random calculation
    public NetworkVariable<bool> UseInitialVelocity { get; private set; } = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // The specific velocity to use if UseInitialVelocity is true
    public NetworkVariable<Vector3> InitialVelocity { get; private set; } = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // -----------------------

    private float currentLifetime;

    public override void OnNetworkSpawn()
    {
        // Movement logic and velocity calculation should only run on the server
        if (!IsServer) return;

        // --- Server-side Velocity Calculation/Setting --- 
        Vector3 calculatedVelocity;
        if (UseInitialVelocity.Value)
        {
            // Use the velocity provided by the spawner
            calculatedVelocity = InitialVelocity.Value;
        }
        else
        {
            // Calculate random speed
            float speed = Random.Range(minSpeed, maxSpeed);
            // Calculate random angle deviation
            float randomAngle = Random.Range(-maxAngleDeviation, maxAngleDeviation);
            // Calculate direction based on the angle
            Vector3 direction = Quaternion.Euler(0, 0, randomAngle) * Vector3.down;
            // Calculate the final velocity vector
            calculatedVelocity = direction.normalized * speed;
        }

        // Store the final velocity in the NetworkVariable for movement
        SyncedVelocity.Value = calculatedVelocity;
        // TargetPlayerRole should be set by the spawner *after* OnNetworkSpawn
        // --- End Server-side Calculation ---

        // Initialize lifetime timer on the server
        currentLifetime = maxLifetime;
    }

    // --- Public getter for speed (used by SpiritController) ---
    public float GetMinSpeed() { return minSpeed; }
    // ---------------------------------------------------------

    private void Update()
    {
        // Only the server calculates and applies movement
        // NetworkTransform will sync the position to clients
        if (!IsServer) return;

        // Move using the velocity stored in the NetworkVariable
        transform.Translate(SyncedVelocity.Value * Time.deltaTime, Space.World);

        // --- Lifetime Check (Server) ---
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0f)
        {
            // Despawn the network object (will destroy it on all clients)
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Despawn();
            }
            // No need to Destroy(gameObject) explicitly, Despawn handles it.
            return; // Exit Update early since the object is being destroyed
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the bullet collided with a shockwave
        if (other.CompareTag("FairyShockwave"))
        {
            // --- NEW: Check Immunity Flag --- 
            if (isImmuneToShockwave)
            {
                return; // Do nothing if immune
            }
            // ----------------------------------

            // Only the server should handle despawning networked objects
            if (IsServer)
            {
                // Despawn the bullet
                NetworkObject networkObject = GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn(true); // Pass true to destroy the GameObject as well
                }
            }
            // Note: Clients do not despawn directly. The server action will synchronize.
        }
    }

    #region IClearableByBomb Implementation

    /// <summary>
    /// Called when the player's death bomb effect should clear this bullet.
    /// Directly performs the server-side despawn if called on the server.
    /// </summary>
    /// <param name="bombingPlayer">The role of the player who activated the bomb (unused by bullets).</param>
    public void ClearByBomb(PlayerRole bombingPlayer)
    {
        // Since PlayerDeathBomb runs on server, this is called on the server instance.
        // We can directly perform the despawn.
        if (!IsServer) 
        {
            Debug.LogWarning($"[Bullet {GetComponent<NetworkObject>()?.NetworkObjectId ?? 0}] ClearByBomb called on non-server. Ignoring.");
            return;
        }

        // Despawn the network object (will destroy it on all clients)
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn(true); // Pass true to destroy the GameObject as well
        }
        else 
        {
             // Log if despawn fails
            ulong netId = networkObject?.NetworkObjectId ?? 0;
            bool spawned = networkObject?.IsSpawned ?? false;
            Debug.LogWarning($"[Server Bullet {netId}] Failed to despawn from ClearByBomb - NetworkObject null or not spawned. IsSpawned={spawned}");
        }
    }

    // ServerRpc called by ClearByBomb() - NO LONGER NEEDED
    /*
    [ServerRpc(RequireOwnership = false)] // Allow any client (or server) to trigger this
    private void RequestClearByBombServerRpc(ServerRpcParams rpcParams = default)
    {
        // Debug.Log($"[Server Bullet {GetComponent<NetworkObject>()?.NetworkObjectId ?? 0}] RPC RequestClearByBombServerRpc received from client {rpcParams.Receive.SenderClientId}."); // <-- REMOVE LOG

        // Despawn the network object (will destroy it on all clients)
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
             // Debug.Log($"[Server Bullet {networkObject.NetworkObjectId}] Attempting to despawn self due to bomb request."); // <-- REMOVE LOG
            networkObject.Despawn(true); // Pass true to destroy the GameObject as well
        }
         else 
        {
            // Keep this warning
            ulong netId = networkObject?.NetworkObjectId ?? 0;
            bool spawned = networkObject?.IsSpawned ?? false;
            Debug.LogWarning($"[Server Bullet {netId}] Failed to despawn due to bomb - NetworkObject null or not spawned. IsSpawned={spawned}"); 
        }
    }
    */
    #endregion
} 