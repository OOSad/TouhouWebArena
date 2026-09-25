class_name MountainPond3D
extends Node3D

## Aya Shameimaru's home stage: a lily pond under drifting clouds.
##
## Read out of PoFV's world10.std, at 64 PoFV units to one world unit (as the other std
## stages). World x is PoFV x, world y is -z (PoFV z negative is up), world z is -y (along).
##   - object 0: the pond (world10.png, 12 up) over three sheets of water light (world10c.png,
##     24, 34 and 44 down, scrolling), all 800-unit quads centred at x -400 / 400 and along
##     430 / 1230. The pond's water is partly transparent, so the light and the
##     reflections show through it.
##   - objects 5-8: the clouds' reflections, flat, 60 down, swaying sideways.
##   - objects 1-4: the clouds, facing the camera, 130 to 310 up, swaying sideways.
##   - the script: fog #A0B0A0 at 800-1100 over a #A0B0A0 sky, a 30 degree field of view,
##     the camera facing (0, 250 ahead, 300 down) and gliding between four points, 1000
##     frames each, eased at both ends (zero tangents), looping every 4000 frames.
## No forward travel: the camera wanders over the same stretch of water.

const UNIT: float = 1.0 / 64.0
const QUAD: float = 800.0

const SKY: Color = Color8(0xa0, 0xb0, 0xa0)
## PoFV fogs at 800-1100; pulled in 100 so the far edge of the water never shows as a line
## when the camera rises (per the user)
const FOG_RANGE: Vector2 = Vector2(700.0, 1000.0)
const FOV: float = 30.0
const FACING: Vector2 = Vector2(250.0, 300.0) # ahead, down

## Camera points (x, along, height), each held as a 1000-frame glide to the next
const CAMERA_KEYS: Array[Vector3] = [
	Vector3(100.0, -200.0, 250.0),
	Vector3(-100.0, 0.0, 450.0),
	Vector3(100.0, -200.0, 250.0),
	Vector3(150.0, 0.0, 350.0),
]
const CAMERA_LEG_FRAMES: float = 1000.0
## op0: the whole camera path sits 400 units higher than its points say (the other stages' op0 is zero)
const CAMERA_BASE_HEIGHT: float = 400.0

## Pond layers, bottom up: [texture, PoFV z, flip, scroll per frame, scroll start, colour]
## (world10.anm scripts 3, 2, 1 and 0)
const RIPPLE_TEX: Texture2D = preload("res://resources/dat_textures/stage_pond_ripple.tres")
const POND_TEX: Texture2D = preload("res://resources/dat_textures/stage_pond.tres")
const CLOUD_TEX: Texture2D = preload("res://resources/dat_textures/stage_pond_cloud.tres")
const LAYER_SHADER: Shader = preload("res://scenes/stages/mountain_pond/mountain_pond_layer.gdshader")
const CLOUD_SHADER: Shader = preload("res://scenes/stages/mountain_pond/mountain_pond_cloud.gdshader")

const LAYERS: Array = [
	[RIPPLE_TEX, 44.0, Vector2(1, -1), Vector2(0.00002, 0.0003), Vector2.ZERO, Color8(0xe0, 0xe0, 0xff, 0x40)],
	[RIPPLE_TEX, 34.0, Vector2(-1, 1), Vector2(0.0004, 0.00005), Vector2(0.0, 0.25), Color8(0xff, 0xff, 0xff, 0x56)],
	[RIPPLE_TEX, 24.0, Vector2(1, 1), Vector2(-0.00026, -0.00005), Vector2.ZERO, Color8(0xff, 0xff, 0xff, 0x32)],
	# script 0 turns the pond half a turn about x, which mirrors it along the path
	[POND_TEX, -12.0, Vector2(1, -1), Vector2.ZERO, Vector2.ZERO, Color(1, 1, 1, 1)],
]
## The quads' corner (x, along): centred at -400 and 430, 800 across
const TILE_ORIGIN: Vector2 = Vector2(-800.0, -110.0)

## Clouds: [x, along, PoFV z, size, mirrored, sway: first leg frames, second leg frames, first way]
## Reflections (objects 5-8, scripts 8-11) first, then the clouds (objects 1-4, scripts 4-7),
## in the file's instance order.
const REFLECTIONS: Array = [
	[-64.0, 520.0, 60.0, Vector2(512, 128), false, 300.0, 300.0, 1.0],
	[128.0, 480.0, 60.0, Vector2(480, 120), true, 260.0, 360.0, -1.0],
	[-128.0, 440.0, 60.0, Vector2(512, 128), false, 400.0, 400.0, -1.0],
	[0.0, 400.0, 60.0, Vector2(480, 120), true, 360.0, 460.0, 1.0],
	[128.0, 350.0, 60.0, Vector2(480, 120), true, 260.0, 360.0, -1.0],
	[-128.0, 300.0, 60.0, Vector2(512, 128), false, 400.0, 400.0, -1.0],
	[0.0, 200.0, 60.0, Vector2(480, 120), true, 360.0, 460.0, 1.0],
]
const CLOUDS: Array = [
	[-64.0, 700.0, -130.0, Vector2(512, 128), false, 300.0, 300.0, 1.0],
	[128.0, 600.0, -160.0, Vector2(400, 100), true, 260.0, 360.0, -1.0],
	[-128.0, 600.0, -210.0, Vector2(480, 120), false, 400.0, 400.0, -1.0],
	[0.0, 500.0, -160.0, Vector2(320, 80), true, 360.0, 460.0, 1.0],
	[64.0, 700.0, -230.0, Vector2(512, 128), false, 300.0, 300.0, 1.0],
	[-128.0, 600.0, -260.0, Vector2(400, 100), true, 260.0, 360.0, -1.0],
	[128.0, 600.0, -310.0, Vector2(480, 120), false, 400.0, 400.0, -1.0],
]
const SWAY: float = 60.0

