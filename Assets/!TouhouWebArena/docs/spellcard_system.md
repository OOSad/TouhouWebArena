# Spellcard System Documentation

In Touhou games, characters can use Spellcards. These are unique patterns of bullets, usually created by some kind of emitter. Not only could the emitter have custom instructions - spawn these lines of bullets around this direction, or spawn these circles of bullets around this spot - but the bullets themselves usually also have custom behaviors attached to them, like go forward, go left, go right, home in, spin in circles, etc. These complex patterns of bullets originating from relatively simple behaviors demand a data-driven solution, using scriptable objects for the game, as the labor of creating even a simple spellcard would quickly become overwhelming and impossible to describe.

## Overview

Spellcards are defined using ScriptableObject assets created in the Unity editor. There are two main types:
*   **Level 2 & 3 Spellcards:** Use `SpellcardData` assets. Define a sequence of `SpellcardAction`s that are executed directly by the `ServerAttackSpawner` from a calculated origin point above the opponent.
*   **Level 4 Spellcards:** Use `Level4SpellcardData` assets. Define a persistent **Illusion** prefab that is spawned on the opponent's side. This illusion moves randomly and executes attacks defined by `CompositeAttackPattern`s, which themselves contain `SpellcardAction`s.

Spellcards are typically activated when a player releases the fire key with sufficient charge (Level 2, 3, or 4). Activation also triggers a bullet-clearing effect around the caster.

## Key Components

*   **`SpellcardData` (ScriptableObject):** Defines Level 2 & 3 spellcards as a sequence of `SpellcardAction`s.
    *   Defined in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/SpellcardData.cs`
    *   Purpose: Data container for simpler, direct bullet patterns.

*   **`Level4SpellcardData` (ScriptableObject):** Defines Level 4 spellcards, focusing on a persistent illusion.
    *   Defined in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/Level4SpellcardData.cs`
    *   Purpose: Data container for Level 4 effects, including the illusion prefab, duration, health, movement parameters, and a pool of `CompositeAttackPattern`s the illusion uses.

*   **`CompositeAttackPattern` (Serializable Class):** Defines a single attack unit used by Level 4 illusions.
    *   Defined in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/CompositeAttackPattern.cs`
    *   Purpose: Groups one or more `SpellcardAction`s that are executed together. Allows complex attacks to be selected randomly from the Level 4 pool. Can be optionally oriented towards the target and include movement of the illusion during the attack.

*   **`SpellcardAction` (Serializable Class):** The fundamental building block defining a single bullet spawn pattern (formation, count, prefab, behavior, etc.).
    *   Defined in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/SpellcardAction.cs`
    *   Purpose: Used within `SpellcardData` and `CompositeAttackPattern` to describe how bullets are spawned.

*   **`PlayerShootingController.cs` (Script):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Characters/`
    *   Purpose: Detects spellcard activation input on the **owner client** and sends `RequestSpellcardServerRpc`.

*   **`SpellBarManager.cs` (Script - Server-Side Service):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose: Consumes spell cost upon receiving `RequestSpellcardServerRpc`.

*   **`ServerAttackSpawner.cs` (Script - Server-Side Service Facade):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose:
        *   Receives forwarded `RequestSpellcardServerRpc` via `ExecuteSpellcard`.
        *   Triggers the spellcard clearing effect via `TriggerSpellcardClear`.
        *   Dispatches execution to specialized helpers based on spell level.
        *   Delegates Level 2/3 setup to `ServerSpellcardExecutor`.
        *   Delegates Level 4 illusion spawning to `ServerIllusionManager`.

*   **`ServerSpellcardExecutor.cs` (Script - Non-MonoBehaviour):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose: Handles Level 2/3 spellcard setup: loads data, finds opponent, calculates origin, and initiates action execution via `ServerSpellcardActionRunner`.

*   **`ServerSpellcardActionRunner.cs` (Script - Server-Side Service MonoBehaviour):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose:
        *   Runs the `RunSpellcardActions` coroutine for Level 2/3 spellcards, executing the sequence of `SpellcardAction`s.
        *   Runs the `ExecuteSingleSpellcardActionFromServerCoroutine` for individual actions (used by Level 4 illusions and the Level 2/3 runner).
        *   Handles delays, formations, and delegates bullet spawning (using `ServerPooledSpawner`) and configuration (using `ServerBulletConfigurer`).

*   **`ServerIllusionManager.cs` (Script - Server-Side Service MonoBehaviour):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose: Handles Level 4 illusion lifecycle: Spawns illusion prefab, manages duration/health tracking, handles despawn notifications, and cleans up illusions on player disconnect or cancellation.

*   **`ServerBulletConfigurer.cs` (Script - Static Helper Class):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose: Configures the behavior components (movement, homing, etc.) of newly spawned spellcard bullets based on `SpellcardAction` data.

*   **`Level4IllusionController.cs` (Script - NetworkBehaviour):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/`
    *   Purpose: Attached to the Level 4 illusion prefab. **Server-side**, it handles the illusion's random movement, handles optional movement *during* specific attacks, and triggers attacks by randomly selecting `CompositeAttackPattern`s from the `Level4SpellcardData` and executing them via `ServerSpellcardActionRunner.Instance.ExecuteSingleSpellcardActionFromServerCoroutine`.

