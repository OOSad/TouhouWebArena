# Spellcard System Documentation

In Touhou games, characters can use Spellcards. These are unique patterns of bullets, usually created by some kind of emitter. Not only could the emitter have custom instructions - spawn these lines of bullets around this direction, or spawn these circles of bullets around this spot - but the bullets themselves usually also have custom behaviors attached to them, like go forward, go left, go right, home in, spin in circles, etc. These complex patterns of bullets originating from relatively simple behaviors demand a data-driven solution, using scriptable objects for the game, as the labor of creating even a simple spellcard would quickly become overwhelming and impossible to describe.

## Overview

Spellcards are defined in .asset files. Inside these asset files, in the inspector, players can add Actions. These could be seen as the emitters that launch the patterns of bullets. Inside of the Actions, players can create multiple Elements. Each element is a unique pattern of bullets, that can take different bullet prefabs, and can be customized with different behaviors like formation type, bullet count, speed and individual bullet behavior like linear movement or homing behavior. You can also define whether a bullet is clearable by deathbomb effects or shockwave effects that occur on fairy/activated spirit deaths.

## Key Components

*   **`SpellcardData` (ScriptableObject):**
    *   Defined in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/SpellcardData.cs`
    *   Purpose: Acts as the data container for a single spellcard. These are created as `.asset` files in the Unity project (likely within the `Resources/Spellcards` folder or similar). Each asset defines the name, level, actions, and elements of one spellcard.

*   **`PlayerShooting.cs` (Script):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Characters/`
    *   Purpose: Attached to the player character prefab. It manages the player's shooting state, including charging the spellbar. When a spellcard charge level is reached and the player releases the button, this script:
        1.  Determines which `SpellcardData` asset to use based on character and charge level.
        2.  Loads the `SpellcardData` asset (likely from the `Resources` folder).
        3.  Tells the server to execute the spellcard, targeting the opponent.
        4.  The server-side logic within this script likely calls the `SpellcardExecutor` on the opponent's client via RPCs.

