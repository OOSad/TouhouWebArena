using UnityEngine;
using TouhouWebArena.Spellcards;
using Unity.Netcode; 

namespace TouhouWebArena.Spellcards.Behaviors
{
    /// <summary>
    /// [Client Only] Static helper class to configure the behavior components 
    /// of locally spawned spellcard bullets based on SpellcardAction data.
    /// </summary>
    public static class ClientBulletConfigurer
    {
        public static void ConfigureBullet(GameObject bulletInstance, SpellcardAction action, ulong casterClientId, ulong targetClientId, int bulletIndex)
        {
            if (bulletInstance == null || action == null)
            {
                Debug.LogError("[ClientBulletConfigurer] ConfigureBullet called with null instance or action.");
                return;
            }

            ClientProjectileLifetime lifetime = bulletInstance.GetComponent<ClientProjectileLifetime>();
            if (lifetime != null && action.lifetime > 0)
            {
                lifetime.Initialize(action.lifetime);
            }
            else if (lifetime == null)
            {
                Debug.LogWarning($"[ClientBulletConfigurer] Bullet prefab '{bulletInstance.name}' is missing ClientProjectileLifetime. Lifetime will not be managed by configurer.");
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

        private static void LogMissingBehaviorError(string bulletName, string scriptName)
        {
            Debug.LogError($"[ClientBulletConfigurer] Prefab '{bulletName}' is missing the required script '{scriptName}'. This behavior cannot be applied.");
        }
    }
} 