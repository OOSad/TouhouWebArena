using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic; // Needed for Lists and Dictionaries
using System.Linq; // Needed for Linq operations on buffers
using System.Collections;

/// <summary>
/// Handles server-authoritative movement for player characters.
/// The owning client sends inputs (<see cref="PlayerInputData"/>) to the server each tick.
/// The server processes inputs authoritatively, calculates the true state (<see cref="PlayerStateData"/>),
/// and directly updates the player's transform. Non-owning clients receive updates via <see cref="NetworkTransform"/>.
/// Interacts with <see cref="CharacterStats"/>, <see cref="CharacterAnimation"/>, <see cref="PlayerHealth"/>,
/// <see cref="PlayerFocusController"/>, and optionally <see cref="PlayerAIController"/>.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))] // Keep Rigidbody2D for physics interactions if needed, set to Kinematic
[RequireComponent(typeof(CharacterAnimation))]
// [RequireComponent(typeof(PlayerPositionSynchronizer))] // REMOVED
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(NetworkTransform))] // Ensure NetworkTransform is present
public class PlayerMovement : NetworkBehaviour
{
    /// <summary>
    /// Hardcoded movement bounds for Player 1 (left side of the screen).
    /// Accessed publicly by other scripts (e.g., PlayerDeathBomb).
    /// </summary>
    public static readonly Rect player1Bounds = new Rect(-8f, -4f, 7f, 8f); // Public Hardcoded Left Bounds
    /// <summary>
    /// Hardcoded movement bounds for Player 2 (right side of the screen).
    /// Accessed publicly by other scripts.
    /// </summary>
    public static readonly Rect player2Bounds = new Rect(1f, -4f, 7f, 8f); // Public Hardcoded Right Bounds

