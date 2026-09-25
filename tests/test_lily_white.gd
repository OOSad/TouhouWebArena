extends SceneTree

func _init() -> void:
	print("--- Running Lily White & Match Timer Tests ---")
	test_match_timer_formatting()
	test_lily_white_instantiation()
	test_lily_white_damage_and_phases()
	test_playfield_spawn_lily()
	test_lily_white_retreat_barrage()
	print("--- All Lily White & Match Timer Tests PASSED! ---")
	quit(0)

func test_match_timer_formatting() -> void:
	print("[Test] MatchTimer formatting and behavior...")
	var timer_scene := preload("res://scenes/ui/match_timer.tscn")
	var timer: MatchTimer = timer_scene.instantiate()
	root.add_child(timer)
	
	timer.set_time(0.0)
	assert(timer.timer_label.text == "00:00", "0.0s should format as 00:00, got: %s" % timer.timer_label.text)
	
	timer.set_time(50.0)
	assert(timer.timer_label.text == "00:50", "50.0s should format as 00:50, got: %s" % timer.timer_label.text)
	
	timer.set_time(80.0)
	assert(timer.timer_label.text == "01:20", "80.0s should format as 01:20, got: %s" % timer.timer_label.text)
	
	timer.set_time(125.0)
	assert(timer.timer_label.text == "02:05", "125.0s should format as 02:05, got: %s" % timer.timer_label.text)
	
	timer.advance(10.0) # paused by default
	assert(timer.timer_label.text == "02:05", "Timer should not advance when paused")
	
	timer.start()
	timer.advance(5.0)
	assert(timer.timer_label.text == "02:10", "Timer should advance when running")
	
	timer.reset()
	assert(timer.timer_label.text == "00:00", "Timer should reset to 00:00")
	
	timer.queue_free()
	print("  -> PASS: MatchTimer formatting & control")

func test_lily_white_instantiation() -> void:
	print("[Test] LilyWhite instantiation & node structure...")
	var lily_scene := preload("res://scenes/enemies/lily_white.tscn")
	var lily: LilyWhite = lily_scene.instantiate()
	root.add_child(lily)
	
	assert(lily.collision_layer == 8, "LilyWhite collision_layer must be 8 (Enemies), got: %d" % lily.collision_layer)
	assert(lily.collision_mask == 4, "LilyWhite collision_mask must be 4 (Player Bullets), got: %d" % lily.collision_mask)
	assert(lily.descent_duration == 2.0, "Descent duration must be 2.0s")
	assert(lily.rest_duration == 0.8, "Rest duration must be 0.8s")
	assert(lily.retreat_duration == 4.0, "Retreat duration must be 4.0s")
	assert(lily.start_position == Vector2(300.0, -100.0), "Start pos must be (300, -100)")
	assert(lily.rest_position == Vector2(300.0, 500.0), "Rest pos must be (300, 500)")
	assert(lily.exit_position == Vector2(300.0, -100.0), "Exit pos must be (300, -100)")
	
	lily._ensure_nodes()
	assert(lily.sprite != null, "Sprite2D must exist")
	assert(lily.sprite.texture != null, "Sprite texture must be assigned")
	assert(lily.sprite.hframes == 4, "Sprite hframes must be 4")
	assert(lily.sprite.vframes == 3, "Sprite vframes must be 3")
	assert(lily.sprite.scale == Vector2(2.0, 2.0), "Sprite scale must be (2, 2)")
	assert(lily.collision_shape != null, "CollisionShape2D must exist")
	
	lily.queue_free()
	print("  -> PASS: LilyWhite instantiation & properties")

