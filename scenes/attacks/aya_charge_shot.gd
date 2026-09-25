class_name AyaChargeShot
extends Area2D

## One crescent of Aya Shameimaru's Level 1 Charge Attack.
## pl10.sht: speed 18 px/frame (2250 px/s at the 2.0833 field scale), a 32x48 hitbox,
## damage 7 against 10 for a normal shot. pl10.anm script 7 draws it at alpha 0x80 turned
## to its heading. On a hit it stops and script 8 plays in its place: the next three
## frames of the strip, additive, alpha 0xc0 fading to 0 over 30 frames.

const PLAYFIELD_MIN_X: float = -80.0
const PLAYFIELD_MAX_X: float = 680.0
const PLAYFIELD_MIN_Y: float = -80.0
const PLAYFIELD_MAX_Y: float = 1040.0

const FADE_DURATION: float = 30.0 / 60.0
const FADE_START_ALPHA: float = 192.0 / 255.0

@export var speed: float = 2250.0
@export var damage: float = 0.7

var direction: Vector2 = Vector2.UP
var _fading: bool = false
var _fade_time: float = 0.0

@onready var sprite: Sprite2D = %Sprite2D

func _ready() -> void:
	collision_layer = 4 # Layer 3 = player_bullets
	collision_mask = 8  # Layer 4 = enemies
	area_entered.connect(_on_area_entered)

func setup(p_pos: Vector2, p_dir: Vector2) -> void:
	global_position = p_pos
	direction = p_dir.normalized()
	rotation = direction.angle() + PI * 0.5

func _physics_process(delta: float) -> void:
	if _fading:
		_fade_time += delta
		var t := _fade_time / FADE_DURATION
		if t >= 1.0:
			queue_free()
			return
		sprite.frame = 1 + mini(int(t * 3.0), 2)
		sprite.modulate.a = FADE_START_ALPHA * (1.0 - t)
		return

	global_position += direction * speed * delta
	if global_position.x < PLAYFIELD_MIN_X or global_position.x > PLAYFIELD_MAX_X or \
	   global_position.y < PLAYFIELD_MIN_Y or global_position.y > PLAYFIELD_MAX_Y:
		queue_free()

func _on_area_entered(area: Area2D) -> void:
	if _fading:
		return
	if area.has_method("take_damage") and area.take_damage(damage, "charge_attack"):
		_fading = true
		set_deferred("monitoring", false)
		set_deferred("monitorable", false)
		var mat := CanvasItemMaterial.new()
		mat.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD
		sprite.material = mat
		sprite.frame = 1
		sprite.modulate.a = FADE_START_ALPHA
