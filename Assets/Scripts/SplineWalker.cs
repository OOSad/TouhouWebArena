using UnityEngine;
using Unity.Netcode;

// Add reference to Fairy script
[RequireComponent(typeof(Fairy))] // Restored
// Inherit from NetworkBehaviour instead of MonoBehaviour
public class SplineWalker : NetworkBehaviour
{
    [SerializeField] private BezierSpline spline; // The spline to follow
    [SerializeField] private float moveSpeed = 5f; // Constant speed along the spline
    [SerializeField] private bool lookForward = true; // Should the object rotate to face the direction of travel?
    [SerializeField] private bool destroyOnComplete = true; // Destroy the object when it reaches the end?

    private float progress; // Current position along the spline (0 to 1)
    private bool movingForward = true; // Direction of travel
    private Fairy ownerFairy; // Reference to the controlling Fairy script

    void Awake()
    {
        // Get the Fairy component on the same GameObject
        ownerFairy = GetComponent<Fairy>();
        if (ownerFairy == null)
        {
            Debug.LogError("SplineWalker could not find Fairy component!", this);
            this.enabled = false;
        }
        // Start disabled, wait for RPC to initialize and enable
        this.enabled = false;
    }

    // Renamed from SetSpline and made private
    private void InitializeSplineInternal(BezierSpline splineToFollow, bool startAtBeginning)
    {
        this.spline = splineToFollow;
        this.movingForward = startAtBeginning;
        this.progress = startAtBeginning ? 0f : 1f; // Set initial progress based on direction

        // Log spline initialization
        // Optional: Add NetworkObjectId for clarity
        // Commented out verbose log:
        // Debug.Log($"[{NetworkManager.Singleton.LocalClientId} Walker NetId:{NetworkObjectId}] InitializeSplineInternal called. Path: {spline?.name ?? "NULL"}. Enabling component.");

        // Immediately set initial position and rotation
        if (spline != null)
        {
            // Set initial position NOW
            UpdatePositionAndRotation(this.progress);
            // Enable the component to start the Update loop
            this.enabled = true;
        }
        else
        {
            Debug.LogError($"[{NetworkManager.Singleton.LocalClientId} Walker NetId:{NetworkObjectId}] InitializeSplineInternal called with NULL path! Walker remains disabled.");
            this.enabled = false; // Ensure it stays disabled
        }
    }

    // NEW ClientRpc to receive path info from the server
    [ClientRpc]
    public void InitializePathClientRpc(int targetPlayerIndex, int pathIndex, bool startAtBeginning)
    {
        // Log reception of RPC
        // Commented out verbose log:
        // Debug.Log($"[{NetworkManager.Singleton.LocalClientId} Walker NetId:{NetworkObjectId}] Received InitializePathClientRpc. TargetPlayer:{targetPlayerIndex}, PathIdx:{pathIndex}, StartAtBegin:{startAtBeginning}");

        // Client gets the specific path directly from PathManager
        if (PathManager.Instance == null)
        {
            Debug.LogError($"[{NetworkManager.Singleton.LocalClientId} Walker NetId:{NetworkObjectId}] PathManager.Instance is null! Cannot set path.");
            return;
        }

        BezierSpline chosenPath = PathManager.Instance.GetPathByIndex(targetPlayerIndex, pathIndex);

        if (chosenPath == null)
        {
             Debug.LogError($"[{NetworkManager.Singleton.LocalClientId} Walker NetId:{NetworkObjectId}] Could not find path for TargetPlayer:{targetPlayerIndex}, PathIdx:{pathIndex} via PathManager!");
             // Don't call InitializeSplineInternal, component remains disabled
             return;
        }

        // Call the internal method to apply the spline and enable the component
        InitializeSplineInternal(chosenPath, startAtBeginning);
    }

    void Update()
    {
        // LOG ADDED HERE (conditional to avoid spam) - REMOVED
        // if (Time.frameCount % 60 == 0) // Log only once per second approx
        //    Debug.Log($"[{NetworkManager.Singleton.LocalClientId} - Walker {this.GetInstanceID()}] Update running. Spline: {spline?.name ?? "NULL"}. Progress: {progress}");
        
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
             // Alternative: Simply don't move this frame if speed is zero?
             // return;
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
        UpdatePositionAndRotation(progress);

        // If the end was reached *this frame*...
        if (reachedEnd)
        {
            // LOG ADDED HERE - REMOVED
            // Debug.Log($"[{NetworkManager.Singleton.LocalClientId} - Walker {this.GetInstanceID()}] Reached end of path. Disabling self and reporting.");
            // Instead of destroying locally, tell the server via the Fairy script
            if (destroyOnComplete)
            {
                ownerFairy.ReportEndOfPath(); // Call the Fairy's notification method
            }
            // Disable this component locally to stop further updates
            this.enabled = false;
        }
    }

    // Helper method to set position and rotation based on progress
    private void UpdatePositionAndRotation(float currentProgress)
    {
        // Get the position on the spline
        Vector3 position = spline.GetPoint(currentProgress);
        transform.position = position; // Use position directly for 2D

        // Optionally rotate to face the direction of movement
        if (lookForward)
        {
            // Get direction, potentially reversed if moving backward
            Vector3 direction = movingForward ? spline.GetDirection(currentProgress) : -spline.GetDirection(currentProgress);

            if (direction != Vector3.zero) // Avoid zero direction vector
            {
                // For 2D, we usually want to rotate around the Z axis
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                // Assuming sprites face upwards by default, adjust angle by -90
                transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }
        }
    }

    // NEW: Public method for server-side initialization
    public void InitializeOnServer(BezierSpline splineToFollow, bool startAtBeginning)
    {
        // Reuse the internal logic
        InitializeSplineInternal(splineToFollow, startAtBeginning);
    }
} 