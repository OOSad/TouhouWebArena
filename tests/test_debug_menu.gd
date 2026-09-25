extends SceneTree

var passed_count: int = 0
var failed_count: int = 0

func _init() -> void:
	print("\n===============================")
	print(" RUNNING DEBUG MENU TESTS")
	print("===============================\n")
	
	test_debug_menu_instantiation()
	test_player_debug_controls()
	test_playfield_rank_freeze_and_set()
	test_debug_menu_rank_button_actions()
	test_playfield_spawner_toggles()
	test_playfield_board_wipes()
	test_ai_dodge_only_mode()
	test_debug_menu_lily_white_toggle()
	
	print("\n-------------------------------")
	print("TEST RESULTS: %d Passed, %d Failed" % [passed_count, failed_count])
	print("-------------------------------\n")
	
	quit(1 if failed_count > 0 else 0)

func assert_true(condition: bool, test_name: String) -> void:
	if condition:
		passed_count += 1
		print("  [PASS] %s" % test_name)
	else:
		failed_count += 1
		print("  [FAIL] %s" % test_name)

const DebugMenuScript = preload("res://scenes/ui/debug_menu.gd")

func test_debug_menu_instantiation() -> void:
	print("Testing DebugMenu instantiation & toggle...")
	var menu_scene: PackedScene = preload("res://scenes/ui/debug_menu.tscn")
	var menu = menu_scene.instantiate()
	root.add_child(menu)
	
	assert_true(menu != null, "DebugMenu instantiated successfully")
	assert_true(menu.layer == 100, "DebugMenu is on layer 100")
	assert_true(menu.visible == false, "DebugMenu is hidden by default")
	assert_true(menu.is_open == false, "DebugMenu is_open is false by default")
	
	menu.toggle()
	assert_true(menu.visible == true, "DebugMenu visible after toggle")
	assert_true(menu.is_open == true, "DebugMenu is_open after toggle")
	
	menu.toggle()
	assert_true(menu.visible == false, "DebugMenu hidden after second toggle")
	assert_true(menu.is_open == false, "DebugMenu is_open is false after second toggle")
	
	menu.queue_free()

func test_player_debug_controls() -> void:
	print("Testing Player debug methods (HP, God Mode, Gauge, Insta-Kill)...")
	var player_scene: PackedScene = preload("res://scenes/player/player.tscn")
	var p: Player = player_scene.instantiate() as Player
	p.player_number = 1
	p.is_local_player = true
	p.character_id = "reimu"
	root.add_child(p)
	
	# Initial HP
	assert_true(p.current_health == 5.0, "Player starts at full HP (5.0)")
	
	# Set HP
	p.set_health(2.5)
	assert_true(p.current_health == 2.5, "set_health(2.5) updates HP to 2.5")
	
	# God mode
	p.god_mode = true
	assert_true(p.god_mode == true, "God mode enabled")
	p.take_damage(2.0)
	assert_true(p.current_health == 2.5, "God mode prevents damage from take_damage")
	
	p.god_mode = false
	p.take_damage(1.0)
	assert_true(p.current_health == 1.5, "Disabling god mode allows damage")
	
	# Gauge manipulation
	p.set_passive_charge(3.5)
	assert_true(p.passive_charge == 3.5, "set_passive_charge(3.5) sets charge to 3.5")
	
	# Insta-kill
	p.insta_kill()
	assert_true(p.is_dead == true, "insta_kill() sets is_dead to true")
	assert_true(p.current_health == 0.0, "insta_kill() drops health to 0")
	
	p.queue_free()

func test_playfield_rank_freeze_and_set() -> void:
	print("Testing Playfield rank freeze and manual override...")
	var pf_scene: PackedScene = preload("res://scenes/arena/playfield.tscn")
	var pf: Playfield = pf_scene.instantiate() as Playfield
	pf.setup(1, false, "reimu")
	root.add_child(pf)
	
	assert_true(pf.current_rank_lv2_3 == 1, "Rank Lv2-3 starts at 1")
	assert_true(pf.current_rank_lv4 == 1, "Rank Lv4 starts at 1")
	
	pf.set_ranks(8, 12)
	assert_true(pf.current_rank_lv2_3 == 8, "set_ranks updates Lv2-3 to 8")
	assert_true(pf.current_rank_lv4 == 12, "set_ranks updates Lv4 to 12")
	
	# Freeze ranks
	pf.freeze_ranks = true
	pf.match_elapsed_time = 100.0
	pf._physics_process(0.1)
	assert_true(pf.current_rank_lv2_3 == 8, "Frozen rank not modified by match_elapsed_time")
	assert_true(pf.current_rank_lv4 == 12, "Frozen rank Lv4 not modified by match_elapsed_time")
	
	# Unfreeze ranks
	pf.freeze_ranks = false
	pf._physics_process(0.1)
	assert_true(pf.current_rank_lv2_3 == 11, "Unfrozen rank recalculates based on match time")
	
	pf.queue_free()

