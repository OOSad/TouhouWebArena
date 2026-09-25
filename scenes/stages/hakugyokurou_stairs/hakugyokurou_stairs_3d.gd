class_name HakugyokurouStairs3D
extends Node3D

## Configuration & Tuning
@export var step_count: int = 44
@export var step_width: float = 3.6
@export var step_depth: float = 0.45
@export var step_height: float = 0.45
@export var cruise_speed: float = 1.2

@export_group("Balustrades / Gutters")
@export var show_left_balustrade: bool = true
@export var show_right_balustrade: bool = true
@export var balustrade_segment_count: int = 11 # 11 long segmented slabs along the slope (~2.55m each)
@export var balustrade_rot_deg: Vector3 = Vector3(-45.0, 0.0, 0.0) # -45 deg aligns QuadMesh along 45 deg slope
@export var balustrade_offset_x: float = 0.0 # extra outward distance (+ widens, - narrows)
@export var balustrade_offset_y: float = -0.15 # height above step surface
@export var balustrade_offset_z: float = 0.0 # forward / backward shift along Z
@export var balustrade_width: float = 0.42 # width across curb
@export var balustrade_length: float = 2.56 # length along slope (~254px 1:1 texture mapping)

@export_group("Fog Tuning")
@export var fog_start: float = 4.8 # Distance along view axis where fog starts rising
@export var fog_end: float = 8.6 # Distance where fog reaches 100% solid opacity

# Calibrated Texture UV Alignments
const BALUSTRADE_RIGHT_UV_SCALE := Vector2(1.0, 1.0)
const BALUSTRADE_RIGHT_UV_OFFSET := Vector2(3.0, 1.73)

const BALUSTRADE_LEFT_UV_SCALE := Vector2(1.0, 1.0)
const BALUSTRADE_LEFT_UV_OFFSET := Vector2(0.0, 0.295)


# Steps, as PoFV builds them (world03.std object 0 with world03.anm scripts 0-15): the stair
# relief is cut into 8 stacked 254x32 bands, and every face of every step shows one band.
# Risers stand upright at full brightness showing band i; treads lie flat at 25% brightness
# (color 0x40) showing band i + 4. That light/dark pairing is what makes each step read.
const RISER_BANDS: int = 8
const RISER_UV_SCALE := Vector2(1.0, 1.0 / RISER_BANDS)
const RISER_UV_OFFSET := Vector2(0.0, 0.0)
const TREAD_BAND_SHIFT: int = 4
const TREAD_TINT := Vector3(0.25, 0.25, 0.25)

# Fog and lighting palettes
const COLOR_FOG_DIM: Color = Color(0.18, 0.12, 0.16, 1.0)
const COLOR_FOG_BRIGHT: Color = Color(0.66, 0.53, 0.57, 1.0)
const COLOR_LIGHT_DIM: Color = Color(0.24, 0.18, 0.22, 1.0)
const COLOR_LIGHT_BRIGHT: Color = Color(1.0, 1.0, 1.0, 1.0)

# Textures
const RISER_TEX: Texture2D = preload("res://resources/dat_textures/stage_stair_riser.tres")
const BALUSTRADE_LEFT_TEX: Texture2D = preload("res://resources/dat_textures/stage_stair_balustrade.tres")
const BALUSTRADE_RIGHT_TEX: Texture2D = preload("res://resources/dat_textures/stage_stair_balustrade_right.tres")
const BALUSTRADE_TEX: Texture2D = BALUSTRADE_RIGHT_TEX
const GROUND_TEX: Texture2D = preload("res://resources/dat_textures/stage_stair_ground.tres")
const CLOUD_A_TEX: Texture2D = preload("res://resources/dat_textures/stage_cloud_a.tres")
const CLOUD_B_TEX: Texture2D = preload("res://resources/dat_textures/stage_cloud_b.tres")

const FOG_SHADER: Shader = preload("res://scenes/stages/hakugyokurou_stairs/hakugyokurou_stage_fog.gdshader")
const CLOUD_MIST_SHADER: Shader = preload("res://scenes/stages/hakugyokurou_stairs/hakugyokurou_cloud_mist.gdshader")

