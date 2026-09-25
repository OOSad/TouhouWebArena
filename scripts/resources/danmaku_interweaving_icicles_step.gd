class_name DanmakuInterweavingIciclesStep
extends DanmakuStep

## Cirno's Level 4 Boss Attack: Interweaving Icicles.
## Spawns two fully-overlapping circles of small cyan icicles expanding outward
## from the boss's position (angular_speed = 0.0, pure radial growth, both circles
## sharing identical angular positions so they stay perfectly coincident). Once the
## target radius is reached, both circles hang motionless for a short hold. Then
## every icicle in one circle rotates its heading to point tangentially to its
## relative left (and the other circle's icicles to their relative right) and both
## accelerate forward from a stop, unraveling into two counter-spiraling arms that
## weave past each other as they fly outward.

@export_group("Bullet Type")
@export var bullet_data: DanmakuBulletData = null

@export_group("Density & Rank Scaling")
## Icicles per circle at Rank 1 (x2 circles = 72 total)
@export var count_min: int = 36
## Icicles per circle at Rank 16 (x2 circles = 104 total)
@export var count_max: int = 52

@export_group("Expansion & Hold")
## Target radius (pixels) the circles expand to before holding in place
@export var target_radius: float = 180.0
## Radial expansion speed at Rank 1 (pixels/sec)
@export var expand_speed_min: float = 150.0
## Radial expansion speed at Rank 16 (pixels/sec)
@export var expand_speed_max: float = 180.0
## Time (seconds) the circles hang motionless once they reach target_radius
@export var hold_time: float = 0.25

@export_group("Breakout")
## Angle (degrees) each icicle's heading rotates from radial-outward to reach its
## relative-left/relative-right tangent direction
@export var breakout_angle_deg: float = 90.0
## Acceleration after breakout at Rank 1 (pixels/sec^2)
@export var breakout_accel_min: float = 560.0
## Acceleration after breakout at Rank 16 (pixels/sec^2)
@export var breakout_accel_max: float = 680.0
## Terminal speed after breakout at Rank 1 (pixels/sec)
@export var breakout_max_speed_min: float = 340.0
## Terminal speed after breakout at Rank 16 (pixels/sec)
@export var breakout_max_speed_max: float = 390.0

@export_group("ZUN Interweave (pl05.ecl sub6)")
## Replaces expand/hold/breakout: two rings of count_* icicles (the second on the half steps)
## at zun_speed, each braking to a stop over zun_brake_time (ZUN's AimRel), turning 90 degrees
## from its own heading (ring A one way, ring B the other) and relaunching at the same speed.
@export var zun_mode: bool = false
@export var zun_speed: float = 250.0
@export var zun_brake_time: float = 1.0

@export_group("Audio")
@export var sfx: String = "se_tan00"
@export var breakout_sfx: String = "se_kira00"

const DEFAULT_CYAN_ICICLE: DanmakuBulletData = preload("res://resources/bullets/cyan_icicle.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return

	if not sfx.is_empty():
		AudioService.play_sfx(sfx)

	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_CYAN_ICICLE
	if zun_mode:
		_fire_zun(playfield, origin, rank, data)
		return

	var t: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var count: int = int(round(lerpf(float(count_min), float(count_max), t)))
	var expand_speed: float = lerpf(expand_speed_min, expand_speed_max, t)
	var breakout_accel: float = lerpf(breakout_accel_min, breakout_accel_max, t)
	var breakout_max_speed: float = lerpf(breakout_max_speed_min, breakout_max_speed_max, t)

	var expand_time: float = target_radius / maxf(expand_speed, 1.0)
	var breakout_time: float = expand_time + hold_time
	var angle_offset: float = deg_to_rad(breakout_angle_deg)

	if not breakout_sfx.is_empty() and breakout_time > 0.0 and is_instance_valid(playfield) and playfield.is_inside_tree():
		playfield.get_tree().create_timer(breakout_time).timeout.connect(func():
			if is_instance_valid(playfield):
				AudioService.play_sfx(breakout_sfx)
		)

	if count <= 0:
		return

	var step_angle: float = TAU / float(count)

	for i in range(count):
		if not is_instance_valid(playfield):
			return

		var angle: float = float(i) * step_angle

		# Circle A: breaks out pointing to its relative left (counter-clockwise tangent)
		playfield.call(
			"spawn_danmaku_bullet_expanding_orbit",
			data, origin, 0.0, angle, expand_speed, 0.0,
			expand_time, breakout_time, -angle_offset, breakout_accel, breakout_max_speed, 0.0
		)

		# Circle B: breaks out pointing to its relative right (clockwise tangent)
		playfield.call(
			"spawn_danmaku_bullet_expanding_orbit",
			data, origin, 0.0, angle, expand_speed, 0.0,
			expand_time, breakout_time, angle_offset, breakout_accel, breakout_max_speed, 0.0
		)


func _fire_zun(playfield: Node2D, origin: Vector2, rank: int, data: DanmakuBulletData) -> void:
	var t: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var count: int = int(round(lerpf(float(count_min), float(count_max), t)))
	if count <= 0:
		return
	var step_angle: float = TAU / float(count)
	var turn: float = deg_to_rad(breakout_angle_deg)
	for i in range(count):
		# Ring A on the grid turns one way, ring B on the half steps the other
		for ring in [[0.0, turn], [0.5, -turn]]:
			var dir: Vector2 = Vector2.from_angle((float(i) + ring[0]) * step_angle)
			var b: DanmakuBullet = playfield.call("spawn_danmaku_bullet_decel_home", data, origin, dir, zun_speed, zun_brake_time, 0.0, zun_speed)
			if b:
				b.relaunch_relative = true
				b.relaunch_turn = ring[1]
	if not breakout_sfx.is_empty() and playfield.is_inside_tree():
		playfield.get_tree().create_timer(zun_brake_time).timeout.connect(func():
			if is_instance_valid(playfield):
				AudioService.play_sfx(breakout_sfx)
		)
