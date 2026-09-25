class_name DanmakuAimedStripStep
extends DanmakuStep

## Marisa's Level 4 Boss Attack 1: Lines of Stars (Magic Sign "Illusion Star")
## Marisa shoots sequential strips of blue, star-shaped bullets towards the opponent's
## last known location. All bullets in each strip spawn simultaneously, but accelerate
## at slightly different rates (accel_min -> accel_max), naturally separating the bullets
## in the strip as they fly across the playfield.
##
## Strip composition & Rank scaling:
## - Each strip has 4 stars at every rank.
## - Rank 1 shoots 5 strips (5 x 4 = 20 stars total).
## - Rank 16 shoots 20 strips (20 x 4 = 80 stars total).
## - Strips are emitted sequentially with a brief interval, tracking the opponent's position
##   as each strip launches to form a sweeping, curved trail.

@export_group("Bullet Settings")
@export var bullet_data: DanmakuBulletData = null

@export_group("Strip Composition & Rank Scaling")
@export var bullets_per_strip_min: int = 4
@export var bullets_per_strip_max: int = 4
@export var strips_min: int = 5
@export var strips_max: int = 20

@export_group("Timing & Delays")
@export var delay_between_strips_r1: float = 0.12
@export var delay_between_strips_r16: float = 0.07

@export_group("Kinematics")
@export var base_speed: float = 220.0
@export var accel_min: float = 90.0
@export var accel_max: float = 210.0
@export var max_bullet_speed: float = 700.0
## ZUN layered shot: when zun_speed_first > 0, bullet k of n flies at a constant
## first - (first - floor) * k / n with no acceleration
@export var zun_speed_first: float = -1.0
@export var zun_speed_floor: float = 0.0

@export_group("ZUN Frame Timing")
## When > 0, strips fire every int(interval_frames_numerator / (rank + interval_rank_offset))
## frames for fire_window_frames frames (ZUN's shoot_interval), overriding strips_* and delay_*
@export var fire_window_frames: int = 0
@export var interval_frames_numerator: int = 60
@export var interval_rank_offset: int = 6

@export_group("Boss Movement While Firing")
## If true and active_boss is present on the playfield, the boss performs a hop to the side while firing this spellcard.
@export var boss_cast_hop: bool = true
## Distance of the hop in pixels (standardized across ranks)
@export var boss_cast_hop_distance: float = 80.0
## Duration of the hop in seconds (standardized across ranks)
@export var boss_cast_hop_duration: float = 1.55

@export_group("Audio")
## Sound effect played when each strip spawns
@export var sfx: String = "se_tan00"

const DEFAULT_BULLET_DATA: DanmakuBulletData = preload("res://resources/bullets/blue_star.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	
	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_BULLET_DATA
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var strips_count: int = int(round(lerpf(float(strips_min), float(strips_max), t_rank)))
	var bullets_count: int = int(round(lerpf(float(bullets_per_strip_min), float(bullets_per_strip_max), t_rank)))
	var strip_delay: float = lerpf(delay_between_strips_r1, delay_between_strips_r16, t_rank)
	if fire_window_frames > 0:
		var interval_frames: int = maxi(interval_frames_numerator / maxi(rank + interval_rank_offset, 1), 1)
		strips_count = ceili(float(fire_window_frames) / float(interval_frames))
		strip_delay = float(interval_frames) / 60.0
	
	# Hop to the side while firing (standardized identically across all ranks)
	if boss_cast_hop:
		var active_boss_node: Node2D = playfield.get("active_boss")
		if active_boss_node and is_instance_valid(active_boss_node) and active_boss_node.has_method("perform_cast_hop"):
			active_boss_node.call("perform_cast_hop", boss_cast_hop_distance, boss_cast_hop_duration)
	
	for s in range(strips_count):
		if not is_instance_valid(playfield):
			return
		
		# Audio cue for each strip release
		if not sfx.is_empty():
			AudioService.play_sfx(sfx)
		
		# Bullet spawn origin: follow active boss position if available, otherwise fallback to origin
		var spawn_pos: Vector2 = origin
		var active_boss: Node2D = playfield.get("active_boss")
		if active_boss and is_instance_valid(active_boss):
			spawn_pos = active_boss.position
		
		# Find opponent's current location to determine aim direction
		var aim_dir: Vector2 = Vector2.DOWN
		var player_node: Node2D = playfield.get("player")
		if player_node and is_instance_valid(player_node):
			var to_player: Vector2 = (player_node.position - spawn_pos).normalized()
			if to_player != Vector2.ZERO:
				aim_dir = to_player
		
		# Spawn all bullets in this strip simultaneously
		for k in range(bullets_count):
			var t_bullet: float = 0.0
			if bullets_count > 1:
				t_bullet = float(k) / float(bullets_count - 1)
			
			var bullet_accel: float = lerpf(accel_min, accel_max, t_bullet)
			var bullet_speed: float = base_speed
			if zun_speed_first > 0.0:
				bullet_speed = zun_speed_first - (zun_speed_first - zun_speed_floor) * float(k) / float(bullets_count)
				bullet_accel = 0.0
			
			playfield.call(
				"spawn_danmaku_bullet",
				data,
				spawn_pos,
				DanmakuBullet.MotionMode.LINEAR,
				aim_dir,
				bullet_speed,
				bullet_accel,
				max_bullet_speed
			)
		
		# Sequential delay between strips
		if s < strips_count - 1 and strip_delay > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(strip_delay).timeout
	
	# Await remaining hop duration so the boss glide and cast pose remain active for the full standardized duration
	if boss_cast_hop:
		var elapsed_barrage: float = float(strips_count - 1) * strip_delay
		var remaining_hop: float = boss_cast_hop_duration - elapsed_barrage
		if remaining_hop > 0.0 and is_instance_valid(playfield) and playfield.is_inside_tree():
			await playfield.get_tree().create_timer(remaining_hop).timeout

