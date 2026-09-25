class_name DanmakuRingStep
extends DanmakuStep

## Spawns a radial ring of Danmaku bullets with customizable density, speeds, and motion profiles.

@export_group("Bullet Types")
## Primary bullet data (used for all bullets, or even-indexed bullets when alternating)
@export var bullet_data: DanmakuBulletData
## Optional secondary bullet data for odd-indexed bullets (e.g. circle -> oval -> circle -> oval)
@export var alternate_bullet_data: DanmakuBulletData = null
## Optional sequence of bullet types to cycle through sequentially. If set, overrides bullet_data / alternate_bullet_data.
@export var bullet_sequence: Array[DanmakuBulletData] = []

@export_group("Motion & Behavior")
## Primary motion mode
@export var motion_mode: DanmakuBullet.MotionMode = DanmakuBullet.MotionMode.LINEAR
## If enabled, alternate bullets use their own motion mode and speeds
@export var alternate_has_separate_motion: bool = false
@export var alternate_motion_mode: DanmakuBullet.MotionMode = DanmakuBullet.MotionMode.DECEL_AND_HOME

@export_group("Density & Rank Scaling")
## Minimum bullet count at Rank 1
@export var count_min: int = 12
## Maximum bullet count at Rank 16
@export var count_max: int = 28
## Offsets the starting angle by half a step to cleanly interleave with other rings
@export var stagger_half_step: bool = false
## Fixed angular offset in degrees
@export var angle_offset_deg: float = 0.0
## Add the wave's shared random angle (SpellcardData.random_wave_angle) to the ring
@export var use_wave_angle: bool = false
## Rotate the ring so bullet 0 points at the victim (ZUN's bullet_circle_aimed)
@export var aim_at_player: bool = false

@export_group("Speed & Movement")
## Bullet movement speed at Rank 1 (pixels/sec)
@export var speed_min: float = 230.0
## Bullet movement speed at Rank 16 (pixels/sec)
@export var speed_max: float = 270.0
## Movement speed at Rank 1 for alternate (odd) bullets when 'Alternate Has Separate Motion' is on
@export var alternate_speed_min: float = 210.0
## Movement speed at Rank 16 for alternate (odd) bullets when 'Alternate Has Separate Motion' is on
@export var alternate_speed_max: float = 250.0

@export_group("Decel & Home Settings")
## Time (seconds) the bullet spends decelerating from initial burst speed to a complete stop
@export var homing_decel_time: float = 0.45
## Time (seconds) the bullet remains suspended motionless in the air before acquiring target
@export var homing_pause_time: float = 0.18
## Speed (pixels/sec) at which the bullet launches toward the targeted player
@export var homing_launch_speed: float = 300.0
## Optional launch speed at Rank 16. If > 0, launch speed scales from homing_launch_speed (Rank 1) to homing_launch_speed_max (Rank 16)
@export var homing_launch_speed_max: float = 0.0
## Number of homing stages (1 = standard lock & go; 2 = travel midway, stop, and home again)
@export var homing_stages: int = 1
## For 2-stage homing: time (seconds) bullet travels toward player during Stage 1 before braking
@export var stage1_flight_duration: float = 0.5
## When > 0, stage 2+ brakes over this long and relaunches at homing_stage2_launch_speed
@export var homing_stage2_decel_time: float = 0.0
@export var homing_stage2_launch_speed: float = 0.0
## ZUN's AimRel: relaunch turned relaunch_turn_deg from the bullet's own heading instead of
## aiming at the player
@export var relaunch_relative: bool = false
@export var relaunch_turn_deg: float = 0.0
## Decel time (seconds) for alternate bullets when using separate motion
@export var alternate_homing_decel_time: float = 0.45
## Pause time (seconds) for alternate bullets when using separate motion
@export var alternate_homing_pause_time: float = 0.18
## Launch speed (pixels/sec) for alternate bullets when using separate motion
@export var alternate_homing_launch_speed: float = 300.0
## Optional launch speed at Rank 16 for alternate bullets when using separate motion
@export var alternate_homing_launch_speed_max: float = 0.0
## Homing stages (1 or 2) for alternate bullets when using separate motion
@export var alternate_homing_stages: int = 1
## Flight duration (seconds) for Stage 1 of alternate bullets when using 2-stage homing
@export var alternate_stage1_flight_duration: float = 0.5

