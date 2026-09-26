class_name DanmakuBullet
extends Area2D

enum MotionMode {
	LINEAR,
	DECEL_AND_HOME,
	EXPANDING_ORBIT,
	LUNAR_WAVE,
	CURVE_THEN_LINE,
	ELLIPSE_THEN_LINE
}

enum DecelState {
	DECELERATING,
	PAUSED_LOCKING,
	LAUNCHED
}

@export var motion_mode: MotionMode = MotionMode.LINEAR
@export var speed: float = 240.0
@export var acceleration: float = 0.0
@export var max_speed: float = 0.0
@export var direction: Vector2 = Vector2.DOWN
@export var damage: float = 1.0
@export var can_be_canceled: bool = true
## Continuous sprite rotation in radians/second
@export var spin_speed: float = 0.0
## Maximum lifespan in seconds before automatically despawning
@export var lifetime: float = 7.0

## Safety boundary beyond visible arena to prevent runaway flight while allowing offscreen spawns
const SAFETY_MIN_X: float = -350.0
const SAFETY_MAX_X: float = 950.0
const SAFETY_MIN_Y: float = -350.0
const SAFETY_MAX_Y: float = 1350.0

## Actual playfield edges (as opposed to the generous SAFETY_* margins above) used only for
## bounce_off_walls - Sakuya's "Reflective Knife Explosion" ricochets off these edges exactly,
## rather than drifting deep offscreen first like a normal despawn check would allow.
const PLAYFIELD_EDGE_LEFT: float = 0.0
const PLAYFIELD_EDGE_RIGHT: float = 600.0
const PLAYFIELD_EDGE_TOP: float = 0.0

static var _shape_cache: Dictionary = {}

static func get_cached_circle_shape(radius: float) -> CircleShape2D:
	if not _shape_cache.has(radius):
		var circle := CircleShape2D.new()
		circle.radius = radius
		_shape_cache[radius] = circle
	return _shape_cache[radius]

static func get_z_index_for_texture(_tex: Texture2D = null) -> int:
	return 10


var pool: NodePool = null
var is_canceled: bool = false
var bullet_data: DanmakuBulletData = null
var _current_lifetime: float = 0.0

# Freeze-and-scatter trigger (Cirno "Perfect Freeze"): when set on a leader bullet expanding
# via EXPANDING_ORBIT, crossing this radius fires a one-shot playfield-wide freeze sweep.
var freeze_trigger_radius: float = -1.0
var freeze_accel: float = 0.0
var freeze_max_speed: float = 0.0
var freeze_duration: float = 1.0
var _freeze_timer: float = 0.0
var _pending_accel: float = 0.0
var owner_playfield: Node2D = null

# Periodic pellet-spawn trail (Sakuya's "Pellet-Spewing Dagger", Lv4 Boss Attack): while this
# bullet is alive, every pellet_spawn_interval seconds it spawns pellet_spawn_count pellets
# scattered within pellet_spawn_radius of its own current position, each picking a random
# direction and accelerating from rest to pellet_spawn_max_speed over pellet_spawn_accel_time
# seconds - the same random-direction accel recipe freeze_and_scatter() applies to an existing
# bullet, just spawned fresh repeatedly instead. Disabled by default (interval <= 0).
var pellet_spawn_interval: float = -1.0
var pellet_spawn_data: DanmakuBulletData = null
var pellet_spawn_count: int = 1
var pellet_spawn_radius: float = 0.0
var pellet_spawn_accel_time: float = 1.0
var pellet_spawn_max_speed: float = 0.0
## pl02.ecl sub8 emitter shape: stop after this many bursts (-1 = while alive), place pellets
## on (not within) a radius that shrinks by pellet_spawn_radius_decay per burst, and give each
## its own top speed within +-pellet_spawn_speed_jitter
var pellet_spawn_remaining: int = -1
var pellet_spawn_on_circle: bool = false
var pellet_spawn_radius_decay: float = 1.0
var pellet_spawn_speed_jitter: float = 0.0
var _pellet_spawn_timer: float = 0.0

# Wall-bounce (Sakuya's "Reflective Knife Explosion", Lv4 Boss Attack 2): when enabled, a
# LINEAR or DECEL_AND_HOME bullet (Yuuka's boss ring balls, pl09.ecl sub3) ricochets off the left/right/top playfield edges (mirroring the velocity
# component perpendicular to whichever edge it crossed) instead of despawning there - but never
# off the bottom/south edge, so it can still exit freely past the player. The bounce swaps to
# bounce_bullet_data (e.g. purple -> blue) and then clears bounce_off_walls - it only ever
# bounces once; the blue dagger it becomes flies straight afterward, exiting normally at any edge.
# bounce_overshoot lets the bullet travel that many extra pixels past the edge (briefly offscreen)
# before it reflects, instead of ricocheting exactly at the boundary line.
var bounce_off_walls: bool = false
var bounce_bullet_data: DanmakuBulletData = null
var bounce_overshoot: float = 0.0

# Repelled by a player (Clownpiece's "Infernal Essence of Grazing", TH15 ex flag 0x200000):
# while repel_time_left runs, a bullet inside repel_radius of repel_source is pushed straight
# away from it at up to repel_speed, never past the radius. Bullets flying at the player
# therefore stall on the edge in a tidy arc until the time runs out and they carry on.
var repel_source: Node2D = null
var repel_radius: float = 0.0
var repel_speed: float = 0.0
var repel_time_left: float = 0.0

