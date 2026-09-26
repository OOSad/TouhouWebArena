class_name CharacterCard
extends Control

## Modular Character Display for Touhou Web Arena Character Select.
## Features full-size, unboxed sumi-e silhouettes and toggles detailed
## combat specifications on hold (Shift key) or click.

@export var player_number: int = 1
@export var is_cpu: bool = false

@onready var name_plate_vbox: VBoxContainer = %NamePlateVBox
@onready var top_hbox: HBoxContainer = %TopHBox
@onready var player_badge: Label = %PlayerBadge
@onready var version_tag: Label = %VersionTag
@onready var title_label: Label = %TitleLabel
@onready var name_label: Label = %NameLabel
@onready var inspect_hint: Label = %InspectHint
@onready var handicap_slot: BoxContainer = %HandicapSlot
@onready var handicap_gauge: HealthGauge = %HandicapGauge
@onready var handicap_label: Label = %HandicapLabel

var handicap_hp: float = 5.0

# Visual Preview & Overlay References
@onready var portrait_rect: TextureRect = %PortraitRect
@onready var info_overlay: PanelContainer = %InfoOverlay

# Kit Specs (Inside Shift Overlay)
@onready var archetype_label: Label = %ArchetypeLabel
@onready var shot_name_label: Label = %ShotNameLabel
@onready var shot_desc_label: Label = %ShotDescLabel
@onready var scope_label: Label = %ScopeLabel
@onready var trait_label: Label = %TraitLabel

# Sub-Cards (Tactical Moves inside Shift Overlay)
@onready var extra_name_label: Label = %ExtraNameLabel
@onready var extra_desc_label: Label = %ExtraDescLabel
@onready var charge_name_label: Label = %ChargeNameLabel
@onready var charge_desc_label: Label = %ChargeDescLabel
@onready var signature_spell_label: Label = %SignatureSpellLabel

const SILHOUETTE_SCALE: float = 1.6
const TARGET_HEAD_Y_P1: float = 14.0 # Top-left headroom
const TARGET_HEAD_Y_P2: float = 180.0 # Staggered lower for bottom-right staging

# Head calibration: (top_pixel_y, head_center_x) within 896x1200 texture
const HEAD_CALIBRATION: Dictionary = {
	"reimu": Vector2(63.0, 449.5),
	"marisa": Vector2(161.0, 334.0),
	"sakuya": Vector2(86.0, 443.0),
	"youmu": Vector2(83.0, 445.5),
	"cirno": Vector2(82.0, 450.0),
	"reisen": Vector2(125.0, 470.0),
	"yuuka": Vector2(120.0, 480.0),
	"aya": Vector2(125.0, 446.0),
	"clownpiece": Vector2(150.0, 430.0),
	"lyrica": Vector2(108.0, 422.0),
}

# The Random pick is a lone "?" mark, not a duelist, so it skips the head-alignment math and
# gets framed by its own bounding box instead: center of the mark inside its 700x1000 texture,
# and where that center should land in the card (mirrored vertically for the P2 side).
const RANDOM_MARK_CENTER := Vector2(349.5, 219.5)
const RANDOM_MARK_CARD_Y: float = 324.0

const SILHOUETTE_SHADER := preload("res://shaders/card_silhouette.gdshader")
var _silhouette_mat: ShaderMaterial = null
var _current_data: CharacterData = null
var is_inspecting: bool = false
var _inspect_tween: Tween = null
var _slide_tween: Tween = null

func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		_update_portrait_layout()

func _ready() -> void:
	_ensure_references()
	_apply_player_theme()
	set_handicap(handicap_hp)
	if info_overlay:
		info_overlay.visible = false
		info_overlay.modulate.a = 0.0
	if portrait_rect:
		portrait_rect.modulate.a = 0.95
		_silhouette_mat = ShaderMaterial.new()
		_silhouette_mat.shader = SILHOUETTE_SHADER
		_silhouette_mat.set_shader_parameter("player_number", player_number)
		portrait_rect.material = _silhouette_mat
	if _current_data:
		_populate_ui(_current_data, true)
	else:
		_update_portrait_layout()

