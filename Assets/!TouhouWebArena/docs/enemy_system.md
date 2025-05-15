# Enemy System Documentation

## Overview

The enemy system manages non-player entities: Fairies (`NormalFairy`, `GreatFairy`) and Spirits (`Spirit`). The system has been refactored to **Server-Authoritative Spawning + Client-Side Simulation**.

*   The server decides *what* enemies spawn and *when/where* (via path data for fairies).
*   Clients receive spawn commands via RPC and then locally instantiate, move, and manage the health/death of these enemies using `ClientGameObjectPool`.

## Spawning

### Fairies (`NormalFairy`, `GreatFairy`)

1.  **Server (`FairySpawner.cs`):**
    *   Defines waves, sequences, timings, enemy types (prefab IDs), and path data (e.g., from `PathManager`).
    *   For each wave, it constructs `FairyWaveData` (containing `FairySpawnData` structs: prefab ID, path ID, start delay, etc.).
    *   Calls `FairySpawnNetworkHandler.Instance.SpawnFairyWaveClientRpc(waveData)` sending this data to all clients.
2.  **All Clients (`FairySpawnNetworkHandler.SpawnFairyWaveClientRpc`):**
    *   Receive `waveData`.
    *   Iterate through `FairySpawnData` entries.
    *   For each entry:
        *   Get the enemy prefab (e.g., "NormalFairy") from `ClientGameObjectPool.Instance.GetObject(data.PrefabID)`.
        *   Retrieve the path from `PathManager.Instance.GetPath(data.PathID)`.
        *   Initialize `SplineWalker.InitializePath(path, data.StartDelay)`.
        *   Set initial position/rotation if needed, activate the GameObject.

### Spirits (`Spirit` prefab - Client-Side Simulated)

The Spirit system has been refactored to a server-triggered, client-simulated model. The server dictates when and where spirits should appear (and basic parameters), but the client handles their entire lifecycle, including movement, activation, attacks, and health.

1.  **Server-Side Trigger (`SpiritSpawner.cs`):**
    *   This server-only singleton MonoBehaviour is responsible for deciding when to spawn spirits. This can be:
        *   **Periodic Spawns:** At regular intervals (`spawnInterval`) in designated `spawnZone1` and `spawnZone2`.
        *   **Revenge Spawns:** When `PlayerAttackRelay` on the server reports a spirit kill (forwarded from a client), `SpiritSpawner.Instance.SpawnRevengeSpirit(targetPlayerRole)` is called.
    *   For both spawn types, `SpiritSpawner.cs` determines:
        *   `spiritPrefabID` (string, typically "Spirit").
        *   `spawnPosition` (Vector3).
        *   `shouldAim` (bool, based on `aimAtPlayerChance` for periodic spawns; also for revenge spawns now, not always true).
        *   `targetNetworkObjectId` (ulong):
            *   If `shouldAim` is true, the server attempts to resolve the target player's `NetworkObject.NetworkObjectId` via `PlayerDataManager` and `NetworkManager.SpawnManager.GetPlayerNetworkObject()`.
            *   If resolution fails or `shouldAim` is false, `targetNetworkObjectId` defaults to `0` (indicating no specific target or aim downwards).
        *   `isRevengeSpawn` (bool).
        *   `initialVelocity` (float, e.g., `2.0f`).
        *   `spiritType` (int, e.g., `0` for normal).
    *   It then calls `ClientSpiritSpawnHandler.Instance.SpawnSpiritClientRpc(...)` with these parameters, broadcasting the spawn command to all clients.

2.  **Client-Side Receiver & Spawner (`ClientSpiritSpawnHandler.cs`):**
    *   This client-only singleton MonoBehaviour receives the `SpawnSpiritClientRpc`.
    *   For each RPC call:
        *   It retrieves a Spirit GameObject from `ClientGameObjectPool.Instance.GetObject(spiritPrefabID)`.
        *   Sets the spirit's `position` and `rotation`.
        *   Gets references to the core spirit components:
            *   `ClientSpiritController`
            *   `ClientSpiritHealth`
            *   `ClientSpiritTimeoutAttack`
        *   Initializes these components using the parameters received in the RPC.
            *   `clientSpiritController.Initialize(owningSide, shouldAim, targetNetworkObjectId, isRevengeSpawn, initialVelocity, spiritType, originTransform: spiritInstance.transform)`
            *   `clientSpiritHealth.Initialize(spiritType)`
            *   The `ClientSpiritTimeoutAttack` is typically initialized implicitly or via `Awake`, but `ClientSpiritController.ActivateSpirit()` will later call its `StartTimeout()` method.
        *   Activates the Spirit GameObject (`SetActive(true)`).

