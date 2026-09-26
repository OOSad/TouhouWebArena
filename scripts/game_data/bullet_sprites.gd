class_name BulletSprites
extends RefCounted
## Gives every bullet type its sprite from the player's own .dat files: PoFV's etama.anm in
## th09.dat, and LoLK's bullet.anm in th15.dat for Clownpiece. The game ships no bullet art:
## the resources in res://resources/bullets hold only gameplay values, and their textures
## are filled in here at startup.
##
## Sprites are packed into one texture built at runtime, so bullets still batch into a
## single draw call. The resources draw them at 2.0833 (our field's size relative to
## PoFV's), and their hitboxes were sized against these sprites (depot README 3.5).
##
## Sprite ids come from the depot's anm/scripts/etama.anm.txt. Rows with 16 colours run
## Black, DarkRed, Red, DarkPurple, Purple, DarkBlue, Blue, DarkCyan, Cyan, DarkGreen,
## Green, YellowGreen, DarkYellow, Yellow, Orange, White. Rows with 8 take the same colour
## number, so they read Black, Red, Purple, Blue, Cyan, Green, Yellow, White; that's why
## ECL "Blue" swords and big balls show up yellow.

const PADDING: int = 2
const SHEET_WIDTH: int = 256

const BULLETS: String = "res://resources/bullets/%s.tres"

# Row starts in etama.anm.
const PELLET: int = 0       # Pellet, etama y240/y248, 16 colours
const RING: int = 16        # RingBall, etama y32, 16 colours
const RICE: int = 32        # Rice, etama y64, 16 colours
const SHARD: int = 80       # Shard, etama y96, 16 colours
const ARROWHEAD: int = 96   # Arrowhead, etama y16, 16 colours
const BIG_BALL: int = 112   # BigBall, etama y112, 8 colours
const KNIFE: int = 128      # type 20, etama y144, 8 colours
const TALISMAN: int = 231   # type 11, etama6 y240, 16 colours
const STAR: int = 247       # type 12, etama6 y32, 16 colours
const CAPSULE: int = 271    # type 16, etama6 y64, 16 colours
const SWORD: int = 335      # Sword, etama6 y128, 8 colours
const BUTTERFLY: int = 120  # Butterfly, etama y176, 8 colours

# Colour numbers as the ECL names them.
const RED: int = 2
const PURPLE: int = 4
const BLUE: int = 6
const CYAN: int = 8
const GREEN: int = 10
const DARK_YELLOW: int = 12
const YELLOW: int = 13
const ORANGE: int = 14
const WHITE: int = 15
# In 8-colour rows.
const RED_8: int = 1
const PURPLE_8: int = 2
const BLUE_8: int = 3
const CYAN_8: int = 4
const GREEN_8: int = 5
const YELLOW_8: int = 6

# bullet resource -> etama sprite id
const SPRITES: Dictionary = {
	"red_pellet": PELLET + RED,
	"white_pellet": PELLET + WHITE,
	"sakuya_dagger_pellet": PELLET + WHITE,
	"green_pellet": PELLET + GREEN,
	"orange_pellet": PELLET + ORANGE,
	"yellow_pellet": PELLET + YELLOW,
	"blue_pellet_small": PELLET + BLUE,
	"red_oval": RICE + RED,
	"white_oval": RICE + WHITE,
	"yellow_rice": RICE + YELLOW,
	"orange_rice": RICE + ORANGE,
	"green_arrow": ARROWHEAD + GREEN,
	"yellow_arrow": ARROWHEAD + YELLOW,
	"dark_yellow_arrow": ARROWHEAD + DARK_YELLOW,
	"red_arrow": ARROWHEAD + RED,
	"blue_arrow": ARROWHEAD + BLUE,
	# ECL DarkRed (1) on an 8-colour row
	"red_butterfly": BUTTERFLY + RED_8,
	# ECL Red (2) on an 8-colour row
	"purple_butterfly": BUTTERFLY + PURPLE_8,
	"blue_icicle": SHARD + BLUE,
	"cyan_icicle": SHARD + CYAN,
	"red_talisman": TALISMAN + RED,
	"white_talisman": TALISMAN + WHITE,
	"blue_star": STAR + BLUE,
	"green_star": STAR + GREEN,
	"yellow_star": STAR + YELLOW,
	"red_ring_ball": RING + RED,
	"yellow_ring_ball": RING + YELLOW,
	"yellow_big_ball": BIG_BALL + YELLOW_8,
	"green_knife": SWORD + GREEN_8,
	"blue_knife": SWORD + BLUE_8,
	"yellow_knife": SWORD + YELLOW_8,
	"sakuya_knife": KNIFE + RED_8,
	"sakuya_knife_blue": KNIFE + BLUE_8,
	"sakuya_knife_cyan": KNIFE + CYAN_8,
	"sakuya_knife_purple": KNIFE + PURPLE_8,
	"sakuya_knife_yellow": KNIFE + YELLOW_8,
	"reisen_wave_bullet": CAPSULE + RED,
	"reisen_spiral_bullet": CAPSULE + PURPLE,
}

