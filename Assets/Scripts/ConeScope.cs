using UnityEngine;

// Manages the expanding cone scope visual (scales length and width)
public class ConeScope : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GameObject containing the scope's SpriteRenderer.")]
    [SerializeField] private GameObject scopeVisual;

    [Header("Expansion Settings")]
    [SerializeField] private float maxLength = 8f; // Max length/height of the cone
    [SerializeField] private float maxWidth = 1.5f; // Max width of the cone at full length
    [SerializeField] private float expansionSpeed = 6f; // Units per second (for length)
    [SerializeField] private float initialLength = 0.2f; // Start small
    [SerializeField] private float initialWidth = 0.1f; // Start narrow

    private bool isExpanding = false;
    private float currentLength = 0f;
    // No need to track currentWidth separately, calculate based on length
    private Vector3 initialLocalScale;
    // Removed baseWidthScale, we calculate dynamically

    void Awake()
    {
         if (scopeVisual == null)
        {
            Debug.LogError("ConeScope: Scope Visual is not assigned!", this);
            enabled = false;
            return;
        }
        // Store the initial scale based on defined initial width/length
        initialLocalScale = new Vector3(initialWidth, initialLength, 1f);
        scopeVisual.transform.localScale = initialLocalScale;
        scopeVisual.SetActive(false);
    }

    void Update()
    {
        if (isExpanding)
        {
            // Expand length over time, clamped to maxLength
            currentLength = Mathf.MoveTowards(currentLength, maxLength, expansionSpeed * Time.deltaTime);

            // Calculate current width based on length progress
            // Lerp between initialWidth and maxWidth based on how close currentLength is to maxLength
            // Avoid division by zero if maxLength is very small or equal to initialLength
            float lengthProgress = (maxLength - initialLength) > 0.01f ? 
                                      Mathf.Clamp01((currentLength - initialLength) / (maxLength - initialLength)) :
                                      1f; // If no length change, assume full width immediately
                                      
            float currentWidth = Mathf.Lerp(initialWidth, maxWidth, lengthProgress);

            // Update visual scale (affecting both X and Y)
            scopeVisual.transform.localScale = new Vector3(currentWidth, currentLength, 1f);
        }
    }

     // Called by FocusModeController to start expanding
    public void Activate()
    {
        if (scopeVisual == null) return;

        currentLength = initialLength; // Reset length
        scopeVisual.transform.localScale = initialLocalScale; // Reset scale
        scopeVisual.SetActive(true);
        isExpanding = true;
    }

    // Called by FocusModeController to stop expanding and hide
    public void Deactivate()
    {
         if (scopeVisual == null) return;

        isExpanding = false;
        scopeVisual.SetActive(false);
    }
}