### Retaliation & Counter Stage Bullets

Stage bullets (e.g., `SmallStageBullet`, `LargeStageBullet`) can spawn on a player's opponent's field through two primary mechanisms:

1.  **Fairy Kill Retaliation:**
    *   When a player kills a Fairy (`ClientFairyHealth.TakeDamage()` called by the local player), `PlayerAttackRelay.LocalInstance.ReportFairyKillServerRpc()` is invoked.
    *   The server then calls `EffectNetworkHandler.Instance.SpawnStageBulletClientRpc(...)` targeting the opponent.
    *   These bullets spawn with randomized position, speed (within `minStageBulletSpeed` to `maxStageBulletSpeed` range in `PlayerAttackRelay`), slight angle variation, and a default lifetime (`DEFAULT_FAIRY_KILL_BULLET_LIFETIME` in `PlayerAttackRelay`).

2.  **Shockwave-Cleared Bullet Counter (New Feature):**
    *   When a `ClientFairyShockwave` (spawned by a dying Fairy or Spirit) clears an existing `StageSmallBulletMoverScript` instance on its owner's side:
        *   The `ClientFairyShockwave` must have its `_canSpawnCounterBullets` flag set to `true` during its `Initialize` method.
            *   Standard Fairy and Spirit death shockwaves initialize with this flag as `true`.
            *   Shockwaves from other sources (e.g., if a future death bomb were to use this script) could set it to `false`.
        *   If the flag is true, `ClientFairyShockwave.OnTriggerStay2D` calls `PlayerAttackRelay.LocalInstance.RequestOpponentStageBulletSpawnServerRpc(...)`.
        *   This RPC takes parameters for `bulletPrefabId`, `initialSpeed`, and `lifetime` from configurable fields on the `ClientFairyShockwave` component (e.g., `opponentBulletPrefabId`, `opponentBulletSpeed`, `opponentBulletLifetime`).
        *   The server, in `PlayerAttackRelay.RequestOpponentStageBulletSpawnServerRpc`, then:
            *   Spawns the specified `bulletPrefabId`.
            *   Uses the provided `lifetime`.
            *   **Overrides** the `initialSpeed` parameter with a randomized speed (within `minStageBulletSpeed` to `maxStageBulletSpeed` range, same as fairy kill retaliation).
            *   Applies randomized spawn position and slight angle variation (same as fairy kill retaliation).
        *   It then calls `EffectNetworkHandler.Instance.SpawnStageBulletClientRpc(...)` targeting the opponent. The `isFromShockwaveClear` parameter in this RPC is set to `true` (previously used for debug coloring, now just informational if needed for other client-side distinctions).
    *   **Recursive Spawning:** These "counter" bullets are themselves `StageSmallBullet` instances. If they are subsequently cleared by another standard shockwave (that has `_canSpawnCounterBullets = true`), they will also trigger a counter bullet on the original player's side, potentially leading to a back-and-forth exchange.
    *   **Exceptions (No Counter Spawn):**
        *   **Player Death Bombs:** `PlayerDeathBomb.ClearObjectsInRadiusClientRpc` directly calls `ForceReturnToPoolByBomb()` on `StageSmallBulletMoverScript` instances. It does not use `ClientFairyShockwave` for this clearing, so no counter bullet is spawned.
        *   **Spellcard Activations:** Server-side spellcard clears (e.g., via `ServerAttackSpawner.TriggerSpellcardClear`) use the `IClearable` interface. `StageSmallBulletMoverScript.Clear()` simply returns the bullet to the pool and does not trigger any counter-spawning logic.
        *   **Bullet Lifetime Expiration:** When a stage bullet's lifetime naturally expires, it returns to the pool without spawning a counter.

## Enemy Types & Behavior (Client-Side Simulation)

### Fairies (`NormalFairy`, `GreatFairy` prefabs)

