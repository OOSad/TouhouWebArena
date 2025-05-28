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
*   **`PooledObjectInfo.cs`:** Stores a `PrefabID` string used by `ClientGameObjectPool` for efficient reuse.
*   **Movement Behavior Scripts (All Disabled by Default):**
    *   `ClientLinearMovement.cs`
    *   `ClientHomingMovement.cs`
    *   `ClientSpiralMovement.cs`
    *   `ClientDelayedHoming.cs`
    *   `ClientDoubleHoming.cs`
    *   `ClientDelayedRandomTurn.cs`
    *   (And any other specialized client-side movement behaviors)
*   **Specific Mover Scripts (e.g., for Stage/Retaliation Bullets):**
    *   `StageSmallBulletMoverScript.cs`: Used for stage-specific and enemy retaliation bullets (including Lily White's). Contains movement logic, lifetime management, wall collision detection, and an `_owningPlayerRole` field initialized via its `Initialize` method.
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
        *   It calls `ClientSpellcardActionRunner.RunSpellcardActionsDynamicOrigin`, passing its own `transform` as the `originTransform`, and crucially, the **original caster's PlayerRole** (obtained from the illusion NetworkObject's OwnerClientId, which is set by the server during illusion spawn). This allows bullets to spawn from the moving illusion and inherit the correct ownership.
    *   If the illusion is static for the attack:
        *   It calls `ClientSpellcardActionRunner.RunSpellcardActions` using its current (static) transform and passing the **original caster's PlayerRole**.
*   **`ClientSpellcardActionRunner.cs`:**
    *   Receives the list of actions, origin info (static or dynamic), and the appropriate **`PlayerRole`** (either the direct caster for L1-3, or the original caster passed via `ClientIllusionView` for L4 illusions).
    *   Iterates through the `SpellcardAction` list from the data.
    *   For each action that spawns bullets:
        *   Obtains a bullet prefab instance from `ClientGameObjectPool.Instance.GetObject()`.
        *   Sets initial position and orientation (potentially from a dynamic origin for moving illusions).
        *   **Calls `ClientBulletConfigurer.ConfigureBullet()`**, passing the bullet instance, action data, and potentially the **received `PlayerRole`** to assign ownership.
        *   Activates the bullet (`SetActive(true)`).

### 3. Bullet Configuration (`ClientBulletConfigurer.cs`)

This static helper class is vital for setting up individual bullets:

*   Takes the `GameObject bulletInstance`, `SpellcardAction action`, `casterClientId`, `targetClientId`, the **`owningPlayerRole` (PlayerRole)**, and `bulletIndex` as parameters (parameter list updated to include role).
*   **Ownership Assignment:**
    *   If the `bulletInstance` has a component that tracks ownership (like `StageSmallBulletMoverScript`), this is where the `owningPlayerRole` parameter would be used to set that component's role (e.g., `bulletInstance.GetComponent<StageSmallBulletMoverScript>()?.InitializeOwnerRole(owningPlayerRole)`). Note that many spellcard bullet prefabs may *not* have `StageSmallBulletMoverScript`, relying instead on their layer for interactions.
    *   For stage-specific and enemy retaliation bullets using `StageSmallBulletMoverScript`, the `OwningPlayerRole` is set directly via the `Initialize` method called by their spawner/controller script (e.g., `LilyWhiteAttackPattern.SpawnClaw`).
    *   This ensures bullets fired by spellcards (including illusions) are correctly associated with the player who cast the original spell, and that stage/enemy bullets are associated with the player on whose side they spawned.
    *   Crucially, the role reset that used to occur in `StageSmallBulletMoverScript.OnEnable()` has been removed, ensuring this explicitly set role persists.
*   **Lifetime:** Initializes `ClientProjectileLifetime` on the bullet using `action.lifetime` (or a default if not specified). For `StageSmallBulletMoverScript`, lifetime is managed directly within that script via `_currentLifetimeRemaining` and a check in `Update()`, calling `ReturnToClientPool()` when the timer expires.
*   **Movement Behavior Reset:** Deactivates all potential movement behavior scripts attached to the bullet prefab (e.g., `ClientLinearMovement`, `ClientHomingMovement`, etc.) to ensure a clean state.
*   **Specific Behavior Activation:** Based on `action.behavior` (e.g., `BehaviorType.Linear`, `BehaviorType.Homing`):
    *   Gets the corresponding movement script (e.g., `bulletInstance.GetComponent<ClientLinearMovement>()`).
    *   Initializes it with parameters from the `action` (e.g., `speed`, `homingSpeed`, `homingDelay`, `targetPlayerId`, `bulletIndex` for speed increments).
    *   Enables the chosen movement script (`linear.enabled = true`). For `StageSmallBulletMoverScript`, movement is handled by its own `Update` method, initialized via the `Initialize` method's direction and speed parameters.

