# Object Pooling Documentation

## Overview

This document describes the object pooling system used in Touhou Web Arena. Object pooling is employed to improve performance by reusing frequently created and destroyed objects, reducing the overhead associated with instantiation, destruction, and garbage collection.

Due to the shift towards **Client-Side Simulation**, the primary pooling mechanism is now **`ClientGameObjectPool.cs`**. This system manages pools of non-`NetworkObject` GameObjects locally on each client.

The previous server-side `NetworkObjectPool.cs` system is currently **DEPRECATED** and likely unused, as enemies, projectiles, and effects are now spawned and simulated client-side based on server commands (RPCs).

## Client-Side Pooling (`ClientGameObjectPool.cs`)

This system is the **core pooling solution** for the game. It manages pools of standard `GameObject`s locally on each client, used for projectiles, enemies, visual effects, etc.

**Location:** `Assets/!TouhouWebArena/Scripts/Client/ClientGameObjectPool.cs`

**Implementation:**
*   Acts as a Singleton (`ClientGameObjectPool.Instance`) for easy access from client-side scripts.
*   Manages pools of standard `GameObject`s, indexed by a string `PrefabID`.

**Requirements for Pooled Prefabs:
**Any prefab intended for client-side pooling **must** have:
1.  A `PooledObjectInfo.cs` component attached. This script holds the unique `PrefabID` (string) that identifies this prefab type. This ID must be set consistently in the Inspector on the prefab asset and in the `ClientGameObjectPool` configuration.
2.  **NO `NetworkObject` component.** These objects are simulated locally and their state is not directly synchronized via Netcode for GameObjects.

**Configuration:**
*   The `ClientGameObjectPool` GameObject in the scene (e.g., attached to a GameManager or a dedicated PoolingManager object) has an Inspector list to configure pools:
    *   **`Pool Configurations` List:** Each entry defines:
        *   `PrefabID` (string): The unique identifier for this pool (must match the `PrefabID` in the `PooledObjectInfo` on the prefab).
        *   `Prefab` (GameObject): The actual prefab asset to be pooled.
        *   `Initial Size`: Number of instances to pre-warm in the pool when the client starts.
*   `Allow Pool Expansion`: (Verify in Inspector) Boolean controlling if pools can grow dynamically.

**Usage (Client-Side Only):**

*   **Getting Objects (Typically in response to RPC or local action):**
    1.  Obtain the `PrefabID` string for the desired object (e.g., from RPC parameters or `CharacterStats`).
    2.  Call `GameObject instance = ClientGameObjectPool.Instance.GetObject(prefabID);`.
    3.  Check if `instance` is not null.
    4.  Set the `transform.position`, `transform.rotation`.
    5.  Initialize necessary components on the instance (e.g., `BulletMovement.Initialize()`, `ClientProjectileLifetime.Initialize()`, `SplineWalker.InitializePath()`, `ClientFairyShockwave.Initialize()`).
    6.  Activate the GameObject (`instance.SetActive(true)`).
*   **Returning Objects:**
    1.  Typically, a script on the pooled object itself (e.g., `ClientProjectileLifetime.cs`, `ClientFairyController.cs`, `ClientFairyShockwave.cs`) is responsible for returning it to the pool when its lifecycle ends (timer, collision, path completion, effect duration).
    2.  This is done by calling `ClientGameObjectPool.Instance.ReturnObject(this.gameObject);`.
    3.  `ReturnObject` deactivates the GameObject and places it back into the appropriate pool based on its `PooledObjectInfo`.

**Use Cases:**
*   Player basic projectiles (`BulletMovement.cs`).
*   Enemies (Fairies via `FairySpawnNetworkHandler` triggering client-side pooling, client-simulated Spirits via `ClientSpiritSpawnHandler` triggering client-side pooling).
*   Spirit death shockwaves (spawned by `ClientSpiritHealth` from the pool, e.g., "FairyShockwave" prefab).
*   Spirit timeout attack bullets (spawned by `ClientSpiritTimeoutAttack` from the pool, e.g., "StageLargeBullet" prefab).
*   Stage bullets / Retaliation bullets (`StageSmallBulletMoverScript.cs`).
*   Visual Effects (`ClientFairyShockwave.cs` - spawned by `ClientFairyHealth` for fairies and spirits).
*   **Spellcard Bullets (All Levels, including Illusion Attacks):** Spawned via `ClientSpellcardActionRunner` (triggered by `SpellcardNetworkHandler` for L1-3 or `ClientIllusionView` for L4 illusion attacks).
*   Other non-networked, frequently spawned GameObjects.

## ~~Server-Side NetworkObject Pooling (`NetworkObjectPool.cs`)~~ - DEPRECATED

This system was previously used for managing server-authoritative `NetworkObject`s. **It is no longer used for core gameplay elements like projectiles or enemies.** It remains in the codebase but should not be used for new client-simulated entities.

(Original documentation for its implementation, requirements like `PoolableObjectIdentity`, and usage can be kept here for historical reference if needed, but clearly marked as deprecated.)

## Key Scripts

*   **`ClientGameObjectPool.cs`:** Manages all active client-side GameObject pools.
*   **`PooledObjectInfo.cs`:** Stores `PrefabID` on prefabs for `ClientGameObjectPool`.
*   **`ClientProjectileLifetime.cs`:** Example script that returns client-pooled projectiles.
*   **`ClientFairyController.cs`:** Returns pooled fairies on path completion/death.
*   **`ClientSpiritController.cs` (indirectly):** Relies on `ClientSpiritHealth` and `ClientSpiritTimeoutAttack` which handle returning the spirit to the pool.
*   **`ClientSpiritHealth.cs`:** Returns pooled spirits on death and spawns shockwaves from the pool.
*   **`ClientSpiritTimeoutAttack.cs`:** Returns pooled spirits on timeout and spawns timeout bullets from the pool.
*   **`ClientFairyShockwave.cs`:** Returns pooled shockwaves after duration.
*   **Scripts that *get* objects from the pool:** `PlayerShootingController`, `EffectNetworkHandler`, `FairySpawnNetworkHandler` (via RPCs triggering client-side logic), `ClientSpiritSpawnHandler` (for spirits), `ClientSpellcardActionRunner` (for all spellcard bullets), `ClientIllusionView` (indirectly, by calling `ClientSpellcardActionRunner`), `ClientFairyHealth` (for fairy shockwaves), `ClientSpiritHealth` (for spirit shockwaves), `ClientSpiritTimeoutAttack` (for spirit timeout bullets).
*   **~~`NetworkObjectPool.cs`~~:** Deprecated.
*   **~~`PoolableObjectIdentity.cs`~~:** Deprecated (associated with `NetworkObjectPool`).

*   **Client-Side Pooling:**
    *   `ClientGameObjectPool.cs`: Manages client-side GameObject pools.
    *   `PooledObjectInfo.cs`: Stores `PrefabID` for client-pooled objects.
    *   `ClientProjectileLifetime.cs`: Example script that returns client-pooled objects.
    *   `PlayerShootingController.cs`: Uses `ClientGameObjectPool` for basic shots.
*   **Server-Side Pooling (for NetworkObjects):**
    *   `NetworkObjectPool.cs`: The central pooling manager for `NetworkObject`s.
    *   `PoolableObjectIdentity.cs`: Component required on server-pooled prefabs.
    *   Scripts using this pool (Server-Side): `ServerAttackSpawner` (for server-authoritative spellcard bullets/entities), enemy spawners, etc. 