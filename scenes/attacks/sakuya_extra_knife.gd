class_name SakuyaExtraKnife
extends Area2D

## One dagger in Sakuya Izayoi's Extra Attack.
## Unlike a normal Extra Attack, no light mote carries this over to the
## opponent's field: the dagger itself spins up at the spot a fairy died on
## the aggressor's own field, then flies the whole distance under its own
## power, crossing directly into the opponent's field mid-flight toward
## wherever the opponent was standing at launch.
##
## Each SubViewport only ever renders its own field, so a real gameplay node
## simply vanishes the instant it crosses that field's edge - it can never be
## drawn over the visible gap between the two split-screen viewports. To make
## the dagger visibly cross that gap, flight rendering is handed off to a
## "ghost" duplicate of the Blade living in the shared screen-space overlay
## (the same layer TravelMote uses). The ghost's position is a single
## uninterrupted straight-line extrapolation anchored to the SOURCE
## container's screen position for the entire flight - it never switches
## reference frames, so it never has a seam to jump across, even though the
## real Area2D (used for actual collision) does reparent into the target
## field's own physics world partway through.

enum State {
	SPINNING,
	FLYING,
	SPENT
}

const PLAYFIELD_WIDTH: float = 600.0
const PLAYFIELD_MIN_Y: float = -80.0
const PLAYFIELD_MAX_Y: float = 1050.0
const FAR_SIDE_MARGIN: float = 200.0

@export var damage: float = 0.7
## Collision threat radius for AI hazard evasion (knife is 56x20; 26px covers the blade length along travel axis)
@export var radius: float = 26.0
@export var spin_duration: float = 0.22
@export var flight_speed: float = 300.0
@export var knife_visual_scale: Vector2 = Vector2(2.0, 2.0)
## Faded out while still over the sender's own field (harmless there, as in 09) - restored to full once it crosses.
@export var pre_cross_alpha: float = 0.4

var state: State = State.SPINNING
var source_playfield: Playfield = null
var target_playfield: Playfield = null
var direction_sign: float = 1.0 # +1 crosses rightward (sender is P1), -1 crosses leftward (sender is P2)
var launch_delay: float = 0.0

var source_container: SubViewportContainer = null
var target_container: SubViewportContainer = null
var overlay: Node2D = null

var _delay_timer: float = 0.0
var _spin_timer: float = 0.0
var _velocity: Vector2 = Vector2.ZERO
var _has_crossed: bool = false
var _ghost: Node2D = null

## How much further apart (in screen pixels) the two SubViewportContainers
## actually are than the two fields' combined logical width - i.e. the real,
## measured width of the visible divider between them.
var _gap_px: float = 0.0
## Uninterrupted position, always measured relative to source_container, used
## only to place the ghost. Never remapped when the real node crosses fields.
var _source_relative_pos: Vector2 = Vector2.ZERO

@onready var blade: Node2D = %Blade
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	collision_layer = 2 # Layer 2 = enemy_bullets (a hazard to whichever player it's currently facing)
	collision_mask = 1  # Layer 1 = player
	body_entered.connect(_on_body_entered)
	monitoring = false
	monitorable = false

	rotation = 0.0
	blade.rotation = 0.0
	blade.scale = knife_visual_scale

func setup(
	p_source: Playfield,
	p_target: Playfield,
	spawn_pos: Vector2,
	p_direction_sign: float,
	delay: float = 0.0,
	p_source_container: SubViewportContainer = null,
	p_target_container: SubViewportContainer = null,
	p_overlay: Node2D = null
) -> void:
	source_playfield = p_source
	target_playfield = p_target
	direction_sign = p_direction_sign
	launch_delay = delay
	source_container = p_source_container
	target_container = p_target_container
	overlay = p_overlay
	position = spawn_pos
	if source_playfield and is_instance_valid(source_playfield):
		source_playfield.register_custom_hazard(self)

func _physics_process(delta: float) -> void:
	match state:
		State.SPINNING:
			_process_spinning(delta)
		State.FLYING:
			_process_flying(delta)
		State.SPENT:
			pass

func _process_spinning(delta: float) -> void:
	_delay_timer += delta
	if _delay_timer < launch_delay:
		return

	_spin_timer += delta
	var progress: float = clampf(_spin_timer / spin_duration, 0.0, 1.0)
	blade.rotation = progress * PI

	if progress >= 1.0:
		_launch()

