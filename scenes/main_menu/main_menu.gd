extends Control

@onready var nickname_input: LineEdit = %NicknameInput
@onready var server_input: LineEdit = %ServerInput if has_node("%ServerInput") else null
@onready var password_input: LineEdit = %PasswordInput if has_node("%PasswordInput") else null
@onready var online_match_btn: Button = %OnlineMatchBtn
@onready var practice_btn: Button = %PracticeBtn if has_node("%PracticeBtn") else null
@onready var settings_btn: Button = %SettingsBtn if has_node("%SettingsBtn") else (%KeybindsBtn if has_node("%KeybindsBtn") else null)
@onready var keybinds_btn: Button = settings_btn
@onready var settings_modal: PanelContainer = %SettingsModal if has_node("%SettingsModal") else (%KeybindsModal if has_node("%KeybindsModal") else null)
@onready var keybinds_modal: PanelContainer = settings_modal
@onready var close_settings_btn: Button = %CloseSettingsBtn if has_node("%CloseSettingsBtn") else (%CloseKeybindsBtn if has_node("%CloseKeybindsBtn") else null)
@onready var close_keybinds_btn: Button = close_settings_btn
@onready var replays_btn: Button = %ReplaysBtn if has_node("%ReplaysBtn") else null
@onready var replays_modal: PanelContainer = %ReplaysModal if has_node("%ReplaysModal") else null
@onready var replay_slots_container: VBoxContainer = %ReplaySlotsContainer if has_node("%ReplaySlotsContainer") else null
@onready var close_replays_btn: Button = %CloseReplaysBtn if has_node("%CloseReplaysBtn") else null

# Settings modal controls
@onready var tab_audio_btn: Button = %TabAudioBtn if has_node("%TabAudioBtn") else null
@onready var tab_controls_btn: Button = %TabControlsBtn if has_node("%TabControlsBtn") else null
@onready var audio_section: VBoxContainer = %AudioSection if has_node("%AudioSection") else null
@onready var controls_section: VBoxContainer = %ControlsSection if has_node("%ControlsSection") else null
@onready var master_slider: HSlider = %MasterSlider if has_node("%MasterSlider") else null
@onready var master_val_label: Label = %MasterValLabel if has_node("%MasterValLabel") else null
@onready var bgm_slider: HSlider = %BgmSlider if has_node("%BgmSlider") else null
@onready var bgm_val_label: Label = %BgmValLabel if has_node("%BgmValLabel") else null
@onready var sfx_slider: HSlider = %SfxSlider if has_node("%SfxSlider") else null
@onready var sfx_val_label: Label = %SfxValLabel if has_node("%SfxValLabel") else null

# Control Style toggle & dynamic labels
@onready var control_style_btn: Button = %ControlStyleBtn if has_node("%ControlStyleBtn") else null
@onready var key_shoot_label: Label = %KeyShoot if has_node("%KeyShoot") else null
@onready var desc_shoot_label: Label = %DescShoot if has_node("%DescShoot") else null
@onready var key_charge_label: Label = %KeyCharge if has_node("%KeyCharge") else null
@onready var desc_charge_label: Label = %DescCharge if has_node("%DescCharge") else null
@onready var key_bomb_label: Label = %KeyBomb if has_node("%KeyBomb") else null
@onready var desc_bomb_label: Label = %DescBomb if has_node("%DescBomb") else null
@onready var key_cancel_label: Label = %KeyCancel if has_node("%KeyCancel") else null

# Queue UI nodes
@onready var queue_header_label: Label = %QueueHeaderLabel
@onready var queue_list_container: VBoxContainer = %QueueListContainer
@onready var empty_notice_label: Label = %EmptyNoticeLabel
@onready var queue_status_label: Label = %QueueStatusLabel
@onready var match_found_label: Label = %MatchFoundLabel

# Modular 5-Layer Main Menu Architecture
@onready var background_tex: TextureRect = %BackgroundTex
@onready var center_ring: TextureRect = %CenterRing
@onready var left_char_slot: Control = %LeftCharSlot
@onready var left_char_a: TextureRect = %LeftCharA
@onready var left_char_b: TextureRect = %LeftCharB
@onready var right_char_slot: Control = %RightCharSlot
@onready var right_char_a: TextureRect = %RightCharA
@onready var right_char_b: TextureRect = %RightCharB
@onready var dark_scrim: ColorRect = %DarkScrim
@onready var margin_container: MarginContainer = %MarginContainer

var intro_active: bool = true
var intro_tween: Tween = null
var char_cycle_timer: float = 0.0
const CHAR_CYCLE_INTERVAL: float = 3.0

var available_character_textures: Array[Texture2D] = []
var current_left_tex: Texture2D = null
var current_right_tex: Texture2D = null
var left_active_is_a: bool = true
var right_active_is_a: bool = true
var cycle_left_turn: bool = true

