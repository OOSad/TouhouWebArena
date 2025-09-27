# Unity Project Structure Guide

This document outlines the standard folder structure for the Touhou Web Arena Unity project. Adhering to this structure will ensure the project remains organized, scalable, and easy to navigate for both human and AI developers.

## Root `Assets` Folder Structure

The `Assets` folder is the root directory for all game assets. The proposed top-level structure is as follows:

```
Assets/
|-- _Project/
|   |-- Scenes/
|   |-- Scripts/
|   |   |-- Player/
|   |   |-- Bullets/
|   |   |-- Managers/
|   |   |-- UI/
|   |   |-- Networking/
|   |-- Prefabs/
|   |   |-- Characters/
|   |   |-- Bullets/
|   |   |-- Effects/
|   |-- ScriptableObjects/
|-- Art/
|   |-- Sprites/
|   |-- Animations/
|   |-- Fonts/
|-- Audio/
|   |-- Music/
|   |-- SFX/
|-- ThirdParty/
```

## Folder Descriptions

### `_Project/`

This is the primary folder for all project-specific, created assets. The underscore prefix ensures it stays at the top of the `Assets` directory for easy access.

*   **`Scenes/`**: Contains all Unity scenes.
    *   `MainMenu.unity`: The main menu and entry point of the application.
    *   `CharacterSelect.unity`: The character selection screen.
    *   `Stage.unity`: The main gameplay scene.

*   **`Scripts/`**: Contains all C# scripts.
    *   **`Player/`**: Scripts related to player character control, stats, and abilities.
    *   **`Bullets/`**: Scripts for projectile behavior, types, and patterns.
    *   **`Managers/`**: Core singleton managers (e.g., `GameManager`, `ObjectPoolManager`, `UIManager`).
    *   **`UI/`**: Scripts for UI elements and interactions (e.g., `DebugOverlay`, `HealthBar`).
    *   **`Networking/`**: Scripts for WebSocket communication with the server.

*   **`Prefabs/`**: Contains all prefabs (pre-configured GameObjects).
    *   **`Characters/`**: Prefabs for the player and enemy characters.
    *   **`Bullets/`**: Prefabs for different types of projectiles.
    *   **`Effects/`**: Prefabs for visual effects (e.g., explosions, impacts).

*   **`ScriptableObjects/`**: Contains ScriptableObject assets for storing data, such as character stats, weapon configurations, or level data.

### `Art/`

Contains all visual assets.

*   **`Sprites/`**: 2D images and spritesheets.
*   **`Animations/`**: Animation clips and animator controllers.
*   **`Fonts/`**: Font files for UI text.

### `Audio/`

Contains all audio assets.

*   **`Music/`**: Background music tracks.
*   **`SFX/`**: Sound effects for actions and events.

### `ThirdParty/`

For assets and packages imported from the Unity Asset Store or other third-party sources. This keeps external code and assets isolated from the project's own files.