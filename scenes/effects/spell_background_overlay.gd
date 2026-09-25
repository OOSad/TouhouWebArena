class_name SpellBackgroundOverlay
extends Control

## Level 4 Spellcard Background Graphics Overlay.
## Renders authentic two-layer background effects (static base + animated overlay)
## during Level 4 Boss spellcard declarations.

@onready var base_rect: TextureRect = $BaseRect
@onready var anim_rotate: Sprite2D = $AnimRotate
@onready var anim_rotate_base: Sprite2D = $AnimRotateBase
@onready var anim_scroll: TextureRect = $AnimScroll
@onready var anim_counter_scroll: TextureRect = $AnimCounterScroll

const ROTATION_SPEED_RAD: float = -0.4712389 # -0.007853982 rad/frame * 60 FPS (~ -27 deg/s)
const ROTATION_SPEED_RAD_BASE: float = -ROTATION_SPEED_RAD # opposite direction, for the bottom layer in "dual_rotate" mode
## "aligned_rotate" (Reisen): PoFV draws both layers at scale 2.0804 on the same centre, so each
## texture is sized to 256 x 2.0804 x 600/288 px however many pixels it has, and both turn together
const ALIGNED_LAYER_SIZE: float = 256.0 * 2.0804145 * 600.0 / 288.0
## "scroll_rotate" (Yuuka): cdbg09b tiles the field scrolling up 0.0033 of a texture a frame
## (pl09.anm script 13) under cdbg09, drawn additive at the aligned size and turning (script 14)
const SCROLL_ROTATE_SPEED: Vector2 = Vector2(0.0, 0.0033333334 * 60.0)
## "twin_rotate" (Clownpiece, LoLK st05enm.anm scripts 11-13): cdbg05a00 sits still under two
## copies of cdbg05b00 multiplied in at half strength (LoLK's alpha 128), scaled 1.4 and 1.5
## (TH15 units, x 2.143 here), centred 192 units down, turning the same way at 0.0017 and
## 0.0052 rad a frame. Multiply was matched against the footage: additive washed the vines
## out pink, normal blending fogged them, subtractive turned the reds green; multiply gives
## the dark vines, maroon edges and yellow core LoLK shows.
## The field is taller than LoLK's, so the base keeps its shape and covers it.
const TWIN_MULTIPLY: ShaderMaterial = preload("res://scenes/effects/spell_background_multiply.tres")
const TWIN_CENTRE: Vector2 = Vector2(300.0, 192.0 * 2.143)
const TWIN_SLOW_SCALE: float = 1.4 * 2.143
const TWIN_FAST_SCALE: float = 1.5 * 2.143
const TWIN_SLOW_SPEED: float = 0.0017453292 * 60.0
const TWIN_FAST_SPEED: float = 0.0052359877 * 60.0
const ADDITIVE: CanvasItemMaterial = preload("res://scenes/effects/spell_background_additive.tres")
const FADE_IN_DURATION: float = 1.0 # 60 frames in PoFV
const DEFAULT_FADE_OUT_DURATION: float = 0.8

var _is_rotating: bool = false
var _is_rotating_base: bool = false
var _base_rotation_speed: float = ROTATION_SPEED_RAD_BASE
var _top_rotation_speed: float = ROTATION_SPEED_RAD
var _default_rotate_position: Vector2 = Vector2.ZERO
var _default_base_stretch: TextureRect.StretchMode = TextureRect.STRETCH_SCALE
# Scene sizes, restored for every other mode after "aligned_rotate" resizes the layers
var _default_rotate_scale: Vector2 = Vector2.ONE
var _default_rotate_base_scale: Vector2 = Vector2.ONE
var _default_scroll_speed: Vector2 = Vector2.ZERO
var _active_tween: Tween = null
var _current_char_id: String = ""

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	if anim_rotate:
		_default_rotate_scale = anim_rotate.scale
	if anim_rotate:
		_default_rotate_position = anim_rotate.position
	if anim_rotate_base:
		_default_rotate_base_scale = anim_rotate_base.scale
	if base_rect:
		_default_base_stretch = base_rect.stretch_mode
	# Each field gets its own copy: modes set the scroll speed, and the two fields can be
	# showing different characters' backgrounds at once
	if anim_scroll and anim_scroll.material:
		anim_scroll.material = anim_scroll.material.duplicate()
		_default_scroll_speed = anim_scroll.material.get_shader_parameter("scroll_speed")
	modulate.a = 0.0
	visible = false

