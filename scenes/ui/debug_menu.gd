class_name DebugMenu
extends CanvasLayer

signal menu_toggled(is_open: bool)

var arena: Control = null
var p1_playfield: Playfield = null
var p2_playfield: Playfield = null
var game_manager: Node = null

var is_open: bool = false

# UI Node References
@onready var root_control: Control = %RootControl
@onready var window_panel: PanelContainer = %WindowPanel
@onready var close_button: Button = %CloseButton

# P1 Controls
@onready var p1_status_label: Label = %P1StatusLabel
@onready var p1_hp_label: Label = %P1HPLabel
@onready var p1_hp_minus_btn: Button = %P1HPMinusBtn
@onready var p1_hp_plus_btn: Button = %P1HPPlusBtn
@onready var p1_hp_guts_btn: Button = %P1HPGutsBtn
@onready var p1_hp_fill_btn: Button = %P1HPFillBtn
@onready var p1_hp_kill_btn: Button = %P1HPKillBtn
@onready var p1_god_mode_check: CheckBox = %P1GodModeCheck
@onready var p1_rank_lv23_label: Label = %P1RankLv23Label
@onready var p1_rank_lv23_minus: Button = %P1RankLv23Minus
@onready var p1_rank_lv23_plus: Button = %P1RankLv23Plus
@onready var p1_rank_lv4_label: Label = %P1RankLv4Label
@onready var p1_rank_lv4_minus: Button = %P1RankLv4Minus
@onready var p1_rank_lv4_plus: Button = %P1RankLv4Plus
@onready var p1_freeze_rank_check: CheckBox = %P1FreezeRankCheck
@onready var p1_rank_preset_1: Button = %P1RankPreset1
@onready var p1_rank_preset_8: Button = %P1RankPreset8
@onready var p1_rank_preset_16: Button = %P1RankPreset16
@onready var p1_boss_rank_label: Label = %P1BossRankLabel
@onready var p1_boss_rank_minus: Button = %P1BossRankMinus
@onready var p1_boss_rank_plus: Button = %P1BossRankPlus
@onready var p1_freeze_boss_rank_check: CheckBox = %P1FreezeBossRankCheck
@onready var p1_boss_rank_preset_1: Button = %P1BossRankPreset1
@onready var p1_boss_rank_preset_8: Button = %P1BossRankPreset8
@onready var p1_boss_rank_preset_16: Button = %P1BossRankPreset16
@onready var p1_gauge_label: Label = %P1GaugeLabel
@onready var p1_gauge_fill_btn: Button = %P1GaugeFillBtn
@onready var p1_gauge_empty_btn: Button = %P1GaugeEmptyBtn
@onready var p1_fire_lv1_btn: Button = %P1FireLv1Btn
@onready var p1_fire_lv2_btn: Button = %P1FireLv2Btn
@onready var p1_fire_lv3_btn: Button = %P1FireLv3Btn
@onready var p1_fire_lv4_btn: Button = %P1FireLv4Btn if has_node("%P1FireLv4Btn") else null
@onready var p1_send_ex_btn: Button = %P1SendExBtn if has_node("%P1SendExBtn") else null
@onready var p1_ai_option: OptionButton = %P1AIOption

# P2 Controls
@onready var p2_status_label: Label = %P2StatusLabel
@onready var p2_hp_label: Label = %P2HPLabel
@onready var p2_hp_minus_btn: Button = %P2HPMinusBtn
@onready var p2_hp_plus_btn: Button = %P2HPPlusBtn
@onready var p2_hp_guts_btn: Button = %P2HPGutsBtn
@onready var p2_hp_fill_btn: Button = %P2HPFillBtn
@onready var p2_hp_kill_btn: Button = %P2HPKillBtn
@onready var p2_god_mode_check: CheckBox = %P2GodModeCheck
@onready var p2_rank_lv23_label: Label = %P2RankLv23Label
@onready var p2_rank_lv23_minus: Button = %P2RankLv23Minus
@onready var p2_rank_lv23_plus: Button = %P2RankLv23Plus
@onready var p2_rank_lv4_label: Label = %P2RankLv4Label
@onready var p2_rank_lv4_minus: Button = %P2RankLv4Minus
@onready var p2_rank_lv4_plus: Button = %P2RankLv4Plus
@onready var p2_freeze_rank_check: CheckBox = %P2FreezeRankCheck
@onready var p2_rank_preset_1: Button = %P2RankPreset1
@onready var p2_rank_preset_8: Button = %P2RankPreset8
@onready var p2_rank_preset_16: Button = %P2RankPreset16
@onready var p2_boss_rank_label: Label = %P2BossRankLabel
@onready var p2_boss_rank_minus: Button = %P2BossRankMinus
@onready var p2_boss_rank_plus: Button = %P2BossRankPlus
@onready var p2_freeze_boss_rank_check: CheckBox = %P2FreezeBossRankCheck
@onready var p2_boss_rank_preset_1: Button = %P2BossRankPreset1
@onready var p2_boss_rank_preset_8: Button = %P2BossRankPreset8
@onready var p2_boss_rank_preset_16: Button = %P2BossRankPreset16
@onready var p2_gauge_label: Label = %P2GaugeLabel
@onready var p2_gauge_fill_btn: Button = %P2GaugeFillBtn
@onready var p2_gauge_empty_btn: Button = %P2GaugeEmptyBtn
@onready var p2_fire_lv1_btn: Button = %P2FireLv1Btn
@onready var p2_fire_lv2_btn: Button = %P2FireLv2Btn
@onready var p2_fire_lv3_btn: Button = %P2FireLv3Btn
@onready var p2_fire_lv4_btn: Button = %P2FireLv4Btn if has_node("%P2FireLv4Btn") else null
@onready var p2_send_ex_btn: Button = %P2SendExBtn if has_node("%P2SendExBtn") else null
@onready var p2_ai_option: OptionButton = %P2AIOption

