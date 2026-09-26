class_name MainMenuTitleScreen
extends Node

## The main menu's animated title: the layered fade-in on boot (background, ring, the two
## duelist silhouettes gliding in, then the menu) and the silhouettes swapping every few seconds.

const CHAR_CYCLE_INTERVAL: float = 3.0
const GLIDE_OFFSET: float = 180.0
const MENU_CHARACTERS_DIR: String = "res://assets/ui/menu_characters"

var intro_active: bool = true

var _intro_tween: Tween = null
var _char_cycle_timer: float = 0.0
var _textures: Array[Texture2D] = []
var _current_left_tex: Texture2D = null
var _current_right_tex: Texture2D = null
var _left_active_is_a: bool = true
var _right_active_is_a: bool = true
var _cycle_left_turn: bool = true

var _background_tex: TextureRect
var _center_ring: TextureRect
var _left_char_slot: Control
var _left_char_a: TextureRect
var _left_char_b: TextureRect
var _right_char_slot: Control
var _right_char_a: TextureRect
var _right_char_b: TextureRect
var _dark_scrim: ColorRect
var _margin_container: MarginContainer

func setup(menu: Control) -> void:
	_background_tex = menu.get_node_or_null("%BackgroundTex")
	_center_ring = menu.get_node_or_null("%CenterRing")
	_left_char_slot = menu.get_node_or_null("%LeftCharSlot")
	_left_char_a = menu.get_node_or_null("%LeftCharA")
	_left_char_b = menu.get_node_or_null("%LeftCharB")
	_right_char_slot = menu.get_node_or_null("%RightCharSlot")
	_right_char_a = menu.get_node_or_null("%RightCharA")
	_right_char_b = menu.get_node_or_null("%RightCharB")
	_dark_scrim = menu.get_node_or_null("%DarkScrim")
	_margin_container = menu.get_node_or_null("%MarginContainer")
	_load_character_textures()
	_start_bootup_sequence()

func _process(delta: float) -> void:
	if intro_active or _textures.is_empty():
		return
	_char_cycle_timer += delta
	if _char_cycle_timer >= CHAR_CYCLE_INTERVAL:
		_char_cycle_timer = 0.0
		_cycle_slot(_cycle_left_turn)
		_cycle_left_turn = not _cycle_left_turn

func _load_character_textures() -> void:
	_textures.clear()
	for file_name in DirAccess.get_files_at(MENU_CHARACTERS_DIR):
		# Accept both .png and .png.import exported formats
		var clean_name := file_name.trim_suffix(".import")
		if clean_name.ends_with(".png"):
			var full_path := MENU_CHARACTERS_DIR.path_join(clean_name)
			if ResourceLoader.exists(full_path):
				var tex := load(full_path) as Texture2D
				if tex and not _textures.has(tex):
					_textures.append(tex)

	# Fallback if folder empty
	if _textures.is_empty():
		if _left_char_a and _left_char_a.texture:
			_textures.append(_left_char_a.texture)
		if _right_char_a and _right_char_a.texture:
			_textures.append(_right_char_a.texture)

func _get_texture_natural_facing(tex: Texture2D) -> int:
	if not tex:
		return 1
	if "marisa" in tex.resource_path.to_lower():
		return -1
	# Reimu and default characters face right (+1)
	return 1

func _apply_character_to_rect(rect: TextureRect, tex: Texture2D, is_left: bool) -> void:
	if not rect:
		return
	rect.texture = tex
	# Left slot must face toward the central ring (RIGHT = +1)
	# Right slot must face toward the central ring (LEFT = -1)
	var target_facing := 1 if is_left else -1
	rect.flip_h = (_get_texture_natural_facing(tex) != target_facing)

	var path_lower := tex.resource_path.to_lower() if tex else ""
	var rect_w := rect.size.x if rect.size.x > 0.0 else 660.0
	var rect_h := rect.size.y if rect.size.y > 0.0 else 960.0
	if "marisa" in path_lower:
		# Scale Marisa 1.2x bigger, pivoting slightly below center so feet and hat balance naturally
		rect.pivot_offset = Vector2(rect_w * 0.5, rect_h * 0.55)
		rect.scale = Vector2(1.2, 1.2)
	else:
		rect.pivot_offset = Vector2(rect_w * 0.5, rect_h * 0.5)
		rect.scale = Vector2.ONE

