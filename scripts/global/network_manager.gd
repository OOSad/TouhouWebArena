extends Node

# WebRTC High-Level Multiplayer Manager for 1v1 Matches
signal lobby_player_list_updated(players: Array)
signal match_ready(p1_name: String, p2_name: String)
signal opponent_character_changed(character_name: String)
signal opponent_handicap_changed(hp: float)
signal opponent_confirmed()
signal opponent_backed_out()
signal connection_status_changed(status: String)

# In-Match Gameplay Synchronization Signals
signal remote_player_updated(pos: Vector2, anim_row: int, is_focus: bool, is_charging: bool, is_shooting: bool, active_charge: float, passive_charge: float)
signal remote_attack_sent(death_pos: Vector2, pellet_count: int, has_big_pellet: bool, has_spirit: bool, has_extra_attack: bool, sender_char: String, bounce_count: int, extra_targets: PackedVector2Array, sender_rank: int)
signal remote_charge_attack_fired(level: int, attack_name: String, sender_char: String)
signal remote_spellcard_activated(level: int, rank: int, sender_char: String)
signal remote_health_updated(hp: float, max_hp: float)
signal remote_hit_shockwave_spawned(landing_pos: Vector2)
signal remote_player_defeated(loser_num: int)
signal remote_round_transition(winner_num: int, p1_wins: int, p2_wins: int, next_round: int, is_final_win: bool)
signal opponent_quit_match()

# Post-Match Handshake Signals
signal post_match_cursor_updated(player_num: int, option_index: int)
signal post_match_confirmed(player_num: int, option_index: int)

# Spectator Mode Signals
signal spectator_joined_match(room_id: String, p1_name: String, p2_name: String)
signal spectator_count_updated(count: int)
signal spectator_match_ended()
signal spectator_match_init(p1_char: String, p2_char: String, stage_id: String, p1_wins: int, p2_wins: int, current_round: int, is_round_active: bool)
signal spectator_player_updated(player_num: int, pos: Vector2, anim_row: int, is_focus: bool, is_charging: bool, is_shooting: bool, active_charge: float, passive_charge: float)
signal spectator_attack_sent(player_num: int, death_pos: Vector2, pellet_count: int, has_big_pellet: bool, has_spirit: bool, has_extra_attack: bool, sender_char: String, bounce_count: int, extra_targets: PackedVector2Array, sender_rank: int)
signal spectator_charge_attack_fired(player_num: int, level: int, attack_name: String, sender_char: String)
signal spectator_spellcard_activated(player_num: int, level: int, rank: int, sender_char: String)
signal spectator_health_updated(player_num: int, hp: float, max_hp: float)
signal spectator_hit_shockwave_spawned(player_num: int, landing_pos: Vector2)
signal spectator_player_defeated(loser_num: int)
signal spectator_round_transition(winner_num: int, p1_wins: int, p2_wins: int, next_round: int, is_final_win: bool)
signal spectator_round_started(current_round: int)

@export var signaling_url: String = "wss://touhou-arena-relay.onrender.com"
const STUN_SERVER: String = "stun:stun.l.google.com:19302"

func set_signaling_url(new_url: String) -> void:
	var trimmed := new_url.strip_edges()
	if not trimmed.is_empty():
		signaling_url = trimmed

# Signaling WebSocket
var ws: WebSocketPeer = WebSocketPeer.new()
var ws_connected: bool = false

# WebRTC Peer
var rtc_peer: WebRTCMultiplayerPeer = null
var rtc_conn: WebRTCPeerConnection = null
var remote_peer_id: int = 0
var _pending_ice_candidates: Array[Dictionary] = []
var _has_remote_description: bool = false

var is_host: bool = false
var is_searching: bool = false
var is_in_bot_match: bool = false
var is_connected_to_lobby: bool = false
var is_p2p_connected: bool = false
var is_spectator: bool = false
var spectating_room_id: String = ""
var spectator_count: int = 0
var spectator_p1_name: String = "Player 1"
var spectator_p2_name: String = "Player 2"
var spectator_p1_char: String = ""
var spectator_p2_char: String = ""
var spectator_stage_id: String = ""
var spectator_p1_wins: int = 0
var spectator_p2_wins: int = 0
var spectator_current_round: int = 1
var spectator_is_round_active: bool = false
var match_seed: int = 99991

