using UnityEngine;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// [Client Only] Moves the GameObject forward while continuously rotating towards a fixed target position.
    /// </summary>
    public class ClientHomingMovement : MonoBehaviour
    {
        private float _moveSpeed;
        private float _turnSpeed; // Degrees per second
        private Vector3 _targetPosition; // Fixed world position to home towards

        private bool _isActive = false;

        public void Initialize(float moveSpeed, float turnSpeed, Vector3 targetPosition)
        {
            _moveSpeed = moveSpeed;
            _turnSpeed = turnSpeed;
            _targetPosition = targetPosition;

            // A simple check if targetPosition is meaningful. 
            // Could be more robust (e.g. if it's Vector3.zero and that's an invalid/default state)
            // For now, assume any non-zero vector might be a target.
            // Or, rely on spawner to not activate if target is invalid.
            _isActive = true; // Assume valid if initialized
            enabled = true;
        }

        void Update()
        {
            if (!_isActive) 
            {
                // If not active, could just move linearly or do nothing.
                // For now, do nothing if not active.
                return; 
            }

            // Calculate direction to target
            Vector3 directionToTarget = (_targetPosition - transform.position).normalized;

            if (directionToTarget == Vector3.zero)
            {
                // Already at target, just move forward based on current rotation
                transform.position += transform.up * _moveSpeed * Time.deltaTime;
                return;
            }

            // Calculate the target angle in degrees. Atan2 returns radians.
            // -90f adjustment because Atan2 calculates angle from positive X-axis, 
            // but transform.up (0,1,0) corresponds to a 90-degree Z rotation from (1,0,0).
            float targetAngleZ = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;

            // Get current rotation around Z axis
            float currentAngleZ = transform.eulerAngles.z;

            // Calculate new angle by rotating towards target angle, clamped by turn speed
            float newAngleZ = Mathf.MoveTowardsAngle(currentAngleZ, targetAngleZ, _turnSpeed * Time.deltaTime);

            // Apply new rotation
            transform.eulerAngles = new Vector3(0, 0, newAngleZ);

            // Move forward based on the new orientation
            transform.position += transform.up * _moveSpeed * Time.deltaTime;
        }

        void OnEnable()
        {
            // Initialize sets _isActive. If it was set to false (e.g. hypothetical invalid target check in Initialize),
            // it might be good to ensure 'enabled' reflects that here, but Initialize already sets 'enabled = true'.
            // If pooling and re-activating, Initialize must be called again.
        }
    }
} 