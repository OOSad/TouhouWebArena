using UnityEngine;
// using Unity.Netcode; // No longer a NetworkBehaviour

namespace TouhouWebArena.Spellcards.Behaviors // Keep namespace for now, can be refactored later
{
    /// <summary>
    /// A client-side movement behavior that moves the attached GameObject forward
    /// along its local Y-axis (<c>transform.up</c>) at a constant or transitioning speed.
    /// The initial direction must be set by rotating the GameObject upon spawning/activation.
    /// </summary>
    public class LinearMovement : MonoBehaviour // Changed from NetworkBehaviour
    {
        // Removed public speed field, now managed internally
        // public float speed = 5f; 

        // --- Internal State for Speed Transition ---
        private bool _useTransition = false;
        private float _initialSpeed = 0f;
        private float _targetSpeed = 5f; // Default target speed
        private float _transitionDuration = 0f;
        private float _startTime = 0f; // Renamed from _spawnTime for clarity, as it's start of movement/transition

        // We assume the initial direction is baked into the transform's rotation
        // by the spawning logic.
        // Movement is executed client-side for performance in bullet hell scenarios.

        /// <summary>
        /// Server-only Update loop to calculate current speed and move the object.
        /// Client positions are updated via NetworkTransform synchronization.
        /// </summary>
        void Update()
        {
            // Logic now runs on the client that owns/manages this projectile
            // if (!IsServer) return; // REMOVED

            float currentSpeed = CalculateCurrentSpeed();
            transform.position += transform.up * currentSpeed * Time.deltaTime;
        }

        /// <summary>
        /// **[Server Only]** Calculates the speed for the current frame, handling the transition if active.
        /// </summary>
        private float CalculateCurrentSpeed()
        {
            if (!_useTransition) 
            {
                return _targetSpeed;
            }

            float elapsedTime = Time.time - _startTime;

            if (elapsedTime >= _transitionDuration)
            {
                _useTransition = false; 
                return _targetSpeed;
            }
            else
            {
                if (_transitionDuration <= 0f) return _targetSpeed;
                return Mathf.Lerp(_initialSpeed, _targetSpeed, elapsedTime / _transitionDuration);
            }
        }

        // --- Initialization Methods (Called by Spawner) --- 

        /// <summary>
        /// **[Server Only]** Initializes the movement with a constant speed (no transition).
        /// </summary>
        /// <param name="targetSpeed">The constant movement speed.</param>
        public void Initialize(float targetSpeed)
        {
            // if (!IsServer) return; // REMOVED
            _useTransition = false;
            _targetSpeed = targetSpeed;
            _startTime = Time.time; // Set start time even for non-transition for consistency if needed later
        }

        /// <summary>
        /// **[Server Only]** Initializes the movement with a speed transition.
        /// </summary>
        /// <param name="initialSpeed">The speed the bullet starts with.</param>
        /// <param name="targetSpeed">The speed the bullet transitions towards.</param>
        /// <param name="transitionDuration">The duration of the speed transition in seconds.</param>
        public void Initialize(float initialSpeed, float targetSpeed, float transitionDuration)
        {
            // if (!IsServer) return; // REMOVED

            if (transitionDuration <= 0f)
            {
                Initialize(targetSpeed);
            }
            else
            {
                _useTransition = true;
                _initialSpeed = initialSpeed;
                _targetSpeed = targetSpeed;
                _transitionDuration = transitionDuration;
                _startTime = Time.time; 
            }
        }
        
        // Call this when the object is enabled/retrieved from pool to reset its movement state
        void OnEnable()
        {
            // If not using transition by default, ensure speed is set based on a default or last initialized value.
            // For now, Initialize() must be called after getting from pool.
            // Consider resetting _startTime here if Initialize isn't called immediately after GetObject + SetActive(true)
            // _startTime = Time.time; // Could be a default reset, but explicit Initialize is better.
        }
    }
}
