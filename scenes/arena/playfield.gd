class_name Playfield
extends Node2D

const PLAYFIELD_WIDTH: float = 600.0
const PLAYFIELD_HEIGHT: float = 960.0

@export var player_number: int = 1
@export var character_id: String = "reimu"
@export var is_local_player: bool = true
@export var enable_3d_background: bool = true

# Containers for gameplay entities (isolated inside this viewport's World2D)
@onready var entities_layer: Node2D = %Entities
@onready var bullets_layer: Node2D = %Bullets
@onready var effects_layer: Node2D = %Effects
@onready var defeat_flash: ColorRect = %DefeatFlash
@onready var dim_overlay: ColorRect = %DimOverlay
@onready var background_3d_container: SubViewportContainer = get_node_or_null("%Background3DContainer")
@onready var background_fallback: ColorRect = get_node_or_null("%Background")
@onready var spell_bg_overlay: Control = get_node_or_null("%SpellBackgroundOverlay")

var player: Player = null

signal attack_sent(death_pos: Vector2, pellet_count: int, combo_count: int, has_big_pellet: bool, has_spirit: bool, has_extra_attack: bool, bounce_count: int)
signal combo_updated(current_combo: int)
signal player_health_changed(player_num: int, current_hp: float, max_hp: float)
signal player_defeated(player_num: int)
signal player_charge_updated(player_num: int, active: float, passive: float, max_segments: int)
signal player_ranks_updated(player_num: int, rank_lv2_3: int, rank_lv4: int)
signal player_charge_attack_fired(player_num: int, level: int, attack_name: String)
signal spellcard_activated(player_num: int, level: int, rank: int)
signal player_shove_landed(player_num: int, landing_pos: Vector2)

const FAIRY_SCENE: PackedScene = preload("res://scenes/enemies/fairy.tscn")
const SPIRIT_SCENE: PackedScene = preload("res://scenes/enemies/spirit.tscn")
const SHOCKWAVE_SCENE: PackedScene = preload("res://scenes/effects/shockwave.tscn")
const HEAVY_SHOCKWAVE_SCENE: PackedScene = preload("res://scenes/effects/heavy_shockwave.tscn")
const DEFEAT_SHOCKWAVE_SCENE: PackedScene = preload("res://scenes/effects/defeat_shockwave.tscn")
const SPELL_BANNER_SCENE: PackedScene = preload("res://scenes/effects/spell_banner.tscn")
const ENEMY_PELLET_SCENE: PackedScene = preload("res://scenes/bullets/enemy_pellet.tscn")
const DANMAKU_BULLET_SCENE: PackedScene = preload("res://scenes/bullets/danmaku_bullet.tscn")
const EARTH_LIGHT_RAY_SCENE: PackedScene = preload("res://scenes/attacks/earth_light_ray.tscn")
const YIN_YANG_ORB_SCENE: PackedScene = preload("res://scenes/attacks/yin_yang_orb.tscn")
const BOSS_CHARACTER_SCENE: PackedScene = preload("res://scenes/enemies/boss_character.tscn")
const LILY_WHITE_SCENE: PackedScene = preload("res://scenes/enemies/lily_white.tscn")
const PICKUP_ITEM_SCENE: PackedScene = preload("res://scenes/items/pickup_item.tscn")
const BAMBOO_STAGE_SCENE: PackedScene = preload("res://scenes/stages/bamboo_road/bamboo_road_3d.tscn")
const HAKUGYOKUROU_STAGE_SCENE: PackedScene = preload("res://scenes/stages/hakugyokurou_stairs/hakugyokurou_stairs_3d.tscn")
const DanmakuDispatcherScript = preload("res://scripts/arena/danmaku_dispatcher.gd")
const PlayfieldSpellExecutorScript = preload("res://scripts/arena/playfield_spell_executor.gd")
const PlayfieldEnemySpawnerScript = preload("res://scripts/arena/playfield_enemy_spawner.gd")
const FAIRY_SPACING: float = 60.0
const FAIRY_SPEED: float = 330.0
const COMBO_TIMEOUT: float = 0.85
const CORNER_SPAWN_X_OFFSET: float = -40.0
const CORNER_SPAWN_Y: float = 1080.0

func get_target_spawn_position() -> Vector2:
	return Vector2(PLAYFIELD_WIDTH / 2.0, PLAYFIELD_HEIGHT * 0.85)

func get_intro_spawn_position() -> Vector2:
	if player_number == 1:
		return Vector2(CORNER_SPAWN_X_OFFSET, CORNER_SPAWN_Y)
	else:
		return Vector2(PLAYFIELD_WIDTH - CORNER_SPAWN_X_OFFSET, CORNER_SPAWN_Y)

func start_round_intro(duration: float = 1.2) -> void:
	if player:
		player.start_intro_glide(get_target_spawn_position(), duration)

var current_combo: int = 0
var _combo_timer: float = 0.0
var danmaku_dispatcher: DanmakuDispatcher = null
var spell_executor: PlayfieldSpellExecutor = null
var enemy_spawner: PlayfieldEnemySpawner = null
var _dim_tween: Tween:
	get: return spell_executor._dim_tween if spell_executor else null
	set(val):
		if spell_executor: spell_executor._dim_tween = val

var pellet_pool: NodePool:
	get:
		_ensure_danmaku_dispatcher()
		return danmaku_dispatcher.pellet_pool if danmaku_dispatcher else null
	set(val):
		_ensure_danmaku_dispatcher()
		if danmaku_dispatcher: danmaku_dispatcher.pellet_pool = val

var danmaku_bullet_pool: NodePool:
	get:
		_ensure_danmaku_dispatcher()
		return danmaku_dispatcher.danmaku_bullet_pool if danmaku_dispatcher else null
	set(val):
		_ensure_danmaku_dispatcher()
		if danmaku_dispatcher: danmaku_dispatcher.danmaku_bullet_pool = val

