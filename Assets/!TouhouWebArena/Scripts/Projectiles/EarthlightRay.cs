using UnityEngine;
using System.Collections;
using Unity.Netcode; // Added for NetworkVariable & NetworkBehaviour

/// <summary>
/// Represents Marisa's extra attack projectile (Master Spark).
/// Handles activation delay, lifetime, collision detection, and despawning.
/// Applies damage to the opponent player on collision.
/// </summary>
public class EarthlightRay : NetworkBehaviour
{
    /// <summary>Time in seconds after spawning before the laser collider activates and can deal damage.</summary>
    public float activationDelay = 0.5f; // Time before the laser becomes harmful
    /// <summary>Total time in seconds the laser exists before automatically despawning.</summary>
    public float lifetime = 2.0f;       // How long the laser stays active
    /// <summary>Maximum random tilt in degrees applied to the laser's rotation upon spawning.</summary>
    public float maxTiltAngle = 10f;   // Maximum tilt in degrees +/-

    // Store the role of the player who fired the laser
    /// <summary>
    /// [Server Write, Client Read] The <see cref="PlayerRole"/> of the player who triggered this laser.
    /// Used to prevent the laser from damaging its owner.
    /// </summary>
    public NetworkVariable<PlayerRole> AttackerRole { get; private set; } =
        new NetworkVariable<PlayerRole>(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Collider2D _collider;

    void Start()
    {
        _collider = GetComponent<Collider2D>();
        if (_collider)
        {
            _collider.enabled = false; // Start deactivated
        }

        // The spawner is now responsible for setting the initial rotation before spawning.

        StartCoroutine(ActivateAndFade());
        // Use Invoke for timed destruction, more robust if coroutine stops early
        Invoke(nameof(SelfDestruct), lifetime);
    }

    private IEnumerator ActivateAndFade()
    {
        // Activation visual cue can be added here (e.g., change color, scale)
        yield return new WaitForSeconds(activationDelay);

        // Activate collision
        if (_collider)
        {
            _collider.enabled = true;
        }
        // Activation feedback visual cue can be added here (e.g., bright flash)

        // Optional: Add fading out visual cue towards the end of lifetime
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Collision logic only matters on the server if damage is server-authoritative
        if (!IsServer) return;

        // Check if it hit a player's hitbox
        if (!other.CompareTag("Player")) return; // Assuming player hitbox has "Player" tag

        ClientAuthMovement clientAuthMovement = other.GetComponentInParent<ClientAuthMovement>();
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (clientAuthMovement != null && playerHealth != null)
        {
            PlayerRole hitPlayerRole = PlayerRole.None; // Default
            NetworkObject hitPlayerNetworkObject = clientAuthMovement.NetworkObject;
            if (hitPlayerNetworkObject != null && PlayerDataManager.Instance != null)
            {
                PlayerData? hitPlayerData = PlayerDataManager.Instance.GetPlayerData(hitPlayerNetworkObject.OwnerClientId);
                if (hitPlayerData.HasValue)
                {
                    hitPlayerRole = hitPlayerData.Value.Role;
                }
                else
                {
                    Debug.LogWarning($"[EarthlightRay] Could not get PlayerData for hit player object: {hitPlayerNetworkObject.NetworkObjectId}", this);
                }
            }
            else
            {
                Debug.LogWarning("[EarthlightRay] Hit player NetworkObject or PlayerDataManager.Instance is null.", this);
            }

            // Ensure it's a valid player and not the player who fired the laser
            if (hitPlayerRole != PlayerRole.None && hitPlayerRole != AttackerRole.Value)
            {
                // Deal damage to the opponent player
                playerHealth.TakeDamage(1); // Assuming 1 damage for now
            }
        }
    }

    private void SelfDestruct()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
} 