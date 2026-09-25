class_name Player
extends CharacterBody2D

# Player controller for Touhou Web Arena
@export var player_number: int = 1
var _character_id_backing: String = "reimu"

@export var character_data: CharacterData = null:
	set(value):
		character_data = value
		if character_data:
			_character_id_backing = character_data.character_id
		if is_node_ready():
			apply_character_data()

@export var character_id: String:
	get:
		return character_data.character_id if character_data else _character_id_backing
	set(value):
		_character_id_backing = value
		var new_data := CharacterData.get_character(value)
		if character_data != new_data:
			character_data = new_data
		elif is_node_ready():
			apply_character_data()

@export var is_local_player: bool = true:
	set(value):
		is_local_player = value
		_update_hurtbox_state()
@export var is_ai: bool = false:
	set(value):
		is_ai = value
		_update_hurtbox_state()
var ai_controller: RudimentaryAI = null
var playfield: Node2D = null

# Movement speeds (in pixels per second - calibrated to 1.0s normal / 2.0s focus screen cross)
@export var normal_speed: float = 540.0
@export var focus_speed: float = 270.0
@export var bullet_damage: float = 1.0

# Health & Life parameters
@export var max_health: float = 5.0
@export var starting_health: float = 5.0
@export var shove_duration: float = 0.85
var current_health: float = 5.0
var is_invulnerable: bool = false
var god_mode: bool = false
var is_shoved: bool = false
var is_dead: bool = false
var _invulnerability_timer: float = 0.0
var _shove_flash_timer: float = 0.0
var _red_blink_timer: float = 0.0
var _shove_tween: Tween = null
const INVULNERABILITY_POST_SHOVE: float = 0.5
const RED_BLINK_TOTAL_TIME: float = 0.5

signal health_changed(current: float, max_val: float)
signal hit_taken(damage: float, remaining: float)
signal shove_landed(landing_pos: Vector2)
signal defeated()
signal charge_updated(active_val: float, passive_val: float, max_segments: int)
signal charge_attack_fired(level: int, attack_name: String)
signal spellcard_fired(level: int, spellcard_name: String)
signal panic_bomb_triggered(level: int, spellcard_name: String)
signal intro_glide_completed()
signal damage_escalation_changed(is_escalated: bool)
signal grazed(bullet: Node2D, total_graze: int)

# Damage Escalation & Sudden Death parameters (Touhou 09 authentic pacing)
const DAMAGE_ESCALATION_TIME: float = 30.0
var time_since_last_hit: float = 0.0
var is_sudden_death_active: bool = false
var _last_escalated_state: bool = false

var _bomb_key_was_down: bool = false

# Spell Gauge & Charge parameters
var passive_charge: float = 1.0
var active_charge: float = 0.0
var is_charging: bool = false
var charge_segments: int = 4
var active_charge_speed: float = 1.4
var passive_charge_per_fairy: float = 0.08
var passive_charge_per_great_fairy: float = 0.16
var passive_charge_per_spirit: float = 0.25
var passive_charge_per_cancel: float = 0.02
var charge_attack_name: String = "Charge Attack"
var hits_taken_this_round: int = 0

# Graze parameters (Touhou 19 inspired)
var graze_count: int = 0
@export var graze_radius: float = 64.0
@export var passive_charge_per_graze: float = 0.015
@export var hurtbox_radius: float = 3.0
const GRAZE_SPARK_SCENE: PackedScene = preload("res://scenes/effects/graze_spark.tscn")

@onready var sprite: Sprite2D = %Sprite2D
@onready var hitbox_indicator: HitboxIndicator = %HitboxIndicator
@onready var collision_shape: CollisionShape2D = %CollisionShape2D
@onready var scope_zone: ScopeZone = %ScopeZone if has_node("%ScopeZone") else get_node_or_null("ScopeZone")
@onready var hurtbox: Area2D = %Hurtbox if has_node("%Hurtbox") else get_node_or_null("Hurtbox")
@onready var hurtbox_shape: CollisionShape2D = %HurtboxShape if has_node("%HurtboxShape") else get_node_or_null("Hurtbox/HurtboxShape")
@onready var graze_area: Area2D = %GrazeArea if has_node("%GrazeArea") else get_node_or_null("GrazeArea")
@onready var graze_shape: CollisionShape2D = %GrazeShape if has_node("%GrazeShape") else get_node_or_null("GrazeArea/GrazeShape")
@onready var item_collector: Area2D = %ItemCollector if has_node("%ItemCollector") else get_node_or_null("ItemCollector")
@onready var collector_shape: CollisionShape2D = %CollectorShape if has_node("%CollectorShape") else get_node_or_null("ItemCollector/CollectorShape")

const BASE_PICKUP_RADIUS: float = 24.0
const FOCUS_PICKUP_RADIUS: float = 48.0

const RED_SILHOUETTE_SHADER: Shader = preload("res://shaders/red_silhouette.gdshader")
var _silhouette_material: ShaderMaterial = null

## Toggles the solid red silhouette visual on the player sprite during an Action Stop
func set_red_silhouette(active: bool) -> void:
	if sprite == null:
		sprite = %Sprite2D if has_node("%Sprite2D") else get_node_or_null("Sprite2D")
	if sprite == null:
		return
	if active:
		if _silhouette_material == null:
			_silhouette_material = ShaderMaterial.new()
			_silhouette_material.shader = RED_SILHOUETTE_SHADER
		sprite.material = _silhouette_material
	else:
		if sprite.material == _silhouette_material:
			sprite.material = null

var is_action_stopped: bool = false
var is_round_over: bool = false