const GLIDE_OFFSET: float = 180.0

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
	var gm := _get_game_manager()
	if gm:
		nickname_input.text = gm.player_nickname
	
	var nm := _get_network_manager()
	if server_input and nm:
		server_input.text = nm.signaling_url
		if not server_input.text_changed.is_connected(_on_server_url_changed):
			server_input.text_changed.connect(_on_server_url_changed)
	
	if password_input and nm and not nm.current_password.is_empty():
		password_input.text = nm.current_password
	
	if not nickname_input.text_changed.is_connected(_on_nickname_changed):
		nickname_input.text_changed.connect(_on_nickname_changed)
	if not online_match_btn.pressed.is_connected(_on_online_match_pressed):
		online_match_btn.pressed.connect(_on_online_match_pressed)
	if settings_btn and not settings_btn.pressed.is_connected(_on_settings_pressed):
		settings_btn.pressed.connect(_on_settings_pressed)
	if practice_btn and not practice_btn.pressed.is_connected(_on_practice_pressed):
		practice_btn.pressed.connect(_on_practice_pressed)
	if replays_btn and not replays_btn.pressed.is_connected(_on_replays_pressed):
		replays_btn.pressed.connect(_on_replays_pressed)
	if close_settings_btn and not close_settings_btn.pressed.is_connected(_on_close_settings_btn_pressed):
		close_settings_btn.pressed.connect(_on_close_settings_btn_pressed)
	if close_replays_btn and not close_replays_btn.pressed.is_connected(_on_close_replays_pressed):
		close_replays_btn.pressed.connect(_on_close_replays_pressed)

	# Connect Settings tab buttons
	if tab_audio_btn and not tab_audio_btn.pressed.is_connected(_on_tab_audio_pressed):
		tab_audio_btn.pressed.connect(_on_tab_audio_pressed)
	if tab_controls_btn and not tab_controls_btn.pressed.is_connected(_on_tab_controls_pressed):
		tab_controls_btn.pressed.connect(_on_tab_controls_pressed)
	if control_style_btn and not control_style_btn.pressed.is_connected(_on_control_style_btn_pressed):
		control_style_btn.pressed.connect(_on_control_style_btn_pressed)
	if control_style_btn and not control_style_btn.focus_entered.is_connected(_on_ui_focus_entered):
		control_style_btn.focus_entered.connect(_on_ui_focus_entered)

	# Connect Audio sliders
	if master_slider and not master_slider.value_changed.is_connected(_on_master_slider_changed):
		master_slider.value_changed.connect(_on_master_slider_changed)
	if bgm_slider and not bgm_slider.value_changed.is_connected(_on_bgm_slider_changed):
		bgm_slider.value_changed.connect(_on_bgm_slider_changed)
	if sfx_slider and not sfx_slider.value_changed.is_connected(_on_sfx_slider_changed):
		sfx_slider.value_changed.connect(_on_sfx_slider_changed)
	
	# Connect UI focus audio
	if not online_match_btn.focus_entered.is_connected(_on_ui_focus_entered):
		online_match_btn.focus_entered.connect(_on_ui_focus_entered)
	if practice_btn and not practice_btn.focus_entered.is_connected(_on_ui_focus_entered):
		practice_btn.focus_entered.connect(_on_ui_focus_entered)
	if replays_btn and not replays_btn.focus_entered.is_connected(_on_ui_focus_entered):
		replays_btn.focus_entered.connect(_on_ui_focus_entered)
	if settings_btn and not settings_btn.focus_entered.is_connected(_on_ui_focus_entered):
		settings_btn.focus_entered.connect(_on_ui_focus_entered)
	if not nickname_input.focus_entered.is_connected(_on_ui_focus_entered):
		nickname_input.focus_entered.connect(_on_ui_focus_entered)
	if close_settings_btn and not close_settings_btn.focus_entered.is_connected(_on_ui_focus_entered):
		close_settings_btn.focus_entered.connect(_on_ui_focus_entered)
	if close_replays_btn and not close_replays_btn.focus_entered.is_connected(_on_ui_focus_entered):
		close_replays_btn.focus_entered.connect(_on_ui_focus_entered)
	if server_input and not server_input.focus_entered.is_connected(_on_ui_focus_entered):
		server_input.focus_entered.connect(_on_ui_focus_entered)
	if password_input and not password_input.focus_entered.is_connected(_on_ui_focus_entered):
		password_input.focus_entered.connect(_on_ui_focus_entered)
	
	# Connect NetworkManager events
	if nm:
		if not nm.lobby_player_list_updated.is_connected(_on_lobby_player_list_updated):
			nm.lobby_player_list_updated.connect(_on_lobby_player_list_updated)
		if not nm.match_ready.is_connected(_on_match_ready):
			nm.match_ready.connect(_on_match_ready)
		if nm.has_signal("spectator_joined_match") and not nm.spectator_joined_match.is_connected(_on_spectator_joined_match):
			nm.spectator_joined_match.connect(_on_spectator_joined_match)
		# Passively connect to the lobby as soon as menu loads
		nm.connect_to_lobby(nickname_input.text)
		if nm.has_method("set_in_bot_match"):
			nm.set_in_bot_match(false)
	
	if settings_modal:
		settings_modal.visible = false
	if replays_modal:
		replays_modal.visible = false
	match_found_label.visible = false
	_refresh_queue_display([])
	
	_load_character_textures()
	_start_bootup_sequence()
	
	# Configure vertical arrow-key focus loop
	var buttons_chain: Array[Control] = []
	if nickname_input: buttons_chain.append(nickname_input)
	if server_input: buttons_chain.append(server_input)
	if password_input: buttons_chain.append(password_input)
	if online_match_btn: buttons_chain.append(online_match_btn)
	if practice_btn: buttons_chain.append(practice_btn)
	if replays_btn: buttons_chain.append(replays_btn)
	if settings_btn: buttons_chain.append(settings_btn)
	
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

