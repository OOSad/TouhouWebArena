class_name MistyLake3D
extends Node3D

## Authentic Misty Lake (霧の湖) 3D Stage Environment for Cirno
## Features the signature multi-scrolling caustic water flow, lavender mist horizon, and gentle camera sway.

@export var forward_speed: float = 0.105
@export var flow_speed_mult: float = 1.0
@export var camera_height: float = 6.6
@export var camera_pitch_deg: float = -48.0

@export var sway_amplitude: float = 0.25
@export var sway_frequency: float = 0.35
@export var roll_amplitude_deg: float = 0.8
@export var time_offset: float = 0.0

@export_group("Atmospheric Fog")
@export var fog_color: Color = Color(0.518, 0.475, 0.733, 1.0) # #8479bb
@export var fog_start: float = 5.0
@export var fog_end: float = 20.0

const WATER_TEX: Texture2D = preload("res://resources/dat_textures/stage_misty_lake_water.tres")
const WATER_SHADER: Shader = preload("res://scenes/stages/misty_lake/misty_lake_water.gdshader")

@onready var camera: Camera3D = get_node_or_null("Camera3D")
@onready var world_env: WorldEnvironment = get_node_or_null("WorldEnvironment")
@onready var water_plane: MeshInstance3D = get_node_or_null("WaterPlane")

var _water_mat: ShaderMaterial
var _time: float = 0.0


func _ready() -> void:
	if camera == null:
		camera = get_node_or_null("Camera3D")
	if camera:
		camera.position = Vector3(0.0, camera_height, 0.0)
		camera.rotation_degrees = Vector3(camera_pitch_deg, 0.0, 0.0)
		if world_env and world_env.environment:
			camera.environment = world_env.environment
	
	_init_water()
	_update_stage(0.0)

func reset_stage(p_offset: float = 0.0) -> void:
	_time = p_offset
	_update_stage(0.0)

func _init_water() -> void:
	if water_plane == null:
		water_plane = get_node_or_null("WaterPlane")
	
	if water_plane == null:
		water_plane = MeshInstance3D.new()
		water_plane.name = "WaterPlane"
		var plane := PlaneMesh.new()
		plane.size = Vector2(36.0, 64.0)
		water_plane.mesh = plane
		water_plane.position = Vector3(0.0, 0.0, -18.0)
		add_child(water_plane)
	
	_water_mat = ShaderMaterial.new()
	_water_mat.shader = WATER_SHADER
	_water_mat.set_shader_parameter("texture_water", WATER_TEX)
	_water_mat.set_shader_parameter("fog_color", fog_color)
	_water_mat.set_shader_parameter("fog_start", fog_start)
	_water_mat.set_shader_parameter("fog_end", fog_end)
	_water_mat.set_shader_parameter("forward_speed", forward_speed)
	
	water_plane.material_override = _water_mat

func _process(delta: float) -> void:
	_time += delta * flow_speed_mult
	_update_stage(delta)

func _update_stage(_delta: float) -> void:
	var total_time := _time + time_offset
	
	# Update water shader time uniform and forward flight speed
	if _water_mat:
		_water_mat.set_shader_parameter("time_val", total_time)
		_water_mat.set_shader_parameter("forward_speed", forward_speed)
	
	# Gentle organic camera sway and roll
	if camera:
		var sway_x := sin(total_time * sway_frequency) * sway_amplitude
		var sway_y := cos(total_time * sway_frequency * 0.7) * (sway_amplitude * 0.4)
		var roll_z := sin(total_time * sway_frequency * 0.85) * roll_amplitude_deg
		
		camera.position = Vector3(sway_x, camera_height + sway_y, 0.0)
		camera.rotation_degrees = Vector3(camera_pitch_deg, 0.0, roll_z)


