# Extra Attack Documentation

## Overview

Extra Attacks are character-specific special attacks that are triggered automatically under certain conditions during gameplay. They are distinct from regular shots, charge attacks, and manually activated spellcards. The system for fairy-triggered extra attacks is assumed to be server-authoritative until refactored. The player death bomb is another automatic effect.

## Fairy-Triggered Extra Attacks (Assumed Server-Authoritative)

*   **Condition:** An Extra Attack is triggered when a player destroys a specific "trigger" fairy.
*   **Marking Triggers:** The `FairySpawner.cs` script marks specific fairies as triggers during the spawning process (e.g., by passing `isTrigger = true` to what would have been `FairyController.InitializeForPooling` in a server-auth model. How this is communicated to the client-side `ClientFairyHealth` for reporting needs review if this system is kept).
*   **Activation:** When a trigger fairy dies, if its `ClientFairyHealth` reports the kill to the server, the server-side logic (perhaps in `PlayerAttackRelay` or a dedicated handler) would need to check if it was a trigger fairy.
*   **Notification:** If a trigger fairy kill is confirmed, the server would call `ExtraAttackManager.Instance.TriggerExtraAttackInternal()` (or a similar refactored method), passing necessary player data.

*   **Coordinator:** The `ExtraAttackManager.cs` script (a NetworkBehaviour Singleton) orchestrates the execution.
*   **Character Specificity:** Inside `TriggerExtraAttackInternal()`, the `ExtraAttackManager` checks the character name of the *attacker*.
*   **Prefab Selection:** Based on the attacker's character, it selects the corresponding Extra Attack prefab.
*   **Spawning:** The `ExtraAttackManager` likely instantiates the selected prefab on the server as a NetworkObject, or sends an RPC to clients to spawn a specific client-simulated effect sequence.
*   **Targeting & Behavior:** The attack's specific behavior. If server-spawned, it uses server-authoritative collision/damage. If client-simulated via RPC, the client handles visuals and reports impacts if necessary.

## Player Death Bomb (Server-Initiated, Client-Side Effect)

This is an automatic effect triggered after a player takes a hit.

*   **Trigger:** Player is hit, `PlayerHitbox` sends RPC, `PlayerHealth.TakeDamage()` is called on the server. If the player survives the hit, `PlayerHealth` starts the invincibility sequence (`TriggerInvincibilityServer()` -> `ServerInvincibilityTimerCoroutine`).
*   **Server Action:** After the invincibility duration (`CharacterStats.GetInvincibilityDuration()`) completes in `ServerInvincibilityTimerCoroutine`, the server calls `PlayerDeathBomb.ExecuteBomb()` (located on the player object).
*   **Effect Command:** `PlayerDeathBomb.ExecuteBomb()` calls its own `ClearObjectsInRadiusClientRpc(bombPosition, bombRadius, bombingPlayerRole, bombingPlayerClientId)` targeting all clients.
    *   `bombPosition` is the player's position at the time the bomb executes.
    *   `bombRadius` is read from `CharacterStats.deathBombRadius`.
    *   `bombingPlayerClientId` identifies the player whose bomb it is.
*   **Client-Side Clearing Execution:**
    *   All clients receive the `ClearObjectsInRadiusClientRpc`.
    *   Each client iterates through its `ClientGameObjectPool.Instance.GetAllActiveObjects()`.
    *   For each active pooled object, it checks if it's within the `bombRadius` of `bombPosition`.
    *   If the object is a bullet (e.g., has `StageSmallBulletMoverScript`), `ForceReturnToPoolByBomb()` is called.
    *   If the object is an enemy (e.g., has `ClientFairyHealth` and `IsAlive`), `TakeDamage(BOMB_DAMAGE_TO_ENEMIES, bombingPlayerClientId)` is called (likely killing it).
    *   (Logic for `ClientSpiritHealth` will be added when refactored).

## Definition

*   **Prefabs (Fairy Extra Attacks):** Character-specific Extra Attack prefabs (e.g., `ReimuExtraAttack.prefab`). Their nature (NetworkObject vs. client-simulated effect) depends on implementation if refactored.
*   **Assignment (Fairy Extra Attacks):** Prefabs assigned to `ExtraAttackManager` component fields.
*   **Behavior Scripts (Fairy Extra Attacks):** Logic attached to the Extra Attack prefabs or defining client-side simulation sequences.
*   **Radius (Death Bomb):** `CharacterStats.deathBombRadius`.

## Execution

*   **Coordinator:** The `ExtraAttackManager.cs` script (a NetworkBehaviour Singleton) orchestrates the execution.
*   **Character Specificity:** Inside `TriggerExtraAttackInternal()`, the `ExtraAttackManager` checks the character name of the *attacker* (passed in via `PlayerData`).
*   **Prefab Selection:** Based on the attacker's character, it selects the corresponding Extra Attack prefab (`reimuExtraAttackPrefab` or `marisaExtraAttackPrefab`) assigned in its Inspector fields.
*   **Spawning:** The `ExtraAttackManager` instantiates the selected prefab on the server.
*   **Targeting & Behavior:** The spawned prefab likely targets the opponent's play area, potentially using helper spawner scripts for positioning. The attack's specific behavior, including **server-authoritative collision detection and damage application**, is contained within the scripts attached to the spawned Extra Attack prefab itself (e.g., `ReimuExtraAttackOrb.cs`, `EarthlightRay.cs`).
*   **Networking:** Extra Attack prefabs are NetworkObjects, ensuring their state is synchronized. The core interaction logic (collision/damage) should execute on the server.

## Definition

*   **Prefabs:** Character-specific Extra Attack prefabs (e.g., `ReimuExtraAttack.prefab`).
*   **Assignment:** Prefabs assigned to `ExtraAttackManager` component fields.
*   **Behavior Scripts:** Logic attached to the Extra Attack prefabs.
*   **Spawner Helpers (Optional):** Scene scripts providing positioning/patterns. 