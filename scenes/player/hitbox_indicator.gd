class_name HitboxIndicator
extends Node2D

# Spinning focus hitbox indicator dot (iconic Touhou focus mode)
@export var outer_radius: float = 6.0
@export var inner_radius: float = 3.0
@export var graze_radius: float = 64.0
@export var rotation_speed: float = 3.5

func _ready() -> void:
	visible = false
	z_as_relative = false
	z_index = 35

func _process(delta: float) -> void:
	if visible:
		rotation += rotation_speed * delta
		queue_redraw()

func _draw() -> void:
	# Subtle outer graze perimeter ring during focus (Touhou 19 authentic feedback)
	draw_arc(Vector2.ZERO, graze_radius, 0.0, TAU, 48, Color(1.0, 1.0, 0.6, 0.35), 1.5)
	draw_arc(Vector2.ZERO, graze_radius + 2.0, 0.0, TAU, 48, Color(1.0, 1.0, 0.6, 0.12), 1.0)
	# Soft outer glow
	draw_circle(Vector2.ZERO, outer_radius + 2.0, Color(1.0, 0.2, 0.3, 0.35))
	# Outer colored ring
	draw_circle(Vector2.ZERO, outer_radius, Color(0.95, 0.15, 0.25, 0.85))
	# Inner bright center (exact graze/collision core)
	draw_circle(Vector2.ZERO, inner_radius, Color.WHITE)
	# 4 subtle cross hair spokes to give it the classic spinning look
	var spoke_len: float = outer_radius + 3.0
	var spoke_col := Color(1.0, 1.0, 1.0, 0.7)
	draw_line(Vector2(-spoke_len, 0), Vector2(-outer_radius, 0), spoke_col, 1.5)
	draw_line(Vector2(outer_radius, 0), Vector2(spoke_len, 0), spoke_col, 1.5)
	draw_line(Vector2(0, -spoke_len), Vector2(0, -outer_radius), spoke_col, 1.5)
	draw_line(Vector2(0, outer_radius), Vector2(0, spoke_len), spoke_col, 1.5)

