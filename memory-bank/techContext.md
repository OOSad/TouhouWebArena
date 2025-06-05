# Technical Context

## Object Pooling

The project utilizes object pooling to improve performance by reusing frequently created and destroyed objects, reducing instantiation, destruction, and garbage collection overhead. The primary pooling mechanism is client-side.

### Client-Side Pooling (`ClientGameObjectPool.cs`)

- **Purpose:** Manages pools of standard `GameObject`s locally on each client.
- **Usage:** Used for client-simulated entities like projectiles, enemies, visual effects, etc.
- **Requirements for Pooled Prefabs:** Must have a `PooledObjectInfo.cs` component with a unique `PrefabID` and **NO** `NetworkObject` component.
- **Access:** Implemented as a Singleton (`ClientGameObjectPool.Instance`).
- **Configuration:** Configured via an Inspector list on a GameObject in the scene, specifying `PrefabID`, `Prefab`, and `Initial Size`.

### ~~Server-Side NetworkObject Pooling (`NetworkObjectPool.cs`)~~ - DEPRECATED

- This system was previously used for server-authoritative `NetworkObject`s but is no longer used for core gameplay elements like projectiles or enemies. It is considered deprecated.

## Character Definition

Playable characters are defined primarily through **Prefabs** containing necessary components and configurations.

- **Player Prefab:** Located in `Assets/!TouhouWebArena/Prefabs/Characters/`. Includes visuals, collider, `NetworkObject`, and scripts.
- **Core Scripts:** `CharacterStats.cs` (holds most character-specific data like name, stats, attack prefabs), `PlayerShootingController.cs` (handles firing logic, including firing sounds, and spell/charge activation), `ClientAuthMovement.cs`, `PlayerHealth.cs`, etc., are attached to the Player Prefab.
- **Data Assets:** Spellcards (`SpellcardData` assets in `Assets/Resources/Spellcards/`) are linked implicitly via a naming convention (`CharacterNameLevelXSpellcard`) using the `characterName` from `CharacterStats`. Charge attack and basic shot prefabs are directly assigned in `CharacterStats`.
- **Naming Convention:** Character names used in code (e.g., in `PlayerData.cs` enum) must match the `CharacterStats.characterName` and follow the `EnumName` format (e.g., `HakureiReimu`), not names with spaces. `PlayerData.SelectedCharacter.ToString()` should be used for reliable referencing.

## Debug Menu

The debug menu is a **server-only** UI accessible via **F11** for testing and balancing.

- **Functionality:** Allows toggling player hitboxes, insta-killing players, locking/setting player HP, giving max spell bars, toggling fairy and spirit spawns, and toggling player AI.
- **Implementation:** Primarily managed by `DebugMenuController.cs`. Options interact with server-side scripts like `PlayerHealth`, `SpellBarManager`, `FairySpawner`, `SpiritSpawner`, and `PlayerAIController`.
- **Important:** It only appears and functions on the game instance running as the server.

## Asset & Code Conventions

The project follows specific conventions for folder structure, naming, and asset management.

- **Folder Structure:** Organized within `Assets/!TouhouWebArena/` with subfolders like `/Scripts`, `/Art`, `/Prefabs`, `/Resources`, `/Scenes`, etc.
- **Naming Conventions:**
    - Scripts: `PascalCase` (e.g., `PlayerShooting`, `ClientAuthMovement`). Descriptive names with suffixes (`Controller`, `Manager`, `Data`, etc.).
    - Prefabs: `PascalCase`, often mirroring script names (e.g., `Spirit`).
    - ScriptableObjects: `PascalCase`, often ending in `Data` or `Asset`. Spellcards specifically use `CharacterNameLevelXSpellcard`.
    - Scenes: `PascalCase`.
    - Folders: `PascalCase`.
    - Methods: `PascalCase` (e.g., `OnNetworkSpawn`). Verb-based names.
    - Variables: `camelCase` (`passiveFillRate`). `[SerializeField]` for private fields needing Inspector access.
    - Properties: `PascalCase`.
    - Enums: `PascalCase` for type and values.
    - Constants: `PascalCase`.
- **Resource Management (`Resources` Folder):** Used sparingly, primarily for `SpellcardData` assets loaded dynamically via `Resources.Load()`. Avoid other asset types here; assign via Inspector references where possible.
- **Code Style:** Consistent bracing (Allman or K&R), readable and descriptive names, use of comments for *why* not *what*, and namespaces for organization. 

## Audio System

The game uses Unity's built-in audio system for sound effects. Key components and practices include:

-   **`AudioSource` Component:**
    *   Attached to GameObjects that need to play sounds (e.g., Player prefab, Enemy prefabs, specific handler GameObjects).
    *   Often added via `RequireComponent(typeof(AudioSource))` in scripts.
    *   Configured in `Awake()` or the Inspector. Common configurations include disabling `Play On Awake` and `Loop` for one-shot sounds, and setting `Spatial Blend` to `1.0f` for 3D positional audio (e.g., for enemy sounds intended to be heard from their location).

-   **`AudioClip` Assets:**
    *   Sound files (e.g., .wav, .mp3) are imported as `AudioClip` assets.
    *   Scripts that play sounds typically have public or `[SerializeField]` private `AudioClip` fields, allowing sound assets to be assigned in the Unity Inspector.

-   **Playback Methods:**
    *   `AudioSource.PlayOneShot(AudioClip clip, float volumeScale)`: Used for playing short, non-looping sound effects. This is the most common method used so far (e.g., player firing, Lily White spawn, Lily White attack burst).
    *   `AudioSource.PlayClipAtPoint(AudioClip clip, Vector3 position, float volume)`: Used to play a sound at a specific point in world space. This is useful for sounds that should emanate from a location rather than directly from an `AudioSource` attached to a continuously moving object, or for sounds that should persist even if the source object is immediately deactivated/pooled (e.g., enemy defeat sounds).

-   **Sound Context & Control:**
    *   **Local Player Sounds:** Sounds directly related to the local player's actions (e.g., firing) are typically played from a script on the player's prefab (`PlayerShootingController`) and often guarded by an `IsOwner` check to ensure they only play for the player performing the action.
    *   **Enemy Sounds:** Sounds for enemy actions (spawn, attack) are played from scripts associated with the enemy (e.g., `ClientLilyWhiteSpawnHandler`, `LilyWhiteAttackPattern`). These are generally intended to be heard by all players, with 3D spatialization.
    *   **Conditional Global Sounds:** For events like enemy deaths, sounds are played using `PlayClipAtPoint` at the enemy's position. The logic to play the sound is often conditional (e.g., only the client who dealt the killing blow plays the sound) to avoid multiple instances of the sound playing simultaneously across all clients.

-   **Volume Management:** Basic volume control is available via the `volumeScale` parameter of `PlayOneShot` or the `volume` parameter of `PlayClipAtPoint`. More complex mixing and mastering may be addressed later. 