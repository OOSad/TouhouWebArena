class_name ClownpieceExtraLasers
extends Node2D

## Clownpiece's Extra Attack: the red lasers of her third non-spell (TH15 `st05bs.ecl` Boss3,
## 24:19 in the reference footage), chosen by the user.
##
## A fan of red sparkle bursts flies out of the landing point, slows and hangs in an arc. Each then
## draws its warning line at where the player is at that moment, and fires a red laser along
## it. The aim is fixed when the warning appears, so the lasers converge on the player's last
## known position: moving once the lines show is the dodge.
##
## ECL (`Boss3_at`): a fan of 4 type 19 orbs, spread 80 degrees (narrowing 10 each volley),
## 4 px/frame (slowing 0.4 each volley), each turning into a laser (sprite 38 colour 2, 16
## units wide, 640 long) aimed at the player: 30 frames of warning, 30 of firing. 3 volleys
## on Easy, 6 from Normal, 20 frames apart. Here one volley, as an Extra Attack arrives
## often, and it fans downward: it lands near the top of the field, where LoLK's upward fan
## would leave the screen.

## Orbs at Rank 1 and Rank 16 (TH15: 4). More lasers leave less room in the crossfire.
@export var orbs_min_rank: int = 3
@export var orbs_max_rank: int = 5
## Spread of the fan, centred straight down.
@export var fan_deg: float = 140.0
## 4 px/frame, braking to a stop over `orb_flight`.
@export var orb_speed: float = 514.29
@export var orb_flight: float = 0.5
## Each orb is PoFV's red sparkle burst blooming as on Sakuya's dagger clusters
## (`KnifeExplosionBurst`): it flies out large and shrinks to rest over `orb_bloom`. Hers
## goes 9.9x to 4.5x; these keep the ratio at a smaller size, as up to five sit ~80 px apart.
@export var orb_start_scale: float = 6.6
@export var orb_rest_scale: float = 3.0
@export var orb_bloom: float = 0.6
@export var orb_fade_in: float = 0.15
## Spin of each orb, radians a second. Set by eye (about a turn a second, as Marisa's spinning
## stars): PoFV's own sparkle scripts never rotate it, and LoLK's orb spins in the footage.
@export var orb_spin_speed: float = 6.0
## TH15: 30 frames of warning, 30 of firing.
@export var warning_time: float = 0.5
@export var fire_time: float = 0.5
@export var laser_damage: float = 1.0
@export var laser_scene: PackedScene = preload("res://scenes/attacks/earth_light_ray_red.tscn")
@export var spawn_sfx: String = "se_exattack"

const ORB_TEXTURE: Texture2D = preload("res://resources/bullets/textures/tex_knife_explosion_burst.tres")
## Earth Light Ray's beam spans local y -60 to 1000 along its axis.
const BEAM_TAIL: float = 60.0
const ORB_FADE_OUT: float = 0.25

var _orb_count: int = 3
var _orbs: Array[Sprite2D] = []
var _dirs: Array[Vector2] = []
var _age: float = 0.0
var _fired: bool = false
var _lasers: Array = []
var _fade: float = 0.0
var _playfield: Node2D = null

func setup_rank(rank: int) -> void:
	var t: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	_orb_count = roundi(lerpf(float(orbs_min_rank), float(orbs_max_rank), t))

func _ready() -> void:
	# Extra Attacks are instanced straight into the target field's bullet layer.
	var node: Node = get_parent()
	while node != null and not (node is Playfield):
		node = node.get_parent()
	_playfield = node as Node2D
	var fan: float = deg_to_rad(fan_deg)
	for i in _orb_count:
		var t: float = 0.5 if _orb_count == 1 else float(i) / float(_orb_count - 1)
		_dirs.append(Vector2.from_angle(PI / 2.0 - fan * 0.5 + fan * t))
		var orb := Sprite2D.new()
		orb.texture = ORB_TEXTURE
		orb.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
		# Above the bullets, as SpawnFlash and KnifeExplosionBurst draw.
		orb.z_index = 11
		orb.z_as_relative = false
		orb.scale = Vector2(orb_start_scale, orb_start_scale)
		orb.modulate.a = 0.0
		add_child(orb)
		_orbs.append(orb)
	if not spawn_sfx.is_empty():
		AudioService.play_sfx(spawn_sfx)

func _physics_process(delta: float) -> void:
	_age += delta
	# Braking uniformly: distance = v * (t - t^2 / (2 * flight)), capped at the stop.
	var t: float = minf(_age, orb_flight)
	var travelled: float = orb_speed * (t - t * t / (2.0 * orb_flight))
	var bloom: float = ease(clampf(_age / orb_bloom, 0.0, 1.0), 0.5)
	var orb_scale: float = lerpf(orb_start_scale, orb_rest_scale, bloom)
	for i in _orbs.size():
		_orbs[i].position = _dirs[i] * travelled
		_orbs[i].scale = Vector2(orb_scale, orb_scale)
		_orbs[i].modulate.a = clampf(_age / orb_fade_in, 0.0, 1.0)
		_orbs[i].rotation += orb_spin_speed * delta
	if not _fired and _age >= orb_flight:
		_fired = true
		_fire_lasers()
	# The orbs cover the start of their lasers, so they stay until every laser is gone
	# (collapse included), then fade, and the attack is done.
	if _fired and not _any_laser_left():
		_fade += delta
		modulate.a = maxf(0.0, 1.0 - _fade / ORB_FADE_OUT)
		if modulate.a <= 0.0:
			queue_free()

## Freed lasers can't be handed to a typed check, so each is tested with is_instance_valid.
func _any_laser_left() -> bool:
	for laser in _lasers:
		if is_instance_valid(laser):
			return true
	return false


## Every orb aims at where the player is right now; the lasers keep that aim.
func _fire_lasers() -> void:
	var layer: Node = get_parent()
	var victim: Node2D = _playfield.get("player") if is_instance_valid(_playfield) else null
	for orb in _orbs:
		var from: Vector2 = position + orb.position
		var aim: float = PI / 2.0
		if victim and is_instance_valid(victim) and victim.position != from:
			aim = (victim.position - from).angle()
		var laser: EarthLightRay = laser_scene.instantiate() as EarthLightRay
		if laser == null or layer == null:
			return
		# The beam runs along its local +y, so it is turned a quarter less than the aim.
		laser.rotation = aim - PI / 2.0
		# Earth Light Ray's beam starts BEAM_TAIL px behind its origin (Marisa's starts above the
		# screen), so the origin is moved on by that much: the beam starts at the orb's centre.
		laser.position = from + Vector2.from_angle(aim) * BEAM_TAIL
		laser.telegraph_duration = warning_time
		laser.fire_duration = fire_time
		laser.damage = laser_damage
		layer.add_child(laser)
		_lasers.append(laser)
