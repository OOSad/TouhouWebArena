extends Control

@onready var p1_viewport_container: SubViewportContainer = %P1ViewportContainer
@onready var p2_viewport_container: SubViewportContainer = %P2ViewportContainer
@onready var p1_playfield: Playfield = %P1Playfield
@onready var p2_playfield: Playfield = %P2Playfield
@onready var effects_overlay: Node2D = %EffectsOverlay
@onready var p1_health_gauge: HealthGauge = %P1HealthGauge
@onready var p2_health_gauge: HealthGauge = %P2HealthGauge
@onready var p1_round_wins: RoundWinsDisplay = %P1RoundWins
@onready var p2_round_wins: RoundWinsDisplay = %P2RoundWins
@onready var p1_spell_bar: SpellBar = %P1SpellBar if has_node("%P1SpellBar") else null
@onready var p2_spell_bar: SpellBar = %P2SpellBar if has_node("%P2SpellBar") else null
@onready var screen_wipe: ScreenWipe = %ScreenWipe
@onready var victory_banner: PanelContainer = %VictoryBanner
@onready var victory_label: Label = %VictoryLabel
@onready var p1_name_label: Label = %P1NameLabel if has_node("%P1NameLabel") else null
@onready var p2_name_label: Label = %P2NameLabel if has_node("%P2NameLabel") else null
@onready var debug_menu: DebugMenu = %DebugMenu if has_node("%DebugMenu") else null
@onready var match_timer: MatchTimer = %MatchTimer if has_node("%MatchTimer") else null
@onready var fullscreen_button: Button = %FullscreenButton if has_node("%FullscreenButton") else null
@onready var stats_toggle_button: Button = %StatsToggleButton if has_node("%StatsToggleButton") else null
@onready var profiler_overlay: CanvasLayer = %ProfilerOverlay if has_node("%ProfilerOverlay") else null
@onready var forfeit_banner: PanelContainer = %ForfeitBanner if has_node("%ForfeitBanner") else null
@onready var forfeit_label: Label = %ForfeitLabel if has_node("%ForfeitLabel") else null

const ActionStopCoordinatorScript = preload("res://scripts/arena/action_stop_coordinator.gd")
const MoteDispatcherScript = preload("res://scripts/arena/mote_dispatcher.gd")
const ArenaReplayControllerScript = preload("res://scripts/arena/arena_replay_controller.gd")
const ArenaSpectatorCoordinatorScript = preload("res://scripts/arena/arena_spectator_coordinator.gd")
const ArenaPresentationScript = preload("res://scripts/arena/arena_presentation.gd")

var action_stop_coordinator = null
var mote_dispatcher = null
var replay_controller = null
var spectator_coordinator = null
var presentation = null

var mote_pool: NodePool:
	get:
		_ensure_mote_dispatcher()
		return mote_dispatcher.mote_pool if mote_dispatcher else null
	set(val):
		_ensure_mote_dispatcher()
		if mote_dispatcher: mote_dispatcher.mote_pool = val

# Red for Player 1 -> Player 2; Blue for Player 2 -> Player 1
const P1_MOTE_COLOR: Color = Color(1.0, 0.35, 0.45, 1.0)
const P2_MOTE_COLOR: Color = Color(0.3, 0.75, 1.0, 1.0)

var _round_elapsed_time: float = 0.0
const LILY_INITIAL_SPAWN_TIME: float = 50.0
const LILY_REPEAT_INTERVAL: float = 30.0
var _next_lily_spawn_time: float = LILY_INITIAL_SPAWN_TIME

const SUDDEN_DEATH_ROUND_TIME: float = 60.0
var _sudden_death_active: bool = false

var _lily_white_spawning_enabled: bool = true
@export var lily_white_spawning_enabled: bool = true:
	get: return _lily_white_spawning_enabled
	set(val):
		_lily_white_spawning_enabled = val
		if p1_playfield: p1_playfield.lily_white_spawning_enabled = val
		if p2_playfield: p2_playfield.lily_white_spawning_enabled = val

var _p1_combo: int = 0
var _p2_combo: int = 0
var _match_over: bool = false
var _round_intermission: bool = false
var _p1_ai_active: bool = false
var _p2_ai_active: bool = false
var _p1_ai_dodge_only: bool = false
var _p2_ai_dodge_only: bool = false

# Netplay state
var _is_networked: bool = false
var local_player_num: int = 1
var _net_broadcast_timer: float = 0.0
const NET_BROADCAST_INTERVAL: float = 0.04 # 25 updates/sec
var _last_remote_packet_time: float = 0.0
const REMOTE_PACKET_TIMEOUT_SEC: float = 10.0

# Forfeit / Escape backout state
var _escape_press_count: int = 0
var _last_escape_time_msec: int = 0
var _escape_timer_tween: Tween = null
const ESCAPE_DOUBLE_TAP_WINDOW_MSEC: int = 3000
var _is_forfeiting: bool = false
var _chosen_bgm_path: String = "res://assets/music/02_spring_lane.ogg"
var _current_stage_id: String = "bamboo_road"
var _is_waiting_for_next_round: bool:
	get: return spectator_coordinator.is_waiting_for_next_round if spectator_coordinator else false
	set(val):
		if spectator_coordinator: spectator_coordinator.is_waiting_for_next_round = val
var _waiting_overlay: Control:
	get: return spectator_coordinator.waiting_overlay if spectator_coordinator else null
	set(val):
		if spectator_coordinator: spectator_coordinator.waiting_overlay = val

# Replay state
var _match_total_elapsed: float = 0.0

var _is_replay_mode: bool:
	get: return replay_controller.is_replay_mode if replay_controller else false

var _action_stop_active: bool:
	get: return action_stop_coordinator.is_active if action_stop_coordinator else false

func _ensure_action_stop_coordinator() -> void:
	if action_stop_coordinator == null:
		action_stop_coordinator = ActionStopCoordinatorScript.new()
		add_child(action_stop_coordinator)
		action_stop_coordinator.setup(self, p1_playfield, p2_playfield)

func _ensure_mote_dispatcher() -> void:
	if mote_dispatcher == null:
		mote_dispatcher = MoteDispatcherScript.new()
		add_child(mote_dispatcher)
		mote_dispatcher.setup(self, effects_overlay)
	elif effects_overlay and mote_dispatcher.effects_overlay != effects_overlay:
		mote_dispatcher.effects_overlay = effects_overlay
		mote_dispatcher._ensure_pool()

func _ensure_replay_controller() -> void:
	if replay_controller == null:
		replay_controller = ArenaReplayControllerScript.new()
		add_child(replay_controller)
		replay_controller.setup(
			self, p1_playfield, p2_playfield, effects_overlay,
			match_timer, victory_banner, victory_label
		)

func _ensure_spectator_coordinator() -> void:
	if spectator_coordinator == null:
		spectator_coordinator = ArenaSpectatorCoordinatorScript.new()
		add_child(spectator_coordinator)
		spectator_coordinator.setup(
			self, p1_playfield, p2_playfield, p1_viewport_container, p2_viewport_container,
			match_timer, p1_health_gauge, p2_health_gauge, p1_spell_bar, p2_spell_bar,
			p1_round_wins, p2_round_wins, p1_name_label, p2_name_label
		)

func _ensure_presentation() -> void:
	if presentation == null:
		presentation = ArenaPresentationScript.new()
		add_child(presentation)
		presentation.setup(
			self, p1_playfield, p2_playfield, screen_wipe,
			victory_banner, victory_label, forfeit_banner, forfeit_label,
			p1_round_wins, p2_round_wins, match_timer
		)

func _ensure_coordinators() -> void:
	_ensure_action_stop_coordinator()
	_ensure_mote_dispatcher()
	_ensure_replay_controller()
	_ensure_spectator_coordinator()
	_ensure_presentation()

