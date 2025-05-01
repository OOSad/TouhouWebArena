using UnityEngine;
using System.Collections;
using Unity.Netcode; // Needed if spawning shockwave as NetworkObject
using TouhouWebArena; // For PlayerRole, FairyRegistry, FairyController

/// <summary>
/// [Server Only] A temporary utility object responsible for executing the delayed part of a fairy chain reaction.
/// Instantiated by <see cref="FairyChainReactionHandler"/>.
/// Waits for a specified duration, spawns a shockwave effect (if provided), finds the next fairy in the line
/// using <see cref="FairyRegistry.FindNextInLine"/>, triggers its death via <see cref="FairyHealth.ApplyLethalDamage"/>,
/// and then destroys its own GameObject.
/// </summary>
public class DelayedActionProcessor : MonoBehaviour
{
    private Vector3 spawnPosition;
    private PlayerRole initialKillerRole;
    private float delay;
    private GameObject shockwavePrefab;
    private System.Guid lineId;
    private int originatingIndexInLine;

    /// <summary>
    /// Initializes the processor with necessary data and starts the <see cref="DelayedActionCoroutine"/>.
    /// Expected to be called immediately after instantiation on the server by <see cref="FairyChainReactionHandler"/>.
    /// Includes a server check and self-destructs if accidentally called on a client.
    /// </summary>
    /// <param name="position">The position where the originating fairy died (used for shockwave spawn).</param>
    /// <param name="killer">The role of the player who killed the originating fairy.</param>
    /// <param name="waitTime">The delay before executing the action.</param>
    /// <param name="effectPrefab">The shockwave prefab to instantiate (can be null).</param>
    /// <param name="id">The line ID of the originating fairy.</param>
    /// <param name="index">The index within the line of the originating fairy.</param>
    public void InitializeAndRun(Vector3 position, PlayerRole killer, float waitTime, GameObject effectPrefab, System.Guid id, int index)
    {
        // Only run on server - Instantiation should also happen on server only
        // Although this component isn't a NetworkBehaviour, its instantiation and logic
        // are tied to server-side game events (Fairy death).
        // Adding an explicit check might be redundant if instantiation is handled correctly,
        // but can prevent issues if accidentally created on a client.
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[DelayedActionProcessor] Initialized on client? Destroying self.", this);
            Destroy(gameObject);
            return;
        }

        this.spawnPosition = position;
        this.initialKillerRole = killer;
        this.delay = waitTime;
        this.shockwavePrefab = effectPrefab;
        this.lineId = id;
        this.originatingIndexInLine = index;

        StartCoroutine(DelayedActionCoroutine());
    }

    /// <summary>
    /// Coroutine that performs the delayed actions: waits, spawns shockwave, finds next fairy, triggers its death, destroys self.
    /// </summary>
    private IEnumerator DelayedActionCoroutine()
    {
        // 1. Wait for the specified delay
        yield return new WaitForSeconds(delay);

        // 2. Spawn shockwave effect (if assigned)
        if (shockwavePrefab != null)
        {
            GameObject effectInstance = Instantiate(shockwavePrefab, spawnPosition, Quaternion.identity);
            // Attempt to spawn the effect over the network if it has a NetworkObject
            NetworkObject effectNob = effectInstance.GetComponent<NetworkObject>();
            if (effectNob != null)
            {
                effectNob.Spawn(true); // Spawn with server ownership
            }
            // If the shockwave has a limited lifetime script, it should handle its own destruction.
            // Otherwise, add a Destroy(effectInstance, lifetime) here.
        }

        // 3. Find the next fairy in the line using the Registry
        if (FairyRegistry.Instance != null)
        {
            // Use FindNextInLine with the originating index
            FairyController nextFairy = FairyRegistry.Instance.FindNextInLine(lineId, originatingIndexInLine);

            // 4. Trigger its death via its FairyHealth component if found and alive
            if (nextFairy != null)
            {
                FairyHealth nextFairyHealth = nextFairy.GetComponent<FairyHealth>();
                if (nextFairyHealth != null && nextFairyHealth.IsAlive()) // Check IsAlive on health component
                {
                    // Use ApplyLethalDamage on health component
                    nextFairyHealth.ApplyLethalDamage(initialKillerRole);
                }
            }
        }
        else
        {
            Debug.LogError("[DelayedActionProcessor] FairyRegistry instance not found! Cannot find next fairy.", this);
        }

        // 5. Destroy self
        Destroy(gameObject);
    }
} 