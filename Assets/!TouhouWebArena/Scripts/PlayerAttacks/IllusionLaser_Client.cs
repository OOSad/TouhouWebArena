using UnityEngine;

namespace TouhouWebArena.PlayerAttacks
{
    /// <summary>
    /// Client-side logic for the illusion/Marisa laser.
    /// Handles visual scaling, positioning, damage dealing, and lifetime.
    /// </summary>
    public class IllusionLaser_Client : MonoBehaviour
    {
        [Header("Laser Settings")]
        [SerializeField] private float laserLength = 20f;
        [SerializeField] private float duration = 1.5f;
        [SerializeField] private float damagePerSecond = 50f; // DPS
        [SerializeField] private float damageTickRate = 0.1f; // How often damage is applied
        [SerializeField] private float followOffsetX = 0f;

        private Transform _ownerTransform; // Transform to follow (illusion or player)
        private float _timeActive;
        private float _damageTickTimer;
        private ClientProjectileLifetime _projectileLifetime;
        private BoxCollider2D _collider;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _projectileLifetime = GetComponent<ClientProjectileLifetime>();
            _collider = GetComponent<BoxCollider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_projectileLifetime == null || _collider == null || _spriteRenderer == null)
            {
                Debug.LogError("IllusionLaser_Client is missing required components (ClientProjectileLifetime, BoxCollider2D, SpriteRenderer)!", this);
            }
        }

        /// <summary>
        /// Initializes the laser.
        /// </summary>
        /// <param name="ownerTransform">The transform the laser should follow.</param>
        public void Initialize(Transform ownerTransform)
        {
            _ownerTransform = ownerTransform;
            _timeActive = 0f;
            _damageTickTimer = damageTickRate; // Apply damage on first possible tick

            if (_projectileLifetime != null)
            {
                _projectileLifetime.Initialize(duration);
            }

            UpdateLaserTransform(); // Initial setup of scale/collider
        }

        void Update()
        {
            _timeActive += Time.deltaTime;
            if (_timeActive >= duration && _projectileLifetime != null)
            {
                _projectileLifetime.ForceReturnToPool();
                return;
            }

            UpdateLaserTransform(); // Keep laser position updated

            // Damage ticking logic
            _damageTickTimer -= Time.deltaTime;
            if (_damageTickTimer <= 0f)
            {
                ApplyDamageInArea();
                _damageTickTimer = damageTickRate;
            }
        }
        
        /// <summary>
        /// Updates the laser's position, length (visual scale + collider size).
        /// Pivot MUST be at the bottom of the sprite for this to work correctly.
        /// </summary>
        private void UpdateLaserTransform()
        {
            if (_ownerTransform != null)
            {
                // Restore offset calculation using followOffsetX along owner's right axis
                transform.position = _ownerTransform.position + (_ownerTransform.right * followOffsetX);
                transform.rotation = _ownerTransform.rotation;
            }

            if (_spriteRenderer != null)
            {
                // Adjust Y scale based on length, assuming original sprite height corresponds to length 1
                float originalSpriteHeight = _spriteRenderer.sprite.bounds.size.y;
                 // Prevent division by zero if sprite height is somehow zero
                if (originalSpriteHeight > 0.001f) 
                { 
                    float scaleY = laserLength / originalSpriteHeight;
                    transform.localScale = new Vector3(transform.localScale.x, scaleY, transform.localScale.z);
                }
                else
                {
                    transform.localScale = new Vector3(transform.localScale.x, 0, transform.localScale.z);
                    Debug.LogWarning("Sprite original height is zero, cannot scale laser Y.", this);
                }
            }

            if (_collider != null)
            {
                // Adjust collider size and offset to match visual length
                _collider.size = new Vector2(_collider.size.x, laserLength);
                _collider.offset = new Vector2(_collider.offset.x, laserLength / 2f); // Offset assumes pivot is at bottom center
            }
        }
        
        private void ApplyDamageInArea()
        {
            // Since OnTriggerStay2D doesn't reliably fire every frame or physics step,
            // we can do an overlap check based on the collider bounds here.
            if (_collider == null) return;

            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position + (Vector3)_collider.offset, _collider.size, transform.eulerAngles.z);

            foreach (Collider2D hit in hits)
            {
                // Apply damage logic similar to OnTriggerStay2D
                 ClientFairyHealth fairyHealth = hit.GetComponent<ClientFairyHealth>();
                 if (fairyHealth != null && fairyHealth.IsAlive)
                 {
                     fairyHealth.TakeDamage(CalculateTickDamage(), (ulong)0); // Pass Attacker ID if needed
                 }

                 ClientSpiritHealth spiritHealth = hit.GetComponent<ClientSpiritHealth>();
                 if (spiritHealth != null && spiritHealth.IsAlive())
                 {
                     spiritHealth.TakeDamage(CalculateTickDamage(), (ulong)0); // Pass Attacker ID if needed, 0 for environment/unspecified?
                 }
            }
        }

        private int CalculateTickDamage()
        {
            // Calculate damage per tick based on DPS and tick rate
            return Mathf.Max(1, Mathf.RoundToInt(damagePerSecond * damageTickRate));
        }
    }
}