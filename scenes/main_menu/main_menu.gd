extends Control

## The main menu: nickname / server / room fields, the four menu buttons, and the online queue
## through to match start or spectating. The animated title, the Settings and Replays modals
## and the lobby list each live in their own helper under scripts/main_menu/.

const CHARACTER_SELECT_SCENE: String = "res://scenes/character_select/character_select.tscn"
const ARENA_SCENE: String = "res://scenes/arena/arena.tscn"

@onready var nickname_input: LineEdit = %NicknameInput
@onready var server_input: LineEdit = %ServerInput if has_node("%ServerInput") else null
@onready var password_input: LineEdit = %PasswordInput if has_node("%PasswordInput") else null
@onready var online_match_btn: Button = %OnlineMatchBtn
@onready var practice_btn: Button = %PracticeBtn if has_node("%PracticeBtn") else null
@onready var replays_btn: Button = %ReplaysBtn if has_node("%ReplaysBtn") else null
@onready var settings_btn: Button = %SettingsBtn if has_node("%SettingsBtn") else null
@onready var queue_status_label: Label = %QueueStatusLabel
@onready var match_found_label: Label = %MatchFoundLabel

var title_screen: MainMenuTitleScreen
var settings: MainMenuSettings
var replays: MainMenuReplays
var lobby_list: MainMenuLobbyList

var is_queued: bool = false
var queue_elapsed: float = 0.0

func _get_game_manager() -> Node:
	if not is_inside_tree():
		return null
	return get_node_or_null("/root/GameManager")

func _get_network_manager() -> Node:
	if not is_inside_tree():
		return null
	return get_node_or_null("/root/NetworkManager")

func _ready() -> void:
	title_screen = _add_helper(MainMenuTitleScreen.new(), "TitleScreen") as MainMenuTitleScreen
	settings = _add_helper(MainMenuSettings.new(), "Settings") as MainMenuSettings
	replays = _add_helper(MainMenuReplays.new(), "Replays") as MainMenuReplays
	lobby_list = _add_helper(MainMenuLobbyList.new(), "LobbyList") as MainMenuLobbyList
	settings.closed.connect(_focus_if_present.bind(settings_btn))
	replays.closed.connect(_focus_if_present.bind(replays_btn))
	replays.watch_requested.connect(_on_watch_replay_requested)
	lobby_list.watch_requested.connect(_on_watch_room_requested)

	var gm := _get_game_manager()
	if gm:
		nickname_input.text = gm.player_nickname

	var nm := _get_network_manager()
	if server_input and nm:
		server_input.text = nm.signaling_url
		server_input.text_changed.connect(_on_server_url_changed)
	if password_input and nm and not nm.current_password.is_empty():
		password_input.text = nm.current_password

	nickname_input.text_changed.connect(_on_nickname_changed)
	online_match_btn.pressed.connect(_on_online_match_pressed)
	if practice_btn:
		practice_btn.pressed.connect(_on_practice_pressed)
	if replays_btn:
		replays_btn.pressed.connect(_on_replays_pressed)
	if settings_btn:
		settings_btn.pressed.connect(_on_settings_pressed)

	# Menu sound as focus moves
	for control in [nickname_input, server_input, password_input, online_match_btn, practice_btn, replays_btn, settings_btn]:
		if control:
			control.focus_entered.connect(_on_ui_focus_entered)

	# Connect NetworkManager events
	if nm:
		nm.lobby_player_list_updated.connect(_on_lobby_player_list_updated)
		nm.match_ready.connect(_on_match_ready)
		if nm.has_signal("spectator_joined_match"):
			nm.spectator_joined_match.connect(_on_spectator_joined_match)
		# Passively connect to the lobby as soon as menu loads
		nm.connect_to_lobby(nickname_input.text)
		if nm.has_method("set_in_bot_match"):
			nm.set_in_bot_match(false)

	match_found_label.visible = false
	_on_lobby_player_list_updated([])

	# Configure vertical arrow-key focus loop
	var buttons_chain: Array[Control] = []
	for control in [nickname_input, server_input, password_input, online_match_btn, practice_btn, replays_btn, settings_btn]:
		if control:
			buttons_chain.append(control)
	for i in range(buttons_chain.size()):
		var curr := buttons_chain[i]
		var prev := buttons_chain[(i - 1 + buttons_chain.size()) % buttons_chain.size()]
		var nxt := buttons_chain[(i + 1) % buttons_chain.size()]
		curr.focus_neighbor_top = curr.get_path_to(prev)
		curr.focus_neighbor_bottom = curr.get_path_to(nxt)

	# Start with Online Match highlighted by default
	if is_inside_tree():
		online_match_btn.grab_focus()

	# Play main menu and title theme
	AudioService.play_music("res://assets/music/01_flower_reflecting_mound.ogg")

