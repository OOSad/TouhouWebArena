# Progress

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