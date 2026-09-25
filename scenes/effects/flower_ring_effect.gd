class_name FlowerRingEffect
extends Node2D

## 5-Point Flower spellcard pre-cast visual. Manifests as a 5-pointed runic flower that
## smoothly rotates and shrinks down to the centre before the bullets burst out of that same
## point, so it reads as the cast gathering rather than as decoration laid over it.
##
## Shared, unmodified, by Cirno's Perfect Freeze (Lv2 & Lv3) and Reisen's Lunar Wave (Lv2 &
## Lv3). Every Touhou spellcast flower has five petals, so the count is fixed in the shader
## and is deliberately not a per-caster dial. Only the opening radius, colour and duration
## differ between callers.

@export var duration: float = 0.65
@export var start_radius: float = 150.0
@export var end_radius: float = 24.0
@export var lobe_depth: float = 0.32
@export var band_width: float = 144.0
@export var rotation_speed: float = 3.2
@export var theme_color: Color = Color(0.3, 0.85, 1.0, 1.0)

var _elapsed: float = 0.0
var _rotation_angle: float = 0.0

@onready var rect: ColorRect = $ColorRect

## The opening radius is optional so Cirno's existing three-argument call keeps working
## untouched; below zero means "leave the scene's value alone".
func setup(spawn_pos: Vector2, p_duration: float = 0.65, p_color: Color = Color(0.3, 0.85, 1.0, 1.0), p_start_radius: float = -1.0) -> void:
	position = spawn_pos
	duration = p_duration
	theme_color = p_color
	if p_start_radius > 0.0:
		start_radius = p_start_radius
	if rect and rect.material:
		(rect.material as ShaderMaterial).set_shader_parameter("color_tint", theme_color)

func _ready() -> void:
	if rect and rect.material:
		rect.material = rect.material.duplicate()
		(rect.material as ShaderMaterial).set_shader_parameter("color_tint", theme_color)
		(rect.material as ShaderMaterial).set_shader_parameter("lobe_depth", lobe_depth)
		(rect.material as ShaderMaterial).set_shader_parameter("band_width", band_width)
	_update_visual(start_radius, 0.0)

func _process(delta: float) -> void:
	_elapsed += delta
	var progress: float = clampf(_elapsed / duration, 0.0, 1.0)
	_rotation_angle += rotation_speed * delta

	# Smooth shrink curve (accelerates slightly inward toward the center)
	var t_shrink: float = progress * progress
	var current_radius: float = lerpf(start_radius, end_radius, t_shrink)

	# Fade in at the very start (first 10%), fade out at the very end (last 10%)
	var alpha: float = 1.0
	if progress < 0.1:
		alpha = progress / 0.1
	elif progress > 0.9:
		alpha = (1.0 - progress) / 0.1

	_update_visual(current_radius, alpha)

	if _elapsed >= duration:
		queue_free()

func _update_visual(r: float, alpha: float = 1.0) -> void:
	if rect == null:
		return
	var current_band: float = minf(band_width, maxf(16.0, r * 1.0))
	var max_r: float = r * (1.0 + lobe_depth) + current_band * 0.5 + 8.0
	rect.position = Vector2(-max_r, -max_r)
	rect.size = Vector2(2.0 * max_r, 2.0 * max_r)

	var mat := rect.material as ShaderMaterial
	if mat:
		mat.set_shader_parameter("base_radius", r)
		mat.set_shader_parameter("band_width", current_band)
		mat.set_shader_parameter("rotation_angle", _rotation_angle)
		mat.set_shader_parameter("color_tint", Color(theme_color.r, theme_color.g, theme_color.b, theme_color.a * alpha))