## Completely locks player movement and shooting during an Action Stop
func set_action_stop(active: bool) -> void:
	is_action_stopped = active
	if active:
		velocity = Vector2.ZERO
		is_shooting = false
		_burst_remaining = 0

var is_intro_gliding: bool = false
var _intro_tween: Tween = null

## Starts the authentic Touhou 09 corner entrance glide into starting position
func start_intro_glide(target_pos: Vector2, duration: float = 1.2) -> void:
	if _intro_tween and _intro_tween.is_valid():
		_intro_tween.kill()
		_intro_tween = null
	
	is_intro_gliding = true
	is_invulnerable = true
	is_shooting = false
	is_charging = false
	velocity = Vector2.ZERO
	_burst_remaining = 0
	
	# Determine directional banking row based on glide heading
	var moving_right: bool = target_pos.x > position.x
	_anim_row = 2 if moving_right else 1 # 2: Right bank, 1: Left bank
	_anim_frame = 0
	_frame_timer = 0.0
	_update_sprite_frame()
	
	if hitbox_indicator:
		hitbox_indicator.visible = false
	
	_intro_tween = create_tween()
	_intro_tween.set_trans(Tween.TRANS_QUAD)
	_intro_tween.set_ease(Tween.EASE_OUT)
	_intro_tween.tween_property(self, "position", target_pos, duration)
	_intro_tween.finished.connect(_on_intro_glide_finished)

func _on_intro_glide_finished() -> void:
	is_intro_gliding = false
	is_invulnerable = false
	velocity = Vector2.ZERO
	_intro_tween = null
	# Settle back to neutral idle hover
	_anim_row = 0
	_anim_frame = 0
	_frame_timer = 0.0
	_update_sprite_frame()
	intro_glide_completed.emit()

# Animation parameters
var _frame_timer: float = 0.0
var _anim_frame: int = 0
var _anim_row: int = 0 # 0: Idle, 1: Left, 2: Right
const FRAME_DURATION: float = 0.08
const FRAMES_PER_ROW: int = 8

signal bullet_spawned(bullet: PlayerBullet)

const PLAYER_BULLET_SCENE: PackedScene = preload("res://scenes/bullets/player_bullet.tscn")

# Shooting parameters
const DEFAULT_SHOT_COOLDOWN: float = 0.075
var shot_cooldown: float = DEFAULT_SHOT_COOLDOWN
const BURST_COUNT: int = 3
const SHOT_OFFSET_X: float = 14.0
const SHOT_OFFSET_Y: float = -20.0
const BULLET_SPEED: float = 1400.0

var _burst_remaining: int = 0
var _shot_cooldown_timer: float = 0.0
var _shoot_held_time: float = 0.0
const POFV_CHARGE_HOLD_DELAY: float = 0.12

# Playfield Clamping Bounds (600x960 px)
const MIN_X: float = 24.0
const MAX_X: float = 576.0
const MIN_Y: float = 36.0
const MAX_Y: float = 924.0

func _ready() -> void:
	motion_mode = CharacterBody2D.MOTION_MODE_FLOATING
	z_as_relative = false
	z_index = 30
	apply_character_data()
	_update_sprite_frame()
	
	if hurtbox == null:
		hurtbox = get_node_or_null("%Hurtbox") if has_node("%Hurtbox") else get_node_or_null("Hurtbox")
	if hurtbox and not hurtbox.area_entered.is_connected(_on_hurtbox_area_entered):
		hurtbox.area_entered.connect(_on_hurtbox_area_entered)
	if item_collector == null:
		item_collector = get_node_or_null("%ItemCollector") if has_node("%ItemCollector") else get_node_or_null("ItemCollector")
	if item_collector and not item_collector.area_entered.is_connected(_on_item_collector_area_entered):
		item_collector.area_entered.connect(_on_item_collector_area_entered)
	if graze_area == null:
		graze_area = get_node_or_null("%GrazeArea") if has_node("%GrazeArea") else get_node_or_null("GrazeArea")
	if graze_area:
		if not graze_area.area_entered.is_connected(_on_graze_area_entered):
			graze_area.area_entered.connect(_on_graze_area_entered)
		if not graze_area.area_exited.is_connected(_on_graze_area_exited):
			graze_area.area_exited.connect(_on_graze_area_exited)
	_update_hurtbox_state()

func update_networking_mode() -> void:
	_update_hurtbox_state()

func _update_hurtbox_state() -> void:
	if hurtbox == null:
		hurtbox = get_node_or_null("%Hurtbox") if has_node("%Hurtbox") else get_node_or_null("Hurtbox")
	var enable_interaction: bool = (is_local_player or is_ai)
	if hurtbox:
		hurtbox.monitoring = enable_interaction
		hurtbox.monitorable = enable_interaction
	if item_collector == null:
		item_collector = get_node_or_null("%ItemCollector") if has_node("%ItemCollector") else get_node_or_null("ItemCollector")
	if item_collector:
		item_collector.monitoring = enable_interaction
		item_collector.monitorable = false
	if graze_area == null:
		graze_area = get_node_or_null("%GrazeArea") if has_node("%GrazeArea") else get_node_or_null("GrazeArea")
	if graze_area:
		graze_area.monitoring = enable_interaction
		graze_area.monitorable = false

