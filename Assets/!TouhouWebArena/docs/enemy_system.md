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

### Lily White (`LilyWhite` prefab - Client-Side Simulated)

Lily White is a client-simulated entity with a predefined movement pattern and attack sequences. **She now has health and can be defeated by player shots.**

*   **Core Client-Side Components (on the LilyWhite prefab):**
    *   `PooledObjectInfo.cs`: Stores `PrefabID` ("LilyWhite") for `ClientGameObjectPool`.
    *   `ClientLilyWhiteController.cs`:
        *   **Responsibilities:** Manages Lily White's entire lifecycle on the client, including her three-phase movement, initiating her attack pattern, and timed/health-based despawn.
        *   `Initialize()`: Called by `ClientLilyWhiteSpawnHandler`. Sets initial position based on the `spawnX` received. Stores the `PlayerRole` (Player1 or Player2) corresponding to her spawn side. Initializes the `ClientLilyWhiteHealth` component. Activates the GameObject and starts the `MovementLifecycleCoroutine` and a fallback `DespawnTimerCoroutine`.
        *   `MovementLifecycleCoroutine()`: Handles the sequence:
            1.  Drift downwards at `initialDriftDownSpeed` to `targetYInCenter`.
            2.  Wait for `waitDuration`.
            3.  Float upwards at `floatUpSpeed` until `transform.position.y > offScreenYTop`.
            *   **Attack Trigger:** When reaching the wait phase, if an `attackPatternHandler` is assigned, it calls `attackPatternHandler.StartAttackSequence()`, passing Lily White's transform and her stored `PlayerRole`.
        *   `DespawnTimerCoroutine()`: A fallback mechanism that ensures Lily White is returned to the pool after `totalLifetime` seconds, regardless of her movement or health state.
        *   `HandleDeath()`: New public method called by `ClientLilyWhiteHealth` when health reaches zero. Stops all coroutines and calls `ReturnToPool()`.
        *   `ReturnToPool()`: Returns the GameObject to `ClientGameObjectPool` using its `PrefabID`.
        *   **Serialized Fields:** `initialDriftDownSpeed`, `floatUpSpeed`, `waitDuration`, `targetYInCenter`, `offScreenYTop`, `initialSpawnY`, `totalLifetime`, `attackPatternHandler`.
    *   `ClientLilyWhiteHealth.cs`: (New Component)
        *   **Responsibilities:** Manages Lily White's health (default 75), handles damage intake from player shots, triggers a visual damage flash, and notifies `ClientLilyWhiteController` upon death.
        *   `Initialize()`: Sets current health to max health.
        *   `TakeDamage(amount, attackerOwnerClientId)`: Reduces health, triggers flash. If health <= 0, calls `_lilyWhiteController.HandleDeath()`.
        *   `ForceReturnToPoolByClear()`: Called by effects like spellcard clears if Lily White needs to be despawned instantly by them. Marks health as 0 and calls `_lilyWhiteController.HandleDeath()`.
        *   **Serialized Fields:** `maxHealth`, `_flashColor`, `_flashDuration`, `_flashIntensity`.
    *   `LilyWhiteAttackPattern.cs`:
        *   **Responsibilities:** Defines and executes Lily White's bullet attack patterns.
        *   `StartAttackSequence()`: Called by `ClientLilyWhiteController`. Receives Lily White's transform and the target `PlayerRole` (Player1 or Player2) corresponding to her spawn side. Stores the `PlayerRole` and starts coroutines for each configured attack sweep.
        *   `ExecuteSweepCoroutine()`: Handles the timing and angle interpolation for a single bullet sweep.
        *   `SpawnClaw()`: Called by `ExecuteSweepCoroutine`. Obtains bullet instances (e.g., "StageSmallBullet") from `ClientGameObjectPool`. Initializes their position and rotation, and crucially, calls `StageSmallBulletMoverScript.Initialize()`, passing the calculated direction, speed, lifetime, and the stored target `PlayerRole`.
        *   **Serialized Fields:** `attackSweeps` (List of `LilySweepParameters` structs).
    *   `SpriteRenderer`: For visual representation.
    *   `Collider2D` (Trigger): **Required.** Must be added to the prefab and set to the **"LilyWhite" tag** to allow player shots (`BulletMovement.cs`) to detect and damage her.

*   **Interaction & Lifecycle:**
    *   **Spawning:** Triggered by server RPC via `LilyWhiteSpawner` -> `ClientLilyWhiteSpawnHandler`.
    *   **Movement:** Deterministic three-phase movement (down, wait, up) handled by `ClientLilyWhiteController`.
    *   **Attacks:** Spawns stage bullets during the wait phase, handled by `LilyWhiteAttackPattern`.
        *   `LilyWhiteAttackPattern.cs` now also handles playing a looping attack sound (`attackSoundClip`) via `PlayOneShot()` on an `AudioSource` during its sweep attacks.
    *   **Despawning:** 
        *   Primarily by a timer (`totalLifetime` in `ClientLilyWhiteController`).
        *   **New:** When health reaches zero due to player shots, `ClientLilyWhiteHealth` calls `ClientLilyWhiteController.HandleDeath()`.
        *   The movement coroutine also has logic to detect when she moves off-screen (though the timer or health depletion are the primary despawn triggers).
    *   **Taking Damage:** Player bullets with `BulletMovement.cs` check for the "LilyWhite" tag on collision. If matched, `ClientLilyWhiteHealth.TakeDamage()` is called. 
    *   **Spawn Sound:** `ClientLilyWhiteSpawnHandler.cs` now plays a sound (`lilyWhiteSpawnSound`) via `PlayOneShot()` on an `AudioSource` when Lily White is initialized locally.
    *   **Defeat Sound:** `ClientLilyWhiteHealth.cs` plays an `enemyDefeatedSound` using `AudioSource.PlayClipAtPoint()` when Lily White is defeated by HP depletion. This sound is only played on the client of the player who dealt the fatal blow.

