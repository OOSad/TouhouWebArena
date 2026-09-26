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
## PoFV shot: hers is a LoLK star (`BulletSprites.cut_th15_shot`), built beside these.
const SHOTS: Dictionary = {
	"reimu": ["00", 24, true, 128.0 / 255.0],
	"marisa": ["01", 24, true, 128.0 / 255.0],
	"sakuya": ["02", 24, true, 128.0 / 255.0],
	"youmu": ["03", 24, true, 128.0 / 255.0],
	"reisen": ["04", 24, true, 128.0 / 255.0],
	"cirno": ["05", 28, true, 160.0 / 255.0],
	"yuuka": ["09", 24, false, 128.0 / 255.0],
	"aya": ["10", 24, true, 128.0 / 255.0],
}

## Spell declaration banners, from plNN_ct00.png: PoFV stores each as a 256x85 body at
## (0, 0) and its 32x85 right-hand end at (0, 85), drawn butted together (scripts 15 and
## 16). The sheet's second pair is the mirrored copy; SpellBanner flips the banner itself.
const BANNERS: Array[String] = ["reimu", "marisa", "sakuya", "youmu", "reisen", "cirno", "yuuka", "aya"]
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
	"aya": {"base": "cdbg10.png", "anim": "cdbg10b.png"},
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
	fade_shot(shot, recipe[3])
	return shot


## Multiplies a shot's alpha, as every PoFV shot script draws at 0x80.
static func fade_shot(shot: Image, alpha: float) -> void:
	if shot == null or alpha >= 1.0:
		return
	for y in shot.get_height():
		for x in shot.get_width():
			var c := shot.get_pixel(x, y)
			c.a *= alpha
			shot.set_pixel(x, y, c)


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


## Clownpiece's Level 4 background, from LoLK's st05enm.anm in th15.dat (her Stage 5 spell
## background); SpellBackgroundOverlay's "twin_rotate" mode draws it.
const LOLK_SPELL_BG: Dictionary = {"base": "cdbg05a00.png", "anim": "cdbg05b00.png"}


## Clownpiece's spell banner. LoLK has no PoFV-style banner strip, so one is composed in
## PoFV's formula from th15.dat: her Level 4 tree pattern tinted in her hat's purple, her
## head from LoLK's spell cut-in (face05ct) over a flat silhouette of itself, and a frame
## line. The "SPELL ATTACK" lettering is recovered from the PoFV banners: it sits at the same
## spot on all of them, so the pixels they all agree on are the letters, and each letter's
## soft edge is as bright as it is on the darkest banner there. No idiom (the user's call).
const LOLK_BANNER_PATTERN_CROP := Rect2i(0, 120, 384, 114)
const LOLK_BANNER_DARK := Color(0.2, 0.08, 0.3)
const LOLK_BANNER_LIGHT := Color(0.5, 0.32, 0.62)
const LOLK_BANNER_SILHOUETTE := Color(0.5, 0.1, 0.35)
const LOLK_BANNER_SILHOUETTE_OFFSET := Vector2i(-18, 0)
## Her face's centre on face05ct, where it lands on the banner, and the cut-in's scale there
## (her head the size of Aya's and Reisen's, checked side by side).
const LOLK_BANNER_FACE_ON_CUTIN := Vector2(140, 205)
const LOLK_BANNER_FACE_AT := Vector2(96, 54)
const LOLK_BANNER_CUTIN_SCALE: float = 1.1

static func build_lolk_banner(st05enm: ThAnm, pofv_banners: Array[Image]) -> Image:
	var pattern := st05enm.sheet_image_by_file(LOLK_SPELL_BG["base"]) if st05enm else null
	var cutin := st05enm.sheet_image_by_file("face05ct.png") if st05enm else null
	if pattern == null or cutin == null or pofv_banners.size() < 2:
		push_warning("CharacterSprites: can't build Clownpiece's banner (th15.dat or PoFV banners missing)")
		return null
	var size := Vector2i(BANNER_BODY.size.x + BANNER_END.size.x, BANNER_BODY.size.y)
	var banner := Image.create(size.x, size.y, false, Image.FORMAT_RGBA8)

	var backdrop := pattern.get_region(LOLK_BANNER_PATTERN_CROP)
	backdrop.convert(Image.FORMAT_RGBA8)
	backdrop.resize(size.x, size.y, Image.INTERPOLATE_LANCZOS)
	for y in size.y:
		for x in size.x:
			var c := backdrop.get_pixel(x, y)
			var luma := c.r * 0.3 + c.g * 0.59 + c.b * 0.11
			banner.set_pixel(x, y, LOLK_BANNER_DARK.lerp(LOLK_BANNER_LIGHT, clampf((luma - 0.15) / 0.5, 0.0, 1.0)))

	var head: Image = cutin.duplicate() as Image
	head.convert(Image.FORMAT_RGBA8)
	head.resize(roundi(head.get_width() * LOLK_BANNER_CUTIN_SCALE), roundi(head.get_height() * LOLK_BANNER_CUTIN_SCALE), Image.INTERPOLATE_LANCZOS)
	var at := Vector2i(LOLK_BANNER_FACE_AT - LOLK_BANNER_FACE_ON_CUTIN * LOLK_BANNER_CUTIN_SCALE)
	var silhouette := Image.create(head.get_width(), head.get_height(), false, Image.FORMAT_RGBA8)
	for y in head.get_height():
		for x in head.get_width():
			if head.get_pixel(x, y).a > 0.9:
				silhouette.set_pixel(x, y, LOLK_BANNER_SILHOUETTE)
	banner.blend_rect(silhouette, Rect2i(Vector2i.ZERO, silhouette.get_size()), at + LOLK_BANNER_SILHOUETTE_OFFSET)
	banner.blend_rect(head, Rect2i(Vector2i.ZERO, head.get_size()), at)

	banner.blend_rect(_spell_attack_lettering(pofv_banners, size), Rect2i(Vector2i.ZERO, size), Vector2i.ZERO)

	var edge := LOLK_BANNER_DARK.darkened(0.3)
	for x in size.x:
		banner.set_pixel(x, 0, edge)
		banner.set_pixel(x, size.y - 1, edge)
	for y in size.y:
		banner.set_pixel(0, y, edge)
		banner.set_pixel(size.x - 1, y, edge)
	return banner


