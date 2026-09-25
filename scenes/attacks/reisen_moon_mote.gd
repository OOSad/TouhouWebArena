class_name ReisenMoonMote
extends Node2D

## The projectile half of Reisen's Triple Moon Shot.
##
## A pale moon that drifts down the field, breathing in and out, carrying a proximity fuse.
## The fuse trips well short of the player: its radius is deliberately larger than the blast
## it leaves behind, so at the instant of detonation the edge of the blast is always clear of
## whoever it was aimed at. Measured off the original, the fuse fires at ~0.30 field-widths
## while the blast tops out at ~0.21, leaving about 0.09 of daylight every single time.
##
## That gap is the whole attack. A mote can never kill you; the crater it leaves can, if you
## fly into it. The three of them together wall off most of the width of the field for about
## a second, which is what makes this area denial rather than a pattern to dodge.
##
## The mote itself has no hitbox. With the fuse tripping at more than twice the mote's own
## radius, contact is unreachable, so a collision shape would only ever be dead weight.

## Set by DanmakuMoonShotStep before the mote enters the tree, or left at these defaults when
## the moon is spawned on its own as Reisen's Extra Attack (see `reisen_extra_moon.tscn`).
var direction: Vector2 = Vector2.DOWN
var speed: float = 250.0
var fuse_radius: float = 181.0
var blast_scene: PackedScene = preload("res://scenes/attacks/reisen_moon_blast.tscn")
var playfield: Node2D = null

## How long the moon takes to resolve: it is born as a wide, near-transparent flare and
## condenses into itself. Only the Extra Attack uses this. The boss's own volley leaves it at
## 0 because its light motes (`ReisenMoonCharge`) already play that beat before the moon
## exists at all, and doing both would resolve the same moon twice.
@export var spawn_in_duration: float = 0.0
## How wide that flare starts, as a multiple of the moon's own size.
const SPAWN_IN_SCALE: float = 2.6

## Played once as the moon resolves. Set only by `reisen_extra_moon.tscn`: an Extra Attack
## landing on the victim's ceiling has to announce itself, which is what `se_exattack` is for
## and what Cirno's stalactite already does. The boss's volley leaves this empty because its
## own step plays the cast sound once for all three moons, rather than three times over.
@export var spawn_sfx: String = ""

## Crater shape, carried through from the step so the whole attack is tuned in one place.
var blast_radius_start: float = 24.0
var blast_radius_end: float = 126.0
var blast_duration: float = 1.05

## Diameter the moon breathes between, as a multiple of the baked sprite's sphere.
const PULSE_MIN: float = 0.50
const PULSE_MAX: float = 1.02
const PULSE_PERIOD: float = 0.30

const DESPAWN_Y: float = 1020.0
const PLAYFIELD_WIDTH: float = 600.0

@onready var sprite: Sprite2D = $Sprite2D

var _age: float = 0.0
var _spent: bool = false

func _ready() -> void:
	# The boss's step hands the moon its playfield. The Extra Attack path does not - it just
	# instantiates the scene into the target field's bullet layer - so find it from there.
	if playfield == null:
		var node: Node = get_parent()
		while node != null and not (node is Playfield):
			node = node.get_parent()
		playfield = node as Node2D

	# _physics_process only runs on the frame after this one, so without seeding the flare
	# here the moon would pop in at full size for a frame before shrinking.
	if spawn_in_duration > 0.0:
		sprite.scale = Vector2(PULSE_MAX * SPAWN_IN_SCALE, PULSE_MAX * SPAWN_IN_SCALE)
		sprite.modulate.a = 0.0

	if not spawn_sfx.is_empty():
		AudioService.play_sfx(spawn_sfx)

func _physics_process(delta: float) -> void:
	if _spent:
		return

	position += direction * speed * delta
	_age += delta

	var phase: float = TAU * _age / PULSE_PERIOD
	var pulse: float = lerpf(PULSE_MIN, PULSE_MAX, 0.5 + 0.5 * sin(phase))
	# Born wide and faint, closing onto itself. Laid over the breathing rather than replacing
	# it, so the moon is already breathing while it resolves, exactly as in the original.
	if spawn_in_duration > 0.0 and _age < spawn_in_duration:
		var t: float = _age / spawn_in_duration
		pulse *= lerpf(SPAWN_IN_SCALE, 1.0, t)
		sprite.modulate.a = t
	elif sprite.modulate.a < 1.0:
		sprite.modulate.a = 1.0
	sprite.scale = Vector2(pulse, pulse)

	# The fuse is the only thing that ever detonates a mote. There is deliberately no timer
	# and no depth limit: confirmed in-game, a moon that never closes on anybody simply falls
	# off the bottom of the screen and is culled, and the same goes for one that drifts out
	# through a side wall. A volley aimed at nothing leaves nothing behind, which is what
	# makes where the boss is standing matter.
	var target: Node2D = playfield.get("player") if playfield and is_instance_valid(playfield) else null
	if target and is_instance_valid(target):
		if position.distance_to(target.position) <= fuse_radius:
			_detonate()
			return

	if position.x < 0.0 or position.x > PLAYFIELD_WIDTH or position.y > DESPAWN_Y:
		queue_free()

func _detonate() -> void:
	if _spent:
		return
	_spent = true

	if blast_scene:
		var blast: Node2D = blast_scene.instantiate()
		blast.position = position
		blast.radius_start = blast_radius_start
		blast.radius_end = blast_radius_end
		blast.duration = blast_duration
		get_parent().add_child(blast)

	queue_free()
