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
*   **`Level4SpellcardData` (ScriptableObject):** Defines Level 4 spellcards (currently assumed server-driven illusion logic).
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/Level4SpellcardData.cs`
*   **`CompositeAttackPattern` (Serializable Class):** Defines attack units used by Level 4 illusions.
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/CompositeAttackPattern.cs`
*   **`SpellcardAction` (Serializable Class):** The fundamental building block defining a single bullet spawn pattern (formation, count, prefab, behavior, timing, offsets, etc.).
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
        *   **Loads `SpellcardData` only to determine if `applyRandomSpawnOffset` is true on the first action.**
        *   **Calculates a single `sharedRandomOffset` Vector2 if requested.**
        *   Calls `SpellcardNetworkHandler.ExecuteSpellcardClientRpc`, passing spellcard path, caster/target IDs, level, and the calculated `sharedRandomOffset`.
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
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/` (Should be on the same GameObject as `ClientSpellcardExecutor`).
    *   Purpose:
        *   Runs the `ExecuteActionSequenceCoroutine`.
        *   Takes the list of actions, origin info, and the `sharedRandomOffset`.
        *   Iterates through `SpellcardAction`s, handling `startDelay`.
        *   For each action, iterates `action.count` times (respecting `skipEveryNth`):
            *   Gets a bullet prefab instance from `ClientGameObjectPool` using `action.bulletPrefabs` and `PooledObjectInfo.PrefabID`.
            *   Calls `CalculateSpawnPosition` to get the final spawn point (incorporating origin, formation, index, `action.positionOffset`, and the **same `sharedRandomOffset`** for all bullets in the sequence).
            *   Calls `CalculateSpawnRotation`.
            *   Sets the bullet's `transform.position` and `transform.rotation`.
            *   Calls `ClientBulletConfigurer.ConfigureBullet`, passing the bullet instance, action data, caster/target IDs, and the bullet `index`.
            *   Sets the bullet `SetActive(true)`.
            *   Handles `intraActionDelay`.
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

*(Existing documentation likely needs review. The flow described above does NOT cover Level 4 illusions, which likely still rely on server-side state and control via `ServerIllusionManager`, `Level4IllusionController`, etc. If Level 4 illusions also need client-side simulation for their attacks, they would need a similar refactor.)*

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

## Character-Specific Implementations

*(Links remain the same, but the content within should be updated if necessary)*

## Data Structures

*(Content largely remains the same, but ensure descriptions match the refactored usage)*

### `SpellcardAction` (Serializable Class):

*   **Header: Random Spawn Offset (Relative to Calculated Position)**
    *   **`Apply Random Spawn Offset` (Bool):** If true **on the first action** of a sequence, a single random offset (using Min/Max below) is calculated by the server and applied consistently to the calculated position of **all** bullets across **all** actions in that sequence.
    *   **`Random Offset Min` (Vector2):** Min random offset used if `applyRandomSpawnOffset` is true on the first action.
    *   **`Random Offset Max` (Vector2):** Max random offset used if `applyRandomSpawnOffset` is true on the first action.
*   **Header: Spawning**
    *   **`Bullet Prefabs` (List<GameObject>):** Bullet prefab(s) to spawn. Cycles if multiple are provided.
    *   **`Position Offset` (Vector2):** Offset relative to the pattern origin point (illusion position for Level 4, calculated point for Level 2/3).
    *   **`Count` (Int):** Number of bullets to spawn in this action.
*   **Header: Formation Shape**
    *   **`Formation` (Enum - `FormationType`):** `Point`, `Circle`, `Line`.
    *   **`Radius` (Float):** Used for `Circle` formation.
    *   **`Spacing` (Float):** Used for `Line` formation.
    *   **`Angle` (Float - Degrees):** Base angle for `Point` or `Circle` formation, orientation angle for `Line` formation. Relative to `baseRotation` (which is identity for Lv2/3, potentially aimed for Lv4 if `orientPatternTowardsTarget` is true).
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