# Cirno's Perfect Freeze turns bullets white by swapping in these.
const FROZEN_SPRITES: Dictionary = {
	"blue_icicle": SHARD + WHITE,
	"blue_pellet_small": PELLET + WHITE,
}

# EnemyPellet (fairy counter-shots, Lily White) picks its sprites in code, not from a
# bullet resource: small pellets are Pellet, big ones Ball (etama y48), and the ring is
# the white RingBall tinted with the player's colour.
const BALL: int = 48
const ENEMY_PELLET_SPRITES: Dictionary = {
	"pellet_blue_tex": PELLET + BLUE,
	"pellet_red_tex": PELLET + RED,
	"pellet_white_tex": PELLET + WHITE,
	"big_blue_tex": BALL + BLUE,
	"big_red_tex": BALL + RED,
	"big_white_tex": BALL + WHITE,
	"ring_tex": RING + WHITE,
}

# Clownpiece's bullets from LoLK's bullet.anm (th15.dat). Each is cut as a whole cell of the
# sheet, with transparent margins, so the resources' scales keep sizing it as before.
# Flame: bullet3.png sprites 484-487, four 32x32 frames of a white-cored orb trailing
# flame (bullet.anm script 108). The anm draws it 4px above the bullet, so 8px of empty
# space below each frame puts the orb, not the middle of the cell, on the bullet.
# Star: bullet2.png (96, 0), the big blue five-pointed star. Red: (32, 0), the pink-red one
# Starry Illusion fires in LoLK.
# Glow ball: bullet1.png sprite 67, the violet one of the 16px white-cored glow balls (type
# 26), picked by colour against the footage of Fake Apollo.
const TH15_FLAME: Array[Rect2i] = [
	Rect2i(0, 128, 32, 32), Rect2i(32, 128, 32, 32), Rect2i(64, 128, 32, 32), Rect2i(96, 128, 32, 32),
]
const TH15_FLAME_SHEET: String = "bullet/bullet3.png"
const TH15_FLAME_PAD_BELOW: int = 8
const TH15_STAR: Rect2i = Rect2i(96, 0, 32, 32)
const TH15_STAR_RED: Rect2i = Rect2i(32, 0, 32, 32)
const TH15_STAR_SHEET: String = "bullet/bullet2.png"
const TH15_GLOW_PURPLE: Rect2i = Rect2i(48, 48, 16, 16)
const TH15_GLOW_SHEET: String = "bullet/bullet1.png"
# Keys of the images cut_th15() makes; bump TH15_VERSION when changing how they are cut.
const TH15_KEYS: Array[String] = ["th15_flame_0", "th15_flame_1", "th15_flame_2", "th15_flame_3", "th15_star", "th15_star_red", "th15_glow_purple"]
const TH15_VERSION: int = 1

