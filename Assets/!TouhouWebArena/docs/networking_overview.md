# Networking Overview

This document provides an overview of the networking architecture for Touhou Web Arena, which uses Unity's Netcode for GameObjects package. The primary goal is a **server-authoritative** model, ensuring fairness and cheat prevention, while employing techniques like **client-side prediction** for player movement to maintain responsiveness.

## Core Concepts

Netcode for GameObjects provides several building blocks for networked applications:

*   **Server Authority:** The server is the ultimate source of truth for the game state. It runs the core simulation, validates player actions, spawns enemies, calculates damage, determines spellcard effects, and manages the overall match flow (scores, rounds). Clients primarily send inputs and receive state updates from the server. [https://pvigier.github.io/2019/09/08/beginner-guide-game-networking.html]
*   **`NetworkObject`:** Key game entities that need to be synchronized across the network (like Players, Enemies, and potentially complex Projectiles) must have a `NetworkObject` component attached. This component gives the object a unique network identity.
*   **`NetworkBehaviour`:** Scripts that contain networking logic (RPCs, NetworkVariables) must inherit from `NetworkBehaviour` instead of `MonoBehaviour`. Many of our core scripts (`PlayerShooting`, `CharacterStats`, enemy controllers) are `NetworkBehaviours`.
*   **RPCs (Remote Procedure Calls):** Allow specific functions to be called across the network.
    *   **`ServerRpc`:** Used by a client to request an action from the server. The function name typically ends with `ServerRpc`. Examples in our code include `RequestFireServerRpc`, `PerformChargeAttackServerRpc`, and `RequestSpellcardServerRpc` in `PlayerShooting.cs`. Clients *ask* the server to perform these actions.
    *   **`ClientRpc`:** Used by the server to command one or more clients to execute a function. The function name typically ends with `ClientRpc`. This might be used for triggering visual effects, playing sounds, or updating UI elements that don't rely on `NetworkVariables`. The server *tells* clients to do these things.
*   **`NetworkVariable`:** Used to automatically synchronize simple data types (like health, score, charge level, position, rotation) from the server to all clients. When the server changes the value of a `NetworkVariable`, the change is automatically propagated to all clients. Examples include player health in `CharacterStats` and charge levels in `SpellBarController`. This is efficient for frequently changing state.

## Key Systems

Here's how major game systems interact with the network under our server-authoritative model:

*   **Player Movement:** This is planned as an exception for responsiveness. The client likely uses **client-side prediction**: it moves the local player character immediately upon input. Simultaneously, it sends the input state to the server via `ServerRpc`. The server processes the input, simulates the movement authoritatively, and sends corrections back if the client's prediction was wrong (e.g., due to collision or latency). This keeps movement feeling smooth on the client even with some network latency.
*   **Shooting / Charge Attacks:** The client requests to fire or use a charge attack by sending a `ServerRpc` (e.g., `RequestFireServerRpc`, `PerformChargeAttackServerRpc`) from `PlayerShooting.cs`. The server receives the request, validates it (e.g., checks cooldowns, charge state), and if valid, authoritatively spawns the necessary projectiles or attack effects using the `NetworkObject` spawning system (potentially via `NetworkObjectPool`).
*   **Spellcard Activation:** When a player charges to a spellcard level and releases, the client sends a `RequestSpellcardServerRpc` from `PlayerShooting.cs`. The server validates the request (correct charge level), identifies the correct `SpellcardData` based on character and level (loading it from `Resources`), consumes the player's charge on the server-side `SpellBarController`, and then initiates the spellcard execution. It likely uses a `ClientRpc` targeted at the *opponent* client, telling that client's `SpellcardExecutor` script to run the specified `SpellcardData`.
*   **Enemy Spawning & Behavior:** This is fully server-controlled. A server-side system (e.g., a Wave Manager or Spawner script) decides when, where, and what type of enemies (fairies, spirits) to spawn. The server spawns their `NetworkObject`s. Enemy movement (like spline paths for fairies) and AI logic run exclusively on the server. The resulting position and rotation are synchronized to clients using components like `NetworkTransform` or potentially custom `NetworkVariable` updates.
*   **Health & Damage:** Damage calculation is server-authoritative. When a server-controlled entity (e.g., a bullet `NetworkObject`) collides with a player `NetworkObject`, the server detects this collision. It calculates the damage, updates the player's health (which is likely a `NetworkVariable` within `CharacterStats.cs`), and the health change automatically synchronizes to all clients. Clients might play feedback effects (sound, visuals) in response to the health change or via a targeted `ClientRpc` from the server.
*   **Game State (Score, Rounds):** The server manages the overall game state, such as the current score for each player and which round it is. When a player loses all HP, the server detects this, updates the score (likely stored in `NetworkVariables` in a GameManager script), checks for match end conditions, and potentially triggers round transition logic via `ClientRpc`s to all clients.

## Important Scripts (Examples)

*   **`PlayerShooting.cs`:** Handles client input for shooting/charging/spellcards, sends `ServerRpc` requests. Contains server-side logic to validate requests and initiate attacks/spellcards.
*   **`CharacterStats.cs`:** Likely holds core player stats like `Health` as `NetworkVariables`.
*   **`PlayerMovement.cs` (or similar):** Implements client-side prediction logic and sends input via `ServerRpc`. Contains server-side logic for validating movement and reconciling position.
*   **`SpellcardData.cs`:** Defines the data structure for spellcards (ScriptableObject). Not a `NetworkBehaviour`.
*   **`SpellcardExecutor.cs`:** Executes spellcard logic on clients when instructed by the server (likely via RPC).
*   **`SpellBarController.cs`:** Manages the UI and state (using `NetworkVariables`) for the player's spell charge bar.
*   **`NetworkObjectPool.cs` (if used):** Helps efficiently manage spawning and despawning of networked objects like bullets and enemies.
*   **Enemy Scripts (e.g., `FairyMovement.cs`):** Contain server-side logic for enemy behavior. May use `NetworkTransform` for synchronization.
*   **GameManager.cs (or similar):** Likely a server-authoritative script holding overall game state like scores, round timers, etc., using `NetworkVariables`. 