using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List
using Unity.Netcode; // For NetworkObject spawning

/// <summary>
/// A temporary MonoBehaviour created by a <see cref="FairyController"/> upon its death
/// to handle actions that need to occur after a short delay (e.g., spawning effects,
/// triggering the next fairy in a chain reaction) without keeping the original Fairy object alive.
/// This processor holds the necessary data and destroys itself after executing the delayed actions.
/// Note: This object itself is NOT networked; it executes server-side logic initiated by the Fairy.
/// </summary>
public class DelayedActionProcessor : MonoBehaviour
{
    // Data needed for actions
    private Vector3 _position;
    private PlayerRole _killerRole;
    private float _delay;
    private GameObject _shockwavePrefab;
    private System.Guid _lineId;
    private int _indexInLine;

    /// <summary>
    /// Initializes the processor with the required data and starts the delayed action coroutine.
    /// </summary>
    /// <param name="position">The position where the original Fairy died (used for effects).</param>
    /// <param name="killer">The <see cref="PlayerRole"/> who killed the Fairy (None if not killed by a player).</param>
    /// <param name="delay">The time in seconds to wait before executing actions.</param>
    /// <param name="shockwavePrefab">The shockwave effect prefab to spawn.</param>
    /// <param name="lineId">The Guid of the line the Fairy belonged to.</param>
    /// <param name="indexInLine">The index of the Fairy within its line.</param>
    public void InitializeAndRun(Vector3 position, PlayerRole killer, float delay, 
                                 GameObject shockwavePrefab, System.Guid lineId, int indexInLine)
    {
        _position = position;
        _killerRole = killer;
        _delay = delay;
        _shockwavePrefab = shockwavePrefab;
        _lineId = lineId;
        _indexInLine = indexInLine;

        // Basic validation
        if (_shockwavePrefab == null)
        {
            
        }

        StartCoroutine(RunDelayedActions());
    }

    // Coroutine to perform delayed actions
    private IEnumerator RunDelayedActions()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(_delay);

        // --- Trigger Death Effects --- 
        if (_shockwavePrefab != null)
        {
            SpawnShockwaveEffect();
        }

        // --- Trigger Chain Reaction (Next Fairy) ONLY if killed by a player --- 
        if (_killerRole != PlayerRole.None)
        {
            TriggerNextFairyInLine();
        }
        // ------------------------------------------------------------------

        // Destroy this processor object
        Destroy(gameObject);
    }

    // Spawns the shockwave effect using the NetworkObjectPool
    private void SpawnShockwaveEffect()
    {
        if (_shockwavePrefab == null) 
        {
            // Debug.LogError("[DelayedActionProcessor] Shockwave prefab is null, cannot spawn effect.", this);
            return; 
        }

        // Get the PoolableObjectIdentity to find the PrefabID
        PoolableObjectIdentity identity = _shockwavePrefab.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
            // Debug.LogError($"[DelayedActionProcessor] Shockwave prefab '{_shockwavePrefab.name}' is missing PoolableObjectIdentity or PrefabID! Cannot use pool.", this);
            // Fallback? Maybe instantiate directly as before, but log error.
            // For now, just return to prevent pool errors.
            return;
        }

        string prefabID = identity.PrefabID;

        // Get object from pool
        NetworkObject pooledNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);

        if (pooledNetworkObject != null)
        {
            // Position, activate, and spawn the pooled object
            pooledNetworkObject.transform.position = _position;
            pooledNetworkObject.transform.rotation = Quaternion.identity;
            pooledNetworkObject.gameObject.SetActive(true);
            pooledNetworkObject.Spawn(false); // false = pool manages lifetime
        }
        else
        {
            // Log error if pool failed to return an object
            // Debug.LogError($"[DelayedActionProcessor] Failed to get shockwave with PrefabID '{prefabID}' from NetworkObjectPool.", this);
        }
    }

    // Finds and triggers the next fairy in the line
    private void TriggerNextFairyInLine()
    {
        if (FairyRegistry.Instance != null)
        {
            FairyController nextFairy = FairyRegistry.Instance.FindNextInLine(_lineId, _indexInLine);
            if (nextFairy != null)
            {
                // Apply damage to the specific next fairy
                nextFairy.ApplyLethalDamage(_killerRole);
            }
            // else: No next fairy found (end of line or already destroyed)
        }
        else
        {
            
        }
    }
} 