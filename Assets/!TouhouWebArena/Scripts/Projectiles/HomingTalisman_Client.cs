using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Client-side version of HomingTalisman
public class HomingTalisman_Client : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int damage = 2;
    [SerializeField] private float speed = 5f;
    // [SerializeField] private float initialDelay = 0.5f; // Replaced by Initialize parameter
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private List<string> targetTags = new List<string>() { "Fairy", "Spirit" };

    [Header("Targeting Boundaries (World Space)")]
    [SerializeField] private float minX = -4f;
    [SerializeField] private float maxX = 4f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;

    private Transform currentTarget;
    private bool canSeek = false;
    private float timeSinceLastRetargetCheck = 0f;
    private const float RETARGET_CHECK_INTERVAL = 0.1f;

    private PlayerRole _ownerPlayerRole = PlayerRole.None;
    private Coroutine _initialDelayCoroutine;
    private Coroutine _lifetimeCoroutine;

    public void Initialize(PlayerRole ownerRole, float startDelay)
    {
        _ownerPlayerRole = ownerRole;
        // Stop existing coroutines if any (e.g., if re-initializing from pool)
        if (_initialDelayCoroutine != null) StopCoroutine(_initialDelayCoroutine);
        if (_lifetimeCoroutine != null) StopCoroutine(_lifetimeCoroutine);

        _initialDelayCoroutine = StartCoroutine(InitialDelayCoroutine(startDelay));
        _lifetimeCoroutine = StartCoroutine(LifetimeCoroutine());
        ResetState();
    }

    void OnEnable()
    {
        // Default initialization if not called via Initialize (e.g., first time from pool without immediate Initialize)
        // However, proper usage is to call Initialize right after GetObject from pool.
        if (_ownerPlayerRole == PlayerRole.None) // Only if not already initialized
        {
            ResetState();
            // Start coroutines with default values, though they should be overridden by Initialize
            if (_initialDelayCoroutine != null) StopCoroutine(_initialDelayCoroutine);
            if (_lifetimeCoroutine != null) StopCoroutine(_lifetimeCoroutine);
            _initialDelayCoroutine = StartCoroutine(InitialDelayCoroutine(0f)); // Default delay
            _lifetimeCoroutine = StartCoroutine(LifetimeCoroutine());
        }
    }

    void OnDisable()
    {
        if (_initialDelayCoroutine != null) StopCoroutine(_initialDelayCoroutine);
        if (_lifetimeCoroutine != null) StopCoroutine(_lifetimeCoroutine);
        _initialDelayCoroutine = null;
        _lifetimeCoroutine = null;
    }

    private void ResetState()
    {
        currentTarget = null;
        canSeek = false;
        timeSinceLastRetargetCheck = RETARGET_CHECK_INTERVAL; // Allow immediate check on first seek frame
    }

    private void FixedUpdate()
    {
        if (!canSeek) return;

        if (currentTarget != null)
        {
            if (!currentTarget.gameObject.activeInHierarchy || IsTargetOutOfBounds(currentTarget.position))
            {
                currentTarget = null;
            }
        }

        if (currentTarget == null)
        {
            timeSinceLastRetargetCheck += Time.fixedDeltaTime;
            if (timeSinceLastRetargetCheck >= RETARGET_CHECK_INTERVAL)
            {
                FindTarget();
                timeSinceLastRetargetCheck = 0f;
            }
        }

        if (currentTarget != null)
        {
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            transform.position += direction * speed * Time.fixedDeltaTime;
            // Optional rotation
            // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward); 
        }
    }

    private IEnumerator InitialDelayCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        canSeek = true;
    }

    private IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(lifetime);
        DespawnInternal();
    }

    private bool IsTargetOutOfBounds(Vector3 position)
    {
        return position.x < minX || position.x > maxX || position.y < minY || position.y > maxY;
    }

    private void FindTarget()
    {
        if (_ownerPlayerRole == PlayerRole.None || targetTags == null || targetTags.Count == 0 || ClientGameObjectPool.Instance == null)
        {
            currentTarget = null;
            return;
        }

        Transform foundTarget = null;
        float closestDistSqr = float.MaxValue;

        // Iterate through all active objects in the pool. This could be optimized.
        var activeObjects = ClientGameObjectPool.Instance.GetAllActiveObjects(); 
        foreach (GameObject obj in activeObjects)
        {
            if (obj == null || !obj.activeInHierarchy) continue;
            if (!targetTags.Contains(obj.tag)) continue;

            PlayerRole enemyOwningSide = PlayerRole.None;
            bool isValidEnemyType = false;

            ClientSpiritController spiritController = obj.GetComponent<ClientSpiritController>();
            if (spiritController != null)
            {
                enemyOwningSide = spiritController.OwningPlayerRole;
                isValidEnemyType = true;
            }
            else
            {
                ClientFairyController fairyController = obj.GetComponent<ClientFairyController>();
                if (fairyController != null)
                {
                    enemyOwningSide = fairyController.GetOwningPlayerRole(); // ASSUMPTION: This method exists
                    isValidEnemyType = true;
                }
            }

            if (isValidEnemyType && enemyOwningSide == _ownerPlayerRole) // Target only enemies on the talisman owner's side
            {
                Vector3 targetPos = obj.transform.position;
                if (!IsTargetOutOfBounds(targetPos))
                {
                    float distSqr = (targetPos - transform.position).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        foundTarget = obj.transform;
                    }
                }
            }
        }
        currentTarget = foundTarget;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!targetTags.Contains(other.gameObject.tag)) return;
        if (_ownerPlayerRole == PlayerRole.None) return; // Don't do anything if not properly initialized

        PlayerRole enemyOwningSide = PlayerRole.None;
        bool damageApplied = false;

        ClientSpiritHealth spiritHealth = other.GetComponent<ClientSpiritHealth>();
        if (spiritHealth != null)
        {
            ClientSpiritController spiritController = other.GetComponent<ClientSpiritController>();
            if (spiritController != null) enemyOwningSide = spiritController.OwningPlayerRole;
            
            if (enemyOwningSide == _ownerPlayerRole)
            {
                // Pass 0 as attackerOwnerClientId for client-side damage
                spiritHealth.TakeDamage(damage, 0);
                damageApplied = true;
            }
        }
        else
        {
            ClientFairyHealth fairyHealth = other.GetComponent<ClientFairyHealth>();
            if (fairyHealth != null)
            {
                ClientFairyController fairyController = other.GetComponent<ClientFairyController>();
                if (fairyController != null) enemyOwningSide = fairyController.GetOwningPlayerRole(); // ASSUMPTION

                if (enemyOwningSide == _ownerPlayerRole)
                {
                    // Pass 0 as attackerOwnerClientId for client-side damage
                    fairyHealth.TakeDamage(damage, 0);
                    damageApplied = true;
                }
            }
        }

        if (damageApplied)
        {
            DespawnInternal();
        }
    }

    private void DespawnInternal()
    {
        if (this.gameObject != null && ClientGameObjectPool.Instance != null)
        {
            canSeek = false; // Stop seeking immediately
            currentTarget = null; // Clear target
            ClientGameObjectPool.Instance.ReturnObject(this.gameObject);
        }
        // else if (this.gameObject != null) { Destroy(this.gameObject); } // Fallback if pool is somehow null, though unlikely
    }
} 