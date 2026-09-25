class_name Spirit
extends Area2D

# Spirit enemy entity in Touhou Web Arena
# Floats downwards, accelerates towards a terminal speed, can be damaged by player bullets and shockwaves.
# Can be activated by player's Scope Style (Step 2/3).

signal hit(damage: float, remaining_health: float)
signal defeated(death_pos: Vector2, source: String)
signal detonated(detonation_pos: Vector2, color_theme: String)

enum SpiritState {
	NORMAL,     # Unactivated wisp, floats down, 8 HP
	ACTIVATED   # Activated orb, floats up, 4 HP (Scope Style)
}

# enemy.anm: the arc a spirit flies as, turned to its heading (scripts 20-22, a frame per 4
# ticks), and the ring it becomes when activated (scripts 23-25, a frame per 2 ticks).
const TEAL_SPIRIT_TEX: Texture2D = preload("res://resources/dat_textures/spirit_teal.tres")
const TEAL_RING_TEX: Texture2D = preload("res://resources/dat_textures/spirit_teal_ring.tres")
const RED_SPIRIT_TEX: Texture2D = preload("res://resources/dat_textures/spirit_red.tres")
const RED_RING_TEX: Texture2D = preload("res://resources/dat_textures/spirit_red_ring.tres")
const ARC_FRAMES: int = 4
const RING_FRAMES: int = 8
const ARC_FRAME_DURATION: float = 4.0 / 60.0
const RING_FRAME_DURATION: float = 2.0 / 60.0

const PLAYFIELD_MIN_X: float = 20.0
const PLAYFIELD_MAX_X: float = 580.0
const PLAYFIELD_DESPAWN_Y: float = 980.0

@export var state: SpiritState = SpiritState.NORMAL
@export var max_health: float = 8.0
var current_health: float = 8.0

# Movement parameters (Normal State)
@export var initial_speed: float = 120.0
@export var terminal_speed: float = 350.0
@export var acceleration: float = 50.0

# Movement parameters (Activated State)
@export var upward_speed: float = 160.0 # Terminal upward speed
@export var activated_initial_speed: float = 0.0 # Initial upward speed upon activation
@export var activated_acceleration_ramp: float = 40.0 # Acceleration ramp-up rate (px/s³)

var current_downward_speed: float = 120.0
var current_upward_speed: float = 0.0
var current_upward_acceleration: float = 0.0
var trajectory_angle: float = 0.0
var detonation_timer: float = 2.8
var current_color_theme: String = "blue"

var is_active: bool = false
var is_dead: bool = false
var is_vulnerable: bool = true
var _dying_from_heavy_shockwave: bool = false

# Animation
var _frame_timer: float = 0.0
var _anim_frame: int = 0

@onready var sprite: Sprite2D = %Sprite2D
@onready var collision_shape: CollisionShape2D = %CollisionShape2D
var _hit_tween: Tween = null

func _ready() -> void:
	collision_layer = 8 # Layer 4 = "enemies"
	collision_mask = 0
	_apply_visuals(current_color_theme)

func setup(spawn_pos: Vector2, p_color_theme: String = "blue", p_state: SpiritState = SpiritState.NORMAL) -> void:
	position = spawn_pos
	state = p_state
	current_color_theme = p_color_theme
	
	if state == SpiritState.NORMAL:
		max_health = 8.0
	else:
		max_health = 4.0
	current_health = max_health
	
	# Movement trajectory
	current_downward_speed = randf_range(110.0, 150.0)
	current_upward_speed = activated_initial_speed
	current_upward_acceleration = 0.0
	trajectory_angle = randf_range(-0.24, 0.24) # ~ +/- 14 degrees
	# Subtle inward bias so spirits spawned near borders drift toward center
	if position.x < 160.0:
		trajectory_angle = randf_range(0.02, 0.22)
	elif position.x > 440.0:
		trajectory_angle = randf_range(-0.22, -0.02)
	
	is_active = true
	is_dead = false
	is_vulnerable = true
	
	if is_node_ready():
		_apply_visuals(p_color_theme)

func _apply_visuals(color_theme: String = "blue") -> void:
	if sprite == null:
		return
	
	current_color_theme = color_theme
	_apply_state_texture()
	sprite.scale = Vector2(1.4, 1.4)
	
	if collision_shape:
		var circle := CircleShape2D.new()
		circle.radius = 20.0
		collision_shape.shape = circle

## The arc while flying, the ring once activated; red for the red theme, teal otherwise.
func _apply_state_texture() -> void:
	var red: bool = current_color_theme.to_lower() == "red"
	_anim_frame = 0
	_frame_timer = 0.0
	if state == SpiritState.NORMAL:
		sprite.texture = RED_SPIRIT_TEX if red else TEAL_SPIRIT_TEX
		sprite.hframes = ARC_FRAMES
	else:
		sprite.texture = RED_RING_TEX if red else TEAL_RING_TEX
		sprite.hframes = RING_FRAMES
		sprite.rotation = 0.0
	sprite.frame = 0

