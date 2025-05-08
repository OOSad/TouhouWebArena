using UnityEngine;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// [Client Only] Moves the GameObject forward (transform.up) at an initial speed for a delay.
    /// After the delay, it locks onto the direction towards a target position (captured at initialization)
    /// and continues moving in that fixed direction at a homing speed.
    /// </summary>
    public class ClientDelayedHoming : MonoBehaviour
    {
        private float _initialSpeed;
        private float _homingSpeed;
        private float _homingDelay;
        private Vector3 _initialTargetPosition; // Target position captured at initialization

        private float _delayTimer;
        private bool _isHoming;
        private Vector3 _lockedTargetDirection;
        private bool _targetDirectionLocked;

        void Update()
        {
            if (!_isHoming)
            {
                // Initial linear movement phase
                transform.position += transform.up * _initialSpeed * Time.deltaTime;
                _delayTimer -= Time.deltaTime;

                if (_delayTimer <= 0f)
                {
                    _isHoming = true; // Start homing phase
                    // Calculate and lock direction only if not already locked
                    if (!_targetDirectionLocked)
                    {
                        if (_initialTargetPosition == Vector3.zero) // Should be set by Initialize
                        {
                            Debug.LogWarning("[ClientDelayedHoming] Target position was not set. Defaulting to forward.");
                            _lockedTargetDirection = transform.up;
                        }
                        else
                        {
                             _lockedTargetDirection = (_initialTargetPosition - transform.position).normalized;
                        }
                       
                        // Prevent zero direction if already at the target position
                        if (_lockedTargetDirection == Vector3.zero)
                        {
                            _lockedTargetDirection = transform.up; // Default to current forward direction
                        }
                        _targetDirectionLocked = true;
                    }
                }
            }
            
            // Move in the locked direction once homing starts and direction is locked
            if (_isHoming && _targetDirectionLocked)
            {
                transform.position += _lockedTargetDirection * _homingSpeed * Time.deltaTime;
            }
        }

        /// <summary>
        /// Initializes the delayed homing behavior.
        /// </summary>
        /// <param name="initialSpeed">Speed during the initial delay phase.</param>
        /// <param name="homingSpeed">Speed during the homing phase (after delay).</param>
        /// <param name="homingDelay">Duration of the initial delay phase.</param>
        /// <param name="targetPosition">The world position of the target to home towards (captured at initialization).</param>
        public void Initialize(float initialSpeed, float homingSpeed, float homingDelay, Vector3 targetPosition)
        {
            _initialSpeed = initialSpeed;
            _homingSpeed = homingSpeed;
            _homingDelay = homingDelay;
            _initialTargetPosition = targetPosition;

            ResetState();
            this.enabled = true; // Ensure it's enabled
        }

        /// <summary>
        /// Resets the internal state variables to their defaults.
        /// </summary>
        private void ResetState()
        {
            _delayTimer = _homingDelay;
            _isHoming = false;
            _targetDirectionLocked = false;
            _lockedTargetDirection = Vector3.zero;
        }

        void OnEnable()
        {
            // Optionally, ResetState here if Initialize might not be called immediately after activation.
            // For now, assuming Initialize sets up the state.
        }
    }
} 