using UnityEngine;
using System;
using Unity.Netcode; // Required for NetworkManager

// Basic client-side health management for a fairy.
public class ClientFairyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 1;
    [Header("Death Effects")][Tooltip("The Pool ID for the shockwave prefab spawned on death.")]
    [SerializeField] private string deathShockwavePrefabId = "FairyShockwave";
    [Tooltip("The maximum radius this fairy's death shockwave reaches.")]
    [SerializeField] private float deathShockwaveMaxRadius = 2f;
    [Tooltip("The duration of this fairy's death shockwave expansion.")]
    [SerializeField] private float deathShockwaveDuration = 0.5f;
    [Tooltip("The damage dealt by this fairy's death shockwave.")]
    [SerializeField] private int deathShockwaveDamage = 5;

    private int _currentHealth;
    private bool _isExtraAttackTrigger = false; // Added for extra attack

    public int CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0;

    public event Action<GameObject> OnClientDeath; // Event to signal death, passing the fairy GameObject

    void OnEnable()
    {
        // Reset health when enabled (e.g., when taken from pool)
        _currentHealth = maxHealth;
        _isExtraAttackTrigger = false; // Reset trigger status on enable
    }

    public void Initialize(int startingHealth, bool isTrigger)
    {
        maxHealth = startingHealth > 0 ? startingHealth : 1;
        _currentHealth = maxHealth;
        _isExtraAttackTrigger = isTrigger;
        // Debug.Log($"{gameObject.name} initialized. Health: {_currentHealth}, IsTrigger: {_isExtraAttackTrigger}");
    }

    public void SetMaxHealth(int newMaxHealth, bool setCurrentToMax = true)
    {
        maxHealth = newMaxHealth > 0 ? newMaxHealth : 1;
        if (setCurrentToMax)
        {
            _currentHealth = maxHealth;
        }
    }

    public void TakeDamage(int amount, ulong attackerOwnerClientId)
    {
        if (!IsAlive) return;

        _currentHealth -= amount;
        // Debug.Log($"{gameObject.name} took {amount} damage from Client {attackerOwnerClientId}, health is now {_currentHealth}");

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;

            SpawnDeathShockwave(attackerOwnerClientId); // Spawn effect first

            // Conditional Kill Reporting
            if (_isExtraAttackTrigger)
            {
                // If it's a trigger fairy, let ClientExtraAttackManager handle it.
                // It will decide if a server report is needed for relaying.
                if (ClientExtraAttackManager.Instance != null)
                {
                    ClientExtraAttackManager.Instance.OnTriggerFairyKilled(attackerOwnerClientId);
                }
                else
                {
                    Debug.LogError($"[ClientFairyHealth on {gameObject.name}] ClientExtraAttackManager.Instance is null. Cannot report trigger fairy kill by {attackerOwnerClientId}.");
                }
            }
            else if (attackerOwnerClientId == NetworkManager.Singleton.LocalClientId) // Only report normal fairy kill if not a trigger and killed by local player
            {
                if (PlayerAttackRelay.LocalInstance != null)
                {
                    PlayerAttackRelay.LocalInstance.ReportFairyKillServerRpc();
                }
                else
                {
                    Debug.LogError($"[ClientFairyHealth on {gameObject.name}] PlayerAttackRelay.LocalInstance is null. Cannot report kill by local player {attackerOwnerClientId}.");
                }
            }
            
            // Trigger death event AFTER effects/reporting
            OnClientDeath?.Invoke(this.gameObject);
        }
    }

    public void ApplyLethalDamage(ulong instigatorClientId)
    {
        if (!IsAlive) return;
        _currentHealth = 0;

        SpawnDeathShockwave(instigatorClientId); // Spawn effect first

        // Conditional Kill Reporting for bombs/special
        // For lethal damage (e.g. bombs), we might not want extra attacks to trigger,
        // or it needs specific rules. For now, extra attacks only trigger from normal TakeDamage kills.
        // If bombs should trigger them, the logic from TakeDamage's _isExtraAttackTrigger block would be duplicated here.
        // For now, only standard kill reporting for lethal damage (if not handled by extra attack logic)
        if (!_isExtraAttackTrigger && instigatorClientId == NetworkManager.Singleton.LocalClientId)
        {
            if (PlayerAttackRelay.LocalInstance != null)
            {
                PlayerAttackRelay.LocalInstance.ReportFairyKillServerRpc(); // Or a specific bomb RPC if needed
            }
            else
            {
                Debug.LogError($"[ClientFairyHealth on {gameObject.name}] PlayerAttackRelay.LocalInstance is null. Cannot report lethal damage kill by local player {instigatorClientId}.");
            }
        }
        
        // Trigger death event AFTER effects/reporting
        OnClientDeath?.Invoke(this.gameObject); 
    }

    private void SpawnDeathShockwave(ulong killerClientId)
    {
        if (string.IsNullOrEmpty(deathShockwavePrefabId) || ClientGameObjectPool.Instance == null)
        {
            Debug.LogWarning($"[ClientFairyHealth on {gameObject.name}] Cannot spawn death shockwave. PrefabID: {deathShockwavePrefabId}, Pool: {ClientGameObjectPool.Instance?.name}");
            return;
        }

        GameObject shockwaveInstance = ClientGameObjectPool.Instance.GetObject(deathShockwavePrefabId);
        if (shockwaveInstance != null)
        {
            shockwaveInstance.transform.position = transform.position;
            shockwaveInstance.SetActive(true); // Activate before getting component?

            ClientFairyShockwave shockwaveScript = shockwaveInstance.GetComponent<ClientFairyShockwave>();
            if (shockwaveScript != null)
            {
                shockwaveScript.Initialize(
                    0.1f, 
                    deathShockwaveMaxRadius,
                    deathShockwaveDuration,
                    null, 
                    deathShockwaveDamage,
                    killerClientId 
                );
                // shockwaveInstance.SetActive(true); // Already set active?
            }
            else
            {
                Debug.LogError($"[ClientFairyHealth on {gameObject.name}] Spawned shockwave prefab '{deathShockwavePrefabId}' is missing the ClientFairyShockwave script! Returning to pool.");
                ClientGameObjectPool.Instance.ReturnObject(shockwaveInstance);
            }
        }
        else
        {
            Debug.LogWarning($"[ClientFairyHealth on {gameObject.name}] Failed to get shockwave '{deathShockwavePrefabId}' from pool.");
        }
    }
} 