# Node references
@onready var camera: Camera3D = get_node_or_null("Camera3D")
@onready var world_container: Node3D = get_node_or_null("WorldContainer")
@onready var world_env: WorldEnvironment = get_node_or_null("WorldEnvironment")

# Materials
var _tread_mat: ShaderMaterial
var _riser_mat: ShaderMaterial
var _balustrade_mat: ShaderMaterial
var _balustrade_left_mat: ShaderMaterial
var _balustrade_right_mat: ShaderMaterial
var _ground_mat: ShaderMaterial
var _cloud_mat_a: ShaderMaterial
var _cloud_mat_b: ShaderMaterial

# MultiMeshes
var _tread_mm: MultiMeshInstance3D
var _riser_mm: MultiMeshInstance3D
var _balustrade_left_mm: MultiMeshInstance3D
var _balustrade_right_mm: MultiMeshInstance3D
var _cloud_a_mm: MultiMeshInstance3D
var _cloud_b_mm: MultiMeshInstance3D
var _ground_mesh: MeshInstance3D

# Camera base poses
@export var cam_pos_straight: Vector3 = Vector3(0.0, 6.0, 3.36)
@export var cam_rot_straight: Vector3 = Vector3(-22.0, 0.0, 0.0) # degrees

@export var cam_pos_diagonal: Vector3 = Vector3(0.015, 5.385, 2.58)
@export var cam_rot_diagonal: Vector3 = Vector3(-11.585, -14.0, -2.5) # degrees

# Cloud data
const CLOUD_COUNT_PER_TYPE: int = 10
var _cloud_data_a: Array[Dictionary] = []
var _cloud_data_b: Array[Dictionary] = []

# State & Timeline
var _time: float = 0.0
var _current_speed: float = 0.0
var _scroll_distance: float = 0.0
var _ground_scroll: float = 0.0

# Optional offset for P2 desync/variation
@export var time_offset: float = 0.0

func _ready() -> void:
	if camera == null:
		camera = get_node_or_null("Camera3D")
	if world_container == null:
		world_container = get_node_or_null("WorldContainer")
	if world_container == null:
		world_container = Node3D.new()
		world_container.name = "WorldContainer"
		add_child(world_container)

	_init_materials()
	_init_stairs_geometry()
	_init_ground_plane()
	_init_clouds()
	
	_update_timeline(0.0)

func reset_stage(p_offset: float = 0.0) -> void:
	_time = p_offset
	_scroll_distance = 0.0
	_current_speed = 0.0
	_update_timeline(0.0)
	_update_stairs_multimesh()

