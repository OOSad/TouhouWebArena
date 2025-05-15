# Spellcard System Documentation

In Touhou games, characters can use Spellcards. These are unique patterns of bullets, usually created by some kind of emitter. Not only could the emitter have custom instructions - spawn these lines of bullets around this direction, or spawn these circles of bullets around this spot - but the bullets themselves usually also have custom behaviors attached to them, like go forward, go left, go right, home in, spin in circles, etc. These complex patterns of bullets originating from relatively simple behaviors demand a data-driven solution, using scriptable objects for the game, as the labor of creating even a simple spellcard would quickly become overwhelming and impossible to describe.

## Overview (Refactored Architecture)

**The spellcard system has been significantly refactored from a server-authoritative model to a server-triggered, client-simulated model.**

Spellcards are still defined using ScriptableObject assets created in the Unity editor.

*   **Level 2 & 3 Spellcards:** Use `SpellcardData` assets. Define a sequence of `SpellcardAction`s. The server validates activation, determines parameters (including a shared random offset if needed), and sends an RPC to clients. Clients then load the data and execute the entire action sequence locally.
*   **Level 4 Spellcards:** Use `Level4SpellcardData` assets. *(Note: Level 4 Illusion logic is assumed to still be primarily server-driven based on previous documentation, but this needs review/refactoring if it hasn't been updated yet. This document primarily focuses on the refactored Level 2/3 flow).*

Activation logic remains similar: player input triggers a server request, cost is validated, and a clear effect occurs around the caster. However, the *execution* of the bullet patterns (for Lv2/3) now happens entirely on the client side.

## Key Components (Refactored)

### Data Structures (ScriptableObjects & Serializable Classes)

*   **`SpellcardData` (ScriptableObject):** Defines Level 2 & 3 spellcards as a sequence of `SpellcardAction`s.
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/SpellcardData.cs`
*   **`Level4SpellcardData` (ScriptableObject):** Defines Level 4 spellcards, which manifest as autonomous illusions.
    *   Specifies illusion properties: prefab, duration, health, movement parameters (`MovementAreaHeight`, `MinMoveDelay`, `MaxMoveDelay`), and an `AttackPool` of `CompositeAttackPattern`s that the illusion can execute.
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/Level4SpellcardData.cs`
*   **`CompositeAttackPattern` (Serializable Class):** Defines a single, complete attack sequence that a Level 4 illusion can perform. Part of the `AttackPool` in `Level4SpellcardData`.
    *   Contains a list of `SpellcardAction`s and properties like `orientPatternTowardsTarget`, `performMovementDuringAttack`, `attackMovementVector`, and `attackMovementDuration`.
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/CompositeAttackPattern.cs`
*   **`SpellcardAction` (Serializable Class):** The fundamental building block defining a single bullet spawn pattern (formation, count, prefab, behavior, timing, offsets, etc.). Used by both `SpellcardData` (for Level 2/3) and `CompositeAttackPattern` (for Level 4 illusions).
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/SpellcardAction.cs`
    *   **New Fields:**
        *   `applyRandomSpawnOffset` (bool): If true on the *first* action of a sequence, triggers calculation of a shared random offset.
        *   `randomOffsetMin` (Vector2): Min range for the shared random offset.
        *   `randomOffsetMax` (Vector2): Max range for the shared random offset.
    *   **Usage Change:**
        *   `speedIncrementPerBullet` (float): Now used by `ClientLinearMovement`.
        *   `positionOffset` (Vector2): Interpreted relative to the base origin (target's field center), then the shared random offset is added.

### Server-Side Components

*   **`PlayerShootingController.cs` (Script):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Characters/`
    *   Purpose: Owner client detects input, sends `RequestSpellcardServerRpc`.
*   **`SpellBarManager.cs` (Script - Server-Side Service):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose: Consumes spell cost.
*   **`ServerAttackSpawner.cs` (Script - Server-Side Service Facade):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose (Refactored):
        *   Receives forwarded spellcard request after cost validation.
        *   Triggers the spellcard clearing effect via `TriggerSpellcardClear`.
        *   Triggers the spellcard banner display via `SpellcardBannerDisplay` RPC.
        *   **For Level 2/3:** Loads `SpellcardData` only to determine if `applyRandomSpawnOffset` is true on the first action, calculates `sharedRandomOffset`, and calls `SpellcardNetworkHandler.ExecuteSpellcardClientRpc`.
        *   **For Level 4:** Instantiates the illusion prefab defined in `Level4SpellcardData`. Initializes and spawns its `ServerIllusionOrchestrator`. Also handles the illusion despawn counter-mechanic.
        *   *(No longer directly executes actions or configures bullets for Lv2/3)*.
*   **`SpellcardNetworkHandler.cs` (Script - NetworkBehaviour Singleton):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose: Relays the spellcard execution command from the server to all clients via `ExecuteSpellcardClientRpc`, including the `sharedRandomOffset`.

### Client-Side Components

*   **`ClientSpellcardExecutor.cs` (Script - MonoBehaviour Singleton):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/`
    *   Purpose:
        *   Receives `ExecuteSpellcardClientRpc` from `SpellcardNetworkHandler`.
        *   Loads the specified `SpellcardData` asset from Resources.
        *   Calculates the base `originPosition` (using `PlayerDataManager` and `SpawnAreaManager` to find the center of the *target's* play area).
        *   Calculates base `originRotation` (identity).
        *   Calls `ClientSpellcardActionRunner.RunSpellcardActions`, passing the loaded actions, origin info, and the received `sharedRandomOffset`.
*   **`ClientSpellcardActionRunner.cs` (Script - MonoBehaviour):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/` (Can be a scene service or attached to objects needing to run actions, like `ClientIllusionView`).
    *   Purpose:
        *   Runs `ExecuteActionSequenceCoroutine` (for static origins, typically Level 2/3 spellcards) or `ExecuteActionSequenceCoroutineDynamicOrigin` (for Level 4 illusions moving during attacks).
        *   Takes lists of actions, origin information (static Vector3/Quaternion or dynamic Transform + explicit Quaternion), and `sharedRandomOffset`.
        *   Iterates through `SpellcardAction`s, managing `startDelay` and `intraActionDelay` to allow for concurrent action execution and timed bullet spawns.
        *   For each bullet: gets instance from `ClientGameObjectPool`, calculates spawn position/rotation (respecting static/dynamic origin and explicit orientation for illusions), calls `ClientBulletConfigurer`, and activates the bullet.
*   **`ClientBulletConfigurer.cs` (Script - Static Helper Class):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/Behaviors/`
    *   Purpose:
        *   Called by `ClientSpellcardActionRunner` for each spawned bullet.
        *   Initializes `ClientProjectileLifetime` using `action.lifetime`.
        *   Calls `DeactivateAllMovementBehaviors` on the bullet instance.
        *   Gets/finds the appropriate client-side behavior component (e.g., `ClientLinearMovement`) based on `action.behavior`.
        *   Calls the `Initialize` method of that behavior component, passing relevant parameters extracted from the `action` data (e.g., `action.speed`, `action.speedIncrementPerBullet`, `bulletIndex`, `action.homingDelay`, `action.homingSpeed`, etc.).
        *   Sets the behavior component `enabled = true`.
*   **Client Behavior Scripts (e.g., `ClientLinearMovement.cs`, `ClientDelayedHoming.cs`, etc.):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/Behaviors/`
    *   Purpose: `MonoBehaviour` components attached (disabled by default) to spellcard bullet prefabs. Contain the actual client-side movement logic in their `Update` methods. Enabled and initialized by `ClientBulletConfigurer`.
*   **`ClientProjectileLifetime.cs` (Script - MonoBehaviour):**
    *   Located likely in `Assets/!TouhouWebArena/Scripts/Gameplay/Projectiles/` or similar.
    *   Purpose: Attached to bullet prefabs. Manages despawn timer and returns the object to `ClientGameObjectPool` upon expiration. Initialized by `ClientBulletConfigurer`.
*   **`ClientGameObjectPool.cs` (Script - MonoBehaviour Singleton):**
    *   Located likely in `Assets/!TouhouWebArena/Scripts/System/` or `Managers`.
    *   Purpose: Manages pooling of client-side GameObjects, including spellcard bullets. Requires prefabs to have `PooledObjectInfo`.

*   **Note on Level 4 Illusion Components:**
    *   The specific components driving Level 4 illusions (`ServerIllusionOrchestrator.cs`, `ClientIllusionView.cs`, `IllusionHealth.cs`) are detailed extensively in the **"Level 4 Spellcards (Illusion System)"** section.

### Removed / Obsolete Components (for Lv2/3 Spellcards)

*   `ServerSpellcardExecutor.cs`
*   `ServerBulletConfigurer.cs`
*   Server-side behavior scripts (e.g., `LinearMovement.cs`, `DelayedHoming.cs`) are no longer used *for spellcard bullets*. They might still be used for other projectile types if not refactored.
*   `NetworkObject`, `NetworkTransform` on spellcard bullet prefabs.

## Refactored Spellcard Activation & Execution Flow (Level 2/3)

1.  Client (PlayerShootingController) -> Detects input, sends `RequestSpellcardServerRpc`.
2.  Server (PlayerShootingController RPC Handler) -> Calls `SpellBarManager.ConsumeSpellCost`.
3.  Server (SpellBarManager) -> If successful, calls `ServerAttackSpawner.ExecuteSpellcard`.
4.  Server (ServerAttackSpawner):
    a.  Calls `TriggerSpellcardClear`. This method **does not** perform the clear directly. Instead, it calculates the `casterPosition`, `clearRadius` (based on spell level), and `casterRole`, then sends these parameters to all clients via `ClientSpellcardExecutor.Instance.TriggerLocalClearEffectClientRpc`.
    b.  Calls `SpellcardBannerDisplay.ShowBannerClientRpc` (RPC to all clients).
    c.  Loads `SpellcardData` for the *first action* (`actions[0]`) from Resources.
    d.  Checks `actions[0].applyRandomSpawnOffset`. If true, calculates a `sharedRandomOffset` Vector2 using `Random.Range` and `actions[0].randomOffsetMin/Max`. Otherwise, offset is `Vector2.zero`.
    e.  Calls `SpellcardNetworkHandler.ExecuteSpellcardClientRpc`, passing casterId, targetId, resource path, level, and the calculated `sharedRandomOffset`.
5.  Network -> RPC is sent to all connected clients.
6.  Client (SpellcardNetworkHandler):
    a.  Receives `ExecuteSpellcardClientRpc`.
    b.  Calls `ClientSpellcardExecutor.Instance.StartLocalSpellcardExecution`, passing all received parameters.
7.  Client (ClientSpellcardExecutor):
    a.  Logs receipt.
    b.  **(Spellcard Clear Effect Handling - Triggered by `TriggerLocalClearEffectClientRpc` from Server step 4.a):**
        i.  Receives `casterPosition`, `clearRadius`, and `casterRole`.
        ii. Performs `Physics2D.OverlapCircleAll(casterPosition, clearRadius)` to find all colliders in the area.
        iii. Iterates through colliders:
            *   **`StageSmallBulletMoverScript`:** If found, checks if `stageBulletMover.OwningPlayerRole == casterRole`.
                *   If true, calls `stageBulletMover.ForceReturnToPoolByBomb()`.
                *   Then, calls `PlayerAttackRelay.LocalInstance.RequestOpponentStageBulletSpawnServerRpc()` to spawn a "revenge" bullet (defaults: "StageSmallBullet" prefab, 2.5f speed, 7f lifetime).
            *   **Other "EnemyProjectiles" Layer Objects:** If an object is on the "EnemyProjectiles" layer and *does not* have a `StageSmallBulletMoverScript` (to avoid double processing), and has a `ClientProjectileLifetime` component, `projectileLifetime.ForceReturnToPool()` is called.
            *   **`ClientFairyHealth`:** If found and `fairyHealth.OwningPlayerRole == casterRole`, calls `fairyHealth.ForceReturnToPool()`.
            *   **`ClientSpiritController`:** If found and `spiritController.OwningPlayerRole == casterRole`, calls `spiritHealth.ForceReturnToPool()` (after getting `ClientSpiritHealth`).
            *   **Opponent's Extra Attacks (`ReimuExtraAttackOrb_Client`, `MarisaExtraAttackLaser_Client`):** If found and `AttackerClientId` does not match the `casterPlayerClientId` (resolved from `casterRole`), calls `ForceReturnToPoolByClear()` on the extra attack.
    c.  Loads the full `SpellcardData` from the resource path (for spellcard bullet spawning, separate from the clear effect).
    d.  Calculates `originPosition` (target's field center via `SpawnAreaManager`).
    e.  Calculates `originRotation` (identity).
    f.  Calls `_actionRunner.RunSpellcardActions`, passing the `spellcardData.actions`, `originPosition`, `originRotation`, and the `sharedRandomOffset`.
8.  Client (ClientSpellcardActionRunner):
    a.  Starts `ExecuteActionSequenceCoroutine` (passing `sharedRandomOffset`).
    b.  Coroutine iterates through each `action` in `spellcardData.actions`.
    c.  Handles `action.startDelay`.
    d.  Coroutine loops `action.count` times (respecting `skipEveryNth`).
    e.  Gets bullet instance from `ClientGameObjectPool`.
    f.  Calls `CalculateSpawnPosition` (passing `originPosition`, `originRotation`, `action`, `index`, `targetPlayerRole`, and the **same** `sharedRandomOffset` for every bullet).
    g.  Calls `CalculateSpawnRotation`.
    h.  Sets bullet `transform.position`/`rotation`.
    i.  Calls `ClientBulletConfigurer.ConfigureBullet` (passing instance, `action`, IDs, `index`).
    j.  Sets bullet `SetActive(true)`.
    k.  Handles `action.intraActionDelay`.
9.  Client (ClientBulletConfigurer):
    a.  Initializes `ClientProjectileLifetime`.
    b.  Deactivates all movement behaviors on the instance.
    c.  Finds the correct client behavior component (e.g., `ClientLinearMovement`).
    d.  Calls its `Initialize` method with parameters from `action` and the `index`.
    e.  Enables the behavior component.
10. Client (Active Behavior Script, e.g., `ClientLinearMovement`):
    a.  `Update()` method executes movement logic based on initialized parameters.
11. Client (ClientProjectileLifetime):
    a.  Returns bullet to pool when timer expires.

## Level 4 Spellcards (Illusion System)

Level 4 spellcards manifest as autonomous "illusion" entities that persist on the field for a duration, periodically moving and executing attack patterns. The system follows a server-triggered, client-simulated model for illusion actions and a client-authoritative model for illusion health (with server notification for death).

### Core Concepts:

*   **Server-Orchestrated, Client-Simulated:** The server dictates the illusion's lifecycle (spawn, despawn), high-level actions (when to move, which attack to perform, where to aim), and manages its logical position. Clients are responsible for the visual representation, smooth movement animations, and executing the bullet patterns for attacks.
*   **Dynamic Attack Origin:** Illusions can move while simultaneously firing bullets. The bullets correctly originate from the moving illusion's transform.
*   **Client-Side Health, Server-Notified Death:** The client targeted by the illusion is responsible for detecting hits from player shots and decrementing the illusion's health. Upon health depletion, this client informs the server, which then despawns the illusion across all clients.

### Key Components for Level 4 Illusions:

*   **`Level4SpellcardData` (ScriptableObject):**
    *   Defines the core attributes of the illusion and its behavior.
    *   Fields include:
        *   `IllusionPrefab`: The GameObject prefab for the illusion.
        *   `Duration`: Total time the illusion remains active.
        *   `Health`: Initial health of the illusion.
        *   `MovementAreaHeight`: Vertical range for idle movement.
        *   `MinMoveDelay`, `MaxMoveDelay`: Time between idle moves/attack cycles.
        *   `AttacksPerMove`: Number of attack patterns to execute after each idle move.
        *   `AttackPool` (List of `CompositeAttackPattern`): The collection of attack patterns the illusion can randomly choose from.

*   **`CompositeAttackPattern` (Serializable Class, within `Level4SpellcardData`):**
    *   Defines a single, potentially complex, attack sequence an illusion can perform.
    *   Fields include:
        *   `patternName`: Descriptive name.
        *   `actions` (List of `SpellcardAction`): The sequence of bullet-spawning actions.
        *   `orientPatternTowardsTarget` (bool): If true, the server calculates the orientation for this pattern to aim at the target player.
        *   `performMovementDuringAttack` (bool): If true, the illusion executes a specific movement defined by `attackMovementVector` and `attackMovementDuration` *while* this pattern's actions are firing.
        *   `attackMovementVector` (Vector2): The local-space vector for the attack-specific movement.
        *   `attackMovementDuration` (float): Duration of the attack-specific movement.

*   **`SpellcardAction` (Serializable Class, within `CompositeAttackPattern`):**
    *   Same structure as used for Level 2/3 spellcards, defining individual bullet spawn characteristics (prefab, count, formation, behavior, timing, etc.).
    *   For illusions:
        *   `Position Offset` is relative to the illusion's current position (or a point derived from its attack-specific movement).
        *   `Angle` is relative to the `attackPatternOrientation` calculated by the server (which includes aiming adjustments).
        *   `applyRandomSpawnOffset` (on the first action of a pattern) results in a `sharedRandomOffset` for all actions in that specific pattern execution, calculated by the server.

*   **`ServerIllusionOrchestrator.cs` (NetworkBehaviour, on Illusion Prefab):**
    *   **Server-side only.** Manages a single illusion instance.
    *   `Initialize()`: Loads `Level4SpellcardData`, sets target, duration, health, calculates movement boundaries with padding. Sends initial state to clients.
    *   `Update()`: Manages illusion lifetime timer and schedules `PerformIdleMoveAndAttack()`.
    *   `PerformIdleMoveAndAttack()`:
        1.  Calculates a new random position for an "idle move" within padded bounds and updates its own `transform.position`.
        2.  RPCs the new transform to `ClientIllusionView`.
        3.  Randomly selects `AttacksPerMove` patterns from the `AttackPool`.
        4.  For each selected pattern:
            *   Determines if `performMovementDuringAttack` is true. If so, calculates `movementStartPos`, `movementEndPos`, and `movementDur`. The server starts a `DelayedUpdateServerPosition` coroutine to update its own logical position to `movementEndPos` after `movementDur`.
            *   Determines if `orientPatternTowardsTarget` is true. If so, calculates the `attackPatternOrientation` (Quaternion) to aim at the target player. This aiming is based on the illusion's position *before* the attack-specific movement begins.
            *   Calculates `sharedRandomOffset` if the pattern's first action requires it.
            *   Calls `ExecuteAttackPatternClientRpc` on `ClientIllusionView` with all necessary parameters (pattern index, movement details, orientation, offset).
    *   `ProcessClientDeathReport()`: Called by `IllusionHealth` (not an RPC itself). Verifies sender and calls `DespawnIllusion()`.
    *   `DespawnIllusion()`: Notifies `ServerIllusionManager` and despawns the `NetworkObject`.

*   **`ClientIllusionView.cs` (NetworkBehaviour, on Illusion Prefab):**
    *   **Client-side only.** Manages the visual representation and client-side actions of an illusion.
    *   `InitializeClientRpc()`: Receives data from server, loads `Level4SpellcardData`, initializes `IllusionHealthComponent`.
    *   `UpdateIllusionTransformClientRpc()`: Receives transform updates from server for idle moves. Updates local `transform.position` (visual rotation is generally not used for the illusion sprite itself).
    *   `ExecuteAttackPatternClientRpc()`: Receives attack command from server.
        *   If `isMovingWithAttack` is true: Starts `AnimateIllusionMovementAndAttack()` coroutine.
        *   Else (static attack): Calls `_actionRunner.RunSpellcardActions()` with the illusion's current position and the server-provided `initialOrientation`.
    *   `AnimateIllusionMovementAndAttack()` (Coroutine):
        1.  Sets illusion `transform.position` to `startPos`.
        2.  Calls `_actionRunner.RunSpellcardActionsDynamicOrigin()`, passing its own `transform` (for dynamic bullet origin) and the `attackOrientationForBullets` (for correct bullet aiming, independent of illusion's visual rotation during movement).
        3.  Lerps the illusion's `transform.position` from `startPos` to `endPos` over `duration`.
    *   **Damage Flash:** Contains a `FlashRed()` method (called by `IllusionHealth` when damage is taken) that triggers a coroutine to briefly tint the `_illusionSpriteRenderer` (configurable color, duration, intensity) and smoothly fade back to the original color.

*   **`IllusionHealth.cs` (NetworkBehaviour, on Illusion Prefab):**
    *   `Initialize()`: Called by `ClientIllusionView`, sets health, target ID, and `isResponsibleClient` flag (true if this client is the one targeted by the illusion).
    *   `OnTriggerEnter2D()`: If `isResponsibleClient`, detects collisions with "PlayerShot".
    *   `TakeDamageClientSide()`: Decrements health. Calls `_clientView.FlashRed()` to trigger the visual effect. If health <= 0, sets `isDead = true` and calls `ReportDeathToServerRpc()`.
    *   `ReportDeathToServerRpc()`: ServerRpc that, on the server, calls `_serverOrchestrator.ProcessClientDeathReport()`.

*   **`ClientSpellcardActionRunner.cs` (MonoBehaviour, often a scene service or on ClientIllusionView):**
    *   `RunSpellcardActions()`: Used for static attacks (not typically by illusions directly after the refactor for dynamic movement).
    *   `RunSpellcardActionsDynamicOrigin()`: Used by `ClientIllusionView.AnimateIllusionMovementAndAttack()`. Takes a `Transform originTransform` and an `explicitAttackOrientation`. Iteratively calls `ExecuteSingleActionCoroutineInternalDynamicOrigin()`.
    *   `ExecuteSingleActionCoroutineInternalDynamicOrigin()`: Handles `startDelay` for an action. For each bullet to spawn, it reads `originTransform.position` and uses `explicitAttackOrientation` to calculate spawn parameters. Then spawns and configures the bullet.

### Illusion Lifecycle & Flow:

1.  **Activation (Player A casts Level 4 Spellcard):**
    *   `PlayerShootingController` -> `RequestSpellcardServerRpc`.
    *   Cost validated by `SpellBarManager`.
    *   `ServerAttackSpawner.ExecuteSpellcard` (case 4):
        *   **Despawn Counter-Mechanic:** Iterates through existing `ServerIllusionOrchestrator` instances. If an illusion (owned by Player B) targets Player A, `DespawnIllusion()` is called on it.
        *   Instantiates the `IllusionPrefab` from `Level4SpellcardData`.
        *   Gets/adds `ServerIllusionOrchestrator` and `IllusionHealth` to the server-side instance.
        *   Calls `serverOrchestrator.Initialize()` with spellcard path and `targetPlayerId` (opponent of caster).
        *   Spawns the illusion `NetworkObject`.

2.  **Initialization:**
    *   Server (`ServerIllusionOrchestrator.Initialize()`):
        *   Loads data, sets timers, calculates bounds.
        *   Calls `_clientView.InitializeClientRpc(path, targetId, health)`.
        *   Calls `_clientView.UpdateIllusionTransformClientRpc(initialPosition, initialRotation)` to sync initial state.
    *   Client (`ClientIllusionView.InitializeClientRpc()`):
        *   Loads data, sets target, initializes its `IllusionHealthComponent`.

3.  **Idle Movement & Attack Cycle (Server):**
    *   `ServerIllusionOrchestrator.Update()` manages `_nextIdleMoveTimer`.
    *   When timer elapses, `PerformIdleMoveAndAttack()` is called:
        *   Illusion "teleports" to a new random `newPositionAfterIdleMove`. Server's `transform.position` is updated.
        *   `_clientView.UpdateIllusionTransformClientRpc(newPositionAfterIdleMove, transform.rotation)` is sent.
        *   `AttacksPerMove` patterns are selected randomly.
        *   For each pattern:
            *   Movement parameters (`isMovingForThisAttack`, `movementStartPos`, `movementEndPos`, `movementDur`) are determined.
            *   Aiming orientation (`attackPatternOrientation`) is calculated if `orientPatternTowardsTarget` is true.
            *   `sharedRandomOffset` is calculated.
            *   `_clientView.ExecuteAttackPatternClientRpc(...)` is called.
            *   If `isMovingForThisAttack`, `StartCoroutine(DelayedUpdateServerPosition(movementEndPos, movementDur))` is called to update the server's logical position after the client-side visual movement.

4.  **Attack Execution (Client):**
    *   `ClientIllusionView.ExecuteAttackPatternClientRpc()`