func apply_character_data() -> void:
	if character_data == null and not character_id.is_empty():
		character_data = CharacterData.get_character(character_id)
	
	if character_data:
		if sprite and character_data.sprite_sheet:
			sprite.texture = character_data.sprite_sheet
		normal_speed = character_data.normal_speed
		focus_speed = character_data.focus_speed
		bullet_damage = character_data.bullet_damage
		if character_data.shot_cooldown > 0.0:
			shot_cooldown = character_data.shot_cooldown
		max_health = character_data.max_health
		if starting_health <= 0.0 or starting_health > max_health:
			starting_health = max_health
		current_health = starting_health
		health_changed.emit(current_health, max_health)
		charge_segments = character_data.spell_bar_segments
		passive_charge = character_data.passive_charge_start
		active_charge = 0.0
		hits_taken_this_round = 0
		active_charge_speed = character_data.active_charge_speed
		passive_charge_per_fairy = character_data.passive_charge_per_fairy
		passive_charge_per_great_fairy = character_data.passive_charge_per_great_fairy
		passive_charge_per_spirit = character_data.passive_charge_per_spirit
		passive_charge_per_cancel = character_data.passive_charge_per_cancel
		if character_data.passive_charge_per_graze > 0.0:
			passive_charge_per_graze = character_data.passive_charge_per_graze
		if character_data.hurtbox_radius > 0.0:
			hurtbox_radius = character_data.hurtbox_radius
			if hurtbox_shape == null:
				hurtbox_shape = get_node_or_null("%HurtboxShape") if has_node("%HurtboxShape") else get_node_or_null("Hurtbox/HurtboxShape")
			if hurtbox_shape and hurtbox_shape.shape is CircleShape2D:
				(hurtbox_shape.shape as CircleShape2D).radius = hurtbox_radius
			if collision_shape and collision_shape.shape is CircleShape2D:
				(collision_shape.shape as CircleShape2D).radius = hurtbox_radius
		if character_data.graze_radius > 0.0:
			graze_radius = character_data.graze_radius
			if graze_shape == null:
				graze_shape = get_node_or_null("%GrazeShape") if has_node("%GrazeShape") else get_node_or_null("GrazeArea/GrazeShape")
			if graze_shape and graze_shape.shape is CircleShape2D:
				(graze_shape.shape as CircleShape2D).radius = graze_radius
			if hitbox_indicator and "graze_radius" in hitbox_indicator:
				hitbox_indicator.graze_radius = graze_radius
		charge_attack_name = character_data.charge_attack_name
		charge_updated.emit(active_charge, passive_charge, charge_segments)
		if scope_zone == null:
			scope_zone = get_node_or_null("%ScopeZone") if has_node("%ScopeZone") else get_node_or_null("ScopeZone")
		if scope_zone:
			scope_zone.apply_character_data(character_data)

func set_ai_mode(enabled: bool, shooting_enabled: bool = true) -> void:
	is_ai = enabled
	if is_ai:
		if ai_controller == null:
			ai_controller = RudimentaryAI.new()
			add_child(ai_controller)
			if playfield == null:
				playfield = _find_playfield()
			ai_controller.setup(self, playfield)
		ai_controller.set_shooting_enabled(shooting_enabled)
	else:
		if ai_controller:
			ai_controller.reset()

func _find_playfield() -> Node2D:
	var cur: Node = get_parent()
	while cur != null:
		if cur is Playfield:
			return cur as Node2D
		cur = cur.get_parent()
	return null

func reset_for_round(start_pos: Vector2, preserve_gauge: bool = true) -> void:
	if _shove_tween and _shove_tween.is_valid():
		_shove_tween.kill()
		_shove_tween = null
	if _intro_tween and _intro_tween.is_valid():
		_intro_tween.kill()
		_intro_tween = null
	is_intro_gliding = false
	
	current_health = starting_health
	is_dead = false
	is_shoved = false
	is_invulnerable = false
	is_action_stopped = false
	is_round_over = false
	_invulnerability_timer = 0.0
	_shove_flash_timer = 0.0
	_red_blink_timer = 0.0
	_burst_remaining = 0
	_shot_cooldown_timer = 0.0
	_shoot_held_time = 0.0
	_bomb_key_was_down = false
	velocity = Vector2.ZERO
	position = start_pos
	
	if not preserve_gauge:
		if character_data:
			passive_charge = character_data.passive_charge_start
		else:
			passive_charge = 1.0
	hits_taken_this_round = 0
	graze_count = 0
	time_since_last_hit = 0.0
	is_sudden_death_active = false
	_last_escalated_state = false
	active_charge = 0.0
	is_charging = false
	charge_updated.emit(active_charge, passive_charge, charge_segments)
	
	if ai_controller:
		ai_controller.reset()
	
	# Reset animation state to neutral idle
	_anim_row = 0
	_anim_frame = 0
	_frame_timer = 0.0
	_update_sprite_frame()
	
	if sprite:
		# Kill any tweens running on sprite (such as defeat fade)
		var tw_cancel := create_tween()
		tw_cancel.kill()
		set_red_silhouette(false)
		sprite.modulate = Color.WHITE
		sprite.visible = true
	
	if hitbox_indicator:
		hitbox_indicator.visible = false
	
	health_changed.emit(current_health, max_health)

var is_shooting: bool = false
var is_focusing: bool = false
var _remote_target_position: Vector2 = Vector2.ZERO
var _has_remote_target: bool = false

func _is_networked_match() -> bool:
	var net_mgr = get_node_or_null("/root/NetworkManager")
	return net_mgr != null and net_mgr.is_network_active()

func _is_pofv_controls() -> bool:
	var gm = get_node_or_null("/root/GameManager")
	if gm != null and gm.has_method("is_pofv_controls"):
		return gm.is_pofv_controls()
	return false

