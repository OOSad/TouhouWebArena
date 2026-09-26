class_name CharacterData
extends Resource

## Modular Character Definition for Touhou Web Arena.
## Encapsulates all identity, movement, danmaku shot parameters, scope styles,
## and extra attacks into an extensible data resource (equivalent to Unity ScriptableObject).

enum ShotPattern { TWIN, THREE_WAY_FAN }
enum ShotHitEffect { NONE, SHARD, CRUMBLE }

enum ScopeShape {
	CIRCLE,
	COLUMN,
	FAN_DOWN,
	LENS,
	SATELLITE_CIRCLES,
	STAR,
	FAN_UP
}

@export_group("Identity")
@export var character_id: String = "reimu"
@export var display_name: String = "Reimu Hakurei"
@export var title: String = "Shrine Maiden of Paradise"
@export var sprite_sheet: Texture2D
@export var primary_color: Color = Color(1.0, 0.35, 0.45, 1.0)
@export var secondary_color: Color = Color(1.0, 0.85, 0.90, 1.0)
@export var max_health: float = 5.0

@export_group("Movement")
@export var normal_speed: float = 540.0
@export var focus_speed: float = 270.0
## Lethal hurtbox radius in pixels (hit detection with bullets). Reimu has a smaller hitbox advantage (~2.4px).
@export var hurtbox_radius: float = 3.0
## Graze field radius in pixels (distance from player center where enemy bullets are grazed).
@export var graze_radius: float = 64.0

@export_group("Danmaku & Shooting")
@export var bullet_damage: float = 1.0
@export var bullet_speed: float = 1400.0
@export var bullet_texture: Texture2D
@export var bullet_scale: Vector2 = Vector2(1.5, 1.5)
@export var bullet_texture_filter: CanvasItem.TextureFilter = CanvasItem.TEXTURE_FILTER_NEAREST
@export var bullet_capsule_height: float = 60.0
@export var shot_cooldown: float = 0.075
## Above 0, the shot hitbox is a circle of this radius (round shots) instead of a capsule.
@export var bullet_round_radius: float = 0.0
## How fast the shot sprite spins in flight, radians per second (Yuuka's blossom, Clownpiece's star).
@export var bullet_spin: float = 0.0
## TWIN: two parallel streams. THREE_WAY_FAN: Yuuka's fan of three (pl09.sht).
@export var shot_pattern: ShotPattern = ShotPattern.TWIN
## Each volley also fires one shot from her centre straight down, behind her (Lyrica, pl06.sht).
@export var rear_shot: bool = false
## What a shot leaves behind when it hits: SHARD flies on through the enemy (Reimu, Marisa),
## CRUMBLE breaks apart where it struck (Cirno). One of shot_hit_textures is picked per hit.
@export var shot_hit_effect: ShotHitEffect = ShotHitEffect.NONE
@export var shot_hit_textures: Array[Texture2D] = []

@export_group("Scope & Abilities")
@export var scope_shape: ScopeShape = ScopeShape.CIRCLE
@export var scope_radius: float = 130.0
@export var scope_expand_duration: float = 0.28
@export var scope_collapse_duration: float = 0.14
@export var extra_attack_scene: PackedScene
## Target Y range where Extra Attacks are delivered on the opponent's field.
## E.g. (60, 130) for top descending attacks (Yin-Yang Orb), (920, 945) for bottom erupting attacks (Earth Light Ray).
@export var extra_attack_y_range: Vector2 = Vector2(60.0, 130.0)
## Target X range where Extra Attacks are delivered on the opponent's field.
## E.g. (35, 565) for full width, or (80, 520) for flat player movement area.
@export var extra_attack_x_range: Vector2 = Vector2(35.0, 565.0)
## Played as each Extra Attack light mote sets off towards the opponent, for characters
## whose attack announces itself on launch rather than when it lands (Yuuka).
@export var extra_attack_launch_sfx: String = ""
## Fewest seconds between two of this character's Extra Attacks.
@export var extra_attack_min_interval: float = 1.2
## Every chained fairy train sends an Extra Attack, instead of 3, then 2, then 1 as the match
## goes on (Cirno).
@export var extra_attack_every_train: bool = false

