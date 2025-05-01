# Object Pooling Documentation

## Overview

This document describes the network object pooling system used in Touhou Web Arena. Object pooling is employed to improve performance by reusing frequently created and destroyed networked objects, such as player bullets, enemy projectiles, enemies themselves, and potentially visual effects. This reduces the overhead associated with instantiation, destruction, and garbage collection, especially critical in a networked environment.

## Implementation

The system is implemented in the `NetworkObjectPool.cs` script, located at `Assets/!TouhouWebArena/Scripts/Networking/`. It acts as a Singleton (`NetworkObjectPool.Instance`) for easy access from other server-side scripts.

**Requirements for Pooled Prefabs:**
Any prefab intended for pooling **must** have the following components:
1.  A `NetworkObject` component.
2.  A `PoolableObjectIdentity` component (custom script). This component requires a unique string `PrefabID` to be assigned in the Inspector. This ID serves as the key for retrieving instances of this prefab from the pool.

## Configuration

*   **Inspector List:** The pool is configured entirely via the Inspector on the GameObject that has the `NetworkObjectPool` script attached (likely the `GameManager` object in the main gameplay scene).
*   **`Pools To Create` List:** This list holds `PoolConfig` entries. For each type of prefab you want to pool:
    *   Assign the `Prefab` GameObject.
    *   Set the `Initial Size` (the number of instances to create and deactivate when the server starts, "pre-warming" the pool).
*   **`Allow Pool Expansion`:** This checkbox is **disabled** (unchecked) in the current configuration (as seen in the Inspector). This means the pool will *not* create new instances at runtime if a request is made when the pool is empty for that prefab type; it will return null instead.

## Usage (Server-Side Only)

All interactions with the `NetworkObjectPool` must occur on the **server**.

*   **Getting Objects:**
    *   **Manual Method:**
        1.  Obtain the unique `PrefabID` string for the desired object (defined in its `PoolableObjectIdentity` component).
        2.  Call `NetworkObject networkObjectInstance = NetworkObjectPool.Instance.GetNetworkObject(prefabID);`.
        3.  Check if `networkObjectInstance` is not null (it will be null if the pool is empty and expansion is disabled).
        4.  Set the `transform.position` and `transform.rotation` of `networkObjectInstance.gameObject`.
        5.  Initialize any necessary components on the instance **before** spawning.
        6.  Spawn the object onto the network: `networkObjectInstance.Spawn(true);`
    *   **Helper Method (`ServerPooledSpawner`):**
        *   The static `ServerPooledSpawner.SpawnSinglePooledBullet` method provides a convenient wrapper for spawning bullet prefabs from the pool.
        *   It handles getting the object by `PrefabID` from the `PoolableObjectIdentity` on the passed `prefab`, setting position/rotation, calling `Spawn(true)`, and assigning the `ownerRole` via `PlayerDataManager`.
*   **Returning Objects:**
    1.  Pooled objects typically have a component (e.g., `NetworkBulletLifetime`, `BulletMovement`, `StageSmallBulletMoverScript`) that automatically calls `NetworkObjectPool.Instance.ReturnNetworkObject(this.NetworkObject)` upon lifetime expiration, collision, or being cleared.
    2.  The `ReturnNetworkObject` method handles calling `networkObject.Despawn(false)`, deactivating the GameObject, and enqueuing it back into the pool based on its `PrefabID`.
    3.  **Important:** Directly calling `networkObject.Despawn(true)` will destroy the GameObject and bypass the pool.

## Pooled Objects

The following prefabs are currently configured for pooling (see the `NetworkObjectPool` component in the Inspector for exact `Initial Size` values):

*   Player Bullets:
    *   `ReimuBulletPrefab`
    *   `MarisaBulletPrefab`
*   Stage/Enemy Bullets:
    *   `StageSmallBulletPrefab`
    *   `StageLargeBulletPrefab`
*   Fairy Effects:
    *   `FairyShockwave`
    *   `FairyShockwaveVisualOnly`
*   Enemies:
    *   `Spirit`
    *   `NormalFairy`
    *   `GreatFairy`
*   Variant Bullets (likely for spellcards/specific patterns):
    *   `RedSmallCircleBullet_Variant`
    *   `RedSmallOvalBullet_Variant`
    *   `WhiteSmallCircleBullet_Variant`
    *   `WhiteSmallOvalBullet_Variant`

*(Note: Ensure any new projectile or frequently spawned enemy prefabs are added to this list in the Inspector and have the required `NetworkObject` and `PoolableObjectIdentity` components.)*

## Key Scripts

*   **`NetworkObjectPool.cs`:** The central pooling manager script.
*   **`PoolableObjectIdentity.cs`:** Component script required on all pooled prefabs to define their unique `PrefabID`.
*   **Scripts using the pool (Server-Side):**
    *   `PlayerShooting.cs` (for basic shots, charge attacks)
    *   `SpellcardExecutor.cs` (or server-side equivalent logic in `PlayerShooting.cs` for spawning spellcard bullets)
    *   `ExtraAttackManager.cs` (for spawning Extra Attack prefabs)
    *   `FairySpawner.cs` / `SpiritSpawner.cs` (for spawning enemies)
    *   Potentially enemy scripts or lifetime components (for returning objects to the pool). 