using UnityEngine;
using Unity.Netcode;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// A server-authoritative movement behavior that moves the attached GameObject forward
    /// along its local Y-axis (<c>transform.up</c>) at a constant speed.
    /// The initial direction must be set by rotating the GameObject upon spawning.
    /// </summary>
    public class LinearMovement : NetworkBehaviour // Inherits from NetworkBehaviour for potential network context, even if movement is local
    {
        // Removed public speed field, now managed internally
        // public float speed = 5f; 

        // --- Internal State for Speed Transition ---
        private bool _useTransition = false;
        private float _initialSpeed = 0f;
        private float _targetSpeed = 5f; // Default target speed
        private float _transitionDuration = 0f;
        private float _spawnTime = 0f;

        // We assume the initial direction is baked into the transform's rotation
        // by the spawning logic.
        // Movement is executed client-side for performance in bullet hell scenarios.

        /// <summary>
        /// Server-only Update loop to calculate current speed and move the object.
        /// Client positions are updated via NetworkTransform synchronization.
        /// </summary>
        void Update()
        {
            if (!IsServer) return;

            float currentSpeed = CalculateCurrentSpeed();

            // Use transform.up because in 2D, forward is typically the Y axis.
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

            float elapsedTime = Time.time - _spawnTime;

            if (elapsedTime >= _transitionDuration)
            {
                // Transition finished
                _useTransition = false; // Stop calculating lerp
                return _targetSpeed;
            }
            else
            {
                // Still transitioning
                // Ensure duration is not zero to avoid division by zero
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
            if (!IsServer) return;
            _useTransition = false;
            _targetSpeed = targetSpeed;
            // _initialSpeed, _transitionDuration, _spawnTime are irrelevant
        }

        /// <summary>
        /// **[Server Only]** Initializes the movement with a speed transition.
        /// </summary>
        /// <param name="initialSpeed">The speed the bullet starts with.</param>
        /// <param name="targetSpeed">The speed the bullet transitions towards.</param>
        /// <param name="transitionDuration">The duration of the speed transition in seconds.</param>
        public void Initialize(float initialSpeed, float targetSpeed, float transitionDuration)
        {
            if (!IsServer) return;

            if (transitionDuration <= 0f)
            {
                // If duration is zero or negative, just use the target speed instantly
                Initialize(targetSpeed);
            }
            else
            {
                _useTransition = true;
                _initialSpeed = initialSpeed;
                _targetSpeed = targetSpeed;
                _transitionDuration = transitionDuration;
                _spawnTime = Time.time; // Record the time transition starts
            }
        }
    }
}
