class_name MainMenuReplays
extends Node

## The main menu's Replay Theater: one row per ReplayManager slot, with Watch and Delete.

signal closed
## The menu leaves the queue first, then starts playback.
signal watch_requested(slot_idx: int)

var modal: PanelContainer
var _slots_container: VBoxContainer
var _close_btn: Button

func setup(menu: Control) -> void:
	modal = menu.get_node_or_null("%ReplaysModal")
	_slots_container = menu.get_node_or_null("%ReplaySlotsContainer")
	_close_btn = menu.get_node_or_null("%CloseReplaysBtn")
	if _close_btn:
		_close_btn.pressed.connect(_on_close_pressed)
		_close_btn.focus_entered.connect(_play_focus_sound)
	if modal:
		modal.visible = false

func is_open() -> bool:
	return modal != null and modal.visible

func open() -> void:
	if modal == null:
		return
	_refresh()
	modal.visible = true
	if _close_btn:
		_close_btn.grab_focus()

## Hides the modal and tells the menu, which puts focus back on its Replays button.
func close() -> void:
	if modal:
		modal.visible = false
	closed.emit()

func _on_close_pressed() -> void:
	AudioService.play_cancel()
	close()

func _replay_manager() -> Node:
	return get_node_or_null("/root/ReplayManager") if is_inside_tree() else null

func _refresh() -> void:
	if not _slots_container:
		return
	for child in _slots_container.get_children():
		_slots_container.remove_child(child)
		child.queue_free()

	var rm := _replay_manager()
	var summaries: Array = rm.get_all_slot_summaries() if rm and rm.has_method("get_all_slot_summaries") else []

	for s in summaries:
		var slot_idx: int = s.get("slot_index", 1)

		var row := HBoxContainer.new()
		row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_theme_constant_override("separation", 12)

		var label := MenuRowStyle.label(23)
		label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(label)

		if s.get("is_empty", true):
			label.text = "Slot %d: [Empty]" % slot_idx
			label.add_theme_color_override("font_color", Color(0.5, 0.5, 0.55, 0.8))
		else:
			var p1: String = s.get("p1_name", "P1")
			var p2: String = s.get("p2_name", "P2")
			var c1: String = s.get("p1_char", "").capitalize()
			var c2: String = s.get("p2_char", "").capitalize()
			var dur: float = float(s.get("duration", 0.0))
			var winner_num: int = s.get("winner_num", 0)

			label.text = "Slot %d: %s (%s) vs %s (%s) [%02d:%02d]" % [slot_idx, p1, c1, p2, c2, floori(dur / 60.0), int(dur) % 60]
			if winner_num == 1:
				label.text += " - %s Win" % p1
			elif winner_num == 2:
				label.text += " - %s Win" % p2
			label.add_theme_color_override("font_color", Color(0.9, 0.95, 1.0, 1.0))

			var watch_btn := MenuRowStyle.button("▶ Watch", Vector2(105, 34), 20,
					Color(0.15, 0.35, 0.5, 0.85), Color(0.4, 0.8, 1.0, 0.9))
			watch_btn.pressed.connect(_on_watch_pressed.bind(slot_idx))
			watch_btn.focus_entered.connect(_play_focus_sound)
			row.add_child(watch_btn)

			var del_btn := MenuRowStyle.button("🗑", Vector2(45, 34), 20,
					Color(0.4, 0.15, 0.15, 0.85), Color(1.0, 0.4, 0.4, 0.9))
			del_btn.pressed.connect(_on_delete_pressed.bind(slot_idx))
			del_btn.focus_entered.connect(_play_focus_sound)
			row.add_child(del_btn)

		_slots_container.add_child(row)

func _on_watch_pressed(slot_idx: int) -> void:
	AudioService.play_confirm()
	watch_requested.emit(slot_idx)

func _on_delete_pressed(slot_idx: int) -> void:
	AudioService.play_cancel()
	var rm := _replay_manager()
	if rm and rm.has_method("delete_slot"):
		rm.delete_slot(slot_idx)
		_refresh()

func _play_focus_sound() -> void:
	AudioService.play_select()
