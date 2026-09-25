class_name GrazeSpark
extends Node2D

## Lightweight cosmetic spark spawned at the contact point when grazing a bullet.
## Features procedural drawing of a 4-pointed diamond star with glowing core,
## quick scale pop, and fast ease-in alpha fade (~0.12s) for zero texture overhead.

@export var duration: float = 0.12

func _ready() -> void:
	z_as_relative = false
	z_index = 25

func setup(spawn_pos: Vector2) -> void:
	global_position = spawn_pos
	rotation = randf_range(0.0, TAU)
	scale = Vector2(0.6, 0.6)
	modulate = Color(1.2, 1.4, 2.0, 0.95)
	
	var tw := create_tween().set_parallel(true)
	tw.tween_property(self, "scale", Vector2(1.25, 1.25), duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_property(self, "modulate:a", 0.0, duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	tw.chain().tween_callback(queue_free)

func _draw() -> void:
	var core_col := Color(1.0, 1.0, 1.0, 0.95)
	var outer_col := Color(0.4, 0.85, 1.0, 0.75)
	# 4-pointed diamond star
	var pts := PackedVector2Array([
		Vector2(0, -9), Vector2(2.5, -2.5),
		Vector2(9, 0), Vector2(2.5, 2.5),
		Vector2(0, 9), Vector2(-2.5, 2.5),
		Vector2(-9, 0), Vector2(-2.5, -2.5)
	])
	draw_colored_polygon(pts, outer_col)
	draw_circle(Vector2.ZERO, 2.5, core_col)
