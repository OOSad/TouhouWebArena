class_name PostMatch
extends Control

## Post-Match / Results Screen for Touhou Web Arena.
## Features winner and loser character portraits, optional "WINNER!" and "DEAD PARROT" badges,
## modular dialogue presets (VictoryDialogueDB), and a 2-player option handshake system.

const WINNER_BADGE_PATH: String = "res://assets/ui/winner_badge.png"
const DEAD_PARROT_PATH: String = "res://assets/ui/dead_parrot.png"
const VictoryDialogueDB = preload("res://scripts/resources/victory_dialogue_db.gd")
const CHARACTERS: String = "res://resources/characters/%s.tres"

## PoFV's "lo" face, shown for the loser (face ids index CharacterSprites.FACES)
const LOSE_FACE: int = 8

enum MenuOption {
	REMATCH = 0,
	CHANGE_CHARACTER = 1,
	MAIN_MENU = 2,
	SAVE_REPLAY = 3
}

const OPTION_NAMES: Array[String] = [
	"REMATCH",
	"CHANGE CHARACTER",
	"RETURN TO MAIN MENU",
	"SAVE REPLAY"
]

# Match configuration (defaults to P1 Reimu Winner vs P2 Marisa Loser for standalone testing)
var winner_player: int = 1
var loser_player: int = 2
var p1_character: String = "reimu"
var p2_character: String = "marisa"

# Handshake state
var p1_selected_option: int = 0
var p2_selected_option: int = 0
var p1_confirmed: bool = false
var p2_confirmed: bool = false
var is_resolving_handshake: bool = false

# Network state
var is_networked: bool = false
var local_player_number: int = 1


# Node references
@onready var p1_side: Control = %P1Side
@onready var p2_side: Control = %P2Side
@onready var p1_portrait: TextureRect = %P1Portrait
@onready var p2_portrait: TextureRect = %P2Portrait
@onready var p1_badge: TextureRect = %P1Badge
@onready var p2_badge: TextureRect = %P2Badge
@onready var p1_name_label: Label = %P1NameLabel
@onready var p2_name_label: Label = %P2NameLabel

@onready var dialogue_box: PanelContainer = %DialogueBox
@onready var speaker_label: Label = %SpeakerLabel
@onready var dialogue_text_label: Label = %DialogueTextLabel

@onready var save_replay_btn: Button = %SaveReplayBtn if has_node("%SaveReplayBtn") else null
@onready var save_replay_modal: Control = %SaveReplayModal if has_node("%SaveReplayModal") else null
@onready var slots_container: VBoxContainer = %SlotsContainer if has_node("%SlotsContainer") else null
@onready var close_save_replay_btn: Button = %CloseSaveReplayBtn if has_node("%CloseSaveReplayBtn") else null

@onready var option_buttons: Array[Button] = [
	%OptionRematch,
	%OptionChangeChar,
	%OptionMainMenu,
	%SaveReplayBtn
]
@onready var p1_cursor_labels: Array[Label] = [
	%P1CursorRematch,
	%P1CursorChangeChar,
	%P1CursorMainMenu,
	%P1CursorSaveReplay
]
@onready var p2_cursor_labels: Array[Label] = [
	%P2CursorRematch,
	%P2CursorChangeChar,
	%P2CursorMainMenu,
	%P2CursorSaveReplay
]
@onready var status_banner: Label = %StatusBanner

func _ready() -> void:
	_cache_nodes()
	_setup_option_buttons()
	_setup_save_replay_ui()
	# Pull match results from GameManager if available and inside tree
	if is_inside_tree():
		var gm: Node = get_node_or_null("/root/GameManager")
		if gm:
			winner_player = gm.last_winner_player
			loser_player = gm.last_loser_player
			p1_character = _normalize_char_name(gm.p1_character)
			p2_character = _normalize_char_name(gm.p2_character)
		
		var nm: Node = get_node_or_null("/root/NetworkManager")
		is_networked = (nm != null and nm.has_method("is_network_active") and nm.is_network_active())
		if is_networked:
			local_player_number = nm.player_number
			if not nm.post_match_cursor_updated.is_connected(_on_remote_cursor_updated):
				nm.post_match_cursor_updated.connect(_on_remote_cursor_updated)
			if not nm.post_match_confirmed.is_connected(_on_remote_confirmed):
				nm.post_match_confirmed.connect(_on_remote_confirmed)
			if not nm.connection_status_changed.is_connected(_on_connection_status_changed):
				nm.connection_status_changed.connect(_on_connection_status_changed)
	
	_setup_portraits_and_dialogue()
	_update_menu_display()
	_animate_intro()

