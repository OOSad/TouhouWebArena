extends Control

const CharacterCarouselSlot = preload("res://scenes/character_select/character_carousel_slot.gd")

## Each character's select-screen art, by id; a character without one shows their portrait_texture.
const DAIRI_PORTRAIT: String = "res://assets/ui/dairi/%s.png"

## The carousel: the roster, then the Random slot.
var playable_characters: Array[String] = CharacterData.get_roster_names()
var characters: Array[String] = _carousel_names()

func _carousel_names() -> Array[String]:
	var names: Array[String] = playable_characters.duplicate()
	names.append("Random")
	return names

func _dairi_portrait(c_data: CharacterData) -> Texture2D:
	if c_data == null:
		return null
	var path := DAIRI_PORTRAIT % c_data.character_id
	if ResourceLoader.exists(path):
		return load(path)
	return c_data.portrait_texture

func _resolve_character(char_name: String) -> String:
	if char_name.to_lower().strip_edges() == "random":
		return playable_characters.pick_random()
	return char_name

var p1_index: int = 0
var p2_index: int = 1

var p1_handicap: float = 5.0
var p2_handicap: float = 5.0

var is_local_confirmed: bool = false
var is_remote_confirmed: bool = false
var is_shift_held: bool = false

enum SelectState { SELECT_PLAYER, SELECT_AI, CONFIRMED }
var select_state: SelectState = SelectState.SELECT_PLAYER

# UI References
@onready var title_label: Label = %TitleLabel
@onready var footer_hints: Label = %FooterHints
@onready var status_banner: Label = %StatusBanner

@onready var carousel_track: Control = get_node_or_null("%CarouselTrack")
@onready var slots_container: Control = get_node_or_null("%SlotsContainer")
var carousel_slots: Array[CharacterCarouselSlot] = []

@onready var p1_card: CharacterCard = %P1Card
@onready var p2_card: CharacterCard = %P2Card

@onready var background_tex: TextureRect = %BackgroundTex
@onready var twilight_scrim: ColorRect = %TwilightScrim

var is_returning_to_menu: bool = false

func _get_network_manager() -> Node:
	var tree := Engine.get_main_loop() as SceneTree
	if tree and tree.root and tree.root.has_node("NetworkManager"):
		return tree.root.get_node("NetworkManager")
	return null

func _get_game_manager() -> Node:
	var tree := Engine.get_main_loop() as SceneTree
	if tree and tree.root and tree.root.has_node("GameManager"):
		return tree.root.get_node("GameManager")
	return null

func _ready() -> void:
	if status_banner:
		status_banner.visible = false
	
	var gm := _get_game_manager()
	if gm:
		p1_handicap = gm.p1_handicap_hp
		p2_handicap = gm.p2_handicap_hp

	var nm := _get_network_manager()
	var is_networked: bool = (nm != null and nm.is_network_active())
	if nm:
		if not nm.opponent_character_changed.is_connected(_on_opponent_character_changed):
			nm.opponent_character_changed.connect(_on_opponent_character_changed)
		if nm.has_signal("opponent_handicap_changed") and not nm.opponent_handicap_changed.is_connected(_on_opponent_handicap_changed):
			nm.opponent_handicap_changed.connect(_on_opponent_handicap_changed)
		if not nm.opponent_confirmed.is_connected(_on_opponent_confirmed):
			nm.opponent_confirmed.connect(_on_opponent_confirmed)
		if not nm.connection_status_changed.is_connected(_on_connection_status_changed):
			nm.connection_status_changed.connect(_on_connection_status_changed)
		if nm.has_signal("opponent_backed_out") and not nm.opponent_backed_out.is_connected(_on_opponent_backed_out):
			nm.opponent_backed_out.connect(_on_opponent_backed_out)
	
	if not is_networked:
		if gm:
			gm.p2_is_ai = true
		if nm and nm.has_method("set_in_bot_match"):
			nm.set_in_bot_match(true)
		if title_label:
			title_label.text = "Select Your Character"
	if carousel_track == null:
		carousel_track = get_node_or_null("CarouselTrack") as Control
	if slots_container == null:
		slots_container = get_node_or_null("CarouselTrack/SlotsContainer") as Control

	carousel_slots.clear()
	if slots_container:
		for child in slots_container.get_children():
			if child is CharacterCarouselSlot:
				carousel_slots.append(child)
	
	var slot_pitch := 155.0
	var total_span := (carousel_slots.size() - 1) * slot_pitch + 240.0
	var start_x := -total_span * 0.5
	
	for i in range(mini(carousel_slots.size(), characters.size())):
		var slot := carousel_slots[i]
		slot.position = Vector2(start_x + i * slot_pitch, -110.0)
		var c_name := characters[i]
		var c_data := CharacterData.get_character(c_name)
		var char_id := c_data.character_id if c_data else ""
		var portrait_tex: Texture2D = _dairi_portrait(c_data)
		slot.setup(i, c_data, portrait_tex)
		if not slot.slot_clicked.is_connected(_select_character_by_index):
			slot.slot_clicked.connect(_select_character_by_index)
	
	_update_display()
	
	# Maintain or start menu BGM
	AudioService.play_music("res://assets/music/01_flower_reflecting_mound.ogg")

