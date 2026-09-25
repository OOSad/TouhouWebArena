extends SceneTree

func _init() -> void:
	print("\n=======================================================")
	print(" RUNNING HIT CHARGE & SPIRIT ACCELERATION UNIT TESTS  ")
	print("=======================================================\n")
	
	test_player_hit_charge()
	test_spirit_activation_acceleration()
	
	print("\n-------------------------------------------------------")
	print(" ALL HIT CHARGE & SPIRIT ACCELERATION TESTS PASSED!    ")
	print("-------------------------------------------------------\n")
	quit(0)

func test_player_hit_charge() -> void:
	print("--- 1. Testing Player Hit Passive Charge Scaling ---")
	var player_scene := preload("res://scenes/player/player.tscn")
	var player: Player = player_scene.instantiate()
	root.add_child(player)
	player._ready()
	
	# Initial conditions
	assert(player.hits_taken_this_round == 0, "hits_taken_this_round must start at 0")
	assert(is_equal_approx(player.passive_charge, 1.0), "passive_charge must start at 1.0")
	print("  [PASS] Initial player state: hits=0, passive_charge=1.0")
	
	# First hit: must award 0.5 segment (half segment)
	player.take_damage(1.0)
	assert(player.hits_taken_this_round == 1, "hits_taken_this_round must be 1 after first hit")
	assert(is_equal_approx(player.passive_charge, 1.5), "First hit must increase passive_charge by 0.5 to 1.5")
	print("  [PASS] First hit: +0.5 segment -> passive_charge = 1.5")
	
	# Clear invulnerability and shove to allow second hit
	player.is_invulnerable = false
	player.is_shoved = false
	
	# Second hit: must award 1.0 segment (full segment)
	player.take_damage(1.0)
	assert(player.hits_taken_this_round == 2, "hits_taken_this_round must be 2 after second hit")
	assert(is_equal_approx(player.passive_charge, 2.5), "Second hit must increase passive_charge by 1.0 to 2.5")
	print("  [PASS] Second hit: +1.0 segment -> passive_charge = 2.5")
	
	# Clear invulnerability and shove to allow third hit
	player.is_invulnerable = false
	player.is_shoved = false
	
	# Third hit: must award another 1.0 segment (full segment)
	player.take_damage(1.0)
	assert(player.hits_taken_this_round == 3, "hits_taken_this_round must be 3 after third hit")
	assert(is_equal_approx(player.passive_charge, 3.5), "Third hit must increase passive_charge by 1.0 to 3.5")
	print("  [PASS] Third hit: +1.0 segment -> passive_charge = 3.5")
	
	# Round Reset (preserve_gauge = false)
	player.reset_for_round(Vector2(300, 800), false)
	assert(player.hits_taken_this_round == 0, "hits_taken_this_round must reset to 0 for new round")
	assert(is_equal_approx(player.passive_charge, 1.0), "passive_charge must reset to 1.0 when not preserved")
	print("  [PASS] Round reset (unpreserved): hits=0, passive_charge=1.0")
	
	# First hit of new round: must award 0.5 again
	player.take_damage(1.0)
	assert(player.hits_taken_this_round == 1, "New round first hit must set hits=1")
	assert(is_equal_approx(player.passive_charge, 1.5), "New round first hit must award 0.5 to 1.5")
	print("  [PASS] New round first hit: awards +0.5 -> passive_charge = 1.5")
	
	# Round Reset with preserve_gauge = true
	player.set_passive_charge(2.0)
	player.reset_for_round(Vector2(300, 800), true)
	assert(player.hits_taken_this_round == 0, "hits_taken_this_round must reset to 0 even with preserved gauge")
	assert(is_equal_approx(player.passive_charge, 2.0), "passive_charge must preserve 2.0")
	
	# First hit with preserved gauge: awards 0.5
	player.take_damage(1.0)
	assert(player.hits_taken_this_round == 1, "Hits must be 1")
	assert(is_equal_approx(player.passive_charge, 2.5), "Preserved gauge first hit: 2.0 + 0.5 = 2.5")
	print("  [PASS] Preserved gauge round: first hit awards +0.5 -> passive_charge = 2.5")
	
	player.queue_free()

