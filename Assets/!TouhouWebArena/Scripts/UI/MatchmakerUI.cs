using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class MatchmakerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private Button queueButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI queueText;

    [Header("Matchmaker Reference")]
    [SerializeField] private Matchmaker matchmaker;

    private ulong localClientId = ulong.MaxValue;

    void Start()
    {
        SetButtonsInteractable(false, false);

        if (queueButton != null) queueButton.onClick.AddListener(OnQueueButtonClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelButtonClicked);

        if (matchmaker == null)
        {
            matchmaker = FindObjectOfType<Matchmaker>();
            if (matchmaker == null)
            {
                Debug.LogError("MatchmakerUI could not find the Matchmaker instance!", this);
                this.enabled = false;
                return;
            }
        }

        UpdateQueueDisplayText("Connecting...");

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            HandleConnection(NetworkManager.Singleton.LocalClientId);
        }

         if (NetworkManager.Singleton != null)
         {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnection;
         }
    }

    void OnDestroy()
    {
        if (queueButton != null) queueButton.onClick.RemoveListener(OnQueueButtonClicked);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancelButtonClicked);

         if (NetworkManager.Singleton != null)
         {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnection;
         }
    }

    private void HandleConnection(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            localClientId = clientId;
            SetButtonsInteractable(true, false);
            UpdateQueueDisplayText("Connected. Enter name and queue.");
        }
    }

     private void HandleDisconnection(ulong clientId)
    {
        if (localClientId != ulong.MaxValue && clientId == localClientId)
        {
            SetButtonsInteractable(false, false);
            UpdateQueueDisplayText("Disconnected");
            localClientId = ulong.MaxValue;
        }
    }

    private void OnQueueButtonClicked()
    {
        if (matchmaker == null) return;

        string username = "Anonymous";
        if (usernameInput != null && !string.IsNullOrWhiteSpace(usernameInput.text))
        {
            username = usernameInput.text;
        }

        matchmaker.RequestJoinQueue(username);
    }

    private void OnCancelButtonClicked()
    {
        if (matchmaker == null) return;
        matchmaker.RequestLeaveQueue();
    }

    public void UpdateQueueStatus(bool isInQueue)
    {
        SetButtonsInteractable(!isInQueue, isInQueue);
    }

    public void UpdateQueueDisplayText(string text)
    {
         if (queueText != null)
         {
             queueText.text = text;
         }
    }

    private void SetButtonsInteractable(bool queueInteractable, bool cancelInteractable)
    {
        if (queueButton != null) queueButton.interactable = queueInteractable;
        if (cancelButton != null) cancelButton.interactable = cancelInteractable;
    }
} 