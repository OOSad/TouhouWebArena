extends SceneTree

func _init() -> void:
	print("--- BEGIN DANMAKU BATCHING AND LIFECYCLE TEST ---")
	
	# 1. Test Unified Danmaku Atlas Batching & Texture RID Sharing
	# Bullet sprites come from the player's th09.dat and th15.dat, so this needs the copies
	# the game saves when they're dropped on its file screen.
	var archive := ThDatArchive.new()
	assert(archive.open("user://game_files/th09.dat"), "Needs th09.dat: run the game once and drop it on the file screen")
	var etama := ThAnm.new()
	assert(etama.parse(archive.extract("etama.anm")), "etama.anm must parse")
	var th15 := ThDatArchive.new()
	assert(th15.open("user://game_files/th15.dat"), "Needs th15.dat: run the game once and drop it on the file screen")
	var th15_bullets := ThAnm.new()
	assert(th15_bullets.parse(th15.extract("bullet.anm"), true), "th15 bullet.anm must parse")
	var th15_images := {}
	for key in BulletSprites.TH15_KEYS:
		th15_images[key] = BulletSprites.cut_th15(th15_bullets, key)
	var filled := BulletSprites.apply(etama, th15_images)
	var red_res: DanmakuBulletData = load("res://resources/bullets/red_pellet.tres")
	var oval_res: DanmakuBulletData = load("res://resources/bullets/red_oval.tres")
	var star_res: DanmakuBulletData = load("res://resources/bullets/blue_star.tres")
	
	assert(red_res != null and oval_res != null and star_res != null, "Bullet resources must load")
	assert(red_res.texture is AtlasTexture and oval_res.texture is AtlasTexture and star_res.texture is AtlasTexture, "All bullets must use AtlasTexture")
	
	var atlas_red: AtlasTexture = red_res.texture as AtlasTexture
	var atlas_oval: AtlasTexture = oval_res.texture as AtlasTexture
	var atlas_star: AtlasTexture = star_res.texture as AtlasTexture
	
	# Crucial: Under Godot 4 2D batching, all AtlasTextures sharing the same master atlas share the exact same GPU RID!
	assert(atlas_red.atlas != null and atlas_red.atlas == atlas_oval.atlas and atlas_red.atlas == atlas_star.atlas, "All bullets must share the one runtime sprite sheet")
	assert(EnemyPellet.pellet_red_tex is AtlasTexture and (EnemyPellet.pellet_red_tex as AtlasTexture).atlas == atlas_red.atlas, "Enemy pellets must share it too")
	assert(filled.size() == BulletSprites.SPRITES.size() + 3, "Every mapped bullet type, plus Clownpiece's three (flame, star, glow ball), must get a sprite")
	for clownpiece in ["clownpiece_flame_red", "clownpiece_big_star_blue"]:
		var data := load(BulletSprites.BULLETS % clownpiece) as DanmakuBulletData
		assert((data.texture as AtlasTexture).atlas == atlas_red.atlas, "%s must share the sheet too" % clownpiece)
	for path in BulletSprites.LEFTOVER_TEXTURES:
		assert((load(path) as AtlasTexture).atlas == atlas_red.atlas, "%s must share the sheet too" % path.get_file())
	assert(atlas_red.atlas.get_rid() == atlas_oval.atlas.get_rid(), "Master atlas GPU RID must match across distinct bullet shapes")
	
	# Uniform z_index eliminates batch interruptions
	assert(DanmakuBullet.get_z_index_for_texture() == 10, "Danmaku bullets must share uniform z_index = 10")
	print("PASS: Unified AtlasTexture texture RID sharing and uniform z_index ensure single-call Danmaku batching")
	
	# 2. Test CircleShape2D Caching (Zero Allocations on Acquire)
	var shape1: CircleShape2D = DanmakuBullet.get_cached_circle_shape(6.0)
	var shape2: CircleShape2D = DanmakuBullet.get_cached_circle_shape(6.0)
	var shape3: CircleShape2D = DanmakuBullet.get_cached_circle_shape(12.0)
	
	assert(shape1 == shape2, "Identical radius must return identical cached CircleShape2D reference")
	assert(shape1 != shape3, "Different radius must return distinct cached CircleShape2D reference")
	assert(shape1.radius == 6.0 and shape3.radius == 12.0, "Cached shapes must preserve correct radius")
	print("PASS: Static CircleShape2D caching eliminates per-bullet heap allocations")
	
	# 3. Test Playfield Exit Despawn Checks
	var bullet_scene := preload("res://scenes/bullets/danmaku_bullet.tscn")
	var white_pellet: DanmakuBulletData = load("res://resources/bullets/white_pellet.tres")
	
	# 3a. Linear bullet inside screen should NOT despawn
	var b_inside: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b_inside)
	b_inside.setup(white_pellet, Vector2(300.0, 500.0), DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 200.0)
	b_inside._physics_process(0.1)
	assert(not b_inside.is_queued_for_deletion(), "Bullet inside playfield must remain active")
	b_inside.queue_free()
	
	# 3b. Linear bullet past bottom floor (y > 1020) moving down should despawn immediately
	var b_bottom: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b_bottom)
	b_bottom.setup(white_pellet, Vector2(300.0, 1030.0), DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 200.0)
	b_bottom._physics_process(0.01)
	assert(b_bottom.is_queued_for_deletion(), "Linear bullet past y=1020 moving down must despawn immediately")
	
	# 3c. Linear bullet past right wall (x > 680) moving right should despawn immediately
	var b_right: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b_right)
	b_right.setup(white_pellet, Vector2(690.0, 500.0), DanmakuBullet.MotionMode.LINEAR, Vector2.RIGHT, 200.0)
	b_right._physics_process(0.01)
	assert(b_right.is_queued_for_deletion(), "Linear bullet past x=680 moving right must despawn immediately")
	
	# 3d. Linear bullet past left wall (x < -80) moving left should despawn immediately
	var b_left: DanmakuBullet = bullet_scene.instantiate()
	root.add_child(b_left)
	b_left.setup(white_pellet, Vector2(-90.0, 500.0), DanmakuBullet.MotionMode.LINEAR, Vector2.LEFT, 200.0)
	b_left._physics_process(0.01)
	assert(b_left.is_queued_for_deletion(), "Linear bullet past x=-80 moving left must despawn immediately")
	print("PASS: Outward playfield exit despawns bullets cleanly")
	
	# 4. Test Playfield max_active_bullets Cap (Ambient Pellets Capped, Spellcards Unrestricted)
	var pf_scene := preload("res://scenes/arena/playfield.tscn")
	var pf: Playfield = pf_scene.instantiate()
	pf.max_active_bullets = 5
	root.add_child(pf)
	
	for i in range(5):
		pf.spawn_pellet(Vector2(300, 300))
	assert(pf.get_active_bullet_count() == 5, "5 ambient pellets spawned up to cap")
	
	pf.spawn_pellet(Vector2(300, 300))
	assert(pf.get_active_bullet_count() == 5, "6th ambient pellet blocked by max_active_bullets ceiling")
	
	# Danmaku spellcards and boss flurries must NEVER be capped so rings/patterns remain complete
	var b_spell := pf.spawn_danmaku_bullet(white_pellet, Vector2(300, 300), DanmakuBullet.MotionMode.LINEAR, Vector2.DOWN, 100)
	assert(b_spell != null, "Spellcard Danmaku bullet must NOT be blocked, preserving full ring geometry")
	print("PASS: Ambient pellets capped while spellcards remain full and unrestricted")

	
	pf.queue_free()
	print("--- ALL DANMAKU BATCHING AND LIFECYCLE TESTS PASSED! ---")
	quit(0)