func _cache_nodes() -> void:
	if p1_side == null and has_node("%P1Side"):
		p1_side = %P1Side
		p2_side = %P2Side
	if p1_portrait == null and has_node("%P1Portrait"):
		p1_portrait = %P1Portrait
		p2_portrait = %P2Portrait
		p1_badge = %P1Badge
		p2_badge = %P2Badge
		p1_name_label = %P1NameLabel
		p2_name_label = %P2NameLabel
		dialogue_box = %DialogueBox
		speaker_label = %SpeakerLabel
		dialogue_text_label = %DialogueTextLabel
		status_banner = %StatusBanner
		option_buttons = [%OptionRematch, %OptionChangeChar, %OptionMainMenu]
		p1_cursor_labels = [%P1CursorRematch, %P1CursorChangeChar, %P1CursorMainMenu]
		p2_cursor_labels = [%P2CursorRematch, %P2CursorChangeChar, %P2CursorMainMenu]
		if has_node("%SaveReplayBtn"):
			option_buttons.append(%SaveReplayBtn)
		if has_node("%P1CursorSaveReplay"):
			p1_cursor_labels.append(%P1CursorSaveReplay)
		if has_node("%P2CursorSaveReplay"):
			p2_cursor_labels.append(%P2CursorSaveReplay)
	if save_replay_btn == null and has_node("%SaveReplayBtn"):
		save_replay_btn = %SaveReplayBtn
		save_replay_modal = %SaveReplayModal
		slots_container = %SlotsContainer
		close_save_replay_btn = %CloseSaveReplayBtn

## Allows manual setup of match outcome (for tests or custom invocations)
func setup_match(winner_p: int, loser_p: int, p1_char: String, p2_char: String) -> void:
	_cache_nodes()
	winner_player = winner_p
	loser_player = loser_p
	p1_character = _normalize_char_name(p1_char)
	p2_character = _normalize_char_name(p2_char)
	_setup_portraits_and_dialogue()
	_update_menu_display()

## Characters with faces: the PoFV cast, and Clownpiece, whose come from LoLK.
func _normalize_char_name(raw_name: String) -> String:
	var lower := raw_name.to_lower().strip_edges()
	for key in CharacterSprites.PLAYER_SHEETS.keys() + ["clownpiece"]:
		if key in lower:
			return key
	return "reimu"

## One of a character's PoFV faces (ids 0..8, CharacterSprites.FACES), cut from the face
## strip built from th09.dat, or null if it isn't there.
static func get_portrait_texture(char_id: String, pofv_face: int) -> Texture2D:
	var path := CHARACTERS % char_id.to_lower()
	if not ResourceLoader.exists(path):
		return null
	var faces := (load(path) as CharacterData).face_sheet
	if faces == null:
		return null
	var size := CharacterSprites.FACE_SIZE
	var atlas := AtlasTexture.new()
	atlas.atlas = faces
	atlas.region = Rect2(clampi(pofv_face, 0, CharacterSprites.FACES.size() - 1) * size.x, 0, size.x, size.y)
	atlas.filter_clip = true
	return atlas

