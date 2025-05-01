# Projectile System Documentation

## Overview

Projectiles (bullets, lasers, orbs, etc.) are fundamental gameplay elements, used in basic shots, charge attacks, spellcards, and extra attacks. This document outlines their common structure, movement behaviors, and interaction systems. The projectile system is server-authoritative, with the server controlling spawning, movement logic, and collision detection.

## Base Structure / Components

Projectile prefabs generally share a common set of components:

*   **`NetworkObject`:** Essential for network synchronization.
*   **`Collider2D`:** Used for collision detection (often set as a Trigger).
*   **Visual Components:** `SpriteRenderer`, `Animator`, `ParticleSystem`, etc. for appearance.
*   **Movement Script(s):** One or more scripts defining how the projectile moves (see below). Attached and configured by the spawning logic (e.g., `ServerAttackSpawner`).
*   **Pooling/Lifetime Script:**
    *   **`NetworkBulletLifetime.cs`:** Used for spellcard bullets. Manages lifetime, boundary checks, pooling, and implements `IClearable`.
    *   **`StageSmallBulletMoverScript.cs`:** Used for stage bullets (from Fairies/Spirits). Manages lifetime, movement, pooling, and implements `IClearable`.
    *   Other projectiles (player shots, charge attacks, extra attacks) might use `BulletMovement.cs` or have custom lifetime/collision logic.
*   **`PoolableObjectIdentity.cs`:** Required for integration with the `NetworkObjectPool`. Contains the unique `PrefabID` string which **must be set in the Inspector on the prefab asset**.
*   **`ProjectileDamager.cs`:** (Optional) Can be attached to projectiles to specify a damage value other than the default (usually 1).

There isn't a single mandatory "BaseProjectile" script; behavior is primarily defined by the combination of attached component scripts.

## Movement Behaviors

These scripts define how projectiles move after being spawned. They reside in `/Scripts/Spellcards/Behaviors/` or `/Scripts/Projectiles/` and are typically controlled server-side. **For spellcard projectiles, these components are attached to the prefab and configured/enabled by `ServerBulletConfigurer` based on `SpellcardAction` data.**

*   **`LinearMovement.cs`:**
    *   Moves the GameObject forward along its `transform.up` direction at a constant speed.
    *   Speed is configured via the `Initialize(float initialSpeed)` method, called by the spawner on the server *before* the object is spawned.
    *   Movement logic runs *only on the server*. Client position is updated via `NetworkObject` transform sync (if attached).
*   **`DelayedHoming.cs`:**
    *   Moves linearly (`transform.up` at `initialSpeed`) for a set duration (`homingDelay`).
    *   After the delay, it locks onto the *direction* towards the initially captured target position.
    *   Continues moving in that locked direction at `homingSpeed`.
    *   Configured via `Initialize(...)` method, called by the spawner on the server *before* the object is spawned. Note: `targetId` parameter is currently unused; it homes towards the initial position vector.
    *   Movement logic runs *only on the server*.
*   **(Other behaviors):** Additional movement scripts can be created and attached to prefabs to implement more complex patterns (e.g., wavy, accelerating, orbiting).

## Lifetime Management

*   **Pooled Projectiles:** Scripts like `NetworkBulletLifetime.cs` and `StageSmallBulletMoverScript.cs` handle returning the projectile to the `NetworkObjectPool` after a `maxLifetime` duration or if boundary checks fail (server-side).
*   **Player Projectiles:** `BulletMovement.cs` uses `Invoke(nameof(ReturnToPool), bulletLifetime)` for time-based pooling.
*   **Collision:** Projectiles are typically returned to the pool immediately upon hitting a valid target (player or enemy), handled within the projectile's own collision logic (e.g., `BulletMovement.OnTriggerEnter2D`).

## Collision & Damage

*   **Detection:** Collision detection happens **on the server**. Projectile `Collider2D` components (usually triggers) detect collisions with other objects. The **Physics 2D Layer Collision Matrix** is the primary method used to filter valid interactions (e.g., player shots only hit enemies/opponent, enemy projectiles only hit players). Tags (`PlayerShot`, `Fairy`, `Spirit`) are used within collision scripts for specific logic checks.
*   **Damage Application:**
    *   Player shots (`BulletMovement`) apply damage via methods on the target (`Fairy.ApplyLethalDamage`, `SpiritController.TakeDamage`).
    *   Enemy projectiles hitting the player are handled by `PlayerHitbox.cs`, which detects the collision (filtered by layers) and calls `PlayerHealth.TakeDamage(1)`.
    *   Damage is always applied authoritatively on the server.

## Clearing Effects (`IClearable` Interface)

Certain projectiles (and enemies) implement the `IClearable` interface, allowing them to be removed by area effects like bombs or shockwaves.

*   **Interface:** `IClearable` defines a `Clear(bool forceClear, PlayerRole sourceRole)` method.
*   **Implementation:** Found on scripts like `NetworkBulletLifetime.cs` (for spellcard bullets) and `StageSmallBulletMoverScript.cs` (for stage bullets). **Not** implemented on standard player shots (`BulletMovement.cs`).
    *   Both `NetworkBulletLifetime` and `StageSmallBulletMoverScript` provide a `public NetworkVariable<PlayerRole> TargetPlayerRole`.
*   **`isNormallyClearable` Flag:** Components implementing `IClearable` have a public boolean field `isNormallyClearable` (settable in the prefab inspector). 
    *   If `forceClear` is `true` (e.g., Player Death Bomb), the `Clear` method always despawns/pools the object **if the bullet's `TargetPlayerRole` matches the bombing player's role**.
    *   If `forceClear` is `false` (e.g., Fairy Shockwave), the `Clear` method only despawns/pools the object if `isNormallyClearable` is also `true`.
*   **Triggers:**
    *   **Player Death Bomb:** Uses `Physics2D.OverlapCircleAll`. Checks the `TargetPlayerRole` of the detected `IClearable` bullet (`NetworkBulletLifetime.TargetPlayerRole.Value`, `StageSmallBulletMoverScript.TargetPlayerRole.Value`). Only calls `Clear(true, ...)` if the bullet's `TargetPlayerRole` matches the role of the player who died.
    *   **Fairy Shockwave:** Uses trigger colliders (`OnTriggerEnter2D`) and calls `Clear(false, ...)`. Shockwaves typically only collide with objects on the same side due to physics layers/positioning, but the `Clear` method itself doesn't perform a role check for shockwaves.

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

*   Movement: `LinearMovement.cs`, `DelayedHoming.cs`, `BulletMovement.cs`, `StageSmallBulletMoverScript.cs`
*   Lifetime/Pooling: `NetworkBulletLifetime.cs`, `StageSmallBulletMoverScript.cs`, `BulletMovement.cs`, `NetworkObjectPool.cs`, `PoolableObjectIdentity.cs`
*   Interactions: `IClearable.cs` (Interface), `NetworkBulletLifetime.cs` (Implementation), `StageSmallBulletMoverScript.cs` (Implementation), `PlayerDeathBomb.cs` (Forced Clear Trigger), `Shockwave.cs` (Normal Clear Trigger), `PlayerHitbox.cs` (Player Damage Receiver), `FairyCollisionHandler.cs` (Enemy Damage Receiver), `SpiritController.cs` (Enemy Damage Receiver)
*   Spawners: `ServerAttackSpawner.cs` (Player Attacks/Spellcards), `StageSmallBulletSpawner.cs`, `SpiritController.cs` (Timeout Spawn), `Fairy.cs` (Death Spawn) 