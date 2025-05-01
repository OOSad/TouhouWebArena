using UnityEngine;
using Unity.Netcode;
using System;
using TouhouWebArena; // For PlayerRole

/// <summary>
/// [Server Only] Manages the health state (<see cref="currentHealth"/> NetworkVariable) of a Fairy.
/// Handles health initialization based on serialized fields (<see cref="initialMaxHealth"/>, <see cref="isGreatFairy"/>),
/// provides methods for server-side damage application (<see cref="ApplyDamageFromServer"/>, <see cref="ApplyDamageFromRpc"/>, <see cref="ApplyLethalDamage"/>),
/// and notifies listeners via the <see cref="OnDeath"/> event when health reaches zero.
/// </summary>
[RequireComponent(typeof(NetworkObject))] // Health is networked
public class FairyHealth : NetworkBehaviour
{
    [Header("Health Configuration")]
    [SerializeField] 
    [Tooltip("The maximum health the fairy starts with (before applying Great Fairy modifier).")]
    private int initialMaxHealth = 1;
    [SerializeField] 
    [Tooltip("If true, the fairy gets 3 max health instead of initialMaxHealth.")]
    private bool isGreatFairy = false;

    // --- State ---
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Flag to prevent OnDeath event from firing multiple times
    private bool hasDied = false; 

    /// <summary>
    /// [Server Only] Event triggered on the server when the fairy's health reaches zero.
    /// Passes the <see cref="PlayerRole"/> of the entity credited with the kill.
    /// Subscribed to by <see cref="FairyController"/> to initiate the death sequence.
    /// </summary>
    public event Action<PlayerRole> OnDeath;

    // --- Initialization ---

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            InitializeHealth();
        }
        // Reset death flag on spawn/respawn
        hasDied = false; 
    }

    /// <summary>
    /// [Server Only] Sets the initial health based on <see cref="isGreatFairy"/> flag and <see cref="initialMaxHealth"/>.
    /// Also resets the <see cref="hasDied"/> flag.
    /// </summary>
    private void InitializeHealth()
    {
        int maxHealth = isGreatFairy ? 3 : initialMaxHealth;
        currentHealth.Value = maxHealth;
        hasDied = false; // Ensure reset when explicitly initialized
    }

    // --- Public Accessors ---

    /// <summary>
    /// Checks if the fairy is currently considered alive (health > 0).
    /// Note: This might return true briefly after health drops to 0 but before the OnDeath event fully processes subscribers.
    /// </summary>
    /// <returns>True if <see cref="currentHealth"/> value is greater than zero, false otherwise.</returns>
    public bool IsAlive()
    {
        // Primary check is health > 0
        return currentHealth.Value > 0;
    }
    
    /// <summary>
    /// Gets the current health value from the <see cref="currentHealth"/> NetworkVariable.
    /// </summary>
    /// <returns>The current health.</returns>
    public int GetCurrentHealth()
    {
        return currentHealth.Value;
    }

    // --- Damage Application (Server-Side) ---

    /// <summary>
    /// [Server Only] Internal logic to apply damage and check for death condition.
    /// Decrements <see cref="currentHealth"/>. If health drops to 0 or below and <see cref="hasDied"/> is false,
    /// sets <see cref="hasDied"/> to true and invokes the <see cref="OnDeath"/> event.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    /// <param name="killerRole">The role credited if this damage is lethal.</param>
    private void ApplyDamageInternal(int amount, PlayerRole killerRole)
    {
        if (!IsServer || hasDied || currentHealth.Value <= 0) return;

        currentHealth.Value -= amount;

        // Check if health dropped to 0 or below and death hasn't been triggered yet
        if (currentHealth.Value <= 0 && !hasDied)
        {
            hasDied = true;
            OnDeath?.Invoke(killerRole);
        }
    }

    /// <summary>
    /// [Server Only] Applies damage received from a client RPC via <see cref="FairyController.TakeDamageServerRpc"/>.
    /// Calls <see cref="ApplyDamageInternal"/>.
    /// </summary>
    /// <param name="amount">Amount of damage.</param>
    /// <param name="killerRole">Role credited with the kill.</param>
    public void ApplyDamageFromRpc(int amount, PlayerRole killerRole)
    {
        ApplyDamageInternal(amount, killerRole);
    }

    /// <summary>
    /// [Server Only] Applies damage directly from a server source (e.g., collision processed on server).
    /// Calls <see cref="ApplyDamageInternal"/>.
    /// </summary>
    /// <param name="amount">Amount of damage.</param>
    /// <param name="killerRole">Role credited with the kill.</param>
    public void ApplyDamageFromServer(int amount, PlayerRole killerRole)
    {
        ApplyDamageInternal(amount, killerRole);
    }

    /// <summary>
    /// [Server Only] Applies lethal damage, bypassing normal health checks. 
    /// Sets <see cref="currentHealth"/> to 0. If <see cref="hasDied"/> is false,
    /// sets <see cref="hasDied"/> to true and triggers the <see cref="OnDeath"/> event.
    /// Useful for effects that should instantly kill fairies (e.g., bombs, chain reactions, path end).
    /// </summary>
    /// <param name="killerRole">The <see cref="PlayerRole"/> attributed to the kill.</param>
    public void ApplyLethalDamage(PlayerRole killerRole)
    {
        if (!IsServer || hasDied) return;

        currentHealth.Value = 0;
        hasDied = true;
        OnDeath?.Invoke(killerRole);
    }
} 