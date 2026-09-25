class_name DanmakuKnifeFanStep
extends DanmakuStep

## Youmu's Level 4 Boss Attack 1: Fan of Yellow Knives
## Youmu charges and fires off a sequential series of expanding dagger waves.
## Each wave contains one more knife than the last, distributed evenly along a fan formation.
##
## Rank scaling:
## - Rank 1: 5 waves (1, 2, 3, 4, 5 knives = 15 total)
## - Rank 16: 9 waves (1, 2, 3, 4, 5, 6, 7, 8, 9 knives = 45 total)
## - Knives fly linearly along their fan angles, forming a descending wedge formation.

@export_group("Bullet Settings")
@export var bullet_data: DanmakuBulletData = null

@export_group("Wave Counts & Scaling")
## pl03.ecl sub5: rank / 4 + 5 waves instead of the waves_min / waves_max lerp
@export var zun_rank_waves: bool = false
@export var waves_min: int = 5
@export var waves_max: int = 9

@export_group("Fan Geometry")
## Angular separation in degrees between adjacent knives in a wave.
@export var angle_step_deg: float = 9.6

@export_group("Timing & Delays")
## Delay before the first wave fires (during which Youmu charges energy).
@export var initial_charge_delay: float = 0.35
@export var wave_delay_r1: float = 0.16
@export var wave_delay_r16: float = 0.12

@export_group("Kinematics")
@export var speed_r1: float = 215.0
@export var speed_r16: float = 275.0

@export_group("Targeting")
## If true, aims the central axis of the fan toward the player's position at cast time.
@export var aim_at_player: bool = true

@export_group("Sound Effects")
## Sound effect played during the attack (e.g. "se_tan00").
@export var sfx: String = "se_tan00"

const DEFAULT_BULLET_DATA: DanmakuBulletData = preload("res://resources/bullets/yellow_knife.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	
	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_BULLET_DATA
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var wave_count: int = int(round(lerpf(float(waves_min), float(waves_max), t_rank)))
	if zun_rank_waves:
		wave_count = clampi(rank, 1, 16) / 4 + 5
	var cur_delay: float = lerpf(wave_delay_r1, wave_delay_r16, t_rank)
	var bullet_speed: float = lerpf(speed_r1, speed_r16, t_rank)
	
	# Determine initial spawn position
	var spawn_pos: Vector2 = origin
	var active_boss: Node2D = playfield.get("active_boss")
	if active_boss and is_instance_valid(active_boss):
		spawn_pos = active_boss.position
	
	# Initial charge delay before attack fires
	if initial_charge_delay > 0.0:
		if is_instance_valid(playfield) and playfield.is_inside_tree():
			await playfield.get_tree().create_timer(initial_charge_delay).timeout
	
	if not is_instance_valid(playfield):
		return
	
	# Re-check boss position after charge delay
	if active_boss and is_instance_valid(active_boss):
		spawn_pos = active_boss.position
	
	# Determine central aim direction
	var aim_dir: Vector2 = Vector2.DOWN
	if aim_at_player:
		var player_node: Node2D = playfield.get("player")
		if player_node and is_instance_valid(player_node):
			var to_player: Vector2 = (player_node.position - spawn_pos).normalized()
			if to_player != Vector2.ZERO:
				# Ensure generally downward orientation into playfield
				if to_player.y < 0.2:
					to_player = Vector2(to_player.x, 0.5).normalized()
				aim_dir = to_player
	
	var base_angle: float = aim_dir.angle()
	
	for w in range(wave_count):
		if not is_instance_valid(playfield):
			return
		
		var cur_spawn: Vector2 = spawn_pos
		if active_boss and is_instance_valid(active_boss):
			cur_spawn = active_boss.position
		
		var knife_count: int = w + 1
		for k in range(knife_count):
			var angle_offset: float = 0.0
			if knife_count > 1:
				angle_offset = (float(k) - float(knife_count - 1) / 2.0) * deg_to_rad(angle_step_deg)
			
			var knife_dir: Vector2 = Vector2.from_angle(base_angle + angle_offset)
			
			playfield.call(
				"spawn_danmaku_bullet",
				data,
				cur_spawn,
				DanmakuBullet.MotionMode.LINEAR,
				knife_dir,
				bullet_speed
			)
		
		if not sfx.is_empty():
			AudioService.play_sfx(sfx)
		
		if w < wave_count - 1 and cur_delay > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(cur_delay).timeout
