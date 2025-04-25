using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic; // For List
using System.Linq; // For FirstOrDefault
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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                StartCoroutine(InitializeAndStartMovement());
            }
        }

        private IEnumerator InitializeAndStartMovement()
        {
            yield return new WaitUntil(() => TargetPlayerRole.Value != PlayerRole.None && _spellData != null);

            // Find Target Client ID
            if (PlayerDataManager.Instance != null && NetworkManager.Singleton != null)
            {
                foreach(var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    PlayerData? data = PlayerDataManager.Instance.GetPlayerData(client.ClientId);
                    if (data.HasValue && data.Value.Role == TargetPlayerRole.Value)
                    {
                        _targetPlayerClientId = client.ClientId;
                        break;
                    }
                }
            }
            if (_targetPlayerClientId == ulong.MaxValue)
            {
                Debug.LogError($"[Level4IllusionController] Could not find Client ID for Target Role {TargetPlayerRole.Value}! Attacks cannot be targeted.", gameObject);
            }

            CalculateMovementBounds();
            StartCoroutine(ServerMoveRandomly());
        }

        private void CalculateMovementBounds()
        {
            if (!IsServer || _spellData == null) return;

            Rect baseBounds = (TargetPlayerRole.Value == PlayerRole.Player1)
                              ? PlayerMovement.player1Bounds
                              : PlayerMovement.player2Bounds;

            float topY = baseBounds.yMax;
            float bottomY = baseBounds.yMax - _spellData.MovementAreaHeight;
            float leftX = baseBounds.xMin;
            float rightX = baseBounds.xMax;

            _movementBounds = new Rect(leftX, bottomY, rightX - leftX, topY - bottomY);
            transform.position = ClampPositionToBounds(transform.position);
        }

        private IEnumerator ServerMoveRandomly()
        {
            if (!IsServer || _spellData == null) yield break;

            while (true)
            {
                float delay = Random.Range(_spellData.MinMoveDelay, _spellData.MaxMoveDelay);
                yield return new WaitForSeconds(delay);

                float targetX = Random.Range(_movementBounds.xMin, _movementBounds.xMax);
                float targetY = Random.Range(_movementBounds.yMin, _movementBounds.yMax);
                Vector3 targetPosition = new Vector3(targetX, targetY, transform.position.z);

                transform.position = targetPosition;
                TryExecuteAttacks(targetPosition);
            }
        }

        private void TryExecuteAttacks(Vector3 currentIllusionPosition)
        {
            if (!IsServer) return;
            if (_spellData == null || _spellData.AttackPool == null || _spellData.AttackPool.Count == 0 || _spellData.AttacksPerMove <= 0) return;
            if (_targetPlayerClientId == ulong.MaxValue) return;
            if (ServerAttackSpawner.Instance == null) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(_targetPlayerClientId, out NetworkClient targetClient) || targetClient.PlayerObject == null) return;
            Vector3 targetPosition = targetClient.PlayerObject.transform.position;
            bool isTargetOnPositiveSide = (TargetPlayerRole.Value == PlayerRole.Player2);

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

            foreach (CompositeAttackPattern pattern in selectedPatterns)
            {
                StartCoroutine(ServerExecuteCompositePattern(pattern, currentIllusionPosition, targetPosition, isTargetOnPositiveSide));
            }
        }

        private IEnumerator ServerExecuteCompositePattern(CompositeAttackPattern pattern, Vector3 originPosition, Vector3 capturedTargetPosition, bool isTargetOnPositiveSide)
        {
            if (!IsServer || pattern == null || pattern.actions == null) yield break;
            if (_targetPlayerClientId == ulong.MaxValue || ServerAttackSpawner.Instance == null) yield break;

            Quaternion patternRotation = Quaternion.identity;
            if (pattern.orientPatternTowardsTarget)
            {
                Vector2 directionToTarget = capturedTargetPosition - originPosition;
                if (directionToTarget != Vector2.zero)
                {
                    float angleToTarget = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
                    patternRotation = Quaternion.Euler(0, 0, angleToTarget);
                }
            }

            foreach (SpellcardAction action in pattern.actions)
            {
                if (action.startDelay > 0.0f)
                {
                    yield return new WaitForSeconds(action.startDelay);
                }
                if (this == null || !this.gameObject.activeInHierarchy || !NetworkObject.IsSpawned) yield break;

                ServerAttackSpawner.Instance.ExecuteSingleSpellcardActionFromServer(
                    action,
                    originPosition,
                    patternRotation,
                    _targetPlayerClientId,
                    capturedTargetPosition,
                    isTargetOnPositiveSide
                );
            }
        }

        private Vector3 ClampPositionToBounds(Vector3 position)
        {
             if (_movementBounds.width <= 0 || _movementBounds.height <= 0) return position;
            return new Vector3(
                Mathf.Clamp(position.x, _movementBounds.xMin, _movementBounds.xMax),
                Mathf.Clamp(position.y, _movementBounds.yMin, _movementBounds.yMax),
                position.z
            );
        }

        public void ServerInitialize(PlayerRole role, Level4SpellcardData data)
        {
            if (!IsServer) return;
            TargetPlayerRole.Value = role;
            _spellData = data;
        }
    }
} 