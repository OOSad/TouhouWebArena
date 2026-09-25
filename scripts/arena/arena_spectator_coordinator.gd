class_name ArenaSpectatorCoordinator
extends Node

## ArenaSpectatorCoordinator
## Manages spectator mode HUD badges, waiting overlays, state synchronization,
## and remote event dispatching for the Arena.

var arena: Node = null
var p1_playfield: Playfield = null
var p2_playfield: Playfield = null
var p1_viewport_container: SubViewportContainer = null
var p2_viewport_container: SubViewportContainer = null
var match_timer: MatchTimer = null
var p1_health_gauge: HealthGauge = null
var p2_health_gauge: HealthGauge = null
var p1_spell_bar: SpellBar = null
var p2_spell_bar: SpellBar = null
var p1_round_wins: RoundWinsDisplay = null
var p2_round_wins: RoundWinsDisplay = null
var p1_name_label: Label = null
var p2_name_label: Label = null

var is_waiting_for_next_round: bool = false
var waiting_overlay: Control = null

const P1_MOTE_COLOR: Color = Color(1.0, 0.35, 0.45, 1.0)
const P2_MOTE_COLOR: Color = Color(0.3, 0.75, 1.0, 1.0)

func setup(
	p_arena: Node,
	p_p1: Playfield,
	p_p2: Playfield,
	p_p1_vp: SubViewportContainer,
	p_p2_vp: SubViewportContainer,
	p_timer: MatchTimer,
	p_p1_gauge: HealthGauge,
	p_p2_gauge: HealthGauge,
	p_p1_bar: SpellBar,
	p_p2_bar: SpellBar,
	p_p1_wins: RoundWinsDisplay,
	p_p2_wins: RoundWinsDisplay,
	p_p1_lbl: Label,
	p_p2_lbl: Label
) -> void:
	arena = p_arena
	p1_playfield = p_p1
	p2_playfield = p_p2
	p1_viewport_container = p_p1_vp
	p2_viewport_container = p_p2_vp
	match_timer = p_timer
	p1_health_gauge = p_p1_gauge
	p2_health_gauge = p_p2_gauge
	p1_spell_bar = p_p1_bar
	p2_spell_bar = p_p2_bar
	p1_round_wins = p_p1_wins
	p2_round_wins = p_p2_wins
	p1_name_label = p_p1_lbl
	p2_name_label = p_p2_lbl

func on_host_spectator_count_updated(count: int) -> void:
	if arena and arena.get("local_player_num") == 1 and count > 0:
		broadcast_host_match_init_to_spectators()

func broadcast_host_match_init_to_spectators() -> void:
	var net_mgr = get_node_or_null("/root/NetworkManager")
	var game_mgr = get_node_or_null("/root/GameManager")
	if net_mgr == null or not net_mgr.is_host:
		return
	var p1_c: String = p1_playfield.character_id if p1_playfield else "youmu"
	var p2_c: String = p2_playfield.character_id if p2_playfield else "marisa"
	var p1_w: int = game_mgr.p1_round_wins if game_mgr else 0
	var p2_w: int = game_mgr.p2_round_wins if game_mgr else 0
	var rnd: int = game_mgr.current_round if game_mgr else 1
	var active_round: bool = false
	if arena:
		var intermission: bool = bool(arena.get("_round_intermission"))
		var match_over: bool = bool(arena.get("_match_over"))
		var elapsed: float = float(arena.get("_round_elapsed_time"))
		active_round = not intermission and not match_over and (match_timer != null and match_timer.is_running) and elapsed > 3.0
	
	net_mgr.broadcast_spectator_event({
		"type": "match_init",
		"p1_char": p1_c,
		"p2_char": p2_c,
		"stage_id": arena.get("_current_stage_id") if arena else "bamboo_road",
		"p1_wins": p1_w,
		"p2_wins": p2_w,
		"current_round": rnd,
		"is_round_active": active_round,
		"match_seed": net_mgr.match_seed
	})

