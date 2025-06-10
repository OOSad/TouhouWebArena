# Health & Damage System Documentation

## Overview

This document describes how health is managed and damage is processed. Player health is server-authoritative. Each hit on a player decrements health, triggers a period of invincibility (with loss of control and visual flashing), and culminates in a "death bomb" effect that clears nearby bullets and enemies. Enemy health and damage are primarily client-side.

## Health Components

*   **`PlayerHealth.cs` (Server-Authoritative):** Attached to the player prefab. Manages `CurrentHealth` (`NetworkVariable<int>`) and `IsInvincible` (`NetworkVariable<bool>`).
*   **`ClientFairyHealth.cs` (Client-Side):** Attached to Fairy prefabs. Manages local health and death effects.
*   **`IllusionHealth.cs` (`NetworkBehaviour`):** Attached to Illusion prefabs (which are `NetworkObject`s).
    *   Manages health for Level 4 Illusions.
    *   **Client-Side:** Stores max health. A specific client (`_isResponsibleClient`, set via `ClientIllusionView.InitializeClientRpc`) handles hit detection in `OnTriggerEnter2D` against "PlayerShot" layer/tag. If hit, calls `TakeDamageClientSide(damage)`. If health depleted, calls `ReportDeathToServerRpc()`.
    *   **Server-Side:** Receives `ReportDeathToServerRpc`. This RPC then calls a public method `ProcessClientDeathReport()` on the illusion's `ServerIllusionOrchestrator` component, which then handles the despawn.
*   **`ClientSpiritHealth.cs` (Client-Side):** Attached to Spirit prefabs. Manages local health, death effects (shockwave), and reporting kills for client-simulated spirits.
    *   **Initialization:** `Initialize(spiritType)` sets HP to `NORMAL_SPIRIT_HP` (e.g., 5) and resets an internal `_isActivated` flag. Called by `ClientSpiritSpawnHandler` after retrieving the spirit from the pool.
    *   **Taking Damage:** `TakeDamage(amount, attackerOwnerClientId)` is called by other client-side systems (e.g., `BulletMovement.cs` when a player shot hits a spirit, or `PlayerDeathBomb.cs` when a spirit is caught in a bomb).
        *   It decrements the local health.
        *   If health drops to 0 or below, it calls the private `Die(attackerOwnerClientId)` method.
    *   **Activation:** `OnActivated()` is called by `ClientSpiritController.ActivateSpirit()` when a spirit enters a Scope Style zone.
        *   Sets HP to `ACTIVATED_SPIRIT_HP` (e.g., 1).
        *   Sets the internal `_isActivated` flag to true.
    *   **Death Sequence (`Die(attackerOwnerClientId)`):
        *   Calls `SpawnDeathShockwave(attackerOwnerClientId)` to create a visual/damaging effect.
            *   This method gets a shockwave prefab (e.g., "FairyShockwave") from `ClientGameObjectPool`.
            *   It initializes the `ClientFairyShockwave` script on it, providing damage, duration, and a radius. The `maxRadius` is chosen from `activatedSpiritShockwaveMaxRadius` if `_isActivated` was true, or `normalSpiritShockwaveMaxRadius` otherwise, allowing activated spirits to produce a larger shockwave.
        *   If the `attackerOwnerClientId` matches the `NetworkManager.Singleton.LocalClientId` (i.e., the local player killed the spirit), it calls `PlayerAttackRelay.LocalInstance.ReportSpiritKillServerRpc()` to inform the server, which may trigger a revenge spawn.
        *   Finally, it returns the spirit GameObject to `ClientGameObjectPool.Instance`.
    *   **Timeout Despawn:** `ForceReturnToPool()` is called by `ClientSpiritTimeoutAttack` when an activated spirit times out.
        *   This method returns the spirit GameObject directly to the pool *without* calling `Die()`, so no shockwave is produced, and no kill is reported for the timeout event itself.
*   **`ClientLilyWhiteHealth.cs` (Client-Side):** (New) Attached to the LilyWhite prefab. Manages local health for Lily White.
    *   **Responsibilities:** Manages Lily White's health (default 75), handles damage intake from player shots, triggers a visual damage flash, and notifies `ClientLilyWhiteController` upon death.
    *   **Initialization:** `Initialize()` is called by `ClientLilyWhiteController.Initialize()` after Lily White is retrieved from the pool. Sets current health to max health and resets sprite color.
    *   **Taking Damage:** `TakeDamage(amount, attackerOwnerClientId)` is called by `BulletMovement.cs` when a player shot (collider on "PlayerShot" layer/tag) collides with Lily White (collider on "LilyWhite" tag).
        *   It decrements local health.
        *   Triggers a visual flash effect (`_flashColor`, `_flashDuration`, `_flashIntensity`).
        *   If health drops to 0 or below, it calls `_lilyWhiteController.HandleDeath()` to initiate despawn.
    *   **Clear Despawn:** `ForceReturnToPoolByClear()` can be called by other systems (e.g., spellcard or bomb effects) if Lily White needs to be despawned instantly. It marks health as 0 and calls `_lilyWhiteController.HandleDeath()`.
    *   Unlike Fairies or Spirits, Lily White currently does not spawn a death shockwave or report her defeat to the server for any special game logic (e.g., scoring, revenge spawns).

## Taking Damage & Invincibility Sequence (Player)

