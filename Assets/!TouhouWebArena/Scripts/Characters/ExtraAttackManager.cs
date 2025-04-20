using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class ExtraAttackManager : NetworkBehaviour
{
    public static ExtraAttackManager Instance { get; private set; }

    [Header("Extra Attack Settings")]
    [SerializeField] private GameObject reimuExtraAttackPrefab;
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
    // Now public so Fairy.cs can call it directly
    public void TriggerExtraAttackInternal(PlayerData attackerData, PlayerRole opponentRole)
    {
        if (!IsServer) return;

        string attackerCharacter = attackerData.SelectedCharacter.ToString();
        Debug.Log($"[ExtraAttackManager] Triggering for {attackerCharacter} (Role: {attackerData.Role}) against {opponentRole}");

        GameObject prefabToSpawn = null;
        Action<GameObject, Transform> spawnLogic = null;
        Transform targetSpawnArea = null;

        switch (attackerCharacter)
        {
            case "Hakurei Reimu":
                prefabToSpawn = reimuExtraAttackPrefab;
                ReimuExtraAttackOrbSpawner reimuSpawner = FindObjectOfType<ReimuExtraAttackOrbSpawner>();
                if (reimuSpawner == null)
                {
                    Debug.LogError("[ExtraAttackManager] Cannot trigger Reimu Extra Attack: ReimuExtraAttackOrbSpawner component not found.", this);
                    return;
                }
                targetSpawnArea = (opponentRole == PlayerRole.Player1) ? reimuSpawner.GetSpawnZone1() : reimuSpawner.GetSpawnZone2();

                spawnLogic = (prefab, spawnArea) => {
                    if(spawnArea != null)
                    {
                         GameObject instance = Instantiate(prefab, spawnArea.position, Quaternion.identity);
                         NetworkObject nob = instance.GetComponent<NetworkObject>();
                         if (nob != null) nob.Spawn(true);
                         ReimuExtraAttackOrb orbScript = instance.GetComponent<ReimuExtraAttackOrb>();
                         if(orbScript != null) orbScript.TargetPlayerRole.Value = opponentRole;
                         else Debug.LogError("[ExtraAttackManager] Failed to get ReimuExtraAttackOrb script from instantiated prefab!");
                    } else Debug.LogError($"[ExtraAttackManager] Spawn Area Transform for Role {opponentRole} was null (Reimu).");
                };
                break;

            case "Kirisame Marisa":
                prefabToSpawn = marisaExtraAttackPrefab;
                MarisaExtraAttackSpawner marisaSpawner = FindObjectOfType<MarisaExtraAttackSpawner>();
                 if (marisaSpawner == null)
                {
                    Debug.LogError("[ExtraAttackManager] Cannot trigger Marisa Extra Attack: MarisaExtraAttackSpawner component not found.", this);
                    return;
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
                    else Debug.LogError("[ExtraAttackManager] Marisa Extra Attack Prefab is missing EarthlightRay component!");
                }

                spawnLogic = (prefab, spawnArea) => {
                    if (spawnArea != null)
                    {
                        float randomOffsetX = UnityEngine.Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
                        Vector3 spawnPosition = spawnArea.position + new Vector3(randomOffsetX, 0, 0);
                        GameObject instance = Instantiate(prefab, spawnPosition, spawnRotation);
                        NetworkObject nob = instance.GetComponent<NetworkObject>();
                        if (nob != null) nob.Spawn(true);
                        EarthlightRay rayScript = instance.GetComponent<EarthlightRay>();
                        if (rayScript != null) rayScript.AttackerRole.Value = attackerData.Role;
                        else Debug.LogError("[ExtraAttackManager] Failed to get EarthlightRay script from instantiated prefab!");
                    }
                    else Debug.LogError($"[ExtraAttackManager] Spawn Area Transform for Role {opponentRole} was null (Marisa).");
                };
                break;

            default:
                 Debug.LogError($"[ExtraAttackManager] No Extra Attack defined for character: {attackerCharacter}");
                 return;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError($"[ExtraAttackManager] Extra Attack Prefab is null for character: {attackerCharacter}.", this);
            return;
        }
        if (targetSpawnArea == null)
        {
             Debug.LogError($"[ExtraAttackManager] Target Extra Attack Spawn Area for Role {opponentRole} (Character: {attackerCharacter}) is not assigned or found.", this);
             return;
        }

        if (spawnLogic != null) spawnLogic(prefabToSpawn, targetSpawnArea);
        else Debug.LogError($"[ExtraAttackManager] Internal Error: Spawn logic not defined for {attackerCharacter}.");
    }
} 