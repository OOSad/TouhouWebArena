using UnityEngine;
using System.Collections;

public class ClientLilyWhiteController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float initialDriftDownSpeed = 5.0f; // Renamed from driftDownSpeed
    public float floatUpSpeed = 2.5f;
    public float waitDuration = 1.0f;
    public float targetYInCenter = 0.0f; // Y position to drift down to
    public float offScreenYTop = 10.0f; // Y position considered off-screen when moving up
    public float initialSpawnY = 8.0f; // Y position to spawn at (top of screen)

    [Header("Lifetime")]
    public float totalLifetime = 15.0f; // Fallback despawn timer

    [Header("Attack Components")]
    [Tooltip("Assign the LilyWhiteAttackPattern component here if present on this GameObject.")]
    public LilyWhiteAttackPattern attackPatternHandler; // Public field for Inspector assignment

    private ClientLilyWhiteHealth _healthComponent; // Added health component reference
    private PooledObjectInfo pooledObjectInfo;
    private Coroutine movementCoroutine;
    private PlayerRole _targetedPlayerRole = PlayerRole.None; // New field to store the targeted player role

    void Awake()
    {
        pooledObjectInfo = GetComponent<PooledObjectInfo>();
        if (pooledObjectInfo == null)
        {
            Debug.LogError("ClientLilyWhiteController requires a PooledObjectInfo component.");
        }

        _healthComponent = GetComponent<ClientLilyWhiteHealth>(); // Get health component
        if (_healthComponent == null)
        {
            Debug.LogWarning("ClientLilyWhiteController: ClientLilyWhiteHealth component not found. Lily White will be invulnerable.");
        }

        // Attempt to get Attack Pattern Handler if not assigned in Inspector
        if (attackPatternHandler == null)
        {
            attackPatternHandler = GetComponent<LilyWhiteAttackPattern>();
            if (attackPatternHandler == null)
            {
                Debug.LogWarning("ClientLilyWhiteController: LilyWhiteAttackPattern component not found on this GameObject. Attacks will not be triggered.");
            }
        }
    }

    public void Initialize(float spawnX, PlayerRole targetedPlayerRole)
    {
        // Set initial position (specific X for playfield, specific Y for top)
        transform.position = new Vector3(spawnX, initialSpawnY, 0);
        _targetedPlayerRole = targetedPlayerRole; // Store the targeted player role
        gameObject.SetActive(true);

        if (_healthComponent != null) // Initialize health component
        {
            _healthComponent.Initialize();
        }

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        movementCoroutine = StartCoroutine(MovementLifecycleCoroutine());
    }

    private IEnumerator MovementLifecycleCoroutine()
    {
        // Phase 1: Drift down with deceleration
        float currentY = transform.position.y;
        float distanceToDescend = currentY - targetYInCenter;
        float calculatedDeceleration = 0f;

        if (distanceToDescend > 0.01f) // Avoid division by zero or if already at target
        {
            // Formula: a = v_initial^2 / (2 * distance) for deceleration to zero speed
            calculatedDeceleration = (initialDriftDownSpeed * initialDriftDownSpeed) / (2 * distanceToDescend);
        }
        
        float currentSpeed = initialDriftDownSpeed;

        while (transform.position.y > targetYInCenter && currentSpeed > 0.01f)
        {
            float moveStep = currentSpeed * Time.deltaTime;
            
            // Ensure we don't overshoot the target
            if (transform.position.y - moveStep < targetYInCenter)
            {
                moveStep = transform.position.y - targetYInCenter;
                currentSpeed = 0; // Effectively stop
            }
            
            transform.position += Vector3.down * moveStep;
            
            currentSpeed -= calculatedDeceleration * Time.deltaTime;
            if (currentSpeed < 0) currentSpeed = 0;
            
            yield return null;
        }
        // Snap to target position precisely
        transform.position = new Vector3(transform.position.x, targetYInCenter, transform.position.z);

        // Phase 2: Wait in center
        yield return new WaitForSeconds(waitDuration);

        // Phase 3: Float up and Trigger Attack
        if (attackPatternHandler != null)
        {
            attackPatternHandler.StartAttackSequence(this.transform, _targetedPlayerRole);
            Debug.Log("ClientLilyWhiteController: Attack sequence started.");
        }
        else
        {
            Debug.LogWarning("ClientLilyWhiteController: attackPatternHandler is null, cannot start attack sequence.");
        }

        while (transform.position.y < offScreenYTop)
        {
            transform.position += Vector3.up * floatUpSpeed * Time.deltaTime;
            yield return null;
        }

        // Despawn (either by reaching off-screen or fallback timer)
        // For now, the coroutine naturally ends when she's off-screen.
        // A separate timer ensures despawn if something goes wrong or if she never reaches offScreenYTop.
        // However, per instructions, a simple timer-based despawn is fine for now.
        // So, we'll let the fallback timer handle it primarily.
        // If we wanted her to despawn *immediately* after going off-screen:
        // ReturnToPool();
    }
    
    void OnEnable()
    {
        // Start a fallback despawn timer in case the movement coroutine doesn't complete as expected
        // or to adhere to the "timer-based despawn" for simplicity initially.
        StartCoroutine(DespawnTimerCoroutine());
    }

    void OnDisable()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
    }
    
    private IEnumerator DespawnTimerCoroutine()
    {
        yield return new WaitForSeconds(totalLifetime);
        if (gameObject.activeSelf) // Check if not already returned by other means (e.g., death)
        {
            ReturnToPool();
        }
    }

    // Public method to be called by ClientLilyWhiteHealth when health reaches zero
    public void HandleDeath()
    {
        // Debug.Log($"[ClientLilyWhiteController] {gameObject.name} HandleDeath called.");
        // Stop all active coroutines for this controller
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        // Stop the fallback despawn timer as well, as death is a definitive end
        StopAllCoroutines(); // More aggressive stop for all controller-managed coroutines
        
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (gameObject.activeSelf) // Ensure we only try to return active objects
        {
            if (pooledObjectInfo != null && ClientGameObjectPool.Instance != null)
            {
                ClientGameObjectPool.Instance.ReturnObject(gameObject);
            }
            else
            {
                gameObject.SetActive(false); // Fallback if pool is not available
            }
        }
    }
} 