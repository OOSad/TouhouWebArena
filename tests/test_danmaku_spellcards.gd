extends SceneTree

func _init() -> void:
	print("--- BEGIN DANMAKU SPELLCARD SYSTEM TEST ---")
	
	# 1. Verify Bullet Resources
	var red_pellet: DanmakuBulletData = load("res://resources/bullets/red_pellet.tres")
	var white_oval: DanmakuBulletData = load("res://resources/bullets/white_oval.tres")
	var white_pellet: DanmakuBulletData = load("res://resources/bullets/white_pellet.tres")
	var red_oval: DanmakuBulletData = load("res://resources/bullets/red_oval.tres")
	var red_talisman: DanmakuBulletData = load("res://resources/bullets/red_talisman.tres")
	var white_talisman: DanmakuBulletData = load("res://resources/bullets/white_talisman.tres")
	
	assert(red_pellet != null, "red_pellet.tres must load")
	assert(white_oval != null, "white_oval.tres must load")
	assert(white_pellet != null, "white_pellet.tres must load")
	assert(red_oval != null, "red_oval.tres must load")
	assert(red_talisman != null, "red_talisman.tres must load")
	assert(white_talisman != null, "white_talisman.tres must load")
	
	assert(_has_pofv_sprite(red_pellet), "red_pellet must have a PoFV sprite mapped")
	assert(white_oval.shape_type == DanmakuBulletData.ShapeType.TEXTURED, "white_oval must be TEXTURED")
	assert(_has_pofv_sprite(white_oval), "white_oval must have a PoFV sprite mapped")
	assert(_has_pofv_sprite(white_talisman), "white_talisman must have a PoFV sprite mapped")
	assert(white_talisman.damage == 1.5, "talisman damage should be 1.5")
	print("PASS: Bullet resources loaded and validated")
	
	# 2. Verify Spellcard Resources
	var reimu_lv2: SpellcardData = load("res://resources/spellcards/reimu/reimu_spell_lv2.tres")
	var reimu_lv3: SpellcardData = load("res://resources/spellcards/reimu/reimu_spell_lv3.tres")
	
	assert(reimu_lv2 != null, "reimu_spell_lv2.tres must load")
	assert(reimu_lv3 != null, "reimu_spell_lv3.tres must load")
	assert(reimu_lv2.level == 2, "reimu_lv2 level must be 2")
	assert(reimu_lv3.level == 3, "reimu_lv3 level must be 3")
	assert(reimu_lv2.steps.size() == 3, "reimu_lv2 must have 3 steps")
	assert(reimu_lv3.steps.size() == 3, "reimu_lv3 must have 3 steps")
	
	# Verify step types
	assert(reimu_lv2.steps[0] is DanmakuRingStep, "Step 0 must be DanmakuRingStep")
	assert(reimu_lv2.steps[1] is DanmakuDelayStep, "Step 1 must be DanmakuDelayStep")
	assert(reimu_lv2.steps[2] is DanmakuRingStep, "Step 2 must be DanmakuRingStep")
	
	var ring_red: DanmakuRingStep = reimu_lv2.steps[0] as DanmakuRingStep
	var ring_white: DanmakuRingStep = reimu_lv2.steps[2] as DanmakuRingStep
	assert(ring_red.motion_mode == DanmakuBullet.MotionMode.LINEAR, "Red ring must be LINEAR")
	assert(ring_white.motion_mode == DanmakuBullet.MotionMode.DECEL_AND_HOME, "White ring must be DECEL_AND_HOME")
	assert(ring_white.stagger_half_step == false, "White ring shares the red grid: pellets on the rice positions, rice on the half steps (pl00.ecl sub0)")
	print("PASS: Spellcard step timeline configuration verified")
	
	# 3. Verify Reimu & Marisa CharacterData Assignment
	var reimu_char: CharacterData = CharacterData.get_character("reimu")
	assert(reimu_char != null, "Reimu CharacterData must exist")
	assert(reimu_char.spellcard_lv2 == reimu_lv2, "Reimu spellcard_lv2 must be linked")
	assert(reimu_char.spellcard_lv3 == reimu_lv3, "Reimu spellcard_lv3 must be linked")
	
	var marisa_lv2: SpellcardData = load("res://resources/spellcards/marisa/marisa_spell_lv2.tres")
	assert(marisa_lv2 != null, "marisa_spell_lv2.tres must load")
	assert(marisa_lv2.steps.size() == 1, "marisa_lv2 must have 1 step")
	assert(marisa_lv2.steps[0] is DanmakuDiagonalStripStep, "Step 0 must be DanmakuDiagonalStripStep")
	var strip_step_lv2: DanmakuDiagonalStripStep = marisa_lv2.steps[0] as DanmakuDiagonalStripStep
	assert(strip_step_lv2.sfx == "se_tan00", "marisa_lv2 step sfx must be se_tan00")
	
	var marisa_lv3: SpellcardData = load("res://resources/spellcards/marisa/marisa_spell_lv3.tres")
	assert(marisa_lv3 != null, "marisa_spell_lv3.tres must load")
	assert(marisa_lv3.steps.size() == 1, "marisa_lv3 must have 1 step")
	assert(marisa_lv3.steps[0] is DanmakuDiagonalStripStep, "Step 0 must be DanmakuDiagonalStripStep")
	var strip_step_lv3: DanmakuDiagonalStripStep = marisa_lv3.steps[0] as DanmakuDiagonalStripStep
	assert(strip_step_lv3.sfx == "se_tan00", "marisa_lv3 step sfx must be se_tan00")
	
	# Marisa Level 4 Boss Spellcards
	var m_boss_1: SpellcardData = load("res://resources/spellcards/marisa/marisa_boss_spell_1.tres")
	assert(m_boss_1 != null and m_boss_1.steps[0] is DanmakuAimedStripStep)
	assert((m_boss_1.steps[0] as DanmakuAimedStripStep).sfx == "se_tan00", "marisa boss spell 1 must have se_tan00")
	
	var m_boss_2: SpellcardData = load("res://resources/spellcards/marisa/marisa_boss_spell_2.tres")
	assert(m_boss_2 != null and m_boss_2.steps[0] is DanmakuExtraAttackStep)
	
	var m_boss_3: SpellcardData = load("res://resources/spellcards/marisa/marisa_boss_spell_3.tres")
	assert(m_boss_3 != null and m_boss_3.steps[0] is DanmakuPinwheelStep)
	assert((m_boss_3.steps[0] as DanmakuPinwheelStep).sfx == "se_tan00", "marisa boss spell 3 must have se_tan00")
	
	var m_boss_4: SpellcardData = load("res://resources/spellcards/marisa/marisa_boss_spell_4.tres")
	assert(m_boss_4 != null and m_boss_4.steps[0] is DanmakuStarSprayStep)
	assert((m_boss_4.steps[0] as DanmakuStarSprayStep).sfx == "se_tan00", "marisa boss spell 4 must have se_tan00")
	
	var m_boss_5: SpellcardData = load("res://resources/spellcards/marisa/marisa_boss_spell_5.tres")
	assert(m_boss_5 != null and m_boss_5.steps[0] is DanmakuFixedLasersStep)
	
	# Youmu Level 2 & 3 Spellcards
	var youmu_lv2: SpellcardData = load("res://resources/spellcards/youmu/youmu_spell_lv2.tres")
	assert(youmu_lv2 != null and youmu_lv2.steps[0] is DanmakuDescendingStripsStep)
	assert((youmu_lv2.steps[0] as DanmakuDescendingStripsStep).sfx == "se_tan00", "youmu lv2 step sfx must be se_tan00")
	
	var youmu_lv3: SpellcardData = load("res://resources/spellcards/youmu/youmu_spell_lv3.tres")
	assert(youmu_lv3 != null and youmu_lv3.steps[0] is DanmakuDescendingStripsStep)
	assert((youmu_lv3.steps[0] as DanmakuDescendingStripsStep).sfx == "se_tan00", "youmu lv3 step sfx must be se_tan00")
	
	# Youmu Level 4 Boss Spellcards
	var y_boss_1: SpellcardData = load("res://resources/spellcards/youmu/youmu_boss_spell_1.tres")
	assert(y_boss_1 != null and y_boss_1.steps[0] is DanmakuKnifeFanStep)
	assert((y_boss_1.steps[0] as DanmakuKnifeFanStep).sfx == "se_tan00", "youmu boss spell 1 sfx must be se_tan00")
	
	var y_boss_2: SpellcardData = load("res://resources/spellcards/youmu/youmu_boss_spell_2.tres")
	assert(y_boss_2 != null and y_boss_2.steps[0] is DanmakuKnifeStreamStep)
	assert((y_boss_2.steps[0] as DanmakuKnifeStreamStep).sfx == "se_tan00", "youmu boss spell 2 sfx must be se_tan00")
	
	var y_boss_3: SpellcardData = load("res://resources/spellcards/youmu/youmu_boss_spell_3.tres")
	assert(y_boss_3 != null and y_boss_3.steps[0] is DanmakuOvalKnifeStep)
	assert((y_boss_3.steps[0] as DanmakuOvalKnifeStep).sfx == "se_tan00", "youmu boss spell 3 sfx must be se_tan00")
	
	print("PASS: Reimu, Marisa & Youmu CharacterData and Boss spellcards linkage and SFX verified")

	# Sakuya Level 2 Spellcard: Time Sign "Private Square" (time-stop dagger fan)
	var sakuya_lv2: SpellcardData = load("res://resources/spellcards/sakuya/sakuya_spell_lv2.tres")
	assert(sakuya_lv2 != null, "sakuya_spell_lv2.tres must load")
	assert(sakuya_lv2.level == 2, "sakuya_lv2 level must be 2")
	assert(sakuya_lv2.steps.size() == 1, "sakuya_lv2 must have 1 step")
	assert(sakuya_lv2.steps[0] is DanmakuTimeStopFanStep, "Step 0 must be DanmakuTimeStopFanStep")
	var fan_step: DanmakuTimeStopFanStep = sakuya_lv2.steps[0] as DanmakuTimeStopFanStep
	assert(fan_step.dagger_count == 10, "sakuya_lv2 must spawn 10 daggers")
	assert(fan_step.knife_data != null and _has_pofv_sprite(fan_step.knife_data), "sakuya knife bullet data must have a PoFV sprite")
	assert(fan_step.pellet_data != null, "sakuya fan step must have paired pellet data")

	var sakuya_char: CharacterData = CharacterData.get_character("sakuya")
	assert(sakuya_char != null, "Sakuya CharacterData must exist")
	assert(sakuya_char.spellcard_lv2 == sakuya_lv2, "Sakuya spellcard_lv2 must be linked")
	print("PASS: Sakuya Level 2 Time Sign \"Private Square\" spellcard loaded and linked")

	# Sakuya Level 3 Spellcard: Time Sign "Private Square" (same time-stop fan step,
	# reconfigured into 4-dagger stacks that peel apart outward/inward/tangential)
	var sakuya_lv3: SpellcardData = load("res://resources/spellcards/sakuya/sakuya_spell_lv3.tres")
	assert(sakuya_lv3 != null, "sakuya_spell_lv3.tres must load")
	assert(sakuya_lv3.level == 3, "sakuya_lv3 level must be 3")
	assert(sakuya_lv3.steps.size() == 1, "sakuya_lv3 must have 1 step")
	assert(sakuya_lv3.steps[0] is DanmakuTimeStopFanStep, "Step 0 must be DanmakuTimeStopFanStep")
	var lv3_fan_step: DanmakuTimeStopFanStep = sakuya_lv3.steps[0] as DanmakuTimeStopFanStep
	assert(lv3_fan_step.stacked_cross_mode, "sakuya_lv3 must use stacked_cross_mode")
	assert(lv3_fan_step.dagger_count == 10, "sakuya_lv3 must spawn 10 stacks")
	assert(lv3_fan_step.knife_data != null and _has_pofv_sprite(lv3_fan_step.knife_data), "sakuya lv3 knife bullet data must have a PoFV sprite")

	assert(sakuya_char.spellcard_lv3 == sakuya_lv3, "Sakuya spellcard_lv3 must be linked")
	print("PASS: Sakuya Level 3 Time Sign \"Private Square\" (stacked cross) spellcard loaded and linked")

	# Yuuka Lv2 / Lv3 Flower Sign "Blossoming of Gensokyo" (pl09.ecl sub0 / sub1)
	var yuuka_char: CharacterData = CharacterData.get_character("yuuka")
	for lv in [2, 3]:
		var y_spell: SpellcardData = load("res://resources/spellcards/yuuka/yuuka_spell_lv%d.tres" % lv)
		assert(y_spell != null and y_spell.level == lv and y_spell.steps[0] is DanmakuBlossomStep)
		var blossom: DanmakuBlossomStep = y_spell.steps[0] as DanmakuBlossomStep
		assert(_has_pofv_sprite(blossom.bullet_data_even) and _has_pofv_sprite(blossom.bullet_data_odd), "yuuka lv%d bullets must have PoFV sprites" % lv)
		assert((yuuka_char.spellcard_lv2 if lv == 2 else yuuka_char.spellcard_lv3) == y_spell, "Yuuka spellcard_lv%d must be linked" % lv)
	print("PASS: Yuuka Level 2 & 3 Flower Sign \"Blossoming of Gensokyo\" loaded and linked")

	# 4. Verify DanmakuBullet Decel & Home Physics
	var bullet_scene := preload("res://scenes/bullets/danmaku_bullet.tscn")
	var bullet: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(bullet)
	
	var dummy_player := Node2D.new()
	dummy_player.position = Vector2(300.0, 800.0)
	root.add_child(dummy_player)
	
	# Initial spawn at top center, moving DOWN at speed 200, decel over 0.2s, pause 0.1s, launch at 400
	bullet.setup_decel_home(white_oval, Vector2(300.0, 100.0), Vector2.DOWN, 200.0, 0.2, 0.1, 400.0, dummy_player)
	assert(bullet.motion_mode == DanmakuBullet.MotionMode.DECEL_AND_HOME, "Motion mode must be DECEL_AND_HOME")
	assert(bullet.speed == 200.0, "Initial speed must be 200")
	
	# Step through decel phase (0.1s -> speed should drop to ~100)
	bullet._physics_process(0.1)
	assert(bullet.speed < 200.0 and bullet.speed > 0.0, "Speed must decelerate during Phase 1")
	
	# Step past decel into pause phase
	bullet._physics_process(0.15)
	assert(bullet.speed == 0.0, "Speed must be 0 during pause phase")
	
	# Move dummy player to right side before launch
	dummy_player.position = Vector2(500.0, 300.0)
	# Step past pause into launched state
	bullet._physics_process(0.15)
	assert(bullet.speed == 400.0, "Speed must become launch_speed after pause")
	# Target direction must point towards dummy player (500, 300) from bullet pos (~300, 120)
	assert(bullet.direction.x > 0.5, "Bullet must snapshot and home towards dummy player position")
	print("PASS: DanmakuBullet staged decel-and-home motion behavior verified")
	
	# Verify bullet survives off-screen expansion (e.g. y = -200, which previously despawned)
	var offscreen_bullet: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(offscreen_bullet)
	offscreen_bullet.setup(white_oval, Vector2(300.0, -200.0), DanmakuBullet.MotionMode.LINEAR, Vector2.UP, 100.0)
	offscreen_bullet._physics_process(0.1)
	assert(not offscreen_bullet.is_queued_for_deletion(), "Bullet must not despawn merely by being off-screen")
	# Step through remaining lifetime to verify time-based despawn
	offscreen_bullet._physics_process(7.0)
	assert(offscreen_bullet.is_queued_for_deletion(), "Bullet must despawn after lifetime expires")
	print("PASS: Time-based bullet despawn and offscreen survival verified")
	
	# Verify Multi-Stage Homing (Stage 1 -> Mid-flight decel -> Stage 2 lock-on)
	var multi_bullet: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(multi_bullet)
	dummy_player.position = Vector2(300.0, 600.0)
	multi_bullet.setup_decel_home(red_talisman, Vector2(300.0, 100.0), Vector2.DOWN, 200.0, 0.1, 0.1, 300.0, dummy_player, 2, 0.2)
	assert(multi_bullet.homing_stages == 2, "Bullet should have 2 homing stages")
	assert(multi_bullet._current_stage == 1, "Initial stage must be 1")
	
	multi_bullet._physics_process(0.1) # decel complete
	multi_bullet._physics_process(0.1) # pause complete, launched toward (300, 600)
	assert(multi_bullet._decel_state == DanmakuBullet.DecelState.LAUNCHED, "Must be launched for Stage 1")
	assert(multi_bullet.direction.y > 0.9, "Must head down towards dummy player")
	
	# Step through stage 1 flight duration -> triggers transition to Stage 2 decel
	multi_bullet._physics_process(0.2)
	assert(multi_bullet._current_stage == 2, "Current stage must advance to 2")
	assert(multi_bullet._decel_state == DanmakuBullet.DecelState.DECELERATING, "Must decelerate for Stage 2")
	
	# Move dummy player to right (600, 300)
	dummy_player.position = Vector2(600.0, 300.0)
	multi_bullet._physics_process(0.1) # decel to 0
	multi_bullet._physics_process(0.1) # pause and lock on
	assert(multi_bullet._decel_state == DanmakuBullet.DecelState.LAUNCHED, "Must be launched for Stage 2")
	assert(multi_bullet.direction.x > 0.5, "Stage 2 homing must lock on towards updated dummy player position")
	multi_bullet.queue_free()
	print("PASS: DanmakuBullet 2-stage homing verified successfully")
	
	# 5. Verify Alternating Even-Odd Bullet Ring Step
	var alt_step := DanmakuRingStep.new()
	alt_step.bullet_data = red_pellet
	alt_step.alternate_bullet_data = white_oval
	alt_step.count_min = 13 # Odd number to verify auto-rounding to even
	alt_step.count_max = 13
	alt_step.motion_mode = DanmakuBullet.MotionMode.LINEAR
	alt_step.alternate_has_separate_motion = true
	alt_step.alternate_motion_mode = DanmakuBullet.MotionMode.DECEL_AND_HOME
	
	var spawned_bullets: Array[Dictionary] = []
	var mock_playfield := Node2D.new()
	# Attach script with callable hooks
	var mock_script := GDScript.new()
	mock_script.source_code = """
extends Node2D
var bullet_records: Array = []
func spawn_danmaku_bullet(p_data, p_pos, p_mode, p_dir, p_speed):
	bullet_records.append({"data": p_data, "mode": p_mode, "speed": p_speed})
func spawn_danmaku_bullet_decel_home(p_data, p_pos, p_dir, p_speed, decel, pause, launch, stages = 1, stage1_dur = 0.5, p_sfx = ""):
	bullet_records.append({"data": p_data, "mode": 1, "speed": p_speed, "stages": stages})
"""
	mock_script.reload()
	mock_playfield.set_script(mock_script)
	root.add_child(mock_playfield)
	
	alt_step.execute(mock_playfield, Vector2(300, 100), 1)
	var records: Array = mock_playfield.get("bullet_records")
	
	# Assert count is 14 (auto-rounded to even)
	assert(records.size() == 14, "Odd count 13 must be rounded up to 14 for seamless alternating loop")
	for i in range(records.size()):
		if i % 2 == 0:
			assert(records[i].data == red_pellet, "Even bullet %d must be primary bullet_data" % i)
			assert(records[i].mode == DanmakuBullet.MotionMode.LINEAR, "Even bullet %d must be LINEAR" % i)
		else:
			assert(records[i].data == white_oval, "Odd bullet %d must be alternate_bullet_data" % i)
			assert(records[i].mode == DanmakuBullet.MotionMode.DECEL_AND_HOME, "Odd bullet %d must be DECEL_AND_HOME" % i)
	print("PASS: Alternating even-odd bullet ring step and separate motion behavior verified")
	
	# 5b. Extra Attacks fired from inside a spellcard or boss pattern have to land in the
	#     same place on both clients. SpellcardData seeds each step from the match's
	#     synced round seed, so the same seed must reproduce a barrage exactly.
	var ex_step := DanmakuExtraAttackStep.new()
	ex_step.character_id = "reimu"
	ex_step.spawn_location = DanmakuExtraAttackStep.SpawnLocation.ACROSS_TOP
	ex_step.count_min = 6
	ex_step.count_max = 6
	ex_step.delay_between_spawns = 0.0 # keeps execute() synchronous for the test
	
	var ex_spell := SpellcardData.new()
	ex_spell.waves_min = 2 # a second wave also exercises the seeded wave-origin jitter
	ex_spell.waves_max = 2
	ex_spell.wave_interval = 0.0
	ex_spell.steps = [ex_step]
	
	var ex_placements := func(p_seed: int) -> Array:
		var field := Node2D.new()
		root.add_child(field)
		ex_spell.execute_sequence(field, Vector2(300, 100), 1, p_seed)
		var spots: Array = []
		for child in field.get_children():
			spots.append(child.position)
		# free() rather than queue_free(): a deferred _ready on a discarded orb would
		# otherwise still fire and try to build a tween.
		root.remove_child(field)
		field.free()
		return spots
	
	var ex_run_a: Array = ex_placements.call(20260921)
	var ex_run_b: Array = ex_placements.call(20260921)
	var ex_run_c: Array = ex_placements.call(20260922)
	assert(ex_run_a.size() == 12, "Two waves of six Extra Attacks must spawn twelve nodes")
	assert(ex_run_a == ex_run_b, "The same pattern seed must place every Extra Attack identically")
	assert(ex_run_a != ex_run_c, "A different pattern seed must still vary the barrage")
	print("PASS: Seeded spellcard patterns place Extra Attacks identically across clients")
	
	# Without a seed the step keeps rolling from the global RNG, which is what the rest
	# of the danmaku still does on purpose - only assert it stays functional.
	var unseeded_field := Node2D.new()
	root.add_child(unseeded_field)
	ex_spell.execute_sequence(unseeded_field, Vector2(300, 100), 1)
	assert(unseeded_field.get_child_count() == 12, "An unseeded pattern must still spawn its full barrage")
	root.remove_child(unseeded_field)
	unseeded_field.free()
	print("PASS: Unseeded local play still runs the pattern from the global RNG")
	
	# 6. Clean up nodes and exit
	dummy_player.queue_free()
	bullet.queue_free()
	mock_playfield.queue_free()
	
	print("ALL DANMAKU SPELLCARD TESTS PASSED!")
	quit(0)



## Bullet art comes from the player's th09.dat at startup, so check the bullet type is mapped
## to a PoFV sprite rather than that a texture is already loaded.
func _has_pofv_sprite(data: DanmakuBulletData) -> bool:
	return data != null and BulletSprites.SPRITES.has(data.resource_path.get_file().get_basename())
