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

        // --- Get Bombing Player's Role --- 
        PlayerRole bombingPlayerRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.PlayerData? playerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
            if (playerData.HasValue)
            {
                bombingPlayerRole = playerData.Value.Role;
            }
            else
            {
                Debug.LogError($"[Server DeathBomb] Could not find PlayerData for bombing client {OwnerClientId}. Cannot determine role.");
                return; // Can't determine role, abort bomb
            }
        }
        else
        {
            Debug.LogError("[Server DeathBomb] PlayerDataManager instance not found. Cannot determine role.");
            return; // Abort if manager is missing
        }
        
        if (bombingPlayerRole == PlayerRole.None)
        {
            Debug.LogError($"[Server DeathBomb] Bombing client {OwnerClientId} has Role None. Aborting bomb.");
            return;
        }
        // --- End Get Bombing Player's Role ---

        // Find all colliders within the radius on the specified bullet layer
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, deathBombRadius, bulletLayerMask);
        
        List<NetworkObject> bulletsToDespawn = new List<NetworkObject>();

        foreach (Collider2D col in colliders)
        {
            // Check if it's a stage bullet we want to destroy
            if (col.CompareTag("StageBullet")) 
            {
                StageSmallBulletMoverScript bulletMover = col.GetComponent<StageSmallBulletMoverScript>();
                NetworkObject netObj = col.GetComponent<NetworkObject>();

                // --- Role Check --- 
                if (bulletMover != null && netObj != null && netObj.IsSpawned)
                {
                    // Only add the bullet if its TargetPlayerRole matches the player triggering the bomb
                    if (bulletMover.TargetPlayerRole.Value == bombingPlayerRole)
                    {
                        bulletsToDespawn.Add(netObj);
                    }
                }
                // --- End Role Check ---
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