func _get_round_seed(round_num: int) -> int:
	var base: int = 99991
	if _is_replay_mode:
		var rep_mgr = get_node_or_null("/root/ReplayManager")
		if rep_mgr and rep_mgr.active_replay.has("match_seed"):
			base = int(rep_mgr.active_replay["match_seed"])
	else:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr and net_mgr.match_seed > 0:
			base = net_mgr.match_seed
	return base + round_num * 10007

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	if p1_viewport_container:
		p1_viewport_container.process_mode = Node.PROCESS_MODE_PAUSABLE
	if p2_viewport_container:
		p2_viewport_container.process_mode = Node.PROCESS_MODE_PAUSABLE
	# Configure player authority
	var net_mgr = get_node_or_null("/root/NetworkManager")
	var game_mgr = get_node_or_null("/root/GameManager")
	
	_is_networked = (net_mgr != null and net_mgr.is_network_active())
	_last_remote_packet_time = Time.get_ticks_msec() / 1000.0
	if _is_networked:
		local_player_num = net_mgr.player_number
		if net_mgr.is_spectator:
			net_mgr.spectator_match_init.connect(_on_spectator_match_init)
			net_mgr.spectator_player_updated.connect(_on_spectator_player_updated)
			net_mgr.spectator_attack_sent.connect(_on_spectator_attack_sent)
			net_mgr.spectator_charge_attack_fired.connect(_on_spectator_charge_attack_fired)
			net_mgr.spectator_spellcard_activated.connect(_on_spectator_spellcard_activated)
			net_mgr.spectator_health_updated.connect(_on_spectator_health_updated)
			net_mgr.spectator_hit_shockwave_spawned.connect(_on_spectator_hit_shockwave_spawned)
			net_mgr.spectator_player_defeated.connect(_on_spectator_player_defeated)
			net_mgr.spectator_round_transition.connect(_on_spectator_round_transition)
			net_mgr.spectator_round_started.connect(_on_spectator_round_started)
			net_mgr.spectator_match_ended.connect(_on_spectator_match_ended)
		else:
			net_mgr.remote_player_updated.connect(_on_remote_player_updated)
			net_mgr.remote_attack_sent.connect(_on_remote_attack_sent)
			net_mgr.remote_charge_attack_fired.connect(_on_remote_charge_attack_fired)
			net_mgr.remote_spellcard_activated.connect(_on_remote_spellcard_activated)
			net_mgr.remote_health_updated.connect(_on_remote_health_updated)
			net_mgr.remote_hit_shockwave_spawned.connect(_on_remote_hit_shockwave_spawned)
			net_mgr.remote_player_defeated.connect(_on_remote_player_defeated)
			net_mgr.remote_round_transition.connect(_on_remote_round_transition)
			if local_player_num == 1:
				net_mgr.spectator_count_updated.connect(_on_host_spectator_count_updated)
		if net_mgr.has_signal("opponent_quit_match"):
			net_mgr.opponent_quit_match.connect(_on_opponent_quit_match)
		if net_mgr.has_signal("connection_status_changed"):
			net_mgr.connection_status_changed.connect(_on_connection_status_changed)
	
	_ensure_coordinators()
	var rep_mgr = get_node_or_null("/root/ReplayManager")
	if replay_controller and replay_controller.init_replay_mode():
		local_player_num = 0
		_is_networked = true
	
	var p1_char := "youmu"
	var p2_char := "marisa"
	if _is_replay_mode and rep_mgr:
		p1_char = rep_mgr.active_replay.get("p1_char", "reimu")
		p2_char = rep_mgr.active_replay.get("p2_char", "marisa")
	elif _is_networked and net_mgr and net_mgr.is_spectator:
		if not net_mgr.spectator_p1_char.is_empty():
			p1_char = net_mgr.spectator_p1_char
		elif game_mgr != null:
			var p1_d := CharacterData.get_character(game_mgr.p1_character)
			if p1_d: p1_char = p1_d.character_id
		if not net_mgr.spectator_p2_char.is_empty():
			p2_char = net_mgr.spectator_p2_char
		elif game_mgr != null:
			var p2_d := CharacterData.get_character(game_mgr.p2_character)
			if p2_d: p2_char = p2_d.character_id
	elif game_mgr != null:
		var p1_data := CharacterData.get_character(game_mgr.p1_character)
		var p2_data := CharacterData.get_character(game_mgr.p2_character)
		if p1_data: p1_char = p1_data.character_id
		if p2_data: p2_char = p2_data.character_id
	
	var p1_res := CharacterData.get_character(p1_char)
	var p2_res := CharacterData.get_character(p2_char)
	
	var p1_title: String = "PLAYER 1"
	var p2_title: String = "PLAYER 2"
	if _is_replay_mode and rep_mgr:
		p1_title = rep_mgr.active_replay.get("p1_name", "PLAYER 1")
		p2_title = rep_mgr.active_replay.get("p2_name", "PLAYER 2")
	elif game_mgr != null:
		if _is_networked:
			p1_title = game_mgr.p1_name
			p2_title = game_mgr.p2_name
		else:
			p1_title = game_mgr.player_nickname if not game_mgr.player_nickname.is_empty() else "PLAYER 1"
			p2_title = "CPU" if game_mgr.p2_is_ai else "PLAYER 2"
	
	var p1_col: Color = p1_res.primary_color if p1_res else P1_MOTE_COLOR
	if p1_name_label:
		p1_name_label.text = p1_title.to_upper()
		p1_name_label.modulate = p1_col
	var p1_handicap: float = game_mgr.p1_handicap_hp if game_mgr else 5.0
	var p2_handicap: float = game_mgr.p2_handicap_hp if game_mgr else 5.0

	if p1_health_gauge:
		p1_health_gauge.set_player_info(p1_title, p1_col)
		p1_health_gauge.update_health(p1_handicap, 5.0)

	var p2_col: Color = p2_res.primary_color if p2_res else P2_MOTE_COLOR
	if p2_name_label:
		p2_name_label.text = p2_title.to_upper()
		p2_name_label.modulate = p2_col
	if p2_health_gauge:
		p2_health_gauge.set_player_info(p2_title, p2_col)
		p2_health_gauge.update_health(p2_handicap, 5.0)
	
	if p1_round_wins:
		var p1_w: int = game_mgr.p1_round_wins if game_mgr else 0
		p1_round_wins.set_wins(p1_w, false)
	if p2_round_wins:
		var p2_w: int = game_mgr.p2_round_wins if game_mgr else 0
		p2_round_wins.set_wins(p2_w, false)
	
	# Determine match stage and music:
	var chosen_stage_scene: PackedScene = null
	if _is_replay_mode and rep_mgr:
		_current_stage_id = rep_mgr.active_replay.get("stage_id", "bamboo_road")
		chosen_stage_scene = CharacterData.get_stage_scene_by_id(_current_stage_id)
		_chosen_bgm_path = rep_mgr.active_replay.get("bgm_path", "res://assets/music/02_spring_lane.ogg")
		if chosen_stage_scene == null:
			chosen_stage_scene = p1_res.get_home_stage_scene() if p1_res else null
	elif _is_networked and net_mgr and net_mgr.is_spectator and not net_mgr.spectator_stage_id.is_empty():
		_current_stage_id = net_mgr.spectator_stage_id
		chosen_stage_scene = CharacterData.get_stage_scene_by_id(_current_stage_id)
		_chosen_bgm_path = CharacterData.get_stage_bgm_by_id(_current_stage_id)
	else:
		var p1_stage_id: String = p1_res.get_home_stage_id() if p1_res else "bamboo_road"
		var p2_stage_id: String = p2_res.get_home_stage_id() if p2_res else "bamboo_road"
		var stage_character: CharacterData = p1_res
		if p1_stage_id == p2_stage_id:
			stage_character = p1_res
		else:
			# Both netplay clients run this independently, so the coin flip has to come from
			# the shared match seed - otherwise each side picks its own stage.
			var pick_p1_stage: bool = randf() < 0.5
			if _is_networked and net_mgr and net_mgr.match_seed > 0:
				var stage_rng := RandomNumberGenerator.new()
				stage_rng.seed = net_mgr.match_seed
				pick_p1_stage = stage_rng.randf() < 0.5
			stage_character = p1_res if pick_p1_stage else p2_res
		_current_stage_id = stage_character.get_home_stage_id() if stage_character else "bamboo_road"
		chosen_stage_scene = stage_character.get_home_stage_scene() if stage_character else null
		_chosen_bgm_path = stage_character.get_stage_bgm_path() if stage_character else "res://assets/music/02_spring_lane.ogg"
	
	print("[Arena] Selected Match Stage: '%s' | BGM: '%s' (P1: %s, P2: %s)" % [
		_current_stage_id,
		_chosen_bgm_path,
		p1_char,
		p2_char
	])

	# In local testing (F6), BOTH P1 (Arrows/WASD) and P2 (Numpad 4/6/8/2) are active!
	var p1_is_local: bool = true if not _is_networked else (local_player_num == 1)
	var p2_is_local: bool = true if not _is_networked else (local_player_num == 2)
	
	if p1_playfield:
		p1_playfield.setup(1, p1_is_local, p1_char, chosen_stage_scene, p1_handicap)
		p1_playfield.attack_sent.connect(_on_p1_attack_sent)
		p1_playfield.player_health_changed.connect(_on_p1_health_changed)
		p1_playfield.player_defeated.connect(_on_player_defeated)
		p1_playfield.player_charge_updated.connect(_on_p1_charge_updated)
		p1_playfield.player_ranks_updated.connect(_on_p1_ranks_updated)
		p1_playfield.spellcard_activated.connect(_on_spellcard_activated)
		p1_playfield.player_charge_attack_fired.connect(_on_player_charge_attack_fired)
		p1_playfield.player_shove_landed.connect(_on_p1_shove_landed)
	
	if p2_playfield:
		p2_playfield.setup(2, p2_is_local, p2_char, chosen_stage_scene, p2_handicap)
		p2_playfield.attack_sent.connect(_on_p2_attack_sent)
		p2_playfield.player_health_changed.connect(_on_p2_health_changed)
		p2_playfield.player_defeated.connect(_on_player_defeated)
		p2_playfield.player_charge_updated.connect(_on_p2_charge_updated)
		p2_playfield.player_ranks_updated.connect(_on_p2_ranks_updated)
		p2_playfield.spellcard_activated.connect(_on_spellcard_activated)
		p2_playfield.player_charge_attack_fired.connect(_on_player_charge_attack_fired)
		p2_playfield.player_shove_landed.connect(_on_p2_shove_landed)

	if p1_playfield and p2_playfield:
		p1_playfield.sibling_playfield = p2_playfield
		p2_playfield.sibling_playfield = p1_playfield

	if p1_spell_bar and p1_res:
		p1_spell_bar.apply_character_data(p1_res)
	if p2_spell_bar and p2_res:
		p2_spell_bar.apply_character_data(p2_res)
	
	if debug_menu:
		if OS.is_debug_build():
			debug_menu.setup(self, p1_playfield, p2_playfield, game_mgr)
		else:
			debug_menu.queue_free()
			debug_menu = null
	
	if profiler_overlay:
		if OS.is_debug_build():
			profiler_overlay.setup(self, p1_playfield, p2_playfield)
		else:
			profiler_overlay.queue_free()
			profiler_overlay = null
	if fullscreen_button:
		fullscreen_button.pressed.connect(toggle_fullscreen)
	if stats_toggle_button:
		if OS.is_debug_build():
			stats_toggle_button.pressed.connect(toggle_profiler_overlay)
		else:
			stats_toggle_button.visible = false
	
	_ensure_coordinators()
	
	# If solo match against CPU requested:
	if not _is_networked and game_mgr != null and game_mgr.p2_is_ai:
		_p2_ai_active = true
		if net_mgr and net_mgr.has_method("set_in_bot_match"):
			net_mgr.set_in_bot_match(true)
		if p2_playfield:
			p2_playfield.set_ai_mode(true, true)
	
	_update_ai_badges()
	
	var init_rnd: int = game_mgr.current_round if game_mgr else 1
	var initial_round_seed: int = _get_round_seed(init_rnd)
	if p1_playfield:
		p1_playfield.set_round_seed(initial_round_seed)
	if p2_playfield:
		p2_playfield.set_round_seed(initial_round_seed)

	var should_wait_for_round: bool = false
	if _is_replay_mode:
		if replay_controller:
			replay_controller.setup_replay_badge(p1_title, p2_title)
	elif _is_networked and net_mgr and net_mgr.is_spectator:
		_setup_spectator_badge(p1_title, p2_title)
		if net_mgr.spectator_is_round_active:
			should_wait_for_round = true
	else:
		if replay_controller:
			replay_controller.start_match_recording(
				p1_title, p2_title, p1_char, p2_char,
				_current_stage_id, _chosen_bgm_path,
				initial_round_seed
			)
	
	_round_elapsed_time = 0.0
	_next_lily_spawn_time = LILY_INITIAL_SPAWN_TIME
	
	if should_wait_for_round:
		var wait_rnd: int = (game_mgr.current_round + 1) if game_mgr else 2
		_enter_spectator_waiting_state(wait_rnd)
	else:
		if match_timer:
			match_timer.reset()
			match_timer.start()
		if _is_networked and local_player_num == 1 and net_mgr:
			net_mgr.broadcast_spectator_event({
				"type": "round_started",
				"current_round": game_mgr.current_round if game_mgr else 1
			})

	# Start match background music
	AudioService.play_music(_chosen_bgm_path)
	
	if _is_networked and local_player_num == 1 and net_mgr:
		_broadcast_host_match_init_to_spectators()