*   **`IllusionHealth.cs` (Script - NetworkBehaviour):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/`
    *   Purpose: Attached to the Level 4 illusion prefab. Manages the illusion's health and handles its destruction when health reaches zero, notifying `ServerIllusionManager` upon despawn.

*   **Bullet Behavior Scripts (e.g., `LinearMovement.cs`, `Homing.cs`, `DelayedHoming.cs`, `DelayedRandomTurn.cs`):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/Behaviors/`
    *   Purpose: Components attached to bullet prefabs defining movement. `DelayedRandomTurn` was added for Illusion Star.

*   **Bullet Prefabs:** Standard Unity prefabs with `NetworkObject`, `NetworkTransform`, behavior scripts, and `NetworkBulletLifetime` (which implements `IClearable`).

*   **Interfaces:**
    *   **`IClearable`:** Implemented by components (like `NetworkBulletLifetime`) on objects that can be cleared by bombs or other effects. Defines `Clear(bool forceClear, PlayerRole sourceRole)`.

## Spellcard Activation Flow & Effects

1.  Client (`PlayerShootingController`) detects input release and sufficient local charge level.
2.  Client sends `RequestSpellcardServerRpc` with `spellLevel`.
3.  Server RPC handler (`PlayerShootingController`) receives the request.
4.  Server calls `SpellBarManager.ConsumeSpellCost`.
5.  If cost is paid:
    a.  Server calls `ServerAttackSpawner.TriggerSpellcardClear(senderClientId, spellLevel)`. This performs an overlap check around the caster and calls `Clear(true, casterRole)` on `IClearable` components found within a radius determined by `spellLevel`.
    b.  Server calls `ServerAttackSpawner.ExecuteSpellcard(senderClientId, spellLevel)`.
        *   This method first triggers the UI banner display via `SpellcardBannerDisplay.Instance.ShowBannerClientRpc`, sending the caster's role and character name to all clients.
        *   Then, it determines if it's Level 2/3 or Level 4.
            *   **Level 2/3:** Delegates to `ServerSpellcardExecutor.ExecuteLevel2or3Spellcard`. This loads `SpellcardData`, calculates origin, and calls `ServerSpellcardActionRunner.Instance.RunSpellcardActions` coroutine.
            *   **Level 4:** Delegates to `ServerIllusionManager.Instance.ServerSpawnLevel4Illusion`. This spawns the Illusion prefab (which initializes `Level4IllusionController` and `IllusionHealth`).
    c.  The `Level4IllusionController` takes over, moving randomly and executing `CompositeAttackPattern`s, which call `ServerSpellcardActionRunner.Instance.ExecuteSingleSpellcardActionFromServerCoroutine` for their actions.
    d.  The `ServerSpellcardActionRunner` (running either `RunSpellcardActions` or `ExecuteSingleSpellcardActionFromServerCoroutine`) executes individual `SpellcardAction`s:
        *   Handles delays (`startDelay`, `intraActionDelay`).
        *   Spawns `NetworkObject` bullets on the server over time (using `ServerPooledSpawner` if applicable), respecting `skipEveryNth`.
        *   Configures bullet behavior via `ServerBulletConfigurer.ConfigureBulletBehavior`.
6.  Netcode handles synchronizing spawned illusions and bullets to clients.

## Level 4 Spellcards (Illusion System)

Level 4 spellcards represent a character's ultimate attack and differ significantly from Level 2/3 spellcards. Instead of just spawning a sequence of bullet patterns directly, activating a Level 4 spellcard spawns a persistent **Illusion** of the casting character onto the opponent's screen.

*   **Illusion Prefab:** Defined in `Level4SpellcardData`. Must have `NetworkObject`, `NetworkTransform`, `Level4IllusionController`, and `IllusionHealth` components. Must be registered in NetworkManager prefabs.
*   **Duration & Health:** Configured in `Level4SpellcardData`. The illusion can be destroyed prematurely if the opponent shoots it down (`IllusionHealth.cs`).
*   **Movement:** The `Level4IllusionController` script on the server moves the illusion periodically to random locations within a defined area at the top of the opponent's playfield.
*   **Attacks:** After each random movement, the `Level4IllusionController` randomly selects `Attacks Per Move` `CompositeAttackPattern`(s) from the `Attack Pool`.
*   **Composite Attack Patterns:** Defined in `Level4SpellcardData`, these group multiple `SpellcardAction`s into a single attack unit.
    *   **Pattern Aiming:** `orientPatternTowardsTarget` flag aims the whole pattern.
    *   **Movement During Attack:** If `performMovementDuringAttack` is true, the illusion performs a short, dynamically directed horizontal movement while executing the pattern. The distance is based on the magnitude of `attackMovementVector.x`, duration by `attackMovementDuration`. Direction is towards the center if near an edge, away from center otherwise.
    *   **Action Delays:** `startDelay` within actions is relative to the composite pattern start.