func _init_materials() -> void:
	_tread_mat = ShaderMaterial.new()
	_tread_mat.shader = FOG_SHADER
	_tread_mat.set_shader_parameter("texture_albedo", RISER_TEX)
	_tread_mat.set_shader_parameter("fog_start", fog_start)
	_tread_mat.set_shader_parameter("fog_end", fog_end)
	_tread_mat.set_shader_parameter("uv_scale", RISER_UV_SCALE)
	_tread_mat.set_shader_parameter("uv_offset", RISER_UV_OFFSET)
	_tread_mat.set_shader_parameter("use_instance_band", true)
	_tread_mat.set_shader_parameter("tint", TREAD_TINT)

	_riser_mat = ShaderMaterial.new()
	_riser_mat.shader = FOG_SHADER
	_riser_mat.set_shader_parameter("texture_albedo", RISER_TEX)
	_riser_mat.set_shader_parameter("fog_start", fog_start)
	_riser_mat.set_shader_parameter("fog_end", fog_end)
	_riser_mat.set_shader_parameter("uv_scale", RISER_UV_SCALE)
	_riser_mat.set_shader_parameter("uv_offset", RISER_UV_OFFSET)
	_riser_mat.set_shader_parameter("use_instance_band", true)

	_balustrade_left_mat = ShaderMaterial.new()
	_balustrade_left_mat.shader = FOG_SHADER
	_balustrade_left_mat.set_shader_parameter("texture_albedo", BALUSTRADE_LEFT_TEX)
	_balustrade_left_mat.set_shader_parameter("fog_start", fog_start)
	_balustrade_left_mat.set_shader_parameter("fog_end", fog_end)
	_balustrade_left_mat.set_shader_parameter("uv_scale", BALUSTRADE_LEFT_UV_SCALE)
	_balustrade_left_mat.set_shader_parameter("uv_offset", BALUSTRADE_LEFT_UV_OFFSET)

	_balustrade_right_mat = ShaderMaterial.new()
	_balustrade_right_mat.shader = FOG_SHADER
	_balustrade_right_mat.set_shader_parameter("texture_albedo", BALUSTRADE_RIGHT_TEX)
	_balustrade_right_mat.set_shader_parameter("fog_start", fog_start)
	_balustrade_right_mat.set_shader_parameter("fog_end", fog_end)
	_balustrade_right_mat.set_shader_parameter("uv_scale", BALUSTRADE_RIGHT_UV_SCALE)
	_balustrade_right_mat.set_shader_parameter("uv_offset", BALUSTRADE_RIGHT_UV_OFFSET)

	_balustrade_mat = _balustrade_right_mat

	_ground_mat = ShaderMaterial.new()
	_ground_mat.shader = FOG_SHADER
	_ground_mat.set_shader_parameter("texture_albedo", GROUND_TEX)
	_ground_mat.set_shader_parameter("fog_start", fog_start)
	_ground_mat.set_shader_parameter("fog_end", fog_end + 1.2)

	_cloud_mat_a = ShaderMaterial.new()
	_cloud_mat_a.shader = CLOUD_MIST_SHADER
	_cloud_mat_a.render_priority = 1
	_cloud_mat_a.set_shader_parameter("texture_albedo", CLOUD_A_TEX)
	_cloud_mat_a.set_shader_parameter("mist_opacity", 0.6)
	_cloud_mat_a.set_shader_parameter("near_fade_start", 3.6)
	_cloud_mat_a.set_shader_parameter("near_fade_end", 4.8)
	_cloud_mat_a.set_shader_parameter("far_fade_start", fog_start + 1.6)
	_cloud_mat_a.set_shader_parameter("far_fade_end", fog_end)

	_cloud_mat_b = ShaderMaterial.new()
	_cloud_mat_b.shader = CLOUD_MIST_SHADER
	_cloud_mat_b.render_priority = 1
	_cloud_mat_b.set_shader_parameter("texture_albedo", CLOUD_B_TEX)
	_cloud_mat_b.set_shader_parameter("mist_opacity", 0.6)
	_cloud_mat_b.set_shader_parameter("near_fade_start", 3.6)
	_cloud_mat_b.set_shader_parameter("near_fade_end", 4.8)
	_cloud_mat_b.set_shader_parameter("far_fade_start", fog_start + 1.6)
	_cloud_mat_b.set_shader_parameter("far_fade_end", fog_end)

