using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TouhouWebArena;
using TouhouWebArena.Spellcards;
using TouhouWebArena.Spellcards.Behaviors; // For ClientBulletConfigurer
using Unity.Netcode;

/// <summary>
/// [Client Only] Handles the client-side execution of spellcard actions defined in `SpellcardAction` objects.
/// This class is responsible for interpreting action sequences, managing delays, calculating bullet spawn positions
/// and rotations based on formation types, and interfacing with the `ClientGameObjectPool` to spawn projectiles.
/// It supports both static origin attacks (where all bullets originate from a fixed point and orientation)
/// and dynamic origin attacks (where bullets can be spawned from a moving `Transform` with an explicit orientation,
/// typically used for illusions moving while attacking).
/// Multiple actions within a sequence can be started concurrently if their `startDelay` allows.
/// </summary>
public class ClientSpellcardActionRunner : MonoBehaviour
{
    // Optional: Singleton instance for global access, if preferred over GetComponent.
    // public static ClientSpellcardActionRunner Instance { get; private set; } 

    // void Awake() { /* Singleton setup if needed */ }
    // void OnDestroy() { /* Singleton cleanup if needed */ }

    private ClientGameObjectPool _poolInstance; // Cached reference to the object pool for projectile spawning.

    void Start()
    {
        // Get the pool instance (ensure ClientGameObjectPool initializes first)
        _poolInstance = ClientGameObjectPool.Instance;
        if (_poolInstance == null)
        {
            Debug.LogError("[ClientSpellcardActionRunner] ClientGameObjectPool instance not found!");
            enabled = false;
        }
    }

    /// <summary>
    /// Initiates the execution of a list of `SpellcardAction` objects from a static origin.
    /// Suitable for spellcards where the caster remains stationary or the attack pattern is fixed relative to a point.
    /// Each action in the list is started as a separate coroutine, allowing for concurrent execution based on `startDelay`.
    /// </summary>
    /// <param name="casterClientId">The NetworkObjectId of the entity casting the spellcard.</param>
    /// <param name="targetClientId">The NetworkObjectId of the target (for homing or targeted effects, passed to ClientBulletConfigurer).</param>
    /// <param name="actions">The list of `SpellcardAction` to execute.</param>
    /// <param name="originPosition">The world-space position from which all actions in this sequence will originate.</param>
    /// <param name="originRotation">The base world-space rotation for all actions in this sequence.</param>
    /// <param name="sharedRandomOffset">A random offset applied consistently to all bullets spawned by this sequence (if the action's `applyRandomSpawnOffset` is true, though current implementation uses this as a sequence-wide base offset).</param>
    public void RunSpellcardActions(ulong casterClientId, ulong targetClientId, List<SpellcardAction> actions, Vector3 originPosition, Quaternion originRotation, Vector2 sharedRandomOffset)
    {
        if (actions == null || actions.Count == 0)
        {
            Debug.LogWarning("[ClientSpellcardActionRunner] RunSpellcardActions called with no actions.");
            return;
        }
        StartCoroutine(ExecuteActionSequenceCoroutine(casterClientId, targetClientId, actions, originPosition, originRotation, sharedRandomOffset));
    }

    /// <summary>
    /// Initiates the execution of a single `SpellcardAction` from a static origin.
    /// Convenience method that wraps the action in a list and calls `RunSpellcardActions`.
    /// </summary>
    /// <param name="casterClientId">The NetworkObjectId of the entity casting the spellcard.</param>
    /// <param name="targetClientId">The NetworkObjectId of the target.</param>
    /// <param name="action">The `SpellcardAction` to execute.</param>
    /// <param name="originPosition">The world-space position from which the action will originate.</param>
    /// <param name="originRotation">The base world-space rotation for the action.</param>
    public void RunSingleSpellcardAction(ulong casterClientId, ulong targetClientId, SpellcardAction action, Vector3 originPosition, Quaternion originRotation)
    {
        if (action == null)
        {
            Debug.LogWarning("[ClientSpellcardActionRunner] RunSingleSpellcardAction called with null action.");
            return;
        }
        // We can reuse the main coroutine logic, just passing a single-item list
        StartCoroutine(ExecuteActionSequenceCoroutine(casterClientId, targetClientId, new List<SpellcardAction>{ action }, originPosition, originRotation, Vector2.zero));
        // Alternatively, create a separate coroutine if single action logic differs significantly.
        // StartCoroutine(ExecuteSingleActionCoroutine(casterClientId, targetClientId, action, originPosition, originRotation)); 
    }

