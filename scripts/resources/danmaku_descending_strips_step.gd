class_name DanmakuDescendingStripsStep
extends DanmakuStep

## Danmaku step that emits horizontal strips of green pellets and green arrowheads side-by-side,
## spawning offscreen above the opponent's playfield and descending with individual directional drifts.
## The arrowheads are slightly faster than the pellets, naturally pulling ahead during flight.
## Used as Youmu's Level 2 Spellcard: Lost Sign "Binding Sword" (迷符「纏縛剣」).

enum DirectionMode {
	AUTO_FROM_OUTSIDE, ## Automatically mirrors sequential spawn direction (Left-to-Right for P1, Right-to-Left for P2)
	LEFT_TO_RIGHT,     ## Always spawn from left to right
	RIGHT_TO_LEFT      ## Always spawn from right to left
}

@export_group("Bullet Resources")
## Primary pellet bullet resource (e.g. green_pellet.tres)
@export var bullet_pellet: DanmakuBulletData = null
## Primary arrow bullet resource (e.g. green_arrow.tres)
@export var bullet_arrow: DanmakuBulletData = null

@export_group("Rank Scaling")
## Number of strips at Rank 1 (3 strips)
@export var strips_min: int = 3
## Number of strips at Rank 16 (7 strips)
@export var strips_max: int = 7

@export_group("Strip Geometry & Speeds")
## Total number of alternating bullets in each strip (e.g. 11: alternating pellet/arrow)
@export var bullets_per_strip: int = 11
## Base downward flight speed for primary/first bullet type in px/s at Rank 1
@export var pellet_speed: float = 250.0
## Downward flight speed for primary/first bullet type at Rank 16 in px/s (if <= 0.0, defaults to pellet_speed)
@export var pellet_speed_rank16: float = -1.0
## Base downward flight speed for secondary/second bullet type in px/s at Rank 1
@export var arrow_speed: float = 285.0
## Downward flight speed for secondary/second bullet type at Rank 16 in px/s (if <= 0.0, defaults to arrow_speed)
@export var arrow_speed_rank16: float = -1.0
## Delay in seconds between sequential bullet spawns in the same strip (~1.5x faster: 0.011s)
@export var delay_between_bullets: float = 0.011
## Delay in seconds between successive strips (~1.5x faster: 0.21s)
@export var delay_between_strips: float = 0.21
## Y coordinate where bullets spawn (negative = offscreen above the playfield)
@export var spawn_y: float = -30.0
## Inset margin from playfield side walls in pixels
@export var horizontal_margin: float = 45.0

@export_group("Trajectory & Drift")
## Maximum individual drift angle deviation in degrees (e.g. 3.0° -> subtle left/straight/right variation per bullet)
@export var drift_angle_max_deg: float = 3.0
## Whether successive strips alternate which bullet type starts at index 0
@export var alternate_bullet_types: bool = true
## Direction mode (auto detects P1 vs P2 playfield for outside-inward sweep)
@export var direction_mode: DirectionMode = DirectionMode.AUTO_FROM_OUTSIDE

@export_group("ZUN Sweep (pl03.ecl sub0 / sub1)")
## rank / 4 + 3 strips of rank / 8 + zun_bullets_base bullets, one per frame, each strip
## sweeping from the wall on the victim's side across to the other wall (x = i x width / n,
## y = 0). The fast type flies at 250 + 12.5 x rank px/s, the slow type at 90% of that; ZUN
## picks the slow one when (bullets left + strips left) is even. zun_slow_in_pellet_slot says
## which export slot holds the slow type (Lv2: pellet, Lv3: arrowhead in bullet_arrow).
@export var zun_mode: bool = false
@export var zun_bullets_base: int = 12
@export var zun_slow_in_pellet_slot: bool = true

@export_group("Sound Effects")
## Sound effect played on each individual bullet spawn (e.g. "se_tan00")
@export var sfx: String = "se_tan00"

func get_strip_count(rank: int) -> int:
	var r: int = clampi(rank, 1, 16)
	var t: float = float(r - 1) / 15.0
	return int(round(lerpf(float(strips_min), float(strips_max), t)))

