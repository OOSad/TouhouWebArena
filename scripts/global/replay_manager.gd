class_name ReplayManagerScript
extends Node

## ReplayManager: Central singleton for recording, persisting, and playing back matches.
## Replays are saved as JSON files in user://replays/slot_{1-10}.json.
## Supports automatic IndexedDB synchronization on HTML5 / Web exports.

const MAX_SLOTS: int = 10
const REPLAY_DIR: String = "user://replays/"
const REPLAY_VERSION: int = 1

var is_recording: bool = false
var current_recording: Dictionary = {}
var last_recorded_replay: Dictionary = {}

var is_replaying: bool = false
var active_replay: Dictionary = {}
var playback_speed: float = 1.0
var playback_paused: bool = false

signal playback_speed_changed(speed: float)
signal playback_pause_toggled(is_paused: bool)

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_ensure_replay_directory()

func _ensure_replay_directory() -> void:
	if not DirAccess.dir_exists_absolute(REPLAY_DIR):
		DirAccess.make_dir_recursive_absolute(REPLAY_DIR)

func start_recording(
	p1_name: String,
	p2_name: String,
	p1_char: String,
	p2_char: String,
	stage_id: String,
	bgm_path: String,
	match_seed: int
) -> void:
	var now_dict := Time.get_datetime_dict_from_system()
	var date_str := "%04d-%02d-%02d %02d:%02d" % [
		now_dict.get("year", 2026),
		now_dict.get("month", 1),
		now_dict.get("day", 1),
		now_dict.get("hour", 0),
		now_dict.get("minute", 0)
	]
	
	current_recording = {
		"version": REPLAY_VERSION,
		"timestamp": Time.get_unix_time_from_system(),
		"date_string": date_str,
		"p1_name": p1_name,
		"p2_name": p2_name,
		"p1_char": p1_char,
		"p2_char": p2_char,
		"stage_id": stage_id,
		"bgm_path": bgm_path,
		"match_seed": match_seed,
		"winner_num": 0,
		"p1_wins": 0,
		"p2_wins": 0,
		"duration": 0.0,
		"events": []
	}
	is_recording = true

func record_event(evt: Dictionary) -> void:
	if not is_recording or not current_recording.has("events"):
		return
	(current_recording["events"] as Array).append(evt)

func record_player_state(
	time_sec: float,
	p_num: int,
	pos: Vector2,
	anim_row: int,
	is_focus: bool,
	is_charging: bool,
	is_shooting: bool,
	active_chg: float,
	passive_chg: float
) -> void:
	if not is_recording:
		return
	record_event({
		"t": snappedf(time_sec, 0.001),
		"type": "state",
		"p": p_num,
		"x": snappedf(pos.x, 0.1),
		"y": snappedf(pos.y, 0.1),
		"r": anim_row,
		"f": is_focus,
		"c": is_charging,
		"s": is_shooting,
		"ac": snappedf(active_chg, 0.01),
		"pc": snappedf(passive_chg, 0.01)
	})

func finish_recording(winner_num: int = 0, p1_wins: int = 0, p2_wins: int = 0, total_duration: float = 0.0) -> Dictionary:
	if not is_recording:
		return last_recorded_replay
	current_recording["winner_num"] = winner_num
	current_recording["p1_wins"] = p1_wins
	current_recording["p2_wins"] = p2_wins
	current_recording["duration"] = snappedf(total_duration, 0.1)
	last_recorded_replay = current_recording.duplicate(true)
	is_recording = false
	return last_recorded_replay

func has_last_recorded_replay() -> bool:
	return not last_recorded_replay.is_empty() and last_recorded_replay.has("events")

func get_slot_path(slot_idx: int) -> String:
	return REPLAY_DIR + "slot_%d.json" % slot_idx

func save_to_slot(slot_idx: int, replay_data: Dictionary = {}) -> bool:
	if slot_idx < 1 or slot_idx > MAX_SLOTS:
		return false
	_ensure_replay_directory()
	
	var data_to_save: Dictionary = replay_data if not replay_data.is_empty() else last_recorded_replay
	if data_to_save.is_empty():
		return false
	
	var json_str := JSON.stringify(data_to_save)
	var file := FileAccess.open(get_slot_path(slot_idx), FileAccess.WRITE)
	if file == null:
		push_error("[ReplayManager] Failed to open slot file for writing: %s" % get_slot_path(slot_idx))
		return false
	
	file.store_string(json_str)
	file.flush()
	file.close()
	
	_sync_web_filesystem()
	print("[ReplayManager] Successfully saved replay to Slot %d" % slot_idx)
	return true

