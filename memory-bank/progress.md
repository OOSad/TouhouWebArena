# Progress & Known Issues

## What Works

*   **Core Gameplay Loop:** Basic 1v1 match structure, players can move, shoot, and defeat each other.
*   **Networking:** Core systems are networked with Netcode for GameObjects, supporting two players.
*   **Character Abilities (Reimu & Marisa):**
    *   Basic shots (tap and hold variants).
    *   Spellcard charging (X key) and execution (C key).
    *   Level 2/3 spellcards are at their hardest settings.
*   **Enemy System:**
    *   Fairies and Spirits spawn and function.
    *   Lily White (Midboss):
        *   Spawns correctly.
        *   Performs sweeping bullet attacks.
        *   Is immune to spellcard shockwave clearing.
*   **Sound Effects:**
    *   Lily White: Spawn and attack sounds.
    *   Player: Firing sound, bullet hit enemy sound.
    *   Enemies: Fairy, Spirit, and Lily White defeat sounds (client-local based on who dealt damage).
*   **Action Stop Effects:**
    *   Brief game slowdown when a spellcard is activated (client-side effect).
    *   Brief game slowdown when a player takes a near-death hit (health drops to 1, or is hit at 1 HP). (client-side effect, triggered by server).
*   **Round Transition Logic (Server-Side):**
    *   `RoundManager.cs` orchestrates a new sequence upon round end:
        *   Entities are cleared.
        *   A "catch breath" period (default 2s) occurs.
        *   A screen wipe effect is triggered on clients (default 1s server-side wait).
        *   The existing `roundResetDelay` occurs before the next round.
    *   `ClientScreenWipeController.cs` exists to manage the client-side wipe animation (pending visual setup and verification by user).
*   **Lily White Spawning:**
    *   Appears at a fixed 40-second interval.
    *   This 40-second timer is correctly reset at the end of each round by `RoundManager.cs` calling `LilyWhiteSpawner.Instance.ResetSpawnTimer()`.
*   **Round Reset Sequence:**
    *   The `RoundResetCoroutine` in `RoundManager.cs` has been refactored with correct method calls for pausing/resuming spawners, server-side entity cleanup, client-side entity cleanup RPC, player spell bar resets, and player position resets, in addition to the Lily White timer reset.

## What's Left to Build / Current To-Do List

*   **Round Transition - Visual Verification:**
    *   User to set up the UI Image for `ClientScreenWipeController` and confirm the screen wipe animation.
    *   Fine-tune timing for `catchBreathDuration`, `screenWipeDuration`, and `roundResetDelay` in `RoundManager` and animation timings in `ClientScreenWipeController`.
*   **Sound Effects (Remaining):**
    *   Menu Navigation (Button clicks, transitions).
    *   Character Select screen actions.
    *   Matchmaking sounds (joining queue, match found).
    *   Refine 3D audio settings for enemy sounds (spatial blend, rolloff) for better positional audio cues.
*   **Music Integration:**
    *   Main Menu music.
    *   Character Select screen music.
    *   Gameplay music (different tracks per stage/round if desired).
*   **Animated Backgrounds:** Implement dynamic/animated backgrounds for the gameplay scene.
*   **Post-Match Character Dialogue Screen:** Implement a screen after match conclusion where characters have a brief dialogue exchange.
*   **Bug Fix:** Lily White's global timer does not reset between rounds.
*   Tweak Level 2/3 spellcards to their hardest settings (User confirmed this was done, but good to keep track if any further tweaks arise).
*   **Sound Effects:**
    *   Menu navigation sounds.
    *   Character select screen sounds.
    *   Matchmaking sounds.
*   **Music:**
    *   Main menu music.
    *   Character select screen music.
    *   Gameplay music (per-character or general).
*   **Visuals & Polish:**
    *   Animated backgrounds for gameplay.
    *   Post-match character dialogue screen/sequence.

## Current Status

*   The Lily White appearance timer has been successfully modified and integrated with the round reset logic.
*   The `RoundResetCoroutine` in `RoundManager.cs` is now more robust due to corrected method calls.

## Known Issues / Bugs

*   **Lily White Timer:** Lily White's global timer does not reset between rounds (reported by user).
*   **Sound Volume/Mixing:** General volume levels and mixing across all new sounds need review once all are implemented.
*   **Editor Visual Bug (User Workaround Found):** Unity Editor Inspector for `SpellcardData` (specifically `actions` list) had display issues with very long lists; user found a workaround.

## Future Considerations / Nice-to-Haves (Post-MVP)

*   More complex enemy attack patterns.
*   Additional playable characters.
*   Different stages with unique mechanics or hazards.
*   Ranked matchmaking / leaderboards.
*   Spectator mode.

