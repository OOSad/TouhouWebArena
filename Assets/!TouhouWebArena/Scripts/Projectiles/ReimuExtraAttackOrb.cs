using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody2D), typeof(Collider2D))]
/// <summary>
/// Represents Reimu's extra attack orb projectile.
/// Handles initial movement impulse, lifetime, collision with the target player,
/// applying damage, and network despawning.
/// </summary>
public class ReimuExtraAttackOrb : NetworkBehaviour
{
    [Header("Movement Settings")]
    /// <summary>The initial upward force applied to the orb on spawn.</summary>
    [SerializeField] private float initialUpwardForce = 5f;
    /// <summary>The magnitude of the initial horizontal force applied (direction is randomized).</summary>
    [SerializeField] private float initialHorizontalForce = 3f; // Added for side-to-side movement
    /// <summary>The duration in seconds the orb exists before being automatically despawned.</summary>
    [SerializeField] private float orbLifetime = 5.0f; // Time in seconds before the orb despawns
    /// <summary>The amount of damage applied to the player upon collision.</summary>
    [SerializeField] private int damageAmount = 1; // Or however much damage it should deal

    /// <summary>Delay in seconds before retrying PlayerObject lookup if the initial damage attempt fails.</summary>
    private const float DAMAGE_RETRY_DELAY = 0.1f; // Seconds to wait before retrying PlayerObject lookup

    // NetworkVariable to store which player this orb should damage
    /// <summary>
    /// [Server Write, Client Read] The <see cref="PlayerRole"/> of the player this orb is intended to hit.
    /// Used during collision checks to ensure the orb only damages the correct opponent.
    /// </summary>
    public NetworkVariable<PlayerRole> TargetPlayerRole { get; private set; } =
        new NetworkVariable<PlayerRole>(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Cached reference to the Rigidbody2D component.</summary>
    private Rigidbody2D rb;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Caches the Rigidbody2D component.
    /// </summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only the server applies the initial force
        if (IsServer)
        {
            // Determine random horizontal direction (-1 or 1)
            float randomDirection = (Random.value < 0.5f) ? -1f : 1f;
            
            // Calculate forces
            float horizontalForce = randomDirection * initialHorizontalForce;
            float upwardForce = initialUpwardForce; // Keep existing upward force
            
            // Create combined force vector
            Vector2 initialForce = new Vector2(horizontalForce, upwardForce);
            
            // Apply the combined force
            rb.AddForce(initialForce, ForceMode2D.Impulse);
            
            // Schedule despawn based on lifetime
            Invoke(nameof(DespawnOrb), orbLifetime);
        }
    }

    /// <summary>
    /// [Server Only] Handles trigger collision events.
    /// Checks if the collided object is the target player's hitbox, if the player is not invincible, and applies damage directly.
    /// </summary>
    /// <param name="otherCollider">The Collider2D that entered the trigger.</param>
    void OnTriggerEnter2D(Collider2D otherCollider)
    {
        // --- SERVER ONLY --- 
        if (!IsServer) return;

        // Check if we hit the PlayerHitbox component
        PlayerHitbox playerHitbox = otherCollider.GetComponent<PlayerHitbox>();
        if (playerHitbox == null) 
        {
            // Log if needed: Debug.Log($"[Orb:{NetworkObjectId}] [Server] Collision wasn't with a PlayerHitbox.");
            return; // Not the hitbox, ignore
        }

        // Get components from the hitbox's parent
        PlayerMovement playerMovement = otherCollider.GetComponentInParent<PlayerMovement>();
        PlayerHealth playerHealth = otherCollider.GetComponentInParent<PlayerHealth>();

        if (playerMovement != null && playerHealth != null)
        {
            PlayerRole hitPlayerRole = playerMovement.GetPlayerRole();

            // Check if it's the correct target player and if they are currently vulnerable
            if (hitPlayerRole == TargetPlayerRole.Value && !playerHealth.IsInvincible.Value)
            {
                Debug.Log($"[Orb:{NetworkObjectId}] [Server] Hit target Player {hitPlayerRole} Hitbox. Applying damage ({damageAmount}).");
                playerHealth.TakeDamage(damageAmount);
                // Orb continues flying, does not despawn on hit.
            }
            // else: Log conditions not met if needed
            // { Debug.Log($"[Orb:{NetworkObjectId}] [Server] Hit Player {hitPlayerRole} Hitbox, but conditions not met (Target: {TargetPlayerRole.Value}, Invincible: {playerHealth.IsInvincible.Value})."); }
        }
        // else: Log missing components if needed
        // { Debug.LogError($"[Orb:{NetworkObjectId}] [Server] Hit PlayerHitbox but couldn't find PlayerMovement/PlayerHealth on parent {otherCollider.transform.root.name}!"); }
    }

    /// <summary>
    /// [Server Only] Called via Invoke after <see cref="orbLifetime"/> has elapsed.
    /// Despawns the orb's NetworkObject if it still exists and is spawned.
    /// </summary>
    private void DespawnOrb()
    {
        // This method is invoked only on the server
        if (!IsServer) return; 

        // Check if the object still exists and is spawned before trying to despawn
        if (this != null && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); // true to destroy the object across the network
        }
        // No else needed - if it's already gone (e.g., hit player), do nothing
    }
}