@export_group("Spell Bar & Charge")
## Number of segments on the spell bar. Default is 4 (PoFV standard: Lv1 Charge, Lv2, Lv3, Lv4 Spellcards).
## Characters can have custom counts (e.g. 1 for a single massive spellcard, or 0 for nimble characters with no bar).
@export var spell_bar_segments: int = 4
## Initial passive segments filled at round start. Standard PoFV begins with 1 segment (25% / Lv 1 Charge ready).
@export var passive_charge_start: float = 1.0
## Passive charge gained per small fairy popped (in segment units, 1.0 = 25% / 100 units of 400).
## Calibrated so ~10 fairy trains fill approx 25% (1.0 segment) of the bar.
@export var passive_charge_per_fairy: float = 0.010
## Passive charge gained per great fairy popped.
@export var passive_charge_per_great_fairy: float = 0.025
## Passive charge gained per spirit popped.
@export var passive_charge_per_spirit: float = 0.040
## Passive charge gained per bullet canceled in shockwaves.
@export var passive_charge_per_cancel: float = 0.001
## Passive charge gained per grazed bullet (in segment units, e.g. 0.015 = 1.5% of a segment).
@export var passive_charge_per_graze: float = 0.015
## Speed of active charging in segments per second.
## Calculated from authentic PoFV charge coefficient C via: active_charge_speed = 0.6 * C.
## Default is 2.4 (Reimu, C = 4.0). Sakuya = 2.16 (C = 3.6), Marisa = 1.98 (C = 3.3), Cirno = 1.74 (C = 2.9), Youmu = 1.32 (C = 2.2).
@export var active_charge_speed: float = 2.4
## Display name of the character's Lv 1 Charge Attack.
@export var charge_attack_name: String = "Charge Attack"
## Scene instantiated when executing the character's Lv 1 Charge Attack.
@export var charge_attack_scene: PackedScene = null
## Color of the passive bar fill for this character (defaults to deep crimson).
@export var spell_bar_color: Color = Color(0.72, 0.22, 0.28, 1.0)

@export_group("Spellcards")
## Authentic Spell Attack Notification banner texture (288x85)
@export var spell_banner_texture: Texture2D = null
## Level 2 Spellcard data resource
@export var spellcard_lv2: SpellcardData = null
## Level 3 Spellcard data resource
@export var spellcard_lv3: SpellcardData = null
## Level 4 Boss Spellcard data resource
@export var spellcard_lv4: SpellcardData = null
## Static base texture for Level 4 Spellcard background overlay (cdbg*.png)
@export var spell_bg_base_texture: Texture2D = null
## Animated top layer texture for Level 4 Spellcard background overlay (cdbg*b.png)
@export var spell_bg_anim_texture: Texture2D = null
## Animation mode for top layer: "rotate" (e.g. Reimu Yin-Yang), "scroll_up" (e.g. Marisa hex cells),
## "counter_scroll" (e.g. Cirno dual ice scroll), or "dual_rotate" (two independently counter-rotating
## full-screen layers - base texture below spinning one way, anim texture above spinning the other, e.g. Sakuya clock/distortion),
## "aligned_rotate" (Reisen) or "scroll_rotate" (base tiled and scrolling up under an additive anim layer turning on the centre, Yuuka)
@export var spell_bg_anim_type: String = "rotate"

@export_group("Spellcard Shockwaves")
## Whether this character emits defensive heavy shockwaves on spellcard activation
@export var has_spellcard_shockwaves: bool = true
## Radius of the shockwave for Level 2 Spellcard (encompasses ~50% of the screen)
@export var shockwave_radius_lv2: float = 420.0
## Radius of the shockwave for Level 3 Spellcard (encompasses ~75% of the screen)
@export var shockwave_radius_lv3: float = 650.0
## Radius of the shockwave for Level 4 Spellcard (encompasses 100% of the screen - full screen reset)
@export var shockwave_radius_lv4: float = 1200.0
## Travelling expansion speed in px/s (calibrated uniform speed: 400 px/s)
@export var shockwave_speed: float = 400.0
## Duration of the shockwave expansion in seconds (base reference / fallback)
@export var shockwave_duration: float = 1.02
## Tint color for the shockwave ring (if Color(0,0,0,0), uses character primary_color)
@export var shockwave_color: Color = Color(0.35, 0.75, 1.0, 1.0)

