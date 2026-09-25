class_name YuukaExtraFlower
extends Area2D

## Yuuka's Extra Attack: one sunflower dropped onto the opponent's field.
## Forms in place with pl09.anm script 11/12 (squashed flat at 2x width, opening to
## full size over 20 frames while fading in over 10, spinning 0.1047 rad/frame), holds
## still while it forms, then falls straight down at a speed rolled per flower.
## Measured from yuuka_extra.mp4: four flowers fell at 85, 104, 205 and 236 px/s,
## each holding ~13 frames before moving and reaching top speed within a few frames.
## The rolled range is widened past those on purpose: at 80-240 flowers read as same-y in play.

const FORM_DURATION: float = 20.0 / 60.0
const FADE_IN_DURATION: float = 10.0 / 60.0
const CLOSE_DURATION: float = 20.0 / 60.0
const HOLD_DURATION: float = 13.0 / 60.0
const RAMP_DURATION: float = 0.1
const SPIN_SPEED: float = 0.10471976 * 60.0
const MIN_FALL_SPEED: float = 60.0
const MAX_FALL_SPEED: float = 320.0
const DESPAWN_Y: float = 1060.0
const DAMAGE: float = 1.0

## Silent by default: Yuuka's se_exattack plays as the light mote sets off
## (CharacterData.extra_attack_launch_sfx), and the flower makes no sound of its own.
@export var spawn_sfx: String = ""

## Read by the bot's hazard prediction, same as the other Extra Attacks.
var _velocity: Vector2 = Vector2.ZERO
var _fall_speed: float = 0.0
var _spin: float = SPIN_SPEED
var _age: float = 0.0
var _closing: bool = false
var _rng: RandomNumberGenerator = null

@onready var sprite: Sprite2D = $Sprite2D

## Fall speed and spin direction are part of where this flower will be, so both
## clients seed them from the synced landing spot (see Playfield.spawn_extra_attack).
func setup_variation(seed_value: int) -> void:
	_rng = RandomNumberGenerator.new()
	_rng.seed = seed_value

func _ready() -> void:
	collision_layer = 2 # enemy_bullets
	collision_mask = 1  # player
	monitoring = false # Harmless while it is still forming.
	body_entered.connect(_on_body_entered)

	if _rng == null:
		_rng = RandomNumberGenerator.new()
		_rng.randomize()
	_fall_speed = _rng.randf_range(MIN_FALL_SPEED, MAX_FALL_SPEED)
	# Script 11 spins clockwise, script 12 counter-clockwise.
	if _rng.randf() < 0.5:
		_spin = -SPIN_SPEED

	if not spawn_sfx.is_empty():
		AudioService.play_sfx(spawn_sfx)

	var base_scale: Vector2 = sprite.scale
	sprite.scale = Vector2(base_scale.x * 2.0, 0.0)
	sprite.modulate.a = 0.25
	var tween := create_tween().set_parallel(true)
	tween.tween_property(sprite, "scale", base_scale, FORM_DURATION).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	tween.tween_property(sprite, "modulate:a", 1.0, FADE_IN_DURATION)

func _physics_process(delta: float) -> void:
	sprite.rotation += _spin * delta
	if _closing:
		return

	_age += delta
	if _age < HOLD_DURATION:
		return
	if not monitoring:
		monitoring = true

	var ramp: float = clampf((_age - HOLD_DURATION) / RAMP_DURATION, 0.0, 1.0)
	_velocity = Vector2(0.0, _fall_speed * ramp)
	position += _velocity * delta

	if position.y >= DESPAWN_Y:
		queue_free()

func _on_body_entered(body: Node2D) -> void:
	if _closing:
		return
	if body.has_method("take_damage"):
		body.take_damage(DAMAGE, self)
	elif body.has_method("hit"):
		body.hit()
	else:
		return
	_close()

func take_damage(_amount: float, source: String = "bullet") -> bool:
	if _closing:
		return false
	if source == "heavy_shockwave" or source == "charge_attack":
		_close()
		return true
	return false

## pl09.anm interrupt[1]: the flower folds shut over 20 frames, then is deleted.
func _close() -> void:
	_closing = true
	_velocity = Vector2.ZERO
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	var tween := create_tween()
	tween.tween_property(sprite, "scale", Vector2.ZERO, CLOSE_DURATION).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	tween.tween_callback(queue_free)