### 4. Client-Side Movement Behaviors

These are individual `MonoBehaviour` scripts attached to bullet prefabs. `ClientBulletConfigurer` enables and initializes the correct one per `SpellcardAction`.

*   **`ClientLinearMovement.cs`:** Moves the bullet in a straight line. Can have initial speed, speed increment per bullet in a volley, and smooth speed transitions.
*   **`ClientHomingMovement.cs`:** Steers the bullet towards a target's position (at the time of initialization or updated if more sophisticated).
*   **`ClientSpiralMovement.cs`:** Moves the bullet in a spiral pattern.
*   **`ClientDelayedHoming.cs`:** Moves linearly for a delay, then homes.
*   **`ClientDoubleHoming.cs`:** Performs two phases of homing with an intermediate delay.
*   **`ClientDelayedRandomTurn.cs`:** Moves linearly, then turns randomly within a specified angle.
*   **`BulletMovement.cs` (For Basic Player Shots):**
    *   Handles client-side movement for basic player projectiles.
    *   Initialized with `ownerClientId`, `speed`, and `lifetime`.
    *   `OnTriggerEnter2D` checks for collisions with entities tagged "Fairy", "Spirit", or **"LilyWhite"**.
    *   If a collision occurs, it calls the appropriate health component's `TakeDamage` method (`ClientFairyHealth`, `ClientSpiritHealth`, or **`ClientLilyWhiteHealth`**).
    *   Returns the bullet to the pool on hit via `ClientProjectileLifetime.ForceReturnToPool()`.
*   All movement scripts are responsible for updating `transform.position` (and sometimes `transform.rotation`) in their `Update()` or `FixedUpdate()` methods.

## Projectile Despawning Mechanisms

Client-side projectiles are removed from the game world and returned to the `ClientGameObjectPool` through several mechanisms:

### 1. Timed Despawn (`ClientProjectileLifetime.cs`)

*   This is the primary despawn method for most projectiles.
*   When `ClientBulletConfigurer.ConfigureBullet()` initializes a bullet, it also calls `Initialize(lifetime)` on the bullet's `ClientProjectileLifetime` component.
*   The `lifetime` is typically derived from `SpellcardAction.lifetime`. If `action.lifetime` is 0 or not set, a default lifetime is used.
*   `ClientProjectileLifetime` tracks its active time and calls `ReturnToPool()` when `_timeActive >= _lifetime`.
*   For `StageSmallBulletMoverScript`, lifetime is managed directly within that script via `_currentLifetimeRemaining` and a check in `Update()`, calling `ReturnToClientPool()` when the timer expires.

### 2. Wall Collision (`ClientBulletWallCollision.cs`)

*   For `StageSmallBulletMoverScript`, the `OnTriggerEnter2D()` method checks if the colliding object is on the "StageWalls" layer and calls `ReturnToClientPool()` if a hit is detected.

### 3. Explicit Clearing Effects (e.g., Bombs, Special Abilities)

Projectiles can be cleared by various game mechanics, such as player deathbombs or spellcard activation. The implementation details vary based on the effect.

*   **Spellcard Activation Clear (Levels 2, 3, 4):**
    *   **Server Initiation:** `ServerAttackSpawner.TriggerSpellcardClear` calculates clear parameters (caster position, radius scaled by spell level, caster role) and sends them to all clients via `TriggerLocalClearEffectClientRpc`.
    *   **Client-Side Execution (`ClientSpellcardExecutor.TriggerLocalClearEffectClientRpc`):**
        *   Each client performs a local `Physics2D.OverlapCircleAll` using the received parameters.
        *   It iterates through colliders:
            *   If a collider has a `StageSmallBulletMoverScript`, the script now checks if the bullet's X position is on the *caster's side* of the arena (X < 0 for Player 1, X > 0 for Player 2). If it is, the bullet is cleared by calling `ForceReturnToPoolByBomb()`. The revenge bullet spawning logic within this clear has been temporarily commented out.
            *   If `collider.gameObject.layer == LayerMask.NameToLayer("EnemyProjectiles")` and it's *not* a `StageSmallBulletMoverScript` (to avoid double processing), it gets the `ClientProjectileLifetime` component and calls `ForceReturnToPool()`. This clears other generic enemy bullet types on this layer within the radius.
            *   It also checks for `ClientFairyHealth` or `ClientSpiritController` components to clear the caster's own entities if their `OwningPlayerRole` matches the caster's role.
            *   Additionally, it checks for `ReimuExtraAttackOrb_Client` and `MarisaExtraAttackLaser_Client` components. If found and their `AttackerClientId` does *not* match the caster's resolved client ID, it **directly calls the component's `ForceReturnToPoolByClear()` method** to clear these hostile extra attacks.
    *   Level 2/3 spellcard activations do *not* clear illusions via this client-side RPC. Level 4 activations have an additional server-side step to despawn enemy illusions targeting the caster.

