# Health & Damage System Documentation

## Overview
_(Describe how health is managed for players and potentially enemies, and how damage is applied and processed, focusing on the server-authoritative nature)._

## Health Components
*   **`PlayerHealth.cs`:** _(Detail this script: attached to player prefab, contains `CurrentHealth` NetworkVariable, `OnHealthChanged` event)._
*   **Fairy Health:** Managed by the `FairyHealth.cs` component attached to Fairy prefabs. Contains `currentHealth` NetworkVariable and specific health values (`initialMaxHealth`, `isGreatFairy`). Triggers an `OnDeath` event when health reaches zero.
*   **Spirit Health:** Managed directly within `SpiritController.cs` via the `currentHp` NetworkVariable. Has different max HP values based on its `isActivated` state.

## Taking Damage
*   **Collision Detection:** _(Explain how collisions causing damage are detected, e.g., server-side `OnTriggerEnter2D` or `OnCollisionEnter2D`, checking tags/layers)._
*   **Damage Application:** 
    *   **Player:** `PlayerHealth.TakeDamage()`.
    *   **Fairy:** Damage is applied server-side via methods on `FairyHealth.cs` (e.g., `ApplyDamageFromServer`, `ApplyDamageFromRpc`).
    *   **Spirit:** Damage is applied server-side via `SpiritController.ApplyDamageServer()`.
*   **Server Authority:** Damage calculation and health updates occur only on the server.

## Invincibility Frames (Player)
_(Explain how invincibility frames work after a player takes damage: Triggering condition, duration (`invincibilityDuration` from `CharacterStats`), visual feedback, effect on collision checks)._

## Death Handling
*   **Player Death:** _(Describe what happens when player `CurrentHealth` reaches zero: Round end triggered, potential death animation/sound, death bomb activation)._
*   **Enemy Death:** 
    *   **Fairy:** `FairyHealth.OnDeath` event triggers `FairyController.HandleDeath` sequence (effects, chain reaction, extra attack, pooling).
    *   **Spirit:** `SpiritController.Die` sequence (effects, revenge spawn, pooling).
*   **Death Bomb:** _(Explain the player death bomb effect: Trigger condition (player death), radius (`deathBombRadius` from `CharacterStats`), effect (clearing projectiles implementing `IClearableByBomb`))._

## Key Scripts
_(List scripts involved: `PlayerHealth.cs`, `CharacterStats.cs`, `FairyHealth.cs`, `SpiritController.cs`, collision handling scripts, `PlayerDeathBomb.cs` (if exists))._ 