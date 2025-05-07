# Projectile System Documentation

## Overview

Projectiles (bullets, lasers, orbs, etc.) are fundamental gameplay elements. This document outlines their common structure and interaction systems. The projectile system is now predominantly **Client-Side Simulated** based on server commands.

## Client-Simulated Player Projectiles (Basic Shots)

This model is used for standard player shots to enhance responsiveness.

### Structure / Components

Client-side projectile prefabs (e.g., `ReimuBullet`, `MarisaBullet`) generally have:

*   **`Collider2D`:** Used for collision detection on the client (often set as a Trigger).
*   **Visual Components:** `SpriteRenderer`, `Animator`, etc.
*   **`BulletMovement.cs`:** A `MonoBehaviour` that handles client-side movement and collision.
*   **`ClientProjectileLifetime.cs`:** A `MonoBehaviour` that manages the projectile's lifespan and returns it to the `ClientGameObjectPool`.
*   **`PooledObjectInfo.cs`:** Stores a `PrefabID` string, used by `ClientGameObjectPool`.
*   **NO `NetworkObject`:** These are simulated locally per client.

### Spawning and Synchronization Workflow

1.  **Owner Client (`PlayerShootingController.cs`):**
    *   Detects fire input.
    *   Spawns a projectile GameObject locally using `ClientGameObjectPool.Instance.GetObject(prefabId)`.
    *   Sets the projectile's position and rotation.
    *   Initializes its `BulletMovement` script (passing its `OwnerClientId`, speed, lifetime). The `FiredByOwnerClientId` is crucial for damage attribution.
    *   Calls `FireShotServerRpc(string prefabId, Vector3 position, Quaternion rotation, float speed, float lifetime)` on its own `PlayerShootingController`.
2.  **Server (`PlayerShootingController.FireShotServerRpc`):**
    *   Receives the `ServerRpc`.
    *   Calls `FireShotClientRpc`, relaying parameters to all clients.
3.  **All Clients (`PlayerShootingController.FireShotClientRpc`):**
    *   **Remote Clients (IsOwner == false):**
        *   Retrieves a projectile from their local `ClientGameObjectPool`.
        *   Sets position/rotation from RPC.
        *   Initializes `BulletMovement` with firer's `OwnerClientId` (from RPC or by looking up the NetworkObject of the firing player), speed, and lifetime.
    *   **Owner Client (IsOwner == true):** May ignore this RPC as it spawned the bullet locally, or use it for confirmation.

### Client-Side Movement & Collision (`BulletMovement.cs`)

*   Attached to player shot prefabs.
*   `Update()` moves the projectile.
*   Initialized with `OwnerClientId`, speed, and lifetime.
*   `OnTriggerEnter2D` handles collisions:
    *   If it collides with an enemy (e.g., object with `ClientFairyHealth`), it calls `other.GetComponent<ClientFairyHealth>().TakeDamage(damageAmount, this.FiredByOwnerClientId)`.
    *   After collision, it typically tells its `ClientProjectileLifetime` component to return the bullet to the pool.

### Client-Side Lifetime & Pooling (`ClientProjectileLifetime.cs`)

*   Attached to client-pooled projectile prefabs.
*   Returns the object to `ClientGameObjectPool` after a set duration or when `ForceReturnToPool()` is called.

## Server-Initiated, Client-Simulated Stage Projectiles (Retaliation/Spirit Bullets)

These bullets are spawned based on server decisions but simulated client-side.

### Spawning Workflow

1.  **Server Event:** An event occurs (e.g., `PlayerAttackRelay.ReportFairyKillServerRpc` is processed).
2.  **Server Logic (`PlayerAttackRelay`):**
    *   Determines the target player for the bullet (e.g., the opponent).
    *   Calculates bullet parameters: `prefabId` (e.g., "StageSmallBullet"), initial spawn position (normalized), `actualSpeed`, and `direction` (Vector2 for angle). Speed and angle can be randomized within configured ranges (`minStageBulletSpeed`, `maxStageBulletSpeed`, `maxStageBulletAngleOffset` in `PlayerAttackRelay`).
3.  **Server Command (`EffectNetworkHandler.cs`):**
    *   The server calls `EffectNetworkHandler.Instance.SpawnStageBulletClientRpc(explicitTargetClientId, bulletPrefabID, normalizedSpawnPosition, actualSpeed, direction, clientRpcParamsForAll)`.
    *   `explicitTargetClientId` indicates on whose field the bullet should conceptually spawn.
    *   The RPC is sent to *all* clients.
