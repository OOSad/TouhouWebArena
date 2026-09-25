class_name CharacterSprites
extends RefCounted
## Builds each PoFV character's player sheet from their plNN.anm in the player's th09.dat,
## and hands it to their CharacterData.
##
## plNN.png starts with the player's 24 frames: sprites 0-23, an 8x3 grid of 32x48 cells
## (idle, bank left, bank right). Each sprite is 30x46 at +1,+1 inside its cell. The 1px
## border around it can hold slivers of the neighbouring frame (Yuuka's parasol tips), so
## each frame is cut by its own sprite rectangle, leaving the border transparent.

## Bump when changing how the sheets are built, so saved copies aren't reused.
const VERSION: int = 2

## Character id (res://resources/characters/<id>.tres) -> PoFV player number.
## Clownpiece's player sheet is fan art in the project, not a PoFV character.
const PLAYER_SHEETS: Dictionary = {
	"reimu": "00",
	"marisa": "01",
	"sakuya": "02",
	"youmu": "03",
	"reisen": "04",
	"cirno": "05",
	"yuuka": "09",
	"aya": "10",
}

## Each character's normal shot: [PoFV player number, sprite on plNN.png, turn upright, alpha].
## The anm rotates shots to their heading and most are drawn pointing right, so they are
## turned a quarter anticlockwise to point up, which is how the game draws player bullets.
## Every shot script draws at alpha 0x80, Cirno's at 0xa0 (script 5 in each plNN.anm).
## Yuuka's is the one script without __rotate_auto, so hers stays as drawn. Clownpiece has no
## PoFV shot and borrows Reimu's amulet.
const SHOTS: Dictionary = {
	"reimu": ["00", 24, true, 128.0 / 255.0],
	"marisa": ["01", 24, true, 128.0 / 255.0],
	"sakuya": ["02", 24, true, 128.0 / 255.0],
	"youmu": ["03", 24, true, 128.0 / 255.0],
	"reisen": ["04", 24, true, 128.0 / 255.0],
	"cirno": ["05", 28, true, 160.0 / 255.0],
	"yuuka": ["09", 24, false, 128.0 / 255.0],
	"aya": ["10", 24, true, 128.0 / 255.0],
	"clownpiece": ["00", 24, true, 128.0 / 255.0],
}

## Spell declaration banners, from plNN_ct00.png: PoFV stores each as a 256x85 body at
## (0, 0) and its 32x85 right-hand end at (0, 85), drawn butted together (scripts 15 and
## 16). The sheet's second pair is the mirrored copy; SpellBanner flips the banner itself.
const BANNERS: Array[String] = ["reimu", "marisa", "sakuya", "youmu", "reisen", "cirno", "yuuka"]
const BANNER_BODY := Rect2i(0, 0, 256, 85)
const BANNER_END := Rect2i(0, 85, 32, 85)

## Level 4 spell card backgrounds: character -> {base, anim} images in their plNN.anm.
## (Sakuya's are her distortion and clock: our own art and DatTextures.)
const SPELL_BGS: Dictionary = {
	"reimu": {"base": "cdbg00.png", "anim": "cdbg00b.png"},
	"marisa": {"base": "cdbg01.png", "anim": "cdbg01b.png"},
	"youmu": {"base": "cdbg03.png", "anim": "cdbg03b.png"},
	"reisen": {"base": "cdbg04.png", "anim": "cdbg04b.png"},
	"cirno": {"base": "cdbg00.png"},
	"yuuka": {"base": "cdbg09b.png", "anim": "cdbg09.png"},
}

const FRAMES: int = 24
const SHEET_SIZE := Vector2i(256, 144)
const CHARACTERS: String = "res://resources/characters/%s.tres"

## Result screen faces, in PoFV's face id order (pl*_fc_no .. _lo = 0..8, as the match
## scripts number them). Each is a 256x256 texture with a 256x64 strip under it
## (plNN_fc_xx.png and plNN_fc_xx_b.png), laid side by side in one 2304x320 image.
const FACES: Array[String] = ["no", "n2", "hp", "an", "sw", "dp", "pr", "sp", "lo"]
const FACE_SIZE := Vector2i(256, 320)
const FACE_TOP_HEIGHT: int = 256


## The 256x144 sheet from plNN.anm, or null if it isn't there.
static func build_player_sheet(anm: ThAnm, pl: String) -> Image:
	var sheet_name := "data/pl/pl%s/pl%s.png" % [pl, pl]
	var source := anm.sheet_image(sheet_name) if anm else null
	if source == null:
		push_warning("CharacterSprites: th09.dat has no %s" % sheet_name)
		return null
	var sheet := Image.create(SHEET_SIZE.x, SHEET_SIZE.y, false, Image.FORMAT_RGBA8)
	for id in FRAMES:
		var rect := anm.sprite_rect(id)
		sheet.blit_rect(source, rect, rect.position)
	return sheet