## Index into bullet_data.anim_frames currently on the sprite, -1 before the first swap.
var _anim_index: int = -1

# Graze tracking & Touhou 19 proximity feedback
var has_been_grazed: bool = false
var is_in_graze_field: bool = false
var _graze_tint_active: bool = false
var _original_modulate: Color = Color.WHITE
var _original_scale: Vector2 = Vector2.ONE

# Decel & Home phase parameters
var initial_speed: float = 220.0
var decel_time: float = 0.45
var pause_time: float = 0.18
var launch_speed: float = 300.0
var target_player: Node2D = null
var homing_stages: int = 1
var stage1_flight_duration: float = 0.5
var homing_sfx: String = ""
## When > 0, every stage after the first brakes over this long and relaunches at
## stage2_launch_speed instead (ZUN chains two AimPlayer effects with their own numbers)
var stage2_decel_time: float = 0.0
var stage2_launch_speed: float = 0.0
## ZUN's AimRel: at relaunch, turn by relaunch_turn from the current heading instead of
## aiming at the player (Reisen's Lunar Wave turns +-120 degrees and doubles back)
var relaunch_relative: bool = false
var relaunch_turn: float = 0.0

# Expanding Orbit parameters
var orbit_center: Vector2 = Vector2.ZERO
var current_radius: float = 0.0
var current_angle: float = 0.0
var radial_speed: float = 0.0
var angular_speed: float = 0.0

# Expanding Orbit "breakout" parameters (Cirno's "Interweaving Icicles"): after
# orbit_freeze_time elapses, radial/angular expansion halts and the bullet hangs in
# place; after orbit_breakout_time elapses, it reorients to its radial-outward
# direction rotated by orbit_breakout_angle_offset and switches to LINEAR motion,
# accelerating from orbit_breakout_initial_speed toward orbit_breakout_max_speed.
# Both times are negative (disabled) by default, preserving plain continuous
# expansion for existing callers (DanmakuDoubleRingStep, DanmakuFreezeRingStep).
var orbit_freeze_time: float = -1.0
var orbit_breakout_time: float = -1.0
var orbit_breakout_angle_offset: float = 0.0
var orbit_breakout_accel: float = 0.0
var orbit_breakout_max_speed: float = 0.0
var orbit_breakout_initial_speed: float = 0.0

# Lunar Wave parameters (Reisen's "Lunar Wave", Lv 2 spellcard): a ring that swells
# outward and brakes to a standstill, hangs there for a beat, drifts slightly back
# inward, then accelerates outward again while the whole ring rotates. Radius and
# angle are tracked in polar coordinates around lw_center exactly like EXPANDING_ORBIT,
# but the radial speed follows a scripted four-phase schedule instead of a constant.
var lw_center: Vector2 = Vector2.ZERO
var lw_expand_speed: float = 200.0
var lw_expand_time: float = 2.10
var lw_hold_time: float = 0.15
var lw_contract_speed: float = 70.0
var lw_contract_time: float = 0.35
var lw_release_speed: float = 30.0
var lw_release_accel: float = 360.0
var lw_release_max_speed: float = 0.0
## Ring rotation once the release phase begins, given as the angle (radians) the bullet's
## travel leans away from straight outward, not as an angular rate. Holding the lean fixed
## keeps every bullet tilted the same amount however far out it gets, and makes the slower
## rings sweep further around per pixel travelled - which is what curls each chain of
## bullets into a spiral arm. Positive leans clockwise on screen (Godot's +Y points down),
## negative counter-clockwise.
var lw_spin_lean: float = 0.70

# Curving parameters
var curve_angular_speed: float = 0.0
var curve_duration: float = 0.0
## Further (turn rate rad/s, acceleration px/s^2) pairs run one after another, each for
## curve_duration, once the first ends: ZUN's chained bullet_effects slots (Lyrica, pl06.ecl).
var curve_chain: PackedVector2Array = PackedVector2Array()
var _curve_chain_index: int = 0

# Ellipse motion parameters
var ellipse_center: Vector2 = Vector2.ZERO
var ellipse_radius: Vector2 = Vector2.ZERO
var ellipse_target_angle: float = 0.0
var fan_direction: Vector2 = Vector2.DOWN
var fan_speed: float = 300.0

var _decel_state: DecelState = DecelState.DECELERATING
var _phase_timer: float = 0.0
var _current_stage: int = 1
var _scale_tween: Tween = null
var _spawn_fade_tween: Tween = null
## Time left forming in place (DanmakuBulletData.spawn_fade_in_holds); nothing moves or hits until 0
var _spawn_hold_timer: float = 0.0

@onready var sprite: Sprite2D = %Sprite2D
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	collision_layer = 2 # enemy_bullets
	collision_mask = 0  # Ignored by other bullets/enemies, hits player layer 1
	_apply_visuals()

