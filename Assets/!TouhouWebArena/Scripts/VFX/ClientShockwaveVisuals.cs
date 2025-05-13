using UnityEngine;

/// <summary>
/// Manages the visual appearance (scaling, fading) of a client-side shockwave effect.
/// Reads initial state from the SpriteRenderer components on Awake.
/// Provides methods to update the visuals based on the shockwave's expansion progress
/// and to reset the visuals when the object is reused from a pool.
/// </summary>
[RequireComponent(typeof(ClientFairyShockwave), typeof(SpriteRenderer))]
public class ClientShockwaveVisuals : MonoBehaviour
{
    [Tooltip("At what progress point (0-1) the fade-out should begin.")]
    [Range(0f, 1f)]
    [SerializeField] private float fadeStartProgress = 0.5f; // Start fading out at 50% duration

    // Visual properties stored on Awake for true reset
    private Color trueInitialColor;
    private Color trueEndColor;
    private Vector3 trueInitialScale; // Keep Z scale
    // private float trueInitialColliderRadiusForScaling; // REMOVED

    // Component references
    private SpriteRenderer _spriteRenderer;
    // private ClientFairyShockwave _clientShockwave; // No longer needed directly here

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // _clientShockwave = GetComponent<ClientFairyShockwave>(); // No longer needed

        if (_spriteRenderer == null)
        {
            Debug.LogError("[ClientShockwaveVisuals] Missing SpriteRenderer!", this);
            enabled = false;
            return;
        }

        trueInitialColor = _spriteRenderer.color;
        trueEndColor = new Color(trueInitialColor.r, trueInitialColor.g, trueInitialColor.b, 0f); 
        trueInitialScale = transform.localScale; // Store initial scale mainly for Z
        // trueInitialColliderRadiusForScaling = _clientShockwave.GetInitialColliderRadiusForVisuals(); // REMOVED
        // if (trueInitialColliderRadiusForScaling <= 0.001f) trueInitialColliderRadiusForScaling = 0.1f; // REMOVED

        ResetVisuals(); 
    }

    public void ResetVisuals()
    {
        if (_spriteRenderer != null) 
        {
             _spriteRenderer.color = trueInitialColor; 
        }
        transform.localScale = trueInitialScale; // Reset to original scale (including X and Y)
    }

    public void UpdateVisuals(float progress, float currentRadius)
    {
        if (!enabled || _spriteRenderer == null) return;

        // --- NEW Simplified Scaling ---
        // Directly set scale based on current radius, preserving original Z scale.
        float scaleXY = currentRadius > 0.01f ? currentRadius : 0.01f; // Prevent zero/negative scale
        transform.localScale = new Vector3(scaleXY, scaleXY, trueInitialScale.z);
        // --- END Simplified Scaling ---

        // --- Modified Fade Logic ---
        // Stay at full opacity until fadeStartProgress, then fade out from that point to the end.
        if (progress < fadeStartProgress)
        {
            _spriteRenderer.color = trueInitialColor;
        }
        else
        {
            // Calculate fade progress only for the remaining duration
            float fadeDuration = 1f - fadeStartProgress;
            float currentFadeTime = progress - fadeStartProgress;
            float fadeProgress = (fadeDuration > 0.001f) ? Mathf.Clamp01(currentFadeTime / fadeDuration) : 1f; 
            _spriteRenderer.color = Color.Lerp(trueInitialColor, trueEndColor, fadeProgress);
        }
        // --- End Modified Fade Logic ---
    }

    void OnDisable()
    {
        ResetVisuals(); 
    }
} 