## A character's shot sprite from plNN.anm, or null if it isn't there.
static func build_shot(anm: ThAnm, character: String) -> Image:
	var recipe: Array = SHOTS[character]
	var sheet_name := "data/pl/pl%s/pl%s.png" % [recipe[0], recipe[0]]
	var source := anm.sheet_image(sheet_name) if anm else null
	if source == null:
		push_warning("CharacterSprites: th09.dat has no %s" % sheet_name)
		return null
	var shot := source.get_region(anm.sprite_rect(recipe[1]))
	if recipe[2]:
		shot.rotate_90(COUNTERCLOCKWISE)
	if recipe[3] < 1.0:
		for y in shot.get_height():
			for x in shot.get_width():
				var c := shot.get_pixel(x, y)
				c.a *= recipe[3]
				shot.set_pixel(x, y, c)
	return shot


static func apply_shot(character: String, shot: Image) -> CharacterData:
	if shot == null:
		return null
	var data := load(CHARACTERS % character) as CharacterData
	data.bullet_texture = ImageTexture.create_from_image(shot)
	return data


## A character's 288x85 spell banner, or null if their cut-in sheet isn't there.
static func build_banner(anm: ThAnm, character: String) -> Image:
	var pl: String = PLAYER_SHEETS[character]
	var sheet_name := "data/pl/pl%s/pl%s_ct00.png" % [pl, pl]
	var source := anm.sheet_image(sheet_name) if anm else null
	if source == null:
		push_warning("CharacterSprites: th09.dat has no %s" % sheet_name)
		return null
	var banner := Image.create(BANNER_BODY.size.x + BANNER_END.size.x, BANNER_BODY.size.y, false, Image.FORMAT_RGBA8)
	banner.blit_rect(source, BANNER_BODY, Vector2i.ZERO)
	banner.blit_rect(source, BANNER_END, Vector2i(BANNER_BODY.size.x, 0))
	return banner


## One of a character's spell backgrounds ("base" or "anim"), or null if it isn't there.
static func build_spell_bg(anm: ThAnm, character: String, kind: String) -> Image:
	var file: String = SPELL_BGS[character][kind]
	var image := anm.sheet_image_by_file(file) if anm else null
	if image == null:
		push_warning("CharacterSprites: pl%s.anm has no %s" % [PLAYER_SHEETS[character], file])
	return image


static func apply_banner(character: String, banner: Image) -> CharacterData:
	if banner == null:
		return null
	var data := load(CHARACTERS % character) as CharacterData
	data.spell_banner_texture = ImageTexture.create_from_image(banner)
	return data


static func apply_spell_bg(character: String, kind: String, image: Image) -> CharacterData:
	if image == null:
		return null
	var data := load(CHARACTERS % character) as CharacterData
	var texture := ImageTexture.create_from_image(image)
	if kind == "base":
		data.spell_bg_base_texture = texture
	else:
		data.spell_bg_anim_texture = texture
	return data


## Gives the character their sheet. Returns their CharacterData so the caller can keep it
## referenced and the resource cache holds on to this copy.
static func apply_player_sheet(character: String, sheet: Image) -> CharacterData:
	if sheet == null:
		return null
	var data := load(CHARACTERS % character) as CharacterData
	data.sprite_sheet = ImageTexture.create_from_image(sheet)
	return data


## A character's nine faces side by side, or null if any is missing.
static func build_faces(anm: ThAnm, pl: String) -> Image:
	var faces := Image.create(FACE_SIZE.x * FACES.size(), FACE_SIZE.y, false, Image.FORMAT_RGBA8)
	for i in FACES.size():
		var file := "pl%s_fc_%s.png" % [pl, FACES[i]]
		var top := anm.sheet_image_by_file(file) if anm else null
		var bottom := anm.sheet_image_by_file(file.replace(".png", "_b.png")) if anm else null
		if top == null or bottom == null:
			push_warning("CharacterSprites: pl%s.anm has no %s" % [pl, file])
			return null
		var x := i * FACE_SIZE.x
		faces.blit_rect(top, Rect2i(0, 0, FACE_SIZE.x, FACE_TOP_HEIGHT), Vector2i(x, 0))
		faces.blit_rect(bottom, Rect2i(0, 0, FACE_SIZE.x, FACE_SIZE.y - FACE_TOP_HEIGHT), Vector2i(x, FACE_TOP_HEIGHT))
	return faces


static func apply_faces(character: String, faces: Image) -> CharacterData:
	if faces == null:
		return null
	var data := load(CHARACTERS % character) as CharacterData
	data.face_sheet = ImageTexture.create_from_image(faces)
	return data
