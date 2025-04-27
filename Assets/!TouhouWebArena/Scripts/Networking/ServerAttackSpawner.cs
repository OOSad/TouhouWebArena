using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TouhouWebArena;
using TouhouWebArena.Spellcards;
using TouhouWebArena.Spellcards.Behaviors;
using System.Collections.Generic;

/// <summary>
/// **[Server Only]** Server-authoritative singleton service responsible for spawning all player-related projectiles
/// (basic shots, charge attacks, spellcards) based on requests received from <see cref="PlayerShootingController"/> RPCs.
/// Interacts with <see cref="NetworkObjectPool"/> for pooled objects and <see cref="PlayerDataManager"/> for owner roles.
/// </summary>
public class ServerAttackSpawner : NetworkBehaviour
{
    /// <summary>Singleton instance of the ServerAttackSpawner.</summary>
    public static ServerAttackSpawner Instance { get; private set; }

    // --- Active Illusion Tracking (Server Only) ---
    private Dictionary<ulong, NetworkObject> _activeIllusionsTargetingPlayer = new Dictionary<ulong, NetworkObject>();
    private Dictionary<ulong, NetworkObject> _activeIllusionsCastByPlayer = new Dictionary<ulong, NetworkObject>();
    // ---------------------------------------------

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
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }
        }
        base.OnDestroy();
    }

    // --- Network Spawn/Disconnect Handling ---
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            if (NetworkManager.Singleton != null)
            {
                 NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            }
        }
    }

    private void HandleClientDisconnect(ulong disconnectedClientId)
    {
        if (!IsServer) return;
        // Clean up illusions related to the disconnected client
        if (_activeIllusionsTargetingPlayer.TryGetValue(disconnectedClientId, out NetworkObject illusionTargeting)) 
        {
            ServerForceDespawnIllusion(illusionTargeting);
        }
        if (_activeIllusionsCastByPlayer.TryGetValue(disconnectedClientId, out NetworkObject illusionCastBy)) 
        {
            ServerForceDespawnIllusion(illusionCastBy);
        }
    }
    // -----------------------------------------

    // --- Illusion Tracking Management (Server Only) ---
    public void ServerNotifyIllusionDespawned(NetworkObject illusionNO)
    {
        if (!IsServer || illusionNO == null) return;
        // Remove from targeting dictionary
        ulong? targetKeyToRemove = null;
        foreach(var kvp in _activeIllusionsTargetingPlayer)
        {
            if (kvp.Value == illusionNO) { targetKeyToRemove = kvp.Key; break; }
        }
        if (targetKeyToRemove.HasValue) _activeIllusionsTargetingPlayer.Remove(targetKeyToRemove.Value);
        // Remove from caster dictionary
        ulong? casterKeyToRemove = null;
        foreach(var kvp in _activeIllusionsCastByPlayer)
        {
             if (kvp.Value == illusionNO) { casterKeyToRemove = kvp.Key; break; }
        }
        if (casterKeyToRemove.HasValue) _activeIllusionsCastByPlayer.Remove(casterKeyToRemove.Value);
    }

    private void ServerForceDespawnIllusion(NetworkObject illusionNO)
    {
        if (!IsServer || illusionNO == null || !illusionNO.IsSpawned) return;
        IllusionHealth health = illusionNO.GetComponent<IllusionHealth>();
        if (health != null)
        {
            health.TakeDamageServerSide(float.MaxValue, PlayerRole.None);
        }
        else
        {   
            Debug.LogWarning($"[ServerAttackSpawner] Illusion {illusionNO.name} missing IllusionHealth, attempting direct despawn.");
            illusionNO.Despawn(true);
            ServerNotifyIllusionDespawned(illusionNO); // Manual notify
        }
    }
    // ---------------------------------------------------

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
        PlayerRole opponentRole = PlayerRole.None;
        Rect opponentBounds = new Rect();
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
            return; // Cannot execute spellcard without an opponent
        }
        // --- Determine Opponent Role and Bounds ---
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? opponentData = PlayerDataManager.Instance.GetPlayerData(opponentClientId);
            if (opponentData.HasValue)
            {
                opponentRole = opponentData.Value.Role;
                opponentBounds = (opponentRole == PlayerRole.Player1) ? PlayerMovement.player1Bounds : PlayerMovement.player2Bounds;
            }
            else { /* Error handling needed */ return; }
        }
        else { /* Error handling needed */ return; }
        // -------------------------------------------
        
        Vector3 capturedOpponentPositionForHoming = opponentPlayerObject.transform.position;

        // --- Load Spellcard Resource based on Level ---
        string resourcePath = $"Spellcards/{senderCharacterName}Level{spellLevel}Spellcard";

        // --- Handle Level 4 --- 
        if (spellLevel == 4)
        {
            // Cancellation Logic
            if (_activeIllusionsTargetingPlayer.TryGetValue(senderClientId, out NetworkObject illusionTargetingSender)) 
            { ServerForceDespawnIllusion(illusionTargetingSender); }
            if (_activeIllusionsCastByPlayer.TryGetValue(senderClientId, out NetworkObject illusionCastBySender)) 
            { ServerForceDespawnIllusion(illusionCastBySender); }
            
            Level4SpellcardData level4Data = Resources.Load<Level4SpellcardData>(resourcePath);
            if (level4Data == null)
            {
                Debug.LogError($"[ServerAttackSpawner.ExecuteSpellcard] Failed to load Level4SpellcardData at path: {resourcePath}.");
                return;
            }
            // Pass opponentClientId needed for tracking
            ServerSpawnLevel4Illusion(level4Data, senderClientId, opponentClientId, opponentPlayerObject, opponentRole);
            return; // Level 4 handled
        }

        // --- Handle Levels 2 & 3 (Existing Logic) ---
        SpellcardData spellcardData = Resources.Load<SpellcardData>(resourcePath);
        if (spellcardData == null)
        {
            Debug.LogError($"[ServerAttackSpawner.ExecuteSpellcard] Failed to load SpellcardData for Level {spellLevel} at path: {resourcePath}");
            return;
        }

        // Calculate Lv2/3 Origin Position based on Character & Opponent Bounds
        Vector3 originPosition = CalculateSpellcardOrigin(senderCharacterName, spellLevel, opponentBounds);
        Quaternion originRotation = Quaternion.identity;

        // Start Spawning Coroutine for Levels 2/3
        StartCoroutine(ServerExecuteSpellcardActions(spellcardData, originPosition, originRotation, opponentClientId, capturedOpponentPositionForHoming));
    }

    /// <summary>
    /// **[Server Only Coroutine]** Executes a single SpellcardAction, spawning projectiles potentially over time.
    /// Uses the provided illusion's transform to get the up-to-date origin for each bullet if movement occurs during the action.
    /// Applies base rotation for pattern aiming and handles target-side flipping.
    /// </summary>
    public IEnumerator ExecuteSingleSpellcardActionFromServerCoroutine(TouhouWebArena.Spellcards.SpellcardAction action, Transform illusionTransform, Quaternion baseRotation, ulong targetClientId, Vector3 capturedTargetPosition, bool isTargetOnPositiveSide)
    {
        if (!IsServer) yield break;
        if (illusionTransform == null) { Debug.LogError("[ExecuteSingleSpellcardActionFromServerCoroutine] Illusion Transform is null!"); yield break; }
        if (action.bulletPrefabs == null || action.bulletPrefabs.Count == 0) yield break; 

        // Relative offset and angle from the action data
        Vector2 relativeOffset = action.positionOffset;
        float relativeAngle = action.angle;
        bool isPatternOriented = !Mathf.Approximately(Quaternion.Angle(baseRotation, Quaternion.identity), 0f);

        // Flip angle/offset if pattern isn't aimed AND target is on the left (-x) side.
        if (!isPatternOriented && !isTargetOnPositiveSide)
        {
            relativeAngle *= -1f; 
            relativeOffset.x *= -1f; 
        }

        int prefabIndex = 0;
        // Base position calculation is now deferred into the loop to use live illusion position

        WaitForSeconds intraActionWait = (action.intraActionDelay > 0f) ? new WaitForSeconds(action.intraActionDelay) : null;

        for (int i = 0; i < action.count; i++)
        {
            // Add delay BEFORE spawning the bullet (except the very first one)
            if (i > 0 && intraActionWait != null)
            {
                yield return intraActionWait;
            }
            
            // --- Skip Nth Bullet Check ---
            if (action.skipEveryNth > 0 && (i + 1) % action.skipEveryNth == 0) // Use i+1 for 1-based counting (skip 4th, 8th etc.)
            {
                prefabIndex++; // Still increment prefab index if cycling
                continue; // Skip spawning this bullet
            }
            // ---------------------------

            // --- Get CURRENT illusion position and calculate base spawn point for THIS bullet ---
            Vector3 currentIllusionPos = illusionTransform.position;
            Vector3 spawnPositionBase = currentIllusionPos + baseRotation * (Vector3)relativeOffset;
            // --------------------------------------------------------------------------------

            GameObject prefabToSpawn = action.bulletPrefabs[prefabIndex % action.bulletPrefabs.Count];
            if (prefabToSpawn == null) continue;

            PoolableObjectIdentity identity = prefabToSpawn.GetComponent<PoolableObjectIdentity>();
            bool usePool = (identity != null && !string.IsNullOrEmpty(identity.PrefabID) && NetworkObjectPool.Instance != null);

            Vector3 spawnPos = spawnPositionBase;
            Quaternion spawnRot = baseRotation;

            switch (action.formation)
            {
                case FormationType.Point:
                    spawnRot = baseRotation * Quaternion.Euler(0, 0, relativeAngle);
                    break;
                case FormationType.Circle:
                    if (action.count > 0)
                    {
                        float angleDegrees = i * (360f / action.count) + relativeAngle;
                        // Use current spawnPositionBase (derived from live illusion pos) for the center
                        spawnPos = spawnPositionBase + baseRotation * (Quaternion.Euler(0, 0, angleDegrees) * Vector3.right * action.radius);
                        spawnRot = baseRotation * Quaternion.Euler(0, 0, angleDegrees - 90f);
                    }
                    break;
                case FormationType.Line:
                    Vector3 directionRelativeToPattern = Quaternion.Euler(0, 0, relativeAngle) * Vector3.right;
                    Vector3 lineDirection = baseRotation * directionRelativeToPattern;
                    float totalLength = action.spacing * (action.count - 1);
                    float startOffset = -totalLength / 2f;
                    // Use current spawnPositionBase (derived from live illusion pos)
                    spawnPos = spawnPositionBase + lineDirection * (startOffset + i * action.spacing);
                    spawnRot = baseRotation;
                    break;
            }

            GameObject bulletInstance = null;
            NetworkObject bulletInstanceNO = null;

            if (usePool)
            {
                bulletInstanceNO = NetworkObjectPool.Instance.GetNetworkObject(identity.PrefabID);
                if (bulletInstanceNO != null)
                {
                    bulletInstance = bulletInstanceNO.gameObject;
                    bulletInstance.transform.position = spawnPos;
                    bulletInstance.transform.rotation = spawnRot;
                    bulletInstance.SetActive(true);
                }
                else { /* Log Error */ continue; }
            }
            else
            {
                bulletInstance = Instantiate(prefabToSpawn, spawnPos, spawnRot);
                bulletInstanceNO = bulletInstance.GetComponent<NetworkObject>();
                if (bulletInstanceNO == null) { /* Log Error */ Destroy(bulletInstance); continue; }
            }

            bulletInstanceNO.Spawn(true); // Spawn server-owned

            float currentBulletSpeed = action.speed;
            if (action.formation == FormationType.Line && action.speedIncrementPerBullet != 0f)
            {
                currentBulletSpeed += (i * action.speedIncrementPerBullet);
            }

            // Pass the SPAWN position base for behavior configuration if needed
            ConfigureBulletBehavior(bulletInstance, action, currentBulletSpeed, targetClientId, capturedTargetPosition, isTargetOnPositiveSide, spawnPositionBase); 

            if (usePool)
            {
                bulletInstanceNO.transform.SetParent(NetworkObjectPool.Instance.transform, worldPositionStays: true);
            }

            prefabIndex++;
        }
    }

    /// <summary>
    /// [Server Only] Calculates the origin position for Level 2/3 spellcards based on character and opponent bounds.
    /// </summary>
    private Vector3 CalculateSpellcardOrigin(string senderCharacterName, int spellLevel, Rect opponentBounds)
    {
         Vector3 origin = Vector3.zero;
         if (senderCharacterName == "HakureiReimu")
         {
             float randomX = Random.Range(opponentBounds.xMin + 0.5f, opponentBounds.xMax - 0.5f);
             origin = new Vector3(randomX, opponentBounds.yMax - 1.0f, 0);
         }
         else if (senderCharacterName == "KirisameMarisa")
         {
             if (spellLevel == 2)
             {
                 float edgeX = opponentBounds.center.x > 0 ? opponentBounds.xMax - 0.5f : opponentBounds.xMin + 0.5f;
                 origin = new Vector3(edgeX, opponentBounds.yMax - 1.0f, 0);
             }
             else if (spellLevel == 3)
             {
                 // Level 3: Spawn from the edge closest to the SENDER.
                 // Note: Original design might have intended simultaneous spawn from both edges,
                 // but current single-edge spawn works with configured offsets.
                 float edgeX;
                 // opponentRole was determined earlier
                 edgeX = opponentBounds.center.x < 0 ? opponentBounds.xMax - 0.5f : opponentBounds.xMin + 0.5f; // Closer edge
                 origin = new Vector3(edgeX, opponentBounds.yMax - 1.0f, 0);
             }
             else
             {   // Fallback for unknown Marisa level
                 origin = new Vector3(opponentBounds.center.x, opponentBounds.yMax - 1.0f, 0);
             }
         }
         else
         {   // Fallback for unknown character
             Debug.LogWarning($"Unknown character '{senderCharacterName}' for spellcard origin. Defaulting to top-center.");
             origin = new Vector3(opponentBounds.center.x, opponentBounds.yMax - 1.0f, 0);
         }
         return origin;
    }

    /// <summary>
    /// **[Server Only]** Spawns the persistent illusion for a Level 4 spellcard.
    /// </summary>
    private void ServerSpawnLevel4Illusion(Level4SpellcardData spellData, ulong senderClientId, ulong opponentClientId, NetworkObject opponentPlayerObject, PlayerRole opponentRole)
    {
        if (spellData.IllusionPrefab == null) { /* Error Log */ return; }
        NetworkObject prefabNO = spellData.IllusionPrefab.GetComponent<NetworkObject>();
        if (prefabNO == null) { /* Error Log */ return; }

        // Determine Spawn Position (Top-center of opponent's bounds)
        Rect opponentBounds = (opponentRole == PlayerRole.Player1) ? PlayerMovement.player1Bounds : PlayerMovement.player2Bounds;
        float spawnX = opponentBounds.center.x;
        float spawnY = opponentBounds.yMax - 1.0f; 
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0);
        Quaternion spawnRotation = Quaternion.identity;

        GameObject illusionInstance = Instantiate(spellData.IllusionPrefab, spawnPosition, spawnRotation);
        NetworkObject illusionNO = illusionInstance.GetComponent<NetworkObject>();

        // Spawn NetworkObject FIRST
        illusionNO.Spawn(true);

        // Update Trackers
        _activeIllusionsTargetingPlayer[opponentClientId] = illusionNO;
        _activeIllusionsCastByPlayer[senderClientId] = illusionNO;

        // Initialize Controller
        Level4IllusionController controller = illusionInstance.GetComponent<Level4IllusionController>();
        if (controller != null) { controller.ServerInitialize(opponentRole, spellData); }
        else { /* Error Log */ }

        // Initialize Health
        IllusionHealth healthComponent = illusionInstance.GetComponent<IllusionHealth>();
        if (healthComponent != null) { healthComponent.ServerInitialize(spellData.Health, opponentRole); }
        else { /* Error Log */ }

        // Start Lifetime Management
        StartCoroutine(ServerManageIllusionLifetime(illusionNO, spellData.Duration));
    }

    /// <summary>
    /// **[Server Only Coroutine]** Manages the timed duration of a spawned Level 4 illusion.
    /// </summary>
    private IEnumerator ServerManageIllusionLifetime(NetworkObject illusionNO, float duration)
    {
        if (duration <= 0) duration = 0.1f; // Prevent zero/negative wait
        yield return new WaitForSeconds(duration);
        // Use force despawn which calls Die() -> Notify for cleanup
        ServerForceDespawnIllusion(illusionNO); 
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

        foreach (TouhouWebArena.Spellcards.SpellcardAction action in spellcardData.actions)
        {
            // Handle delay before this action starts
            if (action.startDelay > 0) yield return new WaitForSeconds(action.startDelay);
            if (action.bulletPrefabs == null || action.bulletPrefabs.Count == 0) continue; // Skip action if no prefabs assigned

            // --- Calculate adjusted position/angle based on target side ---
            Vector2 currentOffset = action.positionOffset;
            float currentAngle = action.angle;
            if (!isTargetOnPositiveSide) // Target is Player 1 (left side)
            {
                currentOffset.x *= -1f; // Flip the horizontal offset
                currentAngle *= -1f;   // Flip the angle across the Y-axis
            }

            int prefabIndex = 0;
            // Use adjusted offset for the base position
            Vector3 spawnPositionBase = originPosition + (Vector3)currentOffset;

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
                        // Rotation is already originRotation (we might need adjusted angle here too?)
                        // Let's assume Point doesn't need angle adjustment for now.
                        break;
                    case FormationType.Circle: // Spawn in a circle around offset point
                        if (action.count > 0) {
                            float angleDegrees = i * (360f / action.count);
                            // Use adjusted angle for the base rotation offset? No, circle bullets aim radially.
                            // Spawn position is fine (center), rotation aims them outwards.
                            spawnPos = spawnPositionBase;
                            // The rotation calculation here uses angleDegrees, which is relative to the circle itself, not the action angle.
                            // We might need to apply currentAngle *if* the base originRotation wasn't identity, but it is.
                            spawnRot = originRotation * Quaternion.Euler(0, 0, angleDegrees - 90f);
                        }
                        break;
                    case FormationType.Line: // Spawn in a line relative to offset point
                        // Use adjusted angle for line direction and rotation
                        float lineAngleRad = currentAngle * Mathf.Deg2Rad;
                        Vector3 lineDirection = originRotation * new Vector3(Mathf.Cos(lineAngleRad), Mathf.Sin(lineAngleRad), 0);
                        float totalLength = action.spacing * (action.count - 1);
                        float startOffset = -totalLength / 2f;
                        spawnPos = spawnPositionBase + lineDirection * (startOffset + i * action.spacing);
                        // Use adjusted angle for rotation
                        spawnRot = originRotation * Quaternion.Euler(0, 0, currentAngle);
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

                // --- Calculate Speed (using original action data) ---
                float currentBulletSpeed = action.speed;
                if (action.formation == FormationType.Line && action.speedIncrementPerBullet != 0f)
                {
                    currentBulletSpeed += (i * action.speedIncrementPerBullet);
                }

                // --- Configure Behavior AFTER Spawning (using original action data for behavior type etc) ---
                ConfigureBulletBehavior(bulletInstance, action, currentBulletSpeed, opponentId, capturedOpponentPosition, isTargetOnPositiveSide, originPosition);

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
    /// <param name="action">The SpellcardAction defining the behavior and other non-speed parameters.</param>
    /// <param name="currentSpeed">The calculated speed for this specific bullet.</param>
    /// <param name="opponentId">The ClientId of the opponent player (target for homing).</param>
    /// <param name="capturedOpponentPosition">The captured position of the opponent (initial target for homing).</param>
    /// <param name="isTargetOnPositiveSide">Whether the target is on the right side (for boundary checks).</param>
    /// <param name="spawnOrigin">The origin position used for calculating relative positions.</param>
    private void ConfigureBulletBehavior(GameObject bulletInstance, TouhouWebArena.Spellcards.SpellcardAction action, float currentSpeed, ulong opponentId, Vector3 capturedOpponentPosition, bool isTargetOnPositiveSide, Vector3 spawnOrigin)
    {
        // Get DoubleHoming component
        var doubleHoming = bulletInstance.GetComponent<DoubleHoming>(); 
        var lifetime = bulletInstance.GetComponent<NetworkBulletLifetime>();
        var spiral = bulletInstance.GetComponent<SpiralMovement>(); // Get SpiralMovement
        var delayedRandomTurn = bulletInstance.GetComponent<DelayedRandomTurn>(); // Get new component

        // Disable all potential behaviors first to ensure clean state
        var linear = bulletInstance.GetComponent<LinearMovement>();
        var delayedHoming = bulletInstance.GetComponent<DelayedHoming>();
        if (linear) linear.enabled = false;
        if (delayedHoming) delayedHoming.enabled = false;
        if (doubleHoming) doubleHoming.enabled = false; 
        if (spiral) spiral.enabled = false; // Disable spiral initially
        if (delayedRandomTurn) delayedRandomTurn.enabled = false; // Disable new one initially

        // Configure Lifetime Boundary Check and Target Role
        if (lifetime != null) {
            lifetime.keepOnPositiveSide = isTargetOnPositiveSide;
            if (action.lifetime > 0f)
            {
                lifetime.maxLifetime = action.lifetime;
            }
            
            // --- Assign Target Role --- 
            if (PlayerDataManager.Instance != null)
            {
                PlayerData? opponentData = PlayerDataManager.Instance.GetPlayerData(opponentId);
                lifetime.TargetPlayerRole.Value = opponentData.HasValue ? opponentData.Value.Role : PlayerRole.None;
            }
            else
            {
                Debug.LogError("[ServerAttackSpawner.ConfigureBulletBehavior] PlayerDataManager missing! Cannot set bullet TargetPlayerRole.");
                lifetime.TargetPlayerRole.Value = PlayerRole.None; // Assign default
            }
            // -------------------------

        } else {
            Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' missing NetworkBulletLifetime component.");
        }

        // Enable and initialize the specified movement behavior
        switch (action.behavior)
        {
            case BehaviorType.Linear:
                if (linear != null) { linear.enabled = true; linear.Initialize(currentSpeed); } // Use currentSpeed
                else { Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to Linear but missing LinearMovement component."); }
                break;
            case BehaviorType.DelayedHoming:
                if (delayedHoming != null)
                {
                    if (opponentId != ulong.MaxValue) // Ensure opponent exists
                    {
                         delayedHoming.enabled = true;
                         // Use currentSpeed for initial linear phase
                         delayedHoming.Initialize(currentSpeed, action.homingSpeed, action.homingDelay, opponentId, capturedOpponentPosition);
                    } else {
                        // Fallback to linear if no opponent found (should be rare in 2-player game)
                        Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DelayedHoming but no opponent found. Falling back to Linear.");
                        if (linear != null) { linear.enabled = true; linear.Initialize(currentSpeed); } // Fallback with currentSpeed
                    }
                }
                 else { Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DelayedHoming but missing DelayedHoming component."); }
                break;
            // --- ADDED BACK: DoubleHoming Case ---    
            case BehaviorType.DoubleHoming:
                if (doubleHoming != null)
                {
                    // Get opponent PlayerMovement component
                    PlayerMovement opponentMovement = null;
                    if (opponentId != ulong.MaxValue && NetworkManager.Singleton.ConnectedClients.TryGetValue(opponentId, out var opponentClient) && opponentClient.PlayerObject != null)
                    {
                        opponentMovement = opponentClient.PlayerObject.GetComponent<PlayerMovement>();
                    }
                    
                    if (opponentMovement != null)
                    {
                        doubleHoming.enabled = true;
                        // Initialize using currentSpeed, homingSpeed, delays, first duration, look-ahead distance, and opponent reference
                        doubleHoming.Initialize(
                            currentSpeed, 
                            action.homingSpeed, 
                            action.homingDelay, 
                            action.secondHomingDelay, 
                            action.firstHomingDuration, // Pass duration 1
                            action.secondHomingLookAheadDistance, // Pass look ahead distance
                            opponentMovement
                        );
                    }
                    else
                    {
                        // Fallback to linear if no opponent or opponent component found
                        Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DoubleHoming but couldn't find opponent PlayerMovement. Falling back to Linear.");
                        if (linear != null) { linear.enabled = true; linear.Initialize(currentSpeed); }
                    }
                }
                else { Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DoubleHoming but missing DoubleHoming component."); }
                break;
            // --- ADDED BACK: Spiral Case --- 
            case BehaviorType.Spiral:
                if (spiral != null)
                {
                    spiral.enabled = true;
                    // Pass spawnOrigin as the spawnCenter
                    spiral.Initialize(currentSpeed, action.tangentialSpeed, spawnOrigin); 
                }
                else
                {
                    Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to Spiral but missing SpiralMovement component.");
                }
                break;
            case BehaviorType.DelayedRandomTurn:
                if (delayedRandomTurn != null) 
                {
                    delayedRandomTurn.enabled = true;
                    delayedRandomTurn.Initialize(
                        currentSpeed, 
                        action.homingDelay, // Reuse homing delay for turn delay
                        action.minTurnSpeed, 
                        action.maxTurnSpeed, 
                        action.spreadAngle
                    );
                }
                 else { Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DelayedRandomTurn but missing DelayedRandomTurn component."); }
                break;
            // TODO: Add other cases like Homing if implemented
            default:
                 Debug.LogWarning($"[ServerAttackSpawner.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' has unhandled BehaviorType: {action.behavior}. Defaulting to Linear if possible.");
                 if (linear != null) { linear.enabled = true; linear.Initialize(currentSpeed); } // Default fallback with currentSpeed
                 break;
        }
    }

    /// <summary>
    /// **[Server Only]** Triggers a bullet-clearing effect around the player who activated a spellcard.
    /// The radius scales with the spell level.
    /// </summary>
    /// <param name="castingPlayerClientId">The ClientId of the player who cast the spellcard.</param>
    /// <param name="spellLevel">The level (2, 3, or 4) of the spellcard cast.</param>
    public void TriggerSpellcardClear(ulong castingPlayerClientId, int spellLevel)
    {
        if (!IsServer) return;

        // Get caster's player object
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(castingPlayerClientId, out NetworkClient networkClient) || networkClient.PlayerObject == null)
        {
            Debug.LogError($"[ServerAttackSpawner.TriggerSpellcardClear] Could not find player object for client {castingPlayerClientId}");
            return;
        }
        Transform playerTransform = networkClient.PlayerObject.transform;
        Vector3 playerPosition = playerTransform.position;

        // Determine radius based on spell level
        float clearRadius = 0f;
        switch (spellLevel)
        {
            case 2: clearRadius = 3.0f; break; // Tune these values
            case 3: clearRadius = 5.0f; break;
            case 4: clearRadius = 10.0f; break; // Large radius for Lv 4
            default: 
                Debug.LogWarning($"[ServerAttackSpawner.TriggerSpellcardClear] Invalid spell level {spellLevel} for clear effect.");
                return;
        }

        // Determine the role of the player casting the spell
        PlayerRole castingPlayerRole = PlayerRole.None;
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? data = PlayerDataManager.Instance.GetPlayerData(castingPlayerClientId);
            if (data.HasValue) { castingPlayerRole = data.Value.Role; }
        }
        if (castingPlayerRole == PlayerRole.None)
        {
            Debug.LogWarning($"[ServerAttackSpawner.TriggerSpellcardClear] Could not determine PlayerRole for ClientId {castingPlayerClientId}.");
            // Decide if we should proceed with PlayerRole.None or return
        }

        // Find colliders in radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(playerPosition, clearRadius);
        // Debug.Log($"[ServerAttackSpawner.TriggerSpellcardClear] Level {spellLevel} clear triggered by {castingPlayerClientId}. Found {colliders.Length} colliders in radius {clearRadius}.");

        foreach (Collider2D col in colliders)
        {
            // Check if the collider belongs to a NetworkObject with IClearable
            NetworkObject netObj = col.GetComponentInParent<NetworkObject>(); // Use GetComponentInParent for flexibility
            if (netObj != null)
            {
                IClearable[] clearables = netObj.GetComponentsInChildren<IClearable>(true); // Include inactive components
                foreach (IClearable clearable in clearables)
                {
                    // Use forced = true for spellcard clear, similar to deathbomb
                    clearable.Clear(true, castingPlayerRole); 
                    // Debug.Log($"    Cleared {netObj.name} via IClearable component.");
                }
            }
        }
        // TODO: Trigger visual effect via ClientRpc?
    }
} 