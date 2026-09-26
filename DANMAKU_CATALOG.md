# Danmaku & Spellcard Architecture Catalog

This document is the **single source of truth** for all Danmaku, spellcard patterns, and bullet projectiles in *Touhou Web Arena*. 
**Before creating new spellcards, character attacks, or boss patterns, consult this catalog to reuse existing assets, resources, and step types.**

---

## 1. System Overview

Pattern generation is entirely **data-driven and timeline-based**. Spellcards are composed of modular steps in the Godot Inspector—no custom engine code is required for standard patterns.

```text
SpellcardData (.tres)
 └── steps: Array[DanmakuStep]
      ├── DanmakuRingStep (Ring 1: Circles + Ovals, Linear + Homing)
      ├── DanmakuDelayStep (Pause between waves)
      └── DanmakuRingStep (Ring 2: Talismans, etc.)
```

- **Bullet Pooling**: Handled automatically by `danmaku_bullet_pool` (250 pre-allocated instances per playfield).
- **Shockwave Cancellation**: Automatically integrated. Regular shockwaves cancel bullets into items; heavy shockwaves vaporize them.
- **Despawning**: Purely **time-based** (`lifetime` timer, default 7.0s) with an extreme safety boundary ($\pm 1200\text{px}$). Bullets can safely expand deep offscreen without getting eaten before homing.

---

## 2. Catalog of Bullet Resources (`resources/bullets/`)

All Danmaku bullets and enemy pellets utilize sub-regions of the unified $512 \times 512$ master texture atlas (`assets/bullets/danmaku_atlas.png`) via dedicated `AtlasTexture` resources stored in `resources/bullets/textures/`. Because all bullets share the exact same underlying GPU texture RID and static `z_index = 10`, Godot 4's Compatibility renderer (`gl_compatibility`) renders all active bullets across both playfields in just 1–2 WebGL draw calls.

**Superseded:** the game ships no bullet art. `BulletSprites` (`scripts/game_data/`) fills each bullet resource's texture, plus `EnemyPellet`'s, at startup: PoFV's `etama.anm` from the player's th09.dat (drawn at 2.0833x) and, for Clownpiece, LoLK's `bullet.anm` from th15.dat, all packed into one runtime texture. The texture and region columns in the table below describe the retired HD art; `danmaku_atlas.png` only keeps two original-art cells (the forming glow and Reisen's moon mote). A new bullet resource needs a row in `SPRITES`, or it draws nothing.

