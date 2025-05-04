using UnityEngine;
using Unity.Netcode;
using System;
using TouhouWebArena; // For PlayerRole, DelayedActionProcessor, StageSmallBulletSpawner

/// <summary>
/// [Server Only] Handles the chain reaction logic initiated when a <see cref="FairyController"/> dies.
/// Responsible for spawning a <see cref="DelayedActionProcessor"/> to handle subsequent fairy destruction
/// and triggering bullet spawns on the opponent's side via <see cref="StageSmallBulletSpawner"/>.
/// Requires serialized references to the <see cref="delayedActionProcessorPrefab"/> and <see cref="deathShockwavePrefab"/>,
/// and the <see cref="chainReactionDelay"/> value.
/// Called by <see cref="FairyController.HandleDeath"/>.
/// </summary>
[RequireComponent(typeof(FairyController))] // Requires access to FairyController context if needed
public class FairyChainReactionHandler : NetworkBehaviour
{
    [Header("Chain Reaction Configuration")]
    [SerializeField]
    [Tooltip("Delay in seconds before the next fairy in line is destroyed.")]
    private float chainReactionDelay = 0.08f;

    [SerializeField]
    [Tooltip("The prefab for the DelayedActionProcessor utility.")]
    private GameObject delayedActionProcessorPrefab;

    [SerializeField]
    [Tooltip("The shockwave effect prefab triggered on death (passed to the DelayedActionProcessor).")]
    private GameObject deathShockwavePrefab;

    // References (could potentially get ownerRole from FairyController if needed)
    // private FairyController fairyController;

    // void Awake()
    // {
    //     fairyController = GetComponent<FairyController>();
    // }

    /// <summary>
    /// [Server Only] Processes the chain reaction effects when called by <see cref="FairyController.HandleDeath"/>.
    /// Instantiates a <see cref="DelayedActionProcessor"/> prefab to handle the delayed kill/shockwave.
    /// If the kill was initiated by a player (<paramref name="killerRole"/> != None), 
    /// triggers a bullet spawn for the opponent via <see cref="StageSmallBulletSpawner"/>.
    /// </summary>
    /// <param name="killerRole">The role of the player who initiated the kill (or None).</param>
    /// <param name="lineId">The ID of the line the dying fairy belonged to.</param>
    /// <param name="indexInLine">The index of the dying fairy within the line.</param>
    /// <param name="ownerRole">The role of the player who owned the dying fairy.</param>
    public void ProcessChainReaction(PlayerRole killerRole, System.Guid lineId, int indexInLine, PlayerRole ownerRole)
    {
        if (!IsServer) return;

        // Check if this fairy was part of a line (redundant check? Die already does this)
        // if (lineId == System.Guid.Empty || indexInLine < 0) return;

        // --- Create DelayedActionProcessor --- 
        if (delayedActionProcessorPrefab != null)
        {
            // Use the Fairy's position for the processor spawn
            GameObject processorGO = Instantiate(delayedActionProcessorPrefab, transform.position, Quaternion.identity);
            DelayedActionProcessor processor = processorGO.GetComponent<DelayedActionProcessor>();
            if (processor != null)
            {
                processor.InitializeAndRun(
                    transform.position,
                    killerRole,
                    chainReactionDelay,
                    null,
                    lineId,
                    indexInLine
                );
            }
            else
            {
                Debug.LogError($"[FairyChainReactionHandler] DelayedActionProcessor prefab is missing the DelayedActionProcessor script!", delayedActionProcessorPrefab);
                Destroy(processorGO);
            }
        }
        else
        {
            Debug.LogError("[FairyChainReactionHandler] DelayedActionProcessor prefab is not assigned! Cannot run delayed actions.", this);
        }
        // --- End DelayedActionProcessor Creation ---

        // Effects below should only happen if killed BY A PLAYER during a chain reaction scenario
        if (killerRole != PlayerRole.None)
        {
            // --- Spawn Regular Bullet on Opponent Side --- 
            if (ownerRole != PlayerRole.None) // Check owner is valid
            {
                if (StageSmallBulletSpawner.Instance != null)
                {
                    StageSmallBulletSpawner.Instance.SpawnBulletForOpponent(ownerRole);
                }
                else
                {
                    Debug.LogWarning("[FairyChainReactionHandler] StageSmallBulletSpawner instance not found.", this);
                }
            }
        } // End if (killerRole != PlayerRole.None)
    }
} 