extends SceneTree

func _init() -> void:
	print("--- Running Test Bullet Cancelability ---")
	test_bullet_resource_flags()
	test_danmaku_bullet_runtime_flags()
	test_shockwave_cancellation_logic()
	test_heavy_shockwave_clears_all()
	print("=== All Bullet Cancelability Tests Passed! ===")
	quit(0)

func assert_true(cond: bool, msg: String) -> void:
	if not cond:
		push_error("Assertion FAILED: %s" % msg)
		quit(1)
	else:
		print("  [OK] %s" % msg)

func test_bullet_resource_flags() -> void:
	print("\n1. Testing DanmakuBulletData resource flags...")
	var cancelable_ids: Array[String] = [
		"res://resources/bullets/red_pellet.tres",
		"res://resources/bullets/white_pellet.tres",
		"res://resources/bullets/blue_pellet_small.tres",
		"res://resources/bullets/green_pellet.tres"
	]
	for path in cancelable_ids:
		var data: DanmakuBulletData = load(path)
		assert_true(data != null, "Loaded %s" % path)
		assert_true(data.can_be_canceled == true, "%s can_be_canceled must be true" % path)

	var immune_ids: Array[String] = [
		"res://resources/bullets/red_talisman.tres",
		"res://resources/bullets/white_talisman.tres",
		"res://resources/bullets/blue_star.tres",
		"res://resources/bullets/green_star.tres",
		"res://resources/bullets/yellow_star.tres",
		"res://resources/bullets/red_oval.tres",
		"res://resources/bullets/white_oval.tres"
	]
	for path in immune_ids:
		var data: DanmakuBulletData = load(path)
		assert_true(data != null, "Loaded %s" % path)
		assert_true(data.can_be_canceled == false, "%s can_be_canceled must be false" % path)

func test_danmaku_bullet_runtime_flags() -> void:
	print("\n2. Testing DanmakuBullet runtime setup propagation...")
	var bullet_scene := preload("res://scenes/bullets/danmaku_bullet.tscn")
	var red_pellet: DanmakuBulletData = load("res://resources/bullets/red_pellet.tres")
	var blue_star: DanmakuBulletData = load("res://resources/bullets/blue_star.tres")
	var red_talisman: DanmakuBulletData = load("res://resources/bullets/red_talisman.tres")
	var white_oval: DanmakuBulletData = load("res://resources/bullets/white_oval.tres")

	var b1: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b1)
	b1.setup(red_pellet, Vector2.ZERO, DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100.0)
	assert_true(b1.can_be_canceled == true, "Red pellet DanmakuBullet runtime can_be_canceled is true")

	var b2: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b2)
	b2.setup(blue_star, Vector2.ZERO, DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100.0)
	assert_true(b2.can_be_canceled == false, "Blue star DanmakuBullet runtime can_be_canceled is false")

	var b3: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b3)
	b3.setup(red_talisman, Vector2.ZERO, DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100.0)
	assert_true(b3.can_be_canceled == false, "Red talisman DanmakuBullet runtime can_be_canceled is false")

	var b4: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b4)
	b4.setup(white_oval, Vector2.ZERO, DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100.0)
	assert_true(b4.can_be_canceled == false, "White oval DanmakuBullet runtime can_be_canceled is false")

	b1.queue_free()
	b2.queue_free()
	b3.queue_free()
	b4.queue_free()

