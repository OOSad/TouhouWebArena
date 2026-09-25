class_name YoumuDarkSpirit
extends Area2D

## Youmu's Extra Attack: Dark Spirit Phantom
## An indestructible stationary dark spirit obstacle that appears flat across the opponent's
## baseline/movement area. Damages the player on contact and dissolves cleanly on timeout.

const FRAME_DURATION: float = 0.08

var is_dark_spirit: bool = true

@export var rank: int = 1:
	set(val):
		rank = clampi(val, 1, 22)
		_calculate_lifetime()

var lifetime: float = 1.0
var _elapsed: float = 0.0
var _anim_timer: float = 0.0
var _current_frame: int = 0
var _is_despawning: bool = false

var sprite: Sprite2D = null
var collision_shape: CollisionShape2D = null

func _ensure_nodes() -> void:
	if sprite == null:
		sprite = get_node_or_null("Sprite2D")
	if collision_shape == null:
		collision_shape = get_node_or_null("CollisionShape2D")

func _ready() -> void:
	_ensure_nodes()
	collision_layer = 2 # Layer 2 = enemy_bullets / hazards
	collision_mask = 1  # Layer 1 = player
	_calculate_lifetime()
	
	body_entered.connect(_on_body_entered)
	
	# Audio cue on manifestation
	AudioService.play_dark_spirit_spawn()
	
	# Spawn scale pop
	scale = Vector2(0.2, 0.2)
	var tween := create_tween()
	tween.tween_property(self, "scale", Vector2.ONE, 0.15).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)

## Allows external callers (Playfield, DanmakuExtraAttackStep) to set the rank (1..22)
func setup_rank(p_rank: int) -> void:
	rank = p_rank

func _calculate_lifetime() -> void:
	var t: float = clampf(float(rank - 1) / 21.0, 0.0, 1.0)
	lifetime = lerpf(1.0, 2.0, t)

func _physics_process(delta: float) -> void:
	if _is_despawning:
		return
	
	_ensure_nodes()
	_elapsed += delta
	
	# Animate pulsating dark spirit frames
	_anim_timer += delta
	if _anim_timer >= FRAME_DURATION:
		_anim_timer -= FRAME_DURATION
		_current_frame = (_current_frame + 1) % 4
		if sprite:
			sprite.frame = _current_frame
	
	# Check despawn
	if _elapsed >= lifetime:
		despawn()

## Clean dissolution: dissolves / fades out with no bullets emitted
func despawn() -> void:
	if _is_despawning:
		return
	_is_despawning = true
	_ensure_nodes()
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	var fade_tween := create_tween()
	fade_tween.tween_property(self, "modulate:a", 0.0, 0.2).set_ease(Tween.EASE_IN)
	fade_tween.parallel().tween_property(self, "scale", Vector2(1.25, 1.25), 0.2).set_ease(Tween.EASE_OUT)
	fade_tween.finished.connect(queue_free)

## Indestructible obstacle: player bullets, slashes, and shockwaves cannot destroy it
func take_damage(_amount: float, _source: String = "bullet") -> bool:
	return false

func _on_body_entered(body: Node2D) -> void:
	if _is_despawning:
		return
	if body.has_method("take_damage"):
		body.take_damage(1.0, self)
	elif body.has_method("hit"):
		body.hit()