func test_spirit_activation_acceleration() -> void:
	print("\n--- 2. Testing Spirit Activation Gradual Acceleration Ramp ---")
	var spirit_scene := preload("res://scenes/enemies/spirit.tscn")
	var spirit: Spirit = spirit_scene.instantiate()
	root.add_child(spirit)
	spirit.setup(Vector2(300.0, 700.0), "blue")
	
	assert(spirit.state == Spirit.SpiritState.NORMAL, "Spirit must start in NORMAL state")
	
	# Activate the spirit (via Scope Style)
	spirit.activate()
	assert(spirit.state == Spirit.SpiritState.ACTIVATED, "Spirit state must be ACTIVATED")
	assert(spirit.current_upward_speed == 0.0, "Activated spirit must start with 0.0 upward speed")
	assert(spirit.current_upward_acceleration == 0.0, "Activated spirit must start with 0.0 upward acceleration")
	print("  [PASS] Activated spirit starts with speed=0.0 and acceleration=0.0")
	
	# Simulate physics process in increments and check gradual ramp
	var initial_y: float = spirit.position.y
	var dt: float = 1.0 / 60.0
	var total_time: float = 0.0
	
	var speed_at_1s: float = 0.0
	var accel_at_1s: float = 0.0
	var speed_at_2s: float = 0.0
	var accel_at_2s: float = 0.0
	var y_at_1s: float = 0.0
	
	while total_time < 2.8:
		spirit._physics_process(dt)
		total_time += dt
		
		if is_equal_approx(total_time, 1.0) or (total_time >= 1.0 and speed_at_1s == 0.0):
			speed_at_1s = spirit.current_upward_speed
			accel_at_1s = spirit.current_upward_acceleration
			y_at_1s = spirit.position.y
		elif is_equal_approx(total_time, 2.0) or (total_time >= 2.0 and speed_at_2s == 0.0):
			speed_at_2s = spirit.current_upward_speed
			accel_at_2s = spirit.current_upward_acceleration
	
	var final_y: float = spirit.position.y
	var total_distance_ascended: float = initial_y - final_y
	var distance_in_first_second: float = initial_y - y_at_1s
	
	print("  Telemetry at 1.0s: accel = %.1f px/s², speed = %.1f px/s, ascended = %.1f px" % [accel_at_1s, speed_at_1s, distance_in_first_second])
	print("  Telemetry at 2.0s: accel = %.1f px/s², speed = %.1f px/s" % [accel_at_2s, speed_at_2s])
	print("  Telemetry at 2.8s: final speed = %.1f px/s, total ascended = %.1f px" % [spirit.current_upward_speed, total_distance_ascended])
	
	# Validations:
	# 1. Acceleration must ramp up over time: accel at 2s > accel at 1s > 0
	assert(accel_at_1s > 0.0, "Acceleration at 1.0s must be > 0.0")
	assert(accel_at_2s > accel_at_1s, "Acceleration must increase over time (ramp up)")
	
	# 2. Speed must ramp up smoothly: speed at 2s > speed at 1s > 0
	assert(speed_at_1s > 0.0, "Speed at 1.0s must be > 0.0")
	assert(speed_at_2s > speed_at_1s, "Speed must increase over time")
	
	# 3. First second must have minimal movement so spirit lingers near activation point
	assert(distance_in_first_second < 20.0, "In first 1.0s, spirit must linger near activation (ascended %.1f px < 20px)" % distance_in_first_second)
	
	# 4. Total upward ascent in 2.8s must be around 140-150px and well under the old 308px
	assert(total_distance_ascended >= 120.0 and total_distance_ascended <= 180.0, 
		"Total ascended distance (%.1f px) must be within calibrated [120, 180] px range" % total_distance_ascended)
	print("  [PASS] Gradual acceleration ramp validated: lingered for 1st second (%.1f px), total ascent %.1f px (< 180px vs old 308px)" % [distance_in_first_second, total_distance_ascended])
	
	spirit.queue_free()

