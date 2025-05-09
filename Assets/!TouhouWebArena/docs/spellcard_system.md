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
    a.  Calls `TriggerSpellcardClear` (local overlap check around caster).
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
    b.  Loads the full `SpellcardData` from the resource path.
    c.  Calculates `originPosition` (target's field center via `SpawnAreaManager`).
    d.  Calculates `originRotation` (identity).
    e.  Calls `_actionRunner.RunSpellcardActions`, passing the `spellcardData.actions`, `originPosition`, `originRotation`, and the `sharedRandomOffset`.
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
        2.  Calls `_actionRunner.RunSpellcardActionsDynamicOrigin()`, passing its own `transform` (for dynamic bullet origin) and the `attackOrientationForBullets` (for correct bullet aiming, independent of illusion's visual rotation).
        3.  Lerps the illusion's `transform.position` from `startPos` to `endPos` over `duration`.

*   **`IllusionHealth.cs` (NetworkBehaviour, on Illusion Prefab):**
    *   `Initialize()`: Called by `ClientIllusionView`, sets health, target ID, and `isResponsibleClient` flag (true if this client is the one targeted by the illusion).
    *   `OnTriggerEnter2D()`: If `isResponsibleClient`, detects collisions with "PlayerShot".
    *   `TakeDamageClientSide()`: Decrements health. If health <= 0, sets `isDead = true` and calls `ReportDeathToServerRpc()`.
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
    *   `ClientIllusionView.ExecuteAttackPatternClientRpc()` receives the call.
    *   If `isMovingWithAttack`:
        *   `AnimateIllusionMovementAndAttack` coroutine starts.
        *   It calls `_actionRunner.RunSpellcardActionsDynamicOrigin(..., this.transform, attackPatternOrientation, ...)`
        *   It Lerps `this.transform.position` from `movementStartPos` to `movementEndPos`.
    *   Else (static attack):
        *   `_actionRunner.RunSpellcardActions(..., transform.position, attackPatternOrientation, ...)` is called.
    *   `ClientSpellcardActionRunner` then proceeds to execute individual `SpellcardAction`s from the pattern, spawning bullets. If dynamic, it reads `this.transform.position` for each bullet and uses the passed `attackPatternOrientation`.

5.  **Damage & Death:**
    *   Client (`Player A` shoots, hits illusion targeting `Player A`):
        *   `IllusionHealth.OnTriggerEnter2D()` on `Player A`'s client (where `isResponsibleClient` is true for this illusion) detects the hit.
        *   `TakeDamageClientSide()` decrements health.
        *   If health <= 0, `isDead = true`, `ReportDeathToServerRpc()` is called.
    *   Server (`IllusionHealth.ReportDeathToServerRpc()` on the illusion object):
        *   The RPC handler calls `_serverOrchestrator.ProcessClientDeathReport()`.
    *   Server (`ServerIllusionOrchestrator.ProcessClientDeathReport()`):
        *   Verifies sender ID matches `_targetPlayerId`.
        *   Calls `DespawnIllusion()`.

6.  **Despawning:**
    *   `ServerIllusionOrchestrator.DespawnIllusion()`:
        *   Notifies `ServerIllusionManager.Instance.ServerNotifyIllusionDespawned(NetworkObject)`.
        *   Calls `NetworkObject.Despawn()`.
    *   This can also be triggered by:
        *   `_illusionLifetimeTimer` in `ServerIllusionOrchestrator.Update()` expiring.
        *   The counter-mechanic in `ServerAttackSpawner`.

## Bullet Prefab Requirements (Spellcards)

*   Must have `PooledObjectInfo` component with a unique `PrefabID`.
*   Must have `ClientProjectileLifetime` component.
*   Must have Collider component (e.g., `CircleCollider2D`) set as Trigger if using physics triggers.
*   Must have all potential client-side movement behavior scripts attached **AND disabled** by default (e.g., `ClientLinearMovement`, `ClientDelayedHoming`, etc.). `ClientBulletConfigurer` will enable the correct one.
*   **SHOULD NOT** have `NetworkObject`, `NetworkTransform`, or old server-side behavior scripts if used solely for client-simulated spellcards.

## Adding a New Spellcard (Level 2/3 Refactored)

1.  **Create Bullet Prefabs (if needed):** Ensure prefabs meet the requirements above (PooledObjectInfo, ClientProjectileLifetime, disabled client behaviors). Register prefab with `ClientGameObjectPool` manager in the scene.
2.  **Create Spellcard Asset:** Right-click -> `Create` -> `TouhouWebArena` -> `Spellcard Data` in `Assets/Resources/Spellcards/`.
3.  **Rename Asset:** `CharacterNameLevelXSpellcard` (e.g., `HakureiReimuLevel3Spellcard`).
4.  **Configure Data:** Select the asset.
    *   Set `Required Charge Level`.
    *   Add `SpellcardAction`(s) to the `Actions` list.
    *   For each action:
        *   Assign `Bullet Prefabs` (ensure these have `PooledObjectInfo` and the ID matches pool configuration).
        *   Set `Position Offset` (relative to target field center).
        *   Set `Count`, `Formation`, `Radius`/`Spacing`, `Angle`.
        *   **If random origin needed for the *whole spellcard*:** On the **first action only**, check `Apply Random Spawn Offset` and set `Random Offset Min/Max`.
        *   Select the desired `Behavior` (e.g., `Linear`, `DelayedHoming`).
        *   Configure all relevant parameters for that behavior (`Speed`, `Speed Increment Per Bullet`, `Homing Speed`, `Homing Delay`, etc.).
        *   Configure timing (`Start Delay`, `Intra Action Delay`, `Lifetime`).
        *   Configure modifiers (`Skip Every Nth`).
5.  **Integration:** Automatic via naming convention (`ServerAttackSpawner` loads based on character/level).
6.  **Test:** Verify visual pattern execution, synchronization across clients, behavior logic, and random offset application.

## Adding a New Level 4 Spellcard (Illusion-based)

1.  **Create Bullet Prefabs (if needed):** Same requirements as for Level 2/3 (PooledObjectInfo, ClientProjectileLifetime, disabled client behaviors). Register with `ClientGameObjectPool`.
2.  **Create Illusion Prefab:**
    *   Design the visual appearance of the illusion.
    *   Attach `NetworkObject` and `ClientAuthoritativeTransform` (or similar if server dictates all movement via RPCs, which is the current model for idle moves).
    *   Attach `ServerIllusionOrchestrator.cs`.
    *   Attach `ClientIllusionView.cs`.
    *   Attach `IllusionHealth.cs`.
    *   Attach `ClientSpellcardActionRunner.cs` (or ensure `ClientIllusionView` can access a scene instance).
    *   Add a `Collider2D` (e.g., `BoxCollider2D` or `CircleCollider2D`) set as a trigger for `IllusionHealth` to detect hits.
    *   Assign any visual components (SpriteRenderer, Animator) if referenced by `ClientIllusionView`.
3.  **Create Level4SpellcardData Asset:** Right-click -> `Create` -> `TouhouWebArena` -> `Spellcard Data` -> `Level 4 Spellcard Data` in `Assets/Resources/Spellcards/` (or appropriate subfolder).
4.  **Rename Asset:** `CharacterNameLevel4Spellcard` (e.g., `HakureiReimuLevel4Spellcard`).
5.  **Configure `Level4SpellcardData`:**
    *   Assign the `Illusion Prefab` created in step 2.
    *   Set `Duration` (total lifetime of the illusion).
    *   Set `Health`.
    *   Set `Movement Area Height` (for idle vertical movement range).
    *   Set `Min Move Delay` and `Max Move Delay` (time between idle moves/attack cycles).
    *   Set `Attacks Per Move` (how many patterns from the pool to fire after each idle move).
    *   Populate the `Attack Pool` list with `CompositeAttackPattern`(s):
        *   For each `CompositeAttackPattern`:
            *   Give it a `Pattern Name`.
            *   Check `Orient Pattern Towards Target` if bullets should aim at the player.
            *   Check `Perform Movement During Attack` if the illusion should move while firing this pattern. Set `Attack Movement Vector` (local offset) and `Attack Movement Duration`.
            *   Add `SpellcardAction`(s) to its `Actions` list, configuring them as needed (bullet prefabs, count, formation, behavior, timing, etc.). `Position Offset` and `Angle` will be relative to the illusion and its calculated attack orientation.
6.  **Integration:** Automatic via naming convention if `ServerAttackSpawner` loads it based on character/level (ensure `case 4:` in `ServerAttackSpawner.ExecuteSpellcard` correctly loads the `Level4SpellcardData` by name).
7.  **Test:** Thoroughly test illusion spawning, idle movement, attack selection (randomness, count), attack execution (aiming, movement during attack, dynamic bullet origin), health/damage, and despawning (timed, damage, counter-mechanic).

## Character-Specific Implementations

*(Links remain the same, but the content within should be updated if necessary)*

## Data Structures

*(Content largely remains the same, but ensure descriptions match the refactored usage)*

### `SpellcardAction` (Serializable Class):

*   **Header: Random Spawn Offset (Relative to Calculated Position)**
    *   **`Apply Random Spawn Offset` (Bool):** If true **on the first action** of a sequence (for Level 2/3) or a `CompositeAttackPattern` (for Level 4 illusions), a single random offset (using Min/Max below) is calculated by the server and applied consistently to the calculated position of **all** bullets across **all** actions in that specific spellcard execution or illusion attack pattern.
    *   **`Random Offset Min` (Vector2):** Min random offset used if `applyRandomSpawnOffset` is true on the first action.
    *   **`Random Offset Max` (Vector2):** Max random offset used if `applyRandomSpawnOffset` is true on the first action.
*   **Header: Spawning**
    *   **`Bullet Prefabs` (List<GameObject>):** Bullet prefab(s) to spawn. Cycles if multiple are provided.
    *   **`Position Offset` (Vector2):** Offset relative to the pattern origin point. For Level 2/3, this is the target's field center. For Level 4 illusions, this is relative to the illusion's current position (or a point derived from its attack-specific movement if applicable).
    *   **`Count` (Int):** Number of bullets to spawn in this action.
*   **Header: Formation Shape**
    *   **`Formation` (Enum - `FormationType`):** `Point`, `Circle`, `Line`.
    *   **`Radius` (Float):** Used for `Circle` formation.
    *   **`Spacing` (Float):** Used for `Line` formation.
    *   **`Angle` (Float - Degrees):** Base angle for `Point` or `Circle` formation, or orientation angle for `Line` formation. For Level 2/3, this is relative to an identity rotation. For Level 4 illusions, this is relative to the `attackPatternOrientation` calculated by the server (which includes aiming adjustments towards the target player if `orientPatternTowardsTarget` is true for the `CompositeAttackPattern`).
*   **Header: Bullet Behavior**
    *   **`Behavior` (Enum - `BehaviorType`):** `Linear`, `Homing`, `DelayedHoming`, `DoubleHoming`, `Spiral`, `DelayedRandomTurn`. Selects the movement script to activate.
*   **Header: Behavior Speeds**
    *   **`Speed` (Float):** Primary speed used by the behavior (linear speed, initial homing speed, forward speed for `Homing`, etc.).
    *   **`Speed Increment Per Bullet` (Float):** Now directly used by `ClientLinearMovement`.
    *   **`Homing Speed` (Float):** Speed used when actively homing in `DelayedHoming`/`DoubleHoming`, or the turning rate (degrees/sec) for `Homing`.
    *   **`Tangential Speed` (Float):** (Spiral Only) Speed moving tangentially.
*   **Header: Initial Speed Transition (Optional)**
    *   **`Use Initial Speed` (Bool):** If true, bullet smoothly transitions from `initialSpeed` to `speed`.
    *   **`Initial Speed` (Float):** Starting speed if `useInitialSpeed` is true.
    *   **`Speed Transition Duration` (Float):** Time for the speed transition.
*   **Header: Behavior Timing & Parameters**
    *   **`Homing Delay` (Float):** Delay used by `DelayedHoming`, `DoubleHoming`, and `DelayedRandomTurn` before the secondary effect (homing/turning) begins.
    *   **`Second Homing Delay` (Float):** (Double Homing Only) Pause duration between homing phases.
    *   **`First Homing Duration` (Float):** (Double Homing Only) Duration of the first homing phase.
    *   **`Second Homing Look Ahead Distance` (Float):** (Double Homing Only) Look-ahead distance for second homing phase target calculation.
    *   **`Spread Angle` (Float):** (Delayed Random Turn Only) Max angle offset (degrees, +/- half this value) applied randomly to initial bullet rotation.
    *   **`Min Turn Speed` (Float):** (Delayed Random Turn Only) Minimum angular speed (degrees/sec) for the random turn.
    *   **`Max Turn Speed` (Float):** (Delayed Random Turn Only) Maximum angular speed (degrees/sec) for the random turn.
*   **Header: Spawning & Formation Modifiers**
    *   **`Skip Every Nth` (Int):** If > 0, skips spawning every Nth bullet (e.g., 4 skips the 4th, 8th, 12th...). Useful for creating gaps.
*   **Header: Timing & Lifetime**
    *   **`Start Delay` (Float):** Seconds to wait before this Action begins, relative to the start of the pattern it belongs to (`SpellcardData` list or `CompositeAttackPattern` list).
    *   **`Intra Action Delay` (Float):** Seconds to wait between spawning each individual bullet *within* this action (if `Count` > 1). Set to 0 for simultaneous spawn.
    *   **`Lifetime` (Float):** Overrides the default lifetime of the spawned bullet prefab (seconds). <= 0 uses prefab default.

*   **Clearability Note:** Bullet clearability by bombs/shockwaves depends on the **bullet prefab** having a component (like `NetworkBulletLifetime`) that implements `IClearable`, not on `SpellcardAction` data.

## Spellcard Clear Effect

When a player successfully activates a spellcard (Level 2, 3, or 4) after the cost is paid:
*   A bullet-clearing effect is immediately triggered around the **casting player**.
*   This uses `Physics2D.OverlapCircleAll` to find nearby colliders.
*   It attempts to find `IClearable` components on the found objects (specifically on their root `NetworkObject`).
*   It calls `Clear(true, castingPlayerRole)` on any found `IClearable` components, forcing them to clear.
*   The radius of the clear effect scales with the spell level:
    *   Level 2: `3.0` units (configurable in `ServerAttackSpawner.TriggerSpellcardClear`)
    *   Level 3: `5.0` units
    *   Level 4: `10.0` units
*   *(TODO: Add visual effect for this clear)*. 