func on_pool_acquire() -> void:
	_spawn_hold_timer = 0.0
	if _spawn_fade_tween:
		_spawn_fade_tween.kill()
		_spawn_fade_tween = null
	is_canceled = false
	modulate.a = 1.0
	scale = Vector2.ONE
	acceleration = 0.0
	max_speed = 0.0
	_phase_timer = 0.0
	_current_lifetime = 0.0
	_current_stage = 1
	_decel_state = DecelState.DECELERATING
	orbit_center = Vector2.ZERO
	current_radius = 0.0
	current_angle = 0.0
	radial_speed = 0.0
	angular_speed = 0.0
	lw_center = Vector2.ZERO
	lw_spin_lean = 0.0
	lw_release_max_speed = 0.0
	orbit_freeze_time = -1.0
	orbit_breakout_time = -1.0
	orbit_breakout_angle_offset = 0.0
	orbit_breakout_accel = 0.0
	orbit_breakout_max_speed = 0.0
	orbit_breakout_initial_speed = 0.0
	curve_angular_speed = 0.0
	curve_duration = 0.0
	curve_chain = PackedVector2Array()
	_curve_chain_index = 0
	ellipse_center = Vector2.ZERO
	ellipse_radius = Vector2.ZERO
	ellipse_target_angle = 0.0
	fan_direction = Vector2.DOWN
	fan_speed = 300.0
	spin_speed = 0.0
	has_been_grazed = false
	is_in_graze_field = false
	freeze_trigger_radius = -1.0
	freeze_accel = 0.0
	freeze_max_speed = 0.0
	freeze_duration = 1.0
	_freeze_timer = 0.0
	_pending_accel = 0.0
	owner_playfield = null
	pellet_spawn_interval = -1.0
	pellet_spawn_data = null
	pellet_spawn_count = 1
	pellet_spawn_radius = 0.0
	pellet_spawn_accel_time = 1.0
	pellet_spawn_max_speed = 0.0
	pellet_spawn_remaining = -1
	pellet_spawn_on_circle = false
	pellet_spawn_radius_decay = 1.0
	pellet_spawn_speed_jitter = 0.0
	_pellet_spawn_timer = 0.0
	bounce_off_walls = false
	bounce_bullet_data = null
	bounce_overshoot = 0.0
	repel_source = null
	repel_radius = 0.0
	repel_speed = 0.0
	repel_time_left = 0.0
	_anim_index = -1
	if _graze_tint_active and sprite:
		sprite.modulate = _original_modulate
		sprite.scale = _original_scale
	_graze_tint_active = false
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	if sprite:
		sprite.rotation = 0.0
		sprite.offset = Vector2.ZERO
	set_deferred("monitorable", true)
	if collision_shape:
		collision_shape.set_deferred("disabled", false)

func on_pool_release() -> void:
	_spawn_hold_timer = 0.0
	if _scale_tween:
		_scale_tween.kill()
		_scale_tween = null
	if _spawn_fade_tween:
		_spawn_fade_tween.kill()
		_spawn_fade_tween = null
	is_canceled = false
	modulate.a = 1.0
	scale = Vector2.ONE
	acceleration = 0.0
	max_speed = 0.0
	orbit_center = Vector2.ZERO
	current_radius = 0.0
	current_angle = 0.0
	radial_speed = 0.0
	angular_speed = 0.0
	orbit_freeze_time = -1.0
	orbit_breakout_time = -1.0
	orbit_breakout_angle_offset = 0.0
	orbit_breakout_accel = 0.0
	orbit_breakout_max_speed = 0.0
	orbit_breakout_initial_speed = 0.0
	curve_angular_speed = 0.0
	curve_duration = 0.0
	curve_chain = PackedVector2Array()
	_curve_chain_index = 0
	ellipse_center = Vector2.ZERO
	ellipse_radius = Vector2.ZERO
	ellipse_target_angle = 0.0
	fan_direction = Vector2.DOWN
	fan_speed = 300.0
	spin_speed = 0.0
	has_been_grazed = false
	is_in_graze_field = false
	freeze_trigger_radius = -1.0
	freeze_accel = 0.0
	freeze_max_speed = 0.0
	freeze_duration = 1.0
	_freeze_timer = 0.0
	_pending_accel = 0.0
	owner_playfield = null
	pellet_spawn_interval = -1.0
	pellet_spawn_data = null
	pellet_spawn_count = 1
	pellet_spawn_radius = 0.0
	pellet_spawn_accel_time = 1.0
	pellet_spawn_max_speed = 0.0
	pellet_spawn_remaining = -1
	pellet_spawn_on_circle = false
	pellet_spawn_radius_decay = 1.0
	pellet_spawn_speed_jitter = 0.0
	_pellet_spawn_timer = 0.0
	bounce_off_walls = false
	bounce_bullet_data = null
	bounce_overshoot = 0.0
	repel_source = null
	repel_radius = 0.0
	repel_speed = 0.0
	repel_time_left = 0.0
	_anim_index = -1
	if _graze_tint_active and sprite:
		sprite.modulate = _original_modulate
		sprite.scale = _original_scale
	_graze_tint_active = false
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	if sprite:
		sprite.rotation = 0.0
		sprite.offset = Vector2.ZERO
	set_deferred("monitorable", false)
	if collision_shape:
		collision_shape.set_deferred("disabled", true)

func on_graze_enter() -> void:
	is_in_graze_field = true
	if sprite and not _graze_tint_active:
		_original_modulate = sprite.modulate
		_original_scale = sprite.scale
		# Intense hot crimson/magenta danger glow & scale pulse from Touhou 19
		sprite.modulate = Color(2.5, 0.35, 0.45, 1.0)
		sprite.scale = _original_scale * 1.2
		_graze_tint_active = true

func on_graze_exit() -> void:
	is_in_graze_field = false
	if sprite:
		sprite.offset = Vector2.ZERO
		if _graze_tint_active:
			sprite.modulate = _original_modulate
			sprite.scale = _original_scale
			_graze_tint_active = false

func despawn() -> void:
	if pool != null:
		pool.release(self)
	else:
		queue_free()

