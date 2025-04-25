# Enemy System Documentation

## Overview

The enemy system manages the non-player entities that populate the playfield: Fairies (`NormalFairy`, `GreatFairy`) and Spirits (`Spirit`). These enemies serve as the primary source of interaction between players, as defeating them sends attacks to the opponent. The system is server-authoritative.

## Spawning

Enemy spawning is controlled entirely by the server.

*   **Fairies:**
    *   Spawned via specific spawner objects configured per player side. Look for prefabs named `PlayerOneFairySpawner` and `PlayerTwoFairySpawner` (likely located in `Assets/!TouhouWebProject/Prefabs/System/`).
    *   These spawner prefabs contain a `FairySpawner` component script which defines the waves, sequences, timing delays between lines, references to the `NormalFairy` and `GreatFairy` prefabs, and specifies which fairies trigger Extra Attacks.
    *   Fairies follow predefined paths using the `SplineWalker.cs` component, which gets path data synchronized from the server.
    *   The `FairyRegistry.cs` likely tracks active fairies.
*   **Spirits:**
    *   Spawned periodically within designated spawn zones for each player (`player1SpawnZoneRef`, `player2SpawnZoneRef` likely referenced in `SpiritSpawner.cs`).
    *   Simplified Spawning: When a spirit is destroyed (by player shot, timeout, or clear effect), a new inactive spirit is immediately spawned on the *opponent's* side. This is handled server-side within `SpiritController.cs` -> `Die()` method.
    *   The `SpiritRegistry.cs` tracks active spirits per side and enforces a maximum count (`maxSpiritsPerSide`).

## Enemy Types & Behavior

*   **Fairies (`NormalFairy`, `GreatFairy` prefabs, using `Fairy.cs`, `SplineWalker.cs`):**
    *   **Movement:** Follow fixed spline paths.
    *   **Interaction:** 
        *   Can be destroyed by player shots.
        *   Implement the `IClearable` interface and are **always** cleared by bombs or shockwaves (see Clearing Effects section below).
    *   **On-Death Effect:** 
        *   When destroyed by a player (or a player-attributed clearing effect), sends bullets to the opponent's side.
        *   Triggers chain reactions (`DelayedActionProcessor.cs`, `FairyRegistry.cs`) or Extra Attacks (configured in `FairySpawner`).
    *   **Path End:** When a fairy reaches the end of its spline path, it is quietly despawned and returned to the object pool **without** triggering chain reaction effects (like shockwaves or killing the next fairy). This ensures smooth offscreen transitions.

*   **Spirits (`Spirit` prefab, using `SpiritController.cs`):**
    *   **States:** Exist in an `inactive` state initially. Become `activated` when touched by the player's Scope Style (Shift key). Activation state (`isActivated`) is a `NetworkVariable` managed by the server.
    *   **Movement:** Move generally downwards (or according to specific logic within `SpiritController`).
    *   **Interaction:**
        *   Can be destroyed by player shots (regardless of owner).
        *   Deal 1 damage to the player on contact, regardless of activation state (`inactive` or `activated`).
        *   Implement the `IClearable` interface and are **always** cleared by bombs or shockwaves (if the effect source matches the spirit's side - see Clearing Effects section below).
    *   **On-Death Effect:**
        *   Simplified: When a spirit is destroyed for any reason (player shot, timeout, clearing effect), it spawns an inactive spirit on the *opponent's* side.

## Clearing Effects (`IClearable` Interface)

Enemies and certain bullets implement the `IClearable` interface, allowing them to be removed by area effects.

*   **Interface:** `IClearable` defines a `Clear(bool forceClear, PlayerRole sourceRole)` method.
*   **Enemy Implementation:** Both `Fairy.cs` and `SpiritController.cs` implement `IClearable`. Their `Clear` method triggers their respective `Die` sequence, regardless of the `forceClear` flag.
    *   Both `Fairy` and `SpiritController` provide a `public PlayerRole GetOwnerRole()` method.
*   **Triggers:**
    *   **Player Death Bomb:** Uses `Physics2D.OverlapCircleAll`. Checks the `ownerRole` of the detected `IClearable` enemy (`Fairy.GetOwnerRole()`, `SpiritController.GetOwnerRole()`). Only calls `Clear(true, ...)` if the enemy's `ownerRole` matches the role of the player who died.
    *   **Fairy Shockwave:** Uses trigger colliders (`OnTriggerEnter2D`) and calls `Clear(false, ...)` on detected `IClearable` objects (a normal clear). Shockwaves typically only collide with objects on the same side due to physics layers/positioning, but the `Clear` method itself doesn't perform an owner check for shockwaves.

## Data Structure / Definition

*   **Prefabs:** Enemies are defined as Unity Prefabs:
    *   `NormalFairy`
    *   `GreatFairy`
    *   `Spirit`
    *   These prefabs contain the necessary scripts (`Fairy.cs` or `SpiritController.cs`), `NetworkObject` component, colliders, and visual components.
*   **Spawner Configuration:** Fairy waves and sequences are defined within the `FairySpawner` component on the `PlayerOneFairySpawner` and `PlayerTwoFairySpawner` prefabs.
*   **Scripts:**
    *   `Fairy.cs` / `SpiritController.cs`: Contain the core logic, health, and interaction rules. These are `NetworkBehaviours`.
    *   `SplineWalker.cs`: Attached to Fairy prefabs for path following.
    *   `FairySpawner.cs`: (Component on spawner prefabs) Defines fairy wave data.
*   **Registries:** `FairyRegistry.cs` and `SpiritRegistry.cs` are likely server-side singletons used for tracking and managing active enemies. 