# Health & Damage System Documentation

## Overview

This document describes how health is managed and damage is processed. Player health is server-authoritative. Each hit on a player decrements health, triggers a period of invincibility (with loss of control and visual flashing), and culminates in a "death bomb" effect that clears nearby bullets and enemies. Enemy health and damage are primarily client-side.

## Health Components

*   **`PlayerHealth.cs` (Server-Authoritative):** Attached to the player prefab. Manages `CurrentHealth` (`NetworkVariable<int>`) and `IsInvincible` (`NetworkVariable<bool>`).
*   **`ClientFairyHealth.cs` (Client-Side):** Attached to Fairy prefabs. Manages local health and death effects.
*   **`ClientSpiritHealth.cs` (Client-Side - Future):** For Spirit prefabs.

## Taking Damage & Invincibility Sequence (Player)

1.  **Hit Detection (Client):** `PlayerHitbox.OnTriggerEnter2D` on the owning client detects a collision with an enemy projectile (by layer) or enemy body (by tag).
2.  **Report Hit (Client to Server):** `PlayerHitbox` calls `ReportHitToServerRpc()`.
3.  **Damage & Invincibility Trigger (Server):** The `ReportHitToServerRpc` on the server:
    *   Checks `PlayerHealth.IsInvincible.Value`. If true, no action.
    *   Otherwise, calls `playerHealth.TakeDamage(1)`.
    *   `PlayerHealth.TakeDamage(1)`: 
        *   Decrements `CurrentHealth.Value`.
        *   If `CurrentHealth.Value` > 0 (or if HP is locked for debug), it calls `TriggerInvincibilityServer()`.
        *   If `CurrentHealth.Value` <= 0 (actual final death), it calls `HandleDeathServer()` (which invokes `OnPlayerDeathServer` for game/round managers).
4.  **Invincibility Period (Server & Client):**
    *   `PlayerHealth.TriggerInvincibilityServer()` starts `ServerInvincibilityTimerCoroutine()`.
    *   This coroutine immediately sets `IsInvincible.Value = true` (synced to all clients).
    *   **Client Reaction (`PlayerInvincibilityVisuals.cs`):**
        *   Subscribes to `playerHealth.IsInvincible.OnValueChanged`.
        *   When `IsInvincible` becomes `true`:
            *   Starts visual flashing.
            *   If on the owning client, it gets `ClientAuthMovement` and sets `IsMovementLocked = true` (player stops, input ignored).
    *   **Server Waits:** `ServerInvincibilityTimerCoroutine` waits for `characterStats.GetInvincibilityDuration()`.
5.  **"Death Bomb" Execution (Server to Clients):**
    *   After invincibility duration, `ServerInvincibilityTimerCoroutine` calls `playerDeathBomb.ExecuteBomb()`.
    *   `PlayerDeathBomb.ExecuteBomb()` (server) calls `ClearObjectsInRadiusClientRpc(center, radius, bomberRole, bomberClientId)` targeting all clients.
6.  **Client-Side Object Clearing (`PlayerDeathBomb.ClearObjectsInRadiusClientRpc`):**
    *   Each client iterates active objects from `ClientGameObjectPool`.
    *   Bullets (`StageSmallBulletMoverScript`) in radius are returned to pool (`ForceReturnToPoolByBomb()`).
    *   Enemies (`ClientFairyHealth`) in radius are damaged (`TakeDamage(BOMB_DAMAGE, bomberClientId)`), likely killing them.
7.  **End Invincibility (Server & Client):**
    *   After `ExecuteBomb()`, `ServerInvincibilityTimerCoroutine` sets `IsInvincible.Value = false`.
    *   **Client Reaction (`PlayerInvincibilityVisuals.cs`):**
        *   Stops visual flashing.
        *   If on the owning client, sets `ClientAuthMovement.IsMovementLocked = false` (player regains control).

## Enemies Taking Damage (Client-Side Simulation)

*   Collision detected by player bullets (`BulletMovement`) or fairy shockwaves (`ClientFairyShockwave`) on any client.
*   Damage applied via `ClientFairyHealth.TakeDamage(damage, attackerId)`.
*   If local player kills an enemy, `ClientFairyHealth` calls `PlayerAttackRelay.ReportFairyKillServerRpc()`.

## Final Death Handling (Player)

*   When `PlayerHealth.CurrentHealth` on the server reaches zero (and is not locked), `HandleDeathServer()` is called.
*   This invokes the static server-event `PlayerHealth.OnPlayerDeathServer`, passing the `OwnerClientId`.
*   Game managers (e.g., `RoundManager`) subscribe to this event to handle round logic, score updates, and potentially respawn sequences or game over states.
*   The "death bomb" from the final hit will have already triggered via the invincibility sequence.

## Key Scripts

*   **Player Health & Damage Cycle:**
    *   `PlayerHealth.cs`: Server-authoritative for health/invincibility. Manages `CurrentHealth`, `IsInvincible`, `TakeDamage()`, `TriggerInvincibilityServer()`, `HandleDeathServer()`.
    *   `PlayerHitbox.cs`: Client-side collision, calls `ReportHitToServerRpc`.
    *   `PlayerInvincibilityVisuals.cs`: Client-side visuals, locks/unlocks owner's `ClientAuthMovement`.
    *   `ClientAuthMovement.cs`: Respects `IsMovementLocked` to halt player input/movement.
    *   `PlayerDeathBomb.cs`: Server-side `ExecuteBomb()` called after invincibility. Sends `ClearObjectsInRadiusClientRpc` to clients, which handle local clearing of bullets and enemies.
    *   `CharacterStats.cs`: Provides invincibility duration, bomb radius, health values.
*   **Enemy Health & Damage (Primarily Client-Side Simulation):**
    *   `ClientFairyHealth.cs`: Manages fairy health, spawns shockwave, reports local player kills.
    *   `BulletMovement.cs`: Deals damage to `ClientFairyHealth`.
    *   `ClientFairyShockwave.cs`: Deals damage to `ClientFairyHealth`.
*   **Networking Support & Managers:**
    *   `PlayerAttackRelay.cs`: Receives enemy kill reports.
    *   `EffectNetworkHandler.cs`: (Not directly used by `PlayerDeathBomb` for its RPC, but for other effects).
    *   `PlayerDataManager.cs`, `ClientGameObjectPool.cs`. 