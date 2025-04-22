# Health & Damage System Documentation

## Overview
_(Describe how health is managed for players and potentially enemies, and how damage is applied and processed, focusing on the server-authoritative nature)._

## Health Component
*   **`PlayerHealth.cs`:** _(Detail this script: attached to player prefab, contains `CurrentHealth` NetworkVariable, `OnHealthChanged` event)._
*   **Enemy Health:** _(Explain how enemy health is tracked, if they have health points - e.g., variables within `Fairy.cs` or `SpiritController.cs`)._

## Taking Damage
*   **Collision Detection:** _(Explain how collisions causing damage are detected, e.g., server-side `OnTriggerEnter2D` or `OnCollisionEnter2D`, checking tags/layers)._
*   **Damage Application:** _(Describe the process: which script calls the method to reduce health? Is there a standardized `TakeDamage` method? Is damage amount fixed or variable?)._
*   **Server Authority:** _(Reiterate that damage calculation and health updates occur only on the server)._

## Invincibility Frames (Player)
_(Explain how invincibility frames work after a player takes damage: Triggering condition, duration (`invincibilityDuration` from `CharacterStats`), visual feedback, effect on collision checks)._

## Death Handling
*   **Player Death:** _(Describe what happens when player `CurrentHealth` reaches zero: Round end triggered, potential death animation/sound, death bomb activation)._
*   **Enemy Death:** _(Describe enemy death sequences: Triggering on-death effects (sending bullets/spirits), notifying registries, returning to object pool)._
*   **Death Bomb:** _(Explain the player death bomb effect: Trigger condition (player death), radius (`deathBombRadius` from `CharacterStats`), effect (clearing projectiles implementing `IClearableByBomb`))._

## Key Scripts
_(List scripts involved: `PlayerHealth.cs`, `CharacterStats.cs`, collision handling scripts (e.g., on player or projectiles), `PlayerDeathBomb.cs` (if exists), enemy scripts)._ 