func _setup_portraits_and_dialogue() -> void:
	var winner_char := p1_character if (winner_player == 1) else p2_character
	var loser_char := p2_character if (winner_player == 1) else p1_character
	
	# Fetch dialogue quote & expression
	var quote_data := VictoryDialogueDB.get_random_quote(winner_char, loser_char)
	var winner_expr: int = quote_data.get("winner_expression", 1)
	var loser_expr: int = LOSE_FACE
	
	var winner_badge_tex: Texture2D = load(WINNER_BADGE_PATH) as Texture2D if ResourceLoader.exists(WINNER_BADGE_PATH) else null
	var dead_parrot_tex: Texture2D = load(DEAD_PARROT_PATH) as Texture2D if ResourceLoader.exists(DEAD_PARROT_PATH) else null
	
	# Configure Player 1
	var is_p1_winner := (winner_player == 1)
	var p1_expr := winner_expr if is_p1_winner else loser_expr
	p1_portrait.texture = get_portrait_texture(p1_character, p1_expr)
	p1_name_label.text = "1P: %s" % p1_character.to_upper()
	p1_portrait.modulate = Color.WHITE if is_p1_winner else Color(0.35, 0.35, 0.45, 1.0)
	if p1_badge:
		var p1_badge_tex: Texture2D = winner_badge_tex if is_p1_winner else dead_parrot_tex
		p1_badge.texture = p1_badge_tex
		p1_badge.visible = (p1_badge_tex != null)
		if p1_badge.visible:
			p1_badge.scale = Vector2.ZERO
	
	# Configure Player 2 (facing inward)
	var is_p2_winner := (winner_player == 2)
	var p2_expr := winner_expr if is_p2_winner else loser_expr
	p2_portrait.texture = get_portrait_texture(p2_character, p2_expr)
	p2_portrait.flip_h = true
	p2_name_label.text = "2P: %s" % p2_character.to_upper()
	p2_portrait.modulate = Color.WHITE if is_p2_winner else Color(0.35, 0.35, 0.45, 1.0)
	if p2_badge:
		var p2_badge_tex: Texture2D = winner_badge_tex if is_p2_winner else dead_parrot_tex
		p2_badge.texture = p2_badge_tex
		p2_badge.visible = (p2_badge_tex != null)
		if p2_badge.visible:
			p2_badge.scale = Vector2.ZERO
	
	# Configure Dialogue Box
	var winner_data := CharacterData.get_character(winner_char)
	if winner_data:
		speaker_label.text = "[ %s ]" % winner_data.display_name.to_upper()
		speaker_label.add_theme_color_override("font_color", winner_data.primary_color)
	else:
		speaker_label.text = "[ %s ]" % winner_char.to_upper()
		speaker_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.3))
	
	dialogue_text_label.text = quote_data.get("text", "Lorem ipsum dolor sit amet...")

func _animate_intro() -> void:
	if p1_side:
		var p1_target_x: float = p1_side.position.x
		p1_side.position.x = -500.0
		var tw_p1 := create_tween()
		tw_p1.tween_property(p1_side, "position:x", p1_target_x, 0.45).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	
	if p2_side:
		var p2_target_x: float = p2_side.position.x
		p2_side.position.x = 1940.0
		var tw_p2 := create_tween()
		tw_p2.tween_property(p2_side, "position:x", p2_target_x, 0.45).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	
	if (p1_badge and p1_badge.visible) or (p2_badge and p2_badge.visible):
		var tw_badges := create_tween()
		tw_badges.tween_interval(0.35)
		if p1_badge and p1_badge.visible:
			tw_badges.parallel().tween_property(p1_badge, "scale", Vector2.ONE, 0.35).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		if p2_badge and p2_badge.visible:
			tw_badges.parallel().tween_property(p2_badge, "scale", Vector2.ONE, 0.35).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)

	dialogue_box.modulate.a = 0.0
	var tw2 := create_tween()
	tw2.tween_property(dialogue_box, "modulate:a", 1.0, 0.3).set_delay(0.2)

func _setup_option_buttons() -> void:
	for i in range(option_buttons.size()):
		var btn: Button = option_buttons[i]
		if not is_instance_valid(btn):
			continue
		btn.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
		var me_callable: Callable = _on_option_mouse_entered.bind(i)
		if not btn.mouse_entered.is_connected(me_callable):
			btn.mouse_entered.connect(me_callable)
		var p_callable: Callable = _on_option_pressed.bind(i)
		if not btn.pressed.is_connected(p_callable):
			btn.pressed.connect(p_callable)

