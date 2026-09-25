class_name KnifeExplosionBurst
extends Node2D

## Spawn effect burst for Sakuya's Reflective Knife Explosion - marks the point each dagger
## circle bursts outward from. Fades in quickly, slowly shrinks down to its (4.5x native size)
## resting scale, then fades out and despawns the moment it settles - no static hold in between,
## the shrink itself is the lingering motion.

## Resting scale once the shrink-in finishes - 4.5x the sprite's native pixel size so the effect
## reads clearly at gameplay zoom instead of getting lost next to the daggers.
const REST_SCALE: Vector2 = Vector2(4.5, 4.5)
const START_SCALE: Vector2 = Vector2(9.9, 9.9)
const FADE_IN_DURATION: float = 0.15
## Slowed way down per user feedback - a static hold at rest scale read as sitting there
## awkwardly, so the shrink itself now carries the effect's whole lingering presence instead.
const SHRINK_DURATION: float = 1.0
const FADE_OUT_DURATION: float = 0.15

@onready var sprite: Sprite2D = $Sprite2D

func _ready() -> void:
	if sprite == null:
		sprite = get_node_or_null("Sprite2D")
	if sprite:
		sprite.modulate.a = 0.0
		sprite.scale = START_SCALE
		var tween := create_tween()
		# Phase 1: Fade in quickly, then keep slowly shrinking down to resting scale
		tween.tween_property(sprite, "modulate:a", 1.0, FADE_IN_DURATION).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		tween.parallel().tween_property(sprite, "scale", REST_SCALE, SHRINK_DURATION).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		# Phase 2: Fade out immediately upon settling, then despawn
		tween.tween_property(sprite, "modulate:a", 0.0, FADE_OUT_DURATION).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
		tween.tween_callback(queue_free)
	else:
		queue_free()