func _select_character_by_index(index: int) -> void:
	if is_returning_to_menu or select_state == SelectState.CONFIRMED:
		return
	
	var nm := _get_network_manager()
	var is_networked: bool = (nm != null and nm.is_network_active())
	
	if is_networked:
		if is_local_confirmed:
			return
		var is_p1: bool = (nm.player_number == 1)
		var current_idx := p1_index if is_p1 else p2_index
		if current_idx == index:
			# Second click confirms selection
			_confirm_local_selection()
		else:
			if is_p1:
				p1_index = index
				_send_selection(characters[p1_index])
			else:
				p2_index = index
				_send_selection(characters[p2_index])
			AudioService.play_select()
			_update_display()
	else:
		# Offline / Practice Mode
		match select_state:
			SelectState.SELECT_PLAYER:
				if p1_index == index:
					_confirm_local_selection()
				else:
					p1_index = index
					AudioService.play_select()
					_update_display()
			SelectState.SELECT_AI:
				if p2_index == index:
					_confirm_ai_selection()
				else:
					p2_index = index
					AudioService.play_select()
					_update_display()

func _on_opponent_backed_out() -> void:
	_handle_opponent_exit("Opponent backed out. Returning to Main Menu...")

func _on_connection_status_changed(status: String) -> void:
	var lower := status.to_lower()
	if "disconnected" in lower or "left" in lower:
		_handle_opponent_exit("Opponent backed out. Returning to Main Menu...")

func _handle_opponent_exit(msg: String) -> void:
	if is_returning_to_menu:
		return
	is_returning_to_menu = true
	status_banner.text = msg
	status_banner.modulate = Color(1.0, 0.45, 0.45, 1.0)
	status_banner.visible = true
	
	var nm := _get_network_manager()
	if nm:
		nm.cancel_matchmaking()
	
	var tree := Engine.get_main_loop() as SceneTree
	if tree:
		tree.create_timer(1.2).timeout.connect(func():
			var t := Engine.get_main_loop() as SceneTree
			if t:
				t.change_scene_to_file("res://scenes/main_menu/main_menu.tscn")
		)

func _input(event: InputEvent) -> void:
	if event is InputEventKey and not event.echo:
		if event.keycode == KEY_SHIFT:
			if event.pressed != is_shift_held:
				_set_shift_inspect_mode(event.pressed)

func _process(_delta: float) -> void:
	if not is_returning_to_menu and select_state != SelectState.CONFIRMED:
		var shift_pressed := Input.is_key_pressed(KEY_SHIFT)
		if shift_pressed != is_shift_held:
			_set_shift_inspect_mode(shift_pressed)

