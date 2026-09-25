class_name GardenOfTheSun3D
extends Node3D

## Yuuka Kazami's home stage: the Garden of the Sun, a sunflower field under a pale haze.
##
## Everything here is read out of PoFV's own world09.std, at 64 PoFV units to one world
## unit (the same scale as the Eientei corridor):
##   - object 0: the earth, 256-unit tiles of world09.png, 12 units up
##   - object 1: twenty-one camera-facing sunflower clusters (world09b.png) per 256-unit tile
##   - the script: camera, facing, fog and their timings, below
##
## Forward travel slides the field towards a camera that only ever changes height and
## pitch, wrapped to one tile, so the field never recycles and holds indefinitely.
##
## Two draw calls: the ground plane, and every flower in one MultiMesh.

const UNIT: float = 1.0 / 64.0
const TILE: float = 256.0
## The first tile starts 584 PoFV units along the path; the field repeats every TILE after
const TILE_PHASE: float = 584.0

## Object 1's quads: (x, y, z) of each cluster's top-left corner within its tile, PoFV
## units (z negative is up), listed far to near as the file lists them. Placed with the
## object's instance offset of x = -20.
const FLOWERS: Array[Vector3] = [
	Vector3(0, 240, -60), Vector3(32, 224, -80), Vector3(-96, 224, -80), Vector3(-128, 208, -90),
	Vector3(-32, 192, -80), Vector3(128, 176, -50), Vector3(-16, 176, -50), Vector3(96, 160, -80),
	Vector3(-72, 144, -100), Vector3(16, 128, -90), Vector3(-96, 128, -90), Vector3(68, 112, -50),
	Vector3(72, 96, -70), Vector3(32, 80, -80), Vector3(-32, 80, -80), Vector3(32, 64, -60),
	Vector3(-128, 48, -80), Vector3(-64, 32, -90), Vector3(64, 32, -90), Vector3(-80, 16, -80),
	Vector3(-48, 0, -100),
]
const FLOWER_OFFSET_X: float = -20.0
const FLOWER_SIZE: Vector2 = Vector2(64.0, 50.0)
const GROUND_HEIGHT: float = 12.0

## Camera script, in 60 fps frames and PoFV units. Position is (along the path, height);
## facing is the look offset (ahead, down). Interpolation modes: 0 linear, 1 eases out
## (decelerating into the target), 4 eases in (accelerating away). That is the reverse of the
## ANM / ECL names for the same numbers, and is what the footage says: read the other way the
## camera drops too early, and flowers show through the dark red fog that PoFV keeps them
## hidden under. world04.std agrees: its mode-1 moves are the ones the user saw decelerate,
## and its mode 4 leads into a steady cruise.
##   t=0     at (400, 450) facing (300, 200)
##   t=0     over 1170f, mode 4, to (680, 250); facing over 1170f, mode 1, to (300, 150)
##   t=1170  over 200f, mode 4, to (872, 250)
##   t=1370  over 200f, linear, to (1384, 250)
##   t=1570  from there, linear, 1024 units every 400f, forever (ZUN loops 1384 -> 2408)
const CRUISE_FROM: float = 1570.0
const CRUISE_Y: float = 1384.0
const CRUISE_SPEED: float = 1024.0 / 400.0

## Fog, colour and near / far in PoFV units. The round opens in a pale #D0D080 haze at
## 600-800 that thickens over 600 frames into a near-black dark red #300000 at 300-400,
## close enough to hide the whole field. It holds until t=1050, then lifts over 150 frames
## to #E0E0C0 at 800-1000, where it stays. Checked against `yuuka_stage.mp4`: dark by 10s
## into the round, lifting at 17.5s, clear by 20s.
const FOG_OPEN: Color = Color8(0xd0, 0xd0, 0x80)
const FOG_DARK: Color = Color8(0x30, 0x00, 0x00)
const FOG_CLEAR: Color = Color8(0xe0, 0xe0, 0xc0)
const FOG_OPEN_RANGE: Vector2 = Vector2(600.0, 800.0)
const FOG_DARK_RANGE: Vector2 = Vector2(300.0, 400.0)
const FOG_CLEAR_RANGE: Vector2 = Vector2(800.0, 1000.0)
const FOG_DARKEN_FRAMES: float = 600.0
const FOG_LIFT_AT: float = 1050.0
const FOG_LIFT_FRAMES: float = 150.0

## Flower tiles built ahead of the camera; the fog is total well before the last one
const FLOWER_TILES: int = 6

## Kept for the Playfield, which staggers every stage's idle sway with it. PoFV's garden
## has no sway, so nothing here reads it; both fields travel in lockstep off _time.
@export var time_offset: float = 0.0

const GROUND_TEX: Texture2D = preload("res://resources/dat_textures/stage_sun_garden_ground.tres")
const FLOWER_TEX: Texture2D = preload("res://resources/dat_textures/stage_sun_garden_sunflowers.tres")
const GROUND_SHADER: Shader = preload("res://scenes/stages/garden_of_the_sun/garden_of_the_sun_ground.gdshader")
const FLOWER_SHADER: Shader = preload("res://scenes/stages/garden_of_the_sun/garden_of_the_sun_sunflowers.gdshader")

@onready var camera: Camera3D = get_node_or_null("Camera3D")
@onready var world_env: WorldEnvironment = get_node_or_null("WorldEnvironment")

