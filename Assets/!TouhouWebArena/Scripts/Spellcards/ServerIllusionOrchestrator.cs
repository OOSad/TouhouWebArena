using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TouhouWebArena.Spellcards;

/// <summary>
/// Server-side component responsible for managing the lifecycle, movement, and attack patterns of a Level 4 spellcard's illusion.
/// It orchestrates the illusion's behavior, including idle movement, random attack selection from a pool,
/// attack-specific movements, and aiming towards the target player.
/// This component communicates with the corresponding ClientIllusionView to update clients.
/// It also handles despawning the illusion due to lifetime expiration or a death report from the client-side IllusionHealth component.
/// </summary>
public class ServerIllusionOrchestrator : NetworkBehaviour
{
    private Level4SpellcardData _spellcardData;
    private ulong _targetPlayerId;

    /// <summary>
    /// Gets the NetworkObjectId of the player this illusion is currently targeting.
    /// Used by ServerAttackSpawner to implement the illusion despawn counter-mechanic.
    /// </summary>
    public ulong TargetPlayerId => _targetPlayerId;
    // private int _currentAttackPoolIndex = 0; // Kept for reference, but random selection is now used.
    private float _illusionLifetimeTimer;
    private bool _isInitialized = false;

    private ClientIllusionView _clientView; // Reference to call ClientRPCs for visual updates and attack execution.
    private IllusionHealth _illusionHealthComponent; // Reference to the illusion's health component.

    // Fields for Idle Movement
    private float _nextIdleMoveTimer;
    private float _initialSpawnY; // Y-coordinate where the illusion was spawned, used as a baseline for vertical movement.
    private const float HORIZONTAL_BOUND_X = 7.0f; // Defines the horizontal extent of the play area from the center.
    private float _minXBound; // Minimum X-coordinate for illusion movement, adjusted by BOUNDARY_PADDING.
    private float _maxXBound; // Maximum X-coordinate for illusion movement, adjusted by BOUNDARY_PADDING.
    private float _effectiveMinY; // Minimum Y-coordinate for illusion movement, considering MovementAreaHeight and BOUNDARY_PADDING.
    private float _effectiveMaxY; // Maximum Y-coordinate for illusion movement, considering spawn position and BOUNDARY_PADDING.

    private const float BOUNDARY_PADDING = 1.0f; // Padding from screen edges to prevent illusions from moving too close to boundaries.

    /// <summary>
    /// Called when the network object is spawned. Ensures this script only runs on the server
    /// and caches necessary components like ClientIllusionView and IllusionHealth.
    /// Disables the component if essential dependencies are missing.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }
        _clientView = GetComponent<ClientIllusionView>();
        if (_clientView == null)
        {
            Debug.LogError("ServerIllusionOrchestrator requires a ClientIllusionView component on the same GameObject.");
            enabled = false;
            // Don't return yet, try to get IllusionHealth
        }

