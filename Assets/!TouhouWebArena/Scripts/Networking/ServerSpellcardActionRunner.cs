using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TouhouWebArena;
using TouhouWebArena.Spellcards;

/// <summary>
/// **[Server Only MonoBehaviour]** Executes the sequence of actions defined in a Level 2/3 SpellcardData.
/// Handles delays, formations, and spawning projectiles using helpers.
/// Should be attached to the same GameObject as <see cref="ServerAttackSpawner"/>.
/// </summary>
public class ServerSpellcardActionRunner : NetworkBehaviour
{
    /// <summary>Singleton instance of the ServerSpellcardActionRunner.</summary>
    public static ServerSpellcardActionRunner Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ServerSpellcardActionRunner] Duplicate instance detected. Destroying self.", gameObject);
            Destroy(this); // Destroy the component, not the GO
        }
        else
        {
            Instance = this;
        }
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        // Only needed on server
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// **[Server Only Coroutine]** Executes the actions defined in a <see cref="SpellcardData"/> ScriptableObject.
    /// Called by <see cref="ServerSpellcardExecutor"/>.
    /// </summary>
    /// <param name="spellcardData">The ScriptableObject defining the spellcard pattern.</param>
    /// <param name="originPosition">The calculated world origin point for the spellcard pattern (usually above the opponent).</param>
    /// <param name="originRotation">The base world rotation for the spellcard pattern.</param>
    /// <param name="opponentId">The NetworkClientId of the opponent player (target for behaviors).</param>
    /// <param name="capturedOpponentPosition">The opponent's world position captured when the spellcard was initiated.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    public IEnumerator RunSpellcardActions(SpellcardData spellcardData, Vector3 originPosition, Quaternion originRotation, ulong opponentId, Vector3 capturedOpponentPosition)
    {
        if (!IsServer) yield break;
        if (NetworkObjectPool.Instance == null) // Check pool instance for pooled bullets
        {
            // If spellcards exclusively use non-pooled bullets, this check might be unnecessary.
            // However, it's safer to keep it if some spellcard bullets *might* be pooled.
            Debug.LogWarning("[ServerSpellcardActionRunner] NetworkObjectPool instance not found! Pooled spellcard bullets cannot be spawned.");
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

             // Use WaitForSeconds for intra-action delay
            WaitForSeconds intraActionWait = (action.intraActionDelay > 0f) ? new WaitForSeconds(action.intraActionDelay) : null;

            // Spawn projectiles for this action
            for (int i = 0; i < action.count; i++)
            {
                 // Intra-action delay BEFORE spawning (except first bullet)
                if (i > 0 && intraActionWait != null) yield return intraActionWait;

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
                         // Adjust rotation based on angle
                        spawnRot *= Quaternion.Euler(0, 0, currentAngle);
                        break;
                    case FormationType.Circle: // Spawn in a circle around offset point
                        if (action.count > 0) {
                            float angleDegrees = i * (360f / action.count) + currentAngle; // Add base angle
                            // Use spawnPositionBase for the center
                            spawnPos = spawnPositionBase + originRotation * (Quaternion.Euler(0, 0, angleDegrees) * Vector3.right * action.radius);
                            // Rotation aims bullets radially outwards relative to pattern rotation
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
                    // Get from pool - use ServerPooledSpawner
                    // Spellcard bullets owned by server (null owner ID)
                    bulletInstanceNO = ServerPooledSpawner.SpawnSinglePooledBullet(prefabToSpawn, spawnPos, spawnRot, ulong.MaxValue); 
                    if (bulletInstanceNO != null)
                    {
                        bulletInstance = bulletInstanceNO.gameObject;
                    }
                    else
                    {
                        // Use string interpolation
                         Debug.LogError($"[ServerSpellcardActionRunner] Failed to get pooled object for PrefabID: {identity.PrefabID} using ServerPooledSpawner.");
                         continue; // Skip this bullet
                    }
                }
                else
                {
                    bulletInstance = Instantiate(prefabToSpawn, spawnPos, spawnRot);
                    bulletInstanceNO = bulletInstance.GetComponent<NetworkObject>();
                    if (bulletInstanceNO == null)
                    {
                         // Use string interpolation
                        Debug.LogError($"[ServerSpellcardActionRunner] Non-pooled spellcard prefab '{prefabToSpawn.name}' is missing NetworkObject component!");
                        Destroy(bulletInstance);
                        continue; // Skip this bullet
                    }
                    bulletInstanceNO.Spawn(true); // Spawn server-owned
                }

                // --- Calculate Speed (Apply increment universally if set) ---
                float currentSpeed = action.speed;
                if (action.speedIncrementPerBullet != 0f)
                {
                    currentSpeed += (i * action.speedIncrementPerBullet);
                }
                // ----------------------------------------------------------

                // --- Configure Behavior AFTER Spawning (using original action data for behavior type etc) ---
                // ServerBulletConfigurer.ConfigureBulletBehavior(bulletInstance, action, currentSpeed, opponentId, capturedOpponentPosition, isTargetOnPositiveSide, originPosition);

                // Parenting for pooled objects is handled by ServerPooledSpawner

                prefabIndex++;
            }
        }
    }

    /// <summary>
    /// **[Server Only Coroutine]** Executes a single SpellcardAction, spawning projectiles potentially over time.
    /// Uses the provided illusion's transform to get the up-to-date origin for each bullet if movement occurs during the action.
    /// Applies base rotation for pattern aiming and handles target-side flipping.
    /// Likely called by <see cref="Level4IllusionController"/>.
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
                    // Use adjusted angle for rotation
                    spawnRot = baseRotation * Quaternion.Euler(0, 0, relativeAngle);
                    
                    // Optional: If bullet prefabs point right by default, uncomment the next line
                    // spawnRot *= Quaternion.Euler(0, 0, -90f); 

                    break;
            }

            GameObject bulletInstance = null;
            NetworkObject bulletInstanceNO = null;

            if (usePool)
            {
                 // Spellcard bullets owned by server
                bulletInstanceNO = ServerPooledSpawner.SpawnSinglePooledBullet(prefabToSpawn, spawnPos, spawnRot, ulong.MaxValue);
                if (bulletInstanceNO != null)
                {
                    bulletInstance = bulletInstanceNO.gameObject;
                }
                 // Use string interpolation
                else { Debug.LogError($"[ServerSpellcardActionRunner] Failed to get pooled object {identity.PrefabID} in single action coroutine."); continue; }
            }
            else
            {
                bulletInstance = Instantiate(prefabToSpawn, spawnPos, spawnRot);
                bulletInstanceNO = bulletInstance.GetComponent<NetworkObject>();
                 // Use string interpolation
                if (bulletInstanceNO == null) { Debug.LogError($"[ServerSpellcardActionRunner] Non-pooled prefab {prefabToSpawn.name} missing NetworkObject in single action coroutine."); Destroy(bulletInstance); continue; }
                bulletInstanceNO.Spawn(true); // Spawn server-owned
            }

            // --- Calculate Speed (Apply increment universally if set) ---
            float currentBulletSpeed = action.speed;
            if (action.speedIncrementPerBullet != 0f)
            {
                currentBulletSpeed += (i * action.speedIncrementPerBullet);
            }
            // ----------------------------------------------------------

            // --- Configure Behavior ---
            // ServerBulletConfigurer.ConfigureBulletBehavior(bulletInstance, action, currentBulletSpeed, targetClientId, capturedTargetPosition, isTargetOnPositiveSide, spawnPositionBase);

            // Parenting handled by pooled spawner

            prefabIndex++;
        }
    }
} 