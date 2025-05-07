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

    // Specific enemy body tags
    private const string FAIRY_TAG = "Fairy";
    private const string SPIRIT_TAG = "Spirit";
    private const string ILLUSION_TAG = "Illusion"; // For future use

    // Layer for enemy projectiles
    private int enemyProjectilesLayer;
    // Layers for enemy bodies (optional if tag check is sufficient, but good for completeness)
    // private int fairiesLayer;
    // private int spiritsLayer;
    // private int illusionsLayer;

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
            return;
        }

        hitboxCollider = GetComponent<Collider2D>();
        if (!hitboxCollider.isTrigger)
        {
            Debug.LogWarning("Hitbox Collider2D is not set to 'Is Trigger'. Forcing it now.", this);
            hitboxCollider.isTrigger = true; 
        }

        // Cache layer IDs
        enemyProjectilesLayer = LayerMask.NameToLayer("EnemyProjectiles");
        // fairiesLayer = LayerMask.NameToLayer("Fairies"); 
        // spiritsLayer = LayerMask.NameToLayer("Spirits");
        // illusionsLayer = LayerMask.NameToLayer("Illusions");

        // Log error if layers aren't found, as this is critical
        if (enemyProjectilesLayer == -1) Debug.LogError("PlayerHitbox: 'EnemyProjectiles' layer not found! Player may not take damage from bullets.");
        // if (fairiesLayer == -1) Debug.LogError("PlayerHitbox: 'Fairies' layer not found!");
        // if (spiritsLayer == -1) Debug.LogError("PlayerHitbox: 'Spirits' layer not found!");
        // if (illusionsLayer == -1) Debug.LogError("PlayerHitbox: 'Illusions' layer not found!");
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
        // This now runs on the client that owns this player object.
        // We only care if we are the owner of this hitbox/player.
        if (!IsOwner) return;

        bool hitDetected = false;

        // Check for collision with an enemy projectile by layer
        if (other.gameObject.layer == enemyProjectilesLayer)
        {
            hitDetected = true;
            // Debug.Log($"[Client {OwnerClientId}] PlayerHitbox detected collision with Enemy Projectile: {other.name} on layer {LayerMask.LayerToName(other.gameObject.layer)}. Reporting to server.");
        }
        // Check for collision with an enemy body by tag
        else if (other.CompareTag(FAIRY_TAG) || 
                 other.CompareTag(SPIRIT_TAG) || 
                 other.CompareTag(ILLUSION_TAG))
        {
            hitDetected = true;
            // Debug.Log($"[Client {OwnerClientId}] PlayerHitbox detected collision with Enemy Body: {other.name} ({other.tag}). Reporting to server.");
        }

        if (hitDetected)
        {
            // Tell the server about the hit
            ReportHitToServerRpc();

            // IMPORTANT: Do NOT destroy the 'other' object here (e.g. enemy bullet).
            // Its lifecycle is managed by its own scripts (e.g., returning to pool on collision with anything, or by lifetime).
            // If enemy bullets should despawn on hitting the player, their own OnTriggerEnter2D should handle that.
        }
    }

    [ServerRpc]
    private void ReportHitToServerRpc(ServerRpcParams rpcParams = default)
    {
        // This code executes on the server.
        // rpcParams.Receive.SenderClientId is the client who reported the hit.
        // We use this.OwnerClientId because this script is on the player object being hit.
        
        if (playerHealth == null) 
        {
            Debug.LogError($"[Server PlayerHitbox for {OwnerClientId}] ReportHitToServerRpc received, but PlayerHealth is null!");
            return;
        }

        // Server-authoritative invincibility check
        if (playerHealth.IsInvincible.Value)
        {
            // Debug.Log($"[Server PlayerHitbox for {OwnerClientId}] Player is invincible, ignoring reported hit.");
            return;
        }

        // Debug.Log($"[Server PlayerHitbox for {OwnerClientId}] Applying damage (1) due to reported client hit.");
        playerHealth.TakeDamage(1); // Apply 1 damage
    }
} 