func _set_shift_inspect_mode(inspecting: bool) -> void:
	is_shift_held = inspecting
	if p1_card:
		p1_card.set_inspect_mode(inspecting)
	if p2_card:
		p2_card.set_inspect_mode(inspecting)
	_update_footer_hints()

func _update_footer_hints() -> void:
	if not footer_hints:
		return
	if is_shift_held:
		footer_hints.text = "[Left / Right] Set Handicap      [Release Shift] Close Specs      [Z / Click] Confirm      [X / Esc] Back"
		return
	
	var nm := _get_network_manager()
	var is_networked: bool = (nm != null and nm.is_network_active())
	if is_networked:
		footer_hints.text = "[Left / Right / Up / Down / Click] Choose Character      [Hold Shift] View Specs      [Z / Click] Confirm      [X / Esc] Back to Menu"
	else:
		if select_state == SelectState.SELECT_PLAYER:
			footer_hints.text = "[Left / Right / Up / Down / Click] Choose Character      [Hold Shift] View Specs      [Z / Click] Confirm Selection      [X / Esc] Back to Menu"
		elif select_state == SelectState.SELECT_AI:
			footer_hints.text = "[Left / Right / Up / Down / Click] Choose Opponent      [Hold Shift] View Specs      [Z / Click] Start Match      [X / Esc] Reselect Character"

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("cancel") or (event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_RIGHT and event.is_pressed()):
		var vp := get_viewport()
		if vp:
			vp.set_input_as_handled()
		
		var nm := _get_network_manager()
		var is_networked: bool = (nm != null and nm.is_network_active())
		if not is_networked and select_state == SelectState.SELECT_AI:
			# Cancel AI selection and return to picking Player 1 character
			AudioService.play_cancel()
			select_state = SelectState.SELECT_PLAYER
			is_local_confirmed = false
			if title_label:
				title_label.text = "Select Your Character"
			_update_footer_hints()
			status_banner.visible = false
			_update_display()
			return
		
		AudioService.play_cancel()
		_return_to_main_menu()
		return
	
	if select_state == SelectState.CONFIRMED or is_returning_to_menu:
		return
	
	var nm := _get_network_manager()
	var is_networked: bool = (nm != null and nm.is_network_active())
	if is_networked and is_local_confirmed:
		return
	
	var is_left: bool = event.is_action_pressed("ui_left") or (event is InputEventKey and event.pressed and not event.echo and event.keycode == KEY_LEFT)
	var is_right: bool = event.is_action_pressed("ui_right") or (event is InputEventKey and event.pressed and not event.echo and event.keycode == KEY_RIGHT)
	if (is_shift_held or Input.is_key_pressed(KEY_SHIFT)) and (is_left or is_right):
		var step: float = -0.5 if is_left else 0.5
		_adjust_handicap(step)
		var vp := get_viewport()
		if vp:
			vp.set_input_as_handled()
		return

	var is_prev: bool = event.is_action_pressed("ui_up") or (not is_shift_held and is_left)
	var is_next: bool = event.is_action_pressed("ui_down") or (not is_shift_held and is_right)

	if is_prev:
		if is_networked:
			var is_p1: bool = (nm.player_number == 1)
			if is_p1:
				p1_index = (p1_index - 1 + characters.size()) % characters.size()
				_send_selection(characters[p1_index])
			else:
				p2_index = (p2_index - 1 + characters.size()) % characters.size()
				_send_selection(characters[p2_index])
		else:
			if select_state == SelectState.SELECT_PLAYER:
				p1_index = (p1_index - 1 + characters.size()) % characters.size()
			elif select_state == SelectState.SELECT_AI:
				p2_index = (p2_index - 1 + characters.size()) % characters.size()
		AudioService.play_select()
		_update_display()
		var vp := get_viewport()
		if vp:
			vp.set_input_as_handled()
	elif is_next:
		if is_networked:
			var is_p1: bool = (nm.player_number == 1)
			if is_p1:
				p1_index = (p1_index + 1) % characters.size()
				_send_selection(characters[p1_index])
			else:
				p2_index = (p2_index + 1) % characters.size()
				_send_selection(characters[p2_index])
		else:
			if select_state == SelectState.SELECT_PLAYER:
				p1_index = (p1_index + 1) % characters.size()
			elif select_state == SelectState.SELECT_AI:
				p2_index = (p2_index + 1) % characters.size()
		AudioService.play_select()
		_update_display()
		var vp := get_viewport()
		if vp:
			vp.set_input_as_handled()
	elif event.is_action_pressed("shoot") or event.is_action_pressed("ui_accept"):
		if is_networked:
			_confirm_local_selection()
		else:
			if select_state == SelectState.SELECT_PLAYER:
				_confirm_local_selection()
			elif select_state == SelectState.SELECT_AI:
				_confirm_ai_selection()
		var vp := get_viewport()
		if vp:
			vp.set_input_as_handled()

