extends SceneTree

## Superseded: the committed `clownpiece_player.png` is a ComfyUI-made sheet (see MEMO.md, Clownpiece).
## Running this replaces it with the older S-bend sheet described below, kept as a fallback.
##
## Builds Clownpiece's player sheet from the AutoSprite render at `clownpiece_raw_sheet.png`.
##
## The raw sheet is a 4x4 grid of 128x128 cells on a flat dithered grey. Only its upright cell is
## used: the render's own sway frames only turn her head, which read as twitching. The grey is
## flood-filled out from the cell's border so no interior pixel can be punched through; the
## wings carry a purple tint that keeps them clear of it.
##
## Banking follows PoFV: the body bends into a soft S rather than rotating as one piece. Each
## pixel row of the idle frame slides sideways by whole pixels along BANK_LEFT, then the frame
## leans a few degrees into the turn. Both ease in over the first frames of the row, as pl00's
## sprites 8-11 do, and hold at full bend.
##
## Cells are 48x48 rather than PoFV's 32x48 because the wings are wider than a 32px cell;
## Sprite2D derives the cell size from the texture, so the player scene needs no change.
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

const IDLE_CELL := Vector2i(0, 0)

## Sideways offset per body row when banking left, in pixels (negative = left), top of the sprite
## to the bottom and resampled over her height. Measured from Reimu's pl00.png: the centre of
## each row in her held banks (sprites 12-15, and 20-23 mirrored) against idle (0-7), smoothed
## over 3 rows. Head leans into the turn, torso holds, hips swing out, knees return, feet trail.
## The top three rows measure -6.7, -5.2, -3.1, which is her bow's ribbons swinging rather than
## her head; applied to a hat that shears the tip off, so they are capped into a head tilt.
const BANK_LEFT: Array[float] = [
	-2.0, -1.6, -1.3, -1.2, -0.6, -0.5, -0.7, -0.9, -1.2, -1.1, -0.9, -0.5, -0.3, -0.2,
	-0.4, -0.8, -0.8, -0.5, -0.1, 0.1, 0.3, 0.8, 1.2, 1.6, 1.7, 1.8, 1.9, 1.8, 1.3, 0.6,
	0.3, 0.2, 0.6, 1.5, 2.8, 4.0, 4.8, 5.0, 5.0, 5.0, 5.3, 5.5,
]
## Share of the full bend on each frame of a bank row; the row holds its last frame.
const BANK_EASE: Array[float] = [0.3, 0.55, 0.8, 1.0, 1.0, 1.0, 1.0, 1.0]
## Whole-body lean into the turn at full bend, on top of the S. Barely visible on purpose.
const LEAN_DEGREES: float = 3.0
## The lean is rotated at this scale with no blending, then each block keeps its commonest
## colour, so it adds no colours the sprite didn't already have.
const LEAN_SCALE: int = 8

func _init() -> void:
	var raw := Image.load_from_file(ProjectSettings.globalize_path(RAW))
	assert(raw != null, "clownpiece_raw_sheet.png must load")
	raw.convert(Image.FORMAT_RGBA8)

	var sheet := Image.create_empty(OUT_CELL.x * FRAMES_PER_ROW, OUT_CELL.y * 3, false, Image.FORMAT_RGBA8)
	var idle := _cut(raw, IDLE_CELL)
	for i in FRAMES_PER_ROW:
		var left := _bank(idle, -BANK_EASE[i])
		var right := _bank(idle, BANK_EASE[i])
		for row_frame in [[0, idle], [1, left], [2, right]]:
			sheet.blit_rect(row_frame[1], Rect2i(Vector2i.ZERO, OUT_CELL), Vector2i(i, row_frame[0]) * OUT_CELL)
	sheet.save_png(ProjectSettings.globalize_path(PLAYER_OUT))

	print("clownpiece player sheet written")
	quit()

## Keys and downscales one source cell. Colour is premultiplied through the resampling so the
## cleared background does not bleed a dark fringe into the edges.
func _cut(raw: Image, cell: Vector2i) -> Image:
	var src := raw.get_region(Rect2i(cell * SRC_CELL, Vector2i(SRC_CELL, SRC_CELL)))
	_clear_background(src)
	_multiply_alpha(src, true)
	var img := src.get_region(SRC_WINDOW)
	img.resize(OUT_CELL.x, OUT_CELL.y, Image.INTERPOLATE_LANCZOS)
	_multiply_alpha(img, false)
	return img

