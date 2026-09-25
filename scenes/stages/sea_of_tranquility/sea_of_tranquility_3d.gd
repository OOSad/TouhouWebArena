class_name SeaOfTranquility3D
extends Node3D

## Clownpiece's home stage: the Sea of Tranquility, LoLK's Stage 5, on the surface of the moon.
##
## Read out of LoLK's own st05.std (th15.dat), at 64 LoLK units to one world unit as the
## PoFV stages use:
##   - one object: the cratered ground (stage05a.png), 1024 units wide, laid in 512-unit
##     tiles along the path
##   - two sky sprites on a 2D layer behind it (st05wl.anm scripts 1-4): the Earth
##     (stage05b.png) and a glow over it (stage05c.png)
##   - the script: camera, facing, fog and their timings, below
##
## Forward travel slides the ground towards the camera, wrapped to one tile, as Garden of
## the Sun does. The script ends in LoLK's boss loop (7100-7300), which holds forever.

const UNIT: float = 1.0 / 64.0
const TILE: float = 512.0
## How far ahead of the camera the ground reaches, LoLK units (see _build_ground).
const GROUND_AHEAD: float = 1700.0

## Camera script, in 60 fps frames and LoLK units: position is (along the path, height),
## facing is (ahead, down). ins_7 sets a 30 degree field of view.
##   t=0     at (0, 800), facing (400, 300); over 790f, linear, to (512, 550)
##   t=790   along 512 every 790f at 550, to t=3950 (ZUN loops 0 -> 512)
##   t=3950  over 850f, mode 2, to (1024, 250); facing over 850f, linear, to (400, 100)
##   t=4800  from there, linear, 512 units every 200f, forever (ZUN's boss loop)
## std mode 2 is read as easing in (accelerating), so the slow high cruise speeds up into
## the fast low one it hands over to; the handover speed matches within 6%.
const DESCEND_AT: float = 3950.0
const DESCEND_FRAMES: float = 850.0
const CRUISE_AT: float = DESCEND_AT + DESCEND_FRAMES
const HIGH_SPEED: float = 512.0 / 790.0
const LOW_SPEED: float = 512.0 / 200.0

## Fog, colour and near / far in LoLK units: a dark purple #200010 at 600-600 turning over
## 790f to dark red #400000 at 800-1300; from t=3160 over 790f to black at the same range;
## over the descent to black at 200-800; from t=5300 over 260f back out to 800-1300.
const FOG_PURPLE: Color = Color8(0x20, 0x00, 0x10)
const FOG_RED: Color = Color8(0x40, 0x00, 0x00)
const FOG_BLACK: Color = Color8(0x00, 0x00, 0x00)

## The sky sprites (384x160 each) on LoLK's 2D layer 3, top-left anchored at (-10, -20),
## turned -10 degrees. LoLK's field is 384 wide and ours is narrower in proportion, so x is
## centred. LoLK slides them in at alpha 64 over the first 800f (scripts 1 and 3) and fades
## them to full over 60f at t=5300 (scripts 2 and 4). Here they stay hidden until t=5300,
## per the user: the dim sky showed during the descent, where the ground's far edge sweeps up
## across the Earth as the camera tilts; by t=5300 the camera has settled with the edge well
## below it, so the Earth only ever appears whole.
const SKY_SCALE: float = 448.0 / 960.0
const SKY_AT: Vector2 = Vector2(-10.0, -20.0)
## LoLK leaves the top half of the Earth above its field (it sits ~54 px into its sprite, placed
## 20 units above the top edge and turned -10 degrees). Lowered 40 units so it shows whole,
## per the user.
const SKY_DROP: float = 40.0
const SKY_BRIGHT_AT: float = 5300.0
const SKY_BRIGHT_FRAMES: float = 60.0

## Kept for the Playfield, which staggers every stage's idle sway with it.
@export var time_offset: float = 0.0

const GROUND_TEX: Texture2D = preload("res://resources/dat_textures/stage_sea_ground.tres")
const SKY_TEX: Texture2D = preload("res://resources/dat_textures/stage_sea_sky.tres")
const SKY_GLOW_TEX: Texture2D = preload("res://resources/dat_textures/stage_sea_sky_glow.tres")
const GROUND_SHADER: Shader = preload("res://scenes/stages/sea_of_tranquility/sea_of_tranquility_ground.gdshader")
const ADDITIVE: CanvasItemMaterial = preload("res://scenes/effects/spell_background_additive.tres")

@onready var camera: Camera3D = get_node_or_null("Camera3D")
@onready var world_env: WorldEnvironment = get_node_or_null("WorldEnvironment")

var _ground_mat: ShaderMaterial
var _sky: Array[Sprite2D] = []
var _sky_back: ColorRect = null
var _time: float = 0.0


