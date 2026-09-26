class_name BossMagicCircle
extends Sprite2D

## The spinning hexagram circle under a Level 4 boss, LoLK's eff_magicsquare.png (effect.anm
## sprite 32), drawn additive at alpha 128/255 as effect.anm script 107 draws it.
##
## - Entrance (60f, from script 107): scale 0 -> 1 decelerating, while the disc turns over from
##   a (-45, -45) degree tilt and makes a full turn as it opens.
## - Then it spins, at half script 107's -0.75 degrees a frame (the full rate read as too fast).
## - It leans toward where the boss is flying and settles flat again once she stops. LoLK does
##   this in the engine, not the anm script, so the lean amounts are tuned by eye.
##   Script 107's size and alpha pulse is left out: LoLK doesn't show it.
##
## The 3D tilt is drawn flat: a tilted plane seen straight on is an affine transform, so the
## sprite stays one ordinary quad and batches like anything else.

const FRAME: float = 1.0 / 60.0
const ENTRANCE: float = 60.0 * FRAME
const SPIN_SPEED: float = -0.01308997 / FRAME * 0.5
const TILT_START: Vector3 = Vector3(-45.0, -45.0, 180.0)
const TILT_END: Vector3 = Vector3(0.0, 0.0, -180.0)
## effect.anm script 108 plays the same sprite shrinking to nothing over 20 frames.
const VANISH: float = 20.0 * FRAME
const ALPHA: float = 128.0 / 255.0

## Degrees of lean per pixel a second the boss moves; a hop peaks around 300 px/s.
const LEAN_PER_SPEED: float = 0.15
const MAX_LEAN: float = 45.0
## How quickly the lean catches up with the boss's movement (and settles once she stops).
const LEAN_RATE: float = 6.0

## LoLK draws the 256px circle on a 384-wide playfield; ours is 600 wide.
@export var base_scale: float = 600.0 / 384.0

var _time: float = 0.0
var _spin: float = 0.0
var _vanish_time: float = -1.0
## Degrees about the circle's vertical axis (from moving sideways) and horizontal axis
## (from moving up or down).
var _lean: Vector2 = Vector2.ZERO
var _last_pos: Vector2 = Vector2.ZERO


func _ready() -> void:
	texture = preload("res://resources/dat_textures/boss_magic_circle.tres")
	var additive := CanvasItemMaterial.new()
	additive.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD
	material = additive
	modulate.a = ALPHA
	_last_pos = global_position
	_apply()


## Shrinks the circle away; the boss calls this when it is beaten, leaves or is dispelled.
func vanish() -> void:
	if _vanish_time < 0.0:
		_vanish_time = 0.0


func _process(delta: float) -> void:
	_time += delta
	if _time > ENTRANCE:
		_spin += SPIN_SPEED * delta
	if delta > 0.0:
		var velocity := (global_position - _last_pos) / delta
		var target := (velocity * LEAN_PER_SPEED).limit_length(MAX_LEAN)
		if _time < ENTRANCE:
			target = Vector2.ZERO
		_lean = _lean.lerp(target, 1.0 - exp(-LEAN_RATE * delta))
	_last_pos = global_position
	if _vanish_time >= 0.0:
		_vanish_time += delta
		if _vanish_time >= VANISH:
			visible = false
			set_process(false)
			return
	_apply()


func _apply() -> void:
	var t := _decelerate(_time / ENTRANCE)
	var tilt := TILT_START.lerp(TILT_END, t)
	var size := t
	if _vanish_time >= 0.0:
		size *= 1.0 - _decelerate(_vanish_time / VANISH)

	var turn := Basis(Vector3.UP, deg_to_rad(tilt.y + _lean.x)) \
		* Basis(Vector3.RIGHT, deg_to_rad(tilt.x - _lean.y)) \
		* Basis(Vector3.BACK, deg_to_rad(tilt.z) + _spin)
	var s := size * base_scale
	transform = Transform2D(Vector2(turn.x.x, turn.x.y) * s, Vector2(turn.y.x, turn.y.y) * s, position)


## anm interpolation mode 4.
static func _decelerate(t: float) -> float:
	t = clampf(t, 0.0, 1.0)
	return 1.0 - (1.0 - t) * (1.0 - t)
