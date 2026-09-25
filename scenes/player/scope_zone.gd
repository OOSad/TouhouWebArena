class_name ScopeZone
extends Area2D

# Scope Style zone for Touhou Web Arena
# Radiates from the player while holding Focus (Shift / Numpad 0)
# Reimu: Circular expanding field
# Marisa: Upward expanding searchlight / column
# Activates any unactivated Spirits that enter or are caught in the zone.

enum ScopeShape {
	CIRCLE,
	COLUMN,
	FAN_DOWN,
	LENS,
	SATELLITE_CIRCLES,
	STAR
}

@export var shape_type: ScopeShape = ScopeShape.CIRCLE
@export var max_radius: float = 130.0
@export var column_base_half_width: float = 26.0
@export var column_top_half_width: float = 46.0
## LENS short axis as a fraction of the long axis. 0.55 matches Reisen's reference footage,
## where the eye reads about twice as long as it is wide.
@export var lens_ratio: float = 0.55
@export var expand_duration: float = 0.28
@export var collapse_duration: float = 0.14
@export var aim_turn_speed: float = TAU * 1.5 # FAN_DOWN turret turn rate, radians/sec
## LENS turn rate. The eye eases out instead of turning at a constant speed, approaching
## its target exponentially, so it decelerates into place rather than stopping dead.
## Higher is snappier; this is the rate of that approach, not a fixed angular velocity.
@export var lens_turn_sharpness: float = 11.0

## SATELLITE_CIRCLES (Yuuka) configuration: 6 satellite circles orbiting clockwise around the central circle.
@export var satellite_spin_speed: float = TAU / 5.0 # Clockwise spin (rad/s, 1 rev every 5s)
@export var satellite_count: int = 6
@export var satellite_radius_ratio: float = 0.44
@export var satellite_orbit_ratio: float = 1.48
## STAR (Clownpiece): the inner corners' distance as a fraction of the points'. 0.5 is a
## little chunkier than a true pentagram (0.38), so the arms stay wide enough to catch spirits.
@export var star_inner_ratio: float = 0.5

var is_focusing: bool = false
var current_progress: float = 0.0 # 0.0 (closed) to 1.0 (fully open)
var aim_angle: float = PI * 0.5 # FAN_DOWN target direction; default = down ("at her back")
var _display_aim_angle: float = PI * 0.5 # Smoothed angle actually used for shape/collision
var _satellite_angle: float = -PI * 0.5 # 12 o'clock initial alignment matching reference
var _satellite_cols: Array[CollisionShape2D] = []

# Visual styling - single uniform darker tone with crisp outline
const FILL_COLOR: Color = Color(0.82, 0.14, 0.52, 0.38)
const OUTLINE_COLOR: Color = Color(0.98, 0.38, 0.72, 0.90)

@onready var circle_col: CollisionShape2D = $CircleShape
@onready var polygon_col: CollisionPolygon2D = $PolygonShape

func _ready() -> void:
	z_index = 0
	collision_layer = 0
	collision_mask = 8 # Layer 4 = "enemies" (Spirits)
	monitorable = false
	monitoring = true
	
	if circle_col and circle_col.shape:
		circle_col.shape = circle_col.shape.duplicate()
	
	area_entered.connect(_on_area_entered)
	_update_shape_config()
	_update_collision_and_draw()

func _init_satellite_shapes() -> void:
	if _satellite_cols.is_empty():
		for i in range(satellite_count):
			var col := CollisionShape2D.new()
			var c_shape := CircleShape2D.new()
			c_shape.radius = 1.0
			col.shape = c_shape
			col.disabled = true
			add_child(col)
			_satellite_cols.append(col)

func apply_character_data(char_data: CharacterData) -> void:
	if char_data:
		match char_data.scope_shape:
			CharacterData.ScopeShape.COLUMN:
				shape_type = ScopeShape.COLUMN
			CharacterData.ScopeShape.FAN_DOWN:
				shape_type = ScopeShape.FAN_DOWN
			CharacterData.ScopeShape.LENS:
				shape_type = ScopeShape.LENS
			CharacterData.ScopeShape.SATELLITE_CIRCLES:
				shape_type = ScopeShape.SATELLITE_CIRCLES
			CharacterData.ScopeShape.STAR:
				shape_type = ScopeShape.STAR
			_:
				shape_type = ScopeShape.CIRCLE
		max_radius = char_data.scope_radius
		expand_duration = char_data.scope_expand_duration
		collapse_duration = char_data.scope_collapse_duration
		_update_shape_config()
		_update_collision_and_draw()

