# Projectile System Documentation

## Overview

Projectiles (bullets, lasers, orbs, etc.) are fundamental gameplay elements in Touhou Web Arena. This document outlines their structure, lifecycle, and interaction systems.

The projectile system for player spellcards and illusion attacks is primarily **Client-Simulated** for responsiveness and visual fidelity. The server dictates *what* projectiles to fire and *their properties* through spellcard data and RPCs, but the clients are responsible for spawning the visual representation, moving them according to defined behaviors, and handling visual-only collisions (like with walls). Critical game state changes resulting from projectile interactions (e.g., player damage, illusion death) are ultimately reported to and validated by the server.

This document covers:
*   Core projectile components.
*   The lifecycle of spellcard-based projectiles (including those from illusions).
*   Client-side movement behaviors and configuration.
*   Projectile despawning mechanisms (timed, wall collision, clearing effects).
*   Collision detection and its relation to damage application.
*   (If applicable) Other projectile types like stage-specific or basic attack projectiles.

## Core Client-Side Projectile Components

Client-side projectile prefabs (e.g., `RedSmallCircleBullet`, `BlueNeedleBullet`) are typically equipped with the following components:

*   **`Collider2D`:** For client-side collision detection (e.g., against stage walls or, in some cases, visual-only hit detection). Often set as a Trigger.
*   **Visual Components:** `SpriteRenderer`, `Animator`, etc., for appearance.
*   **`ClientProjectileLifetime.cs`:** Manages the projectile's timed lifespan and returns it to the `ClientGameObjectPool`. Handles forced despawning.
*   **`ClientBulletWallCollision.cs`:** (Optional, common) Detects collisions with stage boundaries (e.g., "StageWalls" layer) and tells `ClientProjectileLifetime` to despawn the bullet.
*   **`PooledObjectInfo.cs`:** Stores a `PrefabID` string used by `ClientGameObjectPool` for efficient reuse.
*   **Movement Behavior Scripts (All Disabled by Default):**
    *   `ClientLinearMovement.cs`
    *   `ClientHomingMovement.cs`
    *   `ClientSpiralMovement.cs`
    *   `ClientDelayedHoming.cs`
    *   `ClientDoubleHoming.cs`
    *   `ClientDelayedRandomTurn.cs`
    *   (And any other specialized client-side movement behaviors)
*   **NO `NetworkObject`:** These are visual representations simulated locally on each client based on server instructions.

## Spellcard Projectile Lifecycle & Spawning

Projectiles for Level 1-4 spellcards, including those fired by Level 4 Illusions, follow a server-defined, client-executed model.

### 1. Triggering & Server Definition
    
*   **Server Action:** An action on the server (e.g., player uses a spellcard, an illusion decides to attack) initiates the projectile sequence.
*   **Data Loading:** The server loads the relevant `SpellcardData` (for Levels 1-3) or `Level4SpellcardData` (which contains `AttackPatternData` for illusions). This data defines `SpellcardAction` lists.
*   **RPC to Clients:**
    *   For Level 1-3: `ServerAttackSpawner` calls `ExecuteSpellcardClientRpc` on `SpellcardNetworkHandler`, sending the spellcard resource path and target information.
    *   For Level 4 Illusions: `ServerIllusionOrchestrator` calls `ExecuteAttackPatternClientRpc` on the `ClientIllusionView` associated with that illusion, sending the chosen `AttackPatternData`'s ID/details, movement parameters (if any), and target information.

### 2. Client-Side Reception & Execution (`ClientSpellcardActionRunner` / `ClientIllusionView`)

*   **`SpellcardNetworkHandler` (for L1-3):** Receives the RPC, loads the `SpellcardData`, and uses `ClientSpellcardActionRunner.RunSpellcardActions` to execute the spellcard's actions.
*   **`ClientIllusionView` (for L4 Illusions):**
    *   Receives `ExecuteAttackPatternClientRpc`.
    *   Loads the specified `AttackPatternData`.
    *   If the illusion is performing movement *during* the attack (`isMovingWithAttack = true`):
        *   It starts its `AnimateIllusionMovementAndAttack` coroutine. This coroutine Lerps the illusion's visual position/orientation on the client.
        *   It calls `ClientSpellcardActionRunner.RunSpellcardActionsDynamicOrigin`, passing its own `transform` as the `originTransform`. This allows bullets to spawn from the moving illusion.
    *   If the illusion is static for the attack:
        *   It calls `ClientSpellcardActionRunner.RunSpellcardActions` using its current (static) transform.
