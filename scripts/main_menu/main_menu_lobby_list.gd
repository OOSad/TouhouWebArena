class_name MainMenuLobbyList
extends Node

## The main menu's lobby panel: everyone connected to the signaling server, coloured by what
## they're doing (searching, in a room, in a match, in a bot match), with a Watch button on
## matches that can be spectated.

signal watch_requested(room_id: String, password: String)

var _header_label: Label
var _list_container: VBoxContainer
var _empty_notice_label: Label

func setup(menu: Control) -> void:
	_header_label = menu.get_node_or_null("%QueueHeaderLabel")
	_list_container = menu.get_node_or_null("%QueueListContainer")
	_empty_notice_label = menu.get_node_or_null("%EmptyNoticeLabel")

## Rebuilds the list. local_pw is this player's room password, "" when public.
func refresh(players: Array, local_pw: String) -> void:
	var nm: Node = get_node_or_null("/root/NetworkManager") if is_inside_tree() else null

	var searching_count: int = 0
	var in_match_count: int = 0
	for p in players:
		var raw_name: String = str(p.get("name", "Anonymous Fairy"))
		var is_bot: bool = p.get("in_bot_match", false) or raw_name.ends_with("::bot")
		if p.get("is_searching", false):
			searching_count += 1
		elif p.get("in_match", false) or is_bot:
			in_match_count += 1

	if _header_label:
		if in_match_count > 0:
			_header_label.text = "LOBBY PLAYERS (%d searching, %d in match)" % [searching_count, in_match_count]
		else:
			_header_label.text = "PLAYERS IN QUEUE (%d)" % searching_count

	if _list_container == null:
		return
	# Clear previous list items (keeping empty notice)
	for child in _list_container.get_children():
		if child != _empty_notice_label:
			_list_container.remove_child(child)
			child.queue_free()

	if _empty_notice_label:
		_empty_notice_label.visible = players.is_empty()

	for p in players:
		var item_label := MenuRowStyle.label(26)
		item_label.add_theme_color_override("font_outline_color", Color(0, 0, 0, 1.0))
		item_label.add_theme_constant_override("outline_size", 4)
		item_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.95))
		item_label.add_theme_constant_override("shadow_offset_x", 2)
		item_label.add_theme_constant_override("shadow_offset_y", 2)

		var raw_name: String = str(p.get("name", "Anonymous Fairy"))
		var is_local: bool = p.get("is_local", false)
		var in_bot_match: bool = p.get("in_bot_match", false) or raw_name.ends_with("::bot")
		if is_local and nm and nm.get("is_in_bot_match") == true:
			in_bot_match = true
		var status := _status_line(p, raw_name.trim_suffix("::bot").strip_edges(), is_local, in_bot_match, local_pw)
		item_label.text = status[0]
		item_label.add_theme_color_override("font_color", status[1])

		var room_id: String = str(p.get("room_id", ""))
		var can_spectate: bool = p.get("can_spectate", false) and not room_id.is_empty() and not is_local
		if not can_spectate:
			_list_container.add_child(item_label)
			continue

		var row := HBoxContainer.new()
		row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_theme_constant_override("separation", 8)
		item_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(item_label)

		var spectator_count: int = int(p.get("spectator_count", 0))
		var watch_btn := MenuRowStyle.button(
				"👁 Watch" if spectator_count == 0 else "👁 Watch (%d)" % spectator_count,
				Vector2(110, 30), 19, Color(0.12, 0.28, 0.4, 0.85), Color(0.35, 0.8, 1.0, 0.9), 3, false)
		watch_btn.pressed.connect(func(): watch_requested.emit(room_id, local_pw))
		row.add_child(watch_btn)
		_list_container.add_child(row)

## [text, colour] for one lobby entry.
func _status_line(p: Dictionary, display_name: String, is_local: bool, in_bot_match: bool, local_pw: String) -> Array:
	var you := " (You)" if is_local else ""
	if p.get("in_match", false):
		if is_local:
			if not local_pw.is_empty():
				return ["• %s (You) - In Match [Room: %s]" % [display_name, local_pw], Color(1.0, 0.85, 0.4)]
			return ["• %s (You) - In Match (Public)" % display_name, Color(1.0, 0.85, 0.4)]
		if p.get("room_matches_password", false) and not local_pw.is_empty():
			return ["• %s - In Match [Room: %s]" % [display_name, local_pw], Color(0.5, 0.85, 1.0)]
		if p.get("room_has_password", false):
			return ["• %s - In Match (Private)" % display_name, Color(0.65, 0.65, 0.75)]
		return ["• %s - In Match (Public)" % display_name, Color(0.8, 0.75, 0.9)]
	if in_bot_match:
		return ["• %s%s - In Bot Match" % [display_name, you], Color(0.45, 0.9, 0.7) if is_local else Color(0.6, 0.82, 0.75)]
	var searching: bool = p.get("is_searching", false)
	if is_local:
		if not searching:
			return ["• %s (You) - Browsing" % display_name, Color(0.55, 0.65, 0.7)]
		if not local_pw.is_empty():
			return ["• %s (You) - Room: %s" % [display_name, local_pw], Color(0.4, 0.8, 1.0)]
		return ["• %s (You) - Searching (Public)" % display_name, Color(0.4, 0.8, 1.0)]
	if searching:
		if p.get("matches_password", false):
			if not local_pw.is_empty():
				return ["• %s - Ready to Match! [Room: %s]" % [display_name, local_pw], Color(0.3, 1.0, 0.5)]
			return ["• %s - Ready to Match! (Public)" % display_name, Color(0.3, 1.0, 0.5)]
		if p.get("has_password", false):
			return ["• %s - In Private Room" % display_name, Color(0.7, 0.7, 0.8)]
		return ["• %s - Searching (Public)" % display_name, Color(0.9, 0.9, 0.9)]
	return ["• %s - Browsing" % display_name, Color(0.6, 0.6, 0.65)]