*   **Core Client-Side Components:**
    *   `PooledObjectInfo.cs`: Stores `PrefabID` for `ClientGameObjectPool`.
    *   `SplineWalker.cs`: Handles movement along a predefined path, initialized by data from the spawn RPC.
    *   `ClientFairyHealth.cs`: Manages current health and **ownership**.
        *   `Initialize(PlayerRole ownerRole)`: Sets the `_owningPlayerRole` field.
        *   `TakeDamage()`: Reduces health. Also triggers a **visual damage flash** (configurable color tint, duration, intensity) via a coroutine.
        *   When taking damage, it passes its `_owningPlayerRole` to the shockwave if one is spawned.
        *   On death (health <= 0):
            *   Spawns a `ClientFairyShockwave` from `ClientGameObjectPool`, passing damage, visual radius, **effective radius**, duration, the original killer's ID, **its own `_owningPlayerRole`**, and `canSpawnCounterBullets: true`.
            *   If the damage was dealt by a bullet from the *local* player (`attackerOwnerClientId == LocalClientId`), it calls `PlayerAttackRelay.LocalInstance.ReportFairyKillServerRpc()`.
            *   Notifies `ClientFairyController` of death (optional, if controller needs to stop other logic).
        *   `OwningPlayerRole` (Property): Returns the stored `_owningPlayerRole`.
    *   `ClientFairyController.cs`: Primarily handles `SplineWalker.OnPathCompleted` to return the fairy to `ClientGameObjectPool`. May also handle `OnTriggerEnter2D` for direct collision with player shots as an alternative to `BulletMovement` handling it.
    *   `CircleCollider2D` (Trigger): For detecting collisions with player shots or shockwave areas.
    *   Visuals: `SpriteRenderer`, `Animator`.
*   **Interaction & Death:**
    *   **Taking Damage:**
        *   Player bullets (`BulletMovement.cs`) collide, get `ClientFairyHealth`, call `TakeDamage(damage, bulletOwnerClientId)`.
        *   Enemy shockwaves (`ClientFairyShockwave.cs`) collide, get `ClientFairyHealth`, call `TakeDamage(damage, shockwaveOriginalKillerId)`.
    *   **On-Death (handled by `ClientFairyHealth`):** Triggers shockwave and reports kill if applicable (see above).
    *   **Path End:** `SplineWalker` calls `OnPathCompleted` event. `ClientFairyController` subscribes and returns the fairy to `ClientGameObjectPool`.

### Spirits (`Spirit` prefab)

Spirits are client-simulated entities with distinct behaviors before and after activation.

