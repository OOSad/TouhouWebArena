class_name ThAnm
extends RefCounted
## Reads the sprite sheets inside a .anm file taken from the player's own .dat.
##
## An .anm is a chain of entries; each has a sprite list (rectangles on its image) and an
## embedded "THTX" texture. Animation scripts are skipped; we only need pixels. Two header
## layouts, both 64 bytes (https://github.com/thpatch/thtk, anm_types.h):
## anm_header06_t (TH09's version 3) and anm_header11_t (version 7+, TH11 onwards, so TH15).

const HEADER_SIZE: int = 64

const FORMAT_BGRA8888: int = 1
const FORMAT_RGB565: int = 3
const FORMAT_ARGB4444: int = 5
const FORMAT_GRAY8: int = 7

var error: String = ""

var _data: PackedByteArray
# Each: {name, offset (entry start), thtx_offset (0 if none)}
var _entries: Array[Dictionary] = []
# sprite id -> {entry (index), rect (Rect2i)}
var _sprites: Dictionary = {}
# entry index -> decoded Image
var _images: Dictionary = {}


## `modern` selects the anm_header11_t layout (TH11 onwards, from a THA1 archive).
func parse(data: PackedByteArray, modern: bool = false) -> bool:
	_data = data
	_entries.clear()
	_sprites.clear()
	_images.clear()
	var base := 0
	while true:
		if base + HEADER_SIZE > data.size():
			error = "sprite sheet file is damaged"
			return false
		var sprite_count: int
		var name_offset: int
		var thtx_offset: int
		var next_offset: int
		if modern:
			sprite_count = data.decode_u16(base + 4)
			name_offset = data.decode_u32(base + 16)
			thtx_offset = data.decode_u32(base + 28)
			next_offset = data.decode_u32(base + 36)
		else:
			sprite_count = data.decode_u32(base)
			name_offset = data.decode_u32(base + 28)
			thtx_offset = data.decode_u32(base + 48)
			next_offset = data.decode_u32(base + 56)
		var name_end := data.find(0, base + name_offset)
		var index := _entries.size()
		_entries.append({
			"name": data.slice(base + name_offset, name_end).get_string_from_ascii(),
			"offset": base,
			"thtx_offset": thtx_offset,
		})
		for s in sprite_count:
			var sp := base + data.decode_u32(base + HEADER_SIZE + s * 4)
			var rect := Rect2i(
				roundi(data.decode_float(sp + 4)), roundi(data.decode_float(sp + 8)),
				roundi(data.decode_float(sp + 12)), roundi(data.decode_float(sp + 16)))
			_sprites[data.decode_u32(sp)] = {"entry": index, "rect": rect}
		if next_offset == 0:
			break
		base += next_offset
	error = ""
	return true


## A whole decoded sheet by its path inside the .anm (e.g. "bullet/bullet2.png"), or null.
func sheet_image(sheet_name: String) -> Image:
	for i in _entries.size():
		if _entries[i].name == sheet_name:
			return entry_image(i)
	return null


## A whole decoded sheet by its file name alone (e.g. "cdbg00.png"), for sheets whose folder
## varies. Returns the first match, or null.
func sheet_image_by_file(file_name: String) -> Image:
	for i in _entries.size():
		if _entries[i].name.get_file() == file_name:
			return entry_image(i)
	return null


func entry_count() -> int:
	return _entries.size()


## Where a sprite sits on its sheet, or an empty rect if it doesn't exist.
func sprite_rect(sprite_id: int) -> Rect2i:
	return _sprites[sprite_id].rect if _sprites.has(sprite_id) else Rect2i()


func has_sprite(sprite_id: int) -> bool:
	return _sprites.has(sprite_id)


## Returns one sprite cut out of its sheet, or null if it doesn't exist.
func sprite_image(sprite_id: int) -> Image:
	if not _sprites.has(sprite_id):
		return null
	var sprite: Dictionary = _sprites[sprite_id]
	var sheet := entry_image(sprite.entry)
	if sheet == null:
		return null
	# Scrolling strips can be taller than their sheet (the game wraps the texture); cut to it.
	var rect: Rect2i = sprite.rect.intersection(Rect2i(Vector2i.ZERO, sheet.get_size()))
	return sheet.get_region(rect)


## Decodes an entry's whole texture (cached), or null if it has none.
func entry_image(index: int) -> Image:
	if _images.has(index):
		return _images[index]
	var entry: Dictionary = _entries[index]
	if entry.thtx_offset == 0:
		return null
	var t: int = entry.offset + entry.thtx_offset
	if _data.slice(t, t + 4).get_string_from_ascii() != "THTX":
		return null
	var format := _data.decode_u16(t + 6)
	var w := _data.decode_u16(t + 8)
	var h := _data.decode_u16(t + 10)
	var image := decode_pixels(_data.slice(t + 16), format, w, h)
	_images[index] = image
	return image


## Converts ZUN's texture formats to RGBA8.
static func decode_pixels(src: PackedByteArray, format: int, w: int, h: int) -> Image:
	var count := w * h
	var out := PackedByteArray()
	out.resize(count * 4)
	match format:
		FORMAT_ARGB4444:
			for i in count:
				var lo := src[i * 2]      # 0xGB
				var hi := src[i * 2 + 1]  # 0xAR
				out[i * 4] = (hi & 0xf) * 17
				out[i * 4 + 1] = (lo >> 4) * 17
				out[i * 4 + 2] = (lo & 0xf) * 17
				out[i * 4 + 3] = (hi >> 4) * 17
		FORMAT_BGRA8888:
			for i in count:
				out[i * 4] = src[i * 4 + 2]
				out[i * 4 + 1] = src[i * 4 + 1]
				out[i * 4 + 2] = src[i * 4]
				out[i * 4 + 3] = src[i * 4 + 3]
		FORMAT_RGB565:
			for i in count:
				var v := src[i * 2] | (src[i * 2 + 1] << 8)
				# Widen each channel by repeating its top bits, so 0x1f becomes 0xff.
				var r := v >> 11
				var g := (v >> 5) & 0x3f
				var b := v & 0x1f
				out[i * 4] = (r << 3) | (r >> 2)
				out[i * 4 + 1] = (g << 2) | (g >> 4)
				out[i * 4 + 2] = (b << 3) | (b >> 2)
				out[i * 4 + 3] = 255
		FORMAT_GRAY8:
			for i in count:
				out[i * 4] = src[i]
				out[i * 4 + 1] = src[i]
				out[i * 4 + 2] = src[i]
				out[i * 4 + 3] = 255
		_:
			push_warning("ThAnm: unknown texture format %d" % format)
			return null
	return Image.create_from_data(w, h, false, Image.FORMAT_RGBA8, out)
