class_name BossLifeRing
extends Node2D

## The life gauge round a Level 4 boss, as LoLK's front.anm draws it from lifebar.png:
##
## - Script 237: the band (sprite 142) bent into an arc 8 wide at radius 112, starting from
##   the top. The engine sets how much of the turn it covers; here that is the boss's health,
##   running clockwise, so the end retreats back toward the top as she takes damage.
## - Scripts 238 and 239: a 2-wide full circle (sprite 143) on each side of the band, at radius
##   116 and 108.
## - Interrupt 1: fade to nothing over 20 frames when the boss is done.
##
## The ring sweeps in over a second once the boss arrives (engine behaviour, not in the anm).
## LoLK's numbers are taken one to one in our pixels, which keeps the ring at about half the
## magic circle's width, as in LoLK.

const FRAME: float = 1.0 / 60.0
const RADIUS: float = 112.0
const BAND_WIDTH: float = 8.0
const EDGE_WIDTH: float = 2.0
const EDGE_RADII: Array[float] = [108.0, 116.0]
## Segments in a full turn; an arc uses its share of them.
const SEGMENTS: int = 64
const FILL_TIME: float = 60.0 * FRAME
const FADE_TIME: float = 20.0 * FRAME

const BAND_TEXTURE: Texture2D = preload("res://resources/dat_textures/boss_life_ring.tres")
const EDGE_TEXTURE: Texture2D = preload("res://resources/dat_textures/boss_life_ring_edge.tres")

## Share of the boss's health left, 0 to 1.
var health_fraction: float = 1.0:
	set(value):
		health_fraction = clampf(value, 0.0, 1.0)
		queue_redraw()

var _fill: float = 0.0
var _filling: bool = false


func _ready() -> void:
	visible = false
	set_process(false)


## Starts the sweep in; the boss calls this once she has arrived.
func appear() -> void:
	visible = true
	_filling = true
	set_process(true)


## Fades the ring out; the boss calls this when she is beaten, leaves or is dispelled.
func vanish() -> void:
	_filling = false
	set_process(false)
	var tween := create_tween()
	tween.tween_property(self, "modulate:a", 0.0, FADE_TIME)
	tween.tween_callback(hide)


func _process(delta: float) -> void:
	_fill = minf(1.0, _fill + delta / FILL_TIME)
	if _fill >= 1.0:
		set_process(false)
	queue_redraw()


func _draw() -> void:
	for radius in EDGE_RADII:
		_draw_arc(radius, EDGE_WIDTH, TAU * _fill, EDGE_TEXTURE)
	_draw_arc(RADIUS, BAND_WIDTH, TAU * minf(_fill, health_fraction), BAND_TEXTURE)


## An arc of the given texture laid across its width, clockwise from the top.
func _draw_arc(radius: float, width: float, span: float, texture: Texture2D) -> void:
	if span <= 0.0:
		return
	var steps := maxi(1, ceili(SEGMENTS * span / TAU))
	var inner := radius - width * 0.5
	var outer := radius + width * 0.5
	var colors := PackedColorArray([Color.WHITE, Color.WHITE, Color.WHITE, Color.WHITE])
	var uvs := PackedVector2Array([Vector2(0, 0), Vector2(1, 0), Vector2(1, 1), Vector2(0, 1)])
	var prev := Vector2.from_angle(-PI * 0.5)
	for i in steps:
		var next := Vector2.from_angle(-PI * 0.5 + span * float(i + 1) / float(steps))
		draw_primitive(PackedVector2Array([prev * inner, prev * outer, next * outer, next * inner]),
			colors, uvs, texture)
		prev = next
