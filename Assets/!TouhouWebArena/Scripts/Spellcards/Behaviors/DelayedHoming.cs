using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic; // Needed for NetworkManager access?

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// A server-authoritative movement behavior that moves the attached GameObject forward
    /// (<c>transform.up</c>) at an <see cref="initialSpeed"/> for a set <see cref="homingDelay"/>.
    /// After the delay, it locks onto the *direction* towards a target position (captured at initialization)
    /// and continues moving in that fixed direction at <see cref="homingSpeed"/>.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))] 
    public class DelayedHoming : NetworkBehaviour
    {
        /// <summary>The speed during the initial linear movement phase.</summary>
        [Tooltip("Speed during the initial linear phase.")]
        public float initialSpeed = 5f;
        /// <summary>The speed after the delay when moving towards the locked target direction.</summary>
        [Tooltip("Speed after the delay, moving towards the locked target direction.")]
        public float homingSpeed = 4f;
        /// <summary>The duration in seconds of the initial linear movement phase before homing starts.</summary>
        [Tooltip("Duration of the initial linear phase before homing direction is locked.")]
        public float homingDelay = 0.5f;
        // Optional: Add turning speed if you want gradual rotation towards the target
        // public float turnSpeed = 180f; 

        // --- State Variables (Server-Only) ---
        // Store target by ID, not direct Transform reference (ID currently unused, uses position)
        private ulong targetNetworkObjectId = ulong.MaxValue; 
        // The world position of the target captured when Initialize was called.
        private Vector3 initialTargetPosition; 
        // Timer for the initial delay phase.
        private float delayTimer = 0f;
        // Flag indicating if the homing phase has started.
        private bool isHoming = false;
        // The fixed direction vector calculated when the homing phase starts.
        private Vector3 lockedTargetDirection; 
        // Flag to ensure the target direction is calculated only once.
        private bool targetDirectionLocked = false;

        // Start is not strictly necessary as state is reset in Initialize/OnNetworkSpawn
        // void Start() { ... }

        /// <summary>
        /// Ensures the component is disabled on clients and resets state on the server when spawned.
        /// </summary>
        public override void OnNetworkSpawn() {
            base.OnNetworkSpawn();
            // Only run logic on the server
            if (!IsServer) {
                this.enabled = false;
            }
            // Reset state when spawned/reused (though Initialize should handle this primarily)
            ResetState(); 
        }

        /// <summary>
        /// Server-only Update loop.
        /// Handles the initial linear movement and timer countdown.
        /// When the timer expires, calculates and locks the homing direction.
        /// Continues moving in the locked direction during the homing phase.
        /// </summary>
        void Update()
        {
            if (!IsServer) return;
            
            if (!isHoming)
            {
                // Initial linear movement phase
                transform.position += transform.up * initialSpeed * Time.deltaTime;
                delayTimer -= Time.deltaTime;

                if (delayTimer <= 0f)
                {
                    isHoming = true; // Start homing phase
                    // Calculate and lock direction only if not already locked
                    if (!targetDirectionLocked) 
                    {
                        lockedTargetDirection = (initialTargetPosition - transform.position).normalized;
                        // Prevent zero direction if already at the target position
                        if (lockedTargetDirection == Vector3.zero) { 
                            lockedTargetDirection = transform.up; // Default to current forward direction
                        }
                        targetDirectionLocked = true;
                    } 
                }
            }
            
            // Move in the locked direction once homing starts and direction is locked
            if (isHoming && targetDirectionLocked) 
            {
                 transform.position += lockedTargetDirection * homingSpeed * Time.deltaTime; 
            }
        }

        /// <summary>
        /// Initializes the movement behavior parameters. Should be called by the spawning logic on the server
        /// immediately after retrieving the object from the pool and before spawning it.
        /// Resets the internal state for reuse.
        /// </summary>
        /// <param name="initialSpeed">Speed during the initial delay phase.</param>
        /// <param name="homingSpeed">Speed during the homing phase (after delay).</param>
        /// <param name="homingDelay">Duration of the initial delay phase.</param>
        /// <param name="targetId">The NetworkObjectId of the target (currently unused, uses position).</param>
        /// <param name="targetPosition">The world position of the target to home towards (captured at this moment).</param>
        public void Initialize(float initialSpeed, float homingSpeed, float homingDelay, ulong targetId, Vector3 targetPosition)
        {
             // Ensure this is only called on the server
            if (!IsServer) return;

            this.initialSpeed = initialSpeed;
            this.homingSpeed = homingSpeed;
            this.homingDelay = homingDelay;
            this.targetNetworkObjectId = targetId; // Store ID even if unused for now
            this.initialTargetPosition = targetPosition; // Capture position
            
            // Reset state for potential reuse
            ResetState();

            // Ensure the component is enabled if it was disabled
            if(!this.enabled) this.enabled = true; 
        }

        /// <summary>
        /// Resets the internal state variables to their defaults.
        /// </summary>
        private void ResetState() 
        {
            this.delayTimer = this.homingDelay;
            this.isHoming = false;
            this.targetDirectionLocked = false;
            this.lockedTargetDirection = Vector3.zero; // Reset locked direction
        }
        
        // Optional: Add a method to update the target if needed during the bullet's lifetime
        // public void SetTarget(Transform newTarget) {
        //     this.targetTransform = newTarget;
        // }
    }
} 