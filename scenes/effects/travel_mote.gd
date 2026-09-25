class_name TravelMote
extends Node2D

# Point of light that arcs across the screen from one playfield to the other
enum MotePayload {
	PELLET,
	BIG_PELLET,
	SPIRIT,
	EXTRA_ATTACK
}

signal arrived(target_global_pos: Vector2, payload: int)

@export var flight_duration: float = 1.25
@export var arc_height: float = 120.0
@export var mote_color: Color = Color(1.0, 0.4, 0.5, 1.0) # Red for P1, Blue for P2
@export var payload: MotePayload = MotePayload.PELLET

var start_pos: Vector2 = Vector2.ZERO
var target_pos: Vector2 = Vector2.ZERO

var _elapsed: float = 0.0
var _is_arriving: bool = false
var _burst_tween: Tween = null
var pool: NodePool = null

const MOTE_HD_TEXTURE: Texture2D = preload("res://assets/effects/travel_mote_hd.png")
const EX_MOTE_TEXTURE: Texture2D = preload("res://resources/dat_textures/ex_mote.tres")

@onready var sprite: Sprite2D = %Sprite2D
@onready var ex_sprite: Sprite2D = %ExSprite2D

func on_pool_acquire() -> void:
	_elapsed = 0.0
	_is_arriving = false
	scale = Vector2.ONE
	modulate.a = 1.0
	rotation = 0.0
	if _burst_tween:
		_burst_tween.kill()
		_burst_tween = null
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	if ex_sprite == null:
		ex_sprite = get_node_or_null("%ExSprite2D")
	if sprite:
		sprite.visible = true
		sprite.scale = Vector2(0.18, 0.18)
		sprite.modulate = Color.WHITE
	if ex_sprite:
		ex_sprite.visible = false

func on_pool_release() -> void:
	_is_arriving = true
	if _burst_tween:
		_burst_tween.kill()
		_burst_tween = null
	for conn in arrived.get_connections():
		arrived.disconnect(conn.callable)

func setup(p_start: Vector2, p_target: Vector2, p_color: Color, p_duration: float = 1.25, p_arc: float = 120.0, p_payload: MotePayload = MotePayload.PELLET) -> void:
	start_pos = p_start
	target_pos = p_target
	mote_color = p_color
	flight_duration = p_duration
	arc_height = p_arc
	payload = p_payload
	_elapsed = 0.0
	_is_arriving = false
	scale = Vector2.ONE
	modulate.a = 1.0
	rotation = 0.0
	global_position = start_pos
	
	if sprite == null:
		sprite = get_node_or_null("%Sprite2D")
	if ex_sprite == null:
		ex_sprite = get_node_or_null("%ExSprite2D")
	
	if sprite:
		sprite.visible = true
		sprite.texture = MOTE_HD_TEXTURE
		sprite.modulate = mote_color
		match payload:
			MotePayload.SPIRIT:
				sprite.scale = Vector2(0.32, 0.32)
			MotePayload.BIG_PELLET:
				sprite.scale = Vector2(0.25, 0.25)
			MotePayload.EXTRA_ATTACK:
				sprite.scale = Vector2(0.65, 0.65)
			_:
				sprite.scale = Vector2(0.18, 0.18)
	
	if ex_sprite:
		if payload == MotePayload.EXTRA_ATTACK:
			ex_sprite.visible = true
			ex_sprite.texture = EX_MOTE_TEXTURE
			ex_sprite.scale = Vector2(2.0, 2.0)
			ex_sprite.modulate = Color.WHITE
		else:
			ex_sprite.visible = false

func _physics_process(delta: float) -> void:
	if _is_arriving:
		return
	
	_elapsed += delta
	var progress: float = clampf(_elapsed / flight_duration, 0.0, 1.0)
	
	# Smooth ease-in, ease-out (slow start, fast cruise across divider, gentle deceleration into landing)
	var ease_t: float = 0.5 * (1.0 - cos(progress * PI))
	
	var linear_pt: Vector2 = start_pos.lerp(target_pos, ease_t)
	var arc_offset: float = -sin(ease_t * PI) * arc_height
	global_position = linear_pt + Vector2(0.0, arc_offset)
	
	if payload == MotePayload.EXTRA_ATTACK:
		rotation += 7.5 * delta
	
	if progress >= 1.0:
		_on_arrival()

func _on_arrival() -> void:
	_is_arriving = true
	arrived.emit(target_pos, payload)
	
	# Quick flash/pop burst on landing
	if _burst_tween:
		_burst_tween.kill()
	_burst_tween = create_tween()
	var burst_scale := Vector2(1.6, 1.6)
	if payload == MotePayload.BIG_PELLET:
		burst_scale = Vector2(2.4, 2.4)
	elif payload == MotePayload.SPIRIT:
		burst_scale = Vector2(3.0, 3.0)
	elif payload == MotePayload.EXTRA_ATTACK:
		burst_scale = Vector2(5.5, 5.5)
		
	_burst_tween.tween_property(self, "scale", burst_scale, 0.09)
	_burst_tween.parallel().tween_property(self, "modulate:a", 0.0, 0.09)
	_burst_tween.finished.connect(func():
		if pool:
			pool.release(self)
		else:
			queue_free()
	)

