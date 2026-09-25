class_name DanmakuDoubleRingStep
extends DanmakuStep

## Reimu's Level 4 Boss Attack 4: Double Circles of Pellets
## Spawns two concentric rings of small pellets (one red, one white) at the boss's position.
## The two rings "walk" the circumference in opposite directions (red clockwise, white counter-clockwise)
## while smoothly expanding outward radially until leaving the playfield.
## At Rank 1: 36 red + 36 white = 72 bullets total.
## At Rank 16: 52 red + 52 white = 104 bullets total.

@export_group("Bullet Types")
@export var bullet_data_red: DanmakuBulletData = null
@export var bullet_data_white: DanmakuBulletData = null

@export_group("Density & Rank Scaling")
## Number of pellets per ring at Rank 1 (36 x 2 = 72 total)
@export var count_min: int = 36
## Number of pellets per ring at Rank 16 (52 x 2 = 104 total)
@export var count_max: int = 52

@export_group("Motion & Expansion")
## Initial spawn radius around boss origin (pixels, 0.0 = stacked at center)
@export var initial_radius: float = 0.0
## Radial expansion speed at Rank 1 (pixels/sec)
@export var radial_speed_min: float = 170.0
## Radial expansion speed at Rank 16 (pixels/sec)
@export var radial_speed_max: float = 220.0
## Angular rotation speed along circumference at Rank 1 (rad/sec)
@export var angular_speed_min: float = 0.2125
## Angular rotation speed along circumference at Rank 16 (rad/sec)
@export var angular_speed_max: float = 0.275
## pl00.ecl sub7: instead of orbiting the centre, each bullet flies at radial_speed with its
## heading turning at angular_speed for curve_duration seconds, then straight (ZUN's timed
## Accelerate effect). The ring is aimed so bullet 0 of the red ring points at the victim.
@export var curve_mode: bool = false
@export var curve_duration: float = 2.0

@export_group("Audio")
@export var sfx: String = "se_tan00"

const DEFAULT_RED_PELLET: DanmakuBulletData = preload("res://resources/bullets/red_pellet.tres")
const DEFAULT_WHITE_PELLET: DanmakuBulletData = preload("res://resources/bullets/white_pellet.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	
	if not sfx.is_empty():
		AudioService.play_sfx(sfx)
	
	var red_data: DanmakuBulletData = bullet_data_red if bullet_data_red else DEFAULT_RED_PELLET
	var white_data: DanmakuBulletData = bullet_data_white if bullet_data_white else DEFAULT_WHITE_PELLET
	
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var count: int = int(round(lerpf(float(count_min), float(count_max), t_rank)))
	var rad_speed: float = lerpf(radial_speed_min, radial_speed_max, t_rank)
	var ang_speed: float = lerpf(angular_speed_min, angular_speed_max, t_rank)
	
	var step_angle: float = TAU / float(count)
	var half_step: float = step_angle / 2.0
	var base_angle: float = 0.0
	if curve_mode:
		var victim: Node2D = playfield.get("player")
		if victim and is_instance_valid(victim) and victim.position != origin:
			base_angle = (victim.position - origin).angle()
	
	for i in range(count):
		if not is_instance_valid(playfield):
			return
		
		var angle_red: float = base_angle + float(i) * step_angle
		var angle_white: float = angle_red + half_step
		
		if curve_mode:
			for pair in [[red_data, angle_red, ang_speed], [white_data, angle_white, -ang_speed]]:
				if playfield.has_method("spawn_danmaku_bullet_curve_line"):
					playfield.call("spawn_danmaku_bullet_curve_line", pair[0], origin, Vector2.from_angle(pair[1]), rad_speed, pair[2], curve_duration)
				else:
					var b: DanmakuBullet = playfield.call("spawn_danmaku_bullet", pair[0], origin, DanmakuBullet.MotionMode.CURVE_THEN_LINE, Vector2.from_angle(pair[1]), rad_speed)
					if b:
						b.curve_angular_speed = pair[2]
						b.curve_duration = curve_duration
			continue
		
		# Red ring: rotates clockwise (+ang_speed)
		playfield.call("spawn_danmaku_bullet_expanding_orbit", red_data, origin, initial_radius, angle_red, rad_speed, ang_speed)
		
		# White ring: rotates counter-clockwise (-ang_speed)
		playfield.call("spawn_danmaku_bullet_expanding_orbit", white_data, origin, initial_radius, angle_white, rad_speed, -ang_speed)