func _process(delta: float) -> void:

	if not intro_active and not available_character_textures.is_empty():
		char_cycle_timer += delta
		if char_cycle_timer >= CHAR_CYCLE_INTERVAL:
			char_cycle_timer = 0.0
			_cycle_next_character()

	if is_queued:
		queue_elapsed += delta
		var minutes: int = floori(queue_elapsed / 60.0)
		var seconds: int = int(queue_elapsed) % 60
		
		# Animated ellipsis dots
		var dots_count: int = (int(queue_elapsed * 2.0) % 4)
		var dots: String = ".".repeat(dots_count)
		var pw_str: String = password_input.text.strip_edges() if password_input else ""
		if pw_str.is_empty():
			queue_status_label.text = "Status: Searching for opponent%s  [%02d:%02d]" % [dots, minutes, seconds]
		else:
			queue_status_label.text = "Status: Searching [Room: %s]%s  [%02d:%02d]" % [pw_str, dots, minutes, seconds]

func _unhandled_input(event: InputEvent) -> void:
	if intro_active:
		if (event is InputEventKey and event.pressed and not event.echo) or (event is InputEventMouseButton and event.pressed):
			skip_intro()
			get_viewport().set_input_as_handled()
			return

	if event.is_action_pressed("cancel"):
		if replays_modal and replays_modal.visible:
			replays_modal.visible = false
			AudioService.play_cancel()
			get_viewport().set_input_as_handled()
		elif settings_modal and settings_modal.visible:
			settings_modal.visible = false
			AudioService.play_cancel()
			if settings_btn:
				settings_btn.grab_focus()
			get_viewport().set_input_as_handled()
		elif is_queued:
			_leave_queue()
			AudioService.play_cancel()
			get_viewport().set_input_as_handled()

func _load_character_textures() -> void:
	available_character_textures.clear()
	var char_dir := "res://assets/ui/menu_characters"
	var files := DirAccess.get_files_at(char_dir)
	for file_name in files:
		# Accept both .png and .png.import exported formats
		var clean_name := file_name.trim_suffix(".import")
		if clean_name.ends_with(".png"):
			var full_path := char_dir.path_join(clean_name)
			if ResourceLoader.exists(full_path):
				var tex := load(full_path) as Texture2D
				if tex and not available_character_textures.has(tex):
					available_character_textures.append(tex)
	
	# Fallback if folder empty
	if available_character_textures.is_empty():
		if left_char_a and left_char_a.texture:
			available_character_textures.append(left_char_a.texture)
		if right_char_a and right_char_a.texture:
			available_character_textures.append(right_char_a.texture)

func _get_texture_natural_facing(tex: Texture2D) -> int:
	if not tex:
		return 1
	var path_lower := tex.resource_path.to_lower()
	if "marisa" in path_lower:
		return -1
	# Reimu and default characters face right (+1)
	return 1

func _apply_character_to_rect(rect: TextureRect, tex: Texture2D, is_left: bool) -> void:
	if not rect:
		return
	rect.texture = tex
	var natural_facing := _get_texture_natural_facing(tex)
	# Left slot must face toward the central ring (RIGHT = +1)
	# Right slot must face toward the central ring (LEFT = -1)
	var target_facing := 1 if is_left else -1
	rect.flip_h = (natural_facing != target_facing)
	
	var path_lower := tex.resource_path.to_lower() if tex else ""
	var rect_w := rect.size.x if rect.size.x > 0.0 else 660.0
	var rect_h := rect.size.y if rect.size.y > 0.0 else 960.0
	if "marisa" in path_lower:
		# Scale Marisa 1.2x bigger, pivoting slightly below center so feet and hat balance naturally
		rect.pivot_offset = Vector2(rect_w * 0.5, rect_h * 0.55)
		rect.scale = Vector2(1.2, 1.2)
	else:
		rect.pivot_offset = Vector2(rect_w * 0.5, rect_h * 0.5)
		rect.scale = Vector2.ONE

