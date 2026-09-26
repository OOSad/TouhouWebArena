class_name LyricaChargeNote
extends Area2D

## One note of Lyrica Prismriver's Level 1 Charge Attack.
## pl06.sht's third shot list: speed 12 px/frame (1500 px/s at the 2.0833 field scale), an
## 18x48 hitbox, damage 10 (a normal shot's). Drawn with her normal shot's note.

const PLAYFIELD_MIN_X: float = -80.0
const PLAYFIELD_MAX_X: float = 680.0
const PLAYFIELD_MIN_Y: float = -80.0
const PLAYFIELD_MAX_Y: float = 1040.0

@export var speed: float = 1500.0
@export var damage: float = 1.0

var direction: Vector2 = Vector2.UP

@onready var sprite: Sprite2D = %Sprite2D

func _ready() -> void:
	collision_layer = 4 # Layer 3 = player_bullets
	collision_mask = 8  # Layer 4 = enemies
	area_entered.connect(_on_area_entered)

func setup(p_pos: Vector2, texture: Texture2D) -> void:
	global_position = p_pos
	if texture:
		sprite.texture = texture

func _physics_process(delta: float) -> void:
	global_position += direction * speed * delta
	if global_position.x < PLAYFIELD_MIN_X or global_position.x > PLAYFIELD_MAX_X or \
	   global_position.y < PLAYFIELD_MIN_Y or global_position.y > PLAYFIELD_MAX_Y:
		queue_free()

func _on_area_entered(area: Area2D) -> void:
	if area.has_method("take_damage") and area.take_damage(damage, "charge_attack"):
		set_deferred("monitoring", false)
		queue_free()
