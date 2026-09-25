class_name SparkAura
extends Node2D

## Impact burst for Reisen's Level 1 Charge Attack "Mind Explosion".
##
## Timings and proportions are taken from reference footage (60fps): the burst runs about
## 1.1s, the red disc grows the whole way from roughly 88px to 134px radius, and the white
## outline starts as a near-clean circle and works itself up into a violent jagged
## scribble before the red breaks apart and fades.
##
## All the drawing is in shaders/spark_aura.gdshader on a single quad - see the note there
## about why this is not built out of _draw() geometry.
##
## Damage is dealt to everything inside `blast_radius` on the frame it appears, and again
## every `tick_interval` while it lives when that is set.

## Margin beyond the widest the burst can draw, covering the outline's ring thickness and
## its anti-aliasing. The quad is fitted to the radii at spawn rather than being a fixed size
## in the scene, so a caller that asks for a smaller burst pays for a smaller quad.
##
## This matters more than it looks. The fragment shader is not cheap - nine hashed sines and
## an atan per pixel, for four jittering loop rings - and the quad is pure fill cost, so any
## slack in it is paid for on every pixel of every frame the burst is alive. A fixed quad
## sized for the largest caller made the moon craters, which are smaller, carry the
## difference three at a time. This is a browser game first, so that slack is worth removing.
const QUAD_MARGIN: float = 10.0

## Half width of the quad, computed at spawn. Radii are converted into the shader's UV units
## against this, so it must always match the ColorRect's actual half size.
var _quad_half: float = 210.0

@export var duration: float = 1.10
@export var radius_start: float = 88.0
@export var radius_end: float = 134.0
## How far the white outline strays from the disc edge, at the start and at its worst.
@export var jitter_start: float = 2.0
@export var jitter_peak: float = 46.0
@export var blast_radius: float = 110.0
@export var damage: float = 1.2
## Seconds between repeat hits while the burst is alive. 0 hits once, on the first frame.
@export var tick_interval: float = 0.0

var _elapsed: float = 0.0
var _playfield: Node2D = null
var _dealt: bool = false
var _tick_timer: float = 0.0
var _mat: ShaderMaterial = null
var _radius: float = 0.0

## Radius the disc is drawn at this frame. Exposed so a caller using the burst purely as a
## visual - Reisen's moon craters do - can size a hitbox off it instead of re-deriving the
## growth curve and drifting out of sync with it.
func get_current_radius() -> float:
	return _radius

@onready var rect: ColorRect = $ColorRect

func setup(spawn_pos: Vector2, playfield: Node2D, p_damage: float = 1.2) -> void:
	position = spawn_pos
	_playfield = playfield
	damage = p_damage

func _ready() -> void:
	_fit_quad()
	if rect and rect.material:
		# Local copy so several bursts flicker independently.
		rect.material = rect.material.duplicate()
		_mat = rect.material as ShaderMaterial
		_mat.set_shader_parameter("seed", randf() * 100.0)
	_apply(0.0)
	AudioService.play_sfx("se_lazer00")
	_deal_blast_damage()

## Sizes the quad to the widest this particular burst will ever draw: its final radius plus
## the worst the outline strays off it, plus a little for the ring's own thickness. Callers
## set the radii before this node enters the tree, so by here they are known.
func _fit_quad() -> void:
	if rect == null:
		return
	_quad_half = maxf(radius_start, radius_end) + jitter_peak + QUAD_MARGIN
	rect.offset_left = -_quad_half
	rect.offset_top = -_quad_half
	rect.offset_right = _quad_half
	rect.offset_bottom = _quad_half

func _process(delta: float) -> void:
	_elapsed += delta
	var progress: float = clampf(_elapsed / duration, 0.0, 1.0)
	_apply(progress)
	if progress >= 1.0:
		queue_free()
		return
	if tick_interval > 0.0:
		_tick_timer += delta
		while _tick_timer >= tick_interval:
			_tick_timer -= tick_interval
			_hit_everything_in_radius()

func _apply(progress: float) -> void:
	if _mat == null:
		return

	# The disc grows over the whole life, quickly at first.
	var grow: float = 1.0 - pow(1.0 - progress, 2.0)
	var radius: float = lerpf(radius_start, radius_end, grow)
	_radius = radius

	# The outline is nearly a clean circle to begin with and tears itself apart later.
	var rage: float = smoothstep(0.25, 0.85, progress)
	var jitter: float = lerpf(jitter_start, jitter_peak, rage)

	# In the original the red stays bright underneath the scribble and only breaks up
	# near the very end, so both of these are deliberately late. The white outlives it.
	var disc_alpha: float = 1.0 - smoothstep(0.72, 1.0, progress)
	var spark_alpha: float = 1.0 - smoothstep(0.86, 1.0, progress)

	_mat.set_shader_parameter("disc_radius", radius / _quad_half)
	_mat.set_shader_parameter("jitter_amount", jitter / _quad_half)
	_mat.set_shader_parameter("disc_alpha", disc_alpha)
	_mat.set_shader_parameter("spark_alpha", spark_alpha)
	_mat.set_shader_parameter("mottle", smoothstep(0.62, 1.0, progress))

## Catches everything standing in the blast the moment it goes off. The bullet itself has
## already damaged whatever it struck, so that target simply takes this as a second hit.
func _deal_blast_damage() -> void:
	if _dealt:
		return
	_dealt = true
	_hit_everything_in_radius()

func _hit_everything_in_radius() -> void:
	var entities: Node2D = _get_entities()
	if entities == null:
		return
	var r_sq: float = blast_radius * blast_radius
	for child in entities.get_children():
		if not is_instance_valid(child) or not (child is Area2D):
			continue
		if child.is_queued_for_deletion():
			continue
		if "is_dead" in child and child.is_dead:
			continue
		if global_position.distance_squared_to(child.global_position) > r_sq:
			continue
		if child.has_method("take_damage"):
			child.take_damage(damage, "charge_attack")

func _get_entities() -> Node2D:
	if _playfield == null or not is_instance_valid(_playfield):
		return null
	if _playfield.has_node("%Entities"):
		return _playfield.get_node("%Entities")
	if _playfield.has_node("Entities"):
		return _playfield.get_node("Entities")
	return null
