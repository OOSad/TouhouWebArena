class_name ArenaPresentation
extends Node

## ArenaPresentation
## Handles match visual effects, screen shakes, screen wipe transitions,
## victory/forfeit banner animations, and the round defeat sequence choreography.

var arena: Node = null
var p1_playfield: Playfield = null
var p2_playfield: Playfield = null
var screen_wipe: ScreenWipe = null
var victory_banner: Control = null
var victory_label: Label = null
var forfeit_banner: Control = null
var forfeit_label: Label = null
var p1_round_wins: RoundWinsDisplay = null
var p2_round_wins: RoundWinsDisplay = null
var match_timer: MatchTimer = null

func setup(
	p_arena: Node,
	p_p1: Playfield,
	p_p2: Playfield,
	p_wipe: ScreenWipe,
	p_v_banner: Control,
	p_v_label: Label,
	p_f_banner: Control,
	p_f_label: Label,
	p_p1_wins: RoundWinsDisplay,
	p_p2_wins: RoundWinsDisplay,
	p_timer: MatchTimer
) -> void:
	arena = p_arena
	p1_playfield = p_p1
	p2_playfield = p_p2
	screen_wipe = p_wipe
	victory_banner = p_v_banner
	victory_label = p_v_label
	forfeit_banner = p_f_banner
	forfeit_label = p_f_label
	p1_round_wins = p_p1_wins
	p2_round_wins = p_p2_wins
	match_timer = p_timer

## Shakes the target playfield with decaying trauma and snaps cleanly back to Vector2.ZERO.
func play_screen_shake(target_playfield: Playfield, duration: float = 0.35, intensity: float = 10.0) -> void:
	if target_playfield == null:
		return
	var orig_pos: Vector2 = Vector2.ZERO
	var tree := get_tree()
	if tree == null:
		return
	var tw := tree.create_tween()
	var step_time: float = 0.035
	var steps: int = int(duration / step_time)
	for i in range(steps):
		var factor: float = 1.0 - float(i) / float(steps)
		var offset := Vector2(randf_range(-intensity, intensity) * factor, randf_range(-intensity, intensity) * factor)
		tw.tween_property(target_playfield, "position", orig_pos + offset, step_time)
	tw.tween_property(target_playfield, "position", orig_pos, step_time)

## Displays the forfeit / exit confirmation banner.
func show_forfeit_banner(text: String) -> void:
	if forfeit_banner and forfeit_label:
		forfeit_label.text = text
		forfeit_banner.visible = true

## Hides the forfeit banner.
func hide_forfeit_banner() -> void:
	if forfeit_banner:
		forfeit_banner.visible = false

## Shows the victory banner with tweened fade-in.
func show_victory_banner(text: String, fade_duration: float = 0.3) -> void:
	if victory_label:
		victory_label.text = text
	if victory_banner:
		victory_banner.visible = true
		victory_banner.modulate.a = 0.0
		var tree := get_tree()
		if tree:
			var tw := tree.create_tween()
			tw.tween_property(victory_banner, "modulate:a", 1.0, fade_duration)

## Fades out the victory banner.
func hide_victory_banner(fade_duration: float = 0.2) -> void:
	if victory_banner and victory_banner.visible:
		var tree := get_tree()
		if tree:
			var tw := tree.create_tween()
			tw.tween_property(victory_banner, "modulate:a", 0.0, fade_duration)
		else:
			victory_banner.visible = false

