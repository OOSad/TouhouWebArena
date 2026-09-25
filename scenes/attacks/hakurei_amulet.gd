class_name HakureiAmulet
extends Area2D

# Reimu Hakurei's Level 1 Charge Attack: Hakurei Amulet (博麗アミュレット)
# Four large spinning talismans materialize in front of Reimu, grow to exceed
# her sprite size over ~0.45s, accelerate towards the closest enemy, and play
# a spinning fade-out animation using the "spent" talisman texture upon impact.

enum State {
	GROWING,
	HOMING,
	SPENT_DESPAWN
}

const ACTIVE_TEX: Texture2D = preload("res://resources/dat_textures/reimu_charge_amulet.tres")
const SPENT_TEX: Texture2D = preload("res://resources/dat_textures/reimu_charge_amulet_spent.tres")
const SPENT_DARK_TEX: Texture2D = preload("res://resources/dat_textures/reimu_charge_amulet_spent_dark.tres")

const PLAYFIELD_MIN_X: float = -60.0
const PLAYFIELD_MAX_X: float = 660.0
const PLAYFIELD_MIN_Y: float = -80.0
const PLAYFIELD_MAX_Y: float = 1050.0

@export var damage: float = 3.0 # pl00.sht: 30, a normal shot being 10; footage agrees
@export var growth_duration: float = 0.45
@export var min_scale: float = 0.15
@export var max_scale: float = 1.40
@export var spin_speed: float = 12.0
@export var initial_speed: float = 260.0
@export var max_speed: float = 1150.0
@export var acceleration: float = 1600.0
@export var turn_speed: float = 10.0

var state: State = State.GROWING
var target_player: Player = null
var playfield_ref: Node2D = null
var local_offset: Vector2 = Vector2.ZERO

var _growth_timer: float = 0.0
var _current_speed: float = 260.0
var _velocity_dir: Vector2 = Vector2.UP
var _current_target: Area2D = null
var _despawn_tween: Tween = null

@onready var sprite: Sprite2D = %Sprite2D
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	collision_layer = 4 # Layer 3 = player_bullets
	collision_mask = 8  # Layer 4 = enemies
	area_entered.connect(_on_area_entered)
	
	if sprite:
		sprite.texture = ACTIVE_TEX
	
	scale = Vector2(min_scale, min_scale)
	monitoring = false
	monitorable = false
	
	# Initial forward launch vector with slight outward bias based on offset
	_velocity_dir = Vector2(local_offset.x * 0.015, -1.0).normalized()
	_current_speed = initial_speed

func setup(player: Player, playfield: Node2D, offset: Vector2) -> void:
	target_player = player
	playfield_ref = playfield
	local_offset = offset
	scale = Vector2(min_scale, min_scale)
	monitoring = false
	monitorable = false
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D") if has_node("%Sprite2D") else get_node_or_null("Sprite2D")
	if is_instance_valid(player):
		global_position = player.global_position + local_offset
	_velocity_dir = Vector2(local_offset.x * 0.015, -1.0).normalized()

func _physics_process(delta: float) -> void:
	match state:
		State.GROWING:
			_process_growing(delta)
		State.HOMING:
			_process_homing(delta)
		State.SPENT_DESPAWN:
			_process_despawn(delta)

func _process_growing(delta: float) -> void:
	_growth_timer += delta
	var progress: float = clampf(_growth_timer / growth_duration, 0.0, 1.0)
	
	# Follow Reimu as she moves while forming
	if is_instance_valid(target_player):
		global_position = target_player.global_position + local_offset
	
	var current_scale: float = lerpf(min_scale, max_scale, progress)
	scale = Vector2(current_scale, current_scale)
	
	if sprite:
		sprite.rotation += spin_speed * delta
	
	if progress >= 1.0:
		_transition_to_homing()

func _transition_to_homing() -> void:
	state = State.HOMING
	monitoring = true
	monitorable = true
	_current_speed = initial_speed
	_acquire_closest_target()

func _process_homing(delta: float) -> void:
	if sprite:
		sprite.rotation += spin_speed * delta
	
	# Validate or reacquire target
	if not is_instance_valid(_current_target) or not _is_target_valid(_current_target):
		_acquire_closest_target()
	
	if is_instance_valid(_current_target) and _is_target_valid(_current_target):
		var to_target := (_current_target.global_position - global_position).normalized()
		var current_ang := _velocity_dir.angle()
		var target_ang := to_target.angle()
		var next_ang := rotate_toward(current_ang, target_ang, turn_speed * delta)
		_velocity_dir = Vector2.from_angle(next_ang)
	
	_current_speed = minf(_current_speed + acceleration * delta, max_speed)
	global_position += _velocity_dir * _current_speed * delta
	
	# Despawn if exiting screen boundaries
	if (global_position.x < PLAYFIELD_MIN_X or global_position.x > PLAYFIELD_MAX_X or
		global_position.y < PLAYFIELD_MIN_Y or global_position.y > PLAYFIELD_MAX_Y):
		queue_free()