**Completed Recently:**

*   **Lily White Enemy Implementation (Initial Pass & Fixes):**
    *   Addressed issues with Lily White's stage bullets (crossing center, clearing by shockwaves).
    *   Implemented dynamic assignment of bullet's `OwningPlayerRole` for Lily White.
    *   Corrected bullet clearing logic in `ClientFairyShockwave.cs` and `ClientSpellcardExecutor.cs`.
    *   Fixed bug where Lily White was incorrectly despawned by spellcard shockwaves.
*   **Player Firing Mechanics Refactor:**
    *   Overhauled `PlayerShootingController.cs`:
        *   Tap 'Z' for burst fire.
        *   Hold 'Z' for continuous fire.
        *   'X' key dedicated to spellbar charging and spell/charge attack activation.
*   **Sound Effect Implementation (Phase 1):**
    *   Lily White spawn sound (`ClientLilyWhiteSpawnHandler.cs`).
    *   Lily White attack sound (looping during sweeps in `LilyWhiteAttackPattern.cs`).
    *   Player firing sound (local, in `PlayerShootingController.cs`).
    *   Player bullet hitting an enemy sound (local, via `BulletMovement.cs` and `PlayerShootingController.cs`).
    *   Enemy defeat sounds for Fairies, Spirits, and Lily White (via `ClientFairyHealth.cs`, `ClientSpiritHealth.cs`, `ClientLilyWhiteHealth.cs`, played using `AudioSource.PlayClipAtPoint()` on the client dealing fatal blow).
*   **Music System Implementation:**
    *   Implemented scene-specific music players (`MainMenuMusic.cs`, `CharacterSelectMusicPlayer.cs`) for menu and character select screens, allowing continuous playback between these scenes.
    *   Utilized a static `MusicStateManager.cs` to store current music time, clip name, and a `GameplayMusicActive` flag to manage state across scene loads.
    *   Music state (clip name, playback time) is saved on `OnDisable` and resumed on `Start` if appropriate, using clip name comparison for robustness.
    *   Updated `GameplayMusicPlayer.cs` to be a `NetworkBehaviour`, with the server selecting and synchronizing a random gameplay track to clients via `ClientRpc`.
    *   `GameplayMusicPlayer.cs` sets `MusicStateManager.GameplayMusicActive = true` to ensure menu/character select music does not save its state when transitioning to gameplay.
    *   Removed `PersistentMenuMusicPlayer.cs` and `MainMenuMusicActivator.cs`.
*   **Enemy Defeat Sound Refinement:**
    *   Implemented `GlobalAudioSettings.cs` to provide a global SFX volume (`SfxVolume`) for specific `PlayClipAtPoint` sounds and a cooldown mechanism (`LastEnemyDefeatSoundPlayTime`, `MinIntervalBetweenEnemyDefeatSounds`).
    *   Updated enemy health scripts (`ClientFairyHealth`, `ClientSpiritHealth`, `ClientLilyWhiteHealth`) to use this global volume and cooldown, preventing excessively loud and stacked defeat sounds.
*   **Spellcard Tweaks:**
    *   Difficulty of Level 2/3 spellcards adjusted to be their "hardest" version.
*   **Documentation Updates (In Progress - Nearly Complete):**
    *   Updated `/docs/input_handling.md`
    *   Updated `/docs/spellcard_system.md`
    *   Updated `/docs/enemy_system.md`
    *   Updated `/docs/characters/hakureiReimu.md`
    *   Updated `/docs/characters/kirisameMarisa.md`
    *   Updated `memory-bank/activeContext.md`
    *   Updating `memory-bank/progress.md` (this file)

**What's Left (User Checklist):**

*   **Visual Enhancements:**
    *   Animated backgrounds for gameplay arenas.
*   **Gameplay Effects & Polish:**
    *   "Action Stop" effect on spellcard activation.
    *   "Action Stop" effect on player death.
    *   Round Start/End transition screens/animations.
    *   Post-match character dialogue screen.
*   **Audio Refinements:**
    *   Refine 3D audio settings for enemy sounds (spatial blend, rolloff on AudioSources) to improve positional audio clarity.
*   **Testing:**
    *   Thoroughly test all new sound effects and audio features with both players.
    *   Re-verify Lily White's behavior and interactions after recent changes.

**Known Issues/Considerations:**
*   Unity Editor Inspector display bug for `SpellcardData` with 11+ actions (user has a workaround).
*   **3D Audio for Global Sounds:** Implement proper 3D audio settings for sounds like Lily White's spawn/attack so they are positioned correctly in the game world and volume is handled by distance, rather than just playing for everyone at full volume or only for one client. 