var _custom_hazards: Array[Node]:
	get:
		_ensure_danmaku_dispatcher()
		return danmaku_dispatcher._custom_hazards if danmaku_dispatcher else []
	set(val):
		_ensure_danmaku_dispatcher()
		if danmaku_dispatcher: danmaku_dispatcher._custom_hazards = val

var active_boss: BossCharacter:
	get:
		_ensure_enemy_spawner()
		return enemy_spawner.active_boss if enemy_spawner else null
	set(val):
		_ensure_enemy_spawner()
		if enemy_spawner: enemy_spawner.active_boss = val

var active_lily_white: LilyWhite:
	get:
		_ensure_enemy_spawner()
		return enemy_spawner.active_lily_white if enemy_spawner else null
	set(val):
		_ensure_enemy_spawner()
		if enemy_spawner: enemy_spawner.active_lily_white = val

## Deterministic per-round seed (set by Arena via set_round_seed, shared by both netplay
## clients) and a per-spawn counter, combined to give each spawn_boss() call its own seed that
## still matches identically across clients - lets BossCharacter's attack-pattern selection and
## hop AI roll the same "random" choices on both sides instead of diverging (see
## BossCharacter._rng). The same seed also feeds each spellcard cast (see execute_spellcard),
## so any DanmakuStep that opts into DanmakuStep.take_sync_rng() rolls identically on both
## sides - currently DanmakuExtraAttackStep, whose Yin-Yang Orbs and Earth Light Rays are far
## too big and slow to be allowed to land in two different places. Individual bullet-level
## jitter inside the other DanmakuStep resources is still intentionally left unsynced: there
## are hundreds per pattern, nobody tracks one, and syncing them would buy nothing.
var _round_seed: int = 99991
var _boss_spawn_count: int = 0
var _spellcard_cast_count: int = 0
## Same idea for Lily White / boss defeat drops, so both screens float up the same item.
var _pickup_drop_count: int = 0

## The opponent's Playfield (each lives in its own SubViewport, so they're never actual
## scene-tree siblings) - wired up once by Arena after both fields exist. Lets an effect
## that's meant to be seen on both sides of the split screen (e.g. Sakuya's Time Stop dim)
## reach across from whichever field it's actually executing on.
var sibling_playfield: Playfield = null

var match_elapsed_time: float = 0.0
var current_rank_lv2_3: int = 1
var current_rank_lv4: int = 1
var current_boss_rank: int = 1
var current_overall_rank: int = 1
var freeze_ranks: bool = false
var freeze_boss_rank: bool = false
var freeze_overall_rank: bool = false
# Boss Rank and Overall Rank each progress on their own clock so that
# manually setting one rank track (via set_ranks/set_boss_rank/set_overall_rank)
# never nudges the others - the three tracks are fully independent, matching
# how Spellcard Rank, Boss Rank, and Overall Rank are detached in Touhou 09.
var _boss_rank_elapsed_time: float = 0.0
var _overall_rank_elapsed_time: float = 0.0
var _temp_pellet_spawning_enabled: bool = true
var pellet_spawning_enabled: bool:
	get: return danmaku_dispatcher.pellet_spawning_enabled if danmaku_dispatcher else _temp_pellet_spawning_enabled
	set(val):
		_temp_pellet_spawning_enabled = val
		if danmaku_dispatcher: danmaku_dispatcher.pellet_spawning_enabled = val

var _temp_spirit_spawning_enabled: bool = true
var spirit_spawning_enabled: bool:
	get: return enemy_spawner.spirit_spawning_enabled if enemy_spawner else _temp_spirit_spawning_enabled
	set(val):
		_temp_spirit_spawning_enabled = val
		if enemy_spawner: enemy_spawner.spirit_spawning_enabled = val

var _temp_lily_white_enabled: bool = true
@export var lily_white_spawning_enabled: bool = true:
	get: return enemy_spawner.lily_white_spawning_enabled if enemy_spawner else _temp_lily_white_enabled
	set(val):
		_temp_lily_white_enabled = val
		if enemy_spawner: enemy_spawner.lily_white_spawning_enabled = val
		if not val and active_lily_white != null and is_instance_valid(active_lily_white):
			active_lily_white.stop_and_free()
			active_lily_white = null
var _temp_max_active_bullets: int = 350
@export var max_active_bullets: int = 350:
	get: return danmaku_dispatcher.max_active_bullets if danmaku_dispatcher else _temp_max_active_bullets
	set(val):
		_temp_max_active_bullets = val
		if danmaku_dispatcher: danmaku_dispatcher.max_active_bullets = val
var _chained_trains_count: int = 0
var _last_extra_attack_time: float = -10.0

func get_trains_required_for_extra() -> int:
	if character_id == "cirno":
		return 1
	if match_elapsed_time < 30.0:
		return 3
	elif match_elapsed_time < 60.0:
		return 2
	else:
		return 1

var fairy_spawner: FairySpawner:
	get:
		_ensure_enemy_spawner()
		return enemy_spawner.fairy_spawner if enemy_spawner else null
	set(val):
		_ensure_enemy_spawner()
		if enemy_spawner: enemy_spawner.fairy_spawner = val

var _temp_fairy_interval: float = 1.7
var _temp_fairy_enabled: bool = true

@export var fairy_spawn_interval: float = 1.7:
	get:
		_ensure_enemy_spawner()
		return enemy_spawner.fairy_spawner.fairy_spawn_interval if (enemy_spawner and enemy_spawner.fairy_spawner) else _temp_fairy_interval
	set(val):
		_temp_fairy_interval = val
		if enemy_spawner and enemy_spawner.fairy_spawner: enemy_spawner.fairy_spawner.fairy_spawn_interval = val

