class_name IllusionLaser
extends Area2D

# Marisa Kirisame's Level 1 Charge Attack: Illusion Laser (イリュージョンレーザー).
# A rapid, high-intensity vertical laser beam that anchors to Marisa,
# sweeping horizontally with her movement and melting enemy Fairies and Spirits.

const DURATION: float = 0.45
const IGNITION_DURATION: float = 0.04
const COLLAPSE_START: float = 0.38
const COLLAPSE_DURATION: float = 0.07

const DAMAGE_TICK_INTERVAL: float = 0.06
## Footage against Lily White puts the whole 0.45s beam at about 24 normal shots.
const DAMAGE_PER_TICK: float = 3.2

const BASE_SCALE_X: float = 2.0
const HITBOX_WIDTH: float = 20.0
const TOP_MARGIN: float = -40.0

var player: Node2D = null
var playfield: Node2D = null

var _elapsed: float = 0.0
var _beam_height: float = 900.0
var _overlapping_enemies: Dictionary = {}
var _rect_shape: RectangleShape2D = null

@onready var beam_sprite: Sprite2D = %BeamSprite
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	# Ensure unique shape per instance
	_rect_shape = RectangleShape2D.new()
	_rect_shape.size = Vector2(HITBOX_WIDTH, _beam_height)
	collision_shape.shape = _rect_shape
	
	area_entered.connect(_on_area_entered)
	area_exited.connect(_on_area_exited)
	
	_update_geometry()

func setup(p_player: Node2D, p_playfield: Node2D) -> void:
	player = p_player
	playfield = p_playfield
	
	if is_instance_valid(player):
		global_position = Vector2(player.global_position.x, player.global_position.y - 14.0)
	
	_update_geometry()

func _process(delta: float) -> void:
	_elapsed += delta
	if _elapsed >= DURATION:
		queue_free()
		return
	
	# Track player movement horizontally and vertically
	if is_instance_valid(player):
		global_position.x = player.global_position.x
		global_position.y = player.global_position.y - 14.0
	
	_update_geometry()
	
	# Width & opacity lifecycle
	var current_scale_x: float = BASE_SCALE_X
	var current_alpha: float = 1.0
	
	if _elapsed < IGNITION_DURATION:
		var t: float = _elapsed / IGNITION_DURATION
		current_scale_x = lerpf(0.6, BASE_SCALE_X, t)
		current_alpha = 1.0
	elif _elapsed >= COLLAPSE_START:
		var t: float = (_elapsed - COLLAPSE_START) / COLLAPSE_DURATION
		current_scale_x = lerpf(BASE_SCALE_X, 0.0, t)
		current_alpha = maxf(0.0, 1.0 - t)
	
	if beam_sprite:
		beam_sprite.scale.x = maxf(0.0, current_scale_x)
		beam_sprite.modulate.a = current_alpha
	
	if _rect_shape:
		_rect_shape.size.x = maxf(1.0, HITBOX_WIDTH * (current_scale_x / BASE_SCALE_X))
	
	_process_damage_ticks(delta)

func _update_geometry() -> void:
	_beam_height = maxf(100.0, global_position.y - TOP_MARGIN)
	
	if beam_sprite:
		beam_sprite.position = Vector2(0.0, -_beam_height / 2.0)
		beam_sprite.scale.y = _beam_height / 16.0
	
	if collision_shape:
		collision_shape.position = Vector2(0.0, -_beam_height / 2.0)
	
	if _rect_shape:
		_rect_shape.size.y = _beam_height

func _process_damage_ticks(delta: float) -> void:
	var to_remove: Array[Area2D] = []
	
	for area: Area2D in _overlapping_enemies.keys():
		if not is_instance_valid(area) or (("is_dead" in area) and area.is_dead):
			to_remove.append(area)
			continue
		
		var cd: float = _overlapping_enemies[area] - delta
		if cd <= 0.0:
			if area.has_method("take_damage"):
				var took: bool = area.take_damage(DAMAGE_PER_TICK, "charge_attack")
				if not took:
					to_remove.append(area)
					continue
			cd = DAMAGE_TICK_INTERVAL
		
		_overlapping_enemies[area] = cd
	
	for area in to_remove:
		_overlapping_enemies.erase(area)

func _on_area_entered(area: Area2D) -> void:
	if not is_instance_valid(area):
		return
	if ("is_dead" in area) and area.is_dead:
		return
	if _overlapping_enemies.has(area):
		return
	
	if area.has_method("take_damage"):
		var took_dmg: bool = area.take_damage(DAMAGE_PER_TICK, "charge_attack")
		if took_dmg and is_instance_valid(area) and not (("is_dead" in area) and area.is_dead):
			_overlapping_enemies[area] = DAMAGE_TICK_INTERVAL

func _on_area_exited(area: Area2D) -> void:
	_overlapping_enemies.erase(area)

