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

        if (spiritPrefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("SpiritSpawner: Spirit Prefab is missing a NetworkObject component.", this);
            enabled = false;
            return false;
        }
        if (spiritPrefab.GetComponent<SpiritController>() == null)
        {
            Debug.LogError("SpiritSpawner: Spirit Prefab is missing a SpiritController component.", this);
            enabled = false;
            return false;
        }
        // Also check for PoolableObjectIdentity
        if (spiritPrefab.GetComponent<PoolableObjectIdentity>() == null || string.IsNullOrEmpty(spiritPrefab.GetComponent<PoolableObjectIdentity>().PrefabID))
        {
            Debug.LogError("SpiritSpawner: Spirit Prefab is missing PoolableObjectIdentity or has an empty PrefabID.", this);
            enabled = false;
            return false;
        }
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
        if (spiritPrefab == null) return; // Should be validated earlier

        PoolableObjectIdentity identity = spiritPrefab.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
            Debug.LogError($"[SpiritSpawner] Spirit prefab '{spiritPrefab.name}' is missing PoolableObjectIdentity or PrefabID! Cannot spawn.", this);
            return;
        }
        string prefabID = identity.PrefabID;

        // Get object from pool
        NetworkObject pooledNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);
        if (pooledNetworkObject == null)
        {
            Debug.LogError($"[SpiritSpawner] Failed to get Spirit with PrefabID '{prefabID}' from pool.", this);
            return;
        }
        // ----------------------

        // Get components from pooled object
        SpiritController spiritController = pooledNetworkObject.GetComponent<SpiritController>();
        // NetworkObject networkObject = pooledNetworkObject; // Already have reference

        // Position and Activate (BEFORE Initialize and Spawn)
        pooledNetworkObject.transform.position = spawnPosition;
        pooledNetworkObject.transform.rotation = Quaternion.identity;
        pooledNetworkObject.gameObject.SetActive(true); 

        // Determine target player and aim chance
        PlayerRole targetRole = (zoneCenter == spawnZone1) ? PlayerRole.Player1 : PlayerRole.Player2;
        bool shouldAim = Random.value < aimAtPlayerChance;
        Transform targetPlayerTransform = null;

        // Get player transform if aiming
        if (shouldAim)
        {
            // --- Access Player Transform (Requires PlayerDataManager or similar) ---
            if (PlayerDataManager.Instance != null && NetworkManager.Singleton != null)
            {
                PlayerData? playerData = PlayerDataManager.Instance.GetPlayerDataByRole(targetRole);
                if (playerData.HasValue)
                {
                    NetworkObject playerNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerData.Value.ClientId);
                    if (playerNetObj != null)
                    {
                        targetPlayerTransform = playerNetObj.transform;
                    }
                    else
                    {
                        Debug.LogWarning($"[SpiritSpawner] Could not find NetworkObject for Player {targetRole}. Spirit will not aim.", this);
                        shouldAim = false; // Fallback to not aiming
                    }
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
        pooledNetworkObject.Spawn(false); 

        // Initialize the spirit AFTER spawning
        // Zone references no longer needed by SpiritController
        spiritController.Initialize(targetPlayerTransform, targetRole, shouldAim);
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
        UnityEngine.Debug.Log($"Spirit Spawner spawning set to: {enabled}");
    }

    /// <summary>
    /// [Server Only] Spawns a single "revenge" spirit on the specified target player's side.
    /// Triggered by <see cref="SpiritController.Die"/> when a spirit is killed by a player.
    /// Checks the spirit limit (<see cref="maxSpiritsPerSide"/>) for the target player before spawning.
    /// Uses the <see cref="NetworkObjectPool"/> and configures the spirit for the target player within the <see cref="revengeSpawnZoneSize"/>.
    /// </summary>
    /// <param name="targetPlayerRole">The role of the player who will own the newly spawned spirit (the opponent of the killer).</param>
    public void SpawnRevengeSpirit(PlayerRole targetPlayerRole)
    {
        // Basic server and role checks
        if (!IsServer || targetPlayerRole == PlayerRole.None) return;

        // Check Prefab configuration (already validated in Start, but good practice)
        if (spiritPrefab == null)
        {
            Debug.LogError("[SpiritSpawner] SpawnRevengeSpirit: Spirit prefab reference is missing!", this);
            return;
        }
        PoolableObjectIdentity identity = spiritPrefab.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
            Debug.LogError($"[SpiritSpawner] SpawnRevengeSpirit: Spirit prefab '{spiritPrefab.name}' missing identity/ID!", this);
            return;
        }
        string prefabID = identity.PrefabID;

        // Check Pool existence
        if (NetworkObjectPool.Instance == null)
        {
            Debug.LogError("[SpiritSpawner] SpawnRevengeSpirit: NetworkObjectPool instance not found!", this);
            return;
        }

        // Check Registry existence (for count check)
        if (SpiritRegistry.Instance == null)
        {
             Debug.LogWarning("[SpiritSpawner] SpawnRevengeSpirit: SpiritRegistry instance not found.", this);
             // Decide if we should proceed without check or abort. Aborting is safer.
             return; 
        }

        // Check max spirit count for the TARGET player's side.
        int targetSpiritCount = SpiritRegistry.Instance.GetSpiritCount(targetPlayerRole);
        if (targetSpiritCount >= maxSpiritsPerSide)
        {
             Debug.Log($"[SpiritSpawner] SpawnRevengeSpirit: Target player {targetPlayerRole} at max spirit capacity ({targetSpiritCount}/{maxSpiritsPerSide}). Revenge spawn skipped.", this);
             return; // Target player is at max capacity
        }

        // Determine the TARGET player's spawn zone.
        Transform targetSpawnZone = (targetPlayerRole == PlayerRole.Player1) ? spawnZone1 : spawnZone2;
        if (targetSpawnZone == null) // Should be validated in Start, but check anyway
        {
            Debug.LogError($"[SpiritSpawner] SpawnRevengeSpirit: Target player ({targetPlayerRole}) spawn zone reference is null!", this);
            return;
        }
        
        // Calculate random position within the REVENGE spawn zone dimensions.
        float spawnX = Random.Range(-revengeSpawnZoneSize.x / 2f, revengeSpawnZoneSize.x / 2f);
        float spawnY = Random.Range(-revengeSpawnZoneSize.y / 2f, revengeSpawnZoneSize.y / 2f);
        Vector3 spawnPosition = targetSpawnZone.position + new Vector3(spawnX, spawnY, 0);

        // Get spirit from pool
        NetworkObject pooledNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);
        if (pooledNetworkObject == null)
        {    
            Debug.LogError($"[SpiritSpawner] SpawnRevengeSpirit: Failed to get Spirit '{prefabID}' from pool.", this);
            return;
        }

        // Get required components from pooled object
        SpiritController newSpiritController = pooledNetworkObject.GetComponent<SpiritController>();
        if (newSpiritController == null)
        {   
            Debug.LogError("[SpiritSpawner] SpawnRevengeSpirit: Pooled object missing SpiritController! Returning to pool.", this);
            NetworkObjectPool.Instance.ReturnNetworkObject(pooledNetworkObject); // Return broken object
            return;
        }

        // Position and Activate pooled object
        pooledNetworkObject.transform.position = spawnPosition;
        pooledNetworkObject.transform.rotation = Quaternion.identity;
        pooledNetworkObject.gameObject.SetActive(true);

        // Revenge spirits don't aim initially
        Transform opponentPlayerTransform = null; 
        bool shouldAim = false;
        
        // Spawn the new spirit on the network FIRST
        pooledNetworkObject.Spawn(false); // Spawn client-owned or server-owned based on pool config? Assume false for now.
        
        // Initialize the new spirit AFTER spawning (passing the target role as the new owner)
        newSpiritController.Initialize(opponentPlayerTransform, targetPlayerRole, shouldAim);
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