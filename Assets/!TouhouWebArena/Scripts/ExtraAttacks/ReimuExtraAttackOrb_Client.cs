using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(PooledObjectInfo))]
public class ReimuExtraAttackOrb_Client : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float initialUpwardForce = 5f;
    // [SerializeField] private float initialSidewaysForceMin = 2f;
    // [SerializeField] private float initialSidewaysForceMax = 4f;
    [SerializeField] private float lifetime = 5f; // Seconds before returning to pool

    private Rigidbody2D rb;
    private PooledObjectInfo pooledObjectInfo;
    private float currentLifetime;
    private ulong _attackerClientId; // Store the client ID of the player who triggered this attack

    public ulong AttackerClientId => _attackerClientId; // Public getter

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pooledObjectInfo = GetComponent<PooledObjectInfo>();
        // Ensure Rigidbody2D is set to allow gravity and has a physics material for bouncing via its CircleCollider2D.
        // This is best set on the prefab itself.
    }

    void OnEnable()
    {
        currentLifetime = lifetime;
        // Apply initial forces when the object is enabled (spawned)
    }

    public void Initialize(ulong attackerClientId, float predeterminedSidewaysForce)
    {
        this._attackerClientId = attackerClientId;
        // Debug.Log($"{gameObject.name} Initialized by Client ID: {this._attackerClientId}");

        // Reset velocity
        if (rb != null) // rb might be null if Initialize is called before Awake/OnEnable in some pooling scenarios
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            Vector2 initialForce = new Vector2(predeterminedSidewaysForce, initialUpwardForce);
            rb.AddForce(initialForce, ForceMode2D.Impulse);
            // Debug.Log($"{gameObject.name} initialized with force {initialForce}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} Rigidbody2D not ready during Initialize. Force not applied.");
        }
        currentLifetime = lifetime; // Reset lifetime on init as well
    }

    void Update()
    {
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0)
        {
            ReturnToPool();
        }
    }

    // Example collision - could damage opponent fairies/spirits
    void OnTriggerEnter2D(Collider2D other)
    {
        // Implement collision logic here if the orb should interact with things.
        // For example, check if `other` is an enemy on the opponent's side.
        // PlayerData.PlayerRole targetSide = (ownerPlayerRole == PlayerData.PlayerRole.Player1) ? PlayerData.PlayerRole.Player2 : PlayerData.PlayerRole.Player1;
        
        ClientFairyHealth fairyHealth = other.GetComponent<ClientFairyHealth>();
        if (fairyHealth != null && fairyHealth.IsAlive)
        {
            // Consider which player's ID to pass as attacker for the damage
            // For extra attacks, it might be complex. Simplest is to pass the original attacker's client ID if available,
            // or a generic "environment" ID if not directly attributable or if it causes chain reactions for the opponent.
            fairyHealth.TakeDamage(10, this._attackerClientId); // Example: 10 damage, pass stored attacker ID
            // Could also just return to pool on first impact with an enemy
            // Debug.Log($"{gameObject.name} hit fairy {other.name}");
            ReturnToPool(); 
        }

        ClientSpiritHealth spiritHealth = other.GetComponent<ClientSpiritHealth>();
        if (spiritHealth != null && spiritHealth.IsAlive()) // Corrected: IsAlive() method call
        {
            spiritHealth.TakeDamage(10, this._attackerClientId); 
            ReturnToPool();
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
                    // Only report hit if it's an opponent AND this client is the victim
                    if (victimPlayerHealth.OwnerClientId != this._attackerClientId && 
                        NetworkManager.Singleton.LocalClientId == victimPlayerHealth.OwnerClientId)
                    {
                        int damageAmount = 1; 
                        
                        if (PlayerExtraAttackRelay.LocalInstance != null)
                        {
                            PlayerExtraAttackRelay.LocalInstance.ReportExtraAttackPlayerHitServerRpc(victimPlayerHealth.OwnerClientId, damageAmount, this._attackerClientId);
                        }
                        else
                        {
                            Debug.LogError($"{gameObject.name} cannot find PlayerExtraAttackRelay.LocalInstance to report player hit!");
                        }
                        // ReturnToPool(); // REMOVED - Orb should not despawn on hitting an opponent player
                    }
                    // else // OLD: Hit self or friendly
                    // {
                    //     ReturnToPool(); // REMOVED - Orb should not despawn on hitting a friendly player either
                    // }
                }
                // else // OLD: PlayerHealth component not found
                // {
                //     ReturnToPool(); // REMOVED - Orb should not despawn if PlayerHealth is missing for some reason
                // }
            }
            // else // OLD: PlayerHitbox component not found
            // {
            //     ReturnToPool(); // REMOVED - Orb should not despawn if PlayerHitbox component is missing
            // }
            // If any of the above conditions lead to ReturnToPool, it means the orb despawns. To keep it alive, remove those calls.
            // The orb will naturally despawn due to its lifetime in Update().
        }
    }

    /// <summary>
    /// Public method called by external systems (like spellcard clear) to force this object back to the pool.
    /// </summary>
    public void ForceReturnToPoolByClear()
    {
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (ClientGameObjectPool.Instance != null && pooledObjectInfo != null)
        {
            ClientGameObjectPool.Instance.ReturnObject(this.gameObject); // Corrected: ReturnObject takes 1 argument
        }
        else
        {
            gameObject.SetActive(false); // Fallback if pool is not available
        }
    }
} 