# World & Spawner Controls
@onready var spawner_fairies_check: CheckBox = %SpawnerFairiesCheck
@onready var spawner_pellets_check: CheckBox = %SpawnerPelletsCheck
@onready var spawner_spirits_check: CheckBox = %SpawnerSpiritsCheck
@onready var spawner_lily_check: CheckBox = %SpawnerLilyCheck if has_node("%SpawnerLilyCheck") else null
@onready var bullet_cap_check: CheckBox = %BulletCapCheck if has_node("%BulletCapCheck") else null
@onready var spawn_lily_btn: Button = %SpawnLilyBtn if has_node("%SpawnLilyBtn") else null
@onready var clear_bullets_btn: Button = %ClearBulletsBtn
@onready var clear_enemies_btn: Button = %ClearEnemiesBtn
@onready var speed_025_btn: Button = %Speed025Btn
@onready var speed_050_btn: Button = %Speed050Btn
@onready var speed_100_btn: Button = %Speed100Btn
@onready var speed_200_btn: Button = %Speed200Btn
@onready var speed_label: Label = %SpeedLabel
@onready var restart_round_btn: Button = %RestartRoundBtn
@onready var win_p1_btn: Button = %WinP1Btn
@onready var win_p2_btn: Button = %WinP2Btn

# Match Rank Controls (World & Spawners tab)
@onready var match_rank_label: Label = %MatchRankLabel if has_node("%MatchRankLabel") else null
@onready var match_rank_minus: Button = %MatchRankMinus if has_node("%MatchRankMinus") else null
@onready var match_rank_plus: Button = %MatchRankPlus if has_node("%MatchRankPlus") else null
@onready var match_rank_preset_1: Button = %MatchRankPreset1 if has_node("%MatchRankPreset1") else null
@onready var match_rank_preset_8: Button = %MatchRankPreset8 if has_node("%MatchRankPreset8") else null
@onready var match_rank_preset_16: Button = %MatchRankPreset16 if has_node("%MatchRankPreset16") else null
@onready var match_rank_preset_22: Button = %MatchRankPreset22 if has_node("%MatchRankPreset22") else null

# Telemetry Readouts
@onready var p1_telemetry_label: Label = %P1TelemetryLabel if has_node("%P1TelemetryLabel") else null
@onready var p2_telemetry_label: Label = %P2TelemetryLabel if has_node("%P2TelemetryLabel") else null
@onready var total_telemetry_label: Label = %TotalTelemetryLabel if has_node("%TotalTelemetryLabel") else null

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	visible = false
	is_open = false
	_setup_signals()

func setup(p_arena: Control, p1: Playfield, p2: Playfield, p_gm: Node = null) -> void:
	arena = p_arena
	p1_playfield = p1
	p2_playfield = p2
	game_manager = p_gm
	_refresh_all()

func toggle() -> void:
	if is_open:
		close()
	else:
		open()

func open() -> void:
	is_open = true
	visible = true
	_refresh_all()
	menu_toggled.emit(true)

func close() -> void:
	is_open = false
	visible = false
	menu_toggled.emit(false)

func _process(_delta: float) -> void:
	if is_open:
		_refresh_live_readouts()

var _signals_connected: bool = false

