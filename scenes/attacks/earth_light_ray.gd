class_name EarthLightRay
extends Node2D

# Marisa Kirisame's Extra Attack: Earth Light Ray (アースライトレイ)
# A vertical laser beam telegraphed with runic spell text, discharging with an
# expanding additive glow and an ascending pulse, then gracefully shrinking horizontally.

@export var telegraph_duration: float = 0.55
@export var fire_duration: float = 0.35
@export var collapse_duration: float = 0.25
@export var scroll_speed: float = 240.0
@export var tilt_angle_deg: float = 0.0
@export var damage: float = 1.5

@export_group("Primed Travel")
## Skip the rune telegraph and the collapse: the beam starts already firing, slides at
## `velocity` for `fire_duration` seconds and then frees itself. Used by Clownpiece's
## Inferno "Striped Abyss", whose beams slide in from a wall and leave by the other one.
@export var primed: bool = false
@export var velocity: Vector2 = Vector2.ZERO

@export_group("Colors")
@export var glow_color: Color = Color(0.3, 0.6, 1.0, 0.4)
@export var core_color: Color = Color(0.9, 0.96, 1.0, 0.95)
@export var flare_color: Color = Color(1.0, 1.0, 1.0, 1.0)
@export var warning_rune_color: Color = Color(1.0, 1.0, 1.0, 1.0)

@onready var beam_container: Node2D = $BeamContainer
@onready var warning_strip: Sprite2D = $BeamContainer/WarningStrip
@onready var glow_beam: ColorRect = $BeamContainer/GlowBeam
@onready var core_beam: ColorRect = $BeamContainer/CoreBeam
@onready var ascending_pulse: Sprite2D = $AscendingPulse
@onready var base_spark: Sprite2D = $BaseSpark
@onready var hitbox: Area2D = $Hitbox
@onready var collision_shape: CollisionShape2D = $Hitbox/CollisionShape2D

var _is_firing: bool = false
var is_firing: bool = false
var _main_tween: Tween = null

## Setup tilt angle and optional ground anchor X coordinate.
## If ground_anchor_x >= 0.0, positions the node so its base spark at local (0, 950)
## is anchored to (ground_anchor_x, 950.0) at the bottom screen border.
func setup_tilt(angle_deg: float, ground_anchor_x: float = -1.0) -> void:
	tilt_angle_deg = angle_deg
	var rot_rad: float = deg_to_rad(angle_deg)
	rotation = rot_rad
	if ground_anchor_x >= 0.0:
		# Base spark is at local (0.0, 950.0). In global space rotated by rot_rad:
		# spark_global = node_pos + Vector2(-950.0 * sin(rot_rad), 950.0 * cos(rot_rad))
		# Setting spark_global = Vector2(ground_anchor_x, 950.0) gives:
		var offset_x: float = 950.0 * sin(rot_rad)
		var offset_y: float = 950.0 * (1.0 - cos(rot_rad))
		position = Vector2(ground_anchor_x + offset_x, offset_y)

## Configure beam colors dynamically
func setup_colors(p_glow: Color, p_core: Color, p_flare: Color, p_rune: Color) -> void:
	glow_color = p_glow
	core_color = p_core
	flare_color = p_flare
	warning_rune_color = p_rune
	_apply_colors()

func _apply_colors() -> void:
	if glow_beam:
		glow_beam.color = Color(glow_color.r, glow_color.g, glow_color.b, glow_beam.color.a)
	if core_beam:
		core_beam.color = Color(core_color.r, core_color.g, core_color.b, core_beam.color.a)
	if warning_strip:
		warning_strip.modulate = Color(warning_rune_color.r, warning_rune_color.g, warning_rune_color.b, warning_strip.modulate.a)
	if ascending_pulse:
		ascending_pulse.modulate = Color(flare_color.r, flare_color.g, flare_color.b, ascending_pulse.modulate.a)
	if base_spark:
		base_spark.modulate = flare_color

func _ready() -> void:
	if tilt_angle_deg != 0.0 and is_zero_approx(rotation):
		setup_tilt(tilt_angle_deg)
	
	# Initial visual & collision states
	hitbox.monitoring = false
	hitbox.monitorable = false
	if collision_shape:
		collision_shape.disabled = true
	hitbox.body_entered.connect(_on_hitbox_body_entered)
	
	_apply_colors()
	warning_strip.modulate.a = 0.0
	core_beam.color.a = 0.0
	glow_beam.color.a = 0.0
	ascending_pulse.modulate.a = 0.0
	base_spark.scale = Vector2.ZERO
	beam_container.scale.x = 0.5

	if primed:
		_start_primed()
	else:
		_start_laser_sequence()

func _start_primed() -> void:
	_is_firing = true
	is_firing = true
	hitbox.monitoring = true
	hitbox.monitorable = true
	if collision_shape:
		collision_shape.disabled = false
	beam_container.scale.x = 0.9
	core_beam.color.a = 0.95
	glow_beam.color.a = 0.65
	warning_strip.modulate.a = 1.0
	base_spark.scale = Vector2(1.4, 1.4)
	_main_tween = create_tween()
	_main_tween.tween_interval(fire_duration)
	_main_tween.tween_callback(queue_free)