func _physics_process(delta: float) -> void:
	if is_dead or is_action_stopped:
		velocity = Vector2.ZERO
		return
	
	if is_intro_gliding:
		velocity = Vector2.ZERO
		is_shooting = false
		_frame_timer += delta
		if _frame_timer >= FRAME_DURATION:
			_frame_timer -= FRAME_DURATION
			if _anim_frame < FRAMES_PER_ROW - 1:
				_anim_frame += 1
		_update_sprite_frame()
		return
	
	if is_shoved:
		_shove_flash_timer += delta
		# Flash transparent rapidly while being shoved
		var flash_cycle: int = int(_shove_flash_timer * 16.0)
		if sprite:
			sprite.modulate = Color(1.0, 1.0, 1.0, 0.2 if (flash_cycle % 2 == 0) else 0.85)
	else:
		time_since_last_hit += delta
		var currently_escalated := is_damage_escalated()
		if currently_escalated != _last_escalated_state:
			_last_escalated_state = currently_escalated
			damage_escalation_changed.emit(currently_escalated)
		
		if _invulnerability_timer > 0.0:
			_invulnerability_timer -= delta
			if _invulnerability_timer <= 0.0:
				is_invulnerable = false
		
		if _red_blink_timer > 0.0:
			_red_blink_timer -= delta
			# Blink red 4 times over 0.5 seconds (each blink cycle is 0.125s)
			var elapsed: float = RED_BLINK_TOTAL_TIME - _red_blink_timer
			var cycle_pos: float = fmod(elapsed, 0.125)
			var is_red: bool = (cycle_pos < 0.0625) and (_red_blink_timer > 0.0)
			if sprite:
				sprite.modulate = Color(1.0, 0.25, 0.25, 1.0) if is_red else Color.WHITE
			if _red_blink_timer <= 0.0 and sprite:
				sprite.modulate = Color.WHITE
		else:
			if sprite and sprite.modulate != Color.WHITE:
				sprite.modulate = Color.WHITE
	
	if is_local_player or is_ai:
		_update_shoot_held_timer(delta)
		if not is_shoved:
			_handle_movement(delta)
		_handle_charging(delta)
		_handle_panic_bomb()
		if not is_shoved and not is_charging:
			_handle_shooting(delta)
		else:
			is_shooting = false
		_update_animation(delta)
	else:
		# Remote puppet: interpolate position towards remote updates
		if _shot_cooldown_timer > 0.0:
			_shot_cooldown_timer -= delta
		if _remote_target_position != Vector2.ZERO:
			if position.distance_squared_to(_remote_target_position) > 0.1:
				if position.distance_to(_remote_target_position) > 100.0:
					position = _remote_target_position
				else:
					position = position.lerp(_remote_target_position, 0.45)
		_update_animation(delta)
	_update_pickup_collection(delta)

func _update_shoot_held_timer(delta: float) -> void:
	var shoot_held: bool = false
	var is_net := _is_networked_match()
	var is_p2_local_controls := (player_number == 2 and not is_net)
	if is_ai and ai_controller:
		shoot_held = ai_controller.wants_to_shoot()
	elif is_p2_local_controls:
		shoot_held = Input.is_action_pressed("p2_shoot")
	else:
		shoot_held = Input.is_action_pressed("shoot")
	
	if shoot_held:
		_shoot_held_time += delta
	else:
		_shoot_held_time = 0.0

func _handle_movement(delta: float) -> void:
	var input_vector := Vector2.ZERO
	var is_focus := false
	var is_net := _is_networked_match()
	var is_p2_local_controls := (player_number == 2 and not is_net)
	var is_pofv := _is_pofv_controls()
	
	if is_ai and ai_controller:
		ai_controller.update_ai(delta)
		input_vector = ai_controller.get_movement_vector()
		is_focus = ai_controller.is_focusing()
	elif is_p2_local_controls:
		# P2 numpad controls (4: left, 6: right, 8: up, 2: down, 0/5: focus)
		input_vector = Input.get_vector("p2_left", "p2_right", "p2_up", "p2_down")
		# Direct key polling fallback (handles NumLock on/off)
		if input_vector == Vector2.ZERO:
			var x := 0.0
			var y := 0.0
			if Input.is_physical_key_pressed(KEY_KP_4): x -= 1.0
			if Input.is_physical_key_pressed(KEY_KP_6): x += 1.0
			if Input.is_physical_key_pressed(KEY_KP_8): y -= 1.0
			if Input.is_physical_key_pressed(KEY_KP_2): y += 1.0
			input_vector = Vector2(x, y).normalized()
		is_focus = Input.is_action_pressed("p2_focus") or Input.is_physical_key_pressed(KEY_KP_0) or Input.is_physical_key_pressed(KEY_KP_5)
	else:
		# P1 standard controls (Arrow keys / WASD, Shift for focus)
		input_vector = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
		is_focus = Input.is_action_pressed("focus")
	
	is_focusing = is_focus
	if hitbox_indicator:
		hitbox_indicator.visible = is_focus
	if scope_zone == null:
		scope_zone = get_node_or_null("%ScopeZone") if has_node("%ScopeZone") else get_node_or_null("ScopeZone")
	if scope_zone:
		scope_zone.set_focusing(is_focus)
		scope_zone.set_aim_direction(input_vector)

	var current_speed := focus_speed if (is_focus or (is_pofv and is_charging)) else normal_speed
	velocity = input_vector * current_speed
	move_and_slide()
	
	# Clamp position inside the playfield boundaries
	global_position.x = clampf(global_position.x, MIN_X, MAX_X)
	global_position.y = clampf(global_position.y, MIN_Y, MAX_Y)

