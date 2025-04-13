using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Simple script to hold references to paths, assignable in the Inspector.
// Doesn't need to be a NetworkBehaviour.
public class PathManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static PathManager Instance { get; private set; }

    [SerializeField] private GameObject player1PathsParent;
    [SerializeField] private GameObject player2PathsParent;

    private List<BezierSpline> player1Paths = new List<BezierSpline>();
    private List<BezierSpline> player2Paths = new List<BezierSpline>();

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Don't destroy this when loading scenes if paths are in the main gameplay scene
            // DontDestroyOnLoad(gameObject); // Optional: Use if needed
        }

        // Populate path lists from assigned parents
        PopulatePaths();
    }

    private void PopulatePaths()
    {
        player1Paths.Clear();
        if (player1PathsParent != null)
        {
            player1Paths = player1PathsParent.GetComponentsInChildren<BezierSpline>(true).ToList();
            Debug.Log($"PathManager found {player1Paths.Count} paths for Player 1.");
        }
        else
        {
            Debug.LogError("PathManager: Player 1 Paths Parent not assigned!");
        }

        player2Paths.Clear();
        if (player2PathsParent != null)
        {
            player2Paths = player2PathsParent.GetComponentsInChildren<BezierSpline>(true).ToList();
            Debug.Log($"PathManager found {player2Paths.Count} paths for Player 2.");
        }
        else
        {
            Debug.LogError("PathManager: Player 2 Paths Parent not assigned!");
        }
    }

    // Public method for spawners to get the correct path list
    public List<BezierSpline> GetPathsForPlayer(int playerIndex)
    {
        if (playerIndex == 0)
        {
            return player1Paths;
        }
        else if (playerIndex == 1)
        {
            return player2Paths;
        }
        else
        {
            Debug.LogError($"PathManager: Invalid player index requested: {playerIndex}");
            return new List<BezierSpline>(); // Return empty list
        }
    }

     // Public method for spawners/RPCs to get a specific path by index
    public BezierSpline GetPathByIndex(int playerIndex, int pathIndex)
    {
        List<BezierSpline> pathList = GetPathsForPlayer(playerIndex);
        if (pathIndex >= 0 && pathIndex < pathList.Count)
        {
            return pathList[pathIndex];
        }
        else
        {
            Debug.LogError($"PathManager: Invalid path index {pathIndex} requested for Player {playerIndex}. List count: {pathList.Count}");
            return null;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