func set_character(char_id: String) -> void:
	var char_data := CharacterData.get_character(char_id)
	if char_data:
		apply_character_data(char_data)
	else:
		match char_id.to_lower():
			"marisa":
				shape_type = ScopeShape.COLUMN
			"sakuya":
				shape_type = ScopeShape.FAN_DOWN
			"reisen":
				shape_type = ScopeShape.LENS
			"yuuka":
				shape_type = ScopeShape.SATELLITE_CIRCLES
			_:
				shape_type = ScopeShape.CIRCLE
		_update_shape_config()

func _update_shape_config() -> void:
	_init_satellite_shapes()
	if circle_col:
		circle_col.disabled = (shape_type != ScopeShape.CIRCLE and shape_type != ScopeShape.SATELLITE_CIRCLES)
	if polygon_col:
		polygon_col.disabled = (shape_type == ScopeShape.CIRCLE or shape_type == ScopeShape.SATELLITE_CIRCLES)
	for col in _satellite_cols:
		col.disabled = (shape_type != ScopeShape.SATELLITE_CIRCLES)

func set_focusing(focus: bool) -> void:
	is_focusing = focus

func set_aim_direction(dir: Vector2) -> void:
	# Sakuya's fan aims opposite the movement input (e.g. pressing up rotates
	# the fan to face straight down). A zero vector (no input) keeps the last aim.
	# The fan turns toward this target smoothly in _process rather than snapping.
	if dir.length_squared() < 0.0001:
		return
	if shape_type == ScopeShape.LENS:
		# Reisen's eye turns WITH the direction of travel - move sideways and it lies
		# sideways - rather than away from it like Sakuya's fan.
		#
		# DELIBERATE: the eye is symmetric, so aiming at angle + PI would draw the same
		# shape without moving, and reversing direction could be made to leave it still.
		# The original does swing the full 180 degrees, so this follows the raw input
		# angle on purpose. Do not "optimise" the spin away.
		aim_angle = dir.angle()
	else:
		aim_angle = (-dir).angle()

func _rotate_angle_toward(from: float, to: float, max_delta: float) -> float:
	var diff := wrapf(to - from, -PI, PI)
	if absf(diff) <= max_delta:
		return from + diff
	return from + signf(diff) * max_delta

func _process(delta: float) -> void:
	var target := 1.0 if is_focusing else 0.0
	var speed := (1.0 / expand_duration) if is_focusing else (1.0 / collapse_duration)

	if shape_type == ScopeShape.SATELLITE_CIRCLES:
		_satellite_angle = wrapf(_satellite_angle + satellite_spin_speed * delta, 0.0, TAU)

	if _display_aim_angle != aim_angle:
		if shape_type == ScopeShape.LENS:
			# Exponential approach: quick off the mark, easing out as it arrives. The
			# 1 - exp(-k * delta) form keeps the feel identical at any frame rate.
			var t: float = 1.0 - exp(-lens_turn_sharpness * delta)
			_display_aim_angle = lerp_angle(_display_aim_angle, aim_angle, t)
			# Exponential approach never quite lands, so settle it once it is imperceptible.
			if absf(wrapf(aim_angle - _display_aim_angle, -PI, PI)) < 0.002:
				_display_aim_angle = aim_angle
		else:
			_display_aim_angle = _rotate_angle_toward(_display_aim_angle, aim_angle, aim_turn_speed * delta)

	if current_progress != target:
		current_progress = move_toward(current_progress, target, speed * delta)
		_update_collision_and_draw()
	elif visible and shape_type == ScopeShape.COLUMN:
		if polygon_col and not polygon_col.disabled:
			polygon_col.polygon = _build_column_polygon()
		queue_redraw()
	elif visible and shape_type == ScopeShape.FAN_DOWN:
		# Keep rebuilding while stable so the fan tracks aim_angle as the player turns
		if polygon_col and not polygon_col.disabled:
			polygon_col.polygon = _build_fan_polygon()
		queue_redraw()
	elif visible and shape_type == ScopeShape.LENS:
		# Same reason: the eye keeps turning while fully open
		if polygon_col and not polygon_col.disabled:
			polygon_col.polygon = _build_lens_polygon()
		queue_redraw()
	elif visible and shape_type == ScopeShape.SATELLITE_CIRCLES:
		var d_orbit := max_radius * current_progress * satellite_orbit_ratio
		var angle_step := TAU / float(satellite_count)
		for i in range(_satellite_cols.size()):
			var col := _satellite_cols[i]
			var sat_a := _satellite_angle + float(i) * angle_step
			col.position = Vector2.from_angle(sat_a) * d_orbit
		queue_redraw()
	
	if current_progress > 0.05:
		_check_overlapping_spirits()