func _update_animation(delta: float) -> void:
	var target_row: int = 0
	if velocity.x < -10.0:
		target_row = 1 # Left
	elif velocity.x > 10.0:
		target_row = 2 # Right
	else:
		target_row = 0 # Idle
	
	if target_row != _anim_row:
		_anim_row = target_row
		_anim_frame = 0
		_frame_timer = 0.0
	
	_frame_timer += delta
	if _frame_timer >= FRAME_DURATION:
		_frame_timer -= FRAME_DURATION
		if _anim_row == 0:
			# Idle animation loops continuously
			_anim_frame = (_anim_frame + 1) % FRAMES_PER_ROW
		else:
			# Side-to-side banking: advances to the last frame and hangs there while holding direction
			if _anim_frame < FRAMES_PER_ROW - 1:
				_anim_frame += 1
	
	_update_sprite_frame()

func _update_sprite_frame() -> void:
	if sprite:
		sprite.frame = _anim_row * FRAMES_PER_ROW + _anim_frame

func _handle_shooting(delta: float) -> void:
	if is_dead or is_round_over:
		is_shooting = false
		_burst_remaining = 0
		return
	
	if _shot_cooldown_timer > 0.0:
		_shot_cooldown_timer -= delta
	
	var shoot_just_pressed: bool = false
	var shoot_held: bool = false
	var is_net := _is_networked_match()
	var is_p2_local_controls := (player_number == 2 and not is_net)
	var is_pofv := _is_pofv_controls()
	
	if is_ai and ai_controller:
		shoot_held = ai_controller.wants_to_shoot()
		shoot_just_pressed = shoot_held
	elif is_p2_local_controls:
		shoot_just_pressed = Input.is_action_just_pressed("p2_shoot")
		shoot_held = Input.is_action_pressed("p2_shoot")
	else:
		shoot_just_pressed = Input.is_action_just_pressed("shoot")
		shoot_held = Input.is_action_pressed("shoot")
	
	if shoot_just_pressed:
		_burst_remaining = BURST_COUNT
	
	var wants_to_fire: bool = false
	if is_pofv:
		# PoFV style: Mashing / tapping fires bursts; holding does NOT auto-fire indefinitely
		wants_to_fire = (_burst_remaining > 0)
	else:
		# UDoALG style: Holding fires continuous stream
		wants_to_fire = shoot_held or (_burst_remaining > 0)
	
	is_shooting = wants_to_fire
	if wants_to_fire and _shot_cooldown_timer <= 0.0:
		_fire_volley()
		_shot_cooldown_timer = shot_cooldown
		if _burst_remaining > 0:
			_burst_remaining -= 1

func _fire_volley() -> void:
	if character_id == "yuuka":
		_fire_yuuka_volley()
		AudioService.play_player_shot()
		return
	var left_pos := Vector2(position.x - SHOT_OFFSET_X, position.y + SHOT_OFFSET_Y)
	var right_pos := Vector2(position.x + SHOT_OFFSET_X, position.y + SHOT_OFFSET_Y)
	_spawn_bullet(left_pos)
	_spawn_bullet(right_pos)
	AudioService.play_player_shot()

func _fire_yuuka_volley() -> void:
	# Authentic PoFV Yuuka 3-bullet fan spread (-100 deg, -90 deg, -80 deg from pl09.sht):
	# Center straight up, side petals fanned by 10 degrees (tightens to 5 degrees when focusing)
	# pl09.sht gives the side petals damage 8 against the centre's 10
	var spread_deg: float = 5.0 if is_focusing else 10.0
	var left_dir := Vector2.from_angle(deg_to_rad(-90.0 - spread_deg))
	var center_dir := Vector2.UP
	var right_dir := Vector2.from_angle(deg_to_rad(-90.0 + spread_deg))
	
	var left_pos := Vector2(position.x - SHOT_OFFSET_X, position.y + SHOT_OFFSET_Y)
	var center_pos := Vector2(position.x, position.y + SHOT_OFFSET_Y - 2.0)
	var right_pos := Vector2(position.x + SHOT_OFFSET_X, position.y + SHOT_OFFSET_Y)
	
	_spawn_bullet(left_pos, left_dir, 0.8)
	_spawn_bullet(center_pos, center_dir)
	_spawn_bullet(right_pos, right_dir, 0.8)

func _spawn_bullet(spawn_pos: Vector2, shot_dir: Vector2 = Vector2.UP, damage_scale: float = 1.0) -> void:
	var bullet: PlayerBullet = PLAYER_BULLET_SCENE.instantiate()
	bullet.direction = shot_dir
	bullet.rotation = shot_dir.angle() + PI / 2.0
	if character_data:
		bullet.setup_with_data(character_data, spawn_pos)
	else:
		bullet.setup(character_id, spawn_pos, BULLET_SPEED, bullet_damage)
	bullet.damage *= damage_scale
	bullet_spawned.emit(bullet)

func add_passive_charge(amount: float) -> void:
	if charge_segments <= 0 or is_dead or is_round_over:
		return
	passive_charge = clampf(passive_charge + amount, 0.0, float(charge_segments))
	charge_updated.emit(active_charge, passive_charge, charge_segments)