    // --- Input and State Structs ---
    /// <summary>Holds input state for a single network tick.</summary>
    private struct PlayerInputData : INetworkSerializable
    {
        public uint Tick;
        public float HorizontalInput;
        public float VerticalInput;
        public bool IsFocusing; // Include focus state

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref HorizontalInput);
            serializer.SerializeValue(ref VerticalInput);
            serializer.SerializeValue(ref IsFocusing);
        }
    }

    /// <summary>Holds the authoritative state calculated by the server for a tick.</summary>
    private struct PlayerStateData : INetworkSerializable
    {
        public uint Tick;
        public Vector3 Position;
        // public Vector2 Velocity; // Optional: Can include velocity for more complex reconciliation

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Position);
            // serializer.SerializeValue(ref Velocity);
        }
    }

    // --- Component References ---
    /// <summary>Reference to the NetworkTransform component for state synchronization (disabled for owner).</summary>
    private NetworkTransform networkTransform;
    /// <summary>Reference to the PlayerDataManager singleton to determine player role and bounds.</summary>
    private PlayerDataManager playerDataManager;
    /// <summary>Reference to the PlayerHealth component to check for invincibility.</summary>
    private PlayerHealth playerHealth;
    /// <summary>Reference to the CharacterAnimation component to update visual state.</summary>
    private CharacterAnimation characterAnimation;
    /// <summary>Reference to the CharacterStats component to get movement speeds.</summary>
    private CharacterStats characterStats;
    /// <summary>Optional reference to the PlayerAIController for AI-driven movement.</summary>
    private PlayerAIController playerAIController;
    /// <summary>Reference to the PlayerFocusController to read focus input.</summary>
    private PlayerFocusController playerFocusController; // Added reference
    /// <summary>TEMP DEBUG: Reference to the SpriteRenderer for visual flash.</summary>
    // private SpriteRenderer _spriteRenderer; // REMOVED

    // --- State Variables ---
    /// <summary>The movement bounds currently applied to this player instance (determined in OnNetworkSpawn).</summary>
    private Rect currentBounds;
    /// <summary>Stores the horizontal movement input provided by the AI controller, if active.</summary>
    private float aiHorizontalInput = 0f; // Keep for AI control logic

    // --- Server Auth State ---
    // private const int INPUT_BUFFER_SIZE = 1024; // How many ticks of input to buffer
    // private const float RECONCILIATION_THRESHOLD = 0.05f; // Max distance difference before snapping

    // Client specific buffers
    // private List<PlayerInputData> _clientInputBuffer = new List<PlayerInputData>(INPUT_BUFFER_SIZE);
    // private List<PlayerStateData> _clientStateBuffer = new List<PlayerStateData>(INPUT_BUFFER_SIZE); // To store predicted states

    // Server specific buffer
    private Dictionary<ulong, Queue<PlayerInputData>> _serverInputQueue = new Dictionary<ulong, Queue<PlayerInputData>>();
    private PlayerStateData _lastProcessedState; // Server stores the last state it processed

    private uint _currentTick = 0; // Local tick counter for input
    private Rigidbody2D _rb; // Reference to Rigidbody2D

    /// <summary>
    /// Gets the movement bounds currently applied to this player instance.
    /// </summary>
    public Rect CurrentBounds => currentBounds;

    /// <summary>
    /// Called once when the script instance is first loaded.
    /// Used to cache required component references reliably before network spawning.
    /// </summary>
    void Awake()
    {
        networkTransform = GetComponent<NetworkTransform>(); // Good practice to get components early
        playerHealth = GetComponent<PlayerHealth>();
        characterAnimation = GetComponent<CharacterAnimation>();
        characterStats = GetComponent<CharacterStats>(); // Get stats component
        playerAIController = GetComponent<PlayerAIController>(); // Get AI controller
        playerFocusController = GetComponent<PlayerFocusController>(); // Get focus controller
        _rb = GetComponent<Rigidbody2D>(); // Get Rigidbody2D

        // Ensure Rigidbody is kinematic if we are manually setting position
        if (_rb != null)
        {
            _rb.isKinematic = true;
        }

        // Error checking
        if (networkTransform == null) Debug.LogError("PlayerMovement: NetworkTransform not found!", this);
        if (playerHealth == null) Debug.LogError("PlayerMovement: PlayerHealth not found!", this);
        if (characterAnimation == null) Debug.LogError("PlayerMovement: CharacterAnimation not found!", this);
        if (characterStats == null) Debug.LogError("PlayerMovement: CharacterStats not found!", this); // Check stats
        if (playerFocusController == null) Debug.LogError("PlayerMovement: PlayerFocusController not found!", this); // Check focus controller
        if (_rb == null) Debug.LogError("PlayerMovement: Rigidbody2D not found!", this);
        // AI Controller is optional, so no error check here

        // TEMP DEBUG: Get SpriteRenderer // REMOVED
        // _spriteRenderer = GetComponent<SpriteRenderer>();
        // if (_spriteRenderer == null) Debug.LogWarning("PlayerMovement: SpriteRenderer not found on root object for reconcile flash!", this);
    }

    void OnEnable()
    {
        // Log when the component becomes enabled
        Debug.Log($"PlayerMovement enabled for {gameObject.name} (IsOwner: {IsOwner})");
    }

    /// <summary>
    /// Called when the NetworkObject associated with this script is spawned across the network.
    /// Fetches the PlayerDataManager instance, disables the NetworkTransform for the owner,
    /// and determines the appropriate movement <see cref="currentBounds"/> based on the player's role.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Components should already be fetched in Awake
        playerDataManager = PlayerDataManager.Instance;

        // Determine which bounds to use based on PlayerRole
        if (playerDataManager != null)
        {
            // Use top-level PlayerData
            PlayerData? myData = playerDataManager.GetPlayerData(OwnerClientId);
            if (myData.HasValue)
            {
                if (myData.Value.Role == PlayerRole.Player1)
                {
                    currentBounds = player1Bounds;
                }
                else if (myData.Value.Role == PlayerRole.Player2)
                {
                    currentBounds = player2Bounds;
                }
                else
                {
                    currentBounds = new Rect(); // Default empty bounds if role is None or unexpected
                    Debug.LogError($"Owner {OwnerClientId} has unexpected Role {myData.Value.Role}. Applying default bounds.");
                }
            }
            else
            {
                 currentBounds = new Rect();
                 Debug.LogError($"Could not retrieve PlayerData for Owner {OwnerClientId}. Applying default bounds.");
            }
        }
        else
        {
             Debug.LogError("PlayerDataManager not found! Cannot determine player bounds.");
             currentBounds = new Rect();
        }

        // --- Ensure NetworkTransform is ENABLED for ALL clients ---
        // Server applies authoritative position, NetworkTransform syncs it to owner and non-owners.
        if (networkTransform != null)
        {
            networkTransform.enabled = true; // Ensure it's enabled
            // We can remove the log or change it if needed
            Debug.Log($"Player {OwnerClientId} OnNetworkSpawn: Ensuring NetworkTransform is enabled."); 
        }
        else { Debug.LogError("Could not verify NetworkTransform state - component reference missing!"); }
        // --------------------------------------------------------

        // Initialize server input queue if this is the server
        if (IsServer)
        {
            _lastProcessedState = new PlayerStateData { Tick = 0, Position = transform.position }; // Initialize server state
        }
    }

    /// <summary>
    /// Called every fixed framerate frame.
    /// Server: Processes queued inputs for each client.
    /// Owner Client: Collects input, predicts movement locally, and sends input to server.
    /// Non-Owner Client: Does nothing directly; relies on NetworkTransform updates.
    /// </summary>
    void FixedUpdate()
    {
        if (IsServer)
        {
            ServerTick();
        }
        else if (IsOwner)
        {
            ClientTick();
        }
        // Non-owners rely purely on NetworkTransform sync driven by the server
    }

    /// <summary>
    /// Called every frame. Used for non-physics related updates like debug input.
    /// </summary>
    void Update()
    {
        // TEMPORARY DEBUG: Check if Update is running
        // Debug.Log($"PlayerMovement Update running for {gameObject.name} (IsOwner: {IsOwner})");

        // Owner specific debug toggles
        if (IsOwner)
        {
            // TEMPORARY DEBUG: Check if IsOwner block is entered
            // Debug.Log($"PlayerMovement IsOwner block entered for {gameObject.name}");
        }
    }

    // --- Server Logic ---

    /// <summary>
    /// [Server Only] Processes inputs for all connected clients for the current tick.
    /// </summary>
    private void ServerTick()
    {
        // Process inputs for each connected client (including host)
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            // Initialize queue if client just connected
            if (!_serverInputQueue.ContainsKey(clientId))
            {
                _serverInputQueue[clientId] = new Queue<PlayerInputData>();
            }

            // Dequeue and process inputs for this client
            while (_serverInputQueue[clientId].Count > 0)
            {
                PlayerInputData input = _serverInputQueue[clientId].Dequeue();

                // Get the NetworkObject for this client
                NetworkObject clientNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
                if (clientNetworkObject != null)
                {
                     PlayerMovement playerMovement = clientNetworkObject.GetComponent<PlayerMovement>();
                    if (playerMovement != null) // Check if the component exists on the NO
                    {
                         // Process movement using the client's PlayerMovement instance on the server
                         PlayerStateData newState = playerMovement.ProcessMovement(input, Time.fixedDeltaTime, playerMovement._lastProcessedState.Position);
                         playerMovement._lastProcessedState = newState; // Update the state on the server's instance
                         // Server directly applies position in ProcessMovement, NetworkTransform syncs to others
                    }
                    else { Debug.LogError($"Server: PlayerMovement component not found on NetworkObject for client {clientId}"); }
                }
                 else { Debug.LogError($"Server: NetworkObject not found for client {clientId}"); }
            }
        }
    }


    /// <summary>
    /// [ServerRpc] Called by the owning client to submit its input for a tick.
    /// The server queues this input for processing in its FixedUpdate loop.
    /// </summary>
    [ServerRpc]
    private void SubmitInputServerRpc(PlayerInputData input, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!_serverInputQueue.ContainsKey(clientId))
        {
            _serverInputQueue[clientId] = new Queue<PlayerInputData>();
        }

        // Basic validation (e.g., prevent excessively high ticks?) - Optional
        // if (input.Tick > _currentTick + 100) { /* Handle potential cheating */ return; }

        _serverInputQueue[clientId].Enqueue(input);
    }

    // --- Client Logic ---

    /// <summary>
    /// [Owner Client Only] Collects input, predicts movement, stores state, and sends input to server.
    /// </summary>
    private void ClientTick()
    {
        // Check invincibility / game state before processing input
        if (playerHealth != null && playerHealth.IsInvincible.Value)
        {
            // Maybe send a 'zero input' state to server? Or just do nothing.
            // Let's do nothing for now, server will process last known input or default.
            return;
        }

        // 1. Collect Input
        float horizontalInput = 0f;
        float verticalInput = 0f;
        bool isFocusing = playerFocusController != null && playerFocusController.IsFocusingNetworked; // CORRECT: Use public getter

        // Check for AI control (if applicable)
        bool isAIControlled = playerAIController != null && playerAIController.IsAIActive();
        if (isAIControlled)
        {
            horizontalInput = aiHorizontalInput; // Use AI input
            verticalInput = 0; // AI currently only controls horizontal
        }
        else
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
        }

        PlayerInputData currentInput = new PlayerInputData
        {
            Tick = _currentTick,
            HorizontalInput = horizontalInput,
            VerticalInput = verticalInput,
            IsFocusing = isFocusing
        };

        // Update Animation based on local input for responsiveness
        if (characterAnimation != null)
        {
            characterAnimation.SetHorizontalInput(currentInput.HorizontalInput);
        }

        // 2. Buffer Input
        //_clientInputBuffer.Add(currentInput);
        // Optional: Prune buffer if it gets too large
        //while (_clientInputBuffer.Count > INPUT_BUFFER_SIZE)
        //{
        //    _clientInputBuffer.RemoveAt(0);
        //}


        // 3. Predict Movement Locally
        // --- Determine start position for prediction ---
        // Vector3 lastPredictedPos = (_clientStateBuffer.Count > 0) ? _clientStateBuffer[_clientStateBuffer.Count - 1].Position : transform.position;
        // ---------------------------------------------
        // PlayerStateData predictedState = ProcessMovement(currentInput, Time.fixedDeltaTime, lastPredictedPos); // Provide start position
        // _clientStateBuffer.Add(predictedState); // Store the predicted state
         // Optional: Prune state buffer
        // while (_clientStateBuffer.Count > INPUT_BUFFER_SIZE)
        // {
        //      _clientStateBuffer.RemoveAt(0);
        // }

        // Apply predicted movement directly to transform for responsiveness
        // transform.position = predictedState.Position; // --- RE-ENABLE IMMEDIATE PREDICTION APPLICATION ---


        // 4. Send Input to Server
        SubmitInputServerRpc(currentInput);

        // 5. Increment Local Tick
        _currentTick++;
    }

    // --- Shared Movement Logic ---

    /// <summary>
    /// [Server Only] Processes movement based on input for a given delta time.
    /// Calculates the new position and applies it directly to the server's transform.
    /// Requires the starting position for the calculation.
    /// </summary>
    /// <param name="input">The input data for the tick.</param>
    /// <param name="deltaTime">The time delta (usually Time.fixedDeltaTime).</param>
    /// <param name="startPosition">The position to start calculating movement from.</param>
    /// <returns>The calculated PlayerStateData (Tick, Position).</returns>
    private PlayerStateData ProcessMovement(PlayerInputData input, float deltaTime, Vector3 startPosition) // Changed: startPosition is now required
    {
        // --- Log speed calculation context ---
        // string context = IsServer ? $"Server (for Client {input.Tick})" : $"Client Prediction Tick {input.Tick}"; // Simple context, Tick isn't directly client ID on server here
        // ------------------------------------

        // Vector3 startPosition = startPositionOverride ?? transform.position; // REMOVED: startPosition is now a required parameter

        // Get current speed based on focus state from input data
        float currentSpeed = characterStats.GetMoveSpeed();
        float focusModifier = 1.0f; // Default if not focusing
        if (input.IsFocusing)
        {
            focusModifier = characterStats.GetFocusSpeedModifier();
            // currentSpeed *= characterStats.GetFocusSpeedModifier(); // REMOVED - Apply modifier correctly
        }
        currentSpeed *= focusModifier; // Apply modifier AFTER getting base speed

        // --- Detailed Speed Logging ---
        // Debug.Log($"{context}: BaseSpeed={characterStats.GetMoveSpeed():F2}, IsFocusing={input.IsFocusing}, FocusMod={focusModifier:F2} => FinalSpeed={currentSpeed:F4}");
        // -----------------------------

        // Create input vector
        Vector2 currentInputVector = new Vector2(input.HorizontalInput, input.VerticalInput);
        if (currentInputVector.magnitude > 1.0f)
        {
            currentInputVector.Normalize(); // Prevent faster diagonal movement
        }

        // Calculate movement
        Vector3 moveDirection = new Vector3(currentInputVector.x, currentInputVector.y, 0);
        Vector3 moveAmount = moveDirection * currentSpeed * deltaTime;

        // Calculate potential next position based on START position
        Vector3 targetPosition = startPosition + moveAmount;

        // Clamp the position using the correct bounds (server uses client's bounds, client uses its own)
        // Note: On the server, 'currentBounds' might not be set correctly if ProcessMovement is called
        // before OnNetworkSpawn fully establishes the bounds for that specific client's instance.
        // This logic assumes the server calls ProcessMovement on the correct PlayerMovement instance
        // which had its currentBounds set in its own OnNetworkSpawn.
        Rect boundsToUse = currentBounds;
        // If running on server AND processing for a DIFFERENT client, might need to fetch bounds again?
        // For now, assume 'currentBounds' is correct for the instance being processed.
        Vector3 clampedPosition = ClampPositionToBounds(targetPosition, boundsToUse);


        // --- SERVER ONLY: Update transform ---
        // The server *directly* updates the transform position of the corresponding player object.
        // This updated position is what NetworkTransform will synchronize to non-owners.
        if (IsServer)
        {
            // Need to ensure we're updating the correct player's transform on the server
            // The ServerTick loop gets the client's NetworkObject and calls ProcessMovement on *that* instance.
            // So, 'this.transform' inside ProcessMovement when called by ServerTick *is* the correct one.
            transform.position = clampedPosition;
        }
        // --- END SERVER ONLY ---


        // Return the calculated state
        return new PlayerStateData
        {
            Tick = input.Tick,
            Position = clampedPosition
            // Velocity = moveAmount / deltaTime; // Optional
        };
    }

    /// <summary>
    /// Clamps the given position vector within the provided rectangular bounds.
    /// </summary>
    /// <param name="position">The position to clamp.</param>
    /// <param name="bounds">The Rect defining the minimum and maximum X and Y coordinates.</param>
    /// <returns>The clamped position vector.</returns>
    public Vector3 ClampPositionToBounds(Vector3 position, Rect bounds)
    {
        return new Vector3(
            Mathf.Clamp(position.x, bounds.xMin, bounds.xMax),
            Mathf.Clamp(position.y, bounds.yMin, bounds.yMax),
            position.z // Keep original Z position
        );
    }

    /// <summary>
    /// Retrieves the <see cref="PlayerRole"/> (Player1 or Player2) for this player instance.
    /// Relies on the <see cref="PlayerDataManager"/> being available.
    /// </summary>
    /// <returns>The <see cref="PlayerRole"/>, or <see cref="PlayerRole.None"/> if data cannot be retrieved.</returns>
    public PlayerRole GetPlayerRole()
    {
        if (playerDataManager == null)
        {
            // Moved the GetInstance call here
            playerDataManager = PlayerDataManager.Instance;
            if (playerDataManager == null)
            {
                // Still null after trying to get instance
                
                return PlayerRole.None;
            }
        }

        PlayerData? myData = playerDataManager.GetPlayerData(OwnerClientId);
        if (myData.HasValue)
        {
            return myData.Value.Role;
        }
        else
        {
            // Removed the warning log
            return PlayerRole.None;
        }
    }

    // --- AI Control Method ---
    /// <summary>
    /// Allows the <see cref="PlayerAIController"/> (or other scripts) to set the horizontal movement input externally.
    /// The input value is clamped between -1 and 1.
    /// </summary>
    /// <param name="input">Horizontal input value (-1 for left, 1 for right, 0 for none).</param>
    public void SetAIHorizontalInput(float input)
    {
        aiHorizontalInput = Mathf.Clamp(input, -1f, 1f);
    }
    // --------------------------------------------------
} 