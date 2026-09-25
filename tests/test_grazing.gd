extends SceneTree

# Automated regression test for Touhou 19 Graze mechanics
func _init() -> void:
	print("\n--- Running Test Grazing System ---")
	
	test_graze_bullet_interaction()
	test_graze_deduplication()
	test_graze_jitter_and_hitbox_stability()
	test_graze_exit_and_repool_cleanup()
	test_graze_authority_and_invulnerability()
	test_character_graze_traits()
	
	print("\n=== All Grazing Tests Passed! ===")
	quit(0)

# NOTE: these run inside SceneTree._init(), which quits before a frame is ever
# processed, so Godot never fires _ready() on anything we add. @onready vars such as
# DanmakuBullet.sprite would stay null and every sprite-gated branch would silently
# skip - which is exactly what made this suite report failures against perfectly
# working code. Call _ready() by hand, the same way test_ai_hazard_evasion.gd does.
func _create_player() -> Player:
	var player_scene: PackedScene = load("res://scenes/player/player.tscn")
	var player: Player = player_scene.instantiate() as Player
	player.is_local_player = true
	player.is_ai = false
	root.add_child(player)
	player._ready()
	player.position = Vector2(300.0, 500.0)
	return player

func _create_bullet(pos: Vector2) -> DanmakuBullet:
	var bullet_scene: PackedScene = load("res://scenes/bullets/danmaku_bullet.tscn")
	var bullet: DanmakuBullet = bullet_scene.instantiate() as DanmakuBullet
	bullet.position = pos
	root.add_child(bullet)
	bullet._ready()
	return bullet

func _create_pellet(pos: Vector2) -> EnemyPellet:
	var pellet_scene: PackedScene = load("res://scenes/bullets/enemy_pellet.tscn")
	var pellet: EnemyPellet = pellet_scene.instantiate() as EnemyPellet
	pellet.position = pos
	root.add_child(pellet)
	pellet._ready()
	return pellet

func test_graze_bullet_interaction() -> void:
	print("\nTesting graze bullet interaction and charge reward...")
	var player := _create_player()
	var bullet := _create_bullet(Vector2(320.0, 500.0)) # 20px away (inside 32px graze radius, outside 3px hurtbox)
	
	var initial_charge: float = player.passive_charge
	var initial_graze: int = player.graze_count
	
	# Simulate graze detection
	player._on_graze_area_entered(bullet)
	
	assert(bullet.has_been_grazed == true, "Bullet must be flagged has_been_grazed after entering graze area")
	assert(player.graze_count == initial_graze + 1, "Player graze_count must increment by 1")
	assert(player.passive_charge > initial_charge, "Player passive_charge must increase after graze")
	assert(is_equal_approx(player.passive_charge, initial_charge + player.passive_charge_per_graze), "Charge increment must match passive_charge_per_graze")
	print("  [OK] Bullet grazed: has_been_grazed set, graze_count incremented, passive_charge rewarded")
	
	player.queue_free()
	bullet.queue_free()

func test_graze_deduplication() -> void:
	print("\nTesting graze deduplication (single-contribution per bullet)...")
	var player := _create_player()
	var bullet := _create_bullet(Vector2(320.0, 500.0))
	
	player._on_graze_area_entered(bullet)
	var charge_after_first: float = player.passive_charge
	var count_after_first: int = player.graze_count
	
	# Repeated entry simulation while bullet remains active
	player._on_graze_area_entered(bullet)
	player._on_graze_area_entered(bullet)
	
	assert(player.graze_count == count_after_first, "Graze count must NOT increment on already grazed bullet")
	assert(is_equal_approx(player.passive_charge, charge_after_first), "Passive charge must NOT increase on already grazed bullet")
	print("  [OK] Deduplication verified: bullet only contributes once")
	
	player.queue_free()
	bullet.queue_free()

