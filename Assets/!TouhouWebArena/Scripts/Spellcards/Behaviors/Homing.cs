using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// Server-authoritative movement behavior that continuously adjusts its rotation
    /// to face a target position and moves forward.
    /// Requires Rigidbody2D (Kinematic) and NetworkTransform for synchronization.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(NetworkTransform))]
    public class Homing : NetworkBehaviour
    {
        private float _moveSpeed; // Speed of forward movement
        private float _turnSpeed; // Max degrees per second to turn
        private Vector3 _targetPosition; // Captured target world position
        private Rigidbody2D _rb;
        private bool _initialized = false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer) 
            {
                enabled = false;
                return;
            }
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
            {
                Debug.LogError("[Homing] Rigidbody2D component not found!", gameObject);
                enabled = false;
                return;
            }
             _rb.isKinematic = true;
            // Reset state in case of pooling/reuse
            _initialized = false; 
        }

        /// <summary>
        /// Initializes the behavior parameters. Called by the server spawner.
        /// </summary>
        public void Initialize(float moveSpeed, float turnSpeed, ulong targetId, Vector3 targetPosition)
        {
            if (!IsServer) return;
            _moveSpeed = moveSpeed;
            _turnSpeed = turnSpeed;
            _targetPosition = targetPosition;
            _initialized = true;
            enabled = true;
        }

        void FixedUpdate()
        {
            if (!IsServer || !enabled || !_initialized) return;

            // Calculate direction to target
            Vector2 directionToTarget = (_targetPosition - transform.position).normalized;
            if (directionToTarget == Vector2.zero) 
            {
                // Already at target or calculation failed, continue straight
                _rb.linearVelocity = transform.up * _moveSpeed;
                return; 
            }

            // Calculate target rotation
            float targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f; // Adjust for transform.up being forward
            float currentAngle = _rb.rotation;

            // Calculate new angle by rotating towards target angle
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, _turnSpeed * Time.fixedDeltaTime);

            // Apply rotation
            _rb.MoveRotation(newAngle);

            // Apply forward velocity based on the *new* rotation
            _rb.linearVelocity = transform.up * _moveSpeed;
        }
    }
} 