using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class LatencyDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI latencyText;
    [SerializeField] private float updateInterval = 0.5f; // How often to update the display in seconds

    private NetworkManager networkManager;
    private Coroutine updateCoroutine;

    void Start()
    {
        if (latencyText == null)
        {
            Debug.LogError("Latency TextMeshProUGUI component is not assigned in the inspector.", this);
            enabled = false; // Disable script if text component is missing
            return;
        }

        latencyText.text = "-- ms"; // Initial text

        // Using a delay before finding the NetworkManager just in case initialization order matters
        StartCoroutine(FindNetworkManagerAndStartUpdate());
    }

    private IEnumerator FindNetworkManagerAndStartUpdate()
    {
        // Wait a frame to ensure NetworkManager might be initialized
        yield return null; 

        networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogWarning("NetworkManager not found. Latency display will not function.", this);
            latencyText.text = "N/A";
            enabled = false;
            yield break;
        }

        // Start the update loop once NetworkManager is found
        if (updateCoroutine == null)
        {
            updateCoroutine = StartCoroutine(UpdateLatencyText());
        }
    }

    void OnDisable()
    {
        // Stop the coroutine if the object is disabled or destroyed
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }

    private IEnumerator UpdateLatencyText()
    {
        while (true)
        {
            // Ensure NetworkManager is still valid
            if (networkManager == null)
            {
                latencyText.text = "N/A";
                yield return new WaitForSeconds(updateInterval); // Wait before trying again or breaking
                continue;
            }

            if (networkManager.IsConnectedClient)
            {
                // For a client, get RTT to the server
                ulong rtt = networkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
                latencyText.text = $"{rtt} ms";
            }
            else if (networkManager.IsHost)
            {
                // Host has no latency to itself
                 latencyText.text = "0 ms (Host)";
            }
            // Add cases for Server-only if needed
            // else if (networkManager.IsServer)
            // {
            //    latencyText.text = "Latency: N/A (Server)"; 
            // }
            else
            {
                latencyText.text = "-- ms"; // Show default if not connected
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }
} 