using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Attached to the player's hitbox GameObject (which must have a Collider2D set to IsTrigger).
/// Detects collisions with damaging objects (e.g., bullets) on the server side.
/// Checks for player invincibility via <see cref="PlayerHealth"/> before processing hits.
/// If a valid hit occurs, it despawns the projectile and calls <see cref="PlayerHealth.TakeDamage(int)"/> on the server.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlayerHitbox : NetworkBehaviour
{
    /// <summary>Cached reference to the PlayerHealth component on the parent player object.</summary>
    private PlayerHealth playerHealth;
    /// <summary>Cached reference to the Collider2D component on this GameObject.</summary>
    private Collider2D hitboxCollider;
    // Note: Original 'canTakeDamage' bool was redundant with PlayerHealth.IsInvincible check.

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// Caches references to the <see cref="PlayerHealth"/> and <see cref="hitboxCollider"/> components.
    /// Validates that PlayerHealth exists and the collider is set to trigger.
    /// </summary>
    void Start()
    {
        // Find the health script on the root parent object
        playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHitbox could not find PlayerHealth script on parent!", this);
            enabled = false; // Disable if setup is wrong
        }

        hitboxCollider = GetComponent<Collider2D>();
        if (!hitboxCollider.isTrigger)
        {
            Debug.LogWarning("Hitbox Collider2D is not set to 'Is Trigger'. Interactions may not work as expected.", this);
            // Optionally force it: hitboxCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// [Server Only] Called by Unity's physics engine when another Collider2D enters this trigger.
    /// Checks if the server is running and if the player is currently invincible.
    /// Filters for collisions with objects tagged "StageBullet".
    /// If a valid bullet hit occurs, despawns the bullet and calls <see cref="PlayerHealth.TakeDamage"/>.
    /// </summary>
    /// <param name="other">The Collider2D of the object that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; 
        
        // --- Invincibility Check ---
        if (playerHealth != null && playerHealth.IsInvincible.Value) 
        {
            return; // Ignore hit if invincible
        }
        // --- End Invincibility Check ---

        // Check if the colliding object is a stage bullet (adjust tag if needed)
        if (other.CompareTag("StageBullet"))
        {
            // Despawn the bullet on the server (will remove it for all clients)
            NetworkObject bulletNetworkObject = other.GetComponent<NetworkObject>();
            if (bulletNetworkObject != null)
            {
                bulletNetworkObject.Despawn();
            }

            // Process damage application on the server
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }
            else
            {
                // This shouldn't happen if Start() check passed, but log just in case.
                Debug.LogError($"[Server] Hitbox collided, but PlayerHealth reference is missing on {transform.root.name}!");
            }
        }
        // Optional: else if (other.CompareTag("OtherEnemyAttack")) { ... }
    }
} 