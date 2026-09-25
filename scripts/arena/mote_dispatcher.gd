class_name MoteDispatcher
extends Node

## MoteDispatcher
## Manages inter-field travel motes (pellets, big pellets, spirits, and extra attacks),
## pooling, staggered launch timers, parabolic trajectory setup, and opponent field arrival spawns.

const TRAVEL_MOTE_SCENE: PackedScene = preload("res://scenes/effects/travel_mote.tscn")
const SAKUYA_EXTRA_KNIFE_SCENE: PackedScene = preload("res://scenes/attacks/sakuya_extra_knife.tscn")
const P1_MOTE_COLOR: Color = Color(1.0, 0.35, 0.45, 1.0)
const P2_MOTE_COLOR: Color = Color(0.3, 0.75, 1.0, 1.0)

const SAKUYA_KNIFE_COUNT: int = 5
# A touch shorter than one full knife-length of travel time at 2x visual scale
# and flight_speed, so consecutive daggers sit slightly overlapped rather than
# just touching - the flurry reads as one tight chain, tip into hilt.
const SAKUYA_KNIFE_LAUNCH_STAGGER: float = 0.16

var arena: Node = null
var effects_overlay: Node2D = null
var mote_pool: NodePool = null

func setup(p_arena: Node, p_overlay: Node2D) -> void:
	arena = p_arena
	effects_overlay = p_overlay
	if effects_overlay:
		_ensure_pool()

func _ensure_pool() -> void:
	if mote_pool == null and effects_overlay != null:
		mote_pool = NodePool.new(TRAVEL_MOTE_SCENE, effects_overlay, 100)

func _is_blocked() -> bool:
	if arena:
		return arena.get("_round_intermission") == true or arena.get("_match_over") == true
	return false