var _ground_mat: ShaderMaterial
var _flower_mat: ShaderMaterial
var _time: float = 0.0


func _ready() -> void:
	if camera and world_env and world_env.environment:
		camera.environment = world_env.environment
	_build_ground()
	_build_flowers()
	_update_stage()

func reset_stage(p_offset: float = 0.0) -> void:
	_time = p_offset
	_update_stage()

func _process(delta: float) -> void:
	_time += delta
	_update_stage()

func _build_ground() -> void:
	_ground_mat = ShaderMaterial.new()
	_ground_mat.shader = GROUND_SHADER
	_ground_mat.set_shader_parameter("texture_albedo", GROUND_TEX)
	_ground_mat.set_shader_parameter("tile_length", TILE * UNIT)
	var mesh := PlaneMesh.new()
	# Wider and longer than PoFV's 512-unit strip so the frustum never finds an edge; the
	# fog has swallowed everything long before either end.
	mesh.size = Vector2(16.0, 42.0)
	var ground := MeshInstance3D.new()
	ground.name = "Ground"
	ground.mesh = mesh
	ground.position = Vector3(0.0, GROUND_HEIGHT * UNIT, -19.0)
	ground.material_override = _ground_mat
	ground.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(ground)

func _build_flowers() -> void:
	_flower_mat = ShaderMaterial.new()
	_flower_mat.shader = FLOWER_SHADER
	_flower_mat.set_shader_parameter("texture_albedo", FLOWER_TEX)
	var quad := QuadMesh.new()
	quad.size = FLOWER_SIZE * UNIT
	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.mesh = quad
	mm.instance_count = FLOWER_TILES * FLOWERS.size()
	# Farthest tile first, and within a tile the file's own far-to-near order
	var i: int = 0
	for j in range(FLOWER_TILES - 1, -1, -1):
		for f in FLOWERS:
			var centre := Vector3(
				(f.x + FLOWER_OFFSET_X + FLOWER_SIZE.x * 0.5) * UNIT,
				(-f.z - FLOWER_SIZE.y * 0.5) * UNIT,
				-(float(j) * TILE + f.y) * UNIT)
			mm.set_instance_transform(i, Transform3D(Basis.IDENTITY, centre))
			i += 1
	var field := MultiMeshInstance3D.new()
	field.name = "Sunflowers"
	field.multimesh = mm
	field.material_override = _flower_mat
	field.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	# The shader moves every instance by up to a tile; keep the whole field drawn
	field.custom_aabb = AABB(Vector3(-10.0, -2.0, -30.0), Vector3(20.0, 6.0, 36.0))
	add_child(field)

func _update_stage() -> void:
	var frame: float = _time * 60.0
	var cam := _camera_at(frame)
	# How far into the current tile the camera has come, as a slide of the field towards it
	var slide: float = fposmod(cam.x - TILE_PHASE, TILE) * UNIT
	var fog := _fog_at(frame)
	for mat in [_ground_mat, _flower_mat]:
		if mat:
			mat.set_shader_parameter("slide", slide)
			mat.set_shader_parameter("fog_color", fog[0])
			mat.set_shader_parameter("fog_start", fog[1].x * UNIT)
			mat.set_shader_parameter("fog_end", fog[1].y * UNIT)
	if world_env and world_env.environment:
		world_env.environment.background_color = fog[0]
	if camera:
		camera.position = Vector3(0.0, cam.y * UNIT, 0.0)
		camera.rotation_degrees = Vector3(-rad_to_deg(atan2(cam.z, cam.w)), 0.0, 0.0)

## (along the path, height, facing down, facing ahead) at a given frame
func _camera_at(frame: float) -> Vector4:
	var along: float
	var height: float
	if frame < 1170.0:
		var d: float = _ease_in(frame / 1170.0)
		along = lerpf(400.0, 680.0, d)
		height = lerpf(450.0, 250.0, d)
	elif frame < 1370.0:
		along = lerpf(680.0, 872.0, _ease_in((frame - 1170.0) / 200.0))
		height = 250.0
	elif frame < CRUISE_FROM:
		along = lerpf(872.0, CRUISE_Y, (frame - 1370.0) / 200.0)
		height = 250.0
	else:
		along = CRUISE_Y + (frame - CRUISE_FROM) * CRUISE_SPEED
		height = 250.0
	var look_down: float = lerpf(200.0, 150.0, _ease_out(frame / 1170.0))
	return Vector4(along, height, look_down, 300.0)

## [colour, Vector2(near, far)] at a given frame
func _fog_at(frame: float) -> Array:
	if frame < FOG_LIFT_AT:
		var d: float = clampf(frame / FOG_DARKEN_FRAMES, 0.0, 1.0)
		return [FOG_OPEN.lerp(FOG_DARK, d), FOG_OPEN_RANGE.lerp(FOG_DARK_RANGE, d)]
	var t: float = clampf((frame - FOG_LIFT_AT) / FOG_LIFT_FRAMES, 0.0, 1.0)
	return [FOG_DARK.lerp(FOG_CLEAR, t), FOG_DARK_RANGE.lerp(FOG_CLEAR_RANGE, t)]

## std interpolation modes 1 and 4
func _ease_out(t: float) -> float:
	var c: float = clampf(t, 0.0, 1.0)
	return 1.0 - (1.0 - c) * (1.0 - c)

func _ease_in(t: float) -> float:
	var c: float = clampf(t, 0.0, 1.0)
	return c * c