func _start_bootup_sequence() -> void:
	intro_active = true

	# Initial opacities and positions
	if _background_tex:
		_background_tex.modulate.a = 0.0
	if _center_ring:
		_center_ring.modulate.a = 0.0
		_center_ring.scale = Vector2.ONE
	if _left_char_slot:
		_left_char_slot.modulate.a = 1.0
	if _right_char_slot:
		_right_char_slot.modulate.a = 1.0
	if _dark_scrim:
		_dark_scrim.modulate.a = 0.0
	if _margin_container:
		_margin_container.modulate.a = 0.0

	# Select starting textures (Reimu on left, Marisa on right by default)
	var reimu_tex: Texture2D = null
	var marisa_tex: Texture2D = null
	for tex in _textures:
		var p_lower := tex.resource_path.to_lower()
		if "reimu" in p_lower:
			reimu_tex = tex
		elif "marisa" in p_lower:
			marisa_tex = tex

	if reimu_tex and marisa_tex:
		_current_left_tex = reimu_tex
		_current_right_tex = marisa_tex
	elif _textures.size() >= 2:
		_current_left_tex = _textures[0]
		_current_right_tex = _textures[_textures.size() - 1]
	elif _textures.size() == 1:
		_current_left_tex = _textures[0]
		_current_right_tex = _textures[0]

	if _left_char_a and _current_left_tex:
		_apply_character_to_rect(_left_char_a, _current_left_tex, true)
		_left_char_a.modulate.a = 0.0
		_left_char_a.position.x = -GLIDE_OFFSET
	if _left_char_b:
		_left_char_b.modulate.a = 0.0
		_left_char_b.position.x = 0.0

	if _right_char_a and _current_right_tex:
		_apply_character_to_rect(_right_char_a, _current_right_tex, false)
		_right_char_a.modulate.a = 0.0
		_right_char_a.position.x = GLIDE_OFFSET
	if _right_char_b:
		_right_char_b.modulate.a = 0.0
		_right_char_b.position.x = 0.0

	_intro_tween = create_tween()
	_intro_tween.set_parallel(true)

	# 1. Background fades in first (0.0 -> 0.4s)
	if _background_tex:
		_intro_tween.tween_property(_background_tex, "modulate:a", 1.0, 0.4).set_ease(Tween.EASE_OUT)

	# 2. Sacred Ring fades in second, completely static, no pulsation (0.35 -> 0.75s)
	if _center_ring:
		_intro_tween.tween_property(_center_ring, "modulate:a", 1.0, 0.4).set_delay(0.35).set_ease(Tween.EASE_OUT)

	# 3. Characters glide inward from the edges while fading in (0.65 -> 1.3s)
	if _left_char_a:
		_intro_tween.tween_property(_left_char_a, "position:x", 0.0, 0.65).set_delay(0.65).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
		_intro_tween.tween_property(_left_char_a, "modulate:a", 1.0, 0.65).set_delay(0.65).set_ease(Tween.EASE_OUT)
	if _right_char_a:
		_intro_tween.tween_property(_right_char_a, "position:x", 0.0, 0.65).set_delay(0.65).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
		_intro_tween.tween_property(_right_char_a, "modulate:a", 1.0, 0.65).set_delay(0.65).set_ease(Tween.EASE_OUT)

	# 4. Scrim and Menu UI fade in last (1.05 -> 1.45s)
	if _dark_scrim:
		_intro_tween.tween_property(_dark_scrim, "modulate:a", 1.0, 0.4).set_delay(1.05).set_ease(Tween.EASE_OUT)
	if _margin_container:
		_intro_tween.tween_property(_margin_container, "modulate:a", 1.0, 0.4).set_delay(1.05).set_ease(Tween.EASE_OUT)

	_intro_tween.chain().tween_callback(func():
		intro_active = false
	)

## Snaps every layer to its resting state (any key or click during the intro).
func skip_intro() -> void:
	if not intro_active:
		return
	if _intro_tween and _intro_tween.is_valid():
		_intro_tween.kill()

	intro_active = false
	if _background_tex:
		_background_tex.modulate.a = 1.0
	if _center_ring:
		_center_ring.modulate.a = 1.0
		_center_ring.scale = Vector2.ONE
	for rect in [_left_char_a, _right_char_a]:
		if rect:
			rect.modulate.a = 1.0
			rect.position.x = 0.0
	for rect in [_left_char_b, _right_char_b]:
		if rect:
			rect.modulate.a = 0.0
			rect.position.x = 0.0
	if _dark_scrim:
		_dark_scrim.modulate.a = 1.0
	if _margin_container:
		_margin_container.modulate.a = 1.0

func _cycle_slot(is_left: bool) -> void:
	var char_a: TextureRect = _left_char_a if is_left else _right_char_a
	var char_b: TextureRect = _left_char_b if is_left else _right_char_b
	var is_a_active: bool = _left_active_is_a if is_left else _right_active_is_a
	var current_tex: Texture2D = _current_left_tex if is_left else _current_right_tex

	if not char_a or not char_b:
		return

	# Pick a new random texture different from current
	var pool: Array[Texture2D] = []
	for tex in _textures:
		if tex != current_tex:
			pool.append(tex)
	if pool.is_empty():
		pool = _textures

	var new_tex: Texture2D = pool[randi() % pool.size()]

	var outgoing_rect: TextureRect = char_a if is_a_active else char_b
	var incoming_rect: TextureRect = char_b if is_a_active else char_a

	# Each slot's screen edge: left of the left slot, right of the right slot
	var edge_x: float = -GLIDE_OFFSET if is_left else GLIDE_OFFSET

	# Prepare incoming rect at the edge with 0 opacity and correct facing
	_apply_character_to_rect(incoming_rect, new_tex, is_left)
	incoming_rect.position.x = edge_x
	incoming_rect.modulate.a = 0.0

	var cross_tween := create_tween().set_parallel(true)

	# Outgoing: moves from resting position (0.0) towards screen edge (edge_x) while quickly fading out
	cross_tween.tween_property(outgoing_rect, "position:x", edge_x, 0.45).set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	cross_tween.tween_property(outgoing_rect, "modulate:a", 0.0, 0.45).set_ease(Tween.EASE_IN)

	# Incoming: glides from screen edge (edge_x) to resting position (0.0) while fading in
	cross_tween.tween_property(incoming_rect, "position:x", 0.0, 0.65).set_delay(0.1).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	cross_tween.tween_property(incoming_rect, "modulate:a", 1.0, 0.65).set_delay(0.1).set_ease(Tween.EASE_OUT)

	# Reset outgoing rect position when finished so it's ready for future transitions
	cross_tween.chain().tween_callback(func():
		outgoing_rect.position.x = 0.0
	)

	if is_left:
		_left_active_is_a = not _left_active_is_a
		_current_left_tex = new_tex
	else:
		_right_active_is_a = not _right_active_is_a
		_current_right_tex = new_tex