func _add_helper(helper: Node, helper_name: String) -> Node:
	helper.name = helper_name
	add_child(helper)
	helper.setup(self)
	return helper

func _focus_if_present(control: Control) -> void:
	if control:
		control.grab_focus()

func _process(delta: float) -> void:
	if not is_queued:
		return
	queue_elapsed += delta
	var minutes: int = floori(queue_elapsed / 60.0)
	var seconds: int = int(queue_elapsed) % 60

	# Animated ellipsis dots
	var dots: String = ".".repeat(int(queue_elapsed * 2.0) % 4)
	var pw_str: String = password_input.text.strip_edges() if password_input else ""
	if pw_str.is_empty():
		queue_status_label.text = "Status: Searching for opponent%s  [%02d:%02d]" % [dots, minutes, seconds]
	else:
		queue_status_label.text = "Status: Searching [Room: %s]%s  [%02d:%02d]" % [pw_str, dots, minutes, seconds]

func _unhandled_input(event: InputEvent) -> void:
	if title_screen.intro_active:
		if (event is InputEventKey and event.pressed and not event.echo) or (event is InputEventMouseButton and event.pressed):
			title_screen.skip_intro()
			get_viewport().set_input_as_handled()
			return

	if event.is_action_pressed("cancel"):
		if replays.is_open():
			AudioService.play_cancel()
			replays.close()
			get_viewport().set_input_as_handled()
		elif settings.is_open():
			AudioService.play_cancel()
			settings.close()
			get_viewport().set_input_as_handled()
		elif is_queued:
			_leave_queue()
			AudioService.play_cancel()
			get_viewport().set_input_as_handled()

func _on_nickname_changed(new_text: String) -> void:
	var gm := _get_game_manager()
	if gm:
		gm.set_nickname(new_text)
	var nm := _get_network_manager()
	if nm:
		nm.update_nickname(new_text)

func _on_server_url_changed(new_url: String) -> void:
	var nm := _get_network_manager()
	if nm:
		nm.set_signaling_url(new_url)

func _on_online_match_pressed() -> void:
	if not is_queued:
		AudioService.play_confirm()
		_enter_queue()
	else:
		AudioService.play_cancel()
		_leave_queue()

func _on_practice_pressed() -> void:
	AudioService.play_confirm()
	if is_queued:
		_leave_queue()

	var gm := _get_game_manager()
	if gm:
		gm.set_nickname(nickname_input.text)
		gm.p2_is_ai = true
		gm.set_player_names(nickname_input.text, "CPU")

	var nm := _get_network_manager()
	if nm:
		if nm.is_searching:
			nm.cancel_matchmaking()
		if nm.has_method("set_in_bot_match"):
			nm.set_in_bot_match(true)

	get_tree().change_scene_to_file(CHARACTER_SELECT_SCENE)

func _on_settings_pressed() -> void:
	AudioService.play_confirm()
	settings.open()

func _on_replays_pressed() -> void:
	AudioService.play_confirm()
	replays.open()

## Locks or unlocks the fields and side buttons while queued, and relabels Online Match.
func _set_queue_controls(queued: bool) -> void:
	nickname_input.editable = not queued
	if server_input:
		server_input.editable = not queued
	if password_input:
		password_input.editable = not queued
	if practice_btn:
		practice_btn.disabled = queued
	if replays_btn:
		replays_btn.disabled = queued
	if queued:
		online_match_btn.text = "Cancel Queue"
		online_match_btn.add_theme_color_override("font_color", Color(1.0, 0.4, 0.4))
		online_match_btn.add_theme_color_override("font_hover_color", Color(1.0, 0.7, 0.7))
	else:
		online_match_btn.text = "Online Match"
		online_match_btn.remove_theme_color_override("font_color")
		online_match_btn.remove_theme_color_override("font_hover_color")

