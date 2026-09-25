class_name DatTextures
extends RefCounted
## Single sprites from the player's .dat files that scenes use as ordinary textures: charge
## and extra attack art, shot hit effects and the like.
##
## Each has an empty ImageTexture at res://resources/dat_textures/<name>.tres for scenes and
## scripts to reference; at startup the sprite, cut from the .dat through the graphics cache,
## is put into it. Being a whole texture (not a region of one), it repeats and scrolls like
## a normal image, which the laser rune strip and shockwave ribbons need. Adding one: an
## empty .tres there, and a row here.

## Bump when changing a recipe, so saved copies aren't reused.
const VERSION: int = 4

const TEXTURES: String = "res://resources/dat_textures/%s.tres"

## name -> [game, .anm file, sprite id (or ids, laid out left to right as frames), quarter
## turns clockwise, optional settings]. The turn matches how the scene expects the art to
## face (sprites the anm draws sideways are stood upright). Settings, applied in order:
## "region": Rect2i, cut this part of a whole image (see stages below);
## "stack_flipped": true, put a vertically flipped copy under the result (seamless tiling);
## "canvas": Vector2i, centre the result on a transparent canvas of that size;
## "size": Vector2i, stretch the result to that size (smoothly);
## "mipmaps": true, give the texture mipmaps (a 3D stage texture seen at a distance).
## Canvas and size keep a scene's on-screen sizes as they were tuned against the old art.
const RECIPES: Dictionary = {
	# Cirno: Freeze blade (charge attack) and the three frames of a shot crumbling on impact.
	"cirno_freeze_blade": ["th09", "pl05.anm", 32, 0],
	"cirno_shot_crumble_1": ["th09", "pl05.anm", 29, 3],
	"cirno_shot_crumble_2": ["th09", "pl05.anm", 30, 3],
	"cirno_shot_crumble_3": ["th09", "pl05.anm", 31, 3],
	# Cirno: Falling Stalactite (extra attack), her freeze blade stood upright.
	"cirno_stalactite": ["th09", "pl05.anm", 32, 1],
	# Reimu: charge attack amulet (lit, spent, burnt out), the flake a pass-through shot
	# leaves, and the yin-yang orb of the extra attack.
	"reimu_charge_amulet": ["th09", "pl00.anm", 28, 0],
	"reimu_charge_amulet_spent": ["th09", "pl00.anm", 29, 0],
	"reimu_charge_amulet_spent_dark": ["th09", "pl00.anm", 30, 0],
	"reimu_shot_penetrate": ["th09", "pl00.anm", 26, 0],
	"yin_yang_orb": ["th09", "pl00.anm", 36, 0],
	# Marisa: the three shards a shot bursts into, and the charge laser segment.
	"marisa_shot_shard_1": ["th09", "pl01.anm", 25, 0, {"canvas": Vector2i(32, 32)}],
	"marisa_shot_shard_2": ["th09", "pl01.anm", 26, 0, {"canvas": Vector2i(32, 32)}],
	"marisa_shot_shard_3": ["th09", "pl01.anm", 27, 0, {"canvas": Vector2i(32, 32)}],
	"marisa_charge_laser": ["th09", "pl01.anm", 28, 0, {"canvas": Vector2i(16, 16)}],
	# Sakuya: extra attack knives, and the clock face behind her Level 4 card.
	"sakuya_knife_silver": ["th09", "pl02.anm", 32, 0],
	"sakuya_knife_blue": ["th09", "pl02.anm", 35, 0],
	"sakuya_clock": ["th09", "pl02.anm", 75, 0],
	# Youmu: the four frames of her dark spirit and of her slash wave (arcs bowing upward).
	"youmu_dark_spirit": ["th09", "pl03.anm", [32, 33, 34, 35], 0],
	"youmu_slash": ["th09", "pl03.anm", [28, 29, 30, 31], 3, {"size": Vector2i(320, 48)}],
	# Reisen: charge attack bullet, her shot's bigger cartridge.
	"reisen_charge_shot": ["th09", "pl04.anm", 28, 3, {"size": Vector2i(34, 104)}],
	# Yuuka: charge attack petal and extra attack flower.
	"yuuka_charge_petal": ["th09", "pl09.anm", 28, 0],
	"yuuka_extra_flower": ["th09", "pl09.anm", 34, 0],
	# Aya: charge attack crescent (sprite 28) and the three frames it fades through on a hit
	# (29-31, pl10.anm script 8), stood upright.
	"aya_charge_shot": ["th09", "pl10.anm", [28, 29, 30, 31], 3],
	# Shared effects from the bullet sheets: the EX mote (etama2's spiked burst), the flare
	# and rune strip of the earth light ray, and the spellcard shockwave's ribbon (the top
	# 128px of an etama3 strip the game scrolls).
	"ex_mote": ["th09", "etama.anm", 195, 0, {"canvas": Vector2i(68, 68)}],
	"laser_flare": ["th09", "etama.anm", 146, 0, {"canvas": Vector2i(32, 32)}],
	"laser_rune_strip": ["th09", "etama.anm", 227, 0],
	"spellcard_shockwave_ribbon": ["th09", "etama.anm", 224, 0],
	# Enemies (enemy.png): each is a 4x3 block of frames, the rows being enemy.anm's flying
	# loop, turn start and turn loop. Fairies blue, red, green (sprites 0-35), the sunflower
	# great fairy (36-47) and Lily White (84-95).
	"fairy_blue": ["th09", "enemy.anm", "enemy.png", 0, {"region": Rect2i(0, 0, 128, 96)}],
	"fairy_red": ["th09", "enemy.anm", "enemy.png", 0, {"region": Rect2i(0, 96, 192, 96)}],
	"fairy_green": ["th09", "enemy.anm", "enemy.png", 0, {"region": Rect2i(0, 192, 192, 144)}],
	"fairy_great": ["th09", "enemy.anm", "enemy.png", 0, {"region": Rect2i(192, 0, 256, 192)}],
	"lily_white": ["th09", "enemy.anm", "enemy.png", 0, {"region": Rect2i(192, 320, 256, 192)}],
	# Spirits: the 4-frame arc a spirit flies as (drawn facing right, turned to its heading)
	# and the 8-frame ring it becomes once activated. Teal (32px, sprites 48-59) and red
	# (48px, 72-83).
	"spirit_teal": ["th09", "enemy.anm", [48, 49, 50, 51], 0],
	"spirit_teal_ring": ["th09", "enemy.anm", [52, 53, 54, 55, 56, 57, 58, 59], 0],
	"spirit_red": ["th09", "enemy.anm", [72, 73, 74, 75], 0],
	"spirit_red_ring": ["th09", "enemy.anm", [76, 77, 78, 79, 80, 81, 82, 83], 0],
	# Clownpiece: the moon of her Fake Apollo (LoLK st05enm.anm sprite 39, enm5b.png).
	"clownpiece_moon": ["th15", "st05enm.anm", 39, 0],
	# Clownpiece's stage, the Sea of Tranquility (LoLK st05wl.anm): the cratered ground, and the
	# two sky sprites (the Earth, and the glow drawn over it).
	"stage_sea_ground": ["th15", "st05wl.anm", "stage05a.png", 0, {"mipmaps": true}],
	"stage_sea_sky": ["th15", "st05wl.anm", 1, 0],
	"stage_sea_sky_glow": ["th15", "st05wl.anm", 2, 0],
	# HUD: the spinning sakura that marks a round won (front.anm scripts 47-51).
	"round_win_sakura": ["th09", "front.anm", 39, 0],
	# Stage textures, from the worldNN.anm stage sheets. A file name in place of a sprite id
	# means the whole image, or the "region" of it given in the settings.
	# Bamboo Road (world01) floor.
	"stage_bamboo_floor": ["th09", "world01.anm", 0, 0],
	# Its bamboo: the stalk and two leaf clusters, as PoFV draws them.
	"stage_bamboo_stalk": ["th09", "world01.anm", 1, 0],
	"stage_bamboo_leaves_a": ["th09", "world01.anm", 2, 0],
	"stage_bamboo_leaves_b": ["th09", "world01.anm", 3, 0],
	# Eientei corridor (world04): the paper lamp.
	"stage_corridor_lamp": ["th09", "world04.anm", 2, 0, {"mipmaps": true}],
	# The wall is stg5bg.png's top-left quadrant turned so the doors stand upright. The floor art
	# doesn't tile on its own, so rows 8-248 of the top-right quadrant (dropping its dark
	# bands) are stacked over a flipped copy: seamless by construction. Both need mipmaps for
	# the anisotropic filtering the corridor's grazing angles rely on.
	"stage_corridor_wall": ["th09", "world04.anm", "stg5bg.png", 3, {"region": Rect2i(0, 0, 256, 256), "mipmaps": true}],
	"stage_corridor_floor": ["th09", "world04.anm", "stg5bg.png", 0, {"region": Rect2i(256, 8, 256, 240), "stack_flipped": true, "mipmaps": true}],
	# Flowering Night (world00.png): three of its four quadrants are the field's layers.
	"stage_flower_field_base": ["th09", "world00.anm", "world00.png", 0, {"region": Rect2i(0, 0, 256, 256)}],
	"stage_flower_field_mid": ["th09", "world00.anm", "world00.png", 0, {"region": Rect2i(256, 256, 256, 256)}],
	"stage_flower_field_top": ["th09", "world00.anm", "world00.png", 0, {"region": Rect2i(0, 256, 256, 256)}],
	# Garden of the Sun (world09): ground and sunflowers.
	"stage_sun_garden_ground": ["th09", "world09.anm", "world09.png", 0],
	"stage_sun_garden_sunflowers": ["th09", "world09.anm", "world09b.png", 0],
	# Hakugyokurou stairs (world03): ground and left balustrade.
	"stage_stair_ground": ["th09", "world03.anm", 8, 0],
	"stage_stair_balustrade": ["th09", "world03.anm", 12, 0],
	"stage_stair_balustrade_right": ["th09", "world03.anm", "stg5bg.png", 0, {"region": Rect2i(223, 257, 32, 254)}],
	# The stair risers: the stair relief as PoFV cuts it, eight 254x32 bands stacked (world03.anm
	# sprites 0-7, which scripts 0-7 step through); each step shows the next band, so the steps
	# assemble the carving. And the two drifting clouds.
	"stage_stair_riser": ["th09", "world03.anm", "stg5bg.png", 0, {"region": Rect2i(1, 256, 254, 256)}],
	"stage_cloud_a": ["th09", "world03.anm", 9, 0],
	"stage_cloud_b": ["th09", "world03.anm", 10, 0],
	# Misty Lake (world05): the water.
	"stage_misty_lake_water": ["th09", "world05.anm", "world05.png", 0],
}