func _init_stairs_geometry() -> void:
	# 1. Treads (horizontal step planes)
	var tread_mesh := PlaneMesh.new()
	tread_mesh.size = Vector2(step_width, step_depth)
	_tread_mm = MultiMeshInstance3D.new()
	var mm_tread := MultiMesh.new()
	mm_tread.transform_format = MultiMesh.TRANSFORM_3D
	mm_tread.use_custom_data = true
	mm_tread.instance_count = step_count
	mm_tread.mesh = tread_mesh
	_tread_mm.multimesh = mm_tread
	for i in range(step_count):
		var tread_band := float((i + TREAD_BAND_SHIFT) % RISER_BANDS) / RISER_BANDS
		mm_tread.set_instance_custom_data(i, Color(tread_band, 0.0, 0.0, 0.0))
	_tread_mm.material_override = _tread_mat
	world_container.add_child(_tread_mm)

	# 2. Risers (vertical step drop faces)
	var riser_mesh := QuadMesh.new()
	riser_mesh.size = Vector2(step_width, step_height)
	_riser_mm = MultiMeshInstance3D.new()
	var mm_riser := MultiMesh.new()
	mm_riser.transform_format = MultiMesh.TRANSFORM_3D
	mm_riser.use_custom_data = true
	mm_riser.instance_count = step_count
	mm_riser.mesh = riser_mesh
	_riser_mm.multimesh = mm_riser
	# Step i (counted up the slope) shows band i, so the steps assemble the carving.
	for i in range(step_count):
		var band := float(i % RISER_BANDS) / RISER_BANDS
		mm_riser.set_instance_custom_data(i, Color(band, 0.0, 0.0, 0.0))
	_riser_mm.material_override = _riser_mat
	world_container.add_child(_riser_mm)

	# 3. Left Balustrade
	var balustrade_mesh := QuadMesh.new()
	balustrade_mesh.size = Vector2(balustrade_width, balustrade_length)
	_balustrade_left_mm = MultiMeshInstance3D.new()
	var mm_b_left := MultiMesh.new()
	mm_b_left.transform_format = MultiMesh.TRANSFORM_3D
	mm_b_left.instance_count = balustrade_segment_count
	mm_b_left.mesh = balustrade_mesh
	_balustrade_left_mm.multimesh = mm_b_left
	_balustrade_left_mm.material_override = _balustrade_left_mat
	world_container.add_child(_balustrade_left_mm)

	# 4. Right Balustrade
	_balustrade_right_mm = MultiMeshInstance3D.new()
	var mm_b_right := MultiMesh.new()
	mm_b_right.transform_format = MultiMesh.TRANSFORM_3D
	mm_b_right.instance_count = balustrade_segment_count
	mm_b_right.mesh = balustrade_mesh
	_balustrade_right_mm.multimesh = mm_b_right
	_balustrade_right_mm.material_override = _balustrade_right_mat
	world_container.add_child(_balustrade_right_mm)

func _init_ground_plane() -> void:
	# Golden courtyard ground to the right of the stairway
	_ground_mesh = MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2(24.0, 48.0)
	_ground_mesh.mesh = plane
	_ground_mesh.material_override = _ground_mat
	# Position to the right and sloping slightly along the stairway base
	_ground_mesh.position = Vector3(12.5, 3.0, -14.0)
	_ground_mesh.rotation_degrees = Vector3(32.0, 0.0, 0.0)
	world_container.add_child(_ground_mesh)