func _exit_tree() -> void:
	Engine.time_scale = 1.0
	AudioService.stop_music(0.5)
	var net_mgr = get_node_or_null("/root/NetworkManager")
	if net_mgr != null:
		if net_mgr.remote_player_updated.is_connected(_on_remote_player_updated):
			net_mgr.remote_player_updated.disconnect(_on_remote_player_updated)
		if net_mgr.remote_attack_sent.is_connected(_on_remote_attack_sent):
			net_mgr.remote_attack_sent.disconnect(_on_remote_attack_sent)
		if net_mgr.remote_charge_attack_fired.is_connected(_on_remote_charge_attack_fired):
			net_mgr.remote_charge_attack_fired.disconnect(_on_remote_charge_attack_fired)
		if net_mgr.remote_spellcard_activated.is_connected(_on_remote_spellcard_activated):
			net_mgr.remote_spellcard_activated.disconnect(_on_remote_spellcard_activated)
		if net_mgr.remote_health_updated.is_connected(_on_remote_health_updated):
			net_mgr.remote_health_updated.disconnect(_on_remote_health_updated)
		if net_mgr.has_signal("remote_hit_shockwave_spawned") and net_mgr.remote_hit_shockwave_spawned.is_connected(_on_remote_hit_shockwave_spawned):
			net_mgr.remote_hit_shockwave_spawned.disconnect(_on_remote_hit_shockwave_spawned)
		if net_mgr.remote_player_defeated.is_connected(_on_remote_player_defeated):
			net_mgr.remote_player_defeated.disconnect(_on_remote_player_defeated)
		if net_mgr.remote_round_transition.is_connected(_on_remote_round_transition):
			net_mgr.remote_round_transition.disconnect(_on_remote_round_transition)
		if net_mgr.has_signal("spectator_count_updated") and net_mgr.spectator_count_updated.is_connected(_on_host_spectator_count_updated):
			net_mgr.spectator_count_updated.disconnect(_on_host_spectator_count_updated)
		if net_mgr.has_signal("spectator_match_init") and net_mgr.spectator_match_init.is_connected(_on_spectator_match_init):
			net_mgr.spectator_match_init.disconnect(_on_spectator_match_init)
		if net_mgr.has_signal("spectator_player_updated") and net_mgr.spectator_player_updated.is_connected(_on_spectator_player_updated):
			net_mgr.spectator_player_updated.disconnect(_on_spectator_player_updated)
		if net_mgr.has_signal("spectator_attack_sent") and net_mgr.spectator_attack_sent.is_connected(_on_spectator_attack_sent):
			net_mgr.spectator_attack_sent.disconnect(_on_spectator_attack_sent)
		if net_mgr.has_signal("spectator_charge_attack_fired") and net_mgr.spectator_charge_attack_fired.is_connected(_on_spectator_charge_attack_fired):
			net_mgr.spectator_charge_attack_fired.disconnect(_on_spectator_charge_attack_fired)
		if net_mgr.has_signal("spectator_spellcard_activated") and net_mgr.spectator_spellcard_activated.is_connected(_on_spectator_spellcard_activated):
			net_mgr.spectator_spellcard_activated.disconnect(_on_spectator_spellcard_activated)
		if net_mgr.has_signal("spectator_health_updated") and net_mgr.spectator_health_updated.is_connected(_on_spectator_health_updated):
			net_mgr.spectator_health_updated.disconnect(_on_spectator_health_updated)
		if net_mgr.has_signal("spectator_hit_shockwave_spawned") and net_mgr.spectator_hit_shockwave_spawned.is_connected(_on_spectator_hit_shockwave_spawned):
			net_mgr.spectator_hit_shockwave_spawned.disconnect(_on_spectator_hit_shockwave_spawned)
		if net_mgr.has_signal("spectator_player_defeated") and net_mgr.spectator_player_defeated.is_connected(_on_spectator_player_defeated):
			net_mgr.spectator_player_defeated.disconnect(_on_spectator_player_defeated)
		if net_mgr.has_signal("spectator_round_transition") and net_mgr.spectator_round_transition.is_connected(_on_spectator_round_transition):
			net_mgr.spectator_round_transition.disconnect(_on_spectator_round_transition)
		if net_mgr.has_signal("spectator_round_started") and net_mgr.spectator_round_started.is_connected(_on_spectator_round_started):
			net_mgr.spectator_round_started.disconnect(_on_spectator_round_started)
		if net_mgr.has_signal("spectator_match_ended") and net_mgr.spectator_match_ended.is_connected(_on_spectator_match_ended):
			net_mgr.spectator_match_ended.disconnect(_on_spectator_match_ended)
		if net_mgr.has_signal("opponent_quit_match") and net_mgr.opponent_quit_match.is_connected(_on_opponent_quit_match):
			net_mgr.opponent_quit_match.disconnect(_on_opponent_quit_match)
		if net_mgr.has_signal("connection_status_changed") and net_mgr.connection_status_changed.is_connected(_on_connection_status_changed):
			net_mgr.connection_status_changed.disconnect(_on_connection_status_changed)

