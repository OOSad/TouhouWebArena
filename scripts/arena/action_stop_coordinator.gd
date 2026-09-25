class_name ActionStopCoordinator
extends Node

## ActionStopCoordinator
## Coordinates screen freezes, spellcard announcements, player visual shaders,
## warning banners, and timed execution of spellcard effects during Level 2-4 activations.

const ACTION_STOP_DURATION: float = 0.58

var arena: Node = null
var p1_playfield: Playfield = null
var p2_playfield: Playfield = null

var is_active: bool = false
var _action_stop_tween: Tween = null
var _caster_field: Playfield = null

func setup(p_arena: Node, p_p1: Playfield, p_p2: Playfield) -> void:
	arena = p_arena
	p1_playfield = p_p1
	p2_playfield = p_p2

func execute_action_stop(caster_field: Playfield, target_field: Playfield, level: int, rank: int, sender_char: String) -> void:
	if arena and (arena.get("_round_intermission") == true or arena.get("_match_over") == true):
		return
	if is_active:
		conclude_action_stop()
	
	is_active = true
	_caster_field = caster_field
	
	var sender_data: CharacterData = CharacterData.get_character(sender_char)
	var spell_name: String = ""
	if level == 2 and sender_data and sender_data.spellcard_lv2:
		spell_name = sender_data.spellcard_lv2.spellcard_name
	elif level == 3 and sender_data and sender_data.spellcard_lv3:
		spell_name = sender_data.spellcard_lv3.spellcard_name
	elif level >= 4:
		spell_name = "%s - Level 4 Boss Card" % sender_char.to_upper()
	else:
		spell_name = "%s - Level %d Spellcard" % [sender_char.to_upper(), level]
	
	# 1. Caster Field: show authentic banner, turn player sprite red
	if caster_field:
		if level >= 4:
			caster_field.dispel_active_boss()
		caster_field.trigger_spellcard_declaration(level, spell_name)
		if caster_field.player:
			caster_field.player.set_red_silhouette(true)
			caster_field.player.set_action_stop(true)
	
	# Freeze target player movement as well during the action stop, and show incoming warning banner
	if target_field:
		target_field.show_spellcard_warning(spell_name, level, rank)
		if target_field.player:
			target_field.player.set_action_stop(true)
	
	# 2. Freeze scene tree
	if arena and arena.is_inside_tree():
		arena.get_tree().paused = true
	
	# 3. Action stop timer (unpaused tween)
	if _action_stop_tween and _action_stop_tween.is_valid():
		_action_stop_tween.kill()
	
	if arena and arena.is_inside_tree():
		_action_stop_tween = arena.create_tween()
		_action_stop_tween.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
		_action_stop_tween.tween_interval(ACTION_STOP_DURATION)
		_action_stop_tween.tween_callback(func():
			is_active = false
			conclude_action_stop()
			var is_intermission: bool = arena != null and arena.get("_round_intermission") == true
			var is_over: bool = arena != null and arena.get("_match_over") == true
			if is_intermission or is_over:
				return
			# Execute heavy shockwave on caster field AFTER action stop completes
			if caster_field and is_instance_valid(caster_field):
				caster_field.trigger_spellcard_shockwave(level)
			trigger_spellcard_effects(target_field, level, rank, sender_char)
		)

func conclude_action_stop() -> void:
	if _action_stop_tween and _action_stop_tween.is_valid():
		_action_stop_tween.kill()
		_action_stop_tween = null
	
	is_active = false
	
	if _caster_field and is_instance_valid(_caster_field) and _caster_field.player:
		_caster_field.player.set_red_silhouette(false)
	_caster_field = null
	
	if p1_playfield and is_instance_valid(p1_playfield) and p1_playfield.player:
		p1_playfield.player.set_action_stop(false)
	if p2_playfield and is_instance_valid(p2_playfield) and p2_playfield.player:
		p2_playfield.player.set_action_stop(false)
	
	if arena and arena.is_inside_tree():
		arena.get_tree().paused = false

func trigger_spellcard_effects(target_field: Playfield, level: int, rank: int, sender_char: String) -> void:
	if target_field == null or not is_instance_valid(target_field):
		return
	var sender_data: CharacterData = CharacterData.get_character(sender_char)
	var spawn_pos := Vector2(Playfield.PLAYFIELD_WIDTH / 2.0, 110.0)
	if level == 2:
		if sender_data and sender_data.spellcard_lv2:
			target_field.execute_spellcard(sender_data.spellcard_lv2, rank, spawn_pos)
		else:
			target_field.spawn_extra_attack(sender_char, spawn_pos)
	elif level == 3:
		if sender_data and sender_data.spellcard_lv3:
			target_field.execute_spellcard(sender_data.spellcard_lv3, rank, spawn_pos)
		else:
			target_field.spawn_extra_attack(sender_char, spawn_pos + Vector2(-60.0, 0.0))
			target_field.spawn_extra_attack(sender_char, spawn_pos + Vector2(60.0, 0.0))
	elif level >= 4:
		# Boss Rank is its own independent track (not the caster's spellcard rank) -
		# the boss's difficulty scales with the TARGET field's own boss rank progression.
		target_field.spawn_boss(sender_char, target_field.current_boss_rank)