func _init_clouds() -> void:
	var quad_a := QuadMesh.new()
	quad_a.size = Vector2(4.8, 2.4)
	_cloud_a_mm = MultiMeshInstance3D.new()
	var mm_ca := MultiMesh.new()
	mm_ca.transform_format = MultiMesh.TRANSFORM_3D
	mm_ca.instance_count = CLOUD_COUNT_PER_TYPE
	mm_ca.mesh = quad_a
	_cloud_a_mm.multimesh = mm_ca
	_cloud_a_mm.material_override = _cloud_mat_a
	world_container.add_child(_cloud_a_mm)

	var quad_b := QuadMesh.new()
	quad_b.size = Vector2(5.2, 2.6)
	_cloud_b_mm = MultiMeshInstance3D.new()
	var mm_cb := MultiMesh.new()
	mm_cb.transform_format = MultiMesh.TRANSFORM_3D
	mm_cb.instance_count = CLOUD_COUNT_PER_TYPE
	mm_cb.mesh = quad_b
	_cloud_b_mm.multimesh = mm_cb
	_cloud_b_mm.material_override = _cloud_mat_b
	world_container.add_child(_cloud_b_mm)

	# Seed cloud instances with varied, natural spawning: overlapping clusters, varied scale, height, and drift rates
	var config_a: Array[Dictionary] = [
		{"base_dist": 0.8, "x_offset": -0.70, "scale_x": 1.15, "scale_y": 1.05, "height": 0.18, "scroll_rate": 0.86, "sway_speed": 0.60, "sway_amp": 0.42, "sway_phase": 0.2, "flip_h": false},
		{"base_dist": 2.2, "x_offset": 0.75, "scale_x": 0.95, "scale_y": 0.90, "height": 0.15, "scroll_rate": 0.82, "sway_speed": 0.55, "sway_amp": 0.38, "sway_phase": 2.1, "flip_h": true},
		{"base_dist": 5.7, "x_offset": 0.25, "scale_x": 1.30, "scale_y": 1.20, "height": 0.25, "scroll_rate": 0.89, "sway_speed": 0.65, "sway_amp": 0.45, "sway_phase": 4.5, "flip_h": false},
		{"base_dist": 9.1, "x_offset": -0.85, "scale_x": 1.10, "scale_y": 1.00, "height": 0.17, "scroll_rate": 0.84, "sway_speed": 0.58, "sway_amp": 0.40, "sway_phase": 1.4, "flip_h": true},
		{"base_dist": 10.4, "x_offset": -0.20, "scale_x": 1.25, "scale_y": 1.10, "height": 0.22, "scroll_rate": 0.87, "sway_speed": 0.62, "sway_amp": 0.48, "sway_phase": 3.7, "flip_h": false},
		{"base_dist": 15.0, "x_offset": 0.85, "scale_x": 0.90, "scale_y": 0.85, "height": 0.16, "scroll_rate": 0.81, "sway_speed": 0.52, "sway_amp": 0.35, "sway_phase": 0.8, "flip_h": true},
		{"base_dist": 18.2, "x_offset": -0.40, "scale_x": 1.35, "scale_y": 1.25, "height": 0.26, "scroll_rate": 0.91, "sway_speed": 0.68, "sway_amp": 0.50, "sway_phase": 5.1, "flip_h": false},
		{"base_dist": 19.5, "x_offset": 0.35, "scale_x": 1.15, "scale_y": 1.05, "height": 0.20, "scroll_rate": 0.85, "sway_speed": 0.60, "sway_amp": 0.42, "sway_phase": 2.8, "flip_h": true},
		{"base_dist": 23.8, "x_offset": 0.70, "scale_x": 1.05, "scale_y": 1.00, "height": 0.18, "scroll_rate": 0.83, "sway_speed": 0.54, "sway_amp": 0.38, "sway_phase": 4.1, "flip_h": false},
		{"base_dist": 27.2, "x_offset": -0.60, "scale_x": 1.20, "scale_y": 1.10, "height": 0.23, "scroll_rate": 0.88, "sway_speed": 0.64, "sway_amp": 0.46, "sway_phase": 1.9, "flip_h": true},
	]

	var config_b: Array[Dictionary] = [
		{"base_dist": 1.6, "x_offset": -0.15, "scale_x": 1.25, "scale_y": 1.15, "height": 0.24, "scroll_rate": 0.88, "sway_speed": 0.58, "sway_amp": 0.44, "sway_phase": 1.1, "flip_h": true},
		{"base_dist": 4.2, "x_offset": -0.80, "scale_x": 0.95, "scale_y": 0.90, "height": 0.16, "scroll_rate": 0.80, "sway_speed": 0.50, "sway_amp": 0.36, "sway_phase": 3.4, "flip_h": false},
		{"base_dist": 6.5, "x_offset": 0.65, "scale_x": 1.20, "scale_y": 1.10, "height": 0.21, "scroll_rate": 0.86, "sway_speed": 0.62, "sway_amp": 0.42, "sway_phase": 0.5, "flip_h": true},
		{"base_dist": 9.8, "x_offset": 0.10, "scale_x": 1.30, "scale_y": 1.20, "height": 0.27, "scroll_rate": 0.90, "sway_speed": 0.66, "sway_amp": 0.48, "sway_phase": 2.6, "flip_h": false},
		{"base_dist": 13.1, "x_offset": -0.65, "scale_x": 1.05, "scale_y": 0.95, "height": 0.18, "scroll_rate": 0.83, "sway_speed": 0.56, "sway_amp": 0.38, "sway_phase": 4.9, "flip_h": true},
		{"base_dist": 16.2, "x_offset": 0.45, "scale_x": 1.20, "scale_y": 1.10, "height": 0.22, "scroll_rate": 0.87, "sway_speed": 0.61, "sway_amp": 0.45, "sway_phase": 1.7, "flip_h": false},
		{"base_dist": 17.0, "x_offset": -0.10, "scale_x": 1.35, "scale_y": 1.25, "height": 0.28, "scroll_rate": 0.92, "sway_speed": 0.70, "sway_amp": 0.52, "sway_phase": 3.9, "flip_h": true},
		{"base_dist": 21.5, "x_offset": -0.75, "scale_x": 1.10, "scale_y": 1.00, "height": 0.19, "scroll_rate": 0.84, "sway_speed": 0.57, "sway_amp": 0.40, "sway_phase": 0.3, "flip_h": false},
		{"base_dist": 24.6, "x_offset": 0.20, "scale_x": 1.15, "scale_y": 1.05, "height": 0.21, "scroll_rate": 0.85, "sway_speed": 0.59, "sway_amp": 0.42, "sway_phase": 2.4, "flip_h": true},
		{"base_dist": 28.5, "x_offset": 0.80, "scale_x": 0.90, "scale_y": 0.85, "height": 0.15, "scroll_rate": 0.79, "sway_speed": 0.48, "sway_amp": 0.34, "sway_phase": 4.7, "flip_h": false},
	]

	_cloud_data_a = config_a
	_cloud_data_b = config_b