func _ready() -> void:
	if camera and world_env and world_env.environment:
		camera.environment = world_env.environment
	_build_ground()
	_build_sky()
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
	# The ground's far edge is where the sky starts (past the fog it is drawn in fog colour and
	# hides the sky). GROUND_AHEAD is long enough to fill the screen while the camera is high
	# and looking down, hiding the Earth until the descent as LoLK does, and short enough that
	# the tilted-up boss-loop camera sees the whole Earth above it.
	mesh.size = Vector2(40.0, GROUND_AHEAD * UNIT + 2.0)
	var ground := MeshInstance3D.new()
	ground.name = "Ground"
	ground.mesh = mesh
	ground.position = Vector3(0.0, 0.0, 1.0 - mesh.size.y * 0.5)
	ground.material_override = _ground_mat
	ground.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(ground)

## The sky draws on a canvas layer the environment shows as its background, behind the 3D.
func _build_sky() -> void:
	var layer := CanvasLayer.new()
	layer.layer = -1
	add_child(layer)
	# An opaque backdrop in the fog colour: the sky sprites are drawn at quarter alpha for most of
	# the round, and without it what shows through them was the viewport's default grey.
	_sky_back = ColorRect.new()
	_sky_back.size = Vector2(600.0, 960.0)
	_sky_back.mouse_filter = Control.MOUSE_FILTER_IGNORE
	layer.add_child(_sky_back)
	for tex in [SKY_TEX, SKY_GLOW_TEX]:
		var sprite := Sprite2D.new()
		sprite.texture = tex
		sprite.centered = false
		sprite.rotation = deg_to_rad(-10.0)
		sprite.scale = Vector2.ONE / SKY_SCALE
		if tex == SKY_GLOW_TEX:
			sprite.material = ADDITIVE
		layer.add_child(sprite)
		_sky.append(sprite)

func _update_stage() -> void:
	var frame: float = _time * 60.0
	var cam := _camera_at(frame)
	var slide: float = fposmod(cam.x, TILE) * UNIT
	var fog := _fog_at(frame)
	if _ground_mat:
		_ground_mat.set_shader_parameter("slide", slide)
		_ground_mat.set_shader_parameter("fog_color", fog[0])
		_ground_mat.set_shader_parameter("fog_start", fog[1].x * UNIT)
		_ground_mat.set_shader_parameter("fog_end", fog[1].y * UNIT)
	if world_env and world_env.environment:
		world_env.environment.background_color = fog[0]
	if _sky_back:
		_sky_back.color = fog[0]
	if camera:
		camera.position = Vector3(0.0, cam.y * UNIT, 0.0)
		camera.rotation_degrees = Vector3(-rad_to_deg(atan2(cam.z, cam.w)), 0.0, 0.0)
	_update_sky(frame)

## (along the path, height, facing down, facing ahead) at a given frame
func _camera_at(frame: float) -> Vector4:
	if frame < 790.0:
		var t: float = frame / 790.0
		return Vector4(512.0 * t, lerpf(800.0, 550.0, t), 300.0, 400.0)
	if frame < DESCEND_AT:
		return Vector4(HIGH_SPEED * frame, 550.0, 300.0, 400.0)
	if frame < CRUISE_AT:
		var t: float = (frame - DESCEND_AT) / DESCEND_FRAMES
		var e: float = t * t
		return Vector4(1024.0 * e, lerpf(550.0, 250.0, e), lerpf(300.0, 100.0, t), 400.0)
	return Vector4(1024.0 + LOW_SPEED * (frame - CRUISE_AT), 250.0, 100.0, 400.0)

## [colour, Vector2(near, far)] at a given frame
func _fog_at(frame: float) -> Array:
	if frame < 790.0:
		var t: float = frame / 790.0
		return [FOG_PURPLE.lerp(FOG_RED, t), Vector2(600.0, 600.0).lerp(Vector2(800.0, 1300.0), t)]
	if frame < 3160.0:
		return [FOG_RED, Vector2(800.0, 1300.0)]
	if frame < DESCEND_AT:
		return [FOG_RED.lerp(FOG_BLACK, (frame - 3160.0) / 790.0), Vector2(800.0, 1300.0)]
	if frame < 5300.0:
		var t: float = clampf((frame - DESCEND_AT) / DESCEND_FRAMES, 0.0, 1.0)
		return [FOG_BLACK, Vector2(800.0, 1300.0).lerp(Vector2(200.0, 800.0), t)]
	var t: float = clampf((frame - 5300.0) / 260.0, 0.0, 1.0)
	return [FOG_BLACK, Vector2(200.0, 800.0).lerp(Vector2(800.0, 1300.0), t)]

func _update_sky(frame: float) -> void:
	var alpha: float = clampf((frame - SKY_BRIGHT_AT) / SKY_BRIGHT_FRAMES, 0.0, 1.0)
	# LoLK's field is 384 wide with x from its left edge; ours shows its middle.
	var screen := Vector2((SKY_AT.x - 192.0) / SKY_SCALE + 300.0, (SKY_AT.y + SKY_DROP) / SKY_SCALE)
	for sprite in _sky:
		sprite.position = screen
		sprite.modulate.a = alpha
