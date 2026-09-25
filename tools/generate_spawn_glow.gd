extends SceneTree

## Draws the soft glow a forming bullet shows before it sets off (DanmakuBulletData
## spawn_fade_in_texture) into the bullet atlas. Original art: a plain radial falloff, white
## at the heart through yellow to nothing, so it costs no extra draw call and copies nobody.
##
## Yuuka's Level 4 Boss Attack 2 forms each big ball out of this, stacked on her six at a
## time every 4 frames, which is the bright orb PoFV shows there.
##
## Idempotent: the cell is redrawn outright.
##
## Run: Godot --headless --path <project> --script res://tools/generate_spawn_glow.gd
## then Godot --headless --path <project> --import, or the game keeps the old cell.

const ATLAS_PATH: String = "res://assets/bullets/danmaku_atlas.png"
const CELL := Vector2i(288, 384)
const SIZE: int = 64
const CORE := Color(1.0, 1.0, 0.92)
const RIM := Color(1.0, 0.86, 0.25)

func _init() -> void:
	var atlas := Image.load_from_file(ProjectSettings.globalize_path(ATLAS_PATH))
	assert(atlas != null, "danmaku_atlas.png must load")
	atlas.convert(Image.FORMAT_RGBA8)
	var centre := Vector2(SIZE, SIZE) * 0.5
	for y in SIZE:
		for x in SIZE:
			# 0 at the centre, 1 at the cell's inscribed edge
			var d: float = (Vector2(x, y) + Vector2(0.5, 0.5)).distance_to(centre) / (SIZE * 0.5)
			# Solid through the inner third, then a smooth falloff, so stacked glows run white-hot
			var a: float = clampf((1.0 - d) / 0.7, 0.0, 1.0)
			a = a * a * (3.0 - 2.0 * a)
			var c: Color = RIM.lerp(CORE, clampf(1.2 - d * 1.2, 0.0, 1.0))
			atlas.set_pixel(CELL.x + x, CELL.y + y, Color(c, a))
	atlas.save_png(ProjectSettings.globalize_path(ATLAS_PATH))
	print("Baked spawn glow at Rect2(%d, %d, %d, %d)" % [CELL.x, CELL.y, SIZE, SIZE])
	quit(0)
