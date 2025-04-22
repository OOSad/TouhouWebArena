using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic; // Needed for NetworkManager access?

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// Moves the GameObject forward at an initial speed for a specified delay,
    /// then homes in on a target transform.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))] 
    public class DelayedHoming : NetworkBehaviour
    {
        public float initialSpeed = 5f;
        public float homingSpeed = 4f;
        public float homingDelay = 0.5f;
        // Optional: Add turning speed if you want gradual rotation towards the target
        // public float turnSpeed = 180f; 

        // Store target by ID, not direct Transform reference
        private ulong targetNetworkObjectId = ulong.MaxValue; 
        private Vector3 initialTargetPosition; // Store the captured position

        private float delayTimer = 0f;
        private bool isHoming = false;
        // private Vector3 lockedTargetPosition; // Remove locked position
        private Vector3 lockedTargetDirection; // Store the direction when homing starts
        private bool targetDirectionLocked = false; // Flag to ensure we only lock once

        void Start()
        {
            // Initialize timer on Start (or potentially Awake)
            // delayTimer = homingDelay; // Delay is set in Initialize
            // isHoming = false;
            // targetDirectionLocked = false;
        }

        public override void OnNetworkSpawn() {
            base.OnNetworkSpawn();
            // Only run logic on the server
            if (!IsServer) {
                this.enabled = false;
            }
            // Reset state when spawned/reused
            // isHoming = false; // State is reset in Initialize
            // targetDirectionLocked = false;
            // delayTimer = homingDelay; 
        }

        void Update()
        {
            // Remove verbose Update log
            // Debug.Log($"[Server] Bullet {gameObject.name} Update Frame...", this);

            if (!IsServer) return;
            
            // Remove pre-lock log
            // if (isHoming && !targetDirectionLocked && delayTimer <= 0f) { ... }

            if (!isHoming)
            {
                // Initial linear movement phase
                transform.position += transform.up * initialSpeed * Time.deltaTime;
                delayTimer -= Time.deltaTime;

                if (delayTimer <= 0f)
                {
                    isHoming = true;
                    if (!targetDirectionLocked) 
                    {
                        lockedTargetDirection = (initialTargetPosition - transform.position).normalized;
                        if (lockedTargetDirection == Vector3.zero) { 
                            lockedTargetDirection = transform.up; 
                        }
                        targetDirectionLocked = true;
                        // Remove direction lock log
                        // Debug.Log($"[Server] Bullet {gameObject.name} locked homing direction...", this);
                    } 
                }
            }
            
            if (isHoming && targetDirectionLocked) 
            {
                 transform.position += lockedTargetDirection * homingSpeed * Time.deltaTime; 
            }
        }

        /// <summary>
        /// Initializes the behavior with speeds, delay, and the target.
        /// Should be called by the spellcard activation logic after spawning.
        /// </summary>
        public void Initialize(float initialSpeed, float homingSpeed, float homingDelay, ulong targetId, Vector3 targetPosition)
        {
            this.initialSpeed = initialSpeed;
            this.homingSpeed = homingSpeed;
            this.homingDelay = homingDelay;
            this.targetNetworkObjectId = targetId; 
            this.initialTargetPosition = targetPosition; 
            
            // Remove Initialize log
            // string status = (this.targetNetworkObjectId == ulong.MaxValue) ? "INVALID_ID" : "VALID_ID";
            // Debug.Log($"[Server] Bullet {gameObject.name} (Instance:{GetInstanceID()}) Initialize END. TargetID: {this.targetNetworkObjectId} ({status}), InitialTargetPos: {this.initialTargetPosition}. Timer: {this.delayTimer}", this);

            this.delayTimer = this.homingDelay;
            this.isHoming = false;
            this.targetDirectionLocked = false;

            if(!this.enabled) this.enabled = true; 
        }
        
        // Optional: Add a method to update the target if needed during the bullet's lifetime
        // public void SetTarget(Transform newTarget) {
        //     this.targetTransform = newTarget;
        // }
    }
} 