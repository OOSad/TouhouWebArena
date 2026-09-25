class_name EienteiCorridor3D
extends Node3D

## Reisen Udongein Inaba's home stage: the endless Eientei corridor.
## Two facing walls of bamboo-painted sliding doors and a polished wood floor
## between them, with no ceiling. The camera hangs just above where the ceiling
## beam would sit, pitched down into the hallway, drifting slowly forward.

@export_group("Corridor Shape")
## Distance from the centre line to each wall. Held fixed across both camera
## states: the corridor cannot change shape at the transition, and this width
## reproduces the opening frame and the post-transition frame equally well.
@export var half_width: float = 1.95
@export var wall_height: float = 3.4
## How far ahead the corridor is built. Fog swallows the far end long before this.
@export var corridor_length: float = 60.0
## World units covered by one repeat of the wall texture (one bay of four doors).
@export var wall_tile_length: float = 4.0
## World units covered by one repeat of the floor texture.
@export var floor_tile_length: float = 12.0

@export_group("Camera & Motion")
## Units travelled forward per second.
@export var scroll_speed: float = 1.8
## Set by hand by the user against the running game, not fitted to a screenshot.
## It rides ABOVE the doors looking down into the hallway, which is what the
## brief said from the start. Every fit to PoFV footage came out shallower than
## this and read wrong in motion, so the framing here is deliberately not the
## reference's: it trades authenticity for bullets that stay legible against the
## boards. The corridor itself (width, wall height, fog, speed) is still the
## measured one, untouched.
@export var camera_height: float = 4.6
@export var camera_pitch_deg: float = -40.0

## The stage does not hold one pose. It opens in the one above, moves at
## `second_pose_time` and again at `third_pose_time`, settling into a resting
## angle that pans left and right while still advancing. All three poses were
## set by hand against the running game; see the note above.
@export var second_camera_height: float = 3.1
@export var second_camera_pitch_deg: float = -27.0
@export var third_camera_height: float = 2.3
@export var third_camera_pitch_deg: float = -12.0
## Seconds into the round at which each move begins.
@export var second_pose_time: float = 35.0
@export var third_pose_time: float = 60.0
## Each move takes this long and eases out, decelerating into its new angle.
@export var pose_move_duration: float = 2.0
## How far the camera looks to each side once it has settled into the third
## pose. Nothing pans before then.
@export var resting_pan_deg: float = 10.0
## Seconds for one full left-right-left sweep of that pan.
@export var resting_pan_period: float = 21.0
## How far the camera rolls about its view axis during that pan, tilting the whole
## corridor (user-observed in PoFV play). Set by the user in the tuner, deliberately
## exaggerated past the reference because it reads better.
@export var resting_roll_deg: float = -34.0
## Where the roll sits in the pan's cycle: 0 rolls in step with the look, 90 makes the
## view trace a circle instead of rocking side to side.
@export var resting_roll_phase_deg: float = 0.0

@export var sway_amplitude: float = 0.09
@export var sway_frequency: float = 0.3
@export var roll_amplitude_deg: float = 0.35
@export var yaw_amplitude_deg: float = 0.5
@export var time_offset: float = 0.0

@export_group("Lamps")
## Lamp glows from world04.std object 2: four per 384-unit wall section, so one every half
## wall tile, 104 of the wall's 128 units out from centre, alternating between 212 and 160
## units up a 240-unit wall. Stored as fractions so they follow the corridor's own shape.
@export var lamp_size: float = 0.975
@export var lamp_inset: float = 104.0 / 128.0
@export var lamp_height_high: float = 212.0 / 240.0
@export var lamp_height_low: float = 160.0 / 240.0
@export var lamp_intensity: float = 1.0

@export_group("Atmosphere")
@export var fog_color: Color = Color(0.1255, 0.1216, 0.2471, 1.0)
@export var fog_start: float = 3.0
@export var fog_end: float = 13.0
## Lamplight falls off past the walls' own lit band, so the floor reads darker.
@export var floor_tint: Color = Color(1.0, 1.0, 1.0, 1.0)
@export var wall_tint: Color = Color(1.0, 1.0, 1.0, 1.0)