*   **`ClientSpellcardActionRunner.cs`:**
    *   Iterates through the `SpellcardAction` list from the data.
    *   For each action that spawns bullets:
        *   Obtains a bullet prefab instance from `ClientGameObjectPool.Instance.GetObject()`.
        *   Sets initial position and orientation (potentially from a dynamic origin for moving illusions).
        *   **Crucially, calls `ClientBulletConfigurer.ConfigureBullet()` for each spawned instance.**

### 3. Bullet Configuration (`ClientBulletConfigurer.cs`)

This static helper class is vital for setting up individual bullets:

*   Takes the `GameObject bulletInstance`, `SpellcardAction action`, `casterClientId`, `targetClientId`, and `bulletIndex` as parameters.
*   **Lifetime:** Initializes `ClientProjectileLifetime` on the bullet using `action.lifetime` (or a default if not specified). This must happen *after* the bullet GameObject is set active.
*   **Movement Behavior Reset:** Deactivates all potential movement behavior scripts attached to the bullet prefab (e.g., `ClientLinearMovement`, `ClientHomingMovement`, etc.) to ensure a clean state.
*   **Specific Behavior Activation:** Based on `action.behavior` (e.g., `BehaviorType.Linear`, `BehaviorType.Homing`):
    *   Gets the corresponding movement script (e.g., `bulletInstance.GetComponent<ClientLinearMovement>()`).
    *   Initializes it with parameters from the `action` (e.g., `speed`, `homingSpeed`, `homingDelay`, `targetPlayerId`, `bulletIndex` for speed increments).
    *   Enables the chosen movement script (`linear.enabled = true`).

### 4. Client-Side Movement Behaviors

These are individual `MonoBehaviour` scripts attached to bullet prefabs. `ClientBulletConfigurer` enables and initializes the correct one per `SpellcardAction`.

*   **`ClientLinearMovement.cs`:** Moves the bullet in a straight line. Can have initial speed, speed increment per bullet in a volley, and smooth speed transitions.
*   **`ClientHomingMovement.cs`:** Steers the bullet towards a target's position (at the time of initialization or updated if more sophisticated).
*   **`ClientSpiralMovement.cs`:** Moves the bullet in a spiral pattern.
*   **`ClientDelayedHoming.cs`:** Moves linearly for a delay, then homes.
*   **`ClientDoubleHoming.cs`:** Performs two phases of homing with an intermediate delay.
*   **`ClientDelayedRandomTurn.cs`:** Moves linearly, then turns randomly within a specified angle.
*   All movement scripts are responsible for updating `transform.position` (and sometimes `transform.rotation`) in their `Update()` or `FixedUpdate()` methods.

## Projectile Despawning Mechanisms

Client-side projectiles are removed from the game world and returned to the `ClientGameObjectPool` through several mechanisms:

### 1. Timed Despawn (`ClientProjectileLifetime.cs`)

*   This is the primary despawn method for most projectiles.
*   When `ClientBulletConfigurer.ConfigureBullet()` initializes a bullet, it also calls `Initialize(lifetime)` on the bullet's `ClientProjectileLifetime` component.
*   The `lifetime` is typically derived from `SpellcardAction.lifetime`. If `action.lifetime` is 0 or not set, a default lifetime is used.
*   `ClientProjectileLifetime` tracks its active time and calls `ReturnToPool()` when `_timeActive >= _lifetime`.

### 2. Wall Collision (`ClientBulletWallCollision.cs`)

*   This component is attached to projectile prefabs that should despawn upon hitting stage boundaries.
*   It requires a `Collider2D` on the same GameObject.
*   In `Awake()`, it caches the "StageWalls" layer.
*   `OnTriggerEnter2D()` and/or `OnCollisionEnter2D()`:
    *   Checks if the colliding object is on the "StageWalls" layer.
    *   If so, it calls `_projectileLifetime.ForceReturnToPool()`, where `_projectileLifetime` is a cached reference to the `ClientProjectileLifetime` component on the same bullet.

### 3. Explicit Clearing Effects (e.g., Bombs, Special Abilities)

