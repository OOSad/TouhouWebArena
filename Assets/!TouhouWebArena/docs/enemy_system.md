# Enemy System Documentation

## Overview

The enemy system manages the non-player entities that populate the playfield: Fairies (`NormalFairy`, `GreatFairy`) and Spirits (`Spirit`). These enemies serve as the primary source of interaction between players, as defeating them sends attacks to the opponent. The system is server-authoritative.

## Spawning

Enemy spawning is controlled entirely by the server.

*   **Fairies:**
    *   Spawned via specific spawner objects configured per player side (`PlayerOneFairySpawner`, `PlayerTwoFairySpawner`).
    *   These spawner prefabs contain a `FairySpawner` component script which defines waves, sequences, timing delays, references to `NormalFairy` / `GreatFairy` prefabs, and which fairies trigger Extra Attacks.
    *   Fairies follow predefined paths using the `SplineWalker.cs` component, with path data synced from the server via `FairyPathInitializer.cs`.
    *   The `FairyRegistry.cs` tracks active fairies.
*   **Spirits:**
    *   Spawned periodically within designated spawn zones by `SpiritSpawner.cs`.
    *   Revenge Spawn: Handled by `SpiritController.cs` -> `Die()`, which calls `SpiritSpawner.SpawnRevengeSpirit()` to spawn on the opponent's side.
    *   The `SpiritRegistry.cs` tracks active spirits per side and enforces a maximum count.

## Enemy Types & Behavior

*   **Fairies (`NormalFairy`, `GreatFairy` prefabs):**
    *   **Structure:** Fairy behavior is now distributed across multiple components attached to the prefab, coordinated by `FairyController.cs`.
        *   `FairyController.cs`: Coordinator, manages line/owner info, pooling setup, death sequence coordination. Implements `IClearable` (delegates to `FairyHealth`).
        *   `FairyHealth.cs`: Manages the fairy's `NetworkVariable` health, handles damage application, and triggers an `OnDeath` event.
        *   `SplineWalker.cs`: Handles movement along a defined path.
        *   `FairyPathInitializer.cs`: Initializes the `SplineWalker` with path data from the server.
        *   `FairyCollisionHandler.cs`: Handles physics trigger detection (e.g., with player shots) and checks `FairyHealth.IsAlive()`.
        *   `FairyDeathEffects.cs`: Spawns visual/audio effects upon death.
        *   `FairyChainReactionHandler.cs`: Manages the chain reaction logic (spawning `DelayedActionProcessor`, triggering opponent bullets).
        *   `DelayedActionProcessor.cs`: (Utility script/prefab) Handles the delay between chain reaction steps.
        *   `FairyExtraAttackTrigger.cs`: Checks if the fairy should trigger an Extra Attack on death and calls `ExtraAttackManager`.
    *   **Interaction:**
        *   Can be destroyed by player shots (damage handled by `FairyHealth`, collision by `FairyCollisionHandler`).
        *   Always cleared by bombs/shockwaves (via `IClearable` -> `FairyHealth.ApplyLethalDamage`).
    *   **On-Death Effect (Coordinated by `FairyController.HandleDeath`):**
        *   Death effects triggered by `FairyDeathEffects`.
        *   Chain reactions triggered by `FairyChainReactionHandler`.
        *   Extra Attacks triggered by `FairyExtraAttackTrigger`.
    *   **Path End:** When a fairy reaches the end of its path (`SplineWalker` reports to `FairyController`), the `FairyController.ReportEndOfPath` method is called **on the server**. This method now **silently returns the fairy's NetworkObject to the pool** using `NetworkObjectPool.Instance.ReturnNetworkObject`. It **no longer triggers death effects or chain reactions**.

*   **Spirits (`Spirit` prefab, coordinated by `SpiritController.cs`):**
    *   Structure: Uses a component-based approach similar to Fairies (e.g., `SpiritVisualController`, `SpiritDeathEffects`, `SpiritTimeoutAttack`).
    *   States: `inactive` / `activated`.
    *   Movement: Managed by `SpiritController` using `Rigidbody2D`.
    *   Interaction:
        *   Destroyed by player shots.
        *   Damage player on contact.
        *   Cleared by bombs/shockwaves (via `IClearable` -> `SpiritController.Die`).
    *   On-Death Effect: `SpiritController.Die` handles spawning a revenge spirit via `SpiritSpawner` and returning the object to the pool.

## Clearing Effects (`IClearable` Interface)

Enemies and certain bullets implement the `IClearable` interface.

*   **Interface:** `IClearable` defines `Clear(bool forceClear, PlayerRole sourceRole)`.
*   **Implementation:**
    *   `FairyController`: Implements `IClearable`, calls `FairyHealth.ApplyLethalDamage(sourceRole)`.
    *   `SpiritController`: Implements `IClearable`, calls its own `Die(sourceRole)` method.
*   **Triggers:** (Bomb/Shockwave logic likely remains the same, checking owner role before calling `Clear`).

## Data Structure / Definition

*   **Prefabs:** Enemies (`NormalFairy`, `GreatFairy`, `Spirit`) contain multiple component scripts, `NetworkObject`, colliders, visuals.
*   **Spawner Configuration:** `FairySpawner` component defines fairy waves.
*   **Scripts:** Core logic is now spread across coordinator scripts (`FairyController`, `SpiritController`) and specialized behavior components (`FairyHealth`, `FairyChainReactionHandler`, `SpiritVisualController`, etc.).
*   **Registries:** `FairyRegistry.cs`, `SpiritRegistry.cs` track active enemies.