    /// <summary>
    /// Initiates the execution of a list of `SpellcardAction` objects using a dynamic origin `Transform`.
    /// This is designed for scenarios like a moving illusion firing bullets. The bullets will spawn relative to the
    /// `originTransform`'s position at the moment of spawning, but will use the `explicitAttackOrientation` for their rotation.
    /// Each action in the list is started as a separate coroutine.
    /// </summary>
    /// <param name="casterClientId">The NetworkObjectId of the entity casting the spellcard (e.g., the illusion).</param>
    /// <param name="targetClientId">The NetworkObjectId of the target.</param>
    /// <param name="actions">The list of `SpellcardAction` to execute.</param>
    /// <param name="originTransform">The `Transform` whose world position will be used as the origin for spawning bullets. This is read each time a bullet is spawned.</param>
    /// <param name="explicitAttackOrientation">The world-space rotation to be applied to spawned bullets, overriding the `originTransform`'s rotation for bullet orientation.</param>
    /// <param name="sharedRandomOffset">A random offset applied consistently to all bullets spawned by this sequence.</param>
    public void RunSpellcardActionsDynamicOrigin(ulong casterClientId, ulong targetClientId, List<SpellcardAction> actions, Transform originTransform, Quaternion explicitAttackOrientation, Vector2 sharedRandomOffset)
    {
        if (actions == null || actions.Count == 0)
        {
            Debug.LogWarning("[ClientSpellcardActionRunner] RunSpellcardActionsDynamicOrigin called with no actions.");
            return;
        }
        if (originTransform == null)
        {
            Debug.LogError("[ClientSpellcardActionRunner] RunSpellcardActionsDynamicOrigin called with null originTransform.");
            return;
        }
        StartCoroutine(ExecuteActionSequenceCoroutineDynamicOrigin(casterClientId, targetClientId, actions, originTransform, explicitAttackOrientation, sharedRandomOffset));
    }

    /// <summary>
    /// Initiates the execution of a single `SpellcardAction` using a dynamic origin `Transform`.
    /// Convenience method that wraps the action in a list and calls `RunSpellcardActionsDynamicOrigin`.
    /// </summary>
    /// <param name="casterClientId">The NetworkObjectId of the entity casting the spellcard.</param>
    /// <param name="targetClientId">The NetworkObjectId of the target.</param>
    /// <param name="action">The `SpellcardAction` to execute.</param>
    /// <param name="originTransform">The `Transform` whose world position will be used as the origin for spawning bullets. This is read each time a bullet is spawned.</param>
    /// <param name="explicitAttackOrientation">The world-space rotation to be applied to spawned bullets, overriding the `originTransform`'s rotation for bullet orientation.</param>
    public void RunSingleSpellcardActionDynamicOrigin(ulong casterClientId, ulong targetClientId, SpellcardAction action, Transform originTransform, Quaternion explicitAttackOrientation)
    {
        if (action == null)
        {
            Debug.LogWarning("[ClientSpellcardActionRunner] RunSingleSpellcardActionDynamicOrigin called with null action.");
            return;
        }
        if (originTransform == null)
        {
            Debug.LogError("[ClientSpellcardActionRunner] RunSingleSpellcardActionDynamicOrigin called with null originTransform.");
            return;
        }
        StartCoroutine(ExecuteActionSequenceCoroutineDynamicOrigin(casterClientId, targetClientId, new List<SpellcardAction>{ action }, originTransform, explicitAttackOrientation, Vector2.zero));
    }