4.  **All Clients (Execution of `SpawnStageBulletClientRpc` in `EffectNetworkHandler`):**
    *   Each client retrieves the bullet GameObject from its local `ClientGameObjectPool` using `bulletPrefabID`.
    *   Determines the actual world spawn position:
        *   Uses `explicitTargetClientId` to get the `PlayerRole` via `PlayerDataManager`.
        *   Uses the `PlayerRole` to get the correct spawn zone center from `SpawnAreaManager.Instance.GetSpawnCenterForTargetedPlayer(role)`.
        *   Combines the zone center with `normalizedSpawnPosition` to get the final world position.
    *   Initializes the bullet's movement script (e.g., `StageSmallBulletMoverScript.Initialize(actualSpeed, direction, lifetime)`).
    *   Activates the bullet.

### Movement (`StageSmallBulletMoverScript.cs`)

*   `MonoBehaviour` on stage bullet prefabs.
*   Handles client-side movement based on initialized speed and direction.
*   Includes `ForceReturnToPoolByBomb()` for clearing.
*   Handles `OnTriggerEnter2D` for collisions (e.g., with player hitbox, or with enemies if stage bullets can damage enemies).
    *   If it collides with an enemy, it would call `ClientFairyHealth.TakeDamage(damage, 0);` (or a special non-player owner ID if needed).
    *   Returns self to pool on collision or lifetime expiry via `ClientProjectileLifetime`.

## Collision & Damage