@export_group("Boss Character")
## Level 4 Boss character configuration
@export var boss_data: BossData = null

@export_group("Card Presentation")
## Version or era of this character iteration (e.g. "TH09", "TH19", "Classic")
@export var version_tag: String = "TH09"
## Broad combat role or archetype (e.g. "All-Around / Boundary Control", "High-Speed / Linear Heavy Piercing")
@export var archetype: String = ""
## Display name of primary standard shot
@export var primary_shot_name: String = ""
## Concise description of primary standard shot behavior
@export var primary_shot_desc: String = ""
## Unique character trait or passive perk
@export var special_trait: String = ""
## Name of Extra harassment attack delivered to opponent field
@export var extra_attack_name: String = ""
## Concise tactical description of Extra harassment attack
@export var extra_attack_desc: String = ""
## Concise tactical description of Lv 1 Charge Attack
@export var charge_attack_desc: String = ""
## Name of signature Lv 4 Spellcard
@export var signature_spell_name: String = ""
## Character portrait or silhouette texture for character select card
@export var portrait_texture: Texture2D = null
## PoFV's nine result-screen faces side by side (2304x320), filled in from th09.dat
@export var face_sheet: Texture2D = null

@export_group("Home Stage & Music")
## This character's home stage: the folder name under res://scenes/stages/ (e.g. "bamboo_road").
## Empty plays the Bamboo Road.
@export var home_stage_id: String = ""
## Music to play on this character's stage, e.g. "res://assets/music/10_ancient_temple.ogg". The file
## name is a DatMusic track id: the music itself comes from the player's thbgm.dat.
@export var stage_bgm_path: String = ""

## Returns a formatted human-readable scope shape and dimension description
func get_scope_description() -> String:
	match scope_shape:
		ScopeShape.CIRCLE:
			return "Centric Circle (r = %dpx)" % int(scope_radius)
		ScopeShape.COLUMN:
			return "Vertical Column (w = %dpx)" % int(scope_radius)
		ScopeShape.FAN_DOWN:
			return "Downward Fan (r = %dpx, 90°)" % int(scope_radius)
		ScopeShape.LENS:
			return "Lunatic Eye (%dpx across, turns with movement)" % int(scope_radius * 2.0)
		ScopeShape.SATELLITE_CIRCLES:
			return "Flower Blossom (Centric Circle + 6 Orbiting Satellites, r = %dpx)" % int(scope_radius * 1.92)
		ScopeShape.FAN_UP:
			return "Upward Cone (r = %dpx, 100°)" % int(scope_radius)
		ScopeShape.STAR:
			return "Five-Pointed Star (points reach %dpx)" % int(scope_radius)
		_:
			return "Standard Field"

## Returns the shockwave radius for a given spellcard level (0.0 if disabled or invalid level)
func get_shockwave_radius(level: int) -> float:
	if not has_spellcard_shockwaves:
		return 0.0
	match level:
		2:
			return shockwave_radius_lv2
		3:
			return shockwave_radius_lv3
		4:
			return shockwave_radius_lv4
		_:
			return 0.0

## Returns the calculated duration ensuring uniform travelling speed across all levels
func get_shockwave_duration(level: int) -> float:
	var r := get_shockwave_radius(level)
	if r <= 0.0:
		return 0.0
	if shockwave_speed > 0.0:
		return maxf(0.1, (r - 12.0) / shockwave_speed)
	return shockwave_duration

## Returns the configured shockwave glow color or falls back to primary_color
func get_shockwave_color() -> Color:
	if shockwave_color != Color.TRANSPARENT and shockwave_color.a > 0.0:
		return shockwave_color
	return primary_color

## This character's 3D home stage.
func get_home_stage_scene() -> PackedScene:
	return get_stage_scene_by_id(get_home_stage_id())

## Returns the unique home stage identifier string
func get_home_stage_id() -> String:
	if not home_stage_id.is_empty():
		return home_stage_id
	return DEFAULT_STAGE_ID

