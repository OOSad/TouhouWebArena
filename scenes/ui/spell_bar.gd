class_name SpellBar
extends Control

## Modular Spell Gauge UI for Touhou Web Arena.
## Faithfully recreates the Touhou 09 (PoFV) spell bar with:
## - Flanking Rank Numbers (Cirno.ttf bold outline): Left for Lv 2/3 rank, Right for Lv 4 rank (1-16).
## - Dark beveled metallic frame and background track.
## - Crimson/character-themed Passive Charge bar (capacity accumulated from enemy pops).
## - Glowing overlaid Active Charge bar (advancing from the left while holding charge key).
## - Vertical segment divider lines (demarcating 25%, 50%, 75%, 100% thresholds).
## - Threshold flash / level attainment visual cues.

@export var player_number: int = 1
@export var max_segments: int = 4
@export var passive_color: Color = Color(0.74, 0.22, 0.28, 1.0)
@export var active_color: Color = Color(1.0, 0.88, 0.28, 1.0)

var current_passive_charge: float = 1.0
var current_active_charge: float = 0.0
var current_rank_lv2_3: int = 1
var current_rank_lv4: int = 1

var passive_charge: float:
	get: return current_passive_charge
var active_charge: float:
	get: return current_active_charge

var _last_active_level: int = 0
var _flash_alpha: float = 0.0

@onready var left_rank_label: Label = %LeftRankLabel if has_node("%LeftRankLabel") else get_node_or_null("LeftRankLabel")
@onready var right_rank_label: Label = %RightRankLabel if has_node("%RightRankLabel") else get_node_or_null("RightRankLabel")
@onready var bar_track: Control = %BarTrack if has_node("%BarTrack") else get_node_or_null("BarTrack")

func _ready() -> void:
	if left_rank_label == null:
		left_rank_label = get_node_or_null("%LeftRankLabel") if has_node("%LeftRankLabel") else get_node_or_null("LeftRankLabel")
	if right_rank_label == null:
		right_rank_label = get_node_or_null("%RightRankLabel") if has_node("%RightRankLabel") else get_node_or_null("RightRankLabel")
	if bar_track == null:
		bar_track = get_node_or_null("%BarTrack") if has_node("%BarTrack") else get_node_or_null("BarTrack")
	
	if bar_track and not bar_track.draw.is_connected(_on_bar_track_draw):
		bar_track.draw.connect(_on_bar_track_draw)
	
	_update_rank_labels()
	queue_bar_redraw()

func _process(_delta: float) -> void:
	# Keep redrawing while active charge is held to animate the glowing pulse
	if current_active_charge > 0.0 or _flash_alpha > 0.0:
		if bar_track:
			bar_track.queue_redraw()

func apply_character_data(data: CharacterData) -> void:
	if data == null:
		return
	max_segments = data.spell_bar_segments
	if max_segments <= 0:
		visible = false
		return
	else:
		visible = true
	
	if data.spell_bar_color != Color.TRANSPARENT:
		passive_color = data.spell_bar_color
	elif data.primary_color != Color.TRANSPARENT:
		passive_color = data.primary_color.darkened(0.15)
	
	current_passive_charge = data.passive_charge_start
	current_active_charge = 0.0
	_last_active_level = 0
	queue_bar_redraw()

func set_passive_charge(charge_val: float, p_max_segments: int = -1) -> void:
	if p_max_segments > 0:
		max_segments = p_max_segments
	current_passive_charge = clampf(charge_val, 0.0, float(max_segments))
	queue_bar_redraw()

func set_active_charge(charge_val: float, p_max_segments: int = -1) -> void:
	if p_max_segments > 0:
		max_segments = p_max_segments
	var prev_active := current_active_charge
	current_active_charge = clampf(charge_val, 0.0, float(max_segments))
	
	var current_level: int = int(floor(current_active_charge))
	if current_level > _last_active_level and current_level >= 1:
		play_threshold_flash(current_level)
	_last_active_level = current_level
	
	if current_active_charge > 0.0 or prev_active > 0.0:
		queue_bar_redraw()

func set_ranks(lv2_3: int, lv4: int) -> void:
	current_rank_lv2_3 = clampi(lv2_3, 1, 16)
	current_rank_lv4 = clampi(lv4, 1, 16)
	_update_rank_labels()

func play_threshold_flash(level: int) -> void:
	_flash_alpha = 0.8
	var tw := create_tween()
	tw.tween_property(self, "_flash_alpha", 0.0, 0.18).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	
	# Slight punch scale bump
	var tw_bump := create_tween()
	tw_bump.tween_property(self, "scale", Vector2(1.01, 1.03), 0.04).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw_bump.tween_property(self, "scale", Vector2.ONE, 0.08).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)

func play_release_flash() -> void:
	_flash_alpha = 1.0
	var tw := create_tween()
	tw.tween_property(self, "_flash_alpha", 0.0, 0.25).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

func _update_rank_labels() -> void:
	if left_rank_label:
		left_rank_label.text = str(current_rank_lv2_3)
	if right_rank_label:
		right_rank_label.text = str(current_rank_lv4)

func queue_bar_redraw() -> void:
	if bar_track:
		bar_track.queue_redraw()