func _setup_signals() -> void:
	if _signals_connected:
		return
	_signals_connected = true
	
	if close_button:
		close_button.pressed.connect(close)
	
	# P1 HP
	if p1_hp_minus_btn: p1_hp_minus_btn.pressed.connect(func(): _modify_hp(1, -0.5))
	if p1_hp_plus_btn: p1_hp_plus_btn.pressed.connect(func(): _modify_hp(1, 0.5))
	if p1_hp_guts_btn: p1_hp_guts_btn.pressed.connect(func(): _set_hp(1, 0.5))
	if p1_hp_fill_btn: p1_hp_fill_btn.pressed.connect(func(): _set_hp(1, 5.0))
	if p1_hp_kill_btn: p1_hp_kill_btn.pressed.connect(func(): _kill_player(1))
	if p1_god_mode_check: p1_god_mode_check.toggled.connect(func(val): _toggle_god_mode(1, val))
	
	# P1 Ranks
	if p1_rank_lv23_minus: p1_rank_lv23_minus.pressed.connect(func(): _modify_rank_lv23(1, -1))
	if p1_rank_lv23_plus: p1_rank_lv23_plus.pressed.connect(func(): _modify_rank_lv23(1, 1))
	if p1_rank_lv4_minus: p1_rank_lv4_minus.pressed.connect(func(): _modify_rank_lv4(1, -1))
	if p1_rank_lv4_plus: p1_rank_lv4_plus.pressed.connect(func(): _modify_rank_lv4(1, 1))
	if p1_freeze_rank_check: p1_freeze_rank_check.toggled.connect(func(val): _toggle_freeze_rank(1, val))
	if p1_rank_preset_1: p1_rank_preset_1.pressed.connect(func(): _set_both_ranks(1, 1))
	if p1_rank_preset_8: p1_rank_preset_8.pressed.connect(func(): _set_both_ranks(1, 8))
	if p1_rank_preset_16: p1_rank_preset_16.pressed.connect(func(): _set_both_ranks(1, 16))

	# P1 Boss Rank (independent of Spellcard Rank)
	if p1_boss_rank_minus: p1_boss_rank_minus.pressed.connect(func(): _modify_boss_rank(1, -1))
	if p1_boss_rank_plus: p1_boss_rank_plus.pressed.connect(func(): _modify_boss_rank(1, 1))
	if p1_freeze_boss_rank_check: p1_freeze_boss_rank_check.toggled.connect(func(val): _toggle_freeze_boss_rank(1, val))
	if p1_boss_rank_preset_1: p1_boss_rank_preset_1.pressed.connect(func(): _set_boss_rank(1, 1))
	if p1_boss_rank_preset_8: p1_boss_rank_preset_8.pressed.connect(func(): _set_boss_rank(1, 8))
	if p1_boss_rank_preset_16: p1_boss_rank_preset_16.pressed.connect(func(): _set_boss_rank(1, 16))

	# P1 Gauge & Attacks
	if p1_gauge_fill_btn: p1_gauge_fill_btn.pressed.connect(func(): _set_gauge(1, 4.0))
	if p1_gauge_empty_btn: p1_gauge_empty_btn.pressed.connect(func(): _set_gauge(1, 1.0))
	if p1_fire_lv1_btn: p1_fire_lv1_btn.pressed.connect(func(): _fire_charge_lv1(1))
	if p1_fire_lv2_btn: p1_fire_lv2_btn.pressed.connect(func(): _fire_spell(1, 2))
	if p1_fire_lv3_btn: p1_fire_lv3_btn.pressed.connect(func(): _fire_spell(1, 3))
	if p1_fire_lv4_btn: p1_fire_lv4_btn.pressed.connect(func(): _fire_spell(1, 4))
	if p1_send_ex_btn: p1_send_ex_btn.pressed.connect(func(): _send_extra_attack(1))
	
	# P1 AI Option
	if p1_ai_option:
		p1_ai_option.clear()
		p1_ai_option.add_item("Human (Off)", 0)
		p1_ai_option.add_item("Bot: Full (Shoot & Dodge)", 1)
		p1_ai_option.add_item("Bot: Dodge Only (No Shoot)", 2)
		p1_ai_option.item_selected.connect(func(idx): _set_player_ai(1, idx))
	
	# P2 HP
	if p2_hp_minus_btn: p2_hp_minus_btn.pressed.connect(func(): _modify_hp(2, -0.5))
	if p2_hp_plus_btn: p2_hp_plus_btn.pressed.connect(func(): _modify_hp(2, 0.5))
	if p2_hp_guts_btn: p2_hp_guts_btn.pressed.connect(func(): _set_hp(2, 0.5))
	if p2_hp_fill_btn: p2_hp_fill_btn.pressed.connect(func(): _set_hp(2, 5.0))
	if p2_hp_kill_btn: p2_hp_kill_btn.pressed.connect(func(): _kill_player(2))
	if p2_god_mode_check: p2_god_mode_check.toggled.connect(func(val): _toggle_god_mode(2, val))
	
	# P2 Ranks
	if p2_rank_lv23_minus: p2_rank_lv23_minus.pressed.connect(func(): _modify_rank_lv23(2, -1))
	if p2_rank_lv23_plus: p2_rank_lv23_plus.pressed.connect(func(): _modify_rank_lv23(2, 1))
	if p2_rank_lv4_minus: p2_rank_lv4_minus.pressed.connect(func(): _modify_rank_lv4(2, -1))
	if p2_rank_lv4_plus: p2_rank_lv4_plus.pressed.connect(func(): _modify_rank_lv4(2, 1))
	if p2_freeze_rank_check: p2_freeze_rank_check.toggled.connect(func(val): _toggle_freeze_rank(2, val))
	if p2_rank_preset_1: p2_rank_preset_1.pressed.connect(func(): _set_both_ranks(2, 1))
	if p2_rank_preset_8: p2_rank_preset_8.pressed.connect(func(): _set_both_ranks(2, 8))
	if p2_rank_preset_16: p2_rank_preset_16.pressed.connect(func(): _set_both_ranks(2, 16))

	# P2 Boss Rank (independent of Spellcard Rank)
	if p2_boss_rank_minus: p2_boss_rank_minus.pressed.connect(func(): _modify_boss_rank(2, -1))
	if p2_boss_rank_plus: p2_boss_rank_plus.pressed.connect(func(): _modify_boss_rank(2, 1))
	if p2_freeze_boss_rank_check: p2_freeze_boss_rank_check.toggled.connect(func(val): _toggle_freeze_boss_rank(2, val))
	if p2_boss_rank_preset_1: p2_boss_rank_preset_1.pressed.connect(func(): _set_boss_rank(2, 1))
	if p2_boss_rank_preset_8: p2_boss_rank_preset_8.pressed.connect(func(): _set_boss_rank(2, 8))
	if p2_boss_rank_preset_16: p2_boss_rank_preset_16.pressed.connect(func(): _set_boss_rank(2, 16))

	# P2 Gauge & Attacks
	if p2_gauge_fill_btn: p2_gauge_fill_btn.pressed.connect(func(): _set_gauge(2, 4.0))
	if p2_gauge_empty_btn: p2_gauge_empty_btn.pressed.connect(func(): _set_gauge(2, 1.0))
	if p2_fire_lv1_btn: p2_fire_lv1_btn.pressed.connect(func(): _fire_charge_lv1(2))
	if p2_fire_lv2_btn: p2_fire_lv2_btn.pressed.connect(func(): _fire_spell(2, 2))
	if p2_fire_lv3_btn: p2_fire_lv3_btn.pressed.connect(func(): _fire_spell(2, 3))
	if p2_fire_lv4_btn: p2_fire_lv4_btn.pressed.connect(func(): _fire_spell(2, 4))
	if p2_send_ex_btn: p2_send_ex_btn.pressed.connect(func(): _send_extra_attack(2))
	
	# P2 AI Option
	if p2_ai_option:
		p2_ai_option.clear()
		p2_ai_option.add_item("Human (Off)", 0)
		p2_ai_option.add_item("Bot: Full (Shoot & Dodge)", 1)
		p2_ai_option.add_item("Bot: Dodge Only (No Shoot)", 2)
		p2_ai_option.item_selected.connect(func(idx): _set_player_ai(2, idx))
	
	# World / Spawners
	if spawner_fairies_check: spawner_fairies_check.toggled.connect(_toggle_fairies_spawning)
	if spawner_pellets_check: spawner_pellets_check.toggled.connect(_toggle_pellets_spawning)
	if spawner_spirits_check: spawner_spirits_check.toggled.connect(_toggle_spirits_spawning)
	if spawner_lily_check: spawner_lily_check.toggled.connect(_toggle_lily_spawning)
	if bullet_cap_check: bullet_cap_check.toggled.connect(_toggle_bullet_cap)
	if clear_bullets_btn: clear_bullets_btn.pressed.connect(_clear_all_bullets)
	if clear_enemies_btn: clear_enemies_btn.pressed.connect(_clear_all_enemies)
	if spawn_lily_btn: spawn_lily_btn.pressed.connect(_on_spawn_lily_pressed)
	
	# Game Speed
	if speed_025_btn: speed_025_btn.pressed.connect(func(): _set_time_scale(0.25))
	if speed_050_btn: speed_050_btn.pressed.connect(func(): _set_time_scale(0.50))
	if speed_100_btn: speed_100_btn.pressed.connect(func(): _set_time_scale(1.00))
	if speed_200_btn: speed_200_btn.pressed.connect(func(): _set_time_scale(2.00))
	
	# Match / Rounds
	if restart_round_btn: restart_round_btn.pressed.connect(_restart_round)
	if win_p1_btn: win_p1_btn.pressed.connect(func(): _give_round_win(1))
	if win_p2_btn: win_p2_btn.pressed.connect(func(): _give_round_win(2))
	
	# Match Rank
	if match_rank_minus: match_rank_minus.pressed.connect(func(): _adjust_match_rank(-1))
	if match_rank_plus: match_rank_plus.pressed.connect(func(): _adjust_match_rank(1))
	if match_rank_preset_1: match_rank_preset_1.pressed.connect(func(): _set_match_rank(1))
	if match_rank_preset_8: match_rank_preset_8.pressed.connect(func(): _set_match_rank(8))
	if match_rank_preset_16: match_rank_preset_16.pressed.connect(func(): _set_match_rank(16))
	if match_rank_preset_22: match_rank_preset_22.pressed.connect(func(): _set_match_rank(22))