func send_attack_between_fields(
	from_container: SubViewportContainer,
	to_container: SubViewportContainer,
	target_playfield: Playfield,
	local_death_pos: Vector2,
	pellet_count: int,
	mote_color: Color,
	has_big_pellet: bool = false,
	has_spirit: bool = false,
	has_extra_attack: bool = false,
	sender_char: String = "reimu",
	bounce_count: int = 0,
	rank: int = 1,
	source_playfield: Playfield = null,
	extra_targets: PackedVector2Array = PackedVector2Array()
) -> void:
	if _is_blocked():
		return
	if effects_overlay == null or to_container == null or from_container == null:
		return
	
	var global_start: Vector2 = from_container.global_position + local_death_pos
	
	# Determine if this is a full barrage of large pellets (e.g. from the 弾 BULLET pickup)
	var all_big_pellets: bool = has_big_pellet and pellet_count >= 10
	var payload_type: TravelMote.MotePayload = TravelMote.MotePayload.BIG_PELLET if all_big_pellets else TravelMote.MotePayload.PELLET
	var stagger_step: float = 0.035 if all_big_pellets else 0.15
	
	# Spawn pellets
	for i in range(pellet_count):
		# Target coordinates in the opponent's upper playfield (spanning full width including borders)
		var target_local_x: float = randf_range(16.0, 584.0)
		var target_local_y: float = randf_range(70.0, 230.0) if all_big_pellets else randf_range(80.0, 240.0)
		var target_local := Vector2(target_local_x, target_local_y)
		var global_target: Vector2 = to_container.global_position + target_local
		
		if i == 0:
			spawn_mote(global_start, global_target, target_local, target_playfield, mote_color, payload_type, sender_char, bounce_count)
		else:
			var delay: float = float(i) * stagger_step
			if is_inside_tree():
				get_tree().create_timer(delay).timeout.connect(func():
					if _is_blocked():
						return
					if is_instance_valid(target_playfield) and is_instance_valid(effects_overlay):
						spawn_mote(global_start, global_target, target_local, target_playfield, mote_color, payload_type, sender_char, bounce_count)
				)
	
	# If triggered by a Great Fairy or high combo, dispatch a Big Pellet!
	if has_big_pellet and not all_big_pellets:
		var big_local_x: float = randf_range(35.0, 565.0)
		var big_local_y: float = randf_range(70.0, 200.0)
		var big_target_local := Vector2(big_local_x, big_local_y)
		var big_global_target: Vector2 = to_container.global_position + big_target_local
		if is_inside_tree():
			get_tree().create_timer(0.06).timeout.connect(func():
				if _is_blocked():
					return
				if is_instance_valid(target_playfield) and is_instance_valid(effects_overlay):
					spawn_mote(global_start, big_global_target, big_target_local, target_playfield, mote_color, TravelMote.MotePayload.BIG_PELLET, sender_char, bounce_count)
			)
	
	# If combo milestone or Great Fairy chain reached, dispatch a Spirit enemy!
	if has_spirit:
		var spirit_local_x: float = randf_range(35.0, 565.0)
		var spirit_local_y: float = randf_range(30.0, 90.0)
		var spirit_target_local := Vector2(spirit_local_x, spirit_local_y)
		var spirit_global_target: Vector2 = to_container.global_position + spirit_target_local
		if is_inside_tree():
			get_tree().create_timer(0.12).timeout.connect(func():
				if _is_blocked():
					return
				if is_instance_valid(target_playfield) and is_instance_valid(effects_overlay):
					spawn_mote(global_start, spirit_global_target, spirit_target_local, target_playfield, mote_color, TravelMote.MotePayload.SPIRIT, sender_char, bounce_count)
			)
	
	# If combo milestone or Great Fairy chain reached, dispatch an Extra Attack!
	if has_extra_attack and sender_char == "sakuya":
		_dispatch_sakuya_extra_attack(source_playfield, target_playfield, local_death_pos, from_container, to_container)
	elif has_extra_attack:
		# Where an Extra Attack lands is part of the attack's identity, not per-bullet
		# jitter: an Earth Light Ray must come down in the same column on both clients.
		# The aggressor's client rolls the landing spots once and ships them in the
		# attack packet; an empty array means a local match, so roll them here.
		var targets: PackedVector2Array = extra_targets
		if targets.is_empty():
			targets = roll_extra_attack_targets(sender_char, rank)
		var sender_data := CharacterData.get_character(sender_char)
		var launch_sfx: String = sender_data.extra_attack_launch_sfx if sender_data else ""

		for i in range(targets.size()):
			var ex_target_local: Vector2 = targets[i]
			var ex_global_target: Vector2 = to_container.global_position + ex_target_local
			var delay: float = 0.2 + (0.12 * float(i))
			if is_inside_tree():
				get_tree().create_timer(delay).timeout.connect(func():
					if _is_blocked():
						return
					if is_instance_valid(target_playfield) and is_instance_valid(effects_overlay):
						spawn_mote(global_start, ex_global_target, ex_target_local, target_playfield, mote_color, TravelMote.MotePayload.EXTRA_ATTACK, sender_char, bounce_count, rank)
						if not launch_sfx.is_empty():
							AudioService.play_sfx(launch_sfx)
				)

## Rolls how many shots an Extra Attack is made of and where each one lands on the
## opponent's field. Called once, on the aggressor's client, so the result can be
## sent over the wire rather than re-rolled per client (which put the same attack in
## two different places). Sakuya is absent on purpose: her daggers fly from the
## fairy's death spot under their own power, so there is no target to roll.
static func roll_extra_attack_targets(sender_char: String, rank: int = 1) -> PackedVector2Array:
	var targets := PackedVector2Array()
	if sender_char == "sakuya":
		return targets
	
	var sender_data := CharacterData.get_character(sender_char)
	var x_min: float = 35.0
	var x_max: float = 565.0
	var y_min: float = 60.0
	var y_max: float = 130.0
	if sender_data:
		x_min = sender_data.extra_attack_x_range.x
		x_max = sender_data.extra_attack_x_range.y
		y_min = sender_data.extra_attack_y_range.x
		y_max = sender_data.extra_attack_y_range.y
	
	var ex_count: int = 1
	if sender_char == "cirno":
		var t: float = clampf(float(rank - 1) / 21.0, 0.0, 1.0)
		var min_ex: int = roundi(lerpf(1.0, 3.0, t))
		var max_ex: int = roundi(lerpf(2.0, 4.0, t))
		ex_count = randi_range(min_ex, max_ex)
	
	for i in range(ex_count):
		targets.append(Vector2(randf_range(x_min, x_max), randf_range(y_min, y_max)))
	return targets