var local_nickname: String = "Anonymous Fairy"
var remote_nickname: String = ""
var current_password: String = ""

# Role in match: 1 = Host/P1, 2 = Client/P2, 0 = Spectator
var player_number: int = 1

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	if OS.has_feature("web"):
		var web_origin = JavaScriptBridge.eval("window.location.origin.replace(/^http/, 'ws')")
		if web_origin and not str(web_origin).is_empty():
			var origin_str: String = str(web_origin)
			if "localhost" in origin_str or "127.0.0.1" in origin_str:
				signaling_url = origin_str
			else:
				signaling_url = "wss://touhou-arena-relay.onrender.com"
			print("[NetworkManager] Web export detected. Auto-configured signaling_url: ", signaling_url)
	multiplayer.peer_connected.connect(_on_webrtc_peer_connected)
	multiplayer.peer_disconnected.connect(_on_webrtc_peer_disconnected)

func _process(_delta: float) -> void:
	_poll_signaling()
	if rtc_conn:
		rtc_conn.poll()
	if rtc_peer:
		rtc_peer.poll()

func is_network_active() -> bool:
	return ((rtc_peer != null and multiplayer.multiplayer_peer != null and is_p2p_connected) or is_spectator)

func spectate_room(room_id: String, password: String = "") -> void:
	if is_searching:
		set_searching(false)
	is_in_bot_match = false
	is_spectator = true
	spectating_room_id = room_id
	player_number = 0
	print("[NetworkManager] Requesting to spectate room: %s..." % room_id)
	connection_status_changed.emit("Joining spectator room...")
	if ws_connected:
		_send_signaling({
			"type": "spectate_room",
			"roomId": room_id,
			"password": password
		})

func leave_spectate() -> void:
	if is_spectator:
		print("[NetworkManager] Leaving spectate mode.")
		is_spectator = false
		spectating_room_id = ""
		spectator_p1_char = ""
		spectator_p2_char = ""
		spectator_stage_id = ""
		spectator_p1_wins = 0
		spectator_p2_wins = 0
		spectator_current_round = 1
		spectator_is_round_active = false
		player_number = 1
		if ws_connected:
			_send_signaling({"type": "leave_spectate"})

func broadcast_spectator_event(event_dict: Dictionary) -> void:
	if not (is_host and ws_connected):
		return
	var evt_type: String = str(event_dict.get("type", ""))
	var is_lifecycle: bool = evt_type in ["match_init", "round_started", "round_transition", "player_defeated"]
	if spectator_count > 0 or is_lifecycle:
		_send_signaling({
			"type": "spectator_broadcast",
			"event": event_dict
		})

# --- Signaling Connection ---

func get_signaling_nickname() -> String:
	var base := local_nickname.strip_edges() if not local_nickname.strip_edges().is_empty() else "Anonymous Fairy"
	return (base + "::bot") if is_in_bot_match else base

func connect_to_lobby(my_nickname: String) -> void:
	local_nickname = my_nickname.strip_edges() if not my_nickname.strip_edges().is_empty() else "Anonymous Fairy"
	
	if ws_connected or ws.get_ready_state() == WebSocketPeer.STATE_CONNECTING:
		# Update nickname if already connected
		_send_signaling({"type": "update_nickname", "nickname": get_signaling_nickname()})
		return
	
	print("[NetworkManager] Connecting to signaling server at %s..." % signaling_url)
	connection_status_changed.emit("Connecting to lobby...")
	var err: Error = ws.connect_to_url(signaling_url)
	if err != OK:
		print("[NetworkManager] Failed to initiate WebSocket connection: ", err)
		connection_status_changed.emit("Signaling connection failed")

func set_searching(searching: bool, password: String = "") -> void:
	is_searching = searching
	current_password = password.strip_edges() if searching else ""
	if searching:
		is_in_bot_match = false
	if ws_connected:
		_send_signaling({"type": "update_nickname", "nickname": local_nickname})
		_send_signaling({
			"type": "set_searching",
			"searching": is_searching,
			"password": current_password
		})