func _handle_charging(delta: float) -> void:
	if is_dead or is_round_over:
		if is_charging or active_charge > 0.0:
			is_charging = false
			active_charge = 0.0
			charge_updated.emit(active_charge, passive_charge, charge_segments)
		return
	
	var wants_charge: bool = false
	var is_net := _is_networked_match()
	var is_p2_local_controls := (player_number == 2 and not is_net)
	var is_pofv := _is_pofv_controls()
	
	if is_ai and ai_controller:
		wants_charge = ai_controller.has_method("wants_to_charge") and ai_controller.wants_to_charge()
	elif is_p2_local_controls:
		if is_pofv:
			var shoot_held := Input.is_action_pressed("p2_shoot")
			wants_charge = shoot_held and (_shoot_held_time >= POFV_CHARGE_HOLD_DELAY)
		else:
			wants_charge = Input.is_action_pressed("p2_charge") or Input.is_physical_key_pressed(KEY_KP_PERIOD) or Input.is_physical_key_pressed(KEY_PERIOD) or Input.is_physical_key_pressed(KEY_KP_1)
	else:
		if is_pofv:
			var shoot_held := Input.is_action_pressed("shoot")
			wants_charge = shoot_held and (_shoot_held_time >= POFV_CHARGE_HOLD_DELAY)
		else:
			wants_charge = Input.is_action_pressed("charge") or Input.is_physical_key_pressed(KEY_X)
	
	if wants_charge and charge_segments > 0 and passive_charge >= 1.0:
		if not is_charging:
			AudioService.play_charge_start()
		is_charging = true
		var prev_lvl: int = int(floor(active_charge))
		var max_allowed: float = minf(passive_charge, float(charge_segments))
		if active_charge < max_allowed:
			active_charge = minf(active_charge + active_charge_speed * delta, max_allowed)
		var new_lvl: int = int(floor(active_charge))
		if new_lvl > prev_lvl and new_lvl >= 1:
			AudioService.play_charge_threshold()
		charge_updated.emit(active_charge, passive_charge, charge_segments)
	else:
		if is_charging:
			_release_charge()
			is_charging = false

func apply_remote_state(target_pos: Vector2, anim_r: int, focus: bool, charging: bool, shooting: bool, act_chg: float, pass_chg: float) -> void:
	# No raw input vector travels over the network, so approximate the remote
	# player's movement direction from how far the new target moved them.
	var remote_move_dir := (target_pos - _remote_target_position) if _has_remote_target else Vector2.ZERO
	_remote_target_position = target_pos
	_has_remote_target = true
	is_focusing = focus
	if hitbox_indicator:
		hitbox_indicator.visible = focus
	if scope_zone == null:
		scope_zone = get_node_or_null("%ScopeZone") if has_node("%ScopeZone") else get_node_or_null("ScopeZone")
	if scope_zone:
		scope_zone.set_focusing(focus)
		scope_zone.set_aim_direction(remote_move_dir)
	
	_anim_row = anim_r
	_update_sprite_frame()
	
	var was_charging := is_charging
	is_charging = charging
	active_charge = act_chg
	passive_charge = pass_chg
	if charging and not was_charging and charge_segments > 0:
		AudioService.play_charge_start()
	charge_updated.emit(active_charge, passive_charge, charge_segments)
	
	is_shooting = shooting
	if shooting:
		if _shot_cooldown_timer <= 0.0:
			_fire_volley()
			_shot_cooldown_timer = shot_cooldown * 0.95
	else:
		_shot_cooldown_timer = 0.0

func _release_charge() -> void:
	if active_charge >= 1.0:
		var reached_level: int = mini(int(floor(active_charge)), charge_segments)
		if reached_level == 1:
			# Level 1 Charge Attack: 0 passive segments consumed
			charge_attack_fired.emit(1, charge_attack_name)
		elif reached_level >= 2:
			# Level 2 consumes 1 segment, Level 3 consumes 2 segments, Level 4 consumes 3 segments
			var segments_to_consume: float = float(reached_level - 1)
			passive_charge = maxf(1.0, passive_charge - segments_to_consume)
			spellcard_fired.emit(reached_level, "%s - Level %d Spellcard" % [character_id.to_upper(), reached_level])
	
	active_charge = 0.0
	charge_updated.emit(active_charge, passive_charge, charge_segments)

func _handle_panic_bomb() -> void:
	if is_dead or is_round_over or is_action_stopped or is_shoved or is_intro_gliding:
		_bomb_key_was_down = false
		return
	
	var bomb_down: bool = false
	var is_net := _is_networked_match()
	var is_p2_local_controls := (player_number == 2 and not is_net)
	var is_pofv := _is_pofv_controls()
	
	if is_ai and ai_controller:
		bomb_down = ai_controller.has_method("wants_to_bomb") and ai_controller.wants_to_bomb()
	elif is_p2_local_controls:
		if is_pofv:
			bomb_down = Input.is_action_pressed("p2_charge") or Input.is_physical_key_pressed(KEY_KP_1) or Input.is_physical_key_pressed(KEY_KP_PERIOD) or Input.is_physical_key_pressed(KEY_PERIOD) or Input.is_action_pressed("p2_bomb") or Input.is_physical_key_pressed(KEY_KP_3)
		else:
			bomb_down = Input.is_action_pressed("p2_bomb") or Input.is_physical_key_pressed(KEY_KP_3) or Input.is_physical_key_pressed(KEY_COMMA)
	else:
		if is_pofv:
			bomb_down = Input.is_action_pressed("charge") or Input.is_physical_key_pressed(KEY_X) or Input.is_action_pressed("bomb") or Input.is_physical_key_pressed(KEY_C)
		else:
			bomb_down = Input.is_action_pressed("bomb") or Input.is_physical_key_pressed(KEY_C)
	
	var bomb_just_pressed: bool = bomb_down and not _bomb_key_was_down
	_bomb_key_was_down = bomb_down
	
	if bomb_just_pressed:
		trigger_panic_bomb()

