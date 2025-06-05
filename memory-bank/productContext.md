# Product Context

## Core Gameplay Loop & Experience
Touhou Web Arena aims to replicate the fast-paced, bullet-hell gameplay of classic Touhou titles within a competitive 1v1 arena format. The user experience should be responsive, visually clear (despite the chaos), and engaging. Key aspects include:
- Intuitive controls for movement and combat.
- Distinct character playstyles.
- Satisfying feedback for actions (hits, defeats, spellcard activations).
- Input for firing basic shots is via the 'Z' key (tap for burst, hold for continuous stream).
- Input for charging and activating spellcards/charge attacks is via the 'X' key (hold to charge, release to activate).

## Character Abilities

### Hakurei Reimu

- **Basic Shot:** Fires basic talisman bullets. Tap 'Z' for a quick burst of pairs, hold 'Z' for a continuous stream of pairs.
- **Charge Attack (Level 1):** Summons homing talismans that seek nearby enemies.
- **Spellcard (Level 2: "Yin-Yang Sign"):** Spawns two expanding circles of alternating linear and delayed homing bullets. (Currently configured to its hardest difficulty setting).
- **Spellcard (Level 3: "Fantasy Seal"):** Spawns two expanding circles of talismans, one linear and one with double homing behavior. (Currently configured to its hardest difficulty setting).
- **Spellcard (Level 4: "Hakurei Illusion"):** Summons a server-authoritative illusion that executes various attack patterns (Claw with Talismans, Criss-Cross Circles, Three Circles of Talismans, Two Homing Circles of Talismans, Yin Yang Orb Shower - partially unimplemented) using client-simulated bullets.

### Kirisame Marisa

- **Basic Shot:** Fires basic star bullets. Tap 'Z' for a quick burst of pairs, hold 'Z' for a continuous stream of pairs.
- **Charge Attack (Level 1):** Fires a powerful, long-lasting laser.
- **Spellcard (Level 2: "Magic Sign 'Stardust'"):** Spawns six lines of diagonally downward-moving bullets from the outer edges of the opponent's screen, with increasing speed. (Currently configured to its hardest difficulty setting).
- **Spellcard (Level 3: "Magic Sign 'Stardust Reverie'"):** Spawns three lines of diagonally downward-moving bullets on each side of the opponent's field, with increasing speed. (Currently configured to its hardest difficulty setting).
- **Spellcard (Level 4: "Magic Sign 'Illusion Star'"):** Summons a server-authoritative illusion that executes various attack patterns (Four Lines Of Twenty Bullets, Eleven Circles Of Stars And Circles, Yellow Star Barrage, Two Green Lasers - unimplemented, Earthlight Ray Barrage - unimplemented) using client-simulated bullets.

## Audio-Visual Feedback
The game incorporates sound effects for key actions to enhance player immersion and feedback:
- Player firing sounds (local player only).
- Player bullet impact sounds (when local player's bullet hits an enemy).
- Enemy defeat sounds (played by the client of the player dealing the fatal blow for Fairies, Spirits, and Lily White).
- Enemy-specific sounds (e.g., Lily White's spawn and attack sounds, audible to both players, with 3D spatial blend).
Future enhancements will include more ambient sounds, UI sounds, music, and further refinement of 3D audio.