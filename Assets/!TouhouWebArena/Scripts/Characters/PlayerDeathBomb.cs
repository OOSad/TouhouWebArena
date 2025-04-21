using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq; // Added for OfType<T>

[RequireComponent(typeof(CharacterStats))]
public class PlayerDeathBomb : NetworkBehaviour
{
    private CharacterStats characterStats;

    private void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
        // if (characterStats == null) 
    }

    // Call this method from PlayerHealth on the server
    public void ExecuteBomb()
    {
        if (!IsServer)
        {
            // Debug.LogWarning("ExecuteBomb called on client, ignoring.");
            return;
        }
        // Debug.Log($"[Server DeathBomb - {OwnerClientId}] ExecuteBomb called."); // <-- REMOVE Start Log

        // --- Get Bombing Player's Role --- 
        PlayerRole bombingPlayerRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null)
        {
            // Use top-level PlayerData
            PlayerData? playerData = PlayerDataManager.Instance.GetPlayerData(OwnerClientId);
            if (playerData.HasValue)
            {
                bombingPlayerRole = playerData.Value.Role;
            }
            else
            {
                return; // Can't determine role, abort bomb
            }
        }
        else
        {
            return; // Abort if manager is missing
        }

        if (bombingPlayerRole == PlayerRole.None)
        {
            return;
        }
        // --- End Get Bombing Player's Role ---

        // Ensure stats are available before proceeding
        if (characterStats == null)
        {
            return;
        }
        float currentBombRadius = characterStats.GetDeathBombRadius(); // Read radius from stats

        // --- Clear Objects using Interface --- 
        // Find all components implementing IClearableByBomb in the scene
        // Includes inactive objects just in case, but checks validity later
        var clearables = FindObjectsOfType<MonoBehaviour>(true).OfType<IClearableByBomb>();
        // Debug.Log($"[Server DeathBomb - {OwnerClientId}] Found {clearables.Count()} potential objects with IClearableByBomb."); // <-- REMOVE Count Log

        int clearedCount = 0;
        foreach (IClearableByBomb clearable in clearables)
        {
            // Ensure it's a valid MonoBehaviour in the scene
            if (clearable is MonoBehaviour mb && mb.gameObject.scene.IsValid())
            {
                Vector3 position = mb.transform.position;
                 // <-- REMOVE Found Object Info Log -->
                // Debug.Log($"[Server DeathBomb - {OwnerClientId}] Checking clearable: {mb.gameObject.name} at {position} (Type: {clearable.GetType().Name})"); 

                // 1. Check distance
                float distance = Vector3.Distance(transform.position, position); // Calculate distance
                if (distance <= currentBombRadius) // Use radius from stats
                {
                    // 2. Check side
                    bool correctSide = false;
                    if (bombingPlayerRole == PlayerRole.Player1 && PlayerMovement.player1Bounds.Contains(new Vector2(position.x, position.y)))
                    {
                        correctSide = true;
                    }
                    else if (bombingPlayerRole == PlayerRole.Player2 && PlayerMovement.player2Bounds.Contains(new Vector2(position.x, position.y)))
                    {
                        correctSide = true;
                    }

                    // 3. If distance and side are correct, clear it
                    if (correctSide)
                    {
                        // <-- REMOVE Clearing Action Log
                        // The ClearByBomb() method on the object itself handles the necessary ServerRpc call
                        clearable.ClearByBomb(bombingPlayerRole);
                        clearedCount++;
                    }
                    // Optional: Log why it wasn't cleared if distance/side failed
                    // else { }
                }
                // else { }
            }
             else
             {
                 // Keep these warnings for invalid objects found
                 // if (clearable is MonoBehaviour mbInvalid) 
                 // else 
             }
        }
        // Keep this summary log
        // Debug.Log($"[Server DeathBomb] Cleared {clearedCount} objects for player {bombingPlayerRole} (Client {OwnerClientId}).");
        // --- End Clear Objects using Interface ---
    }
} 