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
    *   **Character Synopsis (`CharacterSynopsisData` asset):** Located typically in `Assets/!TouhouWebArena/Resources/CharacterSynopses/` (or similar folder). This `ScriptableObject` holds UI-specific display information (name, title, illustration, stats text, attack descriptions) for the Character Select screen. It is linked to the character via its `internalName` field, which **must match** the `characterName` field in `CharacterStats`.

## Defining a New Character

Adding a new playable character involves these main steps:

1.  **Create Player Prefab:** Duplicate an existing character's prefab (e.g., `ReimuPlayerPrefab`) and rename it for the new character (e.g., `SakuyaPlayerPrefab`).
2.  **Configure `CharacterStats`:** Select the new prefab and modify the `CharacterStats` component in the Inspector:
    *   Set the `Character Name` field to the new character's unique identifier (e.g., "Sakuya"). **This name is critical** as it's used to load the correct spellcards and link to the synopsis data.
    *   Assign the correct `Bullet Prefab` for their basic shot.
    *   Assign the correct `Charge Attack Prefab` for their Level 1 charge attack.
    *   Adjust `Passive Fill Rate`, `Active Charge Rate`.
    *   Configure `Shooting Settings` (spread, burst, cooldowns).
    *   Configure `Movement Settings` (move speed, focus modifier).
    *   Configure `Health & Defense Settings`.
    *   Configure `Bomb Settings`.
3.  **Create Synopsis Data Asset:**
    *   Create a new `CharacterSynopsisData` asset (via `Assets > Create > TouhouWebArena > Character Synopsis Data`).
    *   Rename the asset appropriately (e.g., `SakuyaSynopsis.asset`).
    *   Configure the fields within the Inspector:
        *   Set the `Internal Name` to **exactly match** the `Character Name` set in `CharacterStats` (e.g., "Sakuya").
        *   Fill in the `Display Name`, `Character Title`.
        *   Assign the `Character Illustration` sprite.
        *   Fill in the stats display text fields (`Normal Speed Stat`, `Charge Speed Stat`, etc.).
        *   Fill in the attack information (`Extra Attack Name`, `Extra Attack Icon`, `Extra Attack Description`, etc.).
    *   Place the asset in a designated folder, e.g., `Assets/!TouhouWebArena/Resources/CharacterSynopses/`.
4.  **Create Spellcard Assets:** Create the `SpellcardData` `.asset` files for the new character's Level 2, 3, and 4 spellcards in the `Assets/Resources/Spellcards/` folder, ensuring they follow the strict naming convention: `SakuyaLevel2Spellcard.asset`, `SakuyaLevel3Spellcard.asset`, `SakuyaLevel4Spellcard.asset`. Configure the actions and elements within each asset. (See `docs/spellcard_system.md`).
5.  **Create Specific Prefabs:** Ensure the prefabs for the new character's basic shot and charge attack exist and are assigned correctly in `CharacterStats`.
6.  **Character Selection UI:**
    *   Update the character selection UI/logic (`CharacterSelector.cs`) to include the new character option.
    *   Ensure the new `CharacterSynopsisData` asset is added to the `All Synopsis Data` list on the `CharacterSelector` component in the Character Select scene.
    *   Make sure the `characterInternalName` in the `Character Button Mapping` for the new character's button matches the `internalName` in the synopsis asset and the `characterName` in `CharacterStats`.
7.  **Testing:** Thoroughly test the new character's movement, basic shot, charge attack, all spellcard levels, and verify their information displays correctly on the Character Select screen. 