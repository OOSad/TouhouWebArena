# Spellcard System Documentation

In Touhou games, characters can use Spellcards. These are unique patterns of bullets, usually created by some kind of emitter. Not only could the emitter have custom instructions - spawn these lines of bullets around this direction, or spawn these circles of bullets around this spot - but the bullets themselves usually also have custom behaviors attached to them, like go forward, go left, go right, home in, spin in circles, etc. These complex patterns of bullets originating from relatively simple behaviors demand a data-driven solution, using scriptable objects for the game, as the labor of creating even a simple spellcard would quickly become overwhelming and impossible to describe.

## Overview

Spellcards are defined using `SpellcardData` ScriptableObjects (`.asset` files). Inside these asset files, in the inspector, developers can define a sequence of `SpellcardAction`s. Each action can spawn multiple bullets with specific prefabs, arrangements (formations), and movement behaviors (like linear or homing). Spellcards are typically activated when a player releases the fire key with sufficient charge (Level 2, 3, or 4).

## Key Components

*   **`SpellcardData` (ScriptableObject):**
    *   Defined in: `Assets/!TouhouWebArena/Scripts/Spellcards/Data/SpellcardData.cs`
    *   Purpose: The data container for a single spellcard pattern. Created as `.asset` files in the Unity project (typically `Assets/Resources/Spellcards/`). Each asset defines the name, level, and sequence of `SpellcardAction`s for one spellcard.

*   **`PlayerShootingController.cs` (Script):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Characters/`
    *   Purpose: Attached to the player character prefab. On the **owning client**, it monitors input and the local `SpellBarController` state. When a spellcard level (2, 3, or 4) is reached upon key release, it sends a `RequestSpellcardServerRpc` to the server, indicating the desired `spellLevel`.

*   **`SpellBarManager.cs` (Script - Server-Side Service):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose: A server-authoritative singleton. When it receives the `RequestSpellcardServerRpc` forwarded from the client, its `ConsumeSpellCost` method verifies if the requesting player has enough passive charge for the specified `spellLevel`. If so, it deducts the cost from the player's `SpellBarController`'s `currentPassiveFill` NetworkVariable.

*   **`ServerAttackSpawner.cs` (Script - Server-Side Service):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Networking/`
    *   Purpose: A server-authoritative singleton. If `SpellBarManager.ConsumeSpellCost` returns true, the `RequestSpellcardServerRpc` calls this service's `ExecuteSpellcard` method. This method:
        1.  Loads the correct `SpellcardData` asset from `Resources` based on the requesting player's character and the `spellLevel`.
        2.  Determines the opponent and the spawn origin (typically above the opponent).
        3.  Starts the `ServerExecuteSpellcardActions` coroutine, which iterates through the `SpellcardData`'s actions, spawning and configuring the bullets server-side according to the defined patterns and behaviors.

*   **Bullet Behavior Scripts (e.g., `LinearMovement.cs`, `DelayedHoming.cs`):**
    *   Located in: `Assets/!TouhouWebArena/Scripts/Spellcards/Behaviors/` (or similar)
    *   Purpose: Components attached to bullet prefabs that define how they move after being spawned by the `ServerAttackSpawner`.

*   **Bullet Prefabs:**
    *   Located in: (Likely `Assets/!TouhouWebArena/Prefabs/Projectiles/` or similar)
    *   Purpose: Standard Unity prefabs representing the different visual types of bullets. Referenced within `SpellcardData` assets. Should have necessary components like `NetworkObject`, relevant behavior scripts (e.g., `LinearMovement`), and potentially `PoolableObjectIdentity` if intended for pooling.

*   **Networking:**
    *   Spellcard activation flow:
        1.  Client (`PlayerShootingController`) detects input release and sufficient local charge level.
        2.  Client sends `RequestSpellcardServerRpc` with `spellLevel`.
        3.  Server RPC handler calls `SpellBarManager.ConsumeSpellCost`.
        4.  If cost is paid, server RPC handler calls `ServerAttackSpawner.ExecuteSpellcard`.
        5.  `ServerAttackSpawner` loads data and runs the server-side `ServerExecuteSpellcardActions` coroutine.
        6.  Coroutine spawns `NetworkObject` bullets on the server, configured to target/affect the opponent. Netcode handles synchronizing these spawned bullets to clients.

## Adding a New Spellcard

Follow these steps to create and integrate a new spellcard into the game:

1.  **Create Bullet Prefabs (if needed):** Ensure you have the necessary bullet prefabs with `NetworkObject` and required behavior components (e.g., `LinearMovement`, `DelayedHoming`, `NetworkBulletLifetime`) attached.

