using UnityEngine;

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// [Client Only] Implements a two-phase homing pattern with an initial linear phase and a pause.
    /// </summary>
    public class ClientDoubleHoming : MonoBehaviour
    {
        private float _initialSpeed;
        private float _homingSpeed;
        private float _firstHomingDelay;       // Duration of InitialLinear
        private float _firstHomingDuration;    // Duration of FirstHoming
        private float _secondPauseDelay;       // Duration of PauseBeforeSecondHoming
        private float _secondHomingLookAheadDistance;
        private Transform _targetTransform; // Target player's transform

        private Vector3 _firstHomingTargetPosition;  // Captured at start of FirstHoming
        private Vector3 _secondHomingTargetPosition; // Calculated at start of SecondHoming
        private float _timer;          // Tracks time within the current state
        private HomingState _currentState; // Current state

        private enum HomingState
        {
            InitialLinear,
            FirstHoming,
            PauseBeforeSecondHoming,
            SecondHoming,
            Completed // If target is lost or initialization fails
        }

        public void Initialize(float initialSpeed, float homingSpeed, float firstHomingDelay, 
                             float firstHomingDuration, float secondPauseDelay, 
                             float secondHomingLookAheadDistance, Transform targetTransform)
        {
            _initialSpeed = initialSpeed;
            _homingSpeed = homingSpeed;
            _firstHomingDelay = firstHomingDelay;
            _firstHomingDuration = firstHomingDuration;
            _secondPauseDelay = secondPauseDelay;
            _secondHomingLookAheadDistance = secondHomingLookAheadDistance;
            _targetTransform = targetTransform;

            if (_targetTransform == null)
            {
                Debug.LogWarning("[ClientDoubleHoming] Target transform is null. Behavior will not run.", this);
                _currentState = HomingState.Completed;
                enabled = false;
                return;
            }

            _timer = 0f;
            _currentState = HomingState.InitialLinear;
            enabled = true;
        }

        void Update()
        {
            if (_currentState == HomingState.Completed) 
            {
                enabled = false; // Ensure it stops updating
                return;
            }
            // If target becomes null mid-flight (e.g. player disconnects, despawns)
            if (_targetTransform == null && _currentState != HomingState.SecondHoming) // SecondHoming uses a fixed point
            {
                Debug.LogWarning("[ClientDoubleHoming] Target transform became null. Switching to completed state.", this);
                _currentState = HomingState.Completed;
                enabled = false;
                return;
            }

            _timer += Time.deltaTime;

            switch (_currentState)
            {
                case HomingState.InitialLinear:
                    MoveLinear(_initialSpeed);
                    if (_timer >= _firstHomingDelay)
                    {
                        if(_targetTransform != null) _firstHomingTargetPosition = _targetTransform.position;
                        else { _currentState = HomingState.Completed; break; }
                        _currentState = HomingState.FirstHoming;
                        _timer = 0f;
                    }
                    break;

                case HomingState.FirstHoming:
                    MoveTowards(_firstHomingTargetPosition, _homingSpeed);
                    if (_timer >= _firstHomingDuration)
                    {
                        _currentState = HomingState.PauseBeforeSecondHoming;
                        _timer = 0f;
                    }
                    break;

                case HomingState.PauseBeforeSecondHoming:
                    // No movement
                    if (_timer >= _secondPauseDelay)
                    {
                        if (_targetTransform != null)
                        {
                            Vector3 directionToPlayer = (_targetTransform.position - transform.position).normalized;
                            if (directionToPlayer == Vector3.zero) directionToPlayer = transform.up;
                            _secondHomingTargetPosition = transform.position + directionToPlayer * _secondHomingLookAheadDistance;
                        }
                        else { _currentState = HomingState.Completed; break; }
                        _currentState = HomingState.SecondHoming;
                        _timer = 0f;
                    }
                    break;

                case HomingState.SecondHoming:
                    MoveTowards(_secondHomingTargetPosition, _homingSpeed);
                    // This state is indefinite, lifetime handled by ClientProjectileLifetime
                    break;
            }
        }

        private void MoveLinear(float speed)
        {
            transform.position += transform.up * speed * Time.deltaTime;
        }

        private void MoveTowards(Vector3 targetPosition, float speed)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction == Vector3.zero) return;
            transform.position += direction * speed * Time.deltaTime;
        }

        void OnEnable()
        {
            // If reusing from pool, Initialize should always be called to set target and parameters.
            // If currentState is somehow Completed, ensure it stays disabled.
            if (_currentState == HomingState.Completed) enabled = false; 
        }
    }
} 