func _process_despawn(_delta: float) -> void:
	pass

func _is_target_valid(target) -> bool:
	if target == null or not is_instance_valid(target):
		return false
	if not (target is Area2D):
		return false
	if target.is_queued_for_deletion():
		return false
	if "is_dead" in target and target.is_dead:
		return false
	if "is_active" in target and not target.is_active:
		return false
	if "is_vulnerable" in target and not target.is_vulnerable:
		return false
	return true

func _acquire_closest_target() -> void:
	_current_target = null
	if playfield_ref == null or not is_instance_valid(playfield_ref):
		return
	
	var entities: Node2D = null
	if playfield_ref.has_node("%Entities"):
		entities = playfield_ref.get_node("%Entities")
	elif playfield_ref.has_node("Entities"):
		entities = playfield_ref.get_node("Entities")
	
	if entities == null:
		return
	
	var closest_dist_sq: float = INF
	for child in entities.get_children():
		if is_instance_valid(child) and _is_target_valid(child):
			var dist_sq: float = global_position.distance_squared_to(child.global_position)
			if dist_sq < closest_dist_sq:
				closest_dist_sq = dist_sq
				_current_target = child as Area2D

func _on_area_entered(area: Area2D) -> void:
	if state != State.HOMING:
		return
	
	if area.has_method("take_damage"):
		var accepted: bool = area.take_damage(damage, "charge_attack")
		if accepted:
			_start_spent_despawn(area)

func _start_spent_despawn(target_area: Area2D = null) -> void:
	state = State.SPENT_DESPAWN
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D") if has_node("%Sprite2D") else get_node_or_null("Sprite2D")
	if sprite:
		sprite.texture = SPENT_TEX
	
	var travel_dir := _velocity_dir.normalized() if _velocity_dir.length_squared() > 0.001 else Vector2.UP
	
	# Determine exit point on the far side of the enemy hitbox relative to travel direction
	if target_area != null and is_instance_valid(target_area):
		var enemy_pos := target_area.global_position
		var exit_distance: float = 24.0
		var col_shape: CollisionShape2D = target_area.get_node_or_null("CollisionShape2D")
		if col_shape and col_shape.shape:
			if col_shape.shape is CircleShape2D:
				exit_distance = maxf(exit_distance, (col_shape.shape as CircleShape2D).radius + 6.0)
			elif col_shape.shape is RectangleShape2D:
				var half_sz := (col_shape.shape as RectangleShape2D).size * 0.5
				exit_distance = maxf(exit_distance, maxf(half_sz.x, half_sz.y) + 6.0)
			elif col_shape.shape is CapsuleShape2D:
				exit_distance = maxf(exit_distance, (col_shape.shape as CapsuleShape2D).height * 0.5 + 6.0)
		
		var hit_pos := global_position
		var lateral_proj := (hit_pos - enemy_pos) - travel_dir * (hit_pos - enemy_pos).dot(travel_dir)
		global_position = enemy_pos + lateral_proj * 0.4 + travel_dir * exit_distance
	
	# Ease-out coasting trajectory and tumbling rotation out the other side
	var coast_dist := randf_range(50.0, 75.0)
	var end_pos := global_position + travel_dir * coast_dist
	
	var spin_sign := -1.0 if randf() < 0.5 else 1.0
	var spin_rotations := randf_range(1.5, 2.5)
	var target_rotation: float = (sprite.rotation if sprite else rotation) + spin_sign * spin_rotations * TAU
	
	if _despawn_tween and _despawn_tween.is_valid():
		_despawn_tween.kill()
	
	_despawn_tween = create_tween()
	_despawn_tween.set_parallel(true)
	_despawn_tween.tween_property(self, "global_position", end_pos, 0.28).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	if sprite:
		_despawn_tween.tween_property(sprite, "rotation", target_rotation, 0.28).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	_despawn_tween.tween_property(self, "modulate:a", 0.0, 0.28).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
	_despawn_tween.tween_property(self, "scale", scale * 1.15, 0.28).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	_despawn_tween.chain().tween_callback(queue_free)