# Bullets drawn with a character's own art, cut from their plNN.anm in th09.dat and packed
# with the rest: resource -> [anm file, sheet, sprite id, white backing]. Lyrica's Extra
# Attack is her red double note (pl06_ex.png sprite 69). pl06.anm script 28 names the single
# note, sprite 65, but the footage shows the double one, beam and two heads, for both the
# forming note and the ring, filled white between its strokes (_on_white_backing).
const PLAYER_ANM_BULLETS: Dictionary = {
	"lyrica_note_red": ["pl06.anm", "data/pl/pl06/pl06_ex.png", 69, true],
	# The same note facing along its path: her Level 4 boss ring (ECL bullet type 19).
	"lyrica_note_red_ring": ["pl06.anm", "data/pl/pl06/pl06_ex.png", 69, true],
}
const PLAYER_ANM_VERSION: int = 4

# Effect textures filled from etama.anm. PoFV's sparkle burst (etama y209, 30x30, the same
# eight colours as the big balls). Blue marks where Cirno's icicles appear; red is the
# spawn flash (Reisen's rings and machinegun) and Sakuya's knife explosion.
const EFFECT_SPRITES: Dictionary = {
	"res://resources/bullets/textures/tex_icicle_spawn_burst.tres": 148,
	"res://resources/bullets/textures/tex_knife_explosion_burst.tres": 147,
	# The fairy shockwave: etama2.png's blue magic circle, which PoFV's own fairy explosions
	# (etama.anm scripts 28-31) grow from the same 0.2x and 0.3x our Shockwave starts at.
	# DefeatShockwave tints it red.
	"res://resources/bullets/textures/tex_shockwave.tres": 192,
}

# Bullet art with no ZUN original (the forming glow on Yuuka's big balls, Reisen's moon
# mote). Copied from danmaku_atlas.png onto the same runtime texture, so they batch with
# the rest instead of splitting the draw.
const LEFTOVER_TEXTURES: Array[String] = [
	"res://resources/bullets/textures/tex_spawn_glow_yellow.tres",
	"res://resources/bullets/textures/tex_reisen_moon_mote.tres",
]

# Keeps the repointed leftover and effect textures alive, so the resource cache hands out
# these copies rather than reloading ones with no sprite or the old atlas.
static var _leftovers: Array[AtlasTexture] = []


## Points every bullet type at its sprite. Returns the bullet resources it
## filled in; the caller keeps them referenced so Godot's resource cache holds on to them
## instead of reloading texture-less copies from disk.
static func apply(etama: ThAnm, extra_images: Dictionary) -> Array[DanmakuBulletData]:
	var ids: Array[int] = []
	for id in SPRITES.values() + FROZEN_SPRITES.values() + ENEMY_PELLET_SPRITES.values():
		if id not in ids:
			ids.append(id)
	var textures := _pack(etama, ids, extra_images)

	var changed: Array[DanmakuBulletData] = []
	for bullet in SPRITES:
		var data := load(BULLETS % bullet) as DanmakuBulletData
		if data == null or not textures.has(SPRITES[bullet]):
			push_warning("BulletSprites: skipped %s" % bullet)
			continue
		data.texture = textures[SPRITES[bullet]]
		if FROZEN_SPRITES.has(bullet):
			data.frozen_texture = textures[FROZEN_SPRITES[bullet]]
		changed.append(data)

	var flame := load(BULLETS % "clownpiece_flame_red") as DanmakuBulletData
	var frames: Array[Texture2D] = []
	for i in TH15_FLAME.size():
		frames.append(textures.get("th15_flame_%d" % i))
	flame.anim_frames = frames
	flame.texture = frames[0]
	var star := load(BULLETS % "clownpiece_big_star_blue") as DanmakuBulletData
	star.texture = textures.get("th15_star")
	var red_star := load(BULLETS % "clownpiece_big_star_red") as DanmakuBulletData
	red_star.texture = textures.get("th15_star_red")
	var glow := load(BULLETS % "clownpiece_glow_ball_purple") as DanmakuBulletData
	glow.texture = textures.get("th15_glow_purple")
	changed.append_array([flame, star, red_star, glow])
	for bullet in PLAYER_ANM_BULLETS:
		var own := load(BULLETS % bullet) as DanmakuBulletData
		if own and textures.has(bullet):
			own.texture = textures[bullet]
			changed.append(own)

	var pellet: Dictionary = {}
	for property in ENEMY_PELLET_SPRITES:
		pellet[property] = textures.get(ENEMY_PELLET_SPRITES[property])
	EnemyPellet.pellet_blue_tex = pellet.pellet_blue_tex
	EnemyPellet.pellet_red_tex = pellet.pellet_red_tex
	EnemyPellet.pellet_white_tex = pellet.pellet_white_tex
	EnemyPellet.big_blue_tex = pellet.big_blue_tex
	EnemyPellet.big_red_tex = pellet.big_red_tex
	EnemyPellet.big_white_tex = pellet.big_white_tex
	EnemyPellet.ring_tex = pellet.ring_tex
	return changed