func _ensure_references() -> void:
	if top_hbox == null:
		top_hbox = get_node_or_null("%TopHBox") as HBoxContainer
	if player_badge == null:
		player_badge = get_node_or_null("%PlayerBadge") as Label
	if version_tag == null:
		version_tag = get_node_or_null("%VersionTag") as Label
	if title_label == null:
		title_label = get_node_or_null("%TitleLabel") as Label
	if name_label == null:
		name_label = get_node_or_null("%NameLabel") as Label
	if inspect_hint == null:
		inspect_hint = get_node_or_null("%InspectHint") as Label
	if handicap_slot == null:
		handicap_slot = get_node_or_null("%HandicapSlot") as BoxContainer
	if handicap_gauge == null:
		handicap_gauge = get_node_or_null("%HandicapGauge") as HealthGauge
	if handicap_label == null:
		handicap_label = get_node_or_null("%HandicapLabel") as Label
	if portrait_rect == null:
		portrait_rect = get_node_or_null("%PortraitRect") as TextureRect
	if info_overlay == null:
		info_overlay = get_node_or_null("%InfoOverlay") as PanelContainer
	if archetype_label == null:
		archetype_label = get_node_or_null("%ArchetypeLabel") as Label
	if shot_name_label == null:
		shot_name_label = get_node_or_null("%ShotNameLabel") as Label
	if shot_desc_label == null:
		shot_desc_label = get_node_or_null("%ShotDescLabel") as Label
	if scope_label == null:
		scope_label = get_node_or_null("%ScopeLabel") as Label
	if trait_label == null:
		trait_label = get_node_or_null("%TraitLabel") as Label
	if extra_name_label == null:
		extra_name_label = get_node_or_null("%ExtraNameLabel") as Label
	if extra_desc_label == null:
		extra_desc_label = get_node_or_null("%ExtraDescLabel") as Label
	if charge_name_label == null:
		charge_name_label = get_node_or_null("%ChargeNameLabel") as Label
	if charge_desc_label == null:
		charge_desc_label = get_node_or_null("%ChargeDescLabel") as Label
	if signature_spell_label == null:
		signature_spell_label = get_node_or_null("%SignatureSpellLabel") as Label

## Populates the card with a CharacterData resource and optional player/CPU assignment
func set_character(data: CharacterData, p_num: int = -1, p_is_cpu: bool = false) -> void:
	if p_num > 0:
		player_number = p_num
	is_cpu = p_is_cpu
	_apply_player_theme()
	
	var char_changed: bool = (_current_data != data)
	_current_data = data
	if data != null:
		_populate_ui(data, char_changed)

## Updates CPU status for player 2
func set_is_cpu(val: bool) -> void:
	is_cpu = val
	_apply_player_theme()

## Toggles inspect mode to reveal or hide the Shift overlay box
func set_inspect_mode(inspecting: bool, animated: bool = true) -> void:
	if is_inspecting == inspecting:
		return
	is_inspecting = inspecting
	_ensure_references()
	
	if info_overlay == null:
		return
	
	if _inspect_tween and _inspect_tween.is_valid():
		_inspect_tween.kill()
	
	if not animated:
		info_overlay.visible = inspecting
		info_overlay.modulate.a = 1.0 if inspecting else 0.0
		if portrait_rect:
			portrait_rect.modulate.a = 0.20 if inspecting else 0.95
		if inspect_hint:
			inspect_hint.add_theme_color_override("font_color", Color(1.0, 0.95, 0.5, 1.0) if inspecting else Color(0.68, 0.76, 0.9, 0.85))
		return
	
	if inspect_hint:
		inspect_hint.add_theme_color_override("font_color", Color(1.0, 0.95, 0.5, 1.0) if inspecting else Color(0.68, 0.76, 0.9, 0.85))
	
	_inspect_tween = create_tween().set_parallel(true)
	if inspecting:
		info_overlay.visible = true
		_inspect_tween.tween_property(info_overlay, "modulate:a", 1.0, 0.18).set_ease(Tween.EASE_OUT)
		if portrait_rect:
			_inspect_tween.tween_property(portrait_rect, "modulate:a", 0.20, 0.18).set_ease(Tween.EASE_OUT)
	else:
		_inspect_tween.tween_property(info_overlay, "modulate:a", 0.0, 0.15).set_ease(Tween.EASE_IN)
		if portrait_rect:
			_inspect_tween.tween_property(portrait_rect, "modulate:a", 0.95, 0.15).set_ease(Tween.EASE_IN)
		_inspect_tween.chain().tween_callback(func():
			if not is_inspecting and info_overlay:
				info_overlay.visible = false
		)

