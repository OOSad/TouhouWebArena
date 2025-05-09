using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections; // For IEnumerator

using TouhouWebArena.Spellcards; // Changed from TouhouWebArena.Spellcards.Data

// Forward declare IllusionHealth to satisfy the compiler for the property
// We will create this script next.
// public class IllusionHealth : NetworkBehaviour {}

/// <summary>
/// Client-side component responsible for rendering and animating a Level 4 spellcard's illusion.
/// It receives RPCs from the ServerIllusionOrchestrator to:
/// - Initialize with spellcard data, target player, and health.
/// - Update the illusion's transform (position/rotation).
/// - Execute specific attack patterns, potentially involving movement while attacking.
/// It uses ClientSpellcardActionRunner to handle the actual execution of spellcard actions (bullet spawning).
/// </summary>
public class ClientIllusionView : NetworkBehaviour
{
    private Level4SpellcardData _spellcardData; // Cached spellcard data for attack patterns.
    private ulong _targetPlayerId; // The player ID this illusion is targeting (for action runner).
    private ulong _illusionNetworkId; // This illusion's own NetworkObjectId.

    private ClientSpellcardActionRunner _actionRunner; // Component that executes the spellcard actions.

    // Example: Visual components (uncomment and assign if using)
    // [SerializeField] private SpriteRenderer _illusionSpriteRenderer; 
    // [SerializeField] private Animator _illusionAnimator;

    /// <summary>
    /// Gets the IllusionHealth component attached to this illusion.
    /// Initialized in OnNetworkSpawn/InitializeClientRpc.
    /// </summary>
    public IllusionHealth IllusionHealthComponent { get; private set; }

    private Coroutine _activeMoveCoroutine = null; // Tracks the currently active movement coroutine, if any.

    /// <summary>
    /// Called when the network object is spawned. Ensures this script only runs on clients.
    /// Caches necessary components like ClientSpellcardActionRunner and IllusionHealth.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (IsServer) // Client-only component
        {
            enabled = false;
            return;
        }
        _actionRunner = GetComponent<ClientSpellcardActionRunner>(); 
        if (_actionRunner == null)
        {
            // Fallback: Check children if it's not on the same GameObject
             _actionRunner = GetComponentInChildren<ClientSpellcardActionRunner>();
        }

        if (_actionRunner == null)
        {
            Debug.LogError("[ClientIllusionView] ClientSpellcardActionRunner not found on this GameObject or its children. Spellcard actions will not execute.");
        }
        
