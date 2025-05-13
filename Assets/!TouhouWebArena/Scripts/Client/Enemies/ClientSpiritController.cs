using UnityEngine;
using Unity.Netcode;
using System.Collections;

// namespace TouhouWebArena.Client.Enemies // Keeping it in global for now to match ClientSpiritSpawnHandler
// {
    public class ClientSpiritController : MonoBehaviour
    {
        private bool _isInitialized = false;
        private bool _shouldAim;
        private ulong _targetNetworkObjectId;
        private bool _isRevengeSpawn;
        private float _initialVelocity;
        private int _spiritType;
        private Transform _cachedTransform;
        private Transform _currentTargetTransform; // For homing, used only at init now

        private PlayerRole _owningPlayerRole = PlayerRole.None;

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

        [Header("Normal Spirit Lifetime & Boundaries")]
        [SerializeField] private float normalSpiritMaxLifetime = 15f;
        [SerializeField] private float offScreenLimitTop = 7f;
        [SerializeField] private float offScreenLimitBottom = -7f;
        [SerializeField] private float offScreenLimitHorizontal = 10f;

        private Coroutine _normalLifetimeCoroutine;

        void Awake()
        {
            _cachedTransform = transform;
            _spiritHealth = GetComponent<ClientSpiritHealth>();
            _timeoutAttack = GetComponent<ClientSpiritTimeoutAttack>();

            // Ensure default visual state on Awake, in case it's not set on prefab
            if (normalSpiritVisual != null) normalSpiritVisual.SetActive(true);
            if (activatedSpiritVisual != null) activatedSpiritVisual.SetActive(false);
        }

        public void Initialize(PlayerRole owningPlayerRole,
                               bool shouldAim, ulong targetNetworkObjId, bool isRevengeSpawn, 
                               float initialVelocity, int spiritType, Transform originTransform)
        {
            _owningPlayerRole = owningPlayerRole;
            _shouldAim = shouldAim;
            _targetNetworkObjectId = targetNetworkObjId;
            _isRevengeSpawn = isRevengeSpawn;
            _initialVelocity = initialVelocity;
            _spiritType = spiritType;
            _currentSpeed = _initialVelocity;
            _isActivated = false;
            _currentDirection = Vector2.down; // Default to straight down

            // Reset visuals to normal on initialize/reuse
            if (normalSpiritVisual != null) normalSpiritVisual.SetActive(true);
            if (activatedSpiritVisual != null) activatedSpiritVisual.SetActive(false);

            // Stop any previous lifetime coroutine if re-initializing from pool
            if (_normalLifetimeCoroutine != null)
            {
                StopCoroutine(_normalLifetimeCoroutine);
                _normalLifetimeCoroutine = null;
            }
            // Start lifetime for normal spirits (will be stopped if activated)
            _normalLifetimeCoroutine = StartCoroutine(NormalLifetimeCoroutine());

            // Resolve target transform if aiming and set initial direction
            _currentTargetTransform = null; // Clear previous target
            if (_shouldAim && _targetNetworkObjectId != 0 && NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                // Use NetworkObjectId to get the NetworkObject
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_targetNetworkObjectId, out NetworkObject playerNetObj))
                {
                    _currentTargetTransform = playerNetObj.transform; 
                    _currentDirection = (_currentTargetTransform.position - originTransform.position).normalized;
                    // Debug.Log($"[ClientSpiritController] Initialized for AIMING. Target NO ID: {_targetNetworkObjectId} ({playerNetObj.name}), Initial Direction: {_currentDirection}", this);
                }
                else
                {
                    Debug.LogWarning($"[ClientSpiritController] Could not find NetworkObject for targetNetworkObjectId {_targetNetworkObjectId}. Will move straight down.", this);
                    _shouldAim = false; // Fallback to non-homing
                }
            }
            else if (_shouldAim) // _shouldAim was true but other conditions failed
            {
                 Debug.LogWarning($"[ClientSpiritController] Told to aim but targetNetworkObjectId is 0 or NetworkManager/SpawnManager is unavailable. Will move straight down.", this);
                _shouldAim = false; // Fallback to non-homing
            }
            // If not aiming, _currentDirection remains Vector2.down

            _isInitialized = true;
            // Debug.Log($"[ClientSpiritController] Initialized: Vel={_initialVelocity}, Aim={_shouldAim}, TargetNetObjID={_targetNetworkObjectId}", this);
        }

        private IEnumerator NormalLifetimeCoroutine()
        {
            yield return new WaitForSeconds(normalSpiritMaxLifetime);
            if (!_isActivated && _isInitialized && gameObject.activeInHierarchy) 
            {
                // Debug.Log($"[ClientSpiritController] Normal spirit {gameObject.name} reached max lifetime. Returning to pool.");
                if (_spiritHealth != null) _spiritHealth.ForceReturnToPool(); 
                else if (ClientGameObjectPool.Instance != null) ClientGameObjectPool.Instance.ReturnObject(gameObject);
            }
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
            
            // --- Calculate desired movement for this frame ---
            Vector3 desiredMovement = _currentDirection * _currentSpeed * Time.deltaTime;
            Vector3 potentialPosition = _cachedTransform.position + desiredMovement;
            float boundaryX = 0.0f; // Centerline boundary

            // --- Boundary Check --- 
            bool crossedBoundary = false;
            if (_owningPlayerRole == PlayerRole.Player1 && potentialPosition.x > boundaryX) // P1 spirit crossed right
            {
                crossedBoundary = true;
            }
            else if (_owningPlayerRole == PlayerRole.Player2 && potentialPosition.x < boundaryX) // P2 spirit crossed left
            {
                crossedBoundary = true;
            }

            // --- Apply translation OR despawn if boundary crossed ---
            if (crossedBoundary)
            {
                // Despawn if boundary is crossed
                // Debug.Log($"[ClientSpiritController] Spirit {gameObject.name} (Role: {_owningPlayerRole}) hit boundary X=0. Returning to pool.");
                if (_normalLifetimeCoroutine != null) 
                {
                    StopCoroutine(_normalLifetimeCoroutine);
                    _normalLifetimeCoroutine = null;
                }
                if (_spiritHealth != null) _spiritHealth.ForceReturnToPool();
                else if (ClientGameObjectPool.Instance != null) ClientGameObjectPool.Instance.ReturnObject(gameObject);
                return; // Exit Update early after despawning
            }
            else
            {
                // Apply original movement if boundary not crossed
                _cachedTransform.Translate(desiredMovement);
            }

            // --- Off-screen check (after movement) ---
            if (!_isActivated) // Off-screen check for non-activated spirits
            {
                Vector3 pos = _cachedTransform.position;
                if (pos.y < offScreenLimitBottom || pos.y > offScreenLimitTop || 
                    pos.x < -offScreenLimitHorizontal || pos.x > offScreenLimitHorizontal)
                {
                    // Debug.Log($"[ClientSpiritController] Normal spirit {gameObject.name} went off-screen. Returning to pool.");
                    if (_normalLifetimeCoroutine != null) 
                    {
                        StopCoroutine(_normalLifetimeCoroutine);
                        _normalLifetimeCoroutine = null;
                    }
                    if (_spiritHealth != null) _spiritHealth.ForceReturnToPool();
                    else if (ClientGameObjectPool.Instance != null) ClientGameObjectPool.Instance.ReturnObject(gameObject);
                    return; 
                }
            }
        }

        public void ActivateSpirit()
        {
            if (_isActivated) return;

            if (_normalLifetimeCoroutine != null) // Stop normal lifetime if activated
            {
                StopCoroutine(_normalLifetimeCoroutine);
                _normalLifetimeCoroutine = null;
            }

            _isActivated = true;
            _currentSpeed = activatedInitialUpwardSpeed; // Set to initial upward speed
            _currentDirection = Vector2.up;
            Debug.Log("[ClientSpiritController] Spirit Activated! Moving up with initial speed: " + _currentSpeed, this);

            if (normalSpiritVisual != null) normalSpiritVisual.SetActive(false);
            if (activatedSpiritVisual != null) activatedSpiritVisual.SetActive(true);

            // Notify Health component to reduce HP
            if (_spiritHealth != null) _spiritHealth.OnActivated();

            // Notify TimeoutAttack to start its timer
            if (_timeoutAttack != null)
            {
                // Pass the NetworkObjectId if the timeout attack also needs to target.
                // This assumes ClientSpiritTimeoutAttack is updated to handle NetworkObjectId.
                // If it still expects ClientId, this needs further refactoring there.
                // For now, let's assume it will be updated or can work with 0 if not targeting directly.
                ulong timeoutTargetId = _shouldAim ? _targetNetworkObjectId : 0; // Pass the ID if aiming, else 0
                _timeoutAttack.StartTimeout(1.5f, timeoutTargetId); 
            }
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

            if (_normalLifetimeCoroutine != null)
            {
                StopCoroutine(_normalLifetimeCoroutine);
                _normalLifetimeCoroutine = null;
            }
        }

        void OnDisable()
        {
            // When returned to pool, deinitialize
            Deinitialize();
            if (_timeoutAttack != null) _timeoutAttack.StopTimeoutAttack(); // Also ensure timeout is stopped
        }

        /// <summary>
        /// Public method to allow external systems (like the spellcard clear RPC)
        /// to force this spirit back to the pool.
        /// Stops the lifetime coroutine and delegates to ClientSpiritHealth.
        /// </summary>
        public void ForceReturnToPool()
        {
            if (!_isInitialized) return; // Don't do anything if not initialized

            // Stop lifetime coroutine if running
            if (_normalLifetimeCoroutine != null) 
            {
                StopCoroutine(_normalLifetimeCoroutine);
                _normalLifetimeCoroutine = null;
            }
            
            // Stop timeout attack coroutine if running
            if (_timeoutAttack != null) 
            {
                _timeoutAttack.StopTimeoutAttack();
            }

            // Delegate the actual return to the health component
            if (_spiritHealth != null) 
            {
                _spiritHealth.ForceReturnToPool();
            }
            else 
            {
                // Fallback if health component is missing for some reason
                Debug.LogWarning($"[ClientSpiritController] ForceReturnToPool called on {gameObject.name}, but _spiritHealth is null. Returning via ClientGameObjectPool directly.");
                if (ClientGameObjectPool.Instance != null) 
                {
                    ClientGameObjectPool.Instance.ReturnObject(gameObject);
                }
                else 
                {
                    Destroy(gameObject); // Ultimate fallback
                }
            }

            // Ensure controller state is reset for potential reuse (though health component handles GO disable)
            Deinitialize(); 
        }

        public PlayerRole OwningPlayerRole => _owningPlayerRole;
    }
// } 