func _on_option_mouse_entered(index: int) -> void:
	if is_resolving_handshake:
		return
	var is_p1: bool = (not is_networked or local_player_number == 1)
	var is_my_confirmed: bool = p1_confirmed if is_p1 else p2_confirmed
	if is_my_confirmed:
		return
	
	if is_p1:
		if p1_selected_option != index:
			p1_selected_option = index
			AudioService.play_select()
			_update_menu_display()
			if is_networked:
				var nm: Node = get_node_or_null("/root/NetworkManager")
				if nm:
					nm.send_post_match_cursor(p1_selected_option)
	else:
		if p2_selected_option != index:
			p2_selected_option = index
			AudioService.play_select()
			_update_menu_display()
			if is_networked:
				var nm: Node = get_node_or_null("/root/NetworkManager")
				if nm:
					nm.send_post_match_cursor(p2_selected_option)

func _on_option_pressed(index: int) -> void:
	if is_resolving_handshake:
		return
	if index == MenuOption.SAVE_REPLAY:
		_open_save_replay_modal()
		return
	var is_p1: bool = (not is_networked or local_player_number == 1)
	var is_my_confirmed: bool = p1_confirmed if is_p1 else p2_confirmed
	if is_my_confirmed:
		return
	
	AudioService.play_confirm()
	var nm: Node = get_node_or_null("/root/NetworkManager") if is_networked else null
	if is_p1:
		p1_selected_option = index
		_confirm_p1_choice()
		if nm:
			nm.send_post_match_confirm(p1_selected_option)
	else:
		p2_selected_option = index
		_confirm_p2_choice()
		if nm:
			nm.send_post_match_confirm(p2_selected_option)

func _setup_save_replay_ui() -> void:
	if save_replay_btn:
		save_replay_btn.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	if close_save_replay_btn:
		close_save_replay_btn.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
		if not close_save_replay_btn.pressed.is_connected(_close_save_replay_modal):
			close_save_replay_btn.pressed.connect(_close_save_replay_modal)

func _open_save_replay_modal() -> void:
	if is_resolving_handshake:
		return
	var rep_mgr = get_node_or_null("/root/ReplayManager")
	if rep_mgr == null:
		return
	
	AudioService.play_confirm()
	_refresh_save_slots()
	if save_replay_modal:
		save_replay_modal.visible = true
	
	if slots_container and slots_container.get_child_count() > 0:
		var first_btn: Control = slots_container.get_child(0) as Control
		if first_btn:
			first_btn.grab_focus()

func _close_save_replay_modal() -> void:
	if save_replay_modal:
		save_replay_modal.visible = false
	AudioService.play_cancel()
	_update_menu_display()

func _refresh_save_slots() -> void:
	if slots_container == null:
		return
	for child in slots_container.get_children():
		child.queue_free()
	
	var rep_mgr = get_node_or_null("/root/ReplayManager")
	if rep_mgr == null:
		return
	
	var summaries: Array[Dictionary] = rep_mgr.get_all_slot_summaries()
	var slot_buttons: Array[Button] = []
	for i in range(summaries.size()):
		var slot_idx: int = i + 1
		var sum_data: Dictionary = summaries[i]
		var slot_btn := Button.new()
		slot_btn.custom_minimum_size = Vector2(0, 36)
		slot_btn.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
		slot_btn.add_theme_font_override("font", load("res://Cirno.ttf"))
		slot_btn.add_theme_font_size_override("font_size", 16)
		slot_btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
		
		if sum_data.get("is_empty", true):
			slot_btn.text = "  Slot %2d: [ EMPTY SLOT - Available ]" % slot_idx
			slot_btn.add_theme_color_override("font_color", Color(0.6, 0.7, 0.8))
		else:
			var p1_n: String = sum_data.get("p1_char", "reimu").to_upper()
			var p2_n: String = sum_data.get("p2_char", "marisa").to_upper()
			var win_str: String = "1P WIN" if sum_data.get("winner_num", 0) == 1 else "2P WIN"
			var dur: float = float(sum_data.get("duration", 0.0))
			var m: int = int(dur) / 60
			var s: int = int(dur) % 60
			var dt: String = sum_data.get("date_string", "")
			slot_btn.text = "  Slot %2d: %s vs %s [%s] %02d:%02d  (%s)" % [slot_idx, p1_n, p2_n, win_str, m, s, dt]
			slot_btn.add_theme_color_override("font_color", Color(1.0, 0.95, 0.5))
		
		slot_btn.pressed.connect(_on_slot_selected.bind(slot_idx))
		slot_btn.focus_entered.connect(func(): AudioService.play_select())
		slots_container.add_child(slot_btn)
		slot_buttons.append(slot_btn)
	
	for i in range(slot_buttons.size()):
		var prev_btn := slot_buttons[(i - 1 + slot_buttons.size()) % slot_buttons.size()]
		var next_btn := slot_buttons[(i + 1) % slot_buttons.size()]
		slot_buttons[i].focus_neighbor_top = slot_buttons[i].get_path_to(prev_btn)
		slot_buttons[i].focus_neighbor_bottom = slot_buttons[i].get_path_to(next_btn)