func _start_bootup_sequence() -> void:
	intro_active = true
	
	# Initial opacities and positions
	if background_tex:
		background_tex.modulate.a = 0.0
	if center_ring:
		center_ring.modulate.a = 0.0
		center_ring.scale = Vector2.ONE
	if left_char_slot:
		left_char_slot.modulate.a = 1.0
	if right_char_slot:
		right_char_slot.modulate.a = 1.0
	if dark_scrim:
		dark_scrim.modulate.a = 0.0
	if margin_container:
		margin_container.modulate.a = 0.0
		
	# Select starting textures (Reimu on left, Marisa on right by default)
	var reimu_tex: Texture2D = null
	var marisa_tex: Texture2D = null
	for tex in available_character_textures:
		var p_lower := tex.resource_path.to_lower()
		if "reimu" in p_lower:
			reimu_tex = tex
		elif "marisa" in p_lower:
			marisa_tex = tex
			
	if reimu_tex and marisa_tex:
		current_left_tex = reimu_tex
		current_right_tex = marisa_tex
	elif available_character_textures.size() >= 2:
		current_left_tex = available_character_textures[0]
		current_right_tex = available_character_textures[available_character_textures.size() - 1]
	elif available_character_textures.size() == 1:
		current_left_tex = available_character_textures[0]
		current_right_tex = available_character_textures[0]
	
	if left_char_a and current_left_tex:
		_apply_character_to_rect(left_char_a, current_left_tex, true)
		left_char_a.modulate.a = 0.0
		left_char_a.position.x = -GLIDE_OFFSET
	if left_char_b:
		left_char_b.modulate.a = 0.0
		left_char_b.position.x = 0.0
		
	if right_char_a and current_right_tex:
		_apply_character_to_rect(right_char_a, current_right_tex, false)
		right_char_a.modulate.a = 0.0
		right_char_a.position.x = GLIDE_OFFSET
	if right_char_b:
		right_char_b.modulate.a = 0.0
		right_char_b.position.x = 0.0
		
	# Build intro tween
	intro_tween = create_tween()
	intro_tween.set_parallel(true)
	
	# 1. Background fades in first (0.0 -> 0.4s)
	if background_tex:
		intro_tween.tween_property(background_tex, "modulate:a", 1.0, 0.4).set_ease(Tween.EASE_OUT)
		
	# 2. Sacred Ring fades in second - completely static, no pulsation (0.35 -> 0.75s)
	if center_ring:
		intro_tween.tween_property(center_ring, "modulate:a", 1.0, 0.4).set_delay(0.35).set_ease(Tween.EASE_OUT)
		
	# 3. Characters glide inward from edges while fading in (0.65 -> 1.3s)
	# Left duelist glides in from left to right (-GLIDE_OFFSET -> 0.0)
	if left_char_a:
		intro_tween.tween_property(left_char_a, "position:x", 0.0, 0.65).set_delay(0.65).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
		intro_tween.tween_property(left_char_a, "modulate:a", 1.0, 0.65).set_delay(0.65).set_ease(Tween.EASE_OUT)
	# Right duelist glides in from right to left (+GLIDE_OFFSET -> 0.0)
	if right_char_a:
		intro_tween.tween_property(right_char_a, "position:x", 0.0, 0.65).set_delay(0.65).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
		intro_tween.tween_property(right_char_a, "modulate:a", 1.0, 0.65).set_delay(0.65).set_ease(Tween.EASE_OUT)
		
	# 4. Scrim and Menu UI fade in last (1.05 -> 1.45s)
	if dark_scrim:
		intro_tween.tween_property(dark_scrim, "modulate:a", 1.0, 0.4).set_delay(1.05).set_ease(Tween.EASE_OUT)
	if margin_container:
		intro_tween.tween_property(margin_container, "modulate:a", 1.0, 0.4).set_delay(1.05).set_ease(Tween.EASE_OUT)
		
	intro_tween.chain().tween_callback(func():
		intro_active = false
	)

func skip_intro() -> void:
	if not intro_active:
		return
	if intro_tween and intro_tween.is_valid():
		intro_tween.kill()
		
	intro_active = false
	if background_tex:
		background_tex.modulate.a = 1.0
	if center_ring:
		center_ring.modulate.a = 1.0
		center_ring.scale = Vector2.ONE
	if left_char_a:
		left_char_a.modulate.a = 1.0
		left_char_a.position.x = 0.0
	if left_char_b:
		left_char_b.modulate.a = 0.0
		left_char_b.position.x = 0.0
	if right_char_a:
		right_char_a.modulate.a = 1.0
		right_char_a.position.x = 0.0
	if right_char_b:
		right_char_b.modulate.a = 0.0
		right_char_b.position.x = 0.0
	if dark_scrim:
		dark_scrim.modulate.a = 1.0
	if margin_container:
		margin_container.modulate.a = 1.0

func _cycle_next_character() -> void:
	if available_character_textures.is_empty():
		return
		
	if cycle_left_turn:
		_cycle_slot(true)
	else:
		_cycle_slot(false)
	cycle_left_turn = not cycle_left_turn

func _cycle_slot(is_left: bool) -> void:
	var char_a: TextureRect = left_char_a if is_left else right_char_a
	var char_b: TextureRect = left_char_b if is_left else right_char_b
	var is_a_active: bool = left_active_is_a if is_left else right_active_is_a
	var current_tex: Texture2D = current_left_tex if is_left else current_right_tex
	
	if not char_a or not char_b:
		return
		
	# Pick a new random texture different from current
	var pool: Array[Texture2D] = []
	for tex in available_character_textures:
		if tex != current_tex:
			pool.append(tex)
	if pool.is_empty():
		pool = available_character_textures
		
	var new_tex: Texture2D = pool[randi() % pool.size()]
	
	var outgoing_rect: TextureRect = char_a if is_a_active else char_b
	var incoming_rect: TextureRect = char_b if is_a_active else char_a
	
	# Determine motion direction:
	# Left slot: edge is to the left (-GLIDE_OFFSET)
	# Right slot: edge is to the right (+GLIDE_OFFSET)
	var edge_x: float = -GLIDE_OFFSET if is_left else GLIDE_OFFSET
	
	# Prepare incoming rect at the edge with 0 opacity and correct facing
	_apply_character_to_rect(incoming_rect, new_tex, is_left)
	incoming_rect.position.x = edge_x
	incoming_rect.modulate.a = 0.0
	
	var cross_tween := create_tween().set_parallel(true)
	
	# Outgoing: moves from resting position (0.0) towards screen edge (edge_x) while quickly fading out
	cross_tween.tween_property(outgoing_rect, "position:x", edge_x, 0.45).set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	cross_tween.tween_property(outgoing_rect, "modulate:a", 0.0, 0.45).set_ease(Tween.EASE_IN)
	
	# Incoming: glides from screen edge (edge_x) to resting position (0.0) while fading in
	cross_tween.tween_property(incoming_rect, "position:x", 0.0, 0.65).set_delay(0.1).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	cross_tween.tween_property(incoming_rect, "modulate:a", 1.0, 0.65).set_delay(0.1).set_ease(Tween.EASE_OUT)
	
	# Reset outgoing rect position when finished so it's ready for future transitions
	cross_tween.chain().tween_callback(func():
		outgoing_rect.position.x = 0.0
	)
	
	if is_left:
		left_active_is_a = not left_active_is_a
		current_left_tex = new_tex
	else:
		right_active_is_a = not right_active_is_a
		current_right_tex = new_tex


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
	
	get_tree().change_scene_to_file("res://scenes/character_select/character_select.tscn")

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
	nickname_input.editable = false
	if server_input:
		server_input.editable = false
	if password_input:
		password_input.editable = false
	
	online_match_btn.text = "Cancel Queue"
	online_match_btn.add_theme_color_override("font_color", Color(1.0, 0.4, 0.4))
	online_match_btn.add_theme_color_override("font_hover_color", Color(1.0, 0.7, 0.7))
	if practice_btn:
		practice_btn.disabled = true
	if replays_btn:
		replays_btn.disabled = true
	
	var pw: String = password_input.text.strip_edges() if password_input else ""
	# Tell NetworkManager we are now searching
	if nm:
		nm.set_searching(true, pw)

