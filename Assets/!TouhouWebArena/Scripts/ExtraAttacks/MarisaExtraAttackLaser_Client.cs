using UnityEngine;

// Assuming PlayerRole is global or in an accessible namespace
// Assuming PlayAreaBounds is the struct defined in ClientExtraAttackManager or globally

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(PooledObjectInfo))]
public class MarisaExtraAttackLaser_Client : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private float activeDuration = 2f; // How long the laser stays active
    [SerializeField] private float activationDelay = 0.5f; // Time before the laser becomes damaging
    [SerializeField] private int damageAmount = 1; // Damage dealt per hit
    [SerializeField] private float maxTiltAngle = 10f; // Max degrees for slight tilt

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private PooledObjectInfo pooledObjectInfo;

    private float currentActiveTime;
    private float currentActivationTimer;
    private PlayAreaBounds _targetPlayAreaBounds; // Use the defined struct
    private ulong _attackerClientId;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        pooledObjectInfo = GetComponent<PooledObjectInfo>();

        boxCollider.isTrigger = true; // Ensure collider is a trigger for OnTriggerStay2D
        // if (spriteRenderer == null) Debug.LogError($"{gameObject.name} is missing SpriteRenderer for MarisaExtraAttackLaser_Client!");
    }

    public void Initialize(ulong attackerClientId, PlayAreaBounds playBounds, float predeterminedTiltAngle)
    {
        this._attackerClientId = attackerClientId;
        this._targetPlayAreaBounds = playBounds;
        currentActiveTime = activeDuration;
        currentActivationTimer = activationDelay;

        float laserLength = (_targetPlayAreaBounds.max.y - transform.position.y) + 1f; 

        transform.rotation = Quaternion.Euler(0, 0, predeterminedTiltAngle);
        
        // No need to set LineRenderer positions or update collider shape dynamically if sprite and its collider are pre-configured
    }

    void Update()
    {
        currentActiveTime -= Time.deltaTime;
        if (currentActiveTime <= 0)
        {
            ReturnToPool();
            return;
        }

        if (currentActivationTimer > 0)
        {
            currentActivationTimer -= Time.deltaTime;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (currentActivationTimer > 0) // Check if activation delay is still active
        {
            return; // Not damaging yet
        }

        ClientFairyHealth fairyHealth = other.GetComponent<ClientFairyHealth>();
        if (fairyHealth != null && fairyHealth.IsAlive)
        {
            // fairyHealth.TakeDamage(damageAmount, this._attackerClientId); // Keeping this for now, but laser might not damage fairies. Adjust if needed.
        }

        ClientSpiritHealth spiritHealth = other.GetComponent<ClientSpiritHealth>();
        if (spiritHealth != null && spiritHealth.IsAlive())
        {
            // spiritHealth.TakeDamage(damageAmount, this._attackerClientId); // Keeping this for now, but laser might not damage spirits. Adjust if needed.
        }

        // Check for PlayerHitbox
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerHitbox"))
        {
            PlayerHitbox playerHitbox = other.GetComponent<PlayerHitbox>(); 
            if (playerHitbox != null)
            {
                PlayerHealth victimPlayerHealth = playerHitbox.GetComponentInParent<PlayerHealth>(); 
                if (victimPlayerHealth != null)
                {
                    if (victimPlayerHealth.OwnerClientId != this._attackerClientId)
                    {
                        // Debug.Log($"{gameObject.name} (Attacker: {this._attackerClientId}) hit OPPONENT PlayerHitbox (Victim: {victimPlayerHealth.OwnerClientId}). Reporting. Dmg: {damageAmount}");
                        
                        if (PlayerExtraAttackRelay.LocalInstance != null)
                        {
                            PlayerExtraAttackRelay.LocalInstance.ReportExtraAttackPlayerHitServerRpc(victimPlayerHealth.OwnerClientId, damageAmount, this._attackerClientId);
                        }
                        else
                        {
                            Debug.LogError($"{gameObject.name} cannot find PlayerExtraAttackRelay.LocalInstance to report player hit!");
                        }
                        // damagedThisTick.Add(other); // REMOVED
                    }
                }
            }
        }
    }

    private void ReturnToPool()
    {
        // Reset rotation on pool return if it was tilted
        transform.rotation = Quaternion.identity;

        if (ClientGameObjectPool.Instance != null && pooledObjectInfo != null)
        {
            ClientGameObjectPool.Instance.ReturnObject(this.gameObject); // Corrected: ReturnObject takes 1 argument
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
} 