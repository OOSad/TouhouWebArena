class_name ReisenMoonBlast
extends Area2D

## The crater left by a Reisen moon mote, and the only part of Triple Moon Shot that can
## actually kill you.
##
## It detonates in place and never moves. Three of them overlapping wall off most of the
## field for about a second, and the kill comes from flying into one rather than from the
## shot itself.
##
## The visual is `SparkAura`, the same burst Reisen's Level 1 Charge Attack and her Extra
## Attack leave behind, because in the original this is that same explosion. Only the radii
## and duration differ, so they are passed in rather than the effect being reimplemented.
##
## SparkAura deals its own damage to entities on the field, which is what a player charge
## attack wants and the exact opposite of what this wants. That path keys off a playfield
## reference handed to it by `setup()`, so this deliberately never calls `setup()`: without
## it the burst is inert and purely decorative, and the hazard below is the only thing that
## hits anything.

const AURA_SCENE: PackedScene = preload("res://scenes/effects/spark_aura.tscn")

## Radius the crater opens at, and the radius it tops out at. Measured off the reference at
## ~0.04 and ~0.21 field-widths.
@export var radius_start: float = 24.0
@export var radius_end: float = 126.0
@export var duration: float = 1.05
@export var damage: float = 1.5

## Fraction of its life past which the crater stops being lethal. The disc is well into
## breaking up by then, so nothing dies to something it can barely see.
const LETHAL_UNTIL: float = 0.85

@onready var collision_shape: CollisionShape2D = $CollisionShape2D

var _aura: SparkAura = null
var _elapsed: float = 0.0
var _hit_bodies: Array[Node] = []

func _ready() -> void:
	collision_layer = 2 # 2 (enemy_bullets) - hazards the player, ignored by everything else
	collision_mask = 0  # Player's %Hurtbox monitors layer 2 and routes through take_damage with escalation

	var shape := CircleShape2D.new()
	shape.radius = radius_start
	collision_shape.shape = shape

	_aura = AURA_SCENE.instantiate()
	_aura.duration = duration
	_aura.radius_start = radius_start
	_aura.radius_end = radius_end
	add_child(_aura)

func _physics_process(delta: float) -> void:
	_elapsed += delta
	var progress: float = clampf(_elapsed / duration, 0.0, 1.0)

	# Track the burst's own radius rather than recomputing its growth curve, so the hitbox
	# can never drift away from what the player can see.
	var shape := collision_shape.shape as CircleShape2D
	if shape and is_instance_valid(_aura):
		shape.radius = _aura.get_current_radius()

	if progress >= LETHAL_UNTIL:
		collision_shape.set_deferred("disabled", true)
	if progress >= 1.0:
		queue_free()