1.  **Hit Detection (Client):** `PlayerHitbox.OnTriggerEnter2D` on the owning client detects a collision with an enemy projectile (by layer) or enemy body (by tag).
2.  **Report Hit (Client to Server):** `PlayerHitbox` calls `ReportHitToServerRpc()`.
3.  **Damage & Invincibility Trigger (Server):** The `ReportHitToServerRpc` on the server:
    *   Checks `PlayerHealth.IsInvincible.Value`. If true, no action.
    *   Otherwise, calls `playerHealth.TakeDamage(1)`.
    *   `PlayerHealth.TakeDamage(1)`: 
        *   Stores the `previousHealth` before modification.
        *   Decrements `CurrentHealth.Value`.
        *   **Near-Death Action Stop Trigger (Server):** If `applyHealthChange` is true (HP not locked) and either (`previousHealth > 1 && CurrentHealth.Value == 1`) OR (`previousHealth == 1 && CurrentHealth.Value <= 0`), the server calls `TriggerNearDeathActionStopClientRpc()` on `PlayerHealth.cs`. This RPC causes all clients to execute a brief game slowdown effect via `Time.timeScale` manipulation, managed by a coroutine in `PlayerHealth.cs`.
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

## Illusion Taking Damage & Death

Level 4 Illusions have their own health system, distinct from player invincibility mechanics. The process is as follows:

1.  **Initialization (Client-Side):**
    *   When an illusion is spawned, `ClientIllusionView.InitializeClientRpc` is called by `ServerIllusionOrchestrator`.
    *   This RPC provides initial data to the `ClientIllusionView`, which in turn passes the max health to its `IllusionHealth` component and sets a flag (`_isResponsibleClient`) on one specific client, making it responsible for reporting this illusion's death.

2.  **Hit Detection (Responsible Client):**
    *   A player's client-simulated spellcard bullet collides with an illusion's `Collider2D`.
    *   On the client designated as `_isResponsibleClient` for that illusion, `IllusionHealth.OnTriggerEnter2D()` is triggered.

3.  **Damage Application (Responsible Client):**
    *   `IllusionHealth` verifies the collision is with an object on the "PlayerShot" layer (or equivalent tag).
    *   It calls its own `TakeDamageClientSide(damageAmount)` method, which decrements its local health counter.

4.  **Report Death (Responsible Client to Server):**
    *   If `TakeDamageClientSide` results in the illusion's health dropping to 0 or below, the client-side `IllusionHealth` component calls `ReportDeathToServerRpc()`.

5.  **Server Receives Death Report:**
    *   The `[ServerRpc] ReportDeathToServerRpc` on the server-side instance of `IllusionHealth` (which exists on the server's version of the Illusion `NetworkObject`) is executed.

6.  **Notify Server Orchestrator (Server-Side Method Call):**
    *   The server-side `ReportDeathToServerRpc` method directly calls a public, non-RPC method `ProcessClientDeathReport()` on the `ServerIllusionOrchestrator` component that resides on the *same* Illusion GameObject.

7.  **Despawn Illusion (Server-Side on `ServerIllusionOrchestrator`):
    *   `ServerIllusionOrchestrator.ProcessClientDeathReport()` triggers the despawn sequence.
    *   This typically involves calling `ServerIllusionOrchestrator.DespawnIllusion()`, which:
        *   Notifies the `ServerIllusionManager.Instance.ServerNotifyIllusionDespawned(this)` (or similar method passing its NetworkObjectId or reference).
        *   Calls `NetworkObject.Despawn()` on the Illusion's NetworkObject, which removes the illusion from all clients and destroys the server object.

Illusions do not have an invincibility period or trigger a "death bomb" effect upon being destroyed. They are simply removed from play.

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
    *   `ClientSpiritHealth.cs`: Manages spirit health (normal/activated), damage processing, death sequence (shockwave with variable size, reporting kill to server), and forced despawn for timeouts.
    *   `ClientLilyWhiteHealth.cs`: (New) Manages Lily White's health. Player shots deal damage via `BulletMovement.cs`.
    *   `BulletMovement.cs`: Deals damage to `ClientFairyHealth`, `ClientSpiritHealth`, and now `ClientLilyWhiteHealth`.
    *   `ClientFairyShockwave.cs`: Deals damage to `ClientFairyHealth` (and its prefab is used by `ClientSpiritHealth` for its shockwave).
*   **Illusion Health & Damage Cycle:**
    *   `IllusionHealth.cs`: `NetworkBehaviour` for illusion health. Client detects hits, responsible client reports death via RPC. Server receives and tells `ServerIllusionOrchestrator` to despawn.
    *   `ClientIllusionView.cs`: Its `InitializeClientRpc` sets up `IllusionHealth` on clients.
    *   `ServerIllusionOrchestrator.cs`: Contains `ProcessClientDeathReport()` and `DespawnIllusion()`.
    *   `ServerIllusionManager.cs`: Notified by `ServerIllusionOrchestrator` upon despawn.
*   **Networking Support & Managers:**
    *   `PlayerAttackRelay.cs`: Receives enemy kill reports.
    *   `EffectNetworkHandler.cs`: (Not directly used by `PlayerDeathBomb` for its RPC, but for other effects).
    *   `PlayerDataManager.cs`, `ClientGameObjectPool.cs`.