@export var fairy_spawning_enabled: bool = true:
	get:
		_ensure_enemy_spawner()
		return enemy_spawner.fairy_spawner.fairy_spawning_enabled if (enemy_spawner and enemy_spawner.fairy_spawner) else _temp_fairy_enabled
	set(val):
		_temp_fairy_enabled = val
		if enemy_spawner and enemy_spawner.fairy_spawner: enemy_spawner.fairy_spawner.fairy_spawning_enabled = val

func _ensure_danmaku_dispatcher() -> void:
	if danmaku_dispatcher == null:
		danmaku_dispatcher = DanmakuDispatcherScript.new()
		add_child(danmaku_dispatcher)
		danmaku_dispatcher.pellet_spawning_enabled = _temp_pellet_spawning_enabled
		danmaku_dispatcher.max_active_bullets = _temp_max_active_bullets
		if bullets_layer == null:
			bullets_layer = get_node_or_null("%Bullets")
		danmaku_dispatcher.setup(self, bullets_layer)

func _ensure_spell_executor() -> void:
	if spell_executor == null:
		spell_executor = PlayfieldSpellExecutorScript.new()
		add_child(spell_executor)
		if effects_layer == null:
			effects_layer = get_node_or_null("%Effects")
		if defeat_flash == null:
			defeat_flash = get_node_or_null("%DefeatFlash")
		if dim_overlay == null:
			dim_overlay = get_node_or_null("%DimOverlay")
		if spell_bg_overlay == null:
			spell_bg_overlay = get_node_or_null("%SpellBackgroundOverlay")
		spell_executor.setup(self, effects_layer, defeat_flash, dim_overlay, spell_bg_overlay)

func _ensure_enemy_spawner() -> void:
	if enemy_spawner == null:
		enemy_spawner = PlayfieldEnemySpawnerScript.new()
		add_child(enemy_spawner)
		if entities_layer == null:
			entities_layer = get_node_or_null("%Entities")
		enemy_spawner.setup(self, entities_layer)
		enemy_spawner.spirit_spawning_enabled = _temp_spirit_spawning_enabled
		enemy_spawner.lily_white_spawning_enabled = _temp_lily_white_enabled

func _ready() -> void:
	if bullets_layer == null:
		bullets_layer = get_node_or_null("%Bullets")
	if entities_layer == null:
		entities_layer = get_node_or_null("%Entities")
	_ensure_danmaku_dispatcher()
	_ensure_spell_executor()
	_ensure_enemy_spawner()
	
	_init_background()

	_init_spawner()
	if player == null:
		_spawn_player()

func _init_background(override_stage_scene: PackedScene = null) -> void:
	if background_3d_container:
		background_3d_container.visible = enable_3d_background
		if background_fallback:
			background_fallback.visible = not enable_3d_background
		
		var viewport: SubViewport = background_3d_container.get_node_or_null("%Background3DViewport")
		if viewport == null:
			viewport = background_3d_container.get_node_or_null("Background3DViewport")
		
		if viewport:
			var desired_scene: PackedScene = override_stage_scene
			if desired_scene == null:
				var cdata := CharacterData.get_character(character_id)
				if cdata:
					desired_scene = cdata.get_home_stage_scene()
				if desired_scene == null:
					desired_scene = BAMBOO_STAGE_SCENE
			
			var current_stage: Node = null
			for child in viewport.get_children():
				if child is Node3D:
					current_stage = child
					break
			
			var current_stage_path: String = current_stage.scene_file_path if current_stage else ""
			if current_stage_path.is_empty() and current_stage and current_stage.get_script():
				current_stage_path = current_stage.get_script().resource_path
			
			var stage_needs_change: bool = (current_stage == null)
			if not stage_needs_change and not current_stage_path.is_empty():
				var current_base := current_stage_path.get_file().get_basename()
				var desired_base := desired_scene.resource_path.get_file().get_basename()
				if current_base != desired_base and not desired_scene.resource_path.contains(current_base):
					stage_needs_change = true
			
			if stage_needs_change:
				if current_stage:
					current_stage.queue_free()
				var new_stage: Node = desired_scene.instantiate()
				if player_number == 2:
					new_stage.set("time_offset", 1.8)
				viewport.add_child(new_stage)
			else:
				if current_stage and player_number == 2:
					current_stage.set("time_offset", 1.8)


func reset_for_new_round(preserve_progression: bool = true) -> void:
	# 1. Clear non-player entities (fairies, spirits, bosses, pickups)
	_ensure_enemy_spawner()
	enemy_spawner.clear_all_enemies()
	if spell_bg_overlay:
		spell_bg_overlay.deactivate(0.1)
	
	# 2. Recycle pellets and clear loose bullets/projectiles/lasers/orbs
	clear_all_bullets()
				
	# 3. Clear shockwaves and particle effects
	if effects_layer:
		for child in effects_layer.get_children():
			child.set_process(false)
			child.set_physics_process(false)
			child.queue_free()
	if spell_executor:
		spell_executor.reset()
			
	# 4. Reset player position to intro corner spawn location, health, and status
	if player:
		var corner_pos := get_intro_spawn_position()
		player.reset_for_round(corner_pos, preserve_progression)
		player_health_changed.emit(player_number, player.current_health, player.max_health)
		
	# 5. Reset round combo and spawner timers
	current_combo = 0
	_combo_timer = 0.0
	combo_updated.emit(0)
	if fairy_spawner:
		fairy_spawner.reset()
	_chained_trains_count = 0
	_last_extra_attack_time = -10.0
	if not preserve_progression:
		match_elapsed_time = 0.0
		current_rank_lv2_3 = 1
		current_rank_lv4 = 1
		current_boss_rank = 1
		current_overall_rank = 1
		_boss_rank_elapsed_time = 0.0
		_overall_rank_elapsed_time = 0.0
		player_ranks_updated.emit(player_number, 1, 1)

