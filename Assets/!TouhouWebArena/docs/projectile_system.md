# Projectile System Documentation

## Overview

Projectiles (bullets, lasers, orbs, etc.) are fundamental gameplay elements. This document outlines their common structure and interaction systems. The projectile system for **basic player shots is now client-authoritative**, while complex spellcard projectiles or enemy-fired projectiles might still use server-authoritative logic or a hybrid model.

## Client-Authoritative Basic Player Projectiles

This model is used for standard player shots to enhance responsiveness and reduce server load.

### Structure / Components (Client-Side Projectiles)

Client-side projectile prefabs (e.g., `ReimuBullet`, `MarisaBullet`) generally have:

*   **`Collider2D`:** Used for visual collision detection on the client (often set as a Trigger).
*   **Visual Components:** `SpriteRenderer`, `Animator`, etc.
*   **`BulletMovement.cs`:** A `MonoBehaviour` that handles client-side visual movement (e.g., `transform.Translate`).
*   **`ClientProjectileLifetime.cs`:** A `MonoBehaviour` that manages the projectile's lifespan and returns it to the `ClientGameObjectPool`.
*   **`PooledObjectInfo.cs`:** Stores a `PrefabID` string, used by `ClientGameObjectPool` to identify and manage the prefab type.
*   **NO `NetworkObject`:** These are purely client-side visual representations for basic shots.

### Spawning and Synchronization Workflow (Basic Player Shots)

1.  **Owner Client (`PlayerShootingController.cs`):
    *   Detects fire input.
    *   Spawns a projectile GameObject locally using `ClientGameObjectPool.Instance.GetObject(prefabId)`.
    *   Sets the projectile's position and rotation.
    *   Initializes its `BulletMovement` script (with speed) and `ClientProjectileLifetime` script (with lifetime duration).
    *   Calls a `FireShotServerRpc(string prefabId, Vector3 position, Quaternion rotation, float speed, float lifetime)` method on its own `PlayerShootingController` instance.
2.  **Server (`PlayerShootingController.FireShotServerRpc`):
    *   Receives the `ServerRpc` from the owning client.
    *   Immediately calls a `FireShotClientRpc` method, relaying all the received parameters (prefabId, position, rotation, speed, lifetime) to all clients.
3.  **All Clients (`PlayerShootingController.FireShotClientRpc`):
    *   **If `IsOwner` is true (the client that originally fired):** The method typically returns early, as the bullet was already spawned locally.
    *   **If `IsOwner` is false (remote clients):**
        *   Retrieves a projectile GameObject from their *local* `ClientGameObjectPool.Instance.GetObject(prefabId)` using the received `prefabId`.
        *   Sets the projectile's position and rotation based on the RPC parameters.
        *   Initializes its `BulletMovement` script with the received `speed`.
        *   Initializes its `ClientProjectileLifetime` script with the received `lifetime`.
        *   The projectile is now independently simulated visually on this remote client.

### Client-Side Movement (`BulletMovement.cs`)

*   Attached to basic player shot prefabs.
*   A simple `MonoBehaviour` (not a `NetworkBehaviour`).
*   Its `Update()` method moves the projectile using `transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.Self)` or similar.
*   Speed is set via an `Initialize(float speed, float lifetime)` method.
*   Handles `OnTriggerEnter2D` for visual feedback. If it collides with objects tagged "Fairy" or "Spirit", it tells its `ClientProjectileLifetime` component to return the bullet to the pool.
*   Does not handle "OpponentHitbox" or "WorldBoundary" collisions; lifetime is managed by `ClientProjectileLifetime`.

### Client-Side Lifetime & Pooling (`ClientProjectileLifetime.cs`)

*   Attached to basic player shot prefabs.
*   Initialized with a lifetime duration.
*   Counts down its lifetime. When expired, it calls `ClientGameObjectPool.Instance.ReturnObject()` to return itself to the pool.
*   Provides a `ForceReturnToPool()` method that can be called (e.g., by `BulletMovement` on collision) to immediately return the object to the pool.

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

## Key Projectile Prefabs

*   Client-Side (Basic Player Shots): `ReimuBulletPrefab`, `MarisaBulletPrefab` (Contain `BulletMovement`, `ClientProjectileLifetime`, `PooledObjectInfo`).
*   Server-Side (Examples, if still used): `StageSmallBulletPrefab`, `RedSmallCircleBullet_Variant`, Charge Attack Prefabs (Would contain `NetworkObject` and server-controlled scripts).

## Key Scripts

*   **Client-Side Projectile System:**
    *   `PlayerShootingController.cs`: (Owner) Spawns local shots, initiates ServerRpc->ClientRpc for remote visuals.
    *   `BulletMovement.cs`: Client-side visual movement and basic collision for player shots.
    *   `ClientProjectileLifetime.cs`: Manages client-side lifetime and return to pool for player shots.
    *   `ClientGameObjectPool.cs`: Manages pools of non-NetworkObject GameObjects for client visuals.
    *   `PooledObjectInfo.cs`: Helper for `ClientGameObjectPool`.
*   **Server-Side / Hybrid Projectile System (If applicable for spellcards/enemies):
    *   Movement: `LinearMovement.cs` (if server-controlled), `DelayedHoming.cs` (if server-controlled), `StageSmallBulletMoverScript.cs`.
    *   Lifetime/Pooling: `NetworkBulletLifetime.cs`, `StageSmallBulletMoverScript.cs`, `NetworkObjectPool.cs` (for `NetworkObject`s).
*   Interactions: `IClearable.cs`, `PlayerHitbox.cs`, etc. (Primarily server-authoritative logic).
*   Spawners: `ServerAttackSpawner.cs` (for server-authoritative spellcards/attacks), enemy AI scripts. 