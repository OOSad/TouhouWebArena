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
    [Tooltip("Maximum number of spirits allowed per player side (checked during revenge spawns).")]
    [SerializeField] private int maxSpiritsPerSide = 10; // Default value
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

        if (spiritPrefab.GetComponent<SpiritController>() == null)
        {
            Debug.LogWarning("SpiritSpawner: Spirit Prefab is missing a SpiritController component. This might be intended if it's purely client-side.", this);
            // We might not need a server-side controller anymore.
            // enabled = false;
            // return false;
        }
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

        // --- Pool Integration --- 
        // if (spiritPrefab == null) return; // Should be validated earlier

        // PoolableObjectIdentity identity = spiritPrefab.GetComponent<PoolableObjectIdentity>();
        // if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        // {
        // Debug.LogError($"[SpiritSpawner] Spirit prefab '{spiritPrefab.name}' is missing PoolableObjectIdentity or PrefabID! Cannot spawn.", this);
        // return;
        // }
        // string prefabID = identity.PrefabID;

        // Get object from pool
        // NetworkObject pooledNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);
        // if (pooledNetworkObject == null)
        // {
        // Debug.LogError($"[SpiritSpawner] Failed to get Spirit with PrefabID '{prefabID}' from pool.", this);
        // return;
        // }
        // ----------------------

        // Get components from pooled object
        // SpiritController spiritController = pooledNetworkObject.GetComponent<SpiritController>();
        // NetworkObject networkObject = pooledNetworkObject; // Already have reference

        // Position and Activate (BEFORE Initialize and Spawn)
        // pooledNetworkObject.transform.position = spawnPosition;
        // pooledNetworkObject.transform.rotation = Quaternion.identity;
        // pooledNetworkObject.gameObject.SetActive(true); 

        // Determine target player and aim chance
        PlayerRole targetRole = (zoneCenter == spawnZone1) ? PlayerRole.Player1 : PlayerRole.Player2;
        bool shouldAim = Random.value < aimAtPlayerChance;
        // Transform targetPlayerTransform = null; // Client will resolve target transform
        ulong targetPlayerClientId = 0;


        // Get player transform if aiming
        if (shouldAim)
        {
            // --- Access Player Transform (Requires PlayerDataManager or similar) ---
            if (PlayerDataManager.Instance != null && NetworkManager.Singleton != null)
            {
                PlayerData? playerData = PlayerDataManager.Instance.GetPlayerDataByRole(targetRole);
                if (playerData.HasValue)
                {
                    targetPlayerClientId = playerData.Value.ClientId;
                    // NetworkObject playerNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
                    // if (playerNetObj != null)
                    // {
                    // targetPlayerTransform = playerNetObj.transform;
                    // }
                    // else
                    // {
                    // Debug.LogWarning($"[SpiritSpawner] Could not find NetworkObject for Player {targetRole}. Spirit will not aim.", this);
                    // shouldAim = false; // Fallback to not aiming
                    // }
                }
                else
                {
                    Debug.LogWarning($"[SpiritSpawner] Could not find PlayerData for Role {targetRole}. Spirit will not aim.", this);
                    shouldAim = false; // Fallback to not aiming
                }
            }
            else
            {
                Debug.LogWarning("[SpiritSpawner] PlayerDataManager or NetworkManager not available. Spirit will not aim.", this);
                shouldAim = false; // Fallback to not aiming
            }
            // -----------------------------------------------------------------------
        }

        // Spawn network object FIRST
        // pooledNetworkObject.Spawn(false); 

        // Initialize the spirit AFTER spawning
        // Zone references no longer needed by SpiritController
        // spiritController.Initialize(targetPlayerTransform, targetRole, shouldAim);

        // TODO: Define parameters for RPC based on Spirit needs (velocity, type, etc.)
        // For now, just basic info. Default velocity can be handled client-side initially.
        if (ClientSpiritSpawnHandler.Instance != null)
        {
            // Example default values: initialVelocity = 2.0f, spiritType = 0 (normal)
            ClientSpiritSpawnHandler.Instance.SpawnSpiritClientRpc(spiritPrefabID, spawnPosition, shouldAim, targetPlayerClientId, false /*isRevengeSpawn*/, 2.0f, 0);
        }
        else
        {
            Debug.LogError("[SpiritSpawner] ClientSpiritSpawnHandler.Instance is null. Cannot send SpawnSpiritClientRpc.", this);
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
    /// [Server Only] Spawns a "revenge" spirit on the opposing player's side when a spirit is destroyed.
    /// This is typically called by <see cref="PlayerAttackRelay"/> or a similar system reporting a spirit kill.
    /// </summary>
    /// <param name="targetPlayerRole">The role of the player on whose side the revenge spirit should spawn (i.e., the opponent of the player who killed a spirit).</param>
    public void SpawnRevengeSpirit(PlayerRole targetPlayerRole)
    {
        if (!IsServer) return;

        Transform zoneCenter = (targetPlayerRole == PlayerRole.Player1) ? spawnZone1 : spawnZone2;
        if (zoneCenter == null)
        {
            Debug.LogError($"[SpiritSpawner] Cannot spawn revenge spirit, spawn zone for {targetPlayerRole} is null.", this);
            return;
        }

        // Basic check for max spirits on that side (can be made more sophisticated)
        // This check is simplistic as it doesn't know current client-side spirit counts.
        // For a full client-simulated model, this server-side check might be less critical
        // or would need client-reported counts if strictly enforced.
        /*
        int currentSpiritsOnSide = 0; // Need a way to count this if strictly enforced
        if (SpiritRegistry.Instance != null) // Assuming SpiritRegistry still tracks server-side NetworkObjects
        {
            currentSpiritsOnSide = (targetPlayerRole == PlayerRole.Player1) ? 
                                   SpiritRegistry.Instance.GetSpiritCountForPlayer(ClientPlayerAssignment.Player1ClientId) : 
                                   SpiritRegistry.Instance.GetSpiritCountForPlayer(ClientPlayerAssignment.Player2ClientId);
        }

        if (currentSpiritsOnSide >= maxSpiritsPerSide)
        {
            Debug.Log($"[SpiritSpawner] Max spirits ({maxSpiritsPerSide}) reached for Player {targetPlayerRole}. Revenge spirit not spawned.", this);
            return;
        }
        */

        // Calculate spawn position using revengeSpawnZoneSize for wider placement
        Vector3 center = zoneCenter.position;
        float randomX = Random.Range(-revengeSpawnZoneSize.x / 2f, revengeSpawnZoneSize.x / 2f);
        float randomY = Random.Range(-revengeSpawnZoneSize.y / 2f, revengeSpawnZoneSize.y / 2f);
        Vector3 spawnPosition = new Vector3(center.x + randomX, center.y + randomY, center.z);

        // For revenge spirits, they typically don't aim initially, but this can be configured.
        bool shouldAim = false; // Or use aimAtPlayerChance for revenge spirits too?
        ulong targetPlayerClientId = 0;

        if (PlayerDataManager.Instance != null)
        {
            PlayerData? playerData = PlayerDataManager.Instance.GetPlayerDataByRole(targetPlayerRole);
            if (playerData.HasValue && playerData.Value.ClientId != 0)
            {
                targetPlayerClientId = playerData.Value.ClientId;
            }
            else
            {
                Debug.LogError($"[SpiritSpawner] SpawnRevengeSpirit: Could not get valid PlayerData (or ClientId is 0) for role {targetPlayerRole}. Revenge spirit will not be spawned.", this);
                return;
            }
        }
        else
        {
            Debug.LogError("[SpiritSpawner] SpawnRevengeSpirit: PlayerDataManager.Instance is null. Revenge spirit cannot be targeted and will not be spawned.", this);
            return;
        }

        Debug.Log($"[SpiritSpawner] Attempting to spawn REVENGE spirit for {targetPlayerRole} (Client ID: {targetPlayerClientId}) at {spawnPosition}", this);
        // Similar to SpawnSpiritInZone, but using revenge parameters
        // We'll use the same RPC but flag it as a revenge spawn.
        if (ClientSpiritSpawnHandler.Instance != null)
        {
            // Example default values: initialVelocity = 2.0f, spiritType = 0 (normal)
            ClientSpiritSpawnHandler.Instance.SpawnSpiritClientRpc(spiritPrefabID, spawnPosition, shouldAim, targetPlayerClientId, true /*isRevengeSpawn*/, 2.0f, 0);
        }
        else
        {
            Debug.LogError("[SpiritSpawner] ClientSpiritSpawnHandler.Instance is null during revenge spawn. Cannot send SpawnSpiritClientRpc.", this);
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