func setup(p_data: DanmakuBulletData, spawn_pos: Vector2, p_mode: MotionMode, p_dir: Vector2, p_speed: float, p_target: Node2D = null, p_accel: float = 0.0, p_max_speed: float = 0.0) -> void:
	bullet_data = p_data
	position = spawn_pos
	motion_mode = p_mode
	direction = p_dir.normalized() if p_dir != Vector2.ZERO else Vector2.DOWN
	speed = p_speed
	acceleration = p_accel
	max_speed = p_max_speed
	target_player = p_target
	is_canceled = false
	
	if bullet_data:
		damage = bullet_data.damage
		can_be_canceled = bullet_data.can_be_canceled
		lifetime = bullet_data.lifetime if bullet_data.lifetime > 0.0 else 7.0
		spin_speed = bullet_data.spin_speed
	else:
		damage = 1.0
		can_be_canceled = true
		lifetime = 7.0
		spin_speed = 0.0
	
	_decel_state = DecelState.DECELERATING
	_phase_timer = 0.0
	_current_lifetime = 0.0
	_current_stage = 1
	
	_apply_visuals()
	_update_rotation()
	_start_spawn_fade_in()

func setup_decel_home(
	p_data: DanmakuBulletData,
	spawn_pos: Vector2,
	p_dir: Vector2,
	p_init_speed: float,
	p_decel: float,
	p_pause: float,
	p_launch_speed: float,
	p_target: Node2D = null,
	p_stages: int = 1,
	p_stage1_duration: float = 0.5,
	p_homing_sfx: String = ""
) -> void:
	initial_speed = p_init_speed
	speed = p_init_speed
	decel_time = maxf(p_decel, 0.01)
	pause_time = maxf(p_pause, 0.01)
	launch_speed = p_launch_speed
	homing_stages = max(p_stages, 1)
	stage1_flight_duration = maxf(p_stage1_duration, 0.01)
	homing_sfx = p_homing_sfx
	stage2_decel_time = 0.0
	stage2_launch_speed = 0.0
	relaunch_relative = false
	relaunch_turn = 0.0
	_current_stage = 1
	setup(p_data, spawn_pos, MotionMode.DECEL_AND_HOME, p_dir, p_init_speed, p_target)

func setup_expanding_orbit(
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
) -> void:
	bullet_data = p_data
	motion_mode = MotionMode.EXPANDING_ORBIT
	orbit_center = p_center
	current_radius = p_initial_radius
	current_angle = p_initial_angle
	radial_speed = p_radial_speed
	angular_speed = p_angular_speed
	orbit_freeze_time = p_freeze_time
	orbit_breakout_time = p_breakout_time
	orbit_breakout_angle_offset = p_breakout_angle_offset
	orbit_breakout_accel = p_breakout_accel
	orbit_breakout_max_speed = p_breakout_max_speed
	orbit_breakout_initial_speed = p_breakout_initial_speed
	position = orbit_center + Vector2(cos(current_angle), sin(current_angle)) * current_radius
	direction = Vector2(cos(current_angle), sin(current_angle))
	is_canceled = false
	
	if bullet_data:
		damage = bullet_data.damage
		can_be_canceled = bullet_data.can_be_canceled
		lifetime = bullet_data.lifetime if bullet_data.lifetime > 0.0 else 7.0
	else:
		damage = 1.0
		can_be_canceled = true
		lifetime = 7.0
	
	_decel_state = DecelState.DECELERATING
	_phase_timer = 0.0
	_current_lifetime = 0.0
	_current_stage = 1
	
	_apply_visuals()
	_update_rotation()
	_start_spawn_fade_in()

func setup_lunar_wave(
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
) -> void:
	bullet_data = p_data
	motion_mode = MotionMode.LUNAR_WAVE
	lw_center = p_center
	lw_expand_speed = p_expand_speed
	lw_expand_time = maxf(p_expand_time, 0.01)
	lw_hold_time = maxf(p_hold_time, 0.0)
	lw_contract_speed = p_contract_speed
	lw_contract_time = maxf(p_contract_time, 0.0)
	lw_release_speed = p_release_speed
	lw_release_accel = p_release_accel
	lw_release_max_speed = p_release_max_speed
	lw_spin_lean = p_spin_lean
	current_radius = 0.0
	current_angle = p_initial_angle
	radial_speed = p_expand_speed
	angular_speed = 0.0
	position = lw_center
	direction = Vector2(cos(current_angle), sin(current_angle))
	is_canceled = false
	
	if bullet_data:
		damage = bullet_data.damage
		can_be_canceled = bullet_data.can_be_canceled
		lifetime = bullet_data.lifetime if bullet_data.lifetime > 0.0 else 7.0
	else:
		damage = 1.0
		can_be_canceled = true
		lifetime = 7.0
	
	_decel_state = DecelState.DECELERATING
	_phase_timer = 0.0
	_current_lifetime = 0.0
	_current_stage = 1
	
	_apply_visuals()
	_update_rotation()
	_start_spawn_fade_in()


func setup_curve_line(
	p_data: DanmakuBulletData,
	spawn_pos: Vector2,
	p_dir: Vector2,
	p_speed: float,
	p_curve_angular_speed: float,
	p_curve_duration: float,
	p_accel: float = 0.0,
	p_max_speed: float = 0.0,
	p_spin_speed: float = 0.0
) -> void:
	setup(p_data, spawn_pos, MotionMode.CURVE_THEN_LINE, p_dir, p_speed, null, p_accel, p_max_speed)
	curve_angular_speed = p_curve_angular_speed
	curve_duration = maxf(p_curve_duration, 0.0)
	curve_chain = PackedVector2Array()
	_curve_chain_index = 0
	if p_spin_speed != 0.0:
		spin_speed = p_spin_speed

