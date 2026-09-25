class_name Shockwave
extends Area2D

signal pellet_canceled(pellet_pos: Vector2, bounce_count: int)

# Constants
const BASE_TEXTURE_RADIUS: float = 30.0 # PoFV's shockwave circle (etama2 sprite 192) is 62px across
const DAMAGE_TICK_INTERVAL: float = 0.07 # Seconds between damage ticks (~4 frames at 60fps)
const SPIN_SPEED: float = 1.4 # Radians per second for magical rune rotation

# Configuration per fairy type
const CONFIG_BY_TYPE: Dictionary = {
	Fairy.FairyType.SMALL: {
		"start_scale": 0.2,
		"target_scale": 3.3,  # 30px * 3.3 = 99px radius (198px diameter)
		"duration": 0.40,
		"damage_per_tick": 1.0,
	},
	Fairy.FairyType.GREAT: {
		"start_scale": 0.3,
		"target_scale": 4.8,  # 30px * 4.8 = 144px radius (288px diameter)
		"duration": 0.50,
		"damage_per_tick": 1.0,
	}
}

# State
var fairy_type: Fairy.FairyType = Fairy.FairyType.SMALL
var start_scale: float = 0.2
var target_scale: float = 3.3
var duration: float = 0.40
var damage_per_tick: float = 1.0

var _elapsed: float = 0.0
# Maps Area2D -> cooldown timer float (seconds until next tick)
var _overlapping_fairies: Dictionary = {}

@onready var sprite: Sprite2D = %Sprite2D
@onready var collision_shape: CollisionShape2D = %CollisionShape2D
var _circle_shape: CircleShape2D

func _ready() -> void:
	# Ensure circle shape is unique per instance so dynamic radius does not bleed across shockwaves
	_circle_shape = CircleShape2D.new()
	_circle_shape.radius = BASE_TEXTURE_RADIUS * start_scale
	collision_shape.set_deferred("shape", _circle_shape)
	
	area_entered.connect(_on_area_entered)
	area_exited.connect(_on_area_exited)
	
	sprite.scale = Vector2(start_scale, start_scale)

func setup(p_type: Fairy.FairyType, spawn_pos: Vector2) -> void:
	fairy_type = p_type
	position = spawn_pos
	
	var cfg: Dictionary = CONFIG_BY_TYPE.get(fairy_type, CONFIG_BY_TYPE[Fairy.FairyType.SMALL])
	start_scale = cfg["start_scale"]
	target_scale = cfg["target_scale"]
	duration = cfg["duration"]
	damage_per_tick = cfg["damage_per_tick"]
	
	if is_node_ready():
		sprite.scale = Vector2(start_scale, start_scale)
		if _circle_shape:
			_circle_shape.radius = BASE_TEXTURE_RADIUS * start_scale

func _physics_process(delta: float) -> void:
	_elapsed += delta
	var progress: float = clampf(_elapsed / duration, 0.0, 1.0)
	
	# Ease-out expansion: fast initial burst, then slowing
	var expand_t: float = 1.0 - pow(1.0 - progress, 3.0)
	var current_scale: float = lerp(start_scale, target_scale, expand_t)
	
	if sprite:
		sprite.scale = Vector2(current_scale, current_scale)
		sprite.rotation += SPIN_SPEED * delta
		var alpha: float = 1.0
		if progress > 0.5:
			alpha = (1.0 - progress) / 0.5
		sprite.modulate.a = clampf(alpha, 0.0, 1.0)
	
	if _circle_shape:
		_circle_shape.radius = BASE_TEXTURE_RADIUS * current_scale
	
	# Process damage ticks for all currently overlapping fairies
	_process_damage_ticks(delta)
	
	if _elapsed >= duration:
		queue_free()

func _process_damage_ticks(delta: float) -> void:
	var to_remove: Array[Area2D] = []
	for area: Area2D in _overlapping_fairies.keys():
		if not is_instance_valid(area) or (("is_dead" in area) and area.is_dead):
			to_remove.append(area)
			continue
		
		var cooldown: float = _overlapping_fairies[area] - delta
		if cooldown <= 0.0:
			if area.has_method("take_damage"):
				var took_dmg: bool = area.take_damage(damage_per_tick, "shockwave")
				if not took_dmg:
					to_remove.append(area)
					continue
			cooldown = DAMAGE_TICK_INTERVAL
		
		_overlapping_fairies[area] = cooldown
	
	for area in to_remove:
		_overlapping_fairies.erase(area)

func _on_area_entered(area: Area2D) -> void:
	if not is_instance_valid(area):
		return
	
	# Check for enemy pellet / danmaku bullet clearing
	if area is EnemyPellet or area is DanmakuBullet:
		if not area.is_canceled and area.can_be_canceled:
			var p_pos: Vector2 = area.global_position
			var b_count: int = area.bounce_count if (area is EnemyPellet) else 0
			area.cancel()
			pellet_canceled.emit(p_pos, b_count)
		return
	
	# Enemy damage processing (fairies and spirits)
	if ("is_dead" in area) and area.is_dead:
		return
	if _overlapping_fairies.has(area):
		return
	
	# Deal first tick immediately upon contact
	if area.has_method("take_damage"):
		var took_dmg: bool = area.take_damage(damage_per_tick, "shockwave")
		if took_dmg and is_instance_valid(area) and not (("is_dead" in area) and area.is_dead):
			_overlapping_fairies[area] = DAMAGE_TICK_INTERVAL

func _on_area_exited(area: Area2D) -> void:
	_overlapping_fairies.erase(area)
