class_name DanmakuKnifeStrafeStep
extends DanmakuStep

## Sakuya's Level 4 Boss Attack 4: Knife Strafing.
## The boss glides to the left playfield boundary, then rapidly dashes to the right boundary
## (BossCharacter.perform_strafe_dash, an unbounded edge-to-edge traversal distinct from the
## normal roam_bounds-clamped hop). No daggers fire during the initial glide-to-start-edge -
## only once the dash itself begins. While dashing, at daggers_per_line evenly-spaced ticks
## spanning the whole dash, it fires line_count daggers simultaneously from its current (moving)
## position - one per "line". Each line has its own angle (a fan from near-vertical to
## near-horizontal, all leaning left/behind the dash direction, "as if being pulled by the boss'
## sprite") that stays constant for most of the dash, so a line's daggers cascade into a diagonal
## streak purely from travel-time differences: the tick 0 dagger (fired from the left edge) has
## had the whole dash duration to fly, while a mid-dash tick's dagger has barely moved by
## comparison - no curve or rotation math needed, just fixed angles sampled against a moving
## origin. Over the last taper_ticks emissions, though, every line's lean fades linearly back to
## 0 (straight down) - once the boss actually stops at the wall, there's no more sideways "pull"
## on the knives still being thrown, so the final emission fires perfectly straight down.

@export_group("Bullet Settings")
@export var bullet_data: DanmakuBulletData = null

@export_group("Strafe Movement")
@export var left_edge_x: float = 40.0
@export var right_edge_x: float = 560.0
## How long the boss takes to glide from wherever it currently is to left_edge_x before the
## actual dash (and firing) begins - a smooth lerp instead of an instant teleport.
@export var jump_duration: float = 0.4
@export var dash_duration: float = 2.0

@export_group("Line Geometry - Daggers Fixed, Line Count Rank-Scaled")
@export var daggers_per_line: int = 13
@export var lines_min: int = 2
@export var lines_max: int = 6
## Angle (degrees) from straight-down for the line closest to vertical.
@export var angle_min_deg: float = 15.0
## Angle (degrees) from straight-down for the line closest to horizontal (leans furthest behind).
@export var angle_max_deg: float = 72.0
## Over the last N emissions, every line's lean angle fades linearly to 0 (straight down) as the
## boss loses momentum approaching the wall - the very last emission always fires straight down.
@export var taper_ticks: int = 3

@export_group("Kinematics - Rank Scaled")
@export var speed_min: float = 260.0
@export var speed_max: float = 300.0

@export_group("ZUN Strafe (pl02.ecl sub4)")
## Random side (synced). Glide to (start_x, zun_start_y) over jump_duration, hold, then dash
## decelerating to the far edge at zun_end_y over dash_duration while firing daggers_per_line
## fans of rank / 4 + 2 daggers, 90 / n degrees apart, every zun_tick_interval. Fan centre
## starts at zun_left_start_deg (dashing right) or zun_right_start_deg (dashing left, which
## ZUN did not mirror) and turns zun_turn_deg per fan toward straight down.
@export var zun_mode: bool = false
@export var zun_start_y: float = 400.0
@export var zun_end_y: float = 200.0
@export var zun_hold: float = 40.0 / 60.0
@export var zun_tick_interval: float = 4.0 / 60.0
@export var zun_left_start_deg: float = 128.571
@export var zun_right_start_deg: float = 77.143
@export var zun_turn_deg: float = 2.0571