func _process(delta: float) -> void:
	_time += delta
	_update_timeline(delta)
	_update_stairs_multimesh()
	_update_clouds(delta)

func _update_timeline(delta: float) -> void:
	# 1. Timeline Speed Controller:
	# 0.0s - 14.0s: Accelerates 0 -> cruise_speed
	# 14.0s - 60.0s: Cruising at cruise_speed
	# 60.0s - 80.0s: Decelerates cruise_speed -> 0 over 20s
	# 80.0s - 85.0s: Stationary during camera pan to diagonal
	# 85.0s+: Re-accelerates along diagonal forever
	if _time < 6.0:
		var t_accel := clampf(_time / 6.0, 0.0, 1.0)
		_current_speed = cruise_speed * smoothstep(0.0, 1.0, t_accel)
	elif _time < 60.0:
		_current_speed = cruise_speed
	elif _time < 80.0:
		var t_decel := clampf((_time - 60.0) / 20.0, 0.0, 1.0)
		_current_speed = cruise_speed * (1.0 - smoothstep(0.0, 1.0, t_decel))
	elif _time < 85.0:
		_current_speed = 0.0
	else:
		var t_reaccel := clampf((_time - 85.0) / 4.0, 0.0, 1.0)
		_current_speed = cruise_speed * smoothstep(0.0, 1.0, t_reaccel)

	_scroll_distance += _current_speed * delta

	# 2. Lighting & Fog Controller:
	# 0.0s - 14.0s: Moody dim shadow
	# 14.0s - 15.0s: 1-second transition to bright daylight mauve
	# 15.0s+: Bright Netherworld palette
	var dawn_blend := 0.0
	if _time >= 15.0:
		dawn_blend = 1.0
	elif _time >= 14.0:
		dawn_blend = smoothstep(0.0, 1.0, _time - 14.0)

	var current_fog_color := COLOR_FOG_DIM.lerp(COLOR_FOG_BRIGHT, dawn_blend)
	var current_light_mod := COLOR_LIGHT_DIM.lerp(COLOR_LIGHT_BRIGHT, dawn_blend)

	_apply_palette(current_fog_color, current_light_mod)

	# 3. Camera Position & Orientation Controller:
	# 0.0s - 80.0s: Straight-on view
	# 80.0s - 85.0s: Smooth transition to diagonal perspective
	# 85.0s+: Diagonal perspective
	if camera:
		var cam_blend := 0.0
		if _time >= 85.0:
			cam_blend = 1.0
		elif _time >= 80.0:
			cam_blend = smoothstep(0.0, 1.0, (_time - 80.0) / 5.0)

		# Subtle camera breathing sway (phase-shifted by time_offset for P2)
		var breathe_y := sin((_time + time_offset) * 0.9) * 0.02
		var breathe_x := cos((_time + time_offset) * 0.7) * 0.015

		var target_pos := cam_pos_straight.lerp(cam_pos_diagonal, cam_blend) + Vector3(breathe_x, breathe_y, 0.0)
		var target_rot := cam_rot_straight.lerp(cam_rot_diagonal, cam_blend)

		camera.position = target_pos
		camera.rotation_degrees = target_rot