func _physics_process(delta: float) -> void:
	if _is_replay_mode:
		if replay_controller:
			replay_controller.process_frame(delta, _match_total_elapsed)
		return

	if not _round_intermission and not _match_over and not _action_stop_active:
		_match_total_elapsed += delta
		if replay_controller:
			replay_controller.process_frame(delta, _match_total_elapsed)

		_round_elapsed_time += delta
		if match_timer:
			match_timer.set_time(_round_elapsed_time)
		
		if _round_elapsed_time >= _next_lily_spawn_time:
			_next_lily_spawn_time += LILY_REPEAT_INTERVAL
			spawn_lily_white_both_fields()
			if lily_white_spawning_enabled:
				spawn_lily_white_both_fields()
		
		if not _sudden_death_active and _round_elapsed_time >= SUDDEN_DEATH_ROUND_TIME:
			_sudden_death_active = true
			if match_timer:
				match_timer.set_sudden_death(true)
			AudioService.play_sudden_death_alert()
			if p1_playfield and p1_playfield.player:
				p1_playfield.player.is_sudden_death_active = true
			if p2_playfield and p2_playfield.player:
				p2_playfield.player.is_sudden_death_active = true
	
	if not _is_networked or _action_stop_active:
		return

	# Check for communication timeout (lapse in peer updates)
	if not _match_over and not _is_forfeiting and not _round_intermission:
		var current_sec := Time.get_ticks_msec() / 1000.0
		if _round_elapsed_time > 5.0 and (current_sec - _last_remote_packet_time) > REMOTE_PACKET_TIMEOUT_SEC:
			_trigger_match_exit("Connection to opponent lost (timeout). Returning to Main Menu...", false)
			return

	_net_broadcast_timer += delta
	if _net_broadcast_timer >= NET_BROADCAST_INTERVAL:
		_net_broadcast_timer -= NET_BROADCAST_INTERVAL
		_broadcast_local_player_state()

func spawn_lily_white_both_fields(forced: bool = false) -> void:
	if not forced and not lily_white_spawning_enabled:
		return
	if p1_playfield:
		p1_playfield.spawn_lily_white(forced)
	if p2_playfield:
		p2_playfield.spawn_lily_white(forced)

func _broadcast_local_player_state() -> void:
	var net_mgr = get_node_or_null("/root/NetworkManager")
	if net_mgr == null or not net_mgr.is_network_active() or net_mgr.is_spectator:
		return
	var local_field: Playfield = p1_playfield if local_player_num == 1 else p2_playfield
	if local_field and local_field.player and not local_field.player.is_dead:
		var p: Player = local_field.player
		net_mgr.send_player_state(
			p.position,
			p._anim_row,
			p.is_focusing,
			p.is_charging,
			p.is_shooting,
			p.active_charge,
			p.passive_charge
		)
		if local_player_num == 1 and net_mgr.spectator_count > 0:
			net_mgr.broadcast_spectator_event({
				"type": "player_state",
				"p_num": 1,
				"x": p.position.x,
				"y": p.position.y,
				"anim_row": p._anim_row,
				"is_focus": p.is_focusing,
				"is_charging": p.is_charging,
				"is_shooting": p.is_shooting,
				"active_charge": p.active_charge,
				"passive_charge": p.passive_charge
			})

func _mark_remote_packet_received() -> void:
	_last_remote_packet_time = Time.get_ticks_msec() / 1000.0

func _on_remote_player_updated(pos: Vector2, anim_row: int, is_focus: bool, is_charging: bool, is_shooting: bool, active_chg: float, passive_chg: float) -> void:
	_mark_remote_packet_received()
	var remote_field: Playfield = p2_playfield if local_player_num == 1 else p1_playfield
	if remote_field and remote_field.player:
		remote_field.player.apply_remote_state(pos, anim_row, is_focus, is_charging, is_shooting, active_chg, passive_chg)
	if local_player_num == 1:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr and net_mgr.spectator_count > 0:
			net_mgr.broadcast_spectator_event({
				"type": "player_state",
				"p_num": 2,
				"x": pos.x,
				"y": pos.y,
				"anim_row": anim_row,
				"is_focus": is_focus,
				"is_charging": is_charging,
				"is_shooting": is_shooting,
				"active_charge": active_chg,
				"passive_charge": passive_chg
			})

func _handle_escape_press() -> void:
	if _is_replay_mode:
		Engine.time_scale = 1.0
		if replay_controller:
			replay_controller.apply_replay_pause(false)
		var rep_mgr = get_node_or_null("/root/ReplayManager")
		if rep_mgr:
			rep_mgr.stop_playback(get_tree())
		else:
			get_tree().change_scene_to_file("res://scenes/main_menu/main_menu.tscn")
		return
	if _is_forfeiting:
		return
	var net_mgr = get_node_or_null("/root/NetworkManager")
	if net_mgr and net_mgr.is_spectator:
		_trigger_match_exit("Leaving spectator mode...", true)
		return
	var now := Time.get_ticks_msec()
	if _escape_press_count > 0 and (now - _last_escape_time_msec) <= ESCAPE_DOUBLE_TAP_WINDOW_MSEC:
		_escape_press_count = 0
		if _escape_timer_tween:
			_escape_timer_tween.kill()
		_trigger_match_exit("Returning to Main Menu...", true)
	else:
		_escape_press_count = 1
		_last_escape_time_msec = now
		AudioService.play_select()
		if forfeit_banner and forfeit_label:
			forfeit_label.text = "Press Esc again within 3s to return to Main Menu"
			forfeit_label.modulate = Color(1.0, 0.85, 0.3, 1.0)
			forfeit_banner.visible = true
		if _escape_timer_tween:
			_escape_timer_tween.kill()
		_escape_timer_tween = create_tween()
		_escape_timer_tween.tween_interval(3.0)
		_escape_timer_tween.tween_callback(func():
			if not _is_forfeiting:
				_escape_press_count = 0
				if forfeit_banner:
					forfeit_banner.visible = false
		)