func set_in_bot_match(in_bot: bool) -> void:
	if is_in_bot_match == in_bot:
		return
	is_in_bot_match = in_bot
	if is_in_bot_match:
		is_searching = false
		current_password = ""
	if ws_connected:
		_send_signaling({
			"type": "set_bot_match",
			"in_bot_match": is_in_bot_match
		})
		_send_signaling({
			"type": "update_nickname",
			"nickname": get_signaling_nickname()
		})

func update_nickname(new_name: String) -> void:
	local_nickname = new_name.strip_edges() if not new_name.strip_edges().is_empty() else "Anonymous Fairy"
	if ws_connected:
		_send_signaling({"type": "update_nickname", "nickname": get_signaling_nickname()})

func cancel_matchmaking() -> void:
	if is_spectator:
		leave_spectate()
	is_in_bot_match = false
	set_searching(false, "")
	_cleanup_webrtc()
	if ws_connected:
		_send_signaling({"type": "update_nickname", "nickname": local_nickname})
		_send_signaling({"type": "leave_match"})

func leave_matchmaking() -> void:
	cancel_matchmaking()

func disconnect_from_lobby() -> void:
	cancel_matchmaking()
	if ws.get_ready_state() != WebSocketPeer.STATE_CLOSED:
		ws.close()
	ws_connected = false
	is_connected_to_lobby = false
	connection_status_changed.emit("Disconnected")

func _poll_signaling() -> void:
	ws.poll()
	var state := ws.get_ready_state()
	
	if state == WebSocketPeer.STATE_OPEN:
		if not ws_connected:
			ws_connected = true
			is_connected_to_lobby = true
			print("[NetworkManager] Connected to signaling server!")
			connection_status_changed.emit("Connected to lobby")
			# Send initial hello
			_send_signaling({
				"type": "hello",
				"nickname": get_signaling_nickname()
			})
			if is_in_bot_match:
				_send_signaling({
					"type": "set_bot_match",
					"in_bot_match": true
				})
			elif is_searching:
				_send_signaling({
					"type": "set_searching",
					"searching": true,
					"password": current_password
				})
		
		while ws.get_available_packet_count() > 0:
			var packet := ws.get_packet()
			var msg_text := packet.get_string_from_utf8()
			var json = JSON.parse_string(msg_text)
			if json is Dictionary:
				_handle_signaling_message(json)
			else:
				print("[NetworkManager] Received non-JSON signaling packet: ", msg_text)
				
	elif state == WebSocketPeer.STATE_CLOSED:
		if ws_connected:
			ws_connected = false
			is_connected_to_lobby = false
			print("[NetworkManager] Disconnected from signaling server.")
			connection_status_changed.emit("Disconnected from lobby")
			lobby_player_list_updated.emit([])

func _send_signaling(data: Dictionary) -> void:
	if ws.get_ready_state() == WebSocketPeer.STATE_OPEN:
		var json_str := JSON.stringify(data)
		ws.send_text(json_str)

# --- Signaling Message Handler ---

