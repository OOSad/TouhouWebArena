using UnityEngine;
using Unity.Netcode;
using TouhouWebArena;
using TouhouWebArena.Spellcards; // For SpellcardAction, BehaviorType
using TouhouWebArena.Spellcards.Behaviors; // For specific behavior components like NetworkBulletLifetime, LinearMovement etc.

/// <summary>
/// **[Server Only]** Static helper class for configuring the behavior components
/// of newly spawned spellcard projectiles based on <see cref="SpellcardAction"/> data.
/// </summary>
public static class ServerBulletConfigurer
{
    /// <summary>
    /// **[Server Only]** Helper method to configure a newly spawned spellcard projectile's behavior components
    /// (e.g., <see cref="LinearMovement"/>, <see cref="DelayedHoming"/>) based on the <see cref="SpellcardAction"/> data.
    /// Also configures the <see cref="NetworkBulletLifetime"/> boundary check.
    /// </summary>
    /// <param name="bulletInstance">The instantiated bullet GameObject.</param>
    /// <param name="action">The SpellcardAction defining the behavior and other non-speed parameters.</param>
    /// <param name="currentSpeed">The calculated speed for this specific bullet.</param>
    /// <param name="opponentId">The ClientId of the opponent player (target for homing).</param>
    /// <param name="capturedOpponentPosition">The captured position of the opponent (initial target for homing).</param>
    /// <param name="isTargetOnPositiveSide">Whether the target is on the right side (for boundary checks).</param>
    /// <param name="spawnOrigin">The origin position used for calculating relative positions or as a center (e.g., for Spiral).</param>
    public static void ConfigureBulletBehavior(GameObject bulletInstance, TouhouWebArena.Spellcards.SpellcardAction action, float currentSpeed, ulong opponentId, Vector3 capturedOpponentPosition, bool isTargetOnPositiveSide, Vector3 spawnOrigin)
    {
        // Get DoubleHoming component
        var doubleHoming = bulletInstance.GetComponent<DoubleHoming>(); 
        var lifetime = bulletInstance.GetComponent<NetworkBulletLifetime>();
        var spiral = bulletInstance.GetComponent<SpiralMovement>(); // Get SpiralMovement
        var delayedRandomTurn = bulletInstance.GetComponent<DelayedRandomTurn>(); // Get new component
        var homing = bulletInstance.GetComponent<Homing>(); // Get new component

        // Disable all potential behaviors first to ensure clean state
        var linear = bulletInstance.GetComponent<LinearMovement>();
        var delayedHoming = bulletInstance.GetComponent<DelayedHoming>();
        if (linear) linear.enabled = false;
        if (delayedHoming) delayedHoming.enabled = false;
        if (homing) homing.enabled = false; // Disable new one initially
        if (doubleHoming) doubleHoming.enabled = false; 
        if (spiral) spiral.enabled = false; // Disable spiral initially
        if (delayedRandomTurn) delayedRandomTurn.enabled = false; // Disable new one initially

        // Configure Lifetime Boundary Check and Target Role
        if (lifetime != null) {
            lifetime.keepOnPositiveSide = isTargetOnPositiveSide;
            if (action.lifetime > 0f)
            {
                lifetime.maxLifetime = action.lifetime;
            }
            
            // --- Assign Target Role --- 
            if (PlayerDataManager.Instance != null)
            {
                PlayerData? opponentData = PlayerDataManager.Instance.GetPlayerData(opponentId);
                lifetime.TargetPlayerRole.Value = opponentData.HasValue ? opponentData.Value.Role : PlayerRole.None;
            }
            else
            {
                // Use string interpolation $ for cleaner logging
                Debug.LogError($"[ServerBulletConfigurer.ConfigureBulletBehavior] PlayerDataManager missing! Cannot set bullet TargetPlayerRole.");
                lifetime.TargetPlayerRole.Value = PlayerRole.None; // Assign default
            }
            // -------------------------

        } else {
             // Use string interpolation
            Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' missing NetworkBulletLifetime component.");
        }

        // Enable and initialize the specified movement behavior
        switch (action.behavior)
        {
            case BehaviorType.Linear:
                if (linear != null) { linear.enabled = true; linear.Initialize(currentSpeed); }
                 // Use string interpolation
                else { Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to Linear but missing LinearMovement component."); }
                break;
            case BehaviorType.Homing:
                if (homing != null) 
                {
                    if (opponentId != ulong.MaxValue)
                    {
                        homing.enabled = true;
                        // Initialize with homingSpeed for both move speed and turn speed for simplicity?
                        // Or use currentSpeed for moveSpeed and action.homingSpeed for turnSpeed?
                        // Let's try using action.homingSpeed for turnSpeed and currentSpeed for moveSpeed.
                        homing.Initialize(currentSpeed, action.homingSpeed, opponentId, capturedOpponentPosition);
                    }
                    else 
                    {
                        // Fallback to linear if no opponent
                         // Use string interpolation
                        Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to Homing but no opponent found. Falling back to Linear.");
                        if (linear != null) { linear.enabled = true; linear.Initialize(currentSpeed); }
                    }
                }
                  // Use string interpolation
                 else { Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to Homing but missing Homing component."); }
                break;
            case BehaviorType.DelayedHoming:
                if (delayedHoming != null)
                {
                    if (opponentId != ulong.MaxValue) // Ensure opponent exists
                    {
                         delayedHoming.enabled = true;
                         // Use currentSpeed for initial linear phase
                         delayedHoming.Initialize(currentSpeed, action.homingSpeed, action.homingDelay, opponentId, capturedOpponentPosition);
                    } else {
                        // Fallback to linear if no opponent found (should be rare in 2-player game)
                         // Use string interpolation
                        Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DelayedHoming but no opponent found. Falling back to Linear.");
                        if (linear != null) { linear.enabled = true; linear.Initialize(currentSpeed); } // Fallback with currentSpeed
                    }
                }
                  // Use string interpolation
                 else { Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DelayedHoming but missing DelayedHoming component."); }
                break;
            // --- ADDED BACK: DoubleHoming Case ---    
            case BehaviorType.DoubleHoming:
                if (doubleHoming != null)
                {
                    // Get opponent PlayerMovement component
                    PlayerMovement opponentMovement = null;
                    if (opponentId != ulong.MaxValue && NetworkManager.Singleton.ConnectedClients.TryGetValue(opponentId, out var opponentClient) && opponentClient.PlayerObject != null)
                    {
                        opponentMovement = opponentClient.PlayerObject.GetComponent<PlayerMovement>();
                    }
                    
                    if (opponentMovement != null)
                    {
                        doubleHoming.enabled = true;
                        // Initialize using currentSpeed, homingSpeed, delays, first duration, look-ahead distance, and opponent reference
                        doubleHoming.Initialize(
                            currentSpeed, 
                            action.homingSpeed, 
                            action.homingDelay, 
                            action.secondHomingDelay, 
                            action.firstHomingDuration, // Pass duration 1
                            action.secondHomingLookAheadDistance, // Pass look ahead distance
                            opponentMovement
                        );
                    }
                    else
                    {
                        // Fallback to linear if no opponent or opponent component found
                         // Use string interpolation
                        Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DoubleHoming but couldn't find opponent PlayerMovement. Falling back to Linear.");
                        if (linear != null) { linear.enabled = true; linear.Initialize(currentSpeed); }
                    }
                }
                 // Use string interpolation
                else { Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DoubleHoming but missing DoubleHoming component."); }
                break;
            // --- ADDED BACK: Spiral Case --- 
            case BehaviorType.Spiral:
                if (spiral != null)
                {
                    spiral.enabled = true;
                    // Pass spawnOrigin as the spawnCenter
                    spiral.Initialize(currentSpeed, action.tangentialSpeed, spawnOrigin); 
                }
                else
                {
                     // Use string interpolation
                    Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to Spiral but missing SpiralMovement component.");
                }
                break;
            case BehaviorType.DelayedRandomTurn:
                if (delayedRandomTurn != null) 
                {
                    delayedRandomTurn.enabled = true;
                    delayedRandomTurn.Initialize(
                        currentSpeed, 
                        action.homingDelay, // Reuse homing delay for turn delay
                        action.minTurnSpeed, 
                        action.maxTurnSpeed, 
                        action.spreadAngle
                    );
                }
                  // Use string interpolation
                 else { Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' set to DelayedRandomTurn but missing DelayedRandomTurn component."); }
                break;
            // TODO: Add other cases like Homing if implemented
            default:
                  // Use string interpolation
                 Debug.LogWarning($"[ServerBulletConfigurer.ConfigureBulletBehavior] Spellcard bullet '{bulletInstance.name}' has unhandled BehaviorType: {action.behavior}. Defaulting to Linear if possible.");
                 if (linear != null) { linear.enabled = true; linear.Initialize(currentSpeed); } // Default fallback with currentSpeed
                 break;
        }
    }
} 