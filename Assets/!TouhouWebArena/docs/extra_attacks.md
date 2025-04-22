# Extra Attack Documentation

## Overview

Extra Attacks are character-specific special attacks that are triggered automatically under certain conditions during gameplay. They are distinct from regular shots, charge attacks, and manually activated spellcards. The system is server-authoritative.

## Triggering

*   **Condition:** An Extra Attack is triggered when a player destroys a specific "trigger" fairy.
*   **Marking Triggers:** The `FairySpawner.cs` script (found on `PlayerOneFairySpawner` / `PlayerTwoFairySpawner` prefabs) is configured with an `extraAttackTriggerWaveInterval`. Every Nth wave (where N is the interval), one or more fairies spawned in that wave are marked internally as `isExtraAttackTrigger = true`.
*   **Activation:** When a fairy marked as a trigger (`isExtraAttackTrigger == true`) is destroyed by a player (i.e., `killerRole` is not `None`), the `Fairy.cs` script detects this condition during its death sequence.
*   **Notification:** The dying trigger `Fairy` script calls `ExtraAttackManager.Instance.TriggerExtraAttackInternal()`, passing information about the player who killed it and the role of the opponent.

## Execution

*   **Coordinator:** The `ExtraAttackManager.cs` script (a NetworkBehaviour Singleton) orchestrates the execution.
*   **Character Specificity:** Inside `TriggerExtraAttackInternal()`, the `ExtraAttackManager` checks the character name of the *attacker* (the player who killed the trigger fairy).
*   **Prefab Selection:** Based on the attacker's character, it selects the corresponding Extra Attack prefab (`reimuExtraAttackPrefab` or `marisaExtraAttackPrefab`) assigned in its Inspector fields.
*   **Spawning:** The `ExtraAttackManager` instantiates the selected prefab. This happens on the server.
*   **Targeting & Behavior:** The spawned prefab likely targets the opponent's play area. It might use helper scripts like `ReimuExtraAttackOrbSpawner.cs` or `MarisaExtraAttackSpawner.cs` (if found in the scene) to determine exact spawn locations or patterns. The attack's behavior is contained within the scripts attached to the spawned Extra Attack prefab itself (e.g., `ReimuExtraAttackOrb.cs`).
*   **Networking:** Since `ExtraAttackManager` is a `NetworkBehaviour` and spawns NetworkObjects (presumably the Extra Attack prefabs have `NetworkObject` components), the attack's appearance and effects are synchronized from the server to the clients.

## Definition

*   **Prefabs:** Extra Attacks are defined primarily as unique **Prefabs**. Each character's Extra Attack has its own prefab (e.g., `ReimuExtraAttack.prefab`, `MarisaExtraAttack.prefab`).
*   **Assignment:** These character-specific prefabs are assigned directly to fields in the `ExtraAttackManager` script component (likely placed on a manager object in the scene).
*   **Behavior Scripts:** The logic, movement, and visual effects of the Extra Attack are implemented in scripts attached to the Extra Attack prefab itself (e.g., `ReimuExtraAttackOrb.cs`).
*   **Spawner Helpers (Optional):** Specific helper scripts like `ReimuExtraAttackOrbSpawner.cs` or `MarisaExtraAttackSpawner.cs` might exist in the scene to provide positional information or specialized spawning patterns for certain attacks, and are located and used by the `ExtraAttackManager`. 