class_name FloweringNight3D
extends Node3D

## Sakuya Izayoi's home stage: "Flowering Night" poppy field.
## Three stacked ground planes (base / mid / top, world00.png quadrants) scrolling
## steadily forward under a downward-pitched camera.

@export var forward_speed: float = 0.06
@export var camera_height: float = 6.6
@export var camera_pitch_deg: float = -58.0

@export var sway_amplitude: float = 0.6
@export var sway_frequency: float = 0.35
@export var roll_amplitude_deg: float = 0.8
@export var yaw_amplitude_deg: float = 1.5
@export var time_offset: float = 0.0

@export_group("Atmospheric Fog")
@export var fog_color: Color = Color(0.55, 0.45, 0.4, 1.0)
@export var fog_start: float = 5.0
@export var fog_end: float = 20.0

const BASE_TEX: Texture2D = preload("res://resources/dat_textures/stage_flower_field_base.tres")
const MID_TEX: Texture2D = preload("res://resources/dat_textures/stage_flower_field_mid.tres")
const TOP_TEX: Texture2D = preload("res://resources/dat_textures/stage_flower_field_top.tres")
const FIELD_SHADER: Shader = preload("res://scenes/stages/flowering_night/flowering_night_field.gdshader")

@onready var camera: Camera3D = get_node_or_null("Camera3D")
@onready var world_env: WorldEnvironment = get_node_or_null("WorldEnvironment")
@onready var field_plane: MeshInstance3D = get_node_or_null("FieldPlane")

var _field_mat: ShaderMaterial
var _time: float = 0.0


func _ready() -> void:
	if camera == null:
		camera = get_node_or_null("Camera3D")
	if camera:
		camera.position = Vector3(0.0, camera_height, 0.0)
		camera.rotation_degrees = Vector3(camera_pitch_deg, 0.0, 0.0)
		if world_env and world_env.environment:
			camera.environment = world_env.environment

	_init_field()
	_update_stage(0.0)

func reset_stage(p_offset: float = 0.0) -> void:
	_time = p_offset
	_update_stage(0.0)

func _init_field() -> void:
	if field_plane == null:
		field_plane = get_node_or_null("FieldPlane")

	if field_plane == null:
		field_plane = MeshInstance3D.new()
		field_plane.name = "FieldPlane"
		var plane := PlaneMesh.new()
		plane.size = Vector2(36.0, 64.0)
		field_plane.mesh = plane
		field_plane.position = Vector3(0.0, 0.0, -18.0)
		add_child(field_plane)

	_field_mat = ShaderMaterial.new()
	_field_mat.shader = FIELD_SHADER
	_field_mat.set_shader_parameter("texture_base", BASE_TEX)
	_field_mat.set_shader_parameter("texture_mid", MID_TEX)
	_field_mat.set_shader_parameter("texture_top", TOP_TEX)
	_field_mat.set_shader_parameter("fog_color", fog_color)
	_field_mat.set_shader_parameter("fog_start", fog_start)
	_field_mat.set_shader_parameter("fog_end", fog_end)
	_field_mat.set_shader_parameter("forward_speed", forward_speed)

	field_plane.material_override = _field_mat

func _process(delta: float) -> void:
	_time += delta
	_update_stage(delta)

func _update_stage(_delta: float) -> void:
	var total_time := _time + time_offset

	if _field_mat:
		_field_mat.set_shader_parameter("time_val", total_time)
		_field_mat.set_shader_parameter("forward_speed", forward_speed)

	# Gentle organic side-to-side sway (position drift + a touch of roll and yaw)
	if camera:
		var sway: float = sin(total_time * sway_frequency)
		var sway_x := sway * sway_amplitude
		var sway_y := cos(total_time * sway_frequency * 0.7) * (sway_amplitude * 0.4)
		var roll_z := sin(total_time * sway_frequency * 0.85) * roll_amplitude_deg
		var yaw_y := sway * yaw_amplitude_deg

		camera.position = Vector3(sway_x, camera_height + sway_y, 0.0)
		camera.rotation_degrees = Vector3(camera_pitch_deg, yaw_y, roll_z)