func on_spectator_match_init(p1_c: String, p2_c: String, stage_id: String, p1_w: int, p2_w: int, current_rnd: int, is_active: bool = false) -> void:
	if arena and arena.has_method("_mark_remote_packet_received"):
		arena._mark_remote_packet_received()
	var game_mgr = get_node_or_null("/root/GameManager")
	if game_mgr:
		game_mgr.set_p1_character(p1_c)
		game_mgr.set_p2_character(p2_c)
		game_mgr.p1_round_wins = p1_w
		game_mgr.p2_round_wins = p2_w
		game_mgr.current_round = current_rnd
	if p1_round_wins:
		p1_round_wins.set_wins(p1_w, false)
	if p2_round_wins:
		p2_round_wins.set_wins(p2_w, false)
	
	apply_character_and_stage_setup(p1_c, p2_c, stage_id)
	
	if arena:
		var intermission: bool = bool(arena.get("_round_intermission"))
		var match_over: bool = bool(arena.get("_match_over"))
		if is_active and not intermission and not match_over and not is_waiting_for_next_round:
			enter_spectator_waiting_state(current_rnd + 1)

func apply_character_and_stage_setup(p1_c: String, p2_c: String, stage_id: String) -> void:
	var p1_res := CharacterData.get_character(p1_c)
	var p2_res := CharacterData.get_character(p2_c)
	var game_mgr = get_node_or_null("/root/GameManager")
	
	var p1_title: String = game_mgr.p1_name if game_mgr else "PLAYER 1"
	var p2_title: String = game_mgr.p2_name if game_mgr else "PLAYER 2"
	
	var p1_col: Color = p1_res.primary_color if p1_res else P1_MOTE_COLOR
	if p1_name_label:
		p1_name_label.text = p1_title.to_upper()
		p1_name_label.modulate = p1_col
	if p1_health_gauge:
		p1_health_gauge.set_player_info(p1_title, p1_col)
	
	var p2_col: Color = p2_res.primary_color if p2_res else P2_MOTE_COLOR
	if p2_name_label:
		p2_name_label.text = p2_title.to_upper()
		p2_name_label.modulate = p2_col
	if p2_health_gauge:
		p2_health_gauge.set_player_info(p2_title, p2_col)
	
	if p1_spell_bar and p1_res:
		p1_spell_bar.apply_character_data(p1_res)
	if p2_spell_bar and p2_res:
		p2_spell_bar.apply_character_data(p2_res)
	
	if arena:
		var current_stage: String = str(arena.get("_current_stage_id"))
		if not stage_id.is_empty() and stage_id != current_stage:
			arena.set("_current_stage_id", stage_id)
			var stage_scene := CharacterData.get_stage_scene_by_id(stage_id)
			var chosen_bgm: String = CharacterData.get_stage_bgm_by_id(stage_id)
			arena.set("_chosen_bgm_path", chosen_bgm)
			AudioService.play_music(chosen_bgm)
			if p1_playfield:
				p1_playfield._init_background(stage_scene)
			if p2_playfield:
				p2_playfield._init_background(stage_scene)
	
	if p1_playfield and p1_playfield.character_id != p1_c:
		p1_playfield.character_id = p1_c
		if p1_playfield.player:
			p1_playfield.player.character_id = p1_c
	if p2_playfield and p2_playfield.character_id != p2_c:
		p2_playfield.character_id = p2_c
		if p2_playfield.player:
			p2_playfield.player.character_id = p2_c