func test_debug_menu_rank_button_actions() -> void:
	print("Testing DebugMenu rank button actions and auto-freeze...")
	var pf_scene: PackedScene = preload("res://scenes/arena/playfield.tscn")
	var pf1: Playfield = pf_scene.instantiate() as Playfield
	pf1.setup(1, false, "reimu")
	root.add_child(pf1)
	
	var pf2: Playfield = pf_scene.instantiate() as Playfield
	pf2.setup(2, false, "marisa")
	root.add_child(pf2)
	
	var menu_scene: PackedScene = preload("res://scenes/ui/debug_menu.tscn")
	var menu = menu_scene.instantiate()
	root.add_child(menu)
	menu.notification(Node.NOTIFICATION_READY)
	menu.setup(null, pf1, pf2, null)
	menu.open()
	
	# Initial rank is 1 and not frozen
	assert_true(pf1.current_rank_lv2_3 == 1, "P1 initial rank is 1")
	assert_true(pf1.freeze_ranks == false, "P1 initially unfrozen")
	
	# User clicks Rank Lv2-3 plus button
	menu._modify_rank_lv23(1, 1)
	assert_true(pf1.current_rank_lv2_3 == 2, "P1 Rank Lv2-3 incremented to 2")
	assert_true(pf1.freeze_ranks == true, "Modifying rank auto-freezes progression")
	assert_true(menu.p1_freeze_rank_check.button_pressed == true, "Freeze checkbox is checked after modifying rank")
	assert_true(menu.p1_rank_lv23_label.text.contains("2"), "Menu label displays rank 2")
	
	# Physics process tick must NOT reset the rank
	pf1._physics_process(0.1)
	assert_true(pf1.current_rank_lv2_3 == 2, "Physics tick preserves manually set rank")
	
	# User clicks Preset R8
	menu._set_both_ranks(1, 8)
	assert_true(pf1.current_rank_lv2_3 == 8, "Preset R8 sets Lv2-3 to 8")
	assert_true(pf1.current_rank_lv4 == 8, "Preset R8 sets Lv4 to 8")
	pf1._physics_process(0.1)
	assert_true(pf1.current_rank_lv2_3 == 8, "Physics tick preserves Preset R8")
	
	# User clicks Preset R16
	menu._set_both_ranks(1, 16)
	assert_true(pf1.current_rank_lv2_3 == 16, "Preset R16 sets rank to 16")
	pf1._physics_process(0.1)
	assert_true(pf1.current_rank_lv2_3 == 16, "Physics tick preserves Preset R16")
	
	# User unfreezes rank progression
	menu._toggle_freeze_rank(1, false)
	assert_true(pf1.freeze_ranks == false, "Freeze toggled off")
	pf1._physics_process(0.1)
	# Because match_elapsed_time was aligned, it stays at 16 instead of dropping to 1
	assert_true(pf1.current_rank_lv2_3 == 16, "Unfrozen rank does not instantly drop to 1")
	
	menu.queue_free()
	pf1.queue_free()
	pf2.queue_free()

func test_playfield_spawner_toggles() -> void:
	print("Testing Playfield spawner toggles...")
	var pf_scene: PackedScene = preload("res://scenes/arena/playfield.tscn")
	var pf: Playfield = pf_scene.instantiate() as Playfield
	pf.setup(1, false, "reimu")
	root.add_child(pf)
	
	# Spirits
	assert_true(pf.spirit_spawning_enabled == true, "Spirits enabled by default")
	pf.spawn_spirit(Vector2(100, 100))
	var entities = pf.get_node("%Entities")
	var spirit_count: int = 0
	for child in entities.get_children():
		if child is Spirit:
			spirit_count += 1
	assert_true(spirit_count == 1, "Spirit spawned when enabled")
	
	pf.spirit_spawning_enabled = false
	pf.spawn_spirit(Vector2(200, 100))
	var spirit_count_after: int = 0
	for child in entities.get_children():
		if child is Spirit:
			spirit_count_after += 1
	assert_true(spirit_count_after == 1, "Spirit NOT spawned when spirit_spawning_enabled is false")
	
	# Pellets
	assert_true(pf.pellet_spawning_enabled == true, "Pellets enabled by default")
	pf.spawn_pellet(Vector2(100, 100))
	assert_true(pf.pellet_pool != null and pf.pellet_pool.get_active_count() == 1, "Pellet spawned when enabled")
	
	pf.pellet_spawning_enabled = false
	pf.spawn_pellet(Vector2(120, 100))
	assert_true(pf.pellet_pool.get_active_count() == 1, "Pellet NOT spawned when pellet_spawning_enabled is false")
	
	# Lily White
	assert_true(pf.lily_white_spawning_enabled == true, "Lily White enabled by default")
	var lily := pf.spawn_lily_white()
	assert_true(lily != null, "Lily White spawned when enabled")
	assert_true(pf.active_lily_white == lily, "Playfield tracks active_lily_white")
	
	# Disabling Lily White despawns active Lily White
	pf.lily_white_spawning_enabled = false
	assert_true(pf.active_lily_white == null, "Active Lily White despawned when lily_white_spawning_enabled set to false")
	var lily_blocked := pf.spawn_lily_white()
	assert_true(lily_blocked == null, "Lily White NOT spawned when lily_white_spawning_enabled is false")
	
	# Forced spawn still works
	var lily_forced := pf.spawn_lily_white(true)
	assert_true(lily_forced != null, "Lily White forced spawn succeeds even when disabled")
	pf.clear_all_enemies()
	
	pf.queue_free()

