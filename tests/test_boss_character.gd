extends SceneTree

## Unit & integration test for Level 4 Boss Character mechanics.

const DanmakuExtraAttackStepScript = preload("res://scripts/resources/danmaku_extra_attack_step.gd")
const DanmakuClawStepScript = preload("res://scripts/resources/danmaku_claw_step.gd")
const DanmakuDoubleRingStepScript = preload("res://scripts/resources/danmaku_double_ring_step.gd")
const DanmakuIcicleShowerStepScript = preload("res://scripts/resources/danmaku_icicle_shower_step.gd")

func _init() -> void:
	_run()

func _run() -> void:
	print("--- TEST: BOSS CHARACTER & LEVEL 4 MECHANIC START ---")
	
	# 1. Verify BossData resources
	var reimu_boss: BossData = load("res://resources/bosses/reimu_boss.tres")
	assert(reimu_boss != null, "reimu_boss.tres must load")
	assert(reimu_boss.character_id == "reimu", "reimu boss character_id must be reimu")
	assert(reimu_boss.max_health == 180.0, "reimu boss max_health must be 180.0")
	assert(reimu_boss.duration_safety == 24.0, "reimu boss duration_safety must be 24.0s")
	assert(reimu_boss.departure_mode == BossData.DepartureMode.ATTACK_COUNT, "reimu boss departure_mode must be ATTACK_COUNT")
	assert(reimu_boss.attacks_min == 5, "reimu boss attacks_min must be 5")
	assert(reimu_boss.attacks_max == 9, "reimu boss attacks_max must be 9")
	assert(reimu_boss.randomize_attack_count == true, "reimu boss randomize_attack_count must be true")
	assert(reimu_boss.selection_mode == BossData.AttackSelectionMode.RANDOM, "reimu boss selection_mode must be RANDOM")
	assert(reimu_boss.entrance_target_pos == Vector2(300.0, 150.0), "target hover pos must be (300, 150)")
	assert(reimu_boss.roam_bounds == Rect2(140.0, 110.0, 320.0, 100.0), "roam bounds must be (140, 110, 320, 100)")
	assert(_recipe_matches(reimu_boss), "reimu boss must have a boss sheet recipe matching her frames")
	assert(reimu_boss.hframes == 4, "reimu boss hframes must be 4")
	assert(reimu_boss.vframes == 3, "reimu boss vframes must be 3")
	assert(reimu_boss.sprite_scale == Vector2(2.33, 2.33), "reimu boss sprite_scale must be Vector2(2.33, 2.33)")
	assert(reimu_boss.hop_duration == 0.65, "reimu boss hop_duration must be 0.65s")
	assert(reimu_boss.hop_distance_min == 90.0, "reimu boss hop_distance_min must be 90.0")
	assert(reimu_boss.hop_distance_max == 160.0, "reimu boss hop_distance_max must be 160.0")
	print("PASS: Reimu BossData resource verified (departure_mode=ATTACK_COUNT, 5->8 attacks, RANDOM)")
	
	var marisa_boss: BossData = load("res://resources/bosses/marisa_boss.tres")
	assert(marisa_boss != null, "marisa_boss.tres must load")
	assert(marisa_boss.character_id == "marisa", "marisa boss character_id must be marisa")
	assert(marisa_boss.max_health == 180.0, "marisa boss max_health must be 180.0")
	assert(_recipe_matches(marisa_boss), "marisa boss must have a boss sheet recipe matching her frames")
	assert(marisa_boss.hframes == 4, "marisa boss hframes must be 4")
	assert(marisa_boss.vframes == 3, "marisa boss vframes must be 3")
	assert(marisa_boss.sprite_scale == Vector2(2.33, 2.33), "marisa boss sprite_scale must be Vector2(2.33, 2.33)")
	print("PASS: Marisa BossData resource verified (4x3 frames, 2.33x scale)")
	
	var youmu_boss: BossData = load("res://resources/bosses/youmu_boss.tres")
	assert(youmu_boss != null, "youmu_boss.tres must load")
	assert(youmu_boss.character_id == "youmu", "youmu boss character_id must be youmu")
	assert(youmu_boss.boss_name == "Youmu Konpaku", "youmu boss name must be Youmu Konpaku")
	assert(youmu_boss.hframes == 5, "youmu boss hframes must be 5")
	assert(youmu_boss.vframes == 3, "youmu boss vframes must be 3")
	assert(youmu_boss.duration_safety >= 30.0, "youmu boss duration_safety must be >= 30.0")
	assert(youmu_boss.attacks_min >= 6, "youmu boss attacks_min must be >= 6")
	assert(youmu_boss.attacks_max >= 10, "youmu boss attacks_max must be >= 10")
	assert(_recipe_matches(youmu_boss), "youmu boss must have a boss sheet recipe matching her frames")
	print("PASS: Youmu BossData resource verified (5x3 frames, attacks_min >= 6)")
	
	# 2. Verify CharacterData linkage
	var reimu_data: CharacterData = load("res://resources/characters/reimu.tres")
	assert(reimu_data.boss_data != null, "reimu.tres must have boss_data linked")
	assert(reimu_data.boss_data.character_id == "reimu", "reimu boss_data id must match")
	
	var marisa_data: CharacterData = load("res://resources/characters/marisa.tres")
	assert(marisa_data.boss_data != null, "marisa.tres must have boss_data linked")
	assert(marisa_data.boss_data.character_id == "marisa", "marisa boss_data id must match")
	
	var youmu_data: CharacterData = load("res://resources/characters/youmu.tres")
	assert(youmu_data.boss_data != null, "youmu.tres must have boss_data linked")
	assert(youmu_data.boss_data.character_id == "youmu", "youmu boss_data id must match")
	print("PASS: CharacterData boss_data linkages verified (Reimu, Marisa, Youmu)")
	
	# 3. BossCharacter Instantiation & Entrance
	var boss_scene: PackedScene = load("res://scenes/enemies/boss_character.tscn")
	assert(boss_scene != null, "boss_character.tscn must load")
	var boss: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(boss)
	boss._ready()
	
	boss.setup(reimu_boss, 1)
	assert(boss.state == BossCharacter.State.ENTERING, "Boss must start in ENTERING state")
	assert(boss.is_vulnerable == false, "Boss must be invulnerable during entrance")
	assert(boss.monitorable == false, "Boss must not be monitorable during entrance")
	
	var expected_start_pos := reimu_boss.entrance_target_pos + reimu_boss.entrance_start_offset
	assert(boss.position == expected_start_pos, "Boss must start at offscreen entrance pos")
	assert(boss.sprite.hframes == 4, "Boss sprite hframes must be 4")
	assert(boss.sprite.vframes == 3, "Boss sprite vframes must be 3")
	assert(boss.sprite.scale == Vector2(2.33, 2.33), "Boss sprite scale must be Vector2(2.33, 2.33)")
	assert(boss._frames_per_row == 4, "Boss _frames_per_row must be 4")
	print("PASS: BossCharacter entrance setup and start position (%s) verified" % boss.position)
	
	# 4. Entrance Completion & State Transition
	boss._on_entrance_completed()
	assert(boss.state == BossCharacter.State.ACTIVE, "Boss must transition to ACTIVE after entrance")
	assert(boss.is_vulnerable == true, "Boss must be vulnerable when active")
	assert(boss.monitorable == true, "Boss must be monitorable when active")
	assert(boss._anim_row == 0, "Boss animation must be row 0 (idle) when active")
	print("PASS: Boss entrance completion and active state verified")
	
	# 5. Hopping Logic & Bound Clamping
	for i in range(10):
		boss._perform_hop()
		assert(boss.state == BossCharacter.State.HOPPING, "Boss state must be HOPPING during hop")
		
		# Verify bounds containment
		var bounds := reimu_boss.roam_bounds
		assert(boss._base_position.x >= bounds.position.x and boss._base_position.x <= bounds.end.x,
			"Hop target X (%f) must be within roam bounds [%f, %f]" % [boss._base_position.x, bounds.position.x, bounds.end.x])
		assert(boss._base_position.y >= bounds.position.y and boss._base_position.y <= bounds.end.y,
			"Hop target Y (%f) must be within roam bounds [%f, %f]" % [boss._base_position.y, bounds.position.y, bounds.end.y])
		
		# Complete hop
		boss._on_hop_completed()
		assert(boss.state == BossCharacter.State.ACTIVE, "Boss state must return to ACTIVE after hop")
		assert(boss._anim_row == 0, "Boss animation row must reset to 0 after hop")
	print("PASS: 10 consecutive hops verified strictly within roam bounds and state transitions")
	
	# 5b. Non-looping banking animation test
	boss.state = BossCharacter.State.HOPPING
	boss._set_anim_state(1, false)
	assert(boss._anim_row == 1, "Must be on Row 1 (banking)")
	assert(boss._anim_frame == 0, "Must start at frame 0")
	# Step through 10 frame durations (far exceeding row length of 4)
	for step in range(10):
		boss._physics_process(boss._frame_duration)
	assert(boss._anim_frame == boss._frames_per_row - 1,
		"Banking animation must hold on last frame (index 3), not loop back (was %d)" % boss._anim_frame)
	print("PASS: Banking animation holds on finished state without looping")
	
	# Transition back to idle
	boss.state = BossCharacter.State.ACTIVE
	boss._attack_timer = 999.0 # Prevent attack preempting idle frame loop test
	boss._set_anim_state(0, false)
	assert(boss._anim_row == 0, "Must return to Row 0 (idle)")
	assert(boss._anim_frame == 0, "Idle must reset to frame 0")
	# Step through frames: should loop continuously
	for step in range(boss._frames_per_row):
		boss._physics_process(boss._frame_duration)
	assert(boss._anim_frame == 0, "Idle animation must loop back to frame 0")
	print("PASS: Idle animation loops continuously")
	
	# 6. Combat & Damage Calibration (~7s Marisa fire)
	var hit_signal_fired := [false]
	var last_damage := [0.0]
	var last_remaining_hp := [0.0]
	boss.hit.connect(func(dmg: float, rem: float):
		hit_signal_fired[0] = true
		last_damage[0] = dmg
		last_remaining_hp[0] = rem
	)
	
	var damaged: bool = boss.take_damage(26.7) # ~1 second of Marisa fire
	assert(damaged == true, "Boss should accept damage when active")
	assert(hit_signal_fired[0] == true, "Boss should emit hit signal")
	assert(is_equal_approx(boss.current_health, 180.0 - 26.7), "Health should decrease by 26.7")
	print("PASS: take_damage() correctly registers damage and emits signal (HP: %f)" % boss.current_health)
	
	# 7. Defeat Handling (~7s continuous fire)
	var defeated_signal_fired := [false]
	boss.defeated.connect(func(_pos: Vector2, _src: String):
		defeated_signal_fired[0] = true
	)
	boss.take_damage(200.0)
	assert(boss.current_health == 0.0, "Health must be 0 after lethal damage")
	assert(boss.is_dead == true, "Boss must be flagged dead")
	assert(boss.state == BossCharacter.State.DEFEATED, "State must be DEFEATED")
	assert(defeated_signal_fired[0] == true, "Defeated signal must have fired")
	print("PASS: Defeat handling and signal emission verified")
	
	# 8. Cancel-out / Dispel Test (Counter Level 4 Spellcard)
	var boss2: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(boss2)
	boss2.setup(marisa_boss, 1)
	boss2._on_entrance_completed()
	
	var dispelled_signal_fired := [false]
	boss2.dispelled.connect(func():
		dispelled_signal_fired[0] = true
	)
	boss2.dispel()
	assert(boss2.state == BossCharacter.State.DISPELLED, "Boss state must be DISPELLED")
	assert(boss2.is_vulnerable == false, "Dispelled boss must be invulnerable")
	assert(boss2.monitorable == false, "Dispelled boss must not be monitorable")
	assert(dispelled_signal_fired[0] == true, "Dispelled signal must have fired")
	print("PASS: Dispel / cancel-out mechanism verified")
	
	# 9. Natural Exit / Leave Screen Test
	var boss3: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(boss3)
	boss3.setup(reimu_boss, 1)
	boss3._on_entrance_completed()
	
	var left_screen_fired := [false]
	boss3.left_screen.connect(func():
		left_screen_fired[0] = true
	)
	boss3.leave_screen()
	assert(boss3.state == BossCharacter.State.LEAVING, "Boss state must be LEAVING")
	assert(boss3.is_vulnerable == false, "Leaving boss must be invulnerable")
	assert(left_screen_fired[0] == true, "Left screen signal must fire upon timeout")
	print("PASS: Timeout / leave_screen mechanism verified (explodes in place, emits left_screen)")
	
	# 10. Marisa BossCharacter Instantiation & Setup
	var marisa_entity: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(marisa_entity)
	marisa_entity._ready()
	marisa_entity.setup(marisa_boss, 1)
	assert(marisa_entity.sprite.texture == marisa_boss.sprite_sheet, "Marisa boss entity must use her boss sheet")
	assert(marisa_entity.sprite.hframes == 4, "Marisa boss entity hframes must be 4")
	assert(marisa_entity.sprite.vframes == 3, "Marisa boss entity vframes must be 3")
	assert(marisa_entity.sprite.scale == Vector2(2.33, 2.33), "Marisa boss entity scale must be Vector2(2.33, 2.33)")
	marisa_entity._on_entrance_completed()
	assert(marisa_entity.state == BossCharacter.State.ACTIVE, "Marisa boss entity must transition to ACTIVE")
	marisa_entity.queue_free()
	print("PASS: Marisa BossCharacter instantiation, texture, and active state verified")
	
	# 11. Reimu Boss Attack 1: Two Rings Homing Talisman
	assert(reimu_boss.attack_patterns.size() >= 1, "Reimu boss must have attack patterns configured")
	var reimu_spell1: SpellcardData = reimu_boss.attack_patterns[0]
	assert(reimu_spell1 != null, "reimu_boss_spell_1 must not be null")
	assert(reimu_spell1.spellcard_id == "reimu_boss_spell_1", "Spellcard id must be reimu_boss_spell_1")
	assert(reimu_spell1.steps.size() == 2, "reimu_boss_spell_1 must have 2 steps (inner & outer ring)")
	
	var step_inner: DanmakuRingStep = reimu_spell1.steps[0] as DanmakuRingStep
	var step_outer: DanmakuRingStep = reimu_spell1.steps[1] as DanmakuRingStep
	assert(step_inner != null and step_outer != null, "Both steps must be DanmakuRingStep")
	assert(step_inner.bullet_data.resource_path.ends_with("white_talisman.tres"), "Inner ring must use white_talisman (pl00.ecl sub4 fires White)")
	assert(step_outer.bullet_data.resource_path.ends_with("white_talisman.tres"), "Outer ring must use white_talisman (pl00.ecl sub4 fires White)")
	assert(step_inner.motion_mode == DanmakuBullet.MotionMode.DECEL_AND_HOME, "Inner ring must be DECEL_AND_HOME")
	assert(step_outer.motion_mode == DanmakuBullet.MotionMode.DECEL_AND_HOME, "Outer ring must be DECEL_AND_HOME")
	assert(step_inner.homing_decel_time == step_outer.homing_decel_time, "Inner and outer rings must share identical decel time")
	assert(step_inner.homing_decel_time == 1.5, "Decel time must be 1.5s (90 frames, pl00.ecl sub4)")
	assert(step_outer.stagger_half_step == false, "Outer ring is a second speed layer on the same angles, not a half step")
	
	# Verify Rank 1 scaling (42 inner + 42 outer = 84, ZUN 2 x rank + 40)
	var t_rank1: float = 0.0
	var count_inner_r1: int = int(round(lerpf(float(step_inner.count_min), float(step_inner.count_max), t_rank1)))
	var count_outer_r1: int = int(round(lerpf(float(step_outer.count_min), float(step_outer.count_max), t_rank1)))
	assert(count_inner_r1 == 42, "Inner ring Rank 1 count must be 42")
	assert(count_outer_r1 == 42, "Outer ring Rank 1 count must be 42")
	assert(count_inner_r1 + count_outer_r1 == 84, "Rank 1 total talismans must equal 84 (42 per ring)")
	
	# Verify Rank 16 scaling (72 inner + 72 outer = 144)
	var t_rank16: float = 1.0
	var count_inner_r16: int = int(round(lerpf(float(step_inner.count_min), float(step_inner.count_max), t_rank16)))
	var count_outer_r16: int = int(round(lerpf(float(step_outer.count_min), float(step_outer.count_max), t_rank16)))
	assert(count_inner_r16 == 72, "Inner ring Rank 16 count must be 72")
	assert(count_outer_r16 == 72, "Outer ring Rank 16 count must be 72")
	assert(count_inner_r16 + count_outer_r16 == 144, "Rank 16 total talismans must equal 144 (72 per ring)")
	print("PASS: Two Rings Homing Talisman configuration & Rank scaling verified (Rank 1: 42/ring -> 84 total, Rank 16: 72/ring -> 144 total)")
	
	# Test BossCharacter attack firing routine and animation state transitions
	var mock_playfield := MockBossPlayfield.new()
	root.add_child(mock_playfield)
	
	# Section 11: Verify Boss attack triggering, signal, Row 2 cast animation, and Rank 1 execution
	var test_boss_data: BossData = reimu_boss.duplicate()
	test_boss_data.selection_mode = BossData.AttackSelectionMode.SEQUENTIAL
	test_boss_data.attack_patterns = [load("res://resources/spellcards/reimu/reimu_boss_spell_1.tres")]
	
	var boss4: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(boss4)
	boss4._ready()
	boss4.setup(test_boss_data, 1, mock_playfield)
	boss4._on_entrance_completed()
	
	assert(boss4._attack_timer == reimu_boss.initial_attack_delay, "Initial attack delay must match initial_attack_delay")
	assert(boss4._target_attack_count >= 5 and boss4._target_attack_count <= 9, "Randomized target attack count must be within [5, 9]")
	
	var attack_started_fired := [false, ""]
	boss4.attack_started.connect(func(s_name: String):
		attack_started_fired[0] = true
		attack_started_fired[1] = s_name
	)
	
	# Trigger attack
	boss4._trigger_attack()
	assert(attack_started_fired[0] == true, "attack_started signal must fire")
	assert(attack_started_fired[1] == "Two Rings Homing Talisman", "attack_started name must match")
	assert(boss4._is_casting == true, "Boss must be in casting state")
	assert(boss4._anim_row == 2, "Boss must enter Row 2 (cast pose)")
	assert(mock_playfield.spawned_count == 84, "Mock playfield must receive exactly 84 bullets at Rank 1 (42 per ring)")
	assert(boss4._attack_timer == test_boss_data.attack_rate, "Attack timer must reset to attack_rate (1.6s)")
	assert(boss4._attacks_performed == 1, "Attacks performed must be incremented to 1")
	
	# End cast
	boss4._on_cast_finished()
	assert(boss4._is_casting == false, "Boss must exit casting state")
	assert(boss4._anim_row == 0, "Boss must return to Row 0 (idle hover)")
	print("PASS: Boss attack triggering, signal, Row 2 cast animation, and Rank 1 execution verified")
	
	# Test Rank 16 execution count
	var mock_playfield16 := MockBossPlayfield.new()
	root.add_child(mock_playfield16)
	var boss5: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(boss5)
	boss5._ready()
	boss5.setup(test_boss_data, 16, mock_playfield16)
	boss5._on_entrance_completed()
	assert(boss5._target_attack_count >= 5 and boss5._target_attack_count <= 9, "Randomized target attack count must be within [5, 9]")
	boss5._trigger_attack()
	assert(mock_playfield16.spawned_count == 144, "Mock playfield must receive exactly 144 bullets at Rank 16 (72 per ring)")
	print("PASS: Rank 16 boss attack execution (144 talismans, target in [5, 9] attacks) verified")
	
	# Verify deterministic rank-scaling when randomize_attack_count = false
	var det_data: BossData = reimu_boss.duplicate()
	det_data.randomize_attack_count = false
	var boss_det1: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(boss_det1)
	boss_det1._ready()
	boss_det1.setup(det_data, 1, mock_playfield)
	boss_det1._on_entrance_completed()
	assert(boss_det1._target_attack_count == 5, "Deterministic Rank 1 must equal 5 attacks")
	
	var boss_det16: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(boss_det16)
	boss_det16._ready()
	boss_det16.setup(det_data, 16, mock_playfield)
	boss_det16._on_entrance_completed()
	assert(boss_det16._target_attack_count == 9, "Deterministic Rank 16 must equal 9 attacks")
	
	boss_det1.queue_free()
	boss_det16.queue_free()
	
	boss4.queue_free()
	boss5.queue_free()
	mock_playfield.queue_free()
	mock_playfield16.queue_free()
	
	# Clean up
	boss.queue_free()
	boss2.queue_free()
	boss3.queue_free()
	
	# Section 12: Verify Extra Attack Barrage (reimu_boss_spell_2.tres) and pattern cycling
	var spell2: SpellcardData = load("res://resources/spellcards/reimu/reimu_boss_spell_2.tres")
	assert(spell2 != null, "reimu_boss_spell_2.tres must exist")
	assert(spell2.spellcard_id == "reimu_boss_spell_2", "Spellcard ID must match")
	assert(spell2.spellcard_name == "Extra Attack Barrage", "Spellcard name must match")
	assert(spell2.steps.size() == 1, "Spellcard must have 1 step")
	var extra_step: Resource = spell2.steps[0]
	assert(extra_step != null, "Step must exist")
	assert(extra_step.get_script() == DanmakuExtraAttackStepScript, "Step must be DanmakuExtraAttackStep")
	assert(extra_step.count_min == 4, "Rank 1 count must be 4")
	assert(extra_step.count_max == 9, "Rank 16 count must be 9")
	assert(extra_step.delay_between_spawns == 0.1667, "Spawn delay must be 0.1667s")
	assert(extra_step.initial_upward_speed == 390.0, "Initial upward speed must be 390.0")
	
	# Verify Rank scaling math
	var count_r1: int = int(round(lerpf(float(extra_step.count_min), float(extra_step.count_max), 0.0)))
	var count_r16: int = int(round(lerpf(float(extra_step.count_min), float(extra_step.count_max), 1.0)))
	assert(count_r1 == 4, "Rank 1 must evaluate to 4 orbs")
	assert(count_r16 == 9, "Rank 16 must evaluate to 9 orbs")
	print("PASS: Extra Attack Barrage configuration & Rank scaling (4 -> 9 orbs) verified")
	
	# Verify Attack Pattern Selection & Cycling on Boss
	assert(reimu_boss.attack_patterns.size() == 5, "Reimu boss must have 5 attack patterns")
	var cycling_names: Array[String] = []
	var mock_playfield_cycle := MockBossPlayfield.new()
	root.add_child(mock_playfield_cycle)
	var boss_cycle: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(boss_cycle)
	boss_cycle._ready()
	
	var cycle_data: BossData = reimu_boss.duplicate()
	cycle_data.selection_mode = BossData.AttackSelectionMode.SEQUENTIAL
	boss_cycle.setup(cycle_data, 1, mock_playfield_cycle)
	boss_cycle._on_entrance_completed()
	boss_cycle.attack_started.connect(func(s_name: String):
		cycling_names.append(s_name)
	)
	
	boss_cycle._trigger_attack() # Attack 0: Two Rings Homing Talisman
	boss_cycle._trigger_attack() # Attack 1: Extra Attack Barrage
	boss_cycle._trigger_attack() # Attack 2: Pellet and Talisman Claw
	boss_cycle._trigger_attack() # Attack 3: Double Circles of Pellets
	boss_cycle._trigger_attack() # Attack 4: Four Red Talisman Circles
	boss_cycle._trigger_attack() # Attack 5: Cycles back to Two Rings Homing Talisman
	
	assert(cycling_names.size() == 6, "Must have recorded 6 attacks")
	assert(cycling_names[0] == "Two Rings Homing Talisman", "First attack must be Two Rings Homing Talisman")
	assert(cycling_names[1] == "Extra Attack Barrage", "Second attack must be Extra Attack Barrage")
	assert(cycling_names[2] == "Pellet and Talisman Claw", "Third attack must be Pellet and Talisman Claw")
	assert(cycling_names[3] == "Double Circles of Pellets", "Fourth attack must be Double Circles of Pellets")
	assert(cycling_names[4] == "Four Red Talisman Circles", "Fifth attack must be Four Red Talisman Circles")
	assert(cycling_names[5] == "Two Rings Homing Talisman", "Sixth attack must cycle back to Two Rings Homing Talisman")
	print("PASS: Boss attack pattern sequential cycling (5 attacks -> cycle) verified")
	
	# Verify RANDOM selection mode
	var random_data: BossData = reimu_boss.duplicate()
	random_data.selection_mode = BossData.AttackSelectionMode.RANDOM
	var boss_rand: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(boss_rand)
	boss_rand._ready()
	boss_rand.setup(random_data, 1, mock_playfield_cycle)
	boss_rand._on_entrance_completed()
	var random_names: Array[String] = []
	boss_rand.attack_started.connect(func(s_name: String):
		random_names.append(s_name)
	)
	boss_rand._trigger_attack()
	assert(random_names.size() == 1, "Must have recorded 1 random attack")
	assert(random_names[0] in ["Two Rings Homing Talisman", "Extra Attack Barrage", "Pellet and Talisman Claw", "Double Circles of Pellets", "Four Red Talisman Circles"], "Attack must be in the Reimu attack pool")
	print("PASS: Boss attack pattern RANDOM selection mode verified")
	boss_rand.queue_free()
	boss_cycle.queue_free()
	mock_playfield_cycle.queue_free()
	
	# 13. Verify Reimu Boss Attack 3: Pellet and Talisman Claw
	var spell3: SpellcardData = load("res://resources/spellcards/reimu/reimu_boss_spell_3.tres")
	assert(spell3 != null, "reimu_boss_spell_3.tres must load")
	assert(spell3.spellcard_id == "reimu_boss_spell_3", "Spellcard ID must match")
	assert(spell3.spellcard_name == "Pellet and Talisman Claw", "Spellcard name must match")
	assert(spell3.steps.size() == 1, "Spellcard must have 1 step")
	var claw_step: Resource = spell3.steps[0]
	assert(claw_step != null, "Claw step must exist")
	assert(claw_step.get_script() == DanmakuClawStepScript, "Step must be DanmakuClawStep")
	assert(claw_step.bullets_min == 8, "Rank 1 bullets_min must be 8")
	assert(claw_step.bullets_max == 16, "Rank 16 bullets_max must be 16")
	assert(claw_step.fan_spread_angle_deg == 25.71, "Fan spread angle must be 25.71 degrees")
	assert(claw_step.center_pellet_accel_r1 == 70.0, "Center pellet accel at Rank 1 must be 70.0")
	assert(claw_step.center_pellet_accel_r16 == 110.0, "Center pellet accel at Rank 16 must be 110.0")
	assert(claw_step.side_accel_r1 == 25.0, "Side accel at Rank 1 must be 25.0")
	
	# Verify DanmakuBullet linear acceleration physics
	var bullet_scene := preload("res://scenes/bullets/danmaku_bullet.tscn")
	var test_bullet: DanmakuBullet = bullet_scene.instantiate() as DanmakuBullet
	root.add_child(test_bullet)
	test_bullet._ready()
	test_bullet.setup(null, Vector2(100, 100), DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100.0, null, 50.0, 300.0)
	assert(test_bullet.acceleration == 50.0, "Acceleration must be 50.0")
	test_bullet._physics_process(1.0)
	assert(test_bullet.speed == 150.0, "Speed after 1s acceleration must be 150.0")
	assert(test_bullet.position.y == 250.0, "Position after 1s must be 250.0 (100 + 150)")
	test_bullet.queue_free()
	print("PASS: DanmakuBullet linear acceleration physics verified")
	
	# Verify Rank 1 Claw execution: 8 bullets x 4 (3 pellet strips + 1 talisman strip) = 32 bullets
	var mock_playfield_claw_r1 := MockBossPlayfield.new()
	root.add_child(mock_playfield_claw_r1)
	spell3.execute_sequence(mock_playfield_claw_r1, Vector2(300, 150), 1)
	assert(mock_playfield_claw_r1.spawned_count == 32, "Rank 1 must spawn exactly 32 bullets (got %d)" % mock_playfield_claw_r1.spawned_count)
	print("PASS: Pellet and Talisman Claw Rank 1 execution (32 bullets) verified")
	mock_playfield_claw_r1.queue_free()
	
	# Verify Rank 16 Claw execution: 16 bullets x 4 = 64 bullets
	var mock_playfield_claw_r16 := MockBossPlayfield.new()
	root.add_child(mock_playfield_claw_r16)
	spell3.execute_sequence(mock_playfield_claw_r16, Vector2(300, 150), 16)
	assert(mock_playfield_claw_r16.spawned_count == 64, "Rank 16 must spawn exactly 64 bullets (got %d)" % mock_playfield_claw_r16.spawned_count)
	print("PASS: Pellet and Talisman Claw Rank 16 execution (64 bullets) verified")
	mock_playfield_claw_r16.queue_free()
	
	# 14. Verify Reimu Boss Attack 4: Double Circles of Pellets
	var spell4: SpellcardData = load("res://resources/spellcards/reimu/reimu_boss_spell_4.tres")
	assert(spell4 != null, "reimu_boss_spell_4.tres must load")
	assert(spell4.spellcard_id == "reimu_boss_spell_4", "Spellcard ID must match")
	assert(spell4.spellcard_name == "Double Circles of Pellets", "Spellcard name must match")
	assert(spell4.steps.size() == 1, "Spellcard must have 1 step")
	var ring_step: Resource = spell4.steps[0]
	assert(ring_step != null, "Double ring step must exist")
	assert(ring_step.get_script() == DanmakuDoubleRingStepScript, "Step must be DanmakuDoubleRingStep")
	assert(ring_step.count_min == 31, "Rank 1 count_min must be 31 per ring (rank + 30)")
	assert(ring_step.count_max == 46, "Rank 16 count_max must be 46 per ring")
	assert(ring_step.initial_radius == 0.0, "initial_radius must be 0.0 (stacked at center)")
	assert(ring_step.radial_speed_min == 187.5, "radial_speed_min must be 187.5")
	assert(ring_step.radial_speed_max == 187.5, "radial_speed_max must be 187.5")
	assert(ring_step.angular_speed_min == 0.7854, "angular_speed_min must be 0.7854")
	assert(ring_step.angular_speed_max == 0.7854, "angular_speed_max must be 0.7854")
	
	# Verify DanmakuBullet EXPANDING_ORBIT physics
	var orbit_bullet: DanmakuBullet = bullet_scene.instantiate() as DanmakuBullet
	root.add_child(orbit_bullet)
	orbit_bullet._ready()
	orbit_bullet.setup_expanding_orbit(null, Vector2(300.0, 150.0), 0.0, 0.0, 200.0, 1.0)
	assert(orbit_bullet.motion_mode == DanmakuBullet.MotionMode.EXPANDING_ORBIT, "Motion mode must be EXPANDING_ORBIT")
	assert(is_equal_approx(orbit_bullet.position.x, 300.0), "Initial pos.x must be 300.0")
	assert(is_equal_approx(orbit_bullet.position.y, 150.0), "Initial pos.y must be 150.0")
	orbit_bullet._physics_process(1.0)
	assert(is_equal_approx(orbit_bullet.current_radius, 200.0), "Radius after 1s must be 200.0 (0 + 200)")
	assert(is_equal_approx(orbit_bullet.current_angle, 1.0), "Angle after 1s must be 1.0 rad")
	var expected_x: float = 300.0 + 200.0 * cos(1.0)
	var expected_y: float = 150.0 + 200.0 * sin(1.0)
	assert(is_equal_approx(orbit_bullet.position.x, expected_x), "Position x after 1s must match orbit math")
	assert(is_equal_approx(orbit_bullet.position.y, expected_y), "Position y after 1s must match orbit math")
	orbit_bullet.queue_free()
	print("PASS: DanmakuBullet EXPANDING_ORBIT physics verified")

	# Verify DanmakuBullet EXPANDING_ORBIT "breakout" extension (Cirno Interweaving Icicles):
	# expand -> hold (radius frozen) -> breakout (reorient + LINEAR acceleration ramp)
	var breakout_bullet: DanmakuBullet = bullet_scene.instantiate() as DanmakuBullet
	root.add_child(breakout_bullet)
	breakout_bullet._ready()
	var expand_speed := 160.0
	var target_radius := 180.0
	var expand_time := target_radius / expand_speed
	var hold_time := 0.25
	var breakout_time := expand_time + hold_time
	breakout_bullet.setup_expanding_orbit(null, Vector2(300.0, 150.0), 0.0, 0.0, expand_speed, 0.0, expand_time, breakout_time, deg_to_rad(90.0), 800.0, 450.0, 0.0)
	var dt := 1.0 / 60.0
	var t := 0.0
	while t < expand_time - dt:
		breakout_bullet._physics_process(dt)
		t += dt
	var radius_at_hold: float = breakout_bullet.current_radius
	assert(absf(radius_at_hold - target_radius) < 5.0, "Radius must be ~target_radius when hold begins (got %s)" % radius_at_hold)
	while t < breakout_time - dt:
		breakout_bullet._physics_process(dt)
		t += dt
	assert(absf(breakout_bullet.current_radius - radius_at_hold) < 1.0, "Radius must not drift during hold phase")
	assert(breakout_bullet.motion_mode == DanmakuBullet.MotionMode.EXPANDING_ORBIT, "Motion mode must still be EXPANDING_ORBIT before breakout_time")
	for i in range(30):
		breakout_bullet._physics_process(dt)
	assert(breakout_bullet.motion_mode == DanmakuBullet.MotionMode.LINEAR, "Motion mode must switch to LINEAR after breakout_time")
	assert(breakout_bullet.speed > 0.0, "Speed must have ramped up via acceleration after breakout")
	breakout_bullet.queue_free()
	print("PASS: DanmakuBullet EXPANDING_ORBIT breakout (expand->hold->breakout) physics verified")

	# Verify Rank 1 Double Rings execution: 31 red + 31 white = 62 bullets (rank + 30 per ring)
	var mock_playfield_rings_r1 := MockBossPlayfield.new()
	root.add_child(mock_playfield_rings_r1)
	spell4.execute_sequence(mock_playfield_rings_r1, Vector2(300, 150), 1)
	assert(mock_playfield_rings_r1.spawned_count == 62, "Rank 1 must spawn exactly 62 bullets (got %d)" % mock_playfield_rings_r1.spawned_count)
	print("PASS: Double Circles of Pellets Rank 1 execution (62 bullets) verified")
	mock_playfield_rings_r1.queue_free()
	
	# Verify Rank 16 Double Rings execution: 46 red + 46 white = 92 bullets
	var mock_playfield_rings_r16 := MockBossPlayfield.new()
	root.add_child(mock_playfield_rings_r16)
	spell4.execute_sequence(mock_playfield_rings_r16, Vector2(300, 150), 16)
	assert(mock_playfield_rings_r16.spawned_count == 92, "Rank 16 must spawn exactly 92 bullets (got %d)" % mock_playfield_rings_r16.spawned_count)
	print("PASS: Double Circles of Pellets Rank 16 execution (92 bullets) verified")
	mock_playfield_rings_r16.queue_free()
	
	# 15. Verify Reimu Boss Attack 5: Four Red Talisman Circles
	var spell5: SpellcardData = load("res://resources/spellcards/reimu/reimu_boss_spell_5.tres")
	assert(spell5 != null, "reimu_boss_spell_5.tres must load")
	assert(spell5.spellcard_id == "reimu_boss_spell_5", "Spellcard ID must match")
	assert(spell5.spellcard_name == "Four Red Talisman Circles", "Spellcard name must match")
	assert(spell5.steps.size() == 4, "Spellcard must have 4 steps (4 rings)")
	
	# Verify the 4 rings configuration
	var expected_speeds: Array[Vector2] = [
		Vector2(312.5, 312.5), # Ring 1 (outermost)
		Vector2(250.0, 250.0), # Ring 2
		Vector2(187.5, 187.5), # Ring 3
		Vector2(125.0, 125.0)  # Ring 4 (innermost)
	]
	var expected_staggers: Array[bool] = [false, false, false, false]
	
	for idx in range(4):
		var r_step: Resource = spell5.steps[idx]
		assert(r_step != null, "Ring step %d must exist" % idx)
		assert(r_step is DanmakuRingStep, "Step %d must be DanmakuRingStep" % idx)
		assert(r_step.count_min == 44, "Ring %d count_min must be 44 (4 x rank + 40)" % idx)
		assert(r_step.count_max == 104, "Ring %d count_max must be 104" % idx)
		assert(r_step.speed_min == expected_speeds[idx].x, "Ring %d speed_min must match %f" % [idx, expected_speeds[idx].x])
		assert(r_step.speed_max == expected_speeds[idx].y, "Ring %d speed_max must match %f" % [idx, expected_speeds[idx].y])
		assert(r_step.stagger_half_step == expected_staggers[idx], "Ring %d stagger_half_step must match %s" % [idx, str(expected_staggers[idx])])
		assert(r_step.bullet_data != null and r_step.bullet_data.bullet_id == "red_talisman", "Ring %d must use red_talisman" % idx)
	print("PASS: Four Red Talisman Circles resource structure, speed gradient, and aligned lanes verified")
	
	# Verify Rank 1 execution: 4 * 44 = 176 talismans spawned (4 x rank + 40 per ring)
	var mock_playfield_four_r1 := MockBossPlayfield.new()
	root.add_child(mock_playfield_four_r1)
	spell5.execute_sequence(mock_playfield_four_r1, Vector2(300, 150), 1)
	assert(mock_playfield_four_r1.spawned_count == 176, "Rank 1 must spawn exactly 176 talismans (got %d)" % mock_playfield_four_r1.spawned_count)
	print("PASS: Four Red Talisman Circles Rank 1 execution (176 talismans, full rings) verified")
	mock_playfield_four_r1.queue_free()
	
	# Verify Rank 16 uncapped execution: 4 * 104 = 416 full talismans spawned
	var mock_playfield_four_r16 := MockBossPlayfield.new()
	root.add_child(mock_playfield_four_r16)
	spell5.execute_sequence(mock_playfield_four_r16, Vector2(300, 150), 16)
	assert(mock_playfield_four_r16.spawned_count == 416, "Rank 16 must spawn all 416 talismans (got %d)" % mock_playfield_four_r16.spawned_count)
	print("PASS: Four Red Talisman Circles Rank 16 execution (all 416 talismans, 4 full rings) verified")
	mock_playfield_four_r16.queue_free()
	
	# Verify Reimu Boss attack pool has 5 attacks
	var reimu_boss_check: BossData = load("res://resources/bosses/reimu_boss.tres")
	assert(reimu_boss_check.attack_patterns.size() == 5, "Reimu boss attack pool must contain 5 patterns")
	assert(reimu_boss_check.attack_patterns[4].spellcard_id == "reimu_boss_spell_5", "Attack 5 must be reimu_boss_spell_5")
	print("PASS: Reimu BossData attack pool with all 5 attacks verified")
	
	# Verify Playfield danmaku_bullet_pool pre-allocation capacity is 1200
	var pf_scene := preload("res://scenes/arena/playfield.tscn")
	var pf = pf_scene.instantiate()
	root.add_child(pf)
	pf._ready()
	assert(pf.danmaku_bullet_pool != null, "danmaku_bullet_pool must exist")
	assert(pf.danmaku_bullet_pool._pool.size() == 1200, "danmaku_bullet_pool must have 1200 pre-allocated instances (got %d)" % pf.danmaku_bullet_pool._pool.size())
	pf.queue_free()
	print("PASS: Playfield danmaku_bullet_pool 1200 pre-allocation verified")

	# 12. Cirno BossData: attack pool, randomized on-screen duration
	var cirno_boss: BossData = load("res://resources/bosses/cirno_boss.tres")
	assert(cirno_boss != null, "cirno_boss.tres must load")
	assert(cirno_boss.character_id == "cirno", "cirno boss character_id must be cirno")
	assert(_recipe_matches(cirno_boss), "cirno boss must have a boss sheet recipe matching her 4x3 frames")
	assert(cirno_boss.hframes == 4 and cirno_boss.vframes == 3, "cirno boss must be 4x3 frames")
	assert(cirno_boss.attack_patterns.size() == 3, "cirno boss must have 3 attack patterns so far")
	assert(cirno_boss.attack_patterns[0].spellcard_id == "cirno_boss_spell_1", "Attack 1 must be cirno_boss_spell_1")
	assert(cirno_boss.attack_patterns[1].spellcard_id == "cirno_boss_spell_2", "Attack 2 must be cirno_boss_spell_2")
	assert(cirno_boss.attack_patterns[2].spellcard_id == "cirno_boss_spell_3", "Attack 3 must be cirno_boss_spell_3")

	var cirno_data: CharacterData = load("res://resources/characters/cirno.tres")
	assert(cirno_data.boss_data != null, "cirno.tres must have boss_data linked")

	assert(cirno_boss.departure_mode == BossData.DepartureMode.DURATION, "cirno boss departure_mode must be DURATION (stays on screen 15-20s)")
	assert(cirno_boss.randomize_duration == true, "cirno boss randomize_duration must be true")
	assert(cirno_boss.duration_min == 15.0 and cirno_boss.duration_max == 20.0, "cirno boss duration range must be 15-20s")

	# Randomized duration is rolled once at entrance completion and drives natural departure
	for trial in range(10):
		var cirno_entity: BossCharacter = boss_scene.instantiate() as BossCharacter
		root.add_child(cirno_entity)
		cirno_entity.setup(cirno_boss, 1)
		cirno_entity._on_entrance_completed()
		assert(cirno_entity._target_duration >= 15.0 and cirno_entity._target_duration <= 20.0, "cirno boss _target_duration must land in [15, 20] (got %s)" % cirno_entity._target_duration)
		cirno_entity.queue_free()
	print("PASS: Cirno BossData attack pool (3 attacks) and randomized 15-20s duration verified")

	# Duration-mode departure actually fires leave_screen() once _target_duration elapses
	var cirno_timeout: BossCharacter = boss_scene.instantiate() as BossCharacter
	root.add_child(cirno_timeout)
	cirno_timeout.setup(cirno_boss, 1)
	cirno_timeout._on_entrance_completed()
	cirno_timeout._target_duration = 1.0
	var cirno_left_screen := [false]
	cirno_timeout.left_screen.connect(func(): cirno_left_screen[0] = true)
	for i in range(70):
		cirno_timeout._physics_process(1.0 / 60.0)
	assert(cirno_timeout.state == BossCharacter.State.LEAVING, "Cirno boss must transition to LEAVING once _target_duration elapses")
	assert(cirno_left_screen[0] == true, "Cirno boss must emit left_screen once duration elapses")
	cirno_timeout.queue_free()
	print("PASS: Cirno boss DURATION-mode natural departure verified")

	# 13. Cirno Boss Attack 3: Shower of Icicles
	var cirno_spell3: SpellcardData = cirno_boss.attack_patterns[2]
	assert(cirno_spell3.steps.size() == 1, "cirno_boss_spell_3 must have 1 step")
	var shower_step: DanmakuIcicleShowerStep = cirno_spell3.steps[0] as DanmakuIcicleShowerStep
	assert(shower_step != null, "Step must be DanmakuIcicleShowerStep")
	assert(shower_step.count_min == 31 and shower_step.count_max == 46, "Icicle count must be rank + 30 (pl05.ecl sub3): 31 at Rank 1, 46 at Rank 16")
	assert(shower_step.fall_speed_max_rank1 == 437.5, "fall_speed_max_rank1 must be 437.5 (3.5 px/frame)")
	assert(shower_step.fall_speed_max_rank16 == 437.5, "fall_speed_max_rank16 must be 437.5 (ZUN does not rank-scale it)")
	assert(shower_step.bullet_data.resource_path.ends_with("blue_icicle.tres"), "Shower must use blue_icicle.tres")
	assert(is_equal_approx(shower_step.delay_between_spawns, 1.0 / 60.0), "Icicles must spawn one per frame, not all at once")
	assert(shower_step.spawn_offset_x == 108.0, "spawn_offset_x must be 90.0 * 1.2 = 108.0")
	assert(shower_step.spawn_offset_y_min == -24.0, "spawn_offset_y_min must be -20.0 * 1.2 = -24.0")
	assert(shower_step.spawn_offset_y_max == 192.0, "spawn_offset_y_max must be 160.0 * 1.2 = 192.0")

	# Execute with delay_between_spawns zeroed out on a duplicate so the full 55-icicle
	# burst can be verified synchronously (the real resource stays sequential/staggered
	# in actual gameplay; this only bypasses the per-icicle await for the test).
	var instant_shower_step: DanmakuIcicleShowerStep = shower_step.duplicate() as DanmakuIcicleShowerStep
	instant_shower_step.delay_between_spawns = 0.0

	var mock_playfield_shower_r1 := MockBossPlayfield.new()
	root.add_child(mock_playfield_shower_r1)
	instant_shower_step.execute(mock_playfield_shower_r1, Vector2(300, 150), 1)
	assert(mock_playfield_shower_r1.spawned_count == 31, "Rank 1 must spawn exactly 31 icicles (got %d)" % mock_playfield_shower_r1.spawned_count)
	mock_playfield_shower_r1.queue_free()

	var mock_playfield_shower_r16 := MockBossPlayfield.new()
	root.add_child(mock_playfield_shower_r16)
	instant_shower_step.execute(mock_playfield_shower_r16, Vector2(300, 150), 16)
	assert(mock_playfield_shower_r16.spawned_count == 46, "Rank 16 must spawn exactly 46 icicles (got %d)" % mock_playfield_shower_r16.spawned_count)
	mock_playfield_shower_r16.queue_free()
	print("PASS: Shower of Icicles resource structure, 1.2x spawn area, sequential delay, and Rank 1/16 spawn count (31/46) verified")

	# Verify each icicle falls straight down with a speed inside [fall_speed_min, rank-scaled max]
	var speed_playfield := MockBossSpeedCapturePlayfield.new()
	root.add_child(speed_playfield)
	instant_shower_step.execute(speed_playfield, Vector2(300, 150), 16)
	assert(speed_playfield.speeds.size() == 46, "Speed-capturing mock must record all 46 spawns")
	var saw_variance := false
	var first_speed: float = speed_playfield.speeds[0]
	for s in speed_playfield.speeds:
		assert(s >= shower_step.fall_speed_min - 0.01 and s <= shower_step.fall_speed_max_rank16 + 0.01, "Fall speed %s must be within [%s, %s]" % [s, shower_step.fall_speed_min, shower_step.fall_speed_max_rank16])
		if not is_equal_approx(s, first_speed):
			saw_variance = true
	assert(saw_variance, "Icicles must have slightly different (randomized) falling speeds")
	for d in speed_playfield.dirs:
		assert(d == Vector2.DOWN, "Every icicle must fall straight down")
	speed_playfield.queue_free()
	print("PASS: Shower of Icicles per-icicle speed variance and straight-down direction verified")

	print("--- ALL BOSS CHARACTER & LEVEL 4 MECHANIC TESTS PASSED! ---")
	quit(0)