## Kept for the Playfield, which staggers every stage's idle motion with it
@export var time_offset: float = 0.0

@onready var camera: Camera3D = get_node_or_null("Camera3D")
@onready var world_env: WorldEnvironment = get_node_or_null("WorldEnvironment")

var _layer_mats: Array[ShaderMaterial] = []
var _cloud_nodes: Array[MeshInstance3D] = []
var _cloud_specs: Array = []
var _time: float = 0.0


func _ready() -> void:
	if camera:
		camera.fov = FOV
		if world_env and world_env.environment:
			camera.environment = world_env.environment
	if world_env and world_env.environment:
		world_env.environment.background_color = SKY
	_build_layers()
	for spec in REFLECTIONS:
		_add_cloud(spec, false, 0)
	for spec in CLOUDS:
		_add_cloud(spec, true, LAYERS.size() + 1)
	_update_stage()

func reset_stage(p_offset: float = 0.0) -> void:
	_time = p_offset
	_update_stage()

func _process(delta: float) -> void:
	_time += delta
	_update_stage()

func _build_layers() -> void:
	var mesh := PlaneMesh.new()
	# Far wider and longer than the camera ever sees; the texture repeats in world space
	mesh.size = Vector2(40.0, 50.0)
	for i in LAYERS.size():
		var layer: Array = LAYERS[i]
		var mat := ShaderMaterial.new()
		mat.shader = LAYER_SHADER
		mat.render_priority = i + 1
		mat.set_shader_parameter("texture_albedo", layer[0])
		mat.set_shader_parameter("tile_length", QUAD * UNIT)
		mat.set_shader_parameter("tile_origin", TILE_ORIGIN * UNIT)
		mat.set_shader_parameter("flip", layer[2])
		mat.set_shader_parameter("tint", layer[5])
		_set_fog(mat)
		var plane := MeshInstance3D.new()
		plane.mesh = mesh
		plane.position = Vector3(0.0, -float(layer[1]) * UNIT, -10.0)
		plane.material_override = mat
		plane.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		add_child(plane)
		_layer_mats.append(mat)

func _add_cloud(spec: Array, billboard: bool, priority: int) -> void:
	var mat := ShaderMaterial.new()
	mat.shader = CLOUD_SHADER
	mat.render_priority = priority
	mat.set_shader_parameter("texture_albedo", CLOUD_TEX)
	mat.set_shader_parameter("billboard", billboard)
	mat.set_shader_parameter("mirror", spec[4])
	_set_fog(mat)
	var quad := QuadMesh.new()
	quad.size = spec[3] * UNIT
	var node := MeshInstance3D.new()
	node.mesh = quad
	node.material_override = mat
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	if not billboard:
		# Flat on the water, its top edge the far one
		node.rotation_degrees = Vector3(-90.0, 0.0, 0.0)
	add_child(node)
	_cloud_nodes.append(node)
	_cloud_specs.append(spec)

func _set_fog(mat: ShaderMaterial) -> void:
	mat.set_shader_parameter("fog_color", SKY)
	mat.set_shader_parameter("fog_start", FOG_RANGE.x * UNIT)
	mat.set_shader_parameter("fog_end", FOG_RANGE.y * UNIT)

func _update_stage() -> void:
	var frame: float = (_time + time_offset) * 60.0
	for i in _layer_mats.size():
		var layer: Array = LAYERS[i]
		var s: Vector2 = layer[4] + layer[3] * frame
		_layer_mats[i].set_shader_parameter("scroll", Vector2(fposmod(s.x, 1.0), fposmod(s.y, 1.0)))
	for i in _cloud_nodes.size():
		var spec: Array = _cloud_specs[i]
		var x: float = spec[0] + _sway(frame, spec[5], spec[6], spec[7])
		_cloud_nodes[i].position = Vector3(x, -float(spec[2]), -float(spec[1])) * UNIT
	if camera:
		var cam := _camera_at(frame)
		camera.position = Vector3(cam.x, cam.z + CAMERA_BASE_HEIGHT, -cam.y) * UNIT
		camera.rotation_degrees = Vector3(-rad_to_deg(atan2(FACING.y, FACING.x)), 0.0, 0.0)

## (x, along, height) at a given frame: eased glides between the four points
func _camera_at(frame: float) -> Vector3:
	var legs: float = fposmod(frame / CAMERA_LEG_FRAMES, float(CAMERA_KEYS.size()))
	var leg: int = int(legs)
	var t: float = smoothstep(0.0, 1.0, legs - float(leg))
	return CAMERA_KEYS[leg].lerp(CAMERA_KEYS[(leg + 1) % CAMERA_KEYS.size()], t)

## The anm's sideways drift: out 60 units (decelerating) over `a` frames, back (accelerating)
## over `b`, out the other way, back, repeating
func _sway(frame: float, a: float, b: float, way: float) -> float:
	var period: float = 2.0 * (a + b)
	var f: float = fposmod(frame, period)
	var side: float = way
	if f >= a + b:
		f -= a + b
		side = -way
	if f < a:
		var t: float = f / a
		return side * SWAY * (1.0 - (1.0 - t) * (1.0 - t))
	var u: float = (f - a) / b
	return side * SWAY * (1.0 - u * u)