func _on_bar_track_draw() -> void:
	if bar_track == null or max_segments <= 0:
		return
	
	var track_rect: Rect2 = bar_track.get_rect()
	var w: float = track_rect.size.x
	var h: float = track_rect.size.y
	
	const BLOCK_GAP: float = 3.0
	var total_gaps: float = float(max_segments - 1) * BLOCK_GAP
	var block_w: float = (w - total_gaps) / float(max_segments)
	
	# Color constants
	var border_col := Color(0.24, 0.20, 0.30, 1.0)
	var shadow_col := Color(0.08, 0.05, 0.12, 1.0)
	var highlight_col := Color(0.44, 0.38, 0.52, 1.0)
	var groove_bg := Color(0.06, 0.04, 0.08, 0.95)
	
	# Pulse oscillation while holding active charge
	var pulse: float = 0.88 + 0.12 * sin(float(Time.get_ticks_msec()) * 0.018)
	var glowing_color := active_color
	glowing_color.r = clampf(active_color.r * pulse, 0.0, 1.0)
	glowing_color.g = clampf(active_color.g * pulse, 0.0, 1.0)
	glowing_color.b = clampf(active_color.b * pulse, 0.0, 1.0)
	
	var font: Font = bar_track.get_theme_default_font()
	
	for i in range(max_segments):
		var bx: float = float(i) * (block_w + BLOCK_GAP)
		var by: float = 0.0
		var bw: float = block_w
		var bh: float = h
		
		# 1. Outer Dark Metallic Block Frame
		bar_track.draw_rect(Rect2(bx, by, bw, bh), border_col)
		# Bevel highlight on top & left
		bar_track.draw_line(Vector2(bx, by), Vector2(bx + bw, by), highlight_col, 1.0)
		bar_track.draw_line(Vector2(bx, by), Vector2(bx, by + bh), highlight_col, 1.0)
		# Bevel shadow on bottom & right
		bar_track.draw_line(Vector2(bx, by + bh), Vector2(bx + bw, by + bh), shadow_col, 1.0)
		bar_track.draw_line(Vector2(bx + bw, by), Vector2(bx + bw, by + bh), shadow_col, 1.0)
		
		# 2. Inner Groove / Inset
		var inset: float = 1.0
		var inner_x: float = bx + inset
		var inner_y: float = by + inset
		var inner_w: float = bw - (inset * 2.0)
		var inner_h: float = bh - (inset * 2.0)
		var inner_rect := Rect2(inner_x, inner_y, inner_w, inner_h)
		bar_track.draw_rect(inner_rect, groove_bg)
		
		# 3. Passive Charge Fill for this block
		var p_fill: float = clampf(current_passive_charge - float(i), 0.0, 1.0)
		if p_fill > 0.0:
			var fill_w: float = inner_w * p_fill
			var fill_rect := Rect2(inner_x, inner_y, fill_w, inner_h)
			bar_track.draw_rect(fill_rect, passive_color)
			
			# 3D top shine highlight on passive fill
			var shine_col := Color(1.0, 1.0, 1.0, 0.28)
			bar_track.draw_line(Vector2(inner_x, inner_y + 0.5), Vector2(inner_x + fill_w, inner_y + 0.5), shine_col, 1.0)
			
			# Clean edge cap if not fully filled
			if p_fill < 1.0:
				bar_track.draw_line(Vector2(inner_x + fill_w, inner_y), Vector2(inner_x + fill_w, inner_y + inner_h), Color(1.0, 0.55, 0.65, 0.8), 1.0)
		
		# 4. Active Charge Fill for this block (holding charge)
		var a_fill: float = clampf(current_active_charge - float(i), 0.0, 1.0)
		if a_fill > 0.0:
			var active_fill_w: float = inner_w * a_fill
			var active_rect := Rect2(inner_x, inner_y, active_fill_w, inner_h)
			bar_track.draw_rect(active_rect, glowing_color)
			
			# Bright white top highlight line
			var active_shine := Color(1.0, 1.0, 1.0, 0.65)
			bar_track.draw_line(Vector2(inner_x, inner_y + 0.5), Vector2(inner_x + active_fill_w, inner_y + 0.5), active_shine, 1.0)
			
			# Leading edge glowing cursor
			var cursor_col := Color(1.0, 1.0, 1.0, 0.95)
			bar_track.draw_line(Vector2(inner_x + active_fill_w, inner_y), Vector2(inner_x + active_fill_w, inner_y + inner_h), cursor_col, 1.5)
		
		# 5. Segment Number Label (1, 2, 3, 4)
		if font:
			var num_str := str(i + 1)
			var font_size: int = 10
			var num_y: float = inner_y + (inner_h * 0.5) + 3.5
			# Shadow outline for readability
			bar_track.draw_string(font, Vector2(bx + 1.0, num_y + 1.0), num_str, HORIZONTAL_ALIGNMENT_CENTER, bw, font_size, Color(0, 0, 0, 0.85))
			# Fore color: brighter if filled, muted if empty
			var num_col := Color(1.0, 1.0, 1.0, 0.95) if (a_fill > 0.0 or p_fill >= 1.0) else Color(1.0, 1.0, 1.0, 0.45)
			bar_track.draw_string(font, Vector2(bx, num_y), num_str, HORIZONTAL_ALIGNMENT_CENTER, bw, font_size, num_col)
		
		# 6. Flash on attaining this level or releasing spell
		if _flash_alpha > 0.0:
			if _last_active_level > 0 and i < _last_active_level:
				var flash_col := Color(1.0, 1.0, 1.0, _flash_alpha * 0.5)
				bar_track.draw_rect(inner_rect, flash_col)
			elif _last_active_level == 0:
				var flash_col := Color(1.0, 1.0, 1.0, _flash_alpha * 0.4)
				bar_track.draw_rect(inner_rect, flash_col)