        _illusionHealthComponent = GetComponent<IllusionHealth>();
        if (_illusionHealthComponent == null)
        {
            Debug.LogError("ServerIllusionOrchestrator requires an IllusionHealth component on the same GameObject.");
            // Consider disabling if this is critical, though IllusionHealth primarily reports to this.
            enabled = false; 
        }
    }

    /// <summary>
    /// Initializes the illusion with its spellcard data, target player, and movement boundaries.
    /// This method must be called by the server after instantiating the illusion.
    /// It loads the Level4SpellcardData, sets up timers, calculates movement bounds based on spawn position and padding,
    /// and triggers the initial ClientRPCs to synchronize the illusion's state with clients.
    /// </summary>
    /// <param name="spellcardDataPath">The Resources path to the Level4SpellcardData ScriptableObject.</param>
    /// <param name="targetPlayerId">The NetworkObjectId of the player this illusion should target.</param>
    public void Initialize(string spellcardDataPath, ulong targetPlayerId)
    {
        if (!IsServer) return;

        _spellcardData = Resources.Load<Level4SpellcardData>(spellcardDataPath);
        if (_spellcardData == null)
        {
            Debug.LogError($"[ServerIllusionOrchestrator] Failed to load Level4SpellcardData from path: {spellcardDataPath}");
            NetworkObject.Despawn();
            return;
        }
        _targetPlayerId = targetPlayerId;
        _illusionLifetimeTimer = _spellcardData.Duration;

        _initialSpawnY = transform.position.y; // Capture Y at spawn for movement calculations
        // Determine horizontal bounds based on spawn position (assuming symmetrical playfield)
        if (transform.position.x > 0) // Illusion is on the right side (Player 2's typical side)
        {
            _minXBound = 0 + BOUNDARY_PADDING;
            _maxXBound = HORIZONTAL_BOUND_X - BOUNDARY_PADDING;
        }
        else // Illusion is on the left side (Player 1's typical side)
        {
            _minXBound = -HORIZONTAL_BOUND_X + BOUNDARY_PADDING;
            _maxXBound = 0 - BOUNDARY_PADDING;
        }
        // Ensure min is less than max, can be adjusted if SpawnAreaManager provides these directly
        if (_minXBound >= _maxXBound) { Debug.LogWarning($"[ServerIllusionOrchestrator] Adjusted minXBound ({_minXBound}) is >= maxXBound ({_maxXBound}). Horizontal movement space might be zero or negative. Check HORIZONTAL_BOUND_X ({HORIZONTAL_BOUND_X}) and BOUNDARY_PADDING ({BOUNDARY_PADDING}). Reverting to minimal padding for X."); }

        // Calculate effective Y bounds
        _effectiveMinY = _initialSpawnY - _spellcardData.MovementAreaHeight + BOUNDARY_PADDING;
        _effectiveMaxY = _initialSpawnY - BOUNDARY_PADDING; // Padding down from the spawn line

        if (_effectiveMinY >= _effectiveMaxY)
        {
            Debug.LogWarning($"[ServerIllusionOrchestrator] Adjusted _effectiveMinY ({_effectiveMinY}) is >= _effectiveMaxY ({_effectiveMaxY}). Vertical movement space might be zero or negative. Check MovementAreaHeight ({_spellcardData.MovementAreaHeight}) and BOUNDARY_PADDING ({BOUNDARY_PADDING}). Reverting to minimal padding for Y.");
            _effectiveMaxY = _initialSpawnY - 0.1f; 
            _effectiveMinY = _initialSpawnY - _spellcardData.MovementAreaHeight + 0.1f; 
            if (_effectiveMinY >= _effectiveMaxY) 
            {
                 _effectiveMinY = _effectiveMaxY - 0.1f; 
            }
        }

        // Initialize client view
        if (_clientView != null)
        {
            _clientView.InitializeClientRpc(spellcardDataPath, targetPlayerId, _spellcardData.Health);
            // Send the initial transform to clients right after initialization
            _clientView.UpdateIllusionTransformClientRpc(transform.position, transform.rotation);
        }
        
        _isInitialized = true;
        ScheduleNextIdleMove(); // Start timer for first idle move
    }

    void Update()
    {
        if (!IsServer || !_isInitialized || _spellcardData == null) return;

        // Handle illusion lifetime
        _illusionLifetimeTimer -= Time.deltaTime;
        if (_illusionLifetimeTimer <= 0f)
        {
            DespawnIllusion();
            return;
        }

        // Handle idle movement and attack scheduling
        _nextIdleMoveTimer -= Time.deltaTime;
        if (_nextIdleMoveTimer <= 0f)
        {
            PerformIdleMoveAndAttack();
            ScheduleNextIdleMove();
        }
    }

    /// <summary>
    /// Schedules the next idle movement and attack sequence based on MinMoveDelay and MaxMoveDelay from spellcard data.
    /// </summary>
    private void ScheduleNextIdleMove()
    {
        if (_spellcardData == null) return;
        _nextIdleMoveTimer = Random.Range(_spellcardData.MinMoveDelay, _spellcardData.MaxMoveDelay);
    }

    /// <summary>
    /// Core logic for an illusion's action cycle:
    /// 1. Performs an "idle move" to a new random position within its defined, padded boundaries.
    ///    The new position is immediately RPCed to clients via UpdateIllusionTransformClientRpc.
    /// 2. Selects a number of attack patterns randomly from the AttackPool (defined by AttacksPerMove).
    /// 3. For each selected pattern:
    ///    a. Determines if the pattern involves "movement during attack".
    ///       - If so, calculates the start and end positions for this movement and its duration.
    ///         The server starts a coroutine (DelayedUpdateServerPosition) to update its own logical
    ///         position to the movementEndPos after movementDur. The client handles the visual lerp.
    ///    b. Determines if the pattern should be "oriented towards the target".
    ///       - If so, calculates the rotation needed to aim at the target player. This orientation
    ///         is based on the illusion's position *before* any attack-specific movement (if applicable for aiming).
    ///    c. Generates a shared random offset if the first action in the pattern requires it.
    ///    d. Calls ExecuteAttackPatternClientRpc on the ClientIllusionView, passing all necessary parameters
    ///       (pattern index, movement details, orientation, random offset) for the client to execute the attack.
    /// </summary>
    private void PerformIdleMoveAndAttack()
    {
        if (_spellcardData == null || _clientView == null || _spellcardData.AttackPool == null || _spellcardData.AttackPool.Count == 0)
        {
            Debug.LogWarning("[ServerIllusionOrchestrator] Cannot perform idle move/attack: SpellcardData, ClientView, or AttackPool is invalid.");
            return;
        }

        // 1. Perform Idle Move: Teleport to a new random location within bounds.
        float targetX = Random.Range(_minXBound, _maxXBound);
        float targetY = Random.Range(_effectiveMinY, _effectiveMaxY);
        Vector3 newPositionAfterIdleMove = new Vector3(targetX, targetY, transform.position.z);
        transform.position = newPositionAfterIdleMove; // Server updates its position
        _clientView.UpdateIllusionTransformClientRpc(newPositionAfterIdleMove, transform.rotation); // Notify clients of the new position

        // 2. Execute Attacks from the new position
        int patternsToExecuteThisCycle = _spellcardData.AttacksPerMove;
        if (patternsToExecuteThisCycle <= 0) patternsToExecuteThisCycle = 1;

        for (int i = 0; i < patternsToExecuteThisCycle; i++)
        {
            if (_spellcardData.AttackPool.Count == 0) break;

            // Randomly select an attack pattern from the pool
            int randomPatternIndex = Random.Range(0, _spellcardData.AttackPool.Count);
            CompositeAttackPattern currentPattern = _spellcardData.AttackPool[randomPatternIndex];
            int patternDataIndexForClient = randomPatternIndex; // Index to send to client

            if (currentPattern == null) continue;

            // Position before this specific attack's potential movement (i.e., after the idle move)
            Vector3 positionBeforeAttackSpecificMove = transform.position; 
            // Debug.Log($"[ServerIllusionOrchestrator] Position BEFORE attack-specific move for pattern '{currentPattern.patternName}' (AttackMovementVector: {currentPattern.attackMovementVector}): {positionBeforeAttackSpecificMove}, Current Rotation: {transform.eulerAngles.z}");

            // Default orientation to the illusion's current visual rotation. Will be overridden if orientPatternTowardsTarget is true.
            Quaternion attackPatternOrientation = transform.rotation; 
            
            bool isMovingForThisAttack = false;
            Vector3 movementStartPos = positionBeforeAttackSpecificMove; // Default start for movement is current position
            Vector3 movementEndPos = positionBeforeAttackSpecificMove;   // Default end for movement is current position
            float movementDur = 0f;

            // Check if this pattern involves movement during the attack
            if (currentPattern.performMovementDuringAttack)
            {
                isMovingForThisAttack = true;
                // movementStartPos is already set to positionBeforeAttackSpecificMove

                // Calculate movement relative to illusion's orientation (which is generally upright)
                Vector3 localMovement = new Vector3(currentPattern.attackMovementVector.x, currentPattern.attackMovementVector.y, 0);
                // Note: transform.rotation here is the illusion's visual rotation, which might not be its facing for movement direction.
                // If illusion sprite is always upright, worldMovement should likely be based on a fixed frame or explicit orientation parameter.
                // However, current AttackMovementVector is designed as world-offset or relative to current orientation.
                // For simplicity, let's assume attackMovementVector is intended to be added to current position, potentially modified by illusion's orientation if needed.
                // The client receives start/end and handles lerp, so server calculates the destination.
                Vector3 worldMovementDelta = transform.rotation * localMovement; // If localMovement is relative to illusion's facing.
                                                                              // If attackMovementVector is already a world-space offset from current pos, then:
                                                                              // Vector3 worldMovementDelta = localMovement;

                Vector3 intendedNewPosition = positionBeforeAttackSpecificMove + worldMovementDelta;

                // Clamp the movement destination to within the illusion's allowed bounds
                float clampedX = Mathf.Clamp(intendedNewPosition.x, _minXBound, _maxXBound);
                float clampedY = Mathf.Clamp(intendedNewPosition.y, _effectiveMinY, _effectiveMaxY);
                
                movementEndPos = new Vector3(clampedX, clampedY, intendedNewPosition.z);
                movementDur = currentPattern.attackMovementDuration;

                // The server does NOT move its transform.position immediately here.
                // It schedules DelayedUpdateServerPosition to update its logical position after movementDur.
                // The attackOriginPosition for aiming (if any) and RPC remains based on positionBeforeAttackSpecificMove.
                // The client will visually lerp the illusion and spawn bullets from the moving transform.
                // Debug.Log($"[ServerIllusionOrchestrator] Illusion will perform attack-specific move for '{currentPattern.patternName}' from {movementStartPos} to {movementEndPos} over {movementDur}s.");
            }

            // Check if this pattern needs to be oriented towards the target player
            if (currentPattern.orientPatternTowardsTarget)
            {
                if (NetworkManager.Singleton != null && 
                    NetworkManager.Singleton.ConnectedClients.TryGetValue(_targetPlayerId, out NetworkClient targetClient) && 
                    targetClient.PlayerObject != null)
                {
                    // Aiming calculations should be based on the illusion's position *before* this specific attack's movement starts (if any),
                    // or its current position if it's not moving for this attack.
                    Vector3 aimingOrigin = positionBeforeAttackSpecificMove; // If not moving, or if aiming is decided before movement starts.
                                                                        // If moving, and aiming should reflect the start of that move, movementStartPos is the same.
                    
                    Vector3 targetPlayerPos = targetClient.PlayerObject.transform.position;
                    Vector3 directionToTarget = (targetPlayerPos - aimingOrigin).normalized;
                    if (directionToTarget != Vector3.zero) 
                    {
                        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
                        angle -= 90f; // Adjustment assuming patterns default to "upwards" (Y-axis)
                        attackPatternOrientation = Quaternion.Euler(0, 0, angle);
                        // Debug.Log($"[ServerIllusionOrchestrator] Orienting pattern '{currentPattern.patternName}' towards target. Adjusted Angle: {angle}");
                    }
                }
            }

            // Generate a shared random offset for all actions in this pattern if specified by the first action.
            Vector2 sharedRandomOffset = Vector2.zero;
            if (currentPattern.actions.Count > 0 && currentPattern.actions[0].applyRandomSpawnOffset)
            {
                SpellcardAction firstAction = currentPattern.actions[0];
                sharedRandomOffset = new Vector2(
                    Random.Range(firstAction.randomOffsetMin.x, firstAction.randomOffsetMax.x),
                    Random.Range(firstAction.randomOffsetMin.y, firstAction.randomOffsetMax.y)
                );
            }
            
            _clientView.ExecuteAttackPatternClientRpc(patternDataIndexForClient, 
                                                    isMovingForThisAttack, 
                                                    movementStartPos, 
                                                    movementEndPos, 
                                                    movementDur, 
                                                    attackPatternOrientation, // This is the staticRotationIfNotMoving argument
                                                    sharedRandomOffset);
                                                    
            Debug.Log($"[ServerIllusionOrchestrator] Triggered attack pattern '{currentPattern.patternName}' (index {patternDataIndexForClient}). IsMoving: {isMovingForThisAttack}");

            // If the server moved the illusion for this attack, its actual transform.position needs to be updated 
            // for the *next* cycle of PerformIdleMoveAndAttack. This should happen after movementDur.
            if (isMovingForThisAttack && movementEndPos != transform.position) 
            { // Only start coroutine if there is a move and a duration
                 StartCoroutine(DelayedUpdateServerPosition(movementEndPos, movementDur));
            }
        }
    }

    /// <summary>
    /// Coroutine to update the server's logical position for the illusion after an attack-specific movement has visually completed on the client.
    /// This ensures subsequent actions or idle moves are calculated from the correct new position.
    /// </summary>
    /// <param name="newPosition">The position the illusion should be at after the movement.</param>
    /// <param name="delay">The duration of the movement, used as the delay before updating the server's position.</param>
    private System.Collections.IEnumerator DelayedUpdateServerPosition(Vector3 newPosition, float delay)
    {
        if (delay > 0) 
        {
            yield return new WaitForSeconds(delay);
        }
        transform.position = newPosition;
        // Debug.Log($"[ServerIllusionOrchestrator] Server-side illusion position updated to {newPosition} after attack movement.");
    }

    // ProceedToNextPattern() is effectively integrated into PerformIdleMoveAndAttack()
    // private void ProceedToNextPattern() { ... } // Removed

    /// <summary>
    /// Processes a death report received from the client-side IllusionHealth component (via its ServerRpc).
    /// This method is called directly by IllusionHealth on the server (it is NOT an RPC itself).
    /// It verifies that the report came from the client targeted by this illusion.
    /// If the report is valid, it calls DespawnIllusion().
    /// </summary>
    /// <param name="rpcParams">ServerRpcParams containing the SenderClientId of the client who reported the death.</param>
    public void ProcessClientDeathReport(ServerRpcParams rpcParams) 
    {
        // Debug.Log($"[ServerIllusionOrchestrator {NetworkObjectId}] ProcessClientDeathReport called. Sender: {rpcParams.Receive.SenderClientId}, Expected Target: {_targetPlayerId}"); 
        if (!IsServer) return; 

        if (rpcParams.Receive.SenderClientId != _targetPlayerId)
        {
            Debug.LogWarning($"[ServerIllusionOrchestrator {NetworkObjectId}] Illusion ({NetworkObjectId}) death report from unauthorized client {rpcParams.Receive.SenderClientId}. Expected {_targetPlayerId}. Ignoring.");
            return;
        }

        // Debug.Log($"[ServerIllusionOrchestrator {NetworkObjectId}] Illusion ({NetworkObjectId}) death reported by targeted client {rpcParams.Receive.SenderClientId}. Despawning.");
        DespawnIllusion();
    }

    /// <summary>
    /// Despawns the illusion network object.
    /// Notifies the ServerIllusionManager before despawning.
    /// This can be called due to lifetime expiry, client-reported death, or by external systems (e.g., counter-mechanic).
    /// </summary>
    public void DespawnIllusion()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            // Debug.Log($"[ServerIllusionOrchestrator {NetworkObjectId}] Despawning illusion {NetworkObject.NetworkObjectId}");
            
            if (ServerIllusionManager.Instance != null)
            {
                ServerIllusionManager.Instance.ServerNotifyIllusionDespawned(NetworkObject);
            }
            else
            {
                Debug.LogWarning($"[ServerIllusionOrchestrator] ServerIllusionManager.Instance is null. Cannot notify of despawn for illusion {NetworkObject.NetworkObjectId}");
            }
            // Debug.Log($"[ServerIllusionOrchestrator {NetworkObjectId}] Conditions met for despawn. Calling NetworkObject.Despawn(). IsSpawned: {NetworkObject.IsSpawned}");
            NetworkObject.Despawn(); 
        }
    }

    /// <summary>
    /// Called when the network object is despawned. Used for any necessary cleanup.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        // Cleanup if necessary
    }
}