*   Mechanisms like player "death bombs" or other special spellcard effects might need to clear a large number of bullets from the screen.
*   **Server Initiation:** Typically, the server determines such an event occurs (e.g., `PlayerDeathBomb.cs` logic).
*   **RPC to Clients:** The server sends a `ClientRpc` to all clients (e.g., `EffectNetworkHandler.Instance.ClearBulletsInAreaClientRpc(areaParameters)`).
*   **Client-Side Execution:**
    *   Each client iterates through relevant active projectiles. This could be all active objects in `ClientGameObjectPool.Instance.GetAllActiveObjects()`, or a more targeted list.
    *   For each projectile in the specified area:
        *   It calls `ForceReturnToPool()` on the projectile's `ClientProjectileLifetime` component.
*   The `IClearable` interface or specific methods on projectile scripts (as mentioned in previous versions of this document for `StageSmallBulletMoverScript`) can be seen as conventions leading to this `ForceReturnToPool()` call on `ClientProjectileLifetime`.

## Collision Detection and Damage Application

*   **Visual Collisions (Client-Side):**
    *   Wall collisions are handled by `ClientBulletWallCollision` as described above, leading to despawning. This is purely visual and local.
    *   Bullets visually passing through each other is normal; there's no client-side bullet-to-bullet collision.