func _trigger_match_exit(message: String, is_local_initiated: bool) -> void:
	if _is_forfeiting:
		return
	_is_forfeiting = true
	var tree := get_tree() if is_inside_tree() else null
	if tree:
		tree.paused = false
	
	AudioService.stop_music(0.4)
	if is_local_initiated:
		AudioService.play_cancel()
	
	if forfeit_banner and forfeit_label:
		forfeit_label.text = message
		if message.begins_with("Match concluded"):
			forfeit_label.modulate = Color(0.75, 0.9, 1.0, 1.0)
		else:
			forfeit_label.modulate = Color(1.0, 0.45, 0.45, 1.0)
		forfeit_banner.visible = true
	
	# Pause player physics processing
	if p1_playfield and p1_playfield.player:
		p1_playfield.player.set_physics_process(false)
	if p2_playfield and p2_playfield.player:
		p2_playfield.player.set_physics_process(false)
	
	var net_mgr: Node = get_node_or_null("/root/NetworkManager") if is_inside_tree() else null
	if is_local_initiated and net_mgr and not net_mgr.is_spectator:
		net_mgr.send_match_quit()
	
	var wait_time: float = 0.2 if is_local_initiated else 1.5
	if tree:
		tree.create_timer(wait_time).timeout.connect(func():
			if net_mgr:
				net_mgr.cancel_matchmaking()
			var t := get_tree()
			if t:
				t.change_scene_to_file("res://scenes/main_menu/main_menu.tscn")
		)

func _on_opponent_quit_match() -> void:
	_trigger_match_exit("Opponent left the match. Returning to Main Menu...", false)

func _on_connection_status_changed(status: String) -> void:
	var lower := status.to_lower()
	if "left" in lower or "disconnected" in lower:
		_trigger_match_exit("Opponent disconnected. Returning to Main Menu...", false)

func _unhandled_input(event: InputEvent) -> void:
	if _is_replay_mode:
		if replay_controller and replay_controller.handle_replay_input(event):
			return
		return

	if event is InputEventKey and event.pressed and not event.echo and (event.keycode == KEY_ESCAPE or event.physical_keycode == KEY_ESCAPE):
		_handle_escape_press()
		var vp := get_viewport()
		if vp:
			vp.set_input_as_handled()
		return
	elif event is InputEventKey and event.pressed and not event.echo:
		if (event.keycode == KEY_BACKSPACE or event.physical_keycode == KEY_BACKSPACE) and OS.is_debug_build():
			toggle_debug_menu()
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()
		elif (event.keycode == KEY_F10 or event.physical_keycode == KEY_F10) and OS.is_debug_build():
			toggle_profiler_overlay()
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()
		elif (event.keycode == KEY_F9 or event.physical_keycode == KEY_F9) and OS.is_debug_build():
			copy_profiler_snapshot()
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()
		elif event.keycode == KEY_F11 or event.physical_keycode == KEY_F11:
			toggle_fullscreen()
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()
		elif (event.keycode == KEY_F1 or event.physical_keycode == KEY_F1) and not _is_networked:
			toggle_p1_ai()
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()
		elif (event.keycode == KEY_F2 or event.physical_keycode == KEY_F2) and not _is_networked:
			toggle_p2_ai()
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()
		elif (event.keycode == KEY_F3 or event.physical_keycode == KEY_F3) and OS.is_debug_build():
			if p1_playfield:
				_on_spellcard_activated(1, 2, p1_playfield.current_rank_lv2_3)
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()
		elif (event.keycode == KEY_F4 or event.physical_keycode == KEY_F4) and OS.is_debug_build():
			if p1_playfield:
				_on_spellcard_activated(1, 3, p1_playfield.current_rank_lv2_3)
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()
		elif (event.keycode == KEY_F7 or event.physical_keycode == KEY_F7) and OS.is_debug_build():
			if p1_playfield:
				_on_spellcard_activated(1, 4, p1_playfield.current_rank_lv4)
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()
		elif (event.keycode == KEY_F8 or event.physical_keycode == KEY_F8) and OS.is_debug_build():
			spawn_lily_white_both_fields()
			spawn_lily_white_both_fields(true)
			var vp := get_viewport()
			if vp:
				vp.set_input_as_handled()

func toggle_fullscreen() -> void:
	var mode := DisplayServer.window_get_mode()
	if mode == DisplayServer.WINDOW_MODE_FULLSCREEN or mode == DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	else:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)

func toggle_profiler_overlay() -> void:
	if not OS.is_debug_build():
		return
	if profiler_overlay:
		profiler_overlay.toggle()

func copy_profiler_snapshot() -> void:
	if not OS.is_debug_build():
		return
	if profiler_overlay:
		profiler_overlay.copy_snapshot_to_clipboard()

func toggle_debug_menu() -> void:
	if not OS.is_debug_build():
		return
	if debug_menu:
		debug_menu.toggle()

func toggle_p1_ai() -> void:
	if _is_networked and local_player_num != 1:
		return
	_p1_ai_active = !_p1_ai_active
	_p1_ai_dodge_only = false
	if p1_playfield:
		p1_playfield.set_ai_mode(_p1_ai_active, true)
	_update_ai_badges()
	if debug_menu and debug_menu.is_open:
		debug_menu._refresh_all()

func toggle_p2_ai() -> void:
	if _is_networked and local_player_num != 2:
		return
	_p2_ai_active = !_p2_ai_active
	_p2_ai_dodge_only = false
	if p2_playfield:
		p2_playfield.set_ai_mode(_p2_ai_active, true)
	_update_ai_badges()
	if debug_menu and debug_menu.is_open:
		debug_menu._refresh_all()

func _update_ai_badges() -> void:
	# AI badges removed from arena HUD; AI status is preserved via internal state
	pass

func _on_p1_health_changed(_p_num: int, current: float, max_val: float) -> void:
	if _is_networked and local_player_num != 1:
		return # P1 health on Guest is driven by remote updates
	_record_replay_event({
		"type": "health",
		"p": 1,
		"hp": snappedf(current, 0.1),
		"max_hp": snappedf(max_val, 0.1)
	})
	if p1_health_gauge:
		p1_health_gauge.update_health(current, max_val)
	if _is_networked and local_player_num == 1:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr:
			net_mgr.send_health(current, max_val)
			if net_mgr.spectator_count > 0:
				net_mgr.broadcast_spectator_event({
					"type": "health",
					"p_num": 1,
					"hp": current,
					"max_hp": max_val
				})

func _on_p2_health_changed(_p_num: int, current: float, max_val: float) -> void:
	if _is_networked and local_player_num != 2:
		return # P2 health on Host is driven by remote updates
	_record_replay_event({
		"type": "health",
		"p": 2,
		"hp": snappedf(current, 0.1),
		"max_hp": snappedf(max_val, 0.1)
	})
	if p2_health_gauge:
		p2_health_gauge.update_health(current, max_val)
	if _is_networked and local_player_num == 2:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr:
			net_mgr.send_health(current, max_val)

func _on_remote_health_updated(current: float, max_val: float) -> void:
	_mark_remote_packet_received()
	var remote_p_num: int = 2 if local_player_num == 1 else 1
	_record_replay_event({
		"type": "health",
		"p": remote_p_num,
		"hp": snappedf(current, 0.1),
		"max_hp": snappedf(max_val, 0.1)
	})
	if local_player_num == 1:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr and net_mgr.spectator_count > 0:
			net_mgr.broadcast_spectator_event({
				"type": "health",
				"p_num": 2,
				"hp": current,
				"max_hp": max_val
			})
	var remote_gauge = p2_health_gauge if local_player_num == 1 else p1_health_gauge
	if remote_gauge:
		remote_gauge.update_health(current, max_val)
	var remote_field: Playfield = p2_playfield if local_player_num == 1 else p1_playfield
	if remote_field and remote_field.player:
		remote_field.player.apply_remote_damage_visuals(current)

