# Project Memo & Backlog

What is still open, plus the rules that keep biting. Finished work and its design history live in [MEMO_ARCHIVE.md](MEMO_ARCHIVE.md), newest first: when an item here is done, move it there instead of ticking it off in place.

Roster: Reimu, Marisa, Sakuya, Youmu, Cirno, Reisen, Yuuka, Aya, Clownpiece, Lyrica (in progress) (`CharacterData.ROSTER`).

---

## Open

### Character #10: Lyrica Prismriver (pl06), then Merlin (pl14) and Lunasa (pl15)
- [x] Phase 1: roster, stats and shot. `pl06.sht` header: hitbox 2.3 (hurtbox 3.0, as Sakuya / Reisen / Youmu), normal 4.2, focus 2.2 (567 / 297 px/s), charge C 2.8 (1.68). Normal list: Reimu's twin stream (x +-8, speed 12, damage 10, every 5 frames) plus one shot from her centre straight down (`rear_shot`). Shot is pl06.anm sprite24 at alpha 0xa0. Dairi portrait supplied by the user.
- [ ] Next, in Aya's order: Lv1 charge, Lv2/Lv3, Extra Attack, scope, Lv4 boss (pl06_bs.png), victory lines, banner and Lv4 background, home stage and theme. Leads: `pl06.sht` second list (byte 1076) is one entry, speed 0.5, 16x48, damage 1, with extra fields 2 and 4; the third list (byte 1084, only the Prismrivers have one) is 25 single shots straight up every 3 frames, speed 12. pl06.anm scripts 7 / 8 animate the keyboard (sprites 28-35). Attacks in `ecl/pl06.ecl.txt`.

### Codebase health
- [ ] Watch `scenes/bullets/danmaku_bullet.gd`: per-spellcard fields (freeze, pellet trail, wall bounce, repel, lunar wave) keep landing on the one bullet every attack shares. The next card that needs new bullet behaviour should bring its own motion mode or node rather than more fields.
- [ ] Optional: 15 of the 23 test suites use bare `assert()`, so their output can't tell "12 passed" from "stopped at the first". The runner already catches failures; converting them to the `assert_true` + counter style is about an hour.
- [ ] Log noise, not player-visible: a fairy or spirit killed by a bullet adds its shockwave Area2D during a physics callback, so Godot logs "Can't change this state while flushing queries". Spawning the shockwave with `call_deferred` would silence it.

### Art
- [ ] Clownpiece player sheet: idle frame 5 had her hand high on the torch, so it is now a copy of frame 4 and the loop holds a beat there. Regenerating the sheet (ComfyUI, see the local art setup) could give a real in-between frame and close the gap.
- [ ] **Mixed art styles on the character screens (not intended; it grew one character at a time).** The select carousel uses the Dairi fan portraits (`assets/ui/dairi/`), while the post-match screen uses ZUN's faces from the player's .dat. One style has to win; ask the user which before building anything.

### Sparring AI (stopped on purpose: diminishing returns)
- [ ] Curved bullets are predicted as straight lines (`DECEL_AND_HOME`, `EXPANDING_ORBIT`, `CURVE_THEN_LINE`, `ELLIPSE_THEN_LINE`), so spellcards using them are mispredicted.
- [ ] Spellcards fire on a random timer, not when the bot is cornered.
- [ ] `lookahead_horizon` (1.2s) limits foresight more than perception does; raising it costs linearly.
- [ ] `W_HYSTERESIS` (3.0) has twice nearly vetoed a real preference. Revisit if the bot looks stuck.

### Game files (Bring Your Own .dat)
Players drop their own th09.dat / th15.dat / thbgm.dat; we ship none of ZUN's files and there is no way past the file screen without them.
- [ ] Web music (Step 3n) is built but not browser-tested on its own: on itch, drop thbgm.dat after th09.dat, watch "Preparing music n of 7", reload, confirm it stays loaded and plays.
- [ ] File screen friendliness: a "Browse..." button beside drag-and-drop (on web, a JavaScriptBridge file input) and a "where is it?" hint (Steam: right-click the game, Manage, Browse local files).
- [ ] Bring back the charge aura (user wants it) once th19.dat is on the file screen: `aura.anm` -> `aura.png`, 8 frames of 64x64 rising fire (4x2), additive, behind the player at 1.85x + 0.48x per charge level, ~0.046s a frame, tinted with the character's primary colour. The old `ChargeAura` scene is in the history backup (commit 281ffda).
- [ ] Check the rest of `assets/effects/`: looks original but unverified, and compare our forming glow with PoFV's.
- [ ] Remake the Hakugyokurou stage from world03.std. Texture assignments are likely wrong and the balustrades sit below the stairs. Blueprint: object 0 is one 8-step section, 16 quads of 256x24 alternating riser (scripts 0-7, band i, bright) and tread (scripts 8-15, band i+4, color 0x40), plus four 24x271.5 balustrade quads (scripts 22-25) at x -8 / 4 and 264 / 252 with a 12-unit depth step between each pair. Objects 1 and 7 are the ground (script 16, sprite 8), 2-5 the clouds (scripts 17-20), 6 script 21. Instances repeat object 0 every (0, 192, -192).

---

## Rules learned the hard way

- **New character**: add the id to `CharacterData.ROSTER` and make `resources/characters/<id>.tres`. The select screen builds its slots from the roster; only its optional art framing (`character_card.gd`, `character_carousel_slot.gd`) is tuned per character. Per-character behaviour goes on `CharacterData` fields, never `if character_id == "x"` in shared code.
- **.tres files**: properties written above the `script = ` line are silently dropped.
- **Stages** are found by folder: `scenes/stages/<id>/<id>_3d.tscn`.
- **Netplay**: where a big telegraphed attack lands is attack identity and goes on the wire; per-bullet jitter stays local. An attack that randomizes its own trajectory takes a seed through `setup_variation()`, never a bare `randf()`. A spellcard step that draws synced randomness calls `take_sync_rng()` on its first line, before any `await`.
- **Attack nodes** that roll their trajectory in `_ready()` can't be steered by setting properties before `add_child()`; use a `setup_*` method.
- **RPC signature changes** (e.g. `rpc_attack`) mean both peers must be on the same build: mention it in the playtest release notes.
- **Tests**: suites run inside `SceneTree._init()`, so `_ready()` never fires by itself; any new suite must call it by hand. Don't prune tests for runtime; the whole suite is under a minute.
- **AI hazards**: a hazard whose node position isn't its hitbox (e.g. `EarthLightRay` at the top of a full-height column) needs its own ranking, or the tracking cap culls it.
- **Effects**: flickery full-screen effects are one shader on one quad, not `_draw()` geometry (that cost `travel_mote.gd` 60-150 unbatched draw calls).
- **Lily White claw**: `angle_jitter_deg` must stay well under half of `claw_spread_deg`, or the strands merge into a wall.
- **Licensing**: twinject (the AI's velocity-obstacle idea) is GPL-3.0; never port its source. No ZUN pixels or audio in the repo; everything ZUN-made is read from the player's .dat at runtime.
- **Old commit hashes** in the archive (e.g. 281ffda, 22a2973) predate the 2026-09-25 history reset. They live only in the local backup `%USERPROFILE%/Desktop/TouhouWebArena_history_backup.bundle`, which must never be uploaded; `git clone` it into a scratch folder to look one up.
