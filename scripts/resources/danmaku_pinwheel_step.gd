class_name DanmakuPinwheelStep
extends DanmakuStep

## Step in a Danmaku timeline that spawns rotating pinwheel bursts.
## Inspired by Marisa's Level 4 Boss Attack 3 ("Spiral Pellets and Stars" / Magic Sign "Illusion Star").
## 6 spokes radiate outward in an alternating spiral pattern:
## Burst waves alternate between Stars (Green/Blue) and Pellets across all spokes.
## Each spoke fires a small curved arc/fan of bullets. Clockwise rotation between waves
## winds the pattern into 6 tight, swirling spiral ribbons matching Touhou 09.

@export_group("Bullet Types")
@export var pellet_bullet_data: DanmakuBulletData = null
@export var star_bullet_data: DanmakuBulletData = null
@export var star_bullet_data_alt: DanmakuBulletData = null

@export_group("Bursts & Timing")
@export var bursts: int = 24
@export var burst_interval: float = 0.07

@export_group("Geometry & Vectors")
@export var spoke_count: int = 6
@export var rotation_per_burst_deg: float = 13.5
@export var initial_angle_deg: float = 30.0
@export var spawn_radius: float = 20.0
@export var fan_spread_deg: float = 6.5

@export_group("Rank Scaling")
@export var bullets_per_vector_rank_1: int = 2
@export var bullets_per_vector_rank_16: int = 4
@export var speed_rank_1: float = 160.0
@export var speed_rank_16: float = 340.0

@export_group("ZUN Spiral (pl01.ecl sub7)")
## One fan per 60 fps frame for zun_frames frames, starting straight up. ZUN reuses the
## speed value as the per-frame turn, so the fan turns (zun_speed_base + per_rank x rank)
## radians each frame while flying at 125 px/s per unit of that same value.
## Types cycle pellet_alt -> star -> pellet -> star_alt.
@export var zun_mode: bool = false
@export var pellet_bullet_data_alt: DanmakuBulletData = null
@export var zun_frames: int = 60
@export var zun_speed_base: float = 1.0
@export var zun_speed_per_rank: float = 0.1
@export var zun_fan_spread_deg: float = 12.0

@export_group("Audio")
## Sound effect played on each burst wave
@export var sfx: String = "se_tan00"

const DEFAULT_PELLET_DATA: DanmakuBulletData = preload("res://resources/bullets/green_pellet.tres")
const DEFAULT_STAR_DATA: DanmakuBulletData = preload("res://resources/bullets/green_star.tres")
const DEFAULT_STAR_ALT_DATA: DanmakuBulletData = preload("res://resources/bullets/blue_star.tres")

# Backwards compatibility aliases
var vector_count: int:
	get: return spoke_count
	set(val): spoke_count = val

var transverse_spacing: float = 16.0
var star_offset_angle_deg: float = 60.0
var alternate_stars_per_burst: bool = true

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	if not playfield.has_method("spawn_danmaku_bullet"):
		return
	
	var pellet_data: DanmakuBulletData = pellet_bullet_data if pellet_bullet_data else DEFAULT_PELLET_DATA
	var star_data: DanmakuBulletData = star_bullet_data if star_bullet_data else DEFAULT_STAR_DATA
	var star_alt_data: DanmakuBulletData = star_bullet_data_alt if star_bullet_data_alt else DEFAULT_STAR_ALT_DATA
	
	if zun_mode:
		await _execute_zun_spiral(playfield, origin, rank, pellet_data, star_data, star_alt_data)
		return
	
	var t: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var bullets_per_vector: int = int(round(lerpf(float(bullets_per_vector_rank_1), float(bullets_per_vector_rank_16), t)))
	var bullet_speed: float = lerpf(speed_rank_1, speed_rank_16, t)
	
	var angle_per_spoke: float = TAU / float(maxi(spoke_count, 1))
	var rot_delta: float = deg_to_rad(rotation_per_burst_deg)
	var fan_delta: float = deg_to_rad(fan_spread_deg)
	var base_angle: float = deg_to_rad(initial_angle_deg)
	
	for b in range(bursts):
		if not is_instance_valid(playfield):
			return
		
		# Audio cue for each burst wave
		if not sfx.is_empty():
			AudioService.play_sfx(sfx)
		
		var cur_base_angle: float = base_angle + (rot_delta * float(b))
		
		# Resolve current bullet data for this wave:
		# Wave b % 2 == 1: Pellets
		# Wave b % 4 == 0: Green Stars
		# Wave b % 4 == 2: Blue Stars
		var cur_bullet_data: DanmakuBulletData
		if b % 2 == 1:
			cur_bullet_data = pellet_data
		elif (b / 2) % 2 == 0:
			cur_bullet_data = star_data
		else:
			cur_bullet_data = star_alt_data
		
		# Spawn bullets across all spokes in angular fan
		for v in range(spoke_count):
			var v_center_angle: float = cur_base_angle + (angle_per_spoke * float(v))
			for j in range(bullets_per_vector):
				var offset_idx: float = float(j) - (float(bullets_per_vector - 1) * 0.5)
				var bullet_angle: float = v_center_angle + (offset_idx * fan_delta)
				var bullet_dir := Vector2.from_angle(bullet_angle)
				var spawn_pos: Vector2 = origin + (bullet_dir * spawn_radius)
				
				playfield.spawn_danmaku_bullet(
					cur_bullet_data,
					spawn_pos,
					DanmakuBullet.MotionMode.LINEAR,
					bullet_dir,
					bullet_speed
				)
		
		# Burst delay
		if b < bursts - 1 and burst_interval > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(burst_interval).timeout


func _execute_zun_spiral(playfield: Node2D, origin: Vector2, rank: int, pellet_data: DanmakuBulletData, star_data: DanmakuBulletData, star_alt_data: DanmakuBulletData) -> void:
	var r: int = clampi(rank, 1, 16)
	var fan_count: int = r / 6 + 2
	var zun_speed: float = zun_speed_base + zun_speed_per_rank * float(r)
	var pellet_alt: DanmakuBulletData = pellet_bullet_data_alt if pellet_bullet_data_alt else pellet_data
	var cycle: Array[DanmakuBulletData] = [pellet_alt, star_data, pellet_data, star_alt_data]
	var spread: float = deg_to_rad(zun_fan_spread_deg)
	var tree: SceneTree = playfield.get_tree()
	var elapsed: float = 0.0
	var fired: int = 0
	# Paced off elapsed time so it stays one fan per 60 fps frame at any frame rate;
	# the turn is per fan, so the spiral shape does not depend on timing at all
	while fired < zun_frames:
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return
		var due: int = mini(int(elapsed * 60.0) + 1, zun_frames)
		while fired < due:
			var base_angle: float = -PI / 2.0 + zun_speed * float(fired)
			for j in range(fan_count):
				var dir: Vector2 = Vector2.from_angle(base_angle + (float(j) - float(fan_count - 1) * 0.5) * spread)
				playfield.spawn_danmaku_bullet(cycle[fired % 4], origin + dir * spawn_radius, DanmakuBullet.MotionMode.LINEAR, dir, zun_speed * 125.0)
			if not sfx.is_empty():
				AudioService.play_sfx(sfx)
			fired += 1
		await tree.process_frame
		elapsed += playfield.get_process_delta_time()