func _refresh_all() -> void:
	_refresh_live_readouts()
	if p1_playfield:
		if p1_god_mode_check and p1_playfield.player:
			p1_god_mode_check.button_pressed = p1_playfield.player.god_mode
		if p1_freeze_rank_check:
			p1_freeze_rank_check.button_pressed = p1_playfield.freeze_ranks
		if p1_freeze_boss_rank_check and p2_playfield:
			p1_freeze_boss_rank_check.button_pressed = p2_playfield.freeze_boss_rank
	if p2_playfield:
		if p2_god_mode_check and p2_playfield.player:
			p2_god_mode_check.button_pressed = p2_playfield.player.god_mode
		if p2_freeze_rank_check:
			p2_freeze_rank_check.button_pressed = p2_playfield.freeze_ranks
		if p2_freeze_boss_rank_check and p1_playfield:
			p2_freeze_boss_rank_check.button_pressed = p1_playfield.freeze_boss_rank
	
	if spawner_fairies_check and p1_playfield:
		spawner_fairies_check.button_pressed = p1_playfield.fairy_spawning_enabled
	if spawner_pellets_check and p1_playfield:
		spawner_pellets_check.button_pressed = p1_playfield.pellet_spawning_enabled
	if spawner_spirits_check and p1_playfield:
		spawner_spirits_check.button_pressed = p1_playfield.spirit_spawning_enabled
	if spawner_lily_check:
		if arena and "lily_white_spawning_enabled" in arena:
			spawner_lily_check.button_pressed = arena.lily_white_spawning_enabled
		elif p1_playfield and "lily_white_spawning_enabled" in p1_playfield:
			spawner_lily_check.button_pressed = p1_playfield.lily_white_spawning_enabled
	if bullet_cap_check and p1_playfield:
		bullet_cap_check.button_pressed = (p1_playfield.max_active_bullets > 0)
	
	_update_speed_label()

