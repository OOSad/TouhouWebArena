class_name DanmakuClawStep
extends DanmakuStep

## Reimu's Level 4 Boss Attack 3: Pellet and Talisman Claw
## Fires 3 angled strips of non-homing pellets in a fan/claw spread aimed at the opponent.
## The center strip has an overlaid strip of red non-homing talismans.
## All bullets in each strip have ascending velocities (speed_min -> speed_max), creating
## an "unfurling" claw effect. The center pellets feature higher acceleration than the side
## strips and center talismans, surging forward as the attack travels.

@export_group("Bullet Types")
@export var pellet_bullet_data: DanmakuBulletData = null
@export var talisman_bullet_data: DanmakuBulletData = null

@export_group("Claw Geometry")
@export var fan_spread_angle_deg: float = 14.0

@export_group("Rank Scaling")
@export var bullets_min: int = 9   ## Bullets per strip at Rank 1 (9 x 3 pellets + 9 talismans = 36 total)
@export var bullets_max: int = 16  ## Bullets per strip at Rank 16 (16 x 3 pellets + 16 talismans = 64 total)

@export_group("Speed & Acceleration")
@export var speed_min_r1: float = 140.0
@export var speed_max_r1: float = 290.0
@export var speed_min_r16: float = 170.0
@export var speed_max_r16: float = 350.0

@export var side_accel_r1: float = 25.0
@export var side_accel_r16: float = 45.0
@export var center_pellet_accel_r1: float = 70.0
@export var center_pellet_accel_r16: float = 110.0
@export var center_talisman_accel_r1: float = 25.0
@export var center_talisman_accel_r16: float = 45.0

@export_group("Timing")
@export var delay_between_bullets: float = 0.0

@export_group("ZUN Claw (pl00.ecl sub6)")
## Two phases of rank / 2 + 8 frames each, one emission per 60 fps frame, re-aimed every frame.
## Phase 1: a talisman plus two pellets at +-fan_spread_angle_deg / 2 (ZUN's 2-bullet fan sits
## at half the spread either side), speed start + step x i. Phase 2: one centre pellet per
## frame, faster steps. No acceleration anywhere.
@export var zun_mode: bool = false
@export var zun_phase1_speed_start: float = 112.5
@export var zun_phase1_speed_step: float = 12.5
@export var zun_phase2_speed_start: float = 150.0
@export var zun_phase2_speed_step: float = 25.0

@export_group("Audio")
@export var sfx: String = "se_tan00"