func setup_ellipse_then_line(
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
) -> void:
	bullet_data = p_data
	ellipse_center = p_center
	ellipse_radius = p_radius
	current_angle = p_start_angle
	angular_speed = p_angular_speed
	ellipse_target_angle = p_target_angle
	fan_direction = p_fan_direction.normalized() if p_fan_direction != Vector2.ZERO else Vector2.DOWN
	fan_speed = p_fan_speed
	acceleration = p_accel
	max_speed = p_max_speed
	motion_mode = MotionMode.ELLIPSE_THEN_LINE
	is_canceled = false
	
	if bullet_data:
		damage = bullet_data.damage
		can_be_canceled = bullet_data.can_be_canceled
		lifetime = bullet_data.lifetime if bullet_data.lifetime > 0.0 else 7.0
		spin_speed = bullet_data.spin_speed
	else:
		damage = 1.0
		can_be_canceled = true
		lifetime = 7.0
	
	_decel_state = DecelState.DECELERATING
	_phase_timer = 0.0
	_current_lifetime = 0.0
	_current_stage = 1
	
	# Initial position on ellipse and tangent direction
	position = ellipse_center + Vector2(ellipse_radius.x * cos(current_angle), ellipse_radius.y * sin(current_angle))
	var vel_x: float = -ellipse_radius.x * sin(current_angle) * angular_speed
	var vel_y: float = ellipse_radius.y * cos(current_angle) * angular_speed
	direction = Vector2(vel_x, vel_y).normalized()
	
	_apply_visuals()
	_update_rotation()
	_start_spawn_fade_in()

func _apply_visuals() -> void:
	if collision_shape:
		var r: float = bullet_data.hitbox_radius if bullet_data else 6.0
		collision_shape.shape = get_cached_circle_shape(r)
	
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	
	if sprite:
		sprite.visible = true
		if bullet_data and bullet_data.texture:
			sprite.texture = bullet_data.texture
			sprite.modulate = bullet_data.tint_color
			sprite.scale = bullet_data.base_scale
		else:
			sprite.texture = EnemyPellet.pellet_red_tex
			sprite.modulate = bullet_data.tint_color if bullet_data else Color.WHITE
			var r: float = bullet_data.hitbox_radius if bullet_data else 6.0
			var s: float = (r / 6.0) * EnemyPellet.SCALE_PELLET.x
			sprite.scale = Vector2(s, s)
		_original_modulate = sprite.modulate
		_graze_tint_active = false

	z_as_relative = false
	z_index = 10


## Purely visual spawn-in (see DanmakuBulletData.spawn_fade_in): starts the sprite oversized and
## transparent, then tweens it down to its normal _apply_visuals() scale/alpha. The hitbox is
## unaffected - collision is active at full size from the first frame, same as before.
func _start_spawn_fade_in() -> void:
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	if sprite == null or bullet_data == null or not bullet_data.spawn_fade_in:
		return
	if _spawn_fade_tween:
		_spawn_fade_tween.kill()
	var target_scale: Vector2 = sprite.scale
	var target_alpha: float = sprite.modulate.a
	var scale_trans: Tween.TransitionType = Tween.TRANS_QUAD
	var alpha_trans: Tween.TransitionType = Tween.TRANS_QUAD
	if bullet_data.spawn_fade_in_holds:
		_spawn_hold_timer = bullet_data.spawn_fade_in_duration
		if collision_shape:
			collision_shape.set_deferred("disabled", true)
		var glow: Texture2D = bullet_data.spawn_fade_in_texture
		if glow and sprite.texture:
			# Glow sized to the bullet's own cell, so it hands over at the same size
			target_scale *= sprite.texture.get_size() / glow.get_size()
			sprite.texture = glow
			# ZUN's forming glow shrinks steadily while it brightens fast, so it is at its
			# biggest while already visible rather than only showing once small
			scale_trans = Tween.TRANS_LINEAR
			alpha_trans = Tween.TRANS_CUBIC
	var start_scale: Vector2 = target_scale * bullet_data.spawn_fade_in_start_scale_mult
	if sprite.texture == bullet_data.spawn_fade_in_texture:
		target_scale *= bullet_data.spawn_fade_in_texture_end_scale
	sprite.scale = start_scale
	sprite.modulate.a = 0.0
	_spawn_fade_tween = create_tween()
	_spawn_fade_tween.set_parallel(true)
	_spawn_fade_tween.tween_property(sprite, "scale", target_scale, bullet_data.spawn_fade_in_duration).set_trans(scale_trans).set_ease(Tween.EASE_OUT)
	_spawn_fade_tween.tween_property(sprite, "modulate:a", target_alpha, bullet_data.spawn_fade_in_duration).set_trans(alpha_trans).set_ease(Tween.EASE_OUT)

func _update_rotation() -> void:
	if bullet_data and bullet_data.rotate_with_direction:
		rotation = direction.angle() + (PI / 2.0) + deg_to_rad(bullet_data.rotation_offset_deg)
	else:
		rotation = 0.0

