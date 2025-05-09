using UnityEngine;
using TouhouWebArena.Spellcards;
using Unity.Netcode; 

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// [Client Only] Static helper class responsible for setting up client-side spellcard bullets after they are spawned.
    /// It initializes the bullet's lifetime, deactivates all potential movement behaviors attached to the prefab,
    /// and then activates and initializes the specific movement behavior defined in the `SpellcardAction` data.
    /// This ensures that bullets behave as defined by the spellcard design.
    /// </summary>
    public static class ClientBulletConfigurer
    {
        /// <summary>
        /// Configures a newly spawned spellcard bullet instance based on a `SpellcardAction`.
        /// This involves setting its lifetime, disabling all pre-attached movement scripts,
        /// then finding, initializing, and enabling the correct client-side movement behavior script.
        /// </summary>
        /// <param name="bulletInstance">The GameObject instance of the spawned bullet.</param>
        /// <param name="action">The `SpellcardAction` data defining how this bullet should behave.</param>
        /// <param name="casterClientId">The NetworkObjectId of the entity that cast the spellcard.</param>
        /// <param name="targetClientId">The NetworkObjectId of the target player (used for homing behaviors).</param>
        /// <param name="bulletIndex">The index of this bullet within its spawn sequence (0-based), used for some behaviors like speed increment.</param>
        public static void ConfigureBullet(GameObject bulletInstance, SpellcardAction action, ulong casterClientId, ulong targetClientId, int bulletIndex)
        {
            if (bulletInstance == null || action == null)
            {
                Debug.LogError("[ClientBulletConfigurer] ConfigureBullet called with null instance or action.");
                return;
            }

            Debug.Log($"[ClientBulletConfigurer] Configuring bullet '{bulletInstance.name}'. Action Lifetime from data: {action.lifetime}");

            ClientProjectileLifetime lifetimeComponent = bulletInstance.GetComponent<ClientProjectileLifetime>();
            if (lifetimeComponent != null)
            {
                if (action.lifetime > 0)
                {
                    lifetimeComponent.Initialize(action.lifetime);
                }
                else
                {
                    // Action's lifetime is not set or is invalid, use a default.
                    const float defaultBulletLifetime = 7.0f; // Example: 7 seconds, adjust as needed
                    lifetimeComponent.Initialize(defaultBulletLifetime);
                    Debug.LogWarning($"[ClientBulletConfigurer] Bullet '{bulletInstance.name}' used default lifetime ({defaultBulletLifetime}s) because action.lifetime was {action.lifetime}. Ensure lifetime is set in SpellcardAction if a specific duration is needed.");
                }
            }
            else
            {
                Debug.LogWarning($"[ClientBulletConfigurer] Bullet prefab '{bulletInstance.name}' is missing ClientProjectileLifetime. Lifetime will not be managed.");
            }

            DeactivateAllMovementBehaviors(bulletInstance);

            Debug.Log($"[ClientBulletConfigurer] Configuring bullet '{bulletInstance.name}'. Behavior from action: {action.behavior} (Int value: {(int)action.behavior})");

            switch (action.behavior)
            {
                case BehaviorType.Linear:
                    ClientLinearMovement linear = bulletInstance.GetComponent<ClientLinearMovement>();
                    if (linear != null)
                    {
                        linear.Initialize(
                            action.speed, // baseSpeed
                            action.speedIncrementPerBullet,
                            bulletIndex,
                            action.useInitialSpeed,
                            action.initialSpeed,
                            action.speedTransitionDuration
                        );
                        linear.enabled = true;
                    }
                    else { LogMissingBehaviorError(bulletInstance.name, "ClientLinearMovement"); }
                    break;

                case BehaviorType.DelayedHoming:
                    Debug.Log("[ClientBulletConfigurer] --- Entered DelayedHoming case ---"); 
                    ClientDelayedHoming delayedHoming = bulletInstance.GetComponent<ClientDelayedHoming>();
                    if (delayedHoming != null)
                    {
                        Transform delayedTargetTransform = FindPlayerTransform(targetClientId);
                        Vector3 targetPosition = delayedTargetTransform != null ? delayedTargetTransform.position : bulletInstance.transform.position + bulletInstance.transform.up * 10f; // Fallback position
                        delayedHoming.Initialize(action.speed, action.homingSpeed, action.homingDelay, targetPosition);
                        delayedHoming.enabled = true;
                    }
                    else { LogMissingBehaviorError(bulletInstance.name, "ClientDelayedHoming"); }
                    break;

                case BehaviorType.Spiral:
                    ClientSpiralMovement spiral = bulletInstance.GetComponent<ClientSpiralMovement>();
                    if (spiral != null)
                    {
                        spiral.Initialize(action.speed, action.tangentialSpeed, bulletInstance.transform.position);
                        spiral.enabled = true;
                    }
                    else { LogMissingBehaviorError(bulletInstance.name, "ClientSpiralMovement"); }
                    break;

                case BehaviorType.DoubleHoming:
                    ClientDoubleHoming doubleHoming = bulletInstance.GetComponent<ClientDoubleHoming>();
                    if (doubleHoming != null)
                    {
                        Transform doubleHomingTarget = FindPlayerTransform(targetClientId);
                        doubleHoming.Initialize(action.speed, action.homingSpeed, action.homingDelay, 
                                                action.firstHomingDuration, action.secondHomingDelay, 
                                                action.secondHomingLookAheadDistance, doubleHomingTarget);
                        // ClientDoubleHoming handles its own enabling/disabling based on target validity in Initialize
                    }
                    else { LogMissingBehaviorError(bulletInstance.name, "ClientDoubleHoming"); }
                    break;

                case BehaviorType.DelayedRandomTurn:
                    ClientDelayedRandomTurn delayedRandomTurn = bulletInstance.GetComponent<ClientDelayedRandomTurn>();
                    if (delayedRandomTurn != null)
                    {
                        delayedRandomTurn.Initialize(action.speed, action.homingDelay, action.minTurnSpeed, 
                                                   action.maxTurnSpeed, action.spreadAngle);
                        delayedRandomTurn.enabled = true;
                    }
                    else { LogMissingBehaviorError(bulletInstance.name, "ClientDelayedRandomTurn"); }
                    break;

                case BehaviorType.Homing:
                    ClientHomingMovement homing = bulletInstance.GetComponent<ClientHomingMovement>();
                    if (homing != null)
                    {
                        Transform homingTargetTransform = FindPlayerTransform(targetClientId);
                        if (homingTargetTransform != null)
                        {
                            homing.Initialize(action.speed, action.homingSpeed, homingTargetTransform.position);
                            // ClientHomingMovement handles its own enabling/disabling based on target validity in Initialize
                        }
                        else
                        {
                            Debug.LogWarning($"[ClientBulletConfigurer] Homing behavior for '{bulletInstance.name}' (ID: {targetClientId}) target not found. Homing script will not be enabled.");
                            // Ensure it's disabled if target transform is null, though Initialize should also handle this.
                            homing.enabled = false; 
                        }
                    }
                    else { LogMissingBehaviorError(bulletInstance.name, "ClientHomingMovement"); }
                    break;

                default:
                    Debug.LogWarning($"[ClientBulletConfigurer] BehaviorType '{action.behavior}' not implemented or prefab '{bulletInstance.name}' missing default script. Attempting to fall back to Linear if present.");
                    ClientLinearMovement defaultLinear = bulletInstance.GetComponent<ClientLinearMovement>();
                    if (defaultLinear != null)
                    {
                        defaultLinear.Initialize(
                            action.speed, // baseSpeed
                            action.speedIncrementPerBullet,
                            bulletIndex,
                            action.useInitialSpeed,
                            action.initialSpeed,
                            action.speedTransitionDuration
                        );
                        defaultLinear.enabled = true;
                    }
                    else { Debug.LogError($"[ClientBulletConfigurer] FATAL: BehaviorType '{action.behavior}' not handled AND Linear fallback '{bulletInstance.name}' is missing ClientLinearMovement."); }
                    break;
            }
        }

        /// <summary>
        /// Helper method to explicitly disable all known client-side movement behavior components on a bullet instance.
        /// This is called before enabling the specific behavior dictated by the `SpellcardAction` to ensure a clean state.
        /// </summary>
        /// <param name="bulletInstance">The bullet GameObject to process.</param>
        private static void DeactivateAllMovementBehaviors(GameObject bulletInstance)
        {
            var linear = bulletInstance.GetComponent<ClientLinearMovement>();
            if (linear != null) linear.enabled = false;

            var delayedHoming = bulletInstance.GetComponent<ClientDelayedHoming>();
            if (delayedHoming != null) delayedHoming.enabled = false;

            var spiralMovement = bulletInstance.GetComponent<ClientSpiralMovement>();
            if (spiralMovement != null) spiralMovement.enabled = false;

            var doubleHomingMovement = bulletInstance.GetComponent<ClientDoubleHoming>();
            if (doubleHomingMovement != null) doubleHomingMovement.enabled = false;

            var delayedRandomTurnMovement = bulletInstance.GetComponent<ClientDelayedRandomTurn>();
            if (delayedRandomTurnMovement != null) delayedRandomTurnMovement.enabled = false;

            var homingMovement = bulletInstance.GetComponent<ClientHomingMovement>();
            if (homingMovement != null) homingMovement.enabled = false;
        }

        /// <summary>
        /// Finds the Transform of a connected player's PlayerObject based on their ClientId.
        /// Used by homing behaviors to acquire a target.
        /// </summary>
        /// <param name="clientId">The NetworkObjectId of the client whose PlayerObject transform is needed.</param>
        /// <returns>The Transform of the player's PlayerObject, or null if not found.</returns>
        private static Transform FindPlayerTransform(ulong clientId)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) &&
                client.PlayerObject != null)
            {
                return client.PlayerObject.transform;
            }
            return null;
        }

        /// <summary>
        /// Logs a standardized error message when a required behavior script is missing from a bullet prefab.
        /// </summary>
        /// <param name="bulletName">Name of the bullet prefab.</param>
        /// <param name="scriptName">Name of the missing script.</param>
        private static void LogMissingBehaviorError(string bulletName, string scriptName)
        {
            Debug.LogError($"[ClientBulletConfigurer] Prefab '{bulletName}' is missing the required script '{scriptName}'. This behavior cannot be applied.");
        }
    }
} 