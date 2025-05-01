using UnityEngine;
using Unity.Netcode;
using System.Collections; // Required for Coroutine
using UnityEngine.SceneManagement; // Required for LoadSceneMode

/// <summary>
/// [Server Only] Manages networked scene transitions.
/// Provides a centralized way to load scenes across the network after an optional delay.
/// </summary>
public class SceneTransitionManager : MonoBehaviour // Could be NetworkBehaviour if needed later
{
    // --- Singleton Pattern ---
    public static SceneTransitionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        { 
            Debug.LogWarning("[SceneTransitionManager] Duplicate instance detected. Destroying self.", this);
            Destroy(gameObject); 
        }
        else 
        { 
            Instance = this; 
            // Optional: Keep alive across scenes if this manager needs to persist
            // DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        { 
            Instance = null;
        }
    }
    // -----------------------

    /// <summary>
    /// [Server Only] Initiates loading a scene across the network after a specified delay.
    /// </summary>
    /// <param name="sceneName">The exact name of the scene to load.</param>
    /// <param name="delay">Delay in seconds before initiating the scene load.</param>
    public void LoadNetworkScene(string sceneName, float delay = 0f)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[SceneTransitionManager] LoadNetworkScene called, but not on the server. Aborting scene load.", this);
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
             Debug.LogError("[SceneTransitionManager] LoadNetworkScene called with null or empty sceneName!", this);
             return;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName, delay));
    }

    /// <summary>
    /// [Server Only] Coroutine that waits for the delay and then loads the scene via NetworkManager.
    /// </summary>
    private IEnumerator LoadSceneCoroutine(string sceneName, float delay)
    {
        Debug.Log($"[SceneTransitionManager] Starting delayed scene load for '{sceneName}' in {delay} seconds...", this);
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        
        Debug.Log($"[SceneTransitionManager] Loading scene '{sceneName}' via NetworkManager...", this);
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        // Note: Clients should automatically follow the server's scene change.
    }
} 