using UnityEngine;
using Unity.Netcode;
// using Unity.Netcode.Components; // Removed - No longer directly manipulating NT
using System;
using System.Collections; // Added for Coroutines
// using System.Collections.Generic; // Removed - No longer using List

public class PlayerHealth : NetworkBehaviour
{
    public const int MaxHealth = 5;

    // Synchronize health from server to clients. Only server can write.
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(MaxHealth, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Event to notify UI or other systems about health changes
    public event Action<int> OnHealthChanged;
    // Event to notify game state manager about death (server-side)
    public static event Action<ulong> OnPlayerDeathServer; 

    [Header("Invincibility Settings")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer; // Assign in inspector
    [SerializeField] private float invincibilityDuration = 2f;
    [SerializeField] private float flashInterval = 0.1f;
    [SerializeField] private float flashAlpha = 0.5f; // Transparency during flash

    // NetworkVariable to sync invincibility state (server writes, everyone reads)
    public NetworkVariable<bool> IsInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Coroutine flashingCoroutine;
    private PlayerDeathBomb playerDeathBomb; // Reference to the bomb component
    // Removed PlayerMovement reference as it's not needed for this simplified version
    // private PlayerMovement playerMovement;
    // Removed isServerKnockingBack flag and property
    // private bool isServerKnockingBack = false; 
    // public bool IsServerKnockingBack => isServerKnockingBack;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CurrentHealth.OnValueChanged += HandleHealthChanged;
        IsInvincible.OnValueChanged += HandleInvincibilityChanged;
        // Removed ServerHasMovementControl subscription
        // ServerHasMovementControl.OnValueChanged += HandleMovementControlChanged;

        // Get the death bomb component
        playerDeathBomb = GetComponent<PlayerDeathBomb>();
        if (playerDeathBomb == null)
        {
            Debug.LogWarning("PlayerHealth could not find PlayerDeathBomb component! Bomb effect will not work.", this);
        }

        // Removed playerMovement Get component
        // playerMovement = GetComponent<PlayerMovement>(); 
        // if (playerMovement == null) { Debug.LogError(...); }

        HandleHealthChanged(0, CurrentHealth.Value);
        HandleInvincibilityChanged(false, IsInvincible.Value);
        // Removed initial HandleMovementControlChanged call
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
        IsInvincible.OnValueChanged -= HandleInvincibilityChanged;
        // Removed ServerHasMovementControl unsubscription
    }

    private void HandleHealthChanged(int previousValue, int newValue)
    {
        // Invoke the event whenever health changes (clients will react here for UI)
        OnHealthChanged?.Invoke(newValue);

        // Optional: Client-side effects like flashing sprite could be triggered here
        // Debug.Log($"Client {OwnerClientId} health changed to {newValue}");
    }

    // Called on all clients when IsInvincible changes
    private void HandleInvincibilityChanged(bool previousValue, bool newValue)
    {
        // Debug.Log($"Client {OwnerClientId} invincibility changed to: {newValue}");
        if (playerSpriteRenderer == null) return;

        if (newValue == true)
        {
            // Start flashing if not already doing so
            if (flashingCoroutine == null)
            {
                flashingCoroutine = StartCoroutine(FlashSpriteCoroutine());
            }
        }
        else
        {            
            // Stop flashing and reset alpha
            if (flashingCoroutine != null)
            {
                StopCoroutine(flashingCoroutine);
                flashingCoroutine = null;
            }
            ResetSpriteAlpha();
        }
    }

    private IEnumerator FlashSpriteCoroutine()
    {
        // This runs locally on each client while IsInvincible is true
        bool showFull = true;
        while (IsInvincible.Value) // Loop based on the NetworkVariable state
        {
            if (playerSpriteRenderer != null)
            {
                Color color = playerSpriteRenderer.color;
                color.a = showFull ? 1.0f : flashAlpha;
                playerSpriteRenderer.color = color;
            }
            showFull = !showFull;
            yield return new WaitForSeconds(flashInterval);
        }
        // Ensure alpha is reset when loop finishes (e.g., if IsInvincible becomes false)
        ResetSpriteAlpha(); 
        flashingCoroutine = null; // Mark as stopped
    }

    private void ResetSpriteAlpha()
    {
        if (playerSpriteRenderer != null)
        {
            Color color = playerSpriteRenderer.color;
            color.a = 1.0f;
            playerSpriteRenderer.color = color;
        }
    }

    // Call this method from PlayerHitbox on the server
    public void TakeDamage(int amount)
    {
        if (!IsServer) return; 
        if (IsInvincible.Value) return;
        if (CurrentHealth.Value <= 0) return;

        int newHealth = CurrentHealth.Value - amount;
        CurrentHealth.Value = Mathf.Max(newHealth, 0);
        Debug.Log($"[Server] Player {OwnerClientId} took {amount} damage. Health is now {CurrentHealth.Value}");

        if (CurrentHealth.Value <= 0)
        {
            HandleDeathServer();
        }
        else
        {
            TriggerInvincibilityServer(); // Only triggers invincibility timer now
        }
    }

    // Renamed to indicate it's server-only logic trigger
    private void TriggerInvincibilityServer()
    {
        if (!IsServer) return;
        if (IsInvincible.Value) return;

        StartCoroutine(ServerInvincibilityTimerCoroutine());
        // Removed TriggerDeathBombServer() call from here
        // TriggerDeathBombServer(); 
    }

    private IEnumerator ServerInvincibilityTimerCoroutine()
    {
        IsInvincible.Value = true; 
        yield return new WaitForSeconds(invincibilityDuration);
        
        // --- Trigger Bomb Effect via Separate Component --- 
        if (playerDeathBomb != null)
        {
            playerDeathBomb.ExecuteBomb(); // Call the method on the other component
        }
        else
        {
            Debug.LogError("[Server PlayerHealth] Cannot execute bomb, PlayerDeathBomb component missing!", this);
        }
        // -------------------------------------------------------

        IsInvincible.Value = false;
    }

    private void HandleDeathServer()
    {
        // This runs ONLY on the server when health reaches 0
        Debug.Log($"[Server] Player {OwnerClientId} has died.");
        
        // Invoke server-side event for game manager to handle round end/scoring
        OnPlayerDeathServer?.Invoke(OwnerClientId);

        // Optional: Could trigger a death animation/effect via ClientRpc here
        // DieClientRpc(); 
    }

    // Example: Reset health (e.g., called by a game manager at round start)
    [ServerRpc(RequireOwnership = false)] // Allow server to call this on player objects
    public void ResetHealthServerRpc()
    {
        Debug.Log($"[Server] Resetting health for Player {OwnerClientId}");
        CurrentHealth.Value = MaxHealth;
    }
} 