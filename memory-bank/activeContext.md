# Active Context
 
Current focus is on implementing the Lily White enemy.

**Recent Changes:**
- Addressed two issues with Lily White's stage bullets:
    - Bullets were crossing the center of the playfield.
    - Bullets were not being cleared by player spellcard shockwaves or enemy death shockwaves.
- Further addressed the spellcard clearing issue: Bullets were only being cleared by Player 1's spellcards, not Player 2's.
- Implemented dynamic assignment of the bullet's `OwningPlayerRole` based on which player's side Lily White spawns on.

**Fixes Implemented:**
- Modified `StageSmallBulletMoverScript.cs` to include collision detection for the "StageWalls" layer, returning the bullet to the pool upon collision.
- Modified `ClientLilyWhiteSpawnHandler.cs` to determine `PlayerRole` based on `spawnX` and pass it to `ClientLilyWhiteController.Initialize`.
- Modified `ClientLilyWhiteController.cs` to accept and store the `PlayerRole` and pass it to `LilyWhiteAttackPattern.StartAttackSequence`.
- Modified `LilyWhiteAttackPattern.cs` to accept and store the `PlayerRole` and use it when initializing `StageSmallBulletMoverScript`.
- Corrected the bullet clearing logic in `ClientFairyShockwave.cs` to clear bullets belonging to the *opposing* player (`bulletOwnerRole != _ownerPlayerRole && bulletOwnerRole != PlayerRole.None`), aligning with the documentation's intent for enemy shockwaves.
- Corrected the spellcard clearing logic in `ClientSpellcardExecutor.cs` to clear stage bullets belonging to the *opposing* player (`stageBulletMover.OwningPlayerRole != casterRole && stageBulletMover.OwningPlayerRole != PlayerRole.None`).

**Next Steps:**
- Thoroughly test Lily White's attack behavior to ensure all issues are resolved, especially clearing with both player's spellcards and fairy shockwaves.
- Continue implementing remaining enemy behaviors and patterns. 