// Dummy definitions for Level4SpellcardData, CompositeAttackPattern, SpellcardAction
// Replace with your actual class definitions and namespaces

// [CreateAssetMenu(fileName = "NewLevel4SpellcardData", menuName = "TouhouWebArena/Spellcard Data/Level 4 Spellcard")]
// public class Level4SpellcardData : ScriptableObject
// {
//     public GameObject illusionPrefab;
//     public float illusionTotalDuration = 60f; // Example: Total time illusion stays active
//     public List<CompositeAttackPattern> attackPatterns;
// }

// [System.Serializable]
// public class CompositeAttackPattern
// {
//     public string patternName;
//     public bool orientPatternTowardsTarget;
//     public bool performMovementDuringAttack;
//     public float patternDuration = 10f; // Example: How long this pattern phase lasts
//     public bool endsSpellcard = false; // If true, spellcard ends after this pattern
//     public List<SpellcardAction> actions;
//     // Add movement parameters if performMovementDuringAttack is true
// }

// [System.Serializable]
// public class SpellcardAction // Ensure this matches your existing SpellcardAction
// {
//     public bool applyRandomSpawnOffset;
//     public Vector2 randomOffsetMin;
//     public Vector2 randomOffsetMax;
//     // ... other fields from your SpellcardAction definition
// } 