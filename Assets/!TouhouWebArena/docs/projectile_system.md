# Projectile System Documentation

## Overview

Projectiles (bullets, lasers, orbs, etc.) are fundamental gameplay elements, used in basic shots, charge attacks, spellcards, and extra attacks. This document outlines their common structure, movement behaviors, and interaction systems. The projectile system is server-authoritative, with the server controlling spawning, movement logic, and collision detection.

## Base Structure / Components

Projectile prefabs generally share a common set of components:

*   **`NetworkObject`:** Essential for network synchronization.
*   **`Collider2D`:** Used for collision detection (often set as a Trigger).
*   **Visual Components:** `SpriteRenderer`, `Animator`, `ParticleSystem`, etc. for appearance.
*   **Movement Script(s):** One or more scripts defining how the projectile moves (see below). Attached and configured by the spawning logic (e.g., `PlayerShooting` server-side).
*   **`NetworkBulletLifetime.cs`:** Manages lifetime and boundary checks (server-side).
*   **`PoolableObjectIdentity.cs`:** Required for integration with the `NetworkObjectPool`, contains the unique `PrefabID`.

There isn't a single mandatory "BaseProjectile" script; behavior is primarily defined by the combination of attached component scripts.

## Movement Behaviors

These scripts define how projectiles move after being spawned. They reside in `/Scripts/Spellcards/Behaviors/` or `/Scripts/Projectiles/` and are typically controlled server-side.

*   **`LinearMovement.cs`:**
    *   Moves the GameObject forward along its `transform.up` direction at a constant speed.
    *   Speed is configured via the `Initialize(float initialSpeed)` method called by the spawner.
    *   Movement logic runs *only on the server*. Client position is updated via `NetworkObject` transform sync.
*   **`DelayedHoming.cs`:**
    *   Moves linearly (`transform.up` at `initialSpeed`) for a set duration (`homingDelay`).
    *   After the delay, it locks onto the *direction* towards the initially captured target position.
    *   Continues moving in that locked direction at `homingSpeed`.
    *   Configured via `Initialize(float initialSpeed, float homingSpeed, float homingDelay, ulong targetId, Vector3 targetPosition)`. Note: `targetId` is currently unused; it homes towards the initial position vector.
    *   Movement logic runs *only on the server*.
*   **(Other behaviors):** Additional movement scripts can be created and attached to prefabs to implement more complex patterns (e.g., wavy, accelerating, orbiting).

## Lifetime Management

*   **`NetworkBulletLifetime.cs`:** This server-side script is attached to most pooled projectiles.
    *   **Max Lifetime:** Returns the projectile to the `NetworkObjectPool` after `maxLifetime` seconds.
    *   **Boundary Check:** If `enforceBounds` is true, it checks if the projectile has crossed the center line (`boundaryX`) onto the wrong side (determined by `keepOnPositiveSide`). If out of bounds, it's returned to the pool.
    *   **Pooling Interaction:** Calls `NetworkObjectPool.Instance.ReturnNetworkObject()` to despawn the `NetworkObject` without destroying it and return it to the pool.

## Collision & Damage

*   **Detection:** Collision detection happens **on the server**. Projectile `Collider2D` components (usually triggers) detect collisions with other objects. Tags and Layers are likely used to filter valid interactions (e.g., player bullets only hit enemies/opponent, enemy bullets only hit players).
*   **Damage Application:**
    *   The `NetworkBulletLifetime.cs` script currently includes basic `OnTriggerEnter2D` logic. If it collides with an object having a `PlayerHealth` component, it calls `playerHealth.TakeDamage(1)` on the server.
    *   Other projectile types (e.g., charge attacks, extra attacks) might have their own specific collision scripts to handle different damage amounts or effects.
    *   Damage is always applied authoritatively on the server via scripts like `PlayerHealth`.

## Special Interactions

*   **Bomb Clearability (`IClearableByBomb`):**
    *   Projectiles (and other objects like enemies) that should be destroyed by a player's death bomb must implement the `IClearableByBomb` interface (defined in `IClearableByBomb.cs`).
    *   This interface likely defines a method (e.g., `Clear()` or `HandleBombClear()`) that contains the logic for being destroyed/returned to pool.
    *   The `PlayerDeathBomb.cs` script (server-side) finds all objects implementing `IClearableByBomb` within the bomb radius and calls their clearing method.
*   **(Other interactions):** Systems for projectile reflection, piercing, status effects, etc., are not currently implemented but could be added via new component scripts.

## Key Projectile Prefabs

*   `ReimuBulletPrefab`, `MarisaBulletPrefab` (Player basic shots)
*   `StageSmallBulletPrefab`, `StageLargeBulletPrefab` (Enemy/Stage shots)
*   `ReimuExtraAttackOrb`, `MarisaExtraAttackEarthlightRay` (Extra Attacks)
*   `RedSmallCircleBullet_Variant`, etc. (Spellcard bullets)
*   Charge Attack Prefabs (Assigned in `CharacterStats`)

## Key Scripts

*   Movement: `LinearMovement.cs`, `DelayedHoming.cs`
*   Lifetime/Pooling: `NetworkBulletLifetime.cs`, `NetworkObjectPool.cs`, `PoolableObjectIdentity.cs`
*   Interactions: `IClearableByBomb.cs`, `PlayerDeathBomb.cs` (handler), `PlayerHealth.cs` (damage receiver)
*   Spawners: `PlayerShooting.cs`, `SpellcardExecutor.cs` (logic likely in `PlayerShooting`), `ExtraAttackManager.cs` 