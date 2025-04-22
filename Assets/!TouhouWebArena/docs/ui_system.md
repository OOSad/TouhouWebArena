# UI System Documentation

## Overview

The UI system displays critical game information to the players, including health, spell charge, round status, and menus. It relies heavily on data synchronized from the server via Netcode for GameObjects to ensure consistency across clients.

## Key UI Elements

*   **HUD (Heads-Up Display):**
    *   **Health Bars:** Implemented by `PlayerHealthUI.cs`. Uses individual icons (prefabs) instantiated within a container. Displays the current health for a specific player (Player 1 or Player 2, configured via `targetPlayerId`).
    *   **Spellbar:** Implemented by `SpellBarController.cs`. Uses two `UnityEngine.UI.Image` components (`passiveFillImage`, `activeFillImage`) whose `fillAmount` property is updated to represent the passive and active charge levels.
    *   **Round Counters (Assumed):** UI elements to display the current round wins for each player (likely updated based on `NetworkVariables` from a GameManager).
    *   **Timers (Assumed):** Potential UI for round timers or cooldowns.
*   **Menus:**
    *   **Main Menu:** Contains UI for connecting to the server (`ClientConnectorDisconnector.cs`) and joining the matchmaking queue (`MatchmakerUI.cs`).
    *   **Character Select Screen:** Allows players to choose their character before the match starts (`CharacterSelector.cs`).
    *   **Pause Menu (TBD):** Functionality not yet detailed.
    *   **Results Screen (TBD):** Displayed at the end of a match.

## Data Binding & Updates

The UI needs to reflect the authoritative game state from the server.

*   **`NetworkVariables`:** This is the primary method for updating HUD elements linked to changing game state:
    *   **Health:** `PlayerHealthUI` subscribes to the `OnHealthChanged` event from the `PlayerHealth` script. This event is triggered when the server modifies the `PlayerHealth.CurrentHealth` NetworkVariable. `PlayerHealthUI` then updates the visible health icons locally.
    *   **Spell Charge:** `SpellBarController` continuously reads its `currentPassiveFill` and `currentActiveFill` NetworkVariables in its `Update()` loop and directly sets the `fillAmount` of its `Image` components. The server is responsible for updating these NetworkVariables.
    *   **Round Wins/Game State (Assumed):** Round counters or match status indicators would likely be driven by `NetworkVariables` located in a `GameManager` script. UI scripts would read these variables and update text or images accordingly.
*   **RPCs (`ClientRpc`):** While NetworkVariables handle continuous state, `ClientRpc` calls from the server *could* be used for infrequent, event-based UI updates, such as:
    *   Displaying "Round Start" or "Round Win" messages.
    *   Triggering specific UI animations or effects.
    *   Showing/hiding menus like the pause menu or results screen based on server commands.
*   **Local Updates:** Some UI elements might be purely local:
    *   Menu navigation highlights.
    *   Button hover effects.
    *   Potentially client-side feedback like input prompts (though game actions themselves are server-validated).

## Important UI Scripts

*   **`PlayerHealthUI.cs`:** Manages the health icon display for one player.
*   **`SpellBarController.cs`:** Manages the visual fill for one player's spell bar, driven by NetworkVariables.
*   **`MatchmakerUI.cs`:** Handles UI interactions for joining the matchmaking queue.
*   **`CharacterSelector.cs`:** Handles UI for selecting characters.
*   **`ClientConnectorDisconnector.cs`:** Handles UI button for connecting/disconnecting the client.
*   **(Hypothetical) `UIManager.cs`:** A central manager might exist to coordinate showing/hiding different UI panels or managing overall UI state, though not found in the initial search.
*   **(Hypothetical) `RoundCounterUI.cs`:** A script likely exists to display round wins based on networked game state data. 