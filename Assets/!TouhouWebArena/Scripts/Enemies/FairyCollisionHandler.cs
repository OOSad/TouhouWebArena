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
        if (!IsServer || sourceFairy == null || !sourceFairy.IsAlive()) return; // Also check if fairy is alive

        // Check collision with Player
        if (other.CompareTag("Player"))
        {
            HandlePlayerCollision(other);
        }
        // --- NEW: Check collision with Player Shots --- 
        else if (other.CompareTag("PlayerShot"))
        {
            HandlePlayerShotCollision(other);
        }
        // --------------------------------------------
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

    // --- NEW: Handler for Player Shot Collisions --- 
    private void HandlePlayerShotCollision(Collider2D shotCollider)
    {
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

        // --- REMOVED Player Shot Despawn Logic --- 
        // The BulletMovement script on the shot itself should now handle this
        // when its own OnTriggerEnter2D detects the collision with the Fairy.
        /*
        NetworkObject shotNetworkObject = shotCollider.GetComponent<NetworkObject>();
        if (shotNetworkObject != null)
        {
            PoolableObjectIdentity shotIdentity = shotCollider.GetComponent<PoolableObjectIdentity>();
            if (shotIdentity != null && NetworkObjectPool.Instance != null) 
            {
                NetworkObjectPool.Instance.ReturnNetworkObject(shotNetworkObject);
            }
            else
            {
                 shotNetworkObject.Despawn(true); 
            }
        }
        */
        // -------------------------------------------
    }
    // --------------------------------------------
} 