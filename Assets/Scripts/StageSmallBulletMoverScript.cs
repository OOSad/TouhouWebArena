using UnityEngine;
using Unity.Netcode;

public class StageSmallBulletMoverScript : NetworkBehaviour
{
    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 5f;
    // Max deviation angle from straight down (in degrees)
    [SerializeField] private float maxAngleDeviation = 15f; 
    [SerializeField] private float maxLifetime = 15f; // Seconds before the bullet despawns

    // NetworkVariable to store the calculated velocity, writeable only by the server.
    private NetworkVariable<Vector3> SyncedVelocity = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);
    // NetworkVariable to store which player this bullet belongs to
    public NetworkVariable<PlayerRole> TargetPlayerRole { get; private set; } = new NetworkVariable<PlayerRole>(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private float currentLifetime;

    public override void OnNetworkSpawn()
    {
        // Movement logic and velocity calculation should only run on the server
        if (!IsServer) return;

        // --- Server-side Calculation --- 
        // Calculate random speed
        float speed = Random.Range(minSpeed, maxSpeed);

        // Calculate random angle deviation
        float randomAngle = Random.Range(-maxAngleDeviation, maxAngleDeviation);

        // Calculate direction based on the angle
        Vector3 direction = Quaternion.Euler(0, 0, randomAngle) * Vector3.down;

        // Calculate the final velocity vector
        Vector3 calculatedVelocity = direction.normalized * speed;

        // Store the calculated velocity in the NetworkVariable
        SyncedVelocity.Value = calculatedVelocity;
        // TargetPlayerRole should be set by the spawner *before* OnNetworkSpawn
        // --- End Server-side Calculation ---

        // Initialize lifetime timer on the server
        currentLifetime = maxLifetime;
    }

    private void Update()
    {
        // Only the server calculates and applies movement
        // NetworkTransform will sync the position to clients
        if (!IsServer) return;

        // Move using the velocity stored in the NetworkVariable
        transform.Translate(SyncedVelocity.Value * Time.deltaTime, Space.World);

        // --- Re-enabled --- //
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
        // --- End Re-enabled Section --- //

        // Optional: Add logic here to despawn the bullet if it goes off-screen
        // e.g., if (transform.position.y < -someBoundary) { GetComponent<NetworkObject>().Despawn(); }
    }
} 