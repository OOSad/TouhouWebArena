# Input Handling Documentation

## Overview

Player input controls character movement, shooting, charging spellcards, and activating focus mode. Input is read locally on the client for immediate feedback (especially movement and focus speed) and relevant state/actions are communicated to the server for authoritative processing or synchronized for visual consistency.

## Input System

The game currently uses Unity's legacy **Input Manager**. Input is read via `Input.GetAxisRaw()` for movement axes and `Input.GetKey/Down/Up()` for button presses, using `KeyCode` values.

## Action Mapping

_(List the core player actions and their default bindings):_
*   **Movement:**
    *   Reads `"Horizontal"` and `"Vertical"` axes (Default: Arrow Keys).
*   **Shoot / Charge:**
    *   Reads `KeyCode.Z` (Configurable via `fireKey` field in `PlayerShooting` component).
    *   `KeyDown`: Initiates basic shot burst.
    *   `GetKey`: Used for charging the active spellbar.
    *   `KeyUp`: Triggers Charge Attack (Level 1) or Spellcard (Level 2+) based on active charge.
*   **Focus:**
    *   Reads `KeyCode.LeftShift` (Handled in `PlayerFocusController.cs`).
    *   Activates focus mode (slower speed, visible hitbox, activates Scope Style).

## Client-Side Processing

*   **Movement:** Input axes are read directly in `PlayerMovement.cs`. The resulting movement vector is calculated (using the current focus state for speed), clamped to bounds, and applied *directly* to the local player's `transform.position` each frame. This implements **client-side prediction** for smooth movement feedback. The `NetworkTransform` component is disabled for the local owner to allow this local control.
*   **Focus State:** `PlayerFocusController.cs` reads the Left Shift key. It immediately sets the `IsFocused` property on the local `PlayerMovement.cs` component, allowing movement speed to change instantly.
*   **Charge Level Check:** When the `fireKey` is released, `PlayerShooting.cs` locally reads the `currentActiveFill` value from the player's assigned `SpellBarController` to determine whether to request a Charge Attack or a Spellcard from the server.

## Server Communication & State Sync

*   **Movement Input:** Currently, raw movement input (`Horizontal`/`Vertical` axes) does **not** appear to be sent to the server explicitly via RPC. Movement relies on client-side prediction with state synchronization likely handled primarily by the `NetworkTransform` component (which is disabled for the owner, implying state might only sync *from* the server if reconciliation were added, or rely on `PlayerPositionSynchronizer`). *Further review might be needed if precise server-side movement validation becomes critical.*
*   **Shooting (Basic):** Each individual shot in a burst triggers a `RequestFireServerRpc` call from `PlayerShooting.cs` to the server.
*   **Charge State:** The state of holding the `fireKey` (`isHoldingChargeKey`) is sent every frame from the client owner to the server via `UpdateChargeStateServerRpc` in `PlayerShooting.cs`. The server uses this to update the charge `NetworkVariable`.
*   **Charge Attack/Spellcard Activation:** When the `fireKey` is released with sufficient charge, the client calls `PerformChargeAttackServerRpc` or `RequestSpellcardServerRpc` in `PlayerShooting.cs` to ask the server to execute the corresponding action.
*   **Focus State:** The owner client writes the current focus state (`true`/`false`) to the `NetworkVariable<bool> NetworkedIsFocusing` in `PlayerFocusController.cs`. This variable automatically synchronizes the state to the server and all other clients, primarily ensuring consistent visuals (hitbox, scope style) across all instances.

## Key Scripts

*   **`PlayerMovement.cs`:** Reads movement axes, applies local movement (client-side prediction). Uses `IsFocused` state.
*   **`PlayerShooting.cs`:** Reads `fireKey`, sends RPCs for shooting, charge state, and charge/spellcard activation. Reads local spellbar state.
*   **`PlayerFocusController.cs`:** Reads `LeftShift`, updates local `PlayerMovement.IsFocused` state, and updates the `NetworkedIsFocusing` NetworkVariable for visual synchronization. 