func _refresh_live_readouts() -> void:
	# P1 Readouts
	if p1_playfield:
		if p1_status_label:
			var p1_p = p1_playfield.player
			if p1_p:
				var st := "ALIVE"
				if p1_p.is_dead: st = "DEAD"
				elif p1_p.god_mode: st = "GOD MODE"
				elif p1_p.is_shoved: st = "SHOVE"
				elif p1_p.is_invulnerable: st = "INVULN"
				p1_status_label.text = "%s [%s]" % [p1_p.character_id.to_upper(), st]
		
		if p1_hp_label and p1_playfield.player:
			p1_hp_label.text = "%.1f / %.1f" % [p1_playfield.player.current_health, p1_playfield.player.max_health]
		
		if p1_rank_lv23_label:
			var tag := " [LOCK]" if p1_playfield.freeze_ranks else ""
			p1_rank_lv23_label.text = "Rank Lv2-3: %d%s" % [p1_playfield.current_rank_lv2_3, tag]
		if p1_rank_lv4_label:
			var tag := " [LOCK]" if p1_playfield.freeze_ranks else ""
			p1_rank_lv4_label.text = "Rank Lv4: %d%s" % [p1_playfield.current_rank_lv4, tag]
		if p1_boss_rank_label and p2_playfield:
			var boss_tag := " [LOCK]" if p2_playfield.freeze_boss_rank else ""
			p1_boss_rank_label.text = "Boss Rank: %d%s" % [p2_playfield.current_boss_rank, boss_tag]

		if p1_gauge_label and p1_playfield.player:
			p1_gauge_label.text = "Gauge: %.2f / %d" % [p1_playfield.player.passive_charge, p1_playfield.player.charge_segments]
	
	# P2 Readouts
	if p2_playfield:
		if p2_status_label:
			var p2_p = p2_playfield.player
			if p2_p:
				var st := "ALIVE"
				if p2_p.is_dead: st = "DEAD"
				elif p2_p.god_mode: st = "GOD MODE"
				elif p2_p.is_shoved: st = "SHOVE"
				elif p2_p.is_invulnerable: st = "INVULN"
				p2_status_label.text = "%s [%s]" % [p2_p.character_id.to_upper(), st]
		
		if p2_hp_label and p2_playfield.player:
			p2_hp_label.text = "%.1f / %.1f" % [p2_playfield.player.current_health, p2_playfield.player.max_health]
		
		if p2_rank_lv23_label:
			var tag := " [LOCK]" if p2_playfield.freeze_ranks else ""
			p2_rank_lv23_label.text = "Rank Lv2-3: %d%s" % [p2_playfield.current_rank_lv2_3, tag]
		if p2_rank_lv4_label:
			var tag := " [LOCK]" if p2_playfield.freeze_ranks else ""
			p2_rank_lv4_label.text = "Rank Lv4: %d%s" % [p2_playfield.current_rank_lv4, tag]
		if p2_boss_rank_label and p1_playfield:
			var boss_tag := " [LOCK]" if p1_playfield.freeze_boss_rank else ""
			p2_boss_rank_label.text = "Boss Rank: %d%s" % [p1_playfield.current_boss_rank, boss_tag]

		if p2_gauge_label and p2_playfield.player:
			p2_gauge_label.text = "Gauge: %.2f / %d" % [p2_playfield.player.passive_charge, p2_playfield.player.charge_segments]

	# Telemetry Readouts (thprac style)
	var t1: Dictionary = p1_playfield.get_entity_telemetry() if (p1_playfield and p1_playfield.has_method("get_entity_telemetry")) else {}
	var t2: Dictionary = p2_playfield.get_entity_telemetry() if (p2_playfield and p2_playfield.has_method("get_entity_telemetry")) else {}
	
	if p1_telemetry_label and not t1.is_empty():
		var cap_str: String = "%d" % p1_playfield.max_active_bullets if p1_playfield.max_active_bullets > 0 else "OFF"
		p1_telemetry_label.text = "P1 BULLETS: %d/%s (Pel: %d | Dan: %d)  |  F: %d  S: %d" % [
			t1.total_bullets, cap_str, t1.pellets, t1.danmaku, t1.fairies, t1.spirits
		]
		if p1_playfield.max_active_bullets > 0 and t1.total_bullets >= p1_playfield.max_active_bullets:
			p1_telemetry_label.add_theme_color_override("font_color", Color(1.0, 0.35, 0.35))
		elif p1_playfield.max_active_bullets > 0 and t1.total_bullets >= int(p1_playfield.max_active_bullets * 0.8):
			p1_telemetry_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.3))
		else:
			p1_telemetry_label.add_theme_color_override("font_color", Color(0.85, 0.92, 1.0))
			
	if p2_telemetry_label and not t2.is_empty():
		var cap_str: String = "%d" % p2_playfield.max_active_bullets if p2_playfield.max_active_bullets > 0 else "OFF"
		p2_telemetry_label.text = "P2 BULLETS: %d/%s (Pel: %d | Dan: %d)  |  F: %d  S: %d" % [
			t2.total_bullets, cap_str, t2.pellets, t2.danmaku, t2.fairies, t2.spirits
		]
		if p2_playfield.max_active_bullets > 0 and t2.total_bullets >= p2_playfield.max_active_bullets:
			p2_telemetry_label.add_theme_color_override("font_color", Color(1.0, 0.35, 0.35))
		elif p2_playfield.max_active_bullets > 0 and t2.total_bullets >= int(p2_playfield.max_active_bullets * 0.8):
			p2_telemetry_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.3))
		else:
			p2_telemetry_label.add_theme_color_override("font_color", Color(0.85, 0.92, 1.0))
			
	if total_telemetry_label and not t1.is_empty() and not t2.is_empty():
		var total_b: int = t1.total_bullets + t2.total_bullets
		var total_e: int = t1.total_entities + t2.total_entities
		total_telemetry_label.text = "TOTAL: %d B / %d ENT" % [total_b, total_e]
	
	# Overall Rank readout (independent of Spellcard/Boss Rank)
	if match_rank_label and p1_playfield:
		var freeze_tag: String = " [LOCK]" if p1_playfield.freeze_overall_rank else ""
		match_rank_label.text = "Overall Rank: %d / 22%s" % [p1_playfield.current_overall_rank, freeze_tag]

