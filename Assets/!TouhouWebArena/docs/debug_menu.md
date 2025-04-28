# Debug Menu Documentation

## Overview

This document describes the in-game debug menu, designed to facilitate testing, debugging, and balancing. 

**IMPORTANT:** The debug menu is **server-only**. It will only appear and function when the game instance is running as the server.

## Activation

Press the **F11** key while the game is running as the server to toggle the visibility of the debug menu panel.

The menu UI and its controller logic are primarily managed by `Assets/!TouhouWebArena/Scripts/Debug/DebugMenuController.cs`.

## Available Options

The following options are available on the debug menu:

*   **Toggle Player Hitboxes (P1/P2):**
    *   **UI:** Checkboxes for Player 1 and Player 2.
    *   **Functionality:** Enables or disables the child GameObject named "Hitbox" on the corresponding player prefab. This effectively makes the player invulnerable (or vulnerable again) by removing their collision-receiving component, without affecting the hitbox visual.

*   **Insta-kill Player (P1/P2):**
    *   **UI:** Buttons for Player 1 and Player 2.
    *   **Functionality:** Finds the target player's `PlayerHealth` component and calls its `TakeDamage()` method with the player's current health value, effectively setting health to 0 and triggering death logic.

*   **Lock Player HP (P1/P2):**
    *   **UI:** Checkboxes for Player 1 and Player 2 (default: off).
    *   **Functionality:** Sets a server-side flag (`isHpLocked`) within the target player's `PlayerHealth` component. When locked, the `TakeDamage` method still allows hit detection and triggers the invincibility/deathbomb response, but it skips the step where `CurrentHealth.Value` is modified.

*   **Set Player HP (P1/P2):**
    *   **UI:** Integer input fields for Player 1 and Player 2.
    *   **Functionality:** Finds the target player's `PlayerHealth` component and calls `SetHealthDirectlyServer()`, which directly sets the `CurrentHealth.Value` NetworkVariable, bypassing invincibility and HP lock checks. The value is clamped between 0 and the character's configured starting health (`CharacterStats.GetStartingHealth()`). Note: The UI display might be limited (e.g., to 5 orbs) even if the underlying value is set higher.

*   **Give Max Spell Bars:**
    *   **UI:** Single button.
    *   **Functionality:** Calls `SpellBarManager.Instance.SetPlayerChargeToMaxServer()` for both `PlayerRole.Player1` and `PlayerRole.Player2`, instantly setting their spell bar charge NetworkVariables to the maximum value.

*   **Toggle Fairy Spawns:**
    *   **UI:** Single toggle (default: on).
    *   **Functionality:** Sets the `isDebugSpawningEnabled` flag on all active `FairySpawner` instances found in the scene via their `SetSpawningEnabledServer()` method. This pauses/resumes the spawning coroutine within each spawner.

*   **Toggle Spirit Spawns:**
    *   **UI:** Single toggle (default: on).
    *   **Functionality:** Sets the `isDebugSpawningEnabled` flag on the `SpiritSpawner` instance via `SetSpawningEnabledServer()`. Additionally, when toggled *off*, it finds all active `SpiritController` instances and returns their NetworkObjects to the `NetworkObjectPool`, effectively clearing existing spirits from the field.

*   **Toggle AI (P1/P2):**
    *   **UI:** Checkboxes for Player 1 and Player 2 (default: off).
    *   **Functionality:** Finds the target player's `PlayerAIController` component and calls `SetAIEnabledServer()`, which modifies the `IsAIDebugEnabled` NetworkVariable. The owner client reads this variable to enable/disable the AI's control logic. 