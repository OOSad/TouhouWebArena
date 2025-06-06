using UnityEngine;
using Unity.Netcode;
using TouhouWebArena; // Add namespace for IClearable

namespace TouhouWebArena.Spellcards.Behaviors
{
    // Remove Forward declaration for IClearableByBomb if it exists
    // interface IClearableByBomb { void ClearByBomb(PlayerRole sourceRole); }

    /// <summary>
    /// Server-authoritative script to manage the lifetime and boundary checks for networked projectiles (bullets).
    /// Automatically returns the projectile's NetworkObject to the NetworkObjectPool when its lifetime expires
    /// or if it crosses a defined boundary. Also handles basic collision detection to apply damage.
    /// Should be attached to bullet prefabs managed by the NetworkObjectPool.
    /// Implements IClearable allowing it to be cleared by effects like bombs or shockwaves based on configuration.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))] // Ensures we have a NetworkObject
    public class NetworkBulletLifetime : NetworkBehaviour, IClearable // Implement IClearable, remove IClearableByBomb
    {
        [Header("Lifetime Settings")]
        [Tooltip("Maximum time in seconds before the bullet is returned to the pool.")]
        /// <summary>
        /// Maximum time in seconds the projectile can exist before being automatically returned to the pool by the server.
        /// </summary>
        public float maxLifetime = 5.0f;

        [Header("Boundary Settings")]
        [Tooltip("If true, enforces boundary checks.")]
        /// <summary>
        /// If true, the server checks if the projectile crosses the <see cref="boundaryX"/> coordinate based on <see cref="keepOnPositiveSide"/>.
        /// </summary>
        public bool enforceBounds = true;
        [Tooltip("The X coordinate representing the center boundary.")]
        /// <summary>
        /// The X coordinate used for boundary checks when <see cref="enforceBounds"/> is true. Typically the center of the playfield.
        /// </summary>
        public float boundaryX = 0.0f;
        [Tooltip("Should the bullet stay on the Positive X side (true) or Negative X side (false)?")]
        /// <summary>
        /// Determines which side of the <see cref="boundaryX"/> the projectile should remain on. True for the positive X side (e.g., right player's area), false for the negative X side (e.g., left player's area).
        /// </summary>
        public bool keepOnPositiveSide = true;

        [Header("Clearing Settings")] // Add Header
        [Tooltip("Can this bullet be cleared by standard shockwaves (non-forced clears)?")]
        public bool isNormallyClearable = true;

        // --- NEW: Target Role --- 
        /// <summary>
        /// [Server Write, Client Read] The <see cref="PlayerRole"/> this bullet is primarily associated with (usually the player being targeted).
        /// Used for logic like bomb clearing.
        /// </summary>
        public NetworkVariable<PlayerRole> TargetPlayerRole { get; private set; } = new NetworkVariable<PlayerRole>(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // ------------------------

        private float lifeTimer = 0f;

        /// <summary>
        /// Called when the NetworkObject is spawned. 
        /// Disables the component on clients and resets the lifetime timer on the server.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer) 
            {
                // Disable component on clients - only server manages lifetime/pooling
                this.enabled = false; 
                return;
            }
            
            // Reset timer when spawned by the server
            lifeTimer = 0f;
        }

        /// <summary>
        /// Server-side Update loop. Increments the lifetime timer and checks for lifetime expiration or boundary crossing.
        /// Calls <see cref="ReturnToPool"/> if conditions are met.
        /// </summary>
        void Update()
        {
            if (!IsServer) return; 

            lifeTimer += Time.deltaTime;
            if (lifeTimer >= maxLifetime)
            {
                ReturnToPool();
                return; // Exit after returning
            }

            // --- Boundary Check --- 
            if (enforceBounds)
            {
                bool outOfBounds = false;
                if (keepOnPositiveSide && transform.position.x < boundaryX)
                {
                    outOfBounds = true; // Bullet crossed to the negative side
                }
                else if (!keepOnPositiveSide && transform.position.x > boundaryX)
                {
                    outOfBounds = true; // Bullet crossed to the positive side
                }

                if (outOfBounds)
                {
                    ReturnToPool();
                    return; // Exit after returning
                }
            }
        }

        /// <summary>
        /// Server-side method to return the associated NetworkObject to the <see cref="NetworkObjectPool"/>.
        /// Handles despawning and potential destruction as a fallback.
        /// </summary>
        private void ReturnToPool()
        {
            if (!IsServer) return; // Should already be checked, but safety first

            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null && NetworkObjectPool.Instance != null)
            {
                NetworkObjectPool.Instance.ReturnNetworkObject(networkObject);
                lifeTimer = 0f; // Reset timer in case it gets reused immediately somehow
                // The ReturnNetworkObject handles Despawn(false) and SetActive(false)
            }
            else
            {
                // Fallback if pool is gone or something is wrong
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn(true); // Despawn and destroy
                }
                else if (gameObject != null)
                {
                    Destroy(gameObject); // Destroy if not networked
                }
            }
            // Disable the component after returning to prevent multiple returns? Might not be necessary as ReturnNetworkObject disables the GO.
            // this.enabled = false;
        }

        /// <summary>
        /// Server-side collision detection. Checks if the projectile collides with an object
        /// containing a <see cref="PlayerHealth"/> component (typically the player's hitbox).
        /// If a player is hit, calls <see cref="PlayerHealth.TakeDamage"/>.
        /// Note: Does not currently return the projectile to the pool upon hitting a player.
        /// </summary>
        /// <param name="other">The Collider2D that this projectile collided with.</param>
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;

            // Check if the collided object has a PlayerHealth component IN ITS PARENT or itself
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);

                // Return bullet to pool immediately after dealing damage - REMOVED
                // ReturnToPool();
            }
        }

        // --- Implementation of IClearable ---
        /// <summary>
        /// Called by effects like PlayerDeathBomb or Shockwave to clear this bullet.
        /// On the server, checks if the clear should happen based on forceClear and isNormallyClearable flags,
        /// then returns the bullet to the object pool if applicable.
        /// </summary>
        /// <param name="forceClear">If true, the bullet is cleared regardless of isNormallyClearable.</param>
        /// <param name="sourceRole">The role of the player causing the clear (ignored by this implementation).</param>
        public void Clear(bool forceClear, PlayerRole sourceRole)
        {
            // Clearing logic only runs on the server
            if (!IsServer) return;

            // If it's a forced clear (player bomb) OR this bullet is normally clearable
            if (forceClear || isNormallyClearable)
            {
                // Reuse the existing pooling logic
                ReturnToPool();
            }
            // Else: Normal clear attempt on a bullet that is not normally clearable - do nothing.
        }
        // ------------------------------------
    }
} 