func _update_collision_and_draw() -> void:
	visible = (current_progress > 0.001)
	
	if shape_type == ScopeShape.CIRCLE:
		if circle_col:
			circle_col.disabled = (current_progress <= 0.05)
			if not circle_col.disabled:
				var c_shape := circle_col.shape as CircleShape2D
				if c_shape:
					c_shape.radius = max_radius * current_progress
	elif shape_type == ScopeShape.SATELLITE_CIRCLES:
		var is_open: bool = (current_progress > 0.05)
		if circle_col:
			circle_col.disabled = not is_open
			if is_open:
				var c_shape := circle_col.shape as CircleShape2D
				if c_shape:
					c_shape.radius = max_radius * current_progress
		
		var r_sat := max_radius * current_progress * satellite_radius_ratio
		var d_orbit := max_radius * current_progress * satellite_orbit_ratio
		var angle_step := TAU / float(satellite_count)
		for i in range(_satellite_cols.size()):
			var col := _satellite_cols[i]
			col.disabled = not is_open
			if is_open:
				var s_shape := col.shape as CircleShape2D
				if s_shape:
					s_shape.radius = r_sat
				var sat_a := _satellite_angle + float(i) * angle_step
				col.position = Vector2.from_angle(sat_a) * d_orbit
	elif shape_type == ScopeShape.COLUMN:
		if polygon_col:
			polygon_col.disabled = (current_progress <= 0.05)
			if not polygon_col.disabled:
				polygon_col.polygon = _build_column_polygon()
	elif shape_type == ScopeShape.FAN_DOWN:
		if polygon_col:
			polygon_col.disabled = (current_progress <= 0.05)
			if not polygon_col.disabled:
				polygon_col.polygon = _build_fan_polygon()
	elif shape_type == ScopeShape.LENS:
		if polygon_col:
			polygon_col.disabled = (current_progress <= 0.05)
			if not polygon_col.disabled:
				polygon_col.polygon = _build_lens_polygon()
	elif shape_type == ScopeShape.STAR:
		if polygon_col:
			polygon_col.disabled = (current_progress <= 0.05)
			if not polygon_col.disabled:
				polygon_col.polygon = _build_star_polygon()

	queue_redraw()

func _build_column_polygon() -> PackedVector2Array:
	var base_w := column_base_half_width * current_progress
	var top_w := column_top_half_width * current_progress
	var parent_node := get_parent()
	var player_y: float = parent_node.position.y if parent_node is Node2D else global_position.y
	var top_y := -player_y - 40.0 # Extend past top of playfield
	var bot_y := 12.0
	
	return PackedVector2Array([
		Vector2(-base_w, bot_y),
		Vector2(-top_w, top_y),
		Vector2(top_w, top_y),
		Vector2(base_w, bot_y)
	])

func _build_fan_polygon() -> PackedVector2Array:
	if current_progress <= 0.001:
		return PackedVector2Array()
	
	var r := max_radius
	var half_angle := (PI * 0.25) * current_progress # 45 degrees at full expansion
	var num_segments := 24
	var poly := PackedVector2Array()
	poly.append(Vector2.ZERO)

	# Fan is centered on the smoothed aim direction, spanning +/- half_angle
	var angle_start := _display_aim_angle - half_angle
	var angle_end := _display_aim_angle + half_angle
	
	for i in range(num_segments + 1):
		var t := float(i) / float(num_segments)
		var a := lerpf(angle_start, angle_end, t)
		poly.append(Vector2(cos(a), sin(a)) * r)
	
	return poly

## Reisen's eye: a lens (vesica piscis) centred on her hitbox, built from two circular
## arcs that meet at the two corners. `a` is the half-length along the aim axis, `b` the
## half-width across it. Both arcs share radius R = (a^2 + b^2) / 2b, which is the circle
## through (-a, 0), (0, b) and (a, 0).
func _lens_axes() -> Vector2:
	var a: float = max_radius * current_progress
	var b: float = a * lens_ratio
	return Vector2(a, b)

