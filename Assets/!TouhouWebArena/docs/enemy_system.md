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

### Spirits (`Spirit` - Requires Further Refactoring)

*   **Current State (largely server-authoritative, needs update):** Spawning was handled by `SpiritSpawner.cs`, with revenge spawns initiated by `SpiritController.Die()`.
*   **Future Refactor Goal:** Adapt to the server-command, client-simulation model. Server would send an RPC to clients to spawn a spirit at a location. `SpiritTimeoutAttack` would become a client-side script.

## Enemy Types & Behavior (Client-Side Simulation)

### Fairies (`NormalFairy`, `GreatFairy` prefabs)

*   **Core Client-Side Components:**
    *   `PooledObjectInfo.cs`: Stores `PrefabID` for `ClientGameObjectPool`.
    *   `SplineWalker.cs`: Handles movement along a predefined path, initialized by data from the spawn RPC.
    *   `ClientFairyHealth.cs`: Manages current health. Takes damage from projectiles/shockwaves. On death (health <= 0):
        *   Spawns a `ClientFairyShockwave` from `ClientGameObjectPool` (passing damage, radius, duration, and original killer's ID).
        *   If the damage was dealt by a bullet from the *local* player (`attackerOwnerClientId == LocalClientId`), it calls `PlayerAttackRelay.LocalInstance.ReportFairyKillServerRpc()`.
        *   Notifies `ClientFairyController` of death (optional, if controller needs to stop other logic).
    *   `ClientFairyController.cs`: Primarily handles `SplineWalker.OnPathCompleted` to return the fairy to `ClientGameObjectPool`. May also handle `OnTriggerEnter2D` for direct collision with player shots as an alternative to `BulletMovement` handling it.
    *   `CircleCollider2D` (Trigger): For detecting collisions with player shots or shockwave areas.
    *   Visuals: `SpriteRenderer`, `Animator`.
*   **Interaction & Death:**
    *   **Taking Damage:**
        *   Player bullets (`BulletMovement.cs`) collide, get `ClientFairyHealth`, call `TakeDamage(damage, bulletOwnerClientId)`.
        *   Enemy shockwaves (`ClientFairyShockwave.cs`) collide, get `ClientFairyHealth`, call `TakeDamage(damage, shockwaveOriginalKillerId)`.
    *   **On-Death (handled by `ClientFairyHealth`):** Triggers shockwave and reports kill if applicable (see above).
    *   **Path End:** `SplineWalker` calls `OnPathCompleted` event. `ClientFairyController` subscribes and returns the fairy to `ClientGameObjectPool`.

### Spirits (`Spirit` prefab - Requires Refactoring)

*   Client-side components will likely include `PooledObjectInfo`, a movement script, `ClientSpiritHealth` (similar to `ClientFairyHealth`), and a client-side `ClientSpiritTimeoutAttack`.
*   The timeout attack would spawn a stage bullet (e.g., "StageLargeBullet") locally from `ClientGameObjectPool` and initialize its mover script.

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
    *   `ClientFairyHealth.cs` / `ClientSpiritHealth.cs` (future): Manage health, death effects (shockwave, kill reporting).
    *   `ClientFairyController.cs` / `ClientSpiritController.cs` (future): Coordinate client-side behaviors, path completion pooling.
    *   `SplineWalker.cs`: Client-side path following for fairies.
    *   `PooledObjectInfo.cs`: Essential for `ClientGameObjectPool`.
*   **Server-Side Spawning Logic:**
    *   `FairySpawner.cs`: Calculates fairy waves and parameters.
    *   `FairySpawnNetworkHandler.cs`: Singleton that sends `SpawnFairyWaveClientRpc` to all clients.
    *   (Future `SpiritSpawner.cs` / `SpiritSpawnNetworkHandler.cs`)
*   **Related Systems:**
    *   `ClientGameObjectPool.cs`: Pools all client-side enemies.
    *   `PlayerAttackRelay.cs`: Receives kill reports from `ClientFairyHealth`.
    *   `ClientFairyShockwave.cs`: Spawned on fairy death, can damage other enemies.
    *   `PathManager.cs`: Provides path data to clients.
*   **Deprecated/Replaced (Server-Authoritative Components for Fairies):**
    *   ~~`FairyPathInitializer.cs`~~
    *   ~~Server-side `FairyController.cs` logic for death/path end~~ (now client-side)
    *   ~~`FairyDeathEffects.cs`~~ (Functionality integrated into `ClientFairyHealth`/`ClientFairyShockwave`)
    *   ~~`FairyChainReactionHandler.cs`~~ (Chain reactions are emergent from client-side shockwaves damaging other client-side enemies)