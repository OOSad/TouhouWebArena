class_name ShotHitShard
extends Node2D

## Visual hit effect spawned when a player's shot penetrates through an enemy.
## Based on Touhou 09: Phantasmagoria of Flower View (Reimu's Shot Effect 1, frame 3).
## Purely cosmetic: no collision detection or damage.

@export var duration: float = 0.24
@export var travel_distance: float = 52.0
@export var min_spin_rotations: float = 1.5
@export var max_spin_rotations: float = 2.5

@onready var sprite: Sprite2D = get_node_or_null("Sprite2D")

var _tween: Tween = null

func setup(spawn_pos: Vector2, shot_direction: Vector2, custom_texture: Texture2D = null, custom_scale: Vector2 = Vector2.ZERO) -> void:
	global_position = spawn_pos
	
	if sprite == null:
		sprite = get_node_or_null("Sprite2D")
	if sprite:
		if custom_texture != null:
			sprite.texture = custom_texture
		if custom_scale != Vector2.ZERO:
			sprite.scale = custom_scale
	
	# Subtle natural angular deviation along the travel vector (+- 8 degrees)
	var base_angle := shot_direction.angle() if shot_direction.length_squared() > 0.001 else -PI * 0.5
	var angle_jitter := randf_range(-0.14, 0.14)
	var travel_dir := Vector2.from_angle(base_angle + angle_jitter)
	
	var actual_dist := randf_range(travel_distance * 0.85, travel_distance * 1.15)
	var target_pos := global_position + travel_dir * actual_dist
	
	# Initial random tumble angle
	rotation = randf_range(0.0, TAU)
	
	# Rapid spin: randomized clockwise or counter-clockwise
	var spin_sign := -1.0 if randf() < 0.5 else 1.0
	var spin_rotations := randf_range(min_spin_rotations, max_spin_rotations)
	var target_rotation := rotation + spin_sign * spin_rotations * TAU
	
	if _tween and _tween.is_valid():
		_tween.kill()
	_tween = create_tween().set_parallel(true)
	
	# Move forward with ease-out deceleration (simulating spent bullet casing momentum)
	_tween.tween_property(self, "global_position", target_pos, duration)\
		.set_ease(Tween.EASE_OUT)\
		.set_trans(Tween.TRANS_QUAD)
	
	# Tumbling rotation during flight
	_tween.tween_property(self, "rotation", target_rotation, duration)\
		.set_ease(Tween.EASE_OUT)\
		.set_trans(Tween.TRANS_CUBIC)
	
	# Fade out during the second half of travel
	var fade_delay := duration * 0.45
	var fade_duration := duration - fade_delay
	_tween.tween_property(self, "modulate:a", 0.0, fade_duration)\
		.set_delay(fade_delay)\
		.set_ease(Tween.EASE_IN)\
		.set_trans(Tween.TRANS_QUAD)
	
	_tween.chain().tween_callback(queue_free)