## Clownpiece's normal shot: LoLK's small violet star (bullet1.png sprite at (48, 160), the
## 16px star row), her colour, at the 0x80 alpha every PoFV shot is drawn at.
const TH15_SHOT_STAR: Rect2i = Rect2i(48, 160, 16, 16)
const TH15_SHOT_SHEET: String = "bullet/bullet1.png"

static func cut_th15_shot(th15_bullets: ThAnm) -> Image:
	var sheet := th15_bullets.sheet_image(TH15_SHOT_SHEET) if th15_bullets else null
	if sheet == null:
		return null
	var shot := sheet.get_region(TH15_SHOT_STAR)
	CharacterSprites.fade_shot(shot, 128.0 / 255.0)
	return shot


## One of PLAYER_ANM_BULLETS, cut from its character's anm.
static func cut_player_anm(anm: ThAnm, bullet: String) -> Image:
	var recipe: Array = PLAYER_ANM_BULLETS[bullet]
	var sheet := anm.sheet_image(recipe[1]) if anm else null
	if sheet == null:
		return null
	var sprite := sheet.get_region(anm.sprite_rect(recipe[2]))
	if recipe.size() > 3 and recipe[3]:
		sprite = _on_white_backing(sprite)
	return sprite


## Pixels: gaps narrower than twice this between a sprite's strokes are filled white.
const WHITE_BACKING_RADIUS: int = 6

## The sprite over a white fill of the gaps between its strokes: its solid pixels closed
## (grown by WHITE_BACKING_RADIUS, then shrunk back), so the fill stays inside its outline.
static func _on_white_backing(sprite: Image) -> Image:
	sprite.convert(Image.FORMAT_RGBA8)
	var w := sprite.get_width()
	var h := sprite.get_height()
	var solid: Array[bool] = []
	for y in h:
		for x in w:
			solid.append(sprite.get_pixel(x, y).a > 0.5)
	var closed := _erode(_dilate(solid, w, h), w, h)
	var out := Image.create(w, h, false, Image.FORMAT_RGBA8)
	for y in h:
		for x in w:
			if closed[y * w + x]:
				out.set_pixel(x, y, Color.WHITE)
	out.blend_rect(sprite, Rect2i(0, 0, w, h), Vector2i.ZERO)
	return out


static func _dilate(mask: Array[bool], w: int, h: int) -> Array[bool]:
	return _morph(mask, w, h, true)


static func _erode(mask: Array[bool], w: int, h: int) -> Array[bool]:
	return _morph(mask, w, h, false)


## Grows (dilate) or shrinks (erode) a mask by a disc of WHITE_BACKING_RADIUS. Outside the
## image counts as empty, so eroding eats in from the edges as dilating spread out.
static func _morph(mask: Array[bool], w: int, h: int, dilate: bool) -> Array[bool]:
	var r := WHITE_BACKING_RADIUS
	var out: Array[bool] = []
	out.resize(w * h)
	for y in h:
		for x in w:
			var hit := not dilate
			for dy in range(-r, r + 1):
				for dx in range(-r, r + 1):
					if dx * dx + dy * dy > r * r:
						continue
					var nx := x + dx
					var ny := y + dy
					var v := nx >= 0 and ny >= 0 and nx < w and ny < h and mask[ny * w + nx]
					if dilate and v:
						hit = true
					elif not dilate and not v:
						hit = false
			out[y * w + x] = hit
	return out


