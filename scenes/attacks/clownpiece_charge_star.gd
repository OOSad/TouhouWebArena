class_name ClownpieceChargeStar
extends Area2D

## One star of Clownpiece's charge attack (`ClownpieceChargeAttack`): flies straight on,
## spinning, and breaks on the first enemy it hurts, like Yuuka's charge petals.

const PLAYFIELD_MIN_X: float = -80.0
const PLAYFIELD_MAX_X: float = 680.0
const PLAYFIELD_MIN_Y: float = -80.0
const PLAYFIELD_MAX_Y: float = 1040.0
## Her Starry Illusion star (LoLK's pink-red big star), filled from th15.dat at startup.
const STAR_DATA: DanmakuBulletData = preload("res://resources/bullets/clownpiece_big_star_red.tres")

@export var speed: float = 800.0
## As Yuuka's petals: 1.5 a normal shot.
@export var damage: float = 1.5
@export var spin_rate: float = 12.0

var direction: Vector2 = Vector2.UP
var _has_hit: bool = false

@onready var sprite: Sprite2D = %Sprite2D

func _ready() -> void:
	collision_layer = 4 # Layer 3 = player_bullets
	collision_mask = 8  # Layer 4 = enemies
	area_entered.connect(_on_area_entered)
	sprite.texture = STAR_DATA.texture

func setup(p_dir: Vector2, p_pos: Vector2) -> void:
	direction = p_dir.normalized()
	global_position = p_pos

func _physics_process(delta: float) -> void:
	global_position += direction * speed * delta
	sprite.rotation += spin_rate * delta
	if global_position.x < PLAYFIELD_MIN_X or global_position.x > PLAYFIELD_MAX_X or \
	   global_position.y < PLAYFIELD_MIN_Y or global_position.y > PLAYFIELD_MAX_Y:
		queue_free()

func _on_area_entered(area: Area2D) -> void:
	if _has_hit:
		return
	if area.has_method("take_damage") and area.take_damage(damage, "charge_attack"):
		_has_hit = true
		set_deferred("monitoring", false)
		set_deferred("monitorable", false)
		queue_free()