func _toggle_bullet_cap(enabled: bool) -> void:
	var cap: int = 350 if enabled else 0
	if p1_playfield:
		p1_playfield.max_active_bullets = cap
	if p2_playfield:
		p2_playfield.max_active_bullets = cap

func _get_playfield(player_num: int) -> Playfield:
	return p1_playfield if player_num == 1 else p2_playfield

## Boss Rank is tracked per the field a boss actually appears ON (the target field, not the
## caster's own) - see action_stop_coordinator.gd's trigger_spellcard_effects(). So "P1's" Boss
## Rank controls need to write to P2's playfield (the field P1's own boss lands on) for the
## button on P1's side of the debug panel to control "your own boss's rank", matching how the
## rest of the panel reads.
func _get_opponent_playfield(player_num: int) -> Playfield:
	return p2_playfield if player_num == 1 else p1_playfield

func _modify_hp(player_num: int, delta_hp: float) -> void:
	var pf := _get_playfield(player_num)
	if pf and pf.player:
		pf.player.set_health(pf.player.current_health + delta_hp)

func _set_hp(player_num: int, target_hp: float) -> void:
	var pf := _get_playfield(player_num)
	if pf and pf.player:
		pf.player.set_health(target_hp)

func _kill_player(player_num: int) -> void:
	var pf := _get_playfield(player_num)
	if pf and pf.player:
		pf.player.insta_kill()

