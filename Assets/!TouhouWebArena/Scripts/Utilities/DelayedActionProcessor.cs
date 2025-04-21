using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List
using Unity.Netcode; // For NetworkObject spawning

// This temporary object handles delayed actions after a Fairy dies,
// allowing the Fairy GameObject to be destroyed immediately.
// It holds data directly instead of component references.
public class DelayedActionProcessor : MonoBehaviour
{
    // Data needed for actions
    private Vector3 _position;
    private PlayerRole _killerRole;
    private float _delay;
    private GameObject _shockwavePrefab;
    private System.Guid _lineId;
    private int _indexInLine;

    // Initialize with data and start the coroutine
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

        // --- Trigger Chain Reaction (Next Fairy) ---
        TriggerNextFairyInLine();

        // Destroy this processor object
        Destroy(gameObject);
    }

    // Spawns the shockwave effect
    private void SpawnShockwaveEffect()
    {
        // Instantiate the prefab on the server
        GameObject shockwaveInstance = Instantiate(_shockwavePrefab, _position, Quaternion.identity);
        NetworkObject networkObject = shockwaveInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn(true); // true = destroy with server
        }
        else
        {
            
            Destroy(shockwaveInstance); // Clean up the non-networked instance
        }
    }

    // Finds and triggers the next fairy in the line
    private void TriggerNextFairyInLine()
    {
        if (FairyRegistry.Instance != null)
        {
            Fairy nextFairy = FairyRegistry.Instance.FindNextInLine(_lineId, _indexInLine);
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