func load_from_slot(slot_idx: int) -> Dictionary:
	if slot_idx < 1 or slot_idx > MAX_SLOTS:
		return {}
	var path := get_slot_path(slot_idx)
	if not FileAccess.file_exists(path):
		return {}
	
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return {}
	
	var json_str := file.get_as_text()
	file.close()
	
	var parsed = JSON.parse_string(json_str)
	if parsed is Dictionary:
		return parsed as Dictionary
	return {}

func delete_slot(slot_idx: int) -> bool:
	if slot_idx < 1 or slot_idx > MAX_SLOTS:
		return false
	var path := get_slot_path(slot_idx)
	if FileAccess.file_exists(path):
		var err := DirAccess.remove_absolute(path)
		if err == OK:
			_sync_web_filesystem()
			return true
	return false

func get_slot_summary(slot_idx: int) -> Dictionary:
	var path := get_slot_path(slot_idx)
	if not FileAccess.file_exists(path):
		return {
			"slot_index": slot_idx,
			"is_empty": true
		}
	
	var data := load_from_slot(slot_idx)
	if data.is_empty():
		return {
			"slot_index": slot_idx,
			"is_empty": true
		}
	
	return {
		"slot_index": slot_idx,
		"is_empty": false,
		"version": data.get("version", 1),
		"timestamp": data.get("timestamp", 0),
		"date_string": data.get("date_string", "Unknown"),
		"p1_name": data.get("p1_name", "Player 1"),
		"p2_name": data.get("p2_name", "Player 2"),
		"p1_char": data.get("p1_char", "reimu"),
		"p2_char": data.get("p2_char", "marisa"),
		"winner_num": data.get("winner_num", 0),
		"p1_wins": data.get("p1_wins", 0),
		"p2_wins": data.get("p2_wins", 0),
		"stage_id": data.get("stage_id", "bamboo_road"),
		"duration": data.get("duration", 0.0),
		"event_count": (data.get("events", []) as Array).size()
	}

func get_all_slot_summaries() -> Array[Dictionary]:
	var summaries: Array[Dictionary] = []
	for i in range(1, MAX_SLOTS + 1):
		summaries.append(get_slot_summary(i))
	return summaries

func start_playback(slot_idx: int, tree: SceneTree = null) -> bool:
	var data := load_from_slot(slot_idx)
	if data.is_empty():
		push_error("[ReplayManager] Cannot start playback; Slot %d is empty or corrupted." % slot_idx)
		return false
	
	active_replay = data
	is_replaying = true
	playback_speed = 1.0
	playback_paused = false
	
	var target_tree: SceneTree = tree if tree else (get_tree() if is_inside_tree() else null)
	if target_tree:
		var gm = target_tree.root.get_node_or_null("GameManager") if target_tree.root else null
		if gm:
			gm.p1_character = active_replay.get("p1_char", "reimu")
			gm.p2_character = active_replay.get("p2_char", "marisa")
			gm.p1_name = active_replay.get("p1_name", "1P")
			gm.p2_name = active_replay.get("p2_name", "2P")
			gm.reset_match_rounds()
		
		target_tree.change_scene_to_file("res://scenes/arena/arena.tscn")
	return true

func stop_playback(tree: SceneTree = null) -> void:
	is_replaying = false
	active_replay = {}
	playback_speed = 1.0
	playback_paused = false
	Engine.time_scale = 1.0
	var target_tree: SceneTree = tree if tree else (get_tree() if is_inside_tree() else null)
	if target_tree:
		target_tree.change_scene_to_file("res://scenes/main_menu/main_menu.tscn")

func toggle_pause() -> bool:
	playback_paused = not playback_paused
	playback_pause_toggled.emit(playback_paused)
	return playback_paused

func cycle_speed() -> float:
	playback_speed = 1.0
	return playback_speed

func _sync_web_filesystem() -> void:
	if OS.has_feature("web"):
		JavaScriptBridge.eval("if (window.FS && FS.syncfs) { FS.syncfs(false, function(err) {}); }")
