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
2.  **Gameplay:** Players move, shoot, charge, use spellcards (including Level 4 spellcards which summon server-authoritative Illusions that then perform client-simulated attacks). Enemies spawn via `FairySpawner` (triggering client-simulated fairies) and `SpiritSpawner` (triggering client-simulated spirits). State changes (health, spell charge, enemy death, spellcard execution) are managed authoritatively by the server and synchronized via `NetworkVariables` and RPCs (as detailed in `networking_overview.md`). Brief "action stop" effects (game slowdown) occur locally when spellcards are activated or when a player takes a near-death hit.
3.  **Round End Condition:** A round ends when one player's health reaches zero. This is detected server-side by `PlayerHealth.cs`, which invokes `PlayerHealth.OnPlayerDeathServer`.
4.  **Round Conclusion:** This detailed sequence is orchestrated by `RoundManager.HandlePlayerDeathServer` and its `RoundResetCoroutine` on the server:
    *   The server determines the round winner and updates the score (`Player1Score` / `Player2Score`).
    *   If the match winning score is reached, `MatchEndedClientRpc` is sent to the relevant clients to display the match end UI, and the round reset sequence is skipped.
    *   Otherwise, the following round transition begins:
        *   `IsRoundActive` (NetworkVariable) is set to `false`.
        *   Enemy spawners (`ServerSpawnerManager`) are paused.
        *   Entities are cleared:
            *   Server-side entities (like Illusions) are cleaned up by `ServerEntityCleanupHelper.CleanupAllEntitiesServer()`.
            *   Clients are instructed to clear their client-simulated entities via `ClientEntityCleanupHandler.ClearAllClientSideVisualsClientRpc()`.
            *   This clearing happens after a very brief initial delay (e.g., 0.2s) in the coroutine.
        *   **"Catch Breath" Period:** The server waits for `catchBreathDuration` (e.g., 2 seconds) allowing a pause after entities vanish.
        *   **Screen Wipe & Player Reset:**
            *   The server calls `ExecuteScreenWipeClientRpc` on all clients.
            *   Clients receive this RPC and `ClientScreenWipeController.Instance.StartWipeEffect()` is called, starting the local screen wipe animation (e.g., playspace images fade in, hold, then fade out).
            *   The server waits for `serverPlayerResetDelayDuringWipe` (e.g., 0.4s or 1s, matching the client's wipe-in animation duration).
            *   **Player positions and health are reset** by `ServerPlayerResetHelper.ResetPlayersServer()` during this period, while client screens are ideally obscured by the wipe. Spell bars are also reset.
            *   The server then waits for the remainder of `screenWipeDuration` (total expected client wipe animation time, e.g., 1s or 2.2s).
        *   **Pre-Round Delay:** The server waits for an additional `roundResetDelay` (e.g., 3 seconds).
        *   **Next Round Start:**
            *   `RoundTime` is reset to 0.
            *   `IsRoundActive` is set back to `true`.
            *   Spawners (`ServerSpawnerManager`) are resumed.
            *   Player input, which might have been locked during the transition or invincibility, is implicitly re-enabled as players are no longer in an invincible state and `IsRoundActive` is true.

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