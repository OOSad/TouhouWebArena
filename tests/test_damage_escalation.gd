extends SceneTree

func _init() -> void:
	print("\n=======================================================")
	print(" RUNNING DAMAGE ESCALATION & SUDDEN DEATH TESTS (TOUHOU 09)")
	print("=======================================================\n")
	
	var player_scene := preload("res://scenes/player/player.tscn")
	var player: Player = player_scene.instantiate()
	root.add_child(player)
	
	# Ensure clean initial state
	player.reset_for_round(Vector2(200, 400), false)
	assert(player.current_health == 5.0, "Player must start with 5.0 health")
	assert(player.time_since_last_hit == 0.0, "time_since_last_hit starts at 0.0")
	assert(not player.is_damage_escalated(), "Damage is not escalated at round start")
	print("  [PASS] 1. Initial round state: 5.0 HP, 0.0s elapsed, escalation inactive")
	
	# Test 1: Normal hit deals base 1.0 damage (takes 1 HP away)
	var pellet_scene := preload("res://scenes/bullets/enemy_pellet.tscn")
	var pellet: EnemyPellet = pellet_scene.instantiate()
	pellet.damage = 1.0
	pellet.is_big = false
	root.add_child(pellet)
	
	player._on_hurtbox_area_entered(pellet)
	assert(is_equal_approx(player.current_health, 4.0), "Base pellet must deal 1.0 damage (leaves 4.0 HP)")
	assert(player.hits_taken_this_round == 1, "hits_taken_this_round increments to 1")
	assert(player.time_since_last_hit == 0.0, "time_since_last_hit reset to 0.0 on hit")
	print("  [PASS] 2. Normal hit docks 1.0 HP (1 full orb), resetting survival timer")
	
	# Test 2: Surviving past DAMAGE_ESCALATION_TIME (30.0s) activates 2-orb escalation
	player.is_invulnerable = false
	player.is_shoved = false
	player.time_since_last_hit = 30.5
	assert(player.is_damage_escalated(), "Escalation must be active after 30+ seconds without taking damage")
	assert(player.get_damage_multiplier() == 2.0, "Damage multiplier must be 2.0x when escalated")
	print("  [PASS] 3. Surviving 30.0s without damage triggers 2-Orb Escalation (2.0x multiplier)")
	
	# Test 3: Escalated hit deals 2.0 damage (takes 2 HP away)
	var escalated_pellet: EnemyPellet = pellet_scene.instantiate()
	escalated_pellet.damage = 1.0
	root.add_child(escalated_pellet)
	
	player._on_hurtbox_area_entered(escalated_pellet)
	assert(is_equal_approx(player.current_health, 2.0), "Escalated hit must deal 2.0 damage (leaves 2.0 HP)")
	assert(player.time_since_last_hit == 0.0, "time_since_last_hit reset back to 0.0 after escalated hit")
	assert(not player.is_damage_escalated(), "Escalation resets after hit, providing recovery window")
	print("  [PASS] 4. Escalated hit docks 2.0 HP (2 full orbs) and resets escalation window")
	
	# Test 4: Follow-up hit within recovery window only deals base 1.0 damage
	player.is_invulnerable = false
	player.is_shoved = false
	player.time_since_last_hit = 5.0 # only 5s elapsed
	var follow_up_pellet: EnemyPellet = pellet_scene.instantiate()
	follow_up_pellet.damage = 1.0
	root.add_child(follow_up_pellet)
	
	player._on_hurtbox_area_entered(follow_up_pellet)
	assert(is_equal_approx(player.current_health, 1.0), "Follow-up hit before 30s deals base 1.0 damage (leaves 1.0 HP)")
	print("  [PASS] 5. Follow-up hit during recovery deals base 1.0 HP, avoiding unfair double-loss")
	
	# Test 5: Match Sudden Death override (round timer >= 60s)
	player.is_invulnerable = false
	player.is_shoved = false
	player.is_sudden_death_active = true
	assert(player.is_damage_escalated(), "Sudden death flag forces damage escalation regardless of timer")
	
	# Test 6: Guts preserves player at 0.5 HP even under heavy escalated damage (2.0 damage when at 1.0 HP)
	var sd_pellet: EnemyPellet = pellet_scene.instantiate()
	sd_pellet.damage = 1.0
	root.add_child(sd_pellet)
	player._on_hurtbox_area_entered(sd_pellet) # 1.0 * 2.0 = 2.0 damage on 1.0 HP
	assert(is_equal_approx(player.current_health, 0.5), "Guts rule: Player must survive on 0.5 HP despite 2.0 damage")
	assert(not player.is_dead, "Player must not be dead after Guts survival")
	assert(player.is_damage_escalated(), "Escalation remains locked active under Sudden Death")
	print("  [PASS] 6. Sudden Death docks 2.0 HP and Guts clamps lethal hit to 0.5 HP")
	
	# Test 7: Lethal hit at 0.5 HP triggers defeat
	player.is_invulnerable = false
	player.is_shoved = false
	var final_pellet: EnemyPellet = pellet_scene.instantiate()
	final_pellet.damage = 1.0
	root.add_child(final_pellet)
	player._on_hurtbox_area_entered(final_pellet)
	assert(player.is_dead, "Player is defeated on final hit at 0.5 HP")
	assert(is_equal_approx(player.current_health, 0.0), "Health drops to 0.0 on defeat")
	print("  [PASS] 7. Final hit at 0.5 HP concludes round with defeat")
	
	# Test 8: Round reset cleans up escalation state
	player.reset_for_round(Vector2(200, 400), false)
	assert(player.current_health == 5.0, "Health restored to 5.0")
	assert(player.time_since_last_hit == 0.0, "time_since_last_hit reset to 0.0")
	assert(not player.is_sudden_death_active, "is_sudden_death_active reset to false")
	assert(not player.is_damage_escalated(), "is_damage_escalated reset to false")
	print("  [PASS] 8. Round reset fully cleanses escalation and sudden death state")
	
	# Test 9: MatchTimer UI Sudden Death state
	var timer_scene := preload("res://scenes/ui/match_timer.tscn")
	var match_timer: MatchTimer = timer_scene.instantiate()
	root.add_child(match_timer)
	
	assert(not match_timer.is_sudden_death, "MatchTimer starts with is_sudden_death = false")
	match_timer.set_sudden_death(true)
	assert(match_timer.is_sudden_death, "MatchTimer enters sudden death state")
	match_timer.reset()
	assert(not match_timer.is_sudden_death, "MatchTimer reset cleanses sudden death state")
	assert(match_timer.timer_label.modulate == Color.WHITE, "MatchTimer modulate returns to Color.WHITE")
	print("  [PASS] 9. MatchTimer sudden death visual warning state and reset verified")
	
	print("\n-------------------------------------------------------")
	print(" ALL DAMAGE ESCALATION & SUDDEN DEATH TESTS PASSED!")
	print("-------------------------------------------------------\n")
	quit(0)