## Reassigns a random direction, resets speed to rest, ramps up to max_speed via acceleration,
## and recolors the sprite white. Used by Cirno's "Perfect Freeze" to seize every active bullet
## on the field (including ones that were not part of the spellcard) in place at once.
func freeze_and_scatter(p_accel: float, p_max_speed: float, p_freeze_duration: float = 0.0) -> void:
	if is_canceled:
		return
	motion_mode = MotionMode.LINEAR
	var ang: float = randf_range(0.0, TAU)
	direction = Vector2(cos(ang), sin(ang))
	speed = 0.0
	max_speed = p_max_speed
	if p_freeze_duration > 0.0:
		_freeze_timer = p_freeze_duration
		_pending_accel = p_accel
		acceleration = 0.0
	else:
		_freeze_timer = 0.0
		_pending_accel = 0.0
		acceleration = p_accel
	_update_rotation()
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	if sprite:
		if bullet_data and bullet_data.frozen_texture:
			sprite.texture = bullet_data.frozen_texture
		sprite.modulate = Color.WHITE
	_original_modulate = Color.WHITE

## Spawns one burst of pellet_spawn_count pellets scattered within pellet_spawn_radius of this
## bullet's current position, each starting at rest with a random direction and accelerating
## up to pellet_spawn_max_speed over pellet_spawn_accel_time. See pellet_spawn_interval above.
func _spawn_pellet_burst() -> void:
	if owner_playfield == null or not is_instance_valid(owner_playfield):
		return
	if pellet_spawn_remaining == 0:
		return
	if pellet_spawn_remaining > 0:
		pellet_spawn_remaining -= 1
	for i in range(pellet_spawn_count):
		var ang: float = randf_range(0.0, TAU)
		var dir := Vector2(cos(ang), sin(ang))
		var offset := Vector2(randf_range(-pellet_spawn_radius, pellet_spawn_radius), randf_range(-pellet_spawn_radius, pellet_spawn_radius))
		if pellet_spawn_on_circle:
			offset = Vector2.from_angle(randf_range(0.0, TAU)) * pellet_spawn_radius
		var top_speed: float = pellet_spawn_max_speed + randf_range(-pellet_spawn_speed_jitter, pellet_spawn_speed_jitter)
		var accel: float = top_speed / maxf(pellet_spawn_accel_time, 0.01)
		owner_playfield.call("spawn_danmaku_bullet", pellet_spawn_data, position + offset, MotionMode.LINEAR, dir, 0.0, accel, top_speed)
		AudioService.play_tan_exclusive()
	pellet_spawn_radius *= pellet_spawn_radius_decay

## Ricochets off the left/right/top playfield edges - never the bottom/south edge, which stays a
## normal exit. Reflects whichever velocity component drove the bullet past that edge, clamps
## position back onto the (possibly overshot) boundary, swaps to bounce_bullet_data (if set), and
## clears bounce_off_walls so this only ever fires once per bullet.
func _process_wall_bounce() -> void:
	var bounced: bool = false
	var edge_left: float = PLAYFIELD_EDGE_LEFT - bounce_overshoot
	var edge_right: float = PLAYFIELD_EDGE_RIGHT + bounce_overshoot
	var edge_top: float = PLAYFIELD_EDGE_TOP - bounce_overshoot
	if position.x <= edge_left and direction.x < 0.0:
		position.x = edge_left
		direction.x = -direction.x
		bounced = true
	elif position.x >= edge_right and direction.x > 0.0:
		position.x = edge_right
		direction.x = -direction.x
		bounced = true
	if position.y <= edge_top and direction.y < 0.0:
		position.y = edge_top
		direction.y = -direction.y
		bounced = true

	if not bounced:
		return

	AudioService.play_sfx("se_kira00")

	direction = direction.normalized()
	_update_rotation()
	bounce_off_walls = false
	if bounce_bullet_data != null:
		bullet_data = bounce_bullet_data
		_apply_visuals()
		_update_rotation()

func cancel() -> void:
	if is_canceled or not can_be_canceled:
		return
	is_canceled = true
	
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	
	if _scale_tween:
		_scale_tween.kill()
	
	_scale_tween = create_tween()
	_scale_tween.tween_property(self, "scale", scale * 1.4, 0.08).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	_scale_tween.parallel().tween_property(self, "modulate:a", 0.0, 0.08)
	_scale_tween.finished.connect(despawn)

