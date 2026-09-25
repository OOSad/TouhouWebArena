# Eientei Corridor Stage Assets (Temporary Calibration Placeholders)

> [!WARNING]
> In accordance with ZUN / Team Shanghai Alice derivative work guidelines and project rule 5:
> Ripped assets from official Touhou games are **strictly temporary placeholders** used solely for calibrating 3D plane geometry, UV scroll speeds, and depth fog.
> All assets in this folder must be replaced with original or permissively licensed fan art before public release.

### Extracted Components (from a user-supplied 512x512 px stage sheet):
- `corridor_wall.png`: 256x256 px, top-left quadrant, rotated 90 degrees counter-clockwise so the sliding doors stand upright under their lintel beam. Tiles horizontally (measured edge difference 0.165 against an interior baseline of 0.430), one repeat per 4-unit bay of four bamboo-painted door panels.
- `corridor_floor.png`: 256x512 px. The top-right quadrant (dark polished wood with lamp bloom) cropped to 256x240 to drop the darker bands along its top and bottom edges, then stacked with a vertically flipped copy of itself. The source does not tile on its own; the mirrored stack does, exactly, which is what kept a joint line from appearing every tile down the hallway.
- `corridor_lamp.png`: 126x125 px, `world04.anm` `sprite2` (the warm radial glow at 1,258), cut from the depot's `stg5bg.png`, which keeps its alpha. Drawn additively, facing the camera, flickering between alpha 0xc0 and 0xff every frame, placed per `world04.std` object 2 (see `eientei_corridor_3d.gd`, Lamps).
- Not extracted: the blue glow (`sprite6`) and the dimmer wall (`sprite5`), which belong to the mid-stage transition (`world04.std` objects 3 and 4), not built.

Used for Reisen Udongein Inaba's home stage, the endless Eientei corridor.
