# Networking Overview

This document provides an overview of the networking architecture for Touhou Web Arena, which uses Unity's Netcode for GameObjects package. The primary goal is a **server-authoritative** model, ensuring fairness and cheat prevention, while potentially employing techniques like **client-side prediction** for player movement to maintain responsiveness.

## Core Concepts

Netcode for GameObjects provides several building blocks for networked applications:

*   **Server Authority:** The server is the ultimate source of truth for the game state. It runs the core simulation, validates player actions, spawns enemies, calculates damage, determines spellcard effects, and manages the overall match flow (scores, rounds). Clients primarily send inputs and receive state updates from the server. [https://pvigier.github.io/2019/09/08/beginner-guide-game-networking.html]
*   **`NetworkObject`:** Key game entities that need to be synchronized across the network (like Players, Enemies, and potentially complex Projectiles) must have a `NetworkObject` component attached. This component gives the object a unique network identity.
*   **`NetworkBehaviour`:** Scripts that contain networking logic (RPCs, NetworkVariables) must inherit from `NetworkBehaviour` instead of `MonoBehaviour`. Examples include `PlayerShootingController`, `ServerAttackSpawner`, `SpellBarManager`, `CharacterStats`, and enemy controllers.
*   **RPCs (Remote Procedure Calls):** Allow specific functions to be called across the network.
    *   **`ServerRpc`:** Used by a client to request an action from the server. The function name typically ends with `ServerRpc`. Examples include `RequestFireServerRpc`, `RequestChargeAttackServerRpc`, and `RequestSpellcardServerRpc` in `PlayerShootingController.cs`. The client *asks* the server to perform these actions via the appropriate server-side manager (e.g., `ServerAttackSpawner`, `SpellBarManager`).
    *   **`ClientRpc`:** Used by the server to command one or more clients to execute a function. The function name typically ends with `ClientRpc`. This might be used for triggering visual effects, playing sounds, or updating UI elements that don't rely on `NetworkVariables`.
*   **`NetworkVariable`:** Used to automatically synchronize simple data types (like health, score, charge level, position, rotation) from the server to all clients. When the server changes the value of a `NetworkVariable`, the change is automatically propagated to all clients. Examples include player health in `CharacterStats` and charge levels in `SpellBarController` (managed by `SpellBarManager`).

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

Here's how major game systems interact with the network under our server-authoritative model:

*   **Player Movement:** *(Current implementation needs verification)* Likely uses client-side prediction: the client moves locally and sends input via `ServerRpc`. The server validates and reconciles.
*   **Shooting / Charge Attacks:** The client (`PlayerShootingController`) detects input and sends a `RequestFireServerRpc` or `RequestChargeAttackServerRpc` to the server. The server-side singleton `ServerAttackSpawner` receives the request, validates it (implicitly by receiving the correct RPC), and authoritatively spawns the necessary projectiles using the `NetworkObject` spawning system (potentially via `NetworkObjectPool` for basic shots, direct instantiation for charge attacks).
*   **Spellcard Activation:** The client (`PlayerShootingController`) detects input release at a spellcard charge level and sends `RequestSpellcardServerRpc`. The server-side `SpellBarManager` receives this, checks and consumes the spell cost. If successful, it calls the `ServerAttackSpawner`, which loads the appropriate `SpellcardData`, determines the target, and executes the spellcard pattern by spawning the bullets server-side. These `NetworkObject` bullets are then automatically synchronized to all clients.
*   **Enemy Spawning & Behavior:** Fully server-controlled. A server-side system spawns enemy `NetworkObject`s. Enemy AI and movement run on the server, with state synchronized via `NetworkTransform` or similar.
*   **Health & Damage:** Server-authoritative. Collision between server-controlled entities (bullets, enemies) and player `NetworkObject`s is detected on the server. The server updates the player's health `NetworkVariable` (e.g., in `CharacterStats`), which synchronizes to clients.
*   **Game State (Score, Rounds):** Managed by a server-authoritative GameManager script using `NetworkVariables`.

## Important Scripts (Examples)

*   **`PlayerShootingController.cs`:** Attached to player prefab. Handles **client-side** input detection for shooting/charging/spellcards, manages local burst fire timing, and sends `ServerRpc` requests to server managers.
*   **`ServerAttackSpawner.cs`:** Server-side singleton service. Handles requests for spawning basic shots, charge attacks, and executing spellcard patterns by spawning projectiles/effects authoritatively.
*   **`SpellBarManager.cs`:** Server-side singleton service. Manages the state (`NetworkVariables`) of all player `SpellBarController`s, including passive fill, active charge updates based on client requests, and spell cost consumption.
*   **`CharacterStats.cs`:** Holds core player stats like `Health` (likely `NetworkVariable`).
*   **`PlayerMovement.cs` (or similar):** Handles player movement, likely using client-side prediction and server reconciliation.
*   **`SpellcardData.cs`:** ScriptableObject defining spellcard patterns. Not a `NetworkBehaviour`.
*   **`SpellBarController.cs`:** Attached to UI elements. Displays spell bar state based on its `NetworkVariables` (which are managed by `SpellBarManager`).
*   **`NetworkObjectPool.cs` (if used):** Manages pooled network objects.
*   **Enemy Scripts (e.g., `FairyMovement.cs`):** Server-side enemy logic.
*   **GameManager.cs (or similar):** Server-authoritative game state manager. 