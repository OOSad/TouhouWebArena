extends SceneTree

# Test suite for Boss & Lily White Defeat Reward Drops (Pickups)

var log_lines: Array[String] = []

func log_msg(msg: String) -> void:
	print(msg)
	log_lines.append(msg)

func _init() -> void:
	log_msg("--- Running Test Suite: Defeat Reward Drops (Pickups) ---")
	
	test_pickup_item_initialization()
	test_straight_up_physics()
	test_player_focus_collector_radius()
	test_spawn_single_defeat_pickup()
	test_g_pickup_recharge()
	test_point_pickup_level4()
	test_ex_pickup_extra_attack()
	test_bullet_pickup_cluster()
	test_arena_bullet_pickup_dispatch()
	
	log_msg("--- All Pickup Item Tests Passed Successfully! ---")
	
	var file := FileAccess.open("res://tests/test_pickup_items.log", FileAccess.WRITE)
	if file:
		file.store_string("\n".join(log_lines) + "\n")
		file.close()
	
	quit(0)

func test_pickup_item_initialization() -> void:
	print("[TEST] test_pickup_item_initialization...")
	var scene: PackedScene = load("res://scenes/items/pickup_item.tscn")
	assert(scene != null, "Failed to load pickup_item.tscn")
	
	var item: PickupItem = scene.instantiate() as PickupItem
	assert(item != null, "Failed to instantiate PickupItem")
	
	root.add_child(item)
	item.setup(PickupItem.Type.G, Vector2(200.0, 300.0))
	assert(item.item_type == PickupItem.Type.G, "Item type should be G")
	assert(item.position == Vector2(200.0, 300.0), "Position should match spawn pos")
	assert(item.velocity.x == 0.0, "Initial horizontal velocity must be strictly 0")
	assert(item.velocity.y < 0.0, "Initial vertical velocity must be upward (negative)")
	assert(item.collision_layer == 16, "Collision layer should be 16 (pickups)")
	
	item.queue_free()
	log_msg("  -> Passed.")

func test_straight_up_physics() -> void:
	print("[TEST] test_straight_up_physics...")
	var scene: PackedScene = load("res://scenes/items/pickup_item.tscn")
	var item: PickupItem = scene.instantiate() as PickupItem
	root.add_child(item)
	item.setup(PickupItem.Type.EX, Vector2(100.0, 500.0), null, 400.0)
	
	var initial_vy: float = item.velocity.y
	assert(initial_vy == -400.0, "Expected -400.0 initial vertical velocity")
	
	# Simulate 0.5s of physics
	item._physics_process(0.5)
	assert(item.velocity.x == 0.0, "Horizontal velocity must remain strictly 0 during flight")
	assert(item.velocity.y > initial_vy, "Velocity Y should increase (decelerate) due to gravity")
	assert(item.position.x == 100.0, "Position X must remain constant in straight vertical motion")
	
	# Simulate 3.0s of physics to reach terminal fall velocity
	item._physics_process(3.0)
	assert(is_equal_approx(item.velocity.y, item.max_fall_speed), "Velocity Y should clamp to max_fall_speed")
	assert(item.velocity.x == 0.0, "Horizontal velocity must still be 0")
	
	item.queue_free()
	log_msg("  -> Passed.")

func test_player_focus_collector_radius() -> void:
	print("[TEST] test_player_focus_collector_radius...")
	var player_scene: PackedScene = load("res://scenes/player/player.tscn")
	assert(player_scene != null, "Failed to load player.tscn")
	var player: Player = player_scene.instantiate() as Player
	root.add_child(player)
	player._ready()
	
	assert(player.item_collector != null, "Player must have item_collector")
	assert(player.collector_shape != null, "Player must have collector_shape")
	
	# Default unfocused mode
	player.is_focusing = false
	player._update_pickup_collection(0.016)
	var radius_unfocused: float = (player.collector_shape.shape as CircleShape2D).radius
	assert(is_equal_approx(radius_unfocused, player.BASE_PICKUP_RADIUS), "Unfocused radius should be BASE_PICKUP_RADIUS (24px)")
	
	# Focused mode
	player.is_focusing = true
	player._update_pickup_collection(0.016)
	var radius_focused: float = (player.collector_shape.shape as CircleShape2D).radius
	assert(is_equal_approx(radius_focused, player.FOCUS_PICKUP_RADIUS), "Focused radius should expand to FOCUS_PICKUP_RADIUS (48px)")
	
	player.queue_free()
	log_msg("  -> Passed.")