*   **Core Client-Side Components (on the Spirit prefab):**
    *   `PooledObjectInfo.cs`: Stores `PrefabID` (e.g., "Spirit") for `ClientGameObjectPool`.
    *   `ClientSpiritController.cs`:
        *   **Responsibilities:** Manages overall spirit behavior, movement, activation state, and **ownership**.
        *   `Initialize()`: Sets initial parameters including the `_owningPlayerRole`. `currentDirection` defaults to `Vector2.down`. If `shouldAim` is true and `targetNetworkObjectId` is valid, it aims towards the target. Stores the `_owningPlayerRole` passed in.
        *   `Update()`:
            *   Calculates potential `nextPosition` based on current velocity and `Time.deltaTime`.
            *   **Centerline Check:** Determines if `nextPosition.x` would cross the X=0 boundary based on `_owningPlayerRole` (e.g., if role is P1 and `nextPosition.x <= 0`, or role is P2 and `nextPosition.x >= 0`).
            *   If crossing centerline:
                *   Stops the timeout coroutine (`_timeoutAttack.StopTimeoutCoroutine()`).
                *   Calls `_spiritHealth.ForceReturnToPool()` to despawn immediately.
                *   Returns early from `Update()`.
            *   If not crossing centerline:
                *   Updates `transform.position = nextPosition`.
                *   Handles movement logic (downwards if not activated; upwards with acceleration if activated).
        *   `ActivateSpirit()`:
            *   Called by `ReimuScopeStyleController` or `MarisaScopeStyleController` via `OnTriggerEnter2D`.
            *   Sets `_isActivated = true`.
            *   Changes movement to upward and applies initial activated speed.
            *   Swaps visual GameObjects (`normalSpiritVisual` off, `activatedSpiritVisual` on).
            *   Calls `_spiritHealth.OnActivated()` to set HP to 1.
            *   Calls `_timeoutAttack.StartTimeout(duration, _targetNetworkObjectId)` (duration is e.g., 1.5s). If the spirit was initially aimed, this `_targetNetworkObjectId` is passed; otherwise, `0` is passed.
        *   `Deinitialize()`: Resets state when returned to pool.
        *   `OwningPlayerRole` (Property): Returns the stored `_owningPlayerRole`.
        *   **Serialized Fields:** `normalSpiritVisual`, `activatedSpiritVisual`, `activatedInitialUpwardSpeed`, `activatedMaxUpwardSpeed`, `activatedAcceleration`.
    *   `ClientSpiritHealth.cs`:
        *   **Responsibilities:** Manages spirit health, damage taking, and death sequence.
        *   `Initialize()`: Sets HP to `NORMAL_SPIRIT_HP` (e.g., 5). Resets `_isActivated` flag.
        *   `TakeDamage(amount, attackerOwnerClientId)`: Reduces HP. Triggers a **visual damage flash** (configurable color tint, duration, intensity) via a coroutine, using the correct sprite renderer (`normal` or `activated`) obtained from `ClientSpiritController`. If HP <= 0, calls `Die()`. Called by `BulletMovement.OnTriggerEnter2D` or `PlayerDeathBomb`.
        *   `OnActivated()`: Sets HP to `ACTIVATED_SPIRIT_HP` (e.g., 1) and sets `_isActivated = true`.
        *   `Die(attackerOwnerClientId)`:
            *   Calls `SpawnDeathShockwave(attackerOwnerClientId)` (see below).
            *   If `attackerOwnerClientId` is the local player, calls `PlayerAttackRelay.LocalInstance.ReportSpiritKillServerRpc()` to potentially trigger a revenge spawn.
            *   Returns the spirit GameObject to `ClientGameObjectPool`.
        *   `SpawnDeathShockwave(killerClientId)`:
            *   Gets a shockwave prefab (e.g., "FairyShockwave") from `ClientGameObjectPool`.
            *   Sets its position.
            *   Gets `ClientFairyShockwave` component.
            *   Initializes it, passing damage, visual radius, **effective radius** (both scaled if activated), duration, killer ID, and importantly, the **Spirit's `_owningPlayerRole`** (retrieved from `ClientSpiritController`), and `canSpawnCounterBullets: true`.
        *   `ForceReturnToPool()`: Returns object to pool *without* triggering `Die()` (used by timeout).
        *   **Serialized Fields:** `shockwavePrefabId`, `normalSpiritShockwaveMaxRadius` (Visual), `activatedSpiritShockwaveMaxRadius` (Visual), `normalSpiritShockwaveEffectiveMaxRadius` (Effective), `activatedSpiritShockwaveEffectiveMaxRadius` (Effective), `shockwaveDuration`, `shockwaveDamage`, `shockwaveInitialRadius`.
    *   `ClientSpiritTimeoutAttack.cs`:
        *   **Responsibilities:** Handles the attack pattern when an activated spirit times out.
        *   `StartTimeout(duration, targetNetworkObjectIdToAttack)`: Called by `ClientSpiritController.ActivateSpirit()`. Stores `targetNetworkObjectIdToAttack` and starts `TimeoutAttackCoroutine`.
        *   `TimeoutAttackCoroutine(duration)`:
            *   Waits for `duration`.
            *   If `targetNetworkObjectIdToAttack` (stored from `StartTimeout`) is valid (not 0), it attempts to find the target `Transform` using `NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectIdToAttack, out NetworkObject playerNetObj)`.
            *   If a `targetTransform` is found, `directionToTarget` is calculated towards it.
            *   If `targetNetworkObjectIdToAttack` was 0 or the target wasn't found, `directionToTarget` defaults to `Vector2.down`.
            *   Spawns 3 bullets (e.g., "StageLargeBullet" from `ClientGameObjectPool`) in a claw pattern (`clawPatternAngles`) aimed along `directionToTarget`.
            *   Initializes each bullet's `StageSmallBulletMoverScript.Initialize(direction, timeoutBulletSpeed, timeoutBulletLifetime)`.
            *   Calls `_spiritHealth.ForceReturnToPool()` to despawn the spirit without a death shockwave.
        *   **Serialized Fields:** `timeoutBulletPrefabID`, `clawPatternAngles`, `timeoutBulletSpeed`, `timeoutBulletLifetime`.
    *   Appropriate 2D Colliders (e.g., `BoxCollider2D`) and a `Rigidbody2D` (typically Kinematic).

