class_name ReisenMoonCharge
extends Node2D

## One of the three light motes Reisen conjures before Triple Moon Shot. Each carries a
## single moon: it leaves her, drifts to where that moon will appear, and hands over.
##
## Visually this is the game's Extra Attack mote, the one that flies between playfields when
## a player sends an attack over (`TravelMote` with `MotePayload.EXTRA_ATTACK`): the soft
## additive glow of `travel_mote_hd.png` tinted warm, with the spiky ribbed star of
## `ex_mote.png` laid over it and slowly turning. Both textures, both base scales and the
## additive blend are taken from that scene so the two read as the same object.
##
## It is a separate scene rather than a `TravelMote` because the behaviour has nothing in
## common: `TravelMote` arcs between two playfields, is pooled, and delivers a payload to
## the far field. This just sinks a short way and becomes a moon.
##
## Timed off the reference at 60fps: the motes appear on her at 2.40s and the moons have
## resolved out of them by 3.60s, so the tell runs about 1.2s and nothing that can hurt
## anybody exists until it ends.

## Base scales lifted from `TravelMote`'s EXTRA_ATTACK payload, so a mote at size 1.0 is the
## same size as one of those. The star is deliberately the wider of the two: its petals
## reach past the glow.
const GLOW_BASE_SCALE: float = 0.65
const STAR_BASE_SCALE: float = 2.0

@export var duration: float = 1.20
## Overall size, against a standard Extra Attack mote. These are the "large" ones.
@export var size_start: float = 0.70
@export var size_peak: float = 1.30
@export var size_end: float = 0.95
## Fraction of the life spent drifting out and swelling. The rest is the handover.
@export var swell_fraction: float = 0.72
## Warm tint on the glow underneath, sampled off the reference's salmon petals. The star
## itself stays white, exactly as `TravelMote` leaves it.
@export var glow_tint: Color = Color(1.0, 0.35, 0.45, 1.0)
@export var spin_speed: float = 3.2
## Where this mote drifts to, relative to where it was cast.
@export var travel: Vector2 = Vector2(0.0, 130.0)

var _elapsed: float = 0.0
var _origin: Vector2 = Vector2.ZERO

@onready var glow: Sprite2D = $Glow
@onready var star: Sprite2D = $Star

func setup(spawn_pos: Vector2, p_travel: Vector2, p_duration: float) -> void:
	position = spawn_pos
	_origin = spawn_pos
	travel = p_travel
	duration = p_duration

func _ready() -> void:
	_origin = position
	_apply(0.0)

func _process(delta: float) -> void:
	_elapsed += delta
	if star:
		star.rotation += spin_speed * delta
	var progress: float = clampf(_elapsed / duration, 0.0, 1.0)
	_apply(progress)
	if progress >= 1.0:
		queue_free()

func _apply(progress: float) -> void:
	if glow == null or star == null:
		return

	# It drifts for as long as it is swelling, then holds station for the handover, so the
	# moon appears where the mote came to rest rather than somewhere it is still moving.
	var drift: float = clampf(progress / swell_fraction, 0.0, 1.0)
	position = _origin + travel * (1.0 - pow(1.0 - drift, 2.0))

	var size: float
	if progress < swell_fraction:
		var t: float = progress / swell_fraction
		size = lerpf(size_start, size_peak, 1.0 - pow(1.0 - t, 2.0))
	else:
		var t: float = (progress - swell_fraction) / (1.0 - swell_fraction)
		size = lerpf(size_peak, size_end, t)

	glow.scale = Vector2.ONE * (GLOW_BASE_SCALE * size)
	star.scale = Vector2.ONE * (STAR_BASE_SCALE * size)

	# Holds full strength almost to the end: the handover should read as the mote becoming
	# the moon, not as it fading out and the moon appearing.
	var alpha: float = 1.0 - smoothstep(0.90, 1.0, progress)
	glow.modulate = Color(glow_tint.r, glow_tint.g, glow_tint.b, alpha)
	star.modulate = Color(1.0, 1.0, 1.0, alpha)