func _send_selection(char_name: String) -> void:
	var nm := _get_network_manager()
	if nm:
		nm.send_character_selection(char_name)

func _on_opponent_character_changed(char_name: String) -> void:
	var idx: int = characters.find(char_name)
	if idx != -1:
		var nm := _get_network_manager()
		var is_p1: bool = (nm == null or nm.player_number == 1)
		if is_p1:
			# Opponent is P2
			p2_index = idx
		else:
			# Opponent is P1
			p1_index = idx
		_update_display()

func _adjust_handicap(delta_hp: float) -> void:
	var nm := _get_network_manager()
	var is_networked: bool = (nm != null and nm.is_network_active())
	var gm := _get_game_manager()
	
	if is_networked:
		var is_p1: bool = (nm.player_number == 1)
		if is_p1:
			var prev := p1_handicap
			p1_handicap = clampf(snappedf(p1_handicap + delta_hp, 0.1), 0.5, 5.0)
			if not is_equal_approx(prev, p1_handicap):
				AudioService.play_select()
				if p1_card:
					p1_card.set_handicap(p1_handicap)
				if gm:
					gm.set_p1_handicap(p1_handicap)
				nm.send_handicap_selection(p1_handicap)
		else:
			var prev := p2_handicap
			p2_handicap = clampf(snappedf(p2_handicap + delta_hp, 0.1), 0.5, 5.0)
			if not is_equal_approx(prev, p2_handicap):
				AudioService.play_select()
				if p2_card:
					p2_card.set_handicap(p2_handicap)
				if gm:
					gm.set_p2_handicap(p2_handicap)
				nm.send_handicap_selection(p2_handicap)
	else:
		if select_state == SelectState.SELECT_PLAYER:
			var prev := p1_handicap
			p1_handicap = clampf(snappedf(p1_handicap + delta_hp, 0.1), 0.5, 5.0)
			if not is_equal_approx(prev, p1_handicap):
				AudioService.play_select()
				if p1_card:
					p1_card.set_handicap(p1_handicap)
				if gm:
					gm.set_p1_handicap(p1_handicap)
		elif select_state == SelectState.SELECT_AI:
			var prev := p2_handicap
			p2_handicap = clampf(snappedf(p2_handicap + delta_hp, 0.1), 0.5, 5.0)
			if not is_equal_approx(prev, p2_handicap):
				AudioService.play_select()
				if p2_card:
					p2_card.set_handicap(p2_handicap)
				if gm:
					gm.set_p2_handicap(p2_handicap)

func _on_opponent_handicap_changed(hp: float) -> void:
	var nm := _get_network_manager()
	var is_p1: bool = (nm == null or nm.player_number == 1)
	var gm := _get_game_manager()
	if is_p1:
		p2_handicap = clampf(hp, 0.5, 5.0)
		if gm:
			gm.set_p2_handicap(p2_handicap)
		if p2_card:
			p2_card.set_handicap(p2_handicap)
	else:
		p1_handicap = clampf(hp, 0.5, 5.0)
		if gm:
			gm.set_p1_handicap(p1_handicap)
		if p1_card:
			p1_card.set_handicap(p1_handicap)

