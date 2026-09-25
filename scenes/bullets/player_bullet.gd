class_name PlayerBullet
extends Area2D

# Projectile fired by players (Reimu amulet / Marisa laser needle)
@export var speed: float = 1400.0
@export var damage: float = 1.0
@export var direction: Vector2 = Vector2.UP
@export var character_id: String = "reimu"
@export var character_data: CharacterData = null

var _has_hit: bool = false

@onready var sprite: Sprite2D = %Sprite2D
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	_apply_visuals()
	area_entered.connect(_on_area_entered)

func setup_with_data(char_data: CharacterData, spawn_pos: Vector2) -> void:
	character_data = char_data
	if char_data:
		character_id = char_data.character_id
		speed = char_data.bullet_speed
		damage = char_data.bullet_damage
	position = spawn_pos
	_apply_visuals()

func setup(char_id: String, spawn_pos: Vector2, bullet_speed: float = 1400.0, bullet_damage: float = 1.0) -> void:
	character_id = char_id.to_lower()
	character_data = CharacterData.get_character(character_id)
	position = spawn_pos
	speed = bullet_speed
	damage = bullet_damage
	_apply_visuals()

func _apply_visuals() -> void:
	if character_data == null and not character_id.is_empty():
		character_data = CharacterData.get_character(character_id)
	
	if sprite == null and has_node("%Sprite2D"):
		sprite = %Sprite2D
	if collision_shape == null and has_node("%CollisionShape2D"):
		collision_shape = %CollisionShape2D
	
	if character_data and sprite:
		if character_data.bullet_texture:
			sprite.texture = character_data.bullet_texture
		sprite.scale = character_data.bullet_scale
		sprite.texture_filter = character_data.bullet_texture_filter
	
	if collision_shape:
		if character_id == "yuuka":
			var circle := CircleShape2D.new()
			circle.radius = 9.0
			collision_shape.shape = circle
		else:
			var capsule := CapsuleShape2D.new()
			capsule.radius = 6.0
			capsule.height = character_data.bullet_capsule_height if character_data else 60.0
			collision_shape.shape = capsule

func _physics_process(delta: float) -> void:
	position += direction * speed * delta
	if character_id == "yuuka" and sprite:
		sprite.rotation += 24.0 * delta
	
	# Despawn once exiting boundaries of the playfield (y = 0 is top border)
	if position.y < -50.0 or position.y > 1050.0 or position.x < -50.0 or position.x > 700.0:
		queue_free()

const SHOT_HIT_SHARD_SCENE: PackedScene = preload("res://scenes/effects/shot_hit_shard.tscn")
const ShotHitShard = preload("res://scenes/effects/shot_hit_shard.gd")
const SHOT_CRUMBLE_EFFECT_SCENE: PackedScene = preload("res://scenes/effects/shot_crumble_effect.tscn")
const ShotCrumbleEffect = preload("res://scenes/effects/shot_crumble_effect.gd")

const REIMU_SHARD_TEX: Texture2D = preload("res://resources/dat_textures/reimu_shot_penetrate.tres")
const MARISA_SHARD_TEXS: Array[Texture2D] = [
	preload("res://resources/dat_textures/marisa_shot_shard_1.tres"),
	preload("res://resources/dat_textures/marisa_shot_shard_2.tres"),
	preload("res://resources/dat_textures/marisa_shot_shard_3.tres"),
]
const CIRNO_CRUMBLE_TEXS: Array[Texture2D] = [
	preload("res://resources/dat_textures/cirno_shot_crumble_1.tres"),
	preload("res://resources/dat_textures/cirno_shot_crumble_2.tres"),
	preload("res://resources/dat_textures/cirno_shot_crumble_3.tres"),
]

func _on_area_entered(area: Area2D) -> void:
	if _has_hit:
		return
	if area.has_method("take_damage"):
		var accepted: bool = area.take_damage(damage, "bullet")
		if accepted:
			_has_hit = true
			if character_id == "cirno":
				_spawn_crumble_effect()
			elif character_id == "reimu" or character_id == "marisa":
				_spawn_hit_shard(area)
			set_deferred("monitoring", false)
			set_deferred("monitorable", false)
			queue_free()

func _spawn_crumble_effect() -> void:
	if SHOT_CRUMBLE_EFFECT_SCENE == null:
		return
	
	var crumble: ShotCrumbleEffect = SHOT_CRUMBLE_EFFECT_SCENE.instantiate()
	var spawn_pos := global_position
	var target_rot: float = (direction.angle() + PI * 0.5) if direction.length_squared() > 0.001 else 0.0
	var bullet_scale_val := character_data.bullet_scale if character_data else Vector2(1.5, 1.5)
	
	var target_parent: Node = null
	if get_parent():
		var playfield := get_parent().get_parent()
		if playfield:
			target_parent = playfield.get_node_or_null("%Effects")
			if target_parent == null:
				target_parent = playfield.get_node_or_null("Effects")
	if target_parent == null:
		target_parent = get_parent()

	if target_parent:
		target_parent.add_child(crumble)
		crumble.setup(spawn_pos, CIRNO_CRUMBLE_TEXS, bullet_scale_val, target_rot)

func _spawn_hit_shard(target_area: Area2D) -> void:
	if SHOT_HIT_SHARD_SCENE == null:
		return
	
	# Determine exit point on the far side of the enemy relative to shot direction
	var travel_dir := direction.normalized() if direction.length_squared() > 0.001 else Vector2.UP
	var enemy_pos := target_area.global_position
	
	# Estimate enemy extents from its collision shape if present
	var exit_distance: float = 18.0
	var col_shape: CollisionShape2D = target_area.get_node_or_null("CollisionShape2D")
	if col_shape and col_shape.shape:
		if col_shape.shape is CircleShape2D:
			exit_distance = maxf(exit_distance, (col_shape.shape as CircleShape2D).radius + 4.0)
		elif col_shape.shape is RectangleShape2D:
			var half_sz := (col_shape.shape as RectangleShape2D).size * 0.5
			exit_distance = maxf(exit_distance, maxf(half_sz.x, half_sz.y) + 4.0)
		elif col_shape.shape is CapsuleShape2D:
			exit_distance = maxf(exit_distance, (col_shape.shape as CapsuleShape2D).height * 0.5 + 4.0)
	
	# Calculate exit point on the other side of the enemy, preserving lateral entry offset
	var bullet_pos := global_position
	var lateral_proj := (bullet_pos - enemy_pos) - travel_dir * (bullet_pos - enemy_pos).dot(travel_dir)
	var spawn_pos := enemy_pos + lateral_proj * 0.4 + travel_dir * exit_distance
	
	var shard: ShotHitShard = SHOT_HIT_SHARD_SCENE.instantiate()
	
	var shard_tex: Texture2D = null
	if character_id == "marisa":
		shard_tex = MARISA_SHARD_TEXS.pick_random()
	elif character_id == "reimu":
		shard_tex = REIMU_SHARD_TEX
	
	# Prefer adding to %Effects layer of the playfield for proper render sorting
	var target_parent: Node = null
	if get_parent():
		var playfield := get_parent().get_parent()
		if playfield:
			target_parent = playfield.get_node_or_null("%Effects")
			if target_parent == null:
				target_parent = playfield.get_node_or_null("Effects")
	if target_parent == null:
		target_parent = get_parent()

	if target_parent:
		target_parent.add_child(shard)
		shard.setup(spawn_pos, travel_dir, shard_tex, Vector2(1.5, 1.5))

