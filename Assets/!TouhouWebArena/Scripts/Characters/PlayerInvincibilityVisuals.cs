using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Handles the visual flashing effect of the player's sprite when they are invincible.
/// This component runs locally on all clients, reacting to the <see cref="PlayerHealth.IsInvincible"/> NetworkVariable.
/// It uses a coroutine to rapidly toggle the sprite's alpha value.
/// </summary>
// [RequireComponent(typeof(PlayerHealth))] // TEMPORARILY COMMENTED OUT FOR TESTING
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
    // private PlayerHealth playerHealth; // TEMPORARILY COMMENTED OUT FOR TESTING

    void Awake()
    {
        // playerHealth = GetComponent<PlayerHealth>(); // TEMPORARILY COMMENTED OUT FOR TESTING
        // if (playerHealth == null) // TEMPORARILY COMMENTED OUT FOR TESTING
        // { // TEMPORARILY COMMENTED OUT FOR TESTING
        //     Debug.LogError("PlayerInvincibilityVisuals requires PlayerHealth!", this); // TEMPORARILY COMMENTED OUT FOR TESTING
        //     enabled = false; // TEMPORARILY COMMENTED OUT FOR TESTING
        //     return; // TEMPORARILY COMMENTED OUT FOR TESTING
        // } // TEMPORARILY COMMENTED OUT FOR TESTING
         if (playerSpriteRenderer == null)
        {
             Debug.LogError("PlayerInvincibilityVisuals requires PlayerSpriteRenderer to be assigned!", this);
             enabled = false;
             return;
        }
    }

    void OnEnable()
    {
        // TEMPORARILY COMMENTED OUT FOR TESTING
        // if (playerHealth != null)
        // {
        //     playerHealth.IsInvincible.OnValueChanged += HandleInvincibilityChanged;
        //     HandleInvincibilityChanged(false, playerHealth.IsInvincible.Value);
        // }
    }

    void OnDisable()
    {
        // TEMPORARILY COMMENTED OUT FOR TESTING
        // if (playerHealth != null)
        // {
        //     playerHealth.IsInvincible.OnValueChanged -= HandleInvincibilityChanged;
        // }
        StopFlashing(); // Still good to stop flashing if disabled
    }

    private void HandleInvincibilityChanged(bool previousValue, bool newValue)
    {
        // This method will not be called if the event subscription is commented out.
        // if (newValue == true)
        // {
        //     StartFlashing();
        // }
        // else
        // {
        //     StopFlashing();
        // }
    }

    private void StartFlashing()
    {
        if (flashingCoroutine == null && playerSpriteRenderer != null) // Added null check for safety
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