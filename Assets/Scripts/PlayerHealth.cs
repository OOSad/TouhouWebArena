using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections; // Added for Coroutines

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

    private Coroutine flashingCoroutine; // To manage the client-side flashing

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Subscribe to value changes to update UI etc.
        CurrentHealth.OnValueChanged += HandleHealthChanged;
        IsInvincible.OnValueChanged += HandleInvincibilityChanged; // Subscribe to invincibility changes

        // Set initial state based on network variables
        HandleHealthChanged(0, CurrentHealth.Value);
        HandleInvincibilityChanged(false, IsInvincible.Value); // Handle initial invincibility state
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
        IsInvincible.OnValueChanged -= HandleInvincibilityChanged; // Unsubscribe
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
        if (IsInvincible.Value) return; // Check invincibility state HERE
        if (CurrentHealth.Value <= 0) return; // Already dead

        int newHealth = CurrentHealth.Value - amount;
        CurrentHealth.Value = Mathf.Max(newHealth, 0); 

        Debug.Log($"[Server] Player {OwnerClientId} took {amount} damage. Health is now {CurrentHealth.Value}");

        if (CurrentHealth.Value <= 0)
        {
            HandleDeathServer();
        }
        else
        {
            // Trigger invincibility ONLY if damage was taken and player isn't dead
            TriggerInvincibilityServer();
        }
    }

    // Renamed to indicate it's server-only logic trigger
    private void TriggerInvincibilityServer()
    {
        // This logic runs on the server
        // If already invincible (e.g. coroutine running), don't start another one
        if (IsInvincible.Value) return; 
        
        StartCoroutine(ServerInvincibilityTimerCoroutine());
    }

    private IEnumerator ServerInvincibilityTimerCoroutine()
    {
        // This runs ONLY on the server
        IsInvincible.Value = true; // This change propagates to clients via NetworkVariable
        // Debug.Log($"[Server] Player {OwnerClientId} invincibility START");
        yield return new WaitForSeconds(invincibilityDuration);
        IsInvincible.Value = false; // This change also propagates
        // Debug.Log($"[Server] Player {OwnerClientId} invincibility END");
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