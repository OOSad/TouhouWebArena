# Touhou Web Arena - Project Rules & Guidelines

## Core Development Principles

1. **Strictly Piecemeal & Incremental Progress**:
   - Implement **one small mechanic or feature at a time**.
   - Never introduce massive sweeping architectural overhauls or unrequested multi-system changes in a single step.
   - After every change, provide concise, clear instructions for how to test it in Godot (e.g., "Press F5, pick Yuuka, and ...").

2. **Communication & Session Continuity**:
   - Keep instructions patient, straightforward, and avoid unnecessary jargon. The user verifies by playing, not by reading code: explain changes in gameplay terms.
   - **Lean Plans & No Boilerplate**: When an implementation plan is needed, keep it brief and human-readable: focus strictly on the high-level design, visual/gameplay feel, and any specific questions or choices for the user. Do not include automated test breakdowns, technical file scaffolding, or lengthy verification checklists in user-facing plans.
   - **Direct Execution on Focused Tasks**: For single-feature requests, tweaks, or polish, skip formal plan artifacts and proceed directly to implementation.
   - **Lean & Focused Testing**: Maintain the core regression suite (23 suites in `tests/`, run by `run_tests.bat`, under a minute and silent until the end). Never create throwaway standalone test scripts for minor UI tweaks, asset swaps, or one-line fixes. Run tests quietly behind the scenes to guard against regressions; do not dump test scripts or log dumps into the chat.
   - **Snappy Updates**: Keep turn completions concise (a quick 2-3 sentence summary of what changed and direct instructions like "Press F5 and pick Yuuka to test").
   - The AI writes all GDScript code, text-based scene definitions (`.tscn`), and project configurations.
   - `MEMO.md` is the backlog: open work and the rules learned the hard way. Consult it at the start of a task. When an item is finished, move it to `MEMO_ARCHIVE.md` rather than ticking it off in place, so `MEMO.md` stays short.
   - No em-dashes in docs, README or commit messages; use ":" or rewrite the sentence.

3. **Engine & Architecture Standards**:
   - **Engine**: Godot 4.x (GL Compatibility / WebGL 2.0). Viewport: 1920x1080.
   - **Language**: GDScript 2.0 with static typing (`var speed: float = 300.0`, `@export`, `@onready`, Callables).
   - **Autoloads**: `GameManager` (match/round state, character selection), `NetworkManager` (WebRTC P2P), `AudioManager`, `ReplayManager`, `GameData` (reads the player's .dat files).
   - **2D Collision Layers**: 1 = `player`, 2 = `enemy_bullets`, 3 = `player_bullets`, 4 = `enemies`.
   - **Testing Entry**: F5 opens the game-files screen (`scenes/game_files/`), then `scenes/main_menu/main_menu.tscn`, through character select. The arena's F6 defaults are stale, so don't point testing at them. To look at a screen without playing to it, `tools/capture.gd` opens a scene, runs steps (wait, call, key press) and saves screenshots to `scratch/captures/`; usage is at the top of the file.
   - **Debug Menu**: Backspace in-game (debug builds), matching THPrac.
   - **Character Design**: Data-driven via `CharacterData` Custom Resources (`scripts/resources/character_data.gd`). The roster is `CharacterData.ROSTER`; a new character is an id there plus `resources/characters/<id>.tres`. Anything that differs per character is a `CharacterData` field, never an `if character_id == "x"` in shared code.
   - **Performance**: High-performance Danmaku design (bullet pooling / batch drawing via `_draw` or `MultiMeshInstance2D`). New bullet sprites are rows in the shared `BulletSprites` atlas so the web build can batch them.

4. **Game Design Reference**:
   - Split-screen 1v1 versus shoot-em-up inspired by *Touhou 09: Phantasmagoria of Flower View*.
   - Reference doc: https://docs.google.com/document/d/1UsgTrdHCrtzcEb6zw_KVElGmRIzd13dMLcoT2y7p4Tc/edit?usp=sharing
   - Roster: Reimu, Marisa, Sakuya, Youmu, Cirno, Reisen, Yuuka, Aya, and Clownpiece (from LoLK, designed for PoFV rather than ported).
   - Key mechanics: Fairies & chain reactions, Wisps & Scope Style, Charge attacks & Spellcards (Lv 1-4), Lily White mini-boss.
   - Every spellcard and boss attack scales with rank; no rank-flat exceptions.
   - Cards from mainline games don't port verbatim: PoFV attacks are a quick back-and-forth clash, not an endurance card.

5. **Asset Licensing & Touhou Derivative Work Compliance**:
   - Strictly adhere to ZUN / Team Shanghai Alice derivative work guidelines. See `ASSETS_LICENSING.md`.
   - **No ZUN files in the repo.** Players drop their own th09.dat / th15.dat / thbgm.dat on the game-files screen, and every ZUN-made sprite, sound and music track is read from them at runtime (`GameData`, `CharacterSprites`, `BulletSprites`, `DatTextures`, `DatMusic`). The .dat is mandatory; there is no bundled fallback.
   - Don't mix art styles: ZUN-derived visuals come from the .dat, and only what the games lack is made new (original or permissively licensed art).
   - Measuring shapes, sizes and colours from the depot to make our own art is fine; copying ZUN's pixels or audio into the project is not.

6. **Danmaku, Spellcards & Information Depot Protocol**:
   - Always consult the **Touhou 09 Information Depot** (`%USERPROFILE%\Desktop\th09_depot\`; LoLK's is `th15_depot\` beside it) before implementing or modifying any character, movement speed, shot type, spellcard, boss pattern, or enemy wave:
     - `ecl/`: Exact bullet counts, angles, angular steps, delays, velocities, and spellcard subroutines (`pl00` - `pl15`, `enemy.ecl.txt`).
     - `sht/`: Exact character movement speeds (normal & focused), hitboxes, and shot stream data. Conversions: movement speed x135 (dots per frame to px/s); `active_charge_speed` = 0.6 x the charge coefficient C (PoFV charges 100 points a level at C per frame after a 10-frame delay).
     - `anm/`: Exact sprite dimensions, UV bounds, animation timings, and frame counts.
     - `sfx/`: Exact sound effect cues (`se_*.wav`).
   - Never guess mathematical parameters or gameplay timings: always cross-reference the decompiled bytecode directly from the depot.
   - Always consult `DANMAKU_CATALOG.md` before implementing or modifying spellcards, bosses, or bullet patterns.
   - Reuse existing `DanmakuBulletData` resources, step types (`DanmakuStep`), and motion modes rather than re-implementing duplicate systems or sprite assets.
   - Keep `DANMAKU_CATALOG.md` up to date whenever new bullet sprites, step types, or character spellcards are introduced.

7. **Version Control & Git Commits**:
   - The assistant may stage and commit completed features/milestones using `git add` and `git commit` via the terminal, crafting clear, descriptive commit messages.
   - Never execute destructive repository commands (such as `git reset --hard`, `git clean -fd`, force-pushes, or rebases) without explicit user instruction.
   - Remote pushes and final review can be executed by the user via GitHub Desktop or terminal.
   - No personal paths (`C:\Users\<name>\...`) in anything committed; use `%USERPROFILE%` or `res://`.