func set_handicap(hp: float) -> void:
	handicap_hp = clampf(hp, 0.5, 5.0)
	_ensure_references()
	if handicap_gauge:
		handicap_gauge.update_health(handicap_hp, 5.0)
	if handicap_label:
		if is_equal_approx(handicap_hp, 0.5):
			handicap_label.text = "0.5 HP (Guts)"
			handicap_label.add_theme_color_override("font_color", Color(1.0, 0.4, 0.4, 1.0))
		elif handicap_hp < 5.0:
			handicap_label.text = "%.1f HP" % handicap_hp
			handicap_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.4, 1.0))
		else:
			handicap_label.text = "5.0 HP"
			handicap_label.add_theme_color_override("font_color", Color(0.9, 0.95, 1.0, 0.9))

func _apply_player_theme() -> void:
	_ensure_references()
	var is_p1 := (player_number == 1)
	var badge_color := Color(0.4, 0.8, 1.0, 1.0) if is_p1 else Color(1.0, 0.45, 0.45, 1.0)
	var border_color := Color(0.25, 0.55, 0.85, 0.8) if is_p1 else Color(0.85, 0.35, 0.45, 0.8)
	
	if player_badge:
		if is_p1:
			player_badge.text = "[ 1P ]"
		else:
			player_badge.text = "[ CPU ]" if is_cpu else "[ 2P ]"
		player_badge.add_theme_color_override("font_color", badge_color)
	
	if _silhouette_mat:
		_silhouette_mat.set_shader_parameter("player_number", player_number)
	
	# Apply themed border color and cropped positioning to the Shift overlay panel
	if info_overlay:
		var overlay_sb := info_overlay.get_theme_stylebox("panel")
		if overlay_sb is StyleBoxFlat:
			var dup := overlay_sb.duplicate() as StyleBoxFlat
			dup.border_color = border_color
			info_overlay.add_theme_stylebox_override("panel", dup)
		
		if is_p1:
			# Placed in the open space between P1's NamePlate and "Select Your Character"
			info_overlay.anchor_left = 0.0
			info_overlay.anchor_right = 0.0
			info_overlay.anchor_top = 0.0
			info_overlay.anchor_bottom = 0.0
			info_overlay.offset_left = 320.0
			info_overlay.offset_top = 44.0
			info_overlay.offset_right = 760.0
			info_overlay.offset_bottom = 344.0
		else:
			# Placed in the open space between P2's NamePlate and the [X / Esc] Back hint
			info_overlay.anchor_left = 0.0
			info_overlay.anchor_right = 0.0
			info_overlay.anchor_top = 1.0
			info_overlay.anchor_bottom = 1.0
			info_overlay.offset_left = 20.0
			info_overlay.offset_right = 460.0
			info_overlay.offset_top = -362.0
			info_overlay.offset_bottom = -62.0
	
	# Position NamePlate: Top-Left for P1, Bottom-Right for P2
	if name_plate_vbox == null:
		name_plate_vbox = get_node_or_null("%NamePlateVBox") as VBoxContainer
	if name_plate_vbox:
		if is_p1:
			name_plate_vbox.anchor_top = 0.0
			name_plate_vbox.anchor_bottom = 0.0
			name_plate_vbox.offset_top = 44.0
			name_plate_vbox.offset_bottom = 240.0
		else:
			name_plate_vbox.anchor_top = 1.0
			name_plate_vbox.anchor_bottom = 1.0
			name_plate_vbox.offset_top = -224.0
			name_plate_vbox.offset_bottom = -28.0

	# Bilateral alignment (P1 left-aligned, P2 right-aligned)
	var h_align := HORIZONTAL_ALIGNMENT_LEFT if is_p1 else HORIZONTAL_ALIGNMENT_RIGHT
	if title_label:
		title_label.horizontal_alignment = h_align
	if name_label:
		name_label.horizontal_alignment = h_align
	if inspect_hint:
		inspect_hint.horizontal_alignment = h_align
	if top_hbox:
		top_hbox.alignment = BoxContainer.ALIGNMENT_BEGIN if is_p1 else BoxContainer.ALIGNMENT_END
	if handicap_slot:
		handicap_slot.alignment = BoxContainer.ALIGNMENT_BEGIN if is_p1 else BoxContainer.ALIGNMENT_END
		if handicap_gauge and handicap_label:
			if is_p1:
				handicap_slot.move_child(handicap_gauge, 0)
				handicap_slot.move_child(handicap_label, 1)
				handicap_gauge.is_mirrored = false
			else:
				handicap_slot.move_child(handicap_label, 0)
				handicap_slot.move_child(handicap_gauge, 1)
				handicap_gauge.is_mirrored = true
			handicap_gauge.update_health(handicap_hp, 5.0)
	
	_update_portrait_layout()