func enter_spectator_waiting_state(next_round_num: int) -> void:
	is_waiting_for_next_round = true
	if arena:
		arena.set("_is_waiting_for_next_round", true)
	if match_timer:
		match_timer.pause()
	if p1_playfield:
		p1_playfield.pellet_spawning_enabled = false
		p1_playfield.fairy_spawning_enabled = false
		p1_playfield.clear_all_bullets()
		if p1_playfield.entities_layer:
			for child in p1_playfield.entities_layer.get_children():
				if child != p1_playfield.player:
					child.queue_free()
	if p2_playfield:
		p2_playfield.pellet_spawning_enabled = false
		p2_playfield.fairy_spawning_enabled = false
		p2_playfield.clear_all_bullets()
		if p2_playfield.entities_layer:
			for child in p2_playfield.entities_layer.get_children():
				if child != p2_playfield.player:
					child.queue_free()
	
	if waiting_overlay == null:
		create_waiting_overlay(next_round_num)
	else:
		var sub: Label = waiting_overlay.get_node_or_null("VBox/RoundLabel")
		if sub:
			sub.text = "Waiting for Round %d to begin..." % next_round_num

func exit_spectator_waiting_state() -> void:
	is_waiting_for_next_round = false
	if arena:
		arena.set("_is_waiting_for_next_round", false)
	if waiting_overlay and is_instance_valid(waiting_overlay):
		waiting_overlay.queue_free()
		waiting_overlay = null
	if p1_playfield:
		p1_playfield.pellet_spawning_enabled = true
		p1_playfield.fairy_spawning_enabled = true
	if p2_playfield:
		p2_playfield.pellet_spawning_enabled = true
		p2_playfield.fairy_spawning_enabled = true

func create_waiting_overlay(next_round_num: int) -> void:
	var overlay := PanelContainer.new()
	overlay.name = "SpectatorWaitingOverlay"
	overlay.anchors_preset = Control.PRESET_CENTER
	overlay.anchor_left = 0.5
	overlay.anchor_top = 0.5
	overlay.anchor_right = 0.5
	overlay.anchor_bottom = 0.5
	overlay.offset_left = -300.0
	overlay.offset_right = 300.0
	overlay.offset_top = -100.0
	overlay.offset_bottom = 100.0
	overlay.grow_horizontal = Control.GROW_DIRECTION_BOTH
	overlay.grow_vertical = Control.GROW_DIRECTION_BOTH
	overlay.z_index = 50
	
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.04, 0.07, 0.12, 0.94)
	style.border_color = Color(0.3, 0.8, 1.0, 0.9)
	style.border_width_left = 2
	style.border_width_right = 2
	style.border_width_top = 2
	style.border_width_bottom = 2
	style.corner_radius_top_left = 8
	style.corner_radius_top_right = 8
	style.corner_radius_bottom_right = 8
	style.corner_radius_bottom_left = 8
	style.content_margin_left = 24.0
	style.content_margin_right = 24.0
	style.content_margin_top = 20.0
	style.content_margin_bottom = 20.0
	overlay.add_theme_stylebox_override("panel", style)
	
	var vbox := VBoxContainer.new()
	vbox.name = "VBox"
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	vbox.add_theme_constant_override("separation", 10)
	
	var title := Label.new()
	title.text = "MATCH IN PROGRESS"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_override("font", load("res://Cirno.ttf"))
	title.add_theme_font_size_override("font_size", 28)
	title.add_theme_color_override("font_color", Color(1.0, 0.85, 0.3))
	vbox.add_child(title)
	
	var sub := Label.new()
	sub.name = "RoundLabel"
	sub.text = "Waiting for Round %d to begin..." % next_round_num
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	sub.add_theme_font_override("font", load("res://Cirno.ttf"))
	sub.add_theme_font_size_override("font_size", 20)
	sub.add_theme_color_override("font_color", Color(0.85, 0.9, 1.0))
	vbox.add_child(sub)
	
	var hint := Label.new()
	hint.text = "Spectator will sync automatically when the round begins.\n[Press Esc to Exit]"
	hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hint.add_theme_font_override("font", load("res://Cirno.ttf"))
	hint.add_theme_font_size_override("font_size", 16)
	hint.add_theme_color_override("font_color", Color(0.6, 0.7, 0.8, 0.8))
	vbox.add_child(hint)
	
	overlay.add_child(vbox)
	if arena:
		arena.add_child(overlay)
	waiting_overlay = overlay