| Resource File | Atlas Texture Resource | Sub-Region Rect | Size / Visual | Hitbox Radius | Fairy Shockwave Cancelable | Rotate w/ Dir | Base Damage | Common Role |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| *(EnemyPellet)* | `tex_pellet_blue.tres` | `Rect2(0, 0, 128, 128)` | 128×128 HD circle, electric blue rim, pure `#FFFFFF` core ($0.16\times$) | 6.0 px | **Yes (`true`)** | `false` | 1.0 | Reciprocal counter-pellets, Lily White blue arm |
| *(EnemyPellet)* | `tex_pellet_red.tres` | `Rect2(256, 0, 128, 128)` | 128×128 HD circle, ruby red rim, pure `#FFFFFF` core ($0.16\times$) | 6.0 px | **Yes (`true`)** | `false` | 1.0 | Reciprocal counter-pellets, Lily White red arm |
| `red_pellet.tres` | `tex_pellet_red.tres` | `Rect2(256, 0, 128, 128)` | 128×128 HD circle, ruby red rim, pure `#FFFFFF` core ($0.16\times$) | 6.0 px | **Yes (`true`)** | `false` | 1.0 | Fast linear rings, perimeter screens |
| `white_pellet.tres` | `tex_pellet_white.tres` | `Rect2(384, 0, 128, 128)` | 128×128 HD circle, silver/slate rim, pure `#FFFFFF` core ($0.16\times$) | 6.0 px | **Yes (`true`)** | `false` | 1.0 | White ring patterns, alternating rings |
| `sakuya_dagger_pellet.tres` | `tex_pellet_white.tres` | `Rect2(384, 0, 128, 128)` | Identical art to `white_pellet.tres`, but with `spawn_fade_in = true` ($1.6\times$ oversized & transparent shrinking down to full size/opacity over 0.18s on spawn - purely visual, hitbox is full size immediately) | 6.0 px | **Yes (`true`)** | `false` | 1.0 | Sakuya Lv 4 Boss Attack 1 "Mysterious Jack" pellet trail |
| `blue_pellet_small.tres` | `tex_pellet_blue.tres` | `Rect2(0, 0, 128, 128)` | 128×128 HD circle, electric blue rim, pure `#FFFFFF` core ($0.14\times$) | 5.0 px | **Yes (`true`)** | `false` | 1.0 | Marisa Lv 2 Stardust Reverie diagonal lines |
| `green_pellet.tres` | `tex_pellet_green.tres` | `Rect2(128, 0, 128, 128)` | 128×128 HD circle, emerald green rim, pure `#FFFFFF` core ($0.16\times$) | 6.0 px | **Yes (`true`)** | `false` | 1.0 | Marisa Boss Attack 3 pinwheel, Youmu Lv 2 descending strips |
| `green_arrow.tres` | `tex_arrow_green.tres` | `Rect2(352, 160, 32, 32)` | 32×32px arrowhead/kunai, vibrant green wings, glowing white core | 6.0 px | **No (`false`)** | `true` | 1.0 | Youmu Lv 2 & Lv 3 Lost Sign "Binding Sword" descending strips |
| `green_knife.tres` | `tex_knife_green.tres` | `Rect2(384, 160, 32, 48)` | 32×48px dagger/knife, all-green tinting, luminous white halo centered on whole knife body ($1.5\times$) | 8.0 px | **No (`false`)** | `true` | 1.0 | Youmu Lv 3 Lost Sign "Binding Sword" descending strips |
| `yellow_knife.tres` | `tex_knife_yellow.tres` | `Rect2(416, 160, 32, 48)` | 32×48px dagger/knife, vibrant golden yellow tinting, luminous white halo ($1.5\times$) | 8.0 px | **No (`false`)** | `true` | 1.0 | Youmu Lv 4 Boss Attack 1 Fan of Yellow Knives |
| `blue_knife.tres` | `tex_knife_blue.tres` | `Rect2(448, 160, 32, 48)` | 32×48px dagger/knife, sapphire blue tinting, luminous white halo ($1.5\times$) | 8.0 px | **No (`false`)** | `true` | 1.0 | Youmu Lv 4 Boss Attack 2 Rapid Fire of Blue Daggers |
| `sakuya_knife.tres` | `tex_sakuya_knife_red.tres` (source `sakuya_knife_red.png`) | `Rect2(0, 384, 32, 32)` | 32×32px dagger, blade pointing RIGHT, red tinting ($2.25\times$) | 8.0 px | **No (`false`)** | `true` | 1.0 | Sakuya Lv 2/3 Time Sign "Private Square" fan |
| `sakuya_knife_cyan.tres` | `tex_sakuya_knife_cyan.tres` (source `sakuya_knife_cyan.png`) | `Rect2(80, 384, 32, 32)` | 32×32px dagger, blade pointing RIGHT, cyan tinting ($2.25\times$) | 8.0 px | **No (`false`)** | `true` | 1.0 | Sakuya Lv 4 Boss Attack 1 Time Sign "Mysterious Jack" thrown dagger |
| `sakuya_knife_purple.tres` | `tex_sakuya_knife_purple.tres` (source `sakuya_knife_purple.png`) | `Rect2(120, 384, 32, 32)` | 32×32px dagger, blade pointing RIGHT, purple tinting ($2.25\times$) | 8.0 px | **No (`false`)** | `true` | 1.0 | Sakuya Lv 4 Boss Attack 2 Reflective Knife Explosion ring (pre-bounce) |
| `sakuya_knife_blue.tres` | `tex_sakuya_knife_blue.tres` (source `sakuya_knife_blue.png`; distinct from Youmu's `blue_knife.tres`) | `Rect2(40, 384, 32, 32)` | 32×32px dagger, blade pointing RIGHT, blue tinting ($2.25\times$) | 8.0 px | **No (`false`)** | `true` | 1.0 | Sakuya Lv 4 Boss Attack 2 Reflective Knife Explosion ring (post-bounce) |
| `sakuya_knife_yellow.tres` | `tex_sakuya_knife_yellow.tres` (source `sakuya_knife_yellow.png`; distinct from Youmu's `yellow_knife.tres`) | `Rect2(160, 384, 32, 32)` | 32×32px dagger, blade pointing RIGHT, yellow tinting ($2.25\times$) | 8.0 px | **No (`false`)** | `true` | 1.0 | Sakuya Lv 4 Boss Attack 3 Sweep of Yellow Daggers |
| `red_oval.tres` | `tex_oval_red.tres` | `Rect2(256, 128, 32, 32)` | 20×28px oval needle, red rim | 7.0 px | **No (`false`)** | `true` | 1.0 | Aimed shots, alternating decel rings, Reisen Lv 2 Lunar Wave icicles |
| `reisen_wave_bullet.tres` | `tex_reisen_wave_bullet.tres` (source `reisen_wave_bullet.png`) | `Rect2(240, 384, 36, 64)` | 9×16px rice/casing bullet cut from the Mountain of Faith projectile sheet, rounded nose, red rim, white core, upscaled 4x nearest and rendered at $0.39\times$ (so ~14×25px). Nose already points up, so no `rotation_offset_deg`. Narrow on purpose: Lunar Wave Lv 3 packs 112 of them round one circumference at Rank 16 | 5.0 px | **No (`false`)** | `true` | 1.0 | Reisen Lv 3 Lunar Wave wheel spokes, Reisen Lv 4 Boss Attack 2 rings |
| `reisen_spiral_bullet.tres` | `tex_reisen_spiral_bullet.tres` (source `reisen_spiral_bullet.png`) | `Rect2(200, 384, 32, 64)` | 8×16px rice/casing bullet, white core, violet rim, upscaled 4x nearest and rendered at $0.40\times$ (so ~13×26px). The same row of the Mountain of Faith projectile sheet as `reisen_wave_bullet`, two columns along: that one is column 2 (red, x=30), this is column 4 (violet, x=62). The row is not on an even column pitch and this cell is 8px wide against the red one's 9, so `Rect2i(62, 144, 8, 16)` is read off the sheet rather than derived. Cut by [`tools/generate_reisen_spiral_bullet.gd`](tools/generate_reisen_spiral_bullet.gd). Nose already points up, so no `rotation_offset_deg` | 5.0 px | **No (`false`)** | `true` | 1.0 | Reisen Lv 4 Boss Attack 3 spiral machinegun |
| `clownpiece_flame_red.tres` | `tex_clownpiece_flame_red_0..3.tres` (sources `clownpiece_flame_red_0..3.png`) | `Rect2(0/40/80/120, 448, 32, 40)` | **First animated bullet**: TH15 `bullet3.png` sprites 484-487 (`bullet.anm` script 108), a white-cored orb trailing flame, cycled through `anim_frames` every 0.05s (3 frames). Cut from the TH15 reference sheet at (259, 128) by [`tools/generate_clownpiece_flame.gd`](tools/generate_clownpiece_flame.gd); each 32x32 frame sits on 8px of padding so the cell centre lands on the orb (the anm draws it 4px up). Flame trails upward in the art, so `rotation_offset_deg = 180`. Rendered at 1.5625x, TH15's scale | 6.5 px | **No (`false`)** | `true` | 1.0 | Clownpiece Lv 4 Infernal Essence of Grazing |
| `clownpiece_big_star_blue.tres` | `tex_clownpiece_big_star_blue.tres` (source `clownpiece_big_star_blue.png`) | `Rect2(160, 448, 32, 32)` | TH15's large five-pointed star, blue: `bullet2.png` (96, 0), supplied by the user, cut from the TH15 reference sheet at (96, 258) by [`tools/generate_clownpiece_big_star.gd`](tools/generate_clownpiece_big_star.gd). Rendered at 2.143x (TH15 scale, ~69 px); Star and Stripe shrinks it by rank with node scale | 13.0 px | **No (`false`)** | `false` | 1.0 | Clownpiece Lv 4 Star and Stripe |
| `clownpiece_big_star_red.tres` | (runtime, `BulletSprites`) | th15 bullet2.png `Rect2(32, 0, 32, 32)` | TH15's pink-red big star, the blue one's twin (spins 3 rad/s) | 13.0 px | **No (`false`)** | `false` (spins) | 1.0 | Clownpiece Lv 4 Starry Illusion |
| `clownpiece_glow_ball_purple.tres` | (runtime, `BulletSprites`) | th15 bullet1.png `Rect2(48, 48, 16, 16)` | TH15's violet white-cored glow ball (sprite 67, type 26), at 2.143x (~34 px) | 8.5 px | **No (`false`)** | `false` | 1.0 | Clownpiece Lv 4 Fake Apollo |
| `white_oval.tres` | `tex_oval_white.tres` | `Rect2(288, 128, 32, 32)` | 20×28px oval needle, grey rim | 7.0 px | **No (`false`)** | `true` | 1.0 | Reimu Lv 2 Fantasy Seal homing needles |
| `red_talisman.tres` | `tex_talisman_red.tres` | `Rect2(416, 128, 32, 32)` | 26×30px Shinto parchment, red runes ($0.8\times$) | 6.8 px | **No (`false`)** | `true` | 1.5 | Heavy Lv 3 spellcard strikes, bosses |
| `white_talisman.tres` | `tex_talisman_white.tres` | `Rect2(448, 128, 32, 32)` | 26×30px Shinto parchment, dark runes ($0.8\times$) | 6.8 px | **No (`false`)** | `true` | 1.5 | Heavy Lv 3 spellcard strikes, bosses |
| `blue_star.tres` | `tex_star_blue.tres` | `Rect2(320, 128, 32, 32)` | 24px blue star with white center ($1.5\times$) | 6.0 px | **No (`false`)** | `false` | 1.0 | Marisa Lv 2 & Lv 3 Stardust Reverie right-wall diagonal lines |
| `green_star.tres` | `tex_star_green.tres` | `Rect2(352, 128, 32, 32)` | 24px emerald green star with white center ($1.5\times$) | 6.0 px | **No (`false`)** | `false` | 1.0 | Marisa Lv 3 Stardust Reverie left-wall diagonal lines, Boss Spell 3 |
| `yellow_star.tres` | `tex_star_yellow.tres` | `Rect2(384, 128, 32, 32)` | 24px golden yellow star with white center ($1.5\times$) | 6.0 px | **No (`false`)** | `false` (spins $6.0\text{ rad/s}$) | 1.0 | Marisa Boss Attack 4 ("Star Spray" / Magic Sign "Illusion Star") |
| *(EnemyPellet)* | `tex_ring_white.tres` | `Rect2(0, 128, 128, 128)` | 128×128 HD glowing ring, bright core band ($0.28\times$) | 11.0 px | **No (`false`)** | `false` | 1.0 | Lily White trailing ring barrage & boss rings |
| *(TravelMote)* | `travel_mote_hd.png` | Standalone texture | 128×128 HD glowing orb, pure white core, Gaussian halo ($0.18\text{--}0.32\times$) | N/A (Mote) | N/A | `false` | N/A | Reciprocal attack delivery motes (additive blend) |
| `blue_icicle.tres` | `tex_icicle_blue.tres` | `Rect2(256, 160, 32, 48)` | 32×48px HD crystal icicle, authentic Touhou 3D jewel faceted shading, brilliant white specular ridge, sapphire blue refraction, luminous aura ($0.6667\times$). `frozen_texture` swaps to `tex_icicle_white.tres` (`Rect2(320, 160, 32, 48)`, pure white frost crystal variant) on freeze. | 5.0 px | **No (`false`)** | `true` | 1.0 | Cirno Lv 2 & Lv 3 "Perfect Freeze" expanding ring, Lv 4 Boss Attack 3 "Shower of Icicles" |
| `cyan_icicle.tres` | `tex_icicle_cyan.tres` | `Rect2(288, 160, 32, 48)` | 32×48px HD crystal icicle, vibrant cyan/aqua jewel refraction, brilliant white specular ridge, luminous cyan halo ($0.6667\times$). Aligned alongside blue and white icicles on main projectile row. | 5.0 px | **No (`false`)** | `true` | 1.0 | Cirno Lv 4 Boss Attack 2 "Interweaving Icicles" |
| `yellow_pellet.tres` | `tex_pellet_yellow.tres` | `Rect2(0, 256, 128, 128)` | `red_pellet` re-hued to gold ($0.16\times$) | 6.0 px | **Yes (`true`)** | `false` | 1.0 | Yuuka Lv 2 "Blossoming of Gensokyo" |
| `yellow_rice.tres` | `tex_rice_yellow.tres` | `Rect2(352, 208, 32, 48)` | `blue_icicle` re-hued to yellow. ZUN's rice is a grain pinched at both ends, which is our icicle's shape, not `red_oval`'s egg. Scaled non-uniformly ($0.95 \times 0.786$) to ~16x33, PoFV's grain | 5.9 px | **No (`false`)** | `true` | 1.0 | Yuuka Lv 2 "Blossoming of Gensokyo" |
| `yellow_arrow.tres` | `tex_arrow_yellow.tres` | `Rect2(288, 208, 32, 32)` | `green_arrow` re-hued to yellow ($1.2\times$) | 7.2 px | **No (`false`)** | `true` | 1.0 | Yuuka Lv 3 "Blossoming of Gensokyo" |
| `dark_yellow_arrow.tres` | `tex_arrow_dark_yellow.tres` | `Rect2(320, 208, 32, 32)` | `green_arrow` re-hued toward amber, rim darkened to 72% (ZUN's DarkYellow) ($1.2\times$) | 7.2 px | **No (`false`)** | `true` | 1.0 | Yuuka Lv 3 "Blossoming of Gensokyo" |

| `red_ring_ball.tres` / `yellow_ring_ball.tres` | `tex_ring_ball_red.tres` / `tex_ring_ball_yellow.tres` | `Rect2(256, 256, 64, 64)` / `Rect2(320, 256, 64, 64)` | White core, coloured band, darker outlines: the user's colour-neutral `assets/bullets/circle_in_circle.png` coloured by [`tools/generate_yuuka_ring_balls.gd`](tools/generate_yuuka_ring_balls.gd), 4x nearest to 64px and baked into the atlas, drawn at $0.52\times$ (~33px, ZUN's 16x16 `RingBall`). Re-run the tool for another colour | 11.7 px | **No (`false`)**: large bullets are immune | `false` | 1.0 | Yuuka Lv 4 Boss Attack 1 (forms in: `spawn_fade_in` 2x to 1x over 16 frames, visual only) |
| `yellow_big_ball.tres` | `tex_pellet_yellow.tres` | (as the pellet) | The pellet art at $0.547\times$, taking its visible 106px disc (not the 128px cell) to ~58px, ZUN's 28x28 `BigBall`. The ECL says Blue; the footage shows white with a yellow rim. Forms for 16 frames first as `tex_spawn_glow_yellow` (see `spawn_fade_in_*`) | 20.5 px (off while forming) | **No (`false`)**: large bullets are immune | `false` | 1.0 | Yuuka Lv 4 Boss Attack 2 |
| `orange_rice.tres` / `orange_pellet.tres` | `tex_rice_orange.tres` / `tex_pellet_orange.tres` | `Rect2(384, 208, 32, 48)` / `Rect2(128, 256, 128, 128)` | `yellow_rice` / `yellow_pellet` re-hued to orange, same scales | 5.9 / 6.0 px | as the yellow pair | as the yellow pair | 1.0 | Yuuka Lv 4 Boss Attack 3 (forms in: `spawn_fade_in` 2x to 1x over 16 frames, visual only) |
| `red_arrow.tres` | (runtime, `BulletSprites`) | etama Arrowhead, Red | PoFV red arrowhead at 2.0833x | 7.2 px | **No (`false`)** | `true` | 1.0 | Aya Lv 2 |
| `blue_arrow.tres` | (runtime, `BulletSprites`) | etama Arrowhead, Blue | PoFV blue arrowhead at 2.0833x | 7.2 px | **No (`false`)** | `true` | 1.0 | Lyrica Lv 3 |
| `lyrica_note_red.tres` | (runtime, `BulletSprites.PLAYER_ANM_BULLETS`) | pl06_ex.png sprite 69, red double note | 2.0833x, spinning 6.28 rad/s | 13 px | **No (`false`)** | `false` | 1.0 | Lyrica Extra Attack |
| `red_butterfly.tres` / `purple_butterfly.tres` | (runtime, `BulletSprites`) | etama y176 Butterfly row (sprites 120-127, 8 colours): ECL DarkRed = red, ECL Red = purple | PoFV butterfly at 2.0833x | 8.3 px | **No (`false`)** | `true` | 1.0 | Aya Lv 3, Lv 4 Boss Attack 1 |
*Bullets drawn from their own PNGs (the Sakuya knives, the two Reisen rice bullets) are copied into the bottom band of the atlas by [`tools/bake_bullets_into_atlas.gd`](tools/bake_bullets_into_atlas.gd), so every pooled bullet samples one texture and the web build batches them all; re-run it after editing a source PNG. The four yellow cells and the two orange ones are baked by [`tools/generate_yuuka_bullets.gd`](tools/generate_yuuka_bullets.gd), which recolours our own atlas cells (idempotent). After re-running it, run Godot once with `--import` (or open the editor) or the game keeps drawing the old, empty cells.*

*Note: All bullet resources inherit [`DanmakuBulletData`](scripts/resources/danmaku_bullet_data.gd). You can adjust individual bullet `hitbox_radius`, `can_be_canceled`, `damage`, `lifetime`, or `base_scale` in the Inspector.*
*Sizing Rule: visible sizes (the "Scale" notes above predate this) are matched to the PoFV sprite's visible box x 2.0833, measured from the depot's `etama` sheets: pellets 17 px (ZUN's 8x8 `Pellet`), small stars 33 px, talismans 29x33, arrowheads 30x31, rice 24x33, icicles unchanged (~28 px, matching a PoFV side-by-side). Knives and swords (56-70 px long) were left as they are, slightly above PoFV. The depot has no PoFV bullet hitboxes, so each hitbox grew by its sprite's factor (keeping the old hitbox-to-sprite ratio), unified per PoFV sprite: pellets 6.0, small stars 8.3, talismans 9.4, rice 8.3, arrowheads 7.2.*

*Cancellation Rule: Small circular pellets (`*_pellet.tres`) are cancelable by fairy shockwaves (`can_be_canceled = true`), carving safe paths and charging the player. Signature/special projectiles (talismans, stars, needles/ovals) are immune to fairy shockwaves (`can_be_canceled = false`) and require player evasion or Level 2–4 Heavy Shockwaves.*

**Non-`DanmakuBulletData` atlas tenants.** A couple of hazards are standalone scenes rather than pooled bullets, but still draw from the shared atlas so they batch with everything else. Record them here so a future repack does not paint over them:

| Atlas Region | Texture Resource | Used By | Notes |
| :--- | :--- | :--- | :--- |
| `Rect2(128, 128, 64, 64)` | `tex_reisen_moon_mote.tres` | `scenes/attacks/reisen_moon_mote.tscn` | 64×64 pale moon, 60px sphere: near-neutral white core inside a salmon annulus, crisp dark-plum rim ~2px. Baked by [`tools/generate_moon_mote.gd`](tools/generate_moon_mote.gd), which is idempotent (`blit_rect` replaces the cell rather than blending into it). Drawn from measured reference colour, not cut from the original game. Rendered at 0.50×–1.02× as the mote breathes. |


---

## 3. Pattern Step Types (`scripts/resources/`)

All pattern steps inherit from the base class [`DanmakuStep`](scripts/resources/danmaku_step.gd).

### 1. `DanmakuRingStep` ([`danmaku_ring_step.gd`](scripts/resources/danmaku_ring_step.gd))
- **ECL parity (`pl09.ecl` `sub3`, Yuuka)**: `relaunch_relative` / `relaunch_turn_deg` turn a `DECEL_AND_HOME` bullet from its own heading at relaunch instead of aiming it (ZUN's `AimRel`). `bounce_off_walls` gives every bullet one ricochet off the left, right or top wall, which `DanmakuBullet` now honours in `DECEL_AND_HOME` as well as `LINEAR`.
- **ECL parity (`pl00.ecl`, Reimu)**: `use_wave_angle` takes the one random angle `SpellcardData.random_wave_angle` rolls per wave, so red and white rings of a wave share it (ZUN `F0 = RAND_ANGLE`). `aim_at_player` = `bullet_circle_aimed`. `homing_stage2_decel_time` / `homing_stage2_launch_speed` give a second `AimPlayer` its own numbers (Lv3: brake 1s, relaunch 375, brake 1.5s at once, relaunch 250). ZUN's `0x80` flag bit is what marks the bullets `AimPlayer` applies to, which is why only the white rings home. Lv2 whites sit with pellets on the red rice positions and rice on the half steps. `SpellcardData` also gained `wave_rank_breaks` / `wave_counts` (Reimu: 1/2/3 waves at ranks <8/<15/16), `jitter_wave_origin` and `mirror_player_origin` (ZUN's helper at `-PLAYER_X` clamped to +-80, y 128).
Emits a 360° ring of bullets with dynamic rank scaling, alternating bullet shapes, and independent motion modes.

- **Bullet Types**:
  - `bullet_data`: Primary bullet resource.
  - `alternate_bullet_data`: Optional alternating bullet resource (creates an even-odd `A -> B -> A -> B` ring).
  - `bullet_sequence`: Optional array for arbitrary $N$-way bullet cycling (`A -> B -> C -> ...`).
  - *Seam Protection*: If count is odd, it automatically rounds up by 1 so no two identical bullets collide at the loop seam.
- **Rank Scaling (Rank 1 to 16)**:
  - `count_min` / `count_max`: Interpolated based on match rank (e.g. 122 fixed, or 16 at rank 1 up to 32 at rank 16).
  - `speed_min` / `speed_max`: Minimum (Rank 1) and Maximum (Rank 16) launch speed in px/s.
- **Geometry & Alignment**:
  - `base_angle_deg`: Base orientation angle.
  - `stagger_half_step`: If `true`, offsets the ring by half the angular step ($\pi / N$) to interweave cleanly with preceding rings.
- **Motion Modes**:
  - `motion_mode`: `LINEAR` or `DECEL_AND_HOME`.
  - For `DECEL_AND_HOME`:
    - `homing_decel_time`: Time in seconds to brake from initial speed to 0.
    - `homing_pause_time`: Time spent stationary while snapshotting the player's position.
    - `homing_launch_speed` / `homing_launch_speed_max`: Speed in px/s when accelerating toward the snapshot target. If `homing_launch_speed_max > 0.0`, scales linearly with rank ($162.5\text{ px/s} \to 552.5\text{ px/s}$ as in Reimu Lv 2).
    - `homing_stages`: Number of lock-on stages (`1` for single-target dash as in Lv 2; `2` for double lock-on as in Lv 3).
    - `stage1_flight_duration`: Duration in seconds bullets dash towards the player during stage 1 before braking and initiating stage 2 homing.
- **Independent Alternate Motion**:
  - `alternate_has_separate_motion`: If `true`, the alternating bullets follow `alternate_motion_mode` with separate speed, homing parameters, `alternate_homing_stages`, `alternate_stage1_flight_duration`, and optional `alternate_homing_launch_speed_max` (e.g., circles fly straight while ovals decel and home).

### 2. `DanmakuDelayStep` ([`danmaku_delay_step.gd`](scripts/resources/danmaku_delay_step.gd))
Pauses the timeline before triggering the next step in the array.
- `delay_seconds`: Time to wait (e.g., `0.1`s for rapid cascading rings, `0.5`s between distinct phases).

### 3. `DanmakuDiagonalStripStep` ([`danmaku_diagonal_strip_step.gd`](scripts/resources/danmaku_diagonal_strip_step.gd))
Emits parallel diagonal lines of alternating stars and pellets falling from the outside border toward the inside.
- **Outside-to-Inside Mirroring & Dual Inward**:
  - `DirectionMode.AUTO_FROM_OUTSIDE`: Automatically checks `playfield.player_number`:
    - Opponent is **P1** (left playfield): spawns on left wall ($x \le 0$) and flies down-right ($45^\circ$, vector `(+0.707, +0.707)`).
    - Opponent is **P2** (right playfield): spawns on right wall ($x \ge \text{PLAYFIELD\_WIDTH}$) and flies down-left ($135^\circ$, vector `(-0.707, +0.707)`).
  - `DirectionMode.DUAL_INWARD`: Double-pronged attack! Both left and right walls fire simultaneous pairs inward toward the center at each step index.
- **Line Composition & Rank Scaling**:
  - `lines_min`: 6 lines in single mode / 3 pairs in dual mode (Rank 1).
  - `lines_mid`: 8 lines in single mode / 4 pairs in dual mode (Rank 8).
  - `lines_max`: 12 lines in single mode / 6 pairs in dual mode (Rank 16).
  - Distributed evenly along vertical span `top_y` (-30.0) to `bottom_y` (840.0), allowing the top line to clip slightly into the upper playfield boundary at spawn.
  - Alternates bullet types per line if `alternate_bullet_types = true` (Lv 2: `blue_star` and `blue_pellet_small`); or uses distinct bullets per wall in dual mode (`bullet_star` for left wall, `bullet_secondary` for right wall).
  - Staggered release: sequential delay between lines via `delay_between_lines`.
- **Ascending Bullet Velocities & Stacked Spawning**:
  - Each line has `bullets_per_line` (8 bullets) starting stacked at the exact same point on the wall (`bullet_spacing = 0.0`).
  - Trailing bullet (closest to wall) has `speed_min` (140 px/s), leading bullet (furthest into field) has `speed_max` (400 px/s).
  - As bullets fly across the screen, the velocity difference naturally expands the line into a fan/comb pattern.
- **Timing & Audio**:
  - `delay_between_lines`: Sequential delay between successive line releases (default `0.08s`).
  - `sfx`: Sound effect played on each emitted strip (default `se_tan00`).
- **Lv 2 ECL parity (`pl01.ecl` `sub0`)**: at 125 px/s per ECL px/frame (60 fps x 600/288).
  - `DirectionMode.OPPOSITE_PLAYER`: fires from the wall away from the victim's half at cast time (ZUN's `PLAYER_X < 0` check), toward the victim.
  - `line_rank_breaks` / `line_counts`: stepped counts `(6, 10, 15)` / `(6, 8, 10, 12)`, i.e. `2 x IC0`.
  - `divide_full_height` + `wall_offset = 0`: line i at `i x 960 / count` on the wall itself (ZUN steps `448 / (2 x IC0)` from y = 0 at x = +-144).
  - `zun_layer_speeds`: bullet k of 8 moves at `max - (max - min) x k / 8`, ZUN's layered-shot rule. Stars top out at `speed_max + speed_max_per_rank x (rank - 1)` (197.5 -> 347.5, from `1.5 + 0.08 x rank`); pellet lines use a fixed `pellet_speed_max = 250` (`2.0`). Floor `speed_min = 62.5` (`0.5`).
  - 8-frame wait between lines (`0.1333s`). The slowest bullets need ~11s to cross, so `blue_star` / `blue_pellet_small` carry a 14s lifetime and leave by the off-screen despawn instead.
  - Not reproduced: ZUN alternates star sprite types 12 and 13 between star lines; both use `blue_star` here.

### `DanmakuExtraAttackStep` (`scripts/resources/danmaku_extra_attack_step.gd`)
- **ECL parity (`pl01.ecl` `sub5`, Marisa boss spell 2)**: `zun_sweep` steps rays `64 - 2 x rank` ECL units from a synced random start, alternating between that point and its mirror, `288 / step` rays (4 at Rank 1, 9 at Rank 16) every `100 / count` frames, tilt within +-2.8125 degrees. Not reproduced: ZUN alternates two ray colours.
- **ECL parity (`pl05.ecl` `sub4` / `sub5`, Cirno boss spell 1)**: `zun_top_sweep` drops `rank / 4 + 6` stalactites 10 frames apart in even columns `k x 600 / n`, swept from a synced random side (ZUN's two mirrored subs). The height stays in the regular stalactite band. The spell's opening ring is one aimed 16-pellet ring in ZUN's three speed layers (225 / 183.3 / 141.7 px/s).
Summons a sequential barrage of character Extra Attacks (e.g. Reimu's Yin-Yang Orbs or Marisa's Earth Light Rays).
- **Rank Scaling**: `count_min` (Rank 1) to `count_max` (Rank 16).
- **Sequential Delay**: `delay_between_spawns` (default 0.22s) produces a rhythmic flurry rather than simultaneous cluster.
- **Spawn Placement**:
  - `FROM_BOSS`: Spawns sequentially fanning out horizontally across the boss's position (`horizontal_spread = 120.0`), with alternating left/right toss direction for a balanced fountain effect.
  - `ACROSS_TOP`: Spawns distributed across the top playfield boundary ($X \in [80, 520]$, $Y \in [70, 120]$).
  - `GROUND_RAYS`: Spawns vertical/tilted beams (Earth Light Rays) anchored to the ground border ($Y = 950\text{ px}$) across randomized ground positions ($X \in [75, 525]$, with consecutive ray separation $\ge 45\text{ px}$). Supports halved tilt angles in $[-3.25^\circ, +3.25^\circ]$ (`tilt_angle_min_deg = -3.25`, `tilt_angle_max_deg = 3.25`, `alternate_tilt = false`) pointing both inward and outward across the arena.

### `DanmakuClawStep` (`scripts/resources/danmaku_claw_step.gd`)
- **ECL parity (`pl00.ecl` `sub6`)**: `zun_mode` is two phases of `rank / 2 + 8` frames, re-aimed each frame, no acceleration. Phase 1: talisman plus pellets at **+-12.86** degrees (a 2-bullet ZUN fan sits at half its 0.4488 rad spread either side, not the full 25.7 the description below uses), 112.5 + 12.5 px/s per frame. Phase 2: centre pellets, 150 + 25 px/s per frame.
Fires 3 angled strips of non-homing pellets in a fan/claw spread aimed toward the player, with an overlaid strip of red talismans on the center strip.
- **Aimed Claw Spread**: Computes vector from boss origin to opponent player position; fires 3 strips at $\theta_{\text{aim}} - 25.7^\circ$, $\theta_{\text{aim}}$, and $\theta_{\text{aim}} + 25.7^\circ$ (`fan_spread_angle_deg = 25.71` matching ZUN's $0.4488\text{ rad}$).
- **Bullet Types**: 3 strips of `red_pellet.tres` + 1 strip of `red_talisman.tres` overlaid on the center.
- **Velocity Gradient ("Unfurling")**: Each consecutive bullet in a strip is faster than the previous one ($112.5\text{--}200\text{ px/s}$ at Rank 1, $112.5\text{--}300\text{ px/s}$ at Rank 16), causing the burst to stretch out into an unfurling claw.
- **Center Acceleration Differential**: Pellets in the center strip accelerate at $70\text{--}110\text{ px/s}^2$, while side strips and center talismans accelerate at $25\text{--}45\text{ px/s}^2$. The center pellets surge ahead of the talismans and side prongs during flight.
- **Rank Scaling**: `bullets_min = 8` (32 bullets total) at Rank 1 scaling up to `bullets_max = 16` (64 bullets total) at Rank 16.

### `DanmakuDoubleRingStep` (`scripts/resources/danmaku_double_ring_step.gd`)
- **ECL parity (`pl00.ecl` `sub7`)**: `curve_mode` replaces the orbit: `rank + 30` bullets per ring at 187.5 px/s, aimed, each heading turning at +-0.785 rad/s for 2s then flying straight (red one way, white the other, white on the half steps).
Spawns two concentric rings of small pellets (one red, one white) that "walk" the circumference in opposite directions while smoothly expanding outward until leaving the screen.
- **Motion Mode**: `DanmakuBullet.MotionMode.EXPANDING_ORBIT`. Each bullet updates its polar coordinates $(R(t), \theta(t))$ relative to the boss's cast position.
- **Counter-Rotation**: Red ring rotates clockwise ($+\omega$), white ring rotates counter-clockwise ($-\omega$). Angular speed is $\omega = 0.7854\text{ rad/s}$ ($45.0^\circ/\text{s}$, matching ZUN's $0.01309\text{ rad/frame} \times 60$). White pellets are interleaved at spawn by half a step ($\pi / N$), forming a counter-rotating zipper effect.
- **Radial Expansion**: Radius expands outward from center ($R_0 = 0.0\text{ px}$, all pellets initially stacked inside the boss) at $187.5\text{ px/s}$ (matching ZUN's speed $1.5 \times 125.0$).
- **Rank Scaling**: `count_min = 30` (30 red + 30 white = 60 bullets total) at Rank 1 scaling up to `count_max = 46` (46 red + 46 white = 92 bullets total) at Rank 16.

#### `DanmakuAimedStripStep` (`scripts/resources/danmaku_aimed_strip_step.gd`)
- **ECL parity (`pl01.ecl` `sub3`, Marisa boss spell 1)**: `zun_speed_first = 462.5` / `zun_speed_floor = 250` gives constant speeds 462 / 409 / 356 / 303 px/s (3.7 over a 2.0 floor, no acceleration). `fire_window_frames = 40` with an interval of `int(60 / (rank + 6))` frames gives 5 strips at Rank 1 and 20 at Rank 16.
Emits sequential strips of bullets aimed directly towards the opponent's current location, with all bullets in each strip spawning simultaneously and separating smoothly in flight via an acceleration gradient.
- **Aimed Trajectory**: Re-evaluates the opponent player's position prior to emitting each strip, tracking target movement to form a sweeping, curved trail.
- **Simultaneous Spawn & Acceleration Differential**: All bullets in a strip spawn stacked at the boss origin with initial `base_speed` ($220\text{ px/s}$, $+0.3\text{x}$ speed). An ascending acceleration gradient ($a_{\min} = 90\text{ px/s}^2 \to a_{\max} = 210\text{ px/s}^2$, $+40\text{ px/s}^2$ per bullet) stretches the strip apart as it flies across the screen.
- **Boss Cast Hop**: If `boss_cast_hop = true`, the boss performs a standardized hop to the side ($80\text{ px}$, $1.55\text{s}$) while firing the stream. Hop distance and duration are standardized identically across all ranks (Rank 1 and Rank 16 both travel $80\text{ px}$ over $1.55\text{s}$), authentic to Marisa's PoFV attack behavior. At lower ranks, the boss remains in its glide and cast pose until the full $1.55\text{s}$ hop concludes.
- **Rank Scaling**: Fixed at 4 bullets per strip at all ranks. Strips scale from `strips_min = 5` (20 stars total) at Rank 1 up to `strips_max = 20` (80 stars total) at Rank 16.
- **Sequential Delay & Audio**:
  - Interval between strips scales from `0.12s` (Rank 1) to `0.07s` (Rank 16).
  - `sfx`: Plays authentic `se_tan00` for each strip of stars emitted.

### `DanmakuPinwheelStep` (`scripts/resources/danmaku_pinwheel_step.gd`)
- **ECL parity (`pl01.ecl` `sub7`, Marisa boss spell 3)**: `zun_mode` fires one fan of `rank / 6 + 2` bullets 12 degrees apart every 60 fps frame for 60 frames, starting straight up. ZUN reuses the speed value (`1.0 + 0.1 x rank`) as the per-frame turn in radians, so the fan turns 1.1 rad a frame at Rank 1 and that aliasing draws the arms. Types cycle blue pellet, green star, green pellet, blue star (`pellet_bullet_data_alt`). Spawns at the boss centre (`spawn_radius = 0`).
Spawns sequential rotating pinwheel bursts composed of 6 radiating spokes winding outward into tight spiral ribbons matching Touhou 09.
- **6-Spoke Repeating Sequence**: `spoke_count = 6`. Burst waves alternate cleanly across all 6 spokes:
  - Wave $b \pmod 2 == 1$: Green Pellets (`green_pellet.tres`)
  - Wave $b \pmod 4 == 0$: Green Stars (`green_star.tres`)
  - Wave $b \pmod 4 == 2$: Blue Stars (`blue_star.tres`)
  This creates 6 seamless spiral arms consisting of: Green Stars $\to$ Pellets $\to$ Blue Stars $\to$ Pellets $\to$ Green Stars $\to$ Pellets.
- **Fast Pivot Clockwise Rotation & Audio**:
  - Rotates by `rotation_per_burst_deg = 13.5` ($+0.2356\text{ rad}$) per burst over `bursts = 24` sequential bursts spaced by `burst_interval = 0.070s` ($\approx 4.2$ frames at 60 fps, $\sim 1.6\text{s}$ total cast duration). Calibrated to match Touhou 09 with airy radial bullet spacing and seamless column alignment between pellets and stars.
  - `sfx`: Plays authentic `se_tan00` on each rotating burst wave.
- **Curved Angular Fan**: At each spoke, bullets spawn in a curved circular arc / angular fan with `fan_spread_deg = 6.5` rather than a flat transverse bar, naturally curving along concentric circles and expanding smoothly outward along individual radial rays.
- **Rank Scaling**:
  - Bullets per spoke: `bullets_per_vector_rank_1 = 2` ($2 \times 6 = 12$ bullets/burst; 288 total) scaling up to `bullets_per_vector_rank_16 = 4` ($4 \times 6 = 24$ bullets/burst; 576 total).
  - Projectile speed: `speed_rank_1 = 160.0 px/s` scaling up to `speed_rank_16 = 340.0 px/s`.

### `DanmakuStarSprayStep` (`scripts/resources/danmaku_star_spray_step.gd`)
- **ECL parity (`pl01.ecl` `sub4`, Marisa boss spell 4)**: `zun_mode` streams `60 + rank` stars, one per 60 fps frame, at random angles and `187.5 + 12.5 x rank` px/s. Each star turns at a random rate within +-1.57 rad/s and slows by a random 0-75 px/s^2 for 2s, then flies straight (`CURVE_THEN_LINE` only accelerates during the curve). `yellow_star` lifetime is 20s because the slowest stars drop to ~50 px/s.
Emits a wide omnidirectional spray of curving yellow stars from the caster's center matching Marisa's Attack 4 ("Star Spray" / Magic Sign "Illusion Star").
- **Barrage Composition & Audio**:
  - Fires 60 yellow stars (`yellow_star.tres`) across 6 rapid burst waves (10 stars/wave spaced by $0.08\text{s}$ interval, total burst duration $0.48\text{s}$).
  - `sfx`: Plays authentic `se_tan00` on each burst wave.
- **Continuous Sprite Spinning**: Each star continuously spins around its center at `star_spin_speed = 6.0 rad/s` ($\approx 344^\circ/\text{s}$, $\sim 1\text{ rev/s}$) in the direction of its curve (clockwise for right hemisphere, counter-clockwise for left hemisphere), continuing through its straight flight.
- **Curving Dynamics (`MotionMode.CURVE_THEN_LINE`)**:
  - Stars curve outward for `curve_duration = 1.1s` at an angular velocity of $\approx 1.25\text{ rad/s}$ ($~79^\circ$ angular deflection).
  - Stars traveling towards the right hemisphere ($\text{dir}.x \ge 0$) curve clockwise ($+$), while stars traveling towards the left hemisphere ($\text{dir}.x < 0$) curve counter-clockwise ($-$), creating the characteristic blooming bell/umbrella fountain.
  - After `curve_duration`, angular rotation stops and each star continues traveling indefinitely along a straight linear trajectory.
- **Rank Scaling**:
  - Star count is **constant at exactly 60 yellow stars** across all ranks (Rank 1 to 16).
  - Projectile speed scales drastically with rank: `speed_rank_1 = 200.0 px/s` up to `speed_rank_16 = 520.0 px/s` ($\pm 15\%$ speed variance for organic depth).

### `DanmakuFixedLasersStep` (`scripts/resources/danmaku_fixed_lasers_step.gd`)
- **ECL parity (`pl01.ecl` `sub6`, Marisa boss spell 5)**: `zun_bracket` drops `rank / 5 + 3` lasers `bracket_offset = 133.3` px (64 ECL units) beside the victim's X at that moment, alternating sides, spread over `window_frames = 120`. The fixed columns are not in the ECL.
Spawns sequential vertical green laser beams (`earth_light_ray_green.tscn`) at fixed horizontal positions corresponding to the quarter-splits of the playfield.
- **Fixed Position Geometry & Audio**:
  - Anchors along the bottom ground border: `ground_y = 950.0 px`.
  - Fixed horizontal positions: `laser_x_positions = [150.0, 450.0]`, precisely quarter-splitting the $600\text{ px}$ wide playfield ($0.25 \times 600$ and $0.75 \times 600$).
  - Strictly vertical: `tilt_angle_deg = 0.0` ($0^\circ$).
  - Audio: Plays authentic `se_lazer00` on beam discharge via `EarthLightRay` with full polyphonic overlapping across consecutive lasers.
- **Color Customization (`earth_light_ray_green.tscn`)**:
  - Pre-configured emerald glow `Color(0.18, 0.98, 0.32, 0.45)`, mint-white core `Color(0.92, 1.0, 0.92, 0.95)`, lime base flare `Color(0.35, 1.0, 0.45, 1.0)`, and runic green `Color(0.55, 1.0, 0.65, 1.0)` to distinguish from standard blue Earth Light Rays.
- **Sequential Stagger & Pulsing**:
  - Consecutive lasers in each wave are staggered by `stagger_interval = 0.22s`.
  - Successive waves / pulses are separated by `pulse_interval = 0.65s`.
  - Alternating firing order: `alternate_order = true` reverses execution sequence on each pulse (Pulse 1: Left $\to$ Right; Pulse 2: Right $\to$ Left; Pulse 3: Left $\to$ Right).
### `DanmakuDescendingStripsStep` (`scripts/resources/danmaku_descending_strips_step.gd`)
- **ECL parity (`pl03.ecl` `sub0` Lv2, `sub1` Lv3)**: `zun_mode` fires `rank / 4 + 3` strips of `rank / 8 + 12` (Lv3: `+ 10`) bullets, one per frame, 12 frames between strips, from y 0 at `x = i x 600 / n`, starting at the wall on the **victim's own half**. Fast type 250 + 12.5 x rank px/s, slow type 90% of that (Lv2: pellet slow / arrowhead fast; Lv3: arrowhead slow / sword fast); slow when (bullets left + strips left) is even. Drift +-2.8125 (Lv2) / +-1.40625 (Lv3) degrees. The ECL colours Lv3's sword DarkBlue; ours stays green pending footage.
Emits horizontal strips of green pellets and green arrowheads side-by-side, spawning offscreen above the opponent's playfield and descending with individual directional drifts.
- **Offscreen Spawning & Accelerated Spawn Timing**:
  - `spawn_y = -30.0 px`: Spawns above the visible playfield window and enters smoothly through the top border.
  - `delay_between_bullets = 0.011s`: Rapid sequential emission across the strip with outside-inward sweep (`AUTO_FROM_OUTSIDE`).
  - `delay_between_strips = 0.21s`: Rapid wave interval between successive descending strips.
- **Side-by-Side Bullets & Speed Differential**:
  - Pellets (`green_pellet.tres`, cancelable) and arrowheads (`green_arrow.tres`, non-cancelable) start at the exact same height side-by-side in each strip.
  - Arrowheads are slightly faster (`arrow_speed = 285.0 px/s`) than pellets (`pellet_speed = 250.0 px/s`), naturally pulling ahead during flight to create leading arrowhead waves followed by trailing cancelable pellets.
  - `alternate_bullet_types = true`: Alternates which bullet type starts at column 0 between successive strips.
- **Individual Bullet Drift**:
  - `drift_angle_max_deg = 3.0°`: Rather than the whole strip drifting uniformly, each *individual danmaku bullet* inside the strip receives its own subtle angle deviation (e.g. bullet 1 straight, bullet 2 slight right, bullet 3 slight right, bullet 4 slight left), creating an organic dispersion pattern across the descending wave.
- **Rank Scaling (Strips & Projectile Speeds)**:
  - `strips_min = 3`: Launches 3 strips (33 bullets total) at Rank 1.
  - `strips_max = 7`: Launches 7 strips (77 bullets total) at Rank 16.
  - `pellet_speed_rank16` & `arrow_speed_rank16`: Optional rank-scaled speed ceilings. When set (e.g. in Level 3), speeds interpolate smoothly from base speed at Rank 1 up to $1.5\times$ speed at Rank 16. If left unset (`<= 0.0`), speeds remain fixed across all ranks (e.g. Level 2).
- **Audio**:
  - `sfx = "se_tan00"`: Plays authentic `se_tan00` sound effect for every single bullet spawn in the strip with throttle bypass to preserve the rapid machine-gun cadence.

### `DanmakuKnifeFanStep` (`scripts/resources/danmaku_knife_fan_step.gd`)
- **ECL parity (`pl03.ecl` `sub5`)**: `zun_rank_waves` gives `rank / 4 + 5` fans, growing by one knife each, 11.25 degrees apart, 10 frames apart, aimed once, 262.5 -> 450 px/s, no charge-up pause.
Youmu's Level 4 Boss Attack: charges energy and fires off a sequential series of expanding dagger waves. Each wave contains one more knife than the last, distributed evenly along an expanding fan formation to construct a crisp, descending triangular wedge.
- **Wedge Geometry & Angular Spread**:
  - `angle_step_deg = 9.6°`: Constant angular separation between adjacent daggers in each wave.
  - Wave $w$ (0-indexed) spawns $w + 1$ daggers. Outermost angle is $\pm \frac{w}{2} \cdot 9.6^\circ$.
  - Symmetrical fan centered on the initial aim angle towards the opponent.
- **Timing & Audio**:
  - `initial_charge_delay = 0.35s`: Boss pauses in sword swing pose before wave 0 fires (silent wind-up).
  - `wave_delay_r1 = 0.16s` down to `wave_delay_r16 = 0.12s`: Sequential wave emission sequence with authentic bullet sounds (`se_tan00`).
  - `sfx = "se_tan00"`: Configurable sound effect played on dagger wave launches.
- **Rank Scaling**:
  - `waves_min = 5` at Rank 1 ($1 + 2 + 3 + 4 + 5 = 15$ daggers total).
  - `waves_max = 9` at Rank 16 ($1 + 2 + \dots + 9 = 45$ daggers total).
  - Projectile speed scales smoothly from `speed_r1 = 215.0 px/s` up to `speed_r16 = 275.0 px/s`.

### `DanmakuKnifeStreamStep` (`scripts/resources/danmaku_knife_stream_step.gd`)
- **ECL parity (`pl03.ecl` `sub3`)**: `rank + 10` swords re-aimed each shot, 8 frames apart, 350 -> 725 px/s (`2.6 + 0.2 x rank`), no charge-up pause.
Youmu's Level 4 Boss Attack: charges energy and rapidly fires an aimed stream of blue daggers directly at the opponent's position.
- **Dynamic Aim Tracking**:
  - `track_player_per_knife = true`: Rather than locking in a single target position at cast time, each individual dagger independently evaluates the opponent's current location at the exact instant of its launch.
  - When the player streams or dashes, the barrage cleanly trails their movement trajectory.
- **Timing & Audio**:
  - `initial_charge_delay = 0.35s`: Pre-barrage gathering pause (silent wind-up).
  - `delay_r1 = 0.14s` down to `delay_r16 = 0.075s`: Interval between consecutive dagger launches.
  - `sfx = "se_tan00"`: Configurable sound effect played on dagger launches.
- **Rank Scaling**:
  - `knives_min = 9` daggers at Rank 1.
  - `knives_max = 25` daggers at Rank 16.
  - Bullet speed scales from `speed_r1 = 260.0 px/s` up to `speed_r16 = 400.0 px/s`.

### `DanmakuTimeStopFanStep` (`scripts/resources/danmaku_time_stop_fan_step.gd`)
- **ECL parity (`pl02.ecl` `sub0` Lv2, `sub1` Lv3)**: both accelerate at `(1 + 0.1 x rank) / 60` px/frame^2 (137.5 -> 325 px/s^2) for only 40 frames (`accel_duration`), so top speed is 91.7 -> 216.7 px/s. Lv2 adds `zun_rank_delay` (`int(40 / (rank x 3/4 + 6))` frames between spokes) and `aim_arc_at_point` (arc centred on the victim-to-(300, 466.7) direction, spokes from -64.3 to +51.4 degrees). Lv3 keeps its 4-frame delay and existing arc: ZUN aims it off the victim's own movement during the warning, which does not port cleanly.
Sakuya's Level 2 & 3 Spellcard: Time Sign "Private Square" (時符「プライベートヴィジョン」). Both levels share every timing/freeze/audio/dim mechanic below unchanged - only the per-spoke bullet geometry differs, toggled by `stacked_cross_mode`.
- **Field-Wide Freeze (distinct from the generic 0.58s declaration pause)**:
  - After a short `pre_freeze_delay = 0.15s` beat, the target field's `entities_layer` (fairies, boss, Lily White) and `bullets_layer` (every bullet, incoming and outgoing) are set to `PROCESS_MODE_DISABLED` for `freeze_duration = 1.0s`, freezing everything in place with no per-entity bookkeeping.
  - The victim's own `Player` node is exempted from that disable (`process_mode = PROCESS_MODE_ALWAYS`) and instead individually frozen via the existing `set_action_stop(true)` flag - **except when the victim is also playing Sakuya**, reproducing the authentic PoFV quirk where Sakuya's time-stop never affects another Sakuya.
- **Staggered, Accelerating Daggers (plain `LINEAR` motion, not `EXPANDING_ORBIT`)**:
  - Both levels spawn one spoke group at a time (`delay_between_daggers = 4/60s = 0.0667s` apart, matching ZUN's `wait(4);`) across a `arc_span_deg = 115.7°` arc (ZUN's `0.2244 rad * 9 gaps`) centered directly above the victim, each spoke at a fixed point `knife_radius = 133.0px` out from the victim's position (scaled from ZUN's `F3 = 64.0px` by our $2.083\times$ playfield ratio).
  - **Lv2 geometry (`stacked_cross_mode = false`)**: 10 `sakuya_knife.tres` daggers, one per spoke, each paired with a `red_pellet.tres` at `pellet_radius = 167.0px` (scaled from ZUN's `F6 = 80.0px`). Both fly `inward` (toward the victim).
  - **Lv3 geometry (`stacked_cross_mode = true`)**: 10 spokes, each a 4-dagger cross (`sakuya_knife.tres` only, no pellets) spawned at the same `knife_radius` point (`bullet_circle_aimed(Knife, DarkRed, 4, ...)`). The 4 daggers form a 4-way cross (inward toward victim, outward away, and two tangential). During the time-stop, the interlocking crosses form a dense canopy above the victim; once the freeze lifts, each dagger gently accelerates along its own heading.
  - Every dagger (and Lv2's pellet) spawns via `spawn_danmaku_bullet(..., MotionMode.LINEAR, direction, 0.0, current_accel, max_speed)` - starting at rest and gently accelerating (`accel_r1 = 125.0 px/s²` up to `accel_r16 = 325.0 px/s²`, scaled from ZUN's `F4 = (Rank * 0.1 + 1.0) / 60.0 px/frame²`) up to top speed (`max_speed_r1 = 260 px/s` up to `max_speed_r16 = 340 px/s`).
  - **Rotation fix**: `sakuya_knife_red.png`'s native art points nose-RIGHT, so `rotation_offset_deg` is set to `-90°` on `sakuya_knife.tres` so the tip leads each dagger's actual `LINEAR` travel direction.
- **Both Playfields Dim While the Pattern Forms**: `Playfield.set_dim(target_alpha, duration)` snaps a full-field black `ColorRect` overlay (`%DimOverlay`) instantly to `dim_alpha = 0.35` right as the first dagger spawns, then snaps it back to `0.0` right as the freeze lifts and the daggers actually start moving. Total freeze duration from first dagger spawn through the 20-frame post-formation hold is exactly 60 engine ticks ($1.0\text{s}$).
- **Audio**: `se_timestop0` on freeze-start, `se_tan00` on launch, shared with Cirno's freeze vocabulary.
- **Rank Scaling**: `dagger_count = 10` fixed at every rank for both levels. Acceleration scales from `125.0 px/s²` (Rank 1) to `325.0 px/s²` (Rank 16), and top speed scales from `260.0 px/s` to `340.0 px/s`.
- **Reference**: Bytecode verified against decompiled `pl02.ecl` (`sub0()` for Lv2, `sub1()` for Lv3).

### `DanmakuOvalKnifeStep` (`scripts/resources/danmaku_oval_knife_step.gd`)
- **ECL parity (`pl03.ecl` `sub4`)**: `zun_mode` replaces the ellipse with what ZUN does: `rank + 10` pairs, 8 frames apart, fired at aim -+ 90 degrees (aim fixed at the start) at 237.5 -> 425 px/s, each turning back toward the aim line at 1.57 rad/s for a random 60-119 frames (shared per pair), then straight. The ellipse fields below are the pre-ECL mode.
Youmu's Level 4 Boss Attack 3: Oval of Blue Daggers (迷符「半身大悟」).
- **Dual-Stream Horizontal Ellipse ("Lying on its side")**:
  - Daggers spawn symmetrically in pairs from Youmu's left and right sides and travel around a wide horizontal ellipse ($radius\_x > radius\_y$) extending downwards into the playfield.
  - Uses `MotionMode.ELLIPSE_THEN_LINE` in `DanmakuBullet`: orientation automatically tracks the tangent vector along the curve.
- **Pre-Apex Split & Tangent-Aligned Fan-Out**:
  - Before reaching the bottom apex (around $58^\circ$ along the oval, leaving a wide $\sim 200\text{ px}$ gap), each dagger transitions into linear flight without touching or crossing at the bottom center.
  - Instead of snapping downward toward the screen bottom, daggers continue pointing along their natural heading on the oval: right stream continues down-left, left stream continues down-right.
  - Each knife independently veers slightly left or right ($\pm [8^\circ, 20^\circ]$) relative to its tangent heading, spreading into an intersecting criss-cross curtain.
- **Timing & Audio**:
  - `initial_charge_delay = 0.35s`: Pre-barrage pause (silent wind-up).
  - `sfx = "se_tan00"`: Configurable sound effect played on dagger pair launches.
- **Premature Despawn Protection**:
  - While traversing the ellipse in `ELLIPSE_THEN_LINE`, bullets bypass outer playfield margin checks, ensuring Rank 16 knives that curve near the screen edges complete their journey without despawning prematurely.
- **Rank Scaling**:
  - **Rank 1**: 11 daggers per side (22 total), oval radii $(230, 145)\text{ px}$, speed $225\text{ px/s}$, delay $0.075\text{s}$.
  - **Rank 16**: 26 daggers per side (52 total), oval radii $(400, 260)\text{ px}$ (1.5x larger oval), speed $410\text{ px/s}$, delay $0.035\text{s}$.

### `DanmakuFreezeRingStep` (`scripts/resources/danmaku_freeze_ring_step.gd`)
- **ECL parity (`pl05.ecl` `sub0` Lv2, `sub1` Lv3)**: `zun_speeds` rank-scales the ring (187.5 + 12.5 x rank px/s), puts Lv3's slow ring at the midpoint between that and 125, and fires the freeze 60 frames after the ring (radius = speed x 1s). Lv2 is one alternating ring of `4 x rank + 64` with no second ring; Lv3 two icicle rings of `2 x rank + 48`. Each wave takes the shared random angle. Scatter is unchanged: ZUN hands it to a built-in routine (`__Cirno_BulletSpecial`) whose speed and hang time are not in the script.
Cirno's Level 2 & 3 Spellcards: Freeze Sign "Perfect Freeze" (shared step, reconfigured per level). Spawns a ring of bullets in `MotionMode.EXPANDING_ORBIT` (angular_speed = 0.0, pure radial growth) from a fixed point near the top of the playfield. Once the leading ring crosses a critical radius, it triggers a **playfield-wide freeze sweep** via the new `Playfield.freeze_and_scatter_bullets(accel, max_speed)` / `DanmakuBullet.freeze_and_scatter()` / `EnemyPellet.freeze_and_scatter()` methods.
- **Spawn Position Override**: `spawn_y_override = 260.0` overrides the default Lv2/3 cast Y position (`Playfield.execute_spellcard()` defaults to `Y = 110.0`, near the very top border) so the ring forms low enough to actually threaten the player's movement lane instead of mostly expanding off the top of the screen. Scoped to this step only (set negative to fall back to the caller-supplied origin) - does not change the shared default used by other characters' Lv2/3 spellcards.
- **Bullets Per Ring (Rank-Scaled)**: `bullets_per_ring_min` (Rank 1) to `bullets_per_ring_max` (Rank 16), same interpolation as `DanmakuRingStep`. Lv2 sets both to 80 (constant across all ranks); Lv3 scales 56 (Rank 1) to 88 (Rank 16).
- **Odd-Even Ring Composition (Single Ring, Rank < `second_ring_min_rank`)**:
  - Lv2 only: alternates `blue_icicle.tres` (even index) and `blue_pellet_small.tres` (odd index), 40 of each at Rank 1.
- **Second Ring (Rank >= `second_ring_min_rank`)**:
  - Lv2 (`second_ring_min_rank = 9`): once joined, a second ring of pure icicles spawns at the same angular positions, and the *primary* ring also switches to pure icicles (no more pellets).
  - Lv3 (`second_ring_min_rank = 1`): both rings are pure icicle and present from Rank 1 onward - there is no single-ring/odd-even phase at all.
  - In both cases, the second ring expands at `radial_speed * slow_ring_speed_factor` (default 0.6x). Because both rings are caught by the same freeze trigger, the slower ring is still mid-expansion (smaller) at that moment, producing a natural separation between the two rings without any extra bookkeeping.
- **Freeze Trigger (Leader Bullet Pattern)**:
  - Only the very first bullet spawned (the primary/faster ring's `i = 0` bullet) is marked as the "leader": it alone receives `freeze_trigger_radius`, `freeze_accel`, `freeze_max_speed`, and an `owner_playfield` back-reference. All other ring bullets are passive followers.
  - Each physics frame, the leader checks its own `current_radius` in `DanmakuBullet._physics_process()`. Once it reaches `freeze_trigger_radius` (default 240.0 px), it clears its own trigger (one-shot guard) and calls `owner_playfield.freeze_and_scatter_bullets(accel, max_speed)`.
- **`Playfield.freeze_and_scatter_bullets(accel, max_speed)`**: Iterates `get_active_bullets()` (which already covers **both** `pellet_pool` (`EnemyPellet`, i.e. ordinary fairy attacks) and `danmaku_bullet_pool` (`DanmakuBullet`)) and calls `freeze_and_scatter()` on every entity that supports it — this is what catches bullets "not from the spellcard" per the authentic PoFV attack.
- **`freeze_and_scatter(accel, max_speed)` (mirrored on both bullet classes)**: Reassigns a random direction (`randf_range(0.0, TAU)`), resets `speed = 0.0`, sets `acceleration = accel` and `max_speed = max_speed` (both classes already support acceleration/max_speed ramping in their existing `LINEAR`-style motion, reused as-is), and recolors the bullet white. Since bullet art with saturated colors baked into its pixels can't be turned white via `modulate` alone (multiplying a zero channel stays zero), this swaps `sprite.texture` to a genuine pre-colored white asset instead: `DanmakuBulletData.frozen_texture` (set on `blue_icicle.tres` -> `tex_icicle_white.tres` and `blue_pellet_small.tres` -> the existing `tex_pellet_white.tres`) for `DanmakuBullet`, and the existing `tex_pellet_white.tres` directly for `EnemyPellet` (skipped for ring pellets, whose `RING_TEX` is already a white/glowing asset tinted via `modulate`).
- **Rank Scaling**: See "Bullets Per Ring" above for count. Ring count (1 vs 2) scales via `second_ring_min_rank`. Expansion speed, freeze trigger radius, and scatter acceleration are constant across ranks; `scatter_max_speed` is a fixed per-level value (Lv2: 160.0, Lv3: 220.0 - higher to compensate for Lv3's lower bullet count).
- **Audio**: Plays `se_tan00` once per cast.

### `DanmakuInterweavingIciclesStep` (`scripts/resources/danmaku_interweaving_icicles_step.gd`)
- **ECL parity (`pl05.ecl` `sub6`)**: `zun_mode` fires two rings of `rank + 32` icicles (the second on the half steps) at 250 px/s; each brakes to a stop over 1s (`AimRel`), turns 90 degrees from its own heading (the rings opposite ways) and relaunches at 250. Uses `DECEL_AND_HOME` with `relaunch_relative`. The expand/hold/breakout fields below are the pre-ECL mode.
Cirno's Level 4 Boss Attack 2: "Interweaving Icicles". Spawns two fully-overlapping circles of `cyan_icicle.tres` (identical angular positions each moment, since both share `angular_speed = 0.0` during expansion) that grow outward, hang motionless once they reach a target radius, then each circle's icicles rotate their heading to a tangential direction (one circle to its relative left, the other to its relative right) and accelerate forward from a stop - unraveling into two counter-spiraling arms that weave past each other as they expand.
- **New Engine Capability - `DanmakuBullet` "Orbit Breakout"**: Extended `MotionMode.EXPANDING_ORBIT` (previously pure continuous radial/angular growth, used unmodified by `DanmakuDoubleRingStep` and `DanmakuFreezeRingStep`) with an optional 3-phase lifecycle gated on the bullet's own `_current_lifetime` (already tracked for the despawn timer, reused here for free):
  1. **Expand** (`orbit_freeze_time` not yet reached): radius/angle advance exactly as before.
  2. **Hold** (`orbit_freeze_time <= lifetime < orbit_breakout_time`): radius and angle freeze in place; the bullet hangs motionless.
  3. **Breakout** (`lifetime >= orbit_breakout_time`, one-shot transition): `direction` is set to the bullet's current radial-outward heading rotated by `orbit_breakout_angle_offset`, `motion_mode` switches to `LINEAR`, and `speed`/`acceleration`/`max_speed` are set to `orbit_breakout_initial_speed` (0.0 here) / `orbit_breakout_accel` / `orbit_breakout_max_speed` - reusing the exact same acceleration-ramp fields and `LINEAR` handling already used by `freeze_and_scatter()`, so no new motion mode was needed for the actual flight-out.
  - All new fields default to `-1.0`/`0.0` (disabled), so `setup_expanding_orbit()` and `spawn_danmaku_bullet_expanding_orbit()` gained the fields as trailing optional parameters - existing callers are unaffected and behave identically to before.
- **Overlap, Not Interleave**: Unlike `DanmakuDoubleRingStep` (which offsets its two rings by a half-step to zipper them together), both circles here spawn at the *exact same* angular positions with `initial_radius = 0.0`, so they stay perfectly coincident throughout the entire expand and hold phases - true circumference overlap rather than an interleaved pattern. They only visually separate once the breakout fires.
- **Breakout Angle**: `breakout_angle_deg = 90.0` (tangent to the circle) applied as `-angle_offset` for the "relative left" circle and `+angle_offset` for the "relative right" circle.
- **Rank Scaling**: `count_min = 36` / `count_max = 52` per circle (72/104 total) - reuses `DanmakuDoubleRingStep`'s exact default counts. Expansion speed scales `150.0 -> 180.0 px/s`; breakout acceleration `560.0 -> 680.0 px/s^2`; breakout terminal speed `340.0 -> 390.0 px/s`. Target radius (180px) and hold time (0.25s) are constant across ranks.
- **Audio**: Plays `se_tan00` once per cast.

### `DanmakuIcicleShowerStep` (`scripts/resources/danmaku_icicle_shower_step.gd`)
- **ECL parity (`pl05.ecl` `sub3`)**: `rank + 30` icicles, one per frame, falling at a random 125-437.5 px/s (not rank-scaled), spawned at a random angle and a random 0-133 px distance from the boss (`spawn_radius`).
Cirno's Level 4 Boss Attack 3: "Shower of Icicles". Spawns `blue_icicle.tres` one at a time in sequence (not a single instantaneous burst) at randomized positions scattered around the boss's own position, each independently falling straight down (`MotionMode.LINEAR`, `Vector2.DOWN`) at its own randomly-rolled speed. No new engine capability was needed - the existing `LINEAR` mode, per-bullet rotation (`rotate_with_direction`), and playfield-exit despawn check already cover a straight-falling bullet.
- **Sequential Spawning**: `delay_between_spawns = 0.02s` between each icicle (`await playfield.get_tree().create_timer(...).timeout` between loop iterations, same pattern as `DanmakuExtraAttackStep`) - the icicles pop out one after another rather than all appearing on the same frame.
- **Randomized Spawn Spread**: Each icicle's spawn position is `origin + Vector2(randf_range(-spawn_offset_x, spawn_offset_x), randf_range(spawn_offset_y_min, spawn_offset_y_max))` (`±108px` horizontally, `-24px` to `+192px` vertically - the original estimate widened 1.2x per user feedback) - a wide band around and below the boss, matching the reference footage's already-scattered cluster at the moment of cast rather than a single point.
- **Per-Bullet Speed Randomization**: Each icicle independently rolls `randf_range(fall_speed_min, cur_speed_max)` for its own constant fall speed. Combined with the sequential spawn stagger, the spread in speeds is what fans the cluster out into a cascading shower as they descend - faster icicles pull ahead, slower ones lag behind.
- **Rank Scaling**: Bullet count is a constant 55 at every rank (`count_min = count_max = 55` - this attack doesn't scale density). `fall_speed_min = 140.0 px/s` is fixed across ranks; `fall_speed_max` scales `230.0 px/s` (Rank 1) → `320.0 px/s` (Rank 16), so only the fastest icicles get faster at higher rank.
- **Audio**: Plays `se_tan00` once per cast (matching the convention used by other multi-bullet steps like `DanmakuRingStep`, rather than once per icicle).
- **Testing Note**: Because the sequential delay uses a real `SceneTreeTimer`, the automated test in `test_boss_character.gd` verifies the full 55-bullet spawn count and speed variance against a `duplicate()` of the step with `delay_between_spawns` zeroed out (bypassing the per-icicle `await` so the loop completes synchronously within the test) - the field value itself (`delay_between_spawns == 0.02`) is asserted separately against the real, unmodified resource.

### `DanmakuPelletDaggerStep` (`scripts/resources/danmaku_pellet_dagger_step.gd`)
- **ECL parity (`pl02.ecl` `sub6` + `sub8`)**: dagger 250 px/s. Trail is one pellet every 2 frames, `50 + 2 x rank` times, placed on a circle of 66.7 px shrinking 2% per pellet, each accelerating for 2s to `130 -> 205` px/s +-62.5 (`pellet_bursts_*`, `pellet_radius_decay`, `pellet_speed_jitter`). `sakuya_dagger_pellet` lifetime is 16s for the slow ones.
Sakuya's Level 4 Boss Attack 1: Time Sign "Mysterious Jack" (時符「ミステリアスジャック」) - Pellet-Spewing Dagger. Spawns exactly **one** cyan dagger per `execute()` call, aimed once at the opponent's position at the moment of the throw (a snapshot, not continuous tracking) - the boss's own hop/cast AI loop (`BossData.attack_patterns`) is what produces the repeated "reposition, then throw another dagger" behavior seen in reference footage, not this step.
- **New Engine Capability - `DanmakuBullet` Periodic Pellet Trail**: Added `pellet_spawn_interval`/`pellet_spawn_data`/`pellet_spawn_count`/`pellet_spawn_radius`/`pellet_spawn_accel_time`/`pellet_spawn_max_speed` fields to `DanmakuBullet` (default-disabled at `pellet_spawn_interval = -1.0`, reset alongside every other per-instance field in `on_pool_acquire`/`on_pool_release`). While a bullet has these set and is alive, `_physics_process` ticks a timer and every `pellet_spawn_interval` seconds calls `_spawn_pellet_burst()`, which spawns `pellet_spawn_count` fresh pellets scattered within `pellet_spawn_radius` of the bullet's *current* (already-moved) position via the standard `owner_playfield.spawn_danmaku_bullet(...)` pipeline - each pellet starts at rest with a random direction (`randf_range(0.0, TAU)`) and accelerates (`acceleration = pellet_spawn_max_speed / pellet_spawn_accel_time`) up to `pellet_spawn_max_speed`, the same random-direction-accel recipe `freeze_and_scatter()` already applies to an *existing* bullet, just spawning brand new ones repeatedly instead.
- **Dagger Kinematics - Deliberately NOT Rank-Scaled**: `dagger_speed = 300.0 px/s` fixed at every rank, identical to the Extra Attack's own daggers. Frame-by-frame pixel tracking of both `sakuya_level4_rank1.mp4` and `sakuya_level4_rank16.mp4` reference footage measured the dagger's own travel speed at ~4.4-4.7 px/frame in *both* clips - no measurable rank difference - so unlike most other Danmaku steps, this one intentionally has no `speed_r1`/`speed_r16` pair for the dagger itself. Only the pellet trail scales with rank.
- **Pellet Trail Rank Scaling**: `pellet_spawn_interval = 0.15s` (fixed) between bursts. Pellets per burst scale `pellets_per_burst_min = 3` (Rank 1) to `pellets_per_burst_max = 5` (Rank 16); pellet top speed scales `pellet_speed_min = 140.0 px/s` (Rank 1) to `pellet_speed_max = 190.0 px/s` (Rank 16). Each pellet takes `pellet_accel_time = 1.0s` to reach its top speed. `pellet_spawn_radius = 42.0px` (not rank-scaled) - sized to cover the dagger sprite's own visual extent (~70×45px at `base_scale = 2.25`) plus a small margin, rather than clustering tightly at its exact center point.
- **Bullet Resources**: `sakuya_knife_cyan.tres` (dagger) + `sakuya_dagger_pellet.tres` (trail pellets - same plain white pellet art as `white_pellet.tres`, matching the reference footage, but with `spawn_fade_in = true` so each burst blooms in from oversized/transparent to full size/opacity instead of popping in instantly).
- **Testing**: No full `sakuya_boss.tres` roster existed yet before this attack, so a minimal one was created (`hframes = 4`, `vframes = 3`, matching `sakuya_boss.png`'s $256\times240$ sheet at the same $64\times80$-per-frame grid every other boss sheet uses) with `attack_patterns` containing only this one spell so far, `attacks_min = 5`/`attacks_max = 9` (PoFV's usual per-visit attack quota) so the boss's own hop loop repeats and repositions between throws exactly like the reference footage. Movement/hop/duration fields were copied from `cirno_boss.tres` unchanged as a starting point (same sprite sheet geometry) and are expected to be tuned later as more attacks are added.

### `DanmakuBouncingKnifeRingStep` (`scripts/resources/danmaku_bouncing_knife_ring_step.gd`)
- **ECL parity (`pl02.ecl` `sub3`)**: `zun_rank_timing` gives `rank / 2 + 10` circles `int(60 / count)` frames apart; speed 155 -> 230 px/s; `zun_offset` reproduces ZUN drawing a separate random angle for x and y of the circle offset (radius up to 133.3 px). `bounce_overshoot` is still the footage value; the ECL bounces at the wall.
Sakuya's Level 4 Boss Attack 2: Reflective Knife Explosion (part of the same authentic Time Sign "Mysterious Jack" spell as Boss Attack 1 above, split into its own `boss_spell_N.tres` per this project's one-pattern-per-resource convention). Summons `circle_count` small circles of `daggers_per_circle` purple daggers each, one circle at a time (`delay_between_circles` apart), every circle independently spawned at its own randomized point near the boss. Any dagger that travels `bounce_overshoot` px past the left, right, or top playfield edge ricochets back inward exactly once, turning blue in the process and flying straight from there on - daggers reaching the bottom edge (where the player is) are never reflected, they just exit normally.
- **New Engine Capability - `DanmakuBullet` Wall Bounce**: Added `bounce_off_walls`/`bounce_bullet_data`/`bounce_overshoot` fields to `DanmakuBullet` (default-disabled, reset in `on_pool_acquire`/`on_pool_release` like every other per-instance field). Each physics frame, `_process_wall_bounce()` checks the bullet's already-moved position against the playfield edges offset outward by `bounce_overshoot` (new `PLAYFIELD_EDGE_LEFT/RIGHT/TOP` constants, minus/plus the overshoot - deliberately tighter than the existing generous `SAFETY_*`/despawn margins by default, but lets the bullet travel a little past the visible edge first when `bounce_overshoot > 0`) and, if the bullet is currently heading into an edge it has reached, mirrors that velocity component (`direction.x = -direction.x` for left/right, `direction.y = -direction.y` for top only - never checked against the bottom) and clamps position back onto that (possibly overshot) boundary. On a bounce, `bullet_data` is swapped to `bounce_bullet_data` (if set), `_apply_visuals()`/`_update_rotation()` re-run to pick up the new texture/tint/rotation-offset, and `bounce_off_walls` is immediately cleared - **it only ever bounces once**; the resulting blue dagger flies straight afterward and exits normally at any further edge, including the bottom. Only wired up for `MotionMode.LINEAR` (the only mode this attack uses).
- **Per-Circle Randomized Spawn Points ("in a random area around herself")**: Not a single ring - `circle_count = 10` separate small circles, each independently spawned at `boss.position + a random point inside spawn_offset_radius` (`110.0px`), rolled fresh per circle per cast, plus its own random start angle. The 10 scattered, independently-rotated circles naturally overlap and interleave with each other, which is what produces the dense, layered cluster look in reference footage - not a single dense ring or deliberate per-spoke dagger pairing.
- **Circle Geometry - Fixed at Every Rank**: Each circle is `daggers_per_circle = 7` daggers evenly spaced ($360°/7 ≈ 51.4°$ apart, `MotionMode.LINEAR`, all 7 in a circle launched simultaneously with no hold/freeze phase). Both `daggers_per_circle` and `circle_count` are fixed at every rank (70 daggers total, always) - confirmed by the user ("there are 10 circles on both ranks").
- **Sequential Circle Emission**: Circles appear one at a time rather than all at once - `delay_between_circles = 0.1s` between each (`await playfield.get_tree().create_timer(...).timeout` between loop iterations, same pattern as `DanmakuKnifeStreamStep`/`DanmakuIcicleShowerStep`), `se_tan00` playing on each circle's emission.
- **Bounce Overshoot**: `bounce_overshoot = 40.0px` - daggers travel a little past the playfield edge (briefly offscreen) before ricocheting back inward, rather than reflecting exactly at the boundary line.
- **Rank Scaling**: Only speed scales - `speed_min = 230.0 px/s` (Rank 1) up to `speed_max = 390.0 px/s` (Rank 16, a 1.7x increase) - unlike Boss Attack 1, the user confirmed this attack's daggers do visibly speed up with rank. Originally matched the Pellet-Spewing Dagger's 300 px/s at Rank 1 (a 1.3x ratio to Rank 16, matching `DanmakuTimeStopFanStep`'s Sakuya daggers), but the user found that gap imperceptible in testing, so Rank 1 was slowed further to widen it.
- **Bullet Resources**: `sakuya_knife_purple.tres` (pre-bounce) + `sakuya_knife_blue.tres` (post-bounce) - both new resources (now baked into the atlas by `tools/bake_bullets_into_atlas.gd`) following the same convention as `sakuya_knife.tres`/`sakuya_knife_cyan.tres`.
- **Per-Circle Spawn Burst VFX (`scenes/effects/knife_explosion_burst.gd`/`.tscn`)**: Purely visual, no hitbox - one instance spawned at each circle's `circle_center` (same instant as that circle's 7 daggers, before the per-dagger loop) via the same `effects_layer`-lookup/`instantiate()`/`add_child()` pattern as `DanmakuIcicleShowerStep`'s `IcicleSpawnBurst`. Starts at `2.2x` scale and fully transparent, tweens down to `1.0x` scale while fading in to full opacity over `0.15s` (`TRANS_QUAD`/`EASE_OUT`, the same "shrink down while fading in" recipe as `DanmakuBulletData.spawn_fade_in` on the Pellet-Spewing Dagger's trail pellets - the user identified it as visually the same effect), holds for `0.05s`, then fades back out over `0.12s` and `queue_free()`s. Additive blend (`CanvasItemMaterial.blend_mode = 1`), same as `IcicleSpawnBurst`.
  - **Sprite - `tex_knife_explosion_burst.tres`**: New `AtlasTexture` region `Rect2(40, 224, 34, 32)` cut from the same TH10 "Objects and Projectiles" rip sheet already referenced by other `resources/bullets/textures/*.tres` atlas textures (e.g. `tex_icicle_spawn_burst.tres` at `Rect2(107, 225, 31, 45)`, same row of soft circular burst/flash sprites - this is the red one) - user pointed out the exact sprite on the sheet directly (no new image file needed, matches the project's existing "reference an `AtlasTexture` region of the shared rip sheet" convention for effect sprites rather than exporting a standalone PNG).
  - **Reused verbatim by `SpawnFlash`** (`scenes/effects/spawn_flash.tscn`), which draws this same region for Reisen's Level 4 spawn motes. Only the timing and scale differ: Sakuya's is a slow 1.15s bloom at 4.5-9.9x, Reisen's a 0.18s pop at 1.7-2.6x, because hers fires up to 53 times a second. No new art was cut for it - the user marked this cell on the sheet again and it turned out to already be in the repo.
- **Reference**: Verified against user-provided `sakuya_level4_rank1.mp4` (00:04-00:08) and `sakuya_level4_rank16.mp4` (00:08-00:11) footage of authentic PoFV gameplay. Initial pass misread the footage as one dense simultaneous 24-32-bullet ring with possible per-spoke pairing; user corrected it to 10 independently-scattered, sequentially-emitted 7-dagger circles (fixed count at every rank), clarified the bounce is one-time only (a bounced/blue dagger doesn't bounce again), and asked for the bounce to happen slightly past the edge rather than exactly on it.

### `DanmakuNarrowingSweepStep` (`scripts/resources/danmaku_narrowing_sweep_step.gd`)
- **ECL parity (`pl02.ecl` `sub5`)**: `aim_at_player` centres the pair on the victim, aimed once at the start. 16 pairs 8 frames apart narrowing from +-90 to +-10.59 degrees (ZUN spread pi, minus 0.1848 rad per shot), 262.5 -> 450 px/s. ZUN also adds a leftover `F2` to the aim each shot; that value is whatever the last attack left behind, so it is not reproduced.
Sakuya's Level 4 Boss Attack 3: Sweep of Yellow Daggers. Imagines a tight circle around the boss and fires yellow daggers sequentially from the front (player-facing) half of that circle - starting at the two outermost positions (near-horizontal, left and right) and stepping inward in lockstep symmetric pairs, gradually narrowing toward straight down without ever fully reaching it. Every dagger spawns at the boss's own exact position (`MotionMode.LINEAR`, no spatial ring offset) - the "circle" is purely the angular arrangement of firing directions, not an actual spawn-radius ring like `DanmakuTimeStopFanStep`'s `knife_radius`.
- **Narrowing Angle Sweep (opposite of `DanmakuKnifeFanStep`)**: Each step `i` (0-indexed) fires a pair at `angle_from_center_deg = start_angle_deg - angle_step_deg * i`, where `angle_step_deg = (start_angle_deg - end_angle_deg) / (daggers_per_side - 1)`. Left dagger direction is `Vector2.DOWN.rotated(angle_rad)`, right is the mirror `Vector2.DOWN.rotated(-angle_rad)`. `start_angle_deg = 85.0` (near-horizontal, the first/outermost pair) narrows down to `end_angle_deg = 18.0` (the last/innermost pair, deliberately stopping well short of straight-down/`0°` per the user's "never fully goes to the middle"). Because every dagger travels in a straight line at a fixed angle from the moment it spawns, the earliest (widest-angle) daggers are always the ones that have flown the farthest by any later frame - which is what visually produces the widening chevron/checkmark shape in reference footage, not a spatial fan-out.
- **Sequential Pairs, Not Instantaneous**: `delay_between_steps = 0.07s` between each symmetric pair (`await playfield.get_tree().create_timer(...).timeout`, same sequential pattern as `DanmakuKnifeStreamStep`) - both sides step inward together each tick rather than one side finishing before the other starts.
- **Rank Scaling**: `daggers_per_side = 16` fixed at every rank (32 daggers total, always) - confirmed by the user ("16 daggers for each individual side" on both ranks). Only speed scales: `speed_min = 240.0 px/s` (Rank 1) up to `speed_max = 400.0 px/s` (Rank 16).
- **Bullet Resource**: `sakuya_knife_yellow.tres` - new resource (now baked into the atlas by `tools/bake_bullets_into_atlas.gd`) following the same convention as the other Sakuya dagger colors.
- **Reference**: Verified against user-provided `sakuya_level4_rank1.mp4` (00:07-00:11) and `sakuya_level4_rank16.mp4` (00:02-00:05) footage of authentic PoFV gameplay. Start/end angle values were estimated by pixel-tracking the visible dagger streak angles relative to the boss's spawn position in a mid-sequence frame (grid-overlay analysis put the widest visible pair at ~67° from vertical and the narrowest at ~40° at that specific moment, with the sweep still visibly in progress) and adjusted outward/inward to account for the sequence not yet having reached either extreme in the sampled frame - treat as a starting approximation pending in-game comparison.

### `DanmakuKnifeStrafeStep` (`scripts/resources/danmaku_knife_strafe_step.gd`)
- **ECL parity (`pl02.ecl` `sub4`)**: `zun_mode` picks a synced random side, glides to (33.3 or 566.7, 400) over 40 frames, holds 40, then dashes decelerating to the far edge at y 200 in 50 frames (`perform_strafe_dash` gained `hold_duration` / `dash_ease_out`). 13 fans, 4 frames apart, of `rank / 4 + 2` daggers `90 / n` degrees apart at 193.75 -> 287.5 px/s. Fan centre starts at 128.6 degrees and turns -2.06 per fan when dashing right, but 77.1 degrees turning +2.06 when dashing left (ZUN did not mirror it). The line-angle and taper fields below are the pre-ECL mode.
Sakuya's Level 4 Boss Attack 4: Knife Strafing. The boss jumps to the left playfield boundary, then rapidly dashes to the right boundary; while dashing, it fires `line_count` (rank-scaled) simultaneous daggers at `daggers_per_line = 13` evenly-spaced ticks spanning the whole dash, one dagger per line per tick, from its current (moving) position.
- **New Engine Capability - `BossCharacter.perform_strafe_dash(start_pos, end_pos, jump_duration, dash_duration)`**: The first attack in this boss's kit where the boss itself visibly moves a large distance while firing, rather than staying put and letting the generic hop AI reposition it between casts. Existing movement helpers didn't fit: `_perform_hop()` switches to `State.HOPPING` (which disables idle bobbing) and is bounded to the small `roam_bounds` rect used for between-attack repositioning; `perform_cast_hop()` (Marisa's Lines of Stars) stays in `State.ACTIVE` and tweens `_base_position` so bobbing keeps applying on top - the right *shape* of behavior for "moving while casting" - but is *also* clamped to `roam_bounds`, which only spans $\pm160\text{px}$ from center and can't reach the actual screen edges this attack needs. `perform_strafe_dash` reuses `perform_cast_hop`'s "stay ACTIVE, tween `_base_position`" shape but takes explicit unclamped `start_pos`/`end_pos`, and chains **two** sequential segments on one `Tween` (Godot tweens run queued steps in order by default): first an eased (`TRANS_QUAD`/`EASE_OUT`) glide from wherever the boss currently is to `start_pos` over `jump_duration` (the "jump to the left boundary" - a smooth lerp, not an instant teleport, per user feedback on the first pass), then a linear (`TRANS_LINEAR`, constant-speed) dash from `start_pos` to `end_pos` over `dash_duration`. A `tween_callback` between the two segments emits the new `strafe_jump_landed` signal, which `DanmakuKnifeStrafeStep` awaits directly (`await active_boss.strafe_jump_landed`) so no daggers fire until the boss has actually finished gliding into position and the dash itself begins.
- **Angle Per Line, Moving Origin - Not a Rotating Fan**: Each of `line_count` lines gets its own angle, evenly spaced from `angle_min_deg = 15.0` (closest to vertical) to `angle_max_deg = 72.0` (closest to horizontal) - both measured leaning toward the *start* of the dash (i.e. "behind" the boss as she moves right, since `Vector2.DOWN.rotated(angle_rad)` with positive `angle_rad` leans left, same convention as `DanmakuNarrowingSweepStep`'s left side). At each of 13 ticks (`tick_interval = dash_duration / (daggers_per_line - 1)`, so tick 0 fires the instant the dash starts and tick 12 fires exactly as it ends), all `line_count` directions fire simultaneously from `active_boss.position` sampled *live* at that moment. Because a line's angle stays constant for most of the dash while its origin keeps advancing rightward, earlier-tick daggers (fired further left, with more remaining flight time by any later observation) end up further along their diagonal than later-tick daggers (fired further right, barely moved) - the same "emergent fan from sequential timing" trick used by `DanmakuNarrowingSweepStep` and `DanmakuBouncingKnifeRingStep`, just against a moving instead of a stationary origin. With `line_count` different angles all doing this at once, the result is `line_count` parallel-ish diagonal streaks radiating from the boss's current position - the "feathered wing" shape confirmed against reference footage - without any explicit curve or rotation code.
- **End-of-Dash Straightening**: Over the last `taper_ticks = 3` emissions, every line's angle is multiplied by a `drag_factor` that fades linearly from `1.0` down to `0.0` at the very last tick (`ticks_from_end / taper_ticks`) - so the final emission always fires perfectly straight down, and the two before it lean progressively less. Added after the user noticed the last 2-3 emissions in reference footage go straight down once the boss actually stops at the wall - the sideways "pull" on newly-thrown knives only exists while the boss is still moving, so it should fade out as her momentum does, rather than every line holding its lean all the way to the last tick.
- **Rank Scaling**: `daggers_per_line = 13` fixed at every rank. `line_count` scales `lines_min = 2` (Rank 1) up to `lines_max = 6` (Rank 16) - confirmed by the user ("at rank 1, it's two lines of 13 daggers. At rank 16, it's six lines of 13"). Speed scales mildly, `speed_min = 260.0 px/s` (Rank 1) up to `speed_max = 300.0 px/s` (Rank 16) - "speed is also slightly increased."
- **Dash Geometry**: `left_edge_x = 40.0` / `right_edge_x = 560.0` (playfield is 600px wide, small margins so the boss doesn't fully clip offscreen), `jump_duration = 0.4s` for the glide into position, `dash_duration = 2.0s` for the dash itself - the boss's Y position at the moment this attack triggers is kept unchanged for the whole thing (pure horizontal traversal).
- **Bullet Resource**: Reuses `sakuya_knife_blue.tres` (already created for Boss Attack 2's post-bounce color) - matches the blue daggers seen in reference footage, no new asset needed.
- **Reference**: Verified against user-provided `sakuya_level4_rank1.mp4` (00:17-00:21) and `sakuya_level4_rank16.mp4` (00:05-00:09) footage of authentic PoFV gameplay. Initial angle estimate (`15.0`-`72.0`, was `35.0`-`80.0`) was visually derived from a clean late-dash frame rather than precise pixel-fit given the heavy streak overlap in that frame; the user then corrected it in-game - the bottom (steepest, `angle_min_deg`) lines in particular were leaning too far left of the stage and needed to turn back toward the player, so `angle_min_deg` was pulled down more than `angle_max_deg`.

### `DanmakuLunarWaveStep` (`scripts/resources/danmaku_lunar_wave_step.gd`)
- **ECL parity (`pl04.ecl` `sub0` Lv2, `sub1` Lv3)**: `zun_mode` replaces the four-phase swell. `rank + 14` (Lv3: `+ 12`) four-way circles fire in one frame, circle k turned k x 360 / count (Lv3: 90 / count) from the wave's shared random angle, at 125 + 6.25 x rank px/s. ZUN's `AimRel` brakes each to a stop over 120 frames, turns it 120 degrees from its own heading (so it partly doubles back) and relaunches at `2v - k x (2v / count - 6.25)`: the falling relaunch speed per circle draws the arms. Second figure 20 frames later with the opposite turn (Lv2: rice +120 then pellets -120; Lv3: -120 then +120). Fired from the victim's mirror position (`mirror_player_origin`). Bullets use `DECEL_AND_HOME` with `relaunch_relative`. The phase fields below are the pre-ECL mode.
Reisen's Level 2 and Level 3 Spellcards: Wave Sign "Lunar Wave" (波符「月面波紋（ルナウェーブ）」). A stack of identical rings that swells out of a single point as one apparent circle, stalls, dips, then unpacks into swept spiral arms. Both cards are this one step configured differently: Level 2 stacks four rings of 36 bullets, Level 3 stacks 14-28 interleaved rings of only four.
- **New Motion Mode - `DanmakuBullet.MotionMode.LUNAR_WAVE`**: Tracks polar coordinates around a fixed centre like `EXPANDING_ORBIT`, but drives the radial speed through a scripted four-phase schedule instead of holding it constant. Everything keys off the bullet's own `_current_lifetime`, so a ring needs no shared state and overlapping rings never interfere.
  1. **Swell** (`lw_expand_time`): radial speed eases out on `v = v0 * (1 - (t/T)^2)` — near-full speed for the first two-thirds, then a hard brake into a dead stop exactly at `T`. Fitted to the reference: the PoFV ring holds ~120 px/s for its first 0.75 s and only then decays, which a plain linear brake does not reproduce. Because this curve covers two thirds of the ground a constant speed would, the step launches at `1.5 * stall_radius / expand_time`, where that time is itself rank-scaled.
  2. **Hold** (`lw_hold_time`, 0.15 s): the ring hangs motionless at full size.
  3. **Contract** (`lw_contract_time`, 0.35 s): radial speed goes negative on a sine arc (`-contract_speed * sin(pi*u)`), so the reversal eases in and out rather than snapping. Net inward drift ≈ 16 px.
  4. **Release**: radial speed restarts at `lw_release_speed` and accelerates outward at `lw_release_accel` for the rest of the flight.
- **Spin as a Fixed Lean, Not a Turn Rate (`lw_spin_lean`)**: During the release, `angular_speed = radial_speed * tan(lean) / radius`, i.e. the bullet's travel holds a constant angle away from straight outward instead of a constant degrees-per-second. Two reasons. It keeps the tilt of the bullet art the same for the whole flight — a plain angular rate makes bullets fly almost sideways at the moment of release (radial speed is near zero there) and almost straight out later. And it makes a bullet sweep further around the further *behind* it is, which is exactly what curls a radial chain into a spiral arm. `+40°` reads clockwise on screen, `-40°` counter-clockwise.
- **Ring Stacking (`ring_count_rank1` / `ring_count_rank16` / `release_accel_spread`)**: Every ring launches in the same frame, stacked on the centre, with **identical** swell parameters — so on screen the stack travels and stalls as one circle, which is what the reference shows (a single row of bullets, not concentric rings). They only come apart at the release, where ring *i* accelerates at `base_accel * (1 + release_accel_spread * i / (rings - 1))`. The spread is expressed end-to-end rather than per ring so it means the same thing whether a card stacks 4 rings or 28, which matters because the ring count is rank-scaled.
- **Interleaving (`interleave_rings`, `bullets_per_ring`)**: Set `interleave_rings`, and each ring is rotated a further `1/rings` of the gap between neighbouring bullets. That is what lets Level 3 build its wheel out of rings of only **four** bullets: 14 such rings, each nudged 6.4° round from the last, read as one 56-bullet wheel while swelling, and then peel into four long spiral arms on release, because a ring's four bullets stay 90° apart however far out it gets. Level 2 leaves this off and uses full 36-bullet rings.
- **`ring_spawn_interval`**: Optional delay between rings, `0` by default. Leaving it at zero is what keeps a stack reading as one circle; a nonzero value gives the stack visible radial thickness at the cost of that. Neither Reisen card uses it, but it is the dial to reach for if a stack should look like a thick band from birth.
- **Rank Scaling**: Bullets per ring never scale (user-confirmed: 36 on the Level 2 card, 4 on the Level 3 one). What rank buys is speed and, on Level 3, ring count. `expand_time_rank1 = 2.60 s` against `expand_time_rank16 = 2.10 s`, over a stall radius that also grows 140 → 220 px, puts launch speed at 81 px/s for Rank 1 and 157 px/s for Rank 16. Level 3 additionally goes from 14 rings to 28, doubling the wheel's density.
- **Bullet Resources**: Level 2 uses `red_oval.tres` for the icicles (20×28 oval needle, red rim) and `red_pellet.tres` for the round pellets — zooming the reference at full size shows those are two genuinely different sprites, the pointed one leading and the round one trailing. Level 3 uses `reisen_wave_bullet.tres`, a rice/casing bullet the user marked on the Mountain of Faith projectile sheet and which nothing in the atlas matched. Its narrowness is load-bearing: at Rank 16 the wheel packs 112 bullets round the circumference, roughly 12 px apart, so a full-width oval smears into a solid band and loses the spoke look entirely.
- **Reference**: Fitted against user-provided `reisen_level2_rank1.mp4`, `reisen_level2_rank16.mp4`, `reisen_level3_rank1.mp4` and `reisen_level3_rank16.mp4`. Radii were read off radius-vs-time kymographs of the ring (video pixels converted at 600/444 ≈ 1.35 game px per reference px), spoke counts off the dominant angular frequency of an annulus, and the rotation direction off the bullet tilt plus the sweep of the arms. All four clips cut off partway through, so ring counts and the two-figure structure come from the user rather than the footage.
- **Audio (`sfx`, `release_sfx`)**: two beats, one per phase the player can see. `se_tan00` on the cast, and `se_kira00` at the release, the moment the figure stops being a ring and starts unpacking into spiral arms. The cast sound is played once per step rather than once per ring on purpose: a stack launched in a single frame is one circle as far as the player is concerned, and `se_tan00`'s 30 ms throttle would collapse the repeats anyway. So a card is two of each, the icicle figure and the pellet figure 0.5 s apart, and the releases land 0.20 s apart at every rank, clear of `se_kira00`'s 80 ms throttle.
- **The Release Chime Is Scheduled, Not Awaited**: `SpellcardData.execute_sequence` awaits each step in turn, so blocking inside `execute()` for the ~3 s until the release would push the second figure that far back and take the two-figures-crossing effect apart. It goes on a `create_timer(...).timeout.connect(...)` instead, guarded by `is_instance_valid(playfield)` inside the lambda so a round ending mid-flight does not chime into an empty field. Same shape as `DanmakuInterweavingIciclesStep`'s `breakout_sfx`, which is the same sound doing the same job.
- **Where The Release Moment Comes From**: `expand_time_safe + hold_time + contract_time`, mirroring the `contract_end` boundary `DanmakuBullet` uses for `MotionMode.LUNAR_WAVE`. The bullets run that schedule off their own spawn moment and are never told when to release, so the two are derived from the same three numbers rather than one being told about the other. Measured on a live field at Rank 1: the ring's radius bottoms out at $3.083\text{ s}$ against a computed $3.100\text{ s}$, one physics frame apart, which is the sampling granularity rather than drift.
- **Pre-Cast Flower (`pre_cast_delay`, both cards)**: the 5-point runic flower opens at 210 px and shrinks onto the ring's centre over 0.65 s before any bullet exists, so the gather and the burst share one point. This is `FlowerRingEffect` (`scenes/effects/flower_ring_effect.tscn`) used **unmodified**, the same effect and the same shader as Cirno's Perfect Freeze pre-cast, which was previously its only caller. Every Touhou spellcast flower has five petals, so the count stays hardcoded in the shader and is deliberately *not* a per-caster dial; only the opening radius (210 px against Cirno's 150, because Lunar Wave's ring stalls wider) and the colour differ.
  - *It is set on the first figure only, on both cards.* `SpellcardData.execute_sequence` awaits each step in turn, so a `pre_cast_delay` on both figures would push them $0.65 + 0.50 + 0.65 = 1.80\text{ s}$ apart instead of the 0.50 s the crossing is built around. On the first alone it delays the whole card by 0.65 s and leaves the gap intact, which is what "the card gathers once" means mechanically. **Any future step that awaits before spawning has this same trap.**
  - *`setup()` is called before `add_child()`*, which is the opposite of the usual convention and matters: `FlowerRingEffect._ready()` reads its own exports to seed the shader and size its quad, so an opening radius handed over afterwards is never applied. The freeze ring's call site already does it in this order; this is the general rule for a node that *reads* its properties in `_ready()`, as against one that *writes* them (see `YinYangOrb` and `setup_variation()` for the inverse trap).
  - *No interaction with the Action Stop freeze.* `ActionStopCoordinator` runs `conclude_action_stop()`, which clears `get_tree().paused`, before the tween callback reaches `trigger_spellcard_effects()`, so the pattern only ever starts once the tree is running again. Worth knowing because the effect processes normally while its `create_timer` would not have paused with it.

### `DanmakuMoonShotStep` (`scripts/resources/danmaku_moon_shot_step.gd`)
Reisen's Level 4 Boss Attack 1, informally "Triple Moon Shot". Three pale moons leave the boss on a narrow fan, drift down the field, and burst short of the player, each leaving a stationary crater. Pure area denial: the shot itself cannot kill, the craters can.
- **On the naming**: Scatter Sign "Luna Megalopolis" (散符「栄華之夢（ルナメガロポリス）」) is the name of the **Level 4 Spellcard itself**, which is what the declaration banner announces and what stays up for the whole boss sequence. The individual attacks the boss fires underneath it are unnamed in the original; "Triple Moon Shot" is ours, purely so these can be discussed without saying "Reisen's Level 4 attack number 1". Same convention as Sakuya's Time Sign "Mysterious Jack" attacks and Youmu's.
- **The Tell (`ReisenMoonCharge`, `charge_duration` 1.20s)**: **Three** light motes, one per moon. Each leaves the boss, drifts to exactly where its moon will appear (`spawn_offset` 130 px down, plus that moon's share of `initial_spacing`), swells, and hands over. Nothing that can hurt anybody exists until it finishes. Timed off the reference at 60fps: the motes appear at 2.40s and the moons have resolved out of them by 3.60s. They are cast where the boss is standing and then travel independently, so she is free to hop away while they sink — the moons resolve out of where the motes came to rest, not out of her.
  - *Visually this is the game's own Extra Attack mote*, the one that flies between playfields when a player sends an attack over: `TravelMote` with `MotePayload.EXTRA_ATTACK`. Both of its textures, both base scales and the additive blend are reused — `travel_mote_hd.png` as a warm-tinted soft glow at $0.65\times$, with the spiky ribbed star of `ex_mote.png` laid over it at $2.0\times$ and slowly turning. A `size` multiplier of up to $1.3\times$ on top makes these the "large" ones. It is a separate scene rather than an actual `TravelMote` because the behaviour shares nothing: that one arcs between playfields, is pooled, and delivers a payload to the far field.
  - *Getting here took two wrong turns worth recording.* The first build used `spark_aura.gdshader`, which was the wrong figure entirely. The second used only `travel_mote_hd.png`, the glow half, and read as a soft pink haze with no structure — the spiky sparkle rim that made it look wrong by its absence is `ex_mote.png`, the overlay half, which was the part being left out. Three of these overlapping reproduce the reference's wide blown-out white blob with its jagged rim almost exactly; one large soft glow never could.
- **The Moons Are Not Born From A Point (`initial_spacing`, 40 px)**: They resolve in a row already touching, one diameter apart, and only then diverge. This is easy to get wrong and expensive when you do: extrapolating the fan back to a single point puts the spawn ~120 px too high, which then corrupts the mote speed, the flight time, and the height the boss must hover at for the fuses to reach. The spacing is read off the frame where they resolve (30 reference px between neighbouring centres, scaled) and cross-checked against the divergence rate two frames later.
  - *The offset is derived from each moon's direction, not from its index.* Deriving the two separately once had them disagree in sign, so each outer moon started on one side and flew to the other: the fan crossed over itself and the outer fuses fired almost together instead of 0.2s apart. Caught in simulation rather than in play.
- **The Proximity Fuse Is the Whole Attack**: Each mote bursts when it closes to `fuse_radius` of the player. The crater it leaves tops out at `blast_radius_end` (126 px, ~0.21 field-widths). Because the fuse is set **wider than the crater**, the edge of the blast is always clear of the player at the instant of detonation. Standing still is therefore always survivable, by construction rather than by tuning — the danger is entirely in flying into a crater afterwards. Anything that pushes the crater radius above the fuse radius silently turns this into an unavoidable hit.
- **`fuse_radius` Depends On Reisen Hovering Lower Than The Other Bosses**: 181 px is the original's 133 reference px scaled, and against the 126 px crater it puts the blast edge ~55 px clear of the player, matching the reference. It only holds at PoFV's boss height. The shared roam band every other boss uses ($y \in [110, 210]$, 0.11–0.22 of field height) sits about twice as high as PoFV's Reisen (~0.31), which makes the motes fall twice as far and gives the $\pm 17°$ fan twice as long to diverge; from up there the outer two motes never get within 181 px of anyone, and because the fuse is the only trigger they simply leave the screen. Reisen's `roam_bounds` are therefore set from the reference rather than shared with the other bosses. See `reisen_boss.tres`.
- **Crater Count Varies By Where She Is Standing, By Design**: This is the attack's texture, not a defect. Measured across her band with the player parked at the bottom middle:

| Boss hover $y$ | Centred cast | Cast at $x=200$ | Cast at roam edge $x=140$ |
| :--- | :--- | :--- | :--- |
| 195 (top of band) | 1 crater | 2 craters | 2, stagger 0.32s |
| 220 | 3, stagger 0.40s | 2 | 2, stagger 0.32s |
| 245 | 3, stagger 0.32s | 2 | 2, stagger 0.32s |
| 270 | 3, stagger 0.27s | 2 | 2, stagger 0.32s |
| 295 (bottom of band) | 3, stagger 0.22s | 2 | 2, stagger 0.32s |

  Clearance at detonation holds at 51–55 px in **every** one of these cases, which is the invariant that actually matters. High casts thin the volley out because the fan has longer to spread before it reaches the player; any off-centre cast loses the moon heading away from them. A volley that lands two craters, or one, is working correctly.
- **The Stagger Is Geometry, Not Script**: The three motes are fired simultaneously and carry identical fuses, so detonation order falls out of which one closes the distance fastest. Fired at a player directly below, the centre mote wins and the outers follow ~0.3s later, overlapping into one wall. Fired from off to one side, the near outer mote goes first and they pop in sequence across the field. Both readings appear in the reference footage, from the same code path.
- **The Fan Does Not Aim**: It points straight down regardless of where the player is. Rank 16 footage settles this: the boss fired from well right of centre and the fan still fell vertically rather than leaning toward the player parked at bottom middle. Half-angle ~17°, mote speed ~250 px/s.
- **Breathing Mote (`ReisenMoonMote`)**: The moon pulses between 0.50× and 1.02× of its baked sprite on a 0.30s cycle — its main visual character, and the reason the sprite is baked at a single size and scaled rather than animated. The mote carries **no hitbox at all**: with the fuse tripping at more than twice the mote's own radius, contact is geometrically unreachable, so a collision shape would be dead weight.
- **There Is No Backstop, And That Is Deliberate**: The fuse is the *only* thing that detonates a mote. There is no timer and no depth limit. A moon that never closes on anybody falls off the bottom of the screen and is culled, and so does one that drifts out through a side wall. A badly placed volley leaves two craters, or one, or none, which is what makes where the boss is standing matter. Cast from the left roam edge, the outward moon exits the wall at $y \approx 801$ having done nothing at all; that volley lands 2 craters, and it is correct that it does.
  - *This was got wrong once.* An earlier pass fitted a 1.5s "burst anyway" timer, on the strength of a small ring at the right of the Rank 16 footage around 8.80s that looked like a third crater far from the player. It is not: it sits at $x \approx 1015$, which is the **middle** mote's track, and it is that mote beginning to detonate. The right-hand mote had already crossed the field edge at $x \approx 1119$ and been culled, and no crater ever appears on that side — by 9.60s there is only the middle crater's dying scribble. The invented timer then fired before the fuse ever could, which is what produced the playtest bug where all three burst simultaneously at a fixed height, never past mid-screen. User confirmed against the original: untriggered moons simply leave.
- **Crater (`ReisenMoonBlast`)**: Detonates in place and never moves. The visual is **`SparkAura`** (`scenes/effects/spark_aura.tscn`), the same burst Reisen's Level 1 Charge Attack and Extra Attack leave on impact, because in the original this is that same explosion. Only the radii and duration differ (24 → 126 px over 1.05s against the charge attack's 88 → 134 over 1.10s), so they are passed in rather than the effect being reimplemented. It already does everything the reference shows: a red disc growing quickly then easing, an outline that starts near-circular and tears itself into a violent scribble, and the red breaking up late while the white outlives it.
- **Cost, And Why This Is Not Pooled**: A volley allocates 27 nodes at worst — 3 charge motes at 3 nodes each, 3 moons at 2, 3 craters at 4 including their `SparkAura`. At the boss's 2.2s cadence that is 12.3 nodes/sec per field, against pools of 1200 bullets and 500 pellets and a single Lunar Wave cast of 224 bullets. Pooling it would be noise, and it would also be aimed at the wrong thing: the cost here is **fill**, not allocation. Each crater runs `spark_aura.gdshader`, which is nine hashed sines and an atan per pixel for its four jittering loop rings, and three overlapping craters cover roughly a fifth of a 1080p screen for about a second. Recycling the nodes would not save a single fragment. This follows the project's existing line — pool what spawns in hundreds (bullets, pellets, travel motes), instantiate what spawns in threes (every charge attack, extra attack, laser, knife and burst in the game).
  - *`SparkAura` now fits its quad to its own radii* (`_fit_quad`, radius + `jitter_peak` + margin) rather than carrying a fixed $420 \times 420$ from its scene file. The drawn result is identical, because `disc_radius` is in UV units against the quad half and the two scale together, but a caller asking for a smaller burst stops paying for the slack: $364 \times 364$ for a moon crater (25% less fill) and $380 \times 380$ for the charge attack that owns the effect (18% less). That also removes a magic number that had to be kept in agreement with the ColorRect's offsets by hand.
- **Why the Crater Does Not Call `SparkAura.setup()`**: That effect deals its own damage to entities on the field, which is what a player charge attack wants and the exact opposite of what a boss hazard wants. The damage path keys off a playfield reference handed over by `setup()`, so skipping the call leaves the burst inert and purely decorative. `ReisenMoonBlast` is then the only thing that hits anything, on layer 2 against the player. Its `CollisionShape2D` reads `SparkAura.get_current_radius()` each frame rather than re-deriving the growth curve, so the hitbox cannot drift out of sync with what the player can see. It stops being lethal 85% through, once the disc is well into breaking up.
- **Rank Scaling**: **None.** Speed, fan angle, fuse distance and crater size all measured within noise of each other between Rank 1 and Rank 16, so nothing here scales. The difficulty comes from what else the boss is doing.
- **Reference**: Fitted against user-provided `reisen_level4_rank1.mp4` and `reisen_level4_rank16.mp4`. Video pixels converted at 600/448 ≈ 1.34 game px per reference px. The fuse radius is the load-bearing measurement and was taken from all three craters in the Rank 1 volley against the player's position (130 / 135 / 136 px) and cross-checked against Rank 16 fired from off-centre (133 px).
- **Audio (`sfx`, plus the crater's own)**: one `se_exattack` as the three light motes are conjured, which is the whole of the step's own audio: it plays once for the volley rather than once per mote, because the three are one cast as far as the player is concerned. The detonations are **not** the step's to play, and never were. `ReisenMoonBlast` builds a `SparkAura`, and that effect plays `se_lazer00` in its own `_ready()`, so each crater announces itself wherever and whenever its fuse happens to trip. That is the right shape here, because the fuse is the only trigger and a volley can leave three craters, two, one or none: a sound fired from the step could not know which. `se_lazer00` also runs on the dedicated polyphonic laser voice rather than the throttled pool, so three near-simultaneous craters give three overlapping booms instead of one.

### `DanmakuStaggeredRingsStep` (`scripts/resources/danmaku_staggered_rings_step.gd`)
- **ECL parity (`pl04.ecl` `sub3`)**: 8 rings of `32 + rank` (`bullets_per_rank`), 15 frames apart, random rotation each, 137.5 -> 325 px/s.
Reisen's Level 4 Boss Attack 2, informally "Rings of Red Bullets". Eight rings of 36 `reisen_wave_bullet` rice bullets leave the boss one after another and expand at a constant, identical speed, each one rotated by a random amount.
- **Nothing Distinguishes One Ring From Another But Its Launch Time**: Every ring carries the same 36 bullets at the same speed on the same centre. There is no per-ring acceleration, no spread and no spin, which is what separates this from `DanmakuLunarWaveStep`. That card's whole character is rings that come apart on release, and this one's is rings that never do. What the player reads is one steady pulse rolling outward at a fixed cadence.
- **The Random Rotation Is The Attack (`ring_rotation_spread`, 1.0)**: Straight concentric rings of 36 would leave 36 clean radial corridors open from the boss to the wall, and the card would be free. Each ring is therefore rotated: rolled fresh, uniformly, anywhere inside the 10° gap between two neighbouring bullets. `1.0` means the full gap, and it is the largest setting that means anything: rotating a ring by a whole gap puts every bullet exactly where its neighbour was, so every distinct rotation is already reachable inside one.
  - *It has to be random, not a pattern.* Two patterned versions were built and rejected before this one, and both failed the same way. A monotone twist (each ring a fixed step further round) spreads the offsets evenly and closes the corridors geometrically, but it spreads them *in order*, and the eye reads the result as a pinwheel: rings that still look organised, merely rotated. Alternating the sides instead (0, +1, −1, +2, −2 …) reads, in the user's words, as "four rings clearly twisted right and four clearly twisted left": also organised, just symmetrically. **Any** rule the player can pick out lets them predict where the next ring's gap will be, which is the one thing this attack cannot allow. Random offsets sometimes put two rings nearly in line, and that is correct: it is a real, occasional gift rather than a structure to learn.
  - *The roll is synced, and drawn up front.* The rotations decide where the safe gaps are, so they are attack identity, not per-bullet jitter, and go through `take_sync_rng()` (claimed on the step's first line, before any `await`, per `DanmakuStep`). All eight are rolled in one unbroken run **before** the firing loop rather than one per ring between the awaits: two volleys overlap whenever both playfields have a boss up, and interleaving their draws would desync the sequence even though each was seeded correctly.
- **The Centre Is Fixed For The Whole Volley**: All eight rings are spawned on the cast origin, not on wherever the boss has drifted to by ring 8. The reference boss stands still through the cast, and concentricity is what makes the pulse legible, and rings launched from a moving point stop reading as one figure and start reading as eight separate bursts.
- **Rank Scaling: Speed Only**: 130 px/s at Rank 1 against 310 px/s at Rank 16, nearly two and a half times. Ring count (8), density (36) and launch interval (0.28s) measured identical between the two clips. A Rank 16 volley therefore has the same gaps in the same places; the player just has a fraction of the time to find and take them.
- **Cadence And Cast Length**: 8 rings $\times$ 0.28s is a 1.96s cast, which the boss blocks on (`BossCharacter._is_casting` freezes the attack timer), so it fits inside Reisen's 2.2s `attack_rate` without stacking. 288 bullets per volley, against the 1200-bullet pool.
- **Audio**: `se_tan00` once per ring, not once per volley: the eight launches are audible as the pulse they are. The 0.28s `ring_interval` clears `se_tan00`'s 30ms throttle comfortably, so all eight land. **This is a deliberate departure from the original, which plays nothing at all for this attack** (user-confirmed off the reference audio). Eight silent rings rolling out read as a bug rather than a choice, so the pulse is ours.
- **Spawn Flash: One Red Mote Per Ring (`spawn_flash_scale`, 2.6x)**: `SpawnFlash` (`scenes/effects/spawn_flash.tscn`) fired on the same beat as each ring's `se_tan00`, at the shared centre every ring launches from. **One per ring, not one per bullet**: all 36 bullets of a ring leave the same point in the same frame, so a flash each would be 36 copies stacked on one pixel for no visible gain and 36x the fill. Eight over a 1.96 s cast at 0.18 s each means barely one alive at a time, so it costs nothing and reads as the centre pulsing once per launch.
  - **It draws above the bullets, and that is the point** (`z_index = 11`, `z_as_relative = false`, against the 10 every `DanmakuBullet` pins itself to). The flash exists to hide bullets popping into existence out of nothing; underneath them it hides nothing and the spawn reads as plainly as it did before. Applies to the machinegun below equally. `KnifeExplosionBurst` already sat at 11 for the same reason.
- **Reference**: Fitted against user-provided `reisen_level4_rank1.mp4` (cast at ~20.5s) and `reisen_level4_rank16.mp4` (cast at ~14.5s). Speed and interval were read off thresholded frames: radii of consecutive rings along the clean vertical below the centre give the ring-to-ring gap directly, and tracking the outermost ring across a 0.6–1.0s baseline gives the speed, with the interval falling out as gap / speed. Both ranks agree on 0.27–0.30s. Video pixels converted at 600/432.5 $\approx$ 1.39 game px per reference px, measured off the panel borders at 8$\times$ zoom; the Triple Moon Shot entry above used 1.34 from an earlier reading, and the 3.5% disagreement is inside the noise on both.
  - *The rotations could not have been measured, and trying was the mistake.* Reading per-ring offsets off frozen frames pins each one to about $\pm 1°$ and says nothing about how they were generated. A random sequence, an alternating one and a monotone one all look alike frame by frame at that precision. Two patterned fits were derived that way and both were wrong. The answer came from the user's screenshot of the original and one sentence about it ("it should look like a mess"), which is evidence the footage measurements structurally could not supply.
  - *Verifying it*: `scratch/` is gitignored, so the throwaway scripts used here are not in the repo; what they checked is. Draw the volley straight from the resource across **several** seeds, because one picture of a random volley and one of a patterned volley can look equally messy and only the set shows whether there is a rule. Then assert the properties: that a given seed reproduces, that different seeds diverge, and that the bullets outlive their flight across the field.

### `DanmakuSpiralMachinegunStep` (`scripts/resources/danmaku_spiral_machinegun_step.gd`)
- **ECL parity (`pl04.ecl` `sub4`)**: one bullet per frame for 120 frames, turning 0.0885 rad (5.07 degrees) per frame = 304.2 degrees/s, 387.5 -> 575 px/s. Reisen's bullets are ZUN's type 16, the capsule "gun bullet" (`etama6` y64, 17 x 33 px on our field), so both of her bullet resources are now scaled to 0.52 with hitbox 6.7.
Reisen's Level 4 Boss Attack 3, informally "Purple Bullet Spiral Machinegun". One barrel out of the boss's hitbox, firing flat out while it turns clockwise. The bullets fly straight, so the figure on screen is nothing but a record of where the barrel has been pointing.
- **The Whole Attack Is Three Numbers**: spin rate, fire interval and bullet speed. There is no aiming, no acceleration, no second arm, and nothing the bullets do after they leave. That makes it the cheapest step in her set to reason about, and the two derived quantities are what the player actually feels: the **angular gap** $\omega \times \Delta t$ (how wide a hole there is to thread) and the **pitch** $v \times 360 / \omega$ (how far apart successive turns sit).
- **Sub-Frame Placement Is Load-Bearing**: at ~53 shots/s against a 60Hz frame, two or three bullets are due every frame. Firing them all at the frame's barrel angle staircases the arm into visible clumps of three. Each shot is instead given the angle the barrel held at *its own* moment and pushed out along that angle by however far it would already have flown, so the arm is smooth regardless of frame rate, and stays smooth if the rate drops.
- **Spin Does Not Scale With Rank (300°/s, 1.20s per turn)**: measured by polar-unwrapping each frame about the boss and tracking the arm's angle at a fixed radius, which turns the rotation into a straight line whose slope is $\omega$. The arm's wrap came to 1.1875s at Rank 1 and 1.181s at Rank 16, within half a percent, so this is deliberately one value rather than a rank ramp.
- **What Rank Actually Buys Is Speed, Not Density**: Rank 16 bullets travel $1.6\times$ faster (490 → 790 px/s), read off the spiral's pitch, which widens from 574 to 932 px against that identical spin. Fewer turns sit on the field at once and each crosses it in well under a second. The angular gap barely moves: 5.7° at Rank 1 against 5.4° at Rank 16, measured at a matched radius of 220 reference px. More bullets reach the player per second at Rank 16 even though the arm is no denser, which is what makes it read as the thicker stream the user described.
  - *The user's reading was that Rank 16 adds bullets*, and the first build scaled the interval hard to match (0.017 -> 0.012s, a 3.6° gap). That closes the near field. The daylight between two neighbouring bullets is the arc `deg_to_rad(spin * interval) * distance` minus the bullet's own 13px width, so it shrinks toward the boss: at 200px out a 3.6° gap is negative, and a 6px hurtbox cannot pass. The interval now moves only 0.019 -> 0.016s (5.7° -> 4.8°), which leans to the tight side of the measurement without closing it: at the range this card is normally met (~550px, player near the bottom, boss at the top of her band) Rank 16 still leaves 33px, while up close it is genuinely punishing, which is the right shape for it. **Do not take `fire_interval_max` below about 0.014.**
- **Reference**: Fitted against user-provided `reisen_level4_rank1.mp4` (cast ~14.6s to ~16.8s) and `reisen_level4_rank16.mp4` (cast ~2.2s). Video pixels converted at 600/432.5 $\approx$ 1.39. The polar unwrap is the tool that made this tractable: `geq=lum='p(cx+(Y*0.5)*cos(X/W*2*PI), cy+(Y*0.5)*sin(X/W*2*PI))'` turns the spiral into a straight diagonal whose slope is $\omega / v$, and tiling one radius row per frame turns the rotation into a line whose slope is $\omega$ directly. Reading angles off the raw frames by eye gave answers that disagreed with each other by a factor of two; the unwrap settled it in one pass.
- **Cost**: ~115 bullets per cast at Rank 1, ~137 at Rank 16, against a 1200-bullet pool. The cast is 2.20s, which is exactly Reisen's `attack_rate`, and the boss does not hop while casting, so the barrel stays put for the whole sweep as it does in the original.
- **Audio: One `se_tan00` Per Bullet, On The Exclusive Channel**: every single shot fires `AudioService.play_tan_exclusive()`, which cuts the previous one off rather than stacking. That is what makes ~53 rounds a second read as a machinegun instead of a wall of mush, and it is the same channel and the same reasoning as Sakuya's Time Stop Fan dagger formation. It cannot go through the general pool: `se_tan00` is throttled at 30 ms there, so two thirds of these would be dropped outright and the survivors would pile up across twelve round-robin voices. An earlier pass did use the pool, at one shot in six, which is why the step no longer carries `sfx` / `sfx_every` exports at all: there is nothing left for them to configure.
- **Spawn Flash: One Red Mote Per Bullet (`spawn_flash_scale`, 1.7x)**: `SpawnFlash` (`scenes/effects/spawn_flash.tscn`) on every single shot, placed at the **bullet's own spawn point** rather than at the muzzle. The two are only a few pixels apart - the sub-frame push is at most `speed * interval`, about 9 px - but that is enough to smear the flashes along the barrel's current heading instead of stacking every one of them on a single pixel, and what the player reads is a glow riding the head of the arm.
  - *Lifetime is the binding cost, not scale.* At ~53 shots a second a 0.18 s flash keeps **9 alive at once** (measured on a live field, which matches the arithmetic), each one additive fill piled on the others at the barrel. Raising either the lifetime or the scale is paid for across the whole 2.2 s cast, so both are held deliberately low - well under Sakuya's 1.15 s bloom, which at this rate would put sixty on screen.
  - *The effects layer is resolved once before the loop*, not per shot: the body runs ~115 times a cast at Rank 1.

### `DanmakuBlossomStep` (`scripts/resources/danmaku_blossom_step.gd`)
- **ECL parity (`pl09.ecl` `sub0` / `sub1`)**: `rank / 2 + 5` emission pairs (integer), one emission every 2 frames. Each emission is a 6-way ring turning +6° and a half-step-offset 6-way ring (`bullet_offset_circle`) turning -6°, starting from one `RAND_ANGLE` (`random_wave_angle`). Speed starts at 125 px/s (1.0) and rises 18.75 px/s (0.15) per emission, so later rings overtake earlier ones. `alt_speed_step` (Lv3: 17.5, ZUN's `F3 += 0.14`) adds the second track; the two rings swap tracks every emission.
- Emitter is fixed at `(300, spawn_y_override = 266.7)`, ZUN's `move_position(0.0, 128.0)`. The script also computes a clamped `-PLAYER_X` and never uses it, so unlike Reimu this card does not mirror the victim.
- Bullet type alternates by emission: `bullet_data_even` / `bullet_data_odd`.
- `pre_cast_delay` (Lv2 / Lv3: 0.667s, ZUN's `+40` after `effect_particle(Effect25)`) opens the card with the 5-point `FlowerRingEffect` closing onto the emitter, as on Reisen's Lunar Wave. Off on the boss's rerun, whose sub has no such call.
- **Audio**: flag `0x200`, `se_tan00` on every emission (30/s) via `AudioService.play_tan_exclusive()`.
- 120 bullets at Rank 1, 312 at Rank 16.

### `DanmakuMarchingColumnStep` (`scripts/resources/danmaku_marching_column_step.gd`)
- **ECL parity (`pl10.ecl` `sub0` / `sub1`, Aya Lv 2 / Lv 3)**: an emitter on the top edge (y 0) jumps along it in steps of `32 - rank` ZUN px (x 2.0833), firing one aimed column per stop: `layers` bullets on one line at `top - (top - 312.5) * k / n` px/s, top = 625 + 12.5 rank (ZUN `bullet_fan_aimed`, 1 way, n layers). Opens with Yuuka's flower pre-cast (0.667s).
- `mirrored` (Lv 2): a stop on each wall every 10 frames, marching inward until they would meet; red pellets and red arrowheads swap sides each pair; 8 layers. Otherwise (Lv 3): one stop every 8 frames from one wall to the other (side from the sync RNG), red butterflies, 5 layers.
- **Audio**: `se_tan00` per stop.

### `DanmakuSnakingFanStep` (`scripts/resources/danmaku_snaking_fan_step.gd`)
- **ECL parity (`pl06.ecl` `sub0` / `sub1`, Lyrica Lv 2 / Lv 3, Noise Sign "Soul Noise Flow")**: an emitter above the centre (Lv 2 y 64, Lv 3 y 0, ZUN px) fires 16 downward 7-way fans (11.25 degrees apart) every 8 frames, alternating two bullet types. Speed 187.5 + 12.5 rank px/s (Lv 2) or 187.5 + 6.25 rank (Lv 3). Opens with the flower pre-cast (0.667s).
- **Snaking**: ZUN chains four `bullet_effects` slots of 30 frames: turn +1.57, -1.57, +0.785, -1.57 rad/s, then straight. Bullets use `CURVE_THEN_LINE` with the first turn and `DanmakuBullet.curve_chain` holding the rest (each chained turn lasts `curve_duration`). The starting direction is ZUN RAND_INT % 2 from the sync RNG. Lv 2 (red pellets / red arrowheads): every fan snakes the same way. Lv 3 (`mirror_alternate`, blue / red arrowheads): the red fans snake the mirrored way, so the colours weave.
- **Audio**: `se_tan00` per fan.

### `DanmakuRandomSprayStep` (`scripts/resources/danmaku_random_spray_step.gd`)
- **ECL parity (`pl10.ecl` `sub5`, Aya Lv 4 Boss Attack 3)**: `16 + rank` emissions, one every 2 frames from the boss, each 3 red pellets and 2 red ring balls in random directions at random speeds from 125 px/s to 625 + 12.5 rank (ZUN `bullet_random`). Unsynced per-bullet jitter. `se_tan00` per emission on the single-voice channel.

### `DanmakuBallStreamStep` (`scripts/resources/danmaku_ball_stream_step.gd`)
- **ECL parity (`pl09.ecl` `sub4`)**: after a 40-frame wind-up the boss takes one `PLAYER_ANGLE` and fires a 6-way `bullet_offset_circle` (half a step off the aim, so the victim sits between two streams) every 4 frames: 30 rings straight, then 10 more that each turn a further pi / 64 (2.8125°), all streams the same way. The way is ZUN's `RAND_INT % 2`, drawn from `take_sync_rng()`. 875 px/s (7.0) at every rank, which spaces the balls ~58 px apart, one ball width, so each stream reads as a solid chain (checked against `yuuka_level4_rank1.mp4`).
- **Audio**: `se_tan00` per ring (flag `0x200`) on the single-voice channel. `windup_sfx = se_power0`: ZUN's `Sound5`, identified by the user from PoFV (the same id opens wind-ups in `pl10` and `pl12`).

### `DanmakuRepelRingStep` (`scripts/resources/danmaku_repel_ring_step.gd`)
- **ECL parity (TH15 `st05bs.ecl` `BossCard3_at`)**: a closed ring of 144 flames (64 on Easy), one aimed at the player (`ins_607` mode 3), speed 1.5 (192.9 px/s). TH15 fires three rings 120 frames apart; **ours fires two, 0.3s apart (~58 px), as one doubled ring**, because three held the stage for four seconds and a Hellfire streak crowded out her other cards (per the user). **TH15 converts by field height (960 / 448 = 2.143, so 1 px/frame is 128.6 px/s), not the depot's uniform 1.5625**: TH15's field is wider than ours in proportion, and width scaling left every distance ~27% short, so no ring reached a player below the boss before its repel ran out (the user found the card impossible). Height is also the scale our player sprites are drawn at.
- **Repel (TH15 ex `0x200000`, 120 frames, 2.0)**: any flame inside `repel_radius` of the player is pushed straight away from them, never past the radius. Implemented as `repel_*` fields on `DanmakuBullet`, applied after any motion mode.
- **Deliberately not TH15's repel.** The original pushes at 2.0 px/frame (faster than the ring, so flames stall on a ~50 TH15 px bubble) but only for 120 frames, which only lets through a player who meets the ring close under the boss. The user found that unreadable (Touhou instinct says stay low), so ours lasts the flame's whole life and pushes slower than the ring's 193 px/s: flames bend round the player and open a gap, but still creep in, so standing still gets you hit by the aimed flame. Her boss therefore uses the shared hover band.
- **Rank scales the gap, nothing else**: reach 90 px / push 170 px/s at Rank 1 (~150 px gap) down to 64 px (the graze radius) / 150 px/s at Rank 16 (~100 px gap), which the user playtested as the edge of possible. Lunatic's extra 32-bullet ring (`BossCard3_at2`) and 90-frame spacing are deliberately left out.

### `DanmakuStarAndStripeStep` (`scripts/resources/danmaku_star_and_stripe_step.gd`)
- **ECL parity (TH15 `st05bs.ecl` `BossCard2_at` / `_at3`)**: twelve long beams (`ins_700`, speed 12 = 1543 px/s, length 1600 TH15 px) slide in from the **left wall only**, one row at a time from the bottom up (`y = 448 - rand * 16 - 64n`, 10 frames apart; rows above the field drift down into view), then 90 frames of hold. **Every beam drifts downward at the same rate** (`ins_536([-9981], 0.6, 1.3, 1.8, 1.8)` px/frame, direction read off a time-slice of the footage), so the gaps keep their size and the player rides one down. Meanwhile two spawn points mirrored about the centre sweep 24 TH15 px per drop across the top and back, dropping stars straight down (`BossCard2_at3`: y = -16 + rand * 64, speed 1 + rand px/frame).
- **Rank**: drift from Easy's 0.6 px/frame (77 px/s) to Normal's 1.3 (167 px/s); stars every 18 frames (Hard) down to 12 (Normal); **beams 7 to 11 per cast** (TH15 fires 12); **star size 0.64x to 1.0x** of TH15's big star (node scale, sprite and hitbox together; Rank 1 matches our old small star, Rank 16 is TH15 scale). Lunatic is left out.
- **Mirrored top to bottom (2026-09-25, per the user)**: the first row comes in near the top, each next row one spacing lower, and every row **rises** at the drift speed (rows that start below the field rise into view). Sinking rows squashed players against the bottom of the field; rising ones carry them up toward Fake Apollo's moon. The ECL notes above describe TH15's unmirrored card.
- **Beams slide in at 640 px/s at every rank**, not TH15's 12 px/frame (1543 px/s), which played as a jumpscare (480 px/s, the first fix, then read as too slow). Earth Light Ray's own 1060 px length holds a row about 2.6s, near TH15's 2.2s, so no stretch is applied (the `beam_length` stretch support stays).
- **Stars spin** at 3 rad/s (`spin_speed` on `clownpiece_big_star_blue.tres`); motionless they read oddly. The rate is set by eye: TH15 spins stars in engine code, not in bullet.anm.
- Beams are Marisa's Earth Light Ray recoloured red ([`earth_light_ray_red.tscn`](scenes/attacks/earth_light_ray_red.tscn)) in its **primed** mode (no telegraph, already firing, sliding at `velocity` for `fire_duration`, then freed). `damage` is an export on `EarthLightRay` (1.5 for Marisa, 1.0 here). Cleared with the bullets. The sparring AI scores any non-upright Earth Light Ray at the point nearest the bot (`get_ai_hazard()`, through the node's transform so the stretch counts).
- **History**: this slot was first Inferno "Striped Abyss" (`BossCard4`: beams from both walls, each drifting 0.5 px/frame up or down). Two reworks later the user judged it too complex to port well and switched to this one-sided card.
- **Audio**: `se_lazer00` per beam, throttled by `play_sfx`.

### `DanmakuStarSweepStep` (`scripts/resources/danmaku_star_sweep_step.gd`)
- **Clownpiece Lv 4 boss attack 3, "Starry Illusion"** (`clownpiece_boss_spell_3.tres`, a name the user gave it), from her first non-spell (TH15 `st05bs.ecl` `Boss1_at2`). It was her Lv 2 first (with a closing flower tell, still available as `pre_cast_delay`), but stars appearing from nowhere lost the charm of her spraying the field, so it moved to the boss (2026-09-25). Big blue stars leave her one per frame, each 9 degrees further round: 14 stars fan about 117 degrees centred on the player in a quarter second (TH15: 20 stars, a half circle, narrowed per the user), and the next arc sweeps back. Each star is aimed on its own frame (`ins_607` mode 0), and each arc's centre is turned by a synced random up to 20 degrees (`[-9987] * 0.349`).
- **ECL parity**: 3 px/frame (386 px/s), 24 units (51 px) out from the emitter. TH15 fires 3 one-way arcs on Easy and 5 there-and-back pairs from Normal; **ours fires 3 arcs at Rank 1 to 5 at Rank 16 (Easy to Normal), alternating direction** (a short spray, not a sustained one).
- **Stars**: `clownpiece_big_star_red.tres` at node scale 0.8 (Rank 1) to 1.0 (Rank 16, TH15 size), spinning. `se_tan00` per arc.
- **Flanking beams (per the user)**: one blue Earth Light Ray each side of her, 150 px out (clamped inside the field), dropped from the top with its rune warning at the start of the cast, burning 0.8s (Rank 1) to 1.2s (Rank 16), damage 1.0. LoLK's `Boss1_at` instead flings white orbs up in pairs (spread 80 degrees narrowing 10 a pair, 4 px/frame slowing 0.4 a pair, 5 or 6 pairs 5 frames apart) that each drop a vertical beam (sprite 38 colour 6), a curtain of 5 or 6 a side. The stars are LoLK's pink-red (type 23 colour 1), `clownpiece_big_star_red.tres`.

### `ClownpieceExtraLasers` (`scenes/attacks/clownpiece_extra_lasers.tscn`), Clownpiece's Extra Attack
- **From her third non-spell** (TH15 `st05bs.ecl` `Boss3_at`, 24:19 in the footage), per the user. A fan of orbs bursts from the landing point, each PoFV's red sparkle (etama sprite 147, `tex_knife_explosion_burst`) blooming 6.6x to 3x over 0.6s like Sakuya's dagger clusters and spinning about a turn a second (per the user; harmless), brakes to a stop over 0.5s (4 px/frame at launch) and hangs in an arc; each orb then aims at where the player is at that moment and drops a red Earth Light Ray along that line (0.5s rune warning, 0.5s firing, damage 1.0). The aim is fixed when the warning appears, so the lasers converge on the player's last known position.
- **ECL**: 4 type 19 orbs a fan, spread 80 degrees narrowing 10 a volley, 4 px/frame slowing 0.4 a volley, lasers sprite 38 colour 2 aimed at the player (angle 1000000.56), 16 units wide, 30 frames warning and 30 firing; 3 volleys on Easy, 6 from Normal, 20 frames apart. **Ours**: one volley (an Extra Attack arrives often), fanned 140 degrees **downward**, since it lands near the top of the field where LoLK's upward fan would leave the screen.
- **Rank**: 3 orbs at Rank 1 to 5 at Rank 16.
- The debug menu's **Send EX** button drops a player's Extra Attack on the opponent's field directly (any character but Sakuya).

### `LyricaExtraNote` (`scenes/attacks/lyrica_extra_note.tscn`), Lyrica's Extra Attack
- **Reference**: the user's `lyrica_extra.mp4` from 0:06. A red double note forms where the attack lands (pl06.anm script 28: 2x0 to full over 20 frames, fading in from 0x40, spinning 6.28 rad/s; harmless), and 28 frames later a ring of 20 notes appears 40 px out, at rest, and accelerates outward at a rate rolled per note in 140-280 px/s^2 (measured ~200: radius 45, 75, 120, 196 px at 0.3s steps), so the ring breaks up as it crosses. The note then shrinks away over 20 frames. Ring turn, accelerations and each note's spin phase come from `setup_variation`.
- **Sprite**: the double note, though script 28 names the single one (footage). Bullets cut from a character's own anm go through `BulletSprites.PLAYER_ANM_BULLETS`, packed into the shared atlas.

### `DanmakuFakeApolloStep` (`scripts/resources/danmaku_fake_apollo_step.gd`) + `ClownpieceMoon` (`scenes/attacks/clownpiece_moon.tscn`)
- **Shape, per the user**: TH15's Fake Apollo (`st05bs.ecl` `BossCard5`) keeps three moons circling Clownpiece for the whole card, a timeout card built for endurance. Here the moons make one half turn: they fade in (0.5s, harmless), turn half a circle round the orbit's centre over 4s (eased at both ends), then fade out (0.5s, harmless). That fade is the joke of the name. The turning direction comes from `take_sync_rng()`. The cast holds for the moons' 5s. It was first her Lv 4 boss attack 3; since 2026-09-25 it is her **Lv 2 and Lv 3** instead (Starry Illusion took the boss slot).
- **Lv 2, "Fake Apollo"** (`clownpiece_spell_lv2.tres`): one moon, orbit 300 px round the emitter, swinging from one side of the field to the other underneath it.
- **Lv 3, "Apollo Hoax Theory"** (TH15's Lunatic name for this card; `clownpiece_spell_lv3.tres`): TH15's three moons, 120 degrees apart, turning half a circle as a wheel of radius 190 centred 200 px below the emitter (a 300 px orbit round the top of the field would put two of them off screen). Each ring carries fewer orbs than Lv 2's: 10 to 14, a ring every 1.0s to 0.7s, orbs 0.8 px/frame (103 px/s) to Hard's 1.2 (154 px/s).
- **ECL parity**: orbit radius 140 TH15 units (`ins_411`, 300 px). The moon hurts on contact (`ins_501` 96 units across, 103 px radius, damage 1.5); it is enm5b.png (`st05enm.anm` sprite 39, `DatTextures` `clownpiece_moon`) drawn at 2.0x to match the footage (~109 units across). `BossCard5_Moon_at2` fires rings (`ins_607` mode 2, aimed, turned 10 degrees off the player) of type 26 bullets, 80 units (171 px) out from the moon's centre (ours 130 px, just past the rim: 171 read as too far, per the user), 0.8 px/frame (103 px/s). Heavy shockwaves clear the orbs, not the moon.
- **Rank (Lv 2)**: orbs per ring from Easy's 18 to Normal's 23; gap between rings from Easy's 60 frames (1.0s) to Normal's 30 (0.5s). TH15 eases the gap down from 120 frames over the card; one sweep is too short for that, so the end values are used.
- **Bullet**: `clownpiece_glow_ball_purple.tres`, TH15 bullet1.png sprite 67 (the violet 16px white-cored glow ball), picked by colour against the footage (halo about rgb 140, 68, 180). 2.143x, hitbox 8.5, not cancelable.
- **Audio**: `se_tan00` per ring.

---

## 4. Existing Spellcards (`resources/spellcards/`)

### Reimu Hakurei
- **Level 2 Spellcard**: [`reimu_spell_lv2.tres`](resources/spellcards/reimu/reimu_spell_lv2.tres)
  - *Name*: Spirit Sign "Fantasy Seal -Spread-" (霊符「夢想封印 散」)
  - *Composition*: 32 to 64 bullets per ring (`I0 = Rank * 2 + 32`), alternating Rice (`red_oval.tres`) and Pellet (`red_pellet.tres`) $\rightarrow$ 0.1s delay $\rightarrow$ staggered ring of White Rice and White Pellets with `AimPlayer` homing after 2.0s (120 frames).
  - *Speed & Homing*: Base launch speed $125.0\text{--}425.0\text{ px/s}$ (`F1 = Rank * 0.15 + 1.0` in ECL). Homing launch speed $162.5\text{--}552.5\text{ px/s}$ ($F2 = F1 \times 1.3$).
  - *Audio*: Spawns both red and white rings with `se_tan00`. Plays `se_kira00` chime when white bullets lock on and launch.
- **Level 3 Spellcard**: [`reimu_spell_lv3.tres`](resources/spellcards/reimu/reimu_spell_lv3.tres)
  - *Name*: Jewel Sign "Orbs of Light, Cast into Shade" (宝符「陰陽宝玉」)
  - *Composition*: 48 to 80 Amulets per ring (`I0 = Rank * 2 + 48`), Red Amulets (`red_talisman.tres`) $\rightarrow$ 0.1s delay $\rightarrow$ White Amulets (`white_talisman.tres`) with `AimPlayer` homing after 1.0s (60 frames).
  - *Speed & Homing*: Base launch speed $312.5\text{--}412.5\text{ px/s}$ (`F1 = Rank * 0.05 + 2.5` in ECL). Homing launch speed $375.0\text{ px/s}$ ($3.0 \times 125.0$).
  - *Audio*: Ring spawns play `se_tan00`. Plays `se_kira00` chime when white talismans lock on and launch.
- **Level 4 Boss Spellcard 1**: [`reimu_boss_spell_1.tres`](resources/spellcards/reimu/reimu_boss_spell_1.tres)
  - *Name*: Spirit Sign "Fantasy Seal -Concentrate-" (霊符「夢想封印 散」) [ECL `sub4`]
  - *Composition*: Dual concentric rings of white talismans (`white_talisman.tres` / sprite 11) in `DECEL_AND_HOME` mode.
  - *Behavior*: One aimed `bullet_circle_aimed` with 2 layers on the same angles: $312.5\text{ px/s}$ (ZUN: 2.5) and $237.5\text{ px/s}$ (ZUN layer rule: $2.5 - (2.5 - 1.3) \times 1/2 = 1.9$). Both brake for 1.5s (90 frames), re-aim at the player and launch at $250.0\text{ px/s}$ (ZUN: 2.0).
  - *Rank Scaling*: 42 to 72 talismans/ring (`I0 = Rank * 2 + 40`).
  - *Audio*: Ring spawn plays `se_tan00`.
- **Level 4 Boss Spellcard 2**: [`reimu_boss_spell_2.tres`](resources/spellcards/reimu/reimu_boss_spell_2.tres)
  - *Name*: Divine Arts "Omnidirectional Dragon-Slaying Array" (神技「八方鬼縛陣」) [ECL `sub5`]
  - *Composition*: Modular `DanmakuExtraAttackStep`.
  - *Behavior*: Sequentially launches Yin-Yang Orbs (`yin_yang_orb.tscn`) in an alternating left/right high arc across the arena, matching `ex_ins_call(__Reimu_Attack, 0)`.
  - *Rank Scaling*: 4 orbs at Rank 1 up to 9 orbs at Rank 16 (`I0 = Rank / 3 + 4`), with 10-frame ($0.1667\text{s}$) interval.
  - *Audio*: Plays `se_exattack` for each spawned Yin-Yang orb.
- **Level 4 Boss Spellcard 3**: [`reimu_boss_spell_3.tres`](resources/spellcards/reimu/reimu_boss_spell_3.tres)
  - *Name*: Treasure Sign "Dancing Yin-Yang Orbs" (宝符「陰陽宝玉」) [ECL `sub6`]
  - *Composition*: Modular `DanmakuClawStep`.
  - *Behavior*: $25.7^\circ$ 3-strip claw aimed at the player with center Red Amulets + Red Pellets and side Red Pellets.
  - *Rank Scaling*: 8 to 16 bullets/strip (`I0 = Rank / 2 + 8`), speed gradient $112.5\text{--}200\text{ px/s}$ (Rank 1) to $112.5\text{--}300\text{ px/s}$ (Rank 16).
  - *Audio*: Plays `se_tan00` in sync with rapid bullet emissions ($0.0167\text{s}$ interval).
- **Level 4 Boss Spellcard 4**: [`reimu_boss_spell_4.tres`](resources/spellcards/reimu/reimu_boss_spell_4.tres)
  - *Name*: Divine Spirit "Fantasy Seal -Blink-" (神霊「夢想封印 瞬」) [ECL `sub7`]
  - *Composition*: Modular `DanmakuDoubleRingStep`.
  - *Behavior*: Two concentric rings of small pellets (`red_pellet.tres` and `white_pellet.tres`) counter-rotating at $\omega = 0.7854\text{ rad/s}$ ($45.0^\circ/\text{s}$, from ZUN's $0.01309\text{ rad/frame}$) while expanding outward radially at $187.5\text{ px/s}$ (ZUN: 1.5).
  - *Rank Scaling*: 30 to 46 pellets/ring (`I0 = Rank * 1 + 30`, 60 to 92 total).
  - *Audio*: Plays `se_tan00` upon double ring emission.
- **Level 4 Boss Spellcard 5**: [`reimu_boss_spell_5.tres`](resources/spellcards/reimu/reimu_boss_spell_5.tres)
  - *Name*: "Fantasy Nature" (「無想転生」) [ECL `sub3`]
  - *Composition*: Modular sequence of 4 chained `DanmakuRingStep` resources firing concentric `red_talisman.tres` rings in `LINEAR` motion mode.
  - *Behavior*: 4 concentric aimed rings with step velocity of $62.5\text{ px/s}$ (Ring speeds: $312.5\text{ px/s}$, $250.0\text{ px/s}$, $187.5\text{ px/s}$, $125.0\text{ px/s}$).
  - *Rank Scaling*: 40 to 104 talismans/ring (`I0 = Rank * 4 + 40`, 160 to 416 total across 4 rings).
  - *Audio*: Plays `se_tan00` for each spawned ring.

### Marisa Kirisame
- **Level 2 Spellcard**: [`marisa_spell_lv2.tres`](resources/spellcards/marisa/marisa_spell_lv2.tres)
  - *Name*: Love Sign "Non-Directional Laser" (恋符「ノンディレクショナルレーザー」)
  - *Composition*: Modular `DanmakuDiagonalStripStep` with alternating `blue_star.tres` and `blue_pellet.tres`.
  - *Behavior*: Downward diagonal streams sweeping into the opponent's arena. Spawns along the outside wall and moves down-inward across the screen with speed gradient ($120\text{--}280\text{ px/s}$).
  - *Rank Scaling*: 6 lines at Rank 1, 8 lines at Rank 8, 12 lines at Rank 16.
  - *Audio*: Plays `se_tan00` for each strip of bullets that spawns.
- **Level 3 Spellcard**: [`marisa_spell_lv3.tres`](resources/spellcards/marisa/marisa_spell_lv3.tres)
  - *Name*: Magic Sign "Stardust Reverie" (魔符「スターダストレヴァリエ」)
  - *Composition*: Modular `DanmakuDiagonalStripStep` in `DUAL_INWARD` mode with `green_star.tres` (left wall) and `blue_star.tres` (right wall).
  - *Behavior*: Downward diagonal double-pronged attack emerging simultaneously from both outside boundaries toward the center; 3 pairs at Rank 1, 4 pairs at Rank 8, 6 pairs at Rank 16. Forms a diamond crosshatch pattern of intersecting stars.
  - *Audio*: Plays `se_tan00` for each strip of bullets that spawns.
- **Level 4 Boss Spellcard 1**: [`marisa_boss_spell_1.tres`](resources/spellcards/marisa/marisa_boss_spell_1.tres)
  - *Name*: Magic Sign "Lines of Stars" (魔符「イリュージョンスター」)
  - *Composition*: Modular `DanmakuAimedStripStep` with `blue_star.tres`.
  - *Behavior*: Boss casts sequential strips of 4 blue stars aimed at the player's current location while performing a standardized hop to either side ($80\text{ px}$, $1.55\text{s}$) identical across all ranks. Stars in each strip spawn simultaneously at initial speed $220\text{ px/s}$ and separate naturally in flight via an acceleration gradient ($90 \to 210\text{ px/s}^2$). As the opponent moves, each sequential strip re-targets their updated position, creating a curving stream from a continuously moving origin. Marisa glides for the full $1.55\text{s}$ duration at all ranks.
  - *Rank Scaling*: 5 strips (20 stars total) at Rank 1 scaling up to 20 strips (80 stars total) at Rank 16.
  - *Audio*: Plays `se_tan00` for each strip of stars emitted.
- **Level 4 Boss Spellcard 2**: [`marisa_boss_spell_2.tres`](resources/spellcards/marisa/marisa_boss_spell_2.tres)
  - *Name*: Earth Light Ray Barrage (Extra Attack Barrage)
  - *Composition*: Modular `DanmakuExtraAttackStep` (`SpawnLocation.GROUND_RAYS`).
  - *Behavior*: Boss holds Row 2 casting pose and sequentially summons Earth Light Rays (`earth_light_ray.tscn`) anchored firmly along the bottom ground border ($Y = 950\text{ px}$) shooting upward with halved tilt angles (random in $[-3.25^\circ, +3.25^\circ]$) pointing both inwards and outwards across randomized ground positions ($X \in [75, 525]$ with separation $\ge 45\text{ px}$). Slices across dodging corridors without edge cutoff (extended 1060 px beam length).
  - *Rank Scaling*: 4 rays at Rank 1 scaling up to 8 rays at Rank 16 with $0.22\text{s}$ sequential interval.
  - *Audio*: Plays `se_lazer00` with polyphonic overlapping for every single laser spawned in the barrage.
- **Level 4 Boss Spellcard 3**: [`marisa_boss_spell_3.tres`](resources/spellcards/marisa/marisa_boss_spell_3.tres)
  - *Name*: Spiral Pellets and Stars (Magic Sign "Illusion Star" Pinwheel)
  - *Composition*: Modular `DanmakuPinwheelStep`.
  - *Behavior*: Spawns 24 rotating bursts at a rapid $0.045\text{s}$ interval radiating outward from the boss across 6 spokes. Waves alternate rapidly between Green Stars $\to$ Green Pellets $\to$ Blue Stars $\to$ Green Pellets, while the pivot rotates clockwise at $15.0^\circ$ per wave ($360^\circ$ full turn). Bullets in each spoke are emitted as curved angular arcs ($\approx 6.5^\circ$ spacing per bullet), perfectly winding into 6 dense, coiling vortex ribbons identical to Touhou 09.
  - *Rank Scaling*: 2 bullets per spoke arc ($2 \times 6 = 12$ bullets/burst; 288 total) at calibrated $160.0\text{ px/s}$ at Rank 1 scaling up to 4 bullets per spoke arc ($4 \times 6 = 24$ bullets/burst; 576 total) at $340.0\text{ px/s}$ at Rank 16.
  - *Audio*: Plays `se_tan00` on each rotating burst wave.
- **Level 4 Boss Spellcard 4**: [`marisa_boss_spell_4.tres`](resources/spellcards/marisa/marisa_boss_spell_4.tres)
  - *Name*: Star Spray (Magic Sign "Illusion Star" Spray)
  - *Composition*: Modular `DanmakuStarSprayStep`.
  - *Behavior*: Fires 60 yellow stars (`yellow_star.tres`) across 6 rapid burst waves from Marisa's center. Bullets continuously spin ($6.0\text{ rad/s}$) and curve outward during their first $1.1\text{s}$ ($\pm 1.25\text{ rad/s}$), blossoming outward in a fountain/bell fan shape, and then settle into straight linear motion indefinitely while continuing to spin.
  - *Rank Scaling*: Constant 60 yellow stars at all ranks. Speed scales sharply from $200.0\text{ px/s}$ at Rank 1 to $520.0\text{ px/s}$ at Rank 16 ($\pm 15\%$ individual bullet speed variance).
  - *Audio*: Plays `se_tan00` on each burst wave.
- **Level 4 Boss Spellcard 5**: [`marisa_boss_spell_5.tres`](resources/spellcards/marisa/marisa_boss_spell_5.tres)
  - *Name*: Two Fixed Green Lasers
  - *Composition*: Modular `DanmakuFixedLasersStep` with emerald green Earth Light Rays ([`earth_light_ray_green.tscn`](scenes/attacks/earth_light_ray_green.tscn)).
  - *Behavior*: Two vertical green laser beams ($0^\circ$ tilt) erupt at fixed horizontal positions corresponding to the quarter-splits of the playfield ($X = 150.0\text{ px}$ and $X = 450.0\text{ px}$) anchored at the bottom screen border ($Y = 950.0\text{ px}$). The two lasers emerge sequentially with a $0.22\text{s}$ stagger. At Rank 1, they fire once (2 lasers total); at Rank 16, they pulse/repeat 3 times in total (6 lasers total) spaced by a $0.65\text{s}$ wave interval with alternating firing sequence (Left $\to$ Right, then Right $\to$ Left, then Left $\to$ Right).
  - *Rank Scaling*: 1 pulse at Rank 1 (2 lasers total), 2 pulses at Rank 8 (4 lasers total), 3 pulses at Rank 16 (6 lasers total).
  - *Audio*: Plays `se_lazer00` with polyphonic overlapping for every single laser spawned in each pulse wave.

### Youmu Konpaku
- **Level 2 Spellcard**: [`youmu_spell_lv2.tres`](resources/spellcards/youmu/youmu_spell_lv2.tres)
  - *Name*: Lost Sign "Binding Sword" (迷符「纏縛剣」)
  - *Composition*: Modular `DanmakuDescendingStripsStep` with side-by-side alternating `green_pellet.tres` (cancelable) and `green_arrow.tres` (non-cancelable).
  - *Behavior*: Horizontal strips of side-by-side green pellets and arrowheads spawn offscreen above the opponent's playfield ($Y = -30.0\text{ px}$), sweeping sequentially from the outside wall inward (`AUTO_FROM_OUTSIDE`) at rapid speed ($0.011\text{s}$ bullet delay, $0.21\text{s}$ strip interval). Each individual bullet in the strip receives its own subtle directional drift within $\pm 3.0^\circ$ (some straight, some slightly left, some slightly right). The arrowheads travel slightly faster ($285\text{ px/s}$) than the pellets ($250\text{ px/s}$), naturally pulling ahead into leading waves while trailing cancelable pellets follow behind.
  - *Rank Scaling*: 3 strips (33 bullets total) at Rank 1 scaling up to 7 strips (77 bullets total) at Rank 16. Speeds remain fixed across all ranks.
  - *Audio*: Plays `se_tan00` for every single bullet spawn across the descending strips.
- **Level 3 Spellcard**: [`youmu_spell_lv3.tres`](resources/spellcards/youmu/youmu_spell_lv3.tres)
  - *Name*: Lost Sign "Binding Sword" (迷符「纏縛剣」)
  - *Composition*: Modular `DanmakuDescendingStripsStep` with side-by-side alternating `green_knife.tres` (non-cancelable dagger) and `green_arrow.tres` (non-cancelable arrowhead).
  - *Behavior*: Horizontal strips of side-by-side green knives and arrowheads spawn offscreen above the opponent's playfield ($Y = -30.0\text{ px}$), sweeping sequentially from the outside wall inward (`AUTO_FROM_OUTSIDE`) at rapid speed ($0.011\text{s}$ bullet delay, $0.21\text{s}$ strip interval). Each individual bullet in the strip receives its own subtle directional drift within $\pm 3.0^\circ$. The green knives travel faster ($285.0\text{ px/s}$ at Rank 1) than the trailing arrowheads ($250.0\text{ px/s}$ at Rank 1), pulling ahead down the screen.
  - *Audio*: Plays `se_tan00` for every single bullet spawn across the descending strips.
- **Level 4 Boss Spellcard 1**: [`youmu_boss_spell_1.tres`](resources/spellcards/youmu/youmu_boss_spell_1.tres)
  - *Name*: Lost Sign "Self-Enlightenment" (Fan of Knives) (迷符「半身大悟」)
  - *Composition*: Modular `DanmakuKnifeFanStep` with `yellow_knife.tres` (non-cancelable golden daggers).
  - *Behavior*: Youmu pauses in sword swing cast pose for $0.35\text{s}$, aiming at the opponent's current location. She then launches a sequence of expanding fan waves of golden daggers with `se_tan00`. Each wave has one more knife than the last ($w + 1$ knives in wave $w$), spaced evenly at $9.6^\circ$ intervals. As earlier narrow waves fly ahead followed by later wider waves, they naturally form a crisp, descending wedge / V-shaped arrowhead formation.
  - *Rank Scaling*:
    - **Rank 1**: 5 waves ($1+2+3+4+5 = 15$ daggers total), speed $215.0\text{ px/s}$, wave delay $0.16\text{s}$.
    - **Rank 16**: 9 waves ($1+2+\dots+9 = 45$ daggers total), speed $275.0\text{ px/s}$, wave delay $0.12\text{s}$.
- **Level 4 Boss Spellcard 2**: [`youmu_boss_spell_2.tres`](resources/spellcards/youmu/youmu_boss_spell_2.tres)
  - *Name*: Prison Sign "Infinite Corridor" (Rapid Daggers) (獄符「無限廻廊」)
  - *Composition*: Modular `DanmakuKnifeStreamStep` with `blue_knife.tres` (non-cancelable sapphire daggers).
  - *Behavior*: Youmu pauses in sword swing cast pose for $0.35\text{s}$, then fires an aimed rapid-fire stream of blue daggers directly at the opponent's position with `se_tan00`. Each knife updates its aim angle toward the player at its exact moment of launch, creating a curving stream that trails the player's evasion.
  - *Rank Scaling*:
    - **Rank 1**: 9 daggers, speed $260.0\text{ px/s}$, delay $0.14\text{s}$.
    - **Rank 16**: 25 daggers, speed $400.0\text{ px/s}$, delay $0.075\text{s}$.
- **Level 4 Boss Spellcard 3**: [`youmu_boss_spell_3.tres`](resources/spellcards/youmu/youmu_boss_spell_3.tres)
  - *Name*: Hesitation Sign "Half-Body Insight" (Oval Daggers) (迷符「半身大悟」)
  - *Composition*: Modular `DanmakuOvalKnifeStep` with `blue_knife.tres` (non-cancelable sapphire daggers).
  - *Behavior*: Youmu pauses in sword swing cast pose for $0.35\text{s}$, then emits pairs of sapphire daggers circling around a horizontal ellipse (lying on its side) extending downwards from her position with `se_tan00`. Left and right streams flow symmetrically around the curve with tangent rotation. Before reaching the bottom apex (around $58^\circ$, avoiding bottom apex contact), each dagger detaches and continues flying linearly along its natural heading on the oval (right stream down-left, left stream down-right) with a $\pm [8^\circ, 20^\circ]$ fan deflection.
  - *Rank Scaling*:
    - **Rank 1**: 11 daggers/side (22 total), oval radii $(230, 145)\text{ px}$, speed $225\text{ px/s}$, delay $0.075\text{s}$.
    - **Rank 16**: 26 daggers/side (52 total), oval radii $(400, 260)\text{ px}$, speed $410\text{ px/s}$, delay $0.035\text{s}$.

### Cirno
- **Level 2 Spellcard**: [`cirno_spell_lv2.tres`](resources/spellcards/cirno/cirno_spell_lv2.tres)
  - *Name*: Freeze Sign "Perfect Freeze" (凍符「パーフェクトフリーズ」)
  - *Composition*: Modular `DanmakuFreezeRingStep`, single wave (`waves_min = waves_max = 1`, no repetition).
  - *Behavior*: A ring of alternating small blue icicles and pellets expands outward from a fixed point near the top of the opponent's playfield. Once the ring crosses its critical radius (240 px), **every bullet currently on that field** (including ordinary fairy pellets that weren't part of the spellcard) is seized: each is given a random new direction, recolored white, and accelerates from rest up to a slow terminal speed, scattering the whole field into a drifting cloud.
  - *Rank Scaling*: 80 bullets (40 icicle + 40 pellet) in a single ring below Rank 9. At Rank 9+, a second ring of 80 pure icicles joins, expanding at 0.6x speed; since both rings share the same freeze trigger, the slower ring is naturally smaller (less expanded) when the freeze catches it, separating visually from the first.
  - *Audio*: Plays `se_tan00` on ring spawn.
- **Level 3 Spellcard**: [`cirno_spell_lv3.tres`](resources/spellcards/cirno/cirno_spell_lv3.tres)
  - *Name*: Freeze Sign "Perfect Freeze" (凍符「パーフェクトフリーズ」)
  - *Composition*: Same `DanmakuFreezeRingStep` as Lv2, reconfigured (`second_ring_min_rank = 1`), single wave.
  - *Behavior*: Identical freeze mechanic to Lv2, but both rings are present and pure icicle (no pellets) from Rank 1 onward.
- **Level 4 Boss Spellcard 1**: [`cirno_boss_spell_1.tres`](resources/spellcards/cirno/cirno_boss_spell_1.tres)
  - *Name*: Circle of Blue Pellets and Extra Attack Barrage
  - *Composition*: 3 chained `DanmakuRingStep` resources (all fully synchronous, zero delay between them) + 1 `DanmakuExtraAttackStep` (`SpawnLocation.ACROSS_TOP`).
  - *Behavior*: Boss fires 3 concentric rings of small blue pellets (`blue_pellet_small.tres`) from the same angular positions (no half-step stagger) simultaneously with the opponent's own Falling Stalactite extra attack (`cirno_stalactite.tscn`) raining in across the top of their playfield. Since `DanmakuRingStep.execute()` has no internal awaits, all 3 rings spawn instantly in the same frame; the differing `speed_min`/`speed_max` per ring (130–155 / 165–195 / 200–235 px/s) causes them to separate naturally into 3 distinct expanding circles as they travel, with zero angular stagger needed since speed alone creates the separation.
  - *Rank Scaling*: Ring bullet count fixed at 20/ring (60 total) across all ranks — only speed scales rank 1→16. Stalactite count scales 6 (Rank 1) to 10 (Rank 16), matching `DanmakuExtraAttackStep`'s standard rank interpolation.
  - *Audio*: First ring plays `se_tan00` on spawn (other two rings silent to avoid a triple-stacked sound). Stalactites play their own `se_exattack` individually (already built into `CirnoStalactite._ready()`), so the extra attack step's own `sfx` is left empty to avoid doubling it.
- **Level 4 Boss Spellcard 2**: [`cirno_boss_spell_2.tres`](resources/spellcards/cirno/cirno_boss_spell_2.tres)
  - *Name*: Interweaving Icicles
  - *Composition*: Modular `DanmakuInterweavingIciclesStep` with `cyan_icicle.tres`.
  - *Behavior*: Two fully-overlapping circles of small cyan icicles expand outward from the boss, hold motionless for a brief pause once they reach 180px radius, then each circle's icicles snap to a tangential heading (one circle counter-clockwise/"relative left", the other clockwise/"relative right") and accelerate forward from a stop, unraveling into two counter-spiraling arms that weave past each other as they fly outward and off the playfield.
  - *Rank Scaling*: 36 icicles/circle (72 total) at Rank 1 scaling up to 52 icicles/circle (104 total) at Rank 16. Expansion speed 150→180 px/s; post-breakout acceleration 560→680 px/s²; terminal speed 340→390 px/s (slowed down from the initial pass per user feedback - the breakout read too fast).
  - *Audio*: Plays `se_tan00` on cast.
- **Level 4 Boss Spellcard 3**: [`cirno_boss_spell_3.tres`](resources/spellcards/cirno/cirno_boss_spell_3.tres)
  - *Name*: Shower of Icicles
  - *Composition*: Modular `DanmakuIcicleShowerStep` with `blue_icicle.tres` (reuses the existing icicle bullet as-is, no new asset needed).
  - *Behavior*: Boss summons small blue icicles one at a time (0.02s apart, not a single burst) at randomized positions scattered around her (±108px horizontally, -24 to +192px vertically), each independently falling straight down (`MotionMode.LINEAR`, `Vector2.DOWN`) at its own randomly-rolled speed. The sequential spawn stagger plus the mix of fall speeds fans the cluster out into a cascading shower as the icicles descend, rather than falling as a rigid block.
  - *Rank Scaling*: Bullet count is a **constant 55 at every rank** (this attack doesn't scale density). Per-icicle fall speed is randomized within `[fall_speed_min, fall_speed_max]`; `fall_speed_min` stays fixed at 140 px/s across ranks while `fall_speed_max` scales 230 px/s (Rank 1) → 320 px/s (Rank 16), matching the "same count, just a higher top speed" reference behavior.
  - *Audio*: Plays `se_tan00` once on cast (not per-icicle).

### Reisen Udongein Inaba
- **Level 2 Spellcard**: [`reisen_spell_lv2.tres`](resources/spellcards/reisen/reisen_spell_lv2.tres)
  - *Name*: Wave Sign "Lunar Wave" (波符「月面波紋（ルナウェーブ）」)
  - *Composition*: `DanmakuLunarWaveStep` (red ovals, `spin_lean_deg = 40`) -> `DanmakuDelayStep` (0.50s) -> `DanmakuLunarWaveStep` (red pellets, swelling 0.3s quicker so they close the gap, `spin_lean_deg = -40`, half-step angular offset), one wave (`waves_min = waves_max = 1`).
  - *Behavior*: Four rings of red icicles launch stacked on one point and swell outward as a single circle, braking to a standstill at full size. Four rings of round pellets follow 0.5s later but swell faster, so they close the gap and arrive alongside the icicles rather than playing out afterward. The whole figure holds for a beat, drifts slightly back inward, then unpacks: each ring accelerates harder than the last, and the two sets lean opposite ways, so the icicles and pellets spiral out through each other. That crossing is the point of the attack.
  - *Rank Scaling*: 36 bullets per ring at every rank (288 total across all eight rings). Rank buys speed: the swell takes 2.60s at Rank 1 and 2.10s at Rank 16, over a stall radius that also grows 140 → 220 px. Base release acceleration 60 → 80 px/s².
  - *Audio*: Plays `se_tan00` once per step (two casts, not eight).
  - *Open*: The reference clips end partway through the first pass, so the 0.50s offset between the two sets and the size of the release speed spread are in-game judgement calls rather than measurements.
- **Level 3 Spellcard**: [`reisen_spell_lv3.tres`](resources/spellcards/reisen/reisen_spell_lv3.tres)
  - *Name*: Wave Sign "Lunar Wave" (波符「月面波紋（ルナウェーブ）」)
  - *Composition*: `DanmakuLunarWaveStep` (`reisen_wave_bullet`, `bullets_per_ring = 4`, 14 rings at Rank 1 / 28 at Rank 16, `interleave_rings = true`, `spin_lean_deg = 40`) -> `DanmakuDelayStep` (0.50s) -> the same step mirrored (`spin_lean_deg = -40`, swelling 0.3s quicker), one wave.
  - *Behavior*: Same lifecycle as the Level 2, built the other way round. Instead of a few rings of many bullets, it stacks many rings of **four** bullets, each rotated a fraction of a step from the last, so the stack reads as one dense wheel of spokes while it swells. Because each ring's four bullets stay 90° apart however far out they get, the release peels the wheel into four long spiral arms per figure rather than short radial chains. Two wheels, half a second apart, leaning opposite ways.
  - *Rank Scaling*: 4 bullets per ring at every rank; ring count doubles 14 -> 28, so the wheel goes from 56 to 112 spokes (112 -> 224 bullets across both figures). Swell 2.60s -> 2.10s over a stall radius of 140 -> 220 px.
  - *Audio*: Plays `se_tan00` once per step.
  - *Open*: The clips cut off partway through, so whether the second wheel really counter-spins (assumed, by analogy with the Level 2) and the exact release spread are in-game judgement calls.
- **Level 4 Boss Attack 1**: [`reisen_boss_spell_1.tres`](resources/spellcards/reisen/reisen_boss_spell_1.tres)
  - *Name*: `Scatter Sign "Luna Megalopolis" - Triple Moon Shot`. The spellcard is Scatter Sign "Luna Megalopolis" (散符「栄華之夢（ルナメガロポリス）」); "Triple Moon Shot" is our own label for this attack within it, since the original leaves the boss's individual attacks unnamed.
  - *Composition*: A single `DanmakuMoonShotStep`, one wave.
  - *Behavior*: A large light mote gathers on the boss and sinks about a sprite height over 1.2s, then condenses into three pale moons. They set off on a ~17° fan, breathing as they drift down at 250 px/s. Each carries a proximity fuse that trips 181 px from the player, well short of contact, leaving a stationary 126 px crater that swells and breaks up over about a second. On a centred cast the middle moon fires first and the outer two follow ~0.17s later, matching the Rank 1 reference; cast from off to one side the near moon goes first and the far one leaves the screen without bursting at all, matching Rank 16. When all three land they overlap into a wall across most of the field width. Nobody dies to the shot; they die to flying into what it leaves behind.
  - *Rank Scaling*: None, per measurement. See `DanmakuMoonShotStep` above.
  - *Audio*: None wired yet (`sfx = ""`).
  - *Open*: Spell name is transcribed from the reference banner and may want correcting against an official localisation. All three of her boss attacks are now built.
- **Level 4 Boss Attack 2**: [`reisen_boss_spell_2.tres`](resources/spellcards/reisen/reisen_boss_spell_2.tres)
  - *Name*: `Scatter Sign "Luna Megalopolis" - Rings of Red Bullets`. Same convention as Attack 1: the spellcard is Scatter Sign "Luna Megalopolis", and the attack label is ours.
  - *Composition*: A single `DanmakuStaggeredRingsStep` (`reisen_wave_bullet`, 8 rings of 36, `ring_interval = 0.28`, `ring_rotation_spread = 1.0`), one wave.
  - *Behavior*: Eight rings of red rice bullets leave the boss a third of a second apart and expand at one constant speed, so they roll outward as an evenly spaced pulse. Every ring is rotated by its own random amount, anywhere inside the 10° gap between neighbouring bullets, so the radial corridors a player would thread never line up from one ring to the next and cannot be anticipated either. The safe gap has to be re-found on every ring rather than held. 288 bullets per volley, 1.96s of cast.
  - *Rank Scaling*: Speed only, 130 px/s -> 310 px/s. Ring count, density and interval are identical at both ranks.
  - *Audio*: `se_tan00` once per ring.
  - *Open*: Nothing outstanding on the shape. The rotations are random by design and there is no figure left to fit; what is unverified is how the card actually plays at Rank 16, where the rings cross the field in 2.3s.
- **Level 4 Boss Attack 3**: [`reisen_boss_spell_3.tres`](resources/spellcards/reisen/reisen_boss_spell_3.tres)
  - *Name*: `Scatter Sign "Luna Megalopolis" - Purple Bullet Spiral Machinegun`. Same convention as Attacks 1 and 2: the spellcard is Scatter Sign "Luna Megalopolis", and the attack label is ours (the user's, in this case).
  - *Composition*: A single `DanmakuSpiralMachinegunStep` (`reisen_spiral_bullet`, `spin_rate_deg = 300`, `duration = 2.2`, interval 0.019 -> 0.016, speed 490 -> 790), one wave.
  - *Behavior*: A barrel out of the boss's hitbox fires purple rice bullets flat out while turning clockwise at 300°/s, for 2.2s, which is a little under two full turns. The bullets fly straight, so what builds on screen is a single arm winding outward, tight round the boss and opening up as it goes. The only way through is to cross the arm between two bullets, and since the arm keeps coming round, the crossing has to be taken again every 1.2s. Her fastest attack by a wide margin and her simplest.
  - *Rank Scaling*: Speed, mostly: 490 -> 790 px/s, which nearly doubles the spiral's pitch against an unchanged spin. The shot interval tightens only slightly, 0.019 -> 0.016s, because the reference barely tightens it either and closing it further turns the arm into a wall. See `DanmakuSpiralMachinegunStep` above.
  - *Audio*: `se_tan00` every 6th shot, which at ~53 shots/s is roughly 9 a second: a machinegun rattle rather than 53 overlapping copies of one sample.
  - *Open*: The user's reading of the reference is that Rank 16 adds bullets; measurement says it adds speed and leaves the spacing near enough alone. The build follows the measurement, since the literal reading closes the gap to 2px and makes the attack unsurvivable, but the Rank 16 feel is the thing to check in play.

### Yuuka Kazami
- **Level 2 Spellcard**: [`yuuka_spell_lv2.tres`](resources/spellcards/yuuka/yuuka_spell_lv2.tres)
  - *Name*: Flower Sign "Blossoming of Gensokyo" (花符「幻想郷の開花」)
  - *Composition*: One `DanmakuBlossomStep`, one wave. Rice (`yellow_rice`) on even emissions, pellets (`yellow_pellet`) on odd.
  - *Behavior*: Two counter-turning sets of 6-way rings pour out of one point near the top of the field, each ring faster than the last. They cross into six petals that keep opening as they fall.
  - *Rank Scaling*: 5 to 13 emission pairs; the top speed goes from 294 to 594 px/s because the pattern simply runs longer.
- **Level 3 Spellcard**: [`yuuka_spell_lv3.tres`](resources/spellcards/yuuka/yuuka_spell_lv3.tres)
  - *Name*: Flower Sign "Blossoming of Gensokyo" (花符「幻想郷の開花」)
  - *Composition*: Same step with `yellow_arrow` / `dark_yellow_arrow` and `alt_speed_step = 17.5`.
  - *Behavior*: Same flower in arrowheads. The two rings ride speed tracks 0.15 and 0.14 apart and swap them every emission, so the petals fray into yellow and amber layers as they travel.
- **Level 4 Boss Attack 1**: [`yuuka_boss_spell_1.tres`](resources/spellcards/yuuka/yuuka_boss_spell_1.tres) (`pl09.ecl` `sub3`; the name is ours)
  - *Composition*: Two `DanmakuRingStep`s in `DECEL_AND_HOME` with `relaunch_relative` and `bounce_off_walls`, sharing the wave's random angle; the yellow ring sits on the half steps. A 1s hold after, for the sub's `+60`.
  - *Behavior*: A red and a yellow ring of `rank + 16` ring balls each, at 137.5 -> 325 px/s. Each ball brakes to a stop over 70 frames (ZUN's `AimRel`), turns 90 degrees from its own heading (red clockwise, yellow counter-clockwise) and relaunches at its first speed. Each can ricochet once off the left, right or top wall, at any point (ZUN's `BounceNonBottom`, flag `0x800`; `0x40` is the `AimRel`).
- **Level 4 Boss Attack 2**: [`yuuka_boss_spell_2.tres`](resources/spellcards/yuuka/yuuka_boss_spell_2.tres) (`pl09.ecl` `sub4`; the name is ours)
  - *Composition*: One `DanmakuBallStreamStep` with `yellow_big_ball`, then 14 frames of hold (the sub's trailing `+4` and `+10`). 3.5s of cast in all.
  - *Behavior*: Six solid chains of big balls fan out around the victim, who stands between two of them; the chains' tails curl round one way near the end. Same at every rank.
- **Level 4 Boss Attack 3**: [`yuuka_boss_spell_3.tres`](resources/spellcards/yuuka/yuuka_boss_spell_3.tres) (`pl09.ecl` `sub5`; the name is ours)
  - *Composition*: The Level 2 `DanmakuBlossomStep` with `orange_rice` / `orange_pellet` and `spawn_y_override = -1`, then a 1s hold (the sub's `+60`).
  - *Behavior*: The Level 2 flower, same counts and speeds (`IC2 / 2 + 5` pairs), in orange and fired from wherever the boss stands instead of the fixed top-centre point.

### Clownpiece (TH15, not in PoFV)
- **Level 4 Boss Attack 1**: [`clownpiece_boss_spell_1.tres`](resources/spellcards/clownpiece/clownpiece_boss_spell_1.tres), Hellfire "Infernal Essence of Grazing" (TH15 `st05bs.ecl` `BossCard3`)
  - *Composition*: A single `DanmakuRepelRingStep` (`clownpiece_flame_red`, 3 rings of 144), one wave.
  - *Behavior*: Gapless rings of flames that bend around the player into a narrow gap; standing still is hit by the aimed flame.
- **Level 4 Boss Attack 2**: [`clownpiece_boss_spell_2.tres`](resources/spellcards/clownpiece/clownpiece_boss_spell_2.tres), Hell Sign "Star and Stripe" (TH15 `st05bs.ecl` `BossCard2`)
  - *Composition*: A single `DanmakuStarAndStripeStep`, one wave (~3.3s).
  - *Behavior*: Red stripes stack up from the left wall and all sink together while stars rain through the gaps; ride a gap down and dodge the stars.

### Lyrica Prismriver
- **Level 2 / Level 3**: [`lyrica_spell_lv2.tres`](resources/spellcards/lyrica/lyrica_spell_lv2.tres) / [`lyrica_spell_lv3.tres`](resources/spellcards/lyrica/lyrica_spell_lv3.tres), Noise Sign "Soul Noise Flow" (`pl06.ecl` `sub0` / `sub1`)
  - *Composition*: A single `DanmakuSnakingFanStep`, one wave (~2.8s with the pre-cast).
  - *Behavior*: Fans of snaking bullets pour down from the centre and settle into slanted lines; Lv 3 weaves two colours snaking opposite ways.

---

## 5. Adding New Patterns, Characters & Bosses (Workflow)

When creating new spellcards or character patterns:
1. **Check Existing Bullets**: First check Section 2 above. If the required bullet sprite and color already exist, reuse that `.tres` file.
2. **If New Bullet Sprite Needed**:
   - Save sprite as a 32×32 PNG in `assets/bullets/` (using integer $2\times$ nearest-neighbor scaling if extracted from Touhou sheets).
   - Create a corresponding `DanmakuBulletData` `.tres` in `resources/bullets/`.
   - Update Section 2 of this document.
3. **If New Step Logic Needed** (e.g. expanding spiral, aimed cone spread, streaming lasers):
   - Create a new script in `scripts/resources/` extending `DanmakuStep` (`danmaku_spiral_step.gd`, `danmaku_spread_step.gd`, etc.).
   - Implement `execute(playfield, origin, rank)`.
   - Update Section 3 of this document.
4. **Assemble in Inspector**:
   - Create a new `SpellcardData` resource in `resources/spellcards/<character>/`.
   - Add your steps to the `steps` array, configure rank ranges, and set `wave_interval`.
   - Assign to the character in `resources/characters/<character>.tres`.

---

## 6. Level 4 Boss Characters (`resources/bosses/`)

Level 4 Spellcards summon an interactive Boss character illusion onto the opponent's playfield (`BossCharacter`, `scenes/enemies/boss_character.tscn`).

- **Resource Class**: [`BossData`](scripts/resources/boss_data.gd)
- **Boss Sprite Specifications**:
  - Frame Dimensions: $64 \times 80$ pixels.
  - Sheet Layout: 4 columns (`hframes = 4`), 3 rows (`vframes = 3`), total resolution $256 \times 240$ px.
  - Render Scale: $2.33\times$ (`sprite_scale = Vector2(2.33, 2.33)`), yielding a balanced $149 \times 187$ px rendered presence.
  - Hitbox Collision: $32.0$ px radius circle.
  - **Row 0 (Frames 0–3)**: Idle hovering loop (loops continuously).
  - **Row 1 (Frames 4–7)**: Movement / banking swoop. Advances to final frame (index 3) and holds on that finished state while traveling (does not loop). Moving left sets `flip_h = true`; moving right sets `flip_h = false`.
  - **Row 2 (Frames 8–11)**: Spellcard cast / gohei sweep attack pose. Holds on final frame during cast.
- **Motion & Lifecycle**:
  - **Entrance**: Swoops in from upper offscreen right (`Vector2(170, -260)` offset) to upper center (`Vector2(300, 150)`) over 0.85s.
  - **Cadence Rhythm Loop**: The combat cadence runs on a finite-phase rhythm loop:
    $$[\text{Entrance}] \xrightarrow{0.4\text{s}} [\text{Cast Attack}] \xrightarrow{0.3\text{s}} [\text{Evasive Hop (0.65s)}] \xrightarrow{0.25\text{s}} [\text{Cast Next Attack}] \dots$$
  - **Hopping**: Snappy, wide evasive leaps of $90\text{--}160\text{ px}$ across the upper roam area ($X \in [140, 460]$, $Y \in [110, 210]$) with a $0.65\text{s}$ travel duration.
  - **Departure & Action Quotas**:
    - `DepartureMode.ATTACK_COUNT`: The boss stays on screen until it performs a quota of attacks, randomized between **`attacks_min = 5` and `attacks_max = 9`** (frequently 8, authentic to empirical PoFV testing across all ranks). Upon fulfilling the quota, the boss departs cleanly via `leave_screen()`.
    - `DepartureMode.DURATION`: Time-based screen departure. If `randomize_duration = true`, a target duration is rolled once at entrance completion (`randf_range(duration_min, duration_max)`, cached in `_target_duration`) instead of using the fixed `duration` value directly - e.g. Cirno rolls a fresh 15–20s target every time she's summoned. `duration_safety` remains a separate, always-active fallback ceiling for the other departure modes.
    - `DepartureMode.HYBRID`: Depart when whichever condition is reached first.
  - **Timeout Explosion**: Upon departure or expiration, the boss detonates into an expanding red shockwave (`DefeatShockwave`), popping and vanishing cleanly in place.
- **Boss Attack System**:
  - `attack_patterns: Array[SpellcardData]`: Pool of modular boss spellcards.
  - `selection_mode: AttackSelectionMode`:
    - `AttackSelectionMode.RANDOM` (default for Reimu & Marisa): Purely random pick from the spellcard pool, with repeats allowed.
    - `AttackSelectionMode.SEQUENTIAL`: Fixed spell itinerary cycling linearly through the array (available for future characters).
  - `initial_attack_delay`: Initial rest period after entrance completes before starting the first cast (0.4s).
  - `post_cast_delay`: Rest period after cast finishes before hopping (0.3s).
  - `post_hop_delay`: Rest period after hop finishes before casting (0.25s).
  - **Attack Execution**:
    - Boss enters cast state (`_is_casting = true`), transitions to Row 2 cast animation (`_anim_row = 2`), and emits `attack_started(spell_name)`.
    - Fires the selected `SpellcardData` pattern originating from the boss's current position at the playfield's current rank.
    - Holds the cast pose for $0.55\text{s}$, then smoothly returns to Row 0 idle hover (`_anim_row = 0`, `_is_casting = false`) and proceeds into post-cast delay before hopping.
- **Existing Boss Data**:
  - `resources/bosses/reimu_boss.tres`: Reimu Hakurei boss entity using authentic `assets/characters/reimu/reimu_boss.png` (4 columns $\times$ 3 rows, 64x80 px/frame, $2.33\times$ scale), linked with all 5 Reimu boss spells in RANDOM selection mode with 5–9 randomized attack quota.
  - `resources/bosses/marisa_boss.tres`: Marisa Kirisame boss entity using authentic `assets/characters/marisa/marisa_boss.png` (4 columns $\times$ 3 rows, 64x80 px/frame, $2.33\times$ scale), configured with 5–9 randomized attack quota, linked with all 5 Marisa boss spells (`marisa_boss_spell_1.tres`, `marisa_boss_spell_2.tres`, `marisa_boss_spell_3.tres`, `marisa_boss_spell_4.tres`, and `marisa_boss_spell_5.tres`) in RANDOM selection mode.
  - `resources/bosses/youmu_boss.tres`: Youmu Konpaku boss entity using authentic `assets/characters/youmu/youmu_boss.png` (5 columns $\times$ 3 rows, 64x80 px/frame, $2.33\times$ scale), linked with all 3 Youmu boss spells in RANDOM selection mode.
  - `resources/bosses/reisen_boss.tres`: Reisen Udongein Inaba boss entity using `assets/characters/reisen/reisen_boss.png` (4 columns $\times$ 3 rows, 48x80 px/frame, $2.33\times$ scale — narrower than the other bosses' 64x80 frames because her source art is, but the same 80px frame height means she renders to a matching $\approx 112 \times 186$ px presence). Extracted by [`tools/generate_reisen_boss_sheet.gd`](tools/generate_reisen_boss_sheet.gd) from the existing `reisen_raw_sheet.png` already committed to the repo (never re-ripped from the original game files), same provenance rule as Cirno. Unlike Cirno's, these frames are **already on a transparent background**, so no chroma-key or flood fill is involved. The source packs a 4-column $\times$ 2-row boss grid flush against the art in the sheet's lower-middle block: origin $(175, 159)$, 64px column pitch, two 80px rows split by a 1px gutter. Both coordinates are load-bearing — the grid is pinned off the wider bank row, after which the idle frames sit at a dead-consistent 8px inside every cell (which is what confirms the pitch), and starting a single row lower at $y=160$ shears the top pixel off the rabbit ears. Row semantics follow Cirno: source row 0 (steady forward pose) serves **both** Row 0 idle hover and Row 2 cast, since the source has no separate idle animation; source row 1 (banking lean, drawn moving right) becomes Row 1 movement and is mirrored via `flip_h` for the other hop direction. Uses `DepartureMode.DURATION` with a randomized 15–20s stay, matching Cirno. **Roams lower than the other bosses** (`entrance_target_pos` $y=240$, `roam_bounds` $y \in [195, 295]$, against the shared $[110, 210]$). Both bounds are read off the reference rather than chosen: converting the two casts we have footage of gives boss $y \approx 197$ (the high cast in the user's Rank 16 screenshot) and $y \approx 287$ (the Rank 1 video), so the band spans both. Deriving those required knowing that the moons spawn already spaced rather than from a point — reading the fan as convergent put both casts nearly 120 px too high. Per-character, so the other five bosses are untouched. This matters because Triple Moon Shot's fuses reach differently from different heights — see the crater-count table under `DanmakuMoonShotStep`. An earlier pass instead dropped her a flat 165 px to $[275, 375]$ so that a centred cast would always land all three craters; that was over-correction on a misread, since PoFV's boss demonstrably does return to the top of the field and thinner volleys from up there are authentic. Linked with all three boss spells (`reisen_boss_spell_1.tres`, `reisen_boss_spell_2.tres`, `reisen_boss_spell_3.tres`), fired in sequence.
  - `resources/bosses/yuuka_boss.tres`: Yuuka Kazami, `assets/characters/yuuka/yuuka_boss.png` (4x3 of 64x64, $2.4\times$), built by [`tools/generate_yuuka_boss_sheet.gd`](tools/generate_yuuka_boss_sheet.gd) from `yuuka_boss_raw.png`. The raw sheet has only two cast frames (`pl09.anm` script 33 flickers between them), so row 2 is laid out 68, 69, 68, 69. Movement is from `pl09.ecl` `sub2`: enters to ECL (0, 128) = (300, 267), roams `move_bounds` (-96, 32)-(96, 128) = `Rect2(100, 67, 400, 200)`, and between attacks does only the 60-frame `move_rand_interp` (1s hop, no rests). Leaves after `RAND_INT % 5 + 5` = 5-9 attacks. Built attack by attack; so far Boss Attack 1 only.
  - `resources/bosses/cirno_boss.tres`: Cirno boss entity using `assets/characters/cirno/cirno_boss.png` (4 columns $\times$ 3 rows, 62x64 px/frame — slightly smaller than the other bosses' 64x80 frames; $2.4\times$ scale compensates so her on-screen footprint still reads similar to the other bosses). Unlike Reimu/Marisa/Youmu (all `DepartureMode.ATTACK_COUNT`), Cirno uses `DepartureMode.DURATION` with `randomize_duration = true` (`duration_min = 15.0`, `duration_max = 20.0`) - she stays on screen for a randomized 15–20s per user spec rather than departing after a fixed attack quota. Extracted directly from the existing `cirno_raw_sheet.png` already committed to the repo (never re-ripped from the original game files): the sheet's bottom-right quadrant holds a 4-column $\times$ 3-row grid of larger boss-scale frames (distinct from the small 32x48 playable-character frames used elsewhere on the same sheet), confirmed against a user-annotated reference (`cirno_animations.png`, frames numbered 1-12). Row semantics (standard row-major, matching Reimu/Marisa/Youmu): source row 0 (frames 1-4) = attack/still pose, used for **both** Row 0 idle hover and Row 2 cast (no separate idle animation exists in the source, so the steady casting pose doubles as her resting hover). Source row 1 (frames 5-8) = "moving right" banking lean, used for Row 1 movement — `BossCharacter` already mirrors this single movement row via `flip_h` for the opposite hop direction (same convention as the other 3 bosses), so the source's separate "moving left" row (frames 9-12) is left unused. Background keyed from opaque white to transparent via a border-flood-fill (only white regions contiguous with each frame's edge are cleared, preserving interior whites like the dress and socks). Currently linked with 1 boss spell (`cirno_boss_spell_1.tres`); more will be added attack-by-attack.

---

## 7. Mid-Boss Hazards (`scenes/enemies/lily_white.tscn`)

### Lily White Mid-Boss Entity
Lily White appears periodically in both playfields during extended matches to dispense dual-field danmaku and create high-risk bullet congestion.

- **Sprite Asset**: `lily_white` DatTexture, enemy.anm sprites 84-95 from th09.dat (256x192 px, 4 columns $\times$ 3 rows of 64x64 px frames, scaled $2.0\times$ nearest-neighbor).
- **Spawn Interval**: Exactly **50.0 seconds** into each round, and every **30.0 seconds** thereafter (50s, 80s, 110s, ...).
- **Match Timer**: Centered top HUD panel (`scenes/ui/match_timer.tscn`) displaying elapsed round time in `MM:SS`. Pauses during round defeat hitstop/transition and resets on round start.
- **Motion Lifecycle**:
  1. **Descent**: Enters from `(300, -100)` offscreen top to `(300, 500)` (halfway down the 960px playfield) over **2.0 seconds** with quadratic ease-out (`Tween.TRANS_QUAD, Tween.EASE_OUT`).
  2. **Rest**: Hovers at `(300, 500)` for **0.8 seconds**.
  3. **Retreat Ascent**: Floats back up to `(300, -100)` over **4.0 seconds** with quadratic ease-in (`Tween.TRANS_QUAD, Tween.EASE_IN`).
- **Health & Damage Calibration**:
  - `max_health = 80.0`: **3.0 seconds of uninterrupted player fire** ($3.0\text{s} \times 26.67\text{ DPS} = 80\text{ damage}$, 40 volleys), matching PoFV footage where she dies to ~3.3s of normal fire.
  - `take_damage(amount, source) -> bool`: Consumes colliding player bullets (`return true`), triggers high-brightness hit flash (`modulate = Color(2.5, 2.5, 2.5)`), and decrements HP.
  - **Defeat Shockwave**: Emits `defeated(death_pos)` when lethal damage is dealt, terminating motion tweens and detonating into a `HeavyShockwave` via `_on_lily_white_defeated(death_pos)` in `playfield.gd`.
- **Retreat Danmaku Barrage (Dual-Spiral Claw Triplets & Outlined Pellets Combo)**:
  - **Anchor Origin**: Anchored to Lily White's position as she floats upward from $Y=500$ to $Y=-100$.
  - **Cadence**: 48 sequential waves fired at $0.070\text{s}$ intervals across her retreat ascent ($3.29\text{s}$ active firing duration, 1.5x faster repeat rate matching PoFV).
  - **Arm 1 (Right Vector / Blue)**:
    - Starts pointing diagonally up-right (`-45.0°`).
    - Rotates **clockwise** (`+6.0°` per wave) sweeping through horizontal right (`0°`), down-right (`+45°`), straight down (`+90°`), to down-left (`+143°`).
    - Theme color: Blue (`Color(0.4, 0.8, 1.0)`).
  - **Arm 2 (Left Vector / Red)**:
    - Starts pointing diagonally up-left (`-135.0°`).
    - Rotates **counter-clockwise** (`-6.0°` per wave) sweeping through horizontal left (`-180°`), down-left (`+135°`), straight down (`+90°`), to down-right (`+47°`).
    - Theme color: Red (`Color(1.0, 0.35, 0.45)`).
    - Preserves exact mathematical mirror symmetry across the playfield vertical centerline: $\cos(\theta_{\text{red}}) = -\cos(\theta_{\text{blue}})$ and $\sin(\theta_{\text{red}}) = \sin(\theta_{\text{blue}})$.
  - **Claw Formation & Twin Combo (with Organic Jitter)**:
    - Each vector fires a 3-prong fan (`[-24.0°, 0.0°, +24.0°]`).
    - **Strand Separation (do not break this)**: `angle_jitter_deg` must stay well under half of `claw_spread_deg`. The original `13.0` / `7.0` pairing let the $-13°$ prong cover $[-20°, -6°]$ while the $0°$ prong covered $[-7°, +7°]$ - the ranges overlapped, so neighbouring prongs landed on each other every wave and all three strands smeared into one solid wall with no lanes through it. Reference footage of PoFV shows clearly separated strands with wide navigable gaps between them, which is what the current `24.0` / `2.0` pairing restores.
    - **Dynamic Chaos / Jitter**: Retained for organic variation, but scaled back so it perturbs a strand rather than dissolving it:
      - Angle jitter: Each prong applies a randomized perturbation within `[-2.0°, +2.0°]` (`angle_jitter_deg = 2.0`).
      - Speed jitter: Each prong varies within $[205.0, 235.0]\text{ px/s}$ (`bullet_speed = 220.0`, `speed_jitter = 15.0`) - tightened from $\pm 35$ so the beads stay evenly spaced along each strand instead of clumping.
    - **Locked Twin Synchronization**: The lead solid pellet and trailing circled ring pellet in each prong share the **exact same jittered angle and speed**:
      1. **Lead**: Solid pellet (`is_ring = false`) spawned $30\text{ px}$ ahead along the jittered heading vector (`twin_offset_px = 30.0`), establishing a clean visible gap.
      2. **Trailing**: Outlined ring pellet (`is_ring = true`) spawned at origin.
    - Yields 12 bullets per wave ($2 \times 3 \times 2$), totaling **384 bullets across all 32 waves** (`barrage_wave_count = 32`), emitted over $3.36\text{s}$ at `barrage_step_interval = 0.105`.
    - **Density is coupled to the wave count - read this before retuning.** The three cadence values are one setting in three parts: duration is $\text{count} \times \text{interval}$ and the swept arc is $\text{count} \times \text{rotation}$, so both survive any 1.5x-style rescaling of the trio - but total bullets is $\text{count} \times 12$, which does not. Commit `22a2973` rescaled all three to `48 / 0.070 / 4.0` for a smoother sweep, held duration at $3.36\text{s}$ and the arc at $192°$, and silently took output from 384 to **576** bullets. Against the per-field active bullet cap of 350 that barrage could saturate the field outright, and side-by-side with PoFV reference footage it read as roughly twice the reference density. Reverted to `32 / 0.105 / 6.0`. The wider angular step also spaces the beads along each arc closer to the reference, which shows rings separated by roughly their own diameter.
  - **Lifecycle & Interruption**:
    - Barrage sequence is driven by a Godot 4 `Tween` (`_barrage_tween`).
    - If Lily is defeated or cleaned up before completion, `_barrage_tween.kill()` cleanly halts future waves instantly.
- **Debug Shortcuts**:
  - Press `[F8]` during arena gameplay to immediately spawn Lily White on both playfields.
  - In the Debug Menu (Backspace) under "World & Spawners", click **"🌸 Spawn Lily White (Both Fields) [F8]"**.

---

## 7. Spellcard Declarations & "Action Stop" Architecture

In *Touhou 09: Phantasmagoria of Flower View*, declaring a Level 2, 3, or 4 Spellcard does not fire projectiles instantaneously. Instead, it triggers an **Action Stop** sequence that momentarily freezes both players, presents cinematic notifications, and then resumes play as the attacks deploy.

### 1. Timing & State Progression
```text
Caster Releases Charge (Lv 2-4) / Remote RPC Received / Test Hotkey (F3/F4/F7)
  ├── 1. Heavy Shockwave detonates on Caster Field (vaporizes local pellets)
  ├── 2. Caster Sprite flashes solid red silhouette via red_silhouette.gdshader
  ├── 3. Caster Field displays 288x85 authentic Spell Attack Notification banner
  ├── 4. Target Field displays red "WARNING" and "Clear Fairy Level 0X" subtitle + Spell Name
  ├── 5. get_tree().paused = true (Entire SceneTree freezes for ~0.58s)
  │      └── Processed unpaused via Tween.TWEEN_PAUSE_PROCESS & PROCESS_MODE_ALWAYS
  └── 6. Timeout expires (~0.58s):
         ├── get_tree().paused = false
         ├── Caster sprite material restored to normal
         └── Danmaku pattern / Boss Character spawned on Target Field
```

### 2. Character Banner Assets (`CharacterData.spell_banner_texture`)
- **Dimensions**: $288 \times 85\text{ px}$ extracted directly from character raw sheets (`x: 1, y: 439, width: 288, height: 85`).
- **Render Mode**: $2\times$ nearest-neighbor integer scaling ($576 \times 170\text{ px}$ centered horizontally in 600px playfield at $X=12.0\text{ px}$, $Y=340.0\text{ px}$).
- **Reimu Hakurei**: `assets/characters/reimu/reimu_spell_banner.png` (Reimu portrait + "SPELL ATTACK" + "行雲流水")
- **Marisa Kirisame**: `assets/characters/marisa/marisa_spell_banner.png` (Marisa portrait + "SPELL ATTACK" + "疾風怒濤")
- **Yuuka Kazami**: `assets/characters/yuuka/yuuka_spell_banner.png` (Yuuka portrait + "SPELL ATTACK" + "月に叢雲 花に風"), joined from `pl09_ct00.png`'s 256x85 body and 32x85 end by `tools/generate_yuuka_spell_banner.gd` (`pl09.anm` scripts 15 / 16 butt them together at x -16 / 128).

### 3. Level 4 Spellcard Background Graphics Overlay (`cdbg*`)
During Level 4 Spellcard declarations / Boss invocations, the playfield background displays an authentic two-layer composite overlay extracted from `th09.dat` -> `pl*.anm`:
- **Reimu Hakurei** (`pl00.anm`):
  - **Static Base** (`assets/characters/reimu/reimu_spell_bg_base.png` / `cdbg00.png`): $256 \times 256\text{ px}$, scaled `(1.125, 1.75)`, positioned at `(-144, 0)`, alpha fades in to 255 over 60 frames (1.0s). Shows large glowing blue Yin-Yang orb against dark foliage backdrop.
  - **Animated Top Layer** (`assets/characters/reimu/reimu_spell_bg_anim.png` / `cdbg00b.png`): $256 \times 256\text{ px}$, scaled `2.08x`, positioned at `(0, 224)`, alpha fades in over 60 frames with continuous clockwise rotation at $\omega = -0.00785\text{ rad/frame}$ ($\approx -0.45^\circ/\text{frame}$). Shows dark red floral Yin-Yang motif.
- **Marisa Kirisame** (`pl01.anm`):
  - **Static Base** (`assets/characters/marisa/marisa_spell_bg_base.png` / `cdbg01.png`): $256 \times 256\text{ px}$, scaled `(1.125, 1.75)`, positioned at `(-144, 0)`, alpha fades in to 255 over 60 frames (1.0s). Shows overlapping luminous magical circles at bottom.
  - **Animated Top Layer** (`assets/characters/marisa/marisa_spell_bg_anim.png` / `cdbg01b.png`): $256 \times 256\text{ px}$ seamless tiling texture, tiled at `uv_scale = Vector2(1.125, 1.8)` to maintain exact 1:1 isotropic aspect ratio ($533.33\text{ px}$ per UV repetition in both X and Y) on our $600 \times 960$ playfield, eliminating vertical stretching. Continuous upward UV scroll at $v = 0.667\text{ UV/s}$ ($0.01111\text{ UV/frame}$). Shows repeating hexagonal honeycomb cellular pattern.
- **Youmu Konpaku** (`pl03.anm`):
  - **Static Base** (`assets/characters/youmu/youmu_spell_bg_base.png` / `cdbg03.png`): $256 \times 256\text{ px}$, scaled `(1.125, 1.75)`, positioned at `(-144, 0)`, alpha fades in to 255 over 60 frames (1.0s). Shows mystical deep teal/indigo Netherworld backdrop with rain streaks, weeping branches, swirling mist, and an antique Japanese carriage wheel (*genji-guruma*) crest in the lower left.
  - **Animated Top Layer** (`assets/characters/youmu/youmu_spell_bg_anim.png` / `cdbg03b.png`): $256 \times 256\text{ px}$, scaled `2.08x`, positioned at `(0, 224)`, alpha fades in over 60 frames with continuous clockwise rotation at $\omega = -0.00785\text{ rad/frame}$ ($\approx -0.45^\circ/\text{frame}$). Shows dark cherry blossom (sakura flowers and falling petals) pattern.
- **Cirno** (`pl05.anm`):
  - **Dual Counter-Scrolling Ice Overlay** (`assets/characters/cirno/cirno_spell_bg_base.png` / `cdbg00.png`): $256 \times 256\text{ px}$ 4-way kaleidoscopic mirrored blue ice crystal / mineral frost texture. Renders via `shaders/spell_bg_counter_scroll.gdshader`: Layer 1 scrolls downward with velocity $+0.04\text{ UV/s}$, while a second counter-scrolling instance of the exact same texture scrolls upward with velocity $-0.04\text{ UV/s}$ at $50\%$ alpha (`counter_layer_alpha = 0.5`), reproducing the optical interference and shimmering ice-cavern illusion of Freeze Sign "Cold Divinity" in 1 draw call.
- **Yuuka Kazami** (`pl09.anm`, `spell_bg_anim_type = "scroll_rotate"`):
  - **Scrolling Base** (`assets/characters/yuuka/yuuka_spell_bg_base.png` / `cdbg09b.png`): the dotted field, drawn at 1:1 over the whole field (script 13: 288x448 sprite off a 256 texture) and scrolling up 0.0033 of a texture a frame (0.2/s) through `AnimScroll`, whose material each field now duplicates so two characters' scroll speeds cannot collide.
  - **Turning Sunflower** (`assets/characters/yuuka/yuuka_spell_bg_anim.png` / `cdbg09.png`): scale 2.0804 on the centre like Reisen's layers, **additive** (`blend_mode(Additive)`, `spell_background_additive.tres`), turning -0.00785 rad/frame. Both fade in over 60 frames. Matched against `yuuka_level4_rank1.mp4`: same centre and size; PoFV's petals read ~20 levels brighter, within video compression.
- **Sakuya Izayoi** (Boss Lv4 only, no player-cast Lv2/3 background):
  - **Dual Counter-Rotating Overlay** (`spell_bg_anim_type = "dual_rotate"`, new mode - two independent `Sprite2D` layers spinning opposite directions at the same $\omega = 0.4712389\text{ rad/s}$ ($\approx 27^\circ/\text{s}$) magnitude used by the existing single-layer `"rotate"` mode):
    - **Bottom Layer - Time-Space Distortion** (`assets/effects/sakuya_distortion.png`, `spell_bg_base_texture`): $1113 \times 1113\text{ px}$ full-bleed crimson radial warp/shockwave texture (native size already exceeds the $600\times960$ playfield diagonal). Rendered on the new `AnimRotateBase` node at `scale = 1.05x`, centered at `(300, 480)`, rotating **clockwise** (positive direction, opposite the clock layer).
    - **Top Layer - Pocket Watch** (`assets/effects/sakuya_clock.png`, `spell_bg_anim_texture`): $256 \times 256\text{ px}$ ornate stopped clock-face motif. Reuses the existing `AnimRotate` node (`scale = 4.33333x`, centered at `(300, 480)`), rotating **counter-clockwise**, imposed on top of the distortion layer at $50\%$ alpha (`modulate.a = 0.5`, set only in `dual_rotate` mode so it doesn't affect Reimu/Youmu's opaque `"rotate"` usage) so the distortion swirl shows through underneath.
    - No static `BaseRect` layer is used for this character - both textures are full-screen rotating sprites.

### 4. Engine Architecture & Node Execution
- **`get_tree().paused`**: Freezes all gameplay entities, physics, timers, and particle emitters simultaneously across both playfields without requiring manual pause booleans on individual entities.
- **`PROCESS_MODE_ALWAYS` Nodes**:
  - `NetworkManager`: Ensures WebRTC signaling, ping, and multiplayer RPCs are never interrupted during pauses.
  - `SpellBanner`: Runs slide-in, freeze hold, and slide-out tweens unpaused during Action Stop.
  - `DebugMenu`: Retains responsiveness during pauses.
  - `Arena`: Orchestrates action stop tweens and pauses/unpauses SceneTree.

---

## 8. Modular Spellcard Heavy Shockwaves & Screen Reset

Upon releasing a Level 2, 3, or 4 Spellcard, a defensive circular shockwave emanates from the caster's position. It executes **after the Action Stop freeze concludes**, expanding at a measured, dramatic speed ($2\times$ slowed, $\approx 1.20\text{--}1.30\text{s}$) across the active playfield to vaporize projectiles and enemies in range.

### 1. Radius Calibration & Screen Coverage
- **Level 2 Spellcard**: Encompasses ~50% of the screen ($R = 420\text{ px}$). Clears immediate threats around the caster.
- **Level 3 Spellcard**: Encompasses ~75% of the screen ($R = 650\text{ px}$). Covers the majority of the combat zone.
- **Level 4 Spellcard**: True screen reset button ($R = 1200\text{ px}$). Encompasses 100% of the playfield ($600 \times 960\text{ px}$) from any position on the board.

### 2. Multi-Layered Visual Ring Design
Rendered via a hybrid polar runic ribbon shader and custom `_draw()` vector arcs on `HeavyShockwave`:
1. **Authentic Polar Runic Ribbon (`shaders/polar_ring.gdshader`)**:
   - Mapped dynamically from the authentic 16×128 pixel-art calligraphy ribbon (`assets/effects/spellcard_shockwave_ribbon.png`).
   - Maintains a constant 28px ribbon thickness (`inner_radius = R - 14px`, `outer_radius = R + 14px`) as it expands.
   - Rotates continuously (`SPIN_SPEED = 2.2 rad/s`), additive blending (`render_mode blend_add`), with 1.5px sub-pixel edge feathering.
2. **High-Luminance Vector Rim Arcs (`_draw()`)**:
   - **Outer Soft Glow**: 14px width, $\alpha = 0.35 \times \text{fade}$, tinted to character theme color.
   - **Saturated Mid-Rim**: 6px width, $\alpha = 0.80 \times \text{fade}$, richly saturated character theme color.
   - **White Energetic Core**: 2.5px width, $\alpha = 0.95 \times \text{fade}$, brilliant white core ring.

- **Reimu Hakurei**: Shrine maiden red-pink (`Color(1.0, 0.35, 0.45, 1.0)`).
- **Marisa Kirisame**: Magician magenta/spark pink (`Color(1.0, 0.45, 0.85, 1.0)`).

### 3. Clearing Hierarchy & Fairy Detonation Rules (Option A)
- **Entities Vaporized**:
  - Normal & Big Enemy Pellets (`EnemyPellet`).
  - Danmaku Bullets & Stars (`DanmakuBullet`).
  - Yin-Yang Orbs (`YinYangOrb`).
  - Earth Light Rays / Laser hazards (`EarthLightRay`).
  - Small Fairies, Great Fairies (`Fairy`), and Spirits (`Spirit`).
- **Fairy / Spirit Detonation Cascade (Option A)**:
  - Fairies and Spirits struck by a heavy shockwave do not die instantly; their death is delayed by $\approx 0.12\text{s}$ (spirits $0.10\text{s}$) while they continue travelling along their path.
  - This brief delay gives the rapidly expanding heavy wave sufficient time to sweep and vaporize all surrounding pellets and bullets first.
  - Upon timer expiration, the fairy or spirit bursts into its own standard visual circular `Shockwave` ring at its death position.
  - **Zero Retaliatory Attacks**: Defeated fairies and spirits award passive charge ($50\%$), but do not connect pellet cancellation callbacks or emit `attack_sent`, strictly preventing retaliatory counter-attacks or combo loops back to the opponent.
- **Fairy Shockwaves vs Heavy Shockwaves Matrix**:
  - **Fairy Shockwaves (`scenes/effects/shockwave.gd`)**:
    - Defeating a fairy creates a regular shockwave that expands rapidly.
    - *Cancelable*: Only standard pellets (`EnemyPellet` where `can_be_canceled == true`) and danmaku circular pellets (`red_pellet.tres`, `white_pellet.tres`, `blue_pellet_small.tres`, `green_pellet.tres`). Canceled pellets award charge and trigger retaliatory counter-pellets to the opponent.
    - *Immune*: Large Pellets (`is_big = true`), Ring Pellets (`is_ring = true`), and all signature projectiles: Talismans (`red_talisman.tres`, `white_talisman.tres`), Stars (`blue_star.tres`, `green_star.tres`, `yellow_star.tres`), and Needles/Ovals (`red_oval.tres`, `white_oval.tres`). These projectiles cut straight through fairy shockwaves unharmed.
  - **Heavy Shockwaves (`scenes/effects/heavy_shockwave.gd`)**:
    - Triggered defensively by Level 2–4 Spellcard declarations or boss/Lily White defeats.
    - Vaporizes **all** incoming projectiles unconditionally (pellets, rings, big pellets, talismans, stars, needles, lasers, and yin-yang orbs) within its radius.
- **Boss Immunity & Level 4 Dispel**:
  - Boss characters (`BossCharacter`, `LilyWhite`) are immune to shockwave damage.
  - Casting your own **Level 4 Spellcard** acts as the ultimate defensive counter: it calls `caster_field.dispel_active_boss()`, immediately banishing any active opponent boss entity hovering over your playfield (`State.DISPELLED`).

### 4. Data-Driven Customization (`CharacterData`)
Shockwaves are fully modular per character:
- `has_spellcard_shockwaves: bool = true` (can be disabled completely for custom archetypes).
- `shockwave_radius_lv2: float = 420.0`
- `shockwave_radius_lv3: float = 650.0`
- `shockwave_radius_lv4: float = 1200.0`
- `shockwave_speed: float = 400.0` (calibrated constant travelling speed across all levels: 400 px/s)
- `shockwave_duration: float = 1.02` (automatically computed via `(radius - 12.0) / shockwave_speed`, ensuring all spell levels expand at the exact same constant travelling speed: Lv 2 = 1.02s, Lv 3 = 1.60s, Lv 4 = 2.97s)
- `ribbon_half_width: float = 20.0` (40px calibrated runic ribbon band, stretched radially with multi-tier atmospheric glow)
- `shockwave_color: Color = Color.WHITE`

---

## 9. Player Projectiles & Authentic Shot Hit Effects

Primary shots fired by players and their cosmetic hit responses:

### 1. Reimu Hakurei: Hakurei Amulets & Tumbling Shard Penetration
- **Normal Bullet Texture**: `assets/bullets/amulet.png` ($16 \times 64\text{ px}$, $1.5\times$ scale, red Shinto paper charm with vertical glow trail).
- **Collision Shape**: `CapsuleShape2D` (radius 6.0px, height 60.0px).
- **Authentic Shot Penetration Effect (`ShotHitShard`)**:
  - **Sprite**: `assets/characters/reimu/reimu_shot_penetrate.png` ($16 \times 16\text{ px}$, extracted from `reimu_raw_sheet.png` "Shot Effect 1" frame 3).
  - **Behavior**: On hitting any damageable entity (`take_damage()`), a glowing white card slip with a pink rim emerges from the far side of the enemy's hitbox, continuing along the bullet's travel vector.
  - **Kinematics**: Rapid tumbling spin ($1.5\text{--}2.5$ full rotations, randomized clockwise/counter-clockwise), forward ease-out deceleration ($\sim 52\text{ px}$ travel over $0.24\text{s}$), and smooth alpha fade in the second half of flight.
  - **Gameplay Role**: Purely visual/cosmetic (no collision shape, zero damage, zero charge impact).
- **Level 1 Charge Attack Pass-Through Effect (`HakureiAmulet`)**:
  - **Sprites**: `assets/characters/reimu/reimu_charge_amulet_spent.png` ($64 \times 64\text{ px}$, Box 1 white talisman with pink border) and `assets/characters/reimu/reimu_charge_amulet_spent_dark.png` ($64 \times 64\text{ px}$, Box 2 black talisman with white circuit lines and pink border).
  - **Behavior**: Upon hitting an enemy hitbox and dealing damage, the active red talisman immediately switches to the spent talisman texture, punches through to the far/exit side of the enemy hitbox along `_velocity_dir`, and coasts forward ($\sim 50\text{--}75\text{ px}$) while rapidly tumbling ($1.5\text{--}2.5$ rotations) and fading to alpha 0 over $0.28\text{s}$.
  - **Gameplay Role**: Purely visual exit reaction (collision disabled upon impact).

### 2. Marisa Kirisame: Laser Needles & Tumbling Shard Penetration
- **Normal Bullet Texture**: `assets/bullets/laser_needle.png` ($30 \times 14\text{ px}$, high-velocity green energy needle).
- **Collision Shape**: `RectangleShape2D` (aligned with needle width/height).
- **Authentic Shot Penetration Effects (`ShotHitShard`)**:
  - **Sprites**: Extracted from `marisa_raw_sheet.png` "Shot" row:
    - `assets/characters/marisa/marisa_shot_shard_1.png` ($32 \times 32\text{ px}$, Sprite 2: green crescent energy arc).
    - `assets/characters/marisa/marisa_shot_shard_2.png` ($32 \times 32\text{ px}$, Sprite 3: green flying spark cluster).
    - `assets/characters/marisa/marisa_shot_shard_3.png` ($32 \times 32\text{ px}$, Sprite 4: dispersing green ember particles).
  - **Behavior**: When Marisa's primary shots strike an enemy, one of the three green energy shards is randomly selected and spawned on the far/exit side of the enemy's hitbox along the shot trajectory.
  - **Kinematics**: Rapid tumbling spin ($1.5\text{--}2.5$ rotations), forward ease-out deceleration ($\sim 52\text{ px}$ travel over $0.24\text{s}$), and smooth alpha fade in the second half of flight.
  - **Gameplay Role**: Purely visual/cosmetic.

### 3. Cirno: Icicle Needles & Shatter Crumble Animation (`ShotCrumbleEffect`)
- **Normal Bullet Texture**: `assets/bullets/cirno_shot.png` ($16 \times 64\text{ px}$, Sprite 28 from `pl05.png`, sharp vertical crystalline needle pointing UP).
- **Collision Shape**: `CapsuleShape2D` (radius 6.0px, height 60.0px).
- **Authentic Shot Crumble Sequence (`ShotCrumbleEffect`)**:
  - **Sprites**: Extracted from `pl05.png` (row $y=160$, rotated $90^\circ$ CCW to $16 \times 64\text{ px}$):
    - `assets/characters/cirno/cirno_shot_crumble_1.png` (Sprite 29: needle tip cracking and fracture lines).
    - `assets/characters/cirno/cirno_shot_crumble_2.png` (Sprite 30: needle shattering into crystalline chunks).
    - `assets/characters/cirno/cirno_shot_crumble_3.png` (Sprite 31: disintegrating ice dust and sparkles).
  - **Behavior**: When Cirno's icicle needle impacts an enemy, it spawns `ShotCrumbleEffect` directly at the bullet's impact location (`global_position`), perfectly aligned with the shot's trajectory.
  - **Kinematics**: Plays in-place across 3 frames ($0.08\text{s}$ per frame, matching the authentic $5\text{ ticks}$ per sprite from Touhou 09 `pl05.anm` Script 8, total $0.24\text{s}$) before queue-freeing.
  - **Gameplay Role**: Purely visual/cosmetic impact feedback.

---

## 10. Character Extra Attacks (Harassment Hazards)

Extra attacks sent to the opponent's field via combo milestones or Great Fairy chain reactions:

### 1. Reimu Hakurei: Yin-Yang Orb (`scenes/attacks/yin_yang_orb.tscn`)
- **Appearance**: Spinning $60\text{px}$ Yin-Yang orb tossed upward from top screen border ($Y \in [60, 130]$).
- **Behavior**: Bounces elastically off side walls, falls under gravity, and bounces elastically off other Yin-Yang orbs.
- **Destructibility**: Cleared only by heavy shockwaves (Level 2-4 spellcards or Youmu's charged slash).
- **Damage**: $1.5$ contact damage to player.

### 2. Marisa Kirisame: Earth Light Ray (`scenes/attacks/earth_light_ray.tscn`)
- **Appearance**: Ascending runic warning strip erupting into a full vertical laser beam pillar from bottom border ($Y \in [920, 945]$).
- **Behavior**: Stationary vertical column with scrolling runes and expanding additive flare.
- **Destructibility**: Indestructible energy beam.
- **Damage**: $1.5$ contact damage to player during firing state.

### 3. Youmu Konpaku: Dark Spirit Phantom (`scenes/attacks/youmu_dark_spirit.tscn`)
- **Appearance**: 4-frame $48 \times 48\text{px}$ pulsating dark spirit with magenta halo (`assets/characters/youmu/youmu_dark_spirit.png`). Spawns with authentic Touhou spirit sound `se_gosp.wav`.
- **Target Envelope**: Flat rectangular area laid flat around the player's spawn and movement lane ($X \in [80, 520], Y \in [740, 880]$).
- **Kinematics**: Completely stationary obstacle; remains fixed at arrival coordinates.
- **Lifetime**: Scales with match rank: $1.0\text{s}$ at Rank 1 to $2.0\text{s}$ at Rank 22 (`lerpf(1.0, 2.0, (rank - 1) / 21.0)`).
- **Destructibility**: Indestructible obstacle (ignores player shots, fairy shockwaves, and heavy slashes).

---

## 11. Character Charge Attacks (Level 1)

Level 1 Charge Attacks executed by charging the spell gauge to at least 1.0 segment and releasing without consuming passive gauge:

### 1. Reimu Hakurei: Hakurei Amulet (`scenes/attacks/reimu_charge_attack.tscn`)
- **Appearance & Structure**: 4 large talismans (`assets/characters/reimu/reimu_charge_amulet.png`) orbiting and growing in front of Reimu before homing onto closest enemies.
- **Damage**: 2.5 per talisman. Plays spent despawn flip on hit.

### 2. Marisa Kirisame: Illusion Laser (`scenes/attacks/marisa_charge_attack.tscn`)
- **Appearance & Structure**: Sustained vertical piercing beam anchored to Marisa (`scenes/attacks/illusion_laser.tscn`).
- **Damage**: 1.2 per tick (ticks every 0.06s) piercing all enemies in column.

### 3. Youmu Konpaku: Hesitation-Clearing Slash (`scenes/attacks/youmu_charge_attack.tscn`)
- **Appearance & Structure**: Massive sweeping crescent sword arc (`scenes/attacks/youmu_slash_wave.tscn`) traveling forward at $620\text{ px/s}$.
- **Damage**: 38.0 cleave damage; vaporizes bullets and dispels physical EX attacks.

### 4. Cirno: Freeze Blade (`scenes/attacks/cirno_charge_attack.tscn`)
- **Texture**: `assets/bullets/cirno_freeze_blade.png` ($64 \times 16\text{ px}$, Sprite 32 from `pl05.png`, sharp horizontal crystal icicle blade pointing RIGHT).
- **Appearance & Structure**: Discharges 28 crystalline icicles radiating in a 360-degree radial ring around Cirno at angle steps $\Delta\theta = \frac{2\pi}{28} \approx 12.86^\circ$.
- **Kinematics**: Rapid outward linear flight at $620.0\text{ px/s}$, aligned directly with its velocity vector.
- **Hitbox & Collision**: `CapsuleShape2D` (radius 6.0px, height 58.0px, Layer 3 `player_bullets`, Mask 4 `enemies`).
- **Damage & Impact**: Deals 2.0 damage per icicle to enemies (`charge_attack`). On impact, triggers authentic 3-frame `ShotCrumbleEffect` aligned with bullet rotation.
- **Audio Cue**: Discharges with authentic `se_tan00`.

### 5. Sakuya Izayoi: Silver Knife (`scenes/attacks/sakuya_charge_attack.tscn`)
- **Texture**: `assets/bullets/sakuya_knife_silver.png` ($32 \times 32\text{ px}$, blade pointing RIGHT) paired with `assets/effects/sakuya_knife_glow.png` (additive soft white halo stretched along each blade).
- **Appearance & Structure**: 8 daggers (`scenes/attacks/sakuya_knife.tscn`) cluster within a tight $10\text{ px}$ ring at Sakuya's hitbox. The `Blade` container carries a `Vector2(6.0, 1.0)` scale — stretched $6\times$ **along** the blade's local X axis and left at native width **across** it — yielding a $192 \times 32\text{ px}$ elongated streak silhouette.
- **Kinematics**: Each dagger spins the `Blade` container in place from $0°$ to $180°$ over $0.22\text{s}$ (staggered $0.03\text{s}$ apart per knife for a rapid-fire feel). On launch the **Area2D itself** rotates to `dir.angle()` (resetting the blade's local spin to $0$) so sprite, glow, and hitbox all align with flight, then travels at $3200\text{ px/s}$ toward whichever enemy was nearest at that instant.
- **Hitbox & Collision**: `RectangleShape2D` ($160 \times 24\text{ px}$, long axis along the blade, Layer 3 `player_bullets`, Mask 4 `enemies`).
- **Damage**: 1.6 per dagger (charge_attack), despawns on hit or leaving the playfield bounds.

### 5. Sakuya Izayoi: Illusion Knives (`scripts/arena/mote_dispatcher.gd: _dispatch_sakuya_extra_attack`)
- **Texture**: `assets/bullets/sakuya_knife_blue.png` ($32 \times 32\text{ px}$, blade pointing RIGHT) at $2\times$ scale (unlike the Silver Knife charge attack, these are NOT squished/stretched — they keep their normal dagger silhouette, just enlarged), paired with `assets/effects/sakuya_knife_glow.png`.
- **Departure from the standard Extra Attack pipeline**: every other character's Extra Attack rides a generic `TravelMote` (an arcing light dot) from the death position to a random point already inside the opponent's field, which then spawns the real effect there. Sakuya's daggers skip the mote entirely — the visible dagger itself is the traveling object from spawn to impact.
- **Appearance & Structure**: on a qualifying Great Fairy chain kill, 5 daggers (`scenes/attacks/sakuya_extra_knife.tscn`) spin up directly at the fairy's death position **on the aggressor's own field**, staggered $0.16\text{s}$ apart (slightly tighter than one knife-length of travel time, so the chain sits tip-into-hilt with a bit of overlap).
- **Kinematics**: each dagger spins $0°\to180°$ over $0.22\text{s}$, then aims once at the opponent's position at that instant ("last known position") using a combined coordinate space (opponent's field = aggressor's local X $\pm$ 1 playfield width, so a single straight vector carries across both fields) and flies slowly at $300\text{ px/s}$. On crossing the shared border each dagger is reparented live from the sender's own `%Bullets` layer to the target's `%Bullets` layer (recomputing local X, keeping velocity/rotation) — the same node, now a real hazard in the opponent's physics world, continuing its straight line.
- **Visibly crossing the screen divider**: a `SubViewport` only ever renders its own field, so the real Area2D would simply vanish the instant it left either field's bounds — never visible in the ~60px gap between the two `SubViewportContainer`s (`PlayfieldsHBox` separation). To bridge that gap, on launch the dagger hides its own `Blade` and drives a duplicate of it (a "ghost") inside the same screen-space overlay `TravelMote` uses. The ghost's position is a single uninterrupted straight-line extrapolation anchored to the *source* container's screen position for the entire flight (it never switches reference frames), while the measured real gap width (`target_container.global_position - source_container.global_position - playfield width`) delays the real Area2D's reparent until it has virtually traveled that same extra distance — so the two stay in lockstep and the ghost has no seam to jump across.
- **Harmless to the sender (authentic to 09)**: while still over the sender's own field, the ghost is faded to 40% alpha and `_on_body_entered` ignores the sender's own player entirely (`not _has_crossed` early-out) — the dagger only becomes a real, fully-opaque hazard once it has actually crossed into the opponent's field.
- **Hitbox & Collision**: `RectangleShape2D` ($56 \times 20\text{ px}$), Layer 2 (`enemy_bullets`), Mask 1 (`player`) — like other Extra Attacks, this damages the opposing **player** directly, not fairies.
- **Damage**: 0.7 per dagger (`body.take_damage`), single-hit despawn. Registered with each field's `danmaku_dispatcher` custom-hazard list for round-reset cleanup on whichever side currently owns it.
- **Destructibility**: cleared by heavy shockwaves (hit-landing shoves and Level 2-4 spellcards) via `take_damage(_, "heavy_shockwave")`, but only once it has crossed - a dagger still over the sender's own side is harmless there, so it can't be swatted down there either.
- **Trigger cadence**: uses the shared `get_trains_required_for_extra()` ramp (every ~3rd chained Great Fairy train early, down to every train past 60s) — unchanged from other characters; only the delivery mechanism differs.

### 5. Sakuya Izayoi: Level 2 Spellcard - Time Sign "Private Square" (`resources/spellcards/sakuya/sakuya_spell_lv2.tres`)
- **Composition**: Single `DanmakuTimeStopFanStep` (see step catalog above) with `sakuya_knife.tres` (new) paired with the existing `red_pellet.tres`.
- **Behavior**: A brief beat of nothing, then the target's entire field freezes - bullets, fairies, boss/Lily White, and the victim's own character - while a 115.7° fan of 10 daggers (each with a small red pellet near the grip) opens above the victim one pair at a time (`wait(4)` = 4 frames per spoke). Once the fan is fully formed and the freeze lifts after a 20-frame hold, every dagger and pellet gently accelerates from rest, sailing inward through the victim's position and out the far side.
- **Authentic mirror-match bug, preserved on purpose**: if the victim is also playing Sakuya, they're immune to the time-stop, exactly like the caster herself always is.
- **Rank Scaling**: 10 daggers at every rank. Linear acceleration scales from `125.0 px/s²` to `325.0 px/s²` and top speed scales from `260.0 px/s` to `340.0 px/s`.
- **Reference**: Bytecode verified against decompiled `pl02.ecl` `sub0()`.

### 5. Sakuya Izayoi: Level 3 Spellcard - Time Sign "Private Square" (`resources/spellcards/sakuya/sakuya_spell_lv3.tres`)
- **Composition**: The exact same `DanmakuTimeStopFanStep` as Lv2 (see step catalog above), reconfigured with `stacked_cross_mode = true` and no `pellet_data`. Mechanically identical to Lv2 - same freeze, same timing, same audio, same mirror-match immunity - only the dagger layout differs.
- **Behavior**: Same freeze beat as Lv2, then 10 spokes across the 115.7° arc above the victim each grow a 4-dagger cross (`bullet_circle_aimed(Knife, DarkRed, 4, ...)` at radius 133px) spaced 4 frames apart (`wait(4)`). During the time-stop (which holds for 20 frames after the 10th cross finishes), the interlocking crosses form a tight, woven canopy of 40 criss-crossing daggers right above the victim. Once the freeze lifts, each cross peels apart as its daggers gently accelerate along their individual headings (inward, outward, and tangential).
- **Authentic mirror-match bug, preserved on purpose**: inherited unchanged from the shared step - if the victim is also playing Sakuya, they're immune to the time-stop, exactly like the caster herself always is.
- **Rank Scaling**: 10 crosses (40 daggers total) at every rank. Linear acceleration scales from `125.0 px/s²` to `325.0 px/s²` and top speed scales from `260.0 px/s` to `340.0 px/s`.
- **Reference**: Bytecode verified against decompiled `pl02.ecl` `sub1()`.

### 5. Sakuya Izayoi: Level 4 Boss Spellcard 1 - Pellet-Spewing Dagger (`resources/spellcards/sakuya/sakuya_boss_spell_1.tres`)
- **Composition**: Modular `DanmakuPelletDaggerStep` (see step catalog above) with `sakuya_knife_cyan.tres` (dagger) + `sakuya_dagger_pellet.tres` (trail, spawn-fades in).
- **Behavior**: Throws a single slow cyan dagger toward the opponent's position at the moment of the throw. While airborne, the dagger periodically spews a short burst of white pellets scattered around its own position; each pellet picks a random direction and accelerates from rest to its top speed over 1 second, leaving a trail of little outward-blooming clusters behind the dagger's whole descent. The boss's hop/cast AI loop repeats this (reposition, then throw another dagger) for the rest of the spellcard.
- **Rank Scaling**: Dagger speed fixed at `300.0 px/s` at every rank (no scaling - see step notes above). Pellet trail scales: 3 pellets/burst at `140.0 px/s` top speed (Rank 1) up to 5 pellets/burst at `190.0 px/s` top speed (Rank 16), every `0.15s`.
- **Reference**: Verified against user-provided `sakuya_level4_rank1.mp4` (00:02-00:05) and `sakuya_level4_rank16.mp4` (00:15-00:18) footage of authentic PoFV gameplay - Time Sign "Mysterious Jack" (時符「ミステリアスジャック」).

### 5. Sakuya Izayoi: Level 4 Boss Spellcard 2 - Reflective Knife Explosion (`resources/spellcards/sakuya/sakuya_boss_spell_2.tres`)
- **Composition**: Modular `DanmakuBouncingKnifeRingStep` (see step catalog above) with `sakuya_knife_purple.tres` (pre-bounce) + `sakuya_knife_blue.tres` (post-bounce).
- **Behavior**: Summons 10 small circles of 7 purple daggers each, one circle at a time (0.1s apart), every circle independently spawned at its own randomized point near the boss and launched radially outward. Daggers that travel 40px past the left, right, or top playfield edge ricochet back inward once and turn blue, then fly straight from there; daggers reaching the bottom edge (toward the player) exit normally, never reflected.
- **Rank Scaling**: Fixed at 10 circles of 7 daggers (70 total) at every rank. Speed scales `230.0 px/s` (Rank 1) up to `390.0 px/s` (Rank 16) - confirmed by the user to visibly speed up with rank, unlike Boss Attack 1; Rank 1 was slowed from an initial 300 px/s after the user found the gap to Rank 16 too subtle.
- **Reference**: Verified against user-provided `sakuya_level4_rank1.mp4` (00:04-00:08) and `sakuya_level4_rank16.mp4` (00:08-00:11) footage of authentic PoFV gameplay - same Time Sign "Mysterious Jack" (時符「ミステリアスジャック」) spell as Boss Attack 1.

### 5. Sakuya Izayoi: Level 4 Boss Spellcard 3 - Sweep of Yellow Daggers (`resources/spellcards/sakuya/sakuya_boss_spell_3.tres`)
- **Composition**: Modular `DanmakuNarrowingSweepStep` (see step catalog above) with `sakuya_knife_yellow.tres`.
- **Behavior**: Fires yellow daggers sequentially from the front (player-facing) half of a tight circle around the boss, starting at the two outermost positions (near-horizontal, left and right) and stepping inward in symmetric pairs, gradually narrowing toward straight down without ever fully reaching it.
- **Rank Scaling**: Fixed at 16 daggers per side (32 total) at every rank. Speed scales `240.0 px/s` (Rank 1) up to `400.0 px/s` (Rank 16) - confirmed by the user to increase with rank while the dagger count stays constant.
- **Reference**: Verified against user-provided `sakuya_level4_rank1.mp4` (00:07-00:11) and `sakuya_level4_rank16.mp4` (00:02-00:05) footage of authentic PoFV gameplay - same Time Sign "Mysterious Jack" (時符「ミステリアスジャック」) spell as Boss Attacks 1 and 2.

### 5. Sakuya Izayoi: Level 4 Boss Spellcard 4 - Knife Strafing (`resources/spellcards/sakuya/sakuya_boss_spell_4.tres`)
- **Composition**: Modular `DanmakuKnifeStrafeStep` (see step catalog above) with `sakuya_knife_blue.tres` (reused from Boss Attack 2).
- **Behavior**: Boss jumps to the left playfield boundary, then rapidly dashes to the right boundary over 2 seconds. While dashing, fires several simultaneous lines of daggers at fixed trailing angles from her current position at 13 evenly-spaced moments across the dash - each line's daggers cascade into a diagonal streak as the origin keeps moving, producing a fanned "feathered wing" trail in her wake.
- **Rank Scaling**: 2 lines of 13 daggers (26 total) at Rank 1 up to 6 lines of 13 daggers (78 total) at Rank 16 - confirmed by the user. Speed scales mildly, `260.0 px/s` (Rank 1) up to `300.0 px/s` (Rank 16).
- **Reference**: Verified against user-provided `sakuya_level4_rank1.mp4` (00:17-00:21) and `sakuya_level4_rank16.mp4` (00:05-00:09) footage of authentic PoFV gameplay - same Time Sign "Mysterious Jack" (時符「ミステリアスジャック」) spell as Boss Attacks 1-3. Flagged by the user in advance as the most complex of the four and likely to need follow-up tuning.

---

## 12. Character Extra Attacks (Additional)

### 4. Cirno: Falling Stalactite (`scenes/attacks/cirno_stalactite.tscn`)
- **Texture**: `assets/bullets/cirno_stalactite.png` ($16 \times 64\text{ px}$) — the old vertical icicle needle sprite flipped tip-down via `Image.FLIP_TOP_BOTTOM`, forming an authentic hanging stalactite silhouette.
- **Spawn & Target Envelope**: Deposited by a TravelMote at the top of the opponent's playfield ($X \in [50, 550], Y \in [40, 80]\text{ px}$ from playfield origin).
- **Kinematics**: Hangs motionless for $0.16\text{s}$ (hang phase), then falls under gravity at $340\text{ px/s}^2$ up to a terminal velocity of $420\text{ px/s}$.
- **Rank Scaling (Count)**: Cirno sends between `randi_range(min_ex, max_ex)` stalactites per Great Fairy train, where `min_ex = roundi(lerpf(1, 3, t))` and `max_ex = roundi(lerpf(2, 4, t))` with `t = (rank - 1) / 21.0`. This yields $1$–$2$ at Rank 1 and $3$–$4$ at Rank 22, staggered $0.12\text{s}$ apart.
- **Frequency**: Cirno triggers the extra attack on **every** Great Fairy train (`req_trains = 1`) with a $0.5\text{s}$ minimum cooldown between volleys (other characters: $1.2\text{s}$).
- **Destructibility**: Cleared by heavy shockwaves (Level 2–4 spellcards, Youmu's slash). Spawns `ShotCrumbleEffect` on player impact or shockwave clear. Plays `se_exattack` on spawn.
- **Damage**: $1.0$ HP contact damage to player.
- **Collision**: `Area2D`, Layer 2 (`enemy_bullets`), Mask 1 (`player`). `CapsuleShape2D` (radius 8.0px, height 80.0px) at $1.5\times$ visual scale.

### 6. Reisen Udongein Inaba: Falling Moon (`scenes/attacks/reisen_extra_moon.tscn`)
- **It is the boss's Triple Moon Shot, one moon at a time.** Same scene script (`ReisenMoonMote`), same sprite, same $250\text{ px/s}$ descent, same $181\text{ px}$ proximity fuse and the same `ReisenMoonBlast` crater ($24 \to 126\text{ px}$ over $1.05\text{s}$, $1.5$ damage). Nothing about the moon itself was re-tuned for this: the only differences are **how many** (one, against the boss's three) and **where it starts** (the top of the victim's field, against a fan below the boss). Confirmed against `reisen_extra.mp4`, where a moon's descent measures $\approx 2.85\text{ video px/frame}$ — $\approx 240\text{ game px/s}$ once scaled off the field width, which is the boss volley's figure within reading error.
- **Spawn Envelope**: $X \in [60, 540]$, $Y \in [130, 175]$ (`extra_attack_x_range` / `extra_attack_y_range` in `reisen.tres`). The Y band is read off the reference: the moon resolves $\approx 14\%$ of the way down the field, which is lower than it looks because PoFV's playfield is proportionally shorter than ours, so the fraction rather than the raw pixel offset is what carries over.
- **Straight down, not aimed**: `direction` is left at `Vector2.DOWN`, matching the boss step, which also fires a lone moon straight down (`_fan_angle_deg` returns 0 when `mote_count <= 1`). The reference moons lean $\approx 9°$ off vertical, but *away* from the player rather than toward them, so it reads as spread rather than aim and is inside the error of tracking a sprite that is pulsing while it falls.
- **Resolving out of a flare (`spawn_in_duration`, 0.28s)**: the moon is born $2.6\times$ oversized and fully transparent and closes onto itself, which in the original is the only warning the victim gets that the thing landing on their ceiling is not a pellet. This is laid *over* the breathing rather than replacing it, so it is already pulsing while it resolves. The boss's own volley leaves `spawn_in_duration` at `0.0`, because its light motes (`ReisenMoonCharge`) already play that beat before a moon exists at all and doing both would resolve the same moon twice.
- **Audio (`spawn_sfx`, `se_exattack`)**: played once as the moon resolves, on the same `_ready()` beat as the flare. The export defaults to empty and only `reisen_extra_moon.tscn` sets it, exactly as `spawn_in_duration` works, so the boss's volley of three stays silent here and keeps its cast sound on the step instead of firing three times over. `se_exattack` is what Cirno's stalactite already uses for the same job: an Extra Attack arriving on a field the victim is not watching has to announce itself.
- **No hitbox, as with the boss version**: the moon can never touch the player — the fuse trips at more than twice its own radius — so the crater is the only thing that hits anything. It is therefore also not clearable by heavy shockwaves; there is nothing to clear.
- **Frequency**: the shared `get_trains_required_for_extra()` ramp, unchanged — one moon per 3 chained Great Fairy trains early, per 2 past $30\text{s}$, per train past $60\text{s}$, with the standard $1.2\text{s}$ floor between volleys.
- **Netplay**: rides the ordinary `TravelMote` / `roll_extra_attack_targets` path with `ex_count = 1`, so the aggressor's client rolls the landing spot once and ships it. Where a moon comes down is attack identity, not per-bullet jitter.

### 7. Yuuka Kazami: Falling Sunflower (`scenes/attacks/yuuka_extra_flower.tscn`)
- **Texture**: `assets/bullets/yuuka_extra_flower.png`, sprite34 of `pl09_ex.png` ($64 \times 64$ at `(0,128)`), drawn at $2.0833\times$. The sheet's two larger flowers (sprite32/33) are unused here.
- **Forming (`pl09.anm` script 11/12)**: starts at $2\times$ width and zero height and opens to full size over 20 frames (decelerating) while fading in from alpha `0x40` over 10 frames. Spins $0.1047\text{ rad/frame}$ ($6.28\text{ rad/s}$), clockwise or counter-clockwise (script 11 or 12). Harmless until it starts to fall.
- **Kinematics (measured, not in the depot)**: tracked frame by frame from `yuuka_extra.mp4`. Each flower holds still for ~13 frames ($0.22\text{s}$), reaches top speed within a few frames and then falls straight down at a constant speed. Four flowers measured 85, 104, 205 and 236 px/s, so each rolls its own speed uniformly in $60$-$320\text{ px/s}$: wider than the measured spread, because four samples felt same-y in playtest.
- **Spawn Envelope**: $X \in [60, 540]$, $Y \in [30, 180]$, read from where the four flowers formed.
- **Hitbox & Damage**: `CircleShape2D` radius $54\text{ px}$ (the drawn flower reaches ~66px; at 36 the petals were safe, which felt too forgiving in playtest), Layer 2, Mask 1. $1.0$ contact damage, then folds shut over 20 frames (the anm's `interrupt[1]`). Cleared the same way by heavy shockwaves and charge attacks.
- **Audio**: `se_exattack` on spawn, as with Cirno's and Reisen's.
- **Frequency**: one flower per volley on the shared `get_trains_required_for_extra()` ramp.
- **Netplay**: landing spot rides `roll_extra_attack_targets`; fall speed and spin direction come from `setup_variation()`, seeded off that synced spot.
