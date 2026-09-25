class_name CirnoStalactite
extends Area2D

## Cirno's Extra Attack: Falling Stalactite (氷柱落とし)
## Crystalline stalactites deposited at the top ceiling of the opponent's field
## by incoming light motes. After a brief hang, they fall gently downward under
## gravity, dealing 1.0 contact damage to the player and shattering into ice crumbs.

const SHOT_CRUMBLE_EFFECT_SCENE: PackedScene = preload("res://scenes/effects/shot_crumble_effect.tscn")
const ShotCrumbleEffectClass = preload("res://scenes/effects/shot_crumble_effect.gd")

const CIRNO_CRUMBLE_TEXS: Array[Texture2D] = [
	preload("res://resources/dat_textures/cirno_shot_crumble_1.tres"),
	preload("res://resources/dat_textures/cirno_shot_crumble_2.tres"),
	preload("res://resources/dat_textures/cirno_shot_crumble_3.tres"),
]

const GRAVITY: float = 340.0
const MAX_FALL_SPEED: float = 420.0
const DESPAWN_Y: float = 1060.0
const HANG_DURATION: float = 0.16
const DAMAGE: float = 1.0

@export var rank: int = 1
@export var spawn_sfx: String = "se_exattack"

var velocity_y: float = 0.0
var _hang_timer: float = 0.0
var _has_hit: bool = false

@onready var sprite: Sprite2D = $Sprite2D
@onready var collision_shape: CollisionShape2D = $CollisionShape2D

func _ready() -> void:
	collision_layer = 2 # Layer 2 = enemy_bullets / hazards
	collision_mask = 1  # Layer 1 = player
	body_entered.connect(_on_body_entered)
	
	if not spawn_sfx.is_empty():
		AudioService.play_sfx(spawn_sfx)
	
	# Spawn scale pop
	scale = Vector2(0.2, 0.2)
	var tween := create_tween()
	tween.tween_property(self, "scale", Vector2.ONE, 0.14).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)

func setup_rank(p_rank: int) -> void:
	rank = clampi(p_rank, 1, 22)

func _physics_process(delta: float) -> void:
	if _has_hit:
		return
	
	if _hang_timer < HANG_DURATION:
		_hang_timer += delta
		if _hang_timer < HANG_DURATION:
			return
	
	velocity_y = minf(velocity_y + GRAVITY * delta, MAX_FALL_SPEED)
	position.y += velocity_y * delta
	
	if position.y >= DESPAWN_Y:
		queue_free()

func _on_body_entered(body: Node2D) -> void:
	if _has_hit:
		return
	
	if body is Player or body.has_method("take_damage") or body.has_method("hit"):
		_has_hit = true
		if body.has_method("take_damage"):
			body.take_damage(DAMAGE, self)
		elif body.has_method("hit"):
			body.hit()
		_spawn_crumble_effect()
		set_deferred("monitoring", false)
		set_deferred("monitorable", false)
		queue_free()

func take_damage(_amount: float, source: String = "bullet") -> bool:
	if source == "heavy_shockwave" or source == "charge_attack":
		_has_hit = true
		_spawn_crumble_effect()
		queue_free()
		return true
	return false

func _spawn_crumble_effect() -> void:
	if SHOT_CRUMBLE_EFFECT_SCENE == null:
		return
	
	var crumble: ShotCrumbleEffectClass = SHOT_CRUMBLE_EFFECT_SCENE.instantiate()
	var spawn_pos := global_position
	
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
		# Downward needle orientation (tip down): rotation PI
		crumble.setup(spawn_pos, CIRNO_CRUMBLE_TEXS, Vector2(1.5, 1.5), PI)
