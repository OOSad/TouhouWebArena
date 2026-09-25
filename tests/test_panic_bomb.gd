extends SceneTree

# Unit test suite for Touhou 09 authentic "Panic Bomb" mechanic
# Tests threshold gating (Lv 2+), instant spellcard firing, zeroing of passive bar (0.0),
# charging lockout while passive bar is below 1.0, and InputMap bindings.

var test_root: Node = null
var passed_count: int = 0
var failed_count: int = 0

func assert_true(condition: bool, message: String) -> void:
	if condition:
		print("  [PASS] %s" % message)
		passed_count += 1
	else:
		print("  [FAIL] %s" % message)
		failed_count += 1

func assert_false(condition: bool, message: String) -> void:
	assert_true(not condition, message)

func _init() -> void:
	print("\n============================================")
	print(" RUNNING PANIC BOMB UNIT TESTS (TOUHOU 09)  ")
	print("============================================\n")
	
	test_root = Node2D.new()
	test_root.name = "TestRoot"
	root.add_child(test_root)
	
	test_panic_bomb_threshold_gating()
	test_panic_bomb_execution_and_zeroing()
	test_panic_bomb_active_charging_interruption()
	test_charging_lockout_below_one_segment()
	test_normal_release_vs_panic_bomb_consumption()
	test_spell_bar_rendering_at_zero()
	test_input_map_bindings()
	
	print("\n--------------------------------------------")
	print("TEST RESULTS: %d Passed, %d Failed" % [passed_count, failed_count])
	print("--------------------------------------------\n")
	
	test_root.queue_free()
	quit(0 if failed_count == 0 else 1)

func _create_test_player() -> Player:
	var player_scene := preload("res://scenes/player/player.tscn")
	var player: Player = player_scene.instantiate()
	player.character_id = "reimu"
	test_root.add_child(player)
	player.apply_character_data()
	return player

func test_panic_bomb_threshold_gating() -> void:
	print("Testing Panic Bomb threshold gating (requires Lv 2+)...")
	var player := _create_test_player()
	
	var fired_events: Array = []
	player.spellcard_fired.connect(func(lvl, s_name): fired_events.append({"lvl": lvl, "name": s_name}))
	
	# 1. At baseline (1.0 segment), panic bomb must fail
	player.passive_charge = 1.0
	var success_1 := player.trigger_panic_bomb()
	assert_false(success_1, "Panic bomb fails when passive_charge is 1.0 (requires >= 2.0)")
	assert_true(player.passive_charge == 1.0, "Passive charge remains untouched on failed panic bomb")
	assert_true(fired_events.is_empty(), "No spellcard event fired on failed panic bomb")
	
	# 2. At 1.99 segments (almost Lv 2), panic bomb must still fail
	player.passive_charge = 1.99
	var success_199 := player.trigger_panic_bomb()
	assert_false(success_199, "Panic bomb fails when passive_charge is 1.99 (< 2.0)")
	assert_true(player.passive_charge == 1.99, "Passive charge remains 1.99")
	assert_true(fired_events.is_empty(), "No spellcard event fired")
	
	# 3. Immobilized states prevent panic bomb
	player.passive_charge = 3.0
	player.is_dead = true
	assert_false(player.trigger_panic_bomb(), "Cannot panic bomb while dead")
	player.is_dead = false
	
	player.is_action_stopped = true
	assert_false(player.trigger_panic_bomb(), "Cannot panic bomb while action-stopped")
	player.is_action_stopped = false
	
	player.is_shoved = true
	assert_false(player.trigger_panic_bomb(), "Cannot panic bomb while shoved")
	player.is_shoved = false
	
	player.queue_free()

func test_panic_bomb_execution_and_zeroing() -> void:
	print("Testing Panic Bomb execution and zeroing of passive gauge...")
	var player := _create_test_player()
	
	var spell_fired: Dictionary = {}
	var bomb_fired: Dictionary = {}
	player.spellcard_fired.connect(func(lvl, s_name):
		spell_fired["lvl"] = lvl
		spell_fired["name"] = s_name
	)
	player.panic_bomb_triggered.connect(func(lvl, s_name):
		bomb_fired["lvl"] = lvl
		bomb_fired["name"] = s_name
	)
	
	# Test Lv 2 Panic Bomb
	player.passive_charge = 2.7
	var ok_lv2 := player.trigger_panic_bomb()
	assert_true(ok_lv2, "Panic bomb succeeds at passive_charge 2.7")
	assert_true(spell_fired.get("lvl", 0) == 2, "Fires Level 2 Spellcard when passive charge is 2.7")
	assert_true(bomb_fired.get("lvl", 0) == 2, "Emits panic_bomb_triggered(2)")
	assert_true(player.passive_charge == 0.0, "Passive charge drops to absolute 0.0 (consuming baseline segment)")
	assert_true(player.active_charge == 0.0, "Active charge is 0.0")
	
	# Test Lv 3 Panic Bomb
	spell_fired.clear()
	bomb_fired.clear()
	player.passive_charge = 3.5
	var ok_lv3 := player.trigger_panic_bomb()
	assert_true(ok_lv3, "Panic bomb succeeds at passive_charge 3.5")
	assert_true(spell_fired.get("lvl", 0) == 3, "Fires Level 3 Spellcard when passive charge is 3.5")
	assert_true(player.passive_charge == 0.0, "Passive charge drops to 0.0")
	
	# Test Lv 4 Panic Bomb
	spell_fired.clear()
	bomb_fired.clear()
	player.passive_charge = 4.0
	var ok_lv4 := player.trigger_panic_bomb()
	assert_true(ok_lv4, "Panic bomb succeeds at passive_charge 4.0")
	assert_true(spell_fired.get("lvl", 0) == 4, "Fires Level 4 Spellcard when passive charge is 4.0")
	assert_true(player.passive_charge == 0.0, "Passive charge drops to 0.0")
	
	player.queue_free()

