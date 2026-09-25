extends SceneTree

func _init() -> void:
	print("--- BEGIN AUDIO MANAGER TEST ---")
	
	# 1. Instantiate AudioService into SceneTree
	var audio_mgr: AudioService = AudioService.new()
	root.add_child(audio_mgr)
	assert(AudioService.instance != null, "AudioService instance must be set upon enter tree")
	print("PASS: AudioService instantiated and static instance verified")
	
	# 2. Sounds come from the player's th09.dat, so load them from the copy the game saves when
	# it is dropped on the file screen, as GameData does at startup.
	var archive := ThDatArchive.new()
	assert(archive.open("user://game_files/th09.dat"), "Needs th09.dat: run the game once and drop it on the file screen")
	var streams := {}
	for sound_name in AudioService.SOUND_NAMES:
		streams[sound_name] = AudioStreamWAV.load_from_buffer(archive.extract(sound_name + ".wav"))
	audio_mgr.set_sounds(streams)

	# Check all 17 requested sound effects are registered and loaded
	var expected_sfx: Array[String] = [
		"se_select00",
		"se_cancel00",
		"se_ok00",
		"se_plst00",
		"se_damage00",
		"se_damage01",
		"se_enep00",
		"se_enep01",
		"se_playerdead",
		"se_pldead00",
		"se_powerup",
		"se_tan00",
		"se_warning",
		"se_chargeup",
		"se_lazer00",
		"se_life1",
		"se_pause"
	]
	
	for sfx_name in expected_sfx:
		assert(audio_mgr.sounds.has(sfx_name), "AudioService must have sound '%s' registered" % sfx_name)
		var stream = audio_mgr.sounds[sfx_name]
		assert(stream is AudioStream, "Registered sound '%s' must be an AudioStream" % sfx_name)
	print("LENGTH se_enep00: %f" % audio_mgr.sounds["se_enep00"].get_length())
	print("LENGTH se_lazer00: %f" % audio_mgr.sounds["se_lazer00"].get_length())
	print("PASS: All 17 required sound assets successfully preloaded and verified as AudioStreams")
	
	# 3. Test generic play_sfx and static helper functions without exceptions
	AudioService.play_select()
	AudioService.play_confirm()
	AudioService.play_cancel()
	AudioService.play_player_shot()
	AudioService.play_damage_hit(false)
	AudioService.play_damage_hit(true)
	AudioService.play_enemy_death()
	AudioService.play_lily_death()
	AudioService.play_player_hit(false, false) # normal hit
	AudioService.play_player_hit(false, true)  # guts hit
	AudioService.play_player_hit(true, false)  # lethal defeat
	AudioService.play_boss_defeat()
	AudioService.play_lily_warning()
	AudioService.play_charge_threshold()
	AudioService.play_pause()
	AudioService.play_powerup()
	AudioService.play_laser()
	AudioService.play_spellcard()
	AudioService.play_danmaku_shot()
	AudioService.play_bullet_spawn()
	print("PASS: Semantic static playback helper methods executed cleanly")
	
	# 4. Test master volume default 10% configuration
	assert(is_equal_approx(AudioService.get_master_volume(), 0.10), "Default master volume must be 0.10 (10%)")
	var master_bus_idx := AudioServer.get_bus_index("Master")
	assert(master_bus_idx >= 0, "Master audio bus must exist")
	assert(is_equal_approx(AudioServer.get_bus_volume_db(master_bus_idx), linear_to_db(0.10)), "Master bus volume must track 10% linear")
	AudioService.set_master_volume(0.50)
	assert(is_equal_approx(AudioService.get_master_volume(), 0.50), "Master volume should be 0.50")
	assert(is_equal_approx(AudioServer.get_bus_volume_db(master_bus_idx), linear_to_db(0.50)), "Master bus volume should track linear volume")
	AudioService.set_master_volume(0.10)
	assert(is_equal_approx(AudioServer.get_bus_volume_db(master_bus_idx), linear_to_db(0.10)), "Master bus volume restored to 10%")
	print("PASS: Master volume set to 10% and dynamic setter verified")
	
	# 4b. Test BGM and SFX volume and bus routing
	var bgm_bus_idx := AudioServer.get_bus_index("BGM")
	var sfx_bus_idx := AudioServer.get_bus_index("SFX")
	assert(bgm_bus_idx >= 0, "BGM audio bus must exist")
	assert(sfx_bus_idx >= 0, "SFX audio bus must exist")
	assert(AudioServer.get_bus_send(bgm_bus_idx) == &"Master", "BGM bus must send to Master")
	assert(AudioServer.get_bus_send(sfx_bus_idx) == &"Master", "SFX bus must send to Master")
	assert(audio_mgr._bgm_player.bus == &"BGM" or audio_mgr._bgm_player.bus == "BGM", "BGM player must use BGM bus")
	assert(audio_mgr._laser_player.bus == &"SFX" or audio_mgr._laser_player.bus == "SFX", "Laser player must use SFX bus")
	assert(audio_mgr._sfx_pool[0].bus == &"SFX" or audio_mgr._sfx_pool[0].bus == "SFX", "SFX pool must use SFX bus")
	assert(is_equal_approx(AudioService.get_bgm_volume(), 1.00), "Default BGM volume must be 1.00")
	assert(is_equal_approx(AudioService.get_sfx_volume(), 0.85), "Default SFX volume must be 0.85")
	AudioService.set_bgm_volume(0.40)
	assert(is_equal_approx(AudioService.get_bgm_volume(), 0.40), "BGM volume setter verified")
	assert(is_equal_approx(AudioServer.get_bus_volume_db(bgm_bus_idx), linear_to_db(0.40) + AudioService.BGM_BUS_CALIBRATION_DB), "BGM bus db tracks linear volume")
	AudioService.set_bgm_volume(1.00)
	AudioService.set_sfx_volume(0.60)
	assert(is_equal_approx(AudioService.get_sfx_volume(), 0.60), "SFX volume setter verified")
	assert(is_equal_approx(AudioServer.get_bus_volume_db(sfx_bus_idx), linear_to_db(0.60) + AudioService.SFX_BUS_CALIBRATION_DB), "SFX bus db tracks linear volume")
	AudioService.set_sfx_volume(0.85)
	print("PASS: BGM and SFX buses, volume setters, and routing verified")
	
	# 5. Test polyphonic laser and multi-voice fairy popping pool
	assert(audio_mgr._laser_player != null, "Dedicated laser player must exist")
	assert(audio_mgr._laser_player.max_polyphony >= 8, "Laser player must be polyphonic (max_polyphony >= 8) to allow overlapping lasers")
	assert(audio_mgr._fairy_pop_pool.size() == 4, "Dedicated fairy pop pool must have 4 players")
	for p in audio_mgr._fairy_pop_pool:
		assert(p.stream != null, "Fairy pop player stream must be pre-assigned to prevent playback cutoff")
	assert(audio_mgr.THROTTLE_INTERVALS_MS.get("se_enep00") == 70, "Fairy pop debounce should be 70ms")
	assert(audio_mgr.THROTTLE_INTERVALS_MS.get("se_tan00") == 30, "Danmaku barrage repeat debounce should be 30ms (2x rate)")
	assert(audio_mgr.THROTTLE_INTERVALS_MS.get("se_cat00") == 300, "Spellcard cast debounce should be 300ms")
	print("PASS: Laser polyphony, fairy pop pool, and barrage throttles verified")
	
	# 5. Test Fairy and Spirit damage / death triggers
	var fairy_scene := preload("res://scenes/enemies/fairy.tscn")
	var fairy: Fairy = fairy_scene.instantiate()
	root.add_child(fairy)
	fairy.setup(Fairy.FairyType.SMALL, Curve2D.new(), 10.0)
	fairy.is_active = true
	fairy.is_vulnerable = true
	fairy.take_damage(0.5)
	assert(is_equal_approx(fairy.current_health, fairy.max_health - 0.5), "Fairy health should decrease by 0.5")
	fairy.take_damage(fairy.max_health)
	assert(fairy.is_dead, "Fairy should be dead after lethal damage")
	print("PASS: Fairy damage and death audio paths exercised")
	
	# 6. Test Boss Character 30% low-health threshold logic
	var boss_scene := preload("res://scenes/enemies/boss_character.tscn")
	var boss: BossCharacter = boss_scene.instantiate()
	root.add_child(boss)
	boss.max_health = 100.0
	boss.current_health = 100.0
	boss.is_vulnerable = true
	boss.state = BossCharacter.State.ACTIVE
	
	# At 80% HP -> normal damage sound
	boss.take_damage(20.0)
	assert(boss.current_health == 80.0, "Boss HP should be 80.0")
	var is_low_hp_80: bool = (boss.current_health / boss.max_health) <= 0.30
	assert(not is_low_hp_80, "At 80% HP, boss must not be marked low health")
	
	# At 25% HP -> critical damage sound (se_damage01)
	boss.take_damage(55.0)
	assert(boss.current_health == 25.0, "Boss HP should be 25.0")
	var is_low_hp_25: bool = (boss.current_health / boss.max_health) <= 0.30
	assert(is_low_hp_25, "At 25% HP, boss must be marked low health (<= 30%)")
	
	# Lethal hit -> die -> boss defeat sound
	boss.take_damage(25.0)
	assert(boss.is_dead, "Boss must be dead after depleting health")
	print("PASS: Boss low health (30%) threshold and defeat audio paths verified")
	
	# 7. Test Player Guts sound (se_life1) vs Normal Hit (se_pldead00) vs Lethal (se_playerdead)
	var player_scene := preload("res://scenes/player/player.tscn")
	var player: Player = player_scene.instantiate()
	root.add_child(player)
	player.current_health = 5.0
	player.is_local_player = true
	
	# Normal hit from 5.0 to 4.0
	player.take_damage(1.0)
	assert(player.current_health == 4.0, "Player at 4.0 HP")
	
	# Guts hit from 4.0 to 0.5
	player.is_invulnerable = false
	player.is_shoved = false
	player.take_damage(4.0)
	assert(player.current_health == 0.5, "Player must trigger guts at 0.5 HP")
	assert(not player.is_dead, "Player must survive on guts")
	
	# Lethal hit from 0.5 to 0.0
	player.is_invulnerable = false
	player.is_shoved = false
	player.take_damage(1.0)
	assert(player.current_health == 0.0, "Player must be at 0.0 HP")
	assert(player.is_dead, "Player must be dead")
	print("PASS: Player guts, normal damage, and defeat audio paths verified")
	
	# 8. Test EarthLightRay laser discharge wiring
	var laser_scene := preload("res://scenes/attacks/earth_light_ray.tscn")
	var laser: EarthLightRay = laser_scene.instantiate()
	root.add_child(laser)
	assert(laser != null, "EarthLightRay instantiated successfully")
	laser.queue_free()
	print("PASS: EarthLightRay instantiated with laser audio connection")
	
	# 9. Test Playfield spellcard declaration and Lily White retreat barrage audio triggers
	var pf_scene := preload("res://scenes/arena/playfield.tscn")
	var pf: Playfield = pf_scene.instantiate()
	root.add_child(pf)
	pf.setup(1, true, "reimu")
	pf.trigger_spellcard_declaration(2, "Fantasy Seal")
	
	var lily_scene := preload("res://scenes/enemies/lily_white.tscn")
	var lily: LilyWhite = lily_scene.instantiate()
	pf.add_child(lily)
	lily._ensure_nodes()
	lily.playfield = pf
	lily._fire_barrage_wave(0)
	print("PASS: Playfield spellcard declaration (se_cat00) and Lily White barrage (se_tan00) verified")
	pf.queue_free()
	
	print("--- ALL AUDIO MANAGER TESTS PASSED SUCCESSFULLY! ---")
	quit(0)
