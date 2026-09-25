extends SceneTree

# Test suite for AI Hazard Awareness & Yin-Yang Orb Evasion
var test_root: Node = null
var passed_count: int = 0
var failed_count: int = 0

func assert_true(condition: bool, message: String) -> void:
	if condition:
		print("  [PASS] %s" % message)
		passed_count += 1
	else:
		print("  [FAIL] %s" % message)
		failed_count += 1

func assert_false(condition: bool, message: String) -> void:
	assert_true(not condition, message)

## Mirrors how Extra Attack nodes (Sakuya's knives, Reimu's amulets, Youmu's slash waves)
## actually expose themselves: velocity kept private, no radius property, real collision shape.
class PrivateVelocityHazard extends Area2D:
	var _velocity: Vector2 = Vector2.ZERO

func _init() -> void:
	print("\n============================================")
	print(" RUNNING AI HAZARD & YIN-YANG ORB UNIT TESTS ")
	print("============================================\n")
	
	test_root = Node2D.new()
	test_root.name = "TestRoot"
	root.add_child(test_root)
	
	test_pellet_detection()
	test_yin_yang_orb_threat_recognition()
	test_yin_yang_orb_wall_bounce_prediction()
	test_enemy_body_hazard_detection_and_evasion()
	test_ignore_enemies_behind_player()
	test_spirit_scope_focus()
	test_flank_hazard_detection_and_evasion()
	test_inward_wall_repulsion()
	test_decision_latency_holds_input()
	test_velocity_obstacle_collision_prediction()
	test_velocity_obstacle_avoids_collision_course()
	test_sideways_extra_attack_is_read_correctly()
	test_pinned_against_wall_does_not_fake_escape()
	test_does_not_camp_the_back_wall()
	test_tracks_hazards_behind_the_bot()
	test_hazard_ranking_prefers_incoming_over_close()
	test_uses_the_whole_engagement_band()
	test_rollout_sees_dead_ends()
	test_laser_survives_the_tracking_cap()
	test_corner_avoidance_and_escape()
	
	print("\n--------------------------------------------")
	print("TEST RESULTS: %d Passed, %d Failed" % [passed_count, failed_count])
	print("--------------------------------------------\n")
	
	test_root.queue_free()
	quit(0 if failed_count == 0 else 1)

func _create_test_environment() -> Dictionary:
	var pf_scene := preload("res://scenes/arena/playfield.tscn")
	var pf = pf_scene.instantiate() as Playfield
	root.add_child(pf)
	pf._ready()
	pf.setup(2, false, "reimu")
	
	var player = pf.player
	player.position = Vector2(300.0, 816.0)
	player.set_ai_mode(true)
	var ai = player.ai_controller as RudimentaryAI
	# Decide on every update_ai() call so these tests exercise the decision logic itself.
	# Reaction latency is covered separately by test_decision_latency_holds_input().
	ai.decision_interval = 0.0
	
	return {
		"playfield": pf,
		"player": player,
		"ai": ai
	}