const DEFAULT_DAGGER_DATA: DanmakuBulletData = preload("res://resources/bullets/sakuya_knife_blue.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	# Taken before anything can await: which side she dashes from is attack identity.
	var rng: RandomNumberGenerator = take_sync_rng()
	if zun_mode:
		await _execute_zun(playfield, rank, rng)
		return
	if playfield == null or not is_instance_valid(playfield):
		return

	var active_boss: Node2D = playfield.get("active_boss")
	if active_boss == null or not is_instance_valid(active_boss):
		return

	if daggers_per_line <= 0:
		return

	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_DAGGER_DATA
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var line_count: int = int(round(lerpf(float(lines_min), float(lines_max), t_rank)))
	var speed: float = lerpf(speed_min, speed_max, t_rank)

	if line_count <= 0:
		return

	var dash_y: float = active_boss.position.y
	var start_pos := Vector2(left_edge_x, dash_y)
	var end_pos := Vector2(right_edge_x, dash_y)

	if not active_boss.has_method("perform_strafe_dash"):
		return
	active_boss.call("perform_strafe_dash", start_pos, end_pos, jump_duration, dash_duration)

	# Wait for the glide to the left edge to actually land before firing anything - the dagger
	# lines only start once the dash segment itself begins.
	if active_boss.has_signal("strafe_jump_landed"):
		await active_boss.strafe_jump_landed
	elif is_instance_valid(playfield) and playfield.is_inside_tree():
		await playfield.get_tree().create_timer(jump_duration).timeout

	var tick_interval: float = 0.0
	if daggers_per_line > 1:
		tick_interval = dash_duration / float(daggers_per_line - 1)

	for tick in range(daggers_per_line):
		if not is_instance_valid(playfield) or not is_instance_valid(active_boss):
			return

		# Fades each line's lean back toward 0 (straight down) over the last taper_ticks emissions
		# - by the very last tick, drag_factor is 0 and every line fires straight down.
		var ticks_from_end: int = daggers_per_line - 1 - tick
		var drag_factor: float = 1.0
		if taper_ticks > 0 and ticks_from_end < taper_ticks:
			drag_factor = float(ticks_from_end) / float(taper_ticks)

		var spawn_pos: Vector2 = active_boss.position
		for k in range(line_count):
			var t_line: float = 0.0
			if line_count > 1:
				t_line = float(k) / float(line_count - 1)
			var angle_deg: float = lerpf(angle_min_deg, angle_max_deg, t_line) * drag_factor
			var dir := Vector2.DOWN.rotated(deg_to_rad(angle_deg))
			playfield.call("spawn_danmaku_bullet", data, spawn_pos, DanmakuBullet.MotionMode.LINEAR, dir, speed)

		# Each line's se_tan00 cuts off the previous one instead of stacking, same non-overlapping
		# cadence as Sakuya's own Lv2/3 Time Stop Fan dagger formation.
		AudioService.play_tan_exclusive()

		if tick < daggers_per_line - 1 and tick_interval > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(tick_interval).timeout

func _execute_zun(playfield: Node2D, rank: int, rng: RandomNumberGenerator) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	var active_boss: Node2D = playfield.get("active_boss")
	if active_boss == null or not is_instance_valid(active_boss) or not active_boss.has_method("perform_strafe_dash"):
		return

	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_DAGGER_DATA
	var r: int = clampi(rank, 1, 16)
	var fan_count: int = r / 4 + 2
	var spread: float = deg_to_rad(90.0) / float(fan_count)
	var speed: float = lerpf(speed_min, speed_max, float(r - 1) / 15.0)

	var from_left: bool = (rng.randi() if rng else randi()) % 2 == 0
	var start_x: float = left_edge_x if from_left else right_edge_x
	var end_x: float = right_edge_x if from_left else left_edge_x
	var center: float = deg_to_rad(zun_left_start_deg if from_left else zun_right_start_deg)
	var turn: float = deg_to_rad(-zun_turn_deg if from_left else zun_turn_deg)

	active_boss.call("perform_strafe_dash", Vector2(start_x, zun_start_y), Vector2(end_x, zun_end_y), jump_duration, dash_duration, zun_hold, true)
	if active_boss.has_signal("strafe_jump_landed"):
		await active_boss.strafe_jump_landed
	elif is_instance_valid(playfield) and playfield.is_inside_tree():
		await playfield.get_tree().create_timer(jump_duration + zun_hold).timeout

	for tick in range(daggers_per_line):
		if not is_instance_valid(playfield) or not is_instance_valid(active_boss):
			return
		for k in range(fan_count):
			var dir: Vector2 = Vector2.from_angle(center + (float(k) - float(fan_count - 1) * 0.5) * spread)
			playfield.call("spawn_danmaku_bullet", data, active_boss.position, DanmakuBullet.MotionMode.LINEAR, dir, speed)
		AudioService.play_tan_exclusive()
		center += turn
		if tick < daggers_per_line - 1:
			await playfield.get_tree().create_timer(zun_tick_interval).timeout
