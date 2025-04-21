using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider2D))]
public class PlayerHitbox : NetworkBehaviour
{
    private PlayerHealth playerHealth;
    private Collider2D hitboxCollider; // Cache the collider
    private bool canTakeDamage = true; // Add invincibility frames later if needed

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

    // This method is called by Unity's physics engine
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; 
        
        // --- Invincibility Check ---
        if (playerHealth != null && playerHealth.IsInvincible.Value) 
        {
            return; // Ignore hit if invincible
        }
        // --- End Invincibility Check ---
        
        if (!canTakeDamage) return; // For potential invincibility

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