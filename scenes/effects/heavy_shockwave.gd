class_name HeavyShockwave
extends Area2D

# Heavy defensive shockwave triggered on hit landing and spellcards.
# Unlike fairy shockwaves, this clears EVERYTHING in range (pellets, big bullets, lasers, orbs, fairies, spirits)
# and does NOT generate reciprocal attacks or cascading shockwaves.
# Bosses (BossCharacter) and mid-bosses (LilyWhite) are immune.

const BASE_TEXTURE_RADIUS: float = 30.0
const SPIN_SPEED: float = 2.2 # Faster, high-energy spin

@export var start_scale: float = 0.4
@export var target_scale: float = 8.6 # 30px * 8.6 = 258px radius (~516px diameter default)
@export var duration: float = 1.02
@export var expansion_speed: float = 400.0 # Calibrated constant travelling speed (400 px/s)
@export var theme_color: Color = Color(0.35, 0.75, 1.0, 1.0) # Bright cyan-blue magic ring default
@export var ribbon_half_width: float = 20.0 # 40px thick runic ribbon band (reduced by half)

var _elapsed: float = 0.0
var _current_radius: float = 0.0
var _current_alpha: float = 1.0
var _rotation_angle: float = 0.0
var _circle_shape: CircleShape2D
var playfield: Node2D = null

@onready var ring_rect: ColorRect = get_node_or_null("%RingRect")
@onready var sprite: Sprite2D = get_node_or_null("%Sprite2D")
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_INHERIT
	_circle_shape = CircleShape2D.new()
	_circle_shape.radius = BASE_TEXTURE_RADIUS * start_scale
	collision_shape.set_deferred("shape", _circle_shape)
	
	area_entered.connect(_on_area_entered)
	body_entered.connect(_on_body_entered)
	
	if ring_rect and ring_rect.material:
		ring_rect.material = ring_rect.material.duplicate()
	
	if sprite:
		sprite.scale = Vector2(start_scale, start_scale)
		sprite.modulate = theme_color

func setup(
	spawn_pos: Vector2,
	custom_radius: float = -1.0,
	custom_duration: float = -1.0,
	custom_color: Color = Color.TRANSPARENT,
	p_playfield: Node2D = null,
	custom_speed: float = -1.0
) -> void:
	position = spawn_pos
	if custom_radius > 0.0:
		target_scale = custom_radius / BASE_TEXTURE_RADIUS
	if custom_speed > 0.0:
		expansion_speed = custom_speed
	
	var start_r: float = BASE_TEXTURE_RADIUS * start_scale
	var target_r: float = BASE_TEXTURE_RADIUS * target_scale
	var delta_r: float = maxf(1.0, target_r - start_r)
	
	if custom_duration > 0.0:
		duration = custom_duration
		expansion_speed = delta_r / duration
	elif expansion_speed > 0.0:
		duration = delta_r / expansion_speed
	
	if custom_color != Color.TRANSPARENT and custom_color.a > 0.0:
		theme_color = custom_color
		if ring_rect and ring_rect.material:
			(ring_rect.material as ShaderMaterial).set_shader_parameter("color_tint", theme_color)
		if sprite:
			sprite.modulate = theme_color
	if p_playfield != null:
		playfield = p_playfield

func _physics_process(delta: float) -> void:
	_elapsed += delta
	var progress: float = clampf(_elapsed / duration, 0.0, 1.0)
	
	# Uniform travelling expansion: all shockwaves travel at the exact same constant speed
	var current_scale: float = lerp(start_scale, target_scale, progress)
	_current_radius = BASE_TEXTURE_RADIUS * current_scale
	
	_current_alpha = 1.0
	if progress > 0.8:
		_current_alpha = clampf((1.0 - progress) / 0.2, 0.0, 1.0)
	
	_rotation_angle += SPIN_SPEED * delta
	
	if ring_rect:
		var current_half_width: float = minf(ribbon_half_width, _current_radius * 0.85)
		var inner_r: float = maxf(0.0, _current_radius - current_half_width)
		var outer_r: float = _current_radius + current_half_width
		ring_rect.position = Vector2(-outer_r, -outer_r)
		ring_rect.size = Vector2(2.0 * outer_r, 2.0 * outer_r)
		var ring_mat: ShaderMaterial = ring_rect.material as ShaderMaterial
		if ring_mat:
			ring_mat.set_shader_parameter("inner_radius_px", inner_r)
			ring_mat.set_shader_parameter("outer_radius_px", outer_r)
			ring_mat.set_shader_parameter("rotation_angle", _rotation_angle)
			ring_mat.set_shader_parameter("color_tint", Color(theme_color.r, theme_color.g, theme_color.b, _current_alpha))
	
	if sprite:
		sprite.scale = Vector2(current_scale, current_scale)
		sprite.rotation += SPIN_SPEED * delta
		sprite.modulate = Color(theme_color.r, theme_color.g, theme_color.b, _current_alpha)
	
	if _circle_shape:
		_circle_shape.radius = _current_radius
	
	# Direct sweep against active playfield entities during Action Stop pause
	if playfield != null and is_instance_valid(playfield):
		_sweep_playfield_entities()
	
	queue_redraw()
	
	if _elapsed >= duration:
		queue_free()

