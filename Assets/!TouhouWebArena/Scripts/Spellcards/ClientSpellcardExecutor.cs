using UnityEngine;
using Unity.Netcode;
using TouhouWebArena;
using TouhouWebArena.Spellcards;
using System.Collections.Generic; // For List in RunSpellcardActions
using System.Linq; // Required for Linq operations if used

/// <summary>
/// [Client Only] Client-side singleton responsible for initiating the local execution 
/// of spellcards based on commands received from the server via SpellcardNetworkHandler.
/// It loads the spellcard data and delegates the action sequence execution to ClientSpellcardActionRunner.
/// </summary>
public class ClientSpellcardExecutor : NetworkBehaviour
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

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy(); // Call the base class's OnDestroy method
    }

    /// <summary>
    /// Called by SpellcardNetworkHandler when an ExecuteSpellcardClientRpc is received.
    /// Starts the process of executing the spellcard locally.
    /// </summary>
    public void StartLocalSpellcardExecution(ulong casterClientId, ulong targetClientId, string spellcardDataResourcePath, int spellLevel, Vector2 sharedRandomOffset)
    {
        // Debug.Log($\"[ClientSpellcardExecutor] Attempting to start local execution for Lv{spellLevel} spellcard: {spellcardDataResourcePath}. Caster: {casterClientId}, Target: {targetClientId}, Offset: {sharedRandomOffset}\");

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

    // --- Client-Side Clearing RPC --- 
    /// <summary>
    /// [ClientRpc] Executes the spellcard activation's screen-clearing effect locally.
    /// This is triggered by the server after a spellcard activation.
    /// It clears hostile projectiles within the specified radius based on their layer,
    /// and clears fairies/spirits belonging to the caster based on OwningPlayerRole.
    /// </summary>
    /// <param name="casterPosition">World position where the clear originates (caster's position).</param>
    /// <param name="clearRadius">Radius of the clear effect, determined by the spell level on the server.</param>
    /// <param name="casterRole">The PlayerRole of the player who activated the spellcard.</param>
    [ClientRpc]
    public void TriggerLocalClearEffectClientRpc(Vector3 casterPosition, float clearRadius, PlayerRole casterRole)
    {
        Debug.Log($"[ClientSpellcardExecutor] Received TriggerLocalClearEffectClientRpc. Pos: {casterPosition}, Radius: {clearRadius}, Caster: {casterRole}");
        Collider2D[] colliders = Physics2D.OverlapCircleAll(casterPosition, clearRadius);

        // Resolve Caster Role to ClientId
        ulong casterPlayerClientId = ulong.MaxValue;
        if (PlayerDataManager.Instance != null && (casterRole == PlayerRole.Player1 || casterRole == PlayerRole.Player2))
        {
            // Iterate through the player list to find the client ID for the caster role
            // Need to access the player list somehow. Assuming PlayerDataManager has a way, e.g., a public property or method returning the list.
            // Let's try accessing the NetworkList directly (might need adjustment based on actual implementation)
            // NetworkList<PlayerData> playerList = PlayerDataManager.Instance.players; // Example: Assuming direct access (unlikely)
            // Better: Assume PlayerDataManager has a method like GetPlayerDataByRole
            PlayerData? casterData = PlayerDataManager.Instance.GetPlayerDataByRole(casterRole);
            if (casterData.HasValue)
            {
                casterPlayerClientId = casterData.Value.ClientId;
            }
        }
        
        if (casterPlayerClientId == ulong.MaxValue && (casterRole == PlayerRole.Player1 || casterRole == PlayerRole.Player2))
        {
             Debug.LogWarning($"[ClientSpellcardExecutor] Could not resolve ClientId for casterRole {casterRole}. Cannot reliably clear opponent extra attacks.");
        }

        foreach (Collider2D col in colliders)
        {
            // Clear Enemy Projectiles (Layer Check - Clears regardless of owner)
            if (col.gameObject.layer == LayerMask.NameToLayer("EnemyProjectiles"))
            {
                if (col.TryGetComponent(out ClientProjectileLifetime projectileLifetime))
                {
                    projectileLifetime.ForceReturnToPool();
                }
                continue;
            }

            // Clear Caster's Own Fairies/Spirits (Component & Ownership Check)
            if (col.TryGetComponent(out ClientFairyHealth fairyHealth))
            {
                if (fairyHealth.OwningPlayerRole == casterRole)
                {
                    fairyHealth.ForceReturnToPool();
                }
                continue;
            }

            if (col.TryGetComponent(out ClientSpiritController spiritController))
            {
                if (spiritController.OwningPlayerRole == casterRole)
                {
                    if (col.TryGetComponent(out ClientSpiritHealth spiritHealth)) {
                        spiritHealth.ForceReturnToPool();
                    }
                }
                continue;
            }

            // --- NEW: Clear Opponent's EXTRA Attacks ---
            // Clear Opponent's Reimu Orb
            if (col.TryGetComponent(out ReimuExtraAttackOrb_Client reimuOrb))
            {
                // Debug.Log($"[ClearEffect] Found Reimu Orb: {col.gameObject.name}, AttackerID: {reimuOrb.AttackerClientId}, CasterID: {casterPlayerClientId}"); // DEBUG
                // Compare client IDs
                if (casterPlayerClientId != ulong.MaxValue && reimuOrb.AttackerClientId != casterPlayerClientId && reimuOrb.AttackerClientId != 0)
                {
                    // Debug.Log($"-- Orb belongs to opponent, attempting clear."); // DEBUG
                    reimuOrb.ForceReturnToPoolByClear(); // MODIFIED: Call new method
                    // Debug.Log($"--- Called ForceReturnToPoolByClear on Reimu Orb."); // DEBUG
                }
                continue;
            }

            // Clear Opponent's Marisa Laser
            if (col.TryGetComponent(out MarisaExtraAttackLaser_Client marisaLaser))
            {
                // Debug.Log($"[ClearEffect] Found Marisa Laser: {col.gameObject.name}, AttackerID: {marisaLaser.AttackerClientId}, CasterID: {casterPlayerClientId}"); // DEBUG
                // Compare client IDs
                if (casterPlayerClientId != ulong.MaxValue && marisaLaser.AttackerClientId != casterPlayerClientId && marisaLaser.AttackerClientId != 0)
                {
                    // Debug.Log($"-- Laser belongs to opponent, attempting clear."); // DEBUG
                    marisaLaser.ForceReturnToPoolByClear(); // MODIFIED: Call new method
                    // Debug.Log($"--- Called ForceReturnToPoolByClear on Marisa Laser."); // DEBUG
                }
            }
            // --- END NEW ---
        }
        // TODO: Add visual effect instantiation here?
    }
} 