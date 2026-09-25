class_name DanmakuIcicleShowerStep
extends DanmakuStep

## Cirno's Level 4 Boss Attack: Shower of Icicles.
## Summons a burst of small blue icicles at randomized positions scattered around
## the boss, each falling straight down at its own randomly-rolled speed. The
## spread in individual fall speeds naturally fans the cluster out into a
## cascading shower as the icicles descend.

@export_group("Bullet Type")
@export var bullet_data: DanmakuBulletData = null

@export_group("Density & Rank Scaling")
## Icicle count at Rank 1
@export var count_min: int = 55
## Icicle count at Rank 16 (same as Rank 1 - only speed scales for this attack)
@export var count_max: int = 55

@export_group("Spawn Spread")
## Horizontal spawn offset from the boss origin, +/- pixels
@export var spawn_offset_x: float = 108.0
## Minimum vertical spawn offset from the boss origin (pixels, negative = above)
@export var spawn_offset_y_min: float = -24.0
## Maximum vertical spawn offset from the boss origin (pixels)
@export var spawn_offset_y_max: float = 192.0
## When > 0, spawn at a random angle and a random 0..spawn_radius distance from the boss
## instead of inside the x/y box above (pl05.ecl sub3: RAND_FLOAT x 64 ECL units)
@export var spawn_radius: float = 0.0

@export_group("Fall Speed")
## Minimum per-icicle fall speed (pixels/sec), constant across ranks
@export var fall_speed_min: float = 140.0
## Maximum per-icicle fall speed at Rank 1 (pixels/sec)
@export var fall_speed_max_rank1: float = 230.0
## Maximum per-icicle fall speed at Rank 16 (pixels/sec)
@export var fall_speed_max_rank16: float = 320.0

@export_group("Timing & Audio")
## Delay (seconds) between each icicle's spawn - they pop out sequentially, not all at once
@export var delay_between_spawns: float = 0.02
@export var sfx: String = "se_tan00"

const DEFAULT_BLUE_ICICLE: DanmakuBulletData = preload("res://resources/bullets/blue_icicle.tres")
const SPAWN_BURST_SCENE: PackedScene = preload("res://scenes/effects/icicle_spawn_burst.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not playfield.has_method("spawn_danmaku_bullet"):
		return

	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_BLUE_ICICLE

	var t: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var count: int = int(round(lerpf(float(count_min), float(count_max), t)))
	var cur_speed_max: float = lerpf(fall_speed_max_rank1, fall_speed_max_rank16, t)

	var effects_layer: Node2D = playfield.get("effects_layer")
	if effects_layer == null:
		effects_layer = playfield.get_node_or_null("%Effects")
	if effects_layer == null:
		effects_layer = playfield

	for i in range(count):
		if not is_instance_valid(playfield):
			return

		var offset_x: float = randf_range(-spawn_offset_x, spawn_offset_x)
		var offset_y: float = randf_range(spawn_offset_y_min, spawn_offset_y_max)
		var spawn_pos: Vector2 = origin + Vector2(offset_x, offset_y)
		if spawn_radius > 0.0:
			spawn_pos = origin + Vector2.from_angle(randf() * TAU) * randf() * spawn_radius
		var fall_speed: float = randf_range(fall_speed_min, cur_speed_max)
		playfield.spawn_danmaku_bullet(data, spawn_pos, DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, fall_speed)

		var burst: Node2D = SPAWN_BURST_SCENE.instantiate()
		burst.position = spawn_pos
		effects_layer.add_child(burst)

		if not sfx.is_empty():
			AudioService.play_sfx(sfx, 0.0, 1.0, true)

		if i < count - 1 and delay_between_spawns > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(delay_between_spawns).timeout
