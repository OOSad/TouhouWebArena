using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System;

public class ServerStarterStopper : MonoBehaviour
{
    [SerializeField] private Button serverToggleButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    
    private bool isServerRunning = false;
    
    // Event that will be invoked when server state changes
    public static event Action<bool> OnServerStateChanged;
    
    // Public property to check if server is running
    public bool IsServerRunning => isServerRunning;
    
    private void Start()
    {
        if (serverToggleButton != null)
        {
            serverToggleButton.onClick.AddListener(ToggleServer);
        }
        UpdateButtonText();
    }
    
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
    
    private void StartServer()
    {
        if (!isServerRunning)
        {
            if (NetworkManager.Singleton.StartServer())
            {
                isServerRunning = true;
                Debug.Log("Server started successfully");
            }
            else
            {
                Debug.LogError("Failed to start server");
            }
        }
    }
    
    private void StopServer()
    {
        if (isServerRunning)
        {
            NetworkManager.Singleton.Shutdown();
            isServerRunning = false;
            Debug.Log("Server stopped");
        }
    }
    
    private void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = isServerRunning ? "Stop Server" : "Start Server";
        }
    }
} 