func _on_slot_selected(slot_idx: int) -> void:
	var rep_mgr = get_node_or_null("/root/ReplayManager")
	if rep_mgr == null:
		return
	var success: bool = rep_mgr.save_to_slot(slot_idx)
	if success:
		AudioService.play_confirm()
		if status_banner:
			status_banner.text = "✔ Replay saved to Slot %d!" % slot_idx
			status_banner.visible = true
		if save_replay_btn:
			save_replay_btn.text = "✔ REPLAY SAVED (SLOT %d)" % slot_idx
		if save_replay_modal:
			save_replay_modal.visible = false
	else:
		if status_banner:
			status_banner.text = "Failed to save replay."
			status_banner.visible = true

func _unhandled_input(event: InputEvent) -> void:
	if save_replay_modal and save_replay_modal.visible:
		if event.is_action_pressed("cancel") or (event is InputEventKey and event.pressed and not event.echo and (event.keycode == KEY_ESCAPE or event.physical_keycode == KEY_ESCAPE)):
			_close_save_replay_modal()
			get_viewport().set_input_as_handled()
			return
		return

	if is_resolving_handshake:
		return

	if event is InputEventKey and event.pressed and not event.echo and (
		event.keycode == KEY_S or event.physical_keycode == KEY_S or
		event.keycode == KEY_R or event.physical_keycode == KEY_R
	):
		_open_save_replay_modal()
		get_viewport().set_input_as_handled()
		return

	if is_networked:
		var nm: Node = get_node_or_null("/root/NetworkManager")
		var is_p1: bool = (local_player_number == 1)
		var is_my_confirmed: bool = p1_confirmed if is_p1 else p2_confirmed
		
		if not is_my_confirmed:
			var moved: bool = false
			if event.is_action_pressed("ui_up") or (event is InputEventKey and event.pressed and (event.keycode == KEY_W or event.physical_keycode == KEY_W)):
				if is_p1:
					p1_selected_option = (p1_selected_option - 1 + OPTION_NAMES.size()) % OPTION_NAMES.size()
				else:
					p2_selected_option = (p2_selected_option - 1 + OPTION_NAMES.size()) % OPTION_NAMES.size()
				moved = true
			elif event.is_action_pressed("ui_down") or (event is InputEventKey and event.pressed and (event.keycode == KEY_DOWN or event.physical_keycode == KEY_DOWN)):
				if is_p1:
					p1_selected_option = (p1_selected_option + 1) % OPTION_NAMES.size()
				else:
					p2_selected_option = (p2_selected_option + 1) % OPTION_NAMES.size()
				moved = true
			
			if moved:
				AudioService.play_select()
				_update_menu_display()
				if nm:
					nm.send_post_match_cursor(p1_selected_option if is_p1 else p2_selected_option)
				get_viewport().set_input_as_handled()
				return
			
			# Confirmation
			if event.is_action_pressed("shoot") or event.is_action_pressed("ui_accept"):
				AudioService.play_confirm()
				if is_p1:
					_confirm_p1_choice()
					if nm:
						nm.send_post_match_confirm(p1_selected_option)
				else:
					_confirm_p2_choice()
					if nm:
						nm.send_post_match_confirm(p2_selected_option)
				get_viewport().set_input_as_handled()
				return
			
			if event.is_action_pressed("cancel"):
				AudioService.play_confirm()
				if is_p1:
					p1_selected_option = MenuOption.MAIN_MENU
					_confirm_p1_choice()
					if nm:
						nm.send_post_match_confirm(p1_selected_option)
				else:
					p2_selected_option = MenuOption.MAIN_MENU
					_confirm_p2_choice()
					if nm:
						nm.send_post_match_confirm(p2_selected_option)
				get_viewport().set_input_as_handled()
				return
	else:
		# Offline / Solo testing
		if not p1_confirmed:
			var p1_moved: bool = false
			if event.is_action_pressed("ui_up") or (event is InputEventKey and event.pressed and (event.keycode == KEY_W or event.physical_keycode == KEY_W)):
				p1_selected_option = (p1_selected_option - 1 + OPTION_NAMES.size()) % OPTION_NAMES.size()
				p1_moved = true
			elif event.is_action_pressed("ui_down") or (event is InputEventKey and event.pressed and (event.keycode == KEY_DOWN or event.physical_keycode == KEY_DOWN)):
				p1_selected_option = (p1_selected_option + 1) % OPTION_NAMES.size()
				p1_moved = true
			
			if p1_moved:
				AudioService.play_select()
				_update_menu_display()
				get_viewport().set_input_as_handled()
				return
		
		# P2 numpad controls for local testing
		if not p2_confirmed and (event is InputEventKey and event.pressed):
			if event.keycode == KEY_KP_8 or event.physical_keycode == KEY_KP_8:
				p2_selected_option = (p2_selected_option - 1 + OPTION_NAMES.size()) % OPTION_NAMES.size()
				AudioService.play_select()
				_update_menu_display()
				get_viewport().set_input_as_handled()
				return
			elif event.keycode == KEY_KP_2 or event.physical_keycode == KEY_KP_2:
				p2_selected_option = (p2_selected_option + 1) % OPTION_NAMES.size()
				AudioService.play_select()
				_update_menu_display()
				get_viewport().set_input_as_handled()
				return
			elif event.keycode == KEY_KP_ENTER or event.physical_keycode == KEY_KP_ENTER:
				AudioService.play_confirm()
				_confirm_p2_choice()
				get_viewport().set_input_as_handled()
				return
		
		# P1 Confirm
		if event.is_action_pressed("shoot") or event.is_action_pressed("ui_accept"):
			if not p1_confirmed:
				AudioService.play_confirm()
				_confirm_p1_choice()
				get_viewport().set_input_as_handled()
			elif not p2_confirmed:
				AudioService.play_confirm()
				_confirm_p2_choice()
				get_viewport().set_input_as_handled()
		
		if event.is_action_pressed("cancel"):
			p1_selected_option = MenuOption.MAIN_MENU
			AudioService.play_confirm()
			_confirm_p1_choice()
			get_viewport().set_input_as_handled()

