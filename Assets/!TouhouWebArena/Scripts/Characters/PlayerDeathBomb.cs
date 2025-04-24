using UnityEngine;
using Unity.Netcode;
// using System.Collections.Generic; // No longer needed for FindObjectsOfType
// using System.Linq; // No longer needed for OfType<T>
using TouhouWebArena; // Add namespace for IClearable and PlayerRole

/// <summary>
/// Handles the server-side logic for the player's "death bomb" effect.
/// This effect triggers automatically after a player takes damage and their invincibility period ends (see <see cref="PlayerHealth"/>).
/// It finds all colliders within a radius and attempts to clear any objects implementing <see cref="IClearable"/>
/// using a forced clear (ignores normal clearability rules).
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class PlayerDeathBomb : NetworkBehaviour
{
    private CharacterStats characterStats;

    private void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
        if (characterStats == null)
        {
            Debug.LogError("PlayerDeathBomb requires CharacterStats component!", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Executes the death bomb logic ONLY on the server.
    /// Uses Physics2D.OverlapCircleAll to find objects within the bomb radius
    /// (defined by <see cref="CharacterStats.GetDeathBombRadius"/>) and calls <see cref="IClearable.Clear"/>
    /// with forceClear set to true.
    /// This method is typically called by <see cref="PlayerHealth"/> after the invincibility timer.
    /// </summary>
    public void ExecuteBomb()
    {
        if (!IsServer)
        {
            return;
        }

        // --- Get Bombing Player's Role --- 
        PlayerRole bombingPlayerRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? playerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
            if (playerData.HasValue)
            {
                bombingPlayerRole = playerData.Value.Role;
            }
            else
            {
                Debug.LogError($"[PlayerDeathBomb] Could not get PlayerData for bombing client {OwnerClientId}. Aborting bomb.", gameObject);
                return; // Can't determine role, abort bomb
            }
        }
        else
        {
             Debug.LogError("[PlayerDeathBomb] PlayerDataManager instance not found. Aborting bomb.", gameObject);
            return; // Abort if manager is missing
        }

        // Role check might not be strictly necessary if logic doesn't depend on it, but good for logging/context.
        // if (bombingPlayerRole == PlayerRole.None) return; 

        float currentBombRadius = characterStats.GetDeathBombRadius(); // Read radius from stats

        // --- Clear Objects using Physics Overlap and Interface --- 
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentBombRadius);
        int clearedCount = 0;

        foreach (Collider2D hit in hits)
        {
            // Optional: Ignore the player's own collider if it has IClearable (unlikely but possible)
            // if (hit.transform.root == this.transform.root) continue; 

            IClearable clearable = hit.GetComponent<IClearable>(); // Check directly on hit collider's object
            // If clearable components might be on parent objects of the collider:
            // IClearable clearable = hit.GetComponentInParent<IClearable>();

            if (clearable != null)
            {
                // Player Bomb is a special/forced clear. Side checks are omitted as per requirement.
                clearable.Clear(true, bombingPlayerRole); // Pass true for forced clear
                clearedCount++;
            }
        }
        // Optional: Keep summary log
        // Debug.Log($"[Server DeathBomb] Attempted forced clear on {clearedCount} objects for player {bombingPlayerRole} (Client {OwnerClientId}).");
    }
} 