## The character's stage music: their own track if they have one, else their home stage's.
func get_stage_bgm_path() -> String:
	if not stage_bgm_path.is_empty():
		return stage_bgm_path
	return get_stage_bgm_by_id(get_home_stage_id())


# Static Registry & Resource Cache

## The playable roster, in character-select order. A new character is their id here plus
## res://resources/characters/<id>.tres; every roster list in the game is read from this one.
const ROSTER: Array[String] = [
	"reimu", "marisa", "sakuya", "youmu", "cirno", "reisen", "yuuka", "aya", "clownpiece",
	"lyrica",
]
## The select screen's Random slot. It has a .tres for its card, but is not in the roster.
const RANDOM_ID: String = "random"
## Other spellings a character's name can arrive in besides their id and display name.
const ID_ALIASES: Dictionary = {"udonge": "reisen", "yuka": "yuuka", "shameimaru": "aya"}

static var _registry: Dictionary = {}

static func path_for(id: String) -> String:
	return "res://resources/characters/%s.tres" % id

## A character id from any form of their name ("Reimu Hakurei", "reimu", "Udonge"), or ""
## when the name is no one's.
static func normalize_id(char_name: String) -> String:
	var key := char_name.to_lower().strip_edges()
	if key == RANDOM_ID or key in ROSTER:
		return key
	for id in ROSTER + [RANDOM_ID]:
		if id in key:
			return id
	for alias in ID_ALIASES:
		if alias in key:
			return ID_ALIASES[alias]
	return ""

static func get_character(id: String) -> CharacterData:
	if _registry.is_empty():
		_init_registry()
	var key := normalize_id(id)
	if key in _registry:
		return _registry[key]
	push_warning("CharacterData: no character named '%s', using Reimu" % id)
	return _registry.get("reimu", null)

static func get_all_characters() -> Array[CharacterData]:
	if _registry.is_empty():
		_init_registry()
	var list: Array[CharacterData] = []
	for key in ROSTER:
		if key in _registry:
			list.append(_registry[key])
	return list

## The roster's display names, in select-screen order (the names selections travel as).
static func get_roster_names() -> Array[String]:
	var names: Array[String] = []
	for data in get_all_characters():
		names.append(data.display_name)
	return names

static func _init_registry() -> void:
	for key in ROSTER + [RANDOM_ID]:
		var path := path_for(key)
		if ResourceLoader.exists(path):
			var res = load(path)
			if res is CharacterData:
				_registry[key] = res
			else:
				push_error("CharacterData: %s is not a CharacterData" % path)
		else:
			push_error("CharacterData: %s is in the roster but has no %s" % [key, path])

## The stage whose folder is res://scenes/stages/<id>/, or the Bamboo Road when there is none.
static func get_stage_scene_by_id(stage_id: String) -> PackedScene:
	for id in [stage_id, DEFAULT_STAGE_ID]:
		var path := STAGE_SCENE % [id, id]
		if not id.is_empty() and ResourceLoader.exists(path):
			return load(path)
	return null

const DEFAULT_STAGE_ID: String = "bamboo_road"
const STAGE_SCENE: String = "res://scenes/stages/%s/%s_3d.tscn"

## Stage id -> its music (a DatMusic track id). Stages not listed play Spring Lane.
const STAGE_MUSIC: Dictionary = {
	"hakugyokurou_stairs": "10_ancient_temple",
	"misty_lake": "07_adventure_of_the_lovestruck_tomboy",
	"eientei_corridor": "06_lunatic_eyes_invisible_full_moon",
	"garden_of_the_sun": "13_gensokyo_past_and_present",
	"flowering_night": "04_flowering_night",
	"sea_of_tranquility": "15_pierrot_of_the_star_spangled_banner",
	"mountain_pond": "11_wind_god_girl",
}
const DEFAULT_STAGE_MUSIC: String = "02_spring_lane"


## A stage's music, as the path music is asked for by (its file name is the track id).
static func get_stage_bgm_by_id(stage_id: String) -> String:
	return "res://assets/music/%s.ogg" % STAGE_MUSIC.get(stage_id, DEFAULT_STAGE_MUSIC)
