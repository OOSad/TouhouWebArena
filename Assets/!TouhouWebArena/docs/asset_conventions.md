# Asset & Code Conventions Documentation

## Overview

Following consistent conventions for naming, folder structure, and code style helps keep the project organized, understandable, and easier to maintain, especially as it grows in complexity.

## Folder Structure

The project aims to follow a structured organization within the `Assets/!TouhouWebArena/` directory:

*   **`/Scripts`:** Contains all C# scripts.
    *   `/Characters`: Player-specific scripts (`PlayerMovement`, `PlayerShooting`, `CharacterStats`, `PlayerFocusController`, etc.).
    *   `/Enemies`: Enemy-specific scripts (`Fairy`, `SpiritController`).
    *   `/Gameplay`: Core gameplay mechanics (`FairySpawner`, `SpiritSpawner`, `ExtraAttackManager`, `PathManager`, Registries).
    *   `/Managers`: High-level manager scripts (`PlayerDataManager`, `GameInitializer`).
    *   `/Networking`: Networking-specific utilities (`NetworkObjectPool`, `Matchmaker`, connection helpers).
    *   `/Projectiles`: Scripts related to bullet/projectile behavior (excluding spellcard logic).
    *   `/Spellcards`: Scripts specifically for the spellcard system (`SpellcardData`, `SpellcardExecutor`, `/Behaviors`).
    *   `/UI`: Scripts controlling UI elements (`PlayerHealthUI`, `SpellBarController`, menu scripts).
    *   `/Utilities`: General helper scripts (`SplineWalker`, `PoolableObjectIdentity`).
    *   *(Add other subfolders as needed, e.g., `/Audio`, `/Effects`)*
*   **`/Art`:** Contains visual assets.
    *   `/Animations`: Animation clips and controllers.
    *   `/Sprites`: Source images, sprite sheets.
    *   `/Materials`: Material assets.
    *   `/Fonts`: Font files.
    *   *(Add other subfolders as needed, e.g., `/Effects`, `/Shaders`)*
*   **`/Prefabs`:** Contains all GameObject prefabs.
    *   `/Characters`: Player character prefabs (`ReimuPlayerPrefab`).
    *   `/Enemies`: Enemy prefabs (`NormalFairy`, `GreatFairy`, `Spirit`).
    *   `/Projectiles`: Bullet and other projectile prefabs (`ReimuBulletPrefab`, charge attacks, spellcard bullets).
    *   `/System`: Spawner and manager object prefabs (`PlayerOneFairySpawner`, potentially `GameManager` if prefabbed).
    *   `/UI`: UI element prefabs (health icons, etc.).
    *   `/Effects`: Visual effect prefabs.
*   **`/Resources`:** Holds assets loaded dynamically at runtime via `Resources.Load()`.
    *   `/Spellcards`: Contains only `SpellcardData` `.asset` files, named according to convention.
    *   *(Avoid placing other assets here unless they absolutely need dynamic loading by path).* 
*   **`/Scenes`:** Contains all Unity scene files (`MainMenu`, `CharacterSelect`, `Gameplay`).
*   **`/PhysicsMaterials`:** Contains Physics Material assets.
*   **`/Audio`:** Contains sound effect and music files.

## Naming Conventions

*   **Scripts:** `PascalCase` (e.g., `PlayerShooting`, `NetworkObjectPool`). Use descriptive names. Consider suffixes like `Controller`, `Manager`, `Data`, `Spawner`, `Registry`, `UI`, `Helper` where appropriate.
*   **Prefabs:** `PascalCase`. Often mirrors the main script name (e.g., `Spirit` prefab uses `SpiritController.cs`). Consider suffixes like `Prefab` if needed for clarity, or type-specific (e.g. `ReimuBullet`).
*   **ScriptableObjects:**
    *   `SpellcardData` assets: **Strictly** follow `CharacterNameLevelXSpellcard` (e.g., `ReimuLevel2Spellcard`).
    *   Other SOs: `PascalCase`, often ending in `Data` or `Asset` (e.g., `EnemyWaveData`).
*   **Scenes:** `PascalCase` (e.g., `MainMenu`, `GameplayScene`).
*   **Folders:** `PascalCase`.
*   **Methods:** `PascalCase` (e.g., `OnNetworkSpawn`, `ReturnNetworkObject`). Use verb-based names where possible (e.g., `CalculateMovement`, `SpawnRevengeSpirit`).
*   **Variables (Fields & Local):** `camelCase` (e.g., `passiveFillRate`, `nextFireTime`, `networkObjectInstance`). Use descriptive names. Avoid abbreviations. `[SerializeField]` should be used for private fields that need Inspector access.
*   **Properties:** `PascalCase` (e.g., `IsFocused`, `CurrentBounds`).
*   **Enums:** `PascalCase` for the type name, `PascalCase` for values (e.g., `FormationType.Circle`).
*   **Constants (`const`, `static readonly`):** `PascalCase` (e.g., `MaxFillAmount`).

## Resource Management

*   **`Resources` Folder:** Use **sparingly**. Only place assets here that *must* be loaded dynamically by string path using `Resources.Load()`. Currently, this applies only to `SpellcardData` assets due to the loading mechanism in `PlayerShooting.cs`. Avoid putting prefabs, textures, etc., here; assign those via Inspector references instead.
*   **Asset Bundles (Future):** Not currently used, but maybe considered later for web builds or large projects to manage loading and updates more efficiently.

## Code Style

*   **Bracing:** Use Allman style (braces on new lines) or K&R style (opening brace on the same line) consistently. (Choose one and stick to it).
*   **Readability:** 
    *   Prioritize clear, descriptive, and verbose names for variables, methods, classes, etc., to make the code self-documenting.
    *   Use whitespace (blank lines) to separate logical blocks of code within methods.
    *   Keep methods relatively short and focused on a single responsibility where possible.
*   **Comments:**
    *   Use `///` XML documentation comments for public classes, methods, properties, and fields to explain *what* they do, their parameters, and return values, especially if the purpose isn't immediately obvious from the name. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments]
    *   Use `//` single-line comments only to explain *why* a particular piece of complex or non-obvious logic exists, not *what* the code is doing (the code itself should explain the *what*). Avoid commenting obvious code.
*   **Namespaces:** Organize scripts into namespaces (e.g., `TouhouWebArena.Players`, `TouhouWebArena.Spellcards`) to prevent naming conflicts and improve organization, especially as the project grows. 