func _leave_queue() -> void:
	is_queued = false
	
	nickname_input.editable = true
	if server_input:
		server_input.editable = true
	if password_input:
		password_input.editable = true
	online_match_btn.text = "Online Match"
	online_match_btn.remove_theme_color_override("font_color")
	online_match_btn.remove_theme_color_override("font_hover_color")
	if practice_btn:
		practice_btn.disabled = false
	if replays_btn:
		replays_btn.disabled = false
	
	queue_status_label.text = "Status: Idle"
	
	var nm := _get_network_manager()
	if nm:
		nm.set_searching(false, "")

func _on_lobby_player_list_updated(players: Array) -> void:
	_refresh_queue_display(players)

func _refresh_queue_display(players: Array) -> void:
	# Count searching and in-match players
	var searching_count: int = 0
	var in_match_count: int = 0
	for p in players:
		var raw_name: String = str(p.get("name", "Anonymous Fairy"))
		var is_bot: bool = p.get("in_bot_match", false) or raw_name.ends_with("::bot")
		if p.get("is_searching", false):
			searching_count += 1
		elif p.get("in_match", false) or is_bot:
			in_match_count += 1
	
	if in_match_count > 0:
		queue_header_label.text = "LOBBY PLAYERS (%d searching, %d in match)" % [searching_count, in_match_count]
	else:
		queue_header_label.text = "PLAYERS IN QUEUE (%d)" % searching_count
	
	# Clear previous list items (keeping empty notice)
	for child in queue_list_container.get_children():
		if child != empty_notice_label:
			queue_list_container.remove_child(child)
			child.queue_free()
	
	# Show all connected players (searching ones highlighted, idle ones dimmed)
	var has_any_players: bool = not players.is_empty()
	
	if not has_any_players:
		empty_notice_label.visible = true
	else:
		empty_notice_label.visible = false
		var local_pw: String = ""
		if password_input and not password_input.text.strip_edges().is_empty():
			local_pw = password_input.text.strip_edges()
		elif _get_network_manager() and not _get_network_manager().current_password.is_empty():
			local_pw = _get_network_manager().current_password
		for p in players:
			var item_label := Label.new()
			item_label.add_theme_font_override("font", load("res://Cirno.ttf"))
			item_label.add_theme_font_size_override("font_size", 26)
			item_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 1.0))
			item_label.add_theme_constant_override("outline_size", 4)
			item_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.95))
			item_label.add_theme_constant_override("shadow_offset_x", 2)
			item_label.add_theme_constant_override("shadow_offset_y", 2)
			
			var raw_name: String = str(p.get("name", "Anonymous Fairy"))
			var is_tagged_bot: bool = raw_name.ends_with("::bot")
			var display_name: String = raw_name.trim_suffix("::bot").strip_edges()
			var p_is_local: bool = p.get("is_local", false)
			var p_is_searching: bool = p.get("is_searching", false)
			var p_in_match: bool = p.get("in_match", false)
			var p_in_bot_match: bool = p.get("in_bot_match", false) or is_tagged_bot
			var nm := _get_network_manager()
			if p_is_local and nm and nm.get("is_in_bot_match") == true:
				p_in_bot_match = true
			var has_password: bool = p.get("has_password", false)
			var matches_password: bool = p.get("matches_password", false)
			var room_has_password: bool = p.get("room_has_password", false)
			var room_matches_password: bool = p.get("room_matches_password", false)
			
			var room_id: String = str(p.get("room_id", ""))
			var can_spectate: bool = p.get("can_spectate", false) and not room_id.is_empty() and not p_is_local
			var spectator_count: int = int(p.get("spectator_count", 0))

			if p_in_match:
				if p_is_local:
					if not local_pw.is_empty():
						item_label.text = "• %s (You) - In Match [Room: %s]" % [display_name, local_pw]
					else:
						item_label.text = "• %s (You) - In Match (Public)" % display_name
					item_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.4))
				else:
					if room_matches_password and not local_pw.is_empty():
						item_label.text = "• %s - In Match [Room: %s]" % [display_name, local_pw]
						item_label.add_theme_color_override("font_color", Color(0.5, 0.85, 1.0))
					elif room_has_password:
						item_label.text = "• %s - In Match (Private)" % display_name
						item_label.add_theme_color_override("font_color", Color(0.65, 0.65, 0.75))
					else:
						item_label.text = "• %s - In Match (Public)" % display_name
						item_label.add_theme_color_override("font_color", Color(0.8, 0.75, 0.9))
			elif p_in_bot_match:
				if p_is_local:
					item_label.text = "• %s (You) - In Bot Match" % display_name
					item_label.add_theme_color_override("font_color", Color(0.45, 0.9, 0.7))
				else:
					item_label.text = "• %s - In Bot Match" % display_name
					item_label.add_theme_color_override("font_color", Color(0.6, 0.82, 0.75))
			elif p_is_local and p_is_searching:
				if not local_pw.is_empty():
					item_label.text = "• %s (You) - Room: %s" % [display_name, local_pw]
				else:
					item_label.text = "• %s (You) - Searching (Public)" % display_name
				item_label.add_theme_color_override("font_color", Color(0.4, 0.8, 1.0))
			elif p_is_local:
				item_label.text = "• %s (You) - Browsing" % display_name
				item_label.add_theme_color_override("font_color", Color(0.55, 0.65, 0.7))
			elif p_is_searching:
				if matches_password:
					if not local_pw.is_empty():
						item_label.text = "• %s - Ready to Match! [Room: %s]" % [display_name, local_pw]
					else:
						item_label.text = "• %s - Ready to Match! (Public)" % display_name
					item_label.add_theme_color_override("font_color", Color(0.3, 1.0, 0.5))
				elif has_password:
					item_label.text = "• %s - In Private Room" % display_name
					item_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.8))
				else:
					item_label.text = "• %s - Searching (Public)" % display_name
					item_label.add_theme_color_override("font_color", Color(0.9, 0.9, 0.9))
			else:
				item_label.text = "• %s - Browsing" % display_name
				item_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.65))
			
			if can_spectate:
				var row := HBoxContainer.new()
				row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
				row.add_theme_constant_override("separation", 8)
				
				item_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
				row.add_child(item_label)
				
				var watch_btn := Button.new()
				watch_btn.text = "👁 Watch" if spectator_count == 0 else "👁 Watch (%d)" % spectator_count
				watch_btn.custom_minimum_size = Vector2(110, 30)
				watch_btn.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
				watch_btn.add_theme_font_override("font", load("res://Cirno.ttf"))
				watch_btn.add_theme_font_size_override("font_size", 19)
				watch_btn.add_theme_color_override("font_outline_color", Color(0, 0, 0, 1.0))
				watch_btn.add_theme_constant_override("outline_size", 3)
				
				var btn_style := StyleBoxFlat.new()
				btn_style.bg_color = Color(0.12, 0.28, 0.4, 0.85)
				btn_style.border_color = Color(0.35, 0.8, 1.0, 0.9)
				btn_style.border_width_left = 1
				btn_style.border_width_right = 1
				btn_style.border_width_top = 1
				btn_style.border_width_bottom = 1
				btn_style.corner_radius_top_left = 4
				btn_style.corner_radius_top_right = 4
				btn_style.corner_radius_bottom_right = 4
				btn_style.corner_radius_bottom_left = 4
				btn_style.content_margin_left = 6.0
				btn_style.content_margin_right = 6.0
				btn_style.content_margin_top = 2.0
				btn_style.content_margin_bottom = 2.0
				watch_btn.add_theme_stylebox_override("normal", btn_style)
				
				var captured_room_id := room_id
				var captured_pw := local_pw
				watch_btn.pressed.connect(func():
					_on_watch_button_pressed(captured_room_id, captured_pw)
				)
				row.add_child(watch_btn)
				queue_list_container.add_child(row)
			else:
				queue_list_container.add_child(item_label)