    /// <summary>
    /// Coroutine that iterates through a list of `SpellcardAction` objects and starts an individual coroutine
    /// (`ExecuteSingleActionCoroutineInternal`) for each one. This allows actions to run concurrently based on their `startDelay`.
    /// Uses a static origin position and rotation for all actions in the sequence.
    /// </summary>
    private IEnumerator ExecuteActionSequenceCoroutine(ulong casterClientId, ulong targetClientId, List<SpellcardAction> actions, Vector3 originPosition, Quaternion originRotation, Vector2 sharedRandomOffset)
    {
        if (_poolInstance == null) yield break; 
        PlayerRole targetPlayerRole = PlayerRole.None; 
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? targetData = PlayerDataManager.Instance.GetPlayerData(targetClientId);
            if (targetData.HasValue) targetPlayerRole = targetData.Value.Role;
        }

        // --- CALCULATE SHARED RANDOM OFFSET (based on first action) --- 
        // This offset is calculated once for the entire sequence if the first action requests it.
        // If individual actions need truly independent random offsets, this logic might need to be per-action.
        // For now, assuming a single shared offset for the composite pattern is desired.
        Vector3 sequenceOffset = sharedRandomOffset; 
        // --- END SHARED RANDOM OFFSET ---

        foreach (SpellcardAction action in actions)
        {
            // Launch a separate coroutine for each action. This allows actions with startDelay = 0
            // to effectively begin their execution logic (including the delay itself) in parallel.
            StartCoroutine(ExecuteSingleActionCoroutineInternal(casterClientId, targetClientId, action, originPosition, originRotation, targetPlayerRole, sequenceOffset));
        }
        yield return null; // Yield once to allow all started coroutines to begin processing.
    }

    /// <summary>
    /// Internal coroutine to execute a single `SpellcardAction` from a static origin.
    /// Handles `startDelay`, then iterates `action.count` times to spawn bullets.
    /// Manages `intraActionDelay` between bullet spawns within the same action.
    /// Calculates spawn position and rotation for each bullet and configures it using `ClientBulletConfigurer`.
    /// </summary>
    private IEnumerator ExecuteSingleActionCoroutineInternal(ulong casterClientId, ulong targetClientId, SpellcardAction action, Vector3 originPosition, Quaternion originRotation, PlayerRole targetPlayerRole, Vector3 sequenceOffset)
    {
        if (_poolInstance == null) yield break;

        if (action.startDelay > 0) yield return new WaitForSeconds(action.startDelay);

        int bulletsSpawnedThisAction = 0;
        for (int i = 0; i < action.count; i++)
        {
            if (action.skipEveryNth > 0 && (i + 1) % action.skipEveryNth == 0)
            {
                if (action.intraActionDelay > 0 && i < action.count - 1) yield return new WaitForSeconds(action.intraActionDelay);
                continue; 
            }

            GameObject bulletPrefab = GetBulletPrefab(action.bulletPrefabs, bulletsSpawnedThisAction);
            if (bulletPrefab == null) { Debug.LogError($"[ClientSpellcardActionRunner] Could not get bullet prefab for action."); continue; }
            PooledObjectInfo poolInfo = bulletPrefab.GetComponent<PooledObjectInfo>();
            if (poolInfo == null || string.IsNullOrEmpty(poolInfo.PrefabID)) { Debug.LogError($"[ClientSpellcardActionRunner] Bullet prefab '{bulletPrefab.name}' missing PooledObjectInfo/PrefabID!"); continue; }
            GameObject bulletInstance = _poolInstance.GetObject(poolInfo.PrefabID);
            if (bulletInstance == null) { Debug.LogWarning($"[ClientSpellcardActionRunner] Pool returned null for PrefabID '{poolInfo.PrefabID}'."); continue; }

            if (!bulletInstance.activeSelf)
            {
                bulletInstance.SetActive(true);
            }

            // --- ADDED: Set Owner Role on StageSmallBulletMoverScript ---
            StageSmallBulletMoverScript stageMover = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
            if (stageMover != null)
            {
                // --- MODIFIED: Determine Role based on ORIGINAL Caster (Owner of Illusion) ---
                // If the casterClientId corresponds to a NetworkObject (e.g., an illusion),
                // its OwnerClientId (the original player) is used to determine the bullet's OwningPlayerRole.
                // Otherwise, casterClientId (assumed to be a direct player ID) is used.
                PlayerRole newBulletOwnerRole = PlayerRole.None;
                ulong effectiveOwnerId = casterClientId; // Start with the ID passed in

                // Check if the casterId corresponds to a known NetworkObject (like an illusion)
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterClientId, out NetworkObject casterNetworkObject))
                {
                    // If it's an illusion or other owned object, use the OWNER's ID to find the PlayerRole
                    effectiveOwnerId = casterNetworkObject.OwnerClientId;
                    // Debug.Log($"[CSAR] Caster ID {casterClientId} resolved to Owner ID {effectiveOwnerId} (likely an illusion)");
                }
                // else: casterClientId likely refers to a player directly, use it as is.

                if (PlayerDataManager.Instance != null) {
                    PlayerData? ownerData = PlayerDataManager.Instance.GetPlayerData(effectiveOwnerId);
                    if (ownerData.HasValue) newBulletOwnerRole = ownerData.Value.Role;
                    else Debug.LogWarning($"[CSAR] Effective Owner PData null for {effectiveOwnerId} (original caster: {casterClientId})");
                } else Debug.LogWarning("[CSAR] PDMgr null");
                // --- END MODIFICATION ---
                
                stageMover.InitializeOwnerRole(newBulletOwnerRole);
            }
            // --- END ADDED SECTION ---

            Vector3 spawnPosition = CalculateSpawnPosition(action, i, originPosition, originRotation, targetPlayerRole, sequenceOffset); 
            Quaternion spawnRotation = CalculateSpawnRotation(action, i, originRotation);

            bulletInstance.transform.position = spawnPosition;
            bulletInstance.transform.rotation = spawnRotation;
            ClientBulletConfigurer.ConfigureBullet(bulletInstance, action, casterClientId, targetClientId, i);
            bulletsSpawnedThisAction++;

            if (action.intraActionDelay > 0 && i < action.count - 1) yield return new WaitForSeconds(action.intraActionDelay);
        }
    }
    
    // Helper to cycle through bullet prefabs if multiple are defined in an action.
    private GameObject GetBulletPrefab(List<GameObject> prefabs, int index)
    {
        if (prefabs == null || prefabs.Count == 0) return null;
        return prefabs[index % prefabs.Count];
    }

    // MODIFIED CalculateSpawnPosition signature and logic
    private Vector3 CalculateSpawnPosition(SpellcardAction action, int index, Vector3 originPos, Quaternion originRot, PlayerRole targetPlayerRole, Vector3 sequenceOffset)
    {
        Vector3 baseLocalOffset = Vector3.zero;
        switch (action.formation)
        {
            case FormationType.Point:
                baseLocalOffset = action.positionOffset;
                break;
            case FormationType.Circle:
                float angleStepCircle = 360f / Mathf.Max(1, action.count); 
                float currentAngleRad = Mathf.Deg2Rad * (action.angle + index * angleStepCircle);
                baseLocalOffset = new Vector3(Mathf.Cos(currentAngleRad), Mathf.Sin(currentAngleRad)) * action.radius + (Vector3)action.positionOffset;
                break;
            case FormationType.Line:
                float distance = index * action.spacing;
                Vector3 direction = Quaternion.Euler(0, 0, action.angle) * Vector3.right;
                baseLocalOffset = direction * distance + (Vector3)action.positionOffset;
                break;
        }

        // Apply the sequence-wide shared offset to the calculated local offset of the formation.
        Vector3 finalLocalOffset = baseLocalOffset + sequenceOffset;

        // Transform the final local offset by the origin's rotation and add it to the origin's position.
        return originPos + (originRot * finalLocalOffset);
    }
    
    // Helper to calculate initial spawn rotation for a bullet within an action.
    private Quaternion CalculateSpawnRotation(SpellcardAction action, int index, Quaternion originRot)
    { 
        // Determines the local rotation of a bullet based on its action's properties (e.g., formation angle).
        // This local rotation is then combined with the overall originRotation.
        Quaternion localRotation = Quaternion.Euler(0, 0, action.angle);
        switch (action.formation)
        {
             case FormationType.Circle:
                 // Bullets in a circle often face outwards or inwards, or all same direction
                 // Facing outwards example:
                 float angleStep = 360f / action.count;
                 float currentAngle = action.angle + index * angleStep;
                 localRotation = Quaternion.Euler(0, 0, currentAngle);
                 break;
             case FormationType.Line:
                // Bullets in a line usually share the line's orientation
                 localRotation = Quaternion.Euler(0, 0, action.angle);
                 break;
            case FormationType.Point:
                 // All bullets share the base angle
                 localRotation = Quaternion.Euler(0, 0, action.angle);
                 break;
        }
        return originRot * localRotation;
    }

    /// <summary>
    /// Coroutine that iterates through a list of `SpellcardAction` objects and starts an individual coroutine
    /// (`ExecuteSingleActionCoroutineInternalDynamicOrigin`) for each. This allows concurrent execution based on `startDelay`.
    /// Uses a dynamic `originTransform` for position (read per bullet) and an `explicitAttackOrientation` for rotation.
    /// </summary>
    private IEnumerator ExecuteActionSequenceCoroutineDynamicOrigin(ulong casterClientId, ulong targetClientId, List<SpellcardAction> actions, Transform originTransform, Quaternion explicitAttackOrientation, Vector2 sharedRandomOffset)
    {
        if (_poolInstance == null) yield break; 
        PlayerRole targetPlayerRole = PlayerRole.None; 
        if (PlayerDataManager.Instance != null)
        {
            PlayerData? targetData = PlayerDataManager.Instance.GetPlayerData(targetClientId);
            if (targetData.HasValue) targetPlayerRole = targetData.Value.Role;
        }

        Vector3 sequenceOffset = sharedRandomOffset; 

        foreach (SpellcardAction action in actions)
        {
            // Each action gets its own coroutine, using the shared dynamic transform and explicit orientation.
            StartCoroutine(ExecuteSingleActionCoroutineInternalDynamicOrigin(casterClientId, targetClientId, action, originTransform, explicitAttackOrientation, targetPlayerRole, sequenceOffset));
        }
        yield return null; // Yield once to allow all started coroutines to begin.
    }

    /// <summary>
    /// Internal coroutine to execute a single `SpellcardAction` using a dynamic origin `Transform`.
    /// Handles `startDelay`, then iterates `action.count` times to spawn bullets.
    /// For each bullet, it reads the current `originTransform.position` for the spawn location
    /// but uses the `explicitAttackOrientation` for the bullet's rotation.
    /// Manages `intraActionDelay` between spawns.
    /// </summary>
    private IEnumerator ExecuteSingleActionCoroutineInternalDynamicOrigin(ulong casterClientId, ulong targetClientId, SpellcardAction action, Transform originTransform, Quaternion explicitAttackOrientation, PlayerRole targetPlayerRole, Vector3 sequenceOffset)
    {
        if (_poolInstance == null || action == null || originTransform == null) yield break;

        if (action.startDelay > 0) yield return new WaitForSeconds(action.startDelay);

        int bulletsSpawnedThisAction = 0;
        for (int i = 0; i < action.count; i++)
        {
            if (action.skipEveryNth > 0 && (i + 1) % action.skipEveryNth == 0)
            {
                if (action.intraActionDelay > 0 && i < action.count - 1) yield return new WaitForSeconds(action.intraActionDelay);
                continue; 
            }

            GameObject bulletPrefab = GetBulletPrefab(action.bulletPrefabs, bulletsSpawnedThisAction);
            if (bulletPrefab == null) { Debug.LogError($"[ClientSpellcardActionRunner] Could not get bullet prefab for action."); continue; }
            PooledObjectInfo poolInfo = bulletPrefab.GetComponent<PooledObjectInfo>();
            if (poolInfo == null || string.IsNullOrEmpty(poolInfo.PrefabID)) { Debug.LogError($"[ClientSpellcardActionRunner] Bullet prefab '{bulletPrefab.name}' missing PooledObjectInfo/PrefabID!"); continue; }
            GameObject bulletInstance = _poolInstance.GetObject(poolInfo.PrefabID);
            if (bulletInstance == null) { Debug.LogWarning($"[ClientSpellcardActionRunner] Pool returned null for PrefabID '{poolInfo.PrefabID}'."); continue; }

            if (!bulletInstance.activeSelf)
            {
                bulletInstance.SetActive(true);
            }

            // --- ADDED: Set Owner Role on StageSmallBulletMoverScript (Dynamic Origin) ---
            StageSmallBulletMoverScript stageMoverDynamic = bulletInstance.GetComponent<StageSmallBulletMoverScript>();
            if (stageMoverDynamic != null)
            {
                 // --- MODIFIED: Determine Role based on ORIGINAL Caster (Owner of Illusion) ---
                // For bullets spawned by a dynamic origin (typically an illusion via ClientIllusionView),
                // the casterClientId is the illusion's NetworkObjectId. Its OwnerClientId (the original player)
                // is used to determine the bullet's OwningPlayerRole.
                PlayerRole newBulletOwnerRoleDynamic = PlayerRole.None;
                ulong effectiveOwnerIdDynamic = casterClientId; // Start with the ID passed in (illusion's ID)

                // Check if the casterId corresponds to a known NetworkObject (the illusion)
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(casterClientId, out NetworkObject illusionNetworkObject))
                {
                    // If it's an illusion or other owned object, use the OWNER's ID to find the PlayerRole
                    effectiveOwnerIdDynamic = illusionNetworkObject.OwnerClientId;
                    // Debug.Log($"[CSAR-Dyn] Caster ID {casterClientId} resolved to Owner ID {effectiveOwnerIdDynamic} (likely an illusion)");
                }
                // else: Should not happen for dynamic origin calls from ClientIllusionView, but good failsafe.

                if (PlayerDataManager.Instance != null) {
                    PlayerData? ownerDataDynamic = PlayerDataManager.Instance.GetPlayerData(effectiveOwnerIdDynamic);
                    if (ownerDataDynamic.HasValue) newBulletOwnerRoleDynamic = ownerDataDynamic.Value.Role;
                    else Debug.LogWarning($"[CSAR-Dyn] Effective Owner PData null for {effectiveOwnerIdDynamic} (original caster: {casterClientId})");
                } else Debug.LogWarning("[CSAR-Dyn] PDMgr null");
                // --- END MODIFICATION ---

                stageMoverDynamic.InitializeOwnerRole(newBulletOwnerRoleDynamic);
            }
            // --- END ADDED SECTION ---

            // Fetch current position from the dynamic transform FOR EACH BULLET/SUB-SPAWN
            Vector3 currentOriginPos = originTransform.position;
            // Use the explicitly passed attack orientation for rotation calculations
            Quaternion currentOriginRot = explicitAttackOrientation; 

            Vector3 spawnPosition = CalculateSpawnPosition(action, i, currentOriginPos, currentOriginRot, targetPlayerRole, sequenceOffset); 
            Quaternion spawnRotation = CalculateSpawnRotation(action, i, currentOriginRot);

            bulletInstance.transform.position = spawnPosition;
            bulletInstance.transform.rotation = spawnRotation;
            ClientBulletConfigurer.ConfigureBullet(bulletInstance, action, casterClientId, targetClientId, i);
            bulletsSpawnedThisAction++;

            if (action.intraActionDelay > 0 && i < action.count - 1) yield return new WaitForSeconds(action.intraActionDelay);
        }
    }
} 