func _update_display() -> void:
	var nm := _get_network_manager()
	var is_networked: bool = (nm != null and nm.is_network_active())
	var show_p2_pointer: bool = is_networked or (select_state == SelectState.SELECT_AI or select_state == SelectState.CONFIRMED)
	var is_p2_cpu: bool = (not is_networked)

	for i in range(carousel_slots.size()):
		var slot := carousel_slots[i]
		var is_p1 := (p1_index == i)
		var is_p2 := (p2_index == i) and show_p2_pointer
		slot.set_selection(is_p1, is_p2, is_p2_cpu)

	# Update Flanking Character Cards
	var p1_char_name: String = characters[p1_index] if (p1_index >= 0 and p1_index < characters.size()) else "reimu"
	var p2_char_name: String = characters[p2_index] if (p2_index >= 0 and p2_index < characters.size()) else "marisa"
	var p1_data: CharacterData = CharacterData.get_character(p1_char_name)
	var p2_data: CharacterData = CharacterData.get_character(p2_char_name)
	if p1_card and p1_data:
		p1_card.set_character(p1_data, 1, false)
		p1_card.set_handicap(p1_handicap)
	if p2_card and p2_data:
		p2_card.set_character(p2_data, 2, is_p2_cpu)
		p2_card.set_handicap(p2_handicap)

func _confirm_local_selection() -> void:
	var nm := _get_network_manager()
	var is_networked: bool = (nm != null and nm.is_network_active())
	if is_networked:
		if is_local_confirmed:
			return
		AudioService.play_confirm()
		is_local_confirmed = true
		var is_p1: bool = (nm.player_number == 1)
		var local_char: String = characters[p1_index if is_p1 else p2_index]
		if local_char == "Random":
			var resolved := _resolve_character(local_char)
			# Lock the cursor onto the roll, or every later _resolve_character() re-rolls
			# and this client plays a different character than the one the opponent was sent.
			if is_p1:
				p1_index = characters.find(resolved)
			else:
				p2_index = characters.find(resolved)
			_update_display()
			_send_selection(resolved)
		
		nm.send_confirm_selection()
		
		var game_mgr := _get_game_manager()
		if game_mgr:
			var c1 := _resolve_character(characters[p1_index])
			var c2 := _resolve_character(characters[p2_index])
			game_mgr.set_p1_character(c1)
			game_mgr.set_p2_character(c2)
			game_mgr.set_p1_handicap(p1_handicap)
			game_mgr.set_p2_handicap(p2_handicap)
		
		_check_both_confirmed()
	else:
		# Offline / Practice mode: Lock P1 and advance to picking AI sparring partner
		if select_state != SelectState.SELECT_PLAYER:
			return
		AudioService.play_confirm()
		select_state = SelectState.SELECT_AI
		if title_label:
			title_label.text = "Select Opponent"
		_update_footer_hints()
		if status_banner:
			status_banner.visible = false
		_update_display()

func _confirm_ai_selection() -> void:
	if select_state != SelectState.SELECT_AI:
		return
	AudioService.play_confirm()
	select_state = SelectState.CONFIRMED
	is_local_confirmed = true
	is_remote_confirmed = true
	
	var final_p1 := _resolve_character(characters[p1_index])
	var final_p2 := _resolve_character(characters[p2_index])
	
	var game_mgr := _get_game_manager()
	if game_mgr:
		game_mgr.p2_is_ai = true
		game_mgr.set_p1_character(final_p1)
		game_mgr.set_p2_character(final_p2)
		game_mgr.set_p1_handicap(p1_handicap)
		game_mgr.set_p2_handicap(p2_handicap)
		game_mgr.set_player_names(game_mgr.player_nickname, "CPU (%s)" % final_p2)
	
	var nm := _get_network_manager()
	if nm and nm.has_method("set_in_bot_match"):
		nm.set_in_bot_match(true)
	
	if status_banner:
		status_banner.text = "Match Starting: %s vs %s!" % [final_p1, final_p2]
		status_banner.modulate = Color(0.3, 1.0, 0.5, 1.0)
		status_banner.visible = true
	if footer_hints:
		footer_hints.text = "Entering Match..."
	_start_arena_transition()

