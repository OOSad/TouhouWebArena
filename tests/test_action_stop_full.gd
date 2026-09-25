extends SceneTree

var arena: Node = null
var frame_count: int = 0
var test_step: int = 0
var timer: float = 0.0

func _init() -> void:
	print("==================================================")
	print("[TEST] Starting Full Action Stop & Banner Verification")
	print("==================================================")
	
	var arena_scene = load("res://scenes/arena/arena.tscn")
	if not arena_scene:
		printerr("[TEST FAIL] Could not load arena.tscn")
		quit(1)
		return
	
	arena = arena_scene.instantiate()
	root.add_child(arena)

func _process(delta: float) -> bool:
	frame_count += 1
	
	if test_step == 0:
		if frame_count < 5:
			return false
		
		var p1_field: Playfield = arena.p1_playfield
		var p2_field: Playfield = arena.p2_playfield
		if not p1_field or not p2_field or not p1_field.player or not p2_field.player:
			if frame_count < 20:
				return false
			printerr("[TEST FAIL] Playfields or players not initialized")
			quit(1)
			return true
		print("[TEST PASS] Arena and playfields loaded.")
		
		# Test CharacterData spell_banner_texture
		var reimu_data: CharacterData = CharacterData.get_character("reimu")
		var marisa_data: CharacterData = CharacterData.get_character("marisa")
		if not reimu_data or not reimu_data.spell_banner_texture:
			printerr("[TEST FAIL] Reimu CharacterData is missing spell_banner_texture")
			quit(1)
			return true
		print("[TEST PASS] Reimu spell_banner_texture verified: ", reimu_data.spell_banner_texture.resource_path)
		
		if not marisa_data or not marisa_data.spell_banner_texture:
			printerr("[TEST FAIL] Marisa CharacterData is missing spell_banner_texture")
			quit(1)
			return true
		print("[TEST PASS] Marisa spell_banner_texture verified: ", marisa_data.spell_banner_texture.resource_path)
		
		# Test Triggering Action Stop
		print("[TEST] Triggering Level 2 Spellcard on P1...")
		arena._on_spellcard_activated(1, 2, 1)
		
		if not paused:
			printerr("[TEST FAIL] SceneTree should be paused during Action Stop")
			quit(1)
			return true
		print("[TEST PASS] SceneTree is paused.")
		
		if not arena._action_stop_active:
			printerr("[TEST FAIL] _action_stop_active should be true")
			quit(1)
			return true
		print("[TEST PASS] _action_stop_active is true.")
		
		# Verify ViewportContainers are paused
		if arena.p1_viewport_container.can_process():
			printerr("[TEST FAIL] P1 ViewportContainer should NOT be able to process while paused")
			quit(1)
			return true
		print("[TEST PASS] P1 ViewportContainer is paused (can_process == false).")
		
		if arena.p2_viewport_container.can_process():
			printerr("[TEST FAIL] P2 ViewportContainer should NOT be able to process while paused")
			quit(1)
			return true
		print("[TEST PASS] P2 ViewportContainer is paused (can_process == false).")
		
		# Verify Players cannot process
		if p1_field.player.can_process():
			printerr("[TEST FAIL] P1 Player should NOT be able to process while paused")
			quit(1)
			return true
		print("[TEST PASS] P1 Player cannot process.")
		
		if p2_field.player.can_process():
			printerr("[TEST FAIL] P2 Player should NOT be able to process while paused")
			quit(1)
			return true
		print("[TEST PASS] P2 Player cannot process.")
		
		# Verify is_action_stopped is true on both players
		if not p1_field.player.is_action_stopped:
			printerr("[TEST FAIL] P1 Player is_action_stopped should be true")
			quit(1)
			return true
		print("[TEST PASS] P1 Player is_action_stopped is true.")
		
		if not p2_field.player.is_action_stopped:
			printerr("[TEST FAIL] P2 Player is_action_stopped should be true")
			quit(1)
			return true
		print("[TEST PASS] P2 Player is_action_stopped is true.")
		
		# Verify Caster Player has red silhouette shader active
		if not p1_field.player or p1_field.player.sprite.material == null:
			printerr("[TEST FAIL] P1 Player sprite should have red silhouette shader active")
			quit(1)
			return true
		print("[TEST PASS] P1 Player has red silhouette shader active.")
		
		# Verify SpellBanner instantiated on caster field
		var found_banner: bool = false
		for child in p1_field.effects_layer.get_children():
			if child.has_method("play_banner"):
				found_banner = true
				break
		if not found_banner:
			printerr("[TEST FAIL] SpellBanner node not found in caster effects_layer")
			quit(1)
			return true
		print("[TEST PASS] SpellBanner found in caster effects_layer.")
		
		# Verify authentic warning banner is spawned on target field
		var found_warning: bool = false
		for child in p2_field.effects_layer.get_children():
			if child is Control and child.name == "SpellcardWarningContainer":
				found_warning = true
				break
		if not found_warning:
			printerr("[TEST FAIL] SpellcardWarningContainer should be spawned on opponent's field")
			quit(1)
			return true
		print("[TEST PASS] Target field has authentic SpellcardWarningContainer.")
		
		test_step = 1
		timer = 0.0
		return false
	
	elif test_step == 1:
		timer += delta
		if timer >= 0.7:
			# Verify completion of Action Stop
			var p1_field: Playfield = arena.p1_playfield
			var p2_field: Playfield = arena.p2_playfield
			if paused:
				printerr("[TEST FAIL] SceneTree should be unpaused after Action Stop finishes")
				quit(1)
				return true
			print("[TEST PASS] SceneTree successfully unpaused.")
			
			if arena._action_stop_active:
				printerr("[TEST FAIL] _action_stop_active should be false after completion")
				quit(1)
				return true
			print("[TEST PASS] _action_stop_active is false.")
			
			if p1_field.player.is_action_stopped or p2_field.player.is_action_stopped:
				printerr("[TEST FAIL] is_action_stopped should be false on both players")
				quit(1)
				return true
			print("[TEST PASS] is_action_stopped restored to false on both players.")
			
			if not p1_field.player.can_process() or not p2_field.player.can_process():
				printerr("[TEST FAIL] Players should be able to process after unpausing")
				quit(1)
				return true
			print("[TEST PASS] Players can process again.")
			
			if p1_field.player.sprite.material != null:
				printerr("[TEST FAIL] P1 Player sprite material should be restored to null")
				quit(1)
				return true
			print("[TEST PASS] P1 Player red silhouette restored to normal.")
			
			print("==================================================")
			print("[TEST] ALL ACTION STOP CHECKS PASSED SUCCESSFULLY!")
			print("==================================================")
			quit(0)
			return true
			
	return false
