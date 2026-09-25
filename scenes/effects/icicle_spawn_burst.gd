class_name IcicleSpawnBurst
extends Node2D

## Spawn effect burst for Cirno's Shower of Icicles.
## Fades in quickly from transparent, becomes fully opaque, then fades out and despawns.

@onready var sprite: Sprite2D = $Sprite2D

func _ready() -> void:
	if sprite == null:
		sprite = get_node_or_null("Sprite2D")
	if sprite:
		sprite.modulate.a = 0.0
		sprite.scale = Vector2(0.6, 0.6)
		var tween := create_tween()
		# Phase 1: Quickly become opaque while expanding to normal scale
		tween.tween_property(sprite, "modulate:a", 1.0, 0.07).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		tween.parallel().tween_property(sprite, "scale", Vector2(1.0, 1.0), 0.07).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		# Phase 2: Quickly fade back to transparent and despawn
		tween.tween_property(sprite, "modulate:a", 0.0, 0.11).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
		tween.parallel().tween_property(sprite, "scale", Vector2(1.2, 1.2), 0.11).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
		tween.tween_callback(queue_free)
	else:
		queue_free()

