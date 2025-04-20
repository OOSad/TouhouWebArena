using UnityEngine;

// Rotates the GameObject to face the direction of travel along a SplineWalker's path.
[RequireComponent(typeof(SplineWalker))] // Needs the walker to know the spline and progress
public class SplineLookForward : MonoBehaviour
{
    [SerializeField] private bool lookForwardEnabled = true; // Control whether this behaviour is active
    
    private SplineWalker splineWalker; // Reference to the walker component

    void Awake()
    {
        splineWalker = GetComponent<SplineWalker>();
        if (splineWalker == null)
        {
            Debug.LogError("SplineLookForward requires a SplineWalker component!", this);
            enabled = false; 
        }
    }

    // Use LateUpdate to ensure SplineWalker has updated position/progress in Update
    void LateUpdate()
    {
        if (!lookForwardEnabled || splineWalker == null || splineWalker.Spline == null || !splineWalker.enabled)
        {
            // Don't rotate if disabled, walker is invalid, or walker isn't active
            return; 
        }

        // Get direction from the walker, considering its movement direction
        Vector3 direction = splineWalker.GetCurrentDirection();

        if (direction != Vector3.zero) // Avoid zero direction vector
        {
            // For 2D, we usually want to rotate around the Z axis
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // Assuming sprites face upwards by default, adjust angle by -90
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }
} 