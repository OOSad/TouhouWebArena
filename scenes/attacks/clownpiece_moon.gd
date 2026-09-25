class_name ClownpieceMoon
extends Area2D

## The moon of Clownpiece's "Fake Apollo" (TH15 `st05bs.ecl` BossCard5), cut down from
## three moons orbiting her for the whole card to one sweep, per the user: it fades in at one
## side of her, swings a half circle underneath her to the other side while shedding rings of
## orbs, and fades out there. The fade-out is the card's name: the moon was never real.
##
## Touching the moon hurts, as in TH15 (`ins_501` hitbox 96 units across), but only while
## it is fully there: fading in or out it is harmless.
##
## ECL: `BossCard5_Moon_at2` fires a ring (`ins_607` mode 2, aimed at the player) of type 26
## colour 0 bullets, 18 / 23 / 23 / 26 of them by difficulty, turned 10 degrees off the
## player, at 0.8 / 0.8 / 1.2 / 1.2 px/frame, from 80 units out from the moon's centre. The
## gap between rings eases from 120 frames down to 60 / 30 / 25 / 25 over the card; one swing
## is too short for that, so rank runs from Easy's end gap to Normal's. TH15 units convert by
## field height (960 / 448 = 2.143).

## Drawn size (sprite and hitbox scale with the node); the 128px sprite's disc spans ~120px.
@export var sprite_scale: float = 2.0
## 48 TH15 units.
@export var hitbox_radius: float = 102.86
@export var damage: float = 1.5
## Played with each ring.
@export var ring_sfx: String = "se_tan00"

@onready var sprite: Sprite2D = $Sprite2D
@onready var collision_shape: CollisionShape2D = $CollisionShape2D

## Read by the AI's hazard scan.
var radius: float = 0.0
var velocity: Vector2 = Vector2.ZERO

var _playfield: Node2D = null
var _centre: Vector2 = Vector2.ZERO
var _orbit_radius: float = 0.0
var _from_angle: float = 0.0
var _to_angle: float = 0.0
var _fade_in: float = 0.5
var _swing: float = 4.0
var _fade_out: float = 0.5
var _ring_data: DanmakuBulletData = null
var _ring_count: int = 23
var _ring_interval: float = 0.5
var _ring_speed: float = 102.86
var _ring_offset: float = 171.43
var _ring_turn: float = deg_to_rad(10.0)

var _elapsed: float = 0.0
var _next_ring: float = 0.0

## Starts the moon at `from_angle` round `centre` (radians, 0 = right of it, PI/2 = below)
## and swings it to `to_angle` over `swing` seconds.
func setup(playfield: Node2D, centre: Vector2, orbit_radius: float, from_angle: float, to_angle: float,
		fade_in: float, swing: float, fade_out: float, ring_data: DanmakuBulletData, ring_count: int,
		ring_interval: float, ring_speed: float, ring_offset: float, ring_turn_deg: float) -> void:
	_playfield = playfield
	_centre = centre
	_orbit_radius = orbit_radius
	_from_angle = from_angle
	_to_angle = to_angle
	_fade_in = fade_in
	_swing = swing
	_fade_out = fade_out
	_ring_data = ring_data
	_ring_count = ring_count
	_ring_interval = ring_interval
	_ring_speed = ring_speed
	_ring_offset = ring_offset
	_ring_turn = deg_to_rad(ring_turn_deg)
	position = _orbit_point(_from_angle)

func _ready() -> void:
	collision_layer = 2 # enemy_bullets: the player's hurtbox finds it
	collision_mask = 0
	sprite.scale = Vector2(sprite_scale, sprite_scale)
	var shape := CircleShape2D.new()
	shape.radius = hitbox_radius
	collision_shape.shape = shape
	collision_shape.disabled = true
	radius = hitbox_radius
	modulate.a = 0.0

func _physics_process(delta: float) -> void:
	_elapsed += delta
	var swing_t: float = clampf((_elapsed - _fade_in) / _swing, 0.0, 1.0)
	# Eased at both ends, so it sets off from rest after fading in and settles before fading out.
	var angle: float = lerpf(_from_angle, _to_angle, 0.5 - 0.5 * cos(swing_t * PI))
	var new_pos: Vector2 = _orbit_point(angle)
	velocity = (new_pos - position) / delta if delta > 0.0 else Vector2.ZERO
	position = new_pos

	var solid: bool = _elapsed >= _fade_in and _elapsed <= _fade_in + _swing
	if _elapsed < _fade_in:
		modulate.a = _elapsed / _fade_in
	elif solid:
		modulate.a = 1.0
	else:
		modulate.a = 1.0 - clampf((_elapsed - _fade_in - _swing) / _fade_out, 0.0, 1.0)
	if collision_shape.disabled == solid:
		collision_shape.set_deferred("disabled", not solid)

	if solid and _elapsed - _fade_in >= _next_ring:
		_next_ring += _ring_interval
		_fire_ring()

	if _elapsed >= _fade_in + _swing + _fade_out:
		queue_free()

func _orbit_point(angle: float) -> Vector2:
	return _centre + Vector2.from_angle(angle) * _orbit_radius

## One ring, aimed at the player and turned a little off them, as TH15's `ins_607` mode 2.
func _fire_ring() -> void:
	if _ring_data == null or not is_instance_valid(_playfield) or _ring_count <= 0:
		return
	var start: float = _ring_turn
	var victim: Node2D = _playfield.get("player")
	if victim and is_instance_valid(victim) and victim.position != position:
		start += (victim.position - position).angle()
	var step: float = TAU / float(_ring_count)
	for i in _ring_count:
		var dir := Vector2.from_angle(start + step * float(i))
		_playfield.spawn_danmaku_bullet(_ring_data, position + dir * _ring_offset,
			DanmakuBullet.MotionMode.LINEAR, dir, _ring_speed)
	if not ring_sfx.is_empty():
		AudioService.play_sfx(ring_sfx)
