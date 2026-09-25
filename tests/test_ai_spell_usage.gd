extends SceneTree

# Test suite for AI Spell Bar autonomous charging and spellcard execution
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
	print(" RUNNING AI SPELL BAR & CHARGING UNIT TESTS ")
	print("============================================\n")
	
	test_root = Node2D.new()
	test_root.name = "TestRoot"
	root.add_child(test_root)
	
	test_ai_spell_queue_selection()
	test_ai_gated_charging()
	test_ai_charge_release_on_target()
	test_ai_emergency_release_on_threat()
	test_ai_spells_toggle()
	
	print("\n--------------------------------------------")
	print("TEST RESULTS: %d Passed, %d Failed" % [passed_count, failed_count])
	print("--------------------------------------------\n")
	
	test_root.queue_free()
	quit(0 if failed_count == 0 else 1)

func _create_test_environment() -> Dictionary:
	var pf_scene := preload("res://scenes/arena/playfield.tscn")
	var pf = pf_scene.instantiate() as Playfield
	test_root.add_child(pf)
	pf.setup(2, false, "reimu")
	
	var player = pf.player
	player.set_ai_mode(true)
	var ai = player.ai_controller as RudimentaryAI
	
	return {
		"playfield": pf,
		"player": player,
		"ai": ai
	}

func test_ai_spell_queue_selection() -> void:
	print("Testing AI initial spell queue selection...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	
	assert_true(ai != null, "AI controller successfully attached to player")
	assert_true(ai.queued_spell_level >= 2 and ai.queued_spell_level <= 4, "Initial queued spell level is between Lv 2 and Lv 4: %d" % ai.queued_spell_level)
	
	# Test random selection range across multiple rolls
	var levels_seen: Dictionary = {2: false, 3: false, 4: false}
	for i in range(50):
		var lvl = ai._pick_random_spell_level()
		assert_true(lvl in [2, 3, 4], "Rolled level is valid (2, 3, or 4): %d" % lvl)
		levels_seen[lvl] = true
	assert_true(levels_seen[2] and levels_seen[3], "RNG successfully covers multiple spell levels")
	
	env.playfield.queue_free()

func test_ai_gated_charging() -> void:
	print("Testing AI charge gating by passive gauge...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	
	ai._charge_cooldown = 0.0
	ai.queued_spell_level = 3
	player.passive_charge = 2.0 # Below queued Lv 3!
	
	ai.update_ai(0.016)
	assert_false(ai.wants_to_charge(), "AI does not charge when passive gauge (2.0) is below queued level (3)")
	
	# Increase passive gauge to meet requirement
	player.passive_charge = 3.2
	ai.update_ai(0.016)
	assert_true(ai.wants_to_charge(), "AI begins charging once passive gauge (3.2) satisfies queued level (3)")
	
	env.playfield.queue_free()

func test_ai_charge_release_on_target() -> void:
	print("Testing AI charge release when target active charge reached...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	
	ai._charge_cooldown = 0.0
	ai.queued_spell_level = 2
	player.passive_charge = 2.5
	
	ai.update_ai(0.016)
	assert_true(ai.wants_to_charge(), "AI starts charging towards target Lv 2")
	
	# Advance active charge to reach target
	player.active_charge = 2.05
	ai.update_ai(0.016)
	assert_false(ai.wants_to_charge(), "AI stops charging / releases charge once active charge reaches target Lv 2")
	assert_true(ai._charge_cooldown > 0.0, "AI sets charge cooldown after releasing spell")
	assert_true(ai.queued_spell_level in [2, 3, 4], "AI rolls a new random target spell level after release")
	
	env.playfield.queue_free()

func test_ai_emergency_release_on_threat() -> void:
	print("Testing AI emergency release when threatened by bullets...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	ai._charge_cooldown = 0.0
	ai.queued_spell_level = 3
	player.passive_charge = 3.5
	
	ai.update_ai(0.016)
	assert_true(ai.wants_to_charge(), "AI starts charging towards Lv 3")
	
	# Simulate charge built up to Lv 1.5, then incoming projectile appears ahead
	# AI should continue holding charge (never aborting into a weak Lv 1 charge attack)
	player.active_charge = 1.5
	pf.spawn_pellet(Vector2(player.position.x, player.position.y - 70.0))
	
	ai.update_ai(0.016)
	assert_true(ai.wants_to_charge(), "AI holds charge through Lv 1.5 without aborting into a weak charge attack")
	
	# Once charge reaches at least Lv 2.0, imminent threat triggers emergency release into a Lv 2 spellcard shockwave
	player.active_charge = 2.2
	ai.update_ai(0.016)
	assert_false(ai.wants_to_charge(), "AI emergency-releases into Lv 2+ spellcard when imminent threat arrives")
	
	env.playfield.queue_free()

func test_ai_spells_toggle() -> void:
	print("Testing AI spell capability toggle...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	
	ai._charge_cooldown = 0.0
	ai.queued_spell_level = 2
	player.passive_charge = 3.0
	
	ai.set_spells_enabled(false)
	ai.update_ai(0.016)
	assert_false(ai.wants_to_charge(), "AI does not charge when set_spells_enabled(false)")
	
	ai.set_spells_enabled(true)
	ai.update_ai(0.016)
	assert_true(ai.wants_to_charge(), "AI charges when set_spells_enabled(true)")
	
	env.playfield.queue_free()
