class_name LyricaExtraNote
extends Node2D

## Lyrica's Extra Attack: a red music note forms on the opponent's field and bursts into a
## ring of notes that race outward.
## The note is pl06.anm script 28 (drawn with the double note, see BulletSprites): squashed
## flat at 2x width, opening to full size over 20 frames (DecelerateSlow) while fading in
## from 0x40 over 10, spinning 0.1047 rad/frame; its interrupt shrinks it away over 20
## frames. It is harmless.
## Measured from lyrica_extra.mp4: the ring appears ~28 frames after the note, about 40 px
## out and at rest, then accelerates at ~200 px/s^2 (radius 45, 75, 120, 196 px at 0.3s
## steps), all together, so the ring stays a perfect circle as it crosses the field. The
## notes spin, each from its own angle, so no two face the same way.

const NOTE_DATA: DanmakuBulletData = preload("res://resources/bullets/lyrica_note_red.tres")

const FORM_DURATION: float = 20.0 / 60.0
const FADE_IN_DURATION: float = 10.0 / 60.0
const START_ALPHA: float = 64.0 / 255.0
const SHRINK_DURATION: float = 20.0 / 60.0
const SPIN_SPEED: float = 0.10471976 * 60.0

@export var ring_delay: float = 28.0 / 60.0
@export var ring_count: int = 20
@export var ring_radius: float = 40.0
## Every note of the ring accelerates outward at this rate, px/s^2.
@export var accel: float = 200.0

var _playfield: Node2D = null
var _rng: RandomNumberGenerator = null
var _sprite: Sprite2D = null
var _age: float = 0.0
var _burst: bool = false

## The ring's turn is part of where the notes go, so both clients seed it from the synced
## landing spot (see Playfield.spawn_extra_attack).
func setup_variation(seed_value: int) -> void:
	_rng = RandomNumberGenerator.new()
	_rng.seed = seed_value

func _ready() -> void:
	# Extra Attacks are instanced straight into the target field's bullet layer.
	var node: Node = get_parent()
	while node != null and not (node is Playfield):
		node = node.get_parent()
	_playfield = node as Node2D
	if _rng == null:
		_rng = RandomNumberGenerator.new()
		_rng.randomize()

	_sprite = Sprite2D.new()
	_sprite.texture = NOTE_DATA.texture
	_sprite.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	add_child(_sprite)
	var base_scale: Vector2 = NOTE_DATA.base_scale
	_sprite.scale = Vector2(base_scale.x * 2.0, 0.0)
	_sprite.modulate.a = START_ALPHA
	var tween := create_tween().set_parallel(true)
	tween.tween_property(_sprite, "scale", base_scale, FORM_DURATION).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	tween.tween_property(_sprite, "modulate:a", 1.0, FADE_IN_DURATION)

func _physics_process(delta: float) -> void:
	_sprite.rotation += SPIN_SPEED * delta
	_age += delta
	if _burst or _age < ring_delay:
		return
	_burst = true
	_fire_ring()
	var tween := create_tween()
	tween.tween_property(_sprite, "scale", Vector2.ZERO, SHRINK_DURATION).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	tween.tween_callback(queue_free)

func _fire_ring() -> void:
	if not is_instance_valid(_playfield) or not _playfield.has_method("spawn_danmaku_bullet"):
		return
	var turn: float = _rng.randf() * TAU
	var n: int = maxi(ring_count, 1)
	for i in n:
		var dir := Vector2.from_angle(turn + TAU * float(i) / float(n))
		var note: DanmakuBullet = _playfield.spawn_danmaku_bullet(NOTE_DATA, position + dir * ring_radius, DanmakuBullet.MotionMode.LINEAR, dir, 0.0, accel)
		if note and note.sprite:
			note.sprite.rotation = _rng.randf() * TAU
