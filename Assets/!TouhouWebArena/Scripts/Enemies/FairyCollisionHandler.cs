using UnityEngine;
using Unity.Netcode;

// Handles collision logic for the Fairy
[RequireComponent(typeof(Fairy), typeof(Collider2D))] // Requires Fairy and its Collider
public class FairyCollisionHandler : NetworkBehaviour // Inherit from NetworkBehaviour for IsServer check
{
    private Fairy sourceFairy;

    void Awake()
    {
        sourceFairy = GetComponent<Fairy>();
        if (sourceFairy == null)
        {
            Debug.LogError("[FairyCollisionHandler] Fairy component not found!");
        }
    }

    // Moved collision logic here
    void OnTriggerEnter2D(Collider2D other)
    {
        // Collision logic should only run on the server, and only if the source fairy is valid
        if (!IsServer || sourceFairy == null) return;

        // Check collision with Player
        if (other.CompareTag("Player"))
        {
            HandlePlayerCollision(other);
        }
    }

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
} 