func _launch() -> void:
	state = State.FLYING
	monitoring = true
	monitorable = true

	var target_local: Vector2 = Vector2(PLAYFIELD_WIDTH * 0.5, 850.0)
	if target_playfield and is_instance_valid(target_playfield) and target_playfield.player and is_instance_valid(target_playfield.player):
		target_local = target_playfield.player.position

	_gap_px = 0.0
	if source_container and target_container and is_instance_valid(source_container) and is_instance_valid(target_container):
		_gap_px = maxf(0.0, absf(target_container.global_position.x - source_container.global_position.x) - PLAYFIELD_WIDTH)

	# The opponent's field sits directly beyond this field's edge in the
	# direction of travel, so offsetting by one playfield width lets us aim
	# a single straight-line vector that carries across both fields (the
	# visual gap between them doesn't affect gameplay/aiming, only rendering).
	var combined_target := Vector2(target_local.x + direction_sign * PLAYFIELD_WIDTH, target_local.y)
	var dir: Vector2 = (combined_target - position).normalized()

	_velocity = dir * flight_speed
	rotation = dir.angle()
	blade.rotation = 0.0

	_source_relative_pos = position
	_show_ghost()

func _process_flying(delta: float) -> void:
	position += _velocity * delta
	_source_relative_pos += _velocity * delta

	if not _has_crossed:
		var full_threshold: float = PLAYFIELD_WIDTH + _gap_px
		var crossed: bool = (direction_sign > 0.0 and _source_relative_pos.x > full_threshold) or (direction_sign < 0.0 and _source_relative_pos.x < -_gap_px)
		if crossed:
			_cross_to_target_field()

	_update_ghost()

	if (position.y < PLAYFIELD_MIN_Y or position.y > PLAYFIELD_MAX_Y or
		position.x < -FAR_SIDE_MARGIN or position.x > PLAYFIELD_WIDTH + FAR_SIDE_MARGIN):
		_despawn()

func _cross_to_target_field() -> void:
	if target_playfield == null or not is_instance_valid(target_playfield):
		_despawn()
		return

	var target_bullets: Node = target_playfield.get_node_or_null("%Bullets")
	if target_bullets == null:
		_despawn()
		return

	var full_threshold: float = PLAYFIELD_WIDTH + _gap_px
	var new_x: float = _source_relative_pos.x - direction_sign * full_threshold
	var carried_position_y: float = position.y
	var carried_velocity: Vector2 = _velocity
	var carried_rotation: float = rotation

	if source_playfield and is_instance_valid(source_playfield):
		source_playfield.unregister_custom_hazard(self)

	get_parent().remove_child(self)
	target_bullets.add_child(self)

	position = Vector2(new_x, carried_position_y)
	rotation = carried_rotation
	_velocity = carried_velocity
	_has_crossed = true

	target_playfield.register_custom_hazard(self)

	if _ghost and is_instance_valid(_ghost):
		_ghost.modulate.a = 1.0

## Builds a screen-space duplicate of the Blade so the dagger can be seen
## flying over the gap between the two split-screen viewports, where neither
## SubViewport actually renders anything.
func _show_ghost() -> void:
	if overlay == null or not is_instance_valid(overlay):
		return
	if source_container == null or not is_instance_valid(source_container):
		return

	blade.visible = false
	_ghost = blade.duplicate()
	_ghost.visible = true
	_ghost.modulate.a = pre_cross_alpha
	overlay.add_child(_ghost)
	_update_ghost()

func _update_ghost() -> void:
	if _ghost == null or not is_instance_valid(_ghost):
		return
	if source_container == null or not is_instance_valid(source_container):
		return

	_ghost.global_position = source_container.global_position + _source_relative_pos
	_ghost.global_rotation = rotation

func _remove_ghost() -> void:
	if _ghost and is_instance_valid(_ghost):
		_ghost.queue_free()
	_ghost = null
	if is_instance_valid(blade):
		blade.visible = true

func _on_body_entered(body: Node2D) -> void:
	if state != State.FLYING:
		return
	if not _has_crossed:
		# Harmless to the sender while still over their own field, as in 09 -
		# only a real hazard once it has crossed to the opponent's side.
		return

	if body is Player:
		if body.has_method("take_damage"):
			body.take_damage(damage, self)
		_despawn()

## Heavy shockwaves (hit landings and spellcards) wipe the defending field clean.
## A dagger still over the sender's own side is harmless there, so it cannot be
## swatted down there either - only once it has crossed is it fair game.
func take_damage(_amount: float, source: String = "bullet") -> bool:
	if source == "heavy_shockwave" and state == State.FLYING and _has_crossed:
		_despawn()
		return true
	return false

func _despawn() -> void:
	if state == State.SPENT:
		return
	state = State.SPENT
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	if source_playfield and is_instance_valid(source_playfield):
		source_playfield.unregister_custom_hazard(self)
	if target_playfield and is_instance_valid(target_playfield):
		target_playfield.unregister_custom_hazard(self)
	queue_free()

## Round resets (e.g. clear_all_bullets) can free this node directly instead of
## going through _despawn() - catching cleanup here too guarantees the ghost
## (a separate node living in the overlay, outside this node's own branch of
## the tree) never survives orphaned no matter how this knife is removed.
func _exit_tree() -> void:
	_remove_ghost()