*   **Enemy Death Shockwave Clear (e.g., `ClientFairyShockwave`):**
    *   When an enemy (like a Fairy) dies, it can emit a shockwave (`ClientFairyShockwave`).
    *   This shockwave's `OnTriggerStay2D` checks for collisions with other entities.
    *   If it collides with a projectile that has a `StageSmallBulletMoverScript` component:
        *   It checks if `bullet.OwningPlayerRole != shockwaveOwnerRole && bullet.OwningPlayerRole != PlayerRole.None`.
        *   If the bullet belongs to the opposing player, the shockwave script calls `ForceReturnToPoolByBomb()` on the bullet.
    *   This ensures enemy shockwaves only clear the *other* player's projectiles (and other entities like fairies/spirits based on similar ownership checks).

*   **Player Deathbomb Clear:**
    *   The player deathbomb (`PlayerDeathBomb.ClearObjectsInRadiusClientRpc`) iterates through all active objects from `ClientGameObjectPool`.
    *   It clears objects within the bomb radius based on type and ownership:
        *   `StageSmallBulletMoverScript`: Clears (calls `ForceReturnToPoolByBomb()`).
        *   `ClientFairyHealth` or `ClientSpiritController`: Damages if `OwningPlayerRole == bombingPlayerRole`.
        *   **`ReimuExtraAttackOrb_Client` or `MarisaExtraAttackLaser_Client`**: Clears by **directly calling the component's `ForceReturnToPoolByClear()` method**. (Deathbombs clear extra attacks regardless of owner).
        *   `ClientProjectileLifetime`: For any other object with this component, calls `ForceReturnToPool()`.

In all cases, the end result for a cleared projectile is that its `ClientProjectileLifetime.ForceReturnToPool()` method is called, deactivating it and returning it to the `ClientGameObjectPool`.

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

### Player Charge Attacks (Client-Simulated)

Player charge attacks are special abilities unique to each character, triggered by the server but simulated on all clients. They use dedicated client-side scripts and handlers.

*   **Triggering:** Initiated by `ServerChargeAttackSpawner.cs`, which calls a `ClientRpc` on the character-specific handler (e.g., `ReimuChargeAttackHandler_Client`) on the player's prefab. This RPC is broadcast to all clients.
*   **Client-Side Handling:** The specific handler (e.g., `ReimuChargeAttackHandler_Client` or `MarisaChargeAttackHandler_Client`) receives the RPC, retrieves the necessary projectile prefab(s) from `ClientGameObjectPool`, and initializes them.

#### Reimu's Homing Talismans

