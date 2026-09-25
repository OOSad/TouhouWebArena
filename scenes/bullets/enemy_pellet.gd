class_name EnemyPellet
extends Area2D

# Standard enemy pellet bullet sent across from the opponent's field
@export var speed: float = 240.0
@export var direction: Vector2 = Vector2.DOWN
@export var acceleration: float = 0.0
@export var max_speed: float = 0.0
@export var is_big: bool = false
@export var is_ring: bool = false
@export var bounce_count: int = 0
@export var can_be_canceled: bool = true
@export var theme_color: Color = Color(0.4, 0.8, 1.0)
@export var damage: float = 1.0

func get_damage() -> float:
	if is_big:
		return 1.5
	return damage

const PLAYFIELD_DESPAWN_Y: float = 980.0
const PLAYFIELD_DESPAWN_MIN_X: float = -30.0
const PLAYFIELD_DESPAWN_MAX_X: float = 630.0
const SPAWN_POP_DURATION: float = 0.12

const PELLET_RADIUS: float = 6.0
const GLOW_RADIUS: float = 9.0
const BIG_PELLET_RADIUS: float = 12.0
const BIG_GLOW_RADIUS: float = 18.0
const RING_OUTER_RADIUS: float = 16.0
const RING_CORE_RADIUS: float = 9.0

static var _shape_normal: CircleShape2D = null
static var _shape_big: CircleShape2D = null
static var _shape_ring: CircleShape2D = null

static func _init_shapes() -> void:
	if _shape_normal == null:
		_shape_normal = CircleShape2D.new()
		_shape_normal.radius = PELLET_RADIUS
		_shape_big = CircleShape2D.new()
		_shape_big.radius = BIG_PELLET_RADIUS
		_shape_ring = CircleShape2D.new()
		_shape_ring.radius = 11.0

# PoFV's sprites from the player's th09.dat, filled in at startup by BulletSprites:
# Pellet for small ones, Ball for big ones, and a white RingBall tinted with the theme colour.
static var pellet_blue_tex: Texture2D = null
static var pellet_red_tex: Texture2D = null
static var pellet_white_tex: Texture2D = null
static var big_blue_tex: Texture2D = null
static var big_red_tex: Texture2D = null
static var big_white_tex: Texture2D = null
static var ring_tex: Texture2D = null

# Our field is 2.0833x the size of PoFV's.
const SCALE_PELLET: Vector2 = Vector2(2.0833, 2.0833)

var _is_spawned: bool = false
var _spawn_time: float = 0.0
var _scale_tween: Tween = null
var is_canceled: bool = false
var pool: NodePool = null

# Graze tracking & Touhou 19 proximity feedback
var has_been_grazed: bool = false
var is_in_graze_field: bool = false
var _graze_tint_active: bool = false
var _original_modulate: Color = Color.WHITE
var _original_scale: Vector2 = Vector2.ONE
var _freeze_timer: float = 0.0
var _pending_accel: float = 0.0

@onready var collision_shape: CollisionShape2D = $CollisionShape2D
@onready var sprite: Sprite2D = %Sprite2D

func _ready() -> void:
	collision_layer = 2 # 2d_physics/layer_2="enemy_bullets"
	collision_mask = 0  # Does not collide with bullets or enemies
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	_update_collision_shape()

func on_pool_acquire() -> void:
	is_canceled = false
	_is_spawned = false
	_spawn_time = 0.0
	is_big = false
	is_ring = false
	bounce_count = 0
	can_be_canceled = true
	theme_color = Color(0.4, 0.8, 1.0)
	acceleration = 0.0
	max_speed = 0.0
	_freeze_timer = 0.0
	_pending_accel = 0.0
	modulate.a = 1.0
	has_been_grazed = false
	is_in_graze_field = false
	if _graze_tint_active and sprite:
		sprite.modulate = _original_modulate
		sprite.scale = _original_scale
	_graze_tint_active = false
	set_deferred("monitorable", true)
	if collision_shape:
		collision_shape.set_deferred("disabled", false)
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	if sprite:
		sprite.visible = true
		sprite.modulate = Color.WHITE
		sprite.scale = SCALE_PELLET
		sprite.texture = pellet_blue_tex
		sprite.offset = Vector2.ZERO
	_original_modulate = Color.WHITE

func on_pool_release() -> void:
	if _scale_tween:
		_scale_tween.kill()
		_scale_tween = null
	is_canceled = false
	_is_spawned = false
	acceleration = 0.0
	max_speed = 0.0
	_freeze_timer = 0.0
	_pending_accel = 0.0
	modulate.a = 1.0
	scale = Vector2.ONE
	has_been_grazed = false
	is_in_graze_field = false
	if _graze_tint_active and sprite:
		sprite.modulate = _original_modulate
		sprite.scale = _original_scale
	_graze_tint_active = false
	set_deferred("monitorable", false)
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	if sprite:
		sprite.scale = SCALE_PELLET
		sprite.modulate = Color.WHITE
		sprite.offset = Vector2.ZERO

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

