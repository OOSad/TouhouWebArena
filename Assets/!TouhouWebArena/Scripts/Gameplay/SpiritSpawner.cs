using UnityEngine;
using System.Collections;
using Unity.Netcode;

/// <summary>
/// [Server Only] Manages the spawning of Spirit entities, both periodically during gameplay and
/// as "revenge" spawns when a spirit is destroyed by a player.
/// Uses the <see cref="NetworkObjectPool"/> to manage Spirit instances.
/// Has a configurable chance to aim spawned Spirits towards the corresponding player.
/// Requires references to spawn zone transforms and the Spirit prefab.
/// </summary>
public class SpiritSpawner : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("Transform defining the center of the spawn zone for Player 1's side.")]
    [SerializeField] private Transform spawnZone1;
    [Tooltip("Transform defining the center of the spawn zone for Player 2's side.")]
    [SerializeField] private Transform spawnZone2;
    [Tooltip("The prefab for the Spirit item. Must have NetworkObject, PoolableObjectIdentity, and SpiritController components.")]
    [SerializeField] private GameObject spiritPrefab;

    [Header("Spawning Configuration")]
    [Tooltip("The dimensions (Width, Height) of the rectangular spawn zones centered on the spawnZone transforms.")]
    [SerializeField] private Vector2 spawnZoneSize = new Vector2(2f, 1f);
    [Tooltip("The dimensions (Width, Height) of the zone used for placing revenge-spawned spirits, centered on the target player's spawn zone.")]
    [SerializeField] private Vector2 revengeSpawnZoneSize = new Vector2(7f, 1f); // Default value
    [Tooltip("Average time in seconds between spawning a new Spirit in each zone.")]
    [SerializeField] private float spawnInterval = 2.0f;
    [Tooltip("Probability (0-1) that a newly spawned Spirit will initially move towards the corresponding player.")]
    [SerializeField, Range(0f, 1f)] private float aimAtPlayerChance = 0.25f;

    // Default Spirit Prefab ID to be used by clients
    [Tooltip("The PrefabID string for the standard spirit, used by ClientGameObjectPool.")]
    [SerializeField] private string spiritPrefabID = "Spirit"; // Example ID

    /// <summary>If false, the spawning coroutine will pause.</summary>
    public bool isDebugSpawningEnabled = true;

    // --- Singleton Pattern --- 
    public static SpiritSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        { 
            Destroy(gameObject); 
        } 
        else 
        { 
            Instance = this; 
        }
    }

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// Validates required references and starts the <see cref="SpawnSpirits"/> coroutine if validation passes.
    /// </summary>
    private void Start()
    {
        if (!ValidateReferences()) return;
        StartCoroutine(SpawnSpirits());
    }

    /// <summary>
    /// Validates that all required component references and prefab configurations are set correctly.
    /// Disables the component and logs errors if validation fails.
    /// </summary>
    /// <returns>True if all references and configurations are valid, false otherwise.</returns>
    private bool ValidateReferences()
    {
        if (spawnZone1 == null || spawnZone2 == null)
        {
            Debug.LogError("SpiritSpawner: Spawn Zone 1 or Spawn Zone 2 is not assigned.", this);
            enabled = false;
            return false;
        }

        if (spiritPrefab == null)
        {
            Debug.LogError("SpiritSpawner: Spirit Prefab is not assigned.", this);
            enabled = false;
            return false;
        }

        // Get reference to the SpiritController component on the prefab for later use.
        // if (spiritPrefab.GetComponent<SpiritController>() == null)
        // {
        //     // This is a warning, not an error, as client-side spirits might not have this.
        //     Debug.LogWarning("SpiritSpawner: Spirit Prefab is missing a SpiritController component. This might be intended if it's purely client-side.", this);
        // }
        // Also check for PoolableObjectIdentity
        // if (spiritPrefab.GetComponent<PoolableObjectIdentity>() == null || string.IsNullOrEmpty(spiritPrefab.GetComponent<PoolableObjectIdentity>().PrefabID))
        // {
        // Debug.LogError("SpiritSpawner: Spirit Prefab is missing PoolableObjectIdentity or has an empty PrefabID.", this);
        // enabled = false;
        // return false;
        // }
        return true;
    }

    /// <summary>
    /// [Server Only] Coroutine loop that periodically spawns spirits in both zones.
    /// Contains the main spawning logic, waiting <see cref="spawnInterval"/> seconds between spawns.
    /// </summary>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator SpawnSpirits()
    {
        if (!IsServer) yield break; // Only server spawns

        // Initial delay
        yield return new WaitForSeconds(Random.Range(0f, spawnInterval));

        while (true)
        {
            // Pause spawning if debug flag is false
            while (!isDebugSpawningEnabled)
            {
                yield return null; 
            }

            yield return new WaitForSeconds(spawnInterval);

            // ADDED CHECK: Re-check flag immediately after waiting, before spawning
            if (!isDebugSpawningEnabled) 
            {
                continue; // Skip the rest of this loop iteration if spawning was disabled during the wait
            }

            SpawnSpiritInZone(spawnZone1);
            SpawnSpiritInZone(spawnZone2);
        }
    }

    /// <summary>
    /// [Server Only] Spawns a single spirit within the specified zone.
    /// Calculates a random position, gets a Spirit instance from the <see cref="NetworkObjectPool"/>,
    /// determines if it should aim at the player, finds the player transform if necessary,
    /// spawns the NetworkObject, and calls <see cref="SpiritController.Initialize"/>.
    /// </summary>
    /// <param name="zoneCenter">The Transform representing the center of the spawn zone.</param>
    private void SpawnSpiritInZone(Transform zoneCenter)
    {
        // Calculate spawn position
        Vector3 center = zoneCenter.position;
        float randomX = Random.Range(-spawnZoneSize.x / 2f, spawnZoneSize.x / 2f);
        float randomY = Random.Range(-spawnZoneSize.y / 2f, spawnZoneSize.y / 2f);
        Vector3 spawnPosition = new Vector3(center.x + randomX, center.y + randomY, center.z);

        // Determine target player and aim chance
        PlayerRole targetRole = (zoneCenter == spawnZone1) ? PlayerRole.Player1 : PlayerRole.Player2;
        PlayerRole owningSide = targetRole; // For normal spawns, the owning side is the same as the target role's side
        bool shouldAim = Random.value < aimAtPlayerChance;
        ulong targetNetworkObjectId = 0;

        if (shouldAim)
        {
            if (PlayerDataManager.Instance != null && NetworkManager.Singleton != null)
            {
                PlayerData? playerData = PlayerDataManager.Instance.GetPlayerDataByRole(targetRole);
                if (playerData.HasValue)
                {
                    NetworkObject playerNO = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
                    if (playerNO != null)
                    {
                        targetNetworkObjectId = playerNO.NetworkObjectId;
                    }
                    else
                    {
                        Debug.LogWarning($"[SpiritSpawner] SpawnSpiritInZone: Could not find Player NetworkObject for ClientId {playerData.Value.ClientId} (Role {targetRole}). Spirit will not aim.", this);
                        shouldAim = false;
                    }
                }
                else
                {
                    Debug.LogWarning($"[SpiritSpawner] SpawnSpiritInZone: Could not find PlayerData for Role {targetRole}. Spirit will not aim.", this);
                    shouldAim = false;
                }
            }
            else
            {
                Debug.LogWarning("[SpiritSpawner] SpawnSpiritInZone: PlayerDataManager or NetworkManager not available. Spirit will not aim.", this);
                shouldAim = false;
            }
        }

        if (ClientSpiritSpawnHandler.Instance != null)
        {
            ClientSpiritSpawnHandler.Instance.SpawnSpiritClientRpc(
                owningSide, 
                spiritPrefabID, 
                spawnPosition, 
                shouldAim, 
                targetNetworkObjectId,
                false /*isRevengeSpawn*/, 
                2.0f, // initialVelocity
                0     // spiritType
            );
        }
        else
        {
            Debug.LogError("[SpiritSpawner] SpawnSpiritInZone: ClientSpiritSpawnHandler.Instance is null. Cannot send SpawnSpiritClientRpc.", this);
        }
    }

    /// <summary>
    /// Draws wireframe cubes in the editor scene view to visualize the spawn zones.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; // Use a different color for spirit zones
        if (spawnZone1 != null)
        {
            Gizmos.DrawWireCube(spawnZone1.position, new Vector3(spawnZoneSize.x, spawnZoneSize.y, 0f));
        }
        if (spawnZone2 != null)
        {
            Gizmos.DrawWireCube(spawnZone2.position, new Vector3(spawnZoneSize.x, spawnZoneSize.y, 0f));
        }
    }

    /// <summary>
    /// [Server Only] Sets the debug flag to enable/disable spirit spawning.
    /// </summary>
    /// <param name="enabled">True to enable spawning, false to disable.</param>
    public void SetSpawningEnabledServer(bool enabled)
    {
        if (!IsServer) return;
        isDebugSpawningEnabled = enabled;
        Debug.Log($"[SpiritSpawner] Spawning enabled: {isDebugSpawningEnabled}", this);
    }

    /// <summary>
    /// [Server Only] Spawns a "revenge" Spirit on the opponent's side.
    /// This is typically called when a player destroys an enemy spirit.
    /// The revenge spirit will be aimed at the player who triggered the revenge spawn.
    /// </summary>
    /// <param name="targetPlayerRole">The player role that the revenge spirit will target (i.e., the player who just killed a spirit).</param>
    public void SpawnRevengeSpirit(PlayerRole targetPlayerRole)
    {
        if (!IsServer) return;

        Transform zoneToSpawnIn = (targetPlayerRole == PlayerRole.Player1) ? spawnZone1 : spawnZone2;
        PlayerRole owningSideOfRevengeSpirit = targetPlayerRole; // Revenge spirit spawns on the side of the player it will target

        Vector3 center = zoneToSpawnIn.position;
        float randomX = Random.Range(-revengeSpawnZoneSize.x / 2f, revengeSpawnZoneSize.x / 2f);
        float randomY = Random.Range(-revengeSpawnZoneSize.y / 2f, revengeSpawnZoneSize.y / 2f);
        Vector3 spawnPosition = new Vector3(center.x + randomX, center.y + randomY, center.z);

        ulong targetNetworkObjectId = 0;
        bool shouldAimAtTarget = Random.value < aimAtPlayerChance;

        if (shouldAimAtTarget)
        {
            if (PlayerDataManager.Instance != null)
        {
                PlayerData? playerData = PlayerDataManager.Instance.GetPlayerDataByRole(targetPlayerRole);
                if (playerData.HasValue && playerData.Value.ClientId != 0)
                {
                    NetworkObject playerNO = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
                    if (playerNO != null)
                    {
                        targetNetworkObjectId = playerNO.NetworkObjectId;
                    }
                    else
        {
                        Debug.LogError($"[SpiritSpawner] SpawnRevengeSpirit: Could not find Player NetworkObject for ClientId {playerData.Value.ClientId} (Role {targetPlayerRole}). Revenge spirit will not aim.", this);
                        shouldAimAtTarget = false;
                    }
                }
                else
        {    
                    Debug.LogError($"[SpiritSpawner] SpawnRevengeSpirit: Could not find PlayerData or ClientId for Role {targetPlayerRole}. Revenge spirit will not aim.", this);
                    shouldAimAtTarget = false;
                }
            }
            else
        {   
                Debug.LogError("[SpiritSpawner] SpawnRevengeSpirit: PlayerDataManager not available. Revenge spirit will not aim.", this);
                shouldAimAtTarget = false;
            }
        }

        if (!shouldAimAtTarget)
        {
            targetNetworkObjectId = 0;
        }

        if (ClientSpiritSpawnHandler.Instance != null)
        {
            ClientSpiritSpawnHandler.Instance.SpawnSpiritClientRpc(
                owningSideOfRevengeSpirit,
                spiritPrefabID,
                spawnPosition,
                shouldAimAtTarget,
                targetNetworkObjectId,
                true /*isRevengeSpawn*/,
                2.0f, // initialVelocity
                0     // spiritType (0 for normal, could add a revenge-specific type later)
            );
        }
        else
        {
            Debug.LogError("[SpiritSpawner] SpawnRevengeSpirit: ClientSpiritSpawnHandler.Instance is null. Cannot send SpawnSpiritClientRpc.", this);
        }
    }

    public override void OnDestroy()
    {
        // Call the base method to ensure NetworkBehaviour cleanup runs
        base.OnDestroy();

        if (Instance == this)
        { 
            Instance = null;
        }
    }
} 