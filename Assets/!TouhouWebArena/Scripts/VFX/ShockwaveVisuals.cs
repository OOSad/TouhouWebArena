using UnityEngine;

// Handles the visual scaling and fading of the shockwave effect.
[RequireComponent(typeof(Shockwave), typeof(SpriteRenderer))]
public class ShockwaveVisuals : MonoBehaviour
{
    // Visual properties (could be serialized if needed, but defaults are often fine)
    private Color initialColor;
    private Color endColor;
    private Vector3 initialScale;
    private float initialColliderRadius; // Needed for scaling calculation

    // Component references
    private SpriteRenderer spriteRenderer;
    private Shockwave shockwave; // To potentially get initial collider radius if needed

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        shockwave = GetComponent<Shockwave>(); // Get reference to main script

        if (spriteRenderer == null || shockwave == null)
        {
            Debug.LogError("ShockwaveVisuals is missing required components!", this);
            enabled = false;
            return;
        }

        // Store initial visual state
        initialColor = spriteRenderer.color;
        endColor = new Color(initialColor.r, initialColor.g, initialColor.b, 0f); // Fade out alpha
        initialScale = transform.localScale;
        initialColliderRadius = shockwave.GetInitialRadius(); // Get initial radius from Shockwave
    }

    // Called by Shockwave during its expansion coroutine
    public void UpdateVisuals(float progress, float currentRadius)
    {
        if (!enabled || spriteRenderer == null) return;

        // Calculate scale factor based on current collider radius and initial state
        // Avoid division by zero if initial radius/scale was zero
        float scaleFactor = initialScale.x; // Default to initial scale if calculation fails
        if (initialColliderRadius > 0.001f)
        {
             scaleFactor = currentRadius / (initialColliderRadius / initialScale.x); // Assumes uniform initial scale
        }
        transform.localScale = new Vector3(scaleFactor, scaleFactor, initialScale.z);

        // Fade out sprite based on overall progress (0 to 1)
        spriteRenderer.color = Color.Lerp(initialColor, endColor, progress);
    }

    // Ensure visuals are reset if the component is disabled externally
    void OnDisable()
    {
        // Optional: Reset visuals to default state? 
        // Or assume destruction handles cleanup.
         if (spriteRenderer != null) 
         { 
             spriteRenderer.color = initialColor; 
             transform.localScale = initialScale;
         } 
    }
} 