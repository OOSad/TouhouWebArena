using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles the instantiation of visual effects when a Spirit dies.
/// Should be attached to the same GameObject as the SpiritController.
/// Assumes effects are instantiated on the server.
/// </summary>
public class SpiritDeathEffects : MonoBehaviour // NetworkBehaviour might be needed if effects need pooling/syncing later
{
    [Header("Death Effect Prefabs")]
    [Tooltip("Prefab for the visual effect spawned when the spirit dies in the normal state.")]
    [SerializeField] private GameObject normalDeathEffectPrefab;

    [Tooltip("Prefab for the visual effect spawned when the spirit dies in the activated state.")]
    [SerializeField] private GameObject activatedDeathEffectPrefab;

    /// <summary>
    /// Instantiates the appropriate death visual effect based on the spirit's state at death.
    /// Currently called only on the server by SpiritController.Die().
    /// </summary>
    /// <param name="wasActivated">True if the spirit was activated when it died.</param>
    /// <param name="position">The world position where the effect should spawn.</param>
    public void PlayDeathEffect(bool wasActivated, Vector3 position)
    {
        GameObject effectPrefab = wasActivated ? activatedDeathEffectPrefab : normalDeathEffectPrefab;

        if (effectPrefab != null)
        {
            // Instantiate the effect - consider object pooling if these are frequent
            GameObject effectInstance = Instantiate(effectPrefab, position, Quaternion.identity);
            
            // --- Network Spawn the Effect --- 
            // Effects must have a NetworkObject component to be spawned.
            NetworkObject netObj = effectInstance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                 // Check if we are actually running on the server before spawning
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    netObj.Spawn(true); // Spawn server-owned, will be destroyed automatically if scene changes
                }
                // No else needed: If not server, shouldn't have reached here via SpiritController.Die anyway
            }
            else
            {
                Debug.LogWarning($"Death effect prefab '{effectPrefab.name}' is missing NetworkObject component. Effect will only appear on server.", this);
                // Keep the locally instantiated effect for the server in this case?
                // Or destroy it: Destroy(effectInstance);
            }
            // ----------------------------------
            
            // Optional: Add logic to automatically destroy the effect after some time
            // This should probably be part of the effect prefab's own script if needed.
            // Destroy(effectInstance, 2f); 
        }
        else
        {
            Debug.LogWarning($"Missing death effect prefab for {(wasActivated ? "activated" : "normal")} state.", this);
        }
    }
} 