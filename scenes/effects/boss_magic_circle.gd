class_name BossMagicCircle
extends Sprite2D

## The spinning hexagram circle under a Level 4 boss, LoLK's eff_magicsquare.png (effect.anm
## sprite 32) played the way effect.anm script 107 plays it. Everything below is that script
## at 60 frames a second:
##
## - Entrance (60f): scale 0 -> 1 decelerating, while the 3D tilt swings from (-45, -45, 180)
##   degrees to (20, 30, -180), so the disc turns over and makes a full turn as it opens.
## - Then it spins at -0.75 degrees a frame, and pulses: scale 1.0 -> 0.8 -> 1.0 with alpha
##   128 -> 128 -> 96, 60 frames each way (smoothed), looping.
## - Drawn additive at alpha 128/255.
##
## The 3D tilt is drawn flat: a tilted plane seen straight on is an affine transform, so the
## sprite stays one ordinary quad and batches like anything else.

const FRAME: float = 1.0 / 60.0
const ENTRANCE: float = 60.0 * FRAME
const PULSE_HALF: float = 60.0 * FRAME
const SPIN_SPEED: float = -0.01308997 / FRAME
const TILT_START: Vector3 = Vector3(-45.0, -45.0, 180.0)
const TILT_END: Vector3 = Vector3(20.0, 30.0, -180.0)
## effect.anm script 108 plays the same sprite shrinking to nothing over 20 frames.
const VANISH: float = 20.0 * FRAME

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
	var tilt: Vector3
	var size: float
	var alpha: float = 128.0
	if _time < ENTRANCE:
		var t := _decelerate(_time / ENTRANCE)
		tilt = TILT_START.lerp(TILT_END, t)
		size = t
	else:
		tilt = TILT_END
		# One pulse cycle is two 60-frame legs: shrinking to 0.8 (alpha stays 128), then
		# growing back to 1.0 while alpha eases to 96. From the second cycle on, the first leg
		# brings alpha back up from 96 to 128.
		var cycle := fmod(_time - ENTRANCE, PULSE_HALF * 2.0)
		var looped := _time - ENTRANCE >= PULSE_HALF * 2.0
		if cycle < PULSE_HALF:
			var t := _smooth(cycle / PULSE_HALF)
			size = lerpf(1.0, 0.8, t)
			alpha = lerpf(96.0, 128.0, t) if looped else 128.0
		else:
			var t := _smooth((cycle - PULSE_HALF) / PULSE_HALF)
			size = lerpf(0.8, 1.0, t)
			alpha = lerpf(128.0, 96.0, t)
	if _vanish_time >= 0.0:
		size *= 1.0 - _decelerate(_vanish_time / VANISH)

	var turn := Basis(Vector3.UP, deg_to_rad(tilt.y)) * Basis(Vector3.RIGHT, deg_to_rad(tilt.x)) \
		* Basis(Vector3.BACK, deg_to_rad(tilt.z) + _spin)
	var s := size * base_scale
	transform = Transform2D(Vector2(turn.x.x, turn.x.y) * s, Vector2(turn.y.x, turn.y.y) * s, position)
	modulate.a = alpha / 255.0


## anm interpolation mode 4.
static func _decelerate(t: float) -> float:
	t = clampf(t, 0.0, 1.0)
	return 1.0 - (1.0 - t) * (1.0 - t)


## anm interpolation mode 9: slow at both ends.
static func _smooth(t: float) -> float:
	return 0.5 - 0.5 * cos(PI * clampf(t, 0.0, 1.0))
