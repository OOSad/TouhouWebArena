class_name ShotCrumbleEffect
extends Node2D

## Visual hit effect spawned when a player's shot crumbles/shatters upon hitting an enemy.
## Plays an authentic 3-frame sprite animation in-place at the impact location (Touhou 09 Script 8).
## Purely cosmetic: no collision detection or damage.

@export var frame_duration: float = 0.08  # ~5 ticks at 60 FPS, matching Touhou 09 pl05.anm Script 8
@export var frames: Array[Texture2D] = []

@onready var sprite: Sprite2D = get_node_or_null("%Sprite2D") if has_node("%Sprite2D") else get_node_or_null("Sprite2D")

var _current_frame: int = 0
var _timer: float = 0.0

func setup(spawn_pos: Vector2, custom_frames: Array[Texture2D] = [], custom_scale: Vector2 = Vector2(1.5, 1.5), custom_rotation: float = 0.0) -> void:
	global_position = spawn_pos
	rotation = custom_rotation
	if custom_frames.size() > 0:
		frames = custom_frames
	
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D") if has_node("%Sprite2D") else get_node_or_null("Sprite2D")
	if sprite:
		if custom_scale != Vector2.ZERO:
			sprite.scale = custom_scale
		if frames.size() > 0:
			sprite.texture = frames[0]

func _process(delta: float) -> void:
	_timer += delta
	if _timer >= frame_duration:
		_timer -= frame_duration
		_current_frame += 1
		if _current_frame < frames.size():
			if sprite:
				sprite.texture = frames[_current_frame]
		else:
			queue_free()

