class_name DanmakuKnifeStreamStep
extends DanmakuStep

## Youmu's Level 4 Boss Attack 2: Rapid Fire of Blue Daggers
## Youmu charges and fires daggers in rapid succession towards the target's last known position.
## Each individual dagger calculates its own trajectory toward the opponent's updated location
## at the exact moment of launch, creating a continuous aimed stream of blue daggers that trails
## the player as they move.
##
## Rank scaling:
## - Rank 1: 9 daggers, speed 260.0 px/s, delay 0.14s
## - Rank 16: 25 daggers, speed 400.0 px/s, delay 0.075s

@export_group("Bullet Settings")
@export var bullet_data: DanmakuBulletData = null

@export_group("Dagger Counts & Scaling")
@export var knives_min: int = 9
@export var knives_max: int = 25

@export_group("Timing & Delays")
## Delay before the first dagger fires (during which Youmu charges energy).
@export var initial_charge_delay: float = 0.35
@export var delay_r1: float = 0.14
@export var delay_r16: float = 0.075

@export_group("Kinematics")
@export var speed_r1: float = 260.0
@export var speed_r16: float = 400.0

@export_group("Targeting")
## If true, each dagger continuously tracks the opponent's latest position.
@export var track_player_per_knife: bool = true

@export_group("Sound Effects")
## Sound effect played during the attack (e.g. "se_tan00").
@export var sfx: String = "se_tan00"

const DEFAULT_BULLET_DATA: DanmakuBulletData = preload("res://resources/bullets/blue_knife.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	
	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_BULLET_DATA
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var total_knives: int = int(round(lerpf(float(knives_min), float(knives_max), t_rank)))
	var cur_delay: float = lerpf(delay_r1, delay_r16, t_rank)
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
	
	for i in range(total_knives):
		if not is_instance_valid(playfield):
			return
		
		# Boss might have shifted slightly, keep updated
		var cur_spawn: Vector2 = spawn_pos
		if active_boss and is_instance_valid(active_boss):
			cur_spawn = active_boss.position
		
		# Track player's current position at the instant of this dagger's launch
		var aim_dir: Vector2 = Vector2.DOWN
		if track_player_per_knife:
			var player_node: Node2D = playfield.get("player")
			if player_node and is_instance_valid(player_node):
				var to_player: Vector2 = (player_node.position - cur_spawn).normalized()
				if to_player != Vector2.ZERO:
					# Ensure generally downward orientation into playfield
					if to_player.y < 0.2:
						to_player = Vector2(to_player.x, 0.5).normalized()
					aim_dir = to_player
		
		playfield.call(
			"spawn_danmaku_bullet",
			data,
			cur_spawn,
			DanmakuBullet.MotionMode.LINEAR,
			aim_dir,
			bullet_speed
		)
		
		if not sfx.is_empty():
			AudioService.play_sfx(sfx)
		
		if i < total_knives - 1 and cur_delay > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(cur_delay).timeout