func _build_lens_polygon() -> PackedVector2Array:
	var poly := PackedVector2Array()
	var axes := _lens_axes()
	var a: float = axes.x
	var b: float = axes.y
	if a < 1.0 or b < 0.5:
		return poly

	var r: float = (a * a + b * b) / (2.0 * b)
	var top_centre := Vector2(0.0, b - r)
	var bot_centre := Vector2(0.0, r - b)
	# Angles from the top arc's centre out to the two corners at (+/-a, 0).
	var ang_right: float = atan2(r - b, a)
	var ang_left: float = atan2(r - b, -a)

	var segments: int = 28
	var rot: float = _display_aim_angle
	# Top arc: (a, 0) -> (0, b) -> (-a, 0)
	for i in range(segments + 1):
		var t: float = float(i) / float(segments)
		var ang: float = lerpf(ang_right, ang_left, t)
		poly.append((top_centre + Vector2(cos(ang), sin(ang)) * r).rotated(rot))
	# Bottom arc: (-a, 0) -> (0, -b) -> (a, 0), skipping the shared corners
	for i in range(1, segments):
		var t: float = float(i) / float(segments)
		var ang: float = lerpf(-ang_left, -ang_right, t)
		poly.append((bot_centre + Vector2(cos(ang), sin(ang)) * r).rotated(rot))

	return poly

## Five points, one straight up, alternating with inner corners.
func _build_star_polygon() -> PackedVector2Array:
	var poly := PackedVector2Array()
	var outer: float = max_radius * current_progress
	if outer < 1.0:
		return poly
	for i in range(10):
		var r: float = outer if i % 2 == 0 else outer * star_inner_ratio
		poly.append(Vector2.from_angle(-PI * 0.5 + PI * 0.2 * float(i)) * r)
	return poly

func _draw() -> void:
	if current_progress <= 0.005:
		return
	
	if shape_type == ScopeShape.CIRCLE:
		var r := max_radius * current_progress
		draw_circle(Vector2.ZERO, r, FILL_COLOR)
		draw_arc(Vector2.ZERO, r, 0.0, TAU, 64, OUTLINE_COLOR, 2.0, true)
	elif shape_type == ScopeShape.SATELLITE_CIRCLES:
		var r_center := max_radius * current_progress
		draw_circle(Vector2.ZERO, r_center, FILL_COLOR)
		draw_arc(Vector2.ZERO, r_center, 0.0, TAU, 48, OUTLINE_COLOR, 2.0, true)
		
		var r_sat := max_radius * current_progress * satellite_radius_ratio
		var d_orbit := max_radius * current_progress * satellite_orbit_ratio
		var angle_step := TAU / float(satellite_count)
		for i in range(satellite_count):
			var sat_a := _satellite_angle + float(i) * angle_step
			var sat_center := Vector2.from_angle(sat_a) * d_orbit
			draw_circle(sat_center, r_sat, FILL_COLOR)
			draw_arc(sat_center, r_sat, 0.0, TAU, 32, OUTLINE_COLOR, 2.0, true)
	elif shape_type == ScopeShape.COLUMN:
		var poly := _build_column_polygon()
		if poly.size() >= 4:
			draw_colored_polygon(poly, FILL_COLOR)
			draw_line(poly[0], poly[1], OUTLINE_COLOR, 2.0, true)
			draw_line(poly[2], poly[3], OUTLINE_COLOR, 2.0, true)
			draw_line(poly[1], poly[2], OUTLINE_COLOR, 1.5, true)
			draw_line(poly[3], poly[0], OUTLINE_COLOR, 1.5, true)
	elif shape_type == ScopeShape.FAN_DOWN:
		var poly := _build_fan_polygon()
		if poly.size() >= 3:
			draw_colored_polygon(poly, FILL_COLOR)
			# Radial side boundaries connecting back to the player origin
			draw_line(Vector2.ZERO, poly[1], OUTLINE_COLOR, 2.0, true)
			draw_line(Vector2.ZERO, poly[poly.size() - 1], OUTLINE_COLOR, 2.0, true)
			# Outer curved arc connecting the fan endpoints
			var r := max_radius
			var half_angle := (PI * 0.25) * current_progress
			draw_arc(Vector2.ZERO, r, _display_aim_angle - half_angle, _display_aim_angle + half_angle, 24, OUTLINE_COLOR, 2.0, true)
	elif shape_type == ScopeShape.LENS:
		var poly := _build_lens_polygon()
		if poly.size() >= 3:
			draw_colored_polygon(poly, FILL_COLOR)
			# Closed outline around the whole eye, including back to the first corner
			var outline := poly.duplicate()
			outline.append(poly[0])
			draw_polyline(outline, OUTLINE_COLOR, 2.0, true)
	elif shape_type == ScopeShape.STAR:
		var poly := _build_star_polygon()
		if poly.size() >= 3:
			draw_colored_polygon(poly, FILL_COLOR)
			var outline := poly.duplicate()
			outline.append(poly[0])
			draw_polyline(outline, OUTLINE_COLOR, 2.0, true)

func _on_area_entered(area: Area2D) -> void:
	if current_progress > 0.05:
		_try_activate_spirit(area)