func _apply_palette(fog_col: Color, light_mod: Color) -> void:
	if world_env and world_env.environment:
		world_env.environment.background_color = fog_col
	if _tread_mat:
		_tread_mat.set_shader_parameter("fog_color", fog_col)
		_tread_mat.set_shader_parameter("light_modulate", light_mod)
		_tread_mat.set_shader_parameter("fog_start", fog_start)
		_tread_mat.set_shader_parameter("fog_end", fog_end)
	if _riser_mat:
		_riser_mat.set_shader_parameter("fog_color", fog_col)
		_riser_mat.set_shader_parameter("light_modulate", light_mod)
		_riser_mat.set_shader_parameter("fog_start", fog_start)
		_riser_mat.set_shader_parameter("fog_end", fog_end)
	if _balustrade_left_mat:
		_balustrade_left_mat.set_shader_parameter("fog_color", fog_col)
		_balustrade_left_mat.set_shader_parameter("light_modulate", light_mod)
		_balustrade_left_mat.set_shader_parameter("fog_start", fog_start)
		_balustrade_left_mat.set_shader_parameter("fog_end", fog_end)
	if _balustrade_right_mat:
		_balustrade_right_mat.set_shader_parameter("fog_color", fog_col)
		_balustrade_right_mat.set_shader_parameter("light_modulate", light_mod)
		_balustrade_right_mat.set_shader_parameter("fog_start", fog_start)
		_balustrade_right_mat.set_shader_parameter("fog_end", fog_end)
	if _ground_mat:
		_ground_mat.set_shader_parameter("fog_color", fog_col)
		_ground_mat.set_shader_parameter("light_modulate", light_mod)
		_ground_mat.set_shader_parameter("fog_start", fog_start)
		_ground_mat.set_shader_parameter("fog_end", fog_end + 1.2)
	if _cloud_mat_a:
		_cloud_mat_a.set_shader_parameter("fog_color", fog_col)
		_cloud_mat_a.set_shader_parameter("light_modulate", light_mod)
		_cloud_mat_a.set_shader_parameter("far_fade_start", fog_start + 1.6)
		_cloud_mat_a.set_shader_parameter("far_fade_end", fog_end)
	if _cloud_mat_b:
		_cloud_mat_b.set_shader_parameter("fog_color", fog_col)
		_cloud_mat_b.set_shader_parameter("light_modulate", light_mod)
		_cloud_mat_b.set_shader_parameter("far_fade_start", fog_start + 1.6)
		_cloud_mat_b.set_shader_parameter("far_fade_end", fog_end)

func _update_stairs_multimesh() -> void:
	if _balustrade_left_mm:
		_balustrade_left_mm.visible = show_left_balustrade
	if _balustrade_right_mm:
		_balustrade_right_mm.visible = show_right_balustrade


	var total_loop_dist := float(step_count) * step_depth
	var b_offset_x := step_width * 0.5 + 0.19

	for i in range(step_count):
		# Distance along the 45-degree slope
		var dist := fmod(float(i) * step_depth - _scroll_distance, total_loop_dist)
		if dist < 0.0:
			dist += total_loop_dist

		# Map to 3D space: Y climbs up, Z goes back (45 deg: Y = dist, Z = -dist)
		var y_pos := dist - 0.8
		var z_pos := -dist + 0.8

		# 1. Tread (horizontal quad)
		var tread_xf := Transform3D(Basis(), Vector3(0.0, y_pos, z_pos))
		_tread_mm.multimesh.set_instance_transform(i, tread_xf)

		# 2. Riser (vertical quad on front step edge)
		var riser_xf := Transform3D(Basis(), Vector3(0.0, y_pos - step_height * 0.5, z_pos + step_depth * 0.5))
		_riser_mm.multimesh.set_instance_transform(i, riser_xf)

	# 3. Left & Right Segmented Balustrade Slabs (QuadMesh ramps)
	var b_seg_step := total_loop_dist / float(balustrade_segment_count)
	var b_rot_rad := Vector3(
		deg_to_rad(balustrade_rot_deg.x),
		deg_to_rad(balustrade_rot_deg.y),
		deg_to_rad(balustrade_rot_deg.z)
	)
	var b_basis := Basis.from_euler(b_rot_rad)

	# Wrap offset ensures quads scroll completely off-screen below camera view
	# before wrapping to the top (which is hidden in distance fog), preventing any pop or flash.
	var wrap_offset := 3.0

	for j in range(balustrade_segment_count):
		var b_dist := fmod(float(j) * b_seg_step - _scroll_distance + wrap_offset, total_loop_dist)
		if b_dist < 0.0:
			b_dist += total_loop_dist
		b_dist -= wrap_offset

		var b_y := b_dist - 0.8 + balustrade_offset_y
		var b_z := -b_dist + 0.8 + balustrade_offset_z

		var b_left_pos := Vector3(-(b_offset_x + balustrade_offset_x), b_y, b_z)
		var b_left_xf := Transform3D(b_basis, b_left_pos)
		_balustrade_left_mm.multimesh.set_instance_transform(j, b_left_xf)

		var b_right_pos := Vector3(b_offset_x + balustrade_offset_x, b_y, b_z)
		var b_right_xf := Transform3D(b_basis, b_right_pos)
		_balustrade_right_mm.multimesh.set_instance_transform(j, b_right_xf)

