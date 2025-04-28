using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro; // If you need to access TextMeshPro components directly

/// <summary>
/// Manages the main menu UI elements, including Server/Client start/stop buttons and their text.
/// This script should be placed on an object within the MainMenuScene.
/// It directly interacts with <see cref="NetworkManager.Singleton"/> to start the server or client
/// and updates the UI elements based on NetworkManager's state via callbacks.
/// Requires references to the Server Button, Client Button, and their respective TextMeshProUGUI components
/// to be assigned in the Inspector.
/// Assumes a dedicated server model (no StartHost functionality).
/// </summary>
public class MainMenuUIManager : MonoBehaviour
{
    [Header("Button References")]
    [Tooltip("The button used to start/stop the server.")]
    [SerializeField] private Button serverButton;
    [Tooltip("The button used to connect/disconnect as a client.")]
    [SerializeField] private Button clientButton;

    [Header("Text References")]
    [Tooltip("The TextMeshProUGUI component on the server button.")]
    [SerializeField] private TextMeshProUGUI serverButtonText;
    [Tooltip("The TextMeshProUGUI component on the client button.")]
    [SerializeField] private TextMeshProUGUI clientButtonText;

    // Add references for Matchmaker UI if needed
    [Header("Matchmaker UI (Optional)")]
    [Tooltip("Optional reference to the MatchmakerUI to update its state on disconnect.")]
    [SerializeField] private MatchmakerUI matchmakerUI;

    private bool isServerRunning = false; // Tracks if the server is running locally
    private bool isClientConnected = false; // Tracks if the local client is connected

    /// <summary>
    /// Called on the frame when the script is enabled.
    /// Adds listeners to buttons and subscribes to NetworkManager events.
    /// Performs an initial UI state update.
    /// </summary>
    void Start()
    {
        // Add listeners programmatically
        serverButton?.onClick.AddListener(ToggleServer);
        clientButton?.onClick.AddListener(ToggleClient);

        // Subscribe to NetworkManager events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            NetworkManager.Singleton.OnTransportFailure += HandleTransportFailure;
        }
        else
        { 
            Debug.LogError("[MainMenuUIManager] NetworkManager.Singleton is null on Start!");
        }

