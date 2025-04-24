using UnityEngine;
using Unity.Netcode;
// using TouhouWebArena.Spellcards.Behaviors; // No longer needed as we don't check the component

/// <summary>
/// Attached to the player's hitbox GameObject (which must have a Collider2D set to IsTrigger).
/// Detects collisions with damaging objects (e.g., bullets, enemies) based on Physics Layer settings.
/// Checks for player invincibility via <see cref="PlayerHealth"/> before processing hits.
/// If a valid hit occurs, it calls <see cref="PlayerHealth.TakeDamage(int)"/> on the server.
/// **It does NOT destroy the colliding object.**
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
    /// Relies on the Physics 2D Layer Collision Matrix to ensure only relevant objects trigger this event.
    /// Checks for player invincibility before processing hits.
    /// If a valid hit occurs, calls <see cref="PlayerHealth.TakeDamage"/> but does NOT destroy the collider.
    /// </summary>
    /// <param name="other">The Collider2D of the object that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; 
        
        // --- Invincibility Check ---
        if (playerHealth != null && playerHealth.IsInvincible.Value) 
        {
            // Debug.Log($"[Server] Player invincible, ignoring collision with {other.name}");
            return; // Ignore hit if invincible
        }
        // --- End Invincibility Check ---

        // --- Apply Damage --- 
        // Assume Layer Matrix filtered correctly. Any object reaching here deals damage.
        // DO NOT DESPAWN the 'other' object here.
        if (playerHealth != null)
        {
            // Debug.Log($"[Server] PlayerHitbox applying damage from {other.name}");
            playerHealth.TakeDamage(1); 
        }
        else
        {
            // This shouldn't happen if Start() check passed, but log just in case.
            Debug.LogError($"[Server] Hitbox collided with {other.name}, but PlayerHealth reference is missing on {transform.root.name}! Cannot apply damage.");
        }
        // --- End Apply Damage ---
    }
} 