# Active Context

Current focus is on **updating all relevant project documentation (Memory Bank and `/docs` files)** to reflect the latest changes and implemented features.

**Recently Completed Tasks (Summary):**

1.  **Lily White Spawn Sound:**
    *   Added `AudioClip lilyWhiteSpawnSound` and `AudioSource` to `ClientLilyWhiteSpawnHandler.cs`.
    *   Sound plays via `PlayOneShot()` on spawn.
2.  **Player Firing Mechanics Refactor:**
    *   Modified `PlayerShootingController.cs` for new Z/X key behavior:
        *   Tap 'Z': Burst fire (3 pairs).
        *   Hold 'Z': Continuous fire stream.
        *   Hold 'X': Charge spellbar.
        *   Release 'X': Activate Charge Attack/Spellcard.
3.  **Bug Fix: Lily White Spellcard Clearing:**
    *   Removed code in `ClientSpellcardExecutor.cs` that incorrectly despawned Lily White during spellcard shockwaves.
4.  **Lily White Attack Sound:**
    *   Added `AudioClip attackSoundClip` and `AudioSource` to `LilyWhiteAttackPattern.cs`.
    *   Plays a looping sound with `PlayOneShot()` during sweep attacks.
5.  **Player Firing Sound:**
    *   Added `AudioClip playerFireSound` and `AudioSource` to `PlayerShootingController.cs`.
    *   Plays on local player's shots, guarded by `IsOwner`.
6.  **Player Bullet Hit Enemy Sound:**
    *   Added `AudioClip playerBulletHitEnemySound` to `PlayerShootingController.cs`.
    *   Added `PlayBulletHitEnemySound()` method, called from `BulletMovement.cs` on trigger if the bullet is owned by the local client.
7.  **Enemy Defeat Sounds (Fairy, Spirit, Lily White):**
    *   Added `AudioClip enemyDefeatedSound` to `ClientFairyHealth.cs`, `ClientSpiritHealth.cs`, and `ClientLilyWhiteHealth.cs`.
    *   These scripts now require an `AudioSource`.
    *   Sound plays via `AudioSource.PlayClipAtPoint()` upon HP depletion death, only on the client of the player who dealt the fatal blow.
8.  **Music System Implementation (Menu, Character Select, Gameplay):**
    *   Created `MusicStateManager.cs` to hold menu music state (clip, time) and a `GameplayMusicActive` flag between scene loads.
    *   Implemented `MainMenuMusic.cs` for the main menu scene:
        *   Plays assigned menu music.
        *   Resumes from `MusicStateManager` if returning from character select with the same track.
        *   Saves its state (clip name, time) to `MusicStateManager` via `OnDisable` if gameplay music is not active.
    *   Implemented `CharacterSelectMusicPlayer.cs` for the character select scene:
        *   Plays assigned character select music (typically same as menu).
        *   Resumes from `MusicStateManager` if the track name matches the last played menu clip.
        *   Saves its state to `MusicStateManager` via `OnDisable` if gameplay music is not active.
    *   Updated `GameplayMusicPlayer.cs`:
        *   Sets `MusicStateManager.GameplayMusicActive = true` on spawn to prevent menu music state saving.
        *   Server chooses a random track and synchronizes it to clients via `ClientRpc`.
        *   Gameplay music does not save state for resumption.
    *   Refined logic to use `AudioClip.name` for comparison to ensure reliable resumption.
    *   Switched state saving from `OnDestroy` to `OnDisable` for better reliability during scene transitions.
    *   Removed `PersistentMenuMusicPlayer.cs` and `MainMenuMusicActivator.cs`.
9.  **Enemy Defeat Sound Refinement (Volume & Stacking Mitigation):**
    *   Created `GlobalAudioSettings.cs` (static class) to manage:
        *   `SfxVolume` (global volume for specific `PlayClipAtPoint` sounds).
        *   `LastEnemyDefeatSoundPlayTime` and `MinIntervalBetweenEnemyDefeatSounds` (to implement a cooldown).
    *   Modified `ClientFairyHealth.cs`, `ClientSpiritHealth.cs`, and `ClientLilyWhiteHealth.cs` to:
        *   Use `GlobalAudioSettings.SfxVolume` when playing their defeat sound via `PlayClipAtPoint`.
        *   Check against the cooldown values in `GlobalAudioSettings` before playing the defeat sound, preventing rapid stacking.

