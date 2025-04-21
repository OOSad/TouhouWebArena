using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections; // Added for Coroutines

// NEW: Require the visuals component
[RequireComponent(typeof(PlayerInvincibilityVisuals))]
[RequireComponent(typeof(CharacterStats))]
public class PlayerHealth : NetworkBehaviour
{
    // NetworkVariable constructor needs a default value, cannot use CharacterStats here yet.
    // We will set the correct value authoritatively on the server in OnNetworkSpawn.
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // Default to 1, server will fix

    // Event to notify UI or other systems about health changes
    public event Action<int> OnHealthChanged;
    // Event to notify game state manager about death (server-side)
    public static event Action<ulong> OnPlayerDeathServer; 

    // NetworkVariable to sync invincibility state (server writes, everyone reads)
    public NetworkVariable<bool> IsInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private PlayerDeathBomb playerDeathBomb; // Reference to the bomb component
    private CharacterStats characterStats; // Added reference

    // Added Awake to get components
    private void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
        playerDeathBomb = GetComponent<PlayerDeathBomb>();

        // Logs removed, empty ifs remain
        // if (characterStats == null) 
        // if (playerDeathBomb == null) 
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CurrentHealth.OnValueChanged += HandleHealthChanged;

        // If this is the server, initialize health based on CharacterStats
        if (IsServer && characterStats != null)
        {
            CurrentHealth.Value = characterStats.GetStartingHealth();
        }

        // Trigger initial health UI update (even if server hasn't set value yet, client will get update soon)
        HandleHealthChanged(0, CurrentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int previousValue, int newValue)
    {
        // Invoke the event whenever health changes (clients will react here for UI)
        OnHealthChanged?.Invoke(newValue);
    }

    // Call this method from PlayerHitbox on the server OR via RequestDamageServerRpc
    public void TakeDamage(int amount)
    {
        if (!IsServer) return;
        if (IsInvincible.Value) return;
        if (CurrentHealth.Value <= 0) return;

        int previousHealth = CurrentHealth.Value; 
        int newHealth = CurrentHealth.Value - amount;
        CurrentHealth.Value = Mathf.Max(newHealth, 0);

        if (CurrentHealth.Value <= 0)
        {
            HandleDeathServer();
        }
        else
        {
            TriggerInvincibilityServer(); 
        }
    }

    // Renamed to indicate it's server-only logic trigger
    private void TriggerInvincibilityServer()
    {
        if (!IsServer || IsInvincible.Value) return;
        StartCoroutine(ServerInvincibilityTimerCoroutine());
    }

    private IEnumerator ServerInvincibilityTimerCoroutine()
    {
        IsInvincible.Value = true; 
        // Use duration from CharacterStats
        yield return new WaitForSeconds(characterStats.GetInvincibilityDuration()); 
        
        if (playerDeathBomb != null)
        {
            playerDeathBomb.ExecuteBomb(); 
        }
        
        // This should run regardless of whether the bomb component exists/executed
        IsInvincible.Value = false; 
    }

    private void HandleDeathServer()
    {
        // This runs ONLY on the server when health reaches 0
        OnPlayerDeathServer?.Invoke(OwnerClientId);
    }

    // Example: Reset health (e.g., called by a game manager at round start)
    [ServerRpc(RequireOwnership = false)] // Allow server to call this on player objects
    public void ResetHealthServerRpc()
    {
        // Reset to value from CharacterStats
        if (characterStats != null) 
        {
             CurrentHealth.Value = characterStats.GetStartingHealth();
        }
        else
        {
            
            // Optionally set to a default value here if needed
            // CurrentHealth.Value = 5; 
        }
    }
} 