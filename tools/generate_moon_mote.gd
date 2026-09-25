extends SceneTree

## Bakes Reisen's Level 4 moon mote and blits it into the shared danmaku atlas.
##
## The mote is the projectile half of Triple Moon Shot: a small pale moon that drifts down
## the field, breathing in and out, until its proximity fuse trips. Reference footage shows
## a near-white sphere with a hard dark rim and warm peach shading gathering along the lower
## edge, brightest a little above centre. It is drawn here rather than cut from the original
## game, and lives in the atlas so it batches with every other bullet.
##
## Run: Godot --headless --path <project> --script res://tools/generate_moon_mote.gd

const CELL: int = 64
const ATLAS_POS := Vector2i(128, 128)
const SUPERSAMPLE: int = 4

const CENTRE := Vector2(31.5, 31.5)
const RADIUS: float = 30.0
## Thickness of the dark outline. The original's is a crisp line, not a vignette: roughly
## 2px on a 42px sphere, held about the same weight the whole way round.
const RIM_THICKNESS: float = 2.2
## Just enough inward feather to keep the line from aliasing against the body. The outer
## edge is left to the supersampler.
const RIM_FEATHER: float = 0.7
## Where the light comes from, in unit-disc coordinates. Sampling the original shows the
## upper-left staying near-white out to the very edge while every other direction reddens.
const HIGHLIGHT := Vector2(-0.35, -0.35)

# Sampled off the reference frame at 4.05s. The body is far less peachy than it looks at a
# glance: the middle is all but neutral, and the pink only arrives near the edge.
const COL_CORE := Color(0.937, 0.937, 0.933)
const COL_MID := Color(0.945, 0.796, 0.816)
const COL_WARM := Color(0.812, 0.620, 0.635)
## Rim at the top of the sphere, where it is darkest.
const COL_RIM_DARK := Color(0.239, 0.102, 0.180)
## Rim along the bottom, which the original lifts only slightly.
const COL_RIM_LIGHT := Color(0.349, 0.216, 0.286)

func _init() -> void:
	print("--- Generating Reisen moon mote and updating danmaku atlas ---")
	var img := Image.create(CELL, CELL, false, Image.FORMAT_RGBA8)
	var step: float = 1.0 / float(SUPERSAMPLE)

	for y in range(CELL):
		for x in range(CELL):
			var accum := Color(0.0, 0.0, 0.0, 0.0)
			for sy in range(SUPERSAMPLE):
				var py: float = float(y) + (float(sy) + 0.5) * step
				for sx in range(SUPERSAMPLE):
					var px: float = float(x) + (float(sx) + 0.5) * step
					var col := _sample_moon(Vector2(px, py))
					accum.r += col.r * col.a
					accum.g += col.g * col.a
					accum.b += col.b * col.a
					accum.a += col.a
			var inv_n: float = 1.0 / float(SUPERSAMPLE * SUPERSAMPLE)
			var final_a: float = accum.a * inv_n
			if final_a > 0.005:
				img.set_pixel(x, y, Color(accum.r / accum.a, accum.g / accum.a, accum.b / accum.a, clampf(final_a, 0.0, 1.0)))
			else:
				img.set_pixel(x, y, Color(0.0, 0.0, 0.0, 0.0))

	var mote_path: String = ProjectSettings.globalize_path("res://assets/bullets/reisen_moon_mote.png")
	img.save_png(mote_path)
	print("Saved standalone asset: res://assets/bullets/reisen_moon_mote.png")

	var atlas_path: String = ProjectSettings.globalize_path("res://assets/bullets/danmaku_atlas.png")
	var atlas := Image.load_from_file(atlas_path)
	assert(atlas != null, "danmaku_atlas.png must load")
	# blit_rect replaces the cell outright rather than blending into it, so re-running this
	# tool is idempotent. The cell is reserved for the mote in DANMAKU_CATALOG.md.
	atlas.blit_rect(img, Rect2i(0, 0, CELL, CELL), ATLAS_POS)
	atlas.save_png(atlas_path)
	print("Updated master atlas at Rect2(%d, %d, %d, %d)" % [ATLAS_POS.x, ATLAS_POS.y, CELL, CELL])

	quit(0)

func _sample_moon(p: Vector2) -> Color:
	var offset: Vector2 = p - CENTRE
	var dist: float = offset.length()
	if dist > RADIUS:
		return Color(0.0, 0.0, 0.0, 0.0)

	var unit: Vector2 = offset / RADIUS
	# The rim runs dark at the top and lifts toward the bottom, which is what keeps the
	# sphere from reading as a flat disc with an ink outline round it.
	var rim_col: Color = COL_RIM_DARK.lerp(COL_RIM_LIGHT, 0.5 + 0.5 * unit.y)

	var rim_inner: float = RADIUS - RIM_THICKNESS
	if dist >= rim_inner:
		return rim_col

	# Body shading. The original is mostly concentric - a white core inside a pink annulus -
	# with only a modest lean toward the highlight, which is what puts pink along both side
	# edges at once instead of sweeping it all to one corner.
	var t_radial: float = clampf((dist / rim_inner - 0.62) / 0.38, 0.0, 1.0)
	var t_dir: float = clampf((unit.distance_to(HIGHLIGHT) - 0.55) / 0.85, 0.0, 1.0)
	var t: float = clampf(0.60 * t_radial + 0.40 * t_dir, 0.0, 1.0)
	var body: Color
	if t < 0.45:
		body = COL_CORE.lerp(COL_MID, t / 0.45)
	else:
		body = COL_MID.lerp(COL_WARM, (t - 0.45) / 0.55)

	# Feather the last sliver into the rim so the outline is not a hard step.
	var into_rim: float = dist - (rim_inner - RIM_FEATHER)
	if into_rim > 0.0:
		body = body.lerp(rim_col, into_rim / RIM_FEATHER)

	return body
