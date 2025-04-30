# UI System Documentation

## Overview

The UI system displays critical game information to the players, including health, spell charge, round status, and menus. It relies heavily on data synchronized from the server via Netcode for GameObjects to ensure consistency across clients.

## Key UI Elements

*   **HUD (Heads-Up Display):**
    *   **Health Bars:** Implemented by `PlayerHealthUI.cs`. Uses individual icons (prefabs) instantiated within a container. Displays the current health for a specific player (Player 1 or Player 2, configured via `targetPlayerId`).
    *   **Spellbar:** Implemented by `SpellBarController.cs`. Uses two `UnityEngine.UI.Image` components (`passiveFillImage`, `activeFillImage`) whose `fillAmount` property is updated to represent the passive and active charge levels. Each instance is assigned a `TargetPlayerRole` (Player1 or Player2) in the Inspector to determine which player it represents.
    *   **Round Counters (Assumed):** UI elements to display the current round wins for each player (likely updated based on `NetworkVariables` from a GameManager).
    *   **Timers (Assumed):** Potential UI for round timers or cooldowns.
*   **Menus:**
    *   **Main Menu:** Contains UI for connecting to the server (`ClientConnectorDisconnector.cs`) and joining the matchmaking queue (`MatchmakerUI.cs`). Handles basic keyboard/gamepad navigation (`MainMenuNavigator.cs`).
    *   **Character Select Screen:**
        *   Allows players to choose their character before the match starts (`CharacterSelector.cs`).
        *   Features interactive buttons for each character.
        *   Includes **Synopsis Panels** (`SynopsisPanelController.cs`) for Player 1 and Player 2, displaying detailed character information (illustration, stats, attack details) sourced from `CharacterSynopsisData` ScriptableObjects. Player 2's illustration is mirrored.
        *   Supports keyboard/gamepad navigation and selection confirmation.
    *   **Pause Menu (TBD):** Functionality not yet detailed.
    *   **Results Screen (TBD):** Displayed at the end of a match.

## Data Binding & Updates

The UI needs to reflect the authoritative game state from the server.

*   **`NetworkVariables`:** This is the primary method for updating HUD elements linked to changing game state:
    *   **Health:** `PlayerHealthUI` subscribes to the `OnHealthChanged` event from the `PlayerHealth` script. This event is triggered when the server modifies the `PlayerHealth.CurrentHealth` NetworkVariable. `PlayerHealthUI` then updates the visible health icons locally.
    *   **Spell Charge:**
        *   The server's `SpellBarManager` finds all `SpellBarController` instances and caches them based on their assigned `TargetPlayerRole`.
        *   The `SpellBarManager` updates the `currentPassiveFill` and `currentActiveFill` NetworkVariables based on game events and client input (forwarded via RPCs).
        *   The client-side `SpellBarController` script continuously reads its own `currentPassiveFill` and `currentActiveFill` NetworkVariables in its `Update()` loop and directly sets the `fillAmount` of its `Image` components.
    *   **Character Selections (via `PlayerDataManager`):** The `PlayerDataManager` uses NetworkVariables internally to sync `PlayerData`, including the `SelectedCharacter`. UI elements subscribe to the `PlayerDataManager.OnPlayerDataUpdated` event.
        *   **Character Select Synopsis Panels:** `CharacterSelector` listens to `OnPlayerDataUpdated` and calls `UpdateDisplay` on the appropriate `SynopsisPanelController` for both P1 and P2, showing/hiding the panel based on whether a character is selected.
    *   **Round Wins/Game State (Assumed):** Round counters or match status indicators would likely be driven by `NetworkVariables` located in a `GameManager` script. UI scripts would read these variables and update text or images accordingly.
*   **RPCs (`ClientRpc`):** While NetworkVariables handle continuous state, `ClientRpc` calls from the server *could* be used for infrequent, event-based UI updates, such as:
    *   Displaying "Round Start" or "Round Win" messages.
    *   Triggering specific UI animations or effects.
    *   Showing/hiding menus like the pause menu or results screen based on server commands.
*   **Local Updates:** Some UI elements are driven purely by local interactions or state:
    *   Menu navigation highlights (e.g., `EventSystem.currentSelectedGameObject`).
    *   Button hover effects.
    *   **Character Select Synopsis (Highlight):** The local player's synopsis panel (`localSynopsisPanel` in `CharacterSelector`) is updated immediately in `LateUpdate` based on the `EventSystem`'s currently selected button, providing instant feedback during navigation before a character is confirmed. This uses `CharacterSelector.UpdateSynopsisPanelForSelection`.
    *   Application focus handling (`OnApplicationFocus`) in `CharacterSelector` to maintain navigation state.
    *   Potentially client-side feedback like input prompts (though game actions themselves are server-validated).

## Important UI Scripts

*   **`PlayerHealthUI.cs`:** Manages the health icon display for one player.
*   **`SpellBarController.cs`:** Manages the visual fill for one player's spell bar, driven by NetworkVariables. Must have its `TargetPlayerRole` set in the Inspector.
*   **`MatchmakerUI.cs`:** Handles UI interactions for joining the matchmaking queue.
*   **`CharacterSelector.cs`:** Handles UI for selecting characters, including button listeners, keyboard/gamepad navigation, focus management, and updating the character synopsis panels based on both local highlights and networked player data changes.
*   **`SynopsisPanelController.cs`:** Controls the UI elements within a single character synopsis panel, updating text and images based on data from a `CharacterSynopsisData` object.
*   **`ClientConnectorDisconnector.cs`:** Handles UI button for connecting/disconnecting the client.
*   **`MainMenuNavigator.cs`:** Handles keyboard/gamepad navigation specifically for the Main Menu.
*   **(Hypothetical) `UIManager.cs`:** A central manager might exist to coordinate showing/hiding different UI panels or managing overall UI state, though not found in the initial search.
*   **(Hypothetical) `RoundCounterUI.cs`:** A script likely exists to display round wins based on networked game state data. 