## Clearing Effects (Client-Side)

Enemies are "cleared" by taking lethal damage. Enemy *projectiles* can also be cleared by specific effects.

*   **Spellcard Activation Clear:** Player spellcards triggered via `ClientSpellcardExecutor.TriggerLocalClearEffectClientRpc` clear `StageSmallBulletMoverScript` instances (including Lily White's bullets) that are within the spellcard's radius and located on the *caster's side* of the arena (X < 0 for Player 1, X > 0 for Player 2), regardless of the bullet's `OwningPlayerRole`. This is a client-side visual clear based on position. Lily White herself is no longer affected by this clear.
*   **Fairy Shockwaves:** `ClientFairyShockwave` instances triggered by dying Fairies or Spirits clear `StageSmallBulletMoverScript` instances (including Lily White's bullets) that are within the shockwave's radius and belong to the *opposing player* (checking `bullet.OwningPlayerRole != shockwaveOwnerRole && bullet.OwningPlayerRole != PlayerRole.None`). This is a client-side clear based on bullet ownership.
*   **Player Death Bomb:** The `ClientRpc` for bomb clearing (`PlayerDeathBomb.ClearObjectsInRadiusClientRpc`) clears `StageSmallBulletMoverScript` instances (including Lily White's bullets) within the bomb radius by calling `ForceReturnToPoolByBomb()`, regardless of ownership or position. It also damages/clears other entities like fairies and spirits based on ownership. **It may need to be updated to also call `ForceReturnToPoolByClear()` on `ClientLilyWhiteHealth` if Lily White should be cleared by player bombs.**

## Data Structure / Definition

*   **Client-Side Prefabs:** `NormalFairy`, `GreatFairy`, `Spirit` (future). Contain client-side components listed above.
*   **Server-Side Configuration:**
    *   `FairySpawner.cs`: Defines fairy wave structures, path IDs, timings, and prefab IDs for RPCs.
    *   `PathManager.cs`: Stores spline path data accessible by ID.
*   **New Components:**
    *   `LilyWhiteAttackPattern.cs`: Defines attack sweeps, bullet types, timings, angles, speeds, and lifetimes.
    *   `ClientLilyWhiteHealth.cs`: Manages health and damage for Lily White.

## Key Scripts

*   **Core Client-Side Enemy Components:**
    *   `ClientFairyHealth.cs`: Manage health, death effects (shockwave, kill reporting) for Fairies.
        *   Now requires an `AudioSource` component.
        *   Plays an `enemyDefeatedSound` using `AudioSource.PlayClipAtPoint()` when the fairy is defeated by HP depletion, only on the client of the player who dealt the fatal blow.
    *   `ClientFairyController.cs`: Coordinate client-side behaviors, path completion pooling for Fairies.
    *   `ClientSpiritController.cs`: Manages overall spirit behavior, movement (normal, aimed, activated with acceleration), visual state, and triggers activation consequences (health change, timeout attack start).
    *   `ClientSpiritHealth.cs`: Manages spirit health (normal/activated), damage processing, death sequence (shockwave with variable size, reporting kill to server), and forced despawn for timeouts.
        *   Now requires an `AudioSource` component.
        *   Plays an `enemyDefeatedSound` using `AudioSource.PlayClipAtPoint()` when the spirit is defeated by HP depletion, only on the client of the player who dealt the fatal blow.
    *   `ClientSpiritTimeoutAttack.cs`: Handles the activated spirit's timeout, including determining target side, aiming the 3-bullet claw attack (or firing downwards), and despawning the spirit.
    *   `SplineWalker.cs`: Client-side path following for fairies.
    *   `PooledObjectInfo.cs`: Essential for `ClientGameObjectPool`.
*   **Server-Side Spawning Logic:**
    *   `FairySpawner.cs`: Calculates fairy waves and parameters.
    *   `FairySpawnNetworkHandler.cs`: Singleton that sends `SpawnFairyWaveClientRpc` to all clients.
    *   `SpiritSpawner.cs`: Server-only singleton that decides when/where to spawn spirits (periodic or revenge), resolves initial target parameters, and calls an RPC on `ClientSpiritSpawnHandler`.
    *   `LilyWhiteSpawner.cs`: Server-only singleton that periodically triggers Lily White spawns on all clients via an RPC.
*   **Client-Side Spawning Handlers:**
    *   `ClientSpiritSpawnHandler.cs`: Client-only singleton that receives an RPC from `SpiritSpawner` to spawn and initialize spirits locally from the `ClientGameObjectPool`.
    *   `ClientLilyWhiteSpawnHandler.cs`: Client-only singleton that receives an RPC from `LilyWhiteSpawner` to spawn and initialize Lily White locally from the `ClientGameObjectPool`.
        *   Now includes an `AudioSource` and `AudioClip lilyWhiteSpawnSound` to play a sound on spawn.
    *   `ClientLilyWhiteHealth.cs`: (New) Manages health, damage, and death notification for Lily White.
        *   Now requires an `AudioSource` component.
        *   Plays an `enemyDefeatedSound` using `AudioSource.PlayClipAtPoint()` when Lily White is defeated by HP depletion, only on the client of the player who dealt the fatal blow.
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
    *   `BulletMovement.cs`: Modified to apply damage to `ClientSpiritHealth` upon collision with spirits **and `ClientLilyWhiteHealth` upon collision with entities tagged "LilyWhite"**.
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