**Previous Context (Pre-Sound Implementation & Documentation Update):**
- Addressed issues with Lily White's stage bullets (crossing center, clearing by shockwaves).
- Implemented dynamic assignment of bullet's `OwningPlayerRole` for Lily White.
- Corrected bullet clearing logic in `ClientFairyShockwave.cs` and `ClientSpellcardExecutor.cs` for player spellcards and fairy shockwaves respectively.

**Next Steps (After Documentation Update):**
- Continue with the checklist of features and refinements provided by the user, including:
    - Adding remaining sound effects (menu navigation, character select).
    - Implementing animated backgrounds.
    - Implementing visual/gameplay effects (action stop, round transitions, post-match dialogue).
    - Refine 3D audio settings for enemy sounds (spatial blend, rolloff) for better positional audio cues.
    - Thoroughly test all new sound and music features with both players.

## Current Task: Implementing Feature Checklist for Touhou Web Arena

The primary goal is to implement a list of new features and refinements following the addition of the enemy Lily White.

### Completed Sub-Tasks:
*   Lily White spawn sound effect.
*   Player firing mechanics refactor (tap for burst, hold for continuous, X for spell charge).
*   Bug Fix: Lily White no longer cleared by player spellcard shockwaves.
*   Lily White attack sound effect.
*   Player firing sound effect.
*   Player bullet hit enemy sound effect.
*   Enemy defeat sounds (Fairy, Spirit, Lily White) with local client fix.
*   Level 2/3 spellcards tweaked to hardest setting (manual user task).
*   Action Stop: Spellcard activation (implemented with `ClientSpellcardExecutor`, time slowdown on spell use).
*   Action Stop: Near-death player hit (implemented with `PlayerHealth`, time slowdown when player health is 1 or drops to 0).
*   Round Transition Timings:
    *   Implemented server-side logic in `RoundManager.cs` for a new round end sequence:
        *   Post-entity-clear "catch breath" period (default 2s).
        *   Screen wipe period (default 1s), triggering a client-side effect.
        *   Existing `roundResetDelay` before next round starts.
    *   Created `ClientScreenWipeController.cs` to handle the client-side visual wipe animation.

### Current Focus / Next Steps:
*   **Verify Round Transition Visuals:** The user needs to set up the UI for `ClientScreenWipeController` and confirm the screen wipe animation plays correctly and the overall timing of the new round transition sequence feels right.
*   Address remaining items from the initial checklist.

### Recently Discovered Issues:
*   Lily White's global timer is reportedly not resetting between rounds.

### Key Scripts Involved Recently:
*   `Assets/!TouhouWebArena/Scripts/Spellcards/ClientSpellcardExecutor.cs` (Action stop for spellcards)
*   `Assets/!TouhouWebArena/Scripts/Characters/PlayerHealth.cs` (Action stop for near-death)
*   `Assets/!TouhouWebArena/Scripts/Managers/RoundManager.cs` (Round transition sequence, new delays, RPC for screen wipe)
*   `Assets/!TouhouWebArena/Scripts/Client/UI/ClientScreenWipeController.cs` (Client-side screen wipe animation logic)

### Considerations:
*   The `roundResetDelay` in `RoundManager` might need adjustment after the new `catchBreathDuration` and `screenWipeDuration` are finalized.

## Current Work Focus
- Implementing the refined Lily White appearance timer:
    - Lily White appears every 40 seconds.
    - This timer is reset on round end.
- This involved modifying `LilyWhiteSpawner.cs` to change the interval and add a reset method.
- It also involved updating `RoundManager.cs` to call this reset method during the `RoundResetCoroutine`.

## Recent Changes
- Modified `LilyWhiteSpawner.cs` to use a 40s `spawnInterval` and added `ResetSpawnTimer()`.
- Refactored `RoundManager.cs`'s `RoundResetCoroutine` to include the call to `LilyWhiteSpawner.Instance.ResetSpawnTimer()` and corrected other method calls within the coroutine for entity cleanup and player state reset.

## Next Steps
- Verify the Lily White timer changes in-game.
- Proceed with the next item on the feature list (e.g., post-match character dialogue, remaining sounds/music/backgrounds).

## Active Decisions & Considerations
- The Lily White spawn timer is now server-authoritative and resets explicitly at the end of each round via `RoundManager`.
- The `RoundResetCoroutine` in `RoundManager.cs` has been significantly refactored to ensure correct order of operations and method calls for various reset procedures. 