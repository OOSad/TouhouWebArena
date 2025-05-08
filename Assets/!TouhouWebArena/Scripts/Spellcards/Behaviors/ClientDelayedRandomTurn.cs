using UnityEngine;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// [Client Only] 
    /// 1. Applies an initial random rotation offset.
    /// 2. Moves straight for a specified delay.
    /// 3. After the delay, continues moving straight while also rotating
    ///    in a client-side randomly chosen direction at a client-side randomly chosen speed.
    /// </summary>
    public class ClientDelayedRandomTurn : MonoBehaviour
    {
        private float _speed;
        private float _delay;
        private float _turnSpeed; // Degrees per second, chosen client-side
        private int _turnDirection; // -1 for left, 1 for right, chosen client-side

        private float _timer = 0f;
        private bool _isTurning = false;

        public void Initialize(float speed, float delay, float minTurnSpeed, float maxTurnSpeed, float spreadAngle)
        {
            _speed = speed;
            _delay = delay;
            
            // Apply initial random spread rotation (client-side randomness)
            if (spreadAngle > 0)
            {
                float randomAngleOffset = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
                transform.Rotate(0f, 0f, randomAngleOffset);
            }

            // Choose random turn direction and speed (client-side randomness)
            _turnDirection = (Random.value < 0.5f) ? -1 : 1;
            _turnSpeed = Random.Range(minTurnSpeed, maxTurnSpeed);

            // Reset state variables
            _timer = 0f;
            _isTurning = false;
            enabled = true; 
        }

        void Update()
        {
            _timer += Time.deltaTime;

            if (!_isTurning && _timer >= _delay)
            {
                _isTurning = true;
            }

            // Apply forward movement
            transform.position += transform.up * _speed * Time.deltaTime;

            // Apply rotation if turning phase has started
            if (_isTurning)
            {
                float rotationThisFrame = _turnDirection * _turnSpeed * Time.deltaTime;
                transform.Rotate(0f, 0f, rotationThisFrame);
            }
        }

        void OnEnable()
        {
            // Reset state if re-enabled from a pool, though Initialize should be called.
            _timer = 0f;
            _isTurning = false;
            // Random values (_turnDirection, _turnSpeed, initial spread) will be re-set by Initialize.
        }
    }
} 