*   **Handler:** `ReimuChargeAttackHandler_Client.cs` (on Reimu's player prefab).
*   **Projectile Script:** `HomingTalisman_Client.cs` (on the talisman prefab, e.g., "ReimuChargeTalisman_Client").
*   **Behavior:** 
    *   `ReimuChargeAttackHandler_Client` spawns a configurable number of talismans (e.g., 4) in a circular pattern around the player.
    *   Each `HomingTalisman_Client` instance is initialized with the `ownerRole` and a slight initial delay (for staggered appearance/homing).
    *   The script manages the talisman's movement, actively seeking the nearest enemy (Spirit or Fairy) that matches its `_ownerPlayerRole` (determined by `ClientSpiritController.GetOwningPlayerRole()` or `ClientFairyController.GetOwningPlayerRole()`).
    *   On collision with a valid enemy, it calls `TakeDamage(damage, 0)` on the enemy's `ClientSpiritHealth` or `ClientFairyHealth` component.
    *   It returns itself to the `ClientGameObjectPool` after a set lifetime or upon successful collision leading to its destruction (if applicable, current logic returns on lifetime end or after dealing damage once if it were to destroy itself - needs confirmation if it pierces or not based on HomingTalisman_Client logic).

#### Marisa's Laser

*   **Handler:** `MarisaChargeAttackHandler_Client.cs` (on Marisa's player prefab).
*   **Projectile Script:** `IllusionLaser_Client.cs` (on the laser prefab, e.g., "MarisaChargeLaser_Client"). This script is also used by Illusions for their laser attacks but is configured differently for Marisa's charge attack.
*   **Behavior:**
    *   `MarisaChargeAttackHandler_Client` spawns one laser instance.
    *   The laser is positioned using a dedicated child `Transform` on Marisa's prefab, assigned to the `marisaLaserSpawnPoint` field in `MarisaChargeAttackHandler_Client`. This ensures the laser originates from the correct visual point (e.g., respecting sprite pivots).
    *   `IllusionLaser_Client` is initialized with `ownerRole`, the initial player position (for reference), and the `_ownerTransform` (Marisa's player object transform, for following).
    *   The laser visually scales to a fixed `laserLength` (defined in `IllusionLaser_Client`'s Inspector fields) upwards from its spawn point. Its sprite pivot should be at its visual bottom for correct scaling.
    *   The base of the laser (its `transform.position`) follows the `_ownerTransform`'s X and Y position dynamically, plus any `followOffsetX`.
    *   It deals damage over time to client-side enemies (`ClientSpiritHealth`, `ClientFairyHealth`) that match its `_ownerPlayerRole` and are within its collider.
    *   Returns to the `ClientGameObjectPool` after its `duration` (also an Inspector field on `IllusionLaser_Client`).

### Basic Player Shots (Legacy or Current?)

*   *This section needs to be reviewed based on whether basic player shots still use a separate system (e.g., the old `BulletMovement.cs` and `PlayerShootingController.FireShotServerRpc` flow) or if they have been integrated into the `ServerAttackSpawner` -> `ClientSpellcardActionRunner` -> `ClientBulletConfigurer` model (perhaps by defining them as very simple `SpellcardData` assets).*
*   If they are separate, the original documentation's description of their client-side spawning and `BulletMovement.cs` might still be partly valid, but needs to be explicitly distinct from the spellcard system.

### Stage-Specific / Enemy Retaliation Bullets

*   The system described in the original `projectile_system.md` involving `PlayerAttackRelay` and `EffectNetworkHandler.SpawnStageBulletClientRpc` for "retaliation/spirit bullets" may still be in use for projectiles not directly part of player spellcards.
*   **Spirit Timeout Attack:** Another example is the activated Spirit's timeout attack. The `ClientSpiritTimeoutAttack.cs` script directly spawns "StageLargeBullet" prefabs from the `ClientGameObjectPool`. It then initializes their `StageSmallBulletMoverScript` with a direction (towards the targeted player or downwards) and specific speed/lifetime values.
*   Lily White's bullets also fall into this category, spawned by `LilyWhiteAttackPattern.cs` and using `StageSmallBulletMoverScript.cs`.
*   These bullets use `ClientGameObjectPool` and `StageSmallBulletMoverScript` for movement and lifetime.
*   Their `OwningPlayerRole` is set dynamically during initialization based on which player's side they spawn on (determined by the spawning script, e.g., `ClientLilyWhiteSpawnHandler.SpawnLilyWhiteInstance`).
*   They despawn on wall collision due to logic in `StageSmallBulletMoverScript.OnTriggerEnter2D()`.
*   They are cleared by fairy shockwaves from the opposing player and by spellcard shockwaves on the caster's side.

## Key Scripts (Summary)

*   **`ClientSpellcardActionRunner.cs`:** Central to executing client-side spellcard actions, including bullet spawning and dynamic origin for moving illusions.
*   **`ClientBulletConfigurer.cs`:** Static helper; vital for initializing individual bullets (lifetime, activating and configuring specific movement behaviors). Used primarily for spellcard bullets that aren't `StageSmallBulletMoverScript`. Stage/enemy bullets using `StageSmallBulletMoverScript` are initialized directly via their `Initialize` method.
*   **`ClientProjectileLifetime.cs`:** Manages timed despawn and forced despawn for most pooled client-side projectiles. `StageSmallBulletMoverScript` manages its own lifetime.
*   **`ClientFairyShockwave.cs`:** Client-side script for fairy death shockwaves. **Includes logic to clear `StageSmallBulletMoverScript` instances belonging to the opposing player.**
*   **`ClientGameObjectPool.cs` & `PooledObjectInfo.cs`:** Manage efficient reuse of projectile GameObjects on the client.
*   **`ServerAttackSpawner.cs`:** Server-side; initiates L1-3 spellcards by sending RPCs to `SpellcardNetworkHandler`. Spawns L4 illusion prefabs.
*   **`ServerIllusionOrchestrator.cs`:** Server-side; manages illusion behavior, including triggering illusion attacks (which then become client-simulated via `ClientIllusionView`).
*   **`ClientIllusionView.cs`:** Client-side; receives RPCs for illusion attacks, manages illusion animation during attacks, and uses `ClientSpellcardActionRunner` for bullet spawning.
*   **`IllusionHealth.cs`:** Handles illusion damage detection on the client and reports death to the server.
*   **`SpellcardNetworkHandler.cs`:** Receives RPCs for L1-3 spellcards and delegates to `ClientSpellcardActionRunner`.
*   **`CharacterStats.cs`:** (Assumed) Server-authoritative player health. Client-side scripts would report hits to it.
*   **`PlayerAttackRelay.cs` & `EffectNetworkHandler.cs`:** (If still used for stage/retaliation bullets) Handle their server-side initiation and RPC dispatch.
*   **`ClientSpiritTimeoutAttack.cs`:** Client-side; handles activated spirit timeout, spawns "StageLargeBullet" from pool, and initializes their `StageSmallBulletMoverScript` for movement and lifetime.
*   **`StageSmallBulletMoverScript.cs`:** Client-side movement script for stage-type bullets, including those spawned by `EffectNetworkHandler` (retaliation), `ClientSpiritTimeoutAttack`, and `LilyWhiteAttackPattern`. **Manages its own movement, lifetime, wall collision, and stores the bullet's `OwningPlayerRole` set during initialization.**
*   **`HomingTalisman_Client.cs`:** Client-side script for Reimu's charge attack talismans. Manages seeking behavior, targeting based on `ownerRole`, collision with client-side enemies, and its own lifetime/pooling.
*   **`IllusionLaser_Client.cs`:** Client-side script used for Marisa's charge attack laser (and illusion lasers). For Marisa, it spawns from a defined point, has a fixed `laserLength`, follows the owner's X/Y position, deals damage over time, and manages its lifetime/pooling.
*   **`ReimuChargeAttackHandler_Client.cs`:** (New) On Reimu's prefab. Receives RPC to spawn client-side Homing Talismans.
*   **`MarisaChargeAttackHandler_Client.cs`:** (New) On Marisa's prefab. Receives RPC to spawn client-side Master Spark laser, utilizing a `marisaLaserSpawnPoint` for precise origin.
*   **`ServerAttackSpawner.cs`:** Server-side; initiates L1-3 spellcards by sending RPCs to `SpellcardNetworkHandler`. Spawns L4 illusion prefabs. For charge attacks, gets character-specific client handlers and calls their `SpawnChargeAttackClientRpc`.
*   **`ClientLilyWhiteSpawnHandler.cs`:** Client-side script responsible for spawning Lily White instances on the correct player's side and initiating their movement and attack sequence by calling `ClientLilyWhiteController.Initialize`, passing the determined `PlayerRole`.
*   **`ClientLilyWhiteController.cs`:** Client-side script managing Lily White's movement lifecycle. Receives the `PlayerRole` from the spawn handler and passes it to `LilyWhiteAttackPattern.StartAttackSequence`.
*   **`LilyWhiteAttackPattern.cs`:** Client-side script defining Lily White's bullet attack patterns. Receives the target `PlayerRole` from the controller and uses it when spawning bullets via `SpawnClaw`, passing it to `StageSmallBulletMoverScript.Initialize`.

## Outstanding Questions / Areas for Code Review:

*   How are basic player direct attacks (non-spellcard shots) handled? Do they use the old `BulletMovement.cs` or are they now a type of `SpellcardAction`?
*   How do illusion-fired bullets register damage against a player? Which client-side script on the player detects the hit, and what RPC does it send to the server?
*   Are stage-specific/retaliation bullets (from `PlayerAttackRelay`) still using bespoke movement scripts (e.g., `StageSmallBulletMoverScript`), or do they also leverage `ClientBulletConfigurer`? (Answered: Yes, `StageSmallBulletMoverScript` is used. Ensure its initialization includes setting `OwningPlayerRole`).
*   Review `PlayerHitbox` and related scripts to confirm the illusion-to-player damage pathway.

This rewrite should align `projectile_system.md` much better with the recent refactoring. 