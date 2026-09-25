extends Control

## Hands the corridor camera to the user.
##
## The stage runs a three-pose timeline: it opens in one, moves at 00:35, and
## moves again at 01:00 into a resting angle that pans left and right while
## still advancing. This tunes all three against the reference frame for each.
##
## Holding a pose does not freeze the stage. It rigs the transition times so the
## chosen pose is the one the stage is in, which keeps the corridor scrolling and
## the pan sweeping: a frozen frame cannot be used to judge either.
##
## Renders into a 600x960 viewport, the size the real playfield uses. Running the
## stage scene on its own fills a 1920x1080 window instead, and the framing at
## that aspect is not the framing a match has.
##
## Open this scene in the editor and press F6. Reference panels load out of
## scratch/, which is gitignored, and hide themselves when absent.

const STAGE: PackedScene = preload("res://scenes/stages/eientei_corridor/eientei_corridor_3d.tscn")
const FIELD := Vector2i(600, 960)
const FAR_FUTURE: float = 1.0e9

# label, reference png, height export, pitch export
const POSES: Array = [
	["Opening (to 00:35)", "res://scratch/_state1_ref.png", "camera_height", "camera_pitch_deg"],
	["After 00:35", "res://scratch/_state2_ref.png", "second_camera_height", "second_camera_pitch_deg"],
	["After 01:00, resting", "res://scratch/_state3_ref.png", "third_camera_height", "third_camera_pitch_deg"],
]

# label, step, rebuild needed, suffix, export key (empty = taken from the pose)
const KNOBS: Array = [
	["height", 0.1, false, "", ""],
	["pitch", 1.0, false, " deg", ""],
	["pan (pose 3)", 1.0, false, " deg", "resting_pan_deg"],
	["pan period", 1.0, false, " s", "resting_pan_period"],
	["roll (pose 3)", 1.0, false, " deg", "resting_roll_deg"],
	["roll phase", 15.0, false, " deg", "resting_roll_phase_deg"],
	["corridor width", 0.05, true, "", "half_width"],
	["wall height", 0.1, true, "", "wall_height"],
	["fog start", 0.5, true, "", "fog_start"],
	["fog end", 0.5, true, "", "fog_end"],
	["speed", 0.1, false, " /s", "scroll_speed"],
]

const SHARED_KEYS: Array = ["half_width", "wall_height", "fog_start", "fog_end", "scroll_speed"]
## Pose 3 caught mid-roll, for judging the roll against (V swaps it in)
const ROLLED_REF: String = "res://scratch/_state3_rolled_ref.png"

var _sub: SubViewport
var _stage: Node3D
var _readout: RichTextLabel
var _reference: TextureRect
var _ref_caption: Label
var _ref_textures: Array = []
var _selected: int = 0
var _pose: int = 0
var _playing: bool = false
var _ref_shown: bool = true
var _rolled_tex: Texture2D = null
var _show_rolled: bool = false
var _values: Dictionary = {}