func reset_for_new_match() -> void:
	reset_for_new_round(false)
	if background_3d_container:
		var viewport: SubViewport = background_3d_container.get_node_or_null("%Background3DViewport")
		if viewport == null:
			viewport = background_3d_container.get_node_or_null("Background3DViewport")
		if viewport:
			for child in viewport.get_children():
				if child.has_method("reset_stage"):
					child.reset_stage()

func set_round_over(over: bool) -> void:
	if player:
		player.is_round_over = over

func set_round_seed(seed_val: int) -> void:
	_round_seed = seed_val
	_boss_spawn_count = 0
	_spellcard_cast_count = 0
	_pickup_drop_count = 0
	_ensure_enemy_spawner()
	enemy_spawner.init_fairy_spawner(seed_val, _temp_fairy_interval, _temp_fairy_enabled)

func _init_spawner() -> void:
	_ensure_enemy_spawner()
	enemy_spawner.init_fairy_spawner(99991, _temp_fairy_interval, _temp_fairy_enabled)

func _physics_process(delta: float) -> void:
	match_elapsed_time += delta
	_boss_rank_elapsed_time += delta
	_overall_rank_elapsed_time += delta

	if not freeze_ranks:
		var new_lv2_3: int = clampi(1 + int(match_elapsed_time / 10.0), 1, 16)
		var new_lv4: int = clampi(1 + int(match_elapsed_time / 12.0), 1, 16)
		if new_lv2_3 != current_rank_lv2_3 or new_lv4 != current_rank_lv4:
			current_rank_lv2_3 = new_lv2_3
			current_rank_lv4 = new_lv4
			player_ranks_updated.emit(player_number, current_rank_lv2_3, current_rank_lv4)

	if not freeze_boss_rank:
		current_boss_rank = clampi(1 + int(_boss_rank_elapsed_time / 12.0), 1, 16)

	if not freeze_overall_rank:
		current_overall_rank = clampi(1 + int(_overall_rank_elapsed_time / 10.0), 1, 22)

	if _combo_timer > 0.0:
		_combo_timer -= delta
		if _combo_timer <= 0.0:
			current_combo = 0
			combo_updated.emit(0)
	
	if enemy_spawner:
		enemy_spawner.update_fairy_spawner(delta, _on_fairy_defeated)

func _spawn_fairy_train() -> void:
	if enemy_spawner:
		enemy_spawner.spawn_fairy_train(_on_fairy_defeated)

func setup(p_num: int, is_local: bool, char_id: String = "reimu", stage_scene: PackedScene = null, starting_hp: float = 5.0) -> void:
	player_number = p_num
	is_local_player = is_local
	character_id = char_id
	_init_background(stage_scene)
	_init_spawner()
	if player:
		player.player_number = player_number
		player.is_local_player = is_local_player
		player.character_id = character_id
		player.starting_health = starting_hp
		player.current_health = starting_hp
		player.position = get_intro_spawn_position()
		player.update_networking_mode()
		player_health_changed.emit(player_number, player.current_health, player.max_health)
		player_charge_updated.emit(player_number, player.active_charge, player.passive_charge, player.charge_segments)
		player_ranks_updated.emit(player_number, current_rank_lv2_3, current_rank_lv4)
		start_round_intro()
	else:
		_spawn_player(starting_hp)

func _spawn_player(starting_hp: float = 5.0) -> void:
	if entities_layer == null:
		entities_layer = get_node_or_null("%Entities")
	if bullets_layer == null:
		bullets_layer = get_node_or_null("%Bullets")
	
	var player_scene := preload("res://scenes/player/player.tscn")
	player = player_scene.instantiate()
	player.player_number = player_number
	player.is_local_player = is_local_player
	player.character_id = character_id
	player.starting_health = starting_hp
	player.current_health = starting_hp
	player.position = get_intro_spawn_position()
	if not player.bullet_spawned.is_connected(_on_player_bullet_spawned):
		player.bullet_spawned.connect(_on_player_bullet_spawned)
	if not player.health_changed.is_connected(_on_player_health_changed):
		player.health_changed.connect(_on_player_health_changed)
	if not player.shove_landed.is_connected(_on_player_shove_landed):
		player.shove_landed.connect(_on_player_shove_landed)
	if not player.defeated.is_connected(_on_player_defeated):
		player.defeated.connect(_on_player_defeated)
	if not player.charge_updated.is_connected(_on_player_charge_updated):
		player.charge_updated.connect(_on_player_charge_updated)
	if not player.charge_attack_fired.is_connected(_on_player_charge_attack_fired):
		player.charge_attack_fired.connect(_on_player_charge_attack_fired)
	if not player.spellcard_fired.is_connected(_on_player_spellcard_fired):
		player.spellcard_fired.connect(_on_player_spellcard_fired)
	player.playfield = self
	if entities_layer:
		entities_layer.add_child(player)
	player.update_networking_mode()
	player_health_changed.emit(player_number, player.current_health, player.max_health)
	player_charge_updated.emit(player_number, player.active_charge, player.passive_charge, player.charge_segments)
	player_ranks_updated.emit(player_number, current_rank_lv2_3, current_rank_lv4)
	start_round_intro()

func set_ai_mode(enabled: bool, shooting_enabled: bool = true) -> void:
	if player:
		player.set_ai_mode(enabled, shooting_enabled)

func set_ranks(lv2_3: int, lv4: int) -> void:
	current_rank_lv2_3 = clampi(lv2_3, 1, 16)
	current_rank_lv4 = clampi(lv4, 1, 16)
	match_elapsed_time = maxf(0.0, float(current_rank_lv2_3 - 1) * 10.0)
	player_ranks_updated.emit(player_number, current_rank_lv2_3, current_rank_lv4)