func _process(delta: float) -> void:
	if velocity != Vector2.ZERO:
		position += velocity * delta

	# Magic runes scroll vertically for authentic dynamic Danmaku flair
	if is_instance_valid(warning_strip):
		warning_strip.region_rect.position.y += scroll_speed * delta
	
	# Base spark rotates smoothly at the ground anchor
	if is_instance_valid(base_spark) and base_spark.scale.x > 0.05:
		base_spark.rotation += 10.0 * delta

func _start_laser_sequence() -> void:
	_main_tween = create_tween()
	
	# --- PHASE 1: TELEGRAPH (0.0s -> ~0.55s) ---
	# Base starburst flares up and runes fade in at warning opacity
	_main_tween.parallel().tween_property(base_spark, "scale", Vector2(1.0, 1.0), telegraph_duration * 0.4).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)
	_main_tween.parallel().tween_property(warning_strip, "modulate:a", 0.55, telegraph_duration * 0.7).set_ease(Tween.EASE_IN_OUT)
	_main_tween.parallel().tween_property(glow_beam, "color:a", 0.15, telegraph_duration)
	
	# Small warning pulse right before firing
	_main_tween.tween_interval(telegraph_duration * 0.3)
	
	# --- PHASE 2: BEAM DISCHARGE (FIRE) ---
	_main_tween.tween_callback(func():
		_is_firing = true
		is_firing = true
		AudioService.play_laser()
		hitbox.set_deferred("monitoring", true)
		hitbox.set_deferred("monitorable", true)
		if collision_shape:
			collision_shape.set_deferred("disabled", false)
		ascending_pulse.position = Vector2(0.0, 960.0)
		ascending_pulse.modulate.a = flare_color.a
	)
	
	# Horizontal expansion from thin to full blast (halved horizontally per feedback)
	_main_tween.parallel().tween_property(beam_container, "scale:x", 1.15, 0.08).set_ease(Tween.EASE_OUT)
	_main_tween.parallel().tween_property(core_beam, "color:a", 0.95, 0.05)
	_main_tween.parallel().tween_property(glow_beam, "color:a", 0.65, 0.05)
	_main_tween.parallel().tween_property(warning_strip, "modulate:a", 1.0, 0.05)
	_main_tween.parallel().tween_property(base_spark, "scale", Vector2(1.4, 1.4), 0.08)
	
	# Secondary effect: Ascending pulse rushes rapidly up the beam
	_main_tween.parallel().tween_property(ascending_pulse, "position:y", -120.0, fire_duration * 0.85).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	_main_tween.parallel().tween_property(ascending_pulse, "modulate:a", 0.0, fire_duration * 0.85).set_delay(fire_duration * 0.4)
	
	# Settle slightly after peak explosion
	_main_tween.tween_property(beam_container, "scale:x", 0.9, 0.1)
	_main_tween.tween_interval(fire_duration - 0.18)
	
	# --- PHASE 3: COLLAPSE & DISSIPATION ---
	_main_tween.tween_callback(func():
		_is_firing = false
		is_firing = false
		hitbox.set_deferred("monitoring", false)
		hitbox.set_deferred("monitorable", false)
		if collision_shape:
			collision_shape.set_deferred("disabled", true)
	)
	
	# Gracefully collapse horizontally to 0 and fade out
	_main_tween.parallel().tween_property(beam_container, "scale:x", 0.0, collapse_duration).set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	_main_tween.parallel().tween_property(core_beam, "color:a", 0.0, collapse_duration * 0.6)
	_main_tween.parallel().tween_property(glow_beam, "color:a", 0.0, collapse_duration)
	_main_tween.parallel().tween_property(warning_strip, "modulate:a", 0.0, collapse_duration)
	_main_tween.parallel().tween_property(base_spark, "scale", Vector2.ZERO, collapse_duration).set_ease(Tween.EASE_IN)
	
	# Cleanup
	_main_tween.tween_callback(queue_free)

func _on_hitbox_body_entered(body: Node2D) -> void:
	if not _is_firing:
		return
	if body is Player:
		if body.has_method("take_damage"):
			body.take_damage(damage, self)
		elif body.has_method("hit"):
			body.hit()

## The point of the beam nearest `p`, and how fast that point is moving, for the sparring AI
## when the beam is not upright (upright beams are scored as whole columns instead). Along
## the body the nearest point only closes in with the part of `velocity` across the beam;
## its two ends carry all of it. The beam spans local y -60 to 1000, like its hitbox.
func get_ai_hazard(p: Vector2) -> Dictionary:
	# Through the node's transform, so a beam stretched along its axis (scale.y) reports its
	# real length.
	var a: Vector2 = transform * Vector2(0.0, -60.0)
	var b: Vector2 = transform * Vector2(0.0, 1000.0)
	var ab: Vector2 = b - a
	var t: float = clampf((p - a).dot(ab) / ab.length_squared(), 0.0, 1.0)
	# Mid-beam, only the drift across the beam's axis brings it closer.
	var axis: Vector2 = ab.normalized()
	var vel: Vector2 = velocity if t <= 0.0 or t >= 1.0 else velocity - axis * velocity.dot(axis)
	return {"pos": a + ab * t, "vel": vel, "radius": 7.0}