@export_group("Wall Bounce")
## ZUN's BounceNonBottom: ricochet once off the left, right or top edge
@export var bounce_off_walls: bool = false

@export_group("Audio")
@export var sfx: String = "se_tan00"
@export var homing_sfx: String = ""

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	var wave_angle: float = take_wave_angle()
	if playfield == null or not playfield.has_method("spawn_danmaku_bullet"):
		return
	
	if not sfx.is_empty():
		AudioService.play_sfx(sfx)
	
	var t: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var bullet_count: int = int(round(lerpf(float(count_min), float(count_max), t)))
	var base_speed: float = lerpf(speed_min, speed_max, t)
	var alt_speed: float = lerpf(alternate_speed_min, alternate_speed_max, t)
	
	# Determine sequence or alternating mode
	var seq_len: int = 0
	if not bullet_sequence.is_empty():
		seq_len = bullet_sequence.size()
	elif alternate_bullet_data != null:
		seq_len = 2
	
	# Ensure bullet_count divides cleanly by sequence length so pattern loops seamlessly
	if seq_len > 1 and (bullet_count % seq_len) != 0:
		bullet_count += (seq_len - (bullet_count % seq_len))
	
	if bullet_count <= 0:
		return
	
	var angle_step: float = TAU / float(bullet_count)
	var start_angle: float = deg_to_rad(angle_offset_deg)
	if use_wave_angle:
		start_angle += wave_angle
	if aim_at_player:
		var victim: Node2D = playfield.get("player")
		if victim and is_instance_valid(victim) and victim.position != origin:
			start_angle += (victim.position - origin).angle()
	if stagger_half_step:
		start_angle += angle_step * 0.5
	
	for i in range(bullet_count):
		var angle: float = start_angle + (angle_step * float(i))
		var dir := Vector2.from_angle(angle)
		
		# Resolve current bullet data
		var cur_bullet: DanmakuBulletData = bullet_data
		var is_alt: bool = false
		if seq_len > 0 and not bullet_sequence.is_empty():
			cur_bullet = bullet_sequence[i % seq_len]
			is_alt = (i % seq_len) != 0
		elif alternate_bullet_data != null:
			if (i % 2) == 1:
				cur_bullet = alternate_bullet_data
				is_alt = true
			else:
				cur_bullet = bullet_data
		
		# Resolve motion and speed
		var cur_mode: DanmakuBullet.MotionMode = motion_mode
		var cur_speed: float = base_speed
		var decel: float = homing_decel_time
		var pause: float = homing_pause_time
		var launch: float = homing_launch_speed
		if homing_launch_speed_max > 0.0:
			launch = lerpf(homing_launch_speed, homing_launch_speed_max, t)
		
		var stages: int = homing_stages
		var stage1_dur: float = stage1_flight_duration
		
		if is_alt and alternate_has_separate_motion:
			cur_mode = alternate_motion_mode
			cur_speed = alt_speed
			decel = alternate_homing_decel_time
			pause = alternate_homing_pause_time
			launch = alternate_homing_launch_speed
			if alternate_homing_launch_speed_max > 0.0:
				launch = lerpf(alternate_homing_launch_speed, alternate_homing_launch_speed_max, t)
			stages = alternate_homing_stages
			stage1_dur = alternate_stage1_flight_duration
		
		if cur_mode == DanmakuBullet.MotionMode.DECEL_AND_HOME and playfield.has_method("spawn_danmaku_bullet_decel_home"):
			var homer: DanmakuBullet = playfield.spawn_danmaku_bullet_decel_home(
				cur_bullet,
				origin,
				dir,
				cur_speed,
				decel,
				pause,
				launch,
				stages,
				stage1_dur,
				homing_sfx
			)
			if homer and homing_stage2_decel_time > 0.0:
				homer.stage2_decel_time = homing_stage2_decel_time
				homer.stage2_launch_speed = homing_stage2_launch_speed
			if homer and relaunch_relative:
				homer.relaunch_relative = true
				homer.relaunch_turn = deg_to_rad(relaunch_turn_deg)
			if homer and bounce_off_walls:
				homer.bounce_off_walls = true
		else:
			var b: DanmakuBullet = playfield.spawn_danmaku_bullet(
				cur_bullet,
				origin,
				cur_mode,
				dir,
				cur_speed
			)
			if b and bounce_off_walls:
				b.bounce_off_walls = true