func _draw() -> void:
	if _current_radius <= 0.0 or _current_alpha <= 0.0:
		return
	
	# Atmospheric multi-tier glow matching the stretched runic ribbon
	var rim_color := Color(theme_color.r, theme_color.g, theme_color.b, _current_alpha)
	var current_half_width: float = minf(ribbon_half_width, _current_radius * 0.85)
	
	# 1. Outer ambient halo enveloping the runic ribbon (32 segments for lightweight CPU tessellation)
	draw_arc(Vector2.ZERO, _current_radius, 0.0, TAU, 32, Color(rim_color.r, rim_color.g, rim_color.b, _current_alpha * 0.35), current_half_width * 1.4, false)
	# 2. Luminous core highlighting the runes
	draw_arc(Vector2.ZERO, _current_radius, 0.0, TAU, 32, Color(lerp(rim_color.r, 1.0, 0.4), lerp(rim_color.g, 1.0, 0.4), lerp(rim_color.b, 1.0, 0.4), _current_alpha * 0.6), current_half_width * 0.4, false)

func _sweep_playfield_entities() -> void:
	var global_center := global_position
	var r_sq := _current_radius * _current_radius
	
	# 1. Sweep entities layer (fairies, spirits, yin-yang orbs, lasers)
	if "entities_layer" in playfield and playfield.entities_layer != null:
		for child in playfield.entities_layer.get_children():
			if not is_instance_valid(child) or child.is_queued_for_deletion():
				continue
			if "visible" in child and not child.visible:
				continue
			if ("is_dead" in child) and child.is_dead:
				continue
			if global_center.distance_squared_to(child.global_position) <= r_sq:
				_clear_entity(child)
	
	# 2. Sweep bullets layer via active pools (only iterates ~20-50 active nodes, NOT 1,700 pooled nodes!)
	var active_bullets: Array = []
	if playfield.has_method("get_active_bullets"):
		active_bullets = playfield.get_active_bullets()
	elif "bullets_layer" in playfield and playfield.bullets_layer != null:
		active_bullets = playfield.bullets_layer.get_children()
	
	for child in active_bullets:
		if not is_instance_valid(child) or child.is_queued_for_deletion():
			continue
		if "visible" in child and not child.visible:
			continue
		if ("is_canceled" in child) and child.is_canceled:
			continue
		if global_center.distance_squared_to(child.global_position) <= r_sq:
			_clear_entity(child)

func _clear_entity(entity: Node) -> void:
	if not is_instance_valid(entity) or entity.is_queued_for_deletion():
		return
	if "visible" in entity and not entity.visible:
		return
	if ("is_canceled" in entity) and entity.is_canceled:
		return
	if ("is_dead" in entity) and entity.is_dead:
		return
	
	# Bosses and mid-bosses are strictly immune to defensive shockwaves
	if entity is BossCharacter or entity is LilyWhite:
		return
	
	# Players live in entities_layer, so the direct sweep reaches them too - but a
	# field's own defensive shockwave never touches the pilot standing in it.
	if entity is Player:
		return
	
	# 1. Enemy Pellets (regular, ring, big bullets)
	if entity is EnemyPellet:
		entity.despawn()
		return
	
	# 2. Danmaku Bullets (all shapes: pellets, ovals, talismans, stars)
	if entity is DanmakuBullet:
		entity.despawn()
		return
	
	# 3. EX Attacks: Yin-Yang Orb
	if entity is YinYangOrb:
		entity.queue_free()
		return
	
	# 4. EX Attacks: Earth Light Ray (laser beam container or child hitbox)
	if entity is EarthLightRay:
		entity.queue_free()
		return
	if entity.get_parent() is EarthLightRay:
		entity.get_parent().queue_free()
		return
	
	# 5. Everything else opts in through take_damage()'s source string: fairies and
	# spirits, plus cross-field custom hazards such as Sakuya's Extra Attack daggers
	# and Cirno's stalactites. Hazards meant to survive a blast (YoumuDarkSpirit)
	# simply return false there, so it stays their own decision.
	if entity.has_method("take_damage"):
		entity.take_damage(999.0, "heavy_shockwave")

func _on_area_entered(area: Area2D) -> void:
	_clear_entity(area)

func _on_body_entered(body: Node2D) -> void:
	_clear_entity(body)

