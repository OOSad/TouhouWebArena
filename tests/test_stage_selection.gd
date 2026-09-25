extends SceneTree

## Unit test verifying shared stage selection and stage music routing.

var arena: Node = null
var frame_count: int = 0
var test_phase: int = 0

func _init() -> void:
	print("\n=================================================")
	print(" RUNNING STAGE SELECTION & BGM INTEGRATION TEST  ")
	print("=================================================")
	
	var reimu_data: CharacterData = CharacterData.get_character("reimu")
	var marisa_data: CharacterData = CharacterData.get_character("marisa")
	var youmu_data: CharacterData = CharacterData.get_character("youmu")
	var cirno_data: CharacterData = CharacterData.get_character("cirno")
	var sakuya_data: CharacterData = CharacterData.get_character("sakuya")

	assert(reimu_data != null, "Reimu CharacterData must exist")
	assert(marisa_data != null, "Marisa CharacterData must exist")
	assert(youmu_data != null, "Youmu CharacterData must exist")
	assert(cirno_data != null, "Cirno CharacterData must exist")
	assert(sakuya_data != null, "Sakuya CharacterData must exist")

	# 1. Verify Home Stage IDs
	assert(reimu_data.get_home_stage_id() == "bamboo_road", "Reimu home stage should be bamboo_road")
	assert(marisa_data.get_home_stage_id() == "bamboo_road", "Marisa home stage should be bamboo_road")
	assert(youmu_data.get_home_stage_id() == "hakugyokurou_stairs", "Youmu home stage should be hakugyokurou_stairs")
	assert(cirno_data.get_home_stage_id() == "misty_lake", "Cirno home stage should be misty_lake")
	assert(sakuya_data.get_home_stage_id() == "flowering_night", "Sakuya home stage should be flowering_night")
	print("  [PASS] Character home stage IDs verified.")

	# 2. Verify Home Stage Scenes
	var reimu_scene: PackedScene = reimu_data.get_home_stage_scene()
	var marisa_scene: PackedScene = marisa_data.get_home_stage_scene()
	var youmu_scene: PackedScene = youmu_data.get_home_stage_scene()
	var cirno_scene: PackedScene = cirno_data.get_home_stage_scene()
	var sakuya_scene: PackedScene = sakuya_data.get_home_stage_scene()

	assert(reimu_scene != null and reimu_scene.resource_path.contains("bamboo_road"), "Reimu scene must be Bamboo Road")
	assert(marisa_scene != null and marisa_scene.resource_path.contains("bamboo_road"), "Marisa scene must be Bamboo Road")
	assert(youmu_scene != null and youmu_scene.resource_path.contains("hakugyokurou"), "Youmu scene must be Hakugyokurou Stairs")
	assert(cirno_scene != null and cirno_scene.resource_path.contains("misty_lake"), "Cirno scene must be Misty Lake")
	assert(sakuya_scene != null and sakuya_scene.resource_path.contains("flowering_night"), "Sakuya scene must be Flowering Night")
	print("  [PASS] Character home stage scenes verified.")
	
	# 3. Verify BGM Path Resolution & Fallbacks
	var reimu_bgm: String = reimu_data.get_stage_bgm_path()
	assert(reimu_bgm.contains("02_spring_lane.ogg"), "Reimu BGM should be Spring Lane OGG")
	
	var youmu_bgm: String = youmu_data.get_stage_bgm_path()
	assert(youmu_bgm.contains("10_ancient_temple.ogg"), "Youmu BGM should be Ancient Temple OGG")
	
	var cirno_bgm: String = cirno_data.get_stage_bgm_path()
	assert(cirno_bgm.contains("07_adventure_of_the_lovestruck_tomboy.ogg"), "Cirno BGM should be 07_adventure_of_the_lovestruck_tomboy.ogg")

	var sakuya_bgm: String = sakuya_data.get_stage_bgm_path()
	assert(sakuya_bgm.contains("04_flowering_night.ogg"), "Sakuya BGM should be 04_flowering_night.ogg")
	print("  [PASS] BGM path resolution verified (Reimu: %s | Youmu: %s | Cirno: %s | Sakuya: %s)." % [reimu_bgm, youmu_bgm, cirno_bgm, sakuya_bgm])