func set_boss_rank(rank: int) -> void:
	current_boss_rank = clampi(rank, 1, 16)
	_boss_rank_elapsed_time = maxf(0.0, float(current_boss_rank - 1) * 12.0)

func set_overall_rank(rank: int) -> void:
	current_overall_rank = clampi(rank, 1, 22)
	_overall_rank_elapsed_time = maxf(0.0, float(current_overall_rank - 1) * 10.0)

func clear_all_bullets() -> void:
	if danmaku_dispatcher:
		danmaku_dispatcher.clear_all_bullets()

func clear_all_enemies() -> void:
	_ensure_enemy_spawner()
	enemy_spawner.clear_all_enemies()

func is_ai_mode() -> bool:
	return player.is_ai if player else false

func _on_player_health_changed(current: float, max_val: float) -> void:
	player_health_changed.emit(player_number, current, max_val)

func _on_player_defeated() -> void:
	player_defeated.emit(player_number)

func _on_player_charge_updated(active_val: float, passive_val: float, max_segs: int) -> void:
	player_charge_updated.emit(player_number, active_val, passive_val, max_segs)

func _on_player_charge_attack_fired(level: int, attack_name: String) -> void:
	player_charge_attack_fired.emit(player_number, level, attack_name)
	execute_charge_attack(level)

func execute_charge_attack(level: int) -> void:
	_ensure_spell_executor()
	spell_executor.execute_charge_attack(level)

func _on_player_spellcard_fired(level: int, _spellcard_name: String) -> void:
	var rank: int = current_rank_lv4 if level == 4 else current_rank_lv2_3
	spellcard_activated.emit(player_number, level, rank)

## Triggers the spellcard declaration banner and freeze visuals during Action Stop
func trigger_spellcard_declaration(level: int, spellcard_name: String) -> void:
	_ensure_spell_executor()
	spell_executor.trigger_spellcard_declaration(level, spellcard_name)

## Triggers the expanding heavy defensive shockwave after Action Stop unpauses
func trigger_spellcard_shockwave(level: int) -> void:
	_ensure_spell_executor()
	spell_executor.trigger_spellcard_shockwave(level)

## Combined helper for direct invocation
func trigger_spellcard_visuals(level: int, spellcard_name: String) -> void:
	_ensure_spell_executor()
	spell_executor.trigger_spellcard_visuals(level, spellcard_name)

func _show_spellcard_banner(level: int, spellcard_name: String) -> void:
	_ensure_spell_executor()
	spell_executor.show_spellcard_banner(level, spellcard_name)

## Displays the authentic Touhou 09 PoFV spellcard warning alert and red playfield wash on the target playfield during an Action Stop
func show_spellcard_warning(spell_name: String, level: int, rank: int = 1) -> void:
	_ensure_spell_executor()
	spell_executor.show_spellcard_warning(spell_name, level, rank)

## Flashes the entire playfield red for a couple of frames upon lethal KO
func play_defeat_flash() -> void:
	_ensure_spell_executor()
	spell_executor.play_defeat_flash()

## Fades this field's whole rendered view toward target_alpha of a black overlay
func set_dim(target_alpha: float, duration: float) -> void:
	_ensure_spell_executor()
	spell_executor.set_dim(target_alpha, duration)

## Spawns the cosmetic faint red shockwave bursting out from the player's defeat position
func spawn_defeat_shockwave(death_pos: Vector2) -> void:
	_ensure_spell_executor()
	spell_executor.spawn_defeat_shockwave(death_pos)

func _on_player_shove_landed(landing_pos: Vector2) -> void:
	spawn_hit_shockwave(landing_pos)
	player_shove_landed.emit(player_number, landing_pos)

func spawn_hit_shockwave(landing_pos: Vector2) -> void:
	_ensure_spell_executor()
	spell_executor.spawn_hit_shockwave(landing_pos)

func _on_player_bullet_spawned(bullet: PlayerBullet) -> void:
	if bullets_layer == null:
		bullets_layer = get_node_or_null("%Bullets")
	if bullets_layer:
		bullets_layer.add_child(bullet)

func _on_fairy_defeated(fairy_type: Fairy.FairyType, death_pos: Vector2, source: String = "bullet") -> void:
	if source == "heavy_shockwave":
		# Fairies caught in spellcard blast explode onto their own shockwaves (Option A).
		# Death is delayed so expanding heavy shockwave erases bullets ahead.
		# Spawns visual shockwave without sending counter-attacks or combos to opponent.
		if player and is_instance_valid(player):
			var gain: float = player.passive_charge_per_great_fairy if fairy_type == Fairy.FairyType.GREAT else player.passive_charge_per_fairy
			player.add_passive_charge(gain * 0.5)
		
		if effects_layer == null:
			effects_layer = get_node_or_null("%Effects")
		if effects_layer:
			var shockwave: Shockwave = SHOCKWAVE_SCENE.instantiate()
			shockwave.setup(fairy_type, death_pos)
			effects_layer.add_child(shockwave)
		return

	if player and is_instance_valid(player):
		var gain: float = player.passive_charge_per_great_fairy if fairy_type == Fairy.FairyType.GREAT else player.passive_charge_per_fairy
		player.add_passive_charge(gain)

	if effects_layer == null:
		effects_layer = get_node_or_null("%Effects")
	if effects_layer:
		var shockwave: Shockwave = SHOCKWAVE_SCENE.instantiate()
		shockwave.setup(fairy_type, death_pos)
		shockwave.pellet_canceled.connect(_on_pellet_canceled)
		effects_layer.add_child(shockwave)
	
	if source == "bullet":
		# Direct shot kill sets combo to 1 (isolated kills send 0 pellets in PoFV)
		current_combo = 1
		_combo_timer = COMBO_TIMEOUT
		combo_updated.emit(current_combo)
	elif source == "shockwave":
		# Chained explosion kill increases combo
		current_combo += 1
		_combo_timer = COMBO_TIMEOUT
		combo_updated.emit(current_combo)
		
		# Send pellets to opponent for chained kills
		var pellet_count: int = 1
		if current_combo >= 9:
			pellet_count = 2
		
		var has_big_pellet: bool = (fairy_type == Fairy.FairyType.GREAT)
		var has_spirit: bool = (fairy_type == Fairy.FairyType.GREAT and current_combo >= 3)
		
		# Extra Attack pacing:
		# Starts at ~1 EX per 3 chained fairy trains, scaling down to every 1 train at apex difficulty (60s+)
		var has_extra_attack: bool = false
		if fairy_type == Fairy.FairyType.GREAT and current_combo >= 3:
			_chained_trains_count += 1
			var req_trains: int = get_trains_required_for_extra()
			var ex_interval: float = 0.5 if character_id == "cirno" else 1.2
			if _chained_trains_count >= req_trains and (match_elapsed_time - _last_extra_attack_time) >= ex_interval:
				has_extra_attack = true
				_chained_trains_count = 0
				_last_extra_attack_time = match_elapsed_time
		
		attack_sent.emit(death_pos, pellet_count, current_combo, has_big_pellet, has_spirit, has_extra_attack, 0)

