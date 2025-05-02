# UI System Documentation

## Overview

The UI system displays critical game information to the players, including health, spell charge, round status, and menus. It relies heavily on data synchronized from the server via Netcode for GameObjects to ensure consistency across clients.

## Key UI Elements

*   **HUD (Heads-Up Display):**
    *   **Health Bars:** Implemented by `PlayerHealthUI.cs`. Uses individual icons (prefabs) instantiated within a container. Displays the current health for a specific player (Player 1 or Player 2, configured via `targetPlayerId`).
    *   **Spellbar:** Implemented by `SpellBarController.cs`. Uses two `UnityEngine.UI.Image` components (`passiveFillImage`, `activeFillImage`) whose `fillAmount` property is updated to represent the passive and active charge levels. Each instance is assigned a `TargetPlayerRole` (Player1 or Player2) in the Inspector to determine which player it represents.
    *   **Round Timer:** Implemented by `RoundTimerDisplay.cs`. Displays the elapsed time for the current round in MM:SS format, driven by the `RoundTime` NetworkVariable from `RoundManager.cs`. Typically located at the top-center of the screen.
    *   **Round Indicators:** Implemented by `RoundIndicatorDisplay.cs`. Displays sakura petal icons (typically two per player, near the top-right of their respective play areas) to represent rounds won. Driven by the `Player1Score` and `Player2Score` NetworkVariables from `RoundManager.cs`.
    *   **Latency Counter:** Implemented by `LatencyDisplay.cs`. Displays the current network round-trip time (RTT) in milliseconds for the client connection. Typically located at the bottom-center of the screen.
    *   **Spellcard Banner:** Implemented by `SpellcardBannerDisplay.cs` (a NetworkBehaviour Singleton). Displays a character-specific banner image when a player activates a spellcard. Triggered via `ShowBannerClientRpc` from the server (`ServerAttackSpawner.cs`), which sends the caster's role and character name. Uses a list of `CharacterBannerInfo` (mapping internal names to Sprites) configured in the Inspector.
*   **Menus:**
    *   **Main Menu:** Contains UI for connecting to the server (`ClientConnectorDisconnector.cs`) and joining the matchmaking queue (`MatchmakerUI.cs`). Includes keyboard navigation support via `MainMenuNavigator.cs`.
    *   **Character Select Screen:** 
        *   Implemented by `CharacterSelector.cs`. Allows players assigned Player 1 or Player 2 roles to select their character via button clicks or keyboard navigation.
        *   Displays detailed character information dynamically using **Synopsis Panels** (`SynopsisPanelController.cs`). There's one panel prefab configured for Player 1 and another for Player 2 (with a mirrored layout).
        *   Each panel is populated with data from a corresponding **`CharacterSynopsisData` ScriptableObject**. These ScriptableObjects store character illustrations, names, titles, descriptions, stats (like speed and charge), and icons for Extra/Charge attacks. They are linked to characters via the `internalName` field, which must match the internal names used in `CharacterStats.cs` (e.g., "HakureiReimu").
        *   `CharacterSelector.cs` listens for changes to `PlayerData` (specifically the `SelectedCharacter` NetworkVariable) and updates the appropriate synopsis panel when a character is selected by either player.
        *   Supports keyboard navigation (left/right arrows, Z to confirm) handled within `CharacterSelector.cs`. It uses the `EventSystem` to manage selection and plays audio feedback on selection changes. It also handles regaining focus when the game window is clicked or tabbed back into.
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
*   **`SpellBarController.cs`:** Manages the visual fill for one player's spell bar, driven by NetworkVariables. Must have its `TargetPlayerRole` set in the Inspector.
*   **`MatchmakerUI.cs`:** Handles UI interactions for joining the matchmaking queue.
*   **`CharacterSelector.cs`:** Handles UI for selecting characters, displaying synopsis panels, and managing keyboard navigation in the Character Select scene.
*   **`SynopsisPanelController.cs`:** Controls the UI elements within a single character synopsis panel, populating them with data from a `CharacterSynopsisData` object.
*   **`ClientConnectorDisconnector.cs`:** Handles UI button for connecting/disconnecting the client.
*   **`MainMenuNavigator.cs`:** Handles keyboard navigation specifically for the Main Menu scene.
*   **(Hypothetical) `UIManager.cs`:** A central manager might exist to coordinate showing/hiding different UI panels or managing overall UI state, though not found in the initial search.
*   **(Hypothetical) `RoundCounterUI.cs`:** A script likely exists to display round wins based on networked game state data. 