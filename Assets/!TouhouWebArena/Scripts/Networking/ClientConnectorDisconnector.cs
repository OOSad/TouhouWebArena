using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages the UI button and associated logic for connecting and disconnecting
/// the local application as a Netcode client.
/// Updates button text and interactivity based on connection status and server state.
/// Interacts with <see cref="NetworkManager"/> and <see cref="ServerStarterStopper"/>.
/// </summary>
public class ClientConnectorDisconnector : MonoBehaviour
{
    [Header("UI References")]
    /// <summary>
    /// The button used to toggle the client connection.
    /// </summary>
    [Tooltip("The button used to toggle the client connection.")]
    [SerializeField] private Button clientToggleButton;
    /// <summary>
    /// The TextMeshPro component displaying the button's action (Connect/Disconnect/Server Mode).
    /// </summary>
    [Tooltip("The TextMeshPro component displaying the button's action (Connect/Disconnect/Server Mode).")]
    [SerializeField] private TextMeshProUGUI buttonText;
    
    /// <summary>Tracks the current connection state of the local client.</summary>
    private bool isClientConnected = false;
    
    /// <summary>
    /// Called when the component becomes enabled and active.
    /// Subscribes to relevant <see cref="NetworkManager"/> connection events
    /// and the <see cref="ServerStarterStopper.OnServerStateChanged"/> event.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        
        // Subscribe to server state changes
        ServerStarterStopper.OnServerStateChanged += OnServerStateChanged;
    }

    /// <summary>
    /// Called when the component becomes disabled or inactive.
    /// Unsubscribes from all previously subscribed events to prevent memory leaks.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
        
        // Unsubscribe from server state changes
        ServerStarterStopper.OnServerStateChanged -= OnServerStateChanged;
    }
    
    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// Adds a listener to the <see cref="clientToggleButton"/> and performs initial UI updates
    /// based on the current connection and server state (<see cref="CheckServerState"/>).
    /// </summary>
    void Start()
    {
        if (clientToggleButton != null)
        {
            clientToggleButton.onClick.AddListener(ToggleClientConnection);
        }
        UpdateButtonText();
        
        // Check if server is already running on startup
        CheckServerState();
    }
    
    /// <summary>
    /// Callback handler for the <see cref="ServerStarterStopper.OnServerStateChanged"/> event.
    /// If the server starts while the client is connected, it disconnects the client.
    /// Updates the button's interactivity (<see cref="UpdateButtonInteractivity"/>).
    /// </summary>
    /// <param name="isServerRunning">True if the server is now running, false otherwise.</param>
    private void OnServerStateChanged(bool isServerRunning)
    {
        // If server starts running, disconnect client if connected
        if (isServerRunning && isClientConnected)
        {
            DisconnectClient();
            UpdateButtonText();
        }
        
        // Update button interactability
        UpdateButtonInteractivity(isServerRunning);
    }
    
    /// <summary>
    /// Checks the current state of the server by finding the <see cref="ServerStarterStopper"/> instance
    /// and updates the button's interactivity accordingly.
    /// Called during Start to handle initial state.
    /// </summary>
    private void CheckServerState()
    {
        // Find ServerStarterStopper in the scene
        ServerStarterStopper serverController = FindObjectOfType<ServerStarterStopper>();
        if (serverController != null)
        {
            // Update button interactivity based on server state
            UpdateButtonInteractivity(serverController.IsServerRunning);
        }
    }
    
    /// <summary>
    /// Updates the interactable state and text of the <see cref="clientToggleButton"/>.
    /// Disables the button and shows "Server Mode" if the server is running.
    /// Otherwise, enables the button and updates text via <see cref="UpdateButtonText"/>.
    /// </summary>
    /// <param name="isServerRunning">The current running state of the server.</param>
    private void UpdateButtonInteractivity(bool isServerRunning)
    {
        // Disable client connection button if server is running
        if (clientToggleButton != null)
        {
            clientToggleButton.interactable = !isServerRunning;
            
            // Update tooltip or add visual indication
            if (isServerRunning)
            {
                buttonText.text = "Server Mode";
            }
            else
            {
                UpdateButtonText();
            }
        }
    }

    /// <summary>
    /// Toggles the client connection state. Called by the <see cref="clientToggleButton"/>'s onClick event.
    /// Calls either <see cref="ConnectClient"/> or <see cref="DisconnectClient"/> based on the current state.
    /// </summary>
    public void ToggleClientConnection()
    {
        if (isClientConnected)
        {
            DisconnectClient();
        }
        else
        {
            ConnectClient();
        }
        
        UpdateButtonText();
    }
    
    /// <summary>
    /// Attempts to start the <see cref="NetworkManager"/> as a client if not already connected.
    /// Relies on NetworkManager callbacks (<see cref="OnClientConnected"/>) to update the state.
    /// </summary>
    private void ConnectClient()
    {
        if (!isClientConnected)
        {
            if (NetworkManager.Singleton.StartClient())
            {
                // Note: We'll let the callbacks handle the state change
            }
        }
    }
    
    /// <summary>
    /// Shuts down the <see cref="NetworkManager"/> if currently connected as a client.
    /// Relies on NetworkManager callbacks (<see cref="OnClientDisconnect"/>) to update the state.
    /// </summary>
    private void DisconnectClient()
    {
        if (isClientConnected)
        {
            NetworkManager.Singleton.Shutdown();
            // Note: We'll let the callbacks handle the state change
        }
    }
    
    /// <summary>
    /// Callback handler for <see cref="NetworkManager.OnClientConnectedCallback"/>.
    /// If the connected client is the local client, updates the state (<see cref="isClientConnected"/> = true)
    /// and the button text (<see cref="UpdateButtonText"/>).
    /// </summary>
    /// <param name="clientId">The ClientId of the client that connected.</param>
    private void OnClientConnected(ulong clientId)
    {
        // Only update if this is our local client
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            isClientConnected = true;
            UpdateButtonText();
        }
    }
    
    /// <summary>
    /// Callback handler for <see cref="NetworkManager.OnClientDisconnectCallback"/>.
    /// Updates the state (<see cref="isClientConnected"/> = false) and the button text (<see cref="UpdateButtonText"/>)
    /// regardless of which client disconnected, as this callback handles voluntary disconnects,
    /// server shutdowns, and connection failures for the local client.
    /// </summary>
    /// <param name="clientId">The ClientId of the client that disconnected.</param>
    private void OnClientDisconnect(ulong clientId)
    {
        // This will be called when disconnected for any reason,
        // including server shutdown or local disconnect
        isClientConnected = false;
        UpdateButtonText();
    }
    
    /// <summary>
    /// Updates the text displayed on the <see cref="clientToggleButton"/> based on the <see cref="isClientConnected"/> state.
    /// Shows "Disconnect" if connected, "Connect" otherwise.
    /// Does not change the text if the application is currently in server mode.
    /// </summary>
    private void UpdateButtonText()
    {
        // Don't update text if we're in server mode
        ServerStarterStopper serverController = FindObjectOfType<ServerStarterStopper>();
        if (buttonText != null && (serverController == null || !serverController.IsServerRunning))
        {
            buttonText.text = isClientConnected ? "Disconnect" : "Connect";
        }
    }
}