func on_spectator_player_updated(p_num: int, pos: Vector2, anim_row: int, is_focus: bool, is_charging: bool, is_shooting: bool, active_chg: float, passive_chg: float) -> void:
	if arena and arena.has_method("_mark_remote_packet_received"):
		arena._mark_remote_packet_received()
	var target_field: Playfield = p1_playfield if p_num == 1 else p2_playfield
	if target_field and target_field.player:
		target_field.player.apply_remote_state(pos, anim_row, is_focus, is_charging, is_shooting, active_chg, passive_chg)

func on_spectator_attack_sent(p_num: int, death_pos: Vector2, pellet_count: int, has_big: bool, has_spirit: bool, has_extra: bool, sender_char: String, bounce_count: int, extra_targets: PackedVector2Array = PackedVector2Array(), sender_rank: int = 1) -> void:
	if arena and arena.has_method("_mark_remote_packet_received"):
		arena._mark_remote_packet_received()
	if arena and arena.has_method("_send_attack_between_fields"):
		if p_num == 1:
			arena._send_attack_between_fields(p1_viewport_container, p2_viewport_container, p2_playfield, death_pos, pellet_count, P1_MOTE_COLOR, has_big, has_spirit, has_extra, sender_char, bounce_count, sender_rank, extra_targets)
		else:
			arena._send_attack_between_fields(p2_viewport_container, p1_viewport_container, p1_playfield, death_pos, pellet_count, P2_MOTE_COLOR, has_big, has_spirit, has_extra, sender_char, bounce_count, sender_rank, extra_targets)

func on_spectator_charge_attack_fired(p_num: int, level: int, _attack_name: String, _sender_char: String) -> void:
	if arena and arena.has_method("_mark_remote_packet_received"):
		arena._mark_remote_packet_received()
	var target_field: Playfield = p1_playfield if p_num == 1 else p2_playfield
	var target_bar: SpellBar = p1_spell_bar if p_num == 1 else p2_spell_bar
	if target_bar:
		target_bar.play_release_flash()
	if target_field:
		target_field.execute_charge_attack(level)

func on_spectator_spellcard_activated(p_num: int, level: int, rank: int, sender_char: String) -> void:
	if arena and arena.has_method("_mark_remote_packet_received"):
		arena._mark_remote_packet_received()
	if arena:
		var intermission: bool = bool(arena.get("_round_intermission"))
		var match_over: bool = bool(arena.get("_match_over"))
		if intermission or match_over:
			return
	var caster_field: Playfield = p1_playfield if p_num == 1 else p2_playfield
	var target_field: Playfield = p2_playfield if p_num == 1 else p1_playfield
	var bar: SpellBar = p1_spell_bar if p_num == 1 else p2_spell_bar
	if bar:
		bar.play_release_flash()
	if arena and arena.has_method("_execute_action_stop"):
		arena._execute_action_stop(caster_field, target_field, level, rank, sender_char)

func on_spectator_health_updated(p_num: int, current: float, max_val: float) -> void:
	if arena and arena.has_method("_mark_remote_packet_received"):
		arena._mark_remote_packet_received()
	var target_gauge: HealthGauge = p1_health_gauge if p_num == 1 else p2_health_gauge
	if target_gauge:
		target_gauge.update_health(current, max_val)
	var target_field: Playfield = p1_playfield if p_num == 1 else p2_playfield
	if target_field and target_field.player:
		target_field.player.apply_remote_damage_visuals(current)

func on_spectator_hit_shockwave_spawned(p_num: int, landing_pos: Vector2) -> void:
	if arena and arena.has_method("_mark_remote_packet_received"):
		arena._mark_remote_packet_received()
	var target_field: Playfield = p1_playfield if p_num == 1 else p2_playfield
	if target_field:
		target_field.spawn_hit_shockwave(landing_pos)

