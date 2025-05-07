# Networking Overview

This document provides an overview of the networking architecture for Touhou Web Arena, which uses Unity's Netcode for GameObjects package. The project has transitioned to a primarily **client-authoritative** model for core gameplay elements like player movement and basic projectile attacks to enhance responsiveness and reduce server load. Certain aspects like spellcard execution and enemy spawning may still retain server authority or use a mixed model.

## Core Concepts

Netcode for GameObjects provides several building blocks for networked applications:

*   **Server Authority (for some systems):** While player movement and basic shots are client-authoritative, the server remains the authority for aspects like match state, score, and potentially complex spellcard logic or enemy AI that requires undisputed state. For client-authoritative systems, the server acts more as a relay and a point of synchronization for late-joining clients or for resolving major discrepancies if needed (though this is not the primary model for those systems).
*   **`NetworkObject`:** Key game entities that need to be synchronized across the network (like Players) must have a `NetworkObject` component attached. This component gives the object a unique network identity. Client-side pooled objects (like basic projectiles) do *not* have `NetworkObject` components.
*   **`NetworkBehaviour`:** Scripts that contain networking logic (RPCs, NetworkVariables) on networked objects must inherit from `NetworkBehaviour` instead of `MonoBehaviour`.
*   **RPCs (Remote Procedure Calls):** Allow specific functions to be called across the network.
    *   **`ServerRpc`:** Used by a client (typically the owner of a `NetworkObject`) to request an action from the server, or to report an event that the server needs to know about or relay. The function name typically ends with `ServerRpc`.
    *   **`ClientRpc`:** Used by the server to command one or more clients to execute a function. The function name typically ends with `ClientRpc`. This is often used for triggering visual effects, playing sounds, or instructing clients to perform actions based on a server decision or a relayed client action.
    *   **Common Flow for Client-Initiated Actions:** When an owning client performs an action that other clients need to see (e.g., firing a basic shot), the typical flow is:
        1.  Owner Client: Executes the action locally (e.g., spawns a visual bullet).
        2.  Owner Client: Calls a `[ServerRpc]` method on one of its `NetworkBehaviour`s, sending relevant data to the server.
        3.  Server: The `[ServerRpc]` method executes on the server.
        4.  Server: Inside the `[ServerRpc]`, the server then calls a `[ClientRpc]` method, passing along the necessary data.
        5.  Clients: The `[ClientRpc]` method executes on all (or targeted) clients. Non-owning clients will then perform the visual action (e.g., spawn their own visual representation of the bullet). The owning client might ignore this RPC for that specific action if it already handled it locally.
*   **`NetworkVariable`:** Used to automatically synchronize simple data types from the server to all clients, or from an owner client to the server and then to other clients. Player position (`NetworkedPosition` in `ClientAuthMovement.cs`) is an example where the owner writes, and the server/other clients read. Health and score are likely still server-authoritative `NetworkVariables`.

## Starting a Session / Connection Flow

When running the game locally for development or testing using the Unity editor and standalone builds (or multiple editor instances), follow this specific connection procedure:

1.  **Start the Server:**
    *   **Default (Localhost):** Launch one instance of the game (editor or build). While the Main Menu is loaded, press **F9**. This instance becomes the dedicated server listening on the default address/port (likely 127.0.0.1:7777) and will have `ClientId = 0`. Pressing F9 again will stop the server.
    *   **Custom IP/Port:** Launch one instance. Press **F10** to reveal the custom IP address and Port input fields. Enter the desired values and click the "Start Custom Server" button.
2.  **Start Client(s):** Launch subsequent instances (editor or build).
    *   Enter a desired player name (optional, defaults to "Anonymous#[Random]").
    *   Click the **Queue** button. This will automatically attempt to connect the client to the server (using default transport settings) and, upon successful connection, immediately request to join the matchmaking queue.
    *   The first client connected will receive `ClientId = 1`, the second `ClientId = 2`, and so on.
3.  **Cancel:**
    *   If connecting, clicking **Cancel** stops the connection attempt.
    *   If connected (either waiting to queue or already queued), clicking **Cancel** will leave the queue (if applicable) and disconnect the client.

