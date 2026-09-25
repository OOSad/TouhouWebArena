extends SceneTree

func _init() -> void:
	print("--- Running Test Telemetry & Bullet Cap ---")
	test_nodepool_counter()
	test_playfield_telemetry_and_cap()
	test_pofv_bullet_damping()
	print("=== All Telemetry & Bullet Cap Tests Passed! ===")
	quit(0)

func assert_true(cond: bool, msg: String) -> void:
	if not cond:
		push_error("Assertion FAILED: %s" % msg)
		quit(1)
	else:
		print("  [OK] %s" % msg)

func test_nodepool_counter() -> void:
	print("\nTesting NodePool _active_count tracking...")
	var parent := Node2D.new()
	root.add_child(parent)
	
	var pellet_scene := preload("res://scenes/bullets/enemy_pellet.tscn")
	var pool := NodePool.new(pellet_scene, parent, 10)
	
	assert_true(pool.get_active_count() == 0, "Initial active count is 0")
	
	var p1 = pool.acquire()
	var p2 = pool.acquire()
	var p3 = pool.acquire()
	assert_true(pool.get_active_count() == 3, "Active count is 3 after 3 acquires")
	
	pool.release(p2)
	assert_true(pool.get_active_count() == 2, "Active count is 2 after 1 release")
	
	# Test double-release resilience
	var pool_size_before: int = pool._pool.size()
	pool.release(p2) # Re-release already released node
	pool.release(p2) # Call again
	assert_true(pool.get_active_count() == 2, "Double-releasing does not decrease active count below 2")
	assert_true(pool._pool.size() == pool_size_before, "Double-releasing does not add duplicate instances to pool")
	
	pool.release_all_active()
	assert_true(pool.get_active_count() == 0, "Active count is 0 after release_all_active")
	
	# Calling release_all_active again when 0 active instances
	pool.release_all_active()
	assert_true(pool.get_active_count() == 0, "Repeated release_all_active is clean no-op")
	
	pool.clear()
	parent.queue_free()

func test_playfield_telemetry_and_cap() -> void:
	print("\nTesting Playfield entity telemetry and bullet ceiling...")
	var pf_scene := preload("res://scenes/arena/playfield.tscn")
	var pf = pf_scene.instantiate()
	root.add_child(pf)
	
	# Initial telemetry check
	var t = pf.get_entity_telemetry()
	assert_true(t.has("pellets"), "Telemetry includes pellets")
	assert_true(t.has("danmaku"), "Telemetry includes danmaku")
	assert_true(t.has("total_bullets"), "Telemetry includes total_bullets")
	assert_true(t.has("fairies"), "Telemetry includes fairies")
	assert_true(t.has("spirits"), "Telemetry includes spirits")
	assert_true(t.has("total_entities"), "Telemetry includes total_entities")
	assert_true(t.total_bullets == 0, "Initial bullet count is 0")
	
	# Set cap to 5 for test
	pf.max_active_bullets = 5
	
	# Spawn 5 pellets
	for i in range(5):
		pf.spawn_pellet(Vector2(100 + i * 10, 100))
	
	var t_capped = pf.get_entity_telemetry()
	assert_true(t_capped.total_bullets == 5, "Spawned 5 pellets successfully (at cap)")
	
	# Try to spawn 6th pellet (should be blocked by cap)
	pf.spawn_pellet(Vector2(200, 200))
	var t_blocked = pf.get_entity_telemetry()
	assert_true(t_blocked.total_bullets == 5, "6th pellet blocked by max_active_bullets ceiling")
	
	# Try to spawn ring pellet (should also be blocked by cap)
	pf.spawn_ring_pellet(Vector2(200, 200), 200.0, Vector2.DOWN, Color.WHITE)
	var t_blocked_ring = pf.get_entity_telemetry()
	assert_true(t_blocked_ring.total_bullets == 5, "Ring pellet blocked by max_active_bullets ceiling")
	
	# Raise cap back to 350
	pf.max_active_bullets = 350
	pf.spawn_pellet(Vector2(250, 250))
	var t_raised = pf.get_entity_telemetry()
	assert_true(t_raised.total_bullets == 6, "Spawning succeeds after raising max_active_bullets")
	
	pf.queue_free()

func test_pofv_bullet_damping() -> void:
	print("\nTesting PoFV bullet damping and Large Bullet immunity...")
	var pellet_scene := preload("res://scenes/bullets/enemy_pellet.tscn")
	var p_regular: EnemyPellet = pellet_scene.instantiate()
	p_regular.setup(Vector2(50, 50), -1.0, Vector2.ZERO, false)
	assert_true(p_regular.can_be_canceled == true, "Normal pellet can be canceled")
	assert_true(p_regular.is_big == false, "Normal pellet is_big is false")
	
	var p_big: EnemyPellet = pellet_scene.instantiate()
	p_big.setup(Vector2(50, 50), -1.0, Vector2.ZERO, true)
	assert_true(p_big.can_be_canceled == false, "Large Bullet (is_big) CANNOT be canceled by fairy shockwaves")
	assert_true(p_big.is_big == true, "Large Bullet is_big is true")
	
	var p_ring: EnemyPellet = pellet_scene.instantiate()
	p_ring.setup(Vector2(50, 50), 200.0, Vector2.DOWN, false, true, Color.WHITE)
	assert_true(p_ring.can_be_canceled == false, "Ring bullet CANNOT be canceled by fairy shockwaves")
	
	p_regular.queue_free()
	p_big.queue_free()
	p_ring.queue_free()
	
	# Test Playfield bounce lifecycle
	var pf_scene := preload("res://scenes/arena/playfield.tscn")
	var pf: Playfield = pf_scene.instantiate()
	root.add_child(pf)
	
	var received_attacks: Array[Dictionary] = []
	pf.attack_sent.connect(func(pos: Vector2, count: int, combo: int, big: bool, spirit: bool, extra: bool, bounce: int):
		received_attacks.append({
			"pos": pos,
			"count": count,
			"combo": combo,
			"big": big,
			"spirit": spirit,
			"extra": extra,
			"bounce": bounce
		})
	)
	
	# First cancel (fresh pellet, bounce_count = 0)
	pf._on_pellet_canceled(Vector2(100, 100), 0)
	assert_true(received_attacks.size() == 1, "First cancel emitted attack_sent")
	var a1 = received_attacks[0]
	assert_true(a1.count == 1, "First cancel returns exactly 1 pellet (1:1 ratio)")
	assert_true(a1.big == false, "First cancel does not send big pellet")
	assert_true(a1.spirit == false, "First cancel NEVER sends spirits")
	assert_true(a1.bounce == 1, "Returned pellet has bounce_count = 1")
	
	# Second cancel (bounced pellet, bounce_count = 1) -> evolves into Large Bullet
	pf._on_pellet_canceled(Vector2(150, 150), 1)
	assert_true(received_attacks.size() == 2, "Second cancel emitted attack_sent")
	var a2 = received_attacks[1]
	assert_true(a2.count == 0, "Second cancel does not send normal pellets")
	assert_true(a2.big == true, "Second cancel evolves into un-cancellable Large Bullet")
	assert_true(a2.spirit == false, "Second cancel NEVER sends spirits")
	assert_true(a2.bounce == 2, "Large bullet has bounce_count = 2")
	
	pf.queue_free()
