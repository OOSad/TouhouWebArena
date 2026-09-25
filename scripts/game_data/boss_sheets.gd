class_name BossSheets
extends RefCounted
## Builds the PoFV characters' Level 4 boss sheets from their plNN.anm in the player's
## th09.dat, and hands each to its BossData. (Clownpiece's comes from th15.dat:
## ClownpieceBossSheet.)
##
## BossCharacter plays a sheet as three rows, left to right: idle (loops), bank (holds its
## last cell, mirrored when moving right) and cast (holds its last cell). Each recipe lists
## the anm sprite shown in every cell. Sprites are cut by their own rectangle, so pixels of
## neighbouring frames never come along.
##
## Most bosses are plNN_bs.png. Youmu and Reisen have none: PoFV reuses Youmu's Mountain of
## Faith sprite (stg9enm_ym.png, 48x64 frames) and Reisen's Imperishable Night stage 5 boss
## (stg5enm2.png), both inside their own plNN.anm.

## Bump when changing a recipe, so saved copies aren't reused.
const VERSION: int = 1

const BOSSES: String = "res://resources/bosses/%s_boss.tres"

## boss id -> recipe:
## pl: plNN.anm to read; sheet: image inside it; cell: output cell size;
## rows: sprite id per cell; place: where each sprite's top-left sits in its cell. A
## negative place crops the sprite to the cell (Reisen's 64px frames in 48px cells).
## place_by_sprite overrides place for particular sprites.
const RECIPES: Dictionary = {
	"reimu": {
		"pl": "00", "sheet": "data/pl/pl00/pl00_bs.png", "cell": Vector2i(64, 80),
		"rows": [[60, 61, 62, 63], [64, 65, 66, 67], [68, 69, 70, 71]],
	},
	"marisa": {
		"pl": "01", "sheet": "data/pl/pl01/pl01_bs.png", "cell": Vector2i(64, 80),
		"rows": [[60, 61, 62, 63], [64, 65, 66, 67], [68, 68, 68, 68]],
	},
	"sakuya": {
		"pl": "02", "sheet": "data/pl/pl02/pl02_bs.png", "cell": Vector2i(64, 80),
		"rows": [[63, 63, 63, 63], [67, 68, 69, 69], [63, 63, 63, 63]],
	},
	"youmu": {
		"pl": "03", "sheet": "data/pl/pl03/stg9enm_ym.png", "cell": Vector2i(64, 80),
		"rows": [[63, 64, 65, 66, 71], [67, 68, 69, 70, 72], [75, 76, 77, 78, 79]],
		"place": Vector2i(8, 16),
		# The first two swing frames are 48x80, so they fill the cell's height.
		"place_by_sprite": {75: Vector2i(8, 0), 76: Vector2i(8, 0)},
	},
	"reisen": {
		"pl": "04", "sheet": "data/pl/pl04/stg5enm2.png", "cell": Vector2i(48, 80),
		"rows": [[56, 56, 56, 56], [60, 61, 62, 63], [56, 56, 56, 56]],
		"place": Vector2i(-8, 0),
		# The bank row sits 1px higher, as the sheet the game was tuned against had it.
		"place_by_sprite": {60: Vector2i(-8, -1), 61: Vector2i(-8, -1), 62: Vector2i(-8, -1), 63: Vector2i(-8, -1)},
	},
	"cirno": {
		"pl": "05", "sheet": "data/pl/pl05/pl05_bs.png", "cell": Vector2i(64, 64),
		"rows": [[59, 60, 61, 62], [63, 64, 65, 66], [59, 60, 61, 62]],
	},
	"yuuka": {
		"pl": "09", "sheet": "data/pl/pl09/pl09_bs.png", "cell": Vector2i(64, 64),
		# Script 33 flickers between 68 and 69; played once, it settles on 69.
		"rows": [[60, 61, 62, 63], [64, 65, 66, 67], [68, 69, 68, 69]],
	},
}


## The sheet for one boss, or null if its image isn't in the .anm.
static func build(boss: String, anm: ThAnm) -> Image:
	var recipe: Dictionary = RECIPES[boss]
	var source := anm.sheet_image(recipe.sheet) if anm else null
	if source == null:
		push_warning("BossSheets: th09.dat has no %s" % recipe.sheet)
		return null
	var cell: Vector2i = recipe.cell
	var rows: Array = recipe.rows
	var default_place: Vector2i = recipe.get("place", Vector2i.ZERO)
	var place_by_sprite: Dictionary = recipe.get("place_by_sprite", {})
	var sheet := Image.create(cell.x * rows[0].size(), cell.y * rows.size(), false, Image.FORMAT_RGBA8)
	for row in rows.size():
		for col in rows[row].size():
			var id: int = rows[row][col]
			var sprite := anm.sprite_rect(id)
			var place: Vector2i = place_by_sprite.get(id, default_place)
			# Clip the placed sprite to its cell.
			var placed := Rect2i(place, sprite.size).intersection(Rect2i(Vector2i.ZERO, cell))
			var from := Rect2i(sprite.position + placed.position - place, placed.size)
			sheet.blit_rect(source, from, Vector2i(col, row) * cell + placed.position)
	return sheet


## Gives the boss its sheet. Returns its BossData so the caller can keep it referenced and
## the resource cache holds on to this copy.
static func apply(boss: String, sheet: Image) -> BossData:
	if sheet == null:
		return null
	var data := load(BOSSES % boss) as BossData
	data.sprite_sheet = ImageTexture.create_from_image(sheet)
	return data