**Important:** Do **not** use the "Start Host" button if it still exists in the `NetworkManager` component's UI. The Host mode acts as both a server and a client simultaneously, which does not fit our intended dedicated server + two clients architecture for a standard 1v1 match. The primary methods for starting the server are now the F9 key or the F10 custom panel.

## Key Systems

Here's how major game systems interact with the network:

*   **Player Movement:** Player movement is **client-authoritative**. The owning client (`ClientAuthMovement.cs`) reads local inputs and directly applies movement to its character's `Rigidbody2D`. The owner's position is then synchronized to the server and other clients using a `NetworkVariable<Vector2>` named `NetworkedPosition` (owner writes, others read). Remote clients observe this `NetworkVariable` and update their local representation of the character accordingly. `NetworkTransform` is not used for player-controlled movement.
*   **Shooting / Basic Attacks (Client-Authoritative):**
    *   The owning client's `PlayerShootingController.cs` detects input.
    *   It spawns projectiles locally from `ClientGameObjectPool.cs` (these are non-`NetworkObject` GameObjects).
    *   It initializes client-side scripts on the projectile like `BulletMovement.cs` (for movement) and `ClientProjectileLifetime.cs` (for despawning).
    *   It then calls a `FireShotServerRpc` (example name) on itself, sending necessary parameters (bullet type, position, velocity, etc.) to the server.
    *   The server, upon receiving the `FireShotServerRpc`, calls a corresponding `FireShotClientRpc`.
    *   All clients (including the owner, though it may ignore it for this specific shot) receive the `FireShotClientRpc`. Remote (non-owning) clients will then use the received parameters to spawn their own visual-only representation of the bullet from their local `ClientGameObjectPool` and initialize its client-side behavior scripts.
    *   Bullet movement, lifetime, and basic visual collision (e.g., with "Fairy" or "Spirit" tagged objects for visual feedback) are handled client-side by `BulletMovement.cs` and `ClientProjectileLifetime.cs`.
*   **Charge Attacks & Spellcard Activation:** This is likely a mixed model. The client (`PlayerShootingController`) detects input and sends a `RequestChargeAttackServerRpc` or `RequestSpellcardServerRpc`.
    *   The server-side `SpellBarManager` might still manage charge levels and spell costs authoritatively.
    *   `ServerAttackSpawner` likely still handles the *logic* of these more complex attacks.
    *   How these attacks are *visualized* on clients might adopt the client-authoritative projectile pattern (server tells clients to spawn specific sequences/effects) or use server-spawned `NetworkObject` projectiles if their state needs to be more robustly synchronized. This part needs to be detailed as it's refactored.
*   **Enemy Spawning & Behavior:** Currently assumed to be server-controlled. A server-side system spawns enemy `NetworkObject`s. Enemy AI and movement run on the server, with state synchronized via `NetworkTransform` or similar. (This may be a candidate for future refactoring towards client-side spawning with server commands).
*   **Health & Damage:** Remains server-authoritative. If client-side bullets detect hits for visual purposes (e.g., hitting a "Fairy"), they might send an RPC to the server to register the hit. The server then authoritatively applies damage and updates health `NetworkVariables`.
*   **Game State (Score, Rounds):** Managed by a server-authoritative `RoundManager` script using `NetworkVariables` and RPCs.
*   **Object Pooling:**
    *   **Client-Side:** `ClientGameObjectPool.cs` is used by clients to pool purely visual GameObjects like basic projectiles. These objects do not have `NetworkObject` components. `PooledObjectInfo.cs` helps identify prefabs for this pool.
    *   **Server-Side:** The old `NetworkObjectPool.cs` (if still present) would be used for pooling `NetworkObject`s that are server-authoritatively spawned (e.g., some spellcard bullets, enemies). Its usage for basic player projectiles has been replaced.

## Important Scripts (Examples)

