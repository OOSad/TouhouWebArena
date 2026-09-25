# Touhou Web Arena

A split-screen 1v1 competitive danmaku game, playable in the browser. Two
players share a screen, each clearing their own field, and every fairy you pop
sends bullets into your opponent's half.

Inspired by *Touhou 09: Phantasmagoria of Flower View*. Built in Godot 4 and
free to play; this is an unofficial fan work, not affiliated with Team
Shanghai Alice.

> **Status: in active development.** Playable end to end, but balance, art and
> content are all still moving. See [`MEMO.md`](MEMO.md) for the current
> roadmap.

---

## Playing it

Builds come out on playtest weekends. The Windows version is on the
[Releases](https://github.com/OOSad/TouhouWebArena/releases) page, and the
browser version is on itch.io.

You'll need your own copies of **Touhou 9 ~ Phantasmagoria of Flower View**
and **Touhou 15 ~ Legacy of Lunatic Kingdom**. The first time you launch, drag
`th09.dat`, `th15.dat` and each game's `thbgm.dat` (or just the two game
folders) onto the window.

### Roster

Nine characters, each with a full kit: shot, Scope Style, charge attack,
Level 2 and 3 spellcards, Extra Attack and Level 4 boss.

- Reimu Hakurei
- Marisa Kirisame
- Sakuya Izayoi
- Youmu Konpaku
- Reisen Udongein Inaba
- Cirno
- Yuuka Kazami
- Aya Shameimaru
- Clownpiece (guest from Legacy of Lunatic Kingdom)

---

## Netplay

Online play is peer-to-peer over WebRTC, with a lightweight signaling server
handling matchmaking. The signaling server lives in [`server/`](server/) and is
deployed separately; the game points at it by default, so online play needs no
local setup.

## Running it

You'll need [Godot 4.7.2](https://godotengine.org/download), set to the GL
Compatibility renderer. The project targets WebGL 2.0 at 1920x1080.

```
git clone https://github.com/OOSad/TouhouWebArena.git
```

Open the folder in Godot and press **F5**. The game asks for the `.dat` files
above, then goes to the main menu.

Clone to a reasonably short path. The repo contains a 140-character file path
in `addons/`, and Windows checkouts can fail with *"Filename too long"* if
nested deeply. `git config --global core.longpaths true` fixes it if needed.

### Tests

```
run_tests.bat
```

Runs the Godot regression suite headlessly: 23 suites covering health, rounds,
danmaku pooling and batching, damage scaling, AI behaviour, and netplay
authority. Set the `GODOT` environment variable if your editor binary isn't on
the default path.

---

## Repository layout

| Path | What's in it |
|---|---|
| `scenes/` | Game scenes: arena, menus, player, bullets, stages, enemies |
| `scripts/` | GDScript, including the `GameManager` and `NetworkManager` autoloads |
| `resources/` | Data-driven definitions: characters, spellcards, bullet types |
| `shaders/` | Visual effects |
| `tests/` | Headless regression suite |
| `server/` | WebRTC signaling server (Node) |
| `tools/` | Editor-side generators and maintenance scripts |

Design and reference documents:

- [`DANMAKU_CATALOG.md`](DANMAKU_CATALOG.md): every bullet type, pattern step
  and spellcard.
- [`CHARACTER_BALANCE_REFERENCE.md`](CHARACTER_BALANCE_REFERENCE.md): tuning
  numbers across the roster.
- [`AGENTS.md`](AGENTS.md): project conventions and coding standards.
- [`MEMO.md`](MEMO.md): roadmap and backlog.

---

## Assets and licensing

The repository contains none of ZUN's files. The game reads its Touhou
graphics, sounds and music from the player's own copy of the games (`th09.dat`,
`th15.dat` and `thbgm.dat`) when it starts.

Character select portraits are by Dairi, used under their fan project terms:
**non-commercial only**, so the game stays free.

This project follows ZUN / Team Shanghai Alice's derivative work guidelines.
See [`ASSETS_LICENSING.md`](ASSETS_LICENSING.md) for where every asset comes
from.

Touhou Project is the creation of ZUN / Team Shanghai Alice. This is a fan
work, made with affection and no claim to the original material.