*   **Cancellation:** Activating a Level 4 spellcard immediately despawns any active illusion targeting the caster or previously cast by the caster.

## Adding a New Spellcard

Follow these steps to create and integrate a new spellcard:

### Levels 2 & 3:

1.  **Create Bullet Prefabs (if needed):** Ensure prefabs have `NetworkObject`, `NetworkTransform`, required behavior scripts, and `NetworkBulletLifetime`.
2.  **Create Spellcard Asset:** Right-click -> `Create` -> `TouhouWebArena` -> `Spellcard Data` in `Assets/Resources/Spellcards/`.
3.  **Rename Asset:** `CharacterNameLevelXSpellcard` (e.g., `HakureiReimuLevel3Spellcard`).
4.  **Configure Data:** Select the asset. Add `SpellcardAction`s to the `Actions` list and configure each action's parameters (prefabs, count, formation, behavior, timing, etc.). Use the fields described in the "Data Structures" section below.
5.  **Integration:** Automatic via naming convention.
6.  **Test:** Verify pattern execution and cost deduction.

### Level 4:

1.  **Create Illusion Prefab:** Create the illusion prefab with `NetworkObject`, `NetworkTransform`, `Level4IllusionController`, and `IllusionHealth`. Add it to NetworkManager's registered prefabs list.
2.  **Create Bullet Prefabs (if needed):** Create bullets for the illusion's attacks (with standard components).
3.  **Create Spellcard Asset:** Right-click -> `Create` -> `TouhouWebArena` -> `Level 4 Spellcard Data` in `Assets/Resources/Spellcards/`.
4.  **Rename Asset:** `CharacterNameLevel4Spellcard` (e.g., `KirisameMarisaLevel4Spellcard`).
5.  **Configure Data:** Select the asset.
    *   Assign the **Illusion Prefab**.
    *   Set **Duration**, **Health**.
    *   Configure **Movement** parameters.
    *   Configure the **Attack Pool** list with one or more `CompositeAttackPattern`s.
    *   For each `CompositeAttackPattern`:
        *   Set **Pattern Name**, **Orient Pattern Towards Target**.
        *   Configure **Movement During Attack** (`performMovementDuringAttack`, `attackMovementVector.x` magnitude for distance, `attackMovementDuration`).
        *   Add `SpellcardAction`s to the inner **Actions** list.
        *   Configure each inner `SpellcardAction` using the fields described below.
    *   Set **Attacks Per Move**.
6.  **Integration:** Automatic via naming convention.
7.  **Test:** Verify illusion spawning, movement, attacks, duration, health, and cancellation logic.

## Character-Specific Implementations

For detailed examples of how specific characters' spellcards are configured using these systems, see the following documents:

*   [Hakurei Reimu Spellcard Details](./hakureiReimu.md)
*   [Kirisame Marisa Spellcard Details](./kirisameMarisa.md)
*   *(Add links for new characters here)*

## Data Structures

### `Level4SpellcardData` (ScriptableObject):

*   **`Illusion Prefab` (GameObject):** The prefab for the illusion itself. Needs `NetworkObject`, `NetworkTransform`, `Level4IllusionController`, `IllusionHealth`. Must be registered Network Prefab.
*   **`Duration` (Float):** How long the illusion lasts by default.
*   **`Health` (Float):** Starting health of the illusion.
*   **`Movement Area Height` (Float):** Vertical range for random movement below the top bound.
*   **`Min Move Delay` (Float):** Minimum time between illusion's random repositioning moves.
*   **`Max Move Delay` (Float):** Maximum time between illusion's random repositioning moves.
*   **`Attack Pool` (List of `CompositeAttackPattern`):** Pool of potential attacks the illusion can perform after moving.
*   **`Attacks Per Move` (Int):** Number of patterns selected randomly from the pool per move cycle.

### `CompositeAttackPattern` (Serializable Class):

*   **`Pattern Name` (String):** Identifier for the Inspector.
*   **`Orient Pattern Towards Target` (Bool):** If true, the whole pattern (all actions) rotates to face the opponent when executed.
*   **`Perform Movement During Attack` (Bool):** If true, the illusion moves horizontally while this pattern executes.
*   **`Attack Movement Vector` (Vector2):** Only the **magnitude** of X is used for distance. Direction is determined automatically based on position relative to center. Y is ignored.
*   **`Attack Movement Duration` (Float):** Duration of the movement during the attack.
*   **`Actions` (List of `SpellcardAction`):** The sequence of actions making up this composite pattern.

### `SpellcardAction` (Serializable Class):

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
    *   **`Speed Increment Per Bullet` (Float):** (Line Only) Amount speed increases for each subsequent bullet in the line.
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