func test_lily_white_damage_and_phases() -> void:
	print("[Test] LilyWhite damage handling and defeat...")
	var lily_scene := preload("res://scenes/enemies/lily_white.tscn")
	var lily: LilyWhite = lily_scene.instantiate()
	root.add_child(lily)
	lily.start_sequence()
	lily.position = Vector2(300.0, 500.0)
	
	assert(lily.max_health == 80.0, "Lily max HP must be 80.0 (3 seconds of uninterrupted fire)")
	assert(lily.current_health == 80.0, "Initial HP must be 80.0")
	
	var damage_accepted: bool = lily.take_damage(20.0)
	assert(damage_accepted == true, "take_damage must return true")
	assert(is_equal_approx(lily.current_health, 60.0), "HP must decrease to 60.0 after 20 damage")
	assert(not lily.is_dead, "Lily must not be dead at 60 HP")
	
	# Test PlayerBullet collision directly
	var bullet_scene := preload("res://scenes/bullets/player_bullet.tscn")
	var bullet: PlayerBullet = bullet_scene.instantiate()
	root.add_child(bullet)
	bullet.damage = 1.0
	bullet._on_area_entered(lily)
	assert(bullet._has_hit == true, "PlayerBullet must flag _has_hit = true when hitting LilyWhite")
	assert(is_equal_approx(lily.current_health, 59.0), "HP must decrease to 59.0 after bullet hit")
	bullet.queue_free()
	
	var defeated_death_pos := [Vector2.ZERO]
	var defeated_fired := [false]
	lily.defeated.connect(func(pos: Vector2):
		defeated_fired[0] = true
		defeated_death_pos[0] = pos
	)
	
	lily.take_damage(120.0)
	assert(lily.current_health <= 0.0, "HP must be 0 after lethal hit")
	assert(lily.is_dead, "is_dead must be true")
	assert(defeated_fired[0], "defeated signal must have emitted")
	assert(defeated_death_pos[0] == Vector2(300.0, 500.0), "defeated signal should transmit death pos")
	
	print("  -> PASS: LilyWhite damage and defeat")

func test_playfield_spawn_lily() -> void:
	print("[Test] Playfield.spawn_lily_white()...")
	var playfield_scene := preload("res://scenes/arena/playfield.tscn")
	var playfield: Playfield = playfield_scene.instantiate()
	root.add_child(playfield)
	playfield.setup(1, true, "reimu")
	
	assert(playfield.active_lily_white == null, "Initially active_lily_white must be null")
	var lily: LilyWhite = playfield.spawn_lily_white()
	assert(lily != null, "spawn_lily_white() must return a LilyWhite instance")
	assert(playfield.active_lily_white == lily, "playfield.active_lily_white must reference new instance")
	
	# Spawning again should safely replace or clean up the previous one
	var lily2: LilyWhite = playfield.spawn_lily_white()
	assert(lily2 != null, "second spawn must succeed")
	assert(playfield.active_lily_white == lily2, "active_lily_white must now be the new instance")
	
	playfield.reset_for_new_round()
	assert(playfield.active_lily_white == null, "Round reset must clear active_lily_white")
	
	playfield.queue_free()
	print("  -> PASS: Playfield spawn and reset lifecycle")

