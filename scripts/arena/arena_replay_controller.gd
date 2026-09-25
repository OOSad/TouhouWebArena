class_name ArenaReplayController
extends Node

## ArenaReplayController
## Manages match replay recording, playback timeline processing, pause/speed controls,
## and replay HUD badges for the Arena.

var arena: Node = null
var p1_playfield: Playfield = null
var p2_playfield: Playfield = null
var effects_overlay: Node2D = null
var match_timer: MatchTimer = null
var victory_banner: Control = null
var victory_label: Label = null

var is_replay_mode: bool = false
var replay_events: Array = []
var replay_event_idx: int = 0
var replay_elapsed_time: float = 0.0
var replay_total_duration: float = 0.0
var replay_record_timer: float = 0.0

var replay_hud_badge: PanelContainer = null
var replay_hud_label: Label = null

func setup(
	p_arena: Node,
	p_p1: Playfield,
	p_p2: Playfield,
	p_effects: Node2D,
	p_timer: MatchTimer,
	p_banner: Control,
	p_label: Label
) -> void:
	arena = p_arena
	p1_playfield = p_p1
	p2_playfield = p_p2
	effects_overlay = p_effects
	match_timer = p_timer
	victory_banner = p_banner
	victory_label = p_label

## Initializes replay state from ReplayManager if a playback session is active.
## Returns true if in replay mode.
func init_replay_mode() -> bool:
	var rep_mgr = get_node_or_null("/root/ReplayManager")
	if rep_mgr and rep_mgr.is_replaying:
		is_replay_mode = true
		replay_events = rep_mgr.active_replay.get("events", [])
		replay_event_idx = 0
		replay_elapsed_time = 0.0
		replay_total_duration = float(rep_mgr.active_replay.get("duration", 0.0))
		rep_mgr.playback_speed = 1.0
		rep_mgr.playback_paused = false
		Engine.time_scale = 1.0
		return true
	return false

## Starts auto-recording for non-spectator matches into ReplayManager.
func start_match_recording(
	p1_title: String,
	p2_title: String,
	p1_char: String,
	p2_char: String,
	stage_id: String,
	bgm_path: String,
	match_seed: int
) -> void:
	var rep_recorder = get_node_or_null("/root/ReplayManager")
	# Always start fresh: a match left early never calls finish_recording(), and skipping
	# while that one is still "recording" appended this match onto it under the old header.
	if rep_recorder:
		rep_recorder.start_recording(
			p1_title,
			p2_title,
			p1_char,
			p2_char,
			stage_id,
			bgm_path,
			match_seed
		)

## Concludes active recording in ReplayManager if active.
func finish_match_recording(winner_num: int, p1_wins: int, p2_wins: int, match_total_elapsed: float) -> void:
	var rep_mgr = get_node_or_null("/root/ReplayManager")
	if rep_mgr and rep_mgr.is_recording:
		rep_mgr.finish_recording(winner_num, p1_wins, p2_wins, match_total_elapsed)

## Constructs and displays the persistent HUD badge at the top-center of the arena.
func setup_replay_badge(p1_title: String, p2_title: String) -> void:
	var badge := PanelContainer.new()
	badge.name = "ReplayHUD"
	badge.anchors_preset = Control.PRESET_CENTER_TOP
	badge.anchor_left = 0.5
	badge.anchor_right = 0.5
	badge.anchor_top = 0.0
	badge.anchor_bottom = 0.0
	badge.offset_left = -340.0
	badge.offset_right = 340.0
	badge.offset_top = 16.0
	badge.offset_bottom = 54.0
	badge.grow_horizontal = Control.GROW_DIRECTION_BOTH
	badge.z_index = 60
	
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.04, 0.08, 0.12, 0.92)
	style.border_color = Color(0.3, 0.85, 1.0, 0.9)
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
	
	var lbl := Label.new()
	lbl.name = "Label"
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.add_theme_font_override("font", load("res://Cirno.ttf"))
	lbl.add_theme_font_size_override("font_size", 17)
	lbl.add_theme_color_override("font_color", Color(0.9, 0.95, 1.0))
	badge.add_child(lbl)
	if arena:
		arena.add_child(badge)
	
	replay_hud_badge = badge
	replay_hud_label = lbl
	update_replay_hud_badge()

## Refreshes the replay timeline clock and paused status on the HUD.
func update_replay_hud_badge() -> void:
	if replay_hud_label == null:
		return
	var rep_mgr = get_node_or_null("/root/ReplayManager")
	var paused_str := "❚❚ PAUSED | " if (rep_mgr and rep_mgr.playback_paused) else "▶ "
	var cur_m: int = int(replay_elapsed_time) / 60
	var cur_s: int = int(replay_elapsed_time) % 60
	var tot_m: int = int(replay_total_duration) / 60
	var tot_s: int = int(replay_total_duration) % 60
	replay_hud_label.text = "%sREPLAY %02d:%02d / %02d:%02d [Space: Pause | Esc: Exit]" % [
		paused_str, cur_m, cur_s, tot_m, tot_s
	]