func _on_p1_shove_landed(_player_num: int, landing_pos: Vector2) -> void:
	_record_replay_event({
		"type": "hit_shockwave",
		"p": 1,
		"x": snappedf(landing_pos.x, 0.1),
		"y": snappedf(landing_pos.y, 0.1)
	})
	if _is_networked and local_player_num == 1:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr:
			net_mgr.send_hit_shockwave(landing_pos)
			if net_mgr.spectator_count > 0:
				net_mgr.broadcast_spectator_event({
					"type": "hit_shockwave",
					"p_num": 1,
					"x": landing_pos.x,
					"y": landing_pos.y
				})

func _on_p2_shove_landed(_player_num: int, landing_pos: Vector2) -> void:
	_record_replay_event({
		"type": "hit_shockwave",
		"p": 2,
		"x": snappedf(landing_pos.x, 0.1),
		"y": snappedf(landing_pos.y, 0.1)
	})
	if _is_networked and local_player_num == 2:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr:
			net_mgr.send_hit_shockwave(landing_pos)

func _on_remote_hit_shockwave_spawned(landing_pos: Vector2) -> void:
	_mark_remote_packet_received()
	var remote_p_num: int = 2 if local_player_num == 1 else 1
	_record_replay_event({
		"type": "hit_shockwave",
		"p": remote_p_num,
		"x": snappedf(landing_pos.x, 0.1),
		"y": snappedf(landing_pos.y, 0.1)
	})
	var remote_field: Playfield = p2_playfield if local_player_num == 1 else p1_playfield
	if remote_field:
		remote_field.spawn_hit_shockwave(landing_pos)
	if local_player_num == 1:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr and net_mgr.spectator_count > 0:
			net_mgr.broadcast_spectator_event({
				"type": "hit_shockwave",
				"p_num": 2,
				"x": landing_pos.x,
				"y": landing_pos.y
			})

func _on_p1_charge_updated(_player_num: int, active: float, passive: float, max_segments: int) -> void:
	if p1_spell_bar:
		p1_spell_bar.set_passive_charge(passive, max_segments)
		p1_spell_bar.set_active_charge(active, max_segments)

func _on_p2_charge_updated(_player_num: int, active: float, passive: float, max_segments: int) -> void:
	if p2_spell_bar:
		p2_spell_bar.set_passive_charge(passive, max_segments)
		p2_spell_bar.set_active_charge(active, max_segments)

func _on_p1_ranks_updated(_player_num: int, rank_lv2_3: int, rank_lv4: int) -> void:
	if p1_spell_bar:
		p1_spell_bar.set_ranks(rank_lv2_3, rank_lv4)

func _on_p2_ranks_updated(_player_num: int, rank_lv2_3: int, rank_lv4: int) -> void:
	if p2_spell_bar:
		p2_spell_bar.set_ranks(rank_lv2_3, rank_lv4)

func _on_player_charge_attack_fired(player_num: int, level: int, attack_name: String) -> void:
	var sender_char: String = p1_playfield.character_id if player_num == 1 else p2_playfield.character_id
	_record_replay_event({
		"type": "charge_attack",
		"p": player_num,
		"level": level,
		"attack_name": attack_name,
		"sender_char": sender_char
	})
	if _is_networked and player_num == local_player_num:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr:
			net_mgr.send_charge_attack(level, attack_name, sender_char)
			if local_player_num == 1 and net_mgr.spectator_count > 0:
				net_mgr.broadcast_spectator_event({
					"type": "charge_attack",
					"p_num": 1,
					"level": level,
					"attack_name": attack_name,
					"sender_char": sender_char
				})

func _on_remote_charge_attack_fired(level: int, attack_name: String, sender_char: String) -> void:
	_mark_remote_packet_received()
	var remote_p_num: int = 2 if local_player_num == 1 else 1
	_record_replay_event({
		"type": "charge_attack",
		"p": remote_p_num,
		"level": level,
		"attack_name": attack_name,
		"sender_char": sender_char
	})
	if local_player_num == 1:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr and net_mgr.spectator_count > 0:
			net_mgr.broadcast_spectator_event({
				"type": "charge_attack",
				"p_num": 2,
				"level": level,
				"attack_name": attack_name,
				"sender_char": sender_char
			})
	var remote_pf: Playfield = p2_playfield if local_player_num == 1 else p1_playfield
	var remote_bar: SpellBar = p2_spell_bar if local_player_num == 1 else p1_spell_bar
	if remote_bar:
		remote_bar.play_release_flash()
	if remote_pf:
		remote_pf.execute_charge_attack(level)

func _on_spellcard_activated(player_num: int, level: int, rank: int) -> void:
	if _round_intermission or _match_over:
		return
	var bar: SpellBar = p1_spell_bar if player_num == 1 else p2_spell_bar
	if bar:
		bar.play_release_flash()
	
	var caster_field: Playfield = p1_playfield if player_num == 1 else p2_playfield
	var target_field: Playfield = p2_playfield if player_num == 1 else p1_playfield
	var sender_char: String = p1_playfield.character_id if player_num == 1 else p2_playfield.character_id
	
	_record_replay_event({
		"type": "spellcard",
		"p": player_num,
		"level": level,
		"rank": rank,
		"sender_char": sender_char
	})
	
	if _is_networked:
		if player_num == local_player_num:
			var net_mgr = get_node_or_null("/root/NetworkManager")
			if net_mgr:
				net_mgr.send_spellcard(level, rank, sender_char)
				if local_player_num == 1 and net_mgr.spectator_count > 0:
					net_mgr.broadcast_spectator_event({
						"type": "spellcard",
						"p_num": 1,
						"level": level,
						"rank": rank,
						"sender_char": sender_char
					})
			_broadcast_local_player_state()
			_execute_action_stop(caster_field, target_field, level, rank, sender_char)
	else:
		_execute_action_stop(caster_field, target_field, level, rank, sender_char)

func _on_remote_spellcard_activated(level: int, rank: int, sender_char: String) -> void:
	_mark_remote_packet_received()
	if _round_intermission or _match_over:
		return
	var remote_p_num: int = 2 if local_player_num == 1 else 1
	_record_replay_event({
		"type": "spellcard",
		"p": remote_p_num,
		"level": level,
		"rank": rank,
		"sender_char": sender_char
	})
	if local_player_num == 1:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr and net_mgr.spectator_count > 0:
			net_mgr.broadcast_spectator_event({
				"type": "spellcard",
				"p_num": 2,
				"level": level,
				"rank": rank,
				"sender_char": sender_char
			})
	var local_target_field: Playfield = p1_playfield if local_player_num == 1 else p2_playfield
	var remote_caster_field: Playfield = p2_playfield if local_player_num == 1 else p1_playfield
	var remote_bar: SpellBar = p2_spell_bar if local_player_num == 1 else p1_spell_bar
	if remote_bar:
		remote_bar.play_release_flash()
	_execute_action_stop(remote_caster_field, local_target_field, level, rank, sender_char)

func _execute_action_stop(caster_field: Playfield, target_field: Playfield, level: int, rank: int, sender_char: String) -> void:
	if _round_intermission or _match_over:
		return
	_ensure_action_stop_coordinator()
	action_stop_coordinator.execute_action_stop(caster_field, target_field, level, rank, sender_char)

func _conclude_action_stop() -> void:
	if action_stop_coordinator:
		action_stop_coordinator.conclude_action_stop()
	if is_inside_tree():
		get_tree().paused = false

func _trigger_spellcard_effects(target_field: Playfield, level: int, rank: int, sender_char: String) -> void:
	if action_stop_coordinator:
		action_stop_coordinator.trigger_spellcard_effects(target_field, level, rank, sender_char)

