using UnityEngine;
using Unity.Netcode;

// namespace TouhouWebArena.Client.Enemies // Keeping it in global for now to match ClientSpiritSpawnHandler
// {
    public class ClientSpiritController : MonoBehaviour
    {
        private bool _isInitialized = false;
        private bool _shouldAim;
        private ulong _targetPlayerClientId;
        private bool _isRevengeSpawn;
        private float _initialVelocity;
        private int _spiritType;
        private Transform _cachedTransform;
        private Transform _currentTargetTransform; // For homing, used only at init now

        private bool _isActivated = false;
        private float _currentSpeed;
        private Vector2 _currentDirection = Vector2.down; // Default direction

        // Dependencies (will be set up later)
        private ClientSpiritHealth _spiritHealth;
        private ClientSpiritTimeoutAttack _timeoutAttack;

        [Header("Visuals")]
        [SerializeField] private GameObject normalSpiritVisual;
        [SerializeField] private GameObject activatedSpiritVisual;

        [Header("Activated Movement Settings")]
        [SerializeField] private float activatedInitialUpwardSpeed = 0.5f;
        [SerializeField] private float activatedMaxUpwardSpeed = 3.0f;
        [SerializeField] private float activatedAcceleration = 1.0f; // Units per second per second

        void Awake()
        {
            _cachedTransform = transform;
            _spiritHealth = GetComponent<ClientSpiritHealth>();
            _timeoutAttack = GetComponent<ClientSpiritTimeoutAttack>();

            // Ensure default visual state on Awake, in case it's not set on prefab
            if (normalSpiritVisual != null) normalSpiritVisual.SetActive(true);
            if (activatedSpiritVisual != null) activatedSpiritVisual.SetActive(false);
        }

        public void Initialize(bool shouldAim, ulong targetPlayerClientId, bool isRevengeSpawn, 
                               float initialVelocity, int spiritType, Transform originTransform /* Not used currently, but good for consistency */)
        {
            _shouldAim = shouldAim;
            _targetPlayerClientId = targetPlayerClientId;
            _isRevengeSpawn = isRevengeSpawn;
            _initialVelocity = initialVelocity;
            _spiritType = spiritType;
            _currentSpeed = _initialVelocity;
            _isActivated = false;
            _currentDirection = Vector2.down; // Default to straight down

            // Reset visuals to normal on initialize/reuse
            if (normalSpiritVisual != null) normalSpiritVisual.SetActive(true);
            if (activatedSpiritVisual != null) activatedSpiritVisual.SetActive(false);

            // Resolve target transform if aiming and set initial direction
            if (_shouldAim && _targetPlayerClientId != 0 && NetworkManager.Singleton != null)
            {
                NetworkObject playerNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(_targetPlayerClientId);
                if (playerNetObj != null)
                {
                    _currentTargetTransform = playerNetObj.transform; // Still useful to store for debugging or future use
                    // Calculate initial direction towards player at spawn time
                    _currentDirection = (_currentTargetTransform.position - originTransform.position).normalized;
                    Debug.Log($"[ClientSpiritController] Initialized for AIMING. Target: {playerNetObj.name}, Initial Direction: {_currentDirection}", this);
                }
                else
                {
                    Debug.LogWarning($"[ClientSpiritController] Could not find NetworkObject for target Player CID {_targetPlayerClientId}. Will move straight down.", this);
                    _shouldAim = false; // Fallback to non-homing
                }
            }
            else if (_shouldAim) // _shouldAim was true but other conditions failed
            {
                 Debug.LogWarning($"[ClientSpiritController] Told to aim but targetPlayerClientId is 0 or NetworkManager is unavailable. Will move straight down.", this);
                _shouldAim = false; // Fallback to non-homing
            }
            // If not aiming, _currentDirection remains Vector2.down

            _isInitialized = true;
            // Debug.Log($"[ClientSpiritController] Initialized: Vel={_initialVelocity}, Aim={_shouldAim}, TargetCID={_targetPlayerClientId}", this);
        }

        void Update()
        {
            if (!_isInitialized || !NetworkManager.Singleton.IsClient) // Ensure it runs only on clients and is initialized
            {
                return;
            }

            if (_isActivated)
            {
                // Accelerate if activated and moving up
                if (_currentDirection == Vector2.up)
                {
                    _currentSpeed = Mathf.MoveTowards(_currentSpeed, activatedMaxUpwardSpeed, activatedAcceleration * Time.deltaTime);
                }
            }
            
            _cachedTransform.Translate(_currentDirection * _currentSpeed * Time.deltaTime);

            // TODO: Add logic for despawn if it goes off-screen (handled by ClientProjectileLifetime usually, but spirits aren't projectiles)
            // This might need a separate component or logic here.
        }

        public void ActivateSpirit()
        {
            if (_isActivated) return;

            _isActivated = true;
            _currentSpeed = activatedInitialUpwardSpeed; // Set to initial upward speed
            _currentDirection = Vector2.up;
            Debug.Log("[ClientSpiritController] Spirit Activated! Moving up with initial speed: " + _currentSpeed, this);

            if (normalSpiritVisual != null) normalSpiritVisual.SetActive(false);
            if (activatedSpiritVisual != null) activatedSpiritVisual.SetActive(true);

            // Notify Health component to reduce HP
            if (_spiritHealth != null) _spiritHealth.OnActivated();

            // Notify TimeoutAttack to start its timer
            if (_timeoutAttack != null) _timeoutAttack.StartTimeout(1.5f, _targetPlayerClientId); // Assuming 1.5s timeout NOW
        }

        // Call this before returning to pool to reset state
        public void Deinitialize()
        {
            _isInitialized = false;
            _isActivated = false;
            _currentTargetTransform = null;
            _currentDirection = Vector2.down; // Reset direction on deinitialization
            // Reset other necessary fields

            // Reset visuals on deinitialize
            if (normalSpiritVisual != null) normalSpiritVisual.SetActive(true);
            if (activatedSpiritVisual != null) activatedSpiritVisual.SetActive(false);
        }

        void OnDisable()
        {
            // When returned to pool, deinitialize
            Deinitialize();
            if (_timeoutAttack != null) _timeoutAttack.StopTimeoutAttack(); // Also ensure timeout is stopped
        }
    }
// } 