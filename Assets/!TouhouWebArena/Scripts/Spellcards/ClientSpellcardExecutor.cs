using UnityEngine;
using Unity.Netcode;
using TouhouWebArena;
using TouhouWebArena.Spellcards;
using System.Collections.Generic; // For List in RunSpellcardActions

/// <summary>
/// [Client Only] Client-side singleton responsible for initiating the local execution 
/// of spellcards based on commands received from the server via SpellcardNetworkHandler.
/// It loads the spellcard data and delegates the action sequence execution to ClientSpellcardActionRunner.
/// </summary>
public class ClientSpellcardExecutor : MonoBehaviour
{
    public static ClientSpellcardExecutor Instance { get; private set; }

    private ClientSpellcardActionRunner _actionRunner;
    // private ClientIllusionController illusionController; // Need way to manage/spawn illusions client-side

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            _actionRunner = GetComponent<ClientSpellcardActionRunner>();
            if (_actionRunner == null) 
            {
                Debug.LogError("[ClientSpellcardExecutor] ClientSpellcardActionRunner component not found on the same GameObject! Spellcards may not function.", this);
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Called by SpellcardNetworkHandler when an ExecuteSpellcardClientRpc is received.
    /// Starts the process of executing the spellcard locally.
    /// </summary>
    public void StartLocalSpellcardExecution(ulong casterClientId, ulong targetClientId, string spellcardDataResourcePath, int spellLevel, Vector2 sharedRandomOffset)
    {
        Debug.Log($"[ClientSpellcardExecutor] Attempting to start local execution for Lv{spellLevel} spellcard: {spellcardDataResourcePath}. Caster: {casterClientId}, Target: {targetClientId}, Offset: {sharedRandomOffset}");

        if (_actionRunner == null)
        {
            Debug.LogError("[ClientSpellcardExecutor] ActionRunner is null, cannot execute spellcard.");
            return;
        }

        Vector3 originPosition = CalculateSpellcardOrigin(targetClientId, casterClientId); 
        Quaternion originRotation = CalculateSpellcardRotation(targetClientId, casterClientId, originPosition); 

        ScriptableObject spellcardBaseData = Resources.Load<ScriptableObject>(spellcardDataResourcePath);
        if (spellcardBaseData == null) 
        {
            Debug.LogError($"[ClientSpellcardExecutor] Failed to load SpellcardData from Resources path: {spellcardDataResourcePath}"); 
            return; 
        }

        if (spellLevel == 4 && spellcardBaseData is Level4SpellcardData level4Data)
        {             
            // TODO: Pass sharedRandomOffset to Level 4 handling if needed
            // HandleLevel4Execution(casterClientId, targetClientId, level4Data, originPosition, originRotation, sharedRandomOffset); // Placeholder for now
            Debug.LogWarning("[ClientSpellcardExecutor] Level 4 spellcard execution initiated, but HandleLevel4Execution is not fully implemented yet.");
        }
        else if (spellLevel >= 2 && spellcardBaseData is SpellcardData spellcardData)
        {
            // Pass offset down to action runner
            HandleLevel2Or3Execution(casterClientId, targetClientId, spellcardData, originPosition, originRotation, sharedRandomOffset);
        }
        else 
        {
            Debug.LogError($"[ClientSpellcardExecutor] Loaded spellcard data from path '{spellcardDataResourcePath}' is not of expected type (SpellcardData or Level4SpellcardData for level {spellLevel}). Actual type: {spellcardBaseData.GetType().Name}"); 
        }
    }

    private void HandleLevel2Or3Execution(ulong casterClientId, ulong targetClientId, SpellcardData spellcardData, Vector3 originPosition, Quaternion originRotation, Vector2 sharedRandomOffset)
    {
        if (_actionRunner == null) 
        {
            Debug.LogError("[ClientSpellcardExecutor] ActionRunner is null in HandleLevel2Or3Execution.");
            return;
        }
        // Pass offset down to action runner
        _actionRunner.RunSpellcardActions(casterClientId, targetClientId, spellcardData.actions, originPosition, originRotation, sharedRandomOffset);
    }

    // private void HandleLevel4Execution(ulong casterClientId, ulong targetClientId, Level4SpellcardData level4Data, Vector3 originPosition, Quaternion originRotation)
    // {
        // TODO:
        // 1. Get the illusion prefab from ClientGameObjectPool using level4Data.IllusionPrefab.GetComponent<PooledObjectInfo>().PrefabID (or store ID in data).
        // 2. Set position/rotation.
        // 3. Get ClientIllusionController component.
        // 4. Initialize ClientIllusionController with level4Data, casterId, targetId, originPosition/area.
        // 5. Activate the illusion GameObject.
        // Debug.Log("HandleLevel4Execution would run here.");
    // }

    private Vector3 CalculateSpellcardOrigin(ulong targetClientId, ulong casterClientId)
    {
        // Basic implementation: Spellcard originates from above the target player's area center.
        if (PlayerDataManager.Instance != null && SpawnAreaManager.Instance != null)
        {
            PlayerData? targetPlayerData = PlayerDataManager.Instance.GetPlayerData(targetClientId);
            if (targetPlayerData.HasValue)
            {
                PlayerRole targetPlayerRole = targetPlayerData.Value.Role;
                Transform spawnAreaCenterTransform = SpawnAreaManager.Instance.GetSpawnCenterForTargetedPlayer(targetPlayerRole); // This returns the CENTER of the OPPONENT's area
                if (spawnAreaCenterTransform != null)
                {
                    // For many spellcards, originating from slightly above the center of the target's area is common.
                    // Or, it could be relative to the caster's view of the opponent's area.
                    // For simplicity, let's use the center for now. Specific spellcards might adjust via their action offsets.
                    return spawnAreaCenterTransform.position; 
                }
                else
                {
                    Debug.LogWarning($"[ClientSpellcardExecutor] CalculateSpellcardOrigin: SpawnAreaManager returned null center for target role {targetPlayerRole}. Defaulting to Vector3.zero.");
                }
            }
             else
            {
                Debug.LogWarning($"[ClientSpellcardExecutor] CalculateSpellcardOrigin: Could not get PlayerData for target client {targetClientId}. Defaulting to Vector3.zero.");
            }
        }
        else
        {
            Debug.LogWarning("[ClientSpellcardExecutor] CalculateSpellcardOrigin: PlayerDataManager or SpawnAreaManager instance is null. Defaulting to Vector3.zero.");
        }
        return Vector3.zero; 
    }
    
    private Quaternion CalculateSpellcardRotation(ulong targetClientId, ulong casterClientId, Vector3 originPosition)
    {
        // Basic implementation: Default rotation (Quaternion.identity).
        // Some spellcards might want to aim towards/away from the caster or target.
        // For now, individual bullet rotations are handled by their spawn parameters (action.angle) and behaviors.
        return Quaternion.identity;
    }
} 