func _handle_signaling_message(msg: Dictionary) -> void:
	var msg_type: String = msg.get("type", "")
	
	match msg_type:
		"welcome":
			print("[NetworkManager] Signaling welcome acknowledged (Client #%s)" % str(msg.get("clientId", "")))
			
		"lobby_list":
			var players: Array = msg.get("players", [])
			lobby_player_list_updated.emit(players)
			
		"match_formed":
			var role: String = msg.get("role", "client")
			var my_peer_id: int = int(msg.get("myPeerId", 2))
			remote_peer_id = int(msg.get("remotePeerId", 1))
			remote_nickname = msg.get("opponentName", "Opponent")
			match_seed = int(msg.get("matchSeed", 99991))
			
			print("[NetworkManager] Match formed! Role: %s, MyPeerID: %d, RemotePeerID: %d, Opponent: %s, Seed: %d" % [
				role, my_peer_id, remote_peer_id, remote_nickname, match_seed
			])
			
			_setup_webrtc(role, my_peer_id, remote_peer_id)
			
		"signal":
			var data: Dictionary = msg.get("data", {})
			_handle_webrtc_signal(data)
			
		"opponent_left":
			print("[NetworkManager] Opponent disconnected or left match.")
			_cleanup_webrtc()
			opponent_backed_out.emit()
			opponent_quit_match.emit()
			connection_status_changed.emit("Opponent left")
			
		"spectate_joined":
			is_spectator = true
			player_number = 0
			spectating_room_id = str(msg.get("roomId", ""))
			spectator_p1_name = str(msg.get("p1Name", "Player 1"))
			spectator_p2_name = str(msg.get("p2Name", "Player 2"))
			spectator_count = int(msg.get("spectatorCount", 1))
			match_seed = int(msg.get("matchSeed", 99991))
			
			if msg.has("matchState") and msg["matchState"] != null and typeof(msg["matchState"]) == TYPE_DICTIONARY:
				var ms: Dictionary = msg["matchState"]
				spectator_p1_char = str(ms.get("p1_char", ""))
				spectator_p2_char = str(ms.get("p2_char", ""))
				spectator_stage_id = str(ms.get("stage_id", ""))
				spectator_p1_wins = int(ms.get("p1_wins", 0))
				spectator_p2_wins = int(ms.get("p2_wins", 0))
				spectator_current_round = int(ms.get("current_round", 1))
				spectator_is_round_active = bool(ms.get("is_round_active", false))
			
			_apply_spectator_state_to_game_manager()
			
			print("[NetworkManager] Spectate joined room %s (%s vs %s) | MatchState: chars=(%s, %s), stage=%s, round=%d, active=%s, seed=%d" % [
				spectating_room_id, spectator_p1_name, spectator_p2_name,
				spectator_p1_char, spectator_p2_char, spectator_stage_id, spectator_current_round, str(spectator_is_round_active), match_seed
			])
			spectator_joined_match.emit(spectating_room_id, spectator_p1_name, spectator_p2_name)
			
		"spectate_failed":
			is_spectator = false
			spectating_room_id = ""
			spectator_p1_char = ""
			spectator_p2_char = ""
			spectator_stage_id = ""
			spectator_p1_wins = 0
			spectator_p2_wins = 0
			spectator_current_round = 1
			spectator_is_round_active = false
			player_number = 1
			var reason: String = str(msg.get("reason", "Failed to spectate match."))
			print("[NetworkManager] Spectate failed: ", reason)
			connection_status_changed.emit(reason)
			
		"spectator_joined":
			spectator_count = int(msg.get("spectatorCount", 0))
			print("[NetworkManager] New spectator joined match. Total: ", spectator_count)
			spectator_count_updated.emit(spectator_count)
			
		"spectator_left":
			spectator_count = int(msg.get("spectatorCount", 0))
			print("[NetworkManager] Spectator left match. Total: ", spectator_count)
			spectator_count_updated.emit(spectator_count)
			
		"spectate_match_ended":
			print("[NetworkManager] Spectated match has ended.")
			is_spectator = false
			spectating_room_id = ""
			spectator_p1_char = ""
			spectator_p2_char = ""
			spectator_stage_id = ""
			spectator_p1_wins = 0
			spectator_p2_wins = 0
			spectator_current_round = 1
			spectator_is_round_active = false
			player_number = 1
			spectator_match_ended.emit()
			connection_status_changed.emit("Match ended")
			
		"spectator_event":
			var evt: Dictionary = msg.get("event", {})
			_handle_spectator_event(evt)

func _apply_spectator_state_to_game_manager() -> void:
	var gm = get_node_or_null("/root/GameManager")
	if gm == null:
		return
	gm.set_player_names(spectator_p1_name, spectator_p2_name)
	gm.p2_is_ai = false
	if not spectator_p1_char.is_empty():
		gm.set_p1_character(spectator_p1_char)
	if not spectator_p2_char.is_empty():
		gm.set_p2_character(spectator_p2_char)
	gm.p1_round_wins = spectator_p1_wins
	gm.p2_round_wins = spectator_p2_wins
	gm.current_round = spectator_current_round

