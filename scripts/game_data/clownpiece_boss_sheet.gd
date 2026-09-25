class_name ClownpieceBossSheet
extends RefCounted
## Builds Clownpiece's boss sprite sheet from LoLK's enm5.png (st05enm.anm in the player's
## th15.dat) at startup, and hands it to her BossData.
##
## st05enm.anm lays enm5.png out as: sprites 0-7 idle (64x96, row y=0), 8-11 and 16-19 the
## two banks (64x96, rows y=96 and y=192), 23-30 the torch-raising cast (64x112, row
## y=288). TH15 draws every sprite centred on the boss, so the 96-tall frames are centred
## in the 112-tall output cells rather than bottom-aligned.
##
## BossCharacter plays each row left to right (idle loops, the others hold their last cell)
## and mirrors the bank row when moving right, so each output row is the anm script's frame
## sequence laid out cell by cell:
## - idle: script 0, `0 1 2 3 4 5 6 5 4 7 2 1` at 6 frames each.
## - bank: script 1 (sprites 16-19), the left-leaning set, held on its deepest lean.
## - cast: script 5 (sprites 23-29), held on the last flare.

const BOSS_DATA: String = "res://resources/bosses/clownpiece_boss.tres"
const SHEET: String = "stgenm/stage05/enm5.png"

const CELL := Vector2i(64, 112)
const COLS: int = 12

const IDLE: Array[int] = [0, 1, 2, 3, 4, 5, 6, 5, 4, 7, 2, 1]
const BANK: Array[int] = [16, 17, 18, 19, 19, 19, 19, 19, 19, 19, 19, 19]
const CAST: Array[int] = [23, 24, 25, 26, 27, 28, 29, 29, 29, 29, 29, 29]


## Bump when changing how the sheet is built, so a saved copy isn't reused.
const VERSION: int = 1


## Builds the sheet from st05enm.anm, or null if it isn't there.
static func build(st05enm: ThAnm) -> Image:
	var raw := st05enm.sheet_image(SHEET) if st05enm else null
	if raw == null:
		push_warning("ClownpieceBossSheet: th15.dat has no %s" % SHEET)
		return null
	var sheet := Image.create_empty(CELL.x * COLS, CELL.y * 3, false, Image.FORMAT_RGBA8)
	var rows: Array = [IDLE, BANK, CAST]
	for row in rows.size():
		for col in COLS:
			var src := _sprite_rect(rows[row][col])
			var pad_y: int = (CELL.y - src.size.y) / 2
			sheet.blit_rect(raw, src, Vector2i(col * CELL.x, row * CELL.y + pad_y))
	return sheet


## Gives her BossData the sheet. Returns it so the caller can keep it referenced and the
## resource cache holds on to this copy.
static func apply(sheet: Image) -> BossData:
	if sheet == null:
		return null
	var data := load(BOSS_DATA) as BossData
	data.sprite_sheet = ImageTexture.create_from_image(sheet)
	return data


## Source rect of an enm5.png sprite id, per st05enm.anm.
static func _sprite_rect(id: int) -> Rect2i:
	if id >= 23:
		return Rect2i((id - 23) * 64, 288, 64, 112)
	return Rect2i((id % 8) * 64, (id / 8) * 96, 64, 96)
