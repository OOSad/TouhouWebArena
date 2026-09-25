# Touhou Web Arena - Project Rules & Guidelines

## Core Development Principles

1. **Strictly Piecemeal & Incremental Progress**:
   - Implement **one small mechanic or feature at a time**.
   - Never introduce massive sweeping architectural overhauls or unrequested multi-system changes in a single step.
   - After every change, provide concise, clear instructions for how to test it in Godot (e.g., "Press F5, pick Yuuka, and ...").

2. **Communication & Session Continuity**:
   - Keep instructions patient, straightforward, and avoid unnecessary jargon.
   - **Lean Plans & No Boilerplate**: When an implementation plan is needed, keep it brief and human-readable—focus strictly on the high-level design, visual/gameplay feel, and any specific questions or choices for the user. Do not include automated test breakdowns, technical file scaffolding, or lengthy verification checklists in user-facing plans.
   - **Direct Execution on Focused Tasks**: For single-feature requests, tweaks, or polish, skip formal plan artifacts and proceed directly to implementation.
   - **Lean & Focused Testing**: Maintain the lean core regression suite (~20 essential tests for health, rounds, danmaku pooling, damage scaling, and netplay). Never create throwaway standalone test scripts for minor UI tweaks, asset swaps, or one-line fixes. Run tests quietly behind the scenes to guard against regressions; do not dump test scripts or log dumps into the chat.
   - **Snappy Updates**: Keep turn completions concise (a quick 2-3 sentence summary of what changed and direct instructions like "Press F5 and pick Yuuka to test").
   - The AI writes all GDScript code, text-based scene definitions (`.tscn`), and project configurations.
   - Maintain and consult `MEMO.md` as the living roadmap and backlog across conversations.

3. **Engine & Architecture Standards**:
   - **Engine**: Godot 4.x (GL Compatibility / WebGL 2.0). Viewport: 1920x1080.
   - **Language**: GDScript 2.0 with static typing (`var speed: float = 300.0`, `@export`, `@onready`, Callables).
   - **Autoloads**: `GameManager` (match/round state, character selection), `NetworkManager` (WebRTC P2P).
   - **2D Collision Layers**: 1 = `player`, 2 = `enemy_bullets`, 3 = `player_bullets`, 4 = `enemies`.
   - **Testing Entry**: F5 opens the game-files screen (`scenes/game_files/`), then `scenes/main_menu/main_menu.tscn`, through character select. The arena's F6 defaults are stale, so don't point testing at them.
   - **Debug Menu**: Backspace in-game (debug builds), matching THPrac.
   - **Character Design**: Data-driven via `CharacterData` Custom Resources (`scripts/resources/character_data.gd`).
   - **Performance**: High-performance Danmaku design (bullet pooling / batch drawing via `_draw` or `MultiMeshInstance2D`).

4. **Game Design Reference**:
   - Split-screen 1v1 versus shoot-em-up inspired by *Touhou 09: Phantasmagoria of Flower View*.
   - Reference doc: https://docs.google.com/document/d/1UsgTrdHCrtzcEb6zw_KVElGmRIzd13dMLcoT2y7p4Tc/edit?usp=sharing
   - 2 initial characters: Reimu Hakurei & Marisa Kirisame.
   - Key mechanics: Fairies & chain reactions, Wisps & Scope Style, Charge attacks & Spellcards (Lv 1-4), Lily White mini-boss.

5. **Asset Licensing & Touhou Derivative Work Compliance**:
   - Strictly adhere to ZUN / Team Shanghai Alice derivative work guidelines.
   - Using ripped assets from official Touhou games is strictly prohibited for public releases.
   - Any official sprites currently in `assets/` (e.g. Reimu/Marisa sheets) are **strictly temporary placeholders** for calibrating sprite dimensions (32x48), animation timing, and hitboxes.
   - All sprites must be replaced with original or permissively licensed fan art before release. See `ASSETS_LICENSING.md`.

6. **Danmaku, Spellcards & Information Depot Protocol**:
   - Always consult the **Touhou 09 Information Depot** (`C:\Users\Shrine\Desktop\th09_depot\`) before implementing or modifying any character, movement speed, shot type, spellcard, boss pattern, or enemy wave:
     - `ecl/`: Exact bullet counts, angles, angular steps, delays, velocities, and spellcard subroutines (`pl00` - `pl15`, `enemy.ecl.txt`).
     - `sht/`: Exact character movement speeds (normal & focused), hitboxes, and shot stream data.
     - `anm/`: Exact sprite dimensions, UV bounds, animation timings, and frame counts.
     - `sfx/`: Exact sound effect cues (`se_*.wav`).
   - Never guess mathematical parameters or gameplay timings—always cross-reference the decompiled bytecode directly from the depot.
   - Always consult `DANMAKU_CATALOG.md` before implementing or modifying spellcards, bosses, or bullet patterns.
   - Reuse existing `DanmakuBulletData` resources, step types (`DanmakuStep`), and motion modes rather than re-implementing duplicate systems or sprite assets.
   - Keep `DANMAKU_CATALOG.md` up to date whenever new bullet sprites, step types, or character spellcards are introduced.

7. **Version Control & Git Commits**:
   - The assistant may stage and commit completed features/milestones using `git add` and `git commit` via the terminal, crafting clear, descriptive commit messages.
   - Never execute destructive repository commands (such as `git reset --hard`, `git clean -fd`, force-pushes, or rebases) without explicit user instruction.
   - Remote pushes and final review can be executed by the user via GitHub Desktop or terminal.