## Triggers an authentic Touhou 09 "Panic Bomb" spellcard:
## If player has enough passive charge for a spellcard (Level 2, 3, or 4),
## instantly executes that spellcard and zeros the passive bar (including segment 1).
func trigger_panic_bomb() -> bool:
	if is_dead or is_round_over or is_action_stopped or is_shoved or is_intro_gliding:
		return false
	if charge_segments <= 0:
		return false
	
	# Requires at least Level 2 charge (the threshold for spellcards)
	if passive_charge < 2.0:
		return false
	
	var reached_level: int = mini(int(floor(passive_charge)), charge_segments)
	if reached_level < 2:
		return false
	
	# Consumes entire passive bar to 0 (including the first baseline segment)
	passive_charge = 0.0
	active_charge = 0.0
	is_charging = false
	charge_updated.emit(active_charge, passive_charge, charge_segments)
	
	var spell_name: String = "%s - Level %d Spellcard" % [character_id.to_upper(), reached_level]
	panic_bomb_triggered.emit(reached_level, spell_name)
	spellcard_fired.emit(reached_level, spell_name)
	return true

func set_health(val: float) -> void:
	current_health = clampf(val, 0.0, starting_health)
	health_changed.emit(current_health, max_health)
	if current_health <= 0.0 and not is_dead:
		_on_defeat()

func insta_kill() -> void:
	if is_dead:
		return
	current_health = 0.0
	health_changed.emit(current_health, max_health)
	AudioService.play_player_hit(true, false)
	_on_defeat()

func set_passive_charge(val: float) -> void:
	passive_charge = clampf(val, 0.0, float(charge_segments))
	charge_updated.emit(active_charge, passive_charge, charge_segments)

## Returns true if the player is currently under escalated damage (loss of 2 orbs / 1.0 HP per hit)
func is_damage_escalated() -> bool:
	return is_sudden_death_active or time_since_last_hit >= DAMAGE_ESCALATION_TIME

## Returns the multiplier applied to incoming hazard damage (1.0x normal, 2.0x escalated)
func get_damage_multiplier() -> float:
	return 2.0 if is_damage_escalated() else 1.0

## Computes the effective damage for incoming hazards based on escalation state
func calculate_incoming_damage(base_dmg: float) -> float:
	return base_dmg * get_damage_multiplier()

func take_damage(amount: float, source_hazard: Node2D = null) -> void:
	# Client-authoritative rule: remote puppets NEVER take local damage
	if not is_local_player and not is_ai:
		return
	if is_dead or is_invulnerable or is_shoved or god_mode:
		return
	
	# Taking a hit resets the survival escalation timer
	time_since_last_hit = 0.0
	var now_escalated := is_damage_escalated()
	if now_escalated != _last_escalated_state:
		_last_escalated_state = now_escalated
		damage_escalation_changed.emit(now_escalated)
	
	# Touhou 09 Guts / Last Chance rule:
	# If player currently has > 0.5 hearts, they cannot be one-shot killed;
	# they are spared and survive on 0.5 hearts!
	var prev_health: float = current_health
	var final_health: float = current_health - amount
	var triggered_guts: bool = false
	if current_health > 0.5 and final_health <= 0.0:
		current_health = 0.5
		triggered_guts = true
	else:
		current_health = maxf(0.0, final_health)
	
	hit_taken.emit(amount, current_health)
	health_changed.emit(current_health, max_health)
	
	if current_health <= 0.0:
		AudioService.play_player_hit(true, false)
		_on_defeat()
		return
	
	# Passive spell bar charge on hit:
	# First hit of the round gives half a segment (0.5), subsequent hits give a full segment (1.0).
	var charge_gain: float = 0.5 if hits_taken_this_round == 0 else 1.0
	hits_taken_this_round += 1
	add_passive_charge(charge_gain)
	
	if triggered_guts or (prev_health > 0.5 and current_health <= 0.5):
		AudioService.play_player_hit(false, true)
	else:
		AudioService.play_player_hit(false, false)
	
	# Non-lethal hit: initiate Hit Shove knockback
	_start_hit_shove(source_hazard)