func test_pellet_detection() -> void:
	print("Testing standard pellet detection...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# Spawn pellet directly above player
	pf.spawn_pellet(Vector2(player.position.x, player.position.y - 150.0))
	var threats := ai._scan_front_threats()
	
	assert_true(threats.size() == 1, "Directly aligned pellet is detected as threat")
	if threats.size() > 0:
		assert_true(threats[0].radius == 12.0 or threats[0].radius == 10.0 or threats[0].radius == 8.0, "Pellet has standard bullet radius: %f" % threats[0].radius)
	
	env.playfield.queue_free()

func test_yin_yang_orb_threat_recognition() -> void:
	print("Testing Yin-Yang Orb radius & velocity recognition...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	var orb_scene := preload("res://scenes/attacks/yin_yang_orb.tscn")
	var orb: YinYangOrb = orb_scene.instantiate()
	orb.position = Vector2(player.position.x + 50.0, player.position.y - 150.0)
	
	var bullets_layer = pf.bullets_layer
	bullets_layer.add_child(orb)
	pf.register_custom_hazard(orb)
	orb.velocity = Vector2(-20.0, 250.0)
	
	var threats := ai._scan_front_threats()
	assert_true(threats.size() == 1, "Yin-Yang Orb at dx=50 is detected as threat (under 60+24 danger width)")
	if threats.size() > 0:
		assert_true(threats[0].radius == 60.0, "Threat radius correctly reflects orb radius (60.0)")
		assert_true(threats[0].vel.y == 250.0, "Threat reads velocity directly from orb: %s" % str(threats[0].vel))
	
	orb.queue_free()
	env.playfield.queue_free()

func test_yin_yang_orb_wall_bounce_prediction() -> void:
	print("Testing Yin-Yang Orb wall bounce trajectory prediction...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	var orb_scene := preload("res://scenes/attacks/yin_yang_orb.tscn")
	var orb: YinYangOrb = orb_scene.instantiate()
	# Place orb at x=90, y=player.y - 200. Velocity heading left at 150 px/s, down at 200 px/s
	# time_to_y = 200 / 200 = 1.0s. Raw unbounced x = 90 - 150*1 = -60.
	# Left wall is at 65.0. Distance past wall = 65 - (-60) = 125.
	# Bounced x = 65 + 125 = 190.
	orb.position = Vector2(90.0, player.position.y - 200.0)
	
	var bullets_layer = pf.bullets_layer
	bullets_layer.add_child(orb)
	pf.register_custom_hazard(orb)
	orb.velocity = Vector2(-150.0, 200.0)
	
	# Place player at x=190 so projected bounce collides with player
	player.position.x = 190.0
	
	var threats := ai._scan_front_threats()
	assert_true(threats.size() == 1, "Threat detected based on wall-bounced projected trajectory")
	if threats.size() > 0:
		var pred_x: float = threats[0].pred_x
		assert_true(abs(pred_x - 190.0) < 5.0, "Predicted X correctly accounts for wall reflection: expected ~190, got %f" % pred_x)
	
	orb.queue_free()
	env.playfield.queue_free()

func test_enemy_body_hazard_detection_and_evasion() -> void:
	print("Testing enemy body hazard detection and evasion...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# Place fairy right near player's front-right (dx = 20px, dy = 40px)
	var fairy_scene := preload("res://scenes/enemies/fairy.tscn")
	var fairy = fairy_scene.instantiate()
	fairy.position = Vector2(player.position.x + 20.0, player.position.y - 40.0)
	pf.entities_layer.add_child(fairy)
	
	var threats := ai._scan_front_threats()
	assert_true(threats.size() == 1, "Nearby fairy body is recognized as a collision threat: got %d" % threats.size())
	if threats.size() > 0:
		assert_true(threats[0].get("is_enemy_body", false) == true, "Threat is marked as enemy body")
		ai.update_ai(0.016)
		var evade := ai.get_movement_vector()
		assert_true(evade.x < 0.0, "AI steers left AWAY from fairy body on its right (got %f)" % evade.x)
	
	fairy.queue_free()
	env.playfield.queue_free()

func test_ignore_enemies_behind_player() -> void:
	print("Testing AI ignores enemies behind / below player for alignment...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# Place player at center (X = 300, Y = 816)
	player.position = Vector2(300.0, 816.0)
	
	# Spawn a fairy below the player at bottom-left corner spawn (X = 42, Y = 950)
	var fairy_scene := preload("res://scenes/enemies/fairy.tscn")
	var fairy = fairy_scene.instantiate()
	fairy.position = Vector2(42.0, 950.0)
	pf.entities_layer.add_child(fairy)
	
	# The AI should NOT align towards X = 42 to meet a bottom-spawning fairy behind it!
	var target_x: float = ai._find_alignment_target_x()
	assert_true(target_x == 300.0, "AI does NOT align toward bottom-spawning fairy behind it (target_x = %f)" % target_x)
	
	fairy.queue_free()
	env.playfield.queue_free()

func test_spirit_scope_focus() -> void:
	print("Testing AI Focus mode activation near Spirits...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# Spawn a Spirit within Scope radius (80px away)
	var spirit_scene := preload("res://scenes/enemies/spirit.tscn")
	var spirit = spirit_scene.instantiate()
	spirit.position = Vector2(player.position.x + 60.0, player.position.y - 40.0)
	pf.entities_layer.add_child(spirit)
	
	ai.update_ai(0.016)
	assert_true(ai.is_focusing(), "AI enables Focus mode when unactivated Spirit is in Scope radius")
	
	# Move spirit far away
	spirit.position = Vector2(player.position.x + 300.0, player.position.y - 400.0)
	ai.update_ai(0.016)
	assert_false(ai.is_focusing(), "AI disables Focus mode when Spirit is out of Scope range")
	
	# Move spirit close again, but introduce a wide Yin-Yang Orb threat
	spirit.position = Vector2(player.position.x + 60.0, player.position.y - 40.0)
	var orb_scene := preload("res://scenes/attacks/yin_yang_orb.tscn")
	var orb: YinYangOrb = orb_scene.instantiate()
	orb.position = Vector2(player.position.x, player.position.y - 150.0)
	pf.bullets_layer.add_child(orb)
	pf.register_custom_hazard(orb)
	orb.velocity = Vector2(0.0, 200.0)
	
	ai.update_ai(0.016)
	assert_false(ai.is_focusing(), "AI suppresses Focus mode to maintain full 540 px/s speed when dodging Yin-Yang Orb")
	
	orb.queue_free()
	spirit.queue_free()
	env.playfield.queue_free()

func test_decision_latency_holds_input() -> void:
	print("Testing decision latency holds the previous input between ticks...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# 30 Hz decisions: one 16ms frame is not enough to re-decide
	ai.decision_interval = 1.0 / 30.0
	ai.update_ai(0.016)
	var first := ai.get_movement_vector()
	
	# Drop a hazard right on top of the bot; it must NOT react until its tick comes round
	var orb_scene := preload("res://scenes/attacks/yin_yang_orb.tscn")
	var orb: YinYangOrb = orb_scene.instantiate()
	orb.position = Vector2(player.position.x, player.position.y - 120.0)
	pf.bullets_layer.add_child(orb)
	pf.register_custom_hazard(orb)
	orb.velocity = Vector2(0.0, 300.0)
	
	ai.update_ai(0.016)
	assert_true(ai.get_movement_vector() == first, "Bot holds its previous input inside the decision interval")
	
	# Past the interval it re-decides and gets out of the orb's way
	ai.update_ai(0.05)
	assert_true(ai.get_movement_vector() != Vector2.ZERO, "Bot re-decides and evades once the decision interval elapses")
	
	orb.queue_free()
	env.playfield.queue_free()

func test_velocity_obstacle_collision_prediction() -> void:
	print("Testing velocity-obstacle collision prediction math...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	
	var p_pos: Vector2 = player.position
	# A pellet 200px directly above, falling at 400 px/s, 20px combined collision radius
	var hazard := {
		"pos": Vector2(p_pos.x, p_pos.y - 200.0),
		"vel": Vector2(0.0, 400.0),
		"dy": 200.0,
		"radius": 12.0,
		"kind": RudimentaryAI.HAZARD_BULLET,
		"bounces": false,
		"bounce_min_x": 0.0,
		"bounce_max_x": 0.0
	}
	
	# Standing still: it lands on us. 180px of gap at 400 px/s is 0.45s.
	var standing := ai._evaluate_hazard(hazard, p_pos, Vector2.ZERO, 20.0)
	assert_true(abs(standing.x - 0.45) < 0.01, "Predicts impact in 0.45s when standing still: got %f" % standing.x)
	assert_true(standing.y == 0.0, "Reports no clearance on a direct collision course: got %f" % standing.y)
	
	# Sidestepping at full speed: it misses entirely, with room to spare
	var dodging := ai._evaluate_hazard(hazard, p_pos, Vector2(540.0, 0.0), 20.0)
	assert_true(dodging.x == INF, "Predicts no collision at all once sidestepping")
	assert_true(dodging.y > 50.0, "Reports real clearance once sidestepping: got %f" % dodging.y)
	
	# A hazard already on top of us is an immediate hit, whatever we do
	var touching := ai._evaluate_hazard(hazard, Vector2(p_pos.x, p_pos.y - 195.0), Vector2.ZERO, 20.0)
	assert_true(touching.x == 0.0, "Reports an overlapping hazard as an immediate collision")
	
	env.playfield.queue_free()

func test_velocity_obstacle_avoids_collision_course() -> void:
	print("Testing velocity-obstacle scorer refuses to stand in a bullet's path...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# A pellet aimed straight down the bot's column
	pf.spawn_pellet(Vector2(player.position.x, player.position.y - 200.0))
	ai.update_ai(0.016)
	
	assert_true(ai.get_movement_vector() != Vector2.ZERO, "Bot refuses to hold still under a directly incoming pellet")
	
	env.playfield.queue_free()

func test_sideways_extra_attack_is_read_correctly() -> void:
	print("Testing sideways Extra Attack hazards are read correctly...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# A dagger flying horizontally at the bot from its left, at the bot's own height
	var knife := PrivateVelocityHazard.new()
	knife.position = Vector2(player.position.x - 250.0, player.position.y)
	knife._velocity = Vector2(300.0, 0.0)
	
	var shape := CollisionShape2D.new()
	var rect := RectangleShape2D.new()
	rect.size = Vector2(56.0, 20.0)
	shape.shape = rect
	knife.add_child(shape)
	
	pf.bullets_layer.add_child(knife)
	pf.register_custom_hazard(knife)
	
	var hazards := ai._collect_hazards()
	var found := {}
	for h in hazards:
		if h.pos.is_equal_approx(knife.position):
			found = h
			break
	
	assert_true(not found.is_empty(), "Sideways Extra Attack hazard is collected at all")
	if not found.is_empty():
		# The bug: this used to come back as Vector2(0, 240) - "falling straight down"
		assert_true(found.vel.is_equal_approx(Vector2(300.0, 0.0)), "Reads the private _velocity instead of assuming a downward fall: got %s" % str(found.vel))
		assert_true(abs(found.radius - 10.0) < 0.01, "Reads radius from the real collision shape: got %f" % found.radius)
	
	# And the bot must actually react rather than standing in the lane. Outrunning it
	# sideways is as valid as stepping off the line, so only require that it moves and
	# that it does not close the distance itself.
	ai.update_ai(0.016)
	var move := ai.get_movement_vector()
	assert_true(move != Vector2.ZERO, "Bot refuses to stand in the incoming dagger's lane")
	assert_true(move.x >= 0.0, "Bot does not charge into the oncoming dagger: got %s" % str(move))
	
	knife.queue_free()
	env.playfield.queue_free()

func test_pinned_against_wall_does_not_fake_escape() -> void:
	print("Testing a bot pinned at the back wall does not pick a move that goes nowhere...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# Flush against the bottom of the playfield, pellet falling straight onto it
	player.position = Vector2(300.0, Player.MAX_Y)
	pf.spawn_pellet(Vector2(player.position.x, player.position.y - 250.0))
	
	# Diving further "down" is worth nothing from here and must be scored that way
	var blocked := ai._effective_velocity(player.position, Vector2(0.0, 540.0))
	assert_true(blocked.y == 0.0, "A downward move against the floor yields no speed: got %f" % blocked.y)
	var sideways := ai._effective_velocity(player.position, Vector2(540.0, 0.0))
	assert_true(sideways.x > 100.0, "A sideways move with room to spare keeps its speed: got %f" % sideways.x)
	
	ai.update_ai(0.016)
	var move := ai.get_movement_vector()
	assert_true(move.x != 0.0, "Pinned bot escapes sideways instead of pressing into the wall: got %s" % str(move))
	
	env.playfield.queue_free()

func test_does_not_camp_the_back_wall() -> void:
	print("Testing the bot does not settle against the back wall...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	
	# Parked at the very bottom with nothing threatening it, it should come back up
	player.position = Vector2(300.0, Player.MAX_Y)
	ai.update_ai(0.016)
	assert_true(ai.get_movement_vector().y < 0.0, "Bot moves off the back wall when nothing is threatening it")
	
	env.playfield.queue_free()

func test_tracks_hazards_behind_the_bot() -> void:
	print("Testing hazards behind the bot stay tracked...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# Sit high enough to have room behind, and put a pellet well below - a bullet the bot
	# has already dodged, now sitting in the space it might reverse into.
	player.position = Vector2(300.0, 700.0)
	pf.spawn_pellet(Vector2(300.0, 860.0))
	
	var hazards := ai._collect_hazards()
	var found_behind := false
	for h in hazards:
		if h.pos.y > player.position.y + 100.0:
			found_behind = true
			break
	assert_true(found_behind, "A pellet 160px behind the bot is still collected as a hazard")
	
	# And it must not reverse into it
	ai.update_ai(0.016)
	assert_true(ai.get_movement_vector().y <= 0.0, "Bot does not back into the hazard behind it: got %s" % str(ai.get_movement_vector()))
	
	env.playfield.queue_free()

func test_hazard_ranking_prefers_incoming_over_close() -> void:
	print("Testing hazard ranking keeps incoming bullets over close spent ones...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	player.position = Vector2(300.0, 700.0)
	
	# A spent pellet close by but already past, falling away from the bot
	pf.spawn_pellet(Vector2(300.0, 780.0))
	# A pellet further off but bearing straight down on it, close enough to arrive
	# inside the prediction horizon
	pf.spawn_pellet(Vector2(300.0, 550.0))
	
	var hazards := ai._collect_hazards()
	var spent := {}
	var incoming := {}
	for h in hazards:
		if h.pos.is_equal_approx(Vector2(300.0, 780.0)):
			spent = h
		elif h.pos.is_equal_approx(Vector2(300.0, 550.0)):
			incoming = h
	
	assert_true(not spent.is_empty() and not incoming.is_empty(), "Both pellets are collected")
	if spent.is_empty() or incoming.is_empty():
		env.playfield.queue_free()
		return
	
	assert_true(incoming.dist_sq > spent.dist_sq, "The incoming pellet really is the further away of the two")
	assert_true(incoming.threat_sq < spent.threat_sq, "But it ranks as the bigger threat: incoming %f vs spent %f" % [incoming.threat_sq, spent.threat_sq])
	
	# With room for only one tracked hazard, the incoming one is the one kept
	ai.max_tracked_hazards = 1
	var tracked := ai._limit_hazards(hazards)
	assert_true(tracked.size() == 1, "Tracking cap is honoured")
	if tracked.size() == 1:
		assert_true(tracked[0].pos.is_equal_approx(Vector2(300.0, 550.0)), "The single tracked hazard is the incoming one, not the closest")
	
	env.playfield.queue_free()

func test_uses_the_whole_engagement_band() -> void:
	print("Testing the bot is free to hold mid-field height...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	
	# Mid-field with nothing threatening: it should be content here, not dragged back down
	player.position = Vector2(300.0, 600.0)
	ai.update_ai(0.016)
	assert_true(ai.get_movement_vector().y <= 0.0, "Bot is not dragged back down from mid-field: got %s" % str(ai.get_movement_vector()))
	
	# Above the band, up where fairies enter, it should still come back down
	player.position = Vector2(300.0, 200.0)
	ai.reset()
	ai.update_ai(0.016)
	assert_true(ai.get_movement_vector().y > 0.0, "Bot still returns from above the engagement band: got %s" % str(ai.get_movement_vector()))
	
	env.playfield.queue_free()

func _build_rollout_inputs(ai: RudimentaryAI, tracked: Array[Dictionary]) -> Dictionary:
	var radii := PackedFloat32Array()
	var future_positions := PackedVector2Array()
	radii.resize(tracked.size())
	future_positions.resize(tracked.size())
	for i in tracked.size():
		radii[i] = tracked[i].radius + 3.0 + RudimentaryAI.SAFETY_MARGIN
		future_positions[i] = tracked[i].pos + tracked[i].vel * ai.rollout_step
	return {"radii": radii, "future_positions": future_positions}

func test_rollout_sees_dead_ends() -> void:
	print("Testing the second ply recognises a move with no way out...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	
	var p_pos := Vector2(300.0, 600.0)
	player.position = p_pos
	
	# A ring of bullets closing in on where this move lands. Every follow-up from inside
	# it runs into something - exactly the pocket a one-step scorer walks into.
	var vel := Vector2(540.0, 0.0)
	var landing := p_pos + vel * ai.rollout_step
	var ring: Array[Dictionary] = []
	for i in 16:
		var angle: float = TAU * float(i) / 16.0
		var offset := Vector2(cos(angle), sin(angle)) * 250.0
		ring.append({
			"pos": landing + offset,
			"vel": -offset.normalized() * 400.0,
			"kind": RudimentaryAI.HAZARD_BULLET,
			"radius": 40.0
		})
	
	var ring_inputs := _build_rollout_inputs(ai, ring)
	var trapped: float = ai._escape_potential(ring, ring_inputs.radii, ring_inputs.future_positions, p_pos, vel, 540.0)
	
	# The same move with only a single far-off bullet around has a clean way out
	var open_field: Array[Dictionary] = [{
		"pos": Vector2(60.0, 80.0),
		"vel": Vector2(0.0, 40.0),
		"kind": RudimentaryAI.HAZARD_BULLET,
		"radius": 12.0
	}]
	var open_inputs := _build_rollout_inputs(ai, open_field)
	var unobstructed: float = ai._escape_potential(open_field, open_inputs.radii, open_inputs.future_positions, p_pos, vel, 540.0)
	
	assert_true(unobstructed >= ai.lookahead_horizon, "A move into open field keeps a full-horizon escape: got %f" % unobstructed)
	assert_true(trapped < ai.lookahead_horizon, "A move into the closing ring does not: got %f" % trapped)
	assert_true(trapped < unobstructed, "The dead end scores strictly worse than the open move: %f vs %f" % [trapped, unobstructed])
	
	env.playfield.queue_free()

func test_laser_survives_the_tracking_cap() -> void:
	print("Testing a laser in the bot column is not culled by the tracking cap...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	player.position = Vector2(300.0, 700.0)
	
	# An Earth Light Ray straight down the bot column. Its node sits at the TOP of the
	# field, so straight-line distance badly misrepresents how close it is.
	var ray_scene := preload("res://scenes/attacks/earth_light_ray.tscn")
	var ray: EarthLightRay = ray_scene.instantiate()
	ray.position = Vector2(300.0, 0.0)
	pf.bullets_layer.add_child(ray)
	pf.register_custom_hazard(ray)
	
	# Plus a crowd of pellets, every one of them physically nearer than the ray node
	for i in 12:
		pf.spawn_pellet(Vector2(120.0 + float(i) * 30.0, 640.0))
	
	var hazards := ai._collect_hazards()
	var laser := {}
	for h in hazards:
		if h.kind == RudimentaryAI.HAZARD_LASER:
			laser = h
			break
	
	assert_true(not laser.is_empty(), "Laser is collected as a hazard")
	if laser.is_empty():
		env.playfield.queue_free()
		return
	
	# It is directly overhead, so it must rank as the single biggest threat present
	var most_threatening := true
	for h in hazards:
		if h.kind != RudimentaryAI.HAZARD_LASER and h.threat_sq < laser.threat_sq:
			most_threatening = false
			break
	assert_true(most_threatening, "A laser in the bot column outranks every pellet around it")
	
	# And it therefore survives even a brutally small tracking cap
	ai.max_tracked_hazards = 1
	var tracked := ai._limit_hazards(hazards)
	assert_true(tracked.size() == 1 and tracked[0].kind == RudimentaryAI.HAZARD_LASER, "The laser is what a one-slot tracking budget keeps")
	
	ray.queue_free()
	env.playfield.queue_free()

func test_flank_hazard_detection_and_evasion() -> void:
	print("Testing flank hazard detection (projectiles beside/slightly below player)...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# Spawn a bullet right beside the player closing inward (dx = 25px to the right, dy = -10px, i.e. 10px below player center)
	pf.spawn_directional_pellet(Vector2(player.position.x + 25.0, player.position.y + 10.0), 240.0, Vector2(-1.0, 0.0), Color.WHITE)
	
	var threats := ai._scan_front_threats()
	assert_true(threats.size() == 1, "Threat beside/slightly below player (dy = -10px) is successfully detected: got %d" % threats.size())
	if threats.size() > 0:
		assert_true(threats[0].get("is_flank", false) == true, "Threat is correctly flagged as flank threat")
		
		# AI must steer LEFT away from the bullet beside it, NOT right into it!
		ai.update_ai(0.016)
		var evade: Vector2 = ai.get_movement_vector()
		assert_true(evade != Vector2.ZERO, "AI actively evades the flank threat (got %s)" % str(evade))
		assert_true(evade.x <= 0.0, "AI does not charge right into the flank bullet (got %f)" % evade.x)
	
	env.playfield.queue_free()

func test_inward_wall_repulsion() -> void:
	print("Testing inward wall repulsion when lingering near edge...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield
	
	# Place player near left wall (X = 55.0)
	player.position = Vector2(55.0, 816.0)
	
	# Spawn a fairy towards center (X = 300.0)
	var fairy_scene := preload("res://scenes/enemies/fairy.tscn")
	var fairy = fairy_scene.instantiate()
	fairy.position = Vector2(300.0, 400.0)
	pf.entities_layer.add_child(fairy)
	
	ai.update_ai(0.016)
	var move: Vector2 = ai.get_movement_vector()
	assert_true(move.x > 0.0, "AI repels inward toward center when near wall (got %f)" % move.x)
	
	fairy.queue_free()
	env.playfield.queue_free()

func test_corner_avoidance_and_escape() -> void:
	print("Testing corner pocket avoidance and active corner escape...")
	var env = _create_test_environment()
	var ai: RudimentaryAI = env.ai
	var player: Player = env.player
	var pf: Playfield = env.playfield

	# Spawn a fairy towards center
	var fairy_scene := preload("res://scenes/enemies/fairy.tscn")
	var fairy = fairy_scene.instantiate()
	fairy.position = Vector2(300.0, 400.0)
	pf.entities_layer.add_child(fairy)

	# 1. Bot trapped in bottom-left corner
	player.position = Vector2(Player.MIN_X + 15.0, Player.MAX_Y - 15.0)
	ai.update_ai(0.016)
	var move_bl: Vector2 = ai.get_movement_vector()
	assert_true(move_bl.x > 0.0, "Bot steers right out of bottom-left corner (got move.x = %f)" % move_bl.x)
	assert_true(move_bl.y < 0.0, "Bot steers up out of bottom-left corner (got move.y = %f)" % move_bl.y)

	# 2. Bot trapped in bottom-right corner
	player.position = Vector2(Player.MAX_X - 15.0, Player.MAX_Y - 15.0)
	ai.update_ai(0.05)
	var move_br: Vector2 = ai.get_movement_vector()
	assert_true(move_br.x < 0.0, "Bot steers left out of bottom-right corner (got move.x = %f)" % move_br.x)
	assert_true(move_br.y < 0.0, "Bot steers up out of bottom-right corner (got move.y = %f)" % move_br.y)

	# 3. Positional score comparison: corner penalty vs flat wall vs center
	var p_corner := Vector2(Player.MIN_X + 15.0, Player.MAX_Y - 15.0)
	var p_wall := Vector2(300.0, Player.MAX_Y - 15.0)
	var p_center := Vector2(300.0, 750.0)
	var score_corner: float = ai._positional_score(p_corner, Vector2.ZERO, Vector2.ZERO, 300.0, false, false)
	var score_wall: float = ai._positional_score(p_wall, Vector2.ZERO, Vector2.ZERO, 300.0, false, false)
	var score_center: float = ai._positional_score(p_center, Vector2.ZERO, Vector2.ZERO, 300.0, false, false)
	assert_true(score_corner < score_wall, "Bottom corner scores substantially worse than flat bottom wall: %f vs %f" % [score_corner, score_wall])
	assert_true(score_wall < score_center, "Flat bottom wall scores worse than open center field: %f vs %f" % [score_wall, score_center])

	fairy.queue_free()
	env.playfield.queue_free()

