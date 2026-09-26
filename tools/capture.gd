extends SceneTree

## Opens a scene, runs a short list of steps, saves screenshots and quits: a way to look at a
## screen without playing to it. Run from the project folder (windowed, so it can render):
##
##   Godot.exe --path . --resolution 1920x1080 -s res://tools/capture.gd -- <scene> "<steps>"
##
## <scene> is a res:// path or a path relative to scenes/ ("main_menu/main_menu.tscn"), or "-" to
## start with none (set things up, then open one with a scene: step).
## <steps> are separated by ";":
##   wait:N               let N frames pass (the scene gets 2 frames before step one)
##   scene:PATH           open another scene (same path rules as <scene>)
##   graphics             wait until the player's .dat graphics are built (arena captures)
##   shot:NAME            save scratch/captures/NAME.png
##   call:TARGET.METHOD[:ARG,ARG]
##                        call a method. TARGET is a node path from the scene ("Settings",
##                        "%SettingsBtn"), "/root/..." for autoloads, or empty (".open") for
##                        the scene itself. ARGs are Godot values (1, 2.5, true, "text");
##                        anything else is passed as text.
##   get:TARGET.PROPERTY  print a value (for checking state without a picture)
##   action:NAME          press and release an input action (e.g. cancel, ui_accept)
##
## Prints "CAPTURE ..." lines and exits 1 on the first failed step.
## The main menu joins the live lobby when it loads: fine to look at, never queue from it.

const OUT_DIR := "res://scratch/captures"
const GRAPHICS_TIMEOUT_FRAMES := 60 * 60

var _steps: PackedStringArray = []
var _step_idx: int = 0
var _wait_frames: int = 2
var _waiting_for_graphics: int = -1
var _pending_release: String = ""

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.is_empty():
		_fail("usage: -- <scene> \"<steps>\" (see the top of tools/capture.gd)")
		return
	if args.size() > 1:
		for step in args[1].split(";", false):
			_steps.append(step.strip_edges())
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	print("CAPTURE %d steps" % _steps.size())
	if args[0] != "-":
		_open_scene(args[0])

func _process(_delta: float) -> bool:
	if not _pending_release.is_empty():
		_send_action(_pending_release, false)
		_pending_release = ""
	if _waiting_for_graphics >= 0:
		var gd := root.get_node_or_null("GameData")
		if gd and gd.graphics_are_ready:
			_waiting_for_graphics = -1
		elif _waiting_for_graphics >= GRAPHICS_TIMEOUT_FRAMES:
			_fail("graphics never became ready (are the .dat files on the file screen?)")
		else:
			_waiting_for_graphics += 1
		return false
	if _wait_frames > 0:
		_wait_frames -= 1
		return false
	if _step_idx >= _steps.size():
		print("CAPTURE done")
		quit(0)
		return true
	var step := _steps[_step_idx]
	_step_idx += 1
	_run(step)
	return false

func _run(step: String) -> void:
	var kind := step.get_slice(":", 0)
	var rest := step.substr(kind.length() + 1)
	match kind:
		"wait":
			_wait_frames = maxi(rest.to_int(), 0)
		"scene":
			_open_scene(rest)
		"graphics":
			_waiting_for_graphics = 0
		"shot":
			var path := OUT_DIR.path_join(rest + ".png")
			root.get_viewport().get_texture().get_image().save_png(path)
			print("CAPTURE shot %s" % ProjectSettings.globalize_path(path))
			_wait_frames = 1
		"call":
			var target_method := rest.get_slice(":", 0)
			var arg_text := rest.substr(target_method.length() + 1)
			var node := _target(target_method)
			var method := target_method.get_slice(".", target_method.get_slice_count(".") - 1)
			if node == null or not node.has_method(method):
				_fail("no method %s" % target_method)
				return
			var result = node.callv(method, _parse_args(arg_text))
			print("CAPTURE call %s -> %s" % [target_method, result])
			_wait_frames = 1
		"get":
			var node := _target(rest)
			if node == null:
				_fail("no node for %s" % rest)
				return
			var prop := rest.get_slice(".", rest.get_slice_count(".") - 1)
			print("CAPTURE get %s = %s" % [rest, node.get(prop)])
		"action":
			if not InputMap.has_action(rest):
				_fail("no input action %s" % rest)
				return
			_send_action(rest, true)
			_pending_release = rest
			_wait_frames = 1
		_:
			_fail("unknown step '%s'" % step)

func _open_scene(scene_path: String) -> void:
	if not scene_path.begins_with("res://"):
		scene_path = "res://scenes/".path_join(scene_path)
	if change_scene_to_file(scene_path) != OK:
		_fail("can't open %s" % scene_path)
		return
	print("CAPTURE scene %s" % scene_path)
	_wait_frames = 2

## The node named by everything before the last "." ("" is the scene itself).
func _target(target_member: String) -> Node:
	var dot := target_member.rfind(".")
	var node_path := target_member.substr(0, dot) if dot >= 0 else ""
	if node_path.is_empty():
		return current_scene
	if node_path.begins_with("/"):
		return root.get_node_or_null(node_path)
	return current_scene.get_node_or_null(node_path) if current_scene else null

func _parse_args(text: String) -> Array:
	var args: Array = []
	if text.is_empty():
		return args
	for part in text.split(","):
		var value = str_to_var(part.strip_edges())
		args.append(value if value != null or part.strip_edges() == "null" else part.strip_edges())
	return args

func _send_action(action: String, pressed: bool) -> void:
	var ev := InputEventAction.new()
	ev.action = action
	ev.pressed = pressed
	Input.parse_input_event(ev)

func _fail(message: String) -> void:
	push_error("CAPTURE failed: " + message)
	print("CAPTURE failed: " + message)
	quit(1)