func test_lily_white_retreat_barrage() -> void:
	print("[Test] LilyWhite retreat barrage dual-spiral and claw combos...")
	var lily_scene := preload("res://scenes/enemies/lily_white.tscn")
	var lily: LilyWhite = lily_scene.instantiate()
	root.add_child(lily)
	
	# 1. Verify default barrage parameters
	assert(lily.barrage_wave_count == 32, "Default barrage_wave_count must be 32")
	assert(is_equal_approx(lily.barrage_step_interval, 0.105), "Default barrage_step_interval must be 0.105s")
	assert(is_equal_approx(lily.claw_spread_deg, 24.0), "Default claw_spread_deg must be 24.0")
	assert(is_equal_approx(lily.rotation_step_deg, 6.0), "Default rotation_step_deg must be 6.0")
	# Duration and swept arc are the invariants; total bullets is what the wave count moves.
	assert(is_equal_approx(lily.barrage_wave_count * lily.barrage_step_interval, 3.36), "Barrage must still run 3.36s")
	assert(is_equal_approx(lily.barrage_wave_count * lily.rotation_step_deg, 192.0), "Barrage must still sweep 192 degrees")
	assert(lily.barrage_wave_count * 12 == 384, "Barrage must emit 384 bullets, not the 576 the cadence pass drifted to")
	assert(is_equal_approx(lily.blue_start_angle_deg, -45.0), "Default blue_start_angle_deg must be -45.0")
	assert(is_equal_approx(lily.red_start_angle_deg, -135.0), "Default red_start_angle_deg must be -135.0")
	assert(is_equal_approx(lily.bullet_speed, 220.0), "Default bullet_speed must be 220.0")
	assert(is_equal_approx(lily.speed_jitter, 15.0), "Default speed_jitter must be 15.0")
	assert(is_equal_approx(lily.angle_jitter_deg, 2.0), "Default angle_jitter_deg must be 2.0")
	# The strands only stay separate while jitter cannot bridge the gap between prongs.
	# At the old 13/7 pairing neighbouring prongs overlapped and the barrage lost its lanes.
	assert(lily.angle_jitter_deg * 2.0 < lily.claw_spread_deg, "Angle jitter must stay under half the claw spread, or the prongs merge")
	assert(is_equal_approx(lily.twin_offset_px, 30.0), "Default twin_offset_px must be 30.0")
	
	# 2. Test mathematical symmetry of dual vectors across every wave
	for i in range(lily.barrage_wave_count):
		var blue_deg: float = lily.blue_start_angle_deg + (i * lily.rotation_step_deg)
		var red_deg: float = lily.red_start_angle_deg - (i * lily.rotation_step_deg)
		var blue_rad := deg_to_rad(blue_deg)
		var red_rad := deg_to_rad(red_deg)
		
		# Bilateral mirror symmetry: cos(red) == -cos(blue), sin(red) == sin(blue)
		var cos_diff: float = absf(cos(red_rad) - (-cos(blue_rad)))
		var sin_diff: float = absf(sin(red_rad) - sin(blue_rad))
		assert(cos_diff < 0.001, "Step %d: cos bilateral symmetry failed" % i)
		assert(sin_diff < 0.001, "Step %d: sin bilateral symmetry failed" % i)
	
	lily.queue_free()
	
	# 3. Test firing wave through playfield
	var pf_scene := preload("res://scenes/arena/playfield.tscn")
	var pf: Playfield = pf_scene.instantiate()
	root.add_child(pf)
	pf.setup(1, true, "reimu")
	
	var active_lily: LilyWhite = pf.spawn_lily_white()
	assert(active_lily != null, "spawn_lily_white returned valid instance")
	assert(active_lily.playfield == pf, "active_lily.playfield assigned to pf")
	
	var initial_bullets: int = pf.get_active_bullet_count()
	assert(initial_bullets == 0, "No bullets active initially")
	
	# Fire 1 barrage wave (step 0):
	# 2 vectors (blue + red) * 3 claw prongs * 2 (lead solid + trail ring) = 12 pellets
	active_lily._fire_barrage_wave(0)
	assert(pf.get_active_bullet_count() == 12, "Wave 0 must spawn exactly 12 bullets, got: %d" % pf.get_active_bullet_count())
	
	# Verify bullet pool contains 6 normal pellets and 6 ring pellets
	var ring_count: int = 0
	var normal_count: int = 0
	for child in pf.bullets_layer.get_children():
		if child is EnemyPellet and child.visible:
			assert(child.speed >= (active_lily.bullet_speed - active_lily.speed_jitter - 0.01) and child.speed <= (active_lily.bullet_speed + active_lily.speed_jitter + 0.01), "Bullet speed %f must be within jitter range [%f, %f]" % [child.speed, active_lily.bullet_speed - active_lily.speed_jitter, active_lily.bullet_speed + active_lily.speed_jitter])
			if child.is_ring:
				ring_count += 1
			else:
				normal_count += 1
	assert(ring_count == 6, "Expected 6 ring pellets in wave 0, got: %d" % ring_count)
	assert(normal_count == 6, "Expected 6 normal pellets in wave 0, got: %d" % normal_count)
	
	# 4. Test barrage cancellation on defeat
	active_lily._start_retreat_barrage()
	assert(active_lily._barrage_tween != null and active_lily._barrage_tween.is_valid(), "Barrage tween must be active")
	
	# Dealing lethal damage must kill the barrage tween immediately
	active_lily.take_damage(200.0)
	assert(active_lily.is_dead, "Lily must be dead")
	assert(active_lily._barrage_tween == null or not active_lily._barrage_tween.is_valid(), "Barrage tween must be killed upon defeat")
	
	pf.queue_free()
	print("  -> PASS: LilyWhite retreat barrage dual-spiral and claw combos")