func execute(playfield: Node2D, _origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield) or not playfield.is_inside_tree():
		return
	if zun_mode:
		await _execute_zun(playfield, rank)
		return
	if not playfield.has_method("spawn_danmaku_bullet"):
		return
	
	var strip_count: int = get_strip_count(rank)
	
	var field_width: float = 600.0
	if "PLAYFIELD_WIDTH" in playfield:
		field_width = playfield.PLAYFIELD_WIDTH
	
	var is_rtl: bool = false
	match direction_mode:
		DirectionMode.AUTO_FROM_OUTSIDE:
			if "player_number" in playfield and playfield.player_number == 2:
				is_rtl = true
			else:
				is_rtl = false
		DirectionMode.LEFT_TO_RIGHT:
			is_rtl = false
		DirectionMode.RIGHT_TO_LEFT:
			is_rtl = true
	
	var left_x: float = horizontal_margin
	var right_x: float = field_width - horizontal_margin
	var span: float = right_x - left_x
	var spacing: float = span / float(maxi(bullets_per_strip - 1, 1))
	
	var rank_ratio: float = clampf(float(clampi(rank, 1, 16) - 1) / 15.0, 0.0, 1.0)
	var max_p_speed: float = pellet_speed_rank16 if pellet_speed_rank16 > 0.0 else pellet_speed
	var cur_pellet_speed: float = lerpf(pellet_speed, max_p_speed, rank_ratio)
	
	var max_a_speed: float = arrow_speed_rank16 if arrow_speed_rank16 > 0.0 else arrow_speed
	var cur_arrow_speed: float = lerpf(arrow_speed, max_a_speed, rank_ratio)
	
	for s in range(strip_count):
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return
		
		# Determine starting bullet type for this strip
		var start_with_pellet: bool = true
		if alternate_bullet_types:
			start_with_pellet = (s % 2 == 0)
		
		# Build sequential spawn index list
		var indices: Array[int] = []
		for j in range(bullets_per_strip):
			indices.append(j)
		if is_rtl:
			indices.reverse()
		
		for j in indices:
			if not is_instance_valid(playfield) or not playfield.is_inside_tree():
				return
			
			var bx: float = left_x + float(j) * spacing
			var is_pellet: bool = (j % 2 == 0) if start_with_pellet else (j % 2 != 0)
			
			var b_data: DanmakuBulletData = bullet_pellet if is_pellet else bullet_arrow
			var b_speed: float = cur_pellet_speed if is_pellet else cur_arrow_speed
			
			# Each individual bullet receives its own subtle trajectory drift (left, straight, or right)
			var b_drift_deg: float = randf_range(-drift_angle_max_deg, drift_angle_max_deg)
			var b_rad: float = deg_to_rad(b_drift_deg)
			var b_dir: Vector2 = Vector2(sin(b_rad), cos(b_rad)).normalized()
			
			if b_data != null:
				playfield.spawn_danmaku_bullet(b_data, Vector2(bx, spawn_y), DanmakuBullet.MotionMode.LINEAR, b_dir, b_speed)
				if not sfx.is_empty():
					AudioService.play_sfx(sfx, 0.0, 1.0, true)
			
			if delay_between_bullets > 0.0:
				await playfield.get_tree().create_timer(delay_between_bullets).timeout
		
		# Delay between successive strips
		if s < strip_count - 1 and delay_between_strips > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(delay_between_strips).timeout

func _execute_zun(playfield: Node2D, rank: int) -> void:
	var r: int = clampi(rank, 1, 16)
	var strips: int = r / 4 + 3
	var n: int = r / 8 + zun_bullets_base
	var fast_speed: float = 250.0 + 12.5 * float(r)
	var slow_speed: float = fast_speed * 0.9
	var slow_data: DanmakuBulletData = bullet_pellet if zun_slow_in_pellet_slot else bullet_arrow
	var fast_data: DanmakuBulletData = bullet_arrow if zun_slow_in_pellet_slot else bullet_pellet
	var field_width: float = 600.0
	if "PLAYFIELD_WIDTH" in playfield:
		field_width = playfield.PLAYFIELD_WIDTH
	# ZUN starts on the victim's own half: PLAYER_X < 0 -> left wall
	var from_left: bool = true
	var victim: Node2D = playfield.get("player")
	if victim and is_instance_valid(victim):
		from_left = victim.position.x < field_width / 2.0
	var step_x: float = field_width / float(n)
	
	for s in range(strips):
		for i in range(n):
			if not is_instance_valid(playfield) or not playfield.is_inside_tree():
				return
			var x: float = float(i) * step_x if from_left else field_width - float(i) * step_x
			var is_slow: bool = ((n - i) + (strips - s)) % 2 == 0
			var data: DanmakuBulletData = slow_data if is_slow else fast_data
			var drift: float = deg_to_rad(randf_range(-drift_angle_max_deg, drift_angle_max_deg))
			if data != null:
				playfield.spawn_danmaku_bullet(data, Vector2(x, 0.0), DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN.rotated(drift), slow_speed if is_slow else fast_speed)
				if not sfx.is_empty():
					AudioService.play_sfx(sfx, 0.0, 1.0, true)
			if delay_between_bullets > 0.0:
				await playfield.get_tree().create_timer(delay_between_bullets).timeout
		if s < strips - 1 and delay_between_strips > 0.0 and playfield.is_inside_tree():
			await playfield.get_tree().create_timer(delay_between_strips).timeout