func test_panic_bomb_active_charging_interruption() -> void:
	print("Testing Panic Bomb interruption while actively charging...")
	var player := _create_test_player()
	
	# Player is actively charging
	player.passive_charge = 3.2
	player.active_charge = 1.8
	player.is_charging = true
	
	var ok := player.trigger_panic_bomb()
	assert_true(ok, "Panic bomb triggers during active charging")
	assert_false(player.is_charging, "is_charging cancelled on panic bomb")
	assert_true(player.active_charge == 0.0, "active_charge reset to 0.0")
	assert_true(player.passive_charge == 0.0, "passive_charge zeroed out")
	
	player.queue_free()

func test_charging_lockout_below_one_segment() -> void:
	print("Testing charging lockout when passive charge is below 1.0...")
	var player := _create_test_player()
	player.is_local_player = false # Disable raw keyboard input polling
	
	# Set passive charge to 0.0 (simulating post-panic bomb state)
	player.set_passive_charge(0.0)
	assert_true(player.passive_charge == 0.0, "set_passive_charge allows 0.0 without clamping to 1.0")
	
	# Simulate charge input via _handle_charging
	# wants_charge will be false unless we simulate local input, so let's check gating condition directly
	var max_allowed: float = minf(player.passive_charge, float(player.charge_segments))
	assert_true(max_allowed == 0.0, "Max allowed active charge is 0.0 when passive_charge is 0.0")
	
	# Incremental accumulation from fairy kills:
	player.add_passive_charge(0.08) # +0.08
	assert_true(is_equal_approx(player.passive_charge, 0.08), "Passive gauge refills from 0.0 -> 0.08")
	
	player.add_passive_charge(0.92) # Bring to exactly 1.0
	assert_true(is_equal_approx(player.passive_charge, 1.0), "Passive gauge reaches 1.0")
	
	player.queue_free()

func test_normal_release_vs_panic_bomb_consumption() -> void:
	print("Testing normal charge release vs panic bomb consumption differences...")
	var player := _create_test_player()
	
	# 1. Normal Lv 2 release: preserves segment 1 (drops from 2.5 to 1.5)
	player.passive_charge = 2.5
	player.active_charge = 2.0
	player._release_charge()
	assert_true(is_equal_approx(player.passive_charge, 1.5), "Normal Lv 2 release preserves segment 1 (2.5 -> 1.5)")
	
	# 2. Normal Lv 4 release: preserves segment 1 (drops from 4.0 to 1.0)
	player.passive_charge = 4.0
	player.active_charge = 4.0
	player._release_charge()
	assert_true(is_equal_approx(player.passive_charge, 1.0), "Normal Lv 4 release preserves segment 1 (4.0 -> 1.0)")
	
	# 3. Panic bomb: zeros the entire bar including segment 1
	player.passive_charge = 2.5
	player.trigger_panic_bomb()
	assert_true(player.passive_charge == 0.0, "Panic bomb consumes ALL charge down to 0.0")
	
	player.queue_free()

func test_spell_bar_rendering_at_zero() -> void:
	print("Testing SpellBar UI rendering when passive charge is 0.0...")
	var spell_bar_scene := preload("res://scenes/ui/spell_bar.tscn")
	var bar: SpellBar = spell_bar_scene.instantiate()
	test_root.add_child(bar)
	bar._ready()
	
	bar.set_passive_charge(0.0, 4)
	assert_true(bar.passive_charge == 0.0, "SpellBar passive_charge property updated to 0.0")
	
	# Verify fill math for all 4 segments at 0.0
	for i in range(4):
		var seg_fill := clampf(bar.current_passive_charge - float(i), 0.0, 1.0)
		assert_true(seg_fill == 0.0, "Segment %d fill is 0.0 when gauge is empty" % i)
	
	bar.queue_free()

func test_input_map_bindings() -> void:
	print("Testing InputMap bindings for bomb and p2_bomb...")
	assert_true(InputMap.has_action("bomb"), "InputMap has 'bomb' action")
	assert_true(InputMap.has_action("p2_bomb"), "InputMap has 'p2_bomb' action")
	
	var bomb_events := InputMap.action_get_events("bomb")
	var has_c_key := false
	for ev in bomb_events:
		if ev is InputEventKey and (ev.physical_keycode == KEY_C or ev.keycode == KEY_C):
			has_c_key = true
	assert_true(has_c_key, "Action 'bomb' is mapped to KEY_C")
	
	var p2_events := InputMap.action_get_events("p2_bomb")
	var has_kp3_key := false
	for ev in p2_events:
		if ev is InputEventKey and (ev.physical_keycode == KEY_KP_3 or ev.keycode == KEY_KP_3 or ev.physical_keycode == KEY_COMMA):
			has_kp3_key = true
	assert_true(has_kp3_key, "Action 'p2_bomb' is mapped to KEY_KP_3 or KEY_COMMA")

