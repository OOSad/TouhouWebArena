using UnityEngine;
using System.Collections;
using Unity.Netcode; // Added for NetworkVariable & NetworkBehaviour

public class EarthlightRay : NetworkBehaviour
{
    public float activationDelay = 0.5f; // Time before the laser becomes harmful
    public float lifetime = 2.0f;       // How long the laser stays active
    public float maxTiltAngle = 10f;   // Maximum tilt in degrees +/-

    // Store the role of the player who fired the laser
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
        else
        {
            Debug.LogError("EarthlightRay needs a Collider2D component!");
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

        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerMovement != null && playerHealth != null)
        {
            PlayerRole hitPlayerRole = playerMovement.GetPlayerRole();

            // Ensure it's a valid player and not the player who fired the laser
            if (hitPlayerRole != PlayerRole.None && hitPlayerRole != AttackerRole.Value)
            {
                // Deal damage to the opponent player
                Debug.Log($"Player Role {hitPlayerRole} hit by Earthlight Ray from Role {AttackerRole.Value}!", this);
                playerHealth.TakeDamage(1); // Assuming 1 damage for now
            }
        }
        else
        {
            Debug.LogWarning($"EarthlightRay hit Player tagged collider, but couldn't find PlayerMovement/PlayerHealth in parent: {other.gameObject.name}", this);
        }
        // Ignore collisions with anything else (fairies, bullets, etc.)
    }

    private void SelfDestruct()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
} 