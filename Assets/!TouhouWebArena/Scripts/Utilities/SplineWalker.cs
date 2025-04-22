using UnityEngine;
using Unity.Netcode;

// Add reference to Fairy script
[RequireComponent(typeof(Fairy))] // Restored
// --- NEW: Require the look forward component ---
[RequireComponent(typeof(SplineLookForward))] 
// Inherit from NetworkBehaviour instead of MonoBehaviour
public class SplineWalker : NetworkBehaviour
{
    [SerializeField] private BezierSpline spline; // The spline to follow
    [SerializeField] private float moveSpeed = 5f; // Constant speed along the spline
    [SerializeField] private bool destroyOnComplete = true; // Destroy the object when it reaches the end?

    private float progress; // Current position along the spline (0 to 1)
    private bool movingForward = true; // Direction of travel
    private Fairy ownerFairy; // Reference to the controlling Fairy script

    // --- NEW: Public getter for the spline --- 
    public BezierSpline Spline => spline;
    // ----------------------------------------

    void Awake()
    {
        ownerFairy = GetComponentInParent<Fairy>();
        if (ownerFairy == null)
        {
            
            enabled = false;
        }
        // Disable self initially, wait for Fairy script to initialize path
        enabled = false; 
    }

    // This is now called INTERNALLY by the Fairy script after path data is synced
    public void InitializeSplineInternal(BezierSpline chosenPath, bool startAtBeginning)
    {
        this.spline = chosenPath;
        this.movingForward = startAtBeginning;
        this.progress = startAtBeginning ? 0f : 1f; // Set initial progress based on direction

        // Immediately set initial position and rotation - REMOVED POSITION SET
        if (spline != null)
        {
            // Set initial position NOW - REMOVED
            // UpdatePosition(this.progress);
            // Enable the component to start the Update loop
            this.enabled = true;
        }
        else
        {
            
            this.enabled = false; // Ensure it stays disabled
        }
    }

    void Update()
    {
        if (!IsServer) return;
        if (spline == null || ownerFairy == null || !this.enabled) return; // Need spline and fairy

        // Calculate current velocity magnitude on the spline
        float currentSpeed = spline.GetVelocity(progress).magnitude;

        // Avoid division by zero or extremely small speeds
        if (currentSpeed <= 0.001f)
        {
            // If speed is near zero, we can't calculate progress accurately based on it.
            // We could potentially just nudge progress slightly in the correct direction,
            // or handle this as an edge case depending on desired behavior.
            // For now, let's just advance a tiny fixed amount to prevent getting stuck.
            currentSpeed = 0.01f;
        }

        // Calculate progress delta for this frame based on desired moveSpeed and current speed along curve
        float delta = (moveSpeed * Time.deltaTime) / currentSpeed;

        bool reachedEnd = false;
        if (movingForward)
        {
            if (progress < 1f)
            {
                progress += delta;
                if (progress >= 1f)
                {
                    progress = 1f;
                    reachedEnd = true;
                }
            }
            // Handle edge case where progress starts >= 1
            else { reachedEnd = true; }
        }
        else // Moving backward
        {
            if (progress > 0f)
            {
                progress -= delta;
                if (progress <= 0f)
                {
                    progress = 0f;
                    reachedEnd = true;
                }
            }
            // Handle edge case where progress starts <= 0
            else { reachedEnd = true; }
        }

        // Always update position, even on the frame it reaches the end
        UpdatePosition(progress);

        // If the end was reached *this frame*...
        if (reachedEnd)
        {
            // Instead of destroying locally, tell the server via the Fairy script
            if (destroyOnComplete)
            {
                ownerFairy.ReportEndOfPath(); // Call the Fairy's notification method
            }
            // Disable this component locally to stop further updates
            this.enabled = false;
        }
    }

    // --- NEW: Public method to get current direction ---
    public Vector3 GetCurrentDirection()
    {
        if (spline == null) return Vector3.zero;
        
        Vector3 direction = spline.GetDirection(progress);
        return movingForward ? direction : -direction;
    }
    // -------------------------------------------------

    // --- RENAMED and MODIFIED: Only updates position ---
    private void UpdatePosition(float currentProgress)
    {
        if (!IsServer) return;
        if (spline == null) return; // Safety check
        // Get the position on the spline
        Vector3 position = spline.GetPoint(currentProgress);
        transform.position = position; 
    }
} 