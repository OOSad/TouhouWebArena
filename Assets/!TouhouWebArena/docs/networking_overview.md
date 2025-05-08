# Networking Overview

This document provides an overview of the networking architecture for Touhou Web Arena, which uses Unity's Netcode for GameObjects package. The project utilizes a hybrid approach:

*   **Client-Authoritative Movement:** Player movement is handled entirely client-side (`ClientAuthMovement.cs`) for maximum responsiveness, with the owner synchronizing its position via a `NetworkVariable`.
*   **Server-Authoritative Spawning:** The server decides *what* and *when* to spawn for key gameplay events (enemy waves, retaliation bullets, complex attacks). It then sends targeted `ClientRpc` calls to instruct clients.
*   **Client-Side Simulation:** Clients, upon receiving spawn commands via RPC, instantiate and simulate the relevant objects locally. This includes enemy movement (`SplineWalker.cs`), projectile movement (`BulletMovement.cs`, `StageSmallBulletMoverScript.cs`), object lifetimes (`ClientProjectileLifetime.cs`), and visual effects (`ClientFairyShockwave.cs`). These simulated objects are typically managed by `ClientGameObjectPool.cs` and do not have `NetworkObject` components.
*   **Client Reporting:** Clients report significant events (like dealing the killing blow to a fairy) back to the server via `ServerRpc` (`PlayerAttackRelay.ReportFairyKillServerRpc`).
*   **Server Orchestration:** The server processes these reports and orchestrates subsequent actions (e.g., instructing all clients via `ClientRpc` to spawn a retaliation bullet on the opponent's field).

This model aims for responsiveness in player control and basic actions while retaining server control over critical game flow and preventing simple cheats related to spawning.

## Core Concepts

Netcode for GameObjects provides several building blocks for networked applications:

*   **`NetworkObject`:** Key game entities that need their state *continuously synchronized* or require server-authoritative control (like Players, potentially complex boss entities) must have a `NetworkObject` component attached. Objects spawned and simulated purely client-side based on RPC commands (basic projectiles, fairies, shockwaves) generally do *not* have `NetworkObject` components.
*   **`NetworkBehaviour`:** Scripts that contain networking logic (RPCs, NetworkVariables) must inherit from `NetworkBehaviour`. These typically reside on `NetworkObject`s (like the Player prefab or singleton network handlers).
*   **RPCs (Remote Procedure Calls):** Allow specific functions to be called across the network.
    *   **`ServerRpc`:** Used by a client (typically the owner of a `NetworkObject`, like `PlayerAttackRelay` on the Player) to report an event to the server (e.g., `ReportFairyKillServerRpc`).
    *   **`ClientRpc`:** Used by the server (often from a singleton `NetworkBehaviour` like `EffectNetworkHandler` or `FairySpawnNetworkHandler`) to command one or more clients to execute a function (e.g., `SpawnStageBulletClientRpc`, `SpawnFairyWaveClientRpc`).
    *   **Common Flow for Client-Initiated Actions (e.g., Basic Shot):
        1.  Owner Client (`PlayerShootingController`): Detects input, spawns projectile locally from `ClientGameObjectPool`, initializes `BulletMovement` with `OwnerClientId`.
        2.  Owner Client: Calls `FireShotServerRpc` on its `PlayerShootingController`.
        3.  Server: Receives `FireShotServerRpc`.
        4.  Server: Calls `FireShotClientRpc` on the same `PlayerShootingController` instance on the server, targeting all clients.
        5.  Remote Clients: Receive `FireShotClientRpc`, spawn the projectile from their `ClientGameObjectPool`, initialize `BulletMovement` with firer's `OwnerClientId`.
        6.  Owner Client: Also receives `FireShotClientRpc` but typically ignores it (or could use it to confirm server acknowledgement).
    *   **Common Flow for Server-Initiated Actions (e.g., Fairy Wave Spawn):
        1.  Server (`FairySpawner`): Determines wave parameters.
        2.  Server: Calls `SpawnFairyWaveClientRpc` on a singleton (`FairySpawnNetworkHandler.Instance`), targeting all clients.
        3.  All Clients: Receive `SpawnFairyWaveClientRpc`, get prefab from `ClientGameObjectPool`, initialize position/path (`SplineWalker`), activate the enemy.
*   **`NetworkVariable`:** Used to automatically synchronize simple data types. Mainly used for player position (`ClientAuthMovement.NetworkedPosition`, owner-write) and potentially server-managed states like score or player metadata (`PlayerDataManager`). Health is now primarily client-side (`ClientFairyHealth`) with kills reported to the server, rather than being a directly synchronized variable.

## Starting a Session / Connection Flow

When running the game locally for development or testing using the Unity editor and standalone builds (or multiple editor instances), follow this specific connection procedure:

1.  **Start the Server:**
    *   **Default (Localhost):** Launch one instance of the game (editor or build). While the Main Menu is loaded, press **F9**. This instance becomes the dedicated server listening on the default address/port (likely 127.0.0.1:7777) and will have `ClientId = 0`. Pressing F9 again will stop the server.
    *   **Custom IP/Port:** Launch one instance. Press **F10** to reveal the custom IP address and Port input fields. Enter the desired values and click the "Start Custom Server" button.
2.  **Start Client(s):** Launch subsequent instances (editor or build).
    *   Enter a desired player name (optional, defaults to "Anonymous#[Random]").
    *   Click the **Queue** button. This will automatically attempt to connect the client to the server (using default transport settings) and, upon successful connection, immediately request to join the matchmaking queue.
    *   The first client connected will receive `ClientId = 1`, the second `ClientId = 2`, and so on.
3.  **Cancel:**
    *   If connecting, clicking **Cancel** stops the connection attempt.
    *   If connected (either waiting to queue or already queued), clicking **Cancel** will leave the queue (if applicable) and disconnect the client.

**Important:** Do **not** use the "Start Host" button if it still exists in the `NetworkManager` component's UI. The Host mode acts as both a server and a client simultaneously, which does not fit our intended dedicated server + two clients architecture for a standard 1v1 match. The primary methods for starting the server are now the F9 key or the F10 custom panel.

## Key Systems

Here's how major game systems interact with the network:

*   **Player Movement:** Player movement is **client-authoritative** (`ClientAuthMovement.cs`). Owner writes position to `NetworkedPosition` (`NetworkVariable<Vector2>`), remotes read and apply. `NetworkTransform` is not used.
*   **Shooting / Basic Attacks (Client-Side Simulation):**
    *   Owning `PlayerShootingController.cs` detects input.
    *   Spawns projectile locally from `ClientGameObjectPool.cs`, initializes `BulletMovement` (passing `OwnerClientId`).
    *   Sends `FireShotServerRpc` to server.
    *   Server relays via `FireShotClientRpc` to all clients.
    *   Remote clients receive RPC, spawn visual projectile from their pool, initialize `BulletMovement` with firer's `OwnerClientId`.
    *   `BulletMovement` handles movement. `ClientProjectileLifetime` handles despawn via pool.
*   **Retaliation Bullets (Server-Initiated, Client-Simulated):**
    *   `ClientFairyHealth.TakeDamage` checks if killer is local player (`attackerOwnerClientId == LocalClientId`).
    *   If local kill, `ClientFairyHealth` calls `PlayerAttackRelay.LocalInstance.ReportFairyKillServerRpc()`.
    *   Server (`PlayerAttackRelay.ReportFairyKillServerRpc`) receives report, determines opponent, calculates bullet parameters (prefabId, position, speed, angle).
    *   Server calls `EffectNetworkHandler.Instance.SpawnStageBulletClientRpc(opponentClientId, params...)` targeting *all* clients.
    *   All clients receive RPC, use `opponentClientId` to find the correct player's spawn area via `SpawnAreaManager`, get bullet from `ClientGameObjectPool`, set position, initialize `StageSmallBulletMoverScript` with parameters, activate.
*   **Charge Attacks & Spellcard Activation:** Client (`PlayerShootingController`) sends `ServerRpc` requests (`RequestChargeAttackServerRpc`, `RequestSpellcardServerRpc`). Server (`SpellBarManager`, `ServerAttackSpawner`) validates, consumes costs, and orchestrates effects. Visualization likely involves server sending `ClientRpc`s to instruct clients to spawn specific effects/projectiles locally via `ClientGameObjectPool`, similar to retaliation bullets.
*   **Spellcard Activation & Execution (Level 2/3 - Server Triggered, Client Simulated):**
    *   Client (`PlayerShootingController`) sends `RequestSpellcardServerRpc`.
    *   Server (`SpellBarManager`) validates cost.
    *   Server (`ServerAttackSpawner`) triggers caster clear effect and banner RPC.
    *   Server (`ServerAttackSpawner`) loads first action of `SpellcardData` to check `applyRandomSpawnOffset`, calculates a single `sharedRandomOffset` if needed.
    *   Server (`ServerAttackSpawner`) calls `SpellcardNetworkHandler.ExecuteSpellcardClientRpc`, passing resource path, IDs, level, and the `sharedRandomOffset`.
    *   All clients receive RPC.
    *   Client (`ClientSpellcardExecutor`) loads `SpellcardData`, calculates origin (target field center).
    *   Client (`ClientSpellcardExecutor`) calls `ClientSpellcardActionRunner.RunSpellcardActions`.
    *   Client (`ClientSpellcardActionRunner`) executes the action sequence coroutine:
        *   Applies the *same* `sharedRandomOffset` to all actions.
        *   Spawns bullets from `ClientGameObjectPool`.
        *   Uses `ClientBulletConfigurer` to activate and initialize client-side behavior scripts on bullets.
        *   Handles delays and formations.
    *   Bullet movement and lifetime are handled by client-side components (`ClientLinearMovement`, `ClientProjectileLifetime`, etc.).
*   **Enemy Spawning & Behavior (Server-Initiated, Client-Simulated):**
    *   Server (`FairySpawner.cs`) determines wave data.
    *   Server calls `FairySpawnNetworkHandler.SpawnFairyWaveClientRpc` targeting all clients, passing wave data.
    *   Clients receive RPC, iterate wave data, get enemy prefabs (e.g., "NormalFairy") from `ClientGameObjectPool`.
    *   Clients initialize enemy position and path using `SplineWalker.InitializePath`.
    *   Enemy movement is handled client-side by `SplineWalker`. Enemy health by `ClientFairyHealth`.
*   **Enemy Death & Chain Reactions (Client-Side):**
    *   `BulletMovement` collision calls `ClientFairyHealth.TakeDamage(damage, bulletOwnerId)`.
    *   If health <= 0, `ClientFairyHealth` spawns a `ClientFairyShockwave` from `ClientGameObjectPool`, initializing it with damage parameters and `bulletOwnerId`.
    *   `ClientFairyShockwave` uses an expanding `CircleCollider2D` and `OnTriggerStay2D` with a tick rate limit (`damageTickRate`) to:
        *   Damage other nearby fairies/spirits (`otherHealth.TakeDamage(shockwaveDamage, originalKillerId)`).
        *   Clear certain bullets (e.g., `StageSmallBulletMoverScript.ForceReturnToPoolByBomb()`).
    *   If the kill was by the local player (`bulletOwnerId == LocalClientId`), `ClientFairyHealth` also calls `PlayerAttackRelay.LocalInstance.ReportFairyKillServerRpc()`.
*   **Health & Damage:** Player bullet collision is detected client-side (`BulletMovement` or `ClientFairyController`). Damage is applied locally to `ClientFairyHealth`. If this results in a kill, and the killer is the local player, the kill is reported to the server. Server handles consequences (retaliation bullets, score). Direct player health (`PlayerHealth`) might still be server-authoritative (TBD based on its implementation).
*   **Game State (Score, Rounds):** Managed by a server-authoritative `RoundManager`.
*   **Object Pooling:**
    *   **`ClientGameObjectPool.cs`:** Primary pool used by all clients for non-NetworkObject entities spawned via RPC commands or local actions (player bullets, enemy bullets, fairies, spirits, shockwaves, VFX).
    *   **`NetworkObjectPool.cs`:** Likely **DEPRECATED/UNUSED** unless specific server-authoritative `NetworkObject`s (e.g., a complex boss segment) require pooling.

## Important Scripts (Updated List)

*   **`ClientAuthMovement.cs`:** Client-authoritative player movement, updates `NetworkedPosition`.
*   **`PlayerShootingController.cs`:** Owner client input handler. Spawns local basic shots, sends RPCs for basic shots and requests for charge/spell attacks.
*   **`PlayerAttackRelay.cs`:** On Player prefab. Receives kill reports from `ClientFairyHealth` via `ReportFairyKillServerRpc`. Server-side logic determines retaliation bullet parameters and calls `EffectNetworkHandler`.
*   **`EffectNetworkHandler.cs`:** Server singleton. Receives requests from `PlayerAttackRelay` and sends `SpawnStageBulletClientRpc` to all clients.
*   **`FairySpawnNetworkHandler.cs`:** Server singleton. Receives requests from `FairySpawner` and sends `SpawnFairyWaveClientRpc` to all clients.
*   **`ClientGameObjectPool.cs`:** **Primary** client-side object pool for non-NetworkObjects.
*   **`PooledObjectInfo.cs`:** Attached to prefabs used by `ClientGameObjectPool`, stores string `PrefabID`.
*   **`BulletMovement.cs`:** `MonoBehaviour` on player basic shot prefabs. Handles client-side movement, passes `FiredByOwnerClientId` on collision, calls `ClientProjectileLifetime`.
*   **`StageSmallBulletMoverScript.cs`:** `MonoBehaviour` on stage bullet prefabs. Handles client-side movement, initialized by `EffectNetworkHandler` RPC, includes `ForceReturnToPoolByBomb`.
*   **`ClientProjectileLifetime.cs`:** `MonoBehaviour` for client-side pooled objects. Returns object to pool on timer/collision.
*   **`ClientFairyHealth.cs`:** `MonoBehaviour` on enemy prefabs. Manages health, spawns `ClientFairyShockwave` on death, conditionally calls `PlayerAttackRelay.ReportFairyKillServerRpc`.
*   **`ClientFairyController.cs`:** `MonoBehaviour` on enemy prefabs. Handles path completion pooling.
*   **`SplineWalker.cs`:** `MonoBehaviour` on enemy prefabs. Handles client-side path following.
*   **`ClientFairyShockwave.cs`:** `MonoBehaviour` on shockwave prefab. Handles collider expansion, periodic damage/clearing via `OnTriggerStay2D`, returns self to pool.
*   **`ClientShockwaveVisuals.cs`:** `MonoBehaviour` on shockwave prefab. Handles visual scaling/fading based on data from `ClientFairyShockwave`.
*   **`SpawnAreaManager.cs`:** Singleton providing spawn area positions based on `PlayerRole`.
*   **`PlayerDataManager.cs`:** Manages `PlayerData` structs containing `ClientId` and `PlayerRole`.
*   **`PlayerDeathBomb.cs`:** Server-side script on player(?). Sends `ClearBulletsInRadiusClientRpc` (likely via `EffectNetworkHandler` or similar).
*   **`ServerAttackSpawner.cs`:** Server-side. Still needed for orchestrating server-verified charge/spell attacks.
*   **`SpellBarManager.cs`:** Server-side. Still needed for managing spell costs.
*   **`FairySpawner.cs`:** Server-side. Calculates waves, calls `FairySpawnNetworkHandler`.
*   **~~`NetworkObjectPool.cs`~~:** Likely Deprecated/Unused.
*   **~~`ServerBasicShotSpawner.cs`~~:** Deprecated for player shots.