func _enter_queue() -> void:
	var gm := _get_game_manager()
	if gm:
		gm.set_nickname(nickname_input.text)

	var nm := _get_network_manager()
	if server_input and nm:
		nm.set_signaling_url(server_input.text)
		if not nm.ws_connected:
			nm.connect_to_lobby(nickname_input.text)

	is_queued = true
	queue_elapsed = 0.0
	match_found_label.visible = false
	_set_queue_controls(true)

	# Tell NetworkManager we are now searching
	if nm:
		nm.set_searching(true, password_input.text.strip_edges() if password_input else "")

func _leave_queue() -> void:
	is_queued = false
	_set_queue_controls(false)
	queue_status_label.text = "Status: Idle"

	var nm := _get_network_manager()
	if nm:
		nm.set_searching(false, "")

func _on_lobby_player_list_updated(players: Array) -> void:
	var local_pw: String = ""
	var nm := _get_network_manager()
	if password_input and not password_input.text.strip_edges().is_empty():
		local_pw = password_input.text.strip_edges()
	elif nm and not nm.current_password.is_empty():
		local_pw = nm.current_password
	lobby_list.refresh(players, local_pw)

func _on_match_ready(p1_name: String, p2_name: String) -> void:
	is_queued = false
	queue_status_label.text = "Status: Match Confirmed!"
	match_found_label.text = ">> Match Found: %s vs %s! <<" % [p1_name, p2_name]
	match_found_label.visible = true
	_set_queue_controls(false)

	print("[MainMenu] Match successfully formed: %s vs %s" % [p1_name, p2_name])

	# Transition to Character Select after a brief confirmation delay
	var tween := create_tween()
	tween.tween_interval(1.5)
	tween.tween_callback(func():
		get_tree().change_scene_to_file(CHARACTER_SELECT_SCENE)
	)

func _on_watch_replay_requested(slot_idx: int) -> void:
	var rm: Node = get_node_or_null("/root/ReplayManager")
	if rm and rm.has_method("start_playback"):
		if is_queued:
			_leave_queue()
		rm.start_playback(slot_idx, get_tree())

func _on_ui_focus_entered() -> void:
	AudioService.play_select()

func _on_watch_room_requested(room_id: String, password: String) -> void:
	AudioService.play_confirm()
	if is_queued:
		_leave_queue()
	var nm := _get_network_manager()
	if nm:
		nm.spectate_room(room_id, password)

func _on_spectator_joined_match(_room_id: String, p1_name: String, p2_name: String) -> void:
	match_found_label.text = ">> Spectating: %s vs %s <<" % [p1_name, p2_name]
	match_found_label.visible = true
	AudioService.play_confirm()

	var nm := _get_network_manager()
	if nm == null:
		return

	# If matchState already has character data, the arena is already running: go straight in
	if not nm.spectator_p1_char.is_empty() and not nm.spectator_p2_char.is_empty():
		queue_status_label.text = "Status: Joining match..."
		_change_scene_after(1.0, ARENA_SCENE)
	else:
		# Players are still in character select: wait for match_init
		queue_status_label.text = "Status: Waiting for match to begin..."
		nm.spectator_match_init.connect(_on_spectator_match_init_received, CONNECT_ONE_SHOT)

func _on_spectator_match_init_received(
	_p1_char: String, _p2_char: String, _stage_id: String,
	_p1_wins: int, _p2_wins: int, _current_round: int, _is_round_active: bool
) -> void:
	queue_status_label.text = "Status: Match starting!"
	_change_scene_after(0.5, ARENA_SCENE)

func _change_scene_after(delay: float, scene_path: String) -> void:
	var tween := create_tween()
	tween.tween_interval(delay)
	tween.tween_callback(func():
		var tree := get_tree()
		if tree:
			tree.change_scene_to_file(scene_path)
	)

func _exit_tree() -> void:
	var nm := _get_network_manager()
	if nm:
		if nm.lobby_player_list_updated.is_connected(_on_lobby_player_list_updated):
			nm.lobby_player_list_updated.disconnect(_on_lobby_player_list_updated)
		if nm.match_ready.is_connected(_on_match_ready):
			nm.match_ready.disconnect(_on_match_ready)
		if nm.has_signal("spectator_joined_match") and nm.spectator_joined_match.is_connected(_on_spectator_joined_match):
			nm.spectator_joined_match.disconnect(_on_spectator_joined_match)
		if nm.spectator_match_init.is_connected(_on_spectator_match_init_received):
			nm.spectator_match_init.disconnect(_on_spectator_match_init_received)
