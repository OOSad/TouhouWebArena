using UnityEngine;

/// <summary>
/// Manages the visual appearance (scaling, fading) of a <see cref="Shockwave"/> effect.
/// Reads initial state from the SpriteRenderer and Shockwave components on Awake.
/// Provides methods to update the visuals based on the shockwave's expansion progress
/// and to reset the visuals when the object is reused from a pool.
/// </summary>
//[RequireComponent(typeof(Shockwave), typeof(SpriteRenderer))]
public class ShockwaveVisuals : MonoBehaviour
{
    // Visual properties stored on Awake for true reset
    private Color trueInitialColor;
    private Color trueEndColor;
    private Vector3 trueInitialScale;
    private float trueInitialColliderRadius; // Needed for scaling calculation

    // --- Temporary state variables used during update --- 
    // We keep these separate so ResetVisuals can use the true defaults
    private Color currentInitialColor; 
    private Color currentEndColor;
    private Vector3 currentInitialScale;
    private float currentInitialColliderRadius;
    // --------------------------------------------------

    // Component references
    private SpriteRenderer spriteRenderer;
    private Shockwave shockwave; // To potentially get initial collider radius if needed

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        shockwave = GetComponent<Shockwave>(); // Get reference to main script

        if (spriteRenderer == null || shockwave == null)
        {
            enabled = false;
            return;
        }

        // Store TRUE initial visual state from prefab defaults
        trueInitialColor = spriteRenderer.color;
        trueEndColor = new Color(trueInitialColor.r, trueInitialColor.g, trueInitialColor.b, 0f); // Fade out alpha
        trueInitialScale = transform.localScale;
        trueInitialColliderRadius = shockwave.GetInitialRadius(); // Get initial radius from Shockwave

        // Initialize current state to true defaults
        ResetVisuals(); 
    }

    // --- Public method to reset visual state for pooling ---
    /// <summary>
    /// Resets the visual state (color, scale) to the initial values captured on Awake.
    /// Should be called when the shockwave object is obtained from a pool before reuse.
    /// </summary>
    public void ResetVisuals()
    {
        // Reset current state variables to the true defaults captured in Awake
        currentInitialColor = trueInitialColor;
        currentEndColor = trueEndColor;
        currentInitialScale = trueInitialScale;
        currentInitialColliderRadius = trueInitialColliderRadius;

        // Immediately apply the reset state visually using true defaults
        if (spriteRenderer != null) // Add null check for safety
        {
             spriteRenderer.color = trueInitialColor; 
        }
        transform.localScale = trueInitialScale; 
    }
    // ----------------------------------------------------

    /// <summary>
    /// Updates the shockwave's visual scale and color based on expansion progress.
    /// Calculates scale based on the current collider radius and initial radius/scale.
    /// Lerps the color from its initial value to fully transparent based on progress.
    /// Called by the main <see cref="Shockwave"/> script during its Update loop.
    /// </summary>
    /// <param name="progress">The normalized progress of the shockwave expansion (0 to 1).</param>
    /// <param name="currentRadius">The current radius of the shockwave's collider.</param>
    public void UpdateVisuals(float progress, float currentRadius)
    {
        if (!enabled || spriteRenderer == null) return;

        // Calculate scale factor based on current collider radius and initial state
        // Use the 'currentInitial...' variables which are reset by ResetVisuals()
        float scaleFactor = currentInitialScale.x; 
        if (currentInitialColliderRadius > 0.001f)
        {
             // Calculate scale relative to the *true* initial collider radius and scale
             scaleFactor = currentRadius / (trueInitialColliderRadius / trueInitialScale.x); 
        }
        // Ensure scale doesn't become invalid if calculation fails
        if (float.IsNaN(scaleFactor) || float.IsInfinity(scaleFactor))
        {
             scaleFactor = currentInitialScale.x; // Fallback to initial scale
        }
        transform.localScale = new Vector3(scaleFactor, scaleFactor, currentInitialScale.z);

        // Fade out sprite based on overall progress (0 to 1)
        // Use the 'currentInitial...' variables for lerping
        spriteRenderer.color = Color.Lerp(currentInitialColor, currentEndColor, progress);
    }

    // Ensure visuals are reset if the component is disabled externally
    void OnDisable()
    {
        // Now call the common reset method
        // This ensures it resets to prefab defaults even if deactivated mid-animation
        ResetVisuals(); 
    }
} 