func _handle_spectator_event(evt: Dictionary) -> void:
	var evt_type: String = evt.get("type", "")
	match evt_type:
		"match_init":
			spectator_p1_char = str(evt.get("p1_char", "youmu"))
			spectator_p2_char = str(evt.get("p2_char", "marisa"))
			spectator_stage_id = str(evt.get("stage_id", "bamboo_road"))
			spectator_p1_wins = int(evt.get("p1_wins", 0))
			spectator_p2_wins = int(evt.get("p2_wins", 0))
			spectator_current_round = int(evt.get("current_round", 1))
			spectator_is_round_active = bool(evt.get("is_round_active", false))
			if evt.has("match_seed"):
				match_seed = int(evt["match_seed"])
			_apply_spectator_state_to_game_manager()
			spectator_match_init.emit(
				spectator_p1_char,
				spectator_p2_char,
				spectator_stage_id,
				spectator_p1_wins,
				spectator_p2_wins,
				spectator_current_round,
				spectator_is_round_active
			)
		"round_started":
			spectator_is_round_active = true
			if evt.has("current_round"):
				spectator_current_round = int(evt["current_round"])
				var gm = get_node_or_null("/root/GameManager")
				if gm:
					gm.current_round = spectator_current_round
			spectator_round_started.emit(spectator_current_round)
		"player_state":
			var p_num: int = int(evt.get("p_num", 1))
			var pos := Vector2(float(evt.get("x", 0.0)), float(evt.get("y", 0.0)))
			spectator_player_updated.emit(
				p_num,
				pos,
				int(evt.get("anim_row", 0)),
				bool(evt.get("is_focus", false)),
				bool(evt.get("is_charging", false)),
				bool(evt.get("is_shooting", false)),
				float(evt.get("active_charge", 0.0)),
				float(evt.get("passive_charge", 1.0))
			)
		"attack":
			var p_num: int = int(evt.get("p_num", 1))
			var death_pos := Vector2(float(evt.get("x", 0.0)), float(evt.get("y", 0.0)))
			spectator_attack_sent.emit(
				p_num,
				death_pos,
				int(evt.get("pellet_count", 0)),
				bool(evt.get("has_big", false)),
				bool(evt.get("has_spirit", false)),
				bool(evt.get("has_extra", false)),
				str(evt.get("sender_char", "reimu")),
				int(evt.get("bounce_count", 0)),
				MoteDispatcher.targets_from_flat(evt.get("extra_targets", [])),
				int(evt.get("rank", 1))
			)
		"charge_attack":
			var p_num: int = int(evt.get("p_num", 1))
			spectator_charge_attack_fired.emit(
				p_num,
				int(evt.get("level", 1)),
				str(evt.get("attack_name", "")),
				str(evt.get("sender_char", ""))
			)
		"spellcard":
			var p_num: int = int(evt.get("p_num", 1))
			spectator_spellcard_activated.emit(
				p_num,
				int(evt.get("level", 2)),
				int(evt.get("rank", 1)),
				str(evt.get("sender_char", ""))
			)
		"health":
			var p_num: int = int(evt.get("p_num", 1))
			spectator_health_updated.emit(
				p_num,
				float(evt.get("hp", 5.0)),
				float(evt.get("max_hp", 5.0))
			)
		"hit_shockwave":
			var p_num: int = int(evt.get("p_num", 1))
			var pos := Vector2(float(evt.get("x", 0.0)), float(evt.get("y", 0.0)))
			spectator_hit_shockwave_spawned.emit(p_num, pos)
		"player_defeated":
			spectator_player_defeated.emit(int(evt.get("loser_num", 1)))
		"round_transition":
			spectator_p1_wins = int(evt.get("p1_wins", 0))
			spectator_p2_wins = int(evt.get("p2_wins", 0))
			spectator_current_round = int(evt.get("next_round", 1))
			spectator_is_round_active = false
			_apply_spectator_state_to_game_manager()
			spectator_round_transition.emit(
				int(evt.get("winner_num", 1)),
				spectator_p1_wins,
				spectator_p2_wins,
				spectator_current_round,
				bool(evt.get("is_final", false))
			)

# --- WebRTC P2P Setup ---

