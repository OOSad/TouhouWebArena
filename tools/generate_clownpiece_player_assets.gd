extends SceneTree

## Cuts Clownpiece's player sheet from the AutoSprite render at `clownpiece_raw_sheet.png`.
##
## The raw sheet is a 4x4 grid of 128x128 cells on a flat dithered grey, read left to right as
## one sway loop: upright (row 0), leaning left (rows 1-2), back through upright and leaning
## right (rows 2-3). The grey is flood-filled out from each cell's border so no interior pixel
## can be punched through; the wings carry a purple tint that keeps them clear of it.
##
## The render keeps her in place, so frames keep their native position and are all scaled by
## the same factor. Cells are 48x48 rather than PoFV's 32x48 because the wings are wider than
## a 32px cell; Sprite2D derives the cell size from the texture, so the player scene needs no
## change.
##
## Run: Godot --headless --path <project> --script res://tools/generate_clownpiece_player_assets.gd

const RAW := "res://assets/characters/clownpiece/clownpiece_raw_sheet.png"
const PLAYER_OUT := "res://assets/characters/clownpiece/clownpiece_player.png"

const SRC_CELL: int = 128
## Square window in each source cell holding every frame, hat bobble to toes.
const SRC_WINDOW := Rect2i(31, 30, 64, 64)
const OUT_CELL := Vector2i(48, 48)
const FRAMES_PER_ROW: int = 8

const BACKGROUND := Color8(0x95, 0x90, 0x89)
## Per-channel distance from BACKGROUND still counted as background; the dither spans +-2.
const BACKGROUND_TOLERANCE: float = 8.0 / 255.0

## Cells as (column, row). Idle holds one upright cell: animating through the upright cells
## read as twitching, and one of them carries a grey smear by the torch.
const IDLE_CELL := Vector2i(0, 0)
## Banks in order of increasing lean, holding the deepest lean once reached. The render's own
## lean is only a head turn, so each frame is also tilted by BANK_TILT_DEGREES around the
## hitbox, top towards the direction of travel.
const BANK_LEFT: Array[Vector2i] = [
	Vector2i(0, 1), Vector2i(1, 2), Vector2i(1, 1), Vector2i(0, 2),
	Vector2i(3, 1), Vector2i(2, 1), Vector2i(2, 1), Vector2i(2, 1),
]
const BANK_RIGHT: Array[Vector2i] = [
	Vector2i(2, 2), Vector2i(2, 3), Vector2i(3, 2), Vector2i(1, 3),
	Vector2i(0, 3), Vector2i(0, 3), Vector2i(0, 3), Vector2i(0, 3),
]
const BANK_TILT_DEGREES: Array[float] = [2.0, 4.0, 6.0, 8.0, 10.0, 12.0, 12.0, 12.0]

func _init() -> void:
	var raw := Image.load_from_file(ProjectSettings.globalize_path(RAW))
	assert(raw != null, "clownpiece_raw_sheet.png must load")
	raw.convert(Image.FORMAT_RGBA8)

	var sheet := Image.create_empty(OUT_CELL.x * FRAMES_PER_ROW, OUT_CELL.y * 3, false, Image.FORMAT_RGBA8)
	var idle := _cut(raw, IDLE_CELL, 0.0)
	for i in FRAMES_PER_ROW:
		var left := _cut(raw, BANK_LEFT[i], -BANK_TILT_DEGREES[i])
		var right := _cut(raw, BANK_RIGHT[i], BANK_TILT_DEGREES[i])
		for row_frame in [[0, idle], [1, left], [2, right]]:
			sheet.blit_rect(row_frame[1], Rect2i(Vector2i.ZERO, OUT_CELL), Vector2i(i, row_frame[0]) * OUT_CELL)
	sheet.save_png(ProjectSettings.globalize_path(PLAYER_OUT))

	print("clownpiece player sheet written")
	quit()