func test_graze_jitter_and_hitbox_stability() -> void:
	print("\nTesting visual jitter effect and hitbox stability...")
	var player := _create_player()
	var bullet := _create_bullet(Vector2(320.0, 500.0))
	
	# Hold the bullet still so the only thing that could move it is the jitter itself.
	# speed defaults to 240 px/s, so a live bullet travels ~3.8px per frame under normal
	# motion and this test would blame the jitter for ordinary travel.
	bullet.speed = 0.0
	var initial_bullet_pos := bullet.position
	player._on_graze_area_entered(bullet)
	
	assert(bullet.is_in_graze_field == true, "Bullet must have is_in_graze_field = true while inside graze zone")
	assert(bullet._graze_tint_active == true, "Bullet must have _graze_tint_active = true inside graze zone")
	
	# Process physics frame
	bullet._physics_process(0.016)
	
	# Hitbox position check: bullet.position must NOT have jittered
	assert(bullet.position == initial_bullet_pos, "Bullet physical position must NOT change due to visual jitter")
	# Sprite offset check: sprite.offset must be shifted by subpixel jitter
	assert(bullet.sprite.offset != Vector2.ZERO, "Sprite offset must jitter for visual feedback")
	print("  [OK] Visual jitter applies to sprite.offset while bullet physical position remains rock-steady")
	
	player.queue_free()
	bullet.queue_free()

func test_graze_exit_and_repool_cleanup() -> void:
	print("\nTesting graze exit and pool recycle cleanup...")
	var player := _create_player()
	var bullet := _create_bullet(Vector2(320.0, 500.0))
	
	player._on_graze_area_entered(bullet)
	bullet._physics_process(0.016)
	
	# Simulate bullet exiting graze field
	player._on_graze_area_exited(bullet)
	assert(bullet.is_in_graze_field == false, "is_in_graze_field must clear on exit")
	assert(bullet.sprite.offset == Vector2.ZERO, "sprite.offset must reset to ZERO on exit")
	assert(bullet._graze_tint_active == false, "_graze_tint_active must reset on exit")
	
	# Test pool acquisition reset
	bullet.has_been_grazed = true
	bullet.is_in_graze_field = true
	bullet.on_pool_acquire()
	assert(bullet.has_been_grazed == false, "on_pool_acquire must reset has_been_grazed to false")
	assert(bullet.is_in_graze_field == false, "on_pool_acquire must reset is_in_graze_field to false")
	assert(bullet.sprite.offset == Vector2.ZERO, "on_pool_acquire must reset sprite.offset to ZERO")
	print("  [OK] Exit and pool recycle cleanly resets all visual and tracking states")
	
	player.queue_free()
	bullet.queue_free()

func test_graze_authority_and_invulnerability() -> void:
	print("\nTesting graze authority and invulnerability gating...")
	var player := _create_player()
	var bullet := _create_bullet(Vector2(320.0, 500.0))
	
	# 1. Invulnerable player should not graze
	player.is_invulnerable = true
	player._on_graze_area_entered(bullet)
	assert(bullet.has_been_grazed == false, "Invulnerable player must not graze bullets")
	assert(player.graze_count == 0, "Invulnerable player must not increment graze count")
	
	# 2. Remote puppet player should not graze
	player.is_invulnerable = false
	player.is_local_player = false
	player.is_ai = false
	player._on_graze_area_entered(bullet)
	assert(bullet.has_been_grazed == false, "Remote puppet must not process local graze")
	assert(player.graze_count == 0, "Remote puppet must not increment graze count")
	print("  [OK] Invulnerability and netplay authority correctly gate grazing")
	
	player.queue_free()
	bullet.queue_free()

func test_character_graze_traits() -> void:
	print("\nTesting character data graze and hurtbox traits...")
	var reimu_data: CharacterData = CharacterData.get_character("reimu")
	var marisa_data: CharacterData = CharacterData.get_character("marisa")
	
	assert(reimu_data != null and marisa_data != null, "Reimu and Marisa character data must exist")
	assert(reimu_data.hurtbox_radius < marisa_data.hurtbox_radius, "Reimu must have smaller hurtbox radius than Marisa (Small Hitbox trait)")
	assert(reimu_data.graze_radius == 64.0, "Reimu default graze radius must be 64.0")
	assert(reimu_data.passive_charge_per_graze > 0.0, "passive_charge_per_graze must be greater than 0")
	print("  [OK] Reimu hurtbox (%s px) < Marisa hurtbox (%s px), delivering authentic graze safety trait" % [reimu_data.hurtbox_radius, marisa_data.hurtbox_radius])

