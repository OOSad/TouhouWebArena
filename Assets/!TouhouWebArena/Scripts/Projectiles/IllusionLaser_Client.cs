using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))] // Ensure a collider is present
public class IllusionLaser_Client : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float hitInterval = 0.1f; // Time between damage ticks per enemy
    [SerializeField] private float duration = 0.5f;    // How long the laser lasts
    [Tooltip("Visual/origin X offset relative to player's X position.")]
    [SerializeField] private float followOffsetX = 0f; 
    [SerializeField] private float laserLength = 10f; // Desired length of the laser in world units

    [Header("Targeting")]
    [SerializeField] private List<string> targetTags = new List<string>() { "Fairy", "Spirit", "Illusion" }; // Added Illusion from screenshot

    private Dictionary<Collider2D, float> lastHitTimes = new Dictionary<Collider2D, float>();
    private PlayerRole _ownerPlayerRole = PlayerRole.None;
    private Transform _ownerTransform;
    private Coroutine _despawnCoroutine;

    public void Initialize(PlayerRole ownerRole, Vector3 initialPlayerPositionGivenBySpawner, Transform ownerTransformForFollowing)
    {
        _ownerPlayerRole = ownerRole;
        _ownerTransform = ownerTransformForFollowing;

        // The spawner (MarisaChargeAttackHandler_Client) has already set our transform.position 
        // using the marisaLaserSpawnPoint. This is the base of our laser.

        SpriteRenderer sr = GetComponent<SpriteRenderer>(); // Cache this if performance becomes an issue
        if (sr != null)
        {
            // Scale based on the fixed laserLength
            // This assumes the original sprite is 1 unit tall. If not, laserLength needs to be adjusted or this logic adapted.
            transform.localScale = new Vector3(transform.localScale.x, laserLength, transform.localScale.z);
        }
        else
        {
            Debug.LogWarning("[IllusionLaser_Client] SpriteRenderer not found, cannot scale laser height.", this);
        }
        
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>(); // Cache this if performance becomes an issue
        if (boxCollider != null && sr != null) 
        {
            // We are scaling localScale.y to be laserLength (if original sprite height was 1).
            // So, the BoxCollider should have size.y = 1 (to match original sprite) and offset.y = 0.5 (if pivot is bottom).
            // If your unscaled sprite is 1 unit high and pivot is at bottom (0), then collider of size 1, offset 0.5 fits it.
            
            // If your sprite or box collider setup on the prefab already matches its visual base form (e.g. 1 unit high, collider fitted)
            // and the pivot is at the bottom, you might not need to change collider size/offset at all after scaling the transform.
            // The transform scaling will scale the collider. The key is that the *initial* collider setup on the prefab is correct for the sprite.

            // Let's simplify: Assume the prefab's BoxCollider2D is already correctly sized and offset for the *unscaled* sprite
            // (e.g., if sprite is 1 unit high, collider size Y is 1, offset Y is 0.5 if sprite pivot is at its visual bottom).
            // Then, scaling transform.localScale.y will also scale the collider appropriately.
            // So, we might not need to manually adjust boxCollider.size and boxCollider.offset here IF the prefab is set up well.

            // Let's test without manually changing collider size/offset after transform scale, assuming prefab is correct.
            // If collisions are off, we can revisit this.
        }

        if (_despawnCoroutine != null) StopCoroutine(_despawnCoroutine);
        _despawnCoroutine = StartCoroutine(DespawnAfterDuration());
        lastHitTimes.Clear();
    }

    void OnEnable()
    {
        if (_ownerPlayerRole == PlayerRole.None)
        {
            if (_despawnCoroutine != null) StopCoroutine(_despawnCoroutine);
            _despawnCoroutine = StartCoroutine(DespawnAfterDuration()); 
            lastHitTimes.Clear();
        }
    }

    void OnDisable()
    {
        if (_despawnCoroutine != null) StopCoroutine(_despawnCoroutine);
        _despawnCoroutine = null;
        _ownerTransform = null; 
        // Reset player role if object is returned to pool and might be reused by a different player/context later
        _ownerPlayerRole = PlayerRole.None; 
    }

    void Update()
    {
        if (_ownerTransform != null)
        {
            // Laser base follows the owner's X and Y position.
            transform.position = new Vector3(
                _ownerTransform.position.x + followOffsetX,
                _ownerTransform.position.y, // Follow owner's Y directly
                transform.position.z
            );
        }
    }

    private IEnumerator DespawnAfterDuration()
    {
        yield return new WaitForSeconds(duration);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (this.gameObject != null && ClientGameObjectPool.Instance != null)
        {
            ClientGameObjectPool.Instance.ReturnObject(this.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcessCollision(other, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ProcessCollision(other, false);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (lastHitTimes.ContainsKey(other))
        {
            lastHitTimes.Remove(other);
        }
    }

    private void ProcessCollision(Collider2D other, bool isEnterCollision)
    {
        if (_ownerPlayerRole == PlayerRole.None) return;
        if (!targetTags.Contains(other.gameObject.tag)) return;

        PlayerRole enemyOwningSide = PlayerRole.None;
        bool isValidEnemy = false;

        ClientSpiritController spiritController = other.GetComponent<ClientSpiritController>();
        if (spiritController != null)
        {
            enemyOwningSide = spiritController.OwningPlayerRole;
            isValidEnemy = true;
        }
        else
        {
            ClientFairyController fairyController = other.GetComponent<ClientFairyController>();
            if (fairyController != null)
            {
                enemyOwningSide = fairyController.GetOwningPlayerRole();
                isValidEnemy = true;
            }
        }

        if (!isValidEnemy || enemyOwningSide != _ownerPlayerRole) return;

        float currentTime = Time.time;
        bool canDamage = false;

        if (isEnterCollision)
        {
            canDamage = true;
        }
        else
        {
            if (lastHitTimes.TryGetValue(other, out float lastHitTime))
            {
                if (currentTime >= lastHitTime + hitInterval)
                {
                    canDamage = true;
                }
            }
            else
            {
                canDamage = true; 
            }
        }

        if (canDamage)
        {
            ApplyDamage(other);
            lastHitTimes[other] = currentTime;
        }
    }

    private void ApplyDamage(Collider2D target)
    {
        ClientSpiritHealth spiritHealth = target.GetComponent<ClientSpiritHealth>();
        if (spiritHealth != null)
        {
            spiritHealth.TakeDamage(damageAmount, 0);
            return; 
        }

        ClientFairyHealth fairyHealth = target.GetComponent<ClientFairyHealth>();
        if (fairyHealth != null)
        {
            fairyHealth.TakeDamage(damageAmount, 0);
            return; 
        }
    }
} 