## Spectator events and replay entries are JSON, which has no Vector2, so rolled
## targets travel those two paths as a flat [x, y, x, y, ...] list.
static func targets_to_flat(targets: PackedVector2Array) -> Array:
	var flat: Array = []
	for t in targets:
		flat.append(snappedf(t.x, 0.01))
		flat.append(snappedf(t.y, 0.01))
	return flat

static func targets_from_flat(flat: Array) -> PackedVector2Array:
	var targets := PackedVector2Array()
	var i: int = 0
	while i + 1 < flat.size():
		targets.append(Vector2(float(flat[i]), float(flat[i + 1])))
		i += 2
	return targets

## Sakuya's Extra Attack does not ride a light mote to the opponent's field.
## The daggers themselves spin up at the fairy's death spot on the aggressor's
## own field, then fly the whole way under their own power, crossing directly
## into the opponent's field mid-flight (see SakuyaExtraKnife).
func _dispatch_sakuya_extra_attack(
	source_playfield: Playfield,
	target_playfield: Playfield,
	death_pos: Vector2,
	source_container: SubViewportContainer,
	target_container: SubViewportContainer
) -> void:
	if _is_blocked():
		return
	if source_playfield == null or not is_instance_valid(source_playfield):
		return
	if target_playfield == null or not is_instance_valid(target_playfield):
		return

	var source_bullets: Node = source_playfield.get_node_or_null("%Bullets")
	if source_bullets == null:
		return

	var direction_sign: float = 1.0 if source_playfield.player_number == 1 else -1.0

	for i in range(SAKUYA_KNIFE_COUNT):
		var knife: SakuyaExtraKnife = SAKUYA_EXTRA_KNIFE_SCENE.instantiate()
		source_bullets.add_child(knife)
		knife.setup(source_playfield, target_playfield, death_pos, direction_sign, float(i) * SAKUYA_KNIFE_LAUNCH_STAGGER, source_container, target_container, effects_overlay)

func spawn_mote(
	start_pt: Vector2,
	target_pt: Vector2,
	target_local: Vector2,
	target_playfield: Playfield,
	col: Color,
	payload: TravelMote.MotePayload = TravelMote.MotePayload.PELLET,
	sender_char: String = "reimu",
	bounce_count: int = 0,
	rank: int = 1
) -> void:
	if _is_blocked():
		return
	var duration: float = randf_range(1.15, 1.35)
	var arc: float = randf_range(110.0, 140.0)
	if payload == TravelMote.MotePayload.BIG_PELLET:
		duration = randf_range(1.2, 1.45)
		arc = randf_range(125.0, 155.0)
	elif payload == TravelMote.MotePayload.SPIRIT:
		duration = randf_range(1.35, 1.6)
		arc = randf_range(140.0, 175.0)
	elif payload == TravelMote.MotePayload.EXTRA_ATTACK:
		duration = randf_range(1.3, 1.5)
		arc = randf_range(150.0, 185.0)
	
	_ensure_pool()
	if mote_pool == null:
		return
	
	var mote: TravelMote = mote_pool.acquire() as TravelMote
	if mote == null:
		return
	
	mote.setup(start_pt, target_pt, col, duration, arc, payload)
	mote.arrived.connect(func(_pos: Vector2, p_payload: int):
		if _is_blocked():
			return
		if is_instance_valid(target_playfield):
			if p_payload == TravelMote.MotePayload.EXTRA_ATTACK:
				target_playfield.spawn_extra_attack(sender_char, target_local, rank)
			elif p_payload == TravelMote.MotePayload.SPIRIT:
				# Target receives spirit themed after sender's color
				var color_theme: String = "blue" if col == P1_MOTE_COLOR else "green"
				target_playfield.spawn_spirit(target_local, color_theme)
			else:
				var is_big: bool = (p_payload == TravelMote.MotePayload.BIG_PELLET)
				target_playfield.spawn_pellet(target_local, -1.0, is_big, bounce_count)
	, CONNECT_ONE_SHOT)

func clear_all_motes() -> void:
	if mote_pool:
		mote_pool.release_all_active()
	elif effects_overlay:
		for child in effects_overlay.get_children():
			if child is TravelMote:
				child.queue_free()

