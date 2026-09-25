class_name DanmakuOvalKnifeStep
extends DanmakuStep

## Youmu's Level 4 Boss Attack 3: Oval of Blue Daggers (迷符「半身大悟」)
## Daggers are fired symmetrically in pairs from Youmu's left and right sides and travel
## along the circumference of a horizontal ellipse (lying on its side) extending downwards.
## Before reaching the bottom apex (without touching or crossing at the bottom center),
## each dagger transitions into linear motion, continuing along its natural heading on the oval
## while randomly fanning slightly left or right into the playfield.
##
## Rank scaling:
## - Rank 1: 11 daggers per side (22 total), oval radii (230, 145), speed 340 px/s, delay 0.075s
## - Rank 16: 26 daggers per side (52 total), oval radii (270, 175), speed 560 px/s, delay 0.035s

@export_group("Bullet Settings")
@export var bullet_data: DanmakuBulletData = null

@export_group("Dagger Counts & Scaling")
@export var knives_per_side_min: int = 11
@export var knives_per_side_max: int = 26

@export_group("Timing & Delays")
## Delay before the first dagger pair fires (during which Youmu charges energy).
@export var initial_charge_delay: float = 0.35
@export var delay_r1: float = 0.075
@export var delay_r16: float = 0.035

@export_group("Oval Geometry (Horizontal Ellipse)")
## Horizontal radius (semi-major axis) lying on its side
@export var radius_x_r1: float = 230.0
@export var radius_x_r16: float = 400.0
## Vertical radius (semi-minor axis)
@export var radius_y_r1: float = 145.0
@export var radius_y_r16: float = 260.0

@export_group("Kinematics")
@export var perimeter_speed_r1: float = 225.0
@export var perimeter_speed_r16: float = 410.0
@export var fan_speed_r1: float = 240.0
@export var fan_speed_r16: float = 400.0

@export_group("Split & Fan Out Angles")
## Angle along the oval where daggers detach and split (degrees from 0 = rightmost, 90 = bottom apex)
## E.g. 58 degrees means it splits around 32 degrees BEFORE the bottom apex, leaving a wide gap so knives don't touch
@export var split_angle_deg: float = 58.0
## Angular deflection range when randomly choosing to veer left or right relative to oval tangent heading
@export var fan_deflection_min_deg: float = 8.0
@export var fan_deflection_max_deg: float = 20.0

@export_group("ZUN Curving Pairs (pl03.ecl sub4)")
## Replaces the ellipse: every zun_interval, one sword at aim - 90 and one at aim + 90 degrees
## (aim taken once at the start), both turning back toward the aim line at zun_turn rad/s for
## the same random 1.0-1.98 s, then flying straight. knives_per_side_* pairs (rank + 10).
@export var zun_mode: bool = false
@export var zun_speed_r1: float = 237.5
@export var zun_speed_r16: float = 425.0
@export var zun_turn: float = 1.5708
@export var zun_interval: float = 8.0 / 60.0

@export_group("Sound Effects")
## Sound effect played during the attack (e.g. "se_tan00").
@export var sfx: String = "se_tan00"