        // Initial UI Update based on potential pre-existing state 
        UpdateUIState();
    }

    /// <summary>
    /// Called when the script instance is destroyed.
    /// Removes button listeners and unsubscribes from NetworkManager events.
    /// </summary>
    void OnDestroy()
    {
        // Remove listeners
        serverButton?.onClick.RemoveListener(ToggleServer);
        clientButton?.onClick.RemoveListener(ToggleClient);

        // Unsubscribe from NetworkManager events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            NetworkManager.Singleton.OnTransportFailure -= HandleTransportFailure;
        }
    }

    /// <summary>
    /// Callback handler for <see cref="NetworkManager.OnServerStarted"/>.
    /// Updates local state and UI when the server starts successfully.
    /// </summary>
    private void HandleServerStarted()
    {
        Debug.Log("[MainMenuUIManager] Detected Server Started.");
        isServerRunning = true;
        isClientConnected = false; // Server is not a player client
        UpdateUIState();
    }

    /// <summary>
    /// Callback handler for <see cref="NetworkManager.OnClientConnectedCallback"/>.
    /// Updates local state and UI when the local client connects successfully.
    /// </summary>
    /// <param name="clientId">The clientId that connected.</param>
    private void HandleClientConnected(ulong clientId)
    {
        // Only update if it's the local client connecting
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            isClientConnected = true;
            isServerRunning = false; // If we connected as a client, we aren't the server
            UpdateUIState();
        }
    }

    /// <summary>
    /// Callback handler for <see cref="NetworkManager.OnClientDisconnectCallback"/>.
    /// Updates local state and UI when the local client disconnects or the server shuts down.
    /// </summary>
    /// <param name="clientId">The clientId that disconnected.</param>
    private void HandleClientDisconnect(ulong clientId)
    {
        // Only update UI if the local client disconnected OR if the server shut down
        // (which triggers this for all clients, including the non-player server itself if IsServer was true)
        if (clientId == NetworkManager.Singleton.LocalClientId || isServerRunning)
        {
            Debug.Log($"[MainMenuUIManager] Detected Disconnect/Shutdown (ClientId: {clientId}, WasServer: {isServerRunning}). Resetting state.");
            isClientConnected = false;
            isServerRunning = false; 
            UpdateUIState();

            // Optionally, reset Matchmaker UI state here if needed
            // matchmakerUI?.UpdateQueueDisplayText("Disconnected");
            // matchmakerUI?.UpdateQueueStatus(false); 
        }
    }
    
    /// <summary>
    /// Callback handler for <see cref="NetworkManager.OnTransportFailure"/>.
    /// Resets state and updates UI on transport errors.
    /// </summary>
    private void HandleTransportFailure()
    {
        Debug.LogError("[MainMenuUIManager] Network Transport Failure detected!");
        isClientConnected = false;
        isServerRunning = false;
        UpdateUIState();
    }

    /// <summary>
    /// Toggles the server state (Start/Stop). Called by the Server Button.
    /// </summary>
    private void ToggleServer()
    {
        if (isServerRunning)
        { 
            Debug.Log("[MainMenuUIManager] Shutting down NetworkManager (Server)...");
            NetworkManager.Singleton.Shutdown();
            isServerRunning = false; // Explicitly set here as OnServerStopped isn't used
            isClientConnected = false;
            UpdateUIState(); // Update UI immediately after shutdown request
        }
        else if (!isClientConnected) // Can only start server if not connected as client
        { 
            Debug.Log("[MainMenuUIManager] Starting Server...");
            if (!NetworkManager.Singleton.StartServer())
            {
                Debug.LogError("[MainMenuUIManager] StartServer failed!");
            }
            // State update mostly handled by HandleServerStarted callback
        }
    }

    /// <summary>
    /// Toggles the client connection state (Connect/Disconnect). Called by the Client Button.
    /// </summary>
    private void ToggleClient()
    {
        if (isClientConnected) // If connected as client
        {
            Debug.Log("[MainMenuUIManager] Shutting down NetworkManager (Client)...");
            NetworkManager.Singleton.Shutdown();
            isClientConnected = false; // Explicitly set here
            isServerRunning = false;
            UpdateUIState(); // Update UI immediately
        }
        else if (!isServerRunning) // Can only connect client if server isn't running locally
        {
            Debug.Log("[MainMenuUIManager] Starting Client...");
            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("[MainMenuUIManager] StartClient failed!");
            }
            // State update handled by HandleClientConnected callback
        }
    }

    /// <summary>
    /// Updates the interactability and text of the Server and Client buttons
    /// based on the current network connection state (<see cref="isServerRunning"/>, <see cref="isClientConnected"/>).
    /// </summary>
    private void UpdateUIState()
    {
        if (isServerRunning)
        {
            if (serverButtonText != null) serverButtonText.text = "Stop Server";
            if (serverButton != null) serverButton.interactable = true;
            
            if (clientButtonText != null) clientButtonText.text = "Server Mode"; // Indicate client connection isn't possible
            if (clientButton != null) clientButton.interactable = false;
        }
        else if (isClientConnected)
        {
            if (serverButtonText != null) serverButtonText.text = "Client Mode";
            if (serverButton != null) serverButton.interactable = false;

            if (clientButtonText != null) clientButtonText.text = "Disconnect";
            if (clientButton != null) clientButton.interactable = true;
        }
        else // If disconnected
        {
            if (serverButtonText != null) serverButtonText.text = "Start Server";
            if (serverButton != null) serverButton.interactable = true;

            if (clientButtonText != null) clientButtonText.text = "Start Client";
            if (clientButton != null) clientButton.interactable = true;
        }
    }
} 