*   **Client-Simulated Bullets (Player & Stage):**
    *   Collision is detected client-side by the bullet's movement script (`BulletMovement.cs`, `StageSmallBulletMoverScript.cs`).
    *   The script calls `TakeDamage(damageAmount, attackerOwnerClientId)` on the `ClientFairyHealth` component of the hit enemy.
    *   `ClientFairyHealth`, on the client where the damage occurred:
        *   If `attackerOwnerClientId == NetworkManager.Singleton.LocalClientId` (i.e., *this* client's player fired the killing shot), it calls `PlayerAttackRelay.LocalInstance.ReportFairyKillServerRpc()`.
        *   Spawns a `ClientFairyShockwave` locally.
    *   The server receives the `ReportFairyKillServerRpc` and orchestrates consequences (e.g., retaliation bullets, scoring).

## Bullet Clearing Mechanisms

Bullet clearing is handled client-side, often triggered by RPCs or local effects.

### 1. Fairy/Spirit Death Shockwave Clearing

*   When an enemy with `ClientFairyHealth` dies, it spawns a `ClientFairyShockwave`.
*   `ClientFairyShockwave.cs` (client-side):
    *   Has a `CircleCollider2D` that expands over `_expansionDuration`.
    *   In `OnTriggerStay2D` (with a tick rate limit to avoid multiple clears/damage per frame per object):
        *   It checks collided objects for a clearable bullet script (e.g., `StageSmallBulletMoverScript`).
        *   If found, it calls `bulletMover.ForceReturnToPoolByBomb()`.
        *   It also damages other enemies (`otherHealth.TakeDamage(shockwaveDamage, _ownerIdForChainedDamage)`).

### 2. Player Death Bomb Clearing

*   When a player uses a death bomb (spellcard), `PlayerDeathBomb.cs` (server-side) is invoked.
*   `PlayerDeathBomb.cs` calls a `ClientRpc` (e.g., via `EffectNetworkHandler.Instance.ClearBulletsInRadiusClientRpc(position, radius, paramsToAllClients)`).
*   **All Clients (Executing `ClearBulletsInRadiusClientRpc`):**
    *   Iterate through all *active* GameObjects currently managed by their `ClientGameObjectPool.Instance.GetAllActiveObjects()`.
    *   For each active object:
        *   Check if it's within the specified radius of the bomb's position.
        *   Try to get a component that allows forced pooling (e.g., `StageSmallBulletMoverScript`, `ClientProjectileLifetime`).
        *   If found, call its `ForceReturnToPoolByBomb()` or `ForceReturnToPool()` method.

### `IClearable` Interface

*   While not strictly enforced by a central system, bullet scripts like `StageSmallBulletMoverScript` and `ClientProjectileLifetime` implement methods like `ForceReturnToPoolByBomb()` or `ForceReturnToPool()`. This serves a similar purpose to an `IClearable` interface, providing a common way to despawn bullets.

## Key Projectile Prefabs

*   Player Basic Shots: `ReimuBulletPrefab`, `MarisaBulletPrefab` (Contain `BulletMovement`, `ClientProjectileLifetime`, `PooledObjectInfo`).
*   Stage Bullets: `StageSmallBulletPrefab`, `StageLargeBulletPrefab` (Contain `StageSmallBulletMoverScript` (or similar), `ClientProjectileLifetime`, `PooledObjectInfo`).
*   Effects that interact with projectiles: `FairyShockwavePrefab` (Contains `ClientFairyShockwave`, `ClientShockwaveVisuals`, `PooledObjectInfo`).

## Key Scripts

*   **`PlayerShootingController.cs`:** Owner client spawns local shots, initializes `BulletMovement` with `OwnerClientId`, initiates ServerRpc->ClientRpc for remote visuals.
*   **`BulletMovement.cs`:** Client-side movement for player shots. Passes `FiredByOwnerClientId` to `ClientFairyHealth.TakeDamage()`.
*   **`StageSmallBulletMoverScript.cs`:** Client-side movement for stage bullets. Initialized with speed/angle. Has `ForceReturnToPoolByBomb()`. Can damage enemies.
*   **`ClientProjectileLifetime.cs`:** Manages client-side lifetime and return to `ClientGameObjectPool`. Has `ForceReturnToPool()`.
*   **`ClientGameObjectPool.cs`:** Primary client-side pool.
*   **`PooledObjectInfo.cs`:** Identifies prefabs for pooling.
*   **`ClientFairyHealth.cs`:** Receives damage, conditionally reports kills, spawns shockwaves.
*   **`ClientFairyShockwave.cs`:** Client-side effect. Clears bullets and damages enemies in radius via `OnTriggerStay2D`.
*   **`EffectNetworkHandler.cs`:** Server singleton. Relays `SpawnStageBulletClientRpc` and `ClearBulletsInRadiusClientRpc`.
*   **`PlayerAttackRelay.cs`:** Server-side logic on player. Receives kill reports, initiates retaliation bullets via `EffectNetworkHandler`.
*   **`PlayerDeathBomb.cs`:** Server-side logic. Initiates bomb clearing RPC via `EffectNetworkHandler`.
*   **`SpawnAreaManager.cs`:** Provides spawn locations for stage bullets.
*   **`ServerAttackSpawner.cs` (Spellcards):** Server-side. For complex spellcards, likely still uses server RPCs to tell clients to spawn specific sequences of client-simulated projectiles/effects.

## Server-Authoritative Projectiles (Spellcards, Enemy Bullets - If Applicable)

For projectiles where authority and precise synchronized state are critical (e.g., complex spellcard patterns, enemy bullets that must deal damage authoritatively), the system might still use server-spawned `NetworkObject`s.

*   **Base Structure:** These would retain `NetworkObject`, and their movement/lifetime scripts (`LinearMovement.cs`, `NetworkBulletLifetime.cs`, `StageSmallBulletMoverScript.cs`) would be `NetworkBehaviour`s with server-side logic, synchronizing state via `NetworkVariable`s or `NetworkTransform` (if reliable).
*   **Spawning:** The server (e.g., `ServerAttackSpawner`, enemy AI scripts) would spawn these using `NetworkObjectPool` (if pooled) or by instantiating and spawning `NetworkObject`s directly.
*   **Movement & Logic:** Runs on the server. Client visuals are updated via network synchronization.
*   **Collision & Damage:** Detected and applied authoritatively on the server.

## Collision & Damage (General Approach)

*   **Client-Side Basic Shots:** Collisions detected by `BulletMovement.cs` are primarily for visual feedback (despawning the bullet). If a client-side bullet *hitting an enemy* needs to register damage, the `BulletMovement` script (or another client-side script) would need to send a `ServerRpc` to the server indicating the hit. The server would then validate this (if necessary) and apply damage authoritatively.
*   **Server-Side Projectiles:** Collision is detected and damage is applied authoritatively on the server. Player health (`CharacterStats`) and enemy health would be updated on the server and synchronized to clients.
*   **Physics Layers:** The Physics 2D Layer Collision Matrix remains crucial for filtering which objects can interact.

## Clearing Effects (`IClearable` Interface)

This system likely remains server-authoritative if it affects game state (e.g., scoring, enemy state).
*   If client-side visual bullets need to *react* to a clear event (e.g., a bomb effect initiated by the server), the server would send a `ClientRpc` to the relevant clients, which would then despawn their visual bullets in the affected area.
*   Server-authoritative projectiles implementing `IClearable` would be cleared directly by server logic. 