const DEFAULT_PELLET_DATA: DanmakuBulletData = preload("res://resources/bullets/red_pellet.tres")
const DEFAULT_TALISMAN_DATA: DanmakuBulletData = preload("res://resources/bullets/red_talisman.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	
	var pellet_data: DanmakuBulletData = pellet_bullet_data if pellet_bullet_data else DEFAULT_PELLET_DATA
	var talisman_data: DanmakuBulletData = talisman_bullet_data if talisman_bullet_data else DEFAULT_TALISMAN_DATA
	if zun_mode:
		await _execute_zun(playfield, origin, rank, pellet_data, talisman_data)
		return
	
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var count: int = int(round(lerpf(float(bullets_min), float(bullets_max), t_rank)))
	
	var speed_min: float = lerpf(speed_min_r1, speed_min_r16, t_rank)
	var speed_max: float = lerpf(speed_max_r1, speed_max_r16, t_rank)
	var side_accel: float = lerpf(side_accel_r1, side_accel_r16, t_rank)
	var center_pellet_accel: float = lerpf(center_pellet_accel_r1, center_pellet_accel_r16, t_rank)
	var center_tal_accel: float = lerpf(center_talisman_accel_r1, center_talisman_accel_r16, t_rank)
	
	# Determine aim angle toward player
	var aim_dir: Vector2 = Vector2.DOWN
	var player_node: Node2D = playfield.get("player")
	if player_node and is_instance_valid(player_node):
		var to_player: Vector2 = (player_node.position - origin).normalized()
		if to_player != Vector2.ZERO:
			aim_dir = to_player
	
	var base_angle: float = aim_dir.angle()
	var spread_rad: float = deg_to_rad(fan_spread_angle_deg)
	
	var dir_left: Vector2 = Vector2.from_angle(base_angle - spread_rad)
	var dir_center: Vector2 = Vector2.from_angle(base_angle)
	var dir_right: Vector2 = Vector2.from_angle(base_angle + spread_rad)
	
	for i in range(count):
		if not is_instance_valid(playfield):
			return
		
		if not sfx.is_empty():
			AudioService.play_sfx(sfx)
		
		var t_i: float = float(i) / maxf(1.0, float(count - 1))
		var bullet_speed: float = lerpf(speed_min, speed_max, t_i)
		
		# Left strip: Pellets with side acceleration
		playfield.call("spawn_danmaku_bullet", pellet_data, origin, DanmakuBullet.MotionMode.LINEAR, dir_left, bullet_speed, side_accel)
		
		# Center strip: Pellets with higher center acceleration
		playfield.call("spawn_danmaku_bullet", pellet_data, origin, DanmakuBullet.MotionMode.LINEAR, dir_center, bullet_speed, center_pellet_accel)
		
		# Center strip: Overlaid red talismans with moderate acceleration
		playfield.call("spawn_danmaku_bullet", talisman_data, origin, DanmakuBullet.MotionMode.LINEAR, dir_center, bullet_speed, center_tal_accel)
		
		# Right strip: Pellets with side acceleration
		playfield.call("spawn_danmaku_bullet", pellet_data, origin, DanmakuBullet.MotionMode.LINEAR, dir_right, bullet_speed, side_accel)
		
		if delay_between_bullets > 0.0 and i < count - 1:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(delay_between_bullets).timeout


func _execute_zun(playfield: Node2D, origin: Vector2, rank: int, pellet_data: DanmakuBulletData, talisman_data: DanmakuBulletData) -> void:
	var per_phase: int = clampi(rank, 1, 16) / 2 + 8
	var half_spread: float = deg_to_rad(fan_spread_angle_deg) * 0.5
	var tree: SceneTree = playfield.get_tree() if playfield.is_inside_tree() else null
	var elapsed: float = 0.0
	var fired: int = 0
	# Paced off elapsed time so it stays one emission per 60 fps frame at any frame rate
	while fired < per_phase * 2:
		if not is_instance_valid(playfield):
			return
		# Outside a running tree (tests) there are no frames to wait for, so fire it all at once
		var due: int = mini(int(elapsed * 60.0) + 1, per_phase * 2) if playfield.is_inside_tree() else per_phase * 2
		while fired < due:
			var aim: float = PI / 2.0
			var player_node: Node2D = playfield.get("player")
			if player_node and is_instance_valid(player_node) and player_node.position != origin:
				aim = (player_node.position - origin).angle()
			if fired < per_phase:
				var speed: float = zun_phase1_speed_start + zun_phase1_speed_step * float(fired)
				playfield.call("spawn_danmaku_bullet", talisman_data, origin, DanmakuBullet.MotionMode.LINEAR, Vector2.from_angle(aim), speed)
				playfield.call("spawn_danmaku_bullet", pellet_data, origin, DanmakuBullet.MotionMode.LINEAR, Vector2.from_angle(aim - half_spread), speed)
				playfield.call("spawn_danmaku_bullet", pellet_data, origin, DanmakuBullet.MotionMode.LINEAR, Vector2.from_angle(aim + half_spread), speed)
			else:
				var speed2: float = zun_phase2_speed_start + zun_phase2_speed_step * float(fired - per_phase)
				playfield.call("spawn_danmaku_bullet", pellet_data, origin, DanmakuBullet.MotionMode.LINEAR, Vector2.from_angle(aim), speed2)
			if not sfx.is_empty():
				AudioService.play_sfx(sfx)
			fired += 1
		if fired >= per_phase * 2 or tree == null:
			break
		await tree.process_frame
		elapsed += playfield.get_process_delta_time()