## Orchestrates the complete round defeat sequence:
## 1. Immediate 0.5s hitstop freeze
## 2. Loser defeat impact visuals (flash, shockwave, shake)
## 3. Winner post-KO movement freedom
## 4. Victory banner or ScreenWipe transition into next round
func play_round_defeat_sequence(
	winner_num: int,
	loser_num: int,
	winner_wins: int,
	p1_w: int,
	p2_w: int,
	next_round: int,
	is_final_match_win: bool
) -> void:
	var winner_name: String = "PLAYER %d" % winner_num
	var winner_char: String = "reimu"
	var loser_char: String = "marisa"
	
	var game_mgr = get_node_or_null("/root/GameManager")
	var p1_c: String = game_mgr.p1_character if (game_mgr and not game_mgr.p1_character.is_empty()) else (p1_playfield.character_id if p1_playfield else "youmu")
	var p2_c: String = game_mgr.p2_character if (game_mgr and not game_mgr.p2_character.is_empty()) else (p2_playfield.character_id if p2_playfield else "marisa")
	winner_char = p1_c if winner_num == 1 else p2_c
	loser_char = p2_c if winner_num == 1 else p1_c
	var char_data := CharacterData.get_character(winner_char)
	if char_data:
		winner_name = char_data.display_name.to_upper()
	
	# Update spinning sakura flower display immediately
	if p1_round_wins:
		p1_round_wins.set_wins(p1_w, winner_num == 1)
	if p2_round_wins:
		p2_round_wins.set_wins(p2_w, winner_num == 2)
	
	var loser_playfield: Playfield = p1_playfield if loser_num == 1 else p2_playfield
	var winner_playfield: Playfield = p2_playfield if loser_num == 1 else p1_playfield
	
	var loser_death_pos := Vector2(300.0, 816.0)
	if loser_playfield and loser_playfield.player:
		loser_death_pos = loser_playfield.player.position
	
	# 1. Immediate Hitstop (0.5s freeze): Both playfields freeze completely on the lethal impact frame
	if match_timer:
		match_timer.pause()
	if p1_playfield:
		p1_playfield.process_mode = Node.PROCESS_MODE_DISABLED
	if p2_playfield:
		p2_playfield.process_mode = Node.PROCESS_MODE_DISABLED
	
	var tree := get_tree()
	if tree:
		await tree.create_timer(0.5).timeout
	
	# 2. Defeat Impact:
	# - Loser's sprite goes completely transparent / disabled
	if loser_playfield and loser_playfield.player:
		loser_playfield.player.hide_on_defeat()
	
	# - Loser's side of the field flashes red for a couple of frames
	if loser_playfield:
		loser_playfield.play_defeat_flash()
	
	# - Faint red shockwave effect emanates from the dead player's location
	if loser_playfield:
		loser_playfield.spawn_defeat_shockwave(loser_death_pos)
	
	# - Screen shake on the loser's playfield
	play_screen_shake(loser_playfield, 0.35, 10.0)
	
	# 3. Winner Post-KO Movement:
	if winner_playfield and winner_playfield.player:
		winner_playfield.player.is_invulnerable = true
	if winner_playfield:
		winner_playfield.process_mode = Node.PROCESS_MODE_INHERIT
	
	# Only reveal Victory banner on final match win
	if is_final_match_win:
		if arena:
			arena.set("_match_over", true)
		if game_mgr != null:
			game_mgr.record_match_result(winner_num, loser_num, winner_char, loser_char)
		show_victory_banner("%s WINS THE MATCH!" % winner_name, 0.3)
	else:
		if victory_banner:
			victory_banner.visible = false
	
	if tree:
		await tree.create_timer(2.0).timeout
	
	# Re-freeze winner's playfield before transition
	if winner_playfield:
		winner_playfield.process_mode = Node.PROCESS_MODE_DISABLED
	
	if is_final_match_win:
		hide_victory_banner(0.3)
		if tree:
			await tree.create_timer(0.4).timeout
		
		var is_replay: bool = bool(arena.get("_is_replay_mode")) if arena else false
		if is_replay:
			if arena and arena.get("replay_controller"):
				arena.replay_controller.trigger_replay_concluded(winner_name)
			return
		
		if arena and arena.get("replay_controller"):
			arena.replay_controller.finish_match_recording(winner_num, p1_w, p2_w, float(arena.get("_match_total_elapsed")))
		
		var net_mgr = get_node_or_null("/root/NetworkManager")
		var is_net: bool = bool(arena.get("_is_networked")) if arena else false
		if is_net and net_mgr and net_mgr.is_spectator:
			if arena and arena.has_method("_trigger_match_exit"):
				arena._trigger_match_exit("Match concluded. Returning to Main Menu...", false)
			return
		
		var post_match_scene := preload("res://scenes/post_match/post_match.tscn")
		var post_match := post_match_scene.instantiate() as Control
		post_match.z_index = 100
		if arena:
			arena.add_child(post_match)
		if post_match.has_method("setup_match"):
			post_match.setup_match(winner_num, loser_num, p1_c, p2_c)
	else:
		hide_victory_banner(0.2)
		
		if screen_wipe:
			screen_wipe.wipe_closed.connect(func():
				if arena and arena.has_method("_reset_fields_for_next_round"):
					arena._reset_fields_for_next_round(next_round)
			, CONNECT_ONE_SHOT)
			
			screen_wipe.wipe_completed.connect(func():
				if arena:
					arena.set("_round_intermission", false)
				if p1_playfield:
					p1_playfield.process_mode = Node.PROCESS_MODE_INHERIT
					p1_playfield.start_round_intro()
				if p2_playfield:
					p2_playfield.process_mode = Node.PROCESS_MODE_INHERIT
					p2_playfield.start_round_intro()
				if match_timer:
					match_timer.start()
				var net_m = get_node_or_null("/root/NetworkManager")
				var is_net: bool = bool(arena.get("_is_networked")) if arena else false
				var local_p: int = int(arena.get("local_player_num")) if arena else 1
				if is_net and local_p == 1 and net_m and net_m.spectator_count > 0:
					net_m.broadcast_spectator_event({
						"type": "round_started",
						"current_round": next_round
					})
			, CONNECT_ONE_SHOT)
			
			screen_wipe.play_round_transition(next_round)
		else:
			if arena and arena.has_method("_reset_fields_for_next_round"):
				arena._reset_fields_for_next_round(next_round)
			if arena:
				arena.set("_round_intermission", false)
			if p1_playfield:
				p1_playfield.process_mode = Node.PROCESS_MODE_INHERIT
				p1_playfield.start_round_intro()
			if p2_playfield:
				p2_playfield.process_mode = Node.PROCESS_MODE_INHERIT
				p2_playfield.start_round_intro()
			if match_timer:
				match_timer.start()
			var net_m = get_node_or_null("/root/NetworkManager")
			var is_net: bool = bool(arena.get("_is_networked")) if arena else false
			var local_p: int = int(arena.get("local_player_num")) if arena else 1
			if is_net and local_p == 1 and net_m and net_m.spectator_count > 0:
				net_m.broadcast_spectator_event({
					"type": "round_started",
					"current_round": next_round
				})

