using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic; // For List
using System.Linq; // For FirstOrDefault, LastOrDefault, Max
using Unity.Netcode.Components; // For NetworkTransform

namespace TouhouWebArena.Spellcards
{
    /// <summary>
    /// **[Server Only Logic]** Controls the behavior of the Level 4 illusion prefab.
    /// Handles random movement within the opponent's upper playfield bounds and triggers
    /// the execution of CompositeAttackPatterns from its associated Level4SpellcardData.
    /// Requires NetworkObject and NetworkTransform components on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    public class Level4IllusionController : NetworkBehaviour
    {
        public NetworkVariable<PlayerRole> TargetPlayerRole = new NetworkVariable<PlayerRole>(
            PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Level4SpellcardData _spellData;
        private ulong _targetPlayerClientId = ulong.MaxValue;
        private Rect _movementBounds;
        private Coroutine _mainMovementLoopCoroutine;
        private bool _isExecutingAttack = false; // Flag to pause random movement

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                // Delay initialization slightly to ensure PlayerDataManager is ready
                StartCoroutine(InitializeAndStartBehavior());
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && _mainMovementLoopCoroutine != null)
            {
                StopCoroutine(_mainMovementLoopCoroutine);
                _mainMovementLoopCoroutine = null;
            }
            base.OnNetworkDespawn();
        }

        private IEnumerator InitializeAndStartBehavior()
        {
            // Wait until essential data is available
            yield return new WaitUntil(() => TargetPlayerRole.Value != PlayerRole.None && _spellData != null && PlayerDataManager.Instance != null && NetworkManager.Singleton != null);

            // Find Target Client ID based on Role
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    PlayerData? data = PlayerDataManager.Instance.GetPlayerData(client.ClientId);
                    if (data.HasValue && data.Value.Role == TargetPlayerRole.Value)
                    {
                        _targetPlayerClientId = client.ClientId;
                        break;
                    }
                }

            if (_targetPlayerClientId == ulong.MaxValue)
            {
                Debug.LogError($"[Level4IllusionController] Could not find Client ID for Target Role {TargetPlayerRole.Value}! Attacks cannot be targeted.", gameObject);
                yield break; // Stop if no target
            }

