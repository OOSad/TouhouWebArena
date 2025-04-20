using UnityEngine;
using Unity.Netcode;

// Handles visual/audio effects upon Fairy death 
[RequireComponent(typeof(Fairy))] // Requires the main Fairy script
public class FairyDeathEffects : NetworkBehaviour // Needs NetworkBehaviour for IsServer check 
{
    [Header("Effects")]
    [SerializeField] private GameObject shockwavePrefab; // Assign the FairyShockwave prefab here

    // --- Public method to trigger effects immediately --- 
    // This might be useful elsewhere, or can be removed if truly only used by the processor now.
    public void TriggerEffects(Vector3 position)
    {
        if (!IsServer) return; // Should only run on server

        if (shockwavePrefab != null)
        {
            // Instantiate the prefab on the server
            GameObject shockwaveInstance = Instantiate(shockwavePrefab, position, Quaternion.identity);
            NetworkObject networkObject = shockwaveInstance.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn(true); // true = destroy with server
            }
            else
            {
                 Debug.LogError("[FairyDeathEffects] Shockwave prefab is missing NetworkObject component!", this);
                 Destroy(shockwaveInstance); // Clean up the non-networked instance
            }
        }
    }

    // Getter for the prefab needed by DelayedActionProcessor 
    public GameObject GetShockwavePrefab() 
    {
        return shockwavePrefab;
    }
} 