## One of Clownpiece's cells cut from LoLK's bullet sheets, by its TH15_KEYS key.
static func cut_th15(th15_bullets: ThAnm, key: String) -> Image:
	if th15_bullets == null:
		return null
	if key == "th15_star":
		var star_sheet := th15_bullets.sheet_image(TH15_STAR_SHEET)
		return star_sheet.get_region(TH15_STAR) if star_sheet else null
	if key == "th15_star_red":
		var red_sheet := th15_bullets.sheet_image(TH15_STAR_SHEET)
		return red_sheet.get_region(TH15_STAR_RED) if red_sheet else null
	if key == "th15_glow_purple":
		var glow_sheet := th15_bullets.sheet_image(TH15_GLOW_SHEET)
		return glow_sheet.get_region(TH15_GLOW_PURPLE) if glow_sheet else null
	var flame_sheet := th15_bullets.sheet_image(TH15_FLAME_SHEET)
	if flame_sheet == null:
		return null
	var rect := TH15_FLAME[int(key.trim_prefix("th15_flame_"))]
	var cell := Image.create(rect.size.x, rect.size.y + TH15_FLAME_PAD_BELOW, false, Image.FORMAT_RGBA8)
	cell.blit_rect(flame_sheet, rect, Vector2i.ZERO)
	return cell


## Packs the sprites into one texture, in rows, and returns key -> AtlasTexture, keyed by
## etama sprite id or by the key in `extra` (already-cut images). Also moves
## LEFTOVER_TEXTURES onto the same texture.
static func _pack(etama: ThAnm, ids: Array[int], extra: Dictionary) -> Dictionary:
	# key (sprite id, extra key, or a leftover's AtlasTexture) -> Image
	var images: Dictionary = {}
	for key in extra:
		if extra[key] is Image:
			images[key] = extra[key]
	for id in ids:
		var image := etama.sprite_image(id)
		if image:
			images[id] = image
	var old_atlas: Image = null
	for path in LEFTOVER_TEXTURES:
		var leftover := load(path) as AtlasTexture
		if leftover == null or leftover.atlas == null:
			continue
		if old_atlas == null:
			old_atlas = leftover.atlas.get_image()
			old_atlas.convert(Image.FORMAT_RGBA8)
		images[leftover] = old_atlas.get_region(Rect2i(leftover.region))
	for path in EFFECT_SPRITES:
		var effect := load(path) as AtlasTexture
		var effect_image := etama.sprite_image(EFFECT_SPRITES[path])
		if effect and effect_image:
			images[effect] = effect_image

	var cells: Dictionary = {}
	var x := PADDING
	var y := PADDING
	var row_height := 0
	for key in images:
		var size: Vector2i = images[key].get_size()
		if x + size.x + PADDING > SHEET_WIDTH:
			x = PADDING
			y += row_height + PADDING
			row_height = 0
		cells[key] = Rect2i(Vector2i(x, y), size)
		x += size.x + PADDING
		row_height = maxi(row_height, size.y)

	var sheet := Image.create(SHEET_WIDTH, y + row_height + PADDING, false, Image.FORMAT_RGBA8)
	for key in images:
		sheet.blit_rect(images[key], Rect2i(Vector2i.ZERO, images[key].get_size()), cells[key].position)
	var sheet_texture := ImageTexture.create_from_image(sheet)

	var textures: Dictionary = {}
	_leftovers.clear()
	for key in cells:
		if key is AtlasTexture:
			# Repointed in place, so every bullet and scene already using it follows along.
			key.atlas = sheet_texture
			key.region = Rect2(cells[key])
			_leftovers.append(key)
		else:
			var atlas_texture := AtlasTexture.new()
			atlas_texture.atlas = sheet_texture
			atlas_texture.region = Rect2(cells[key])
			textures[key] = atlas_texture
	return textures