const WALL_TEX: Texture2D = preload("res://resources/dat_textures/stage_corridor_wall.tres")
const FLOOR_TEX: Texture2D = preload("res://resources/dat_textures/stage_corridor_floor.tres")
const CORRIDOR_SHADER: Shader = preload("res://scenes/stages/eientei_corridor/eientei_corridor.gdshader")
const LAMP_TEX: Texture2D = preload("res://resources/dat_textures/stage_corridor_lamp.tres")
const LAMP_SHADER: Shader = preload("res://scenes/stages/eientei_corridor/eientei_corridor_lamp.gdshader")

## How far behind the camera the geometry starts, so the near edge is off-screen.
const BEHIND_CAMERA: float = 6.0

## The backdrop hangs this far in front of the camera, well past the point where
## fog is already total, so it hides the corridor's far end without cropping
## anything you could still see.
const BACKDROP_DISTANCE: float = 22.0

@onready var camera: Camera3D = get_node_or_null("Camera3D")
@onready var world_env: WorldEnvironment = get_node_or_null("WorldEnvironment")

var _floor_plane: MeshInstance3D = null
var _backdrop: MeshInstance3D = null
var _left_wall: MeshInstance3D = null
var _right_wall: MeshInstance3D = null

var _floor_mat: ShaderMaterial
var _backdrop_mat: ShaderMaterial
var _left_wall_mat: ShaderMaterial
var _right_wall_mat: ShaderMaterial
var _lamp_mat: ShaderMaterial
# Lamp pairs from the camera outward; index parity picks the height
var _lamps: Array[MeshInstance3D] = []

var _time: float = 0.0


func _ready() -> void:
	if camera == null:
		camera = get_node_or_null("Camera3D")
	if world_env == null:
		world_env = get_node_or_null("WorldEnvironment")
	if camera and world_env and world_env.environment:
		camera.environment = world_env.environment

	_build_corridor()
	_update_stage()

func reset_stage(p_offset: float = 0.0) -> void:
	_time = p_offset
	_update_stage()

func _build_corridor() -> void:
	var centre_z: float = BEHIND_CAMERA - (corridor_length * 0.5)

	_floor_mat = _make_material(FLOOR_TEX, Vector2(1.0, corridor_length / floor_tile_length), floor_tint)
	_left_wall_mat = _make_material(WALL_TEX, Vector2(corridor_length / wall_tile_length, 1.0), wall_tint)
	_right_wall_mat = _make_material(WALL_TEX, Vector2(corridor_length / wall_tile_length, 1.0), wall_tint)

	var floor_mesh := PlaneMesh.new()
	floor_mesh.size = Vector2(half_width * 2.0, corridor_length)
	_floor_plane = MeshInstance3D.new()
	_floor_plane.name = "FloorPlane"
	_floor_plane.mesh = floor_mesh
	_floor_plane.position = Vector3(0.0, 0.0, centre_z)
	_floor_plane.material_override = _floor_mat
	add_child(_floor_plane)

	var wall_mesh := QuadMesh.new()
	wall_mesh.size = Vector2(corridor_length, wall_height)

	# A QuadMesh faces +Z, so each wall is yawed a quarter turn to face inward.
	_left_wall = MeshInstance3D.new()
	_left_wall.name = "LeftWall"
	_left_wall.mesh = wall_mesh
	_left_wall.position = Vector3(-half_width, wall_height * 0.5, centre_z)
	_left_wall.rotation_degrees = Vector3(0.0, 90.0, 0.0)
	_left_wall.material_override = _left_wall_mat
	add_child(_left_wall)

	_right_wall = MeshInstance3D.new()
	_right_wall.name = "RightWall"
	_right_wall.mesh = wall_mesh
	_right_wall.position = Vector3(half_width, wall_height * 0.5, centre_z)
	_right_wall.rotation_degrees = Vector3(0.0, -90.0, 0.0)
	_right_wall.material_override = _right_wall_mat
	add_child(_right_wall)

	_build_backdrop()
	_build_lamps()