func _check_overlapping_spirits() -> void:
	# 1. Physics query
	for area in get_overlapping_areas():
		_try_activate_spirit(area)
	
	# 2. Direct geometric overlap check against all spirits in playfield
	# Bulletproof fallback independent of physics server ticks or collision polygon edge cases
	var entities := _get_entities_layer()
	if entities:
		for child in entities.get_children():
			if child is Spirit and child.state == Spirit.SpiritState.NORMAL and not child.is_dead:
				if _is_spirit_in_scope(child):
					child.activate()

func _get_entities_layer() -> Node2D:
	var p := get_parent()
	if p:
		var ent := p.get_parent()
		if ent is Node2D:
			return ent
	return null

func _is_spirit_in_scope(spirit: Spirit) -> bool:
	const SPIRIT_RADIUS: float = 20.0
	var p_pos := global_position
	var s_pos := spirit.global_position
	
	if shape_type == ScopeShape.CIRCLE:
		var r := max_radius * current_progress + SPIRIT_RADIUS
		return p_pos.distance_squared_to(s_pos) <= (r * r)
	elif shape_type == ScopeShape.COLUMN:
		# Column mode (Marisa)
		# Spirit must be above the player's baseline (with a small margin below)
		if s_pos.y > p_pos.y + 24.0:
			return false
		
		# Compute beam half-width at the spirit's Y coordinate
		var t := clampf((p_pos.y - s_pos.y) / maxf(p_pos.y, 1.0), 0.0, 1.0)
		var beam_w := lerpf(column_base_half_width, column_top_half_width, t) * current_progress + SPIRIT_RADIUS
		var x_dist := absf(s_pos.x - p_pos.x)
		return x_dist <= beam_w
	elif shape_type == ScopeShape.FAN_DOWN:
		var rel := s_pos - p_pos
		var dist := rel.length()
		var r := max_radius + SPIRIT_RADIUS
		if dist > r:
			return false
		if dist < 0.001:
			return true
		var angle_diff := absf(rel.angle_to(Vector2.from_angle(_display_aim_angle)))
		var half_angle := (PI * 0.25) * current_progress
		var angle_tolerance := asin(clampf(SPIRIT_RADIUS / maxf(dist, SPIRIT_RADIUS), 0.0, 1.0))
		return angle_diff <= (half_angle + angle_tolerance)
	elif shape_type == ScopeShape.LENS:
		var axes := _lens_axes()
		var a: float = axes.x
		var b: float = axes.y
		if a < 1.0 or b < 0.5:
			return false
		# Into lens-local space, where the long axis lies along +X.
		var local := (s_pos - p_pos).rotated(-_display_aim_angle)
		var r := (a * a + b * b) / (2.0 * b) + SPIRIT_RADIUS
		# The lens is the overlap of the two arc circles, so the point must be inside both.
		var top_centre := Vector2(0.0, b - (a * a + b * b) / (2.0 * b))
		var bot_centre := Vector2(0.0, (a * a + b * b) / (2.0 * b) - b)
		return local.distance_squared_to(top_centre) <= r * r \
			and local.distance_squared_to(bot_centre) <= r * r
	elif shape_type == ScopeShape.STAR:
		# Inside the star, or close enough to an edge that the spirit's body overlaps it.
		var poly := _build_star_polygon()
		if poly.size() < 3:
			return false
		var local := s_pos - p_pos
		if Geometry2D.is_point_in_polygon(local, poly):
			return true
		for i in poly.size():
			var edge_point := Geometry2D.get_closest_point_to_segment(local, poly[i], poly[(i + 1) % poly.size()])
			if local.distance_squared_to(edge_point) <= SPIRIT_RADIUS * SPIRIT_RADIUS:
				return true
		return false
	elif shape_type == ScopeShape.SATELLITE_CIRCLES:
		var r_center := max_radius * current_progress + SPIRIT_RADIUS
		if p_pos.distance_squared_to(s_pos) <= (r_center * r_center):
			return true
		
		var r_sat := (max_radius * current_progress * satellite_radius_ratio) + SPIRIT_RADIUS
		var r_sat_sq := r_sat * r_sat
		var d_orbit := max_radius * current_progress * satellite_orbit_ratio
		var angle_step := TAU / float(satellite_count)
		for i in range(satellite_count):
			var sat_a := _satellite_angle + float(i) * angle_step
			var sat_center := p_pos + Vector2.from_angle(sat_a) * d_orbit
			if sat_center.distance_squared_to(s_pos) <= r_sat_sq:
				return true
		return false
	return false

func _try_activate_spirit(area: Area2D) -> void:
	if area is Spirit:
		if area.state == Spirit.SpiritState.NORMAL and not area.is_dead:
			area.activate()