*   **`SpellcardExecutor.cs` (Script):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/`
    *   Purpose: This script is responsible for the actual execution of a spellcard pattern on the client-side (specifically, the opponent's screen). It receives a `SpellcardData` asset (from the server via `PlayerShooting.cs`) and iterates through its `Actions` and `Elements` to spawn and configure the bullets according to the data defined in the asset.

*   **Bullet Prefabs:**
    *   Located in: (Likely `Assets/!TouhouWebArena/Prefabs/Projectiles/` or similar)
    *   Purpose: Standard Unity prefabs representing the different visual types of bullets used in spellcards. These are referenced within the `SpellcardData` assets.

*   **Networking:**
    *   Spellcard activation is server-authoritative, initiated within `PlayerShooting.cs` on the server.
    *   The server likely uses an RPC (Remote Procedure Call) to tell the specific opponent's client to run the `SpellcardExecutor.cs` script with the appropriate `SpellcardData`.

## Adding a New Spellcard

Follow these steps to create and integrate a new spellcard into the game:

1.  **Create the Spellcard Asset:**
    *   In the Unity Project window, navigate to the `Assets/Resources/Spellcards/` folder. (Create the `Spellcards` folder inside `Resources` if it doesn't exist).
    *   Right-click within the `Spellcards` folder.
    *   Go to `Create` -> `TouhouWebArena` -> `Spellcard Data`.
    *   A new `.asset` file will be created.

2.  **Rename the Asset:**
    *   **Crucially**, rename the newly created asset using the exact naming convention: `CharacterNameLevelXSpellcard`.
    *   Replace `CharacterName` with the character who will use this spellcard (e.g., `Reimu`, `Marisa`). Make sure this matches the character's identifier used internally (check `CharacterStats` or existing assets if unsure).
    *   Replace `X` with the required charge level (e.g., `2`, `3`, or `4`).
    *   **Example:** For Reimu's Level 3 spellcard, the file must be named `ReimuLevel3Spellcard.asset`.

3.  **Configure the Spellcard Data:**
    *   Select the renamed `.asset` file.
    *   In the Inspector window, configure the spellcard's properties:
        *   Set the **`Required Charge Level`** to match the level in the filename (e.g., `3` for a Level 3 spellcard).
        *   Add one or more **`Actions`** to the list.
        *   For each `Action`, configure its **Timing**, **Spawning** (including `Bullet Prefabs`, `Formation`, `Count`, etc.), **Behavior** (`Linear`, `Homing`, etc.), and **Interaction** (`Is Clearable By Bomb`) properties as detailed in the `Data Structure` section above.

4.  **Integration (Automatic):**
    *   Thanks to the naming convention and the `Resources.Load` mechanism in `PlayerShooting.cs`, no further steps are needed to *link* the spellcard to the character. As long as the asset is correctly named and placed in `Assets/Resources/Spellcards/`, the `PlayerShooting.cs` script will automatically find and load it when the player activates that specific spell level.

5.  **Test:**
    *   Play the game, select the correct character, charge the spellbar to the appropriate level, and activate the spellcard to ensure it functions as designed. Debug any issues by reviewing the `SpellcardData` configuration in the Inspector.

## Data Structure

The core data for spellcards is defined in the `SpellcardData` ScriptableObject (`.asset` files). Here are the main fields you configure in the Inspector:

**Top-Level Fields (on the `SpellcardData` asset itself):**

*   **`Required Charge Level` (Integer):**
    *   Specifies the charge level (e.g., 2 for 50%, 3 for 75%, 4 for 100%) needed to activate this specific spellcard.
*   **`Actions` (List of `SpellcardAction`):**
    *   This is the main part where you define the sequence of bullet patterns for the spellcard. You can add multiple "Actions" to this list, and each one will execute in order (or based on their `Start Delay`).

**Fields within each `SpellcardAction` (in the `Actions` list):**

Each `SpellcardAction` represents a specific "burst" or "wave" of bullets within the spellcard.

*   **Timing:**
    *   **`Start Delay` (Decimal):** How many seconds to wait *after* the spellcard begins before this specific Action starts. A delay of 0 means it starts immediately.
*   **Spawning:**
    *   **`Bullet Prefabs` (List of GameObjects):** Drag the bullet prefab(s) you want this Action to spawn here. If you add multiple prefabs, the system will cycle through them when spawning bullets in a formation (e.g., for alternating colors in a circle).
    *   **`Formation` (Dropdown Menu - `FormationType`):** Choose how the bullets are initially arranged:
        *   `Point`: Spawns all bullets at a single point (defined by `Position Offset`). `Count` determines how many overlap.
        *   `Circle`: Arranges `Count` bullets in a circle with a specific `Radius`.
        *   `Line`: Arranges `Count` bullets in a straight line with `Spacing` between them, oriented at a specific `Angle`.
    *   **`Position Offset` (X, Y Coordinates):** Where the formation is centered relative to the spawn point (usually the opponent player's position).
    *   **`Count` (Integer):** How many bullets to spawn in this Action (used by `Circle` and `Line` formations).
    *   **`Radius` (Decimal):** Size of the circle (only used if `Formation` is `Circle`).
    *   **`Spacing` (Decimal):** Distance between bullets (only used if `Formation` is `Line`).
    *   **`Angle` (Decimal - Degrees):** Orientation of the line relative to the spawner's forward direction (only used if `Formation` is `Line`). 0 is forward.
*   **Behavior:**
    *   **`Behavior` (Dropdown Menu - `BehaviorType`):** Choose how the spawned bullets move:
        *   `Linear`: Move in a straight line based on the formation's initial angle.
        *   `Homing`: Start homing towards the player immediately (uses `Homing Speed`).
        *   `Delayed Homing`: Move linearly first for `Homing Delay` seconds, then start homing (uses `Speed` initially, then `Homing Speed`).
    *   **`Speed` (Decimal):** The initial speed for `Linear` movement or the speed before homing starts for `Delayed Homing`.
    *   **`Homing Speed` (Decimal):** The speed bullets move at when actively homing.
    *   **`Homing Delay` (Decimal):** How long to wait before homing begins (only used for `Delayed Homing`).
*   **Interaction:**
    *   **`Is Clearable By Bomb` (Checkbox):** Check this if these bullets should be destroyed by the player's deathbomb effect. 