## A flat screen-parallel plane riding with the camera, drawn through the same
## shader and the same fog_color uniform as the corridor.
##
## Letting the Environment paint the background instead leaves the corridor's
## far end faintly visible: the corridor fades to fog_color through the shader's
## colour pipeline while the Environment clears to its own, and the two do not
## land on identical pixels. Whatever that value turns out to be, the backdrop
## carries the same one, so the seam cannot exist.
func _build_backdrop() -> void:
	if camera == null:
		return
	var mesh := QuadMesh.new()
	# Square and generous, so it still covers the frustum at this distance when the
	# resting pan rolls the camera.
	mesh.size = Vector2(BACKDROP_DISTANCE * 3.0, BACKDROP_DISTANCE * 3.0)

	_backdrop_mat = _make_material(WALL_TEX, Vector2.ONE, Color.WHITE)
	# Fully fogged at any distance, so it outputs fog_color flat.
	_backdrop_mat.set_shader_parameter("fog_start", 0.0)
	_backdrop_mat.set_shader_parameter("fog_end", 0.001)

	_backdrop = MeshInstance3D.new()
	_backdrop.name = "Backdrop"
	_backdrop.mesh = mesh
	_backdrop.position = Vector3(0.0, 0.0, -BACKDROP_DISTANCE)
	_backdrop.material_override = _backdrop_mat
	camera.add_child(_backdrop)

func _make_material(tex: Texture2D, uv_scale: Vector2, tint: Color) -> ShaderMaterial:
	var mat := ShaderMaterial.new()
	mat.shader = CORRIDOR_SHADER
	mat.set_shader_parameter("texture_albedo", tex)
	mat.set_shader_parameter("uv_scale", uv_scale)
	mat.set_shader_parameter("uv_offset", Vector2.ZERO)
	mat.set_shader_parameter("tint", tint)
	mat.set_shader_parameter("fog_color", fog_color)
	mat.set_shader_parameter("fog_start", fog_start)
	mat.set_shader_parameter("fog_end", fog_end)
	return mat

func _process(delta: float) -> void:
	_time += delta
	_update_stage()

func _update_stage() -> void:
	var total_time: float = _time + time_offset
	# Travel is shared by both players; only the sway is offset per field, so
	# P1 and P2 advance down the corridor in lockstep.
	# Offsets run negative along each surface UV axis: sliding the texture
	# backwards is what pulls the corridor towards the camera, i.e. forward
	# travel. Wrapped to one tile so the offset cannot drift into float
	# imprecision over a long match; both textures repeat with period 1.0.
	var travelled: float = _time * scroll_speed

	if _floor_mat:
		_floor_mat.set_shader_parameter("uv_offset", Vector2(0.0, -fmod(travelled / floor_tile_length, 1.0)))
	# The two walls are mirror images, so their UVs run in opposite directions.
	if _left_wall_mat:
		_left_wall_mat.set_shader_parameter("uv_offset", Vector2(fmod(travelled / wall_tile_length, 1.0), 0.0))
	_update_lamps(travelled)
	if _right_wall_mat:
		_right_wall_mat.set_shader_parameter("uv_offset", Vector2(-fmod(travelled / wall_tile_length, 1.0), 0.0))

	if camera:
		var pose: Vector2 = _pose_at(_time)
		var sway: float = sin(total_time * sway_frequency)
		camera.position = Vector3(sway * sway_amplitude, pose.x, 0.0)
		camera.rotation_degrees = Vector3(
			pose.y,
			sway * yaw_amplitude_deg + _resting_pan(_time),
			-sway * roll_amplitude_deg + _resting_roll(_time)
		)