func _start_hit_shove(source_hazard: Node2D = null) -> void:
	is_shoved = true
	is_invulnerable = true
	_shove_flash_timer = 0.0
	_red_blink_timer = 0.0
	
	var shove_dir := Vector2.ZERO
	if source_hazard and is_instance_valid(source_hazard):
		shove_dir = (global_position - source_hazard.global_position).normalized()
	
	# If no clear hazard direction, or straight downward, bias upward into the playfield
	if shove_dir == Vector2.ZERO or shove_dir.y > 0.6:
		var angle: float = randf_range(-PI * 0.8, -PI * 0.2)
		shove_dir = Vector2(cos(angle), sin(angle))
	else:
		shove_dir = shove_dir.rotated(randf_range(-0.35, 0.35)).normalized()
	
	var shove_dist: float = randf_range(180.0, 240.0)
	var target_pos: Vector2 = global_position + shove_dir * shove_dist
	target_pos.x = clampf(target_pos.x, MIN_X, MAX_X)
	target_pos.y = clampf(target_pos.y, MIN_Y, MAX_Y)
	
	if _shove_tween and _shove_tween.is_valid():
		_shove_tween.kill()
	
	_shove_tween = create_tween()
	# Easy-out deceleration: rapid initial shove settling smoothly at destination
	_shove_tween.tween_property(self, "global_position", target_pos, shove_duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	_shove_tween.tween_callback(func():
		is_shoved = false
		_invulnerability_timer = INVULNERABILITY_POST_SHOVE
		_red_blink_timer = RED_BLINK_TOTAL_TIME
		shove_landed.emit(global_position)
	)

func _on_defeat() -> void:
	is_dead = true
	is_invulnerable = true
	is_shoved = false
	is_action_stopped = false
	_red_blink_timer = 0.0
	_invulnerability_timer = 0.0
	
	if is_charging or active_charge > 0.0:
		is_charging = false
		active_charge = 0.0
		charge_updated.emit(active_charge, passive_charge, charge_segments)
	
	if _shove_tween and _shove_tween.is_valid():
		_shove_tween.kill()
		_shove_tween = null
	velocity = Vector2.ZERO
	if hitbox_indicator:
		hitbox_indicator.visible = false
	if sprite:
		set_red_silhouette(false)
		sprite.modulate = Color.WHITE
	defeated.emit()

## Called after the 0.5s defeat hitstop freeze: completely hides loser's sprite
func hide_on_defeat() -> void:
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D") if has_node("%Sprite2D") else get_node_or_null("Sprite2D")
	if sprite:
		sprite.visible = false
		sprite.modulate.a = 0.0
	if hitbox_indicator:
		hitbox_indicator.visible = false

## Called when remote opponent takes damage on their client, to trigger visual hit flash / red blink
func apply_remote_damage_visuals(remaining_hp: float) -> void:
	var prev_health: float = current_health
	current_health = remaining_hp
	health_changed.emit(current_health, max_health)
	_red_blink_timer = RED_BLINK_TOTAL_TIME
	if sprite:
		sprite.modulate = Color(1.0, 0.25, 0.25, 1.0)
	if remaining_hp <= 0.0:
		AudioService.play_player_hit(true, false)
	elif prev_health > 0.5 and remaining_hp <= 0.5:
		AudioService.play_player_hit(false, true)
	else:
		AudioService.play_player_hit(false, false)

func _on_hurtbox_area_entered(area: Area2D) -> void:
	# Client-authoritative rule: remote puppets NEVER register local hazard collisions
	if not is_local_player and not is_ai:
		return
	if is_dead or is_invulnerable or is_shoved or not is_instance_valid(area):
		return
	
	var base_dmg: float = 1.0
	var should_despawn: bool = false
	var source: Node2D = area
	
	if area is SakuyaExtraKnife:
		# Handles its own damage and despawn via body_entered/_has_crossed
		# (harmless to the sender until it crosses to the opponent's field) -
		# the generic hurtbox path must stay out of it or it double-hits/self-hits.
		return
	elif area is YuukaExtraFlower:
		# Same reason: it deals its own damage (only once it has formed) and folds shut.
		return
	elif area is EnemyPellet:
		base_dmg = area.get_damage() if area.has_method("get_damage") else 1.0
		should_despawn = true
	elif area is DanmakuBullet:
		base_dmg = area.damage
		should_despawn = true
	elif area is ReisenMoonBlast:
		base_dmg = area.damage
	elif area is ClownpieceMoon:
		base_dmg = area.damage
	elif area is YinYangOrb:
		base_dmg = 1.5
	elif area.get_parent() is EarthLightRay:
		var ray: EarthLightRay = area.get_parent() as EarthLightRay
		if ray and ray.is_firing:
			base_dmg = ray.damage
			source = ray
		else:
			return
	elif area is Fairy:
		base_dmg = 1.0
	elif area is Spirit:
		base_dmg = 1.0
	elif area is YoumuDarkSpirit:
		base_dmg = 1.0
	
	var effective_damage: float = calculate_incoming_damage(base_dmg)
	take_damage(effective_damage, source)
	if should_despawn and is_instance_valid(area):
		area.despawn()

func _update_pickup_collection(delta: float) -> void:
	if item_collector == null or not is_instance_valid(item_collector):
		return
	if collector_shape and collector_shape.shape is CircleShape2D:
		var target_radius: float = FOCUS_PICKUP_RADIUS if is_focusing else BASE_PICKUP_RADIUS
		if (collector_shape.shape as CircleShape2D).radius != target_radius:
			(collector_shape.shape as CircleShape2D).radius = target_radius
	
	if is_focusing and (is_local_player or is_ai):
		for area in item_collector.get_overlapping_areas():
			if area is PickupItem and not area.is_collected:
				area.attract_towards(global_position, 280.0, delta)

func _on_item_collector_area_entered(area: Area2D) -> void:
	if not (is_local_player or is_ai) or is_dead:
		return
	if area is PickupItem and not area.is_collected:
		area.collect(self)

func _on_graze_area_entered(area: Area2D) -> void:
	# Client-authoritative rule: remote puppets NEVER process local hazard interactions
	if not is_local_player and not is_ai:
		return
	if is_dead or is_invulnerable or is_shoved or not is_instance_valid(area):
		return
	
	# Only genuine enemy bullets and pellets contribute to grazing
	if not (area is DanmakuBullet or area is EnemyPellet):
		return
	
	if area.has_method("on_graze_enter"):
		area.on_graze_enter()
	
	if not area.get("has_been_grazed"):
		area.set("has_been_grazed", true)
		graze_count += 1
		add_passive_charge(passive_charge_per_graze)
		AudioService.play_graze()
		_spawn_graze_spark(area.global_position)
		grazed.emit(area, graze_count)

func _on_graze_area_exited(area: Area2D) -> void:
	if is_instance_valid(area) and area.has_method("on_graze_exit"):
		area.on_graze_exit()

func _spawn_graze_spark(pos: Vector2) -> void:
	var spark := GRAZE_SPARK_SCENE.instantiate() as GrazeSpark
	if spark == null:
		return
	if playfield != null and is_instance_valid(playfield) and "effects_layer" in playfield and playfield.effects_layer != null:
		playfield.effects_layer.add_child(spark)
	elif get_parent() != null:
		get_parent().add_child(spark)
	else:
		add_child(spark)
	spark.setup(pos)
