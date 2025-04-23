using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TouhouWebArena.Spellcards;
using TouhouWebArena.Spellcards.Behaviors;

/// <summary>
/// **[Server Only]** Server-authoritative singleton service responsible for spawning all player-related projectiles
/// (basic shots, charge attacks, spellcards) based on requests received from <see cref="PlayerShootingController"/> RPCs.
/// Interacts with <see cref="NetworkObjectPool"/> for pooled objects and <see cref="PlayerDataManager"/> for owner roles.
/// </summary>
public class ServerAttackSpawner : NetworkBehaviour
{
    /// <summary>Singleton instance of the ServerAttackSpawner.</summary>
    public static ServerAttackSpawner Instance { get; private set; }

    // Constant vertical offset from player center for spawning basic shot pairs.
    private const float firePointVerticalOffset = 0.5f;

    /// <summary>
    /// Unity Awake method. Implements the singleton pattern.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ServerAttackSpawner] Duplicate instance detected. Destroying self.", gameObject);
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Consider DontDestroyOnLoad(gameObject) if this manager needs to persist across scenes independently.
        }
    }

    /// <summary>
    /// Unity OnDestroy method. Clears the singleton instance if this is the active instance.
    /// </summary>
    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnDestroy();
    }

    // --- Public Methods Called by PlayerShootingController RPCs ---

    /// <summary>
    /// **[Server Only]** Spawns a pair of basic shot bullets for the requesting player.
    /// Called via <see cref="PlayerShootingController.RequestFireServerRpc"/>.
    /// </summary>
    /// <param name="requesterClientId">The ClientId of the player who requested the shot.</param>
    public void SpawnBasicShot(ulong requesterClientId)
    {
        // Ensure execution only happens on the server.
        if (!IsServer) return;

        // Get Sender's Player Object and Character Stats
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out NetworkClient networkClient) || networkClient.PlayerObject == null)
        {
            Debug.LogError($"[ServerAttackSpawner.SpawnBasicShot] Could not find player object for client {requesterClientId}");
            return;
        }
        CharacterStats senderStats = networkClient.PlayerObject.GetComponent<CharacterStats>();
        if (senderStats == null)
        {
            Debug.LogError($"[ServerAttackSpawner.SpawnBasicShot] Player object for client {requesterClientId} missing CharacterStats.");
            return;
        }

        // Fetch the correct bullet prefab from the player's stats.
        GameObject bulletToSpawn = senderStats.GetBulletPrefab();
        if (bulletToSpawn == null)
        {
            Debug.LogError($"[ServerAttackSpawner.SpawnBasicShot] Character {senderStats.GetCharacterName()} has no bullet prefab assigned in CharacterStats.");
            return;
        }

        // Calculate spawn points and rotation based on player transform and stats.
        Transform playerTransform = networkClient.PlayerObject.transform;
        Vector3 centerSpawnPoint = playerTransform.position + playerTransform.up * firePointVerticalOffset;
        Quaternion spawnRotation = playerTransform.rotation;
        float spread = senderStats.GetBulletSpread();
        Vector3 rightOffset = playerTransform.right * (spread / 2f);

        // Spawn the pair using the pooling helper method.
        SpawnSinglePooledBullet(bulletToSpawn, centerSpawnPoint - rightOffset, spawnRotation, requesterClientId);
        SpawnSinglePooledBullet(bulletToSpawn, centerSpawnPoint + rightOffset, spawnRotation, requesterClientId);
    }

    /// <summary>
    /// **[Server Only]** Spawns the appropriate (non-pooled) charge attack for the requesting player's character.
    /// Called via <see cref="PlayerShootingController.RequestChargeAttackServerRpc"/>.
    /// </summary>
    /// <param name="requesterClientId">The ClientId of the player who requested the attack.</param>
    public void SpawnChargeAttack(ulong requesterClientId)
    {
        if (!IsServer) return;

        // Get player object and stats
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(requesterClientId, out NetworkClient networkClient) || networkClient.PlayerObject == null)
        {
            Debug.LogError($"[ServerAttackSpawner.SpawnChargeAttack] Could not find player object for client {requesterClientId}");
            return;
        }
        Transform playerTransform = networkClient.PlayerObject.transform;
        CharacterStats stats = networkClient.PlayerObject.GetComponent<CharacterStats>();
        if (stats == null)
        {
             Debug.LogError($"[ServerAttackSpawner.SpawnChargeAttack] Player object for client {requesterClientId} missing CharacterStats.");
             return;
        }

        // Fetch the charge attack prefab from stats.
        GameObject chargePrefab = stats.GetChargeAttackPrefab();
        if (chargePrefab == null)
        {
            Debug.LogError($"[ServerAttackSpawner.SpawnChargeAttack] Character {stats.GetCharacterName()} has no charge attack prefab assigned in CharacterStats.");
            return;
        }

        // Determine Character and Execute specific spawn logic (no pooling).
        string characterName = stats.GetCharacterName();
        if (characterName == "HakureiReimu")
        {
            SpawnReimuChargeAttack(playerTransform, requesterClientId, chargePrefab);
        }
        else if (characterName == "KirisameMarisa")
        {
            SpawnMarisaChargeAttack(playerTransform, requesterClientId, chargePrefab);
        }
        else
        {
            Debug.LogWarning($"[ServerAttackSpawner.SpawnChargeAttack] Unknown character '{characterName}' attempting charge attack for client {requesterClientId}. No attack defined.");
        }
    }

     /// <summary>
    /// **[Server Only]** Initiates the execution of a spellcard pattern against the opponent.
    /// Called by <see cref="PlayerShootingController.RequestSpellcardServerRpc"/> after cost is confirmed by <see cref="SpellBarManager"/>.
    /// Loads <see cref="SpellcardData"/> and starts the <see cref="ServerExecuteSpellcardActions"/> coroutine.
    /// </summary>
    /// <param name="senderClientId">The ClientId of the player declaring the spellcard.</param>
    /// <param name="spellLevel">The level of the spellcard (2, 3, or 4).</param>
    public void ExecuteSpellcard(ulong senderClientId, int spellLevel)
    {
        if (!IsServer) return;

        // --- Get Sender Info ---
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out NetworkClient senderClient) || senderClient.PlayerObject == null)
        {
            Debug.LogError($"[ServerAttackSpawner.ExecuteSpellcard] Could not find sender player object for client {senderClientId}");
            return;
        }
        CharacterStats senderStats = senderClient.PlayerObject.GetComponent<CharacterStats>();
         if (senderStats == null)
        {
            Debug.LogError($"[ServerAttackSpawner.ExecuteSpellcard] Sender player object for client {senderClientId} missing CharacterStats.");
            return;
        }
        string senderCharacterName = senderStats.GetCharacterName();

        // --- Find Opponent ---
        ulong opponentClientId = ulong.MaxValue;
        NetworkObject opponentPlayerObject = null;
        foreach (var connectedClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (connectedClient.ClientId != senderClientId)
            {
                opponentClientId = connectedClient.ClientId;
                opponentPlayerObject = connectedClient.PlayerObject;
                break;
            }
        }
        if (opponentPlayerObject == null)
        {
            Debug.LogWarning($"[ServerAttackSpawner.ExecuteSpellcard] Could not find opponent for client {senderClientId} to execute spellcard.");
            return;
        }

        // --- Load SpellcardData Resource ---
        string resourcePath = $"Spellcards/{senderCharacterName}Level{spellLevel}Spellcard";
        SpellcardData spellcardData = Resources.Load<SpellcardData>(resourcePath);
        if (spellcardData == null)
        {
            Debug.LogError($"[ServerAttackSpawner.ExecuteSpellcard] Failed to load SpellcardData at path: {resourcePath}");
            return;
        }

        // --- Calculate Origin & Start Spawning Coroutine ---
        Vector3 opponentCurrentPos = opponentPlayerObject.transform.position;
        Vector3 originPosition = opponentCurrentPos + new Vector3(0, 5f, 0); // Example origin above opponent
        Quaternion originRotation = Quaternion.identity;

        StartCoroutine(ServerExecuteSpellcardActions(spellcardData, originPosition, originRotation, opponentClientId, opponentCurrentPos));
    }


    // --- Private Spawning Helpers ---

    /// <summary>
    /// **[Server Only]** Spawns Reimu's charge attack pattern (**Non-Pooled**).
    /// Instantiates, assigns ownership, and spawns multiple talismans.
    /// </summary>
    private void SpawnReimuChargeAttack(Transform playerTransform, ulong ownerClientId, GameObject attackPrefab)
    {
        // Define spawn offsets relative to the player
        Vector3 forward = playerTransform.up;
        Vector3 right = playerTransform.right;
        float forwardOffset = 0.8f;
        float horizontalSpread = 0.3f;

        Vector3[] relativePositions = new Vector3[4]
        {
            forward * forwardOffset - right * horizontalSpread * 1.5f,
            forward * forwardOffset - right * horizontalSpread * 0.5f,
            forward * forwardOffset + right * horizontalSpread * 0.5f,
            forward * forwardOffset + right * horizontalSpread * 1.5f
        };

        for (int i = 0; i < 4; i++)
        {
            Vector3 spawnPos = playerTransform.position + relativePositions[i];
            // Instantiate and spawn directly, DO NOT use the pool helper
            GameObject instance = Instantiate(attackPrefab, spawnPos, playerTransform.rotation);
            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                // Optional: Assign owner role if charge attacks have BulletMovement component
                // BulletMovement bm = instance.GetComponent<BulletMovement>();
                // if (bm != null && PlayerDataManager.Instance != null) { ... assign bm.OwnerRole.Value ... }
                netObj.SpawnWithOwnership(ownerClientId);
            }
            else
            {
                Debug.LogError($"[ServerAttackSpawner] Reimu Charge Attack prefab '{attackPrefab.name}' is missing NetworkObject component!");
                Destroy(instance);
            }
        }
    }

    /// <summary>
    /// **[Server Only]** Spawns Marisa's charge attack (Illusion Laser) (**Non-Pooled**).
    /// Instantiates, assigns ownership, and spawns the laser.
    /// </summary>
    private void SpawnMarisaChargeAttack(Transform playerTransform, ulong ownerClientId, GameObject attackPrefab)
    {
        float forwardOffset = 0.5f;
        Vector3 spawnPos = playerTransform.position + playerTransform.up * forwardOffset;
        Quaternion spawnRot = Quaternion.identity; // Laser rotation usually fixed or handled by prefab/component

        // Instantiate and spawn directly, DO NOT use the pool helper
        GameObject instance = Instantiate(attackPrefab, spawnPos, spawnRot);
        NetworkObject netObj = instance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            // Optional: Assign owner role if charge attacks have BulletMovement component
            netObj.SpawnWithOwnership(ownerClientId);
        }
        else
        {
            Debug.LogError($"[ServerAttackSpawner] Marisa Charge Attack prefab '{attackPrefab.name}' is missing NetworkObject component!");
            Destroy(instance);
        }
    }

    /// <summary>
    /// **[Server Only]** Core helper method to spawn a single projectile **using the NetworkObjectPool**.
    /// Gets an object from the pool via <see cref="PoolableObjectIdentity.PrefabID"/>, positions it,
    /// spawns it with ownership, assigns owner role via <see cref="PlayerDataManager"/> **after spawning**,
    /// and parents it to the pool.
    /// Used for basic shots and potentially spellcard bullets (if pooled).
    /// </summary>
    /// <param name="prefab">The pooled prefab to spawn.</param>
    /// <param name="position">World position for spawning.</param>
    /// <param name="rotation">World rotation for spawning.</param>
    /// <param name="ownerId">The ClientId of the player owning this bullet.</param>
    private void SpawnSinglePooledBullet(GameObject prefab, Vector3 position, Quaternion rotation, ulong ownerId)
    {
        if (prefab == null)
        {
            Debug.LogError("[ServerAttackSpawner.SpawnSinglePooledBullet] Attempted to spawn a null prefab.");
            return;
        }
        if (NetworkObjectPool.Instance == null)
        {
            Debug.LogError("[ServerAttackSpawner.SpawnSinglePooledBullet] NetworkObjectPool instance not found!");
            return;
        }

        // Ensure prefab has the identity component for pooling
        PoolableObjectIdentity identity = prefab.GetComponent<PoolableObjectIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.PrefabID))
        {
             Debug.LogError($"[ServerAttackSpawner.SpawnSinglePooledBullet] Prefab '{prefab.name}' is missing PoolableObjectIdentity or PrefabID.");
             return;
        }
        string prefabID = identity.PrefabID;

        // Get from pool
        NetworkObject bulletNetworkObject = NetworkObjectPool.Instance.GetNetworkObject(prefabID);
        if (bulletNetworkObject == null)
        {
            // Pool likely returned null (e.g., pool empty and cannot grow)
            Debug.LogError($"[ServerAttackSpawner.SpawnSinglePooledBullet] Failed to get NetworkObject from pool for PrefabID: {prefabID}. Pool might be exhausted.");
            return;
        }

        // Position, Rotate, Activate
        bulletNetworkObject.transform.position = position;
        bulletNetworkObject.transform.rotation = rotation;
        bulletNetworkObject.gameObject.SetActive(true);

        // --- Spawn FIRST ---
        bulletNetworkObject.SpawnWithOwnership(ownerId);

        // --- Assign Owner Role AFTER Spawning ---
        BulletMovement bulletMovement = bulletNetworkObject.GetComponent<BulletMovement>();
        if (bulletMovement != null)
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(ownerId);
                // Now it's safe to set the NetworkVariable
                bulletMovement.OwnerRole.Value = ownerData.HasValue ? ownerData.Value.Role : PlayerRole.None;
            }
            else
            {
                 Debug.LogError("[ServerAttackSpawner.SpawnSinglePooledBullet] PlayerDataManager instance not found! Cannot set bullet owner role.");
                 bulletMovement.OwnerRole.Value = PlayerRole.None; // Assign default
            }
        }
        else
        {
            // Optional: Log warning if expected component is missing, but don't stop spawning.
            // Debug.LogWarning($"[ServerAttackSpawner.SpawnSinglePooledBullet] Spawned object '{prefabID}' is missing BulletMovement component.");
        }

        // Parent AFTER setting position/rotation and spawning
        bulletNetworkObject.transform.SetParent(NetworkObjectPool.Instance.transform, worldPositionStays: true);

    }

    /// <summary>
    /// **[Server Only Coroutine]** Executes the actions defined in a <see cref="SpellcardData"/> ScriptableObject.
    /// Handles delays and iterates through actions, spawning projectiles (potentially using the pool via <see cref="SpawnSinglePooledBullet"/>
    /// if spellcard bullet prefabs have <see cref="PoolableObjectIdentity"/>) and configuring their behavior.
    /// </summary>
    /// <param name="spellcardData">The ScriptableObject defining the spellcard pattern.</param>
    /// <param name="originPosition">The calculated world origin point for the spellcard pattern (usually above the opponent).</param>
    /// <param name="originRotation">The base world rotation for the spellcard pattern.</param>
    /// <param name="opponentId">The NetworkClientId of the opponent player (target for behaviors).</param>
    /// <param name="capturedOpponentPosition">The opponent's world position captured when the spellcard was initiated.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator ServerExecuteSpellcardActions(SpellcardData spellcardData, Vector3 originPosition, Quaternion originRotation, ulong opponentId, Vector3 capturedOpponentPosition)
    {
         if (!IsServer) yield break;
         if (NetworkObjectPool.Instance == null) // Check pool instance for pooled bullets
         {
             // If spellcards exclusively use non-pooled bullets, this check might be unnecessary.
             // However, it's safer to keep it if some spellcard bullets *might* be pooled.
             Debug.LogWarning("[ServerAttackSpawner.ServerExecuteSpellcardActions] NetworkObjectPool instance not found! Pooled spellcard bullets cannot be spawned.");
             // Decide if we should yield break or continue with non-pooled spawning only.
             // yield break; // Uncomment if pooled bullets are essential for spellcards
         }

        bool isTargetOnPositiveSide = (capturedOpponentPosition.x >= 0.0f); // Determine target side for boundary checks

        foreach (SpellcardAction action in spellcardData.actions)
        {
            // Handle delay before this action starts
            if (action.startDelay > 0) yield return new WaitForSeconds(action.startDelay);
            if (action.bulletPrefabs == null || action.bulletPrefabs.Count == 0) continue; // Skip action if no prefabs assigned

            int prefabIndex = 0;
            Vector3 spawnPositionBase = originPosition + (Vector3)action.positionOffset;

            // Spawn projectiles for this action
            for (int i = 0; i < action.count; i++)
            {
                GameObject prefabToSpawn = action.bulletPrefabs[prefabIndex % action.bulletPrefabs.Count];
                if (prefabToSpawn == null) continue;

                // --- Check if this spellcard bullet prefab should be pooled ---
                PoolableObjectIdentity identity = prefabToSpawn.GetComponent<PoolableObjectIdentity>();
                bool usePool = (identity != null && !string.IsNullOrEmpty(identity.PrefabID) && NetworkObjectPool.Instance != null);

                Vector3 spawnPos = spawnPositionBase;
                Quaternion spawnRot = originRotation;

                // Calculate formation position and rotation (relative to origin)
                switch (action.formation)
                {
                    case FormationType.Point: // Spawn at offset point
                        // Position is already spawnPositionBase
                        // Rotation is already originRotation
                        break;
                    case FormationType.Circle: // Spawn in a circle around offset point
                        if (action.count > 0) {
                            float angleDegrees = i * (360f / action.count);
                            float angleRad = angleDegrees * Mathf.Deg2Rad;
                            // Position offset for circle radius
                            // Vector3 circleOffset = originRotation * new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0) * action.radius;
                            spawnPos = spawnPositionBase; // Spawn all bullets at the center point for outward expansion
                            // Rotation needs to align transform.up radially outward (which is at angleDegrees)
                            spawnRot = originRotation * Quaternion.Euler(0, 0, angleDegrees - 90f); // Use angleDegrees - 90 to align transform.up outwards
                        }
                        break;
                    case FormationType.Line: // Spawn in a line relative to offset point
                        float lineAngleRad = action.angle * Mathf.Deg2Rad;
                        // Calculate direction relative to origin rotation
                        Vector3 lineDirection = originRotation * new Vector3(Mathf.Cos(lineAngleRad), Mathf.Sin(lineAngleRad), 0);
                        float totalLength = action.spacing * (action.count - 1);
                        float startOffset = -totalLength / 2f;
                        spawnPos = spawnPositionBase + lineDirection * (startOffset + i * action.spacing);
                        // Rotation matches line angle relative to origin
                        spawnRot = originRotation * Quaternion.Euler(0, 0, action.angle);
                        break;
                }

                // --- Spawn the bullet (Pooled or Non-Pooled) ---
                GameObject bulletInstance = null;
                NetworkObject bulletInstanceNO = null;

                if (usePool)
                {
                    // Get from pool
                    bulletInstanceNO = NetworkObjectPool.Instance.GetNetworkObject(identity.PrefabID);
                    if (bulletInstanceNO != null)
                    {
                        bulletInstance = bulletInstanceNO.gameObject;
                        bulletInstance.transform.position = spawnPos;
                        bulletInstance.transform.rotation = spawnRot;
                        bulletInstance.SetActive(true);
                    }
                    else
                    {
                         Debug.LogError($"[ServerAttackSpawner.ServerExecuteSpellcardActions] Failed to get pooled object for PrefabID: {identity.PrefabID}");
                         continue; // Skip this bullet
                    }
                }
                else
                {
                    // Instantiate directly
                    bulletInstance = Instantiate(prefabToSpawn, spawnPos, spawnRot);
                    bulletInstanceNO = bulletInstance.GetComponent<NetworkObject>();
                    if (bulletInstanceNO == null)
                    {
                        Debug.LogError($"[ServerAttackSpawner.ServerExecuteSpellcardActions] Non-pooled spellcard prefab '{prefabToSpawn.name}' is missing NetworkObject component!");
                        Destroy(bulletInstance);
                        continue; // Skip this bullet
                    }
                }

                // --- Spawn NetworkObject FIRST ---
                // Spellcard bullets are typically environment hazards, spawn as server-owned.
                bulletInstanceNO.Spawn(true); // Spawn server-owned (or use SpawnWithOwnership if needed)

                // --- Configure Behavior AFTER Spawning ---
                ConfigureBulletBehavior(bulletInstance, action, opponentId, capturedOpponentPosition, isTargetOnPositiveSide);

                // --- Parent (if pooled) ---
                if (usePool)
                {
                    bulletInstanceNO.transform.SetParent(NetworkObjectPool.Instance.transform, worldPositionStays: true);
                }
                // Non-pooled instances are not parented to the pool.

                prefabIndex++;
            }
        }
    }

    /// <summary>
    /// **[Server Only]** Helper method to configure a newly spawned spellcard projectile's behavior components
    /// (e.g., <see cref="LinearMovement"/>, <see cref="DelayedHoming"/>) based on the <see cref="SpellcardAction"/> data.
    /// Also configures the <see cref="NetworkBulletLifetime"/> boundary check.
    /// </summary>
    /// <param name="bulletInstance">The instantiated bullet GameObject.</param>
    /// <param name="action">The SpellcardAction defining the behavior.</param>
    /// <param name="opponentId">The ClientId of the opponent player (target for homing).</param>
    /// <param name="capturedOpponentPosition">The captured position of the opponent (initial target for homing).</param>
    /// <param name="isTargetOnPositiveSide">Whether the target is on the right side (for boundary checks).</param>
    private void ConfigureBulletBehavior(GameObject bulletInstance, SpellcardAction action, ulong opponentId, Vector3 capturedOpponentPosition, bool isTargetOnPositiveSide)
    {
        // Disable all potential behaviors first to ensure clean state
        var linear = bulletInstance.GetComponent<LinearMovement>();
        var delayedHoming = bulletInstance.GetComponent<DelayedHoming>();
        var lifetime = bulletInstance.GetComponent<NetworkBulletLifetime>();
        if (linear) linear.enabled = false;
        if (delayedHoming) delayedHoming.enabled = false;

        // Configure Lifetime Boundary Check
        if (lifetime != null) {
            lifetime.keepOnPositiveSide = isTargetOnPositiveSide;
        } else {
            Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' missing NetworkBulletLifetime component.");
        }

        // Enable and initialize the specified movement behavior
        switch (action.behavior)
        {
            case BehaviorType.Linear:
                if (linear != null) { linear.enabled = true; linear.Initialize(action.speed); }
                else { Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to Linear but missing LinearMovement component."); }
                break;
            case BehaviorType.DelayedHoming:
                if (delayedHoming != null)
                {
                    if (opponentId != ulong.MaxValue) // Ensure opponent exists
                    {
                         delayedHoming.enabled = true;
                         delayedHoming.Initialize(action.speed, action.homingSpeed, action.homingDelay, opponentId, capturedOpponentPosition);
                    } else {
                        // Fallback to linear if no opponent found (should be rare in 2-player game)
                        Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DelayedHoming but no opponent found. Falling back to Linear.");
                        if (linear != null) { linear.enabled = true; linear.Initialize(action.speed); } // Fallback
                    }
                }
                 else { Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DelayedHoming but missing DelayedHoming component."); }
                break;
            // TODO: Add other cases like Homing if implemented
            default:
                 Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' has unhandled BehaviorType: {action.behavior}. Defaulting to Linear if possible.");
                 if (linear != null) { linear.enabled = true; linear.Initialize(action.speed); } // Default fallback
                 break;
        }
    }
} 