# System Patterns

## Networking Architecture

The project uses a hybrid networking model with Unity's Netcode for GameObjects:

- **Client-Authoritative Movement:** Player movement is handled client-side for responsiveness, synchronizing position via `NetworkVariable`.
- **Server-Authoritative Control:** Server manages spawning and control of key entities like Level 4 Illusions.
- **Client-Side Simulation:** Projectiles, basic enemies, and effects are simulated client-side based on server commands (RPCs).

### Communication Patterns

- **`ServerRpc`:** Clients report events to the server.
- **`ClientRpc`:** Server commands clients to execute functions or spawn client-simulated objects.
- **`NetworkVariable`:** Synchronizes simple data types, primarily player position.

### Key Flows

- Client-initiated actions (basic shots, charge attacks, spellcards) involve a client sending an `ServerRpc` to the server, which then relays a `ClientRpc` to all clients.
- Server-initiated actions (fairy waves, spirits, Level 1-3 spellcards) involve the server sending a `ClientRpc` to all clients.
- Extra Attacks use a client-authoritative trigger, client-side parameter generation, and a server relay via `ServerRpc` and `ClientRpc` to ensure synchronized execution on all clients.
- Level 4 Illusions are server-spawned `NetworkObject`s with server-side orchestration and client-side views that receive RPCs for transform updates and attack execution.

### Round Reset

- Server-side `RoundManager` orchestrates resets.
- Involves pausing spawners, cleaning up server-authoritative entities, sending RPC to clear client-side visuals (pooled objects), resetting player health/position (with a `TeleportClientRpc` for client consistency), and resuming spawners.

## Projectile System

Projectiles are primarily client-simulated based on server commands. Clients handle visual spawning, movement, and local collisions (like walls). Critical interactions (player damage, illusion death) are reported to the server for validation.

- **Core Components:** Client-side prefabs typically have `Collider2D`, visual components, `ClientProjectileLifetime`, `ClientBulletWallCollision`, `PooledObjectInfo`, and various disabled movement behavior scripts (e.g., `ClientLinearMovement`, `ClientHomingMovement`). They do NOT have `NetworkObject`.
- **Spellcard/Illusion Projectile Lifecycle:** Server loads spellcard/attack pattern data defining actions. Server sends `ClientRpc` to clients (`SpellcardNetworkHandler` or `ClientIllusionView`). Client-side `ClientSpellcardActionRunner` executes actions, gets bullets from `ClientGameObjectPool`, and uses `ClientBulletConfigurer` to initialize lifetime, activate, and configure the correct movement behavior script based on action data and the owning player role.
- **Configuration:** `ClientBulletConfigurer` assigns ownership role (for interactions like shockwave clearing), sets lifetime, disables all movement scripts, and enables/initializes the specific movement script defined by the spellcard action.
- **Movement:** Handled by various client-side `MonoBehaviour` scripts (`ClientLinearMovement`, `ClientHomingMovement`, etc.) enabled by `ClientBulletConfigurer`.
- **Despawning:** Primarily timed via `ClientProjectileLifetime`, or forced by wall collisions (`ClientBulletWallCollision`), or explicit clearing effects (spellcards, deathbombs, enemy shockwaves).
- **Collision & Damage:** Visual collisions (walls) are client-side. Player hitting Illusion is detected client-side (`IllusionHealth`), reports death to server via `ServerRpc`. Illusion hitting Player requires a player-side script to detect client-side hit and report damage to server via `ServerRpc`.
- **Charge Attacks:** Server (`ServerChargeAttackSpawner`) receives client request, validates, and calls character-specific handler (`ReimuChargeAttackHandler_Client`, `MarisaChargeAttackHandler_Client`) via `ClientRpc` on the player's object. Handlers spawn and configure client-simulated projectiles from the pool.
- **Other Projectiles:** Stage/retaliation bullets (`StageSmallBulletMoverScript`) and Spirit timeout bullets are also client-simulated from the pool, initiated by server RPCs or client scripts respectively.

