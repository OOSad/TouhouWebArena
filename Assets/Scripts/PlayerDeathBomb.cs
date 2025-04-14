using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerDeathBomb : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float deathBombRadius = 5f;
    [SerializeField] private LayerMask bulletLayerMask; // Assign the layer your bullets are on
    [SerializeField] private LayerMask fairyLayerMask;  // Assign the layer your fairies are on

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

        // --- Clear Bullets --- 
        Collider2D[] bulletColliders = Physics2D.OverlapCircleAll(transform.position, deathBombRadius, bulletLayerMask);
        List<NetworkObject> bulletsToDespawn = new List<NetworkObject>();
        foreach (Collider2D col in bulletColliders)
        {
            if (col.CompareTag("StageBullet")) 
            {
                StageSmallBulletMoverScript bulletMover = col.GetComponent<StageSmallBulletMoverScript>();
                NetworkObject netObj = col.GetComponent<NetworkObject>();
                if (bulletMover != null && netObj != null && netObj.IsSpawned && bulletMover.TargetPlayerRole.Value == bombingPlayerRole)
                {
                    bulletsToDespawn.Add(netObj);
                }
            }
        }
        foreach (NetworkObject bulletNetObj in bulletsToDespawn)
        {
            if (bulletNetObj != null && bulletNetObj.IsSpawned) // Double-check before despawning
            {
                 bulletNetObj.Despawn();
            }
        }
        // Debug.Log($"[Server DeathBomb] Despawned {bulletsToDespawn.Count} bullets.");
        // --- End Clear Bullets ---

        // --- Kill Fairies --- 
        Collider2D[] fairyColliders = Physics2D.OverlapCircleAll(transform.position, deathBombRadius, fairyLayerMask);
        int killedFairyCount = 0;
        foreach (Collider2D fairyCol in fairyColliders)
        {
            Fairy fairy = fairyCol.GetComponent<Fairy>();
            if (fairy != null)
            {
                // Call ApplyLethalDamage on the server, passing the role of the player who triggered the bomb
                fairy.ApplyLethalDamage(bombingPlayerRole); 
                killedFairyCount++;
            }
        }
        // Debug.Log($"[Server DeathBomb] Killed {killedFairyCount} fairies.");
        // --- End Kill Fairies ---
    }
} 