func on_spectator_player_defeated(loser_num: int) -> void:
	if arena and arena.has_method("_mark_remote_packet_received"):
		arena._mark_remote_packet_received()
	if arena:
		var intermission: bool = bool(arena.get("_round_intermission"))
		var match_over: bool = bool(arena.get("_match_over"))
		if intermission or match_over:
			return
	var loser_field: Playfield = p1_playfield if loser_num == 1 else p2_playfield
	if loser_field and loser_field.player:
		loser_field.player.current_health = 0.0
		loser_field.player._on_defeat()

func on_spectator_round_transition(winner_num: int, p1_wins: int, p2_wins: int, next_round: int, is_final_win: bool) -> void:
	if arena and arena.has_method("_mark_remote_packet_received"):
		arena._mark_remote_packet_received()
	if is_waiting_for_next_round:
		exit_spectator_waiting_state()
	if arena and arena.has_method("_on_remote_round_transition"):
		arena._on_remote_round_transition(winner_num, p1_wins, p2_wins, next_round, is_final_win)

func on_spectator_round_started(current_round_num: int) -> void:
	if arena and arena.has_method("_mark_remote_packet_received"):
		arena._mark_remote_packet_received()
	if is_waiting_for_next_round:
		exit_spectator_waiting_state()
		var round_seed: int = arena._get_round_seed(current_round_num) if arena and arena.has_method("_get_round_seed") else 99991
		if p1_playfield:
			p1_playfield.reset_for_new_round()
			p1_playfield.set_round_seed(round_seed)
		if p2_playfield:
			p2_playfield.reset_for_new_round()
			p2_playfield.set_round_seed(round_seed)
		if match_timer:
			match_timer.reset()
			match_timer.start()

func on_spectator_match_ended() -> void:
	if arena and arena.has_method("_trigger_match_exit"):
		arena._trigger_match_exit("Match concluded. Returning to Main Menu...", false)

func setup_spectator_badge(p1_title: String, p2_title: String) -> void:
	var badge := PanelContainer.new()
	badge.name = "SpectatorBadge"
	badge.anchors_preset = Control.PRESET_CENTER_TOP
	badge.anchor_left = 0.5
	badge.anchor_right = 0.5
	badge.anchor_top = 0.0
	badge.anchor_bottom = 0.0
	badge.offset_left = -220.0
	badge.offset_right = 220.0
	badge.offset_top = 16.0
	badge.offset_bottom = 54.0
	badge.grow_horizontal = Control.GROW_DIRECTION_BOTH
	
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.04, 0.08, 0.12, 0.88)
	style.border_color = Color(0.3, 0.8, 1.0, 0.85)
	style.border_width_left = 1
	style.border_width_right = 1
	style.border_width_top = 1
	style.border_width_bottom = 1
	style.corner_radius_top_left = 6
	style.corner_radius_top_right = 6
	style.corner_radius_bottom_right = 6
	style.corner_radius_bottom_left = 6
	style.content_margin_left = 14.0
	style.content_margin_right = 14.0
	style.content_margin_top = 6.0
	style.content_margin_bottom = 6.0
	badge.add_theme_stylebox_override("panel", style)
	
	var hbox := HBoxContainer.new()
	hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	hbox.add_theme_constant_override("separation", 14)
	
	var lbl := Label.new()
	lbl.text = "👁 SPECTATING: %s vs %s" % [p1_title.to_upper(), p2_title.to_upper()]
	lbl.add_theme_font_override("font", load("res://Cirno.ttf"))
	lbl.add_theme_font_size_override("font_size", 20)
	lbl.add_theme_color_override("font_color", Color(0.4, 0.9, 1.0))
	hbox.add_child(lbl)
	
	var exit_lbl := Label.new()
	exit_lbl.text = "[Esc: Exit]"
	exit_lbl.add_theme_font_override("font", load("res://Cirno.ttf"))
	exit_lbl.add_theme_font_size_override("font_size", 16)
	exit_lbl.add_theme_color_override("font_color", Color(0.7, 0.7, 0.8, 0.75))
	hbox.add_child(exit_lbl)
	
	badge.add_child(hbox)
	if arena:
		arena.add_child(badge)

