using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TouhouWebArena;
using TouhouWebArena.Spellcards;
using TouhouWebArena.Spellcards.Behaviors; // For ClientBulletConfigurer

/// <summary>
/// [Client Only] Client-side service responsible for executing sequences of SpellcardActions locally.
/// Handles delays, calculates formations, and spawns projectiles using ClientGameObjectPool.
/// </summary>
public class ClientSpellcardActionRunner : MonoBehaviour
{
    // Optional: Singleton instance if needed, or accessed via ClientSpellcardExecutor
    // public static ClientSpellcardActionRunner Instance { get; private set; } 

    // void Awake() { /* Singleton setup if needed */ }
    // void OnDestroy() { /* Singleton cleanup if needed */ }

    // Reference to the object pool
    private ClientGameObjectPool _poolInstance;

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
    /// Starts the execution of a sequence of spellcard actions (e.g., for Level 2/3 spellcards).
    /// </summary>
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
    /// Starts the execution of a single spellcard action (e.g., for Level 4 illusion attacks).
    /// </summary>
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
        Vector3 sequenceOffset = sharedRandomOffset;
        // --- END SHARED RANDOM OFFSET ---

        foreach (SpellcardAction action in actions)
        {
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

                // Pass the single sequenceOffset to CalculateSpawnPosition
                Vector3 spawnPosition = CalculateSpawnPosition(action, i, originPosition, originRotation, targetPlayerRole, sequenceOffset); 
                Quaternion spawnRotation = CalculateSpawnRotation(action, i, originRotation);

                bulletInstance.transform.position = spawnPosition;
                bulletInstance.transform.rotation = spawnRotation;
                ClientBulletConfigurer.ConfigureBullet(bulletInstance, action, casterClientId, targetClientId, i);
                bulletInstance.SetActive(true);
                bulletsSpawnedThisAction++;

                if (action.intraActionDelay > 0 && i < action.count - 1) yield return new WaitForSeconds(action.intraActionDelay);
            }
        }
    }
    
    // Helper to cycle through bullet prefabs
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

        // APPLY SHARED sequence offset AFTER formation offset is calculated
        Vector3 finalLocalOffset = baseLocalOffset + sequenceOffset;

        return originPos + (originRot * finalLocalOffset);
    }
    
    // Helper to calculate initial spawn rotation
    private Quaternion CalculateSpawnRotation(SpellcardAction action, int index, Quaternion originRot)
    { 
        // TODO: Implement more complex rotation logic if needed (e.g., circle bullets facing out)
        // For now, basic rotation based on formation angle + origin rotation.
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
} 