            CalculateMovementBounds();
            // Start the main behavior loop
            _mainMovementLoopCoroutine = StartCoroutine(ServerBehaviorLoop());
        }

        private void CalculateMovementBounds()
        {
            if (!IsServer || _spellData == null) return;

            Rect baseBounds = (TargetPlayerRole.Value == PlayerRole.Player1)
                              ? PlayerMovement.player1Bounds
                              : PlayerMovement.player2Bounds;

            // Clamp movement area height to prevent invalid rects
            float actualMovementHeight = Mathf.Min(_spellData.MovementAreaHeight, baseBounds.height);

            float topY = baseBounds.yMax;
            float bottomY = baseBounds.yMax - actualMovementHeight;
            float leftX = baseBounds.xMin;
            float rightX = baseBounds.xMax;

            _movementBounds = new Rect(leftX, bottomY, rightX - leftX, topY - bottomY);

            // Ensure starting position is within bounds
            transform.position = ClampPositionToBounds(transform.position);
            if (_movementBounds.width <= 0 || _movementBounds.height <= 0)
            {
                 Debug.LogWarning($"[Level4IllusionController] Calculated movement bounds have zero width or height for {TargetPlayerRole.Value}. Clamping position only.", gameObject);
            }
        }

        private IEnumerator ServerBehaviorLoop()
        {
            if (!IsServer || _spellData == null) yield break;
            // Initial wait before first move/attack
            yield return new WaitForSeconds(Random.Range(_spellData.MinMoveDelay, _spellData.MaxMoveDelay));

            while (true)
            {
                 if (_isExecutingAttack) // Wait if an attack is already in progress
                 {
                     yield return null;
                     continue;
                 }

                // 1. Move to a new random position
                Vector3 targetPosition = CalculateRandomTargetPosition();
                // TODO: Implement smooth movement over time instead of instant teleport
                transform.position = targetPosition;

                // 2. Execute Attacks
                if (_spellData.AttackPool != null && _spellData.AttackPool.Count > 0 && _spellData.AttacksPerMove > 0)
                {
                     _isExecutingAttack = true;
                     yield return StartCoroutine(ServerExecuteAttackSequence(targetPosition));
                     _isExecutingAttack = false;
                }

                // 3. Wait for the next cycle
                float delay = Random.Range(_spellData.MinMoveDelay, _spellData.MaxMoveDelay);
                yield return new WaitForSeconds(delay);
            }
        }

        private Vector3 CalculateRandomTargetPosition()
        {
            if (_movementBounds.width <= 0 || _movementBounds.height <= 0)
            {
                // Fallback if bounds are invalid, stay near top center
                Rect baseBounds = (TargetPlayerRole.Value == PlayerRole.Player1) ? PlayerMovement.player1Bounds : PlayerMovement.player2Bounds;
                 return new Vector3(baseBounds.center.x, baseBounds.yMax - 0.5f, transform.position.z);
            }

                float targetX = Random.Range(_movementBounds.xMin, _movementBounds.xMax);
                float targetY = Random.Range(_movementBounds.yMin, _movementBounds.yMax);
            return new Vector3(targetX, targetY, transform.position.z);
        }

        // Renamed from TryExecuteAttacks for clarity
        private IEnumerator ServerExecuteAttackSequence(Vector3 illusionPositionAtAttackStart)
        {
            if (!IsServer) yield break;
            if (_spellData == null || _spellData.AttackPool == null || _spellData.AttackPool.Count == 0 || _spellData.AttacksPerMove <= 0) yield break;
            if (_targetPlayerClientId == ulong.MaxValue || ServerAttackSpawner.Instance == null) yield break;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(_targetPlayerClientId, out NetworkClient targetClient) || targetClient.PlayerObject == null) yield break;

            // Capture target state at the beginning of the sequence
            Vector3 targetPosition = targetClient.PlayerObject.transform.position;
            bool isTargetOnPositiveSide = (TargetPlayerRole.Value == PlayerRole.Player2);

            // Select patterns to execute for this sequence
            List<CompositeAttackPattern> selectedPatterns = SelectAttackPatterns();

            // Execute selected patterns sequentially
            foreach (CompositeAttackPattern pattern in selectedPatterns)
            {
                // Wait for the pattern (and its potential movement) to complete
                yield return StartCoroutine(ServerExecuteSingleCompositePattern(pattern, illusionPositionAtAttackStart, targetPosition, isTargetOnPositiveSide));
                // Update illusion position if the pattern involved movement, for the next pattern (if any)
                illusionPositionAtAttackStart = transform.position;
            }
        }

        private List<CompositeAttackPattern> SelectAttackPatterns()
        {
            List<CompositeAttackPattern> selectedPatterns = new List<CompositeAttackPattern>();
            List<int> availableIndices = Enumerable.Range(0, _spellData.AttackPool.Count).ToList();
            int patternsToSelect = Mathf.Min(_spellData.AttacksPerMove, _spellData.AttackPool.Count);

            for (int i = 0; i < patternsToSelect; i++)
            {
                if (availableIndices.Count == 0) break;
                int randomIndex = Random.Range(0, availableIndices.Count);
                int selectedPoolIndex = availableIndices[randomIndex];
                selectedPatterns.Add(_spellData.AttackPool[selectedPoolIndex]);
                availableIndices.RemoveAt(randomIndex);
            }
            return selectedPatterns;
        }

        // Renamed from ServerExecuteCompositePattern
        private IEnumerator ServerExecuteSingleCompositePattern(CompositeAttackPattern pattern, Vector3 originPosition, Vector3 capturedTargetPosition, bool isTargetOnPositiveSide)
        {
            if (!IsServer || pattern == null || pattern.actions == null) yield break;
            if (_targetPlayerClientId == ulong.MaxValue || ServerAttackSpawner.Instance == null) yield break;

            // --- Handle Movement During Attack --- 
            Coroutine attackMovementCoroutine = null;
            float actualAttackDuration = 0f;
            if (pattern.performMovementDuringAttack && pattern.attackMovementDuration > 0)
            {
                // --- Determine Dynamic Direction ---
                float boundsCenterX = _movementBounds.center.x;
                float currentX = originPosition.x;
                float horizontalDirection = (currentX < boundsCenterX) ? 1f : -1f; // Move right if left of center, left if right of center
                
                // Use magnitude of X from pattern data for distance, apply dynamic direction.
                // Assume horizontal movement only for now.
                float moveDistance = Mathf.Abs(pattern.attackMovementVector.x);
                Vector2 dynamicMoveVector = new Vector2(horizontalDirection * moveDistance, 0f);
                // --------------------------------

                attackMovementCoroutine = StartCoroutine(ServerPerformAttackMovement(originPosition, dynamicMoveVector, pattern.attackMovementDuration));
                actualAttackDuration = pattern.attackMovementDuration;
            }
            // ------------------------------------

            Quaternion patternRotation = Quaternion.identity;
            if (pattern.orientPatternTowardsTarget)
            {
                Vector2 directionToTarget = capturedTargetPosition - originPosition;
                if (directionToTarget != Vector2.zero)
                {
                    float angleToTarget = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
                    // Adjust angle if needed based on sprite orientation (e.g., -90 for up)
                    patternRotation = Quaternion.Euler(0, 0, angleToTarget - 90f); // Assuming -90 aims correctly
                }
            }

            // --- Execute Bullet Actions --- 
            float lastActionEndTime = 0f;
            foreach (SpellcardAction action in pattern.actions)
            {
                 // Calculate the time this action is supposed to start
                float actionStartTime = action.startDelay;

                 // Wait until it's time for this action to start
                float waitTime = actionStartTime - lastActionEndTime;
                if (waitTime > 0.001f) // Add tolerance for float comparisons
                {
                    yield return new WaitForSeconds(waitTime);
                    lastActionEndTime = actionStartTime;
                }
                // If waitTime is <= 0, it means this action starts immediately after the previous one ended (or at the same time)

                // Check if object is still valid before spawning
                if (this == null || !this.gameObject.activeInHierarchy || !NetworkObject.IsSpawned) yield break;

                // Use the *current* illusion position for spawning if movement is happening
                Vector3 currentOrigin = pattern.performMovementDuringAttack ? transform.position : originPosition;

                // Start the action coroutine using the ServerSpellcardActionRunner instance
                if (ServerSpellcardActionRunner.Instance != null)
                {
                    StartCoroutine(ServerSpellcardActionRunner.Instance.ExecuteSingleSpellcardActionFromServerCoroutine(
                    action,
                        transform, // Use live transform of the illusion
                    patternRotation,
                    _targetPlayerClientId,
                    capturedTargetPosition,
                    isTargetOnPositiveSide
                ));
                }
                else
                {
                    Debug.LogError("[Level4IllusionController] ServerSpellcardActionRunner instance is null!");
                }

                // Calculate the time this action is expected to END based on its properties
                float actionDuration = (action.count * action.intraActionDelay); // Rough estimate, could be more precise
                lastActionEndTime = Mathf.Max(lastActionEndTime, actionStartTime + actionDuration);

                // IMPORTANT: We DON'T yield return the spawnCoroutine here.
                // This allows the *next* action's startDelay timer to begin immediately
                // while this action is still spawning its bullets over time.
                // The final wait at the end of the composite pattern handles the total duration.
            }
            // ----------------------------

            // --- Wait for Attack Completion --- 
            // Calculate total duration based on the END time of the last action started.
            float actionsDuration = lastActionEndTime;
            // If the pattern involved movement, ensure we wait at least that long.
            float waitDuration = Mathf.Max(actualAttackDuration, actionsDuration);

            // Wait for the remaining time for the entire composite pattern to finish.
            // This ensures movement finishes and all bullets from the last action have spawned.
            float elapsedTimeSincePatternStart = Time.time - (Time.time - lastActionEndTime); // A bit redundant, effectively just lastActionEndTime relative to start?
            // Let's rethink the wait: We need to wait until the calculated waitDuration has passed since the pattern START.
            // We already tracked lastActionEndTime relative to the pattern start.
            float timeAlreadyPassed = lastActionEndTime; // Time until the last bullet of the last action *starts* spawning.
            
            // Wait for the longest required duration (movement or the end of the final action's spawning sequence)
             if (timeAlreadyPassed < waitDuration)
            {
                 yield return new WaitForSeconds(waitDuration - timeAlreadyPassed);
            }

             // Ensure movement coroutine is stopped if it exists (though it should finish naturally)
            if (attackMovementCoroutine != null)
            {
                // Don't stop it here, let it finish naturally based on its duration.
                // StopCoroutine(attackMovementCoroutine);
            }
            // ----------------------------------
        }

        private IEnumerator ServerPerformAttackMovement(Vector3 startPos, Vector2 moveVector, float duration)
        {
            if (duration <= 0) yield break;
            Vector3 endPos = startPos + (Vector3)moveVector;
            float timer = 0f;
            while (timer < duration)
            {
                 // Check if object is still valid
                if (this == null || !this.gameObject.activeInHierarchy || !NetworkObject.IsSpawned) yield break;

                transform.position = Vector3.Lerp(startPos, endPos, timer / duration);
                timer += Time.deltaTime;
                yield return null; // Wait for next frame
            }
            // Ensure final position is set accurately
            transform.position = endPos;
        }


        private Vector3 ClampPositionToBounds(Vector3 position)
        {
            if (_movementBounds.width <= 0 || _movementBounds.height <= 0) return position; // Cannot clamp if bounds are invalid

            return new Vector3(
                Mathf.Clamp(position.x, _movementBounds.xMin, _movementBounds.xMax),
                Mathf.Clamp(position.y, _movementBounds.yMin, _movementBounds.yMax),
                position.z
            );
        }

        // Called by ServerAttackSpawner to inject necessary data
        public void ServerInitialize(PlayerRole role, Level4SpellcardData data)
        {
            if (!IsServer) return;
            TargetPlayerRole.Value = role;
            _spellData = data;
            // Initialization logic moved to InitializeAndStartBehavior coroutine
        }
    }
} 