# Projectile System Documentation

This document outlines the design and implementation of the projectile system in TouhouWebArena.

## Overview

The projectile system handles the creation, movement, collision detection, and destruction of bullets fired by players (and potentially other entities in the future). It is designed with network synchronization in mind, leveraging Netcode for GameObjects for server-authoritative control.

## Core Components

-   **Bullet Prefabs:** Standard Unity prefabs representing different visual types of bullets. These contain necessary components like `SpriteRenderer`, `Collider2D` (set as Trigger), and networking components.
-   **`NetworkObject`:** Attached to bullet prefabs to enable network synchronization.
-   **`NetworkObjectPool`:** Manages the efficient spawning and despawning of networked bullet objects, reducing overhead compared to frequent instantiation and destruction.
-   **`NetworkBulletLifetime.cs`:** A server-authoritative script attached to bullet prefabs. It manages the bullet's maximum lifetime and handles boundary checks, returning the bullet to the pool when necessary. It also contains the logic for detecting collisions with players and triggering damage.
-   **Spellcard Scripts (e.g., `ShotA.cs`):** Specific scripts that define bullet patterns, firing rates, and initial velocity/direction. These scripts utilize the `NetworkObjectPool` to request and spawn bullet instances on the server.

## Projectile Lifecycle (Server-Authoritative)

1.  **Request:** A player action (e.g., firing a shot) triggers a server RPC call defined within a spellcard script (like `ShotA.cs`).
2.  **Spawn:** The server-side spellcard script requests a bullet instance from the `NetworkObjectPool`.
3.  **Initialization:** The server retrieves a pooled `NetworkObject` (or instantiates a new one if the pool is empty), sets its position, rotation, and initial velocity. It then calls `Spawn(true)` on the `NetworkObject`.
4.  **Synchronization:** Netcode for GameObjects automatically synchronizes the newly spawned bullet's state (position, rotation, etc.) to all connected clients. Clients visually render the bullet based on this synchronized state.
5.  **Movement:** Bullet movement can be handled in several ways:
    *   **Simple Physics:** A `Rigidbody2D` component with simulated physics (gravity scale usually 0). Initial velocity is set by the spawning script.
    *   **Custom Script:** A dedicated script that updates `transform.position` directly each frame based on velocity and direction. If used, this script should ideally run *only* on the server or be carefully designed to avoid client-side prediction issues unless specifically intended. *Currently, simple physics or direct transform manipulation within the server-side spawning logic is preferred.*
6.  **Lifetime/Boundary Check (`NetworkBulletLifetime.cs`):**
    *   The server continuously monitors the bullet's age via `lifeTimer`.
    *   If `enforceBounds` is true, the server checks if the bullet has crossed its designated boundary (`boundaryX`, `keepOnPositiveSide`).
    *   If the bullet's `lifeTimer` exceeds `maxLifetime` OR it goes out of bounds, the server calls `ReturnToPool()`.
7.  **Collision (`NetworkBulletLifetime.cs`):**
    *   The script's `OnTriggerEnter2D` method runs *only on the server*.
    *   It checks if the colliding object has a `PlayerHealth` component.
    *   If a player is hit, the server calls the `TakeDamage()` method on the player's `PlayerHealth` script.
    *   *Note: Currently, bullets are **not** returned to the pool immediately upon hitting a player. They persist until their lifetime or boundary condition is met.*
8.  **Despawn/Return:**
    *   When `ReturnToPool()` is called (due to lifetime, boundary, or potentially other game logic), the server uses `NetworkObjectPool.Instance.ReturnNetworkObject()`.
    *   This method calls `Despawn(false)` on the bullet's `NetworkObject` (removing it from clients without destroying the instance) and deactivates the GameObject, making it available for reuse.

## Key Considerations

-   **Server Authority:** All critical logic (spawning, lifetime management, collision detection, damage application) resides on the server to prevent cheating and ensure consistency.
-   **Pooling:** Essential for performance, especially with a high number of projectiles common in Touhou-style games.
-   **Client-Side:** Clients primarily receive state updates and render the bullets. They do not run authoritative gameplay logic for projectiles. Interpolation is handled by the `NetworkTransform` component (or similar) to smooth visual movement.
-   **Collision Layers:** Ensure proper physics collision layers are set up so bullets only interact with intended objects (e.g., players, potentially enemy hitboxes) and not other bullets unless specifically designed for that interaction.

## Future Enhancements

-   Different bullet behaviors (homing, accelerating, sinusoidal movement).
-   Bullet clearing mechanics (e.g., player bombing).
-   Specialized colliders for grazing.
-   Visual effects on spawn/despawn/hit. 