func _setup_webrtc(role: String, my_peer_id: int, target_peer_id: int) -> void:
	_cleanup_webrtc()
	
	is_host = (role == "host")
	player_number = 1 if is_host else 2
	
	rtc_peer = WebRTCMultiplayerPeer.new()
	var err: Error
	if is_host:
		err = rtc_peer.create_server()
	else:
		err = rtc_peer.create_client(my_peer_id)
	
	if err != OK:
		print("[NetworkManager] Error initializing WebRTCMultiplayerPeer: ", err)
		return
	
	multiplayer.multiplayer_peer = rtc_peer
	
	rtc_conn = WebRTCPeerConnection.new()
	var init_err: Error = rtc_conn.initialize({
		"iceServers": [
			{"urls": [STUN_SERVER, "stun:stun1.l.google.com:19302"]}
		]
	})
	
	if init_err != OK:
		print("[NetworkManager] Error initializing WebRTCPeerConnection: ", init_err)
		return
	
	rtc_conn.session_description_created.connect(_on_session_description_created)
	rtc_conn.ice_candidate_created.connect(_on_ice_candidate_created)
	
	var add_err: Error = rtc_peer.add_peer(rtc_conn, target_peer_id)
	if add_err != OK:
		print("[NetworkManager] Error adding peer to WebRTCMultiplayerPeer: ", add_err)
		return
	
	if is_host:
		print("[NetworkManager] Host creating WebRTC Offer...")
		rtc_conn.create_offer()

func _on_session_description_created(type: String, sdp: String) -> void:
	if rtc_conn == null:
		return
	var set_err := rtc_conn.set_local_description(type, sdp)
	if set_err != OK:
		print("[NetworkManager] Error setting local description: ", set_err)
	print("[NetworkManager] WebRTC local description set: %s. Sending via signaling..." % type)
	_send_signaling({
		"type": "signal",
		"data": {
			"type": type,
			"sdp": sdp
		}
	})

func _on_ice_candidate_created(media: String, index: int, name: String) -> void:
	print("[NetworkManager] Discovered ICE candidate. Sending via signaling...")
	_send_signaling({
		"type": "signal",
		"data": {
			"type": "candidate",
			"mid": media,
			"mline_index": index,
			"candidate": name
		}
	})

func _handle_webrtc_signal(data: Dictionary) -> void:
	if rtc_conn == null:
		print("[NetworkManager] Received WebRTC signal without active connection.")
		return
	
	var sig_type: String = data.get("type", "")
	
	if sig_type == "offer":
		print("[NetworkManager] Received WebRTC Offer! Setting remote description (answer generated automatically)...")
		var set_err := rtc_conn.set_remote_description("offer", data.get("sdp", ""))
		if set_err == OK:
			_has_remote_description = true
			_flush_pending_ice_candidates()
		else:
			print("[NetworkManager] Error setting remote description for offer: ", set_err)
	elif sig_type == "answer":
		print("[NetworkManager] Received WebRTC Answer! Setting remote description...")
		var set_err := rtc_conn.set_remote_description("answer", data.get("sdp", ""))
		if set_err == OK:
			_has_remote_description = true
			_flush_pending_ice_candidates()
		else:
			print("[NetworkManager] Error setting remote description for answer: ", set_err)
	elif sig_type == "candidate":
		if not _has_remote_description:
			print("[NetworkManager] Received ICE candidate before remote description; queueing...")
			_pending_ice_candidates.append(data)
		else:
			var mid: String = data.get("mid", "")
			var mline_index: int = int(data.get("mline_index", 0))
			var candidate: String = data.get("candidate", "")
			var add_err := rtc_conn.add_ice_candidate(mid, mline_index, candidate)
			if add_err != OK:
				print("[NetworkManager] Error adding ICE candidate: ", add_err)

func _flush_pending_ice_candidates() -> void:
	if rtc_conn == null:
		return
	print("[NetworkManager] Flushing %d pending ICE candidates..." % _pending_ice_candidates.size())
	for c in _pending_ice_candidates:
		var mid: String = c.get("mid", "")
		var mline_index: int = int(c.get("mline_index", 0))
		var candidate: String = c.get("candidate", "")
		var add_err := rtc_conn.add_ice_candidate(mid, mline_index, candidate)
		if add_err != OK:
			print("[NetworkManager] Error adding queued ICE candidate: ", add_err)
	_pending_ice_candidates.clear()

