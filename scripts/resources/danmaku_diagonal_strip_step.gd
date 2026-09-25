class_name DanmakuDiagonalStripStep
extends DanmakuStep

## Danmaku step that emits parallel diagonal lines of alternating stars and pellets
## sweeping from the outside edge of the playfield toward the inside.
## Bullets along each line feature ascending velocities to create a smooth separation effect.

enum DirectionMode {
	AUTO_FROM_OUTSIDE, ## Automatically mirrors based on target playfield (Left-to-Right for P1, Right-to-Left for P2)
	LEFT_TO_RIGHT,     ## Always spawn on left wall and fly down-right
	RIGHT_TO_LEFT,     ## Always spawn on right wall and fly down-left
	DUAL_INWARD,       ## Downward diagonal double-pronged attack from BOTH sides simultaneously
	OPPOSITE_PLAYER    ## Spawn on the wall away from the victim's half and fly toward it (ZUN's PLAYER_X < 0 check)
}

@export_group("Bullet Resources")
## Primary bullet resource (used for stars in Lv 2, or left wall in DUAL_INWARD mode)
@export var bullet_star: DanmakuBulletData = null
## Alternating bullet resource (used for pellets in Lv 2)
@export var bullet_pellet: DanmakuBulletData = null
## Secondary bullet resource (used for right wall in DUAL_INWARD mode)
@export var bullet_secondary: DanmakuBulletData = null
## Whether to alternate between bullet_star and bullet_pellet across successive lines
@export var alternate_bullet_types: bool = true

@export_group("Rank Scaling")
## Number of diagonal lines at Rank 1 (or pairs in DUAL_INWARD mode)
@export var lines_min: int = 6
## Number of diagonal lines at Rank 8 (or pairs in DUAL_INWARD mode)
@export var lines_mid: int = 8
## Number of diagonal lines at Rank 16 (or pairs in DUAL_INWARD mode)
@export var lines_max: int = 12
## Optional stepped line counts: rank below line_rank_breaks[i] uses line_counts[i], anything
## higher uses the last entry. Overrides the min/mid/max lerp when set.
@export var line_rank_breaks: PackedInt32Array = PackedInt32Array()
@export var line_counts: PackedInt32Array = PackedInt32Array()

@export_group("Line Geometry & Speeds")
## Number of bullets per diagonal line
@export var bullets_per_line: int = 8
## Spacing in pixels between consecutive bullets along the line at spawn (0.0 = all start stacked at the same point)
@export var bullet_spacing: float = 0.0
## Speed in px/s of the slowest trailing bullet (closest to wall)
@export var speed_min: float = 140.0
## Speed in px/s of the fastest leading bullet (furthest into playfield)
@export var speed_max: float = 400.0
## Added to speed_max per rank above 1 (star lines only when pellet_speed_max is set)
@export var speed_max_per_rank: float = 0.0
## Fastest speed for pellet lines; negative means pellet lines use the star speeds
@export var pellet_speed_max: float = -1.0
## ZUN's layered-shot rule: bullet k of n moves at max - (max - min) * k / n, so the
## slowest never quite reaches speed_min. Off = plain lerp from speed_min to speed_max.
@export var zun_layer_speeds: bool = false
## Distance outside the wall that lines spawn from
@export var wall_offset: float = 15.0
## Space lines evenly down the whole field (line i at i * height / count), ignoring top_y/bottom_y
@export var divide_full_height: bool = false
## Dual-inward only: the right wall fires first and the left wall this many seconds later
@export var dual_side_delay: float = 0.0
## Delay in seconds between successive line releases
@export var delay_between_lines: float = 0.08
## Top vertical coordinate for the first line on the outside wall (negative to clip slightly through top)
@export var top_y: float = -30.0
## Bottom vertical coordinate for the last line on the outside wall
@export var bottom_y: float = 840.0
## Diagonal angle in degrees relative to the horizontal wall
@export var angle_deg: float = 45.0
## Direction mode (auto detects P1 vs P2 playfield for mirroring)
@export var direction_mode: DirectionMode = DirectionMode.AUTO_FROM_OUTSIDE
## Whether the first line begins with stars (true) or pellets (false)
@export var start_with_stars: bool = true

@export_group("Audio")
## Sound effect played when each diagonal strip spawns
@export var sfx: String = "se_tan00"

func get_line_count(rank: int) -> int:
	var r: int = clampi(rank, 1, 16)
	if not line_counts.is_empty():
		for i in range(mini(line_rank_breaks.size(), line_counts.size())):
			if r < line_rank_breaks[i]:
				return line_counts[i]
		return line_counts[line_counts.size() - 1]
	if r <= 8:
		var t: float = float(r - 1) / 7.0
		return int(round(lerpf(float(lines_min), float(lines_mid), t)))
	else:
		var t: float = float(r - 8) / 8.0
		return int(round(lerpf(float(lines_mid), float(lines_max), t)))

## Speed of bullet j along a line, j = 0 being the trailing (slowest) bullet
func get_bullet_speed(j: int, is_pellet: bool, rank: int) -> float:
	var top: float = speed_max + speed_max_per_rank * float(clampi(rank, 1, 16) - 1)
	if is_pellet and pellet_speed_max >= 0.0:
		top = pellet_speed_max
	if zun_layer_speeds:
		var layer: int = bullets_per_line - 1 - j
		return top - (top - speed_min) * float(layer) / float(maxi(bullets_per_line, 1))
	var t_speed: float = float(j) / float(maxi(bullets_per_line - 1, 1))
	return lerpf(speed_min, top, t_speed)