## Keys, tilts (clockwise degrees, positive leans right) and downscales one source cell.
## Colour is premultiplied through the resampling so the cleared background does not bleed a
## dark fringe into the edges.
func _cut(raw: Image, cell: Vector2i, tilt_degrees: float) -> Image:
	var src := raw.get_region(Rect2i(cell * SRC_CELL, Vector2i(SRC_CELL, SRC_CELL)))
	_clear_background(src)
	_multiply_alpha(src, true)
	var img := src.get_region(SRC_WINDOW)
	if tilt_degrees != 0.0:
		img = _rotated(src, deg_to_rad(tilt_degrees))
	img.resize(OUT_CELL.x, OUT_CELL.y, Image.INTERPOLATE_LANCZOS)
	_multiply_alpha(img, false)
	return img

## SRC_WINDOW of `src` rotated about the window centre, which lands on the hitbox.
## Bilinear samples, so `src` must already be premultiplied.
func _rotated(src: Image, angle: float) -> Image:
	var out := Image.create_empty(SRC_WINDOW.size.x, SRC_WINDOW.size.y, false, Image.FORMAT_RGBA8)
	var pivot := Vector2(SRC_WINDOW.get_center())
	for y in SRC_WINDOW.size.y:
		for x in SRC_WINDOW.size.x:
			var from := (Vector2(SRC_WINDOW.position + Vector2i(x, y)) + Vector2(0.5, 0.5) - pivot).rotated(-angle) + pivot
			out.set_pixel(x, y, _sample_bilinear(src, from - Vector2(0.5, 0.5)))
	return out

func _sample_bilinear(img: Image, p: Vector2) -> Color:
	var x0 := floori(p.x)
	var y0 := floori(p.y)
	var f := p - Vector2(x0, y0)
	var top := _pixel_or_clear(img, x0, y0).lerp(_pixel_or_clear(img, x0 + 1, y0), f.x)
	var bottom := _pixel_or_clear(img, x0, y0 + 1).lerp(_pixel_or_clear(img, x0 + 1, y0 + 1), f.x)
	return top.lerp(bottom, f.y)

func _pixel_or_clear(img: Image, x: int, y: int) -> Color:
	if x < 0 or y < 0 or x >= img.get_width() or y >= img.get_height():
		return Color(0, 0, 0, 0)
	return img.get_pixel(x, y)

func _clear_background(cell: Image) -> void:
	var size := cell.get_size()
	var stack: Array[Vector2i] = []
	for x in size.x:
		stack.append(Vector2i(x, 0))
		stack.append(Vector2i(x, size.y - 1))
	for y in size.y:
		stack.append(Vector2i(0, y))
		stack.append(Vector2i(size.x - 1, y))
	while not stack.is_empty():
		var p: Vector2i = stack.pop_back()
		if p.x < 0 or p.y < 0 or p.x >= size.x or p.y >= size.y:
			continue
		var c := cell.get_pixelv(p)
		if c.a == 0.0 or not _is_background(c):
			continue
		cell.set_pixelv(p, Color(0, 0, 0, 0))
		stack.append_array([p + Vector2i.LEFT, p + Vector2i.RIGHT, p + Vector2i.UP, p + Vector2i.DOWN])

func _is_background(c: Color) -> bool:
	return absf(c.r - BACKGROUND.r) <= BACKGROUND_TOLERANCE \
		and absf(c.g - BACKGROUND.g) <= BACKGROUND_TOLERANCE \
		and absf(c.b - BACKGROUND.b) <= BACKGROUND_TOLERANCE

func _multiply_alpha(img: Image, premultiply: bool) -> void:
	for y in img.get_height():
		for x in img.get_width():
			var c := img.get_pixel(x, y)
			if premultiply:
				c = Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a)
			elif c.a > 0.0:
				c = Color(minf(c.r / c.a, 1.0), minf(c.g / c.a, 1.0), minf(c.b / c.a, 1.0), c.a)
			img.set_pixel(x, y, c)