*   **Interaction & Lifecycle:**
    *   **Spawning:** Triggered by server RPC via `ClientSpiritSpawnHandler`.
    *   **Movement:**
        *   Normal: Downwards or one-time aim at spawn.
        *   Activated: Upwards with acceleration.
    *   **Activation:** `ClientSpiritController.ActivateSpirit()` called by `OnTriggerEnter2D` of `ReimuScopeStyleController` or `MarisaScopeStyleController` when the spirit overlaps the active zone.
    *   **Taking Damage:**
        *   Player bullets (`BulletMovement.cs` modified to check for "Spirit" tag) collide, get `ClientSpiritHealth`, call `TakeDamage(damage, bulletOwnerClientId)`.
    *   **Death (Normal or Activated, by Damage):**
        *   `ClientSpiritHealth.Die()` is called.
        *   A shockwave is spawned (larger if activated).
        *   Kill is reported to server for revenge spawn if local player killed it.
        *   Spirit returned to pool.
    *   **Timeout (Activated Spirit):**
        *   `ClientSpiritTimeoutAttack.TimeoutAttackCoroutine` completes.
        *   Fires a 3-bullet claw pattern (targeted at player on its side, or downwards).
        *   Spirit is returned to pool via `ForceReturnToPool()` (no shockwave).
    *   **Player Death Bomb:** `PlayerDeathBomb.ClearObjectsInRadiusClientRpc` iterates pooled objects, finds spirits via `ClientSpiritHealth`, and calls `TakeDamage()`.

## Clearing Effects (Client-Side)

Enemies are "cleared" by taking lethal damage.

*   **Fairy Shockwaves:** `ClientFairyShockwave` deals damage to other enemies within its radius, potentially triggering chain reactions if that damage is lethal.
*   **Player Death Bomb:** The `ClientRpc` for bomb clearing (`EffectNetworkHandler.ClearBulletsInRadiusClientRpc`) primarily targets bullets. If bombs are also meant to clear enemies, clients receiving this RPC would need to:
    *   Iterate active GameObjects from `ClientGameObjectPool`.
    *   Identify enemies (e.g., by tag or component like `ClientFairyHealth`).
    *   If in radius, call `enemyHealth.TakeDamage(bombDamage, bombingPlayerId)` or a specific `ForceKill()` method on the health component.

## Data Structure / Definition

*   **Client-Side Prefabs:** `NormalFairy`, `GreatFairy`, `Spirit` (future). Contain client-side components listed above.
*   **Server-Side Configuration:**
    *   `FairySpawner.cs`: Defines fairy wave structures, path IDs, timings, and prefab IDs for RPCs.
    *   `PathManager.cs`: Stores spline path data accessible by ID.

## Key Scripts

*   **Core Client-Side Enemy Components:**
    *   `ClientFairyHealth.cs`: Manage health, death effects (shockwave, kill reporting) for Fairies.
    *   `ClientFairyController.cs`: Coordinate client-side behaviors, path completion pooling for Fairies.
    *   `ClientSpiritController.cs`: Manages overall spirit behavior, movement (normal, aimed, activated with acceleration), visual state, and triggers activation consequences (health change, timeout attack start).
    *   `ClientSpiritHealth.cs`: Manages spirit health (normal/activated), damage processing, death sequence (shockwave with variable size, reporting kill to server), and forced despawn for timeouts.
    *   `ClientSpiritTimeoutAttack.cs`: Handles the activated spirit's timeout, including determining target side, aiming the 3-bullet claw attack (or firing downwards), and despawning the spirit.
    *   `SplineWalker.cs`: Client-side path following for fairies.
    *   `PooledObjectInfo.cs`: Essential for `ClientGameObjectPool`.
*   **Server-Side Spawning Logic:**
    *   `FairySpawner.cs`: Calculates fairy waves and parameters.
    *   `FairySpawnNetworkHandler.cs`: Singleton that sends `SpawnFairyWaveClientRpc` to all clients.
    *   `SpiritSpawner.cs`: Server-only singleton that decides when/where to spawn spirits (periodic or revenge), resolves initial target parameters, and calls an RPC on `ClientSpiritSpawnHandler`.