func _on_pellet_canceled(pellet_pos: Vector2, bounce_count: int = 0) -> void:
	if player and is_instance_valid(player):
		player.add_passive_charge(player.passive_charge_per_cancel)

	# In PoFV, canceling enemy bullets in a shockwave adds +1 to combo hit count and resets timeout
	current_combo += 1
	_combo_timer = COMBO_TIMEOUT
	combo_updated.emit(current_combo)
	
	# PoFV Damping Rules:
	# 1. Canceling a pellet NEVER spawns spirits (has_spirit = false).
	# 2. Returned pellets never duplicate (pellet_count is at most 1).
	# 3. If a pellet has already bounced once (bounce_count >= 1), it evolves into an un-cancellable Large Bullet!
	#    Once it becomes a Large Bullet, fairy explosions cannot cancel it, breaking the infinite rally.
	if bounce_count >= 1:
		attack_sent.emit(pellet_pos, 0, current_combo, true, false, false, bounce_count + 1)
	else:
		attack_sent.emit(pellet_pos, 1, current_combo, false, false, false, bounce_count + 1)

func _on_spirit_defeated(death_pos: Vector2, source: String = "bullet") -> void:
	if source == "heavy_shockwave":
		# Spirits caught in spellcard blast explode onto their own shockwaves (Option A).
		# Spawns visual shockwave without sending counter-attacks or combos to opponent.
		if player and is_instance_valid(player):
			player.add_passive_charge(player.passive_charge_per_spirit * 0.5)
		if effects_layer == null:
			effects_layer = get_node_or_null("%Effects")
		if effects_layer:
			var shockwave: Shockwave = SHOCKWAVE_SCENE.instantiate()
			shockwave.setup(Fairy.FairyType.SMALL, death_pos)
			effects_layer.add_child(shockwave)
		return

	if player and is_instance_valid(player):
		player.add_passive_charge(player.passive_charge_per_spirit)

	if effects_layer == null:
		effects_layer = get_node_or_null("%Effects")
	if effects_layer:
		var shockwave: Shockwave = SHOCKWAVE_SCENE.instantiate()
		shockwave.setup(Fairy.FairyType.SMALL, death_pos)
		shockwave.pellet_canceled.connect(_on_pellet_canceled)
		effects_layer.add_child(shockwave)

	current_combo += 1
	_combo_timer = COMBO_TIMEOUT
	combo_updated.emit(current_combo)
	
	# Destroying a spirit sends 2 pellets + 1 big pellet (never loops into another spirit)
	attack_sent.emit(death_pos, 2, current_combo, true, false, false, 0)

func spawn_extra_attack(sender_character: String, target_pos: Vector2, rank: int = 1) -> void:
	if bullets_layer == null:
		bullets_layer = get_node_or_null("%Bullets")
	if bullets_layer == null:
		return
	
	var sender_data := CharacterData.get_character(sender_character)
	if sender_data == null or sender_data.extra_attack_scene == null:
		return
	
	var attack_node: Node2D = sender_data.extra_attack_scene.instantiate()
	var clamped_x: float = clampf(target_pos.x, 36.0, PLAYFIELD_WIDTH - 36.0)
	
	if attack_node is EarthLightRay:
		attack_node.position = Vector2(clamped_x, 0.0)
	else:
		attack_node.position = Vector2(clamped_x, target_pos.y)
	
	if attack_node.has_method("setup_rank"):
		attack_node.setup_rank(rank)
	
	# Both clients are handed the same rolled target for this shot, so hashing it
	# gives the attack a stable variation without another field in the packet.
	if attack_node.has_method("setup_variation"):
		attack_node.setup_variation(hash(Vector2i(roundi(target_pos.x * 100.0), roundi(target_pos.y * 100.0))))
	
	bullets_layer.add_child(attack_node)
	register_custom_hazard(attack_node)

func spawn_spirit(local_pos: Vector2, color_theme: String = "blue") -> void:
	_ensure_enemy_spawner()
	enemy_spawner.spawn_spirit(local_pos, color_theme, _on_spirit_defeated, _on_spirit_detonated)

func _on_spirit_detonated(detonation_pos: Vector2, color_theme: String) -> void:
	_ensure_enemy_spawner()
	enemy_spawner.detonate_spirit(detonation_pos, color_theme)

func register_custom_hazard(node: Node) -> void:
	_ensure_danmaku_dispatcher()
	danmaku_dispatcher.register_custom_hazard(node)

func unregister_custom_hazard(node: Node) -> void:
	if danmaku_dispatcher:
		danmaku_dispatcher.unregister_custom_hazard(node)

