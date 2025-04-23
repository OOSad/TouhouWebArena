using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections; // Added for Coroutines

// NEW: Require the visuals component
[RequireComponent(typeof(PlayerInvincibilityVisuals))]
[RequireComponent(typeof(CharacterStats))]
/// <summary>
/// Manages the health state of a player character in a networked environment.
/// Handles taking damage, death, invincibility frames, and synchronization of health/invincibility state.
/// This component is server-authoritative for all health modifications and state changes.
/// </summary>
public class PlayerHealth : NetworkBehaviour
{
    /// <summary>
    /// The current health of the player. Synchronized from server to clients.
    /// Initialized by the server in OnNetworkSpawn based on CharacterStats.
    /// </summary>
    [Tooltip("Current health points. Synced from server.")]
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // Default to 1, server will fix

    /// <summary>
    /// Event invoked on both server and clients whenever <see cref="CurrentHealth"/> changes.
    /// Primarily used by UI elements to update health displays.
    /// </summary>
    [Tooltip("Invoked when health changes. Parameter is the new health value.")]
    public event Action<int> OnHealthChanged;
    /// <summary>
    /// Static server-side event invoked ONLY on the server when a player's health reaches zero or less.
    /// Carries the NetworkClientId of the player who died. Used by game managers to handle player death logic.
    /// </summary>
    [Tooltip("Server-only event invoked when a player dies. Parameter is the OwnerClientId.")]
    public static event Action<ulong> OnPlayerDeathServer; 

    /// <summary>
    /// Networked state indicating if the player is currently invincible (cannot take damage).
    /// Controlled and synchronized by the server.
    /// </summary>
    [Tooltip("Is the player currently invincible? Synced from server.")]
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

    /// <summary>
    /// Server-authoritative method to apply damage to the player.
    /// Checks for invincibility and current health state before applying damage.
    /// Triggers invincibility frames or handles death if health drops to zero.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
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

    /// <summary>
    /// Server-side logic executed when the player's health reaches zero.
    /// Invokes the static <see cref="OnPlayerDeathServer"/> event.
    /// </summary>
    private void HandleDeathServer()
    {
        // This runs ONLY on the server when health reaches 0
        OnPlayerDeathServer?.Invoke(OwnerClientId);
    }

    /// <summary>
    /// ServerRpc allowing the server (or potentially clients with authority, though not recommended for health)
    /// to reset the player's health to its starting value defined in <see cref="CharacterStats"/>.
    /// Useful for starting new rounds or respawning.
    /// </summary>
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