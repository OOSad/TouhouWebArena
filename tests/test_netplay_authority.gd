extends SceneTree

func _init() -> void:
	print("\n==============================================")
	print(" RUNNING NETPLAY CLIENT-AUTHORITY UNIT TESTS  ")
	print("==============================================")
	
	var player_scene := preload("res://scenes/player/player.tscn")
	var player: Player = player_scene.instantiate()
	root.add_child(player)
	player._ready()
	
	# 1. Test local player initial state
	assert(player.is_local_player == true, "Default player must be local")
	var hb: Area2D = player.hurtbox
	assert(hb != null, "Hurtbox must exist")
	assert(hb.monitoring == true, "Local player hurtbox must be monitoring")
	assert(hb.monitorable == true, "Local player hurtbox must be monitorable")
	print("  [PASS] Local player hurtbox enabled by default")
	
	# 2. Test switching to remote puppet mode
	player.is_local_player = false
	assert(hb.monitoring == false, "Remote puppet hurtbox monitoring must be disabled")
	assert(hb.monitorable == false, "Remote puppet hurtbox monitorable must be disabled")
	print("  [PASS] Remote puppet hurtbox monitoring and monitorable disabled")
	
	# 3. Test that take_damage does NOT affect remote puppet
	var initial_hp: float = player.current_health
	player.take_damage(2.0)
	assert(player.current_health == initial_hp, "Remote puppet must not take local damage")
	assert(player.is_dead == false, "Remote puppet must not die from local damage")
	print("  [PASS] Remote puppet completely ignores local take_damage calls")
	
	# 4. Test that AI mode re-enables hurtbox even if not local
	player.set_ai_mode(true)
	assert(hb.monitoring == true, "AI player hurtbox monitoring must be active")
	assert(hb.monitorable == true, "AI player hurtbox monitorable must be active")
	player.take_damage(1.0)
	assert(player.current_health == initial_hp - 1.0, "AI player must take damage")
	print("  [PASS] AI player hurtbox active and takes damage for autonomous play")
	
	# 5. Test apply_remote_damage_visuals
	player.set_ai_mode(false)
	player.is_local_player = false
	player.apply_remote_damage_visuals(2.5)
	assert(player.current_health == 2.5, "apply_remote_damage_visuals must update health gauge value")
	assert(player._red_blink_timer > 0.0, "apply_remote_damage_visuals must trigger red blink")
	print("  [PASS] apply_remote_damage_visuals updates HP and triggers red blink without damage shove")
	

	# 6. Extra Attack placement is rolled once by the aggressor and shipped in the packet.
	#    Re-rolling it per client is what made an Earth Light Ray land in two places.
	var marisa_targets: PackedVector2Array = MoteDispatcher.roll_extra_attack_targets("marisa", 5)
	assert(marisa_targets.size() == 1, "Marisa's Extra Attack must roll exactly one landing spot")
	var marisa_data: CharacterData = CharacterData.get_character("marisa")
	assert(marisa_targets[0].x >= marisa_data.extra_attack_x_range.x and marisa_targets[0].x <= marisa_data.extra_attack_x_range.y, "Rolled X must sit inside the character's range")
	assert(marisa_targets[0].y >= marisa_data.extra_attack_y_range.x and marisa_targets[0].y <= marisa_data.extra_attack_y_range.y, "Rolled Y must sit inside the character's range")
	print("  [PASS] Extra Attack landing spots are rolled inside the sender's configured range")

	# Sakuya is the exception: her daggers fly from the fairy's death spot, no target to roll.
	assert(MoteDispatcher.roll_extra_attack_targets("sakuya", 12).is_empty(), "Sakuya must roll no Extra Attack targets")
	print("  [PASS] Sakuya's self-propelled daggers roll no landing spots")

	# Cirno's shot COUNT is rolled too, so it has to travel in the packet like the spots do.
	for i in range(30):
		var low: int = MoteDispatcher.roll_extra_attack_targets("cirno", 1).size()
		var high: int = MoteDispatcher.roll_extra_attack_targets("cirno", 22).size()
		assert(low >= 1 and low <= 2, "Cirno at rank 1 must roll 1-2 stalactites")
		assert(high >= 3 and high <= 4, "Cirno at rank 22 must roll 3-4 stalactites")
	print("  [PASS] Cirno's rank-scaled stalactite count is part of the rolled payload")

	# Spectator events and replays are JSON, so the roll round-trips through a flat list.
	var flat: Array = MoteDispatcher.targets_to_flat(marisa_targets)
	assert(flat.size() == marisa_targets.size() * 2, "Flattened targets must be two floats per shot")
	var restored: PackedVector2Array = MoteDispatcher.targets_from_flat(flat)
	assert(restored.size() == marisa_targets.size(), "Flat round-trip must preserve the shot count")
	assert(absf(restored[0].x - marisa_targets[0].x) < 0.05 and absf(restored[0].y - marisa_targets[0].y) < 0.05, "Flat round-trip must preserve each landing spot")
	print("  [PASS] Rolled targets survive the JSON spectator/replay round-trip")

	# Reimu's orb picks its own toss direction, which decides every bounce after it.
	# Seeding it from the synced landing spot keeps both clients on the same trajectory.
	var orb_scene := preload("res://scenes/attacks/yin_yang_orb.tscn")
	var orb_velocities: Array[Vector2] = []
	for seed_value in [4242, 4242, 9001]:
		var orb: YinYangOrb = orb_scene.instantiate()
		orb.setup_variation(seed_value)
		root.add_child(orb)
		orb._ready()  # this suite never processes a frame, so _ready must be called by hand
		orb_velocities.append(orb.velocity)
		# free() rather than queue_free(): the deferred _ready would otherwise still
		# fire later and re-connect body_entered on a node the test is done with.
		root.remove_child(orb)
		orb.free()
	assert(orb_velocities[0].is_equal_approx(orb_velocities[1]), "Same seed must produce the same opening toss on both clients")
	assert(not orb_velocities[0].is_equal_approx(orb_velocities[2]), "A different seed must still vary the toss")
	print("  [PASS] Yin-Yang Orb's opening toss is seeded, not re-rolled per client")

	player.queue_free()
	print("\n----------------------------------------------")
	print(" ALL NETPLAY CLIENT-AUTHORITY TESTS PASSED!   ")
	print("----------------------------------------------\n")
	quit(0)