func _confirm_p1_choice() -> void:
	if p1_selected_option == MenuOption.SAVE_REPLAY:
		_open_save_replay_modal()
		return
	p1_confirmed = true
	if not is_networked:
		# In solo / local mode, P1's confirmation also confirms for P2 if not already confirmed
		p2_selected_option = p1_selected_option
		p2_confirmed = true
	_update_menu_display()
	_check_handshake()

func _confirm_p2_choice() -> void:
	if p2_selected_option == MenuOption.SAVE_REPLAY:
		_open_save_replay_modal()
		return
	p2_confirmed = true
	_update_menu_display()
	_check_handshake()

func _on_remote_cursor_updated(sender_num: int, option_idx: int) -> void:
	if sender_num == 1:
		p1_selected_option = option_idx
	else:
		p2_selected_option = option_idx
	AudioService.play_select()
	_update_menu_display()

func _on_remote_confirmed(sender_num: int, option_idx: int) -> void:
	if sender_num == 1:
		p1_selected_option = option_idx
		p1_confirmed = true
	else:
		p2_selected_option = option_idx
		p2_confirmed = true
	AudioService.play_confirm()
	_update_menu_display()
	_check_handshake()

func _on_connection_status_changed(status: String) -> void:
	var lower := status.to_lower()
	if "disconnected" in lower or "left" in lower:
		status_banner.text = "Opponent disconnected. Returning to Main Menu..."
		status_banner.visible = true
		var tree: SceneTree = get_tree() if is_inside_tree() else null
		if tree and not is_resolving_handshake:
			is_resolving_handshake = true
			var nm: Node = get_node_or_null("/root/NetworkManager")
			if nm and nm.has_method("cancel_matchmaking"):
				nm.cancel_matchmaking()
			tree.create_timer(1.2).timeout.connect(func():
				tree.change_scene_to_file("res://scenes/main_menu/main_menu.tscn")
			)

