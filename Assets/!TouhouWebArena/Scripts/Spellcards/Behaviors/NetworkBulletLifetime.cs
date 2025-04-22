using UnityEngine;
using Unity.Netcode;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// Server-authoritative script to return a NetworkObject bullet to the pool after a set lifetime.
    /// Should be attached to bullet prefabs managed by the NetworkObjectPool.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))] // Ensures we have a NetworkObject
    public class NetworkBulletLifetime : NetworkBehaviour
    {
        [Header("Lifetime Settings")] // Added Header
        [Tooltip("Maximum time in seconds before the bullet is returned to the pool.")]
        public float maxLifetime = 5.0f;

        [Header("Boundary Settings")] // Added Header
        [Tooltip("If true, enforces boundary checks.")]
        public bool enforceBounds = true; // Enable check by default
        [Tooltip("The X coordinate representing the center boundary.")]
        public float boundaryX = 0.0f;
        [Tooltip("Should the bullet stay on the Positive X side (true) or Negative X side (false)?")]
        public bool keepOnPositiveSide = true; // Default for opponent bullets on right side

        private float lifeTimer = 0f;

        // Only run the timer logic on the server
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

        // Optional: Could add collision handling here on the server to return early
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
    }
} 