func _physics_process(delta: float) -> void:
	if is_canceled:
		return
	
	if is_in_graze_field and sprite:
		# Touhou 19 bullet buzz jitter inside graze field (visual only, hitbox is untouched)
		# In screen pixels, so it doesn't grow with the sprite's scale (offset is in texture pixels).
		const GRAZE_JITTER_PX: float = 2.0
		var jitter: float = GRAZE_JITTER_PX / maxf(absf(sprite.scale.x), 0.01)
		sprite.offset = Vector2(randf_range(-jitter, jitter), randf_range(-jitter, jitter))
	
	_current_lifetime += delta
	if _current_lifetime >= lifetime:
		despawn()
		return

	if _spawn_hold_timer > 0.0:
		_spawn_hold_timer -= delta
		if _spawn_hold_timer > 0.0:
			if bullet_data and motion_mode == MotionMode.LINEAR:
				position += direction * speed * bullet_data.spawn_fade_in_speed_mult * delta
			return
		if collision_shape:
			collision_shape.set_deferred("disabled", false)
		if bullet_data and bullet_data.spawn_fade_in_texture and sprite:
			if _spawn_fade_tween:
				_spawn_fade_tween.kill()
				_spawn_fade_tween = null
			sprite.texture = bullet_data.texture
			sprite.scale = bullet_data.base_scale
			sprite.modulate = bullet_data.tint_color

	match motion_mode:
		MotionMode.LINEAR:
			if _freeze_timer > 0.0:
				_freeze_timer -= delta
				if _freeze_timer <= 0.0:
					_freeze_timer = 0.0
					acceleration = _pending_accel
			if acceleration != 0.0:
				speed += acceleration * delta
				if max_speed > 0.0 and speed > max_speed:
					speed = max_speed
			position += direction * speed * delta
		MotionMode.DECEL_AND_HOME:
			_process_decel_and_home(delta)
		MotionMode.EXPANDING_ORBIT:
			if orbit_breakout_time >= 0.0 and _current_lifetime >= orbit_breakout_time:
				var radial_dir := Vector2(cos(current_angle), sin(current_angle))
				direction = radial_dir.rotated(orbit_breakout_angle_offset)
				motion_mode = MotionMode.LINEAR
				speed = orbit_breakout_initial_speed
				acceleration = orbit_breakout_accel
				max_speed = orbit_breakout_max_speed
				_update_rotation()
				position += direction * speed * delta
			else:
				if orbit_freeze_time < 0.0 or _current_lifetime < orbit_freeze_time:
					current_radius += radial_speed * delta
					current_angle += angular_speed * delta
				position = orbit_center + Vector2(cos(current_angle), sin(current_angle)) * current_radius
				direction = Vector2(cos(current_angle), sin(current_angle))
				_update_rotation()
				if freeze_trigger_radius > 0.0 and current_radius >= freeze_trigger_radius:
					freeze_trigger_radius = -1.0
					if owner_playfield != null and is_instance_valid(owner_playfield) and owner_playfield.has_method("freeze_and_scatter_bullets"):
						owner_playfield.freeze_and_scatter_bullets(freeze_accel, freeze_max_speed, freeze_duration)
		MotionMode.LUNAR_WAVE:
			_process_lunar_wave(delta)
		MotionMode.CURVE_THEN_LINE:
			if _phase_timer >= curve_duration and _curve_chain_index < curve_chain.size():
				_phase_timer -= curve_duration
				curve_angular_speed = curve_chain[_curve_chain_index].x
				acceleration = curve_chain[_curve_chain_index].y
				_curve_chain_index += 1
			if _phase_timer < curve_duration:
				_phase_timer += delta
				direction = direction.rotated(curve_angular_speed * delta)
				_update_rotation()
				# Acceleration belongs to the curve phase, like ZUN's timed bullet_effects:
				# it stops with the turning, so a slowing bullet never reverses.
				if acceleration != 0.0:
					speed = maxf(speed + acceleration * delta, 0.0)
					if max_speed > 0.0 and speed > max_speed:
						speed = max_speed
			position += direction * speed * delta
		MotionMode.ELLIPSE_THEN_LINE:
			current_angle += angular_speed * delta
			var has_reached_target: bool = false
			if angular_speed > 0.0 and current_angle >= ellipse_target_angle:
				has_reached_target = true
			elif angular_speed < 0.0 and current_angle <= ellipse_target_angle:
				has_reached_target = true
			
			if has_reached_target:
				position = ellipse_center + Vector2(ellipse_radius.x * cos(ellipse_target_angle), ellipse_radius.y * sin(ellipse_target_angle))
				motion_mode = MotionMode.LINEAR
				speed = fan_speed
				direction = fan_direction
				_update_rotation()
				position += direction * speed * delta
			else:
				position = ellipse_center + Vector2(ellipse_radius.x * cos(current_angle), ellipse_radius.y * sin(current_angle))
				var vel_x: float = -ellipse_radius.x * sin(current_angle) * angular_speed
				var vel_y: float = ellipse_radius.y * cos(current_angle) * angular_speed
				direction = Vector2(vel_x, vel_y).normalized()
				_update_rotation()
	
	if bounce_off_walls and (motion_mode == MotionMode.LINEAR or motion_mode == MotionMode.DECEL_AND_HOME):
		_process_wall_bounce()

	if repel_time_left > 0.0:
		repel_time_left -= delta
		_process_repel(delta)

	if pellet_spawn_interval > 0.0 and pellet_spawn_data != null:
		_pellet_spawn_timer += delta
		if _pellet_spawn_timer >= pellet_spawn_interval:
			_pellet_spawn_timer -= pellet_spawn_interval
			_spawn_pellet_burst()

	if spin_speed != 0.0:
		if sprite == null:
			sprite = get_node_or_null("%Sprite2D")
		if sprite:
			sprite.rotation += spin_speed * delta

	if bullet_data and bullet_data.anim_frames.size() > 1 and sprite and _spawn_hold_timer <= 0.0:
		var frame_time: float = maxf(bullet_data.anim_frame_duration, 0.001)
		var index: int = int(_current_lifetime / frame_time) % bullet_data.anim_frames.size()
		if index != _anim_index:
			_anim_index = index
			sprite.texture = bullet_data.anim_frames[index]

	# Playfield exit despawn check
	# Playfield visible area is (0, 0) to (600, 960).
	# Once bullets exit the playfield margin traveling outward, despawn immediately.
	if motion_mode == MotionMode.LINEAR:
		if (position.y > 1020.0 and direction.y > 0.0) or \
		   (position.x < -80.0 and direction.x < 0.0) or \
		   (position.x > 680.0 and direction.x > 0.0) or \
		   (position.y < -300.0 and direction.y < 0.0):
			despawn()
			return
	elif motion_mode == MotionMode.DECEL_AND_HOME:
		if _decel_state == DecelState.LAUNCHED and _current_stage >= homing_stages:
			if (position.y > 1020.0 and direction.y > 0.0) or \
			   (position.x < -80.0 and direction.x < 0.0) or \
			   (position.x > 680.0 and direction.x > 0.0) or \
			   (position.y < -80.0 and direction.y < 0.0):
				despawn()
				return
	elif motion_mode == MotionMode.CURVE_THEN_LINE:
		if _phase_timer >= curve_duration and _curve_chain_index >= curve_chain.size():
			if (position.y > 1020.0 and direction.y > 0.0) or \
			   (position.x < -80.0 and direction.x < 0.0) or \
			   (position.x > 680.0 and direction.x > 0.0) or \
			   (position.y < -80.0 and direction.y < 0.0):
				despawn()
				return
	elif motion_mode == MotionMode.EXPANDING_ORBIT or motion_mode == MotionMode.LUNAR_WAVE:
		if current_radius > 900.0:
			despawn()
			return

	# Safety boundary check to prevent runaway off-screen positions
	if (position.x < SAFETY_MIN_X or position.x > SAFETY_MAX_X or
		position.y < SAFETY_MIN_Y or position.y > SAFETY_MAX_Y):
		despawn()