func _cleanup_webrtc() -> void:
	is_p2p_connected = false
	_has_remote_description = false
	_pending_ice_candidates.clear()
	if rtc_conn:
		rtc_conn.close()
		rtc_conn = null
	if rtc_peer:
		rtc_peer.close()
		rtc_peer = null
	if is_inside_tree() and multiplayer.multiplayer_peer != null:
		multiplayer.multiplayer_peer = null

# --- High-Level Multiplayer Callbacks ---

func _on_webrtc_peer_connected(connected_peer_id: int) -> void:
	print("[NetworkManager] WebRTC P2P DataChannel connected with peer: %d!" % connected_peer_id)
	is_p2p_connected = true
	is_searching = false
	
	# Determine P1 and P2 display names
	var p1_name: String = local_nickname if is_host else remote_nickname
	var p2_name: String = remote_nickname if is_host else local_nickname
	
	var gm = get_node_or_null("/root/GameManager")
	if gm:
		gm.player_nickname = local_nickname
		gm.set_player_names(p1_name, p2_name)
		gm.p2_is_ai = false
	
	match_ready.emit(p1_name, p2_name)

func _on_webrtc_peer_disconnected(disconnected_peer_id: int) -> void:
	print("[NetworkManager] WebRTC P2P peer disconnected: %d" % disconnected_peer_id)
	is_p2p_connected = false
	_cleanup_webrtc()
	opponent_backed_out.emit()
	opponent_quit_match.emit()
	connection_status_changed.emit("Opponent disconnected")

# --- Character Select Sync ---

func send_character_selection(char_name: String) -> void:
	if is_network_active():
		rpc("rpc_sync_character", char_name)

@rpc("any_peer", "call_remote", "reliable")
func rpc_sync_character(char_name: String) -> void:
	opponent_character_changed.emit(char_name)

func send_handicap_selection(hp: float) -> void:
	if is_network_active():
		rpc("rpc_sync_handicap", hp)

@rpc("any_peer", "call_remote", "reliable")
func rpc_sync_handicap(hp: float) -> void:
	opponent_handicap_changed.emit(hp)

func send_confirm_selection() -> void:
	if is_network_active():
		rpc("rpc_confirm_selection")

@rpc("any_peer", "call_remote", "reliable")
func rpc_confirm_selection() -> void:
	opponent_confirmed.emit()

func send_character_select_backout() -> void:
	if is_network_active():
		rpc("rpc_opponent_backed_out")
		if rtc_conn:
			rtc_conn.poll()
		if rtc_peer:
			rtc_peer.poll()
	if ws_connected:
		_send_signaling({"type": "leave_match"})

@rpc("any_peer", "call_remote", "reliable")
func rpc_opponent_backed_out() -> void:
	print("[NetworkManager] Opponent backed out of character select.")
	opponent_backed_out.emit()
	connection_status_changed.emit("Opponent left")

# --- In-Match Gameplay Synchronization RPCs ---

func send_player_state(pos: Vector2, anim_row: int, is_focus: bool, is_charging: bool, is_shooting: bool, active_chg: float, passive_chg: float) -> void:
	if is_network_active():
		rpc("rpc_player_state", pos, anim_row, is_focus, is_charging, is_shooting, active_chg, passive_chg)

@rpc("any_peer", "call_remote", "unreliable")
func rpc_player_state(pos: Vector2, anim_row: int, is_focus: bool, is_charging: bool, is_shooting: bool, active_chg: float, passive_chg: float) -> void:
	remote_player_updated.emit(pos, anim_row, is_focus, is_charging, is_shooting, active_chg, passive_chg)

func send_attack(death_pos: Vector2, pellet_count: int, has_big_pellet: bool, has_spirit: bool, has_extra_attack: bool, sender_char: String, bounce_count: int = 0, extra_targets: PackedVector2Array = PackedVector2Array(), sender_rank: int = 1) -> void:
	if is_network_active():
		rpc("rpc_attack", death_pos, pellet_count, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, extra_targets, sender_rank)

