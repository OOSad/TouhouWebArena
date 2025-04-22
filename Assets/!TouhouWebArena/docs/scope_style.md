# Scope Style Mechanic Documentation

## Overview
_(Explain the purpose of the Scope Style: activated during Focus mode, used to "activate" idle spirits)._

## Implementation
*   **Activation:** _(Explain that it's tied to Focus mode, managed by `PlayerFocusController.cs`)._
*   **Zone of Influence:** _(Describe how the area is defined, e.g., a child GameObject with a Collider2D set to be a Trigger)._
*   **Visuals:** _(Mention the character-specific visual effect, handled by scripts inheriting from `BaseScopeStyleController.cs`)._

## Spirit Interaction
*   **Detection:** _(Explain how the Scope Style detects spirits, likely via `OnTriggerEnter2D` checking for objects with the `SpiritController` script or a specific tag/layer)._
*   **Activation Signal:** _(Describe how the Scope Style signals the Spirit to activate, likely by calling a method on the detected `SpiritController.cs`)._
*   **Effect on Spirit:** _(Reiterate that activated spirits become visually distinct and potentially easier to destroy, as handled within `SpiritController.cs`)._

## Key Scripts
*   **`PlayerFocusController.cs`:** _(Activates/deactivates the Scope Style based on Focus input, likely enables/disables the Scope Style GameObject/Collider)._
*   **`BaseScopeStyleController.cs` (and derived classes):** _(Handle the visual appearance and potentially the trigger logic for the Scope Style area)._
*   **`SpiritController.cs`:** _(Contains the logic to handle being "activated" when interacted with by the Scope Style)._ 