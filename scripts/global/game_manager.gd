extends Node

# Global game state & persistent player data (persists between scenes)
var player_nickname: String = "Anonymous Fairy"
var p1_name: String = "Player 1"
var p2_name: String = "Player 2"
var p2_is_ai: bool = false

# Selected characters
var p1_character: String = "Youmu Konpaku"
var p2_character: String = "Marisa Kirisame"

func set_nickname(new_name: String) -> void:
	var trimmed := new_name.strip_edges()
	if trimmed.is_empty():
		player_nickname = "Anonymous Fairy"
	else:
		player_nickname = trimmed
	p1_name = player_nickname

func set_player_names(p1: String, p2: String) -> void:
	p1_name = p1.strip_edges() if not p1.strip_edges().is_empty() else "Player 1"
	p2_name = p2.strip_edges() if not p2.strip_edges().is_empty() else "Player 2"

const PLAYABLE_CHARACTERS: Array[String] = [
	"Reimu Hakurei",
	"Marisa Kirisame",
	"Youmu Konpaku",
	"Cirno"
]

func set_p1_character(char_name: String) -> void:
	if char_name.to_lower().strip_edges() == "random":
		p1_character = PLAYABLE_CHARACTERS.pick_random()
	else:
		p1_character = char_name

func set_p2_character(char_name: String) -> void:
	if char_name.to_lower().strip_edges() == "random":
		p2_character = PLAYABLE_CHARACTERS.pick_random()
	else:
		p2_character = char_name

# Handicap (Pre-match starting health - default 5.0, min 0.5 guts)
var p1_handicap_hp: float = 5.0
var p2_handicap_hp: float = 5.0

func set_p1_handicap(hp: float) -> void:
	p1_handicap_hp = clampf(hp, 0.5, 5.0)

func set_p2_handicap(hp: float) -> void:
	p2_handicap_hp = clampf(hp, 0.5, 5.0)

func reset_handicaps() -> void:
	p1_handicap_hp = 5.0
	p2_handicap_hp = 5.0

# Match outcome data for Post-Match Results screen
var last_winner_player: int = 1
var last_loser_player: int = 2
var last_winner_character: String = "reimu"
var last_loser_character: String = "marisa"

func record_match_result(winner_player: int, loser_player: int, winner_char: String, loser_char: String) -> void:
	last_winner_player = winner_player
	last_loser_player = loser_player
	last_winner_character = winner_char
	last_loser_character = loser_char

# Round tracking (Best of 3)
const ROUNDS_TO_WIN: int = 2
var p1_round_wins: int = 0
var p2_round_wins: int = 0
var current_round: int = 1

func reset_match_rounds() -> void:
	p1_round_wins = 0
	p2_round_wins = 0
	current_round = 1

func add_round_win(winner_player: int) -> int:
	if winner_player == 1:
		p1_round_wins += 1
		return p1_round_wins
	else:
		p2_round_wins += 1
		return p2_round_wins

func is_match_over() -> bool:
	return p1_round_wins >= ROUNDS_TO_WIN or p2_round_wins >= ROUNDS_TO_WIN

func get_match_winner() -> int:
	if p1_round_wins >= ROUNDS_TO_WIN:
		return 1
	elif p2_round_wins >= ROUNDS_TO_WIN:
		return 2
	return 0

# Gameplay Settings & Match Control Style
# "udoalg": Hold Z to shoot (auto-fire), Hold X to charge, C to panic bomb
# "pofv": Tap/mash Z to shoot, Hold Z to charge, X to panic bomb
const SETTINGS_FILE_PATH: String = "user://settings.cfg"
var control_style: String = "udoalg"

func set_control_style(new_style: String) -> void:
	control_style = "pofv" if new_style == "pofv" else "udoalg"
	save_gameplay_settings()

func get_control_style() -> String:
	return control_style

func is_pofv_controls() -> bool:
	return control_style == "pofv"

func load_gameplay_settings() -> void:
	var config := ConfigFile.new()
	var err := config.load(SETTINGS_FILE_PATH)
	if err == OK:
		if config.has_section_key("gameplay", "control_style"):
			control_style = str(config.get_value("gameplay", "control_style", "udoalg"))

func save_gameplay_settings() -> void:
	var config := ConfigFile.new()
	var _err := config.load(SETTINGS_FILE_PATH)
	config.set_value("gameplay", "control_style", control_style)
	config.save(SETTINGS_FILE_PATH)

# Scene Transition Wipe
var _scene_transition: SceneTransition = null

func _ready() -> void:
	load_gameplay_settings()
	var transition_scene := preload("res://scenes/effects/scene_transition.tscn")
	_scene_transition = transition_scene.instantiate() as SceneTransition
	add_child(_scene_transition)

func change_scene_with_wipe(target_scene: String, duration_in: float = 0.30, hold_time: float = 0.10, duration_out: float = 0.30) -> void:
	if _scene_transition != null and is_instance_valid(_scene_transition):
		_scene_transition.play_transition(target_scene, duration_in, hold_time, duration_out)
	else:
		get_tree().change_scene_to_file(target_scene)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_F11 or event.physical_keycode == KEY_F11 or (event.alt_pressed and (event.keycode == KEY_ENTER or event.physical_keycode == KEY_ENTER)):
			toggle_fullscreen()
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()

func toggle_fullscreen() -> void:
	var mode := DisplayServer.window_get_mode()
	if mode == DisplayServer.WINDOW_MODE_FULLSCREEN or mode == DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	else:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)

