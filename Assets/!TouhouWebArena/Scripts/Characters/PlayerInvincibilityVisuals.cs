using UnityEngine;
using Unity.Netcode;
using System.Collections;

// Handles the visual flashing effect when the player is invincible.
[RequireComponent(typeof(PlayerHealth))] // Needs to know the health state
public class PlayerInvincibilityVisuals : MonoBehaviour // Doesn't need NetworkBehaviour directly
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer; // Assign in inspector
    [SerializeField] private float flashInterval = 0.1f;
    [SerializeField] private float flashAlpha = 0.5f; // Transparency during flash

    private Coroutine flashingCoroutine;
    private PlayerHealth playerHealth;

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

    // Called locally when PlayerHealth.IsInvincible changes
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

    private void StartFlashing()
    {
        // Start flashing if not already doing so
        if (flashingCoroutine == null)
        {
            flashingCoroutine = StartCoroutine(FlashSpriteCoroutine());
        }
    }

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