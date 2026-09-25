# Flowering Night Stage Assets (Temporary Calibration Placeholders)

> [!WARNING]
> In accordance with ZUN / Team Shanghai Alice derivative work guidelines and project rule 5:
> Ripped assets from official Touhou games are **strictly temporary placeholders** used solely for calibrating 3D plane geometry, UV scroll speeds, and layer alpha blending.
> All assets in this folder must be replaced with original or permissively licensed fan art before public release.

### Extracted Components (from `th09.dat` -> `world00.anm`, `world00.png`, 512x512 px):
- `flower_field_base.png`: 256x256 px, top-left quadrant. Opaque poppy-field grass base layer (bottommost plane).
- `flower_field_mid.png`: 256x256 px, bottom-right quadrant. Red/pink flower texture with native per-pixel alpha (max ~67%), stacked above the base (middle plane).
- `flower_field_top.png`: 256x256 px, bottom-left quadrant. Sparse white/gold flower-cluster dots with native per-pixel alpha, stacked above the mid layer (topmost plane).
- The remaining top-right quadrant of `world00.png` (green bush/tree blob shapes) was not extracted — it appears to be an unrelated scenery sprite packed into the same sheet, not part of the 3-plane ground stack. Left in `scratch/th09_extracted/data/world00/world00.png` for future reference if needed.

All three textures are quad-mirrored (kaleidoscope) source art, so they already tile seamlessly when UV-repeated — used for Sakuya Izayoi's home stage, "Flowering Night".