func test_playfield_board_wipes() -> void:
	print("Testing Playfield clear_all_bullets and clear_all_enemies...")
	var pf_scene: PackedScene = preload("res://scenes/arena/playfield.tscn")
	var pf: Playfield = pf_scene.instantiate() as Playfield
	pf.setup(1, false, "reimu")
	root.add_child(pf)
	
	pf.pellet_spawning_enabled = true
	pf.spawn_pellet(Vector2(100, 100))
	pf.spawn_pellet(Vector2(120, 100))
	assert_true(pf.pellet_pool.get_active_count() == 2, "2 active pellets before clear")
	
	pf.clear_all_bullets()
	assert_true(pf.pellet_pool.get_active_count() == 0, "0 active pellets after clear_all_bullets()")
	
	pf.spirit_spawning_enabled = true
	pf.spawn_spirit(Vector2(100, 100))
	var entities = pf.get_node("%Entities")
	var has_spirit := false
	for child in entities.get_children():
		if child is Spirit:
			has_spirit = true
	assert_true(has_spirit, "Spirit present before clear_all_enemies")
	
	pf.clear_all_enemies()
	# Check next tick or immediate queue_free
	var spirit_count := 0
	for child in entities.get_children():
		if child is Spirit and not child.is_queued_for_deletion():
			spirit_count += 1
	assert_true(spirit_count == 0, "Spirits removed after clear_all_enemies()")
	
	pf.queue_free()

func test_ai_dodge_only_mode() -> void:
	print("Testing RudimentaryAI dodge-only mode...")
	var ai := RudimentaryAI.new()
	assert_true(ai.can_shoot == true, "AI can_shoot is true by default")
	
	ai.set_shooting_enabled(false)
	assert_true(ai.can_shoot == false, "set_shooting_enabled(false) turns off shooting")
	assert_true(ai.wants_to_shoot() == false, "wants_to_shoot returns false in dodge-only mode")
	
	ai.set_shooting_enabled(true)
	assert_true(ai.can_shoot == true, "set_shooting_enabled(true) turns on shooting")

func test_debug_menu_lily_white_toggle() -> void:
	print("Testing DebugMenu Lily White checkbox toggle...")
	var pf1_scene: PackedScene = preload("res://scenes/arena/playfield.tscn")
	var pf1: Playfield = pf1_scene.instantiate() as Playfield
	pf1.setup(1, false, "reimu")
	root.add_child(pf1)
	
	var pf2: Playfield = pf1_scene.instantiate() as Playfield
	pf2.setup(2, false, "marisa")
	root.add_child(pf2)
	
	var menu_scene: PackedScene = preload("res://scenes/ui/debug_menu.tscn")
	var menu = menu_scene.instantiate()
	root.add_child(menu)
	menu.notification(Node.NOTIFICATION_READY)
	menu.setup(null, pf1, pf2, null)
	menu.open()
	
	assert_true(menu.spawner_lily_check != null, "SpawnerLilyCheck exists in DebugMenu")
	assert_true(menu.spawner_lily_check.button_pressed == true, "SpawnerLilyCheck default is pressed")
	assert_true(pf1.lily_white_spawning_enabled == true, "P1 Lily White initially enabled")
	assert_true(pf2.lily_white_spawning_enabled == true, "P2 Lily White initially enabled")
	
	# User unchecks Lily White
	menu._toggle_lily_spawning(false)
	assert_true(pf1.lily_white_spawning_enabled == false, "P1 Lily White disabled after uncheck")
	assert_true(pf2.lily_white_spawning_enabled == false, "P2 Lily White disabled after uncheck")
	
	# User checks Lily White again
	menu._toggle_lily_spawning(true)
	assert_true(pf1.lily_white_spawning_enabled == true, "P1 Lily White re-enabled after check")
	assert_true(pf2.lily_white_spawning_enabled == true, "P2 Lily White re-enabled after check")
	
	menu.queue_free()
	pf1.queue_free()
	pf2.queue_free()