func test_spawn_single_defeat_pickup() -> void:
	print("[TEST] test_spawn_single_defeat_pickup...")
	var playfield_scene: PackedScene = load("res://scenes/arena/playfield.tscn")
	var pf: Playfield = playfield_scene.instantiate() as Playfield
	root.add_child(pf)
	pf._ready()
	pf.setup(1, true, "reimu")
	
	var initial_children_count: int = pf.entities_layer.get_child_count()
	var spawned_item: PickupItem = pf.spawn_defeat_pickups(Vector2(300.0, 200.0))
	assert(spawned_item != null, "spawn_defeat_pickups must return the spawned item")
	assert(pf.entities_layer.get_child_count() == initial_children_count + 1, "Exactly ONE pickup item must be spawned")
	assert(spawned_item.position == Vector2(300.0, 200.0), "Pickup spawn position must match defeat point")
	assert(spawned_item.velocity.x == 0.0, "Velocity X must be 0")
	assert(spawned_item.velocity.y == -390.0, "Velocity Y must be upward -390")
	
	pf.queue_free()
	log_msg("  -> Passed.")

func test_g_pickup_recharge() -> void:
	print("[TEST] test_g_pickup_recharge...")
	var playfield_scene: PackedScene = load("res://scenes/arena/playfield.tscn")
	var pf: Playfield = playfield_scene.instantiate() as Playfield
	root.add_child(pf)
	pf._ready()
	pf.setup(1, true, "reimu")
	
	# Drain player charge
	pf.player.passive_charge = 1.0
	pf.player.charge_updated.emit(pf.player.active_charge, pf.player.passive_charge, pf.player.charge_segments)
	assert(pf.player.passive_charge == 1.0, "Charge should start at 1.0")
	
	# Create G pickup and collect
	var item_scene: PackedScene = load("res://scenes/items/pickup_item.tscn")
	var item: PickupItem = item_scene.instantiate() as PickupItem
	pf.entities_layer.add_child(item)
	item.setup(PickupItem.Type.G, Vector2(300.0, 500.0), pf)
	item.collected.connect(pf._on_pickup_collected)
	
	item.collect(pf.player)
	assert(is_equal_approx(pf.player.passive_charge, float(pf.player.charge_segments)), "G pickup should fully recharge passive_charge to max segments (4.0)")
	
	pf.queue_free()
	log_msg("  -> Passed.")

func test_point_pickup_level4() -> void:
	print("[TEST] test_point_pickup_level4...")
	var playfield_scene: PackedScene = load("res://scenes/arena/playfield.tscn")
	var pf: Playfield = playfield_scene.instantiate() as Playfield
	root.add_child(pf)
	pf._ready()
	pf.setup(1, true, "reimu")
	
	var result := {"pnum": 0, "level": 0}
	pf.spellcard_activated.connect(func(p_num: int, lvl: int, _rank: int):
		result["pnum"] = p_num
		result["level"] = lvl
	)
	
	var item_scene: PackedScene = load("res://scenes/items/pickup_item.tscn")
	var item: PickupItem = item_scene.instantiate() as PickupItem
	pf.entities_layer.add_child(item)
	item.setup(PickupItem.Type.POINT, Vector2(300.0, 500.0), pf)
	item.collected.connect(pf._on_pickup_collected)
	
	item.collect(pf.player)
	assert(result["pnum"] == 1, "Expected spellcard from player 1")
	assert(result["level"] == 4, "POINT pickup must fire Level 4 Spellcard against opponent")
	
	pf.queue_free()
	log_msg("  -> Passed.")