func _get_line_y(playfield: Node2D, i: int, line_count: int) -> float:
	if divide_full_height:
		var field_height: float = 960.0
		if "PLAYFIELD_HEIGHT" in playfield:
			field_height = playfield.PLAYFIELD_HEIGHT
		return float(i) * field_height / float(maxi(line_count, 1))
	var t_line: float = float(i) / float(maxi(line_count - 1, 1))
	return lerpf(top_y, bottom_y, t_line)

func _fire_line(playfield: Node2D, data: DanmakuBulletData, base: Vector2, dir: Vector2, is_pellet: bool, rank: int, play_sound: bool = true) -> void:
	if data == null:
		return
	if play_sound and not sfx.is_empty():
		AudioService.play_sfx(sfx)
	for j in range(bullets_per_line):
		var b_pos: Vector2 = base + dir * (float(j) * bullet_spacing)
		playfield.spawn_danmaku_bullet(data, b_pos, DanmakuBullet.MotionMode.LINEAR, dir, get_bullet_speed(j, is_pellet, rank))

func execute(playfield: Node2D, _origin: Vector2, rank: int) -> void:
	if playfield == null or not playfield.is_inside_tree():
		return
	if not playfield.has_method("spawn_danmaku_bullet"):
		return
	
	var field_width: float = 600.0
	if "PLAYFIELD_WIDTH" in playfield:
		field_width = playfield.PLAYFIELD_WIDTH
	
	var rad: float = deg_to_rad(angle_deg)
	var line_count: int = get_line_count(rank)
	
	# Dual-inward mode: both walls fire pairs toward the center. With dual_side_delay the
	# right wall fires first and the left wall answers at the same height (ZUN's sub1 order).
	if direction_mode == DirectionMode.DUAL_INWARD:
		var dir_left: Vector2 = Vector2(cos(rad), sin(rad)).normalized()
		var dir_right: Vector2 = Vector2(-cos(rad), sin(rad)).normalized()
		var spawn_x_left: float = -wall_offset
		var spawn_x_right: float = field_width + wall_offset
		
		var data_left: DanmakuBulletData = bullet_star
		var data_right: DanmakuBulletData = bullet_secondary if bullet_secondary != null else bullet_star
		
		for i in range(line_count):
			if not is_instance_valid(playfield) or not playfield.is_inside_tree():
				return
			
			var line_y: float = _get_line_y(playfield, i, line_count)
			
			if dual_side_delay > 0.0:
				_fire_line(playfield, data_right, Vector2(spawn_x_right, line_y), dir_right, false, rank)
				await playfield.get_tree().create_timer(dual_side_delay).timeout
				if not is_instance_valid(playfield) or not playfield.is_inside_tree():
					return
				_fire_line(playfield, data_left, Vector2(spawn_x_left, line_y), dir_left, false, rank)
			else:
				_fire_line(playfield, data_left, Vector2(spawn_x_left, line_y), dir_left, false, rank)
				_fire_line(playfield, data_right, Vector2(spawn_x_right, line_y), dir_right, false, rank, false)
			
			# Stagger delay before launching next pair
			if i < line_count - 1 and delay_between_lines > 0.0:
				if is_instance_valid(playfield) and playfield.is_inside_tree():
					await playfield.get_tree().create_timer(delay_between_lines).timeout
		return
	
	# Single-wall mode (AUTO_FROM_OUTSIDE, LEFT_TO_RIGHT, or RIGHT_TO_LEFT)
	var is_right_wall: bool = false
	match direction_mode:
		DirectionMode.AUTO_FROM_OUTSIDE:
			if "player_number" in playfield and playfield.player_number == 2:
				is_right_wall = true
			else:
				is_right_wall = false
		DirectionMode.LEFT_TO_RIGHT:
			is_right_wall = false
		DirectionMode.RIGHT_TO_LEFT:
			is_right_wall = true
		DirectionMode.OPPOSITE_PLAYER:
			# Victim on the left half -> fire from the right wall, and vice versa.
			# Read once at cast time, like the Sakuya fan's centre.
			var victim: Node2D = playfield.player if "player" in playfield else null
			if victim != null and is_instance_valid(victim):
				is_right_wall = victim.position.x < field_width / 2.0

	var dir_x: float = -cos(rad) if is_right_wall else cos(rad)
	var dir_y: float = sin(rad)
	var dir: Vector2 = Vector2(dir_x, dir_y).normalized()

	var spawn_x: float = (field_width + wall_offset) if is_right_wall else -wall_offset
	
	for i in range(line_count):
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return
		
		# Alternate bullet types per line if enabled
		var bullet_data: DanmakuBulletData = bullet_star
		var is_pellet_line: bool = false
		if alternate_bullet_types and bullet_pellet != null:
			var is_star: bool = (i % 2 == 0) if start_with_stars else (i % 2 != 0)
			bullet_data = bullet_star if is_star else bullet_pellet
			is_pellet_line = not is_star
		elif bullet_data == null:
			bullet_data = bullet_pellet
			is_pellet_line = true
		
		_fire_line(playfield, bullet_data, Vector2(spawn_x, _get_line_y(playfield, i, line_count)), dir, is_pellet_line, rank)
		
		# Stagger delay before launching next line
		if i < line_count - 1 and delay_between_lines > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(delay_between_lines).timeout
