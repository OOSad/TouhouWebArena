using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

/// <summary>
/// A server-authoritative singleton responsible for managing and triggering character-specific Extra Attacks.
/// This manager is typically called when a player fulfills a specific condition (e.g., defeating a certain enemy type).
/// It determines the correct extra attack prefab and spawn logic based on the attacking character and targets the opponent.
/// </summary>
public class ExtraAttackManager : NetworkBehaviour
{
    /// <summary>
    /// Singleton instance of the ExtraAttackManager.
    /// </summary>
    [Tooltip("Singleton instance.")]
    public static ExtraAttackManager Instance { get; private set; }

    [Header("Extra Attack Prefabs")]
    [Tooltip("Prefab for Reimu Hakurei's extra attack (likely Homing Orbs). Required if Reimu is playable.")]
    [SerializeField] private GameObject reimuExtraAttackPrefab;
    [Tooltip("Prefab for Marisa Kirisame's extra attack (likely Earthlight Ray). Required if Marisa is playable.")]
    [SerializeField] private GameObject marisaExtraAttackPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnDestroy();
    }

    // --- Core Attack Trigger Logic ---
    /// <summary>
    /// Server-only internal method to trigger an extra attack.
    /// Called by other server systems (e.g., when a specific fairy is killed) to initiate the attack.
    /// Determines the correct prefab and spawn logic based on the attacker's character, finds the appropriate
    /// spawn area/target for the opponent, and executes the spawn logic.
    /// </summary>
    /// <param name="attackerData">PlayerData of the player initiating the attack.</param>
    /// <param name="opponentRole">The PlayerRole of the opponent who will be targeted by the attack.</param>
    public void TriggerExtraAttackInternal(PlayerData attackerData, PlayerRole opponentRole)
    {
        if (!IsServer) return;

        string attackerCharacter = attackerData.SelectedCharacter.ToString();
        
        GameObject prefabToSpawn = null;
        Action<GameObject, Transform> spawnLogic = null;
        Transform targetSpawnArea = null;

        switch (attackerCharacter)
        {
            case "HakureiReimu":
                prefabToSpawn = reimuExtraAttackPrefab;
                ReimuExtraAttackOrbSpawner reimuSpawner = FindObjectOfType<ReimuExtraAttackOrbSpawner>();
                if (reimuSpawner == null)
                {
                    return; // Silently return if helper spawner not found
                }
                targetSpawnArea = (opponentRole == PlayerRole.Player1) ? reimuSpawner.GetSpawnZone1() : reimuSpawner.GetSpawnZone2();

                // Restore original delegate logic
                spawnLogic = (prefab, spawnArea) => {
                    try
                    {
                        if(spawnArea != null)
                        {
                            GameObject instance = Instantiate(prefab, spawnArea.position, Quaternion.identity);
                            if (instance == null) { return; } // Instantiate check
                            
                            NetworkObject nob = instance.GetComponent<NetworkObject>();
                            if (nob == null) { Destroy(instance); return; } // NetworkObject check
                            
                            nob.Spawn(true); // Spawn with server ownership
                            
                            ReimuExtraAttackOrb orbScript = instance.GetComponent<ReimuExtraAttackOrb>();
                            if (orbScript != null) { orbScript.TargetPlayerRole.Value = opponentRole; } // Set Target Role
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[ExtraAttackManager] Reimu spawnLogic EXCEPTION: {ex.Message}", prefab);
                    }
                };
                break;

            case "KirisameMarisa":
                prefabToSpawn = marisaExtraAttackPrefab;
                MarisaExtraAttackSpawner marisaSpawner = FindObjectOfType<MarisaExtraAttackSpawner>();
                 if (marisaSpawner == null)
                {
                    return; // Silently return if helper spawner not found
                }
                targetSpawnArea = (opponentRole == PlayerRole.Player1) ? marisaSpawner.GetPlayer1TargetArea() : marisaSpawner.GetPlayer2TargetArea();
                float spawnWidth = marisaSpawner.GetSpawnWidth();

                float maxTilt = 0f;
                Quaternion spawnRotation = Quaternion.identity;
                if (prefabToSpawn != null)
                {
                    EarthlightRay prefabRayScript = prefabToSpawn.GetComponent<EarthlightRay>();
                    if (prefabRayScript != null)
                    {
                        maxTilt = prefabRayScript.maxTiltAngle;
                        float tilt = UnityEngine.Random.Range(-maxTilt, maxTilt);
                        spawnRotation = Quaternion.Euler(0, 0, tilt);
                    }
                }

                // Restore original delegate logic
                spawnLogic = (prefab, spawnArea) => {
                     try
                    {
                        if (spawnArea != null)
                        {
                            float randomOffsetX = UnityEngine.Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
                            Vector3 spawnPosition = spawnArea.position + new Vector3(randomOffsetX, 0, 0);
                            GameObject instance = Instantiate(prefab, spawnPosition, spawnRotation);
                            if (instance == null) { return; } // Instantiate check
                            
                            NetworkObject nob = instance.GetComponent<NetworkObject>();
                            if (nob == null) { Destroy(instance); return; } // NetworkObject check
                            
                            nob.Spawn(true); // Spawn with server ownership
                            
                            EarthlightRay rayScript = instance.GetComponent<EarthlightRay>();
                            if (rayScript != null) { rayScript.AttackerRole.Value = attackerData.Role; } // Set Attacker Role
                        }
                    }
                    catch (System.Exception ex)
                    {
                         Debug.LogError($"[ExtraAttackManager] Marisa spawnLogic EXCEPTION: {ex.Message}", prefab);
                    }
                };
                break;

            default:
                 // Unknown character, already logged if default case reached
                 return;
        }

        if (prefabToSpawn == null)
        {
            // Prefab not assigned in Inspector
            return;
        }
        if (targetSpawnArea == null)
        {
            // Helper spawner Get...Area method returned null or wasn't found
             return;
        }

        // Restore original delegate invocation
        if (spawnLogic != null)
        {
            spawnLogic(prefabToSpawn, targetSpawnArea);
        }
        else
        {
            // Should not happen if switch case is correct
        }
    }
} 