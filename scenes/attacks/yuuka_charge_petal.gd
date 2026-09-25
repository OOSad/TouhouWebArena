class_name YuukaChargePetal
extends Area2D

## Yuuka Kazami's Level 1 Charge Attack Projectile: Beauty of Nature
## A spinning flower bud projectile surrounded by radiating needle petals.
## Authentic PoFV characteristics from pl09.sht and pl09.anm.txt (script 7):
## - Speed: 8.0 px/frame (1000.0 px/s); the 16.0 beside it in pl09.sht is the 16x16 hitbox
## - Continuous blossom rotation at 24.0 rad/s (0.4 rad/frame)
## - Damage: 15 per petal in pl09.sht against 10 for a normal shot = 1.5

const PLAYFIELD_MIN_X: float = -80.0
const PLAYFIELD_MAX_X: float = 680.0
const PLAYFIELD_MIN_Y: float = -80.0
const PLAYFIELD_MAX_Y: float = 1040.0

@export var speed: float = 1000.0
@export var damage: float = 1.5
@export var spin_rate: float = 24.0

var direction: Vector2 = Vector2.UP
var target_player: Node2D = null
var playfield_ref: Node2D = null
var _has_hit: bool = false
var _alive_time: float = 0.0

@onready var sprite: Sprite2D = %Sprite2D
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	collision_layer = 4 # Layer 3 = player_bullets
	collision_mask = 8  # Layer 4 = enemies
	area_entered.connect(_on_area_entered)
	modulate.a = 0.0

func setup(p_player: Node2D, p_playfield: Node2D, p_dir: Vector2, p_pos: Vector2) -> void:
	target_player = p_player
	playfield_ref = p_playfield
	direction = p_dir.normalized()
	global_position = p_pos

func _physics_process(delta: float) -> void:
	_alive_time += delta
	# Fast 2-frame fade in from script 7
	if modulate.a < 1.0:
		modulate.a = minf(1.0, _alive_time / (2.0 / 60.0))
	
	global_position += direction * speed * delta
	if sprite:
		sprite.rotation += spin_rate * delta
	
	# Despawn if outside playfield boundaries
	if global_position.x < PLAYFIELD_MIN_X or global_position.x > PLAYFIELD_MAX_X or \
	   global_position.y < PLAYFIELD_MIN_Y or global_position.y > PLAYFIELD_MAX_Y:
		queue_free()

func _on_area_entered(area: Area2D) -> void:
	if _has_hit:
		return
	
	if area.has_method("take_damage"):
		var accepted: bool = area.take_damage(damage, "charge_attack")
		if accepted:
			_has_hit = true
			set_deferred("monitoring", false)
			set_deferred("monitorable", false)
			queue_free()

