using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// Server-authoritative movement behavior.
    /// 1. Applies an initial random rotation offset within a defined spread angle.
    /// 2. Moves straight for a specified delay.
    /// 3. After the delay, continues moving straight while also rotating
    ///    in a randomly chosen direction (left/right) at a randomly chosen speed.
    /// Requires Rigidbody2D (Kinematic) and NetworkTransform for synchronization.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(NetworkTransform))]
    public class DelayedRandomTurn : NetworkBehaviour
    {
        private float _speed;
        private float _delay;
        private float _turnSpeed; // Degrees per second
        private int _turnDirection; // -1 for left, 1 for right

        private float _timer = 0f;
        private bool _isTurning = false;
        private Rigidbody2D _rb;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer) 
            {
                enabled = false; // Only server controls behavior
                return;
            }
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
            {
                Debug.LogError("[DelayedRandomTurn] Rigidbody2D component not found!", gameObject);
                enabled = false;
                return;
            }
             _rb.isKinematic = true; // Ensure kinematic for direct transform/rigidbody manipulation
        }

        /// <summary>
        /// Initializes the behavior parameters. Called by the server spawner.
        /// </summary>
        public void Initialize(float speed, float delay, float minTurnSpeed, float maxTurnSpeed, float spreadAngle)
        {
            if (!IsServer) return;

            _speed = speed;
            _delay = delay;
            
            // Apply initial random spread rotation
            if (spreadAngle > 0)
            {
                float randomAngleOffset = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
                transform.Rotate(0f, 0f, randomAngleOffset);
            }

            // Choose random turn direction and speed
            _turnDirection = (Random.value < 0.5f) ? -1 : 1;
            _turnSpeed = Random.Range(minTurnSpeed, maxTurnSpeed);

            // Reset state variables
            _timer = 0f;
            _isTurning = false;
            enabled = true; // Ensure the component is enabled server-side
        }

        void FixedUpdate() // Using FixedUpdate for Rigidbody movement
        {
            if (!IsServer || !enabled) return;
            
            _timer += Time.fixedDeltaTime;

            if (!_isTurning && _timer >= _delay)
            {
                _isTurning = true;
            }

            // Calculate forward movement based on current rotation
            Vector2 forwardVelocity = transform.up * _speed;

            // Apply rotation if turning phase has started
            if (_isTurning)
            {
                float rotationThisFrame = _turnDirection * _turnSpeed * Time.fixedDeltaTime;
                _rb.MoveRotation(_rb.rotation + rotationThisFrame);
            }

            // Apply forward velocity
             _rb.velocity = forwardVelocity;
        }

         // Optional: Consider disabling on becoming Host/Server if needed for specific scenarios
        /*
        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            enabled = false; // Disable for owner if server is authoritative
        }
        */
    }
} 