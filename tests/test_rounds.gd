extends SceneTree

var passed_count: int = 0
var failed_count: int = 0

func _init() -> void:
	print("\n===============================")
	print(" RUNNING BEST-OF-3 ROUNDS TESTS")
	print("===============================\n")
	
	test_game_manager_round_state()
	test_round_wins_display()
	test_screen_wipe_structure()
	test_scene_transition_wipe()
	test_playfield_round_reset()
	test_defeat_ko_sequence()
	test_node_pool_release_all()
	
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

func assert_false(condition: bool, test_name: String) -> void:
	assert_true(not condition, test_name)

func test_game_manager_round_state() -> void:
	print("Testing GameManager round state...")
	# Instantiate game_manager script directly for isolated test
	var gm_script := preload("res://scripts/global/game_manager.gd")
	var gm = gm_script.new()
	
	assert_true(gm.p1_round_wins == 0 and gm.p2_round_wins == 0, "GM starts with 0-0 round score")
	assert_true(gm.current_round == 1, "GM starts at round 1")
	assert_true(gm.is_match_over() == false, "Match is not over initially")
	
	var p1_w = gm.add_round_win(1)
	assert_true(p1_w == 1 and gm.p1_round_wins == 1, "P1 first round win recorded")
	assert_true(gm.is_match_over() == false, "Match not over at 1-0")
	
	var p2_w = gm.add_round_win(2)
	assert_true(p2_w == 1 and gm.p2_round_wins == 1, "P2 round win recorded")
	assert_true(gm.is_match_over() == false, "Match not over at 1-1")
	
	p1_w = gm.add_round_win(1)
	assert_true(p1_w == 2 and gm.p1_round_wins == 2, "P1 second round win recorded")
	assert_true(gm.is_match_over() == true, "Match is over at 2-1")
	assert_true(gm.get_match_winner() == 1, "P1 is match winner")
	
	gm.reset_match_rounds()
	assert_true(gm.p1_round_wins == 0 and gm.p2_round_wins == 0, "Reset restores 0-0 score")
	assert_true(gm.current_round == 1, "Reset restores current_round = 1")
	assert_true(gm.is_match_over() == false, "Match is not over after reset")

func test_round_wins_display() -> void:
	print("Testing RoundWinsDisplay...")
	var rwd_scene := preload("res://scenes/ui/round_wins_display.tscn")
	var rwd = rwd_scene.instantiate() as RoundWinsDisplay
	root.add_child(rwd)
	rwd._ready()
	
	assert_true(rwd.slot0 != null and rwd.slot1 != null, "RoundWinsDisplay has both flower slots")
	assert_true(rwd.slot0.texture != null, "Sakura flower texture is loaded on slot 0")
	
	# Initial state: 0 wins
	rwd.set_wins(0, false)
	assert_true(rwd.slot0.modulate.a < 0.5 and rwd.slot1.modulate.a < 0.5, "0 wins: both slots dimmed silhouette")
	
	# 1 win
	rwd.set_wins(1, false)
	assert_true(rwd.slot0.modulate.a > 0.9, "1 win: slot 0 active")
	assert_true(rwd.slot1.modulate.a < 0.5, "1 win: slot 1 dimmed silhouette")
	
	# Rotation in process
	var initial_rot: float = rwd.slot0.rotation
	rwd._process(0.2)
	assert_true(rwd.slot0.rotation > initial_rot, "Active slot 0 rotates over time")
	assert_true(rwd.slot1.rotation == 0.0, "Inactive slot 1 does not rotate")
	
	# 2 wins
	rwd.set_wins(2, false)
	assert_true(rwd.slot0.modulate.a > 0.9 and rwd.slot1.modulate.a > 0.9, "2 wins: both slots active")
	
	rwd.queue_free()

func test_screen_wipe_structure() -> void:
	print("Testing ScreenWipe structure...")
	var wipe_scene := preload("res://scenes/effects/screen_wipe.tscn")
	var wipe = wipe_scene.instantiate() as ScreenWipe
	root.add_child(wipe)
	wipe._ready()
	
	assert_true(wipe.left_panel != null, "LeftPanel exists")
	assert_true(wipe.right_panel != null, "RightPanel exists")
	assert_true(wipe.ready_label != null, "ReadyLabel exists")
	assert_true(wipe.left_panel.position.x == -600.0, "LeftPanel default start position is offscreen left (-600)")
	assert_true(wipe.right_panel.position.x == 600.0, "RightPanel default start position is offscreen right (+600)")
	
	wipe.queue_free()

func test_scene_transition_wipe() -> void:
	print("Testing SceneTransition structure...")
	var trans_scene := preload("res://scenes/effects/scene_transition.tscn")
	var trans = trans_scene.instantiate() as SceneTransition
	root.add_child(trans)
	trans._ready()
	
	assert_true(trans.left_panel != null, "SceneTransition LeftPanel exists")
	assert_true(trans.right_panel != null, "SceneTransition RightPanel exists")
	assert_true(trans.left_panel.position.x == -trans.PANEL_WIDTH, "LeftPanel default start position is offscreen left")
	assert_true(trans.right_panel.position.x == 1920.0, "RightPanel default start position is offscreen right (1920)")
	assert_true(trans.layer == 120, "SceneTransition layer is 120 (above UI)")
	
	trans.queue_free()