func _process(_delta: float) -> bool:
	frame_count += 1
	
	if test_phase == 0 and frame_count == 1:
		# Test Phase 1: Youmu vs Youmu in Arena
		var game_mgr = root.get_node_or_null("GameManager")
		if game_mgr != null:
			game_mgr.p1_character = "Youmu Konpaku"
			game_mgr.p2_character = "Youmu Konpaku"
		
		var arena_scene: PackedScene = load("res://scenes/arena/arena.tscn")
		arena = arena_scene.instantiate()
		root.add_child(arena)
		test_phase = 1
		
	elif test_phase == 1 and frame_count == 3:
		var p1_field: Playfield = arena.get_node_or_null("%P1Playfield")
		var p2_field: Playfield = arena.get_node_or_null("%P2Playfield")
		assert(p1_field != null and p2_field != null, "Playfields must exist")
		
		var p1_stage = _get_stage_child(p1_field)
		var p2_stage = _get_stage_child(p2_field)
		assert(p1_stage != null and p2_stage != null, "Both playfields must have a 3D stage")
		assert(p1_stage.get_script().resource_path.contains("hakugyokurou"), "P1 stage must be Hakugyokurou for Youmu vs Youmu")
		assert(p2_stage.get_script().resource_path.contains("hakugyokurou"), "P2 stage must be Hakugyokurou for Youmu vs Youmu")
		print("  [PASS] Same-stage match (Youmu vs Youmu) successfully synchronized Hakugyokurou to both fields.")
		
		arena.queue_free()
		test_phase = 2
		
	elif test_phase == 2 and frame_count == 4:
		# Test Phase 2: Reimu vs Marisa (both share Bamboo Road)
		var game_mgr = root.get_node_or_null("GameManager")
		if game_mgr != null:
			game_mgr.p1_character = "Reimu Hakurei"
			game_mgr.p2_character = "Marisa Kirisame"
		
		var arena_scene: PackedScene = load("res://scenes/arena/arena.tscn")
		arena = arena_scene.instantiate()
		root.add_child(arena)
		test_phase = 3
		
	elif test_phase == 3 and frame_count == 6:
		var p1_field: Playfield = arena.get_node_or_null("%P1Playfield")
		var p2_field: Playfield = arena.get_node_or_null("%P2Playfield")
		var p1_stage = _get_stage_child(p1_field)
		var p2_stage = _get_stage_child(p2_field)
		assert(p1_stage != null and p2_stage != null, "Both playfields must have a 3D stage")
		assert(p1_stage.get_script().resource_path.contains("bamboo_road"), "P1 stage must be Bamboo Road for Reimu vs Marisa")
		assert(p2_stage.get_script().resource_path.contains("bamboo_road"), "P2 stage must be Bamboo Road for Reimu vs Marisa")
		print("  [PASS] Same-stage match (Reimu vs Marisa) successfully synchronized Bamboo Road to both fields.")
		
		arena.queue_free()
		test_phase = 4
		
	elif test_phase == 4 and frame_count == 7:
		# Test Phase 3: Reimu vs Youmu (different home stages -> 1 stage chosen, both fields synchronized)
		var game_mgr = root.get_node_or_null("GameManager")
		if game_mgr != null:
			game_mgr.p1_character = "Reimu Hakurei"
			game_mgr.p2_character = "Youmu Konpaku"
		
		var arena_scene: PackedScene = load("res://scenes/arena/arena.tscn")
		arena = arena_scene.instantiate()
		root.add_child(arena)
		test_phase = 5
		
	elif test_phase == 5 and frame_count == 9:
		var p1_field: Playfield = arena.get_node_or_null("%P1Playfield")
		var p2_field: Playfield = arena.get_node_or_null("%P2Playfield")
		var p1_stage = _get_stage_child(p1_field)
		var p2_stage = _get_stage_child(p2_field)
		assert(p1_stage != null and p2_stage != null, "Both playfields must have a 3D stage")
		
		var p1_is_youmu: bool = p1_stage.get_script().resource_path.contains("hakugyokurou")
		var p2_is_youmu: bool = p2_stage.get_script().resource_path.contains("hakugyokurou")
		assert(p1_is_youmu == p2_is_youmu, "Both playfields MUST have identical stage when home stages differ!")
		print("  [PASS] Different-stage match (Reimu vs Youmu) successfully synchronized stage ('%s') across both fields." % [
			"Hakugyokurou" if p1_is_youmu else "Bamboo Road"
		])
		
		arena.queue_free()
		test_phase = 6
		
	elif test_phase == 6 and frame_count == 10:
		# Test Phase 4: Cirno vs Cirno (Misty Lake)
		var game_mgr = root.get_node_or_null("GameManager")
		if game_mgr != null:
			game_mgr.p1_character = "Cirno"
			game_mgr.p2_character = "Cirno"
		
		var arena_scene: PackedScene = load("res://scenes/arena/arena.tscn")
		arena = arena_scene.instantiate()
		root.add_child(arena)
		test_phase = 7
		
	elif test_phase == 7 and frame_count == 12:
		var p1_field: Playfield = arena.get_node_or_null("%P1Playfield")
		var p2_field: Playfield = arena.get_node_or_null("%P2Playfield")
		var p1_stage = _get_stage_child(p1_field)
		var p2_stage = _get_stage_child(p2_field)
		assert(p1_stage != null and p2_stage != null, "Both playfields must have a 3D stage")
		assert(p1_stage.get_script().resource_path.contains("misty_lake"), "P1 stage must be Misty Lake for Cirno vs Cirno")
		assert(p2_stage.get_script().resource_path.contains("misty_lake"), "P2 stage must be Misty Lake for Cirno vs Cirno")
		print("  [PASS] Same-stage match (Cirno vs Cirno) successfully synchronized Misty Lake to both fields.")
		
		arena.queue_free()
		test_phase = 8
		
	elif test_phase == 8 and frame_count == 13:
		# Test Phase 5: Sakuya vs Sakuya (Flowering Night)
		var game_mgr = root.get_node_or_null("GameManager")
		if game_mgr != null:
			game_mgr.p1_character = "Sakuya Izayoi"
			game_mgr.p2_character = "Sakuya Izayoi"
		
		var arena_scene: PackedScene = load("res://scenes/arena/arena.tscn")
		arena = arena_scene.instantiate()
		root.add_child(arena)
		test_phase = 9
		
	elif test_phase == 9 and frame_count == 15:
		var p1_field: Playfield = arena.get_node_or_null("%P1Playfield")
		var p2_field: Playfield = arena.get_node_or_null("%P2Playfield")
		var p1_stage = _get_stage_child(p1_field)
		var p2_stage = _get_stage_child(p2_field)
		assert(p1_stage != null and p2_stage != null, "Both playfields must have a 3D stage")
		assert(p1_stage.get_script().resource_path.contains("flowering_night"), "P1 stage must be Flowering Night for Sakuya vs Sakuya")
		assert(p2_stage.get_script().resource_path.contains("flowering_night"), "P2 stage must be Flowering Night for Sakuya vs Sakuya")
		print("  [PASS] Same-stage match (Sakuya vs Sakuya) successfully synchronized Flowering Night to both fields.")
		
		arena.queue_free()
		print("-------------------------------------------------")
		print(" STAGE SELECTION & BGM INTEGRATION TEST PASSED!  ")
		print("-------------------------------------------------\n")
		quit(0)
		return true
		
	return false


func _get_stage_child(pf: Playfield) -> Node:
	if pf == null or pf.background_3d_container == null:
		return null
	var vp: SubViewport = pf.background_3d_container.get_node_or_null("%Background3DViewport")
	if vp == null:
		vp = pf.background_3d_container.get_node_or_null("Background3DViewport")
	if vp == null:
		return null
	for child in vp.get_children():
		if child is Node3D:
			return child
	return null

