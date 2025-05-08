using UnityEngine;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// [Client Only] A client-side movement behavior that moves the attached GameObject forward
    /// along its local Y-axis (<c>transform.up</c>) at a constant or transitioning speed.
    /// The initial direction must be set by rotating the GameObject upon spawning/activation.
    /// </summary>
    public class ClientLinearMovement : MonoBehaviour
    {
        private float _speed; // This will be the final calculated speed
        private bool _useInitialSpeed;
        private float _initialSpeedValue; // The initial speed if _useInitialSpeed is true
        private float _transitionDuration;
        private float _currentTransitionTime;

        // ADDED for speed increment logic
        private float _baseSpeed;
        private float _speedIncrement;
        private int _bulletIndex;

        private bool _isInitialized = false;

        // MODIFIED Initialize signature
        public void Initialize(float baseSpeed, float speedIncrement, int bulletIndex, bool useInitialSpeed, float initialSpeedValue, float transitionDuration)
        {
            _baseSpeed = baseSpeed;
            _speedIncrement = speedIncrement;
            _bulletIndex = bulletIndex;

            _useInitialSpeed = useInitialSpeed;
            _initialSpeedValue = initialSpeedValue;
            _transitionDuration = transitionDuration > 0 ? transitionDuration : 0.001f; // Avoid division by zero
            _currentTransitionTime = 0f;

            // Calculate the final target speed for this specific bullet
            float targetSpeed = _baseSpeed + (_bulletIndex * _speedIncrement);

            if (_useInitialSpeed)
            {
                _speed = _initialSpeedValue; // Start at initial speed
                // If initial speed is different from target speed, we need to transition
                // If they are the same, transition logic will effectively do nothing.
            }
            else
            {
                _speed = targetSpeed; // Start directly at target speed
            }
            
            // If we are using initial speed and it's different from the calculated target speed, 
            // _speed will be adjusted towards targetSpeed in Update.
            // Otherwise, _speed is already the final speed.

            _isInitialized = true;
            // Debug.Log($"[ClientLinearMovement] Initialized bullet {_bulletIndex}: baseSpeed={_baseSpeed}, increment={_speedIncrement}, finalSpeed (before transition)={_speed}, targetSpeed={targetSpeed}");
        }

        void Update()
        {
            if (!_isInitialized || !enabled) return;

            float currentSpeedThisFrame = _speed;
            float targetSpeed = _baseSpeed + (_bulletIndex * _speedIncrement);

            if (_useInitialSpeed && _currentTransitionTime < _transitionDuration)
            {
                _currentTransitionTime += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(_currentTransitionTime / _transitionDuration);
                currentSpeedThisFrame = Mathf.Lerp(_initialSpeedValue, targetSpeed, lerpFactor);
                _speed = currentSpeedThisFrame; // Update _speed for next frame if transition isn't done
            }
            else if (_useInitialSpeed)
            {
                // Transition finished, ensure speed is set to targetSpeed
                currentSpeedThisFrame = targetSpeed;
                _speed = targetSpeed; 
                _useInitialSpeed = false; // Stop transitioning
            }
            // If not using initial speed, currentSpeedThisFrame is already _speed (which is targetSpeed)

            transform.Translate(Vector3.up * currentSpeedThisFrame * Time.deltaTime);
        }
    }
} 