func _ready() -> void:
	var bg := ColorRect.new()
	bg.color = Color(0.08, 0.08, 0.10)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	var holder := SubViewportContainer.new()
	holder.position = Vector2(40, 50)
	holder.custom_minimum_size = Vector2(FIELD)
	add_child(holder)

	_sub = SubViewport.new()
	_sub.size = FIELD
	_sub.own_world_3d = true
	_sub.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	holder.add_child(_sub)

	var live := Label.new()
	live.position = Vector2(40, 20)
	live.text = "Ours (live)"
	live.add_theme_color_override("font_color", Color(0.6, 0.62, 0.7))
	add_child(live)

	_reference = TextureRect.new()
	_reference.position = Vector2(680, 50)
	_reference.size = Vector2(FIELD)
	_reference.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_reference.stretch_mode = TextureRect.STRETCH_SCALE
	add_child(_reference)

	_ref_caption = Label.new()
	_ref_caption.position = Vector2(680, 20)
	_ref_caption.add_theme_color_override("font_color", Color(0.6, 0.62, 0.7))
	add_child(_ref_caption)

	for p in POSES:
		var tex: Texture2D = null
		if FileAccess.file_exists(p[1]):
			var img: Image = Image.load_from_file(p[1])
			if img != null:
				tex = ImageTexture.create_from_image(img)
		_ref_textures.append(tex)

	if FileAccess.file_exists(ROLLED_REF):
		var rolled: Image = Image.load_from_file(ROLLED_REF)
		if rolled != null:
			_rolled_tex = ImageTexture.create_from_image(rolled)

	_readout = RichTextLabel.new()
	_readout.bbcode_enabled = true
	_readout.position = Vector2(1330, 50)
	_readout.size = Vector2(570, 1000)
	_readout.add_theme_font_size_override("normal_font_size", 18)
	add_child(_readout)

	var probe: Node3D = STAGE.instantiate()
	for p in POSES:
		_values[p[2]] = probe.get(p[2])
		_values[p[3]] = probe.get(p[3])
	for k in ["resting_pan_deg", "resting_pan_period", "resting_roll_deg", "resting_roll_phase_deg"] + SHARED_KEYS:
		_values[k] = probe.get(k)
	probe.free()

	_rebuild()


func _rebuild() -> void:
	if _stage != null and is_instance_valid(_stage):
		_sub.remove_child(_stage)
		_stage.free()
	_stage = STAGE.instantiate()
	_push(_stage)
	_sub.add_child(_stage)
	_stage._time = 0.0


## Pushes every value, then rigs the transition times so the stage sits in the
## pose being edited while still running.
func _push(target: Node3D) -> void:
	for key in _values:
		target.set(key, _values[key])
	if _playing:
		return
	if _pose == 0:
		target.set("second_pose_time", FAR_FUTURE)
		target.set("third_pose_time", FAR_FUTURE)
	elif _pose == 1:
		target.set("second_pose_time", 0.0)
		target.set("third_pose_time", FAR_FUTURE)
	else:
		target.set("second_pose_time", 0.0)
		target.set("third_pose_time", 0.01)


func _knob_key(i: int) -> String:
	var explicit: String = KNOBS[i][4]
	if not explicit.is_empty():
		return explicit
	return POSES[_pose][2] if i == 0 else POSES[_pose][3]


func _unhandled_key_input(event: InputEvent) -> void:
	if not (event is InputEventKey) or not event.pressed:
		return
	var key: int = event.keycode

	if key == KEY_F1 or key == KEY_F2 or key == KEY_F3:
		_pose = key - KEY_F1
		_playing = false
		_rebuild()
		return
	if key >= KEY_1 and key <= KEY_9 and key - KEY_1 < KNOBS.size():
		_selected = key - KEY_1
		return
	if key == KEY_0 and KNOBS.size() > 9:
		_selected = 9
		return
	if key == KEY_LEFT or key == KEY_RIGHT:
		_selected = posmod(_selected + (1 if key == KEY_RIGHT else -1), KNOBS.size())
		return
	if key == KEY_R:
		_playing = true
		_rebuild()
		return
	if key == KEY_V:
		_show_rolled = not _show_rolled
		return
	if key == KEY_TAB:
		_ref_shown = not _ref_shown
		return
	if key == KEY_C:
		DisplayServer.clipboard_set(_as_text())
		return
	if key == KEY_ESCAPE:
		get_tree().quit()
		return

	var dir: float = 0.0
	if key == KEY_UP or key == KEY_EQUAL or key == KEY_KP_ADD:
		dir = 1.0
	elif key == KEY_DOWN or key == KEY_MINUS or key == KEY_KP_SUBTRACT:
		dir = -1.0
	if is_zero_approx(dir):
		return

	var knob: Array = KNOBS[_selected]
	var step: float = knob[1]
	if event.shift_pressed:
		step *= 0.1
	elif event.ctrl_pressed:
		step *= 5.0
	var k: String = _knob_key(_selected)
	_values[k] = snappedf(float(_values[k]) + dir * step, 0.001)

	if knob[2]:
		var carry: float = _stage._time
		_rebuild()
		_stage._time = carry
	else:
		_push(_stage)


