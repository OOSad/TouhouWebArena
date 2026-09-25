class_name CirnoFreezeBlade
extends Area2D

## Cirno's Level 1 Charge Attack Projectile: Freeze Blade (フリーズブレード)
## A crystalline icicle shard fired in a 28-way radial circle around Cirno.
## Travels outward at high speed, dealing 1.5 damage (pl05.sht) to oncoming enemies and
## triggering an authentic 3-frame ice crumble effect upon impact.

const SHOT_CRUMBLE_EFFECT_SCENE: PackedScene = preload("res://scenes/effects/shot_crumble_effect.tscn")
const ShotCrumbleEffectClass = preload("res://scenes/effects/shot_crumble_effect.gd")

const CIRNO_CRUMBLE_TEXS: Array[Texture2D] = [
	preload("res://resources/dat_textures/cirno_shot_crumble_1.tres"),
	preload("res://resources/dat_textures/cirno_shot_crumble_2.tres"),
	preload("res://resources/dat_textures/cirno_shot_crumble_3.tres"),
]

const PLAYFIELD_MIN_X: float = -80.0
const PLAYFIELD_MAX_X: float = 680.0
const PLAYFIELD_MIN_Y: float = -80.0
const PLAYFIELD_MAX_Y: float = 1040.0

@export var speed: float = 930.0
@export var damage: float = 1.5

var direction: Vector2 = Vector2.RIGHT
var target_player: Node2D = null
var playfield_ref: Node2D = null
var _has_hit: bool = false

@onready var sprite: Sprite2D = %Sprite2D
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	collision_layer = 4 # Layer 3 = player_bullets
	collision_mask = 8  # Layer 4 = enemies
	area_entered.connect(_on_area_entered)

func setup(p_player: Node2D, p_playfield: Node2D, p_dir: Vector2, p_angle: float, p_pos: Vector2) -> void:
	target_player = p_player
	playfield_ref = p_playfield
	direction = p_dir.normalized()
	rotation = p_angle
	global_position = p_pos

func _physics_process(delta: float) -> void:
	global_position += direction * speed * delta
	
	# Despawn if outside playfield boundaries
	if global_position.x < PLAYFIELD_MIN_X or global_position.x > PLAYFIELD_MAX_X or \
	   global_position.y < PLAYFIELD_MIN_Y or global_position.y > PLAYFIELD_MAX_Y:
		queue_free()

func _on_area_entered(area: Area2D) -> void:
	if _has_hit:
		return
	
	if area.has_method("take_damage"):
		var accepted: bool = area.take_damage(damage, "charge_attack")
		if accepted:
			_has_hit = true
			_spawn_crumble_effect()
			set_deferred("monitoring", false)
			set_deferred("monitorable", false)
			queue_free()

func _spawn_crumble_effect() -> void:
	if SHOT_CRUMBLE_EFFECT_SCENE == null:
		return
	
	var crumble: ShotCrumbleEffectClass = SHOT_CRUMBLE_EFFECT_SCENE.instantiate()
	var spawn_pos := global_position
	var target_rot: float = rotation + PI * 0.5
	
	var target_parent: Node = null
	if playfield_ref:
		if playfield_ref.has_node("%Effects"):
			target_parent = playfield_ref.get_node("%Effects")
		elif playfield_ref.has_node("Effects"):
			target_parent = playfield_ref.get_node("Effects")
	elif get_parent():
		var playfield := get_parent().get_parent()
		if playfield:
			target_parent = playfield.get_node_or_null("%Effects")
			if target_parent == null:
				target_parent = playfield.get_node_or_null("Effects")
	
	if target_parent == null:
		target_parent = get_parent()
	
	if target_parent:
		target_parent.add_child(crumble)
		crumble.setup(spawn_pos, CIRNO_CRUMBLE_TEXS, Vector2(1.5, 1.5), target_rot)

