using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerDeathBomb : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float deathBombRadius = 5f;
    [SerializeField] private LayerMask bulletLayerMask; // Assign the layer your bullets are on

    // Call this method from PlayerHealth on the server
    public void ExecuteBomb()
    {
        if (!IsServer) 
        { 
            // Debug.LogWarning("ExecuteBomb called on client, ignoring."); // Keep this one? Or remove?
            return; 
        }

        // Debug.Log($"[Server DeathBomb] Player {OwnerClientId} triggering death bomb at {transform.position} with radius {deathBombRadius}");

        // Find all colliders within the radius on the specified bullet layer
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, deathBombRadius, bulletLayerMask);
        
        List<NetworkObject> bulletsToDespawn = new List<NetworkObject>();

        foreach (Collider2D col in colliders)
        {
            // Check if it's a stage bullet we want to destroy
            if (col.CompareTag("StageBullet")) 
            {
                NetworkObject netObj = col.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    bulletsToDespawn.Add(netObj);
                }
            }
            // TODO: Add checks for other enemy types later if needed
        }

        // Debug.Log($"[Server DeathBomb] Found {bulletsToDespawn.Count} bullets to despawn.");

        // Despawn collected bullets
        foreach (NetworkObject bulletNetObj in bulletsToDespawn)
        {
            bulletNetObj.Despawn();
        }
    }
} 