## Enemy System

Enemies (Fairies, Spirits, Lily White) use a Server-Authoritative Spawning + Client-Side Simulation model.

- **Fairies:** Server (`FairySpawner`) defines waves and calls `FairySpawnNetworkHandler.SpawnFairyWaveClientRpc`. Clients receive RPC, get fairies from `ClientGameObjectPool`, initialize `SplineWalker` for path movement, `ClientFairyHealth` (health/ownership/damage flash/death shockwave/kill reporting/defeat sound), and `ClientFairyController` for path completion pooling.
- **Spirits:** Server (`SpiritSpawner`) triggers periodic or revenge spawns and calls `ClientSpiritSpawnHandler.SpawnSpiritClientRpc`. Clients receive RPC, get spirits from `ClientGameObjectPool`, initialize `ClientSpiritController` (movement, activation, ownership, centerline check, visual swap), `ClientSpiritHealth` (health, damage flash, death shockwave, kill reporting/defeat sound), and `ClientSpiritTimeoutAttack` (timeout attack pattern). Spirits despawn via centerline check or timeout without death shockwave.
- **Lily White:** Server (`LilyWhiteSpawner`) triggers timed spawns via `ClientLilyWhiteSpawnHandler.SpawnLilyWhiteClientRpc`. Clients receive RPC, get Lily White from `ClientGameObjectPool`. `ClientLilyWhiteSpawnHandler` plays a spawn sound. `ClientLilyWhiteController` handles her three-phase movement and timed despawn. `LilyWhiteAttackPattern` executes her bullet attacks and plays an attack sound. `ClientLilyWhiteHealth` manages her health, damage intake, death notification to the controller, and plays a defeat sound. She is no longer cleared by spellcard shockwaves.
- **Retaliation/Counter Bullets:** Spawned server-side via `EffectNetworkHandler.SpawnStageBulletClientRpc` (triggered by fairy kills reported via `PlayerAttackRelay.ReportFairyKillServerRpc` or shockwave-cleared stage bullets reported via `PlayerAttackRelay.RequestOpponentStageBulletSpawnServerRpc`). Clients receive RPC, get bullet from pool, and initialize `StageSmallBulletMoverScript`. Shockwave-cleared counter bullets use specific parameters and randomized speed.
- **Clearing:** Enemies are cleared by taking lethal damage, triggering death sequences (shockwaves, pooling, defeat sounds). Player deathbombs clear enemies in radius by dealing damage. Spellcard activations clear caster's own pooled enemies via client-side RPC.

## Spellcard System

Spellcards are defined using ScriptableObject assets and follow a server-triggered, client-simulated model (especially for Levels 2/3).

