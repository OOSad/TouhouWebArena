using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Handles the visual flashing effect of the player's sprite when they are invincible.
/// This component runs locally on all clients, reacting to the <see cref="PlayerHealth.IsInvincible"/> NetworkVariable.
/// It uses a coroutine to rapidly toggle the sprite's alpha value.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(ClientAuthMovement))]
public class PlayerInvincibilityVisuals : MonoBehaviour 
{
    [Header("Visual Settings")]
    [Tooltip("Reference to the main SpriteRenderer of the player character.")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [Tooltip("The time interval (in seconds) between each flash state change.")]
    [SerializeField] private float flashInterval = 0.1f;
    [Tooltip("The alpha value (transparency) the sprite flashes to (0 = fully transparent, 1 = fully opaque).")]
    [SerializeField] private float flashAlpha = 0.5f;

    private Coroutine flashingCoroutine;
    private PlayerHealth playerHealth;
    private ClientAuthMovement clientAuthMovement;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        clientAuthMovement = GetComponent<ClientAuthMovement>();

        if (playerHealth == null)
        {
            Debug.LogError("PlayerInvincibilityVisuals requires PlayerHealth!", this);
            enabled = false;
            return;
        }
        if (clientAuthMovement == null)
        {
            Debug.LogError("PlayerInvincibilityVisuals requires ClientAuthMovement!", this);
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
        if (playerHealth != null)
        {
            playerHealth.IsInvincible.OnValueChanged += HandleInvincibilityChanged;
            HandleInvincibilityChanged(playerHealth.IsInvincible.Value, playerHealth.IsInvincible.Value);
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.IsInvincible.OnValueChanged -= HandleInvincibilityChanged;
        }
        StopFlashing();
    }

    private void HandleInvincibilityChanged(bool previousValue, bool newValue)
    {
        if (newValue == true)
        {
            StartFlashing();
            if (clientAuthMovement != null && clientAuthMovement.IsOwner)
            {
                clientAuthMovement.IsMovementLocked = true;
            }
        }
        else
        {
            StopFlashing();
            if (clientAuthMovement != null && clientAuthMovement.IsOwner)
            {
                clientAuthMovement.IsMovementLocked = false;
            }
        }
    }

    private void StartFlashing()
    {
        if (flashingCoroutine == null && playerSpriteRenderer != null)
        {
            flashingCoroutine = StartCoroutine(FlashSpriteCoroutine());
        }
    }

     private void StopFlashing()
    {
        if (flashingCoroutine != null)
        {
            StopCoroutine(flashingCoroutine);
            flashingCoroutine = null;
        }
        ResetSpriteAlpha();
    }

    private IEnumerator FlashSpriteCoroutine()
    {
        bool showFull = true;
        while (playerHealth != null && playerHealth.IsInvincible.Value)
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
        ResetSpriteAlpha();
        flashingCoroutine = null;
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