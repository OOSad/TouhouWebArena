using UnityEngine;
using Unity.Netcode;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// Server-authoritative movement behavior that implements a two-phase homing pattern.
    /// The sequence is:
    /// 1. Initial linear movement based on spawn rotation (<see cref="HomingState.InitialLinear"/>) for <see cref="firstHomingDelay"/> seconds.
    /// 2. First homing phase (<see cref="HomingState.FirstHoming"/>) moving towards the target player's captured position for <see cref="firstHomingDuration"/> seconds.
    /// 3. Pause (<see cref="HomingState.PauseBeforeSecondHoming"/>) with no movement for <see cref="secondPauseDelay"/> seconds.
    /// 4. Second homing phase (<see cref="HomingState.SecondHoming"/>) moving towards a fixed point calculated relative to the bullet's position and the target's position at the start of this phase. This phase continues indefinitely.
    /// Movement uses Rigidbody position updates, requiring a Kinematic Rigidbody2D and synchronization via NetworkTransform.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))] // Assuming kinematic Rigidbody for movement
    public class DoubleHoming : NetworkBehaviour
    {
        // --- Parameters set by Initialize --- 
        private float initialSpeed;
        private float currentHomingSpeed;
        private float firstHomingDelay; // Duration of initial linear phase
        private float secondPauseDelay; // Duration of pause phase
        private float firstHomingDuration; // Duration of first homing phase
        private float secondHomingLookAheadDistance; // Distance used to calculate second homing target
        private ClientAuthMovement targetPlayer; // The opponent player to home towards

        // --- Internal State ---
        private Vector3 firstHomingTargetPosition; // Fixed position captured at start of first homing
        private Vector3 secondHomingTargetPosition; // Fixed position calculated at start of second homing
        private float timer; // Tracks time within the current state
        private HomingState currentState; // Current state in the movement pattern

        /// <summary>
        /// Defines the different stages of the double homing movement pattern.
        /// </summary>
        private enum HomingState
        {
            /// <summary>Initial straight movement before any homing.</summary>
            InitialLinear,
            /// <summary>First phase, homing towards a captured position for a fixed duration.</summary>
            FirstHoming,
            /// <summary>Pause phase, no movement between homing phases.</summary>
            PauseBeforeSecondHoming,
            /// <summary>Second phase, homing towards a calculated fixed point indefinitely.</summary>
            SecondHoming,
            /// <summary>State reached if initialization fails or behavior is manually stopped (currently unused in standard flow).</summary>
            Completed 
        }

        /// <summary>
        /// Initializes the behavior with parameters from the SpellcardAction.
        /// Must be called by the spawner on the server immediately after instantiation.
        /// </summary>
        /// <param name="speed">The initial speed for the <see cref="HomingState.InitialLinear"/> phase.</param>
        /// <param name="homingSpeed">The speed used during both <see cref="HomingState.FirstHoming"/> and <see cref="HomingState.SecondHoming"/> phases.</param>
        /// <param name="delay1">Duration (seconds) of the <see cref="HomingState.InitialLinear"/> phase (<see cref="firstHomingDelay"/>).</param>
        /// <param name="delay2">Duration (seconds) of the <see cref="HomingState.PauseBeforeSecondHoming"/> phase (<see cref="secondPauseDelay"/>).</param>
        /// <param name="duration1">Duration (seconds) of the <see cref="HomingState.FirstHoming"/> phase (<see cref="firstHomingDuration"/>).</param>
        /// <param name="lookAhead">Distance used to calculate the fixed target point for the <see cref="HomingState.SecondHoming"/> phase (<see cref="secondHomingLookAheadDistance"/>).</param>
        /// <param name="target">Reference to the opponent's <see cref="ClientAuthMovement"/> component.</param>
        public void Initialize(float speed, float homingSpeed, float delay1, float delay2, float duration1, float lookAhead, ClientAuthMovement target)
        {
            if (!IsServer) 
            {
                enabled = false; // Ensure only server runs logic
                return; 
            }

            initialSpeed = speed;
            currentHomingSpeed = homingSpeed;
            firstHomingDelay = delay1;
            secondPauseDelay = delay2;
            firstHomingDuration = duration1;
            secondHomingLookAheadDistance = lookAhead;
            targetPlayer = target;

            if (targetPlayer == null)
            {
                Debug.LogError("[DoubleHoming] Initialization failed: Target ClientAuthMovement is null.", this);
                currentState = HomingState.Completed;
                enabled = false;
                return;
            }

            timer = 0f;
            currentState = HomingState.InitialLinear;
            enabled = true; // Ensure component is enabled on server
        }

        /// <summary>
        /// Server-only Update loop driving the state machine.
        /// </summary>
        void Update()
        {
            // Redundant check as Initialize should disable if not server or null target, but safe to keep.
            if (!IsServer || targetPlayer == null || currentState == HomingState.Completed) return; 

            timer += Time.deltaTime;

            switch (currentState)
            {
                case HomingState.InitialLinear:
                    MoveLinear(initialSpeed);
                    if (timer >= firstHomingDelay)
                    {
                        // Capture target position ONLY when first homing starts
                        firstHomingTargetPosition = targetPlayer.transform.position;
                        currentState = HomingState.FirstHoming;
                        timer = 0f; // Reset timer for next state's duration
                    }
                    break;

                case HomingState.FirstHoming:
                    MoveTowards(firstHomingTargetPosition, currentHomingSpeed);
                    // Check if homing phase 1 duration is complete
                    if (timer >= firstHomingDuration)
                    {
                        currentState = HomingState.PauseBeforeSecondHoming;
                        timer = 0f;
                    }
                    break;

                case HomingState.PauseBeforeSecondHoming:
                    // Bullet stops during the pause (no movement call)
                    if (timer >= secondPauseDelay)
                    {
                        // Calculate direction towards player's current position at this instant
                        Vector3 directionToPlayer = (targetPlayer.transform.position - transform.position).normalized;
                         // Handle potential case where bullet is exactly on player
                        if (directionToPlayer == Vector3.zero) directionToPlayer = transform.up; 
                        // Calculate the fixed target point using look ahead distance
                        secondHomingTargetPosition = transform.position + directionToPlayer * secondHomingLookAheadDistance;
                        
                        currentState = HomingState.SecondHoming;
                        timer = 0f; // Reset timer (though not strictly needed for SecondHoming)
                    }
                    break;

                case HomingState.SecondHoming:
                    // Move towards the calculated relative target point INDEFINITELY
                    MoveTowards(secondHomingTargetPosition, currentHomingSpeed);
                    // No timer check, no transition out of this state
                    // Lifetime handled by NetworkBulletLifetime component
                    break;

                // Completed state is only entered via initialization failure now
                case HomingState.Completed:
                    enabled = false;
                    break;
            }
        }

        /// <summary>
        /// Moves the GameObject forward along its local Y-axis.
        /// </summary>
        private void MoveLinear(float speed)
        {
            transform.position += transform.up * speed * Time.deltaTime;
        }

        /// <summary>
        /// Moves the GameObject towards a target position without rotation.
        /// </summary>
        private void MoveTowards(Vector3 targetPosition, float speed)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            // Avoid NaN if already at target (should be rare)
            if (direction == Vector3.zero) return; 
            transform.position += direction * speed * Time.deltaTime;
        }

        // --- Targeting Note ---
        // Target is now ClientAuthMovement, passed via Initialize.
        // Spawner needs to get reference to opponent's ClientAuthMovement component.

        // TODO: Refine Homing state completion conditions.
    }
} 