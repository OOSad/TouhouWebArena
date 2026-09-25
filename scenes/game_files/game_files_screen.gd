extends Control
## First screen on boot: asks the player for their own copy of ZUN's data files.
## Files are dropped onto the window; the GameData autoload does the actual loading.

const MAIN_MENU_SCENE: String = "res://scenes/main_menu/main_menu.tscn"

# One row per file the game needs.
const REQUIRED_FILES: Array[Dictionary] = [
	{"id": "th09", "file": "th09.dat", "game": "Touhou 9 ~ Phantasmagoria of Flower View"},
	{"id": "th15", "file": "th15.dat", "game": "Touhou 15 ~ Legacy of Lunatic Kingdom"},
	{"id": "thbgm", "file": "thbgm.dat", "game": "Touhou 9 music (in the same folder as th09.dat)"},
]

const COLOR_MISSING := Color(0.95, 0.55, 0.5)
const COLOR_LOADED := Color(0.55, 0.95, 0.6)
const COLOR_INFO := Color(0.85, 0.85, 0.9)

@onready var file_list: VBoxContainer = %FileList
@onready var message_label: Label = %MessageLabel
@onready var continue_btn: Button = %ContinueBtn
@onready var cirno_font: Font = preload("res://Cirno.ttf")

# file id -> status Label
var _status_labels: Dictionary = {}
var _graphics_ready: bool = false


func _ready() -> void:
	# Files remembered from an earlier launch: nothing to ask for. Wait for the graphics
	# built from them (loaded from the cache, a moment) so no match can start without them.
	if _game_data().has_all_files():
		if not _game_data().graphics_are_ready:
			await _game_data().graphics_ready
		_go_to_menu.call_deferred()
		return
	for f in REQUIRED_FILES:
		_add_file_row(f)
	# Dropped folders are searched on desktop only so far; the browser's drop is separate.
	if not OS.has_feature("web"):
		($Center/Panel/VBox/HintLabel as Label).text = "Drag them onto this window, or drop a whole folder holding your games\n(your Steam library, say) and they'll be found. The Touhou games are sold on Steam."
	_show_message("")
	continue_btn.pressed.connect(_go_to_menu)
	var game_data := _game_data()
	game_data.archive_loaded.connect(_on_archive_loaded)
	game_data.archive_failed.connect(_on_archive_failed)
	game_data.graphics_ready.connect(_on_graphics_ready)
	game_data.music_preparing.connect(_on_music_preparing)
	_refresh()


func _add_file_row(f: Dictionary) -> void:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 28)
	row.alignment = BoxContainer.ALIGNMENT_CENTER
	row.add_child(_make_label(f.file, 48, Color.WHITE))
	row.add_child(_make_label(f.game, 30, Color(0.8, 0.8, 0.85)))
	var status := _make_label("", 34, COLOR_MISSING)
	row.add_child(status)
	_status_labels[f.id] = status
	file_list.add_child(row)


func _make_label(text: String, size: int, color: Color) -> Label:
	var label := Label.new()
	label.text = text
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.add_theme_font_override("font", cirno_font)
	label.add_theme_font_size_override("font_size", size)
	label.add_theme_color_override("font_color", color)
	label.add_theme_color_override("font_outline_color", Color.BLACK)
	label.add_theme_constant_override("outline_size", 6)
	return label


func _refresh() -> void:
	var all_loaded := true
	for f in REQUIRED_FILES:
		var loaded: bool = _game_data().has_archive(f.id)
		var status: Label = _status_labels[f.id]
		status.text = "Loaded" if loaded else "Missing"
		status.add_theme_color_override("font_color", COLOR_LOADED if loaded else COLOR_MISSING)
		all_loaded = all_loaded and loaded
	continue_btn.visible = all_loaded and _graphics_ready
	if all_loaded and not _graphics_ready:
		_show_message("Preparing graphics from your files (first time only)...", COLOR_INFO)
	if continue_btn.visible:
		continue_btn.grab_focus()


func _on_archive_loaded(_game_id: String, _file_count: int) -> void:
	# A new drop rebuilds the graphics.
	_graphics_ready = false
	_show_message("")
	_refresh()


func _on_archive_failed(message: String) -> void:
	_show_message(message)


func _go_to_menu() -> void:
	get_tree().change_scene_to_file(MAIN_MENU_SCENE)


func _game_data() -> Node:
	return get_node("/root/GameData")


func _on_graphics_ready() -> void:
	_graphics_ready = true
	_show_message("")
	_refresh()


## Errors show in red; `color` is for progress notes.
func _show_message(text: String, color: Color = COLOR_MISSING) -> void:
	message_label.text = text
	message_label.visible = not text.is_empty()
	message_label.add_theme_color_override("font_color", color)


func _on_music_preparing(track: int, of: int) -> void:
	_show_message("Preparing music from thbgm.dat (first time only)... %d of %d" % [track, of], COLOR_INFO)
