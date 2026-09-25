class_name SpawnFlash
extends Node2D

## A short red flash marking the point a bullet was born at. Pops in while growing, then
## fades out while still growing, so it reads as a muzzle flash rather than a hazard.
##
## It draws at `z_index = 11` absolute, one above the 10 every bullet pins itself to, so it
## covers the bullets rather than sitting under them. That is the whole job: the flash is
## there to hide bullets popping into existence out of nothing, and underneath them it hides
## nothing at all. `KnifeExplosionBurst` sits at the same index for the same reason.
##
## Reisen's Level 4 uses it twice over, at very different cadences: one per ring on Rings of
## Red Bullets (8 in a cast, 0.28s apart) and one per bullet on the Spiral Machinegun (~115 in
## a cast, ~53 a second). Everything is exported so the two can diverge, but they deliberately
## share the same numbers today - it is the same flash, only the rate differs.
##
## The sprite is `tex_knife_explosion_burst.tres`: PoFV's red sparkle burst (etama.anm sprite 147),
## filled from the player's th09.dat at startup by BulletSprites. Sakuya's `KnifeExplosionBurst` draws the same
## region; only the timing and scale differ, hers being a slow 1.15s bloom at 4.5-9.9x against
## this one's 0.18s at around 2x.
##
## `IcicleSpawnBurst` and `KnifeExplosionBurst` are this same shape with their numbers baked in
## as constants. Folding all three into this one is worth doing, but is deliberately left for
## its own pass rather than smuggled in here.

## Lifetime is the binding constraint on the machinegun, not the look: at ~53 shots a second a
## 0.18s flash keeps about ten alive at once, and each one is additive fill. Raising it is
## paid for linearly across the whole cast.
@export var fade_in_duration: float = 0.07
@export var fade_out_duration: float = 0.11
## Scales, as a multiple of the sprite's 30x30 native size (PoFV's sparkle, 26px visible).
@export var start_scale: float = 1.2
@export var peak_scale: float = 2.0
@export var end_scale: float = 2.4

@onready var sprite: Sprite2D = $Sprite2D

## Called before `add_child()`, because the tween is built in `_ready()` off these values.
func setup(spawn_pos: Vector2, p_peak_scale: float = -1.0, p_fade_in: float = -1.0, p_fade_out: float = -1.0) -> void:
	position = spawn_pos
	if p_peak_scale > 0.0:
		# The opening and closing scales are kept in proportion to the peak, so a caller
		# asking for a bigger flash gets the same motion rather than a differently shaped one.
		var ratio: float = p_peak_scale / peak_scale
		start_scale *= ratio
		end_scale *= ratio
		peak_scale = p_peak_scale
	if p_fade_in > 0.0:
		fade_in_duration = p_fade_in
	if p_fade_out > 0.0:
		fade_out_duration = p_fade_out

func _ready() -> void:
	if sprite == null:
		sprite = get_node_or_null("Sprite2D")
	if sprite == null:
		queue_free()
		return

	sprite.modulate.a = 0.0
	sprite.scale = Vector2(start_scale, start_scale)

	var tween := create_tween()
	tween.tween_property(sprite, "modulate:a", 1.0, fade_in_duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(sprite, "scale", Vector2(peak_scale, peak_scale), fade_in_duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tween.tween_property(sprite, "modulate:a", 0.0, fade_out_duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	tween.parallel().tween_property(sprite, "scale", Vector2(end_scale, end_scale), fade_out_duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	tween.tween_callback(queue_free)
