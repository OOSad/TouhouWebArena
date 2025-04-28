using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // Assuming NetworkManager is in this namespace

/// <summary>
/// Ensures only one instance of the GameObject containing the NetworkManager
/// persists across scene loads using DontDestroyOnLoad.
/// Attach this script to the same GameObject as the NetworkManager component
/// in the initial scene (e.g., MainMenuScene).
/// </summary>
public class NetworkManagerSingleton : MonoBehaviour
{
    /// <summary>
    /// Static reference to the single instance of this component.
    /// </summary>
    private static NetworkManagerSingleton _instance;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Checks if an instance already exists. If so, destroys the current GameObject
    /// to prevent duplicates. Otherwise, sets this as the instance and marks
    /// the GameObject to persist across scene loads.
    /// </summary>
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // If an instance already exists and it's not this one, destroy this one.
            Debug.LogWarning("Duplicate NetworkManagerSingleton detected. Destroying the new GameObject.");
            Destroy(gameObject);
            return; // Prevent rest of Awake from running on the duplicate
        }
        else
        {
            // If no instance exists, this becomes the instance.
            _instance = this;
            // Mark this GameObject to not be destroyed when loading new scenes.
            DontDestroyOnLoad(gameObject);
            Debug.Log("NetworkManagerSingleton initialized and marked as DontDestroyOnLoad.");
        }
    }

    // Note: No Start() or Update() needed for this simple singleton.
}