func _on_player_defeated(loser_num: int) -> void:
	if _match_over or _round_intermission:
		return
	
	if _is_networked:
		# Strictly client-authoritative: a client can ONLY report their OWN local defeat!
		# Remote puppet defeat events must never trigger round resolution!
		if loser_num != local_player_num:
			return
		
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr:
			net_mgr.send_player_defeated(loser_num)
		
		# Host arbitrates the round end and notifies client if host died locally
		if local_player_num == 1:
			_execute_round_resolution(loser_num, true)
	else:
		_execute_round_resolution(loser_num, false)

func _on_remote_player_defeated(loser_num: int) -> void:
	_mark_remote_packet_received()
	if _match_over or _round_intermission:
		return
	
	# Trigger defeat visuals on the remote player's playfield
	var remote_field: Playfield = p1_playfield if loser_num == 1 else p2_playfield
	if remote_field and remote_field.player:
		remote_field.player.current_health = 0.0
		remote_field.player._on_defeat()
	
	# Host processes remote client's defeat and arbitrates round end
	if local_player_num == 1:
		_execute_round_resolution(loser_num, true)

func _execute_round_resolution(loser_num: int, broadcast_net: bool) -> void:
	if _action_stop_active:
		_conclude_action_stop()
	_round_intermission = true
	if p1_playfield:
		p1_playfield.set_round_over(true)
	if p2_playfield:
		p2_playfield.set_round_over(true)
	var winner_num: int = 2 if loser_num == 1 else 1
	var game_mgr = get_node_or_null("/root/GameManager")
	var winner_wins: int = 1
	var current_rnd: int = 1
	if game_mgr != null:
		current_rnd = game_mgr.current_round
		winner_wins = game_mgr.add_round_win(winner_num)
	var p1_w: int = game_mgr.p1_round_wins if game_mgr else (winner_wins if winner_num == 1 else 0)
	var p2_w: int = game_mgr.p2_round_wins if game_mgr else (winner_wins if winner_num == 2 else 0)
	var is_final: bool = (winner_wins >= 2)
	var next_rnd: int = current_rnd + 1
	if game_mgr != null:
		game_mgr.current_round = next_rnd
	
	_record_replay_event({
		"type": "player_defeated",
		"loser_num": loser_num
	})
	_record_replay_event({
		"type": "round_transition",
		"winner_num": winner_num,
		"p1_wins": p1_w,
		"p2_wins": p2_w,
		"next_round": next_rnd,
		"is_final": is_final
	})
	
	if broadcast_net:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr:
			net_mgr.send_round_transition(winner_num, p1_w, p2_w, next_rnd, is_final)
			if net_mgr.spectator_count > 0:
				net_mgr.broadcast_spectator_event({
					"type": "player_defeated",
					"loser_num": loser_num
				})
				net_mgr.broadcast_spectator_event({
					"type": "round_transition",
					"winner_num": winner_num,
					"p1_wins": p1_w,
					"p2_wins": p2_w,
					"next_round": next_rnd,
					"is_final": is_final
				})
	
	_play_round_defeat_sequence(winner_num, loser_num, winner_wins, p1_w, p2_w, next_rnd, is_final)

func _on_remote_round_transition(winner_num: int, p1_wins: int, p2_wins: int, next_round: int, is_final_win: bool) -> void:
	_mark_remote_packet_received()
	if _match_over:
		return
	if _action_stop_active:
		_conclude_action_stop()
	_round_intermission = true
	if p1_playfield:
		p1_playfield.set_round_over(true)
	if p2_playfield:
		p2_playfield.set_round_over(true)
	var loser_num: int = 2 if winner_num == 1 else 1
	var game_mgr = get_node_or_null("/root/GameManager")
	if game_mgr != null:
		game_mgr.p1_round_wins = p1_wins
		game_mgr.p2_round_wins = p2_wins
		game_mgr.current_round = next_round
	var winner_wins: int = p1_wins if winner_num == 1 else p2_wins
	_play_round_defeat_sequence(winner_num, loser_num, winner_wins, p1_wins, p2_wins, next_round, is_final_win)

func _play_round_defeat_sequence(winner_num: int, loser_num: int, winner_wins: int, p1_w: int, p2_w: int, next_round: int, is_final_match_win: bool) -> void:
	if presentation:
		await presentation.play_round_defeat_sequence(winner_num, loser_num, winner_wins, p1_w, p2_w, next_round, is_final_match_win)

## Shakes the target playfield with decaying trauma and snaps cleanly back to Vector2.ZERO
func _play_screen_shake(target_playfield: Playfield, duration: float = 0.35, intensity: float = 10.0) -> void:
	if presentation:
		presentation.play_screen_shake(target_playfield, duration, intensity)

func _reset_fields_for_next_round(target_round: int = -1) -> void:
	if _action_stop_active:
		_conclude_action_stop()
	_round_elapsed_time = 0.0
	_next_lily_spawn_time = LILY_INITIAL_SPAWN_TIME
	_sudden_death_active = false
	if match_timer:
		match_timer.reset()
	
	# Purge all inter-field attack motes
	if mote_dispatcher:
		mote_dispatcher.clear_all_motes()
	elif effects_overlay:
		for child in effects_overlay.get_children():
			child.queue_free()
	
	var game_mgr = get_node_or_null("/root/GameManager")
	var current_rnd: int = target_round if target_round > 0 else (game_mgr.current_round if game_mgr else 1)
	if game_mgr and target_round > 0:
		game_mgr.current_round = target_round
	var round_seed: int = _get_round_seed(current_rnd)
	_record_replay_event({
		"type": "round_started",
		"round_num": current_rnd
	})
	print("[Arena] Resetting fields for Round %d (Seed: %d)" % [current_rnd, round_seed])
	if p1_playfield:
		p1_playfield.set_round_over(false)
		p1_playfield.reset_for_new_round()
		p1_playfield.set_round_seed(round_seed)
		p1_playfield.set_ai_mode(_p1_ai_active)
	if p2_playfield:
		p2_playfield.set_round_over(false)
		p2_playfield.reset_for_new_round()
		p2_playfield.set_round_seed(round_seed)
		p2_playfield.set_ai_mode(_p2_ai_active)
	if p1_health_gauge:
		var p1_cur: float = p1_playfield.player.current_health if (p1_playfield and p1_playfield.player) else 5.0
		var p1_max: float = p1_playfield.player.max_health if (p1_playfield and p1_playfield.player) else 5.0
		p1_health_gauge.update_health(p1_cur, p1_max)
	if p2_health_gauge:
		var p2_cur: float = p2_playfield.player.current_health if (p2_playfield and p2_playfield.player) else 5.0
		var p2_max: float = p2_playfield.player.max_health if (p2_playfield and p2_playfield.player) else 5.0
		p2_health_gauge.update_health(p2_cur, p2_max)

func _on_p1_attack_sent(death_pos: Vector2, pellet_count: int, _combo_count: int, has_big_pellet: bool, has_spirit: bool, has_extra_attack: bool = false, bounce_count: int = 0) -> void:
	var sender_char: String = p1_playfield.character_id if p1_playfield else "reimu"
	var sender_rank: int = p1_playfield.current_overall_rank if p1_playfield else 1
	# Rolled once, here, on the client that owns the attack, then sent along with it -
	# so both clients put the same Extra Attack in the same place.
	var extra_targets: PackedVector2Array = MoteDispatcher.roll_extra_attack_targets(sender_char, sender_rank) if has_extra_attack else PackedVector2Array()
	if _is_networked:
		if local_player_num == 1:
			var net_mgr = get_node_or_null("/root/NetworkManager")
			if net_mgr:
				net_mgr.send_attack(death_pos, pellet_count, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, extra_targets, sender_rank)
				if net_mgr.spectator_count > 0:
					net_mgr.broadcast_spectator_event({
						"type": "attack",
						"p_num": 1,
						"x": death_pos.x,
						"y": death_pos.y,
						"pellet_count": pellet_count,
						"has_big": has_big_pellet,
						"has_spirit": has_spirit,
						"has_extra": has_extra_attack,
						"sender_char": sender_char,
						"bounce_count": bounce_count,
						"extra_targets": MoteDispatcher.targets_to_flat(extra_targets),
						"rank": sender_rank
					})
			_send_attack_between_fields(p1_viewport_container, p2_viewport_container, p2_playfield, death_pos, pellet_count, P1_MOTE_COLOR, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, sender_rank, extra_targets)
	else:
		_send_attack_between_fields(p1_viewport_container, p2_viewport_container, p2_playfield, death_pos, pellet_count, P1_MOTE_COLOR, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, sender_rank, extra_targets)

