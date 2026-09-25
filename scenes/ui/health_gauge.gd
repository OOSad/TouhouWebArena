class_name HealthGauge
extends BoxContainer

# Dynamic Yin-Yang orb health gauge for Touhou Web Arena.
# Renders 5 (or any count) Yin-Yang orbs showing full, half, and empty states.
# Supports both horizontal (top bar) and vertical (side margin) orientations.

const FULL_ORB_TEX: Texture2D = preload("res://assets/ui/life_orb_full.png")
const HALF_ORB_TEX: Texture2D = preload("res://assets/ui/life_orb_half.png")
const EMPTY_ORB_TEX: Texture2D = preload("res://assets/ui/life_orb_empty.png")

@export var player_number: int = 1
@export var orb_size: Vector2 = Vector2(36, 36)
@export var is_vertical: bool = false
@export var is_mirrored: bool = false
@export var deplete_from_top: bool = false
@export var show_name_label: bool = false
@export var enable_pillar: bool = true
@export var base_bottom_y: float = -1.0

@onready var orbs_container: BoxContainer = %OrbsContainer
@onready var name_label: Label = %NameLabel

var _orb_rects: Array[TextureRect] = []
var _last_health: float = -1.0

func _ready() -> void:
	if orbs_container == null:
		orbs_container = get_node_or_null("%OrbsContainer") if has_node("%OrbsContainer") else get_node_or_null("OrbsContainer")
	if name_label == null:
		name_label = get_node_or_null("%NameLabel") if has_node("%NameLabel") else get_node_or_null("NameLabel")
	if base_bottom_y < 0.0 and offset_bottom > 0.0:
		base_bottom_y = offset_bottom
	_apply_layout()

func _apply_layout() -> void:
	vertical = is_vertical
	if orbs_container:
		orbs_container.vertical = is_vertical
	if name_label:
		name_label.visible = show_name_label
	
	if is_mirrored and not is_vertical:
		alignment = BoxContainer.ALIGNMENT_END
		if orbs_container and name_label:
			move_child(orbs_container, 0)
			move_child(name_label, 1)
			name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			orbs_container.alignment = BoxContainer.ALIGNMENT_END

func set_player_info(p_name: String, p_color: Color = Color.WHITE) -> void:
	if name_label:
		name_label.text = p_name.to_upper()
		name_label.modulate = p_color

func update_health(current: float, max_val: float) -> void:
	if orbs_container == null:
		orbs_container = get_node_or_null("%OrbsContainer") if has_node("%OrbsContainer") else get_node_or_null("OrbsContainer")
	if orbs_container == null:
		return
	
	var total_slots: int = maxi(1, int(ceil(max_val)))
	
	# Adjust orb count if max_val changed or initial setup
	while _orb_rects.size() < total_slots:
		var rect := TextureRect.new()
		rect.custom_minimum_size = orb_size
		rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		rect.texture = FULL_ORB_TEX
		orbs_container.add_child(rect)
		_orb_rects.append(rect)
	
	while _orb_rects.size() > total_slots:
		var last_rect = _orb_rects.pop_back()
		last_rect.queue_free()
	
	# Order children in orbs_container if vertical with top-down depletion:
	# Top child = highest slot (empties first); Bottom child = slot 0 (last life).
	if is_vertical and deplete_from_top:
		for i in range(total_slots):
			var target_child_idx: int = total_slots - 1 - i
			orbs_container.move_child(_orb_rects[i], target_child_idx)
	
	# Dynamically adjust vertical height and bottom-anchoring to fit any max health count
	var sep: float = 6.0
	if orbs_container:
		sep = float(orbs_container.get_theme_constant("separation"))
	var needed_h: float = float(total_slots) * orb_size.y + float(maxi(0, total_slots - 1)) * sep
	
	if is_vertical and deplete_from_top and base_bottom_y > 0.0:
		offset_bottom = base_bottom_y
		offset_top = base_bottom_y - needed_h
		custom_minimum_size.y = needed_h
		size.y = needed_h
		pivot_offset = Vector2(size.x * 0.5, needed_h)
	
	# Update texture state per orb slot
	for i in range(total_slots):
		var rect := _orb_rects[i]
		var slot_idx: int = i
		if not is_vertical and is_mirrored:
			slot_idx = total_slots - 1 - i
		
		var remaining: float = current - float(slot_idx)
		
		if remaining >= 1.0:
			rect.texture = FULL_ORB_TEX
			rect.flip_h = false
		elif remaining >= 0.5:
			rect.texture = HALF_ORB_TEX
			rect.flip_h = is_mirrored
		else:
			rect.texture = EMPTY_ORB_TEX
			rect.flip_h = false
	
	queue_redraw()
	
	# If health decreased, play a brief punch/scale pop on the damaged slot
	if _last_health > 0.0 and current < _last_health:
		_play_hit_bump()
	_last_health = current

func _draw() -> void:
	if not enable_pillar or not is_vertical:
		return
	if _orb_rects.is_empty():
		return
	
	var total_slots: int = _orb_rects.size()
	var sep: float = 6.0
	if orbs_container:
		sep = float(orbs_container.get_theme_constant("separation"))
	var stack_h: float = float(total_slots) * orb_size.y + float(maxi(0, total_slots - 1)) * sep
	
	# Padding around orbs
	const PAD_X: float = 4.0
	const PAD_Y: float = 5.0
	
	var oc_pos := orbs_container.position if orbs_container else Vector2.ZERO
	var pillar_rect := Rect2(oc_pos.x - PAD_X, oc_pos.y - PAD_Y, orb_size.x + (PAD_X * 2.0), stack_h + (PAD_Y * 2.0))
	
	# Rounded pillar capsule (StyleBoxFlat)
	var sb := StyleBoxFlat.new()
	sb.bg_color = Color(0.04, 0.08, 0.05, 0.82) # Dark translucent bamboo charcoal
	sb.border_color = Color(0.12, 0.24, 0.16, 0.95) # Dark jade hairline matching playfield border
	sb.set_border_width_all(1)
	sb.set_corner_radius_all(12)
	sb.anti_aliasing = true
	draw_style_box(sb, pillar_rect)
	
	# Recessed socket indentations behind each orb
	var socket_radius: float = 17.0
	var socket_bg := Color(0.02, 0.04, 0.03, 0.92)
	var socket_rim := Color(0.14, 0.26, 0.18, 0.40)
	
	for k in range(total_slots):
		var orb_cy: float = oc_pos.y + float(k) * (orb_size.y + sep) + (orb_size.y * 0.5)
		var orb_cx: float = oc_pos.x + (orb_size.x * 0.5)
		var center := Vector2(orb_cx, orb_cy)
		
		draw_circle(center, socket_radius, socket_bg)
		draw_arc(center, socket_radius, 0.0, TAU, 32, socket_rim, 1.0)

func _play_hit_bump() -> void:
	var tw := create_tween()
	tw.tween_property(self, "scale", Vector2(1.10, 1.10), 0.08).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_property(self, "scale", Vector2.ONE, 0.12).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
