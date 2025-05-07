# Object Pooling Documentation

## Overview

This document describes the object pooling systems used in Touhou Web Arena. Object pooling is employed to improve performance by reusing frequently created and destroyed objects. This reduces the overhead associated with instantiation, destruction, and garbage collection.

Two distinct pooling systems are now in place:
1.  **`ClientGameObjectPool.cs`:** For client-side visual objects (e.g., basic player projectiles) that do not have `NetworkObject` components.
2.  **`NetworkObjectPool.cs`:** For server-authoritative `NetworkObject`s (e.g., enemies, complex spellcard projectiles that need to be network-aware).

## 1. Client-Side Pooling (`ClientGameObjectPool.cs`)

This system is used for managing pools of purely visual GameObjects on each client, primarily for effects like basic player shots that are client-authoritatively spawned for the owner and then visually replicated on remote clients via RPCs.

**Location:** `Assets/!TouhouWebArena/Scripts/Projectiles/ClientGameObjectPool.cs` (or similar path)

**Implementation:**
*   Acts as a Singleton (`ClientGameObjectPool.Instance`) for easy access from client-side scripts.
*   Manages pools of standard `GameObject`s, indexed by a string `PrefabID`.

**Requirements for Pooled Prefabs (Client-Side):
**Any prefab intended for client-side pooling **must** have:
1.  A `PooledObjectInfo.cs` component. This script holds the unique `PrefabID` (string) that identifies this prefab type. This ID must be set in the Inspector on the prefab asset.
2.  **NO `NetworkObject` component.** These are not networked entities in the NGO sense.

**Configuration:**
*   The `ClientGameObjectPool` GameObject in the scene (e.g., attached to a GameManager or a dedicated PoolingManager object) will have an Inspector list to configure pools:
    *   **`Pool Configurations` List:** Each entry defines:
        *   `PrefabID` (string): The unique identifier for this pool (must match the `PrefabID` in the `PooledObjectInfo` on the prefab).
        *   `Prefab` (GameObject): The actual prefab to be pooled.
        *   `Initial Size`: Number of instances to pre-warm in the pool when the client starts.
*   `Allow Pool Expansion`: A boolean to control if the pool can grow at runtime if an object is requested but none are available. (Current state to be verified in Inspector).

**Usage (Client-Side Only):**

*   **Getting Objects:**
    1.  Obtain the `PrefabID` string for the desired object.
    2.  Call `GameObject instance = ClientGameObjectPool.Instance.GetObject(prefabID);`.
    3.  Check if `instance` is not null.
    4.  Set the `transform.position`, `transform.rotation`, and activate the GameObject (`instance.SetActive(true)`).
    5.  Initialize any necessary components on the instance (e.g., `BulletMovement.Initialize()`, `ClientProjectileLifetime.Initialize()`).
*   **Returning Objects:**
    1.  Typically, a script on the pooled object itself (e.g., `ClientProjectileLifetime.cs`) is responsible for returning it to the pool.
    2.  This is done by calling `ClientGameObjectPool.Instance.ReturnObject(this.gameObject, prefabID);`, where `prefabID` is usually retrieved from the object's `PooledObjectInfo` component.
    3.  `ReturnObject` deactivates the GameObject and places it back into the appropriate pool.

**Use Cases:**
*   Basic player projectiles spawned by `PlayerShootingController.cs` (both for the owner and for remote client visuals triggered by RPCs).
*   Other client-side visual effects.

## 2. Server-Side NetworkObject Pooling (`NetworkObjectPool.cs`)

This system remains for managing `NetworkObject`s that are spawned and controlled authoritatively by the server.

**Location:** `Assets/!TouhouWebArena/Scripts/Networking/NetworkObjectPool.cs`

**Implementation & Requirements:** (This section largely remains the same as the original documentation for `NetworkObjectPool.cs`, assuming it's still used for server-authoritative objects).
*   Singleton (`NetworkObjectPool.Instance`).
*   Pooled prefabs **must** have `NetworkObject` and `PoolableObjectIdentity` (with a `PrefabID` set in Inspector).

**Configuration:** (Remains the same - via Inspector list on the `NetworkObjectPool` GameObject).

**Usage (Server-Side Only):** (Remains the same - `GetNetworkObject`, `ReturnNetworkObject`, spawning via `networkObjectInstance.Spawn(true)`).

**Use Cases / Pooled Objects (Server-Side):
**This pool is now primarily for:
*   Enemies (`Spirit`, `NormalFairy`, `GreatFairy`).
*   Complex spellcard projectiles or other `NetworkObject`s that require server authority for their lifecycle and behavior (e.g., `StageSmallBulletPrefab`, `StageLargeBulletPrefab`, various `...Bullet_Variant` prefabs if they are `NetworkObject`s).
*   Fairy Effects if they are `NetworkObject`s (`FairyShockwave`).
*   **Player basic shots (`ReimuBulletPrefab`, `MarisaBulletPrefab`) are NO LONGER pooled by this system.** They use `ClientGameObjectPool.cs`.

## Key Scripts

*   **Client-Side Pooling:**
    *   `ClientGameObjectPool.cs`: Manages client-side GameObject pools.
    *   `PooledObjectInfo.cs`: Stores `PrefabID` for client-pooled objects.
    *   `ClientProjectileLifetime.cs`: Example script that returns client-pooled objects.
    *   `PlayerShootingController.cs`: Uses `ClientGameObjectPool` for basic shots.
*   **Server-Side Pooling (for NetworkObjects):**
    *   `NetworkObjectPool.cs`: The central pooling manager for `NetworkObject`s.
    *   `PoolableObjectIdentity.cs`: Component required on server-pooled prefabs.
    *   Scripts using this pool (Server-Side): `ServerAttackSpawner` (for server-authoritative spellcard bullets/entities), enemy spawners, etc. 