class MockBossPlayfield extends Node2D:
	var spawned_count: int = 0
	var max_active_bullets: int = 0
	var player: Node2D = null
	func spawn_danmaku_bullet(_p_data, _p_pos, _p_mode, _p_dir, _p_speed, _p_accel = 0.0, _p_max_speed = 0.0):
		if max_active_bullets > 0 and spawned_count >= max_active_bullets:
			return null
		spawned_count += 1
		return null
	func spawn_danmaku_bullet_decel_home(_p_data, _p_pos, _p_dir, _p_speed, _p_decel, _p_pause, _p_launch, _p_stages = 1, _p_stage1_dur = 0.5, _p_sfx = ""):
		if max_active_bullets > 0 and spawned_count >= max_active_bullets:
			return null
		spawned_count += 1
		return null
	func spawn_danmaku_bullet_expanding_orbit(_p_data, _p_center, _p_radius, _p_angle, _p_rad_speed, _p_ang_speed, _p_freeze_time = -1.0, _p_breakout_time = -1.0, _p_breakout_angle = 0.0, _p_breakout_accel = 0.0, _p_breakout_max_speed = 0.0, _p_breakout_init_speed = 0.0):
		if max_active_bullets > 0 and spawned_count >= max_active_bullets:
			return null
		spawned_count += 1
		return null

## Records each spawn_danmaku_bullet() call's speed and direction, for steps
## (like DanmakuIcicleShowerStep) whose per-bullet randomization needs verifying.
class MockBossSpeedCapturePlayfield extends Node2D:
	var speeds: Array[float] = []
	var dirs: Array[Vector2] = []
	var player: Node2D = null
	func spawn_danmaku_bullet(_p_data, _p_pos, _p_mode, p_dir, p_speed, _p_accel = 0.0, _p_max_speed = 0.0):
		speeds.append(p_speed)
		dirs.append(p_dir)
		return null



## Boss sheets are built from the player's th09.dat at startup, so check the recipe instead
## of a file: it has to exist and lay out exactly the boss's hframes x vframes grid.
func _recipe_matches(data: BossData) -> bool:
	if not BossSheets.RECIPES.has(data.character_id):
		return false
	var rows: Array = BossSheets.RECIPES[data.character_id].rows
	for row in rows:
		if row.size() != data.hframes:
			return false
	return rows.size() == data.vframes