func _on_p2_attack_sent(death_pos: Vector2, pellet_count: int, _combo_count: int, has_big_pellet: bool, has_spirit: bool, has_extra_attack: bool = false, bounce_count: int = 0) -> void:
	var sender_char: String = p2_playfield.character_id if p2_playfield else "reimu"
	var sender_rank: int = p2_playfield.current_overall_rank if p2_playfield else 1
	var extra_targets: PackedVector2Array = MoteDispatcher.roll_extra_attack_targets(sender_char, sender_rank) if has_extra_attack else PackedVector2Array()
	if _is_networked:
		if local_player_num == 2:
			var net_mgr = get_node_or_null("/root/NetworkManager")
			if net_mgr:
				net_mgr.send_attack(death_pos, pellet_count, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, extra_targets, sender_rank)
			_send_attack_between_fields(p2_viewport_container, p1_viewport_container, p1_playfield, death_pos, pellet_count, P2_MOTE_COLOR, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, sender_rank, extra_targets)
	else:
		_send_attack_between_fields(p2_viewport_container, p1_viewport_container, p1_playfield, death_pos, pellet_count, P2_MOTE_COLOR, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, sender_rank, extra_targets)

func _on_remote_attack_sent(death_pos: Vector2, pellet_count: int, has_big_pellet: bool, has_spirit: bool, has_extra_attack: bool, sender_char: String, bounce_count: int = 0, extra_targets: PackedVector2Array = PackedVector2Array(), sender_rank: int = 1) -> void:
	_mark_remote_packet_received()
	if local_player_num == 1:
		var net_mgr = get_node_or_null("/root/NetworkManager")
		if net_mgr and net_mgr.spectator_count > 0:
			net_mgr.broadcast_spectator_event({
				"type": "attack",
				"p_num": 2,
				"x": death_pos.x,
				"y": death_pos.y,
				"pellet_count": pellet_count,
				"has_big": has_big_pellet,
				"has_spirit": has_spirit,
				"has_extra": has_extra_attack,
				"sender_char": sender_char,
				"bounce_count": bounce_count,
				"extra_targets": MoteDispatcher.targets_to_flat(extra_targets),
				"rank": sender_rank
			})
		_send_attack_between_fields(p2_viewport_container, p1_viewport_container, p1_playfield, death_pos, pellet_count, P2_MOTE_COLOR, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, sender_rank, extra_targets)
	else:
		_send_attack_between_fields(p1_viewport_container, p2_viewport_container, p2_playfield, death_pos, pellet_count, P1_MOTE_COLOR, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, sender_rank, extra_targets)

func _send_attack_between_fields(from_container: SubViewportContainer, to_container: SubViewportContainer, target_playfield: Playfield, local_death_pos: Vector2, pellet_count: int, mote_color: Color, has_big_pellet: bool = false, has_spirit: bool = false, has_extra_attack: bool = false, sender_char: String = "reimu", bounce_count: int = 0, rank: int = 1, extra_targets: PackedVector2Array = PackedVector2Array()) -> void:
	_record_replay_event({
		"type": "attack",
		"p": (1 if from_container == p1_viewport_container else 2),
		"x": snappedf(local_death_pos.x, 0.1),
		"y": snappedf(local_death_pos.y, 0.1),
		"pellet_count": pellet_count,
		"has_big": has_big_pellet,
		"has_spirit": has_spirit,
		"has_extra": has_extra_attack,
		"sender_char": sender_char,
		"bounce_count": bounce_count,
		"extra_targets": MoteDispatcher.targets_to_flat(extra_targets),
		"rank": rank
	})
	_ensure_mote_dispatcher()
	var source_playfield: Playfield = p1_playfield if from_container == p1_viewport_container else p2_playfield
	mote_dispatcher.send_attack_between_fields(from_container, to_container, target_playfield, local_death_pos, pellet_count, mote_color, has_big_pellet, has_spirit, has_extra_attack, sender_char, bounce_count, rank, source_playfield, extra_targets)

func _spawn_mote(start_pt: Vector2, target_pt: Vector2, target_local: Vector2, target_playfield: Playfield, col: Color, payload: TravelMote.MotePayload = TravelMote.MotePayload.PELLET, sender_char: String = "reimu", bounce_count: int = 0) -> void:
	_ensure_mote_dispatcher()
	mote_dispatcher.spawn_mote(start_pt, target_pt, target_local, target_playfield, col, payload, sender_char, bounce_count)

# --- Spectator Mode Handlers & Host Relaying ---

func _on_host_spectator_count_updated(count: int) -> void:
	if spectator_coordinator: spectator_coordinator.on_host_spectator_count_updated(count)

func _enter_spectator_waiting_state(next_round_num: int) -> void:
	if spectator_coordinator: spectator_coordinator.enter_spectator_waiting_state(next_round_num)

func _exit_spectator_waiting_state() -> void:
	if spectator_coordinator: spectator_coordinator.exit_spectator_waiting_state()

func _broadcast_host_match_init_to_spectators() -> void:
	if spectator_coordinator: spectator_coordinator.broadcast_host_match_init_to_spectators()

func _on_spectator_match_init(p1_c: String, p2_c: String, stage_id: String, p1_w: int, p2_w: int, current_rnd: int, is_active: bool = false) -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_match_init(p1_c, p2_c, stage_id, p1_w, p2_w, current_rnd, is_active)

func _on_spectator_player_updated(p_num: int, pos: Vector2, anim_row: int, is_focus: bool, is_charging: bool, is_shooting: bool, active_chg: float, passive_chg: float) -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_player_updated(p_num, pos, anim_row, is_focus, is_charging, is_shooting, active_chg, passive_chg)

func _on_spectator_attack_sent(p_num: int, death_pos: Vector2, pellet_count: int, has_big: bool, has_spirit: bool, has_extra: bool, sender_char: String, bounce_count: int, extra_targets: PackedVector2Array = PackedVector2Array(), sender_rank: int = 1) -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_attack_sent(p_num, death_pos, pellet_count, has_big, has_spirit, has_extra, sender_char, bounce_count, extra_targets, sender_rank)

func _on_spectator_charge_attack_fired(p_num: int, level: int, attack_name: String, sender_char: String) -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_charge_attack_fired(p_num, level, attack_name, sender_char)

func _on_spectator_spellcard_activated(p_num: int, level: int, rank: int, sender_char: String) -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_spellcard_activated(p_num, level, rank, sender_char)

func _on_spectator_health_updated(p_num: int, current: float, max_val: float) -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_health_updated(p_num, current, max_val)

func _on_spectator_hit_shockwave_spawned(p_num: int, landing_pos: Vector2) -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_hit_shockwave_spawned(p_num, landing_pos)

func _on_spectator_player_defeated(loser_num: int) -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_player_defeated(loser_num)

func _on_spectator_round_transition(winner_num: int, p1_wins: int, p2_wins: int, next_round: int, is_final_win: bool) -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_round_transition(winner_num, p1_wins, p2_wins, next_round, is_final_win)

func _on_spectator_round_started(current_round_num: int) -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_round_started(current_round_num)

func _on_spectator_match_ended() -> void:
	if spectator_coordinator: spectator_coordinator.on_spectator_match_ended()

func _setup_spectator_badge(p1_title: String, p2_title: String) -> void:
	if spectator_coordinator: spectator_coordinator.setup_spectator_badge(p1_title, p2_title)

# --- Replay System Event Forwarding ---

func _record_replay_event(evt: Dictionary) -> void:
	if replay_controller:
		replay_controller.record_event(evt, _match_total_elapsed)
