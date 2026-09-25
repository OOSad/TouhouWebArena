class_name SakuyaKnife
extends Area2D

## One dagger in Sakuya Izayoi's Level 1 Charge Attack: Silver Knife.
## Spawns pointing right, spins 180 degrees in place as a telegraph, then
## launches at extreme speed toward whichever enemy is nearest at that moment.

enum State {
	SPINNING,
	LAUNCHED,
	SPENT
}

const PLAYFIELD_MIN_X: float = -60.0
const PLAYFIELD_MAX_X: float = 660.0
const PLAYFIELD_MIN_Y: float = -80.0
const PLAYFIELD_MAX_Y: float = 1050.0

@export var damage: float = 1.0 # pl02.sht: 10, same as a normal shot
@export var spin_duration: float = 0.22
@export var launch_speed: float = 3200.0
## Stretched along the blade (local X, the direction it points) and thinned across it.
@export var knife_visual_scale: Vector2 = Vector2(6.0, 1.0)

var state: State = State.SPINNING
var target_player: Player = null
var playfield_ref: Node2D = null
var local_offset: Vector2 = Vector2.ZERO
var launch_delay: float = 0.0

var _delay_timer: float = 0.0
var _spin_timer: float = 0.0
var _velocity: Vector2 = Vector2.ZERO

@onready var blade: Node2D = %Blade
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	collision_layer = 4 # Layer 3 = player_bullets
	collision_mask = 8  # Layer 4 = enemies
	area_entered.connect(_on_area_entered)
	monitoring = false
	monitorable = false

	rotation = 0.0
	blade.rotation = 0.0
	blade.scale = knife_visual_scale

func setup(player: Player, playfield: Node2D, offset: Vector2, delay: float = 0.0) -> void:
	target_player = player
	playfield_ref = playfield
	local_offset = offset
	launch_delay = delay
	if is_instance_valid(player):
		global_position = player.global_position + local_offset

func _physics_process(delta: float) -> void:
	match state:
		State.SPINNING:
			_process_spinning(delta)
		State.LAUNCHED:
			_process_launched(delta)
		State.SPENT:
			pass

func _process_spinning(delta: float) -> void:
	# Follow Sakuya while the daggers are still forming at her hitbox
	if is_instance_valid(target_player):
		global_position = target_player.global_position + local_offset

	_delay_timer += delta
	if _delay_timer < launch_delay:
		return

	_spin_timer += delta
	var progress: float = clampf(_spin_timer / spin_duration, 0.0, 1.0)
	blade.rotation = progress * PI

	if progress >= 1.0:
		_launch()

func _launch() -> void:
	state = State.LAUNCHED
	monitoring = true
	monitorable = true

	var dir: Vector2 = Vector2.UP
	var target: Area2D = _find_closest_enemy()
	if target != null:
		dir = (target.global_position - global_position).normalized()

	_velocity = dir * launch_speed
	# Turn the whole knife so the blade, its glow, and the hitbox all face the target
	rotation = dir.angle()
	blade.rotation = 0.0

func _process_launched(delta: float) -> void:
	global_position += _velocity * delta

	if (global_position.x < PLAYFIELD_MIN_X or global_position.x > PLAYFIELD_MAX_X or
		global_position.y < PLAYFIELD_MIN_Y or global_position.y > PLAYFIELD_MAX_Y):
		queue_free()

func _find_closest_enemy() -> Area2D:
	if playfield_ref == null or not is_instance_valid(playfield_ref):
		return null

	var entities: Node2D = null
	if playfield_ref.has_node("%Entities"):
		entities = playfield_ref.get_node("%Entities")
	elif playfield_ref.has_node("Entities"):
		entities = playfield_ref.get_node("Entities")

	if entities == null:
		return null

	var closest: Area2D = null
	var closest_dist_sq: float = INF
	for child in entities.get_children():
		if is_instance_valid(child) and _is_target_valid(child):
			var dist_sq: float = global_position.distance_squared_to(child.global_position)
			if dist_sq < closest_dist_sq:
				closest_dist_sq = dist_sq
				closest = child as Area2D

	return closest

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

func _on_area_entered(area: Area2D) -> void:
	if state != State.LAUNCHED:
		return

	if area.has_method("take_damage"):
		var accepted: bool = area.take_damage(damage, "charge_attack")
		if accepted:
			state = State.SPENT
			queue_free()
