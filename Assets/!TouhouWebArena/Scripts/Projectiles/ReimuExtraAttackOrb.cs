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
    [SerializeField] private int damageAmount = 10; // Or however much damage it should deal

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
    /// Handles trigger collision events.
    /// Checks if the collided object is the target player and initiates the damage sequence via <see cref="RequestDamageServerRpc"/>.
    /// </summary>
    /// <param name="otherCollider">The Collider2D that entered the trigger.</param>
    void OnTriggerEnter2D(Collider2D otherCollider)
    {
        // Check if we hit an object tagged "Player"
        if (otherCollider.CompareTag("Player"))
        {
            // Try to get the player's identity/controller script from the parent object
            PlayerMovement playerMovement = otherCollider.GetComponentInParent<PlayerMovement>(); // Use GetComponentInParent

            if (playerMovement != null)
            {
                // Determine the role of the player we hit
                PlayerRole hitPlayerRole = playerMovement.GetPlayerRole(); // Call method on PlayerMovement

                // If the hit player's role matches the target role for this orb...
                if (hitPlayerRole != PlayerRole.None && hitPlayerRole == TargetPlayerRole.Value)
                {
                    // Request the server to apply damage and destroy the orb
                    RequestDamageServerRpc(playerMovement.OwnerClientId); // Pass the ClientId of the player hit
                }
            }
             else
            {
                 // Keep warning for actual failure
            }
        }
    }

    /// <summary>
    /// [ServerRpc] Initiated by the client when the orb collides with the target player.
    /// Attempts to apply damage immediately using <see cref="TryApplyDamage"/>.
    /// If the target PlayerObject isn't immediately available, starts the <see cref="DelayedDamageCheck"/> coroutine.
    /// </summary>
    /// <param name="targetClientId">The ClientId of the player who was hit.</param>
    [ServerRpc(RequireOwnership = false)]
    private void RequestDamageServerRpc(ulong targetClientId)
    {
        // Attempt to apply damage immediately
        if (!TryApplyDamage(targetClientId))
        {
            // If immediate attempt failed (likely PlayerObject not ready), start delayed check
            StartCoroutine(DelayedDamageCheck(targetClientId));
        }
    }

    /// <summary>
    /// [Server Only] Coroutine to wait briefly and retry applying damage if the initial attempt failed.
    /// Used to handle cases where the PlayerObject might not be immediately accessible when the RPC arrives.
    /// </summary>
    /// <param name="targetClientId">The ClientId of the player to damage.</param>
    private IEnumerator DelayedDamageCheck(ulong targetClientId)
    {
        yield return new WaitForSeconds(DAMAGE_RETRY_DELAY);

        // Retry applying damage
        if (!TryApplyDamage(targetClientId))
        {
            // If it *still* failed after the delay, log final error and despawn
             if (NetworkObject != null) NetworkObject.Despawn(true);
        }
    }

    /// <summary>
    /// [Server Only] Attempts to find the target player's <see cref="PlayerHealth"/> component and apply damage.
    /// Despawns the orb if damage is applied or if the player lacks a health component.
    /// </summary>
    /// <param name="targetClientId">The ClientId of the player to damage.</param>
    /// <returns>True if damage was applied or the player/health component was missing (orb despawned). False if the PlayerObject could not be found (damage retry needed).</returns>
    private bool TryApplyDamage(ulong targetClientId)
    {
        if (!IsServer) return false; 

        NetworkObject targetPlayerNetworkObject = NetworkManager.Singleton.ConnectedClients[targetClientId]?.PlayerObject;

        if (targetPlayerNetworkObject != null)
        {
            PlayerHealth playerHealth = targetPlayerNetworkObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                if (NetworkObject != null) NetworkObject.Despawn(true);
                return true; // Damage applied, orb handled
            }
            else
            {
                if (NetworkObject != null) NetworkObject.Despawn(true);
                return true; // Considered handled (error case, but orb despawned)
            }
        }
        else
        {
            return false; // Indicate failure to find PlayerObject
        }
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
    // --- END NEW --- 
}

