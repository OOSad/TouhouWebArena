using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// [Server Only Debug Script]
/// Instantiates and spawns the Extra Attack prefabs at fixed locations
/// for testing purposes, bypassing the normal trigger logic.
/// Attach to a GameObject in the scene and assign prefabs in the Inspector.
/// </summary>
public class DebugExtraAttackSpawner : NetworkBehaviour
{
    [Header("Prefabs to Spawn")]
    [SerializeField] private GameObject reimuExtraPrefab;
    [SerializeField] private GameObject marisaExtraPrefab;

    [Header("Spawn Positions")]
    [SerializeField] private Vector3 player1SpawnPos = new Vector3(-5, 5, 0); // Example position in P1 area
    [SerializeField] private Vector3 player2SpawnPos = new Vector3(5, 5, 0);  // Example position in P2 area

    private bool _spawned = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only run on the server, and only run once
        if (!IsServer || _spawned)
        {
            enabled = false; // Disable component on clients or after first run
            return;
        }

        // Start a coroutine to handle the delayed spawn
        StartCoroutine(DelayedSpawnCoroutine());
    }

    private IEnumerator DelayedSpawnCoroutine()
    {
        // Prevent running again if somehow called multiple times
        if (_spawned) yield break;
        _spawned = true;

        Debug.Log("[DebugExtraAttackSpawner] OnNetworkSpawn (Server). Waiting for delay before debug spawn...");

        // Wait for a short period after network spawn/scene load
        yield return new WaitForSeconds(2.0f); 

        Debug.Log("[DebugExtraAttackSpawner] Delay finished. Attempting debug spawn...");

        // Spawn Reimu's Attack
        SpawnPrefab(reimuExtraPrefab, player1SpawnPos, "Reimu");

        // Spawn Marisa's Attack
        SpawnPrefab(marisaExtraPrefab, player2SpawnPos, "Marisa");

        Debug.Log("[DebugExtraAttackSpawner] Debug spawn attempts finished.");
        enabled = false; // Disable self after running
    }

    private void SpawnPrefab(GameObject prefab, Vector3 position, string characterName)
    {
        if (prefab == null)
        {
            Debug.LogError($"[DebugExtraAttackSpawner] Prefab for {characterName} is not assigned!");
            return;
        }

        try
        {
            Debug.Log($"[DebugExtraAttackSpawner] PRE-INSTANTIATE for {characterName} ({prefab.name}) at {position}");
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            Debug.Log($"[DebugExtraAttackSpawner] POST-INSTANTIATE for {characterName}. Instance is null: {instance == null}");

            if (instance != null)
            {
                Debug.Log($"[DebugExtraAttackSpawner] Attempting to get NetworkObject for {characterName} instance...");
                NetworkObject nob = instance.GetComponent<NetworkObject>();
                if (nob != null)
                {
                    Debug.Log($"[DebugExtraAttackSpawner] Attempting to Spawn NetworkObject for {characterName} ({nob.NetworkObjectId})...");
                    nob.Spawn(true); // Spawn with server ownership
                    Debug.Log($"[DebugExtraAttackSpawner] Spawn call executed for {characterName}.");
                }
                else
                {
                    Debug.LogError($"[DebugExtraAttackSpawner] Instance for {characterName} is missing NetworkObject component!", instance);
                }
            }
            else
            {
                 Debug.LogError($"[DebugExtraAttackSpawner] Instantiate failed for {characterName}!");
            }
        }
        catch (System.Exception ex)
        {
             Debug.LogError($"[DebugExtraAttackSpawner] EXCEPTION during {characterName} spawn! Msg: {ex.Message}\nTrace: {ex.StackTrace}", prefab);
        }
    }
} 