func test_ex_pickup_extra_attack() -> void:
	print("[TEST] test_ex_pickup_extra_attack...")
	var playfield_scene: PackedScene = load("res://scenes/arena/playfield.tscn")
	var pf: Playfield = playfield_scene.instantiate() as Playfield
	root.add_child(pf)
	pf._ready()
	pf.setup(1, true, "reimu")
	
	var result := {"attack_sent_count": 0, "had_extra_attack": false}
	pf.attack_sent.connect(func(_pos: Vector2, _pellets: int, _combo: int, _big: bool, _spirit: bool, extra: bool, _bounce: int):
		result["attack_sent_count"] += 1
		if extra:
			result["had_extra_attack"] = true
	)
	
	var item_scene: PackedScene = load("res://scenes/items/pickup_item.tscn")
	var item: PickupItem = item_scene.instantiate() as PickupItem
	pf.entities_layer.add_child(item)
	item.setup(PickupItem.Type.EX, Vector2(300.0, 500.0), pf)
	item.collected.connect(pf._on_pickup_collected)
	
	item.collect(pf.player)
	assert(result["attack_sent_count"] >= 1, "EX pickup must emit attack_sent")
	assert(result["had_extra_attack"] == true, "EX pickup must have extra attack flag set")
	
	pf.queue_free()
	log_msg("  -> Passed.")

func test_bullet_pickup_cluster() -> void:
	print("[TEST] test_bullet_pickup_cluster...")
	var playfield_scene: PackedScene = load("res://scenes/arena/playfield.tscn")
	var pf: Playfield = playfield_scene.instantiate() as Playfield
	root.add_child(pf)
	pf._ready()
	pf.setup(1, true, "reimu")
	
	var result := {"pellet_count": 0, "had_big_pellet": false}
	pf.attack_sent.connect(func(_pos: Vector2, pellets: int, _combo: int, big: bool, _spirit: bool, _extra: bool, _bounce: int):
		result["pellet_count"] = pellets
		result["had_big_pellet"] = big
	)
	
	var item_scene: PackedScene = load("res://scenes/items/pickup_item.tscn")
	var item: PickupItem = item_scene.instantiate() as PickupItem
	pf.entities_layer.add_child(item)
	item.setup(PickupItem.Type.BULLET, Vector2(300.0, 500.0), pf)
	item.collected.connect(pf._on_pickup_collected)
	
	item.collect(pf.player)
	assert(result["pellet_count"] == 30, "BULLET pickup must send exactly 30 large pellets")
	assert(result["had_big_pellet"] == true, "BULLET pickup must include big pellet")
	
	pf.queue_free()
	log_msg("  -> Passed.")

func test_arena_bullet_pickup_dispatch() -> void:
	print("[TEST] test_arena_bullet_pickup_dispatch...")
	var arena_script = load("res://scenes/arena/arena.gd")
	var arena = arena_script.new()
	root.add_child(arena)
	
	var overlay := Node2D.new()
	arena.add_child(overlay)
	arena.effects_overlay = overlay
	
	var p1_cont := SubViewportContainer.new()
	var p2_cont := SubViewportContainer.new()
	arena.add_child(p1_cont)
	arena.add_child(p2_cont)
	
	var playfield_scene: PackedScene = load("res://scenes/arena/playfield.tscn")
	var p2_pf: Playfield = playfield_scene.instantiate() as Playfield
	arena.add_child(p2_pf)
	
	# Dispatch 30 pellets with has_big_pellet = true (the 弾 BULLET pickup payload)
	arena._send_attack_between_fields(p1_cont, p2_cont, p2_pf, Vector2(300.0, 500.0), 30, Color.RED, true, false, false, "reimu", 1)
	
	# Verify that the spawned mote has BIG_PELLET payload
	var motes: Array = []
	for child in overlay.get_children():
		if child is TravelMote and child.visible:
			motes.append(child)
	
	assert(motes.size() >= 1, "Mote should be spawned immediately for the bullet barrage")
	var first_mote: TravelMote = motes[0] as TravelMote
	assert(first_mote.payload == TravelMote.MotePayload.BIG_PELLET, "Mote payload must be BIG_PELLET when has_big_pellet=true and pellet_count >= 10")
	
	arena.queue_free()
	log_msg("  -> Passed.")
