using UnityEngine;
using Unity.Netcode;

// Handles collision logic for the Fairy
/// <summary>
/// [Server Only] Handles physics trigger events for the Fairy.
/// Primarily detects collisions with "Player" and "PlayerShot" tags.
/// Requires references to the associated <see cref="FairyController"/> and <see cref="FairyHealth"/>.
/// Ensures collisions are only processed on the server and only if the fairy is alive (<see cref="FairyHealth.IsAlive"/>).
/// </summary>
[RequireComponent(typeof(FairyController), typeof(Collider2D), typeof(FairyHealth))] // Added FairyHealth
public class FairyCollisionHandler : NetworkBehaviour // Inherit from NetworkBehaviour for IsServer check
{
    private FairyController sourceFairy;
    private FairyHealth fairyHealth; // Added reference

    void Awake()
    {
        sourceFairy = GetComponent<FairyController>();
        fairyHealth = GetComponent<FairyHealth>(); // Get reference
        if (sourceFairy == null || fairyHealth == null)
        {
            Debug.LogError("[FairyCollisionHandler] FairyController or FairyHealth component not found!");
        }
    }

    /// <summary>
    /// [Server Only] Detects trigger enter events.
    /// Checks if the source fairy is alive via <see cref="FairyHealth"/> before processing.
    /// Calls specific handlers based on the collider's tag ("Player", "PlayerShot").
    /// </summary>
    /// <param name="other">The Collider2D that entered the trigger.</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        // Collision logic should only run on the server, and only if the source fairy is valid and alive
        if (!IsServer || sourceFairy == null || fairyHealth == null || !fairyHealth.IsAlive()) return; // Check fairyHealth.IsAlive()

        // Check collision with Player
        if (other.CompareTag("Player"))
        {
            HandlePlayerCollision(other);
        }
        // --- NEW: Check collision with Player Shots --- 
        /* // Temporarily commented out - to be replaced by client-side hit detection + RPC
        else if (other.CompareTag("PlayerShot"))
        {
            HandlePlayerShotCollision(other);
        }
        */
        // --------------------------------------------
    }

    /// <summary>
    /// [Server Only] Handles collision with an object tagged "Player".
    /// Attempts to deal damage to the player via <see cref="PlayerHealth.TakeDamage"/> if the player is not invincible.
    /// </summary>
    /// <param name="playerCollider">The collider of the player object.</param>
    private void HandlePlayerCollision(Collider2D playerCollider)
    {
        PlayerHealth playerHealth = playerCollider.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            // Only damage player if they are vulnerable
            if (!playerHealth.IsInvincible.Value)
            {
                playerHealth.TakeDamage(1); // Deal 1 damage to the player
                // Note: The fairy does NOT die from colliding with the player
            }
        }
        else
        {
            // Log error if Player tag found but no PlayerHealth component
            Debug.LogError($"[Server FairyCollisionHandler NetId:{sourceFairy?.NetworkObjectId}] Collided with Player ({playerCollider.name}) but PlayerHealth is NULL in parent!");
        }
    }

    /// <summary>
    /// [Server Only] Handles collision with an object tagged "PlayerShot".
    /// Retrieves damage amount from <see cref="ProjectileDamager"/> and killer role from <see cref="BulletMovement"/>.
    /// Applies damage to the fairy via <see cref="FairyController.ApplyDamageServer"/> (which delegates to <see cref="FairyHealth"/>).
    /// </summary>
    /// <param name="shotCollider">The collider of the player shot object.</param>
    private void HandlePlayerShotCollision(Collider2D shotCollider)
    {
        /* // Temporarily commented out
        // Get damage amount from the projectile
        int damageAmount = 1; // Default damage
        ProjectileDamager damager = shotCollider.GetComponent<ProjectileDamager>();
        if (damager != null) { damageAmount = damager.damage; }

        // Get the role of the player who fired the shot
        PlayerRole killerRole = PlayerRole.None;
        BulletMovement bullet = shotCollider.GetComponent<BulletMovement>();
        if (bullet != null) { killerRole = bullet.OwnerRole.Value; }

        // Apply damage directly on the server
        sourceFairy.ApplyDamageServer(damageAmount, killerRole);
        */
    }
    // --------------------------------------------
} 