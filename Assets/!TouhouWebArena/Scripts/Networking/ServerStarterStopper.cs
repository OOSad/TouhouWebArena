using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System;

/// <summary>
/// Manages the UI button and associated logic for starting and stopping
/// the local application as a Netcode server (or host).
/// Updates button text based on the server's running state and invokes
/// the static <see cref="OnServerStateChanged"/> event when the state changes.
/// </summary>
public class ServerStarterStopper : MonoBehaviour
{
    [Header("UI References")]
    /// <summary>
    /// The button used to toggle the server/host state.
    /// </summary>
    [Tooltip("The button used to toggle the server/host state.")]
    [SerializeField] private Button serverToggleButton;
    /// <summary>
    /// The TextMeshPro component displaying the button's action (Start Server/Stop Server).
    /// </summary>
    [Tooltip("The TextMeshPro component displaying the button's action (Start Server/Stop Server).")]
    [SerializeField] private TextMeshProUGUI buttonText;
    
    /// <summary>Tracks the current running state of the local server/host.</summary>
    private bool isServerRunning = false;
    
    /// <summary>
    /// Static event invoked whenever the server/host is started or stopped via this component.
    /// Provides a boolean indicating the new running state (true = running, false = stopped).
    /// </summary>
    public static event Action<bool> OnServerStateChanged;
    
    /// <summary>
    /// Public property to query the current running state of the server/host managed by this component.
    /// </summary>
    public bool IsServerRunning => isServerRunning;
    
    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// Adds a listener to the <see cref="serverToggleButton"/> and sets the initial button text.
    /// </summary>
    private void Start()
    {
        if (serverToggleButton != null)
        {
            serverToggleButton.onClick.AddListener(ToggleServer);
        }
        UpdateButtonText();
    }
    
    /// <summary>
    /// Toggles the server/host state. Called by the <see cref="serverToggleButton"/>'s onClick event.
    /// Calls either <see cref="StartServer"/> or <see cref="StopServer"/> based on the current state.
    /// Updates the button text and invokes the <see cref="OnServerStateChanged"/> event.
    /// </summary>
    public void ToggleServer()
    {
        if (isServerRunning)
        {
            StopServer();
        }
        else
        {
            StartServer();
        }
        
        UpdateButtonText();
        
        // Notify subscribers about the server state change
        OnServerStateChanged?.Invoke(isServerRunning);
    }
    
    /// <summary>
    /// Attempts to start the <see cref="NetworkManager"/> as a server/host if not already running.
    /// Updates the <see cref="isServerRunning"/> state upon successful start.
    /// </summary>
    private void StartServer()
    {
        if (!isServerRunning)
        {
            // --- REVERTED: Ensure previous session is stopped --- 
            /*
            if (NetworkManager.Singleton != null && 
               (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
            { ... }
            */
            // ----------------------------------------------------

            Debug.Log("[ServerStarterStopper] Attempting StartServer...");
            if (NetworkManager.Singleton.StartServer())
            {
                Debug.Log("[ServerStarterStopper] StartServer successful.");
                isServerRunning = true;
            }
            else
            {
                Debug.LogError("[ServerStarterStopper] StartServer failed!");
            }
        }
    }
    
    /// <summary>
    /// Shuts down the <see cref="NetworkManager"/> if currently running as a server/host.
    /// Updates the <see cref="isServerRunning"/> state.
    /// </summary>
    private void StopServer()
    {
        if (isServerRunning)
        {
            NetworkManager.Singleton.Shutdown();
            isServerRunning = false;
        }
    }
    
    /// <summary>
    /// Updates the text displayed on the <see cref="serverToggleButton"/> based on the <see cref="isServerRunning"/> state.
    /// Shows "Stop Server" if running, "Start Server" otherwise.
    /// </summary>
    private void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = isServerRunning ? "Stop Server" : "Start Server";
        }
    }
} 