2.  **Create the Spellcard Asset:**
    *   In the Unity Project window, navigate to `Assets/Resources/Spellcards/`.
    *   Right-click -> `Create` -> `TouhouWebArena` -> `Spellcard Data`.

3.  **Rename the Asset:**
    *   **Crucially**, rename the asset using the convention: `CharacterNameLevelXSpellcard`.
    *   Replace `CharacterName` with the exact identifier used in `CharacterStats.GetCharacterName()` (e.g., `HakureiReimu`, `KirisameMarisa`).
    *   Replace `X` with the required charge level (`2`, `3`, or `4`).
    *   **Example:** `KirisameMarisaLevel4Spellcard.asset`.

4.  **Configure the Spellcard Data:**
    *   Select the renamed `.asset` file.
    *   In the Inspector:
        *   *(Optional)* Consider adding a `Spellcard Name` field for display/debugging if needed.
        *   Add one or more `Actions` to the list.
        *   For each `Action`, configure its `Start Delay`, `Bullet Prefabs`, `Formation`, positioning (`Position Offset`), bullet `Count`, behavior parameters (`Behavior`, `Speed`, `Homing Speed`, `Homing Delay`), and other properties as needed.
        *   Reference the bullet prefabs created in step 1.

5.  **Integration (Automatic):**
    *   No explicit code changes are typically needed in `PlayerShootingController` or `ServerAttackSpawner` for new spellcards.
    *   The system relies on the correct naming convention and placement in `Assets/Resources/Spellcards/` for the `ServerAttackSpawner` to load the correct `SpellcardData` asset at runtime via `Resources.Load`.

6.  **Test:**
    *   Run the game as Host/Server and connect a Client.
    *   Select the correct character, charge the spellbar to the appropriate level, and release the fire key.
    *   Verify the spellcard pattern executes correctly on the opponent's side and that the spell cost is deducted from the user's spell bar.
    *   Debug issues by checking the server logs and the `SpellcardData` asset configuration.

## Data Structure

The core data for spellcards is defined in the `SpellcardData` ScriptableObject (`.asset` files). Here are the main fields you configure in the Inspector:

**Top-Level Fields (on the `SpellcardData` asset itself):**

*   *(Consider adding)* **`Spellcard Name` (String):** For identification/debugging.
*   **`Actions` (List of `SpellcardAction`):** The sequence of bullet patterns.

**Fields within each `SpellcardAction` (in the `Actions` list):**

*   **Timing:**
    *   **`Start Delay` (Float):** Seconds to wait before this Action begins relative to the spellcard start.
*   **Spawning:**
    *   **`Bullet Prefabs` (List of GameObjects):** Bullet prefab(s) to spawn. Cycles if multiple are provided.
    *   **`Formation` (Enum - `FormationType`):** `Point`, `Circle`, `Line`.
    *   **`Position Offset` (Vector2):** Offset relative to the spellcard origin point.
    *   **`Count` (Int):** Number of bullets to spawn.
    *   **`Radius` (Float):** Used for `Circle` formation.
    *   **`Spacing` (Float):** Used for `Line` formation.
    *   **`Angle` (Float - Degrees):** Used for `Line` formation orientation.
*   **Behavior:**
    *   **`Behavior` (Enum - `BehaviorType`):** `Linear`, `Homing`, `DelayedHoming`.
    *   **`Speed` (Float):** Initial/linear speed. For `Line` formations, this is the speed of the first bullet.
    *   **`Speed Increment Per Bullet` (Float):** (Only used for `Line` formations) The amount of speed added to each subsequent bullet in the line (e.g., if speed is 1 and increment is 1, bullets will have speeds 1, 2, 3...). Set to 0 to have all bullets use the base `Speed`.
    *   **`Homing Speed` (Float):** Speed when homing (used by `Homing`, `DelayedHoming`).
    *   **`Homing Delay` (Float):** Delay before `DelayedHoming` activates.
*   **Interaction / Lifetime:**
    *   **`Lifetime` (Float):** Overrides the default lifetime of the spawned bullet prefab. Set to a positive value (in seconds) to enable the override. Values less than or equal to 0 will make the bullet use its prefab's default lifetime behavior (e.g., from `NetworkBulletLifetime.maxLifetime` or boundary checks).

    *   **Clearability (Note):** Whether a bullet can be cleared by bombs or shockwaves is **not** determined by the `SpellcardAction`. Instead, it depends on the **bullet prefab** itself. Prefabs intended to be clearable must have a component attached (like `NetworkBulletLifetime`) that implements the `IClearableByBomb` interface. Prefabs without such a component will not be cleared.

// ... (Potential future additions can remain or be removed) ... 