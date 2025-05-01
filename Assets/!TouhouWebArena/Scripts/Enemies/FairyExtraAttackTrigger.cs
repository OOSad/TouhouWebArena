using UnityEngine;
using TouhouWebArena; // Required for PlayerRole and potentially ExtraAttackManager namespace
using Unity.Netcode; // Required for NetworkBehaviour and IsServer check

/// <summary>
/// Handles triggering an extra attack via the ExtraAttackManager 
/// when the associated Fairy is destroyed, if designated as a trigger.
/// Requires initialization with trigger status and owner role.
/// </summary>
public class FairyExtraAttackTrigger : MonoBehaviour
{
    private bool isExtraAttackTrigger = false;
    private PlayerRole ownerRole = PlayerRole.None;

    /// <summary>
    /// Initializes the component with necessary data.
    /// </summary>
    /// <param name="isTrigger">Whether this fairy triggers an extra attack on death.</param>
    /// <param name="owner">The PlayerRole owning this fairy.</param>
    public void Initialize(bool isTrigger, PlayerRole owner)
    {
        this.isExtraAttackTrigger = isTrigger;
        this.ownerRole = owner;
    }

    /// <summary>
    /// [Server Only] Checks if this fairy is a trigger and, if so, calls the ExtraAttackManager.
    /// Requires ExtraAttackManager and PlayerDataManager singletons to be available.
    /// Retrieves attacker data using PlayerDataManager, determines the opponent, and calls ExtraAttackManager.TriggerExtraAttackInternal.
    /// Should be called from the FairyController's Die method (which should also ensure it's called only on the server).
    /// </summary>
    /// <param name="killerRole">The role of the player who killed the fairy (used for logging).</param>
    public void TriggerExtraAttackIfApplicable(PlayerRole killerRole)
    {
        // Only the server should trigger extra attacks
        if (!NetworkManager.Singleton.IsServer) return; 

        if (isExtraAttackTrigger)
        {
            if (ExtraAttackManager.Instance == null)
            {
                Debug.LogError("[FairyExtraAttackTrigger] ExtraAttackManager instance not found! Cannot trigger attack.", this);
                return;
            }
            if (PlayerDataManager.Instance == null)
            {
                 Debug.LogError("[FairyExtraAttackTrigger] PlayerDataManager instance not found! Cannot retrieve player data.", this);
                 return;
            }

            // 1. Get Attacker PlayerData using the ownerRole
            PlayerData? attackerData = PlayerDataManager.Instance.GetPlayerDataByRole(ownerRole);
            if (attackerData == null)
            {
                Debug.LogError($"[FairyExtraAttackTrigger] Could not find PlayerData for owner role {ownerRole}.", this);
                return;
            }

            // 2. Determine Opponent Role
            PlayerRole opponentRole = PlayerRole.None;
            if (ownerRole == PlayerRole.Player1)
            {
                opponentRole = PlayerRole.Player2;
            }
            else if (ownerRole == PlayerRole.Player2)
            {
                opponentRole = PlayerRole.Player1;
            }
            
            if (opponentRole == PlayerRole.None)
            {
                 Debug.LogError($"[FairyExtraAttackTrigger] Invalid owner role ({ownerRole}) encountered. Cannot determine opponent.", this);
                 return;
            }

            // 3. Call the correct ExtraAttackManager method
            Debug.Log($"Fairy {gameObject.name} triggering Extra Attack by owner {ownerRole} targeting opponent {opponentRole} (killed by {killerRole})");
            // Assuming TriggerExtraAttackInternal takes PlayerData attackerData, PlayerRole opponentRole
            ExtraAttackManager.Instance.TriggerExtraAttackInternal(attackerData.Value, opponentRole);
        }
    }
} 