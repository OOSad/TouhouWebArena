extends SceneTree

func _init() -> void:
	print("--- BEGIN HEALTH SYSTEM TEST ---")
	var player_scene := preload("res://scenes/player/player.tscn")
	var player: Player = player_scene.instantiate()
	root.add_child(player)
	
	# 1. Initial health verification
	assert(player.current_health == 5.0, "Player must start with 5.0 health")
	assert(player.max_health == 5.0, "Player max_health must be 5.0")
	print("PASS: Initial health is 5.0")
	
	# 2. Pellet damage test (1.0 heart)
	player.take_damage(1.0)
	assert(player.current_health == 4.0, "Player health must be 4.0 after 1.0 damage")
	print("PASS: 1.0 damage leaves 4.0 health")
	
	# Clear invulnerability for testing
	player.is_invulnerable = false
	player.is_shoved = false
	
	# 3. Big pellet / EX damage test (1.5 hearts)
	player.take_damage(1.5)
	assert(is_equal_approx(player.current_health, 2.5), "Player health must be 2.5 after 1.5 damage")
	print("PASS: 1.5 damage leaves 2.5 health")
	
	player.is_invulnerable = false
	player.is_shoved = false
	
	# 4. GUTS TEST: When at 2.5, taking massive lethal hit (e.g. 5.0) MUST NOT kill; must leave 0.5!
	player.take_damage(5.0)
	assert(is_equal_approx(player.current_health, 0.5), "GUTS RULE: Player must survive on 0.5 health!")
	assert(not player.is_dead, "Player must not be dead after Guts survival")
	print("PASS: Guts threshold preserves player at 0.5 health on lethal hit")
	
	player.is_invulnerable = false
	player.is_shoved = false
	
	# 5. LETHAL HIT AT 0.5 HP:
	var flags := {"defeated": false}
	player.defeated.connect(func(): flags["defeated"] = true)
	player.take_damage(1.0)
	assert(is_equal_approx(player.current_health, 0.0), "Player health must drop to 0.0")
	assert(player.is_dead, "Player must be dead")
	assert(flags["defeated"] == true, "Defeated signal must be emitted")
	print("PASS: Hit while at 0.5 health triggers defeat")
	
	# 6. Test HealthGauge rendering logic
	var gauge_scene := preload("res://scenes/ui/health_gauge.tscn")
	var gauge: HealthGauge = gauge_scene.instantiate()
	root.add_child(gauge)
	gauge.update_health(3.5, 5.0)
	assert(gauge._orb_rects.size() == 5, "Gauge must have 5 orb rects")
	assert(gauge._orb_rects[0].texture == HealthGauge.FULL_ORB_TEX, "Orb 1 must be full")
	assert(gauge._orb_rects[1].texture == HealthGauge.FULL_ORB_TEX, "Orb 2 must be full")
	assert(gauge._orb_rects[2].texture == HealthGauge.FULL_ORB_TEX, "Orb 3 must be full")
	assert(gauge._orb_rects[3].texture == HealthGauge.HALF_ORB_TEX, "Orb 4 must be half")
	assert(gauge._orb_rects[4].texture == HealthGauge.EMPTY_ORB_TEX, "Orb 5 must be empty")
	print("PASS: HealthGauge correctly maps 3.5 health to [Full, Full, Full, Half, Empty]")
	
	# 7. Test HeavyShockwave clearing
	var heavy_wave_scene := preload("res://scenes/effects/heavy_shockwave.tscn")
	var heavy_wave: HeavyShockwave = heavy_wave_scene.instantiate()
	root.add_child(heavy_wave)
	
	var pellet_scene := preload("res://scenes/bullets/enemy_pellet.tscn")
	var pellet: EnemyPellet = pellet_scene.instantiate()
	root.add_child(pellet)
	pellet.setup(Vector2(100, 100), -1.0, Vector2.DOWN, true) # Big pellet
	heavy_wave._on_area_entered(pellet)
	assert(pellet.is_queued_for_deletion(), "HeavyShockwave must delete big pellets")
	print("PASS: HeavyShockwave deletes big pellets")
	
	var orb_scene := preload("res://scenes/attacks/yin_yang_orb.tscn")
	var orb: YinYangOrb = orb_scene.instantiate()
	root.add_child(orb)
	heavy_wave._on_area_entered(orb)
	assert(orb.is_queued_for_deletion(), "HeavyShockwave must clear Yin-Yang Orbs")
	print("PASS: HeavyShockwave clears Yin-Yang Orbs")
	# 8. Test shove duration and fairy defeat with shockwave source
	assert(player.shove_duration == 0.85, "Shove duration must be 0.85s")
	var fairy_scene := preload("res://scenes/enemies/fairy.tscn")
	var fairy: Fairy = fairy_scene.instantiate()
	root.add_child(fairy)
	fairy.setup(Fairy.FairyType.SMALL, Curve2D.new(), 10.0)
	fairy.is_active = true
	fairy.is_vulnerable = true
	var defeated_source := {"src": ""}
	fairy.defeated.connect(func(_type, _pos, src): defeated_source["src"] = src)
	heavy_wave._on_area_entered(fairy)
	assert(defeated_source["src"] == "shockwave" or defeated_source["src"] == "heavy_shockwave", "Fairy must be defeated with shockwave or heavy_shockwave source")
	print("PASS: Fairy defeated by HeavyShockwave with shockwave source")
	
	# 9. Test shove transparency and red blink on landing
	player.is_dead = false
	player.is_local_player = false
	if player.sprite == null:
		player.sprite = Sprite2D.new()
		player.add_child(player.sprite)
	
	player.is_shoved = true
	player._physics_process(0.01)
	assert(player.sprite.modulate.a < 0.9, "Sprite must flash transparent while shoved")
	
	player.is_shoved = false
	player._red_blink_timer = 0.5
	player._physics_process(0.01)
	assert(player.sprite.modulate.r > 0.9 and player.sprite.modulate.g < 0.5, "Sprite must be tinted red during active blink")
	
	player._red_blink_timer = 0.5 - 0.07 # Cycle 1 off phase
	player._physics_process(0.001)
	assert(player.sprite.modulate == Color.WHITE, "Sprite must be white during blink off phase")
	print("PASS: Shove transparency and 4-blink red landing effect verified")
	
	print("--- ALL TESTS PASSED SUCCESSFULLY! ---")
	quit(0)