@rpc("any_peer", "call_remote", "reliable")
func rpc_attack(death_pos: Vector2, pellet_count: int, has_big_pellet: bool, has_spirit: bool, has_extra_attack: bool, sender_char: String, bounce_count: int = 0, extra_targets: PackedVector2Array = PackedVector2Array(), sender_rank: int = 1) -> void:
	remote_attack_sent.emit(death_pos, pellet_count, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, extra_targets, sender_rank)

func send_charge_attack(level: int, attack_name: String, sender_char: String) -> void:
	if is_network_active():
		rpc("rpc_charge_attack", level, attack_name, sender_char)

@rpc("any_peer", "call_remote", "reliable")
func rpc_charge_attack(level: int, attack_name: String, sender_char: String) -> void:
	remote_charge_attack_fired.emit(level, attack_name, sender_char)

func send_spellcard(level: int, rank: int, sender_char: String) -> void:
	if is_network_active():
		rpc("rpc_spellcard", level, rank, sender_char)

@rpc("any_peer", "call_remote", "reliable")
func rpc_spellcard(level: int, rank: int, sender_char: String) -> void:
	remote_spellcard_activated.emit(level, rank, sender_char)

func send_health(hp: float, max_hp: float) -> void:
	if is_network_active():
		rpc("rpc_health", hp, max_hp)

@rpc("any_peer", "call_remote", "reliable")
func rpc_health(hp: float, max_hp: float) -> void:
	remote_health_updated.emit(hp, max_hp)

func send_hit_shockwave(landing_pos: Vector2) -> void:
	if is_network_active():
		rpc("rpc_hit_shockwave", landing_pos)

@rpc("any_peer", "call_remote", "reliable")
func rpc_hit_shockwave(landing_pos: Vector2) -> void:
	remote_hit_shockwave_spawned.emit(landing_pos)

func send_player_defeated(loser_num: int) -> void:
	if is_network_active():
		rpc("rpc_player_defeated", loser_num)

@rpc("any_peer", "call_remote", "reliable")
func rpc_player_defeated(loser_num: int) -> void:
	remote_player_defeated.emit(loser_num)

func send_round_transition(winner_num: int, p1_wins: int, p2_wins: int, next_round: int, is_final_win: bool) -> void:
	if is_network_active():
		rpc("rpc_round_transition", winner_num, p1_wins, p2_wins, next_round, is_final_win)

@rpc("any_peer", "call_remote", "reliable")
func rpc_round_transition(winner_num: int, p1_wins: int, p2_wins: int, next_round: int, is_final_win: bool) -> void:
	remote_round_transition.emit(winner_num, p1_wins, p2_wins, next_round, is_final_win)

# --- Post-Match Handshake RPCs ---

func send_post_match_cursor(option_idx: int) -> void:
	if is_network_active():
		rpc("rpc_post_match_cursor", player_number, option_idx)

@rpc("any_peer", "call_remote", "reliable")
func rpc_post_match_cursor(sender_num: int, option_idx: int) -> void:
	post_match_cursor_updated.emit(sender_num, option_idx)

func send_post_match_confirm(option_idx: int) -> void:
	if is_network_active():
		rpc("rpc_post_match_confirm", player_number, option_idx)

@rpc("any_peer", "call_remote", "reliable")
func rpc_post_match_confirm(sender_num: int, option_idx: int) -> void:
	post_match_confirmed.emit(sender_num, option_idx)

# --- In-Match Forfeit / Quit RPCs ---

func send_match_quit() -> void:
	if is_network_active():
		rpc("rpc_opponent_quit_match")
		if rtc_conn:
			rtc_conn.poll()
		if rtc_peer:
			rtc_peer.poll()
	if ws_connected:
		_send_signaling({"type": "leave_match"})

@rpc("any_peer", "call_remote", "reliable")
func rpc_opponent_quit_match() -> void:
	print("[NetworkManager] Opponent forfeited/quit match.")
	opponent_quit_match.emit()
	connection_status_changed.emit("Opponent left match")