func _physics_process(delta: float) -> void:
	if is_dead:
		return
	
	_handle_movement(delta)
	_update_animation(delta)
	
	if state == SpiritState.ACTIVATED:
		detonation_timer -= delta
		if detonation_timer <= 0.8 and sprite:
			# Flashing warning effect before detonation
			var flash: float = pingpong(detonation_timer * 8.0, 1.0)
			sprite.modulate = Color(1.0 + flash * 0.8, 1.0 + flash * 0.8, 1.0 + flash * 0.8, 1.0)
		if detonation_timer <= 0.0:
			detonate()

func _handle_movement(delta: float) -> void:
	if state == SpiritState.NORMAL:
		# Accelerate downwards towards terminal speed along chosen angle
		current_downward_speed = minf(terminal_speed, current_downward_speed + acceleration * delta)
		var move_dir := Vector2(sin(trajectory_angle), cos(trajectory_angle))
		
		position += move_dir * current_downward_speed * delta
		position.x = clampf(position.x, PLAYFIELD_MIN_X, PLAYFIELD_MAX_X)
		if sprite:
			sprite.rotation = move_dir.angle()

		# Despawn when floating off the bottom
		if position.y > PLAYFIELD_DESPAWN_Y:
			queue_free()
	else:
		# Activated state: starts with no acceleration (0.0) and zero initial speed.
		# Upward acceleration and speed gradually ramp up, so spirits linger near
		# their activation point before accelerating into triple pellet detonation.
		current_upward_acceleration += activated_acceleration_ramp * delta
		current_upward_speed = minf(upward_speed, current_upward_speed + current_upward_acceleration * delta)
		
		position.y -= current_upward_speed * delta
		position.x += sin(trajectory_angle) * 20.0 * delta
		position.x = clampf(position.x, PLAYFIELD_MIN_X, PLAYFIELD_MAX_X)
		
		# Despawn when floating off the top
		if position.y < -50.0:
			queue_free()

func activate() -> void:
	if state == SpiritState.ACTIVATED or is_dead:
		return
	
	state = SpiritState.ACTIVATED
	max_health = 4.0
	current_health = minf(current_health, 4.0)
	detonation_timer = 2.8
	current_upward_speed = activated_initial_speed
	current_upward_acceleration = 0.0
	
	# Visual activation pop
	if sprite:
		_apply_state_texture()
		sprite.scale = Vector2(1.7, 1.7)
		var tween := create_tween()
		tween.tween_property(sprite, "scale", Vector2(1.4, 1.4), 0.12)
		sprite.modulate = Color(1.3, 1.3, 1.3, 1.0)
		var tween_col := create_tween()
		tween_col.tween_property(sprite, "modulate", Color.WHITE, 0.15)

func detonate() -> void:
	if is_dead:
		return
	is_dead = true
	detonated.emit(position, current_color_theme)
	queue_free()

func _update_animation(delta: float) -> void:
	var normal: bool = state == SpiritState.NORMAL
	var frame_duration: float = ARC_FRAME_DURATION if normal else RING_FRAME_DURATION
	_frame_timer += delta
	if _frame_timer >= frame_duration:
		_frame_timer -= frame_duration
		_anim_frame = (_anim_frame + 1) % (ARC_FRAMES if normal else RING_FRAMES)
		if sprite:
			sprite.frame = _anim_frame

func take_damage(amount: float, source: String = "bullet") -> bool:
	if not is_active or is_dead or _dying_from_heavy_shockwave:
		return false
	if not is_vulnerable and source != "heavy_shockwave":
		return false
	
	current_health = maxf(0.0, current_health - amount)
	_flash_hit()
	AudioService.play_damage_hit(false)
	hit.emit(amount, current_health)
	
	if current_health <= 0.0:
		if source == "heavy_shockwave":
			_start_delayed_heavy_shockwave_death()
		else:
			die(source)
	
	return true

func _start_delayed_heavy_shockwave_death() -> void:
	if _dying_from_heavy_shockwave or is_dead:
		return
	_dying_from_heavy_shockwave = true
	is_vulnerable = false
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	
	if is_inside_tree():
		var tree := get_tree()
		if tree:
			var timer := tree.create_timer(0.10)
			timer.timeout.connect(func() -> void:
				if is_instance_valid(self) and not is_queued_for_deletion():
					_dying_from_heavy_shockwave = false
					die("heavy_shockwave")
			)
			return
	die("heavy_shockwave")

func _flash_hit() -> void:
	if sprite:
		if _hit_tween and _hit_tween.is_valid():
			_hit_tween.kill()
		sprite.modulate = Color(1.0, 0.4, 0.4, 1.0)
		_hit_tween = create_tween()
		_hit_tween.tween_property(sprite, "modulate", Color.WHITE, 0.06)

func die(source: String = "bullet") -> void:
	if is_dead:
		return
	is_dead = true
	AudioService.play_enemy_death()
	
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	
	defeated.emit(position, source)
	queue_free()
