using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody2D), typeof(Collider2D))]
public class ReimuExtraAttackOrb : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float initialUpwardForce = 5f;
    [SerializeField] private float initialHorizontalForce = 3f; // Added for side-to-side movement
    [SerializeField] private float orbLifetime = 5.0f; // Time in seconds before the orb despawns
    [SerializeField] private int damageAmount = 10; // Or however much damage it should deal

    private const float DAMAGE_RETRY_DELAY = 0.1f; // Seconds to wait before retrying PlayerObject lookup

    // NetworkVariable to store which player this orb should damage
    public NetworkVariable<PlayerRole> TargetPlayerRole { get; private set; } =
        new NetworkVariable<PlayerRole>(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Rigidbody2D rb;

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

    // Collision detection runs on server and clients - Changed to Trigger
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
                 Debug.LogWarning($"[Orb {NetworkObjectId} Trigger] Collided with Player tagged object, but could NOT get PlayerMovement script in parent of {otherCollider.gameObject.name}.");
            }
        }
    }

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

    // Server-side coroutine to retry damage application after a delay
    private IEnumerator DelayedDamageCheck(ulong targetClientId)
    {
        yield return new WaitForSeconds(DAMAGE_RETRY_DELAY);

        // Retry applying damage
        if (!TryApplyDamage(targetClientId))
        {
            // If it *still* failed after the delay, log final error and despawn
            Debug.LogError($"[Orb {NetworkObjectId} ServerRPC Delayed] PlayerObject lookup/damage failed even after {DAMAGE_RETRY_DELAY}s delay for ClientId {targetClientId}. Despawning orb.");
             if (NetworkObject != null) NetworkObject.Despawn(true);
        }
    }

    // Refactored damage logic - returns true if damage applied (or object missing health), false if PlayerObject missing
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
                Debug.LogError($"[Orb {NetworkObjectId} TryApplyDamage] Found PlayerObject '{targetPlayerNetworkObject.name}' for ClientId {targetClientId}, but it is missing the PlayerHealth component! Despawning orb.");
                if (NetworkObject != null) NetworkObject.Despawn(true);
                return true; // Considered handled (error case, but orb despawned)
            }
        }
        else
        {
            return false; // Indicate failure to find PlayerObject
        }
    }

    // --- Method to handle timed despawn --- 
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

