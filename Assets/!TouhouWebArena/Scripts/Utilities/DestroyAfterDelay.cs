using UnityEngine;
using Unity.Netcode;

/// <summary>
/// [Server Only] A simple utility component that destroys its networked GameObject
/// after a specified delay.
/// Uses <see cref="NetworkObject.Despawn(true)"/> to remove the object across the network.
/// </summary>
public class DestroyAfterDelay : NetworkBehaviour
{
    [SerializeField] private float delay = 1.0f; // Time in seconds before destroying

    public override void OnNetworkSpawn()
    {
        // Only the server should initiate the destruction countdown
        if (IsServer)
        {
            Invoke(nameof(DestroyObject), delay);
        }
    }

    private void DestroyObject()
    {
        // Ensure we are still on the server and the object exists and is spawned
        if (!IsServer || NetworkObject == null || !NetworkObject.IsSpawned) return;
        
        NetworkObject.Despawn(true); // Despawn and destroy on all clients
    }

    // Optional: Stop Invoke if the object is disabled prematurely
    void OnDisable()
    {
        if (IsServer)
        {
            CancelInvoke(nameof(DestroyObject));
        }
    }
} 