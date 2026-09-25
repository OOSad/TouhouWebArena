class_name DanmakuNarrowingSweepStep
extends DanmakuStep

## Sakuya's Level 4 Boss Attack 3: Sweep of Yellow Daggers.
## Imagines a tight circle around the boss and fires yellow daggers sequentially from the front
## (player-facing) half of that circle - starting at the two outermost positions (near-horizontal,
## left and right) and stepping inward in lockstep pairs each delay_between_steps, gradually
## narrowing toward straight down without ever fully reaching it (end_angle_deg stops short of 0).
## Each dagger spawns at the boss's own position and simply flies outward at its fixed angle for
## its entire straight-line flight (MotionMode.LINEAR, no homing) - the "circle" is a purely
## angular fan, not an actual spatial ring offset from the boss.
## Fixed at daggers_per_side (16) on each side at every rank - only speed scales with rank.

@export_group("Bullet Settings")
@export var bullet_data: DanmakuBulletData = null

@export_group("Sweep Geometry - Fixed at Every Rank")
@export var daggers_per_side: int = 16
## Angle (degrees) from straight-down for the very first pair fired - near-horizontal (left/right).
@export var start_angle_deg: float = 85.0
## Angle (degrees) from straight-down for the very last pair fired - stops short of center (0°).
@export var end_angle_deg: float = 18.0
## pl02.ecl sub5: centre the sweep on the victim (aimed once, as the attack starts) instead of straight down
@export var aim_at_player: bool = false
## Seconds between each successive pair - both sides step inward together, not one at a time.
@export var delay_between_steps: float = 0.07

@export_group("Kinematics - Rank Scaled")
@export var speed_min: float = 240.0
@export var speed_max: float = 400.0

@export_group("Sound Effects")
@export var sfx: String = "se_tan00"

const DEFAULT_DAGGER_DATA: DanmakuBulletData = preload("res://resources/bullets/sakuya_knife_yellow.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return

	if daggers_per_side <= 0:
		return

	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_DAGGER_DATA
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var speed: float = lerpf(speed_min, speed_max, t_rank)

	var spawn_pos: Vector2 = origin
	var active_boss: Node2D = playfield.get("active_boss")
	if active_boss and is_instance_valid(active_boss):
		spawn_pos = active_boss.position

	var center_dir: Vector2 = Vector2.DOWN
	if aim_at_player:
		var player_node: Node2D = playfield.get("player")
		if player_node and is_instance_valid(player_node) and player_node.position != spawn_pos:
			center_dir = (player_node.position - spawn_pos).normalized()

	var angle_step_deg: float = 0.0
	if daggers_per_side > 1:
		angle_step_deg = (start_angle_deg - end_angle_deg) / float(daggers_per_side - 1)

	for i in range(daggers_per_side):
		if not is_instance_valid(playfield):
			return

		var angle_from_center_deg: float = start_angle_deg - (angle_step_deg * float(i))
		var angle_rad: float = deg_to_rad(angle_from_center_deg)
		var left_dir: Vector2 = center_dir.rotated(angle_rad)
		var right_dir: Vector2 = center_dir.rotated(-angle_rad)

		playfield.call("spawn_danmaku_bullet", data, spawn_pos, DanmakuBullet.MotionMode.LINEAR, left_dir, speed)
		playfield.call("spawn_danmaku_bullet", data, spawn_pos, DanmakuBullet.MotionMode.LINEAR, right_dir, speed)

		if not sfx.is_empty():
			AudioService.play_sfx(sfx)

		if i < daggers_per_side - 1 and delay_between_steps > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(delay_between_steps).timeout
