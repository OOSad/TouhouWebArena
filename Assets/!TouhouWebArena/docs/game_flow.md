# Game Flow & State Management Documentation

## Overview

This document describes the typical sequence of events from launching the application to completing a match. The game uses Netcode for GameObjects, with the server holding authority over the core game state and progression.

## Connection & Matchmaking

1.  **Launch:** The player launches the game executable.
2.  **Server Startup (Manual/Local):** Currently, the server must be running locally. This is likely initiated manually using components like `ServerStarterStopper.cs` before clients can connect.
3.  **Client Connection (Manual):** From the main menu scene, the player clicks a "Connect" button (handled by `ClientConnectorDisconnector.cs` or similar UI script). This calls `NetworkManager.Singleton.StartClient()`.
4.  **Queueing:** Once connected to the server, the client interacts with the `MatchmakerUI` (which uses `Matchmaker.cs`). The player provides a name and clicks a "Join Queue" button.
    *   `MatchmakerUI` calls `Matchmaker.JoinQueueServerRpc()`, sending the player's name and `LocalClientId` to the server.
    *   The server-side `Matchmaker.cs` adds the player to a waiting queue.
5.  **Match Found:** When two players are in the queue, the server-side `Matchmaker.cs` identifies them as opponents.
6.  **Match Start:** The `Matchmaker.cs` triggers the transition to the next phase, by:
    *   Storing matched player data (in `PlayerDataManager.cs`).
    *   Instructing the `NetworkManager.SceneManager` to load the character selection scene for the matched clients.

## Scene Management

*   Scene transitions are managed by the server using `NetworkManager.Singleton.SceneManager.LoadScene()`.
*   Clients are automatically transitioned when the server initiates a scene load.
*   **Scenes:**
    *   **Main Menu Scene:** Initial scene with connection and queueing UI.
    *   **Character Select Scene:** Loaded after a match is found. Players select their characters here (UI likely interacts with `CharacterSelector.cs` or `PlayerSetupManager.cs`). Character selections might be sent to the server via `ServerRpc`.
    *   **Gameplay Scene:** Loaded after character selection is complete. Contains the main playfield, spawners, etc.

## Round Lifecycle (Server-Authoritative)

1.  **Round Start:** When the Gameplay Scene loads, the server initializes the round. The **`GameInitializer.cs`** script (on the `GameManager` object) likely plays a key role here:
    *   It potentially activates or configures the `FairySpawner` prefabs it holds references to.
    *   It likely ensures player prefabs are spawned correctly based on selections (coordinating with `PlayerSetupManager` if applicable).
    *   Resets player health (to `startingHealth` from `CharacterStats`).
    *   Resets spell bars (`SpellBarController` state).
    *   Starts the **`SpiritSpawner.cs`** (on the `GameManager`) to begin periodic spirit spawning. `SpiritSpawner.cs` then sends RPCs to `ClientSpiritSpawnHandler.cs` on clients, which handle the actual spawning and simulation of spirits locally.
    *   Other managers on `GameManager` (`FairyRegistry`, `SpiritRegistry`, `PathManager`, `ExtraAttackManager`) are ready.
2.  **Gameplay:** Players move, shoot, charge, use spellcards (including Level 4 spellcards which summon server-authoritative Illusions that then perform client-simulated attacks). Enemies spawn via `FairySpawner` (triggering client-simulated fairies) and `SpiritSpawner` (triggering client-simulated spirits). State changes (health, spell charge, enemy death, spellcard execution) are managed authoritatively by the server and synchronized via `NetworkVariables` and RPCs (as detailed in `networking_overview.md`).
3.  **Round End Condition:** A round ends when one player's health reaches zero. This is detected server-side (likely by monitoring the health `NetworkVariable` in `CharacterStats`).
4.  **Round Conclusion:**
    *   The server determines the round winner (the player whose opponent reached zero health).
    *   Gameplay might pause briefly. Enemy spawners (`FairySpawner`, `SpiritSpawner`) are likely stopped/paused by the `GameManager` or a related script.
    *   The server updates the match score (TBD, potentially managed by a script component on `GameManager`).
    *   The server checks if the match end condition has been met.

## Match Lifecycle (Server-Authoritative)

1.  **Match Score:** A match consists of multiple rounds (currently First-to-Two points wins, as per `README.md`). The server tracks round wins per player.
2.  **Match End Condition:** The match ends when one player reaches the required number of round wins (e.g., 2).
3.  **Match Conclusion:**
    *   The server determines the match winner.
    *   The server might trigger a results screen or transition back to a menu scene using `NetworkManager.SceneManager`.
    *   Player data might be cleared from `PlayerDataManager` or the `Matchmaker`.

## Key Scripts & Systems

*   **`GameManager` (GameObject):** The central GameObject in the Gameplay Scene hosting many core management systems as components. Has a `NetworkObject` component, indicating its state or components might be networked.
*   **`NetworkManager` (Singleton):** Core Netcode component handling connections, scene management, spawning. Accessed via `NetworkManager.Singleton`.
*   **`GameInitializer.cs` (Component on GameManager):** Responsible for initializing the game state and systems when the Gameplay Scene loads. References the `PlayerOneFairySpawner` and `PlayerTwoFairySpawner` prefabs, likely activating/configuring them at round start.
*   **`Matchmaker.cs`:** Handles the player queue and initiating matches on the server (likely runs in the Main Menu or a persistent scene).
*   **`PlayerDataManager.cs`:** Stores data about connected/matched players on the server.
*   **`FairyRegistry.cs` (Component on GameManager):** Server-side singleton registry tracking active fairies.
*   **`SpiritRegistry.cs` (Component on GameManager):** Server-side singleton registry tracking active spirits.
*   **`PathManager.cs` (Component on GameManager):** Manages references to the fairy path data (held under `PlayerOneFairyPaths`, `PlayerTwoFairyPaths` transforms) for each player side.
*   **`SpiritSpawner.cs` (Component on GameManager):** Server-side script responsible for periodically spawning `Spirit` prefabs in designated zones, using configured intervals and the `Spirit` prefab reference.
*   **`ExtraAttackManager.cs` (Component on GameManager):** Server-side singleton that manages the triggering and execution of character-specific Extra Attacks, using assigned prefabs (`ReimuExtraAttackOrb`, `MarisaExtraAttackEarthlightRay`).
*   **Character-Specific Spawners (Components on GameManager):** `ReimuExtraAttackOrbSpawner.cs`, `MarisaExtraAttackSpawner.cs` provide helper logic/positions for their respective extra attacks.
*   **`CharacterSelector.cs` / `PlayerSetupManager.cs`:** Involved in handling character selection UI and communicating choices to the server, potentially triggering the gameplay scene load.
*   **UI Scripts (`MatchmakerUI`, `ClientConnectorDisconnector`, etc.):** Handle user interaction in menu scenes and communicate with networking components. 