## Reassigns a random direction, resets speed to rest, ramps up to max_speed via acceleration,
## and recolors white. Mirrors DanmakuBullet.freeze_and_scatter() for Cirno's "Perfect Freeze".
func freeze_and_scatter(p_accel: float, p_max_speed: float, p_freeze_duration: float = 0.0) -> void:
	if is_canceled:
		return
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
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	if sprite:
		if not is_ring:
			sprite.texture = big_white_tex if is_big else pellet_white_tex
		sprite.modulate = Color.WHITE
	_original_modulate = Color.WHITE

func despawn() -> void:
	if pool != null:
		pool.release(self)
	else:
		queue_free()

func setup(spawn_pos: Vector2, custom_speed: float = -1.0, custom_dir: Vector2 = Vector2.ZERO, p_is_big: bool = false, p_is_ring: bool = false, p_theme_color: Color = Color(0.4, 0.8, 1.0), p_bounce_count: int = 0) -> void:
	position = spawn_pos
	is_big = p_is_big
	is_ring = p_is_ring
	theme_color = p_theme_color
	bounce_count = p_bounce_count
	can_be_canceled = (not is_big and not is_ring) # Big pellets and ring pellets cannot be canceled by fairy shockwaves!
	_update_collision_shape()
	
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	if sprite:
		if is_ring:
			sprite.texture = ring_tex
			sprite.scale = SCALE_PELLET
			sprite.modulate = theme_color
		else:
			var is_red: bool = false
			if theme_color.r > theme_color.b + 0.15:
				is_red = true
			elif theme_color.b > theme_color.r + 0.15:
				is_red = false
			else:
				is_red = (randf() < 0.5)
			
			if is_big:
				sprite.texture = big_red_tex if is_red else big_blue_tex
			else:
				sprite.texture = pellet_red_tex if is_red else pellet_blue_tex
			sprite.scale = SCALE_PELLET
			sprite.modulate = Color.WHITE
		
		z_as_relative = false
		z_index = 10
		_original_modulate = sprite.modulate
		_graze_tint_active = false
	
	# Varied speed: Big pellets are heavier (160-210 px/s), small pellets are faster (195-265 px/s), ring bullets (240-270 px/s)
	if custom_speed > 0.0:
		speed = custom_speed
	elif is_ring:
		speed = 250.0
	elif is_big:
		speed = randf_range(160.0, 210.0)
	else:
		speed = randf_range(195.0, 265.0)
	
	# Downward angle with inward bias based on spawn X position
	if custom_dir != Vector2.ZERO:
		direction = custom_dir.normalized()
	else:
		# Normalized horizontal position: -1.0 (left edge) to +1.0 (right edge)
		var norm_x: float = clampf((spawn_pos.x - 300.0) / 300.0, -1.0, 1.0)
		# Subtle inward bias (up to ~3 degrees) to keep general downward motion while threatening the edges
		var inward_bias_deg: float = -norm_x * 3.0
		# Random base fan: +/- 18 degrees
		var random_fan_deg: float = randf_range(-18.0, 18.0)
		var final_deg: float = clampf(random_fan_deg + inward_bias_deg, -30.0, 30.0)
		var angle_rad: float = deg_to_rad(final_deg)
		direction = Vector2(sin(angle_rad), cos(angle_rad)).normalized()
	
	# Spawn scale pop - zero-allocation inline timer handled in _physics_process
	scale = Vector2(0.2, 0.2)
	_spawn_time = 0.0
	_is_spawned = false

func _update_collision_shape() -> void:
	if collision_shape:
		_init_shapes()
		if is_ring:
			collision_shape.shape = _shape_ring
		elif is_big:
			collision_shape.shape = _shape_big
		else:
			collision_shape.shape = _shape_normal

func cancel() -> void:
	if is_canceled:
		return
	is_canceled = true
	
	# Disable collision immediately
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	
	# Quick pop and fade
	if _scale_tween:
		_scale_tween.kill()
	
	_scale_tween = create_tween()
	_scale_tween.tween_property(self, "scale", scale * 1.5, 0.08).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
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
	
	if not _is_spawned:
		_spawn_time += delta
		if _spawn_time >= SPAWN_POP_DURATION:
			_is_spawned = true
			scale = Vector2.ONE
		else:
			var t: float = _spawn_time / SPAWN_POP_DURATION
			# TRANS_BACK, EASE_OUT formula without allocating a Tween
			var c1: float = 1.70158
			var c3: float = c1 + 1.0
			var back_factor: float = 1.0 + c3 * pow(t - 1.0, 3.0) + c1 * pow(t - 1.0, 2.0)
			var current_scale: float = lerpf(0.2, 1.0, back_factor)
			scale = Vector2(current_scale, current_scale)
	
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

	if position.y > PLAYFIELD_DESPAWN_Y or position.x < PLAYFIELD_DESPAWN_MIN_X or position.x > PLAYFIELD_DESPAWN_MAX_X:
		despawn()

