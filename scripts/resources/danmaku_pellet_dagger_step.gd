class_name DanmakuPelletDaggerStep
extends DanmakuStep

## Sakuya's Level 4 Boss Attack: Pellet-Spewing Dagger.
## Throws a single slow cyan dagger toward the player's position at the moment of the throw
## ("last known position" - it does not re-aim mid-flight). While airborne, the dagger itself
## periodically spews a short-lived burst of pellets scattered around its own position; each
## pellet picks a random direction and accelerates from rest up to its top speed over
## pellet_accel_time seconds, so a trail of little outward-blooming clusters follows the dagger
## the whole way down.
##
## The boss's own hop/cast AI loop (BossData.attack_patterns) is what produces the repeated
## "reposition, then throw another dagger" behavior seen in reference footage - this step only
## ever spawns ONE dagger per execute() call.

@export_group("Bullet Settings")
@export var dagger_data: DanmakuBulletData = null
@export var pellet_data: DanmakuBulletData = null

@export_group("Dagger Kinematics")
## Fixed travel speed for the dagger itself - same at every rank, as slow as the Extra Attack's
## own daggers (300 px/s). Only the pellet trail scales with rank, not the dagger's own throw.
@export var dagger_speed: float = 300.0

@export_group("Pellet Trail")
## Seconds between each pellet burst while the dagger is airborne.
@export var pellet_spawn_interval: float = 0.15
## Pellets spawned per burst - rank scaling ends of the range.
@export var pellets_per_burst_min: int = 3
@export var pellets_per_burst_max: int = 5
## Radius around the dagger's own position that each burst's pellets scatter within - covers
## the full visual extent of the dagger sprite (~70x45px at base_scale 2.25) plus a small margin.
@export var pellet_spawn_radius: float = 42.0
## Time for a spawned pellet to accelerate from rest up to its top speed.
@export var pellet_accel_time: float = 1.0
## Pellet top speed - rank scaling ends of the range.
@export var pellet_speed_min: float = 140.0
@export var pellet_speed_max: float = 190.0
## pl02.ecl sub8 shape: when pellet_bursts_base >= 0 the trail stops after
## base + per_rank x rank bursts, spawns on a circle that shrinks by pellet_radius_decay
## per burst, and jitters each pellet's top speed by +-pellet_speed_jitter
@export var pellet_bursts_base: int = -1
@export var pellet_bursts_per_rank: int = 0
@export var pellet_radius_decay: float = 1.0
@export var pellet_speed_jitter: float = 0.0

@export_group("Sound Effects")
@export var sfx: String = "se_tan00"

const DEFAULT_DAGGER_DATA: DanmakuBulletData = preload("res://resources/bullets/sakuya_knife_cyan.tres")
const DEFAULT_PELLET_DATA: DanmakuBulletData = preload("res://resources/bullets/white_pellet.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return

	var data: DanmakuBulletData = dagger_data if dagger_data else DEFAULT_DAGGER_DATA
	var p_data: DanmakuBulletData = pellet_data if pellet_data else DEFAULT_PELLET_DATA
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var pellet_count: int = int(round(lerpf(float(pellets_per_burst_min), float(pellets_per_burst_max), t_rank)))
	var pellet_max_speed: float = lerpf(pellet_speed_min, pellet_speed_max, t_rank)

	var spawn_pos: Vector2 = origin
	var active_boss: Node2D = playfield.get("active_boss")
	if active_boss and is_instance_valid(active_boss):
		spawn_pos = active_boss.position

	# Aim once at the player's current position - a snapshot ("last known position"), not
	# continuous per-frame tracking.
	var aim_dir: Vector2 = Vector2.DOWN
	var player_node: Node2D = playfield.get("player")
	if player_node and is_instance_valid(player_node):
		var to_player: Vector2 = (player_node.position - spawn_pos).normalized()
		if to_player != Vector2.ZERO:
			if to_player.y < 0.2:
				to_player = Vector2(to_player.x, 0.5).normalized()
			aim_dir = to_player

	var dagger: DanmakuBullet = playfield.call(
		"spawn_danmaku_bullet",
		data,
		spawn_pos,
		DanmakuBullet.MotionMode.LINEAR,
		aim_dir,
		dagger_speed
	)
	if dagger:
		dagger.owner_playfield = playfield
		dagger.pellet_spawn_data = p_data
		dagger.pellet_spawn_interval = pellet_spawn_interval
		dagger.pellet_spawn_count = pellet_count
		dagger.pellet_spawn_radius = pellet_spawn_radius
		dagger.pellet_spawn_accel_time = pellet_accel_time
		dagger.pellet_spawn_max_speed = pellet_max_speed
		if pellet_bursts_base >= 0:
			dagger.pellet_spawn_remaining = pellet_bursts_base + pellet_bursts_per_rank * clampi(rank, 1, 16)
			dagger.pellet_spawn_on_circle = true
		dagger.pellet_spawn_radius_decay = pellet_radius_decay
		dagger.pellet_spawn_speed_jitter = pellet_speed_jitter

	if not sfx.is_empty():
		AudioService.play_sfx(sfx)