func _update_clouds(_delta: float) -> void:
	# Extended loop range: [-2.5, 27.5] meters along stair incline
	# Within this range, shader near_fade and far_fade guarantee 0% opacity at both ends,
	# so wrap-arounds happen completely invisibly with zero pop-in or pop-out.
	var cloud_loop_span := 30.0
	var cloud_loop_offset := -2.5
	
	# Cloud A
	for i in range(CLOUD_COUNT_PER_TYPE):
		var data := _cloud_data_a[i]
		var raw_dist := float(data["base_dist"]) - _scroll_distance * float(data.get("scroll_rate", 0.85))
		var dist := fmod(raw_dist - cloud_loop_offset, cloud_loop_span)
		if dist < 0.0:
			dist += cloud_loop_span
		dist += cloud_loop_offset

		var sway: float = sin((_time + time_offset) * float(data["sway_speed"]) + float(data["sway_phase"])) * float(data["sway_amp"])
		var h: float = float(data.get("height", 0.18))
		var y_pos := dist - 0.8 + h
		var z_pos := -dist + 0.8 + h
		var pos := Vector3(float(data["x_offset"]) + sway, y_pos, z_pos)
		
		# Parallel to 45-degree stairs slope with per-cloud scale and horizontal flip
		var flip_factor: float = -1.0 if bool(data.get("flip_h", false)) else 1.0
		var scale_x: float = float(data.get("scale_x", 1.0)) * flip_factor
		var scale_y: float = float(data.get("scale_y", 1.0))
		var basis := Basis(Vector3.RIGHT, deg_to_rad(-45.0)).scaled(Vector3(scale_x, scale_y, 1.0))
		var xf := Transform3D(basis, pos)
		_cloud_a_mm.multimesh.set_instance_transform(i, xf)

	# Cloud B
	for i in range(CLOUD_COUNT_PER_TYPE):
		var data := _cloud_data_b[i]
		var raw_dist := float(data["base_dist"]) - _scroll_distance * float(data.get("scroll_rate", 0.85))
		var dist := fmod(raw_dist - cloud_loop_offset, cloud_loop_span)
		if dist < 0.0:
			dist += cloud_loop_span
		dist += cloud_loop_offset

		var sway: float = sin((_time + time_offset) * float(data["sway_speed"]) + float(data["sway_phase"])) * float(data["sway_amp"])
		var h: float = float(data.get("height", 0.18))
		var y_pos := dist - 0.8 + h
		var z_pos := -dist + 0.8 + h
		var pos := Vector3(float(data["x_offset"]) + sway, y_pos, z_pos)
		
		var flip_factor: float = -1.0 if bool(data.get("flip_h", false)) else 1.0
		var scale_x: float = float(data.get("scale_x", 1.0)) * flip_factor
		var scale_y: float = float(data.get("scale_y", 1.0))
		var basis := Basis(Vector3.RIGHT, deg_to_rad(-45.0)).scaled(Vector3(scale_x, scale_y, 1.0))
		var xf := Transform3D(basis, pos)
		_cloud_b_mm.multimesh.set_instance_transform(i, xf)
