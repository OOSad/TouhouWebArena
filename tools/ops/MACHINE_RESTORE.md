# Backing up and restoring this machine

Everything you'd hate to lose lives in **four** places. Only one of them
(the project folder) is the obvious one.

| What | Where it lives | Size | Replaceable? |
|---|---|---|---|
| The game itself | `%USERPROFILE%\Desktop\TouhouWebArena` | ~1.1 GB (excl. caches) | Mostly — it's on GitHub, except `scratch\` |
| Claude conversations + memory | `%USERPROFILE%\.claude\` | ~290 MB | **No.** 30 conversations, nowhere else |
| Gemini / Antigravity conversations | `%USERPROFILE%\.gemini\antigravity\` | ~2.4 GB | **No.** One `.db` file per conversation |
| Editor + Godot settings | `%APPDATA%\Code`, `Antigravity IDE`, `Godot` | ~30 MB | Annoying to redo |

Total to copy: **about 4 GB.** Use an external drive or a decent USB stick.

---

## The one rule that matters

**Restore to the exact same path: `%USERPROFILE%\Desktop\TouhouWebArena`,
under a Windows user with the same name as before.**

Both assistants file their history under the project's full path. Claude's
folder is named after that path, with `:` and `\` turned into `-`. If the new
machine's user is `Andre` or the project sits in `Documents`, both assistants
will look at the right folder and see *zero* past conversations — the files
will be sitting right there, unread. So: same username, same Desktop location.

---

### A second reason to keep the path short

This repo contains a 140-character file path (the macOS WebRTC framework in
`addons\`). Windows long-path support is **not** enabled on this machine, so if
you ever re-clone from GitHub into somewhere deep like
`%USERPROFILE%\Documents\Projects\Godot\TouhouWebArena`, the clone will fail
partway through with *"Filename too long"*. Cloning to
`%USERPROFILE%\Desktop\TouhouWebArena` has plenty of room. Verified both ways.

If you ever do need a deep path, run this once first:
```
git config --global core.longpaths true
```

---

## Before you format

1. **Push your commits.** Open a terminal in the project and run:
   ```
   git push origin main
   ```
   Check for commits that exist only on this machine first — `git log
   origin/main..main --oneline` lists them. Anything it prints is not on
   GitHub yet and would be lost with the drive.

2. **Run the backup.** Plug in your drive, then double-click
   `backup_machine.bat` and type the destination, e.g. `E:\TWA_Backup`.
   (Or drag the drive folder onto the .bat file.)

3. **Check it actually worked.** Open the backup folder and confirm these two
   are not empty:
   - `claude\dot-claude\projects\<the path-derived folder name>\` — should
     have ~30 `.jsonl` files and a `memory\` folder
   - `gemini\antigravity\conversations\` — should have a pile of `.db` files

   If both look right, you're safe to wipe the drive.

---

## After the fresh install

Do these **in order** — the apps need to run once each to create their folders
before the backup can be dropped in.

1. Create the Windows user with **the same name it had before** — the backup
   records it as `Source user name:` in `BACKUP_INFO.txt` on the drive.
2. Install **Godot 4.7.2**, **VS Code + the Claude Code extension**, and
   **Antigravity IDE**. Launch each one once, then close it.

   Godot is a portable .exe, so it lives *outside* the project and is not in
   the backup — just re-download it. It must end up at exactly
   `%USERPROFILE%\Desktop\Godot_v4.7.2-stable_win64.exe`, because
   `tests\run_tests.ps1` hardcodes that path. (You can override it with a
   `GODOT` environment variable instead, if you'd rather keep it elsewhere.)
3. Run `restore_machine.bat` from the backup drive, pointing it at the backup
   folder.
4. Finish the leftovers the script reminds you about:
   - Godot → *Editor → Manage Export Templates → Download* (513 MB, skipped on
     purpose since it's a plain re-download)
   - Log back in to Claude and Antigravity (no passwords were backed up)
5. Reinstall the command-line tooling. **Required:**
   - **Git for Windows** — install this *first*. Without it you cannot push or
     pull the repo, and the assistants lose their shell. Tick *"Git from the
     command line and also from 3rd-party software"* during setup so `git`
     lands on your PATH. (GitHub Desktop alone is **not** enough — it bundles
     its own private copy that nothing else can see.)
   - **GitHub CLI (`gh`)** — `export_release.bat` uses it to create the draft
     release. Run `gh auth login` once after installing.

   After installing any of these, **fully quit and reopen VS Code**. Opening a
   new terminal is not enough: VS Code caches the environment from when it
   launched, so until you restart it, `git` and `node` stay invisible to the
   editor and to the assistants even though they're correctly installed.

   You can skip the installer's *Windows Explorer integration* option — those
   are just right-click menu entries and nothing here uses them. Unchecking
   *"Git Bash Here"* does **not** remove Git Bash itself, which does matter.

   **Optional — install only when the need actually comes up:**
   - **Node.js 24.x**, then `npm install` in `server\`. Not needed for normal
     play or testing: the editor connects straight to the Render signaling
     server by default (`network_manager.gd` line 42), and the localhost
     fallback below it only fires for a *web* build served from a local URL.
     The one case that needs it is editing `server\signaling_server.js`
     itself — without Node you'd be pushing changes to Render blind, and a
     bad deploy takes online play down with no way to reproduce it locally.
     `tests\test_signaling_matchmaking.js` also needs it, and is not part of
     `run_tests.ps1`.
   - **Python 3.11** with `pip install opencv-python numpy pillow`. The game
     does not use these — there is no Python in the repo at all. They are how
     the assistants crop spritesheets and pull frames out of videos you share,
     since ffmpeg isn't set up. Skip until that comes up.
6. Open the project in Godot once and let it re-import. Press **F5** — if the
   main menu comes up and you can start a match, the restore worked.
7. Open Claude in VS Code and check that past conversations are listed. Same in
   Antigravity.

---

## What was deliberately left out, and why

- `.godot\` (137 MB) and `build\` (422 MB) — Godot regenerates both.
- `server\node_modules\` — `npm install` regenerates it from `package-lock.json`.
- Godot export templates (513 MB) — a plain download.
- Gemini's `brain\` folder (3.8 GB) — its codebase index. Gemini rebuilds it.
  If you'd rather keep it anyway, run `backup_machine.bat E:\TWA_Backup brain`.
- Login tokens (`.credentials.json`) — they're tied to this machine and you'd
  have to log in again regardless.

`scratch\` **is** backed up even though git ignores it, because it holds your
reference captures and tuning artifacts that exist nowhere else.

---

## If it turns out to be hardware, not the OS

Nothing above changes — the backup is worth making either way, and it restores
onto a different machine just as well, as long as you keep the username and the
Desktop path the same.
