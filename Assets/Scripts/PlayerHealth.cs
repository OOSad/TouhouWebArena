using UnityEngine;
using Unity.Netcode;
using System; // For Action event
using System.Collections;
using Unity.Netcode.Components; // Added for NetworkTransform

[RequireComponent(typeof(AudioSource))] // Ensure AudioSource exists
public class PlayerHealth : NetworkBehaviour
{
    public const int MaxHealth = 5;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.0f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSoundClip; // Assign in inspector

    [Header("Knockback on Hit")]
    [SerializeField] private float knockbackDistance = 0.5f;
    [SerializeField] private float knockbackDuration = 0.2f; 

    // Networked Health: Server has write authority, clients can read.
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        MaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server // Only Server can change health
    );

    // Server-side state for invincibility
    private bool _isInvincible = false;
    private float _invincibilityTimer = 0f;

    // Event for UI updates (invoked on all clients when health changes)
    public event Action<int> OnHealthChanged;

    // Component References
    private AudioSource _audioSource;
    private Rigidbody2D _rigidbody2D; // For knockback movement
    private PlayerMovement _playerMovement; // To disable input
    private NetworkTransform _networkTransform; // To disable during knockback

    void Awake()
    {
        // Cache components
        _audioSource = GetComponent<AudioSource>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _playerMovement = GetComponent<PlayerMovement>();
        _networkTransform = GetComponent<NetworkTransform>(); // Cache NetworkTransform

        if (_audioSource == null) Debug.LogError("PlayerHealth requires an AudioSource component!", this);
        if (_rigidbody2D == null) Debug.LogError("PlayerHealth requires a Rigidbody2D component!", this);
        if (_playerMovement == null) Debug.LogError("PlayerHealth requires a PlayerMovement component!", this);
        if (_networkTransform == null) Debug.LogError("PlayerHealth requires a NetworkTransform component!", this); // Add null check
    }

    public override void OnNetworkSpawn()
    {
        // Reset health on spawn (server-authoritative)
        if (IsServer)
        {
            CurrentHealth.Value = MaxHealth;
            _isInvincible = false;
            _invincibilityTimer = 0f;
        }

        // Subscribe to health changes to invoke the local event for UI
        CurrentHealth.OnValueChanged += HandleHealthChanged;

        // Trigger initial UI update
        HandleHealthChanged(CurrentHealth.Value, CurrentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe to prevent errors
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int previousValue, int newValue)
    {
        // Invoke the event on all clients so UI can react
        OnHealthChanged?.Invoke(newValue);

        // --- Play Hit Sound Locally on Owner --- 
        if (newValue < previousValue) // Play if health decreased (IsOwner check removed)
        { 
            if (_audioSource != null && hitSoundClip != null)
            {
                _audioSource.PlayOneShot(hitSoundClip);
            }
            else if (hitSoundClip == null)
            {
                Debug.LogWarning("PlayerHealth: Hit Sound Clip not assigned!", this);
            }
        }
        // ---------------------------------------

        // --- Add Debug Log ---
        Debug.Log($"[Client {NetworkManager.Singleton?.LocalClientId}] Player {OwnerClientId} health changed from {previousValue} to {newValue}");
        // ---------------------

        // --- Placeholder for Death ---
        // Check for death (server handles actual death logic)
        // if (IsServer && newValue <= 0) {
        //     HandleDeath(); // Implement this later for round loss
        // }
    }

    void Update()
    {
        // Only the server ticks the invincibility timer
        if (!IsServer) return;

        if (_isInvincible)
        {
            _invincibilityTimer -= Time.deltaTime;
            if (_invincibilityTimer <= 0f)
            {
                _isInvincible = false;
                // If player wasn't defeated, and input is still disabled (e.g., knockback just ended)
                // ensure input is re-enabled. This is a fallback.
                // if (CurrentHealth.Value > 0 && _playerMovement != null && !_playerMovement.CanProcessInput.Value)
                // {
                //     _playerMovement.CanProcessInput.Value = true;
                // }
            }
        }
    }

    // This RPC is called by the client owner when they detect a hit
    [ServerRpc(RequireOwnership = true)] // Only owner can request damage for themselves
    public void RequestTakeDamageServerRpc()
    {
        if (!IsServer) return; // Should not happen, but safety check

        TakeDamage(1); // Apply 1 damage from basic bullets
    }

    // Internal method, only called on the server
    private void TakeDamage(int damageAmount)
    {
        if (_isInvincible)
        {
            Debug.Log($"Player {OwnerClientId} hit but invincible.");
            return; // Cannot take damage while invincible
        }

        if (CurrentHealth.Value <= 0)
        {
             Debug.Log($"Player {OwnerClientId} already at 0 health.");
            return; // Already defeated
        }

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - damageAmount);
        Debug.Log($"Player {OwnerClientId} took {damageAmount} damage. Health: {CurrentHealth.Value}");

        // Apply invincibility if still alive
        if (CurrentHealth.Value > 0)
        {
            _isInvincible = true;
            _invincibilityTimer = invincibilityDuration;
            Debug.Log($"Player {OwnerClientId} now invincible for {invincibilityDuration}s.");
             // Optional: Could trigger an RPC here to start flashing visuals if needed

            // --- Start Knockback --- 
            StartKnockback();
            // -----------------------
        }
        else
        {
            // Health reached 0 - trigger death/round loss logic (to be implemented later)
             Debug.Log($"Player {OwnerClientId} defeated!");
             // HandleDeath();
        }
    }

    // --- Knockback Logic (Server Only) ---
    private void StartKnockback()
    {
        if (!IsServer || _playerMovement == null || _rigidbody2D == null) return;

        // 1. Disable player input
        _playerMovement.CanProcessInput.Value = false;
        // --- LOGGING --- 
        Debug.Log($"[Server] Player {OwnerClientId}: StartKnockback initiated. CanProcessInput = {_playerMovement.CanProcessInput.Value}");
        // ---------------

        // 2. Calculate random direction and target position
        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;
        Vector2 startPosition = _rigidbody2D.position; // Use Rigidbody position for calculation start
        Vector2 targetPosition = startPosition + randomDirection * knockbackDistance;

        // TODO: Optional - Clamp targetPosition to player bounds (requires access to PlayerMovement bounds logic)

        // 3. Start the server movement coroutine
        StartCoroutine(KnockbackCoroutine(startPosition, targetPosition));

        // --- ADDED: Trigger visuals on owner client --- 
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
        };
        TriggerLocalKnockbackVisualsClientRpc(targetPosition, knockbackDuration, clientRpcParams);
        // ---------------------------------------------
    }

    private IEnumerator KnockbackCoroutine(Vector2 startPos, Vector2 targetPos)
    {
        float timer = 0f;
        // --- LOGGING --- 
        Debug.Log($"[Server] Player {OwnerClientId}: KnockbackCoroutine started. Moving from {startPos} to {targetPos}");
        // ---------------

        while (timer < knockbackDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / knockbackDuration);
            
            // --- CHANGE: Use transform.position directly --- 
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, progress);
            transform.position = newPos; 
            // --- LOGGING --- 
            Debug.Log($"[Server] Player {OwnerClientId}: KnockbackCoroutine frame. Progress: {progress:F2}, Setting position: {newPos}");
            // ---------------
            
            yield return null; // Wait for next frame
        }

        // --- CHANGE: Ensure final position is set on transform --- 
        transform.position = targetPos; 
        // --- LOGGING --- 
        Debug.Log($"[Server] Player {OwnerClientId}: KnockbackCoroutine finished. Final position: {targetPos}");
        // ---------------

        // 4. Re-enable player input ONLY IF still alive
        if (CurrentHealth.Value > 0 && _playerMovement != null)
        {
            // --- ADDED: Send final position directly to owner client --- 
            ClientRpcParams clientRpcParams = new ClientRpcParams {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[]{ OwnerClientId } } 
            };
            CorrectPositionClientRpc(targetPos, clientRpcParams);
            // ---------------------------------------------------------

            _playerMovement.CanProcessInput.Value = true;
            // --- LOGGING --- 
            Debug.Log($"[Server] Player {OwnerClientId}: KnockbackCoroutine ended. Sent CorrectPositionClientRpc({targetPos}). Re-enabling input. CanProcessInput = {_playerMovement.CanProcessInput.Value}");
            // ---------------
        }
        else
        {
             // No need to correct position if defeated
             // --- LOGGING --- 
             Debug.Log($"[Server] Player {OwnerClientId}: KnockbackCoroutine ended. Player defeated. Input remains disabled.");
             // ---------------
        }

        // TODO: Trigger bullet clearing effect here
    }
    // ----------------------------------

    // --- ADDED ClientRpc ---
    [ClientRpc]
    private void CorrectPositionClientRpc(Vector3 serverPosition, ClientRpcParams clientRpcParams = default)
    {
        // Only the owner client needs to apply this correction
        if (!IsOwner) return;

        // Directly set the position, bypassing local movement logic
        transform.position = serverPosition;
        Debug.Log($"[Client {OwnerClientId}] Received CorrectPositionClientRpc. Set position to {serverPosition}");
    }
    // ----------------------

    // --- ADDED ClientRpc for triggering local visuals ---
    [ClientRpc]
    private void TriggerLocalKnockbackVisualsClientRpc(Vector3 targetPosition, float duration, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;

        Debug.Log($"[Client {OwnerClientId}] Received TriggerLocalKnockbackVisualsClientRpc. Starting local interpolation to {targetPosition} over {duration}s.");
        // Stop any previous visual coroutine just in case
        StopCoroutine(nameof(LocalKnockbackVisualsCoroutine)); 
        StartCoroutine(LocalKnockbackVisualsCoroutine(targetPosition, duration));
    }

    private IEnumerator LocalKnockbackVisualsCoroutine(Vector3 targetPosition, float duration)
    {
        // This coroutine only runs on the owner for smooth visuals
        Vector3 startPos = transform.position;
        float timer = 0f;

        while (timer < duration)
        {
            // Important: Only run if input is still disabled. If server re-enables input mid-coroutine,
            // normal movement should take over.
            // --- REMOVED CHECK --- Let coroutine run its full duration
            // if (_playerMovement != null && !_playerMovement.CanProcessInput.Value) 
            // {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / duration);
                transform.position = Vector3.Lerp(startPos, targetPosition, progress);
            // }
            // else
            // {
            //      Debug.LogWarning($"[Client {OwnerClientId}] LocalKnockbackVisualsCoroutine interrupted: CanProcessInput became true.");
            //      yield break; // Exit if input gets re-enabled early
            // }
            // ---------------------
            yield return null;
        }

        // We rely on CorrectPositionClientRpc for the final position, so this coroutine
        // doesn't necessarily need to set the absolute final position itself.
        Debug.Log($"[Client {OwnerClientId}] LocalKnockbackVisualsCoroutine finished.");
    }
    // -------------------------------------------------

    // --- Add method for resetting health for new rounds later ---
    // [ServerRpc]
    // public void ResetHealthServerRpc() {
    //     if (!IsServer) return;
    //     CurrentHealth.Value = MaxHealth;
    //     _isInvincible = false;
    //     _invincibilityTimer = 0f;
    // }
}
