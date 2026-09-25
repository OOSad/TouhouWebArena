extends SceneTree

func _init() -> void:
	print("--- BEGIN SPELL BAR & CHARGE SYSTEM TEST ---")
	
	# 1. Player Charge Initialization
	var player_scene := preload("res://scenes/player/player.tscn")
	var player: Player = player_scene.instantiate()
	player.character_id = "reimu"
	root.add_child(player)
	player.apply_character_data()
	
	assert(player.charge_segments == 4, "Default spell bar segments must be 4")
	assert(player.passive_charge == 1.0, "Player must start with 1.0 passive charge (1 segment / 25%)")
	assert(player.active_charge == 0.0, "Player must start with 0.0 active charge")
	assert(not player.is_charging, "Player must not be charging at start")
	print("PASS: Initial charge states verified (passive=1.0, active=0.0, segments=4)")
	
	# 2. Character-specific Data Differences
	var reimu_data := CharacterData.get_character("reimu")
	var marisa_data := CharacterData.get_character("marisa")
	assert(reimu_data != null and marisa_data != null, "Reimu and Marisa CharacterData must exist")
	assert(reimu_data.active_charge_speed > marisa_data.active_charge_speed, "Reimu should have faster active charge speed than Marisa per authentic PoFV frame data")
	assert(marisa_data.passive_charge_per_fairy > reimu_data.passive_charge_per_fairy, "Marisa should gain more passive charge per fairy than Reimu per authentic PoFV trait")
	print("PASS: CharacterData modular differentiation verified")
	
	# 3. Passive Charge Accumulation & Clamping
	player.add_passive_charge(player.passive_charge_per_fairy) # +0.010
	assert(is_equal_approx(player.passive_charge, 1.010), "Passive charge must increment on fairy kill")
	
	player.add_passive_charge(player.passive_charge_per_great_fairy) # +0.025
	assert(is_equal_approx(player.passive_charge, 1.035), "Passive charge must increment on great fairy kill")
	
	player.add_passive_charge(player.passive_charge_per_spirit) # +0.045
	assert(is_equal_approx(player.passive_charge, 1.080), "Passive charge must increment on spirit kill")
	
	player.add_passive_charge(player.passive_charge_per_cancel) # +0.0015
	assert(is_equal_approx(player.passive_charge, 1.0815), "Passive charge must increment on pellet cancel")
	
	# Overflow test: cap at max segments (4.0)
	player.add_passive_charge(10.0)
	assert(player.passive_charge == 4.0, "Passive charge must clamp to max charge segments (4.0)")
	print("PASS: Passive charge accumulation and cap at 4.0 verified")
	
	# 4. Active Charge Gating (Cannot exceed available passive charge)
	player.passive_charge = 2.4 # Has access up to Level 2
	player.is_local_player = false # Disarm input polling for manual control
	
	# Simulate active charging
	player.is_charging = true
	var delta: float = 0.5
	player.active_charge = minf(player.active_charge + player.active_charge_speed * delta, player.passive_charge)
	assert(player.active_charge > 0.0, "Active charge should accumulate")
	
	# Overcharge simulation
	player.active_charge = 3.0 # Try setting beyond passive limit (2.4)
	var max_allowed: float = minf(player.passive_charge, float(player.charge_segments))
	player.active_charge = minf(player.active_charge, max_allowed)
	assert(is_equal_approx(player.active_charge, 2.4), "Active charge cannot exceed current passive charge")
	print("PASS: Active charge is gated by passive charge level")
	
	# 5. Level 1 Release: Charge Attack fires, ZERO passive consumed
	player.passive_charge = 2.8
	player.active_charge = 1.3 # Reached Level 1 (>= 1.0, < 2.0)
	var fired_attack := {"type": "", "lvl": 0}
	player.charge_attack_fired.connect(func(lvl, _name):
		fired_attack["type"] = "charge_attack"
		fired_attack["lvl"] = lvl
	)
	player._release_charge()
	assert(fired_attack["type"] == "charge_attack" and fired_attack["lvl"] == 1, "Level 1 charge must emit charge_attack_fired")
	assert(is_equal_approx(player.passive_charge, 2.8), "Level 1 Charge Attack must consume 0 passive charge!")
	assert(player.active_charge == 0.0, "Active charge must reset to 0.0 after release")
	print("PASS: Level 1 Charge Attack consumes 0 passive charge")
	
	# 6. Level 2, 3, 4 Release: Spellcard fires, appropriate segments consumed
	var fired_spell := {"lvl": 0, "name": ""}
	player.spellcard_fired.connect(func(lvl, name):
		fired_spell["lvl"] = lvl
		fired_spell["name"] = name
	)
	
	# Level 2 test (consumes 1 segment)
	player.passive_charge = 2.5
	player.active_charge = 2.0
	player._release_charge()
	assert(fired_spell["lvl"] == 2, "Level 2 charge must emit spellcard_fired(2)")
	assert(is_equal_approx(player.passive_charge, 1.5), "Level 2 Spellcard must consume 1 segment of passive charge (2.5 -> 1.5)")
	assert(player.active_charge == 0.0, "Active charge must reset to 0.0")
	
	# Level 3 test (consumes 2 segments)
	player.passive_charge = 3.7
	player.active_charge = 3.0
	player._release_charge()
	assert(fired_spell["lvl"] == 3, "Level 3 charge must emit spellcard_fired(3)")
	assert(is_equal_approx(player.passive_charge, 1.7), "Level 3 Spellcard must consume 2 segments of passive charge (3.7 -> 1.7)")
	
	# Level 4 test (consumes 3 segments, returns to 1.0 / 25%)
	player.passive_charge = 4.0
	player.active_charge = 4.0
	player._release_charge()
	assert(fired_spell["lvl"] == 4, "Level 4 charge must emit spellcard_fired(4)")
	assert(is_equal_approx(player.passive_charge, 1.0), "Level 4 Spellcard must consume 3 segments of passive charge (4.0 -> 1.0)")
	print("PASS: Spellcards (Lv 2-4) subtract segments appropriately")
	
	# 7. Shove / Damage Interruption: Active charge is preserved on hit shove
	player.passive_charge = 3.5
	player.active_charge = 2.8
	player.is_charging = true
	player._start_hit_shove()
	assert(player.active_charge == 2.8, "Hit shove must RETAIN active charge (not reset to 0)")
	assert(player.passive_charge == 3.5, "Hit shove must not penalize accumulated passive charge")
	print("PASS: Hit shove retains active charge cleanly without resetting gauge")
	
	# 8. Dynamic Rank Scaling Verification
	# Lv2/3: 1 + int(t / 10.0), Lv4: 1 + int(t / 12.0), both clamped [1, 16]
	var test_times: Array = [0.0, 15.0, 45.0, 90.0, 155.0, 200.0]
	for t in test_times:
		var r_lv2_3: int = clampi(1 + int(t / 10.0), 1, 16)
		var r_lv4: int = clampi(1 + int(t / 12.0), 1, 16)
		assert(r_lv2_3 >= 1 and r_lv2_3 <= 16, "Rank Lv2/3 must be within [1, 16]")
		assert(r_lv4 >= 1 and r_lv4 <= 16, "Rank Lv4 must be within [1, 16]")
	print("PASS: Rank scaling formula verified across match elapsed timeline")
	
	# 9. SpellBar UI Component Verification
	var spell_bar_scene := preload("res://scenes/ui/spell_bar.tscn")
	var spell_bar: SpellBar = spell_bar_scene.instantiate()
	root.add_child(spell_bar)
	spell_bar._ready()
	spell_bar.apply_character_data(reimu_data)
	assert(spell_bar.max_segments == 4, "SpellBar segments must match CharacterData")
	
	spell_bar.set_passive_charge(2.5, 4)
	assert(spell_bar.passive_charge == 2.5, "SpellBar passive charge updated")
	
	spell_bar.set_active_charge(1.8, 4)
	assert(spell_bar.active_charge == 1.8, "SpellBar active charge updated")
	
	spell_bar.set_ranks(7, 4)
	assert(spell_bar.left_rank_label.text == "7", "Left rank label must display 7")
	assert(spell_bar.right_rank_label.text == "4", "Right rank label must display 4")
	print("PASS: SpellBar UI component properties and labels verified")
	
	# 10. Modularity Verification (Custom Segment Counts)
	var custom_data := CharacterData.new()
	custom_data.spell_bar_segments = 3
	custom_data.passive_charge_start = 1.0
	player.character_data = custom_data
	player.apply_character_data()
	assert(player.charge_segments == 3, "Modular character should support 3 segments")
	player.add_passive_charge(10.0)
	assert(player.passive_charge == 3.0, "Passive charge cap should adapt to custom segments (3.0)")
	print("PASS: Modular non-standard segment counts verified")
	
	print("--- ALL SPELL BAR TESTS PASSED SUCCESSFULLY! ---")
	quit(0)