func _on_opponent_confirmed() -> void:
	is_remote_confirmed = true
	var game_mgr := _get_game_manager()
	if game_mgr:
		var c1 := _resolve_character(characters[p1_index])
		var c2 := _resolve_character(characters[p2_index])
		game_mgr.set_p1_character(c1)
		game_mgr.set_p2_character(c2)
		game_mgr.set_p1_handicap(p1_handicap)
		game_mgr.set_p2_handicap(p2_handicap)
	_check_both_confirmed()

func _check_both_confirmed() -> void:
	var nm := _get_network_manager()
	var is_networked: bool = (nm != null and nm.is_network_active())
	if not is_networked:
		if select_state != SelectState.CONFIRMED:
			_confirm_ai_selection()
	elif is_local_confirmed and is_remote_confirmed:
		var final_p1 := _resolve_character(characters[p1_index])
		var final_p2 := _resolve_character(characters[p2_index])
		var game_mgr := _get_game_manager()
		if game_mgr:
			game_mgr.set_p1_character(final_p1)
			game_mgr.set_p2_character(final_p2)
		if status_banner:
			status_banner.text = "Both Players Ready: %s vs %s! Entering Match..." % [final_p1, final_p2]
			status_banner.visible = true
		print("[CharacterSelect] Both players confirmed: %s vs %s" % [final_p1, final_p2])
		_start_arena_transition()
	elif is_local_confirmed:
		if status_banner:
			status_banner.text = "You are Ready! Waiting for Opponent..."
			status_banner.visible = true
	elif is_remote_confirmed:
		if status_banner:
			status_banner.text = "Opponent is Ready! Press [Z] to Confirm."
			status_banner.visible = true

func _start_arena_transition() -> void:
	var game_mgr := _get_game_manager()
	if game_mgr != null:
		game_mgr.reset_match_rounds()
		var tree := Engine.get_main_loop() as SceneTree
		if tree:
			tree.create_timer(0.4).timeout.connect(func():
				if game_mgr.has_method("change_scene_with_wipe"):
					game_mgr.change_scene_with_wipe("res://scenes/arena/arena.tscn")
				else:
					var t := Engine.get_main_loop() as SceneTree
					if t:
						t.change_scene_to_file("res://scenes/arena/arena.tscn")
			)
	else:
		var tree := Engine.get_main_loop() as SceneTree
		if tree:
			tree.create_timer(0.8).timeout.connect(func():
				var t := Engine.get_main_loop() as SceneTree
				if t:
					t.change_scene_to_file("res://scenes/arena/arena.tscn")
			)

func _return_to_main_menu() -> void:
	if is_returning_to_menu:
		return
	is_returning_to_menu = true
	
	if status_banner:
		status_banner.text = "Returning to Main Menu..."
		status_banner.modulate = Color(0.9, 0.9, 0.9, 1.0)
		status_banner.visible = true
	
	var nm := _get_network_manager()
	if nm:
		if nm.has_method("set_in_bot_match"):
			nm.set_in_bot_match(false)
		nm.send_character_select_backout()
		var tree := Engine.get_main_loop() as SceneTree
		if tree:
			tree.create_timer(0.15).timeout.connect(func():
				if nm:
					nm.cancel_matchmaking()
				var t := Engine.get_main_loop() as SceneTree
				if t:
					t.change_scene_to_file("res://scenes/main_menu/main_menu.tscn")
			)
			return
		else:
			nm.cancel_matchmaking()
	
	var tree := Engine.get_main_loop() as SceneTree
	if tree:
		tree.change_scene_to_file("res://scenes/main_menu/main_menu.tscn")