- **Data Structures:** `SpellcardData` (Lv2/3 actions), `Level4SpellcardData` (Illusion definition, attack pool), `CompositeAttackPattern` (L4 attack sequence), `SpellcardAction` (single bullet pattern/action).
- **Level 2 & 3 Spellcards:**
    - Server validates activation/cost (`SpellBarManager`), triggers clear/banner RPCs (`ServerAttackSpawner`), calculates shared random offset if needed, and calls `SpellcardNetworkHandler.ExecuteSpellcardClientRpc`.
    - Clients receive RPC, load `SpellcardData`, calculate origin based on target field, and use `ClientSpellcardActionRunner` to execute actions.
    - `ClientSpellcardActionRunner` iterates actions, gets pooled bullets, calculates spawn positions/rotations (applying shared offset), and calls `ClientBulletConfigurer`.
    - Spellcard clear effect is handled client-side (`ClientSpellcardExecutor`) upon receiving a separate RPC, using `Physics2D.OverlapCircleAll` to find and return pooled objects (own bullets/enemies, opponent's extra attacks) within a radius. Lily White is no longer cleared by this effect.
- **Level 4 Spellcards (Illusion System):**
    - Server (`ServerAttackSpawner`) spawns a server-authoritative Illusion `NetworkObject` with `ServerIllusionOrchestrator`, `ClientIllusionView`, and `IllusionHealth`.
    - Server-side `ServerIllusionOrchestrator` manages lifecycle, idle movement (RPCs transform updates to clients), selects attack patterns from `AttackPool`, calculates orientation/offset, and calls `ClientIllusionView.ExecuteAttackPatternClientRpc`.
    - Client-side `ClientIllusionView` receives RPCs for transform updates and attack execution. It uses `ClientSpellcardActionRunner.RunSpellcardActionsDynamicOrigin` for moving attacks, passing its own transform as the origin.
    - Client-side `IllusionHealth` (on the client targeting the illusion) detects player shot hits, tracks health, triggers damage flash, and calls `ReportDeathToServerRpc` when health is depleted.
    - Server-side `IllusionHealth` receives the death report and calls `ServerIllusionOrchestrator.ProcessClientDeathReport` which despawns the illusion `NetworkObject`.
    - A counter-mechanic in `ServerAttackSpawner` despawns an opponent's illusion if the current player casts a Level 4 spell.

## Extra Attack System

Extra attacks are character-specific abilities triggered by destroying designated fairies. They follow a client-authoritative trigger and simulation model, with the server acting as a relay for synchronized parameters.

- **Triggering:** Server (`FairySpawner`) marks trigger fairies. Client (`ClientFairyHealth`) detects trigger fairy kill and calls `ClientExtraAttackManager.OnTriggerFairyKilled`.
- **Synchronization:** The triggering client calculates all necessary random parameters (spawn points, forces, angles) and sends them with event data via `PlayerExtraAttackRelay.InformServerOfExtraAttackTriggerServerRpc` to the server. The server relays these exact parameters to all clients via `ClientExtraAttackManager.RelayExtraAttackToClientsClientRpc`. All clients then use these synchronized parameters to spawn the visual attack locally.
- **Client-Side Spawning/Behavior:** Clients receive the relayed RPC and use `ClientExtraAttackManager.SpawnExtraAttackInternal` to get the character-specific prefab (Reimu Orb, Marisa Laser) from `ClientGameObjectPool`. Character-specific scripts (`ReimuExtraAttackOrb_Client`, `MarisaExtraAttackLaser_Client`) handle client-side movement (physics for Orb, transform/scaling for Laser), lifetime, pooling, and collision detection.
- **Damage Application:** Client-side attack scripts detect collision with the opponent's player hitbox (`OnTriggerEnter2D`/`OnTriggerStay2D`) and report the hit to the server via `PlayerExtraAttackRelay.ReportExtraAttackPlayerHitServerRpc`. The server processes the hit and applies damage to the server-authoritative `PlayerHealth`.

## Health & Damage System

Player health is server-authoritative, managed by `PlayerHealth` with `NetworkVariable`s for `CurrentHealth` and `IsInvincible`. Enemy health (Fairies, Spirits, Lily White) is client-side, managed by respective health scripts (`ClientFairyHealth`, `ClientSpiritHealth`, `ClientLilyWhiteHealth`). Illusion health is client-side with server reporting, managed by `IllusionHealth`.

- **Player Damage:** Client (`PlayerHitbox`) detects collision and reports to server (`ReportHitToServerRpc`). Server (`PlayerHealth`) applies damage, triggers invincibility (`IsInvincible` `NetworkVariable`), and manages death. Invincibility on server triggers client-side visuals (`PlayerInvincibilityVisuals`) and movement lock (`ClientAuthMovement`). After invincibility, server (`PlayerDeathBomb`) triggers client-side deathbomb clear (`ClearObjectsInRadiusClientRpc`) of pooled bullets and enemies.
- **Enemy Damage:** Client-side scripts (`BulletMovement`, `ClientFairyShockwave`) detect collision and apply damage directly to client-side health scripts. If local player kills enemy, client reports kill to server (`PlayerAttackRelay.ReportFairyKillServerRpc` / `ReportSpiritKillServerRpc`) and the local health script plays a defeat sound using `AudioSource.PlayClipAtPoint()` if the kill was by HP depletion and initiated by the local player.
- **Illusion Damage:** Client-side `IllusionHealth` (on the responsible client) detects player shot hits and applies damage locally. If health is depleted, client reports death to server (`IllusionHealth.ReportDeathToServerRpc`). Server receives RPC and triggers despawn via `ServerIllusionOrchestrator`.

## UI System

The UI displays game information driven by server-synchronized data.

- **HUD:** Includes Health Bars (`PlayerHealthUI`), Spellbar (`SpellBarController`), Round Timer (`RoundTimerDisplay`), Round Indicators (`RoundIndicatorDisplay`), and Latency Counter (`LatencyDisplay`). These elements primarily subscribe to `NetworkVariable` changes.
- **Menus:** Main Menu (`MatchmakerUI`, `ClientConnectorDisconnector`), Character Select (`CharacterSelector`, `SynopsisPanelController` using `CharacterSynopsisData` ScriptableObjects), Pause Menu (TBD), Results Screen (TBD).
- **Synchronization:** `NetworkVariable`s are the primary method for continuous updates (health, spell charge, scores). `ClientRpc`s are used for event-based updates (spellcard banners, likely round start/end messages).
- **Input:** UI elements like buttons interact with networking scripts (`MatchmakerUI`, `ClientConnectorDisconnector`, `CharacterSelector`) to send requests (e.g., join queue, select character) to the server.

## Scope Style

The Scope Style is a mechanic activated when a player enters Focus mode. Its primary function is to interact with and "activate" idle Spirits.

- **Activation:** Tied to the player's Focus mode, managed by `PlayerFocusController.cs`, which likely enables/disables the Scope Style GameObject/Collider.
- **Zone of Influence:** Defined by a trigger collider, typically on a child GameObject.
- **Spirit Interaction:** Detects Spirits (likely via `OnTriggerEnter2D`) and signals the detected `ClientSpiritController` to activate the Spirit. Activated Spirits become visually distinct and potentially easier to destroy.

## Audio System Patterns

Sound effects are being integrated to enhance feedback and immersion. Emerging patterns include:

-   **Local Player Feedback Sounds:**
    *   Sounds for actions initiated by the local player (e.g., firing a shot, bullet impacting an enemy) are typically played by a script on the player's character (e.g., `PlayerShootingController`).
    *   These often use an `AudioSource` component on the player's prefab.
    *   Playback (e.g., `audioSource.PlayOneShot(clip)`) is often conditional on `IsOwner` (for `NetworkBehaviour` scripts) or other logic to ensure the sound is primarily for the local player's benefit.
    *   Example: `PlayerShootingController` plays `playerFireSound`; `BulletMovement` calls a method on the owner `PlayerShootingController` to play `playerBulletHitEnemySound`.

-   **Enemy Action Sounds (Global/Spatialized):**
    *   Sounds related to enemy actions (e.g., Lily White spawning, Lily White attacking) are typically played by scripts on the enemy's client-side representation (e.g., `ClientLilyWhiteSpawnHandler`, `LilyWhiteAttackPattern`).
    *   These utilize an `AudioSource` component on the enemy prefab or its controlling scripts.
    *   These sounds are generally intended to be heard by all players. Future work will refine 3D spatialization (e.g., `spatialBlend`, rolloff curves on the `AudioSource`) for better positional audio.

-   **Conditional Global Sounds (e.g., Enemy Defeat):**
    *   For events that occur globally but should have a single, distinct audio cue (e.g., an enemy being defeated), the sound is often played by the client that triggered or is most directly responsible for the event.
    *   `AudioSource.PlayClipAtPoint(clip, position, volume)` is used to play the sound at the event's location in world space, making it audible to all nearby players.
    *   The decision to play the sound is conditional (e.g., `if (attackerOwnerClientId == NetworkManager.Singleton.LocalClientId)` in enemy health scripts) to prevent the sound from playing multiple times if the death event is processed by all clients.
    *   Examples: `ClientFairyHealth`, `ClientSpiritHealth`, `ClientLilyWhiteHealth` play `enemyDefeatedSound` this way.
    *   **Cooldown for Stacking Sounds:** To prevent an excessive number of identical sounds from playing simultaneously (e.g., multiple enemy defeat sounds), a global static class `GlobalAudioSettings` is used. This class holds `LastEnemyDefeatSoundPlayTime` and `MinIntervalBetweenEnemyDefeatSounds`. Scripts like `ClientFairyHealth`, `ClientSpiritHealth`, and `ClientLilyWhiteHealth` check against these values before playing their defeat sound via `PlayClipAtPoint`, ensuring a minimum time interval has passed.

-   **Global Volume Control (for `PlayClipAtPoint`):**
    *   The `GlobalAudioSettings` class also contains a static `SfxVolume` field (e.g., `0.05f`).
    *   Sounds played using `AudioSource.PlayClipAtPoint` by scripts like the enemy health scripts now use this `GlobalAudioSettings.SfxVolume` to ensure consistent volume levels, rather than relying on potentially varying `AudioSource` component volumes or default values.

-   **Audio Components & Assets:**
    *   Reusable sounds are stored as `AudioClip` assets.
    *   Scripts requiring audio playback typically have serialized `AudioClip` fields (assigned in the Unity Inspector) and an `AudioSource` component (often added via `RequireComponent(typeof(AudioSource))` and configured in `Awake()`).

-   **Music System (Menu, Character Select, Gameplay):**
    *   **State Management:** A static class `MusicStateManager.cs` is used to maintain music state across scene loads. It stores:
        *   `LastPlayedMenuClip` (AudioClip reference)
        *   `LastMenuClipTime` (float)
        *   `GameplayMusicActive` (bool flag)
    *   **Menu & Character Select Music (`MainMenuMusic.cs`, `CharacterSelectMusicPlayer.cs`):**
        *   These are scene-specific MonoBehaviour scripts attached to GameObjects with an `AudioSource` in their respective scenes.
        *   On `Start()`: They check `MusicStateManager`. 
            *   If `GameplayMusicActive` was true, they reset state and play their assigned clip from the beginning.
            *   Otherwise, if `LastPlayedMenuClip.name` matches their assigned `AudioClip.name`, they resume playback from `LastMenuClipTime`.
            *   Otherwise, they play their clip from the beginning.
        *   On `OnDisable()`: If `GameplayMusicActive` is false and their `AudioSource` is playing their assigned clip, they save the `audioSource.clip` to `LastPlayedMenuClip` and `audioSource.time` to `LastMenuClipTime` in `MusicStateManager`.
        *   This allows for seamless music continuation between the Main Menu and Character Select scenes if they use the same audio track.
    *   **Gameplay Music (`GameplayMusicPlayer.cs`):**
        *   This is a `NetworkBehaviour` attached to a GameObject in the gameplay scene.
        *   On `OnNetworkSpawn()`: It sets `MusicStateManager.GameplayMusicActive = true`. This prevents `MainMenuMusic` or `CharacterSelectMusicPlayer` from saving their state when the gameplay scene loads.
        *   The server randomly selects a track from a list of `AudioClip`s and sends the choice to all clients via a `ClientRpc`.
        *   Clients (and the host) receive the RPC and play the synchronized gameplay track from the beginning.
        *   Gameplay music does not attempt to save or resume state.
    *   **Clip Comparison:** `AudioClip.name` is used for comparing clips to ensure reliable resumption, as direct `AudioClip` object comparison can be unreliable across scene loads or different Inspector assignments of the same asset.
    *   **Scene Transitions:** State saving is performed in `OnDisable()` rather than `OnDestroy()` for increased reliability during scene unloads. 