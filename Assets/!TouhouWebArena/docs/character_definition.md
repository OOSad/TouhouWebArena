``# Character Definition Documentation

## Overview

Playable characters in Touhou Web Arena are defined primarily through **Prefabs**. Each character (e.g., Reimu, Marisa) has its own Player Prefab, which contains all the necessary components and configuration settings. Character-specific stats and attack references are configured directly on components attached to this prefab, primarily the `CharacterStats` script.

## Key Components

*   **Player Prefab:**
    *   Located in: `Assets/!TouhouWebArena/Prefabs/Characters/` (e.g., `ReimuPlayerPrefab`, `MarisaPlayerPrefab`).
    *   Structure: This prefab includes the visual representation, collider, `NetworkObject`, and all necessary scripts.
*   **Core Scripts (Attached to Player Prefab):**
    *   **`CharacterStats.cs`:** Holds most character-specific data (name, base stats, projectile/attack prefabs, movement speed, health, bomb radius, etc.). See details below.
    *   **`PlayerShooting.cs`:** Manages shooting input, charge bar logic, and initiates basic shots, charge attacks, and spellcards (by loading the appropriate `SpellcardData` based on `characterName` from `CharacterStats`).
    *   **`PlayerMovement.cs` (or similar):** Handles movement input and potentially client-side prediction logic. Uses speed values from `CharacterStats`.
    *   *(Other essential player scripts like Health management, Collision handling, etc.)*
*   **Data Assets (Associated via Scripts/Convention):**
    *   **Spellcards (`SpellcardData` assets):** Located in `Assets/Resources/Spellcards/`. Linked *implicitly* via the naming convention `CharacterNameLevelXSpellcard`, using the `characterName` field from `CharacterStats.cs`. (See `docs/spellcard_system.md` for details).
    *   **Charge Attack Prefab:** Directly assigned to the `chargeAttackPrefab` field within the `CharacterStats` component on the Player Prefab.
    *   **Basic Shot Prefab:** Directly assigned to the `bulletPrefab` field within the `CharacterStats` component on the Player Prefab.

## Defining a New Character

Adding a new playable character involves these main steps:

1.  **Create Player Prefab:** Duplicate an existing character's prefab (e.g., `ReimuPlayerPrefab`) and rename it for the new character (e.g., `SakuyaPlayerPrefab`).
2.  **Configure `CharacterStats`:** Select the new prefab and modify the `CharacterStats` component in the Inspector:
    *   Set the `Character Name` field to the new character's unique identifier (e.g., "Sakuya"). **This name is critical** as it's used to load the correct spellcards.
    *   **Important Naming Convention:** When referencing characters in code (e.g., in `switch` statements, dictionary keys, or for loading resources), always use the exact `enum` name defined in `PlayerData.cs` (e.g., `HakureiReimu`, `KirisameMarisa`). Do **not** use names with spaces (like "Hakurei Reimu"). Use `PlayerData.SelectedCharacter.ToString()` to get the correct format reliably. This convention was crucial in fixing bugs related to Extra Attack triggering.
    *   Assign the correct `Bullet Prefab` for their basic shot.
    *   Assign the correct `Charge Attack Prefab` for their Level 1 charge attack.
    *   Adjust `Passive Fill Rate`, `Active Charge Rate`.
    *   Configure `Shooting Settings` (spread, burst, cooldowns).
    *   Configure `Movement Settings` (move speed, focus modifier).
    *   Configure `Health & Defense Settings`.
    *   Configure `Bomb Settings`.
3.  **Create Spellcard Assets:** Create the `SpellcardData` `.asset` files for the new character's Level 2, 3, and 4 spellcards in the `Assets/Resources/Spellcards/` folder, ensuring they follow the strict naming convention: `SakuyaLevel2Spellcard.asset`, `SakuyaLevel3Spellcard.asset`, `SakuyaLevel4Spellcard.asset`. Configure the actions and elements within each asset. (See `docs/spellcard_system.md`).
4.  **Create Specific Prefabs:** Ensure the prefabs for the new character's basic shot and charge attack exist and are assigned correctly in `CharacterStats`.
5.  **Character Selection:** Update the character selection UI/logic to include the new character option and ensure it spawns the correct Player Prefab.
6.  **Testing:** Thoroughly test the new character's movement, basic shot, charge attack, and all spellcard levels. 