*   **`PlayerShootingController.cs`:** Attached to player prefab. Handles **client-side** input detection. For basic shots, it spawns projectiles locally for the owner using `ClientGameObjectPool`, initializes their client-side behaviors, and then uses a ServerRpc -> ClientRpc flow to instruct other clients to spawn corresponding visual projectiles. For charge attacks/spellcards, it sends `ServerRpc` requests to server managers.
*   **`ClientAuthMovement.cs`:** Attached to player prefab. Handles **client-authoritative** movement. Reads local input, applies movement directly to its `Rigidbody2D`, and updates a `NetworkVariable<Vector2>` (`NetworkedPosition`) for synchronization. Non-owning instances read `NetworkedPosition`.
*   **`BulletMovement.cs`:** A `MonoBehaviour` attached to client-side projectile prefabs. Handles visual movement (`transform.Translate`) and basic collision detection (e.g., against "Fairy", "Spirit" tags) to trigger despawning via `ClientProjectileLifetime`.
*   **`ClientProjectileLifetime.cs`:** A `MonoBehaviour` attached to client-side projectile prefabs. Manages returning the projectile to the `ClientGameObjectPool` after a set duration or on collision.
*   **`ClientGameObjectPool.cs`:** Manages pools of non-NetworkObject GameObjects on the client-side, used for visual projectiles.
*   **`PooledObjectInfo.cs`:** Stores a prefab ID string for use with `ClientGameObjectPool`.
*   **`ServerAttackSpawner.cs`:** Server-side singleton. Likely still central for orchestrating server-authoritative attacks like complex spellcards or charge attacks if they are not fully client-visualized.
*   **`SpellBarManager.cs`:** Server-side singleton service. Manages `NetworkVariables` for spell bar states and handles spell cost consumption.
*   **`CharacterStats.cs`:** Holds core player stats like `Health` (`NetworkVariable`, server-authoritative).
*   **`ServerBasicShotSpawner.cs`:** DEPRECATED for player basic shots. May still be used if enemies or spellcards fire similar "basic" shots that are server-authoritative.
*   **`ServerPooledSpawner.cs`:** Helper for spawning server-authoritative pooled NetworkObjects.
*   **`NetworkObjectPool.cs`:** Manages server-authoritative pooled `NetworkObject`s. Its direct use for player projectiles is replaced by `ClientGameObjectPool`.
*   **`RoundManager.cs` (or similar):** Server-authoritative game state manager.

## Networked Systems Overview

This document provides a high-level overview of the key networked systems in Touhou Web Arena, built using Unity's Netcode for GameObjects package.

### Player Movement

Player movement is handled with a **client-authoritative** approach to prioritize responsiveness and reduce server load. The core script responsible is `ClientAuthMovement.cs`.

*   **Input:** The owning client's `ClientAuthMovement` script reads local player inputs (horizontal, vertical, focus) each frame in its `Update()` method.
*   **Local Movement Application:** The owning client directly calculates and applies the movement to its `Rigidbody2D` component in `FixedUpdate()` using `rb.MovePosition()`. This ensures immediate responsiveness to player input. Movement speed is determined by `CharacterStats` and the focus state from `PlayerFocusController`.
*   **State Synchronization:**
    *   After the owner applies movement locally, it updates a `NetworkVariable<Vector2> NetworkedPosition` with its new `rb.position`.
    *   This `NetworkVariable` is configured with `NetworkVariableWritePermission.Owner`, so only the owning client can change its value.
    *   The change to `NetworkedPosition` is automatically sent from the owner to the server, which then relays it to all other (remote) clients.
*   **Remote Client Update:**
    *   Remote clients (where `IsOwner` is false for the player character) have their `ClientAuthMovement` script's `Update()` method observe `NetworkedPosition.Value`.
    *   Currently, remote clients directly set their local `Rigidbody2D`'s position to this `NetworkedPosition.Value`, resulting in a direct snap to the synchronized position. (Interpolation is planned for future implementation to smooth this visual update).
*   **`NetworkTransform` Not Used for Player Position/Rotation:** The built-in `NetworkTransform` component is *not* used for synchronizing the authoritative position or rotation of player characters to avoid previous issues with authority conflicts and snapping.
*   **Benefits:** This client-authoritative approach significantly reduces input lag for the controlling player and offloads the processing of movement updates from the server.
*   **Considerations:** Cheating (e.g., speed hacks, teleportation) is not actively prevented with this model for movement, as the client has full control over its position. Visual discrepancies for remote clients due to latency are expected but managed by the `NetworkVariable` updates.
