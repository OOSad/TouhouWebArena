using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class ClientConnectorDisconnector : MonoBehaviour
{
    [SerializeField] private Button clientToggleButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    
    private bool isClientConnected = false;
    
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
    
    // Start is called before the first frame update
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
    
    private void ConnectClient()
    {
        if (!isClientConnected)
        {
            if (NetworkManager.Singleton.StartClient())
            {
                // Note: We'll let the callbacks handle the state change
                Debug.Log("Client connection attempt started");
            }
            else
            {
                Debug.LogError("Failed to start client connection");
            }
        }
    }
    
    private void DisconnectClient()
    {
        if (isClientConnected)
        {
            NetworkManager.Singleton.Shutdown();
            // Note: We'll let the callbacks handle the state change
            Debug.Log("Client disconnection initiated");
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        // Only update if this is our local client
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            isClientConnected = true;
            Debug.Log("Client connected successfully with ID: " + clientId);
            UpdateButtonText();
        }
    }
    
    private void OnClientDisconnect(ulong clientId)
    {
        // This will be called when disconnected for any reason,
        // including server shutdown or local disconnect
        isClientConnected = false;
        Debug.Log("Client disconnected with ID: " + clientId);
        UpdateButtonText();
    }
    
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