func _get_texture_natural_facing(tex: Texture2D) -> int:
	if not tex:
		return 1
	var path_lower := tex.resource_path.to_lower()
	if "marisa" in path_lower:
		return -1
	return 1

## True when the portrait slot is showing the Random "?" mark rather than a character.
func _is_random_mark() -> bool:
	if portrait_rect == null or portrait_rect.texture == null:
		return false
	return "random" in portrait_rect.texture.resource_path.to_lower()

func _update_portrait_facing() -> void:
	if portrait_rect and portrait_rect.texture:
		# A "?" has no facing - mirroring it just renders it backwards.
		if _is_random_mark():
			portrait_rect.flip_h = false
			return
		var natural := _get_texture_natural_facing(portrait_rect.texture)
		# Left slot (P1) faces toward center (RIGHT = +1)
		# Right slot (P2/CPU) faces toward center (LEFT = -1)
		var target := 1 if (player_number == 1) else -1
		portrait_rect.flip_h = (natural != target)

func _update_portrait_layout(animate_slide: bool = false) -> void:
	_ensure_references()
	if portrait_rect == null or portrait_rect.texture == null:
		return
	
	_update_portrait_facing()
	
	var char_id: String = _current_data.character_id if _current_data else ""
	var calib: Vector2 = HEAD_CALIBRATION.get(char_id, Vector2(80.0, 448.0))
	var top_y: float = calib.x
	var head_center_x: float = calib.y
	
	var natural: int = _get_texture_natural_facing(portrait_rect.texture)
	var target: int = 1 if (player_number == 1) else -1
	var is_flipped: bool = (natural != target)
	
	var tex_size: Vector2 = portrait_rect.texture.get_size()
	if tex_size.x <= 0.0 or tex_size.y <= 0.0:
		tex_size = Vector2(896.0, 1200.0)
	
	if is_flipped:
		head_center_x = tex_size.x - head_center_x
	
	var scaled_w: float = tex_size.x * SILHOUETTE_SCALE
	var scaled_h: float = tex_size.y * SILHOUETTE_SCALE
	
	portrait_rect.size = Vector2(scaled_w, scaled_h)
	
	# Target duelist head center horizontally in card column
	var card_w: float = size.x if size.x > 0.0 else 760.0
	var target_head_x: float = card_w * 0.5
	var target_head_y: float = TARGET_HEAD_Y_P1 if (player_number == 1) else TARGET_HEAD_Y_P2
	
	var target_x: float = target_head_x - (head_center_x * SILHOUETTE_SCALE)
	var target_y: float = target_head_y - (top_y * SILHOUETTE_SCALE)
	
	if _is_random_mark():
		var card_h: float = size.y if size.y > 0.0 else 1080.0
		var mark_center_y: float = RANDOM_MARK_CARD_Y if (player_number == 1) else (card_h - RANDOM_MARK_CARD_Y)
		target_x = (card_w * 0.5) - (RANDOM_MARK_CENTER.x * SILHOUETTE_SCALE)
		target_y = mark_center_y - (RANDOM_MARK_CENTER.y * SILHOUETTE_SCALE)
	
	var target_pos := Vector2(target_x, target_y)
	
	if _slide_tween and _slide_tween.is_valid():
		_slide_tween.kill()
	
	if not animate_slide or not is_inside_tree():
		portrait_rect.position = target_pos
		return
	
	# Slide in from the player's edge of the screen:
	# P1 slides in from the left (negative X offset)
	# P2 slides in from the right (positive X offset)
	var slide_distance: float = 380.0
	var start_x: float = target_x - slide_distance if (player_number == 1) else target_x + slide_distance
	portrait_rect.position = Vector2(start_x, target_y)
	
	var target_alpha: float = 0.20 if is_inspecting else 0.95
	portrait_rect.modulate.a = 0.0
	
	_slide_tween = create_tween().set_parallel(true)
	_slide_tween.tween_property(portrait_rect, "position", target_pos, 0.28).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	_slide_tween.tween_property(portrait_rect, "modulate:a", target_alpha, 0.20).set_ease(Tween.EASE_OUT)

