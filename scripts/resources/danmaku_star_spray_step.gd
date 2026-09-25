class_name DanmakuStarSprayStep
extends DanmakuStep

## Marisa's Level 4 Boss Attack 4: Star Spray (Magic Sign "Illusion Star")
## Marisa fires a spray of ~60 yellow stars from her center in seemingly random directions around herself.
## The stars curve outwards for a brief period, then settle on traveling in straight lines.
## Rank 1 and Rank 16 have the same amount of stars (60), with bullet speed drastically increased at Rank 16.

@export_group("Bullet Settings")
@export var bullet_data: DanmakuBulletData = null

@export_group("Barrage Composition")
@export var total_stars: int = 60
@export var waves: int = 6
@export var wave_interval: float = 0.08

@export_group("Kinematics & Rank Scaling")
@export var speed_rank_1: float = 200.0
@export var speed_rank_16: float = 520.0
@export var speed_variance: float = 0.15

@export_group("Curving Dynamics")
@export var curve_duration: float = 1.1
@export var curve_angular_speed: float = 1.25
@export var curve_variance: float = 0.20
## Visual continuous sprite rotation speed in radians/second
@export var star_spin_speed: float = 6.0

@export_group("ZUN Stream (pl01.ecl sub4)")
## One star per frame at a random angle for stars_base + rank frames, instead of waves.
## Each star turns at a random rate and slows by a random amount for zun_effect_duration.
@export var zun_mode: bool = false
@export var stars_base: int = 60
@export var zun_speed_base: float = 187.5
@export var zun_speed_per_rank: float = 12.5
@export var zun_max_turn: float = 1.5708
@export var zun_max_decel: float = 75.0
@export var zun_effect_duration: float = 2.0

@export_group("Boss Movement While Firing")
@export var boss_cast_hop: bool = false
@export var boss_cast_hop_distance: float = 60.0
@export var boss_cast_hop_duration: float = 1.0

@export_group("Audio")
## Sound effect played on each burst wave
@export var sfx: String = "se_tan00"

const DEFAULT_BULLET_DATA: DanmakuBulletData = preload("res://resources/bullets/yellow_star.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	
	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_BULLET_DATA
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var base_speed: float = lerpf(speed_rank_1, speed_rank_16, t_rank)
	
	if boss_cast_hop:
		var active_boss_node: Node2D = playfield.get("active_boss")
		if active_boss_node and is_instance_valid(active_boss_node) and active_boss_node.has_method("perform_cast_hop"):
			active_boss_node.call("perform_cast_hop", boss_cast_hop_distance, boss_cast_hop_duration)
	
	if zun_mode:
		await _execute_zun_stream(playfield, origin, rank, data)
		return
	
	var safe_waves: int = maxi(1, waves)
	var stars_per_wave: int = total_stars / safe_waves
	var remainder_stars: int = total_stars % safe_waves
	
	for w in range(safe_waves):
		if not is_instance_valid(playfield):
			return
		
		# Audio cue for each burst wave
		if not sfx.is_empty():
			AudioService.play_sfx(sfx)
		
		# Bullet spawn origin: center on active boss if present, else default origin
		var spawn_pos: Vector2 = origin
		var active_boss: Node2D = playfield.get("active_boss")
		if active_boss and is_instance_valid(active_boss):
			spawn_pos = active_boss.position
		
		var count_in_this_wave: int = stars_per_wave + (1 if w < remainder_stars else 0)
		var base_wave_angle: float = float(w) * 0.42 + randf_range(-0.1, 0.1)
		var angle_step: float = TAU / float(maxi(1, count_in_this_wave))
		
		for i in range(count_in_this_wave):
			var bullet_angle: float = base_wave_angle + float(i) * angle_step + randf_range(-0.1, 0.1)
			var bullet_dir: Vector2 = Vector2.from_angle(bullet_angle)
			
			# Outward curving: right hemisphere curves clockwise (+), left hemisphere curves counter-clockwise (-)
			var curve_sign: float = 1.0 if bullet_dir.x >= 0.0 else -1.0
			var ang_speed: float = curve_angular_speed * curve_sign * (1.0 + randf_range(-curve_variance, curve_variance))
			var bullet_spd: float = base_speed * (1.0 + randf_range(-speed_variance, speed_variance))
			var spin: float = star_spin_speed * curve_sign
			
			if playfield.has_method("spawn_danmaku_bullet_curve_line"):
				playfield.call("spawn_danmaku_bullet_curve_line", data, spawn_pos, bullet_dir, bullet_spd, ang_speed, curve_duration, 0.0, 0.0, spin)
			elif playfield.has_method("spawn_danmaku_bullet"):
				var b: DanmakuBullet = playfield.call("spawn_danmaku_bullet", data, spawn_pos, DanmakuBullet.MotionMode.CURVE_THEN_LINE, bullet_dir, bullet_spd)
				if b:
					b.curve_angular_speed = ang_speed
					b.curve_duration = curve_duration
					b.spin_speed = spin
		
		# Interval between successive bursts
		if w < safe_waves - 1 and wave_interval > 0.0:
			var tree: SceneTree = playfield.get_tree()
			if tree:
				await tree.create_timer(wave_interval).timeout


func _execute_zun_stream(playfield: Node2D, origin: Vector2, rank: int, data: DanmakuBulletData) -> void:
	var total: int = stars_base + clampi(rank, 1, 16)
	var speed: float = zun_speed_base + zun_speed_per_rank * float(clampi(rank, 1, 16))
	var tree: SceneTree = playfield.get_tree()
	var elapsed: float = 0.0
	var fired: int = 0
	# Paced off elapsed time so the stream holds one star per 60 fps frame at any frame rate
	while fired < total:
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return
		var due: int = mini(int(elapsed * 60.0) + 1, total)
		while fired < due:
			var spawn_pos: Vector2 = origin
			var active_boss: Node2D = playfield.get("active_boss")
			if active_boss and is_instance_valid(active_boss):
				spawn_pos = active_boss.position
			var turn: float = randf_range(-zun_max_turn, zun_max_turn)
			var spin: float = star_spin_speed * (1.0 if turn >= 0.0 else -1.0)
			playfield.call("spawn_danmaku_bullet_curve_line", data, spawn_pos, Vector2.from_angle(randf() * TAU), speed, turn, zun_effect_duration, -randf() * zun_max_decel, 0.0, spin)
			if not sfx.is_empty():
				AudioService.play_sfx(sfx)
			fired += 1
		await tree.process_frame
		elapsed += playfield.get_process_delta_time()