## Pauses or unpauses playfields and effects layers during replay playback.
func apply_replay_pause(paused: bool) -> void:
	if paused:
		if p1_playfield:
			p1_playfield.process_mode = Node.PROCESS_MODE_DISABLED
		if p2_playfield:
			p2_playfield.process_mode = Node.PROCESS_MODE_DISABLED
		if effects_overlay:
			effects_overlay.process_mode = Node.PROCESS_MODE_DISABLED
	else:
		if p1_playfield:
			p1_playfield.process_mode = Node.PROCESS_MODE_INHERIT
		if p2_playfield:
			p2_playfield.process_mode = Node.PROCESS_MODE_INHERIT
		if effects_overlay:
			effects_overlay.process_mode = Node.PROCESS_MODE_INHERIT

## Handles Space (pause) and Escape (exit) during replay playback. Returns true if handled.
func handle_replay_input(event: InputEvent) -> bool:
	if not event is InputEventKey or not event.pressed or event.echo:
		return false
	var key_ev := event as InputEventKey
	var vp := arena.get_viewport() if arena else null
	if key_ev.keycode == KEY_ESCAPE or key_ev.physical_keycode == KEY_ESCAPE:
		if vp:
			vp.set_input_as_handled()
		apply_replay_pause(false)
		var rep_mgr = get_node_or_null("/root/ReplayManager")
		if rep_mgr:
			rep_mgr.stop_playback(arena.get_tree() if arena else null)
		elif arena:
			arena.get_tree().change_scene_to_file("res://scenes/main_menu/main_menu.tscn")
		return true
	elif key_ev.keycode == KEY_SPACE or key_ev.physical_keycode == KEY_SPACE:
		if vp:
			vp.set_input_as_handled()
		var rep_mgr = get_node_or_null("/root/ReplayManager")
		if rep_mgr:
			var is_p: bool = rep_mgr.toggle_pause()
			apply_replay_pause(is_p)
			update_replay_hud_badge()
		return true
	return false

## Advances playback if in replay mode, or captures player state samples if recording.
func process_frame(delta: float, match_total_elapsed: float) -> void:
	if is_replay_mode:
		_process_replay_playback(delta)
	else:
		replay_record_timer += delta
		if replay_record_timer >= 0.04:
			replay_record_timer -= 0.04
			record_players_state(match_total_elapsed)

func _process_replay_playback(delta: float) -> void:
	var rep_mgr = get_node_or_null("/root/ReplayManager")
	if rep_mgr == null:
		return
	if rep_mgr.playback_paused:
		update_replay_hud_badge()
		return
	
	replay_elapsed_time += delta
	if arena:
		arena.set("_match_total_elapsed", replay_elapsed_time)
	if match_timer:
		match_timer.set_time(replay_elapsed_time)
	
	while replay_event_idx < replay_events.size():
		var evt: Dictionary = replay_events[replay_event_idx]
		var evt_time: float = float(evt.get("t", 0.0))
		if evt_time > replay_elapsed_time:
			break
		dispatch_replay_event(evt)
		replay_event_idx += 1
	
	update_replay_hud_badge()
	
	var match_over: bool = bool(arena.get("_match_over")) if arena else false
	if replay_event_idx >= replay_events.size() and not match_over:
		if replay_elapsed_time >= (replay_total_duration + 2.0):
			trigger_replay_concluded("MATCH")

