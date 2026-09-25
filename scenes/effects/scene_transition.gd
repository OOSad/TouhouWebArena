class_name SceneTransition
extends CanvasLayer

## Full-screen dual-panel screen wipe transition for masking scene loads.
## Left and right panels sweep inward meeting at the center (x=960),
## swapping the scene seamlessly while the screen is 100% masked, then sweeping outward.

signal wipe_closed
signal wipe_opened

@onready var wipe_control: Control = $WipeControl
@onready var left_panel: Control = $WipeControl/LeftPanel
@onready var right_panel: Control = $WipeControl/RightPanel

const HALF_WIDTH: float = 960.0
const PANEL_WIDTH: float = 964.0

var is_transitioning: bool = false

func _ready() -> void:
	layer = 120
	visible = false
	_reset_panels()

func _reset_panels() -> void:
	if left_panel:
		left_panel.position.x = -PANEL_WIDTH
	if right_panel:
		right_panel.position.x = 1920.0
	if wipe_control:
		wipe_control.mouse_filter = Control.MOUSE_FILTER_IGNORE

func play_transition(target_scene: String, duration_in: float = 0.30, hold_time: float = 0.10, duration_out: float = 0.30) -> void:
	if is_transitioning:
		return
	is_transitioning = true
	visible = true
	if wipe_control:
		wipe_control.mouse_filter = Control.MOUSE_FILTER_STOP
	_reset_panels()
	
	# Phase 1: Sweep in from left and right to center (meeting at x=960)
	var tw_in := create_tween().set_parallel(true)
	tw_in.tween_property(left_panel, "position:x", 0.0, duration_in).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	tw_in.tween_property(right_panel, "position:x", HALF_WIDTH - 4.0, duration_in).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	await tw_in.finished
	
	wipe_closed.emit()
	
	# Change scene while the screen is completely covered in black
	var tree := get_tree()
	if tree and not target_scene.is_empty():
		tree.change_scene_to_file(target_scene)
		# Wait for the scene to mount and render its initial frame
		await tree.process_frame
		await tree.process_frame
	
	if hold_time > 0.0:
		await get_tree().create_timer(hold_time).timeout
	
	# Phase 2: Sweep out from center back to edges
	var tw_out := create_tween().set_parallel(true)
	tw_out.tween_property(left_panel, "position:x", -PANEL_WIDTH, duration_out).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN)
	tw_out.tween_property(right_panel, "position:x", 1920.0, duration_out).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN)
	await tw_out.finished
	
	wipe_opened.emit()
	visible = false
	if wipe_control:
		wipe_control.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_reset_panels()
	is_transitioning = false

