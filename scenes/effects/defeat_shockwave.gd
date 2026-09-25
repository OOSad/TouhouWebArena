class_name DefeatShockwave
extends Node2D

## Purely visual defeat shockwave that emanates from the defeated player's location.
## Expands with a faint red tint and fades out without affecting gameplay hazards.

const SPIN_SPEED: float = 2.0

@export var start_scale: float = 0.3
@export var target_scale: float = 8.5
@export var duration: float = 0.65
@export var base_color: Color = Color(1.0, 0.22, 0.28, 0.55)

var _elapsed: float = 0.0

@onready var sprite: Sprite2D = $Sprite2D

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	if sprite:
		sprite.scale = Vector2(start_scale, start_scale)
		sprite.modulate = base_color

func setup(spawn_pos: Vector2) -> void:
	position = spawn_pos

func _process(delta: float) -> void:
	_elapsed += delta
	var progress: float = clampf(_elapsed / duration, 0.0, 1.0)
	
	# Ease-out quad expansion: quick burst then gradual coast
	var expand_t: float = 1.0 - pow(1.0 - progress, 3.0)
	var current_scale: float = lerp(start_scale, target_scale, expand_t)
	
	if sprite:
		sprite.scale = Vector2(current_scale, current_scale)
		sprite.rotation += SPIN_SPEED * delta
		var alpha: float = base_color.a * (1.0 - progress)
		sprite.modulate = Color(base_color.r, base_color.g, base_color.b, alpha)
	
	if _elapsed >= duration:
		queue_free()

