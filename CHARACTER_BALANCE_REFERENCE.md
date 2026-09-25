# Character Balance & Authentic Frame Data Reference
## Touhou 09: Phantasmagoria of Flower View (東方花映塚) Analysis

This document is the **authoritative reference** for character movement speeds, active charge formulas, frame data, hitboxes, and passive traits in *Touhou Web Arena*. 
All baseline metrics derive from disassembled game data and competitive analysis tables of *Touhou 09: Phantasmagoria of Flower View* (東方花映塚 解析データ).

---

## 1. Movement Speeds

In the original game ($640 \times 480$ screen resolution @ 60 FPS), velocities are defined in **dots per frame**.
In our engine ($1920 \times 1080$ viewport, $600 \times 960$ playfields), velocities scale by a fixed factor of **$135.0\text{ px/s}$ per dot/frame** (anchored to Reimu's canonical $540.0\text{ px/s}$ normal speed).

| Character | Original Normal (dots/f) | Original Focus (dots/f) | Focus Drop Ratio | Engine Normal (`px/s`) | Engine Focus (`px/s`) | Archetype / Movement Profile |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **Marisa Kirisame** | **5.0** | 3.0 | $1.67\times$ | **675.0** | 405.0 | Fastest unfocused dash; fast even when focused. |
| **Youmu Konpaku** | **5.0** | **2.1** | **$2.38\times$** | **675.0** | **283.5** | Extreme contrast: blazing forward speed, dropping to a concentrated crawl while charging/focusing. |
| **Cirno** | 4.7 | **3.3** | $1.42\times$ | 634.5 | **445.5** | Very nimble, high focused mobility for micro-dodging. |
| **Reisen Udongein** | 4.5 | 2.5 | $1.80\times$ | 607.5 | 337.5 | Balanced medium-fast mobility. |
| **Sakuya Izayoi** | 4.0 | **3.2** | **$1.25\times$** | 540.0 | **432.0** | **Barely slows down in focus!** Highly agile focus movement for weaving through tight knife/bullet blankets. |
| **Reimu Hakurei** | 4.0 | 2.0 | $2.00\times$ | 540.0 | 270.0 | Moderate standard speed, clean $2:1$ focus ratio. |

---

## 2. Active Charge Speed & Frame Data

### The Authentic Formula
The original game engine charges the active spell gauge according to:
$$\text{Charge Frames} = 10\text{ frames (initial delay)} + \left\lceil \frac{\text{Meter Required}}{\text{Charge Coefficient } C} \right\rceil$$

Where each level segment represents **100 internal points**.
In our continuous-time Godot engine, the active charge rate in **segments per second** is:
$$\text{active\_charge\_speed} = 0.6 \times C$$

### Character Charge Table & Timing

| Character | Charge Coeff ($C$) | Engine `active_charge_speed` (seg/s) | C1 Charge Atk ($1.0$) | C2 Spellcard ($2.0$) | C3 Spellcard ($3.0$) | C4 Boss Spell ($4.0$) | Tactical Playstyle |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **Reimu Hakurei** | **4.0** | **2.40** | 35 frames ($0.58\text{s}$) | 60 frames ($1.00\text{s}$) | 85 frames ($1.42\text{s}$) | 110 frames ($1.83\text{s}$) | Fastest spellcaster; rapid level 2 defense cycling. |
| **Sakuya Izayoi** | **3.6** | **2.16** | 38 frames ($0.63\text{s}$) | 66 frames ($1.10\text{s}$) | 94 frames ($1.57\text{s}$) | 122 frames ($2.03\text{s}$) | Very high charge rate; high-tempo trap pressure. |
| **Reisen Udongein** | **3.5** | **2.10** | 39 frames ($0.65\text{s}$) | 68 frames ($1.13\text{s}$) | 97 frames ($1.62\text{s}$) | 126 frames ($2.10\text{s}$) | Fast-medium charging. |
| **Marisa Kirisame** | **3.3** | **1.98** | 41 frames ($0.68\text{s}$) | 71 frames ($1.18\text{s}$) | 101 frames ($1.68\text{s}$) | 132 frames ($2.20\text{s}$) | Average active charge, balanced by massive passive generation. |
| **Cirno** | **2.9** | **1.74** | 45 frames ($0.75\text{s}$) | 79 frames ($1.32\text{s}$) | 114 frames ($1.90\text{s}$) | 148 frames ($2.47\text{s}$) | Moderate charge; relies on freeze chain reactions. |
| **Youmu Konpaku** | **2.2** | **1.32** | 56 frames ($0.93\text{s}$) | 101 frames ($1.68\text{s}$) | 147 frames ($2.45\text{s}$) | 192 frames ($3.20\text{s}$) | Slowest charge in the game; high commitment for big payoff. |

---

## 3. Hitboxes, Grazing & Hurtboxes

| Character | Hurtbox Radius (`hurtbox_radius`) | Graze Field Radius (`graze_radius`) | Special Defensive Traits |
| :--- | :---: | :---: | :--- |
| **Reimu Hakurei** | **2.4 px** | 64.0 px | Canonical micro-hitbox advantage (easiest to navigate dense patterns). |
| **Cirno** | **2.8 px** | 64.0 px | Small fairy silhouette advantage. |
| **Youmu Konpaku** | **3.0 px** | 64.0 px | Standard hitbox. |
| **Sakuya Izayoi** | **3.0 px** | 64.0 px | Standard hitbox; compensated by top-tier focused agility ($432\text{ px/s}$). |
| **Marisa Kirisame** | **3.2 px** | 64.0 px | Slightly larger hurtbox to balance extreme speed and firepower. |

---

## 4. Passive Meter Economy & Special Traits

PoFV balances characters by giving them asymmetric advantages in the resource game:

- **Marisa Kirisame — High Passive Accumulation**:
  - `passive_charge_per_fairy = 0.014` (vs baseline `0.010`).
  - Marisa charges active meter slower than Reimu, but her passive gauge fills rapidly whenever fairies are wiped.
- **Reimu Hakurei — Natural Evader & Fast Spell Turnover**:
  - Micro hurtbox ($2.4\text{px}$), highest active charge coefficient ($4.0$).
  - `passive_charge_per_spirit = 0.045` (spiritual resonance).
- **Cirno — Persistent Spirits**:
  - Activated spirits linger on the field longer before expiring, allowing extended chain reactions.
  - Quick, wide-spread freeze-bursts.
- **Youmu Konpaku — Slashing Concentration**:
  - Enormous `scope_radius = 260.0px` (draws spirits into massive clumps).
  - Heavy charge commitment ($1.32\text{ seg/s}$) with deadly directional dagger waves.
- **Sakuya Izayoi — Spatial Time Trap**:
  - **Downward Fan Scope Style (`FAN_DOWN`)**: Sakuya's scope is a $90^\circ$ circular sector with full vertical reach ($r = 400\text{ px}$) that unfurls purely horizontally to both sides from the center down axis ($45^\circ \dots 135^\circ$ directly below her, spanning $\approx 566\text{ px}$ across the bottom). Gathers spirits hovering below her lane for coordinated point-blank detonation.
  - Activated spirits **do not drift upward**; they remain anchored horizontally in her lane.
  - Standardized PoFV 3-pair parallel vertical knife volley (`assets/bullets/sakuya_shot.png`). All characters in Touhou 09 share the standardized 3 pairs of 2 parallel straight forward shots, while their distinct gameplay identities derive from projectile sprites, Level 1 Charge Attacks, Extra Attacks, Spellcards (Lv 2-4), unique scope styles, and passive traits.

---

## 5. Boss HP & Escalation Rules

In *Phantasmagoria of Flower View*, Boss characters summoned via Level 4 Spellcards are not static:
- **Baseline Max HP**: $180.0\text{ HP}$.
- **Re-Summon Escalation**: Each subsequent boss summon within the same round increases the boss's max HP by $+30.0\text{ HP}$ (up to a ceiling of $300.0\text{ HP}$), ensuring late-game bosses demand serious survival effort and cannot be instantly vaporized.

