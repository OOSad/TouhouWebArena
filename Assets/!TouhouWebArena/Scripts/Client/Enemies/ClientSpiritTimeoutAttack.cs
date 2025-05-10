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

        // [Tooltip("The X-coordinate that divides Player 1's side (less than) from Player 2's side (greater than or equal to).")]
        // [SerializeField] private float stageCenterXCoordinate = 0f; // No longer primary targeting method

        private Coroutine _timeoutCoroutine;
        private ulong _currentTargetNetworkObjectId; // Store the target ID for the coroutine

        void Awake()
        {
            _spiritHealth = GetComponent<ClientSpiritHealth>();
            _pooledObjectInfo = GetComponent<PooledObjectInfo>();
        }

        public void StartTimeout(float duration, ulong targetNetworkObjectIdToAttack)
        {
            if (!gameObject.activeInHierarchy || !enabled) return;

            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
            }
            _currentTargetNetworkObjectId = targetNetworkObjectIdToAttack;
            _timeoutCoroutine = StartCoroutine(TimeoutAttackCoroutine(duration));
        }

        private IEnumerator TimeoutAttackCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);

            if (!gameObject.activeSelf || _spiritHealth == null)
            {
                yield break; 
            }

            Transform targetTransform = null;
            // Try to get target transform using the NetworkObjectId passed from ClientSpiritController
            if (_currentTargetNetworkObjectId != 0 && NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_currentTargetNetworkObjectId, out NetworkObject playerNetObj))
                {
                    targetTransform = playerNetObj.transform;
                    // Debug.Log($"[ClientSpiritTimeoutAttack] Spirit timeout! Successfully found target by NetworkObjectId: {_currentTargetNetworkObjectId} ({playerNetObj.name})", this);
                }
                else
                {
                    Debug.LogWarning($"[ClientSpiritTimeoutAttack] Spirit timeout! Could not find NetworkObject for targetNetworkObjectId {_currentTargetNetworkObjectId}. Firing downwards.", this);
                }
            }
            else
            {
                // Debug.LogWarning($"[ClientSpiritTimeoutAttack] Spirit timeout! No valid targetNetworkObjectId ({_currentTargetNetworkObjectId}) provided or NetworkManager unavailable. Firing downwards.", this);
                // No specific target ID, or couldn't find it, default to firing downwards.
            }

            Vector2 directionToTarget = (targetTransform != null) ? 
                                        ((Vector3)targetTransform.position - transform.position).normalized :
                                        Vector2.down; 

            // Debug.Log($"[ClientSpiritTimeoutAttack] Firing timeout bullets. TargetFound: {targetTransform != null}, Direction: {directionToTarget}");

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