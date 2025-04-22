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
    *   Spawned periodically within designated spawn zones for each player (`player1SpawnZoneRef`, `player2SpawnZoneRef` likely referenced in the spawning script). The exact script triggering periodic spirit spawns needs identification (possibly a Game Manager or dedicated spawner).
    *   Revenge Spirits: When a player destroys one of their *own* normal (unactivated) spirits, a new "revenge" spirit is spawned on the *opponent's* side. This is handled server-side within `SpiritController.cs`.
    *   The `SpiritRegistry.cs` tracks active spirits per side and enforces a maximum count (`maxSpiritsPerSide`).

## Enemy Types & Behavior

*   **Fairies (`NormalFairy`, `GreatFairy` prefabs, using `Fairy.cs`, `SplineWalker.cs`):**
    *   **Movement:** Follow fixed spline paths.
    *   **Interaction:** Can be destroyed by player shots.
    *   **On-Death Effect:** When destroyed by a player, sends bullets to the opponent's side (details of bullet type/pattern TBD). Specific fairies (`GreatFairy`?) trigger chain reactions (`DelayedActionProcessor.cs`, `FairyRegistry.cs`) or Extra Attacks (configured in `FairySpawner`).
*   **Spirits (`Spirit` prefab, using `SpiritController.cs`):**
    *   **States:** Exist in an `inactive` state initially. Become `activated` when touched by the player's Scope Style (Shift key). Activation state (`isActivated`) is a `NetworkVariable` managed by the server.
    *   **Movement:** Move generally downwards (or according to specific logic within `SpiritController`).
    *   **Interaction:**
        *   Can be destroyed by player shots.
        *   Deal 1 damage to the player on contact, regardless of activation state (`inactive` or `activated`).
        *   Can be cleared by player death bombs (`IClearableByBomb` interface, handled in `SpiritController.cs` on the server).
    *   **On-Death Effect:**
        *   If killed by the *owner* while `inactive`, spawns a "revenge" spirit (inactive) on the *opponent's* side.
        *   If killed by the *opponent*, or killed while `activated` (by owner or opponent), it spawns an inactive spirit on the *opponent's* side. (Essentially, killing a spirit always sends an inactive spirit to the opponent, except when cleared by a bomb).

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