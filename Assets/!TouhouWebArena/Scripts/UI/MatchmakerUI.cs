using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;
using Unity.Netcode.Transports.UTP; // Required for UnityTransport

public class MatchmakerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private Button queueButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI queueText;

    [Header("Matchmaker Reference")]
    [SerializeField] private Matchmaker matchmaker;

    [Header("Custom Server UI (Optional)")]
    [Tooltip("Parent GameObject holding the custom server IP/Port/Button.")]
    [SerializeField] private GameObject customServerPanel; // Assign the parent panel here
    [SerializeField] private TMP_InputField customIpInput;
    [SerializeField] private TMP_InputField customPortInput;
    [SerializeField] private Button startCustomServerButton;

    [Header("Settings")]
    [SerializeField] private float connectionTimeout = 10.0f; // Seconds before connection attempt times out

    private ulong localClientId = ulong.MaxValue;
    private bool isConnecting = false;
    private string pendingUsername = null; // Store username for auto-queue after connect
    private Coroutine connectionCoroutine = null; // Reference to the timeout coroutine

    void Start()
    {
        // Ensure custom panel is off initially
        if(customServerPanel != null) customServerPanel.SetActive(false);

        // Don't set interactable here, let UpdateStatusTextBasedOnState handle initial state
        // SetButtonsInteractable(false, false);

        if (queueButton != null) queueButton.onClick.AddListener(OnQueueButtonClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelButtonClicked);
        if(startCustomServerButton != null) startCustomServerButton.onClick.AddListener(OnStartCustomServerButtonClicked);

        if (matchmaker == null) matchmaker = FindObjectOfType<Matchmaker>();

        if (NetworkManager.Singleton != null)
            {
             // Subscribe first
             NetworkManager.Singleton.OnClientConnectedCallback += HandleConnection;
             NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnection; // Generic disconnect
             NetworkManager.Singleton.OnClientDisconnectCallback += HandleLocalClientOrServerStop; // Specific handler
             NetworkManager.Singleton.OnServerStarted += HandleServerStarted;

             // Then set initial state based on current NM status
             UpdateStatusTextBasedOnState();
        }
         else { UpdateQueueDisplayText("NetworkManager not found!"); this.enabled = false; }
    }

    void Update()
    {
        // Check for F10 key press to toggle custom server UI
        if (Input.GetKeyDown(KeyCode.F10))
        {
            // Only allow toggling if not currently connected/hosting/serving
            if (NetworkManager.Singleton == null ||
               (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsConnectedClient))
            {
                 ToggleCustomServerUI();
            }
            else
            {
                 Debug.LogWarning("Cannot toggle Custom Server UI while NetworkManager is active.");
            }
        }

        // ADDED: Check for F9 key press to toggle default server
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ToggleDefaultServer();
         }
    }

    void OnDestroy()
    {
        if (queueButton != null) queueButton.onClick.RemoveListener(OnQueueButtonClicked);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
        if (startCustomServerButton != null) startCustomServerButton.onClick.RemoveListener(OnStartCustomServerButtonClicked);

         if (NetworkManager.Singleton != null)
         {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnection;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleLocalClientOrServerStop; // Updated name
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
         }
         if (connectionCoroutine != null) StopCoroutine(connectionCoroutine);
    }

    // --- Network Callback Handlers ---

    private void HandleConnection(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            localClientId = clientId;
            isConnecting = false;

            if (connectionCoroutine != null) { StopCoroutine(connectionCoroutine); connectionCoroutine = null; }

            if (pendingUsername != null)
            {
                 string userToQueue = pendingUsername;
                 pendingUsername = null;
                 UpdateQueueDisplayText("Connected! Queueing...");
                 RequestJoinQueueInternal(userToQueue);
            }
            else { UpdateStatusTextBasedOnState(); } // Update UI based on connected state
        }
    }

     private void HandleServerStarted() { UpdateStatusTextBasedOnState(); }
     private void HandleDisconnection(ulong clientId) { /* Can be used for other client disconnects if needed */ }

     // Renamed and refined: Handles the LOCAL client disconnecting OR the server stopping.
     private void HandleLocalClientOrServerStop(ulong clientId)
        {
         // Determine if the NetworkManager is now truly idle AFTER this callback
         // It's possible the state isn't updated immediately, so we might check in UpdateStatusTextBasedOnState too
         bool wasLocalClientDisconnect = (localClientId != ulong.MaxValue && clientId == localClientId);

         // Reset local client ID if it was us
         if (wasLocalClientDisconnect)
         {
            localClientId = ulong.MaxValue;
        }

         // Regardless of ID match, if we were connecting, stop the process
         if (isConnecting)
         {
             isConnecting = false;
             if (connectionCoroutine != null)
             {
                 StopCoroutine(connectionCoroutine);
                 connectionCoroutine = null;
             }
             // Don't set text here, let UpdateStatusTextBasedOnState handle it based on final state
             // UpdateQueueDisplayText("Connection Failed or Cancelled.");
         }

         // Crucially, update the UI based on the *current* (potentially just changed) NetworkManager state
         UpdateStatusTextBasedOnState();
     }

    // --- Button Click Handlers ---

    private void OnQueueButtonClicked()
    {
        if (isConnecting) return;

        string username = "Anonymous";
        if (usernameInput != null && !string.IsNullOrWhiteSpace(usernameInput.text)) username = usernameInput.text;
        else if (usernameInput != null) { usernameInput.text = username + "#" + Random.Range(1000, 10000); username = usernameInput.text; }

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsConnectedClient)
        {
            UpdateQueueDisplayText("Connecting...");
            SetButtonsInteractable(false, true); // Disable Queue, Enable Cancel
            isConnecting = true;
            pendingUsername = username;

            if (connectionCoroutine != null) StopCoroutine(connectionCoroutine);
            connectionCoroutine = StartCoroutine(ConnectionTimeout());

            try { NetworkManager.Singleton.StartClient(); }
            catch (System.Exception e)
            {
                Debug.LogError($"[MatchmakerUI] Failed to start client: {e.Message}");
                if (connectionCoroutine != null) { StopCoroutine(connectionCoroutine); connectionCoroutine = null; }
                isConnecting = false;
                pendingUsername = null;
                UpdateQueueDisplayText($"Connection Failed: {e.Message}");
                UpdateStatusTextBasedOnState();
        }
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        { RequestJoinQueueInternal(username); }
    }

    private void RequestJoinQueueInternal(string username)
    {
         if (matchmaker == null) { UpdateQueueDisplayText("Error: Matchmaker not found!"); pendingUsername = null; return; }
         UpdateQueueDisplayText("Joining queue...");
         // Let UpdateQueueStatus handle button state changes after Matchmaker confirms queue status
        matchmaker.RequestJoinQueue(username);
    }

    private void OnCancelButtonClicked()
    {
        if (isConnecting)
        {
             if (connectionCoroutine != null) { StopCoroutine(connectionCoroutine); connectionCoroutine = null; }
             isConnecting = false;
             pendingUsername = null;
             NetworkManager.Singleton?.Shutdown();
             // HandleLocalClientOrServerStop callback will trigger UpdateStatusTextBasedOnState
             return;
        }

        if (matchmaker != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        { UpdateQueueDisplayText("Leaving queue..."); matchmaker.RequestLeaveQueue(); }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        { NetworkManager.Singleton.Shutdown(); }
        else { UpdateStatusTextBasedOnState(); }
    }

    // Called by Matchmaker after server confirms join/leave queue
    public void UpdateQueueStatus(bool isInQueue)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            if (!isConnecting) // Don't interfere if connection is in progress
    {
        SetButtonsInteractable(!isInQueue, isInQueue);
                 if (isInQueue) UpdateQueueDisplayText("In queue... Waiting for match.");
                 else UpdateQueueDisplayText("Connected. Ready to queue.");
            }
        }
    }

    // --- UI State Management ---

    public void UpdateQueueDisplayText(string text) { if (queueText != null) queueText.text = text; }

    public void ToggleCustomServerUI()
    {
        if (customServerPanel != null)
        {
            bool isActive = !customServerPanel.activeSelf;
            customServerPanel.SetActive(isActive);
             UpdateStatusTextBasedOnState();
        } else { Debug.LogWarning("Custom Server Panel not assigned in MatchmakerUI."); }
    }

     private void ToggleDefaultServer()
     {
         if (NetworkManager.Singleton == null) { UpdateQueueDisplayText("NetworkManager not found!"); return; }
         if (customServerPanel != null && customServerPanel.activeSelf) customServerPanel.SetActive(false);

         if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
         { Debug.Log("Stopping Server/Host via F9..."); UpdateQueueDisplayText("Stopping server..."); NetworkManager.Singleton.Shutdown(); StartCoroutine(DelayedUIUpdateAfterShutdown()); }
         else if (!NetworkManager.Singleton.IsClient)
         {
             try { Debug.Log("Starting Server (default settings) via F9..."); UpdateQueueDisplayText("Starting default server..."); NetworkManager.Singleton.StartServer(); }
             catch (System.Exception e) { Debug.LogError($"Failed to start default server: {e.Message}\n{e.StackTrace}"); UpdateQueueDisplayText($"Error starting server: {e.Message}"); UpdateStatusTextBasedOnState(); }
         } else { Debug.LogWarning("Cannot start server via F9: NetworkManager is already running as a Client."); UpdateQueueDisplayText("Cannot start server while connected as Client."); }
     }

     private IEnumerator DelayedUIUpdateAfterShutdown()
     {
         yield return null;
         yield return null;
         Debug.Log("Updating UI state after server shutdown delay.");
         UpdateStatusTextBasedOnState();
     }


    public void OnStartCustomServerButtonClicked()
    {
        if (NetworkManager.Singleton == null) { UpdateQueueDisplayText("Error: NetworkManager not found!"); return; }
         if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient) { UpdateQueueDisplayText("Error: NetworkManager already active!"); return; }

        string ipAddress = customIpInput != null ? customIpInput.text : "127.0.0.1";
        string portString = customPortInput != null ? customPortInput.text : "7777";
        if (string.IsNullOrWhiteSpace(ipAddress)) ipAddress = "127.0.0.1";

        if (!ushort.TryParse(portString, out ushort port) || port == 0)
        { UpdateQueueDisplayText("Error: Invalid Port Number."); Debug.LogError($"[MatchmakerUI] Invalid port entered: {portString}"); return; }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null) { UpdateQueueDisplayText("Error: UnityTransport not found!"); Debug.LogError("[MatchmakerUI] UnityTransport component not found."); return; }

        try { transport.SetConnectionData(ipAddress, port); Debug.Log($"[MatchmakerUI] Set transport to: {ipAddress}:{port}"); }
        catch (System.Exception e) { UpdateQueueDisplayText($"Error setting transport: {e.Message}"); Debug.LogError($"[MatchmakerUI] Failed to set connection data: {e.Message}"); return; }

        try
        { UpdateQueueDisplayText($"Starting server on {ipAddress}:{port}..."); NetworkManager.Singleton.StartServer(); }
        catch (System.Exception e)
        { UpdateQueueDisplayText($"Error starting server: {e.Message}"); Debug.LogError($"[MatchmakerUI] Failed to start custom server: {e.Message}\n{e.StackTrace}"); UpdateStatusTextBasedOnState(); }
    }

    // Central place to update UI text and button states based on current conditions
    private void UpdateStatusTextBasedOnState()
    {
         if (NetworkManager.Singleton == null) { UpdateQueueDisplayText("NetworkManager not found!"); SetButtonsInteractable(false, false); return; }

         // State 1: Connecting
         if (isConnecting)
         {
             UpdateQueueDisplayText("Connecting...");
             SetButtonsInteractable(false, true); // Allow Cancel
             if (customServerPanel != null) customServerPanel.SetActive(false);
         }
         // State 2: Running as Server/Host
         else if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
         {
              UpdateQueueDisplayText("Running as Host/Server. (F9 to stop)");
              SetButtonsInteractable(false, false); // No Queue/Cancel
              if (customServerPanel != null) customServerPanel.SetActive(false);
         }
         // State 3: Connected as Client (buttons/text managed by UpdateQueueStatus)
         else if (NetworkManager.Singleton.IsConnectedClient)
         {
              // Check queue status if Matchmaker available
              bool _isInQueue = false; // Assume not in queue unless Matchmaker says otherwise
              // TODO: Ideally, get actual queue status from Matchmaker instance if needed here
              UpdateQueueStatus(_isInQueue); // Let this handle text and buttons
              if (customServerPanel != null) customServerPanel.SetActive(false);
         }
         // State 4: Idle / Disconnected
         else
         {
             if(customServerPanel != null && customServerPanel.activeSelf)
             {
                  UpdateQueueDisplayText("Enter Custom Server IP/Port.");
                  SetButtonsInteractable(false, false); // No Queue/Cancel when panel up
             }
             else
             {
                  // MODIFIED LINE: Added hint back
                  UpdateQueueDisplayText("Enter name and Queue to connect. (F10 for Custom Panel)");
                  SetButtonsInteractable(true, false); // Allow Queue, No Cancel
             }
         }
    }

    // Only enables/disables buttons, doesn't change text
    private void SetButtonsInteractable(bool queueInteractable, bool cancelInteractable)
    {
        if (queueButton != null) queueButton.interactable = queueInteractable;
        if (cancelButton != null) cancelButton.interactable = cancelInteractable;
    }

    private IEnumerator ConnectionTimeout()
    {
        yield return new WaitForSeconds(connectionTimeout);
        if (isConnecting)
        {
             Debug.LogWarning("Connection attempt timed out.");
             isConnecting = false;
             pendingUsername = null;
             NetworkManager.Singleton?.Shutdown();
             UpdateQueueDisplayText("Connection Failed (Timeout)");
             UpdateStatusTextBasedOnState();
        }
        connectionCoroutine = null;
    }
} 