## The white "SPELL ATTACK" letters every PoFV banner shares, on a transparent image.
static func _spell_attack_lettering(banners: Array[Image], size: Vector2i) -> Image:
	const AGREE: float = 0.06
	var core := {}
	for y in size.y:
		for x in size.x:
			var first := banners[0].get_pixel(x, y)
			if first.v <= 0.8:
				continue
			var shared := true
			for i in range(1, banners.size()):
				var c := banners[i].get_pixel(x, y)
				if absf(c.r - first.r) > AGREE or absf(c.g - first.g) > AGREE or absf(c.b - first.b) > AGREE:
					shared = false
					break
			if shared:
				core[Vector2i(x, y)] = true
	var letters := Image.create(size.x, size.y, false, Image.FORMAT_RGBA8)
	for y in size.y:
		for x in size.x:
			var p := Vector2i(x, y)
			if core.has(p):
				letters.set_pixel(x, y, Color.WHITE)
				continue
			if not (core.has(p + Vector2i.LEFT) or core.has(p + Vector2i.RIGHT) or core.has(p + Vector2i.UP) or core.has(p + Vector2i.DOWN)):
				continue
			var coverage := 1.0
			for banner in banners:
				var c := banner.get_pixel(x, y)
				coverage = minf(coverage, minf(c.r, minf(c.g, c.b)))
			letters.set_pixel(x, y, Color(1, 1, 1, coverage))
	return letters


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


## Clownpiece's faces, from LoLK's st05enm.anm in th15.dat, laid out like PoFV's (see
## FACES). LoLK draws a 610x1000 body with a blank face (face05bs) and pastes a 180x160
## expression on it at (205, 404); its lost pose (face05lo) comes whole. Each is cropped to
## PoFV's framing (hat to chest, her head about Cirno's size, checked side by side) and
## scaled into the 256x320 cell.
## LoLK has six of PoFV's nine faces; the other three take the nearest.
const LOLK_FACE_FILES: Array[String] = ["no", "n2", "hp", "n2", "dp", "dp", "pr", "pr", "lo"]
const LOLK_FACE_AT := Vector2i(205, 404)
const LOLK_BUST_CROP := Rect2i(75, 215, 420, 525)

static func build_lolk_faces(st05enm: ThAnm) -> Image:
	var body := st05enm.sheet_image_by_file("face05bs.png") if st05enm else null
	if body == null:
		push_warning("CharacterSprites: th15.dat has no face05bs.png")
		return null
	var faces := Image.create(FACE_SIZE.x * FACES.size(), FACE_SIZE.y, false, Image.FORMAT_RGBA8)
	for i in LOLK_FACE_FILES.size():
		var file := "face05%s.png" % LOLK_FACE_FILES[i]
		var part := st05enm.sheet_image_by_file(file)
		if part == null:
			push_warning("CharacterSprites: th15.dat has no %s" % file)
			return null
		var whole: Image = part
		if part.get_size() != body.get_size():
			whole = body.duplicate()
			whole.convert(Image.FORMAT_RGBA8)
			part.convert(Image.FORMAT_RGBA8)
			whole.blend_rect(part, Rect2i(Vector2i.ZERO, part.get_size()), LOLK_FACE_AT)
		var bust := whole.get_region(LOLK_BUST_CROP)
		bust.resize(FACE_SIZE.x, FACE_SIZE.y, Image.INTERPOLATE_LANCZOS)
		bust.convert(Image.FORMAT_RGBA8)
		faces.blit_rect(bust, Rect2i(Vector2i.ZERO, FACE_SIZE), Vector2i(i * FACE_SIZE.x, 0))
	return faces


static func apply_faces(character: String, faces: Image) -> CharacterData:
	if faces == null:
		return null
	var data := load(CHARACTERS % character) as CharacterData
	data.face_sheet = ImageTexture.create_from_image(faces)
	return data