func _toggle_god_mode(player_num: int, enabled: bool) -> void:
	var pf := _get_playfield(player_num)
	if pf and pf.player:
		pf.player.god_mode = enabled

func _modify_rank_lv23(player_num: int, delta_rank: int) -> void:
	var pf := _get_playfield(player_num)
	if pf:
		pf.freeze_ranks = true
		pf.set_ranks(pf.current_rank_lv2_3 + delta_rank, pf.current_rank_lv4)
		_sync_freeze_checkboxes()
		_refresh_live_readouts()

func _modify_rank_lv4(player_num: int, delta_rank: int) -> void:
	var pf := _get_playfield(player_num)
	if pf:
		pf.freeze_ranks = true
		pf.set_ranks(pf.current_rank_lv2_3, pf.current_rank_lv4 + delta_rank)
		_sync_freeze_checkboxes()
		_refresh_live_readouts()

func _set_both_ranks(player_num: int, rank_val: int) -> void:
	var pf := _get_playfield(player_num)
	if pf:
		pf.freeze_ranks = true
		pf.set_ranks(rank_val, rank_val)
		_sync_freeze_checkboxes()
		_refresh_live_readouts()

func _toggle_freeze_rank(player_num: int, freeze: bool) -> void:
	var pf := _get_playfield(player_num)
	if pf:
		pf.freeze_ranks = freeze
		if not freeze:
			pf.match_elapsed_time = maxf(0.0, float(pf.current_rank_lv2_3 - 1) * 10.0)
		_sync_freeze_checkboxes()
		_refresh_live_readouts()

func _modify_boss_rank(player_num: int, delta_rank: int) -> void:
	var pf := _get_opponent_playfield(player_num)
	if pf:
		pf.freeze_boss_rank = true
		pf.set_boss_rank(pf.current_boss_rank + delta_rank)
		_sync_freeze_checkboxes()
		_refresh_live_readouts()

func _set_boss_rank(player_num: int, rank_val: int) -> void:
	var pf := _get_opponent_playfield(player_num)
	if pf:
		pf.freeze_boss_rank = true
		pf.set_boss_rank(rank_val)
		_sync_freeze_checkboxes()
		_refresh_live_readouts()

func _toggle_freeze_boss_rank(player_num: int, freeze: bool) -> void:
	var pf := _get_opponent_playfield(player_num)
	if pf:
		pf.freeze_boss_rank = freeze
		_sync_freeze_checkboxes()
		_refresh_live_readouts()

func _sync_freeze_checkboxes() -> void:
	if p1_freeze_rank_check and p1_playfield:
		p1_freeze_rank_check.set_pressed_no_signal(p1_playfield.freeze_ranks)
	if p2_freeze_rank_check and p2_playfield:
		p2_freeze_rank_check.set_pressed_no_signal(p2_playfield.freeze_ranks)
	if p1_freeze_boss_rank_check and p2_playfield:
		p1_freeze_boss_rank_check.set_pressed_no_signal(p2_playfield.freeze_boss_rank)
	if p2_freeze_boss_rank_check and p1_playfield:
		p2_freeze_boss_rank_check.set_pressed_no_signal(p1_playfield.freeze_boss_rank)


func _set_gauge(player_num: int, val: float) -> void:
	var pf := _get_playfield(player_num)
	if pf and pf.player:
		pf.player.set_passive_charge(val)

func _fire_charge_lv1(player_num: int) -> void:
	var pf := _get_playfield(player_num)
	if pf and pf.player:
		pf.player.charge_attack_fired.emit(1, pf.player.charge_attack_name)

func _fire_spell(player_num: int, level: int) -> void:
	if arena and arena.has_method("_on_spellcard_activated"):
		var pf := _get_playfield(player_num)
		var rank: int = (pf.current_rank_lv4 if level >= 4 else pf.current_rank_lv2_3) if pf else 1
		arena._on_spellcard_activated(player_num, level, rank)

