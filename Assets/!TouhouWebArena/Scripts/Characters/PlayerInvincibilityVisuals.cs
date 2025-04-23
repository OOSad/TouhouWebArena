using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Handles the visual flashing effect of the player's sprite when they are invincible.
/// This component runs locally on all clients, reacting to the <see cref="PlayerHealth.IsInvincible"/> NetworkVariable.
/// It uses a coroutine to rapidly toggle the sprite's alpha value.
/// </summary>
[RequireComponent(typeof(PlayerHealth))] // Needs to access the IsInvincible state.
public class PlayerInvincibilityVisuals : MonoBehaviour // No NetworkBehaviour needed; driven by PlayerHealth's NetworkVariable.
{
    [Header("Visual Settings")]
    [Tooltip("Reference to the main SpriteRenderer of the player character.")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [Tooltip("The time interval (in seconds) between each flash state change.")]
    [SerializeField] private float flashInterval = 0.1f;
    [Tooltip("The alpha value (transparency) the sprite flashes to (0 = fully transparent, 1 = fully opaque).")]
    [SerializeField] private float flashAlpha = 0.5f;

    /// <summary>Reference to the currently running flashing coroutine, if any.</summary>
    private Coroutine flashingCoroutine;
    /// <summary>Cached reference to the PlayerHealth component.</summary>
    private PlayerHealth playerHealth;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Caches references and validates that required components/references exist.
    /// </summary>
    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
             Debug.LogError("PlayerInvincibilityVisuals requires PlayerHealth!", this);
             enabled = false;
             return;
        }
         if (playerSpriteRenderer == null)
        {
             Debug.LogError("PlayerInvincibilityVisuals requires PlayerSpriteRenderer to be assigned!", this);
             enabled = false;
             return;
        }
    }

    /// <summary>
    /// Called when the component becomes enabled and active.
    /// Subscribes to the <see cref="PlayerHealth.IsInvincible"/> NetworkVariable's value changed event.
    /// Also applies the initial visual state based on the current value of IsInvincible.
    /// </summary>
    void OnEnable()
    {
        // Subscribe to the health component's NetworkVariable change event
        if (playerHealth != null)
        {
            playerHealth.IsInvincible.OnValueChanged += HandleInvincibilityChanged;
            // Trigger initial state check in case IsInvincible was true before this script enabled/spawned
             HandleInvincibilityChanged(false, playerHealth.IsInvincible.Value);
        }
    }

    /// <summary>
    /// Called when the component becomes disabled or inactive.
    /// Unsubscribes from the <see cref="PlayerHealth.IsInvincible"/> event.
    /// Ensures any active flashing effect is stopped (<see cref="StopFlashing"/>).
    /// </summary>
    void OnDisable()
    {
        // Unsubscribe
        if (playerHealth != null)
        {
            playerHealth.IsInvincible.OnValueChanged -= HandleInvincibilityChanged;
        }
        // Ensure flashing stops if disabled
         StopFlashing();
    }

    /// <summary>
    /// Callback handler executed on all clients when <see cref="PlayerHealth.IsInvincible"/> changes.
    /// Starts or stops the flashing effect based on the new invincibility state.
    /// </summary>
    /// <param name="previousValue">The previous value of IsInvincible.</param>
    /// <param name="newValue">The new (current) value of IsInvincible.</param>
    private void HandleInvincibilityChanged(bool previousValue, bool newValue)
    {
        if (newValue == true)
        {
            StartFlashing();
        }
        else
        {
            StopFlashing();
        }
    }

    /// <summary>
    /// Starts the sprite flashing effect if it's not already running.
    /// Initiates the <see cref="FlashSpriteCoroutine"/>.
    /// </summary>
    private void StartFlashing()
    {
        // Start flashing if not already doing so
        if (flashingCoroutine == null)
        {
            flashingCoroutine = StartCoroutine(FlashSpriteCoroutine());
        }
    }

    /// <summary>
    /// Stops the sprite flashing effect if it is currently running.
    /// Stops the <see cref="FlashSpriteCoroutine"/> and resets the sprite's alpha using <see cref="ResetSpriteAlpha"/>.
    /// </summary>
     private void StopFlashing()
    {
         // Stop flashing and reset alpha
        if (flashingCoroutine != null)
        {
            StopCoroutine(flashingCoroutine);
            flashingCoroutine = null;
        }
        ResetSpriteAlpha();
    }

    /// <summary>
    /// Coroutine that handles the continuous flashing effect.
    /// Runs indefinitely while active, toggling the <see cref="playerSpriteRenderer"/>'s alpha
    /// between 1.0f and <see cref="flashAlpha"/> every <see cref="flashInterval"/> seconds.
    /// Relies on <see cref="StopFlashing"/> to be terminated.
    /// </summary>
    /// <returns>An IEnumerator for the coroutine.</returns>
    private IEnumerator FlashSpriteCoroutine()
    {
        // This runs locally on each client while this component is active and should be flashing
        bool showFull = true;
        // No need to check IsInvincible.Value here, OnDisable/HandleInvincibilityChanged handle stopping
        while (true)
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
    }

    /// <summary>
    /// Resets the <see cref="playerSpriteRenderer"/>'s alpha value back to fully opaque (1.0f).
    /// </summary>
    private void ResetSpriteAlpha()
    {
        if (playerSpriteRenderer != null)
        {
            Color color = playerSpriteRenderer.color;
            color.a = 1.0f;
            playerSpriteRenderer.color = color;
        }
    }
} 