        IllusionHealthComponent = GetComponent<IllusionHealth>();
        if (IllusionHealthComponent == null)
        {
            Debug.LogWarning($"[ClientIllusionView] IllusionHealth component not found on {gameObject.name} at OnNetworkSpawn. Will try again in InitializeClientRpc.");
        }
        _illusionNetworkId = NetworkObject.NetworkObjectId; // Store our own ID
    }

    /// <summary>
    /// [ClientRpc] Initializes the illusion on the client with necessary data from the server.
    /// Loads spellcard data, stores the target player ID, and initializes the IllusionHealth component.
    /// </summary>
    /// <param name="spellcardDataPath">Resources path to the Level4SpellcardData.</param>
    /// <param name="targetPlayerIdForIllusion">The NetworkObjectId of the player this illusion targets.</param>
    /// <param name="initialHealth">The starting health of the illusion.</param>
    [ClientRpc]
    public void InitializeClientRpc(string spellcardDataPath, ulong targetPlayerIdForIllusion, float initialHealth)
    {
        if (IsServer) return; // Should only run on clients

        _spellcardData = Resources.Load<Level4SpellcardData>(spellcardDataPath);
        if (_spellcardData == null)
        {
            Debug.LogError($"[ClientIllusionView] Failed to load Level4SpellcardData from path: {spellcardDataPath}");
            // Consider self-destruct or visual error state
            return;
        }
        _targetPlayerId = targetPlayerIdForIllusion; // The player the illusion will target with attacks

        if (IllusionHealthComponent == null) // Try to get it again if it was null during OnNetworkSpawn
        {
             IllusionHealthComponent = GetComponent<IllusionHealth>();
        }

        if (IllusionHealthComponent != null)
        {
            IllusionHealthComponent.Initialize(initialHealth, _targetPlayerId, NetworkManager.Singleton.LocalClientId == _targetPlayerId);
        }
        else
        {
            Debug.LogError($"[ClientIllusionView] IllusionHealth component STILL not found on {gameObject.name} during InitializeClientRpc. Health/damage logic will not work.");
        }
        // Debug.Log($"[ClientIllusionView {_illusionNetworkId}] Initialized for spellcard: {_spellcardData.name}, targeting player: {_targetPlayerId}, initial health: {initialHealth}");
    }

    /// <summary>
    /// [ClientRpc] Updates the illusion's transform (position and rotation) based on server commands.
    /// If an attack-specific movement animation is active (_activeMoveCoroutine != null), this update is ignored
    /// to prevent interference with the smooth movement.
    /// Illusion sprite itself does not rotate based on newRotation; this is primarily for bullet orientation if needed directly from here.
    /// </summary>
    /// <param name="newPosition">The new world position for the illusion.</param>
    /// <param name="newRotation">The new world rotation (primarily for internal logic, not visual sprite rotation).</param>
    [ClientRpc]
    public void UpdateIllusionTransformClientRpc(Vector3 newPosition, Quaternion newRotation)
    {
        if (IsServer) return;
        // Only update transform directly if not currently in an attack-move animation, 
        // as AnimateIllusionMovementAndAttack coroutine will be controlling the transform.
        if (_activeMoveCoroutine == null) 
        {
            transform.position = newPosition;
            // The illusion's visual sprite typically remains upright. `newRotation` might be used by
            // `ClientSpellcardActionRunner` if actions are fired without `initialOrientation` from `ExecuteAttackPatternClientRpc`,
            // but usually `initialOrientation` (aim) is explicitly passed for attacks.
            // transform.rotation = newRotation; 
        }
    }

    /// <summary>
    /// [ClientRpc] Triggers the execution of a specific attack pattern on the client.
    /// Determines if the attack involves movement. If so, starts the AnimateIllusionMovementAndAttack coroutine.
    /// Otherwise, directly calls ClientSpellcardActionRunner to execute the static actions.
    /// </summary>
    /// <param name="patternPoolIndex">Index of the CompositeAttackPattern in the Level4SpellcardData's AttackPool.</param>
    /// <param name="isMovingWithAttack">True if the illusion should move while performing this attack pattern.</param>
    /// <param name="movementStartPos">World position where the attack-specific movement begins.</param>
    /// <param name="movementEndPos">World position where the attack-specific movement ends.</param>
    /// <param name="movementDur">Duration of the attack-specific movement in seconds.</param>
    /// <param name="initialOrientation">If static attack: the fixed orientation for projectiles. If dynamic (moving) attack: the explicit orientation for projectiles, independent of the illusion's visual rotation during movement.</param>
    /// <param name="sharedRandomOffset">A pre-calculated random offset to be applied consistently to all actions in this pattern execution.</param>
    [ClientRpc]
    public void ExecuteAttackPatternClientRpc(int patternPoolIndex, 
                                            bool isMovingWithAttack, 
                                            Vector3 movementStartPos, 
                                            Vector3 movementEndPos, 
                                            float movementDur, 
                                            Quaternion initialOrientation, // If static, this is its fixed orientation. If dynamic, this is aim for bullets.
                                            Vector2 sharedRandomOffset)
    {
        if (IsServer || _spellcardData == null || _actionRunner == null) return;
        if (patternPoolIndex < 0 || patternPoolIndex >= _spellcardData.AttackPool.Count)
        {
            Debug.LogError($"[ClientIllusionView {_illusionNetworkId}] Invalid pattern index: {patternPoolIndex}");
            return;
        }

        CompositeAttackPattern pattern = _spellcardData.AttackPool[patternPoolIndex];
        if (pattern == null)
        {
            Debug.LogError($"[ClientIllusionView {_illusionNetworkId}] Null pattern at index: {patternPoolIndex}");
            return;
        }
        
        if (_activeMoveCoroutine != null) { StopCoroutine(_activeMoveCoroutine); _activeMoveCoroutine = null;}

        if (isMovingWithAttack)
        {
             _activeMoveCoroutine = StartCoroutine(AnimateIllusionMovementAndAttack(pattern, movementStartPos, movementEndPos, movementDur, initialOrientation, sharedRandomOffset));
        }
        else
        {
            _actionRunner.RunSpellcardActions(
                _illusionNetworkId, // casterId (the illusion itself)
                _targetPlayerId,
                pattern.actions,
                transform.position, 
                initialOrientation, 
                sharedRandomOffset
            );
        }
    }
    
    /// <summary>
    /// Coroutine that animates the illusion's movement from a start to an end position over a duration,
    /// while simultaneously triggering its spellcard actions with a dynamic origin (this illusion's transform).
    /// This allows bullets to be spawned from the illusion as it moves.
    /// </summary>
    /// <param name="pattern">The CompositeAttackPattern to execute.</param>
    /// <param name="startPos">World position to start the movement.</param>
    /// <param name="endPos">World position to end the movement.</param>
    /// <param name="duration">Duration of the movement in seconds.</param>
    /// <param name="attackOrientationForBullets">The explicit orientation for projectiles, used by ClientSpellcardActionRunner instead of the moving illusion's visual transform.rotation.</param>
    /// <param name="sharedRandomOffset">A shared random offset for all actions in this pattern.</param>
    private IEnumerator AnimateIllusionMovementAndAttack(CompositeAttackPattern pattern, Vector3 startPos, Vector3 endPos, float duration, Quaternion attackOrientationForBullets, Vector2 sharedRandomOffset)
    {
        transform.position = startPos; // Snap to start position before lerping and attacking.
        
        // Trigger actions that will use this.transform as a dynamic origin for spawning bullets.
        // The attackOrientationForBullets ensures bullets aim correctly even if the illusion sprite itself doesn't rotate for aiming.
        _actionRunner.RunSpellcardActionsDynamicOrigin(
            _illusionNetworkId, // casterId (the illusion itself)
            _targetPlayerId,
            pattern.actions,
            this.transform, 
            attackOrientationForBullets, 
            sharedRandomOffset
        );

        float elapsedTime = 0f;
        if (duration > 0) // Only Lerp if duration is positive
        {
            while (elapsedTime < duration)
            {
                transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        transform.position = endPos; // Ensure it ends exactly at the target
        _activeMoveCoroutine = null; // Mark movement as complete and allow normal transform updates.
    }

    // Called by IllusionHealth when it dies on the client
    public void NotifyServerOfDespawn()
    {
        if (IsOwner) // This check might be tricky; ownership of illusion is server.
                     // The RPC call will be on ServerIllusionOrchestrator, invoked by the targeted client.
        {
            // We need to find the ServerIllusionOrchestrator to call an RPC on it.
            // This is problematic because ClientIllusionView doesn't know about ServerIllusionOrchestrator directly.
            // The RPC should be on IllusionHealth itself, calling up to the server.
            // Or, IllusionHealth tells its ServerIllusionOrchestrator via an RPC.
        }
        // The actual ServerRpc call will be made from IllusionHealth.cs
    }

    /// <summary>
    /// Called when the network object is despawned. Stops any active movement coroutines.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (_activeMoveCoroutine != null) { StopCoroutine(_activeMoveCoroutine); _activeMoveCoroutine = null;}
        base.OnNetworkDespawn();
    }
}

// Dummy definitions (ensure these match ServerIllusionOrchestrator's and your actual data)
// public class Level4SpellcardData : ScriptableObject
// {
//     public List<CompositeAttackPattern> attackPatterns;
//     // ... other fields
// }

// [System.Serializable]
// public class CompositeAttackPattern
// {
//     public string patternName;
//     public List<SpellcardAction> actions;
//     // ... other fields
// }

// [System.Serializable]
// public class SpellcardAction { /* ... */ }

// Assume ClientSpellcardActionRunner exists and has a method with this signature:
// public class ClientSpellcardActionRunner : MonoBehaviour
// {
//     public void RunSpellcardActions(
//         List<SpellcardAction> actions,
//         Vector3 originPosition,
//         Quaternion originRotation,
//         Vector2 sharedRandomOffset,
//         ulong casterId,
//         ulong targetId)
//     {
//         // Implementation of action sequence execution
//     }
// } 