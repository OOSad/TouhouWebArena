using UnityEngine;
using System.Collections;
using Unity.Netcode;
// using TouhouWebArena.Spellcards.Behaviors; // ClientLinearMovement no longer needed here

// Assuming PlayerDataManager and PlayerRole are globally accessible
// Might need: using TouhouWebArena.Managers; if PlayerDataManager is namespaced

// namespace TouhouWebArena.Client.Enemies // Global for now
// {
    public class ClientSpiritTimeoutAttack : MonoBehaviour
    {
        private ClientSpiritHealth _spiritHealth;
        private PooledObjectInfo _pooledObjectInfo; // For logging if needed

        [Tooltip("Prefab ID of the large bullet to spawn for the timeout attack.")]
        [SerializeField] private string timeoutBulletPrefabID = "StageLargeBullet"; // Example, make sure this exists in ClientGameObjectPool
        
        [Tooltip("Offset angles for the claw pattern (e.g., -15, 0, 15 degrees).")]
        [SerializeField] private float[] clawPatternAngles = new float[] { -15f, 0f, 15f };

        [Tooltip("Speed of the timeout bullets.")]
        [SerializeField] private float timeoutBulletSpeed = 5f;

        [Tooltip("Lifetime of the timeout bullets in seconds.")]
        [SerializeField] private float timeoutBulletLifetime = 5f;

        [Tooltip("The X-coordinate that divides Player 1's side (less than) from Player 2's side (greater than or equal to).")]
        [SerializeField] private float stageCenterXCoordinate = 0f;

        private Coroutine _timeoutCoroutine;

        void Awake()
        {
            _spiritHealth = GetComponent<ClientSpiritHealth>();
            _pooledObjectInfo = GetComponent<PooledObjectInfo>();
        }

        // The targetPlayerId from ClientSpiritController is now less relevant for targeting,
        // but kept for potential debugging or other uses if StartTimeout is called from elsewhere.
        public void StartTimeout(float duration, ulong initialTargetPlayerId_UnusedForTargeting)
        {
            if (!gameObject.activeInHierarchy || !enabled) return;

            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
            }
            // Pass the original target ID for logging, but the coroutine will re-determine the actual target.
            _timeoutCoroutine = StartCoroutine(TimeoutAttackCoroutine(duration, initialTargetPlayerId_UnusedForTargeting));
        }

        private IEnumerator TimeoutAttackCoroutine(float duration, ulong loggedSpawnTargetPlayerId)
        {
            yield return new WaitForSeconds(duration);

            if (!gameObject.activeSelf || _spiritHealth == null)
            {
                yield break; 
            }

            // Determine target based on current position
            PlayerRole determinedSideRole = (transform.position.x < stageCenterXCoordinate) ? PlayerRole.Player1 : PlayerRole.Player2;
            ulong actualTargetPlayerId = 0;
            Transform targetTransform = null;

            if (PlayerDataManager.Instance != null)
            {
                PlayerData? playerData = PlayerDataManager.Instance.GetPlayerDataByRole(determinedSideRole);
                if (playerData.HasValue && playerData.Value.ClientId != 0)
                {
                    actualTargetPlayerId = playerData.Value.ClientId;
                    // Now find the NetworkObject for this actualTargetPlayerId
                    if (NetworkManager.Singleton != null)
                    {
                        foreach (NetworkObject netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
                        {
                            if (netObj.OwnerClientId == actualTargetPlayerId && netObj.GetComponent<CharacterStats>() != null)
                            {
                                targetTransform = netObj.transform;
                                break;
                            }
                        }
                    }
                }
            }

            Debug.Log($"[ClientSpiritTimeoutAttack] Spirit timeout! Determined side: {determinedSideRole}, Attempting to target CID: {actualTargetPlayerId} (Spawn-time CID was: {loggedSpawnTargetPlayerId})", this);

            if (targetTransform == null)
            {
                Debug.LogWarning($"[ClientSpiritTimeoutAttack] Could not find/resolve player on side {determinedSideRole} (Target CID: {actualTargetPlayerId}). Timeout attack will fire downwards.", this);
            }

            Vector2 directionToTarget = (targetTransform != null) ? 
                                        ((Vector3)targetTransform.position - transform.position).normalized :
                                        Vector2.down; 

            foreach (float angleOffset in clawPatternAngles)
            {
                GameObject bulletInstance = ClientGameObjectPool.Instance.GetObject(timeoutBulletPrefabID);
                if (bulletInstance == null)
                {
                    Debug.LogError($"[ClientSpiritTimeoutAttack] Failed to get bullet prefab '{timeoutBulletPrefabID}' from pool.", this);
                    continue;
                }

                bulletInstance.transform.position = transform.position;
                Quaternion baseRotation = Quaternion.LookRotation(Vector3.forward, directionToTarget);
                bulletInstance.transform.rotation = baseRotation * Quaternion.Euler(0, 0, angleOffset);
                
                StageSmallBulletMoverScript mover = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
                if (mover != null)
                {
                    mover.Initialize(bulletInstance.transform.up, timeoutBulletSpeed, timeoutBulletLifetime);
                }
                else
                {
                    Debug.LogWarning($"[ClientSpiritTimeoutAttack] Bullet '{timeoutBulletPrefabID}' does not have StageSmallBulletMoverScript. Bullet will not move or despawn correctly.", bulletInstance);
                }
                
                bulletInstance.SetActive(true);
            }
            
            if (_spiritHealth != null) _spiritHealth.ForceReturnToPool();
            else if (_pooledObjectInfo != null) ClientGameObjectPool.Instance.ReturnObject(gameObject);
            else Destroy(gameObject);
            _timeoutCoroutine = null;
        }

        public void StopTimeoutAttack()
        {
            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }
        }

        void OnDisable()
        {
            StopTimeoutAttack();
        }
    }
// } 