func _check_handshake() -> void:
	if is_resolving_handshake:
		return
	
	var tree: SceneTree = get_tree() if is_inside_tree() else null
	
	# Handshake resolution rules:
	# 1. If either player confirms MAIN_MENU -> Immediately return to Main Menu
	if (p1_confirmed and p1_selected_option == MenuOption.MAIN_MENU) or (p2_confirmed and p2_selected_option == MenuOption.MAIN_MENU):
		is_resolving_handshake = true
		status_banner.text = "Returning to Main Menu..."
		status_banner.visible = true
		var nm: Node = get_node_or_null("/root/NetworkManager")
		if nm:
			if is_networked and nm.has_method("cancel_matchmaking"):
				nm.cancel_matchmaking()
			if nm.has_method("set_in_bot_match"):
				nm.set_in_bot_match(false)
		if tree:
			tree.create_timer(0.9).timeout.connect(func():
				tree.change_scene_to_file("res://scenes/main_menu/main_menu.tscn")
			)
		return
	
	# 2. If either player confirms CHANGE_CHARACTER -> Immediately return to Character Select
	if (p1_confirmed and p1_selected_option == MenuOption.CHANGE_CHARACTER) or (p2_confirmed and p2_selected_option == MenuOption.CHANGE_CHARACTER):
		is_resolving_handshake = true
		status_banner.text = "Returning to Character Select..."
		status_banner.visible = true
		if tree:
			tree.create_timer(0.9).timeout.connect(func():
				tree.change_scene_to_file("res://scenes/character_select/character_select.tscn")
			)
		return
	
	# 3. If both choose REMATCH -> Reload Arena
	if p1_confirmed and p2_confirmed:
		if p1_selected_option == MenuOption.REMATCH and p2_selected_option == MenuOption.REMATCH:
			is_resolving_handshake = true
			status_banner.text = "Both chose Rematch! Restarting Match..."
			status_banner.visible = true
			var game_mgr = get_node_or_null("/root/GameManager")
			if game_mgr != null:
				game_mgr.reset_match_rounds()
			if tree:
				tree.create_timer(0.9).timeout.connect(func():
					if tree.current_scene and tree.current_scene.scene_file_path.ends_with("arena.tscn"):
						tree.reload_current_scene()
					else:
						tree.change_scene_to_file("res://scenes/arena/arena.tscn")
				)
			return
	
	if p1_confirmed or p2_confirmed:
		status_banner.text = "Waiting for other player..."
		status_banner.visible = true

func _update_menu_display() -> void:
	for i in range(option_buttons.size()):
		var btn := option_buttons[i]
		var p1_cur := p1_cursor_labels[i]
		var p2_cur := p2_cursor_labels[i]
		
		var is_p1_here := (p1_selected_option == i)
		var is_p2_here := (p2_selected_option == i)
		
		# Cursor text & visibility (always keep visible=true so HBox never collapses or shifts)
		p1_cur.visible = true
		p2_cur.visible = true
		if is_p1_here:
			p1_cur.text = "[1P READY]" if p1_confirmed else ">> [1P]"
			p1_cur.modulate = Color(0.3, 0.9, 1.0, 1.0) if not p1_confirmed else Color(0.4, 1.0, 0.4, 1.0)
		else:
			p1_cur.modulate.a = 0.0
			
		if is_p2_here:
			p2_cur.text = "[2P READY]" if p2_confirmed else "[2P] <<"
			p2_cur.modulate = Color(1.0, 0.4, 0.7, 1.0) if not p2_confirmed else Color(0.4, 1.0, 0.4, 1.0)
		else:
			p2_cur.modulate.a = 0.0
			
		# Highlight button style
		if is_p1_here or is_p2_here:
			btn.add_theme_color_override("font_color", Color(1.0, 0.95, 0.4))
		else:
			btn.add_theme_color_override("font_color", Color(0.7, 0.7, 0.75))