## Bends `frame` into the bank. `amount` runs -1..1: negative banks left, positive right.
func _bank(frame: Image, amount: float) -> Image:
	var w := frame.get_width()
	var h := frame.get_height()
	var src := frame.get_data()
	var top := -1
	var bottom := -1
	for y in h:
		for x in w:
			if src[(y * w + x) * 4 + 3] > 0:
				if top < 0:
					top = y
				bottom = y
				break
	var bent := PackedByteArray()
	bent.resize(src.size())
	var last := BANK_LEFT.size() - 1
	for y in range(top, bottom + 1):
		var t := float(y - top) / float(maxi(bottom - top, 1)) * last
		var shift := _round_half_even(_interp(t) * -amount)
		for x in w:
			var from := x - shift
			if from < 0 or from >= w:
				continue
			for c in 4:
				bent[(y * w + x) * 4 + c] = src[(y * w + from) * 4 + c]
	var pivot := Vector2(w / 2.0, (top + bottom) / 2.0)
	return _lean(bent, w, h, LEAN_DEGREES * -amount, pivot)

## BANK_LEFT sampled at fractional index `t`, linearly between neighbours.
func _interp(t: float) -> float:
	var last := BANK_LEFT.size() - 1
	if t >= last:
		return BANK_LEFT[last]
	var j := floori(t)
	return (BANK_LEFT[j + 1] - BANK_LEFT[j]) * (t - j) + BANK_LEFT[j]

## Rounds .5 to the even neighbour, so a row's shift never depends on which side a tie falls.
func _round_half_even(v: float) -> int:
	var f := floorf(v)
	var diff := v - f
	if diff > 0.5 or (diff == 0.5 and int(f) % 2 != 0):
		return int(f) + 1
	return int(f)

## Rotates by `degrees` (counter-clockwise, so banking left tips the top left) about `pivot`.
## Works at LEAN_SCALE with nearest sampling, then each LEAN_SCALE block becomes its commonest
## opaque colour (ties to the lowest RGBA), or transparent when fewer than half are opaque.
func _lean(data: PackedByteArray, w: int, h: int, degrees: float, pivot: Vector2) -> Image:
	if absf(degrees) < 0.01:
		return Image.create_from_data(w, h, false, Image.FORMAT_RGBA8, data)
	var s := LEAN_SCALE
	var bw := w * s
	var bh := h * s
	var angle := -deg_to_rad(degrees)
	var a := cos(angle)
	var b := sin(angle)
	var d := -sin(angle)
	var e := cos(angle)
	var cx := pivot.x * s
	var cy := pivot.y * s
	var c0 := a * -cx + b * -cy + cx
	var f0 := d * -cx + e * -cy + cy
	var out := PackedByteArray()
	out.resize(w * h * 4)
	for y in h:
		for x in w:
			var counts := {}
			var opaque := 0
			for sy in s:
				for sx in s:
					var px := x * s + sx + 0.5
					var py := y * s + sy + 0.5
					var xin := a * px + b * py + c0
					var yin := d * px + e * py + f0
					if xin < 0.0 or yin < 0.0:
						continue
					var ix := int(xin) / s
					var iy := int(yin) / s
					if int(xin) >= bw or int(yin) >= bh:
						continue
					var i := (iy * w + ix) * 4
					if data[i + 3] == 0:
						continue
					opaque += 1
					var key := Vector4i(data[i], data[i + 1], data[i + 2], data[i + 3])
					counts[key] = counts.get(key, 0) + 1
			if opaque * 2 < s * s:
				continue
			var best := Vector4i.ZERO
			var best_count := -1
			for key in counts:
				var n: int = counts[key]
				if n > best_count or (n == best_count and _rgba_less(key, best)):
					best = key
					best_count = n
			var o := (y * w + x) * 4
			out[o] = best.x
			out[o + 1] = best.y
			out[o + 2] = best.z
			out[o + 3] = best.w
	return Image.create_from_data(w, h, false, Image.FORMAT_RGBA8, out)

func _rgba_less(p: Vector4i, q: Vector4i) -> bool:
	for c in 4:
		if p[c] != q[c]:
			return p[c] < q[c]
	return false

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
