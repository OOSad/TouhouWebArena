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
    /// Relies on the Physics 2D Layer Collision Matrix to ensure only relevant objects (e.g., EnemyProjectiles layer)
    /// trigger this event. Checks for player invincibility before processing hits.
    /// If a valid hit occurs, despawns the projectile and calls <see cref="PlayerHealth.TakeDamage"/>.
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

        // --- REMOVED Tag Check --- 
        // The Layer Collision Matrix should now filter collisions, so any object 
        // reaching this point is assumed to be a valid projectile.
        // if (other.CompareTag("StageBullet")) 
        // { ... } // <--- Removed the surrounding if statement

        // Despawn the projectile on the server (will remove it for all clients)
        NetworkObject projectileNetworkObject = other.GetComponent<NetworkObject>(); // Renamed for clarity
        if (projectileNetworkObject != null)
        {
            // Consider using ReturnToPool if projectiles are pooled and have a specific script
            // For now, just despawn.
            projectileNetworkObject.Despawn(); 
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
        // --- End Original Tag Check Block --- 
    }
} 