func test_shockwave_cancellation_logic() -> void:
	print("\n3. Testing Shockwave area entrance on DanmakuBullets and EnemyPellets...")
	var shockwave_scene := preload("res://scenes/effects/shockwave.tscn")
	var bullet_scene := preload("res://scenes/bullets/danmaku_bullet.tscn")
	var pellet_scene := preload("res://scenes/bullets/enemy_pellet.tscn")

	var shockwave: Shockwave = shockwave_scene.instantiate()
	root.add_child(shockwave)
	shockwave.setup(Fairy.FairyType.SMALL, Vector2(100.0, 100.0))

	var stats := {"canceled_count": 0}
	shockwave.pellet_canceled.connect(func(_pos: Vector2, _b_count: int):
		stats.canceled_count += 1
	)

	# 3a. Cancelable DanmakuBullet (red_pellet)
	var red_pellet: DanmakuBulletData = load("res://resources/bullets/red_pellet.tres")
	var b_cancelable: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b_cancelable)
	b_cancelable.setup(red_pellet, Vector2(100.0, 100.0), DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100.0)

	shockwave._on_area_entered(b_cancelable)
	assert_true(b_cancelable.is_canceled == true, "Cancelable DanmakuBullet was canceled by Shockwave")
	assert_true(stats.canceled_count == 1, "Shockwave emitted pellet_canceled signal for cancelable DanmakuBullet")

	# 3b. Immune DanmakuBullet (blue_star)
	var blue_star: DanmakuBulletData = load("res://resources/bullets/blue_star.tres")
	var b_immune: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b_immune)
	b_immune.setup(blue_star, Vector2(100.0, 100.0), DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100.0)

	shockwave._on_area_entered(b_immune)
	assert_true(b_immune.is_canceled == false, "Immune DanmakuBullet was NOT canceled by Shockwave")
	assert_true(stats.canceled_count == 1, "Shockwave did NOT emit pellet_canceled for immune bullet")

	# 3c. Immune DanmakuBullet (red_talisman)
	var red_talisman: DanmakuBulletData = load("res://resources/bullets/red_talisman.tres")
	var b_talisman: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b_talisman)
	b_talisman.setup(red_talisman, Vector2(100.0, 100.0), DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100.0)

	shockwave._on_area_entered(b_talisman)
	assert_true(b_talisman.is_canceled == false, "Red talisman was NOT canceled by Shockwave")
	assert_true(stats.canceled_count == 1, "Shockwave did NOT emit pellet_canceled for talisman")

	# 3d. Normal EnemyPellet (cancelable)
	var p_normal: EnemyPellet = pellet_scene.instantiate()
	root.add_child(p_normal)
	p_normal.setup(Vector2(100.0, 100.0), 100.0, Vector2.DOWN, false, false)
	assert_true(p_normal.can_be_canceled == true, "Normal EnemyPellet can_be_canceled is true")

	shockwave._on_area_entered(p_normal)
	assert_true(p_normal.is_canceled == true, "Normal EnemyPellet was canceled by Shockwave")
	assert_true(stats.canceled_count == 2, "Shockwave emitted pellet_canceled for normal EnemyPellet")

	# 3e. Big EnemyPellet (immune)
	var p_big: EnemyPellet = pellet_scene.instantiate()
	root.add_child(p_big)
	p_big.setup(Vector2(100.0, 100.0), 100.0, Vector2.DOWN, true, false)
	assert_true(p_big.can_be_canceled == false, "Big EnemyPellet can_be_canceled is false")

	shockwave._on_area_entered(p_big)
	assert_true(p_big.is_canceled == false, "Big EnemyPellet was NOT canceled by Shockwave")
	assert_true(stats.canceled_count == 2, "Shockwave did NOT emit pellet_canceled for big pellet")

	# 3f. Ring EnemyPellet (immune)
	var p_ring: EnemyPellet = pellet_scene.instantiate()
	root.add_child(p_ring)
	p_ring.setup(Vector2(100.0, 100.0), 100.0, Vector2.DOWN, false, true)
	assert_true(p_ring.can_be_canceled == false, "Ring EnemyPellet can_be_canceled is false")

	shockwave._on_area_entered(p_ring)
	assert_true(p_ring.is_canceled == false, "Ring EnemyPellet was NOT canceled by Shockwave")
	assert_true(stats.canceled_count == 2, "Shockwave did NOT emit pellet_canceled for ring pellet")

	# Cleanup
	b_cancelable.queue_free()
	b_immune.queue_free()
	b_talisman.queue_free()
	p_normal.queue_free()
	p_big.queue_free()
	p_ring.queue_free()
	shockwave.queue_free()

func test_heavy_shockwave_clears_all() -> void:
	print("\n4. Testing HeavyShockwave clears both cancelable and immune Danmaku bullets...")
	var heavy_scene := preload("res://scenes/effects/heavy_shockwave.tscn")
	var bullet_scene := preload("res://scenes/bullets/danmaku_bullet.tscn")
	var blue_star: DanmakuBulletData = load("res://resources/bullets/blue_star.tres")
	var red_pellet: DanmakuBulletData = load("res://resources/bullets/red_pellet.tres")

	var heavy: HeavyShockwave = heavy_scene.instantiate()
	root.add_child(heavy)

	var b_star: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b_star)
	b_star.setup(blue_star, Vector2(100.0, 100.0), DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100.0)

	var b_pellet: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b_pellet)
	b_pellet.setup(red_pellet, Vector2(100.0, 100.0), DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100.0)

	heavy._clear_entity(b_star)
	heavy._clear_entity(b_pellet)

	assert_true(b_star.is_queued_for_deletion(), "HeavyShockwave clears immune blue_star DanmakuBullet")
	assert_true(b_pellet.is_queued_for_deletion(), "HeavyShockwave clears cancelable red_pellet DanmakuBullet")

	b_star.queue_free()
	b_pellet.queue_free()
	heavy.queue_free()
