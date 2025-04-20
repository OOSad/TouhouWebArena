using UnityEngine;
using Unity.Netcode;

// Require NetworkObject as this script assumes the bullet is networked
[RequireComponent(typeof(NetworkObject))]
public class BulletMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 10f; // Speed of the bullet
    [SerializeField] private float bulletLifetime = 3.0f; // Seconds before the bullet despawns automatically

    // NetworkVariable to identify the owner
    public NetworkVariable<PlayerRole> OwnerRole { get; private set; } = 
        new NetworkVariable<PlayerRole>(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

    // Server-side collision detection
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // Only server handles collisions

        // --- NEW: Check for Shockwave collision --- 
        if (other.CompareTag("FairyShockwave")) // Ensure Shockwave prefab has this tag
        {
            DespawnBullet();
            return; // Bullet is destroyed, no need for further checks
        }
        // ----------------------------------------

        // Check if we hit a fairy (using the "Fairy" tag)
        if (other.CompareTag("Fairy")) // Correct check
        {
            Debug.Log($"Bullet owned by {OwnerRole.Value} hit Fairy: {other.name}");

            // Try to get the fairy script
            Fairy fairy = other.GetComponent<Fairy>();
            if (fairy != null)
            {
                // Call the server-side lethal damage method directly
                fairy.ApplyLethalDamage(OwnerRole.Value); // Correct call

                // Despawn bullet immediately after hitting a fairy
                DespawnBullet();
            }
            else
            {
                Debug.LogWarning($"Bullet hit object tagged Fairy, but couldn't find Fairy script on {other.name}");
            }
        }
        // --- NEW: Check if we hit a Spirit --- 
        else if (other.CompareTag("Spirit")) // Add check for Spirit tag
        {
            Debug.Log($"Bullet owned by {OwnerRole.Value} hit Spirit: {other.name}");

            // No need to apply damage here, SpiritController handles it.
            // Just despawn the bullet.
            DespawnBullet();
        }
        // ------------------------------------

        // Optional: Add checks for other collidable objects here (e.g., environment)
        // else if (other.CompareTag("Wall")) { DespawnBullet(); }
    }

    // Method called by Invoke on the server to despawn the bullet
    // Made public so ClearByBomb can call it
    public void DespawnBullet()
    {
        // Check if the object hasn't already been destroyed and is still spawned
        if (gameObject != null && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); // True to destroy the object after despawning
        }
    }
} 