func _on_match_ready(p1_name: String, p2_name: String) -> void:
	is_queued = false
	
	queue_status_label.text = "Status: Match Confirmed!"
	match_found_label.text = ">> Match Found: %s vs %s! <<" % [p1_name, p2_name]
	match_found_label.visible = true
	
	# Reset button state
	online_match_btn.text = "Online Match"
	online_match_btn.remove_theme_color_override("font_color")
	online_match_btn.remove_theme_color_override("font_hover_color")
	nickname_input.editable = true
	if server_input:
		server_input.editable = true
	if password_input:
		password_input.editable = true
	if practice_btn:
		practice_btn.disabled = false
	if replays_btn:
		replays_btn.disabled = false
	
	print("[MainMenu] Match successfully formed: %s vs %s" % [p1_name, p2_name])
	
	# Transition to Character Select after a brief confirmation delay
	var tween := create_tween()
	tween.tween_interval(1.5)
	tween.tween_callback(func():
		get_tree().change_scene_to_file("res://scenes/character_select/character_select.tscn")
	)

func _sync_settings_sliders() -> void:
	if master_slider:
		var m_val: float = round(AudioService.get_master_volume() * 100.0)
		master_slider.set_value_no_signal(m_val)
		if master_val_label:
			master_val_label.text = "%d%%" % int(m_val)
	if bgm_slider:
		var b_val: float = round(AudioService.get_bgm_volume() * 100.0)
		bgm_slider.set_value_no_signal(b_val)
		if bgm_val_label:
			bgm_val_label.text = "%d%%" % int(b_val)
	if sfx_slider:
		var s_val: float = round(AudioService.get_sfx_volume() * 100.0)
		sfx_slider.set_value_no_signal(s_val)
		if sfx_val_label:
			sfx_val_label.text = "%d%%" % int(s_val)

