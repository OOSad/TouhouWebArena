using UnityEngine;
using Unity.Netcode;

// Handles receiving path info and initializing the SplineWalker for a Fairy
[RequireComponent(typeof(Fairy), typeof(SplineWalker))] // Requires Fairy and SplineWalker
public class FairyPathInitializer : NetworkBehaviour
{
    // --- Path Initialization NetworkVariables ---
    private NetworkVariable<int> pathOwnerPlayerIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> pathIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> startAtBeginning = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // ------------------------------------------------

    // References
    private SplineWalker splineWalker;
    private bool pathInitialized = false; // Prevent double initialization

    void Awake()
    {
        splineWalker = GetComponent<SplineWalker>();
        if (splineWalker == null)
        {
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to path variable changes to initialize walker on all clients (and host/server)
        pathOwnerPlayerIndex.OnValueChanged += OnPathInfoChanged;
        pathIndex.OnValueChanged += OnPathInfoChanged;
        startAtBeginning.OnValueChanged += OnPathInfoChanged;

        // Attempt initial initialization immediately in case variables were set before spawn completed
        TryInitializePath();
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe
        pathOwnerPlayerIndex.OnValueChanged -= OnPathInfoChanged;
        pathIndex.OnValueChanged -= OnPathInfoChanged;
        startAtBeginning.OnValueChanged -= OnPathInfoChanged;
        base.OnNetworkDespawn();
    }

    // Public method for the Spawner (via Fairy script) to set path info on the Server
    public void SetPathInfoOnServer(int ownerIndex, int pIndex, bool startAtBegin)
    {
        if (!IsServer)
        {
            return;
        }
        pathOwnerPlayerIndex.Value = ownerIndex;
        pathIndex.Value = pIndex;
        startAtBeginning.Value = startAtBegin;

        // Attempt initialization immediately on server as well
        // TryInitializePath(); // NetworkVariable callbacks will handle this
    }

    // Called when any of the path NetworkVariables change
    private void OnPathInfoChanged(int previousValue, int newValue) => TryInitializePath();
    private void OnPathInfoChanged(bool previousValue, bool newValue) => TryInitializePath();

    // Tries to initialize the path using the NetworkVariables
    private void TryInitializePath()
    {
        // Only initialize once, and only if we have valid indices and splineWalker
        if (pathInitialized || splineWalker == null || pathOwnerPlayerIndex.Value < 0 || pathIndex.Value < 0) return;

        if (PathManager.Instance == null)
        {
            // Logged error, maybe retry later? Or maybe PathManager needs earlier initialization
            return;
        }

        BezierSpline chosenPath = PathManager.Instance.GetPathByIndex(pathOwnerPlayerIndex.Value, pathIndex.Value);

        if (chosenPath == null)
        {
             return;
        }

        // Initialize the SplineWalker locally
        splineWalker.InitializeSplineInternal(chosenPath, startAtBeginning.Value);
        pathInitialized = true; // Mark as initialized
    }
} 