## Dispatches an event from the recorded event stream to the arena.
func dispatch_replay_event(evt: Dictionary) -> void:
	if arena == null:
		return
	var evt_type: String = str(evt.get("type", ""))
	match evt_type:
		"state":
			var p_num: int = int(evt.get("p", 1))
			var target_field: Playfield = p1_playfield if p_num == 1 else p2_playfield
			if target_field and target_field.player:
				target_field.player.apply_remote_state(
					Vector2(float(evt.get("x", 0.0)), float(evt.get("y", 0.0))),
					int(evt.get("r", 0)),
					bool(evt.get("f", false)),
					bool(evt.get("c", false)),
					bool(evt.get("s", false)),
					float(evt.get("ac", 0.0)),
					float(evt.get("pc", 1.0))
				)
		"attack":
			var p_num: int = int(evt.get("p", 1))
			var death_pos := Vector2(float(evt.get("x", 0.0)), float(evt.get("y", 0.0)))
			var pellet_count: int = int(evt.get("pellet_count", 0))
			var has_big: bool = bool(evt.get("has_big", false))
			var has_spirit: bool = bool(evt.get("has_spirit", false))
			var has_extra: bool = bool(evt.get("has_extra", false))
			var sender_char: String = str(evt.get("sender_char", "reimu"))
			var bounce_count: int = int(evt.get("bounce_count", 0))
			var extra_targets: PackedVector2Array = MoteDispatcher.targets_from_flat(evt.get("extra_targets", []))
			var sender_rank: int = int(evt.get("rank", 1))
			if arena.has_method("_on_spectator_attack_sent"):
				arena._on_spectator_attack_sent(p_num, death_pos, pellet_count, has_big, has_spirit, has_extra, sender_char, bounce_count, extra_targets, sender_rank)
		"charge_attack":
			var p_num: int = int(evt.get("p", 1))
			var level: int = int(evt.get("level", 1))
			var attack_name: String = str(evt.get("attack_name", ""))
			var sender_char: String = str(evt.get("sender_char", ""))
			if arena.has_method("_on_spectator_charge_attack_fired"):
				arena._on_spectator_charge_attack_fired(p_num, level, attack_name, sender_char)
		"spellcard":
			var p_num: int = int(evt.get("p", 1))
			var level: int = int(evt.get("level", 2))
			var rank: int = int(evt.get("rank", 1))
			var sender_char: String = str(evt.get("sender_char", ""))
			if arena.has_method("_on_spectator_spellcard_activated"):
				arena._on_spectator_spellcard_activated(p_num, level, rank, sender_char)
		"health":
			var p_num: int = int(evt.get("p", 1))
			var hp: float = float(evt.get("hp", 5.0))
			var max_hp: float = float(evt.get("max_hp", 5.0))
			if arena.has_method("_on_spectator_health_updated"):
				arena._on_spectator_health_updated(p_num, hp, max_hp)
		"hit_shockwave":
			var p_num: int = int(evt.get("p", 1))
			var pos := Vector2(float(evt.get("x", 0.0)), float(evt.get("y", 0.0)))
			if arena.has_method("_on_spectator_hit_shockwave_spawned"):
				arena._on_spectator_hit_shockwave_spawned(p_num, pos)
		"player_defeated":
			var loser_num: int = int(evt.get("loser_num", 1))
			if arena.has_method("_on_spectator_player_defeated"):
				arena._on_spectator_player_defeated(loser_num)
		"round_transition":
			var winner_num: int = int(evt.get("winner_num", 1))
			var p1_w: int = int(evt.get("p1_wins", 0))
			var p2_w: int = int(evt.get("p2_wins", 0))
			var next_rnd: int = int(evt.get("next_round", 2))
			var is_fin: bool = bool(evt.get("is_final", false))
			if arena.has_method("_on_spectator_round_transition"):
				arena._on_spectator_round_transition(winner_num, p1_w, p2_w, next_rnd, is_fin)
		"round_started":
			var r_num: int = int(evt.get("round_num", 1))
			if arena.has_method("_on_spectator_round_started"):
				arena._on_spectator_round_started(r_num)

## Shows the replay completion banner and updates the HUD.
func trigger_replay_concluded(winner_name: String) -> void:
	if arena:
		if arena.get("_match_over") == true:
			return
		arena.set("_match_over", true)
	if victory_label:
		victory_label.text = "%s - REPLAY COMPLETE" % winner_name
	if victory_banner:
		victory_banner.visible = true
	if replay_hud_label:
		replay_hud_label.text = "✔ REPLAY CONCLUDED | [Esc: Exit to Main Menu]"

## Captures position and input flags from both players for replay logging.
func record_players_state(match_total_elapsed: float) -> void:
	var rep_mgr = get_node_or_null("/root/ReplayManager")
	if rep_mgr == null or not rep_mgr.is_recording:
		return
	if p1_playfield and p1_playfield.player and not p1_playfield.player.is_dead:
		var p1: Player = p1_playfield.player
		rep_mgr.record_player_state(
			match_total_elapsed, 1, p1.position, p1._anim_row,
			p1.is_focusing, p1.is_charging, p1.is_shooting,
			p1.active_charge, p1.passive_charge
		)
	if p2_playfield and p2_playfield.player and not p2_playfield.player.is_dead:
		var p2: Player = p2_playfield.player
		rep_mgr.record_player_state(
			match_total_elapsed, 2, p2.position, p2._anim_row,
			p2.is_focusing, p2.is_charging, p2.is_shooting,
			p2.active_charge, p2.passive_charge
		)

## Records an individual match event (attack, charge attack, spellcard, etc.) with timestamp.
func record_event(evt: Dictionary, match_total_elapsed: float) -> void:
	if is_replay_mode:
		return
	var rep_mgr = get_node_or_null("/root/ReplayManager")
	if rep_mgr and rep_mgr.is_recording:
		evt["t"] = snappedf(match_total_elapsed, 0.001)
		rep_mgr.record_event(evt)