# Keeps the filled textures alive, so the resource cache hands out these copies rather
# than reloading empty ones.
static var _filled: Array[ImageTexture] = []


## One recipe's image, or null if a sprite isn't in the .anm.
static func build(texture_name: String, anm: ThAnm) -> Image:
	var recipe: Array = RECIPES[texture_name]
	var settings: Dictionary = recipe[4] if recipe.size() > 4 else {}
	var frames: Array[Image] = []
	if recipe[2] is String:
		var whole := anm.sheet_image_by_file(recipe[2]) if anm else null
		if whole == null:
			push_warning("DatTextures: %s has no %s" % [recipe[1], recipe[2]])
			return null
		frames.append(whole.get_region(settings.region) if settings.has("region") else whole)
	else:
		for id in (recipe[2] if recipe[2] is Array else [recipe[2]]):
			if anm == null or not anm.has_sprite(id):
				push_warning("DatTextures: %s has no sprite %d" % [recipe[1], id])
				return null
			frames.append(anm.sprite_image(id))
	for frame in frames:
		for i in recipe[3]:
			frame.rotate_90(CLOCKWISE)
	var image := frames[0]
	if frames.size() > 1:
		var cell := frames[0].get_size()
		image = Image.create(cell.x * frames.size(), cell.y, false, Image.FORMAT_RGBA8)
		for i in frames.size():
			image.blit_rect(frames[i], Rect2i(Vector2i.ZERO, cell), Vector2i(i * cell.x, 0))
	if settings.get("stack_flipped", false):
		var flipped: Image = image.duplicate()
		flipped.flip_y()
		var stacked := Image.create(image.get_width(), image.get_height() * 2, false, Image.FORMAT_RGBA8)
		stacked.blit_rect(image, Rect2i(Vector2i.ZERO, image.get_size()), Vector2i.ZERO)
		stacked.blit_rect(flipped, Rect2i(Vector2i.ZERO, flipped.get_size()), Vector2i(0, image.get_height()))
		image = stacked
	if settings.has("canvas"):
		var canvas: Vector2i = settings.canvas
		var padded := Image.create(canvas.x, canvas.y, false, Image.FORMAT_RGBA8)
		padded.blit_rect(image, Rect2i(Vector2i.ZERO, image.get_size()), (canvas - image.get_size()) / 2)
		image = padded
	if settings.has("size"):
		image.resize(settings.size.x, settings.size.y, Image.INTERPOLATE_BILINEAR)
	return image


static func apply(texture_name: String, image: Image) -> void:
	if image == null:
		return
	var recipe: Array = RECIPES[texture_name]
	if recipe.size() > 4 and recipe[4].get("mipmaps", false):
		# Mipmaps aren't kept in the graphics cache's PNGs, so they're made here each time.
		image.generate_mipmaps()
	var texture := load(TEXTURES % texture_name) as ImageTexture
	# Filled in place, so scenes that already loaded the empty texture show the sprite too.
	texture.set_image(image)
	if texture not in _filled:
		_filled.append(texture)
