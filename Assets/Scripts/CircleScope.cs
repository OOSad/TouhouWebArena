using UnityEngine;

// Manages the expanding circular scope visual
public class CircleScope : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The GameObject containing the scope's SpriteRenderer.")]
    [SerializeField] private GameObject scopeVisual;

    [Header("Expansion Settings")]
    [SerializeField] private float maxRadius = 5f;
    [SerializeField] private float expansionSpeed = 4f; // Units per second
    [SerializeField] private float initialScale = 0.1f; // Start small

    private bool isExpanding = false;
    private float currentRadius = 0f;
    private Vector3 initialLocalScale;

    void Awake()
    {
        if (scopeVisual == null)
        {
            Debug.LogError("CircleScope: Scope Visual is not assigned!", this);
            enabled = false;
            return;
        }
        // Assuming the sprite visual's base size represents a 1-unit radius circle
        // Adjust initialLocalScale if your sprite's base size is different.
        initialLocalScale = new Vector3(initialScale * 2, initialScale * 2, 1f);
        scopeVisual.transform.localScale = initialLocalScale;
        scopeVisual.SetActive(false);
    }

    void Update()
    {
        if (isExpanding)
        {
            // Expand radius over time, clamped to maxRadius
            currentRadius = Mathf.MoveTowards(currentRadius, maxRadius, expansionSpeed * Time.deltaTime);

            // Update visual scale (assuming base sprite is 1 unit radius)
            // We scale by diameter (Radius * 2)
            scopeVisual.transform.localScale = new Vector3(currentRadius * 2f, currentRadius * 2f, 1f);
        }
    }

    // Called by FocusModeController to start expanding
    public void Activate()
    {
        if (scopeVisual == null) return;

        currentRadius = initialScale; // Reset radius to initial size
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
        // Optionally reset currentRadius here if needed immediately,
        // but Activate already does it.
    }
}