func _populate_ui(data: CharacterData, animate_slide: bool = false) -> void:
	_ensure_references()
	
	# Version & Title
	if version_tag:
		version_tag.text = "[ %s ]" % (data.version_tag if not data.version_tag.is_empty() else "TH09")
	if title_label:
		title_label.text = data.title if not data.title.is_empty() else "Combatant"
	if name_label:
		name_label.text = data.display_name
	if archetype_label:
		archetype_label.text = "Role: %s" % (data.archetype if not data.archetype.is_empty() else "All-Around")
	
	# Portrait Silhouette
	if portrait_rect:
		if data.portrait_texture != null:
			portrait_rect.texture = data.portrait_texture
			portrait_rect.visible = true
			if not animate_slide:
				portrait_rect.modulate.a = 0.20 if is_inspecting else 0.95
			_update_portrait_layout(animate_slide)
		else:
			portrait_rect.visible = false
	
	# Primary Shot
	if shot_name_label:
		var s_name := data.primary_shot_name if not data.primary_shot_name.is_empty() else "Primary Shot"
		shot_name_label.text = "• Shot: %s" % s_name
	if shot_desc_label:
		shot_desc_label.text = data.primary_shot_desc if not data.primary_shot_desc.is_empty() else "Standard projectile volley"
	
	# Scope
	if scope_label:
		scope_label.text = "• Scope: %s" % data.get_scope_description()
	
	# Trait
	if trait_label:
		var trait_text := data.special_trait if not data.special_trait.is_empty() else "Standard attributes"
		trait_label.text = "• Trait: %s" % trait_text
	
	# Extra Attack Sub-Card
	if extra_name_label:
		var ex_name := data.extra_attack_name if not data.extra_attack_name.is_empty() else "Extra Attack"
		extra_name_label.text = ex_name
	if extra_desc_label:
		extra_desc_label.text = data.extra_attack_desc if not data.extra_attack_desc.is_empty() else "Delivers harassment to opponent field"
	
	# Charge Attack Sub-Card
	if charge_name_label:
		var ch_name := data.charge_attack_name if not data.charge_attack_name.is_empty() else "Charge Attack (Lv 1)"
		charge_name_label.text = ch_name
	if charge_desc_label:
		charge_desc_label.text = data.charge_attack_desc if not data.charge_attack_desc.is_empty() else "Consumes 1 gauge segment"
	
	# Signature Spellcard
	if signature_spell_label:
		var sp_name := data.signature_spell_name
		if sp_name.is_empty() and data.spellcard_lv2 != null:
			sp_name = data.spellcard_lv2.spellcard_name
		if sp_name.is_empty():
			sp_name = "Level 4 Boss Spellcard"
		signature_spell_label.text = sp_name