*   **Client-Side Spawning Handlers:**
    *   `ClientSpiritSpawnHandler.cs`: Client-only singleton that receives an RPC from `SpiritSpawner` to spawn and initialize spirits locally from the `ClientGameObjectPool`.
*   **Related Systems:**
    *   `ClientGameObjectPool.cs`: Pools all client-side enemies (Fairies, Spirits) and their effects (shockwaves, spirit timeout bullets).
    *   `PlayerAttackRelay.cs`:
        *   Receives kill reports from `ClientFairyHealth` and `ClientSpiritHealth`.
        *   `ReportFairyKillServerRpc`: Handles fairy kill retaliation, calls `EffectNetworkHandler.SpawnStageBulletClientRpc` to spawn a stage bullet on the opponent's side with randomized parameters and a default lifetime.
        *   `RequestOpponentStageBulletSpawnServerRpc`: Handles counter-bullet requests from shockwave clears. Receives bullet type, initial speed (overridden by randomization on server), and lifetime. Calls `EffectNetworkHandler.SpawnStageBulletClientRpc` to spawn a stage bullet on the opponent's side with randomized parameters (position, speed, direction) and the specified lifetime.
    *   `ClientFairyShockwave.cs`: Spawned on fairy/spirit death. Takes owner's `PlayerRole` on init. Its `Initialize` method now accepts separate `visualMaxRadius` and `effectiveMaxRadius`. In `Update`, it scales its visuals based on `visualMaxRadius` and its `CircleCollider2D` based on `effectiveMaxRadius`. `OnTriggerStay2D` checks collided object's `OwningPlayerRole` (from `ClientFairyHealth`, `ClientSpiritController`, `StageSmallBulletMoverScript`) and only applies damage/clearing if roles *do not match*.
        *   Now has an `_canSpawnCounterBullets` flag, set during `Initialize`.
        *   If this flag is true, and the shockwave clears a same-side `StageSmallBulletMoverScript`, it calls `PlayerAttackRelay.LocalInstance.RequestOpponentStageBulletSpawnServerRpc` to trigger a counter bullet on the opponent's side using configurable `opponentBulletPrefabId`, `opponentBulletSpeed` (which gets overridden by randomization on the server), and `opponentBulletLifetime`.
    *   `EffectNetworkHandler.cs`:
        *   `SpawnStageBulletClientRpc`: Now takes `bulletLifetime` and an `isFromShockwaveClear` (boolean, informational) parameter. It uses the provided `bulletLifetime` when initializing the `StageSmallBulletMoverScript`.
    *   `PathManager.cs`: Provides path data to clients for fairies.
    *   `ReimuScopeStyleController.cs` / `MarisaScopeStyleController.cs`: Their `OnTriggerEnter2D` methods call `ClientSpiritController.ActivateSpirit()`.
    *   `BulletMovement.cs`: Modified to apply damage to `ClientSpiritHealth` upon collision with spirits.
    *   `PlayerDeathBomb.cs`: Modified to apply damage to `ClientSpiritHealth` for spirits in radius.
    *   `StageSmallBulletMoverScript.cs`: Used by bullets spawned from `ClientSpiritTimeoutAttack`.
        *   Its `IClearable.Clear()` method simply returns the bullet to the pool, ensuring server-side clears (like spellcards) do not trigger counter-spawns.
        *   Its `ForceReturnToPoolByBomb()` method (called by player death bombs) also just returns the bullet to the pool, preventing counter-spawns.
*   **Deprecated/Replaced (Server-Authoritative Components for Fairies & Old Spirit System):**
    *   ~~`FairyPathInitializer.cs`~~
    *   ~~Server-side `FairyController.cs` logic for death/path end~~ (now client-side)
    *   ~~`FairyDeathEffects.cs`~~ (Functionality integrated into `ClientFairyHealth`/`ClientFairyShockwave`)
    *   ~~`FairyChainReactionHandler.cs`~~ (Chain reactions are emergent from client-side shockwaves damaging other client-side enemies)
    *   ~~Old server-side `SpiritController.cs` and its direct `NetworkObjectPool` usage for spirits.~~