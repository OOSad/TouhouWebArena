class_name BossMagicCircle
extends Sprite2D

## The spinning hexagram circle under a Level 4 boss, LoLK's eff_magicsquare.png (effect.anm
## sprite 32), drawn additive at alpha 128/255 as effect.anm script 107 draws it.
##
## - Entrance (60f): scale 0 -> 1 decelerating, while the rotation swings from (-45, -45, 180)
##   degrees to (20, 30, -180), so the disc turns over and makes a full turn as it opens.
## - Then the z angle keeps turning, at half script 107's -0.75 degrees a frame (the full rate
##   read as too fast).
## - Script 107 sets rotation order 3 (ins 437): y, then z, then x. With the spin sitting
##   between the two tilts, the disc's lean changes as it turns: nearly flat at one point of
##   the turn, about 50 degrees at the opposite point. That wobble is the whole look.
##   Script 107's size and alpha pulse is left out: LoLK doesn't show it.
##
## The 3D rotation is drawn flat: a tilted plane seen straight on is an affine transform, so
## the sprite stays one ordinary quad and batches like anything else.

const FRAME: float = 1.0 / 60.0
const ENTRANCE: float = 60.0 * FRAME
const SPIN_SPEED: float = -0.01308997 / FRAME * 0.5
const TILT_START: Vector3 = Vector3(-45.0, -45.0, 180.0)
const TILT_END: Vector3 = Vector3(20.0, 30.0, -180.0)
## effect.anm script 108 plays the same sprite shrinking to nothing over 20 frames.
const VANISH: float = 20.0 * FRAME
const ALPHA: float = 128.0 / 255.0

## LoLK draws the 256px circle on a 384-wide playfield; ours is 600 wide.
@export var base_scale: float = 600.0 / 384.0

var _time: float = 0.0
var _spin: float = 0.0
var _vanish_time: float = -1.0


func _ready() -> void:
	texture = preload("res://resources/dat_textures/boss_magic_circle.tres")
	var additive := CanvasItemMaterial.new()
	additive.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD
	material = additive
	modulate.a = ALPHA
	_apply()


## Shrinks the circle away; the boss calls this when it is beaten, leaves or is dispelled.
func vanish() -> void:
	if _vanish_time < 0.0:
		_vanish_time = 0.0


func _process(delta: float) -> void:
	_time += delta
	if _time > ENTRANCE:
		_spin += SPIN_SPEED * delta
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

	# Order 3: y applied first, then z, then x.
	var turn := Basis(Vector3.RIGHT, deg_to_rad(tilt.x)) \
		* Basis(Vector3.BACK, deg_to_rad(tilt.z) + _spin) \
		* Basis(Vector3.UP, deg_to_rad(tilt.y))
	var s := size * base_scale
	transform = Transform2D(Vector2(turn.x.x, turn.x.y) * s, Vector2(turn.y.x, turn.y.y) * s, position)


## anm interpolation mode 4.
static func _decelerate(t: float) -> float:
	t = clampf(t, 0.0, 1.0)
	return 1.0 - (1.0 - t) * (1.0 - t)