## Camera height and pitch at a given moment, as (height, pitch).
##
## Driven by _time rather than total_time: time_offset exists only to put the
## two fields' idle sway out of phase, and both players must reach each pose
## together.
func _pose_at(t: float) -> Vector2:
	var first := Vector2(camera_height, camera_pitch_deg)
	var second := Vector2(second_camera_height, second_camera_pitch_deg)
	var third := Vector2(third_camera_height, third_camera_pitch_deg)
	if t <= second_pose_time:
		return first
	if t < third_pose_time:
		return first.lerp(second, _ease_out((t - second_pose_time) / pose_move_duration))
	return second.lerp(third, _ease_out((t - third_pose_time) / pose_move_duration))


## Cubic ease-out: fast away from the old angle, decelerating into the new one.
func _ease_out(t: float) -> float:
	var c: float = clampf(t, 0.0, 1.0)
	return 1.0 - pow(1.0 - c, 3.0)


## The left-right pan, which starts the moment the camera settles into its
## third pose and runs from zero rather than jumping mid-sweep.
func _resting_pan(t: float) -> float:
	var settled: float = third_pose_time + pose_move_duration
	if t <= settled or resting_pan_period <= 0.0:
		return 0.0
	return sin((t - settled) * TAU / resting_pan_period) * resting_pan_deg


## The roll that rides along with the resting pan. Same period; its phase is its own.
## Eased in over the first pose_move_duration so a non-zero phase does not snap in.
func _resting_roll(t: float) -> float:
	var settled: float = third_pose_time + pose_move_duration
	if t <= settled or resting_pan_period <= 0.0:
		return 0.0
	var ease_in: float = clampf((t - settled) / maxf(pose_move_duration, 0.01), 0.0, 1.0)
	var phase: float = deg_to_rad(resting_roll_phase_deg)
	return sin((t - settled) * TAU / resting_pan_period + phase) * resting_roll_deg * ease_in


## One pair of lamps (left and right wall) every half wall tile, from behind the camera to
## past total fog. They slide towards the camera with the walls and wrap every full tile,
## where the high/low pattern repeats, so the wrap is invisible.
func _build_lamps() -> void:
	_lamp_mat = ShaderMaterial.new()
	_lamp_mat.shader = LAMP_SHADER
	_lamp_mat.set_shader_parameter("texture_albedo", LAMP_TEX)
	_lamp_mat.set_shader_parameter("intensity", lamp_intensity)
	_lamp_mat.set_shader_parameter("fog_start", fog_start)
	_lamp_mat.set_shader_parameter("fog_end", fog_end)
	var quad := QuadMesh.new()
	quad.size = Vector2(lamp_size, lamp_size)
	var spacing: float = wall_tile_length * 0.5
	var rows: int = ceili((BEHIND_CAMERA + fog_end + wall_tile_length) / spacing)
	for i in range(rows):
		for side in [-1.0, 1.0]:
			var lamp := MeshInstance3D.new()
			lamp.name = "Lamp%d%s" % [i, "L" if side < 0.0 else "R"]
			lamp.mesh = quad
			lamp.material_override = _lamp_mat
			lamp.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
			var height: float = lamp_height_high if i % 2 == 0 else lamp_height_low
			lamp.position = Vector3(side * half_width * lamp_inset, height * wall_height, 0.0)
			add_child(lamp)
			_lamps.append(lamp)


func _update_lamps(travelled: float) -> void:
	if _lamps.is_empty():
		return
	var spacing: float = wall_tile_length * 0.5
	var slide: float = fmod(travelled, wall_tile_length)
	for n in range(_lamps.size()):
		var row: int = n / 2
		_lamps[n].position.z = BEHIND_CAMERA - float(row) * spacing + slide - wall_tile_length
	# PoFV swaps the glow between alpha 0xc0 and 0xff every 60 fps frame.
	_lamp_mat.set_shader_parameter("flicker", 1.0 if int(_time * 60.0) % 2 == 0 else 192.0 / 255.0)
