# Extra Attack Documentation

## Overview

Extra Attacks are character-specific special attacks that are triggered automatically under certain conditions during gameplay. They are distinct from regular shots, charge attacks, and manually activated spellcards. The system is server-authoritative.

## Triggering

*   **Condition:** An Extra Attack is triggered when a player destroys a specific "trigger" fairy.
*   **Marking Triggers:** The `FairySpawner.cs` script marks specific fairies as triggers during the spawning process (e.g., by passing `isTrigger = true` to `FairyController.InitializeForPooling`).
*   **Activation:** When a trigger fairy dies, its `FairyController` coordinates the death sequence. The `FairyExtraAttackTrigger.cs` component (attached to the fairy prefab) is responsible for detecting if it's a trigger and initiating the Extra Attack.
*   **Notification:** The `FairyExtraAttackTrigger.TriggerExtraAttackIfApplicable()` method uses `PlayerDataManager` to find the attacker's data and determines the opponent role. It then calls `ExtraAttackManager.Instance.TriggerExtraAttackInternal()`, passing the necessary player data and opponent role.

## Execution

*   **Coordinator:** The `ExtraAttackManager.cs` script (a NetworkBehaviour Singleton) orchestrates the execution.
*   **Character Specificity:** Inside `TriggerExtraAttackInternal()`, the `ExtraAttackManager` checks the character name of the *attacker* (passed in via `PlayerData`).
*   **Prefab Selection:** Based on the attacker's character, it selects the corresponding Extra Attack prefab (`reimuExtraAttackPrefab` or `marisaExtraAttackPrefab`) assigned in its Inspector fields.
*   **Spawning:** The `ExtraAttackManager` instantiates the selected prefab on the server.
*   **Targeting & Behavior:** The spawned prefab likely targets the opponent's play area, potentially using helper spawner scripts for positioning. The attack's behavior is contained within the scripts attached to the spawned Extra Attack prefab itself.
*   **Networking:** Extra Attack prefabs are NetworkObjects, ensuring synchronization.

## Definition

*   **Prefabs:** Character-specific Extra Attack prefabs (e.g., `ReimuExtraAttack.prefab`).
*   **Assignment:** Prefabs assigned to `ExtraAttackManager` component fields.
*   **Behavior Scripts:** Logic attached to the Extra Attack prefabs.
*   **Spawner Helpers (Optional):** Scene scripts providing positioning/patterns. 