func _on_settings_pressed() -> void:
	AudioService.play_confirm()
	_sync_settings_sliders()
	_sync_control_style_ui()
	_show_settings_tab(true)
	if settings_modal:
		settings_modal.visible = true
	if master_slider:
		master_slider.grab_focus()

func _on_keybinds_pressed() -> void:
	_on_settings_pressed()

func _on_close_settings_btn_pressed() -> void:
	AudioService.play_cancel()
	if settings_modal:
		settings_modal.visible = false
	if settings_btn:
		settings_btn.grab_focus()

func _on_close_keybinds_btn_pressed() -> void:
	_on_close_settings_btn_pressed()

func _on_close_keybinds_pressed() -> void:
	_on_close_settings_btn_pressed()

func _show_settings_tab(is_audio: bool) -> void:
	if audio_section:
		audio_section.visible = is_audio
	if controls_section:
		controls_section.visible = not is_audio
	if tab_audio_btn:
		tab_audio_btn.add_theme_color_override("font_color", Color(1.0, 0.9, 0.4, 1.0) if is_audio else Color(0.7, 0.75, 0.85, 1.0))
	if tab_controls_btn:
		tab_controls_btn.add_theme_color_override("font_color", Color(1.0, 0.9, 0.4, 1.0) if not is_audio else Color(0.7, 0.75, 0.85, 1.0))

func _on_tab_audio_pressed() -> void:
	AudioService.play_select()
	_show_settings_tab(true)
	if master_slider:
		master_slider.grab_focus()

func _on_tab_controls_pressed() -> void:
	AudioService.play_select()
	_sync_control_style_ui()
	_show_settings_tab(false)
	if control_style_btn:
		control_style_btn.grab_focus()
	elif close_settings_btn:
		close_settings_btn.grab_focus()

func _on_control_style_btn_pressed() -> void:
	AudioService.play_confirm()
	var gm := _get_game_manager()
	var current: String = gm.get_control_style() if (gm and gm.has_method("get_control_style")) else "udoalg"
	var new_style := "pofv" if current == "udoalg" else "udoalg"
	if gm and gm.has_method("set_control_style"):
		gm.set_control_style(new_style)
	_sync_control_style_ui()

func _sync_control_style_ui() -> void:
	var gm := _get_game_manager()
	var is_pofv: bool = gm.is_pofv_controls() if (gm and gm.has_method("is_pofv_controls")) else false
	if control_style_btn:
		control_style_btn.text = "PoFV-Style (Classic Touhou 09)" if is_pofv else "UDoALG-Style (Hold Z Shoot)"
	if key_shoot_label:
		key_shoot_label.text = "Tap Z" if is_pofv else "Z"
	if desc_shoot_label:
		desc_shoot_label.text = "Shoot (Mash for continuous stream)" if is_pofv else "Shoot / Select Item (Hold to fire)"
	if key_charge_label:
		key_charge_label.text = "Hold Z" if is_pofv else "Hold X"
	if desc_charge_label:
		desc_charge_label.text = "Charge Spell Gauge (Lv 1-4)"
	if key_bomb_label:
		key_bomb_label.text = "X" if is_pofv else "C"
	if desc_bomb_label:
		desc_bomb_label.text = "Panic Bomb (Highest Tier Spell)"
	if key_cancel_label:
		key_cancel_label.text = "Esc" if is_pofv else "Esc  /  X"

func _on_master_slider_changed(val: float) -> void:
	var linear := val / 100.0
	AudioService.set_master_volume(linear)
	if master_val_label:
		master_val_label.text = "%d%%" % int(val)
	AudioService.save_audio_settings()

func _on_bgm_slider_changed(val: float) -> void:
	var linear := val / 100.0
	AudioService.set_bgm_volume(linear)
	if bgm_val_label:
		bgm_val_label.text = "%d%%" % int(val)
	AudioService.save_audio_settings()

var _last_sfx_preview_time: int = 0
func _on_sfx_slider_changed(val: float) -> void:
	var linear := val / 100.0
	AudioService.set_sfx_volume(linear)
	if sfx_val_label:
		sfx_val_label.text = "%d%%" % int(val)
	AudioService.save_audio_settings()
	
	# Play subtle test chime debounced to max ~80ms
	var now: int = Time.get_ticks_msec()
	if now - _last_sfx_preview_time > 80:
		_last_sfx_preview_time = now
		AudioService.play_select()

func _on_replays_pressed() -> void:
	AudioService.play_confirm()
	if replays_modal:
		_refresh_replays_list()
		replays_modal.visible = true
		if close_replays_btn:
			close_replays_btn.grab_focus()

func _on_close_replays_pressed() -> void:
	AudioService.play_cancel()
	if replays_modal:
		replays_modal.visible = false
	if replays_btn:
		replays_btn.grab_focus()

func _apply_replay_button_style(btn: Button, bg: Color, border: Color) -> void:
	var style := StyleBoxFlat.new()
	style.bg_color = bg
	style.border_color = border
	style.border_width_left = 1
	style.border_width_right = 1
	style.border_width_top = 1
	style.border_width_bottom = 1
	style.corner_radius_top_left = 4
	style.corner_radius_top_right = 4
	style.corner_radius_bottom_right = 4
	style.corner_radius_bottom_left = 4
	style.content_margin_left = 6.0
	style.content_margin_right = 6.0
	style.content_margin_top = 2.0
	style.content_margin_bottom = 2.0
	btn.add_theme_stylebox_override("normal", style)
	var hover := style.duplicate() as StyleBoxFlat
	hover.bg_color = bg.lightened(0.2)
	btn.add_theme_stylebox_override("hover", hover)
	btn.add_theme_stylebox_override("focus", hover)

