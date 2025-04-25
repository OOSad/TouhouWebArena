using UnityEngine;
using Unity.Netcode;
using System.Collections; // Needed for StopAllCoroutines

namespace TouhouWebArena.Spellcards
{
    /// <summary>
    /// **[Server Authoritative]** Manages the health of a Level 4 illusion.
    /// Allows the illusion to take damage from the opponent player's bullets
    /// and despawns the illusion when health reaches zero.
    /// Requires NetworkObject on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class IllusionHealth : NetworkBehaviour
    {
        // --- Network Variables --- 
        [Tooltip("Current health of the illusion.")]
        [SerializeField] // Visible in inspector for debugging
        private NetworkVariable<float> currentHealth =
            new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // --- State Variables --- 
        private bool _isDead = false;

        // Store which player this illusion is TARGETING.
        // Only bullets from this player should damage the illusion.
        private PlayerRole _targetedPlayerRole = PlayerRole.None;

        // --- Properties --- 
        public float CurrentHealth => currentHealth.Value;
        public PlayerRole TargetedPlayerRole => _targetedPlayerRole;

        // --- Initialization (Server Only) --- 

        /// <summary>
        /// **[Server Only]** Initializes the illusion's health and target role.
        /// Called by ServerAttackSpawner when the illusion is spawned.
        /// </summary>
        public void ServerInitialize(float initialHealth, PlayerRole targetRole)
        {
            if (!IsServer) return;
            currentHealth.Value = initialHealth;
            _targetedPlayerRole = targetRole;
            _isDead = false;
        }

        /// <summary>
        /// **[Server Only]** Directly applies damage to the illusion. 
        /// Called server-side by colliding objects (e.g., BulletMovement).
        /// Contains the core damage application and death check logic.
        /// </summary>
        public void TakeDamageServerSide(float damage, PlayerRole damageDealerRole)
        {
            if (!IsServer || _isDead) return;

            // Allow PlayerRole.None to bypass target check (for forced despawn/timeout)
            bool isForcedDespawn = (damageDealerRole == PlayerRole.None);
            if (!isForcedDespawn)
            {
                 // Validate damage came from the correct player (the one being targeted)
                 if (damageDealerRole != _targetedPlayerRole)
                 {
                    return;
                 }
            }

            // Apply damage
            currentHealth.Value -= damage;

            // Check for death
            if (currentHealth.Value <= 0)
            {
                Die();
            }
        }

        // --- Death Logic (Server Only) --- 

        /// <summary>
        /// **[Server Only]** Handles the illusion's death (notifying spawner, despawning).
        /// </summary>
        private void Die()
        { 
            if (!IsServer || _isDead) return;
            _isDead = true;
            
            // Notify Spawner BEFORE Despawning
            if (ServerAttackSpawner.Instance != null)
            {
                ServerAttackSpawner.Instance.ServerNotifyIllusionDespawned(this.NetworkObject);
            }
            else
            {
                Debug.LogError("[IllusionHealth] ServerAttackSpawner instance is null! Cannot notify about despawn.", gameObject);
            }

            // Stop attacks/movement (optional, despawn might handle it)
            var controller = GetComponent<Level4IllusionController>();
            if(controller != null) controller.StopAllCoroutines();

            // Despawn the NetworkObject
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true); // true = destroy object after despawning
            }
            else
            {
                Destroy(gameObject); // Fallback
            }
        }
    }
} 