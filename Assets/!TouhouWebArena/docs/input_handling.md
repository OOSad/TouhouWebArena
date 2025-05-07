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

*   **Movement:** Input axes (`"Horizontal"`, `"Vertical"`) are read by the owning client's `ClientAuthMovement.cs` in `Update()`. In `FixedUpdate()`, the script calculates the movement vector (using the current focus state from `PlayerFocusController` for speed from `CharacterStats`), clamps it to gameplay bounds, and applies it directly to the local player's `Rigidbody2D` via `rb.MovePosition()`. This provides immediate client-side responsiveness.
*   **Focus State:** `PlayerFocusController.cs` reads the `KeyCode.LeftShift`. It updates a `NetworkVariable<bool> NetworkedIsFocusing` which synchronizes the focus state. The owning client's `ClientAuthMovement.cs` reads `playerFocusController.IsFocusingNetworked` to adjust movement speed.
*   **Charge Level Check:** When the `fireKey` is released, `PlayerShooting.cs` locally reads the `currentActiveFill` value from the player's assigned `SpellBarController` to determine whether to request a Charge Attack or a Spellcard from the server.

## Server Communication & State Sync

*   **Movement State:**
    *   The owning client's `ClientAuthMovement.cs`, after applying local movement in `FixedUpdate()`, updates a `NetworkVariable<Vector2> NetworkedPosition` with its new `rb.position`.
    *   This `NetworkVariable` (with owner write permission) automatically sends the updated position to the server, which then relays it to all other clients.
    *   Remote clients read `NetworkedPosition` in their `Update()` loop (via their instance of `ClientAuthMovement.cs`) and currently set their local character's `rb.position` directly to this value.
    *   The `NetworkTransform` component is not used for synchronizing player position.
*   **Shooting (Basic):** Each individual shot in a burst triggers a `RequestFireServerRpc` call from `PlayerShooting.cs` to the server.
*   **Charge State:** The state of holding the `fireKey` (`isHoldingChargeKey`) is sent every frame from the client owner to the server via `UpdateChargeStateServerRpc` in `PlayerShooting.cs`. The server uses this to update the charge `NetworkVariable`.
*   **Charge Attack/Spellcard Activation:** When the `fireKey` is released with sufficient charge, the client calls `PerformChargeAttackServerRpc` or `RequestSpellcardServerRpc` in `PlayerShooting.cs` to ask the server to execute the corresponding action.
*   **Focus State:** The owner client writes the current focus state (`true`/`false`) to the `NetworkVariable<bool> NetworkedIsFocusing` in `PlayerFocusController.cs`. This variable automatically synchronizes the state to the server and all other clients, primarily ensuring consistent visuals (hitbox, scope style) across all instances.

## Key Scripts

*   **`ClientAuthMovement.cs`:** (Replaces `PlayerMovement.cs`) Reads movement axes and focus state (from `PlayerFocusController`) on the owning client. Applies movement directly to its `Rigidbody2D`. Synchronizes its position to other clients via a `NetworkVariable<Vector2> NetworkedPosition`. Non-owning instances use this `NetworkVariable` to update their representation.
*   **`PlayerShooting.cs`:** Reads `fireKey`, sends RPCs for shooting, charge state, and charge/spellcard activation. Reads local spellbar state.
*   **`PlayerFocusController.cs`:** Reads `LeftShift` key on the owning client. Manages a `NetworkVariable<bool> NetworkedIsFocusing` to synchronize focus state. Visuals (hitbox, scope style) are updated based on this `NetworkVariable`. `ClientAuthMovement` reads the focus state from this script to modify speed. 