func _refresh_replays_list() -> void:
	if not replay_slots_container:
		return
	for child in replay_slots_container.get_children():
		replay_slots_container.remove_child(child)
		child.queue_free()
		
	var rm: Node = get_node_or_null("/root/ReplayManager")
	var summaries: Array = rm.get_all_slot_summaries() if rm and rm.has_method("get_all_slot_summaries") else []
	
	for s in summaries:
		var slot_idx: int = s.get("slot_index", 1)
		var is_empty: bool = s.get("is_empty", true)
		
		var row := HBoxContainer.new()
		row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_theme_constant_override("separation", 12)
		
		var label := Label.new()
		label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		label.add_theme_font_override("font", load("res://Cirno.ttf"))
		label.add_theme_font_size_override("font_size", 23)
		
		if is_empty:
			label.text = "Slot %d: [Empty]" % slot_idx
			label.add_theme_color_override("font_color", Color(0.5, 0.5, 0.55, 0.8))
			row.add_child(label)
		else:
			var p1: String = s.get("p1_name", "P1")
			var p2: String = s.get("p2_name", "P2")
			var c1: String = s.get("p1_char", "").capitalize()
			var c2: String = s.get("p2_char", "").capitalize()
			var dur: float = float(s.get("duration", 0.0))
			var winner_num: int = s.get("winner_num", 0)
			var dur_min: int = floori(dur / 60.0)
			var dur_sec: int = int(dur) % 60
			
			label.text = "Slot %d: %s (%s) vs %s (%s) [%02d:%02d]" % [slot_idx, p1, c1, p2, c2, dur_min, dur_sec]
			if winner_num == 1:
				label.text += " - %s Win" % p1
			elif winner_num == 2:
				label.text += " - %s Win" % p2
			label.add_theme_color_override("font_color", Color(0.9, 0.95, 1.0, 1.0))
			row.add_child(label)
			
			var watch_btn := Button.new()
			watch_btn.text = "▶ Watch"
			watch_btn.custom_minimum_size = Vector2(105, 34)
			watch_btn.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
			watch_btn.add_theme_font_override("font", load("res://Cirno.ttf"))
			watch_btn.add_theme_font_size_override("font_size", 20)
			_apply_replay_button_style(watch_btn, Color(0.15, 0.35, 0.5, 0.85), Color(0.4, 0.8, 1.0, 0.9))
			var captured_idx: int = slot_idx
			watch_btn.pressed.connect(func(): _on_watch_replay_pressed(captured_idx))
			watch_btn.focus_entered.connect(_on_ui_focus_entered)
			row.add_child(watch_btn)
			
			var del_btn := Button.new()
			del_btn.text = "🗑"
			del_btn.custom_minimum_size = Vector2(45, 34)
			del_btn.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
			del_btn.add_theme_font_override("font", load("res://Cirno.ttf"))
			del_btn.add_theme_font_size_override("font_size", 20)
			_apply_replay_button_style(del_btn, Color(0.4, 0.15, 0.15, 0.85), Color(1.0, 0.4, 0.4, 0.9))
			del_btn.pressed.connect(func(): _on_delete_replay_pressed(captured_idx))
			del_btn.focus_entered.connect(_on_ui_focus_entered)
			row.add_child(del_btn)
			
		replay_slots_container.add_child(row)

func _on_watch_replay_pressed(slot_idx: int) -> void:
	AudioService.play_confirm()
	var rm: Node = get_node_or_null("/root/ReplayManager")
	if rm and rm.has_method("start_playback"):
		if is_queued:
			_leave_queue()
		rm.start_playback(slot_idx, get_tree())

func _on_delete_replay_pressed(slot_idx: int) -> void:
	AudioService.play_cancel()
	var rm: Node = get_node_or_null("/root/ReplayManager")
	if rm and rm.has_method("delete_slot"):
		rm.delete_slot(slot_idx)
		_refresh_replays_list()

func _on_ui_focus_entered() -> void:
	AudioService.play_select()

func _on_watch_button_pressed(room_id: String, password: String) -> void:
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
	
	# If matchState already has character data, the arena is already running — go straight in
	if not nm.spectator_p1_char.is_empty() and not nm.spectator_p2_char.is_empty():
		queue_status_label.text = "Status: Joining match..."
		var tween := create_tween()
		tween.tween_interval(1.0)
		tween.tween_callback(func():
			var tree := get_tree()
			if tree:
				tree.change_scene_to_file("res://scenes/arena/arena.tscn")
		)
	else:
		# Players are still in character select — wait for match_init
		queue_status_label.text = "Status: Waiting for match to begin..."
		nm.spectator_match_init.connect(_on_spectator_match_init_received, CONNECT_ONE_SHOT)

func _on_spectator_match_init_received(
	_p1_char: String, _p2_char: String, _stage_id: String,
	_p1_wins: int, _p2_wins: int, _current_round: int, _is_round_active: bool
) -> void:
	queue_status_label.text = "Status: Match starting!"
	var tween := create_tween()
	tween.tween_interval(0.5)
	tween.tween_callback(func():
		var tree := get_tree()
		if tree:
			tree.change_scene_to_file("res://scenes/arena/arena.tscn")
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
