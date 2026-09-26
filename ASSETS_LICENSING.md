# Asset Attribution & Touhou Derivative Work Compliance

This project follows **ZUN's Touhou Project Derivative Work Guidelines** (上海アリス幻樂団 東方Projectの二次創作ガイドライン).

### Derivative Work Rule:
Under the official guidelines, derivative fan games may freely use the characters, world, and concepts of Touhou Project, but **are strictly prohibited from ripping, extracting, or reusing original game assets (graphics, sound effects, BGM, code) directly from Team Shanghai Alice's original releases** in any published or distributed build.

### Bring Your Own .dat:
The game reads ZUN's graphics, sounds and music from the player's own `th09.dat`, `th15.dat` and `thbgm.dat` at runtime (they must provide all three before the main menu). Every ZUN-made image, sound and track the game shows or plays comes from there: bullets, effects, item pickups, the PoFV characters' player, shot, boss, charge and extra attack sprites, spell banners and backgrounds, result screen faces, fairies, spirits and Lily White, the round-win sakura, stage textures, sound effects and music, and Clownpiece's boss sprites. The repository ships none of ZUN's files.

### Non-Commercial:
The character select portraits are Dairi's, whose terms allow use in fan projects but not commercially. The game must stay free: it can't be sold or used to make money.

### Third-Party Art:
- `assets/ui/dairi/*.png` (all ten, including `random.png`): character select portraits by **Dairi** (dairi / dairi104). Free to use in fan projects, **non-commercial only**. Credit isn't required but is given on the main menu footer.
- `assets/characters/clownpiece/clownpiece_raw_sheet.png`: fan-made Clownpiece sprite sheet generated with AutoSprite and supplied by the project owner; not ripped from any Team Shanghai Alice game. Check AutoSprite's usage terms before release.
- `assets/characters/clownpiece/clownpiece_player.png` (8x3 grid of 48x48 player cells): generated locally in ComfyUI from the AutoSprite sheet above, posed by the project owner. Models: NoobAI-XL v1.1 (Fair AI Public License 1.0-SD), the pixel-art-xl LoRA (CreativeML OpenRAIL-M) and Laxhar's `noob_openpose` ControlNet, whose Hugging Face page states no license (the rest of the NoobAI release is FAIPL); confirm its terms before release. Disclosed as generative AI on the itch.io page.

### Our Own Art:
- `assets/ui/` (apart from `dairi/`): playfield frame, bezel, pillars, divider, title and stage backgrounds, shimenawa ring and life orbs, generated for this project.
- `assets/ui/menu_characters/*.png`: sumi-e ink wash silhouettes for the character cards (Clownpiece's supplied by the project owner).
- `assets/effects/`: `sakuya_knife_glow.png` is procedurally generated; the other effect textures are our own but not yet individually verified.
