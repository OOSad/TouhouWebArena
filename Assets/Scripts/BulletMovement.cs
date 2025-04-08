using UnityEngine;
using Unity.Netcode;

// Require NetworkObject as this script assumes the bullet is networked
[RequireComponent(typeof(NetworkObject))]
public class BulletMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 10f; // Speed of the bullet
    [SerializeField] private float bulletLifetime = 3.0f; // Seconds before the bullet despawns automatically

    // Store the initial forward direction (local up)
    // private Vector3 initialForwardDirection;

    public override void OnNetworkSpawn()
    {
        // Only the server should manage the lifetime and despawning
        if (IsServer)
        {
            Invoke(nameof(DespawnBullet), bulletLifetime);
        }
    }

    void Update()
    {
        // --- Server-Authoritative Movement ---
        if (IsServer) // Re-added server check
        {
            // Move the bullet forward based on its local 'up' direction
            // Assumes the bullet sprite/model is oriented so 'up' is forward
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.Self);
        }

        // Server still handles despawn timer (via Invoke in OnNetworkSpawn)
        // Clients rely on NetworkTransform primarily for correction/late-join sync now
        // With interpolation enabled, NetworkTransform provides smooth movement on clients.
    }

    // Method called by Invoke on the server to despawn the bullet
    private void DespawnBullet()
    {
        // Check if the object hasn't already been destroyed and is still spawned
        if (gameObject != null && NetworkObject != null && NetworkObject.IsSpawned)
        {
            Debug.Log($"Despawning bullet {gameObject.name} due to lifetime.");
            NetworkObject.Despawn(true); // True to destroy the object after despawning
        }
    }

    // Example placeholder for despawn logic
    // private void CheckBoundsAndDespawn()
    // {
    //     // Example: Check if bullet is way off screen
    //     if (Mathf.Abs(transform.position.y) > 20f || Mathf.Abs(transform.position.x) > 15f)
    //     {
    //         Debug.Log($"Despawning bullet {gameObject.name} due to out of bounds.");
    //         GetComponent<NetworkObject>().Despawn(true); // Despawn and destroy
    //     }
    // }
} 