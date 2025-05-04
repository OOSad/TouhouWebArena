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

                // Calculate estimated one-way latency
                float oneWayLatency = rtt / 2.0f;

                // Estimate frame delay (assuming target 60 FPS)
                const float targetFrameTimeMs = 1000.0f / 60.0f;
                float rttFrames = targetFrameTimeMs > 0 ? rtt / targetFrameTimeMs : 0; // Avoid division by zero
                float oneWayFrames = targetFrameTimeMs > 0 ? oneWayLatency / targetFrameTimeMs : 0;

                // Format the text to show all values
                latencyText.text = $"{rtt}ms RTT ({rttFrames:F1}f) / {oneWayLatency:F1}ms Est ({oneWayFrames:F1}f)";
            }
            else if (networkManager.IsHost)
            {
                // Host has no latency to itself
                 latencyText.text = "0ms RTT (0.0f) / 0.0ms Est (0.0f) (Host)"; // Show zero values for host
            }
            // Add cases for Server-only if needed
            // else if (networkManager.IsServer)
            else
            {
                latencyText.text = "-- ms"; // Show default if not connected
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }
} 