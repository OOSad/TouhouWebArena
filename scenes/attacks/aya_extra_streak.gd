class_name AyaExtraStreak
extends Area2D

## Aya's Extra Attack: one red streak dropped onto the opponent's field.
## Forms in place with pl10.anm scripts 9 and 10 (stretched to 2x length and zero width,
## settling to full size over 20 frames; the body fades 0x80 -> 0xff over 5 frames, an
## additive copy 0 -> 0x80 over 20), then dives at where the player is at that moment,
## accelerating from rest. Measured from extra_attack.mp4: ~24 frames between appearing
## and moving, a near-straight line onto the player, ~850 px/s^2 (about 900 px/s by the
## time it reaches the bottom of the field).

const FORM_DURATION: float = 20.0 / 60.0
const BODY_FADE_DURATION: float = 5.0 / 60.0
const HOLD_DURATION: float = 24.0 / 60.0
const CLOSE_DURATION: float = 20.0 / 60.0
const ACCELERATION: float = 850.0
const MAX_SPEED: float = 1100.0
const DESPAWN_MARGIN: float = 120.0
const DAMAGE: float = 1.0

## Read by the bot's hazard prediction, same as the other Extra Attacks.
var _velocity: Vector2 = Vector2.ZERO
var _direction: Vector2 = Vector2.DOWN
var _speed: float = 0.0
var _age: float = 0.0
var _launched: bool = false
var _closing: bool = false
var _playfield: Node2D = null

@onready var visual: Node2D = $Visual
@onready var body_sprite: Sprite2D = $Visual/Body
@onready var glow_sprite: Sprite2D = $Visual/Glow

func _ready() -> void:
	collision_layer = 2 # enemy_bullets
	collision_mask = 1  # player
	monitoring = false # Harmless while it is still forming.
	body_entered.connect(_on_body_entered)

	# Extra Attacks are instanced straight into the target field's bullet layer.
	var node: Node = get_parent()
	while node != null and not (node is Playfield):
		node = node.get_parent()
	_playfield = node as Node2D

	# The art lies along the visual's y axis: script 9's x scale is its length.
	visual.scale = Vector2(0.0, 2.0)
	body_sprite.modulate.a = 128.0 / 255.0
	glow_sprite.modulate.a = 0.0
	var tween := create_tween().set_parallel(true)
	tween.tween_property(visual, "scale", Vector2.ONE, FORM_DURATION).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	tween.tween_property(body_sprite, "modulate:a", 1.0, BODY_FADE_DURATION)
	tween.tween_property(glow_sprite, "modulate:a", 128.0 / 255.0, FORM_DURATION)

func _physics_process(delta: float) -> void:
	if _closing:
		return
	_age += delta
	if _age < HOLD_DURATION:
		return

	if not _launched:
		_launched = true
		monitoring = true
		var victim: Node2D = _playfield.get("player") if is_instance_valid(_playfield) else null
		if victim and is_instance_valid(victim):
			var to_victim: Vector2 = victim.position - position
			if to_victim.length_squared() > 1.0:
				_direction = to_victim.normalized()
		rotation = _direction.angle() - PI * 0.5

	_speed = minf(_speed + ACCELERATION * delta, MAX_SPEED)
	_velocity = _direction * _speed
	position += _velocity * delta

	var width: float = Playfield.PLAYFIELD_WIDTH
	if position.y > 1040.0 + DESPAWN_MARGIN or position.x < -DESPAWN_MARGIN or position.x > width + DESPAWN_MARGIN:
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

## pl10.anm interrupt[1]: shrinks to nothing over 20 frames, then is deleted.
func _close() -> void:
	_closing = true
	_velocity = Vector2.ZERO
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	var tween := create_tween()
	tween.tween_property(visual, "scale", Vector2.ZERO, CLOSE_DURATION).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	tween.tween_callback(queue_free)