const DEFAULT_BULLET_DATA: DanmakuBulletData = preload("res://resources/bullets/blue_knife.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	
	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_BULLET_DATA
	if zun_mode:
		await _execute_zun(playfield, origin, rank, data)
		return
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var pairs_count: int = int(round(lerpf(float(knives_per_side_min), float(knives_per_side_max), t_rank)))
	var cur_delay: float = lerpf(delay_r1, delay_r16, t_rank)
	var radius_x: float = lerpf(radius_x_r1, radius_x_r16, t_rank)
	var radius_y: float = lerpf(radius_y_r1, radius_y_r16, t_rank)
	var p_speed: float = lerpf(perimeter_speed_r1, perimeter_speed_r16, t_rank)
	var f_speed: float = lerpf(fan_speed_r1, fan_speed_r16, t_rank)
	
	# Determine oval center anchored below Youmu's position
	var spawn_pos: Vector2 = origin
	var active_boss: Node2D = playfield.get("active_boss")
	if active_boss and is_instance_valid(active_boss):
		spawn_pos = active_boss.position
	
	var ellipse_center := Vector2(spawn_pos.x, spawn_pos.y + radius_y)
	var ellipse_radii := Vector2(radius_x, radius_y)
	
	# Angular speed along the perimeter based on average radius
	var r_avg: float = maxf((radius_x + radius_y) * 0.5, 10.0)
	var base_omega: float = p_speed / r_avg
	
	# Initial charge delay before attack fires
	if initial_charge_delay > 0.0:
		if is_instance_valid(playfield) and playfield.is_inside_tree():
			await playfield.get_tree().create_timer(initial_charge_delay).timeout
	
	if not is_instance_valid(playfield):
		return
	
	# Fire dagger pairs in rapid succession
	for i in range(pairs_count):
		if not is_instance_valid(playfield):
			return
		
		# Target split angle for this pair (with slight organic jitter +- 2.5 deg)
		var split_jitter: float = randf_range(-2.5, 2.5)
		var pair_split_deg: float = split_angle_deg + split_jitter
		
		# --- Left stream dagger: counter-clockwise (omega < 0) ---
		# Starts near top apex (-PI/2) and sweeps around the left curve
		var left_target_phi: float = deg_to_rad(-180.0 - pair_split_deg)
		var left_vel_x: float = -radius_x * sin(left_target_phi) * (-base_omega)
		var left_vel_y: float = radius_y * cos(left_target_phi) * (-base_omega)
		var left_tangent_dir: Vector2 = Vector2(left_vel_x, left_vel_y).normalized()
		
		var left_choose_left: bool = randf() < 0.5
		var left_deflection: float = deg_to_rad(randf_range(fan_deflection_min_deg, fan_deflection_max_deg))
		var left_fan_dir: Vector2 = left_tangent_dir.rotated(-left_deflection if left_choose_left else left_deflection)
		
		playfield.call(
			"spawn_danmaku_bullet_ellipse_then_line",
			data,
			ellipse_center,
			ellipse_radii,
			-PI * 0.5 - 0.11, # slight offset to the left of Youmu
			-base_omega,
			left_target_phi,
			left_fan_dir,
			f_speed,
			0.0,
			0.0
		)
		
		# --- Right stream dagger: clockwise (omega > 0) ---
		# Starts near top apex (-PI/2) and sweeps around the right curve
		var right_target_phi: float = deg_to_rad(pair_split_deg)
		var right_vel_x: float = -radius_x * sin(right_target_phi) * base_omega
		var right_vel_y: float = radius_y * cos(right_target_phi) * base_omega
		var right_tangent_dir: Vector2 = Vector2(right_vel_x, right_vel_y).normalized()
		
		var right_choose_left: bool = randf() < 0.5
		var right_deflection: float = deg_to_rad(randf_range(fan_deflection_min_deg, fan_deflection_max_deg))
		var right_fan_dir: Vector2 = right_tangent_dir.rotated(-right_deflection if right_choose_left else right_deflection)
		
		playfield.call(
			"spawn_danmaku_bullet_ellipse_then_line",
			data,
			ellipse_center,
			ellipse_radii,
			-PI * 0.5 + 0.11, # slight offset to the right of Youmu
			base_omega,
			right_target_phi,
			right_fan_dir,
			f_speed,
			0.0,
			0.0
		)
		
		if not sfx.is_empty():
			AudioService.play_sfx(sfx)
		
		if i < pairs_count - 1 and cur_delay > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(cur_delay).timeout

func _execute_zun(playfield: Node2D, origin: Vector2, rank: int, data: DanmakuBulletData) -> void:
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var pairs: int = int(round(lerpf(float(knives_per_side_min), float(knives_per_side_max), t_rank)))
	var speed: float = lerpf(zun_speed_r1, zun_speed_r16, t_rank)
	
	var spawn_pos: Vector2 = origin
	var active_boss: Node2D = playfield.get("active_boss")
	if active_boss and is_instance_valid(active_boss):
		spawn_pos = active_boss.position
	var aim: float = PI / 2.0
	var victim: Node2D = playfield.get("player")
	if victim and is_instance_valid(victim) and victim.position != spawn_pos:
		aim = (victim.position - spawn_pos).angle()
	
	for i in range(pairs):
		if not is_instance_valid(playfield):
			return
		if active_boss and is_instance_valid(active_boss):
			spawn_pos = active_boss.position
		# ZUN: RAND_INT % 60 + 60 frames, shared by both swords of the pair
		var turn_time: float = float(randi() % 60 + 60) / 60.0
		for side in [-1.0, 1.0]:
			var dir: Vector2 = Vector2.from_angle(aim + side * PI / 2.0)
			var turn: float = -side * zun_turn
			if playfield.has_method("spawn_danmaku_bullet_curve_line"):
				playfield.call("spawn_danmaku_bullet_curve_line", data, spawn_pos, dir, speed, turn, turn_time)
			else:
				var b: DanmakuBullet = playfield.call("spawn_danmaku_bullet", data, spawn_pos, DanmakuBullet.MotionMode.CURVE_THEN_LINE, dir, speed)
				if b:
					b.curve_angular_speed = turn
					b.curve_duration = turn_time
		if not sfx.is_empty():
			AudioService.play_sfx(sfx)
		if i < pairs - 1 and playfield.is_inside_tree():
			await playfield.get_tree().create_timer(zun_interval).timeout
