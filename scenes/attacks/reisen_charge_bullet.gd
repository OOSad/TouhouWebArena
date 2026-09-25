class_name ReisenChargeBullet
extends Area2D

## The single large cartridge of Reisen Udongein Inaba's Level 1 Charge Attack.
##
## It does not simply appear: it resolves into existence, starting oversized and fully
## transparent and settling to its normal size as it becomes opaque. It then flies
## straight ahead. On hitting something it damages that target, fades back out, and
## leaves a SparkAura burst behind at the point of impact.

enum State {
	MATERIALISING,
	FLYING,
	SPENT
}

const AURA_SCENE: PackedScene = preload("res://scenes/effects/spark_aura.tscn")

## The aura keeps hitting everything inside it for its whole 1.1s life. Footage of the charge
## against Lily White puts cartridge plus aura at about 44 normal shots, so ~41 over 11 hits.
const AURA_TICK_INTERVAL: float = 0.1
const AURA_DAMAGE_PER_TICK: float = 3.7

const PLAYFIELD_MIN_X: float = -60.0
const PLAYFIELD_MAX_X: float = 660.0
const PLAYFIELD_MIN_Y: float = -80.0
const PLAYFIELD_MAX_Y: float = 1050.0

## Dealt to whatever the cartridge itself strikes (pl04.sht: 30, a normal shot being 10).
@export var damage: float = 3.0
@export var speed: float = 1500.0
## How long the bullet takes to resolve from oversized-and-invisible to its real size.
@export var materialise_duration: float = 0.09
@export var materialise_start_scale: float = 3.2
## How long it takes to fade back out once it has struck something.
@export var fade_out_duration: float = 0.07

var state: State = State.MATERIALISING
var playfield_ref: Node2D = null

var _timer: float = 0.0
var _direction: Vector2 = Vector2.UP

@onready var sprite: Sprite2D = %Sprite
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	collision_layer = 4 # Layer 3 = player_bullets
	collision_mask = 8  # Layer 4 = enemies
	area_entered.connect(_on_area_entered)
	# Nothing can be hit until it has finished resolving into place.
	monitoring = false
	monitorable = false
	_apply_materialise(0.0)

func setup(player: Player, playfield: Node2D, direction: Vector2 = Vector2.UP) -> void:
	playfield_ref = playfield
	if direction.length_squared() > 0.0001:
		_direction = direction.normalized()
	# Art points up at zero rotation, so aiming along `d` needs d.angle() + PI/2 - the same
	# convention the standard shot uses in player.gd. Subtracting flips it a full 180.
	rotation = _direction.angle() + PI * 0.5
	if is_instance_valid(player):
		global_position = player.global_position
	AudioService.play_sfx("se_tan00")

func _physics_process(delta: float) -> void:
	match state:
		State.MATERIALISING:
			_timer += delta
			var t: float = clampf(_timer / materialise_duration, 0.0, 1.0)
			_apply_materialise(t)
			# It already travels while resolving, so the entrance does not stall the shot.
			global_position += _direction * speed * delta
			if t >= 1.0:
				state = State.FLYING
				monitoring = true
				monitorable = true
		State.FLYING:
			global_position += _direction * speed * delta
			if (global_position.x < PLAYFIELD_MIN_X or global_position.x > PLAYFIELD_MAX_X
					or global_position.y < PLAYFIELD_MIN_Y or global_position.y > PLAYFIELD_MAX_Y):
				queue_free()
		State.SPENT:
			_timer += delta
			var f: float = clampf(_timer / fade_out_duration, 0.0, 1.0)
			if sprite:
				sprite.modulate.a = 1.0 - f
			if f >= 1.0:
				queue_free()

## t = 0 is oversized and invisible, t = 1 is the real bullet.
func _apply_materialise(t: float) -> void:
	if sprite == null:
		return
	var eased: float = 1.0 - pow(1.0 - t, 3.0)
	var s: float = lerpf(materialise_start_scale, 1.0, eased)
	sprite.scale = Vector2(s, s)
	sprite.modulate.a = eased

func _on_area_entered(area: Area2D) -> void:
	if state != State.FLYING:
		return
	if not area.has_method("take_damage"):
		return
	var accepted: bool = area.take_damage(damage, "charge_attack")
	if not accepted:
		return

	_burst()

func _burst() -> void:
	state = State.SPENT
	_timer = 0.0
	monitoring = false
	monitorable = false

	var aura: SparkAura = AURA_SCENE.instantiate()
	var parent: Node = get_parent()
	if parent:
		# Set up before entering the tree: the aura deals its first hit in _ready.
		aura.setup(global_position, playfield_ref, AURA_DAMAGE_PER_TICK)
		aura.tick_interval = AURA_TICK_INTERVAL
		parent.add_child(aura)