## Drops this player's Extra Attack straight onto the opponent's field, skipping the chain
## that normally earns one and the light mote that carries it.
func _send_extra_attack(player_num: int) -> void:
	var pf := _get_playfield(player_num)
	var opponent := _get_opponent_playfield(player_num)
	if pf == null or opponent == null or pf.player == null:
		return
	var sender: String = pf.player.character_id
	for target in MoteDispatcher.roll_extra_attack_targets(sender, pf.current_rank_lv2_3):
		opponent.spawn_extra_attack(sender, target, pf.current_rank_lv2_3)

func _set_player_ai(player_num: int, mode_idx: int) -> void:
	var pf := _get_playfield(player_num)
	if pf:
		match mode_idx:
			0: # Human (Off)
				pf.set_ai_mode(false, true)
			1: # Full AI (Shoot & Dodge)
				pf.set_ai_mode(true, true)
			2: # Dodge Only (No Shoot)
				pf.set_ai_mode(true, false)
	
	if arena and arena.has_method("_update_ai_badges"):
		if player_num == 1:
			arena._p1_ai_active = (mode_idx != 0)
			arena._p1_ai_dodge_only = (mode_idx == 2)
		else:
			arena._p2_ai_active = (mode_idx != 0)
			arena._p2_ai_dodge_only = (mode_idx == 2)
		arena._update_ai_badges()

func _toggle_fairies_spawning(enabled: bool) -> void:
	if p1_playfield: p1_playfield.fairy_spawning_enabled = enabled
	if p2_playfield: p2_playfield.fairy_spawning_enabled = enabled

func _toggle_pellets_spawning(enabled: bool) -> void:
	if p1_playfield: p1_playfield.pellet_spawning_enabled = enabled
	if p2_playfield: p2_playfield.pellet_spawning_enabled = enabled

func _toggle_spirits_spawning(enabled: bool) -> void:
	if p1_playfield: p1_playfield.spirit_spawning_enabled = enabled
	if p2_playfield: p2_playfield.spirit_spawning_enabled = enabled

func _toggle_lily_spawning(enabled: bool) -> void:
	if arena and "lily_white_spawning_enabled" in arena:
		arena.lily_white_spawning_enabled = enabled
	if p1_playfield: p1_playfield.lily_white_spawning_enabled = enabled
	if p2_playfield: p2_playfield.lily_white_spawning_enabled = enabled

func _clear_all_bullets() -> void:
	if p1_playfield: p1_playfield.clear_all_bullets()
	if p2_playfield: p2_playfield.clear_all_bullets()

func _clear_all_enemies() -> void:
	if p1_playfield: p1_playfield.clear_all_enemies()
	if p2_playfield: p2_playfield.clear_all_enemies()

func _on_spawn_lily_pressed() -> void:
	if arena and arena.has_method("spawn_lily_white_both_fields"):
		arena.spawn_lily_white_both_fields()
		arena.spawn_lily_white_both_fields(true)
	else:
		if p1_playfield: p1_playfield.spawn_lily_white()
		if p2_playfield: p2_playfield.spawn_lily_white()
		if p1_playfield: p1_playfield.spawn_lily_white(true)
		if p2_playfield: p2_playfield.spawn_lily_white(true)

func _set_time_scale(scale: float) -> void:
	Engine.time_scale = scale
	_update_speed_label()

func _update_speed_label() -> void:
	if speed_label:
		speed_label.text = "Speed: %.2fx" % Engine.time_scale

func _restart_round() -> void:
	if arena and arena.has_method("_start_new_round"):
		arena._start_new_round()
	elif p1_playfield and p2_playfield:
		p1_playfield.reset_for_new_round()
		p2_playfield.reset_for_new_round()

func _give_round_win(winner_player: int) -> void:
	var loser_player: int = 2 if winner_player == 1 else 1
	var loser_pf := _get_playfield(loser_player)
	if loser_pf and loser_pf.player:
		loser_pf.player.insta_kill()

func _get_current_match_rank() -> int:
	if p1_playfield:
		return p1_playfield.current_overall_rank
	return 1

func _set_match_rank(rank: int) -> void:
	# Overall Rank is fully independent of Spellcard Rank and Boss Rank -
	# this only ever touches current_overall_rank, never match_elapsed_time
	# or the spellcard/boss rank tracks.
	if p1_playfield:
		p1_playfield.freeze_overall_rank = true
		p1_playfield.set_overall_rank(rank)
	if p2_playfield:
		p2_playfield.freeze_overall_rank = true
		p2_playfield.set_overall_rank(rank)
	_refresh_live_readouts()

func _adjust_match_rank(delta: int) -> void:
	_set_match_rank(_get_current_match_rank() + delta)
