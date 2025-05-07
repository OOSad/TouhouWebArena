using UnityEngine;
using UnityEngine.Events; // Added for OnPathCompleted event

// Keeping SplineLookForward for now, assuming it will also be client-side.
[RequireComponent(typeof(SplineLookForward))] 
/// <summary>
/// [Client-Side] Moves a GameObject along a <see cref="BezierSpline"/> at a constant speed.
/// Handles calculating progress along the spline based on desired speed and curve velocity.
/// Can optionally invoke an event or notify a controller upon reaching the end.
/// Requires initialization via <see cref="InitializePath"/>.
/// </summary>
public class SplineWalker : MonoBehaviour // Changed from NetworkBehaviour
{
    [SerializeField] private BezierSpline _spline; // Renamed for clarity
    [SerializeField] private float moveSpeed = 5f;
    // Removed: [SerializeField] private bool destroyOnComplete = true; 
    // End-of-path action will be handled by listeners or a controller

    [Tooltip("Event triggered when the walker reaches the end of the spline.")]
    public UnityEvent OnPathCompleted;

    private float _progress; // Current position along the spline (0 to 1) - Renamed
    private bool _movingForward = true; // Direction of travel - Renamed
    
    // Removed: private FairyController ownerFairy; 
    // If a controller is needed, it can subscribe to OnPathCompleted or be set via a different mechanism.

    /// <summary>
    /// Gets the <see cref="BezierSpline"/> currently being followed.
    /// </summary>
    public BezierSpline CurrentSpline => _spline; // Use renamed field

    /// <summary>
    /// Gets or sets the current progress along the spline (0 to 1).
    /// Setting this will also update the object's position.
    /// </summary>
    public float NormalizedProgress
    {
        get => _progress;
        set
        {
            _progress = Mathf.Clamp01(value);
            UpdateTransformPosition(_progress);
        }
    }

    /// <summary>
    /// Gets or sets whether the walker is moving towards the end (true) or beginning (false) of the spline.
    /// </summary>
    public bool IsMovingForward
    {
        get => _movingForward;
        set => _movingForward = value;
    }


    void Awake()
    {
        // Removed: ownerFairy related logic
        // Initial state is disabled. Movement starts after InitializePath is called.
        enabled = false; 
    }

    /// <summary>
    /// [Client-Side] Initializes the walker with the spline to follow and the starting direction/progress.
    /// Enables the component to start movement.
    /// </summary>
    /// <param name="newPath">The <see cref="BezierSpline"/> to follow.</param>
    /// <param name="startAtBeginning">True to start at progress 0 and move forward, false to start at progress 1 and move backward.</param>
    public void InitializePath(BezierSpline newPath, bool startAtBeginning)
    {
        this._spline = newPath;
        this._movingForward = startAtBeginning;
        this._progress = startAtBeginning ? 0f : 1f;

        if (_spline != null)
        {
            UpdateTransformPosition(this._progress); // Set initial position
            this.enabled = true; // Enable the component to start the Update loop
        }
        else
        {
            Debug.LogError("[SplineWalker] InitializePath called with a null spline. Disabling.", this);
            this.enabled = false;
        }
    }

    void Update()
    {
        // Removed: if (!IsServer) return;
        if (_spline == null || !this.enabled) return;

        float currentCurveSpeed = _spline.GetVelocity(_progress).magnitude;

        if (currentCurveSpeed <= 0.001f)
        {
            // If at a cusp or very slow part of the curve, avoid division by zero.
            // A small fixed step or alternative logic might be needed if paths often have zero-velocity points.
            // For now, advancing a tiny bit to try and move past it.
            currentCurveSpeed = 0.01f; 
        }

        float delta = (moveSpeed * Time.deltaTime) / currentCurveSpeed;

        bool reachedEnd = false;
        if (_movingForward)
        {
            if (_progress < 1f)
            {
                _progress += delta;
                if (_progress >= 1f)
                {
                    _progress = 1f;
                    reachedEnd = true;
                }
            }
            else { reachedEnd = true; } // Already at or past the end
        }
        else // Moving backward
        {
            if (_progress > 0f)
            {
                _progress -= delta;
                if (_progress <= 0f)
                {
                    _progress = 0f;
                    reachedEnd = true;
                }
            }
            else { reachedEnd = true; } // Already at or past the beginning
        }

        UpdateTransformPosition(_progress);

        if (reachedEnd)
        {
            // Removed: ownerFairy.ReportEndOfPath();
            OnPathCompleted?.Invoke(); // Invoke the UnityEvent
            
            // Disable this component locally to stop further updates.
            // The listener (e.g., a fairy controller) will handle deactivation/pooling.
            this.enabled = false; 
        }
    }

    public Vector3 GetCurrentDirection()
    {
        if (_spline == null) return Vector3.zero;
        
        Vector3 direction = _spline.GetDirection(_progress);
        return _movingForward ? direction : -direction;
    }

    private void UpdateTransformPosition(float currentProgress)
    {
        // Removed: if (!IsServer) return;
        if (_spline == null) return;
        Vector3 position = _spline.GetPoint(currentProgress);
        transform.position = position; 
    }
} 