func get_active_bullet_count() -> int:
	return danmaku_dispatcher.get_active_bullet_count() if danmaku_dispatcher else 0

## Returns an array of only active bullets (pellets + danmaku + non-pooled hazards), avoiding 1,700-node pool scans.
func get_active_bullets() -> Array:
	return danmaku_dispatcher.get_active_bullets() if danmaku_dispatcher else []

## Cirno "Perfect Freeze": seizes every active bullet currently on this field (pellets and
## danmaku bullets alike, including ones not spawned by the triggering spellcard), reassigning
## each a random new direction and recoloring it white. Bullets hang motionless in place for
## freeze_duration (playing se_timestop0), then begin accelerating outward (playing se_tan00).
func freeze_and_scatter_bullets(p_accel: float, p_max_speed: float, freeze_duration: float = 1.0) -> void:
	AudioService.play_sfx("se_timestop0")
	for entity in get_active_bullets():
		if not is_instance_valid(entity):
			continue
		if ("visible" in entity) and not entity.visible:
			continue
		if entity.has_method("freeze_and_scatter"):
			entity.freeze_and_scatter(p_accel, p_max_speed, freeze_duration)
	
	if freeze_duration > 0.0 and is_inside_tree():
		get_tree().create_timer(freeze_duration).timeout.connect(func():
			if is_instance_valid(self):
				AudioService.play_sfx("se_tan00")
		)

func get_entity_telemetry() -> Dictionary:
	var p_count: int = pellet_pool.get_active_count() if pellet_pool else 0
	var d_count: int = danmaku_bullet_pool.get_active_count() if danmaku_bullet_pool else 0
	var f_count: int = 0
	var s_count: int = 0
	if entities_layer:
		for child in entities_layer.get_children():
			if not child.visible:
				continue
			if child is Fairy:
				f_count += 1
			elif child is Spirit:
				s_count += 1
	var total_b: int = p_count + d_count
	var total_e: int = total_b + f_count + s_count + (1 if active_boss else 0)
	return {
		"pellets": p_count,
		"danmaku": d_count,
		"total_bullets": total_b,
		"fairies": f_count,
		"spirits": s_count,
		"has_boss": active_boss != null,
		"total_entities": total_e
	}

func spawn_pellet(local_pos: Vector2, speed: float = -1.0, is_big: bool = false, bounce_count: int = 0, p_color: Color = Color.WHITE) -> void:
	_ensure_danmaku_dispatcher()
	danmaku_dispatcher.spawn_pellet(local_pos, speed, is_big, bounce_count, p_color)

func spawn_ring_pellet(local_pos: Vector2, speed: float, direction: Vector2, color: Color) -> void:
	_ensure_danmaku_dispatcher()
	danmaku_dispatcher.spawn_ring_pellet(local_pos, speed, direction, color)

func spawn_directional_pellet(local_pos: Vector2, speed: float, direction: Vector2, color: Color, is_ring_pellet: bool = false) -> void:
	_ensure_danmaku_dispatcher()
	danmaku_dispatcher.spawn_directional_pellet(local_pos, speed, direction, color, is_ring_pellet)

func spawn_danmaku_bullet(p_data: DanmakuBulletData, spawn_pos: Vector2, p_mode: DanmakuBullet.MotionMode, p_dir: Vector2, p_speed: float, p_accel: float = 0.0, p_max_speed: float = 0.0) -> DanmakuBullet:
	_ensure_danmaku_dispatcher()
	return danmaku_dispatcher.spawn_danmaku_bullet(p_data, spawn_pos, p_mode, p_dir, p_speed, p_accel, p_max_speed)

func spawn_danmaku_bullet_decel_home(
	p_data: DanmakuBulletData,
	spawn_pos: Vector2,
	p_dir: Vector2,
	p_init_speed: float,
	p_decel: float,
	p_pause: float,
	p_launch_speed: float,
	p_stages: int = 1,
	p_stage1_duration: float = 0.5,
	p_homing_sfx: String = ""
) -> DanmakuBullet:
	_ensure_danmaku_dispatcher()
	return danmaku_dispatcher.spawn_danmaku_bullet_decel_home(p_data, spawn_pos, p_dir, p_init_speed, p_decel, p_pause, p_launch_speed, p_stages, p_stage1_duration, p_homing_sfx)

func spawn_danmaku_bullet_expanding_orbit(
	p_data: DanmakuBulletData,
	p_center: Vector2,
	p_initial_radius: float,
	p_initial_angle: float,
	p_radial_speed: float,
	p_angular_speed: float,
	p_freeze_time: float = -1.0,
	p_breakout_time: float = -1.0,
	p_breakout_angle_offset: float = 0.0,
	p_breakout_accel: float = 0.0,
	p_breakout_max_speed: float = 0.0,
	p_breakout_initial_speed: float = 0.0
) -> DanmakuBullet:
	_ensure_danmaku_dispatcher()
	return danmaku_dispatcher.spawn_danmaku_bullet_expanding_orbit(p_data, p_center, p_initial_radius, p_initial_angle, p_radial_speed, p_angular_speed, p_freeze_time, p_breakout_time, p_breakout_angle_offset, p_breakout_accel, p_breakout_max_speed, p_breakout_initial_speed)

func spawn_danmaku_bullet_lunar_wave(
	p_data: DanmakuBulletData,
	p_center: Vector2,
	p_initial_angle: float,
	p_expand_speed: float,
	p_expand_time: float,
	p_hold_time: float,
	p_contract_speed: float,
	p_contract_time: float,
	p_release_speed: float,
	p_release_accel: float,
	p_release_max_speed: float,
	p_spin_lean: float
) -> DanmakuBullet:
	_ensure_danmaku_dispatcher()
	return danmaku_dispatcher.spawn_danmaku_bullet_lunar_wave(p_data, p_center, p_initial_angle, p_expand_speed, p_expand_time, p_hold_time, p_contract_speed, p_contract_time, p_release_speed, p_release_accel, p_release_max_speed, p_spin_lean)


func spawn_danmaku_bullet_curve_line(
	p_data: DanmakuBulletData,
	spawn_pos: Vector2,
	p_dir: Vector2,
	p_speed: float,
	p_curve_angular_speed: float,
	p_curve_duration: float,
	p_accel: float = 0.0,
	p_max_speed: float = 0.0,
	p_spin_speed: float = 0.0
) -> DanmakuBullet:
	_ensure_danmaku_dispatcher()
	return danmaku_dispatcher.spawn_danmaku_bullet_curve_line(p_data, spawn_pos, p_dir, p_speed, p_curve_angular_speed, p_curve_duration, p_accel, p_max_speed, p_spin_speed)

func spawn_danmaku_bullet_ellipse_then_line(
	p_data: DanmakuBulletData,
	p_center: Vector2,
	p_radius: Vector2,
	p_start_angle: float,
	p_angular_speed: float,
	p_target_angle: float,
	p_fan_direction: Vector2,
	p_fan_speed: float,
	p_accel: float = 0.0,
	p_max_speed: float = 0.0
) -> DanmakuBullet:
	_ensure_danmaku_dispatcher()
	return danmaku_dispatcher.spawn_danmaku_bullet_ellipse_then_line(p_data, p_center, p_radius, p_start_angle, p_angular_speed, p_target_angle, p_fan_direction, p_fan_speed, p_accel, p_max_speed)



func execute_spellcard(spell_data: SpellcardData, rank: int, custom_origin: Vector2 = Vector2.ZERO) -> void:
	_ensure_spell_executor()
	spell_executor.execute_spellcard(spell_data, rank, custom_origin)

## Dispels any active boss illusion currently hovering on this playfield (e.g. when player uses their own Lv 4 spellcard)
func dispel_active_boss() -> void:
	_ensure_enemy_spawner()
	enemy_spawner.dispel_active_boss()

func spawn_boss(sender_character: String, rank: int = 1) -> BossCharacter:
	_ensure_enemy_spawner()
	return enemy_spawner.spawn_boss(sender_character, rank, _on_boss_defeated, _on_boss_dispelled_or_left)

func _on_boss_dispelled_or_left() -> void:
	if spell_bg_overlay:
		spell_bg_overlay.deactivate()

func activate_spell_bg(character_name: String) -> void:
	_ensure_spell_executor()
	spell_executor.activate_spell_bg(character_name)

func deactivate_spell_bg(duration: float = 0.8) -> void:
	_ensure_spell_executor()
	spell_executor.deactivate_spell_bg(duration)

func _on_boss_defeated(death_pos: Vector2, _source: String = "bullet") -> void:
	active_boss = null
	if spell_bg_overlay:
		spell_bg_overlay.deactivate()
	if effects_layer == null:
		effects_layer = get_node_or_null("%Effects")
	if effects_layer:
		var heavy_wave: HeavyShockwave = HEAVY_SHOCKWAVE_SCENE.instantiate()
		heavy_wave.setup(death_pos)
		effects_layer.add_child(heavy_wave)
	spawn_defeat_pickups(death_pos)

func spawn_lily_white(forced: bool = false) -> LilyWhite:
	_ensure_enemy_spawner()
	return enemy_spawner.spawn_lily_white(forced, _on_lily_white_defeated)

func _on_lily_white_defeated(death_pos: Vector2) -> void:
	if effects_layer == null:
		effects_layer = get_node_or_null("%Effects")
	if effects_layer:
		var heavy_wave: HeavyShockwave = HEAVY_SHOCKWAVE_SCENE.instantiate()
		heavy_wave.setup(death_pos)
		effects_layer.add_child(heavy_wave)
	spawn_defeat_pickups(death_pos)

## Spawns an authentic Touhou 09 defeat reward drop (G, 点, EX, or 弾).
## A single pickup is chosen at random and launched straight up vertically.
func spawn_defeat_pickups(center_pos: Vector2, forced_type: int = -1) -> PickupItem:
	_ensure_enemy_spawner()
	return enemy_spawner.spawn_defeat_pickups(center_pos, forced_type, _on_pickup_collected)

func _on_pickup_collected(item: PickupItem, collector: Player) -> void:
	if collector == null or item == null:
		return
	
	var item_pos: Vector2 = item.position
	var item_type: PickupItem.Type = item.item_type
	
	# In Touhou 09, collecting any of the defeat reward pickups plays se_powerup
	AudioService.play_powerup()
	
	match item_type:
		PickupItem.Type.G:
			# Fully recharge spell gauge
			collector.set_passive_charge(float(collector.charge_segments))
			player_charge_updated.emit(player_number, collector.active_charge, collector.passive_charge, collector.charge_segments)
		
		PickupItem.Type.POINT:
			# Activate Level 4 Spellcard against opponent!
			spellcard_activated.emit(player_number, 4, current_rank_lv4)
		
		PickupItem.Type.EX:
			# Dispatch barrage of Extra Attacks to opponent
			for k in range(3):
				var delay: float = float(k) * 0.18
				if delay <= 0.0:
					attack_sent.emit(item_pos, 0, 0, false, false, true, 0)
				else:
					if is_inside_tree():
						get_tree().create_timer(delay).timeout.connect(func():
							if is_instance_valid(self):
								attack_sent.emit(item_pos, 0, 0, false, false, true, 0)
						)
					else:
						attack_sent.emit(item_pos, 0, 0, false, false, true, 0)
		
		PickupItem.Type.BULLET:
			# Dispatch dense barrage of 30 large pellets to opponent
			attack_sent.emit(item_pos, 30, 0, true, false, false, 1)