func _as_text() -> String:
	var lines: PackedStringArray = []
	for i in range(POSES.size()):
		lines.append("state%d (%s): %s = %s, %s = %s" % [
			i + 1, POSES[i][0],
			POSES[i][2], String.num(float(_values[POSES[i][2]]), 3),
			POSES[i][3], String.num(float(_values[POSES[i][3]]), 3)])
	lines.append("pan: resting_pan_deg = %s, resting_pan_period = %s, resting_roll_deg = %s, resting_roll_phase_deg = %s" % [
		String.num(float(_values["resting_pan_deg"]), 3),
		String.num(float(_values["resting_pan_period"]), 3),
		String.num(float(_values["resting_roll_deg"]), 3),
		String.num(float(_values["resting_roll_phase_deg"]), 3)])
	var sh: PackedStringArray = []
	for k in SHARED_KEYS:
		sh.append("%s = %s" % [k, String.num(float(_values[k]), 3)])
	lines.append("shared: " + ", ".join(sh))
	return "\n".join(lines)


func _process(_delta: float) -> void:
	var s: String = "[b]Corridor camera[/b]\n"
	for i in range(POSES.size()):
		var active: bool = (not _playing) and i == _pose
		var col: String = "#e8e8f0" if active else "#6a7080"
		var mark: String = ">" if active else " "
		s += "[color=%s]%s F%d  %s[/color]\n" % [col, mark, i + 1, POSES[i][0]]
	if _playing:
		s += "[color=#7ad67a]>  R   playing timeline   %.1fs[/color]\n" % _stage._time
	else:
		s += "[color=#6a7080]   R   play whole timeline[/color]\n"

	s += "\n[b]%s[/b]\n" % ("live" if _playing else POSES[_pose][0])
	for i in range(KNOBS.size()):
		if i == 2 or i == 6:
			s += "\n"
		var k: Array = KNOBS[i]
		var val: String = String.num(float(_values[_knob_key(i)]), 3) + String(k[3])
		if i == _selected:
			s += "[bgcolor=#3a4a6a] %d  %-15s %10s [/bgcolor]\n" % [i + 1, k[0], val]
		else:
			s += "[color=#9aa0b0] %d  %-15s %10s[/color]\n" % [i + 1, k[0], val]

	s += "\n[color=#9aa0b0]F1-F3  hold a pose (still moving)\n"
	s += "R      play the whole timeline\n"
	s += "1-9,0  pick a value (Left/Right to step)\n"
	s += "Up/Dn  change it\n"
	s += "Shift  finer   Ctrl  bigger\n"
	s += "TAB    show/hide reference\n"
	s += "V      pose 3: centred / mid-roll reference\n"
	s += "C      copy everything\n"
	s += "Esc    quit[/color]\n"
	s += "\n[color=#6a7080]Pan and roll only show on pose 3. Roll phase 0 rolls in\nstep with the look; 90 makes the view trace a circle.[/color]"
	_readout.text = s

	var ref_tex: Texture2D = _ref_textures[_pose]
	var rolled_now: bool = _pose == 2 and _show_rolled and _rolled_tex != null
	if rolled_now:
		ref_tex = _rolled_tex
	var have: bool = ref_tex != null
	_reference.texture = ref_tex
	_reference.visible = _ref_shown and have and not _playing
	_ref_caption.visible = _reference.visible
	_ref_caption.text = "PoFV reference  -  " + String(POSES[_pose][0]) + ("  (mid-roll)" if rolled_now else "") + "  -  TAB to hide"