func test_playfield_round_reset() -> void:
	print("Testing Playfield.reset_for_new_round()...")
	var pf_scene := preload("res://scenes/arena/playfield.tscn")
	var pf = pf_scene.instantiate() as Playfield
	root.add_child(pf)
	pf.setup(1, true, "reimu")
	
	# Simulate taking damage, building combo, gaining passive charge, and advancing time
	assert_true(pf.player != null, "Player spawned in playfield")
	pf.player.take_damage(2.0)
	pf.player.position = Vector2(100.0, 200.0)
	pf.player.velocity = Vector2(50.0, -100.0)
	pf.current_combo = 8
	pf.match_elapsed_time = 45.0
	pf.current_rank_lv2_3 = 5
	pf.player.passive_charge = 2.5
	
	# Spawn a test fairy in entities_layer to ensure it gets cleared
	var dummy_fairy := Area2D.new()
	pf.entities_layer.add_child(dummy_fairy)
	
	# Perform round reset (preserving match progression)
	pf.reset_for_new_round(true)
	assert_true(dummy_fairy.is_queued_for_deletion(), "Round reset queued lingering fairies for deletion")
	assert_false(dummy_fairy.visible, "Round reset immediately hid lingering fairies")
	assert_true(pf.player.current_health == pf.player.max_health, "Reset restored player to full health")
	assert_true(pf.player.position == pf.get_intro_spawn_position(), "Reset restored player to intro corner spawn location")
	assert_true(pf.player.velocity == Vector2.ZERO, "Reset cleared player velocity")
	assert_true(not pf.player.is_dead, "Reset cleared is_dead")
	assert_true(pf.player.is_shoved == false, "Reset cleared is_shoved")
	assert_true(pf.player.is_invulnerable == false, "Reset cleared is_invulnerable")
	assert_true(pf.current_combo == 0, "Reset cleared combo")
	assert_true(pf.player.passive_charge == 2.5, "Round reset preserved player passive spell charge")
	assert_true(pf.current_rank_lv2_3 == 5, "Round reset preserved rank progression")
	assert_true(pf.match_elapsed_time == 45.0, "Round reset preserved match elapsed time")
	
	# Test start_round_intro glide
	pf.start_round_intro()
	assert_true(pf.player.is_intro_gliding, "Player is in intro gliding state")
	assert_true(pf.player.is_invulnerable, "Player is invulnerable during intro glide")
	assert_true(pf.player._anim_row == 2, "Player 1 banks right (row 2) while gliding from left corner")
	
	# Conclude glide
	pf.player._on_intro_glide_finished()
	assert_true(not pf.player.is_intro_gliding, "Intro glide flag cleared on completion")
	assert_true(not pf.player.is_invulnerable, "Invulnerability cleared when intro glide completes")
	assert_true(pf.player._anim_row == 0, "Sprite settles to neutral idle (row 0) after glide")
	
	# Perform full match reset
	pf.reset_for_new_match()
	assert_true(pf.player.passive_charge == 1.0, "Match reset restored default passive charge")
	assert_true(pf.current_rank_lv2_3 == 1, "Match reset restored Rank 1")
	assert_true(pf.match_elapsed_time == 0.0, "Match reset cleared match elapsed time")
	
	pf.queue_free()

func test_node_pool_release_all() -> void:
	print("Testing NodePool.release_all_active()...")
	var parent := Node2D.new()
	root.add_child(parent)
	
	var pellet_scene := preload("res://scenes/bullets/enemy_pellet.tscn")
	var pool := NodePool.new(pellet_scene, parent, 10)
	
	# Acquire 5 pellets
	var active_nodes: Array = []
	for i in range(5):
		var n = pool.acquire()
		active_nodes.append(n)
		assert_true(n.visible == true, "Acquired node is visible")
	
	# Call release_all_active
	pool.release_all_active()
	for n in active_nodes:
		assert_true(n.visible == false, "Released node is deactivated / hidden")
	
	pool.clear()
	parent.queue_free()

func test_defeat_ko_sequence() -> void:
	print("Testing Defeat KO sequence components...")
	# 1. Test DefeatShockwave
	var sw_scene := preload("res://scenes/effects/defeat_shockwave.tscn")
	var sw = sw_scene.instantiate()
	root.add_child(sw)
	sw.setup(Vector2(300.0, 500.0))
	assert_true(sw.position == Vector2(300.0, 500.0), "DefeatShockwave sets spawn position")
	assert_true(sw.process_mode == Node.PROCESS_MODE_ALWAYS, "DefeatShockwave processes even when board is frozen")
	assert_true(sw.has_node("Sprite2D"), "DefeatShockwave has Sprite2D")
	sw.queue_free()
	
	# 2. Test Playfield defeat methods
	var pf_scene := preload("res://scenes/arena/playfield.tscn")
	var pf = pf_scene.instantiate() as Playfield
	root.add_child(pf)
	pf.setup(1, true, "reimu")
	
	assert_true(pf.has_node("%DefeatFlash"), "Playfield has DefeatFlash node")
	pf.play_defeat_flash()
	assert_true(pf.defeat_flash != null and pf.defeat_flash.modulate.a > 0.0, "play_defeat_flash activates red flash overlay")
	
	pf.spawn_defeat_shockwave(Vector2(200.0, 300.0))
	assert_true(pf.effects_layer.get_child_count() > 0, "spawn_defeat_shockwave added child to effects_layer")
	
	# 3. Test Player hide_on_defeat
	assert_true(pf.player != null, "Player exists")
	pf.player.hide_on_defeat()
	assert_true(pf.player.sprite != null and pf.player.sprite.visible == false, "hide_on_defeat hides player sprite")
	assert_true(pf.player.sprite.modulate.a == 0.0, "hide_on_defeat zeros sprite alpha")
	
	pf.queue_free()