## Pushes the bullet out of repel_radius around repel_source, at most repel_speed per second and
## never past the radius itself, so a bullet flying at the player settles on the edge instead
## of bouncing around it. Worked in the bullet's parent space: the player lives elsewhere in
## the playfield tree.
func _process_repel(delta: float) -> void:
	if repel_source == null or not is_instance_valid(repel_source) or get_parent() == null:
		return
	var source: Vector2 = get_parent().to_local(repel_source.global_position)
	var offset: Vector2 = position - source
	var dist: float = offset.length()
	if dist >= repel_radius:
		return
	var away: Vector2 = offset / dist if dist > 0.001 else direction
	position = source + away * minf(dist + repel_speed * delta, repel_radius)

## Reisen's "Lunar Wave": walks the bullet through the ring's four-phase radial
## schedule. Everything is driven off _current_lifetime, so every bullet spawned in
## the same ring stays perfectly in step with its neighbours without any shared state.
func _process_lunar_wave(delta: float) -> void:
	var t: float = _current_lifetime
	var hold_end: float = lw_expand_time + lw_hold_time
	var contract_end: float = hold_end + lw_contract_time
	
	if t < lw_expand_time:
		# Swell outward, holding near full speed at first and then braking hard into a
		# standstill exactly at lw_expand_time - the ease-out curve PoFV's ring follows
		var e: float = t / lw_expand_time
		radial_speed = lw_expand_speed * (1.0 - e * e)
	elif t < hold_end:
		radial_speed = 0.0
	elif t < contract_end:
		# Sink back in on a sine arc so the reversal eases in and out instead of snapping
		var u: float = (t - hold_end) / maxf(lw_contract_time, 0.0001)
		radial_speed = -lw_contract_speed * sin(PI * u)
	else:
		if radial_speed < lw_release_speed:
			radial_speed = lw_release_speed
		radial_speed += lw_release_accel * delta
		if lw_release_max_speed > 0.0:
			radial_speed = minf(radial_speed, lw_release_max_speed)
		# A fixed lean off straight-outward rather than a fixed turn rate, so the tilt reads
		# the same however far out the bullet gets and the slower rings curl further around.
		angular_speed = radial_speed * tan(lw_spin_lean) / maxf(current_radius, 1.0)

	current_radius = maxf(current_radius + radial_speed * delta, 0.0)
	current_angle += angular_speed * delta
	position = lw_center + Vector2(cos(current_angle), sin(current_angle)) * current_radius
	# Face actual travel: radial outward plus the tangential drag of the spin, which is
	# what tilts the pellets into the swept spiral once the ring starts turning.
	var radial_dir := Vector2(cos(current_angle), sin(current_angle))
	var tangent_dir := Vector2(-sin(current_angle), cos(current_angle))
	var velocity := radial_dir * radial_speed + tangent_dir * (angular_speed * current_radius)
	if velocity.length_squared() > 0.01:
		direction = velocity.normalized()
	else:
		direction = radial_dir
	_update_rotation()


func _process_decel_and_home(delta: float) -> void:
	_phase_timer += delta
	match _decel_state:
		DecelState.DECELERATING:
			var t: float = clampf(_phase_timer / decel_time, 0.0, 1.0)
			# Smooth ease-out brake down to 0
			speed = lerpf(initial_speed, 0.0, t)
			position += direction * speed * delta
			if t >= 1.0:
				speed = 0.0
				_decel_state = DecelState.PAUSED_LOCKING
				_phase_timer = 0.0
		
		DecelState.PAUSED_LOCKING:
			# Stay stationary during pause time
			if _phase_timer >= pause_time:
				# Snapshot player's exact position at this moment!
				if relaunch_relative:
					direction = direction.rotated(relaunch_turn)
					_update_rotation()
				elif is_instance_valid(target_player):
					var to_player := (target_player.global_position - global_position).normalized()
					if to_player != Vector2.ZERO:
						direction = to_player
						_update_rotation()
				
				speed = launch_speed
				_decel_state = DecelState.LAUNCHED
				_phase_timer = 0.0
		
		DecelState.LAUNCHED:
			position += direction * speed * delta
			if _current_stage < homing_stages:
				if _phase_timer >= stage1_flight_duration:
					_current_stage += 1
					initial_speed = speed
					if stage2_decel_time > 0.0:
						decel_time = stage2_decel_time
						launch_speed = stage2_launch_speed
					_decel_state = DecelState.DECELERATING
					_phase_timer = 0.0