*   **Player-vs-Illusion and Illusion-vs-Player (Primary Focus of Recent Refactor):**
    *   **Player Shots vs. Illusion Health:**
        *   If a player's client-simulated spellcard bullet *visually* overlaps with an illusion's `Collider2D` on that player's client:
        *   The `IllusionHealth.OnTriggerEnter2D()` method (on the illusion's client-side representation) is triggered.
        *   `IllusionHealth` checks if the hit was from a "PlayerShot" layer/tag.
        *   It then calls its own `TakeDamageClientSide(damageAmount)`.
        *   If health drops to zero, `IllusionHealth.ReportDeathToServerRpc()` is called.
        *   The server's `IllusionHealth` receives this RPC and calls `ServerIllusionOrchestrator.ProcessClientDeathReport()` which then despawns the illusion server-side (and thus for all clients).
    *   **Illusion Shots vs. Player Health:**
        *   An illusion's client-simulated bullet visually overlaps with a player's `Collider2D` (e.g., on the `PlayerHitbox` GameObject).
        *   A script on the player's hitbox (e.g., `ClientPlayerCollisionController.cs` - *name hypothetical*) detects this.
        *   This script would then need to call a `ServerRpc` to the player's own `CharacterStats` (or a central health manager) on the server, e.g., `ReportHitByIllusionBulletServerRpc(bulletType, damageAmount)`.
        *   The server validates the hit (e.g., basic cooldown, checks if player is invincible) and applies damage to the server-authoritative `CharacterStats.Health`. Health is then synced back to clients.
        *   *Note: The exact implementation for illusion bullet to player damage needs to be confirmed from scripts like `CharacterStats` or any player-side collision handlers.*

*   **General Principles:**
    *   Clients handle the "Am I visually hitting something?" question for immediate feedback (like bullet despawning on walls).
    *   For game state changes (damage, death), the client that observes the event (or is responsible for the entity being hit) informs the server via an RPC.
    *   The server has the final say on damage application and state changes.
    *   Physics layers (`Edit -> Project Settings -> Physics 2D -> Layer Collision Matrix`) are crucial for defining what *can* interact.

## Other Projectile Types (If Applicable)

### Basic Player Shots (Legacy or Current?)

*   *This section needs to be reviewed based on whether basic player shots still use a separate system (e.g., the old `BulletMovement.cs` and `PlayerShootingController.FireShotServerRpc` flow) or if they have been integrated into the `ServerAttackSpawner` -> `ClientSpellcardActionRunner` -> `ClientBulletConfigurer` model (perhaps by defining them as very simple `SpellcardData` assets).*
*   If they are separate, the original documentation's description of their client-side spawning and `BulletMovement.cs` might still be partly valid, but needs to be explicitly distinct from the spellcard system.

### Stage-Specific / Enemy Retaliation Bullets

*   The system described in the original `projectile_system.md` involving `PlayerAttackRelay` and `EffectNetworkHandler.SpawnStageBulletClientRpc` for "retaliation/spirit bullets" may still be in use for projectiles not directly part of player spellcards.
*   **Spirit Timeout Attack:** Another example is the activated Spirit's timeout attack. The `ClientSpiritTimeoutAttack.cs` script directly spawns "StageLargeBullet" prefabs from the `ClientGameObjectPool`. It then initializes their `StageSmallBulletMoverScript` with a direction (towards the targeted player or downwards) and specific speed/lifetime values. These bullets are entirely client-simulated after this initial setup.
*   These bullets would also use `ClientGameObjectPool` and `ClientProjectileLifetime`.
*   Their movement scripts (e.g., `StageSmallBulletMoverScript.cs`) would need to be assessed:
    *   Do they still exist as monolithic movers? (Yes, for stage bullets and spirit timeout bullets, `StageSmallBulletMoverScript` is used directly).
    *   Or have they been refactored to also use `ClientBulletConfigurer` with generic movement behaviors, with the `EffectNetworkHandler.SpawnStageBulletClientRpc` perhaps carrying parameters similar to a simplified `SpellcardAction`? (Not for stage/spirit timeout bullets, they use their specific mover script).
    *   If they are configured, then `ClientBulletConfigurer` would need to handle their specific `BehaviorType` or they'd need a dedicated configuration path.

## Key Scripts (Summary)

*   **`ClientSpellcardActionRunner.cs`:** Central to executing client-side spellcard actions, including bullet spawning and dynamic origin for moving illusions.
*   **`ClientBulletConfigurer.cs`:** Static helper; vital for initializing individual bullets (lifetime, activating and configuring specific movement behaviors).
*   **`ClientProjectileLifetime.cs`:** Manages timed despawn and forced despawn for all pooled client-side projectiles.
*   **`ClientBulletWallCollision.cs`:** Handles bullet despawn on contact with "StageWalls" layer.
*   **Movement Behaviors (Client-Side):** `ClientLinearMovement.cs`, `ClientHomingMovement.cs`, etc. – individual scripts defining how bullets move.
*   **`ClientGameObjectPool.cs` & `PooledObjectInfo.cs`:** Manage efficient reuse of projectile GameObjects on the client.
*   **`ServerAttackSpawner.cs`:** Server-side; initiates L1-3 spellcards by sending RPCs to `SpellcardNetworkHandler`. Spawns L4 illusion prefabs.
*   **`ServerIllusionOrchestrator.cs`:** Server-side; manages illusion behavior, including triggering illusion attacks (which then become client-simulated via `ClientIllusionView`).
*   **`ClientIllusionView.cs`:** Client-side; receives RPCs for illusion attacks, manages illusion animation during attacks, and uses `ClientSpellcardActionRunner` for bullet spawning.
*   **`IllusionHealth.cs`:** Handles illusion damage detection on the client and reports death to the server.
*   **`SpellcardNetworkHandler.cs`:** Receives RPCs for L1-3 spellcards and delegates to `ClientSpellcardActionRunner`.
*   **`CharacterStats.cs`:** (Assumed) Server-authoritative player health. Client-side scripts would report hits to it.
*   **`PlayerAttackRelay.cs` & `EffectNetworkHandler.cs`:** (If still used for stage/retaliation bullets) Handle their server-side initiation and RPC dispatch.
*   **`ClientSpiritTimeoutAttack.cs`:** Client-side; handles activated spirit timeout, spawns "StageLargeBullet" from pool, and initializes their `StageSmallBulletMoverScript` for movement and lifetime.
*   **`StageSmallBulletMoverScript.cs`:** Client-side movement script for stage-type bullets, including those spawned by `EffectNetworkHandler` (retaliation) and `ClientSpiritTimeoutAttack`.

## Outstanding Questions / Areas for Code Review:

*   How are basic player direct attacks (non-spellcard shots) handled? Do they use the old `BulletMovement.cs` or are they now a type of `SpellcardAction`?
*   How do illusion-fired bullets register damage against a player? Which client-side script on the player detects the hit, and what RPC does it send to the server?
*   Are stage-specific/retaliation bullets (from `PlayerAttackRelay`) still using bespoke movement scripts (e.g., `StageSmallBulletMoverScript`), or do they also leverage `ClientBulletConfigurer`?
*   Review `PlayerHitbox` and related scripts to confirm the illusion-to-player damage pathway.

This rewrite should align `projectile_system.md` much better with the recent refactoring. 