func _process(delta: float) -> void:
	if _is_rotating and anim_rotate and anim_rotate.visible:
		anim_rotate.rotation += _top_rotation_speed * delta
	if _is_rotating_base and anim_rotate_base and anim_rotate_base.visible:
		anim_rotate_base.rotation += _base_rotation_speed * delta

## Activates the spellcard background for the given character.
func activate(char_data: CharacterData) -> void:
	if char_data == null:
		return
	
	_current_char_id = char_data.character_id
	_base_rotation_speed = ROTATION_SPEED_RAD_BASE
	_top_rotation_speed = ROTATION_SPEED_RAD
	if base_rect:
		base_rect.stretch_mode = _default_base_stretch
	for layer in [anim_rotate, anim_rotate_base]:
		if layer:
			layer.position = _default_rotate_position
			layer.material = null
	if anim_rotate:
		anim_rotate.scale = _default_rotate_scale
	if anim_rotate_base:
		anim_rotate_base.scale = _default_rotate_base_scale
	if anim_rotate:
		anim_rotate.material = null
	if anim_scroll and anim_scroll.material:
		anim_scroll.material.set_shader_parameter("scroll_speed", _default_scroll_speed)

	# Configure static base layer
	if base_rect:
		if char_data.spell_bg_base_texture and char_data.spell_bg_anim_type != "counter_scroll" and char_data.spell_bg_anim_type != "dual_rotate" and char_data.spell_bg_anim_type != "aligned_rotate" and char_data.spell_bg_anim_type != "scroll_rotate":
			base_rect.texture = char_data.spell_bg_base_texture
			base_rect.visible = true
		else:
			base_rect.visible = false

	# Configure animated top layer
	# "rotate_reverse" (Aya, pl10.anm script 12): the same layers, turning the other way
	if char_data.spell_bg_anim_type == "rotate" or char_data.spell_bg_anim_type == "rotate_reverse":
		if char_data.spell_bg_anim_type == "rotate_reverse":
			_top_rotation_speed = -ROTATION_SPEED_RAD
		if anim_rotate:
			anim_rotate.texture = char_data.spell_bg_anim_texture
			anim_rotate.visible = char_data.spell_bg_anim_texture != null
			anim_rotate.rotation = 0.0
			anim_rotate.modulate.a = 1.0
			_is_rotating = anim_rotate.visible
		if anim_rotate_base:
			anim_rotate_base.visible = false
			_is_rotating_base = false
		if anim_scroll:
			anim_scroll.visible = false
		if anim_counter_scroll:
			anim_counter_scroll.visible = false
	elif char_data.spell_bg_anim_type == "dual_rotate":
		# Two independently counter-rotating full-screen layers (e.g. Sakuya's clock over distortion)
		if anim_rotate_base:
			anim_rotate_base.texture = char_data.spell_bg_base_texture
			anim_rotate_base.visible = char_data.spell_bg_base_texture != null
			anim_rotate_base.rotation = 0.0
			anim_rotate_base.modulate.a = 1.0
			_is_rotating_base = anim_rotate_base.visible
		if anim_rotate:
			anim_rotate.texture = char_data.spell_bg_anim_texture
			anim_rotate.visible = char_data.spell_bg_anim_texture != null
			anim_rotate.rotation = 0.0
			anim_rotate.modulate.a = 0.5
			_is_rotating = anim_rotate.visible
		if anim_scroll:
			anim_scroll.visible = false
		if anim_counter_scroll:
			anim_counter_scroll.visible = false
	elif char_data.spell_bg_anim_type == "aligned_rotate":
		# Two stacked layers on one centre, turning together so they never drift out of line
		# (Reisen's moon over its swirl)
		for pair in [[anim_rotate_base, char_data.spell_bg_base_texture], [anim_rotate, char_data.spell_bg_anim_texture]]:
			var layer: Sprite2D = pair[0]
			var tex: Texture2D = pair[1]
			if layer:
				layer.texture = tex
				layer.visible = tex != null
				layer.rotation = 0.0
				layer.modulate.a = 1.0
				if tex:
					layer.scale = Vector2.ONE * (ALIGNED_LAYER_SIZE / float(tex.get_width()))
		_is_rotating_base = anim_rotate_base != null and anim_rotate_base.visible
		_is_rotating = anim_rotate != null and anim_rotate.visible
		_base_rotation_speed = ROTATION_SPEED_RAD
		if anim_scroll:
			anim_scroll.visible = false
		if anim_counter_scroll:
			anim_counter_scroll.visible = false
	elif char_data.spell_bg_anim_type == "scroll_rotate":
		# A slowly scrolling field under one additive layer turning on the centre (Yuuka's
		# sunflower over the sunflower field)
		if anim_scroll:
			anim_scroll.texture = char_data.spell_bg_base_texture
			anim_scroll.visible = char_data.spell_bg_base_texture != null
			if anim_scroll.material:
				anim_scroll.material.set_shader_parameter("scroll_speed", SCROLL_ROTATE_SPEED)
		if anim_rotate:
			var tex: Texture2D = char_data.spell_bg_anim_texture
			anim_rotate.texture = tex
			anim_rotate.visible = tex != null
			anim_rotate.rotation = 0.0
			anim_rotate.modulate.a = 1.0
			anim_rotate.material = ADDITIVE
			if tex:
				anim_rotate.scale = Vector2.ONE * (ALIGNED_LAYER_SIZE / float(tex.get_width()))
			_is_rotating = anim_rotate.visible
		if anim_rotate_base:
			anim_rotate_base.visible = false
			_is_rotating_base = false
		if anim_counter_scroll:
			anim_counter_scroll.visible = false
	elif char_data.spell_bg_anim_type == "twin_rotate":
		if base_rect and base_rect.visible:
			base_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_COVERED
		for spec in [[anim_rotate_base, TWIN_SLOW_SCALE], [anim_rotate, TWIN_FAST_SCALE]]:
			var layer: Sprite2D = spec[0]
			if layer:
				layer.texture = char_data.spell_bg_anim_texture
				layer.visible = char_data.spell_bg_anim_texture != null
				layer.rotation = 0.0
				layer.modulate.a = 0.5
				layer.material = TWIN_MULTIPLY
				layer.position = TWIN_CENTRE
				layer.scale = Vector2(spec[1], spec[1])
		_is_rotating_base = anim_rotate_base != null and anim_rotate_base.visible
		_is_rotating = anim_rotate != null and anim_rotate.visible
		_base_rotation_speed = TWIN_SLOW_SPEED
		_top_rotation_speed = TWIN_FAST_SPEED
		if anim_scroll:
			anim_scroll.visible = false
		if anim_counter_scroll:
			anim_counter_scroll.visible = false
	elif char_data.spell_bg_anim_type == "scroll_up":
		if anim_scroll:
			anim_scroll.texture = char_data.spell_bg_anim_texture
			anim_scroll.visible = char_data.spell_bg_anim_texture != null
		if anim_rotate:
			anim_rotate.visible = false
			_is_rotating = false
		if anim_rotate_base:
			anim_rotate_base.visible = false
			_is_rotating_base = false
		if anim_counter_scroll:
			anim_counter_scroll.visible = false
	elif char_data.spell_bg_anim_type == "counter_scroll":
		if anim_counter_scroll:
			anim_counter_scroll.texture = char_data.spell_bg_base_texture
			anim_counter_scroll.visible = char_data.spell_bg_base_texture != null
		if anim_rotate:
			anim_rotate.visible = false
			_is_rotating = false
		if anim_rotate_base:
			anim_rotate_base.visible = false
			_is_rotating_base = false
		if anim_scroll:
			anim_scroll.visible = false
	else:
		if anim_rotate:
			anim_rotate.visible = false
		if anim_rotate_base:
			anim_rotate_base.visible = false
		if anim_scroll:
			anim_scroll.visible = false
		if anim_counter_scroll:
			anim_counter_scroll.visible = false
		_is_rotating = false
		_is_rotating_base = false
	
	# Smooth fade-in
	visible = true
	if _active_tween and _active_tween.is_valid():
		_active_tween.kill()
	
	_active_tween = create_tween()
	_active_tween.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	_active_tween.tween_property(self, "modulate:a", 1.0, FADE_IN_DURATION).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

## Smoothly fades out and disables the spellcard background overlay.
func deactivate(duration: float = DEFAULT_FADE_OUT_DURATION) -> void:
	if not visible and modulate.a <= 0.0:
		return
	
	if _active_tween and _active_tween.is_valid():
		_active_tween.kill()
	
	_active_tween = create_tween()
	_active_tween.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	_active_tween.tween_property(self, "modulate:a", 0.0, duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	_active_tween.tween_callback(func() -> void:
		visible = false
		_is_rotating = false
		_is_rotating_base = false
		_current_char_id = ""
	)
