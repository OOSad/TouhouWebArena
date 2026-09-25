class_name ScreenWipe
extends Control

signal wipe_closed
signal wipe_completed

const PLAYFIELD_PANEL_WIDTH: float = 600.0

@onready var left_panel: Control = $LeftHalf/LeftPanel
@onready var right_panel: Control = $RightHalf/RightPanel
@onready var ready_label: Label = $LeftHalf/LeftPanel/ReadyLabel
@onready var right_ready_label: Label = $RightHalf/RightPanel/RightReadyLabel
@onready var left_container: Control = $LeftHalf
@onready var right_container: Control = $RightHalf

var is_animating: bool = false

func _ready() -> void:
	if left_panel == null: left_panel = get_node_or_null("LeftHalf/LeftPanel")
	if right_panel == null: right_panel = get_node_or_null("RightHalf/RightPanel")
	if ready_label == null: ready_label = get_node_or_null("LeftHalf/LeftPanel/ReadyLabel")
	if right_ready_label == null: right_ready_label = get_node_or_null("RightHalf/RightPanel/RightReadyLabel")
	if left_container == null: left_container = get_node_or_null("LeftHalf")
	if right_container == null: right_container = get_node_or_null("RightHalf")
	visible = false
	_reset_panels()

func _reset_panels() -> void:
	if left_panel:
		left_panel.position.x = -PLAYFIELD_PANEL_WIDTH
	if right_panel:
		right_panel.position.x = PLAYFIELD_PANEL_WIDTH
	if ready_label:
		ready_label.modulate.a = 0.0
	if right_ready_label:
		right_ready_label.modulate.a = 0.0

## Plays the dual directional screen wipe specifically across the two playfields
## (Left-to-Right for P1, Right-to-Left for P2)
func play_round_transition(round_num: int, duration_in: float = 0.35, hold_time: float = 0.45, duration_out: float = 0.35) -> void:
	if is_animating:
		return
	is_animating = true
	visible = true
	_reset_panels()
	
	var label_text: String = "ROUND %d\nREADY?" % round_num
	var labels: Array[Label] = []
	if ready_label: labels.append(ready_label)
	if right_ready_label: labels.append(right_ready_label)
	
	for lbl in labels:
		lbl.text = label_text
		lbl.modulate.a = 0.0
		lbl.scale = Vector2(0.85, 0.85)
		lbl.pivot_offset = lbl.size / 2.0
	
	# Phase 1: Sweep in (P1: -600 -> 0; P2: 600 -> 0)
	var tw_in := create_tween().set_parallel(true)
	tw_in.tween_property(left_panel, "position:x", 0.0, duration_in).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	tw_in.tween_property(right_panel, "position:x", 0.0, duration_in).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	
	await tw_in.finished
	
	# Playfields are fully covered: Emit wipe_closed so playfields and hazards can be reset
	wipe_closed.emit()
	
	# Reveal Ready labels
	var tw_label := create_tween().set_parallel(true)
	for lbl in labels:
		tw_label.tween_property(lbl, "modulate:a", 1.0, 0.15)
		tw_label.tween_property(lbl, "scale", Vector2.ONE, 0.25).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	
	# Hold covered while player reads Ready and boards finish clean reset
	await get_tree().create_timer(hold_time).timeout
	
	# Fade labels before sweeping out
	var tw_label_out := create_tween().set_parallel(true)
	for lbl in labels:
		tw_label_out.tween_property(lbl, "modulate:a", 0.0, 0.1)
	
	# Phase 2: Sweep out (P1: 0 -> 600; P2: 0 -> -600)
	var tw_out := create_tween().set_parallel(true)
	tw_out.tween_property(left_panel, "position:x", PLAYFIELD_PANEL_WIDTH, duration_out).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN)
	tw_out.tween_property(right_panel, "position:x", -PLAYFIELD_PANEL_WIDTH, duration_out).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN)
	
	await tw_out.finished
	
	visible = false
	_reset_panels()
	is_animating = false
	wipe_completed.emit()
