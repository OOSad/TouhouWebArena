class_name RudimentaryAI
extends Node

## Rudimentary AI Sparring Partner for Touhou Web Arena.
## Implements simple, responsive bot behaviors for playtesting:
## 1. Constant, uninterrupted shooting.
## 2. Horizontal alignment with the closest active enemy.
## 3. High-priority evasion of projectiles approaching from the front.
## 4. Strict screen edge avoidance to stay clear of fairy spawn paths.

var player: Player = null
var playfield: Node2D = null

# Safe operating boundaries (Screen size: 600x960 px)
# Safe operating boundaries (Screen size: 600x960 px)
# Keep a safe margin from the outer playfield edges [95.0, 505.0] to avoid bottom-corner fairy spawn paths
const SAFE_MIN_X: float = 95.0
const SAFE_MAX_X: float = 505.0
const BASELINE_Y: float = 816.0 # Standard 85% playfield height
## Heights the bot may use without paying for it. Wide on purpose: dodging upward into
## open field is often the only real escape, and pinning it low throws that away.
const ENGAGEMENT_BAND_TOP: float = 420.0
const ENGAGEMENT_BAND_BOTTOM: float = 856.0

# Threat detection parameters
const FRONT_SCAN_HEIGHT: float = 320.0 # Vertical distance ahead to scan for bullets
const FLANK_SCAN_DEPTH: float = 40.0   # Vertical distance below/alongside player to scan for flank threats
const DANGER_HALF_WIDTH: float = 38.0  # Base proximity considered a direct collision threat
const LASER_DANGER_HALF_WIDTH: float = 55.0 # Column half-width of a vertical laser hazard
const ENEMY_BODY_SCAN_HEIGHT: float = 140.0 # Vertical distance ahead to scan for enemy body collisions
const ENEMY_BODY_SCAN_DEPTH: float = 50.0   # Vertical distance below the player to scan for enemy bodies
const DEFAULT_BOUNCE_MIN_X: float = 65.0 # Fallback wall bounds for bouncing hazards missing their own
const DEFAULT_BOUNCE_MAX_X: float = 535.0

# Hazard collection band - the widest window any consumer needs, so one pass serves them all.
# Anything outside it is rejected before we pay for a single dynamic property lookup.
const HAZARD_BAND_ABOVE: float = 320.0 # maxf(FRONT_SCAN_HEIGHT, ENEMY_BODY_SCAN_HEIGHT)
const HAZARD_BAND_BELOW: float = 50.0  # maxf(FLANK_SCAN_DEPTH, ENEMY_BODY_SCAN_DEPTH)
## Omnidirectional awareness radius. Separate from the band above, which only describes what
## is in FRONT of the bot - the scorer also needs to know what is beside and behind it.
const HAZARD_SCAN_RADIUS: float = 460.0
const HAZARD_SCAN_RADIUS_SQ: float = HAZARD_SCAN_RADIUS * HAZARD_SCAN_RADIUS

# Normalized hazard kinds
const HAZARD_BULLET: int = 0
const HAZARD_LASER: int = 1
const HAZARD_ENEMY_BODY: int = 2

var _movement_vector: Vector2 = Vector2.ZERO
var _is_focus: bool = false
var _wants_to_shoot: bool = true
var can_shoot: bool = true
@export var can_use_spells: bool = true
var queued_spell_level: int = 2
var _is_charging: bool = false
var _charge_cooldown: float = 0.5
var _decision_timer: float = 0.0
## Last pass's hazard positions, keyed by instance id, for measuring unknown velocities
var _position_samples: Dictionary = {}

# --- Velocity-obstacle scorer tuning ---
## How many seconds of collision prediction the bot gets. Short = ZUN-style twitchy.
@export var lookahead_horizon: float = 1.2
## Seconds between decisions. Doubles as reaction latency and as the perf budget.
@export var decision_interval: float = 1.0 / 30.0
## Cap on how many of the nearest hazards the bot considers at once (0 = no cap).
## Awareness is omnidirectional now, so this is what bounds the cost in dense danmaku,
## rather than a narrow scan region doing it by accident.
@export var max_tracked_hazards: int = 48
## Random jitter added to every candidate score, so a weak bot picks wrong sometimes.
@export var score_noise: float = 0.0
## How many of the leading candidates get the second-ply rollout (0 disables it).
@export var rollout_candidates: int = 3
## How far the first move is projected before asking what follow-ups remain.
@export var rollout_step: float = 0.2

const W_SURVIVAL: float = 100.0      # Points per predicted second of survival
const W_CLEARANCE: float = 45.0      # Full value of having plenty of room to spare
const CLEARANCE_SCALE: float = 60.0  # Px of room worth most of that value
const W_ALIGNMENT: float = 0.06      # Points lost per px off the firing line
const W_BASELINE: float = 0.08       # Points lost per px outside the engagement band.
									 # Higher than the old tether's weight on purpose: it
									 # now applies nowhere inside the band, so it can
									 # afford to pull firmly once the bot is truly out of
									 # position rather than nagging everywhere.
const W_EDGE: float = 25.0           # Full penalty for standing flat against a wall
const CORNER_MARGIN: float = 130.0   # Distance from walls where corner pocket avoidance engages
const W_CORNER: float = 35.0         # Penalty for settling into any 2-wall corner pocket
const W_BOTTOM_CORNER_EXTRA: float = 35.0 # Extra penalty for bottom corners where descending danmaku creates deathtraps
const W_HYSTERESIS: float = 3.0      # Bonus for holding the current heading
const W_EFFORT: float = 0.005        # Points lost per px/s of committed speed
const W_ESCAPE: float = 40.0         # Points per second of survival still reachable afterwards
const W_SCOPE_FOCUS: float = 4.0     # Bonus for focusing with a Spirit in scope range
const FOCUS_MOBILITY_PENALTY: float = 1.5
const EDGE_SOFT_MARGIN: float = 90.0 # Distance from the wall where edge pressure starts
const POSITION_PROJECTION: float = 0.25 # Seconds ahead used to judge a move's position
const SAFETY_MARGIN: float = 4.0     # Px of slack added to every collision radius
const PLAYFIELD_CENTER_X: float = 300.0
const SPIRIT_SCOPE_RANGE: float = 130.0
const WIDE_HAZARD_RADIUS: float = 30.0 # At or above this, keep full movement speed
const DEFAULT_HAZARD_RADIUS: float = 12.0

func setup(p_player: Player, p_playfield: Node2D) -> void:
	player = p_player
	playfield = p_playfield
	queued_spell_level = _pick_random_spell_level()

func reset() -> void:
	_movement_vector = Vector2.ZERO
	_is_focus = false
	_wants_to_shoot = true
	_is_charging = false
	_charge_cooldown = 0.5
	_decision_timer = 0.0
	_position_samples.clear()
	queued_spell_level = _pick_random_spell_level()

func update_ai(delta: float) -> void:
	if player == null or not is_instance_valid(player) or player.is_dead:
		_movement_vector = Vector2.ZERO
		_is_charging = false
		return
	
	# Constant, uninterrupted shooting
	_wants_to_shoot = true
	
	var hazards := _collect_hazards(delta)
	var front_threats := _filter_front_threats(hazards)
	_update_spell_charging(delta, front_threats)
	
	# Decide on a fixed cadence rather than every frame. This is both the perf
	# budget and the bot's reaction latency - it holds the last input in between.
	_decision_timer -= delta
	if _decision_timer <= 0.0:
		_decision_timer = decision_interval
		_pick_best_move(hazards)

func get_movement_vector() -> Vector2:
	return _movement_vector

func is_focusing() -> bool:
	return _is_focus

func wants_to_shoot() -> bool:
	return _wants_to_shoot and can_shoot

func set_shooting_enabled(enabled: bool) -> void:
	can_shoot = enabled

func set_spells_enabled(enabled: bool) -> void:
	can_use_spells = enabled

func wants_to_charge() -> bool:
	return _is_charging

## Target spell levels strictly encompass spellcards (Lv 2, 3, or 4) - never Lv 1 (Charge Attack)
func _pick_random_spell_level() -> int:
	var roll := randf()
	if roll < 0.50:
		return 2
	elif roll < 0.82:
		return 3
	else:
		return 4

func _has_imminent_threat(front_threats: Array[Dictionary], threshold_y: float = 90.0) -> bool:
	for t in front_threats:
		if t.get("is_laser", false) == true:
			return true
		var dy: float = t.get("dy", 999.0)
		if dy >= -FLANK_SCAN_DEPTH and dy <= threshold_y:
			return true
	return false

func _update_spell_charging(delta: float, front_threats: Array[Dictionary]) -> void:
	if not can_use_spells or player == null:
		_is_charging = false
		return
		
	if _charge_cooldown > 0.0:
		_charge_cooldown -= delta
	
	if _is_charging:
		# Target spellcard level reached? (Exclusively Lv 2, 3, or 4)
		if player.active_charge >= float(queued_spell_level):
			_is_charging = false
			_charge_cooldown = randf_range(1.5, 3.5)
			queued_spell_level = _pick_random_spell_level()
		# Or passive charge ceiling reached at or above Lv 2?
		elif player.active_charge >= player.passive_charge and player.active_charge >= 2.0:
			_is_charging = false
			_charge_cooldown = randf_range(1.5, 3.5)
			queued_spell_level = _pick_random_spell_level()
		# Emergency release ONLY if at least Lv 2 is charged AND an imminent threat is about to hit
		# (Lv 1 charge attack does NOT clear bullets with shockwave, so never emergency release at Lv 1)
		elif player.active_charge >= 2.0 and _has_imminent_threat(front_threats, 90.0):
			_is_charging = false
			_charge_cooldown = randf_range(1.0, 2.5)
			queued_spell_level = _pick_random_spell_level()
	else:
		# Start charging if cooldown is ready, passive gauge has enough for queued spell (Lv 2-4),
		# and there is no imminent threat on top of player
		if _charge_cooldown <= 0.0 and not _has_imminent_threat(front_threats, 90.0):
			if player.passive_charge >= float(queued_spell_level):
				_is_charging = true

## Normalizes every live hazard into a flat record set with plain, pre-resolved fields.
## Every dynamic lookup (`in`, `get()`, `is`) is paid exactly once here, behind an early
## vertical gate, so downstream scoring only ever touches Vector2s and floats.
func _collect_hazards(delta: float = 0.0) -> Array[Dictionary]:
	var hazards: Array[Dictionary] = []
	# Rebuilt from scratch each pass so samples for freed hazards cannot pile up
	var fresh_samples: Dictionary = {}
	if playfield == null or not is_instance_valid(playfield) or player == null:
		return hazards
	
	var p_pos: Vector2 = player.position
	
	var bullet_list: Array = []
	if playfield.has_method("get_active_bullets"):
		bullet_list = playfield.get_active_bullets()
	else:
		var bullets_layer = playfield.get("bullets_layer")
		if bullets_layer != null and is_instance_valid(bullets_layer):
			bullet_list = bullets_layer.get_children()
	
	for child in bullet_list:
		if not is_instance_valid(child) or not child.visible:
			continue
		
		# Ignore player's own outgoing bullets
		if child is PlayerBullet:
			continue
		
		# Special hazard: Marisa's vertical EarthLightRay covers its whole column,
		# so it is never gated on vertical distance
		if child is EarthLightRay and absf(child.rotation) < 0.01:
			var laser_dx: float = child.position.x - p_pos.x
			var laser_gap_sq: float = laser_dx * laser_dx
			hazards.append({
				"pos": child.position,
				"vel": Vector2.ZERO,
				"dy": p_pos.y - child.position.y,
				"radius": 32.0,
				"kind": HAZARD_LASER,
				"bounces": false,
				"bounce_min_x": 0.0,
				"bounce_max_x": 0.0,
				# A laser is a column spanning the whole field height, so its node sitting at
				# y=0 says nothing about how dangerous it is - only the horizontal gap does.
				# Ranking it by straight-line distance scored it ~490,000 against a few
				# thousand for nearby pellets, so the tracking cap culled it first every
				# time and the bot walked into Earth Light Ray without ever seeing it.
				"dist_sq": laser_gap_sq,
				"threat_sq": laser_gap_sq
			})
			continue

		# A tilted or sideways Earth Light Ray (Clownpiece's sliding stripes) is not a column, and
		# can be longer than the field, so its node says nothing; it reports the point on it
		# nearest the bot instead, and scores as a round hazard there.
		if child is EarthLightRay:
			var near: Dictionary = child.get_ai_hazard(p_pos)
			var near_pos: Vector2 = near["pos"]
			var near_sq: float = p_pos.distance_squared_to(near_pos)
			hazards.append({
				"pos": near_pos,
				"vel": near["vel"],
				"dy": p_pos.y - near_pos.y,
				"radius": near["radius"],
				"kind": HAZARD_BULLET,
				"bounces": false,
				"bounce_min_x": 0.0,
				"bounce_max_x": 0.0,
				"dist_sq": near_sq,
				"threat_sq": near_sq
			})
			continue
		
		# Early gate, before paying for any property lookups. The vertical band is what the
		# legacy front-threat filter needs; the radius is what the scorer needs, because it
		# moves in all eight directions and a bullet it just dodged is now BEHIND it. Gating
		# on 'in front' alone is why the bot would sidestep a bullet and reverse straight
		# back into it.
		var b_pos: Vector2 = child.position
		var dy: float = p_pos.y - b_pos.y
		var dist_sq: float = p_pos.distance_squared_to(b_pos)
		var in_band: bool = dy >= -HAZARD_BAND_BELOW and dy <= HAZARD_BAND_ABOVE
		if not in_band and dist_sq > HAZARD_SCAN_RADIUS_SQ:
			continue
		
		# If the bullet is canceled or inactive, ignore
		if child.get("is_canceled") == true:
			continue
		
		# Resolve velocity once. Hazards publish it inconsistently: pellets and danmaku carry
		# speed + direction, orbs expose `velocity`, but Extra Attack nodes keep theirs in a
		# private `_velocity`. Assuming "falls straight down at 240" for those is what let
		# Sakuya's knives - which fly sideways across the field - walk straight into the bot.
		var b_vel := Vector2.ZERO
		var has_direct_vel: bool = false
		if "velocity" in child and child.velocity is Vector2:
			b_vel = child.velocity
			has_direct_vel = true
		elif child.has_method("get_velocity"):
			b_vel = child.get_velocity()
			has_direct_vel = true
		elif "_velocity" in child and child._velocity is Vector2:
			b_vel = child._velocity
			has_direct_vel = true
		elif "speed" in child and "direction" in child and child.direction is Vector2:
			b_vel = child.direction * float(child.speed)
			has_direct_vel = true
		
		var hazard_id: int = child.get_instance_id()
		if child is ReisenMoonBlast:
			b_vel = Vector2.ZERO
			has_direct_vel = true
		elif not has_direct_vel and delta > 0.0 and _position_samples.has(hazard_id):
			# Nothing declared a velocity, so measure the node's own motion rather than guess.
			# Covers any hazard type added later without the bot needing to know about it.
			var measured: Vector2 = (b_pos - _position_samples[hazard_id]) / delta
			b_vel = measured
			has_direct_vel = true
		if delta > 0.0:
			fresh_samples[hazard_id] = b_pos
		
		if not has_direct_vel:
			b_vel = Vector2.DOWN * 240.0
		
		# Resolve hazard collision radius once
		var hazard_radius: float = DEFAULT_HAZARD_RADIUS
		if child is YinYangOrb or ("radius" in child and child.radius is float):
			hazard_radius = child.radius
		elif child is ReisenMoonMote:
			# Reisen's falling moon carries a proximity fuse of 181px leaving a 126px crater
			hazard_radius = 80.0
		elif child.get("is_big") == true:
			hazard_radius = 20.0
		elif child.get("bullet_data") != null and child.bullet_data.get("hitbox_radius") != null:
			hazard_radius = child.bullet_data.hitbox_radius
		elif not (child is EnemyPellet or child is DanmakuBullet):
			# Bespoke hazard node (Extra Attacks, charge attacks). Only ever a handful on
			# screen, so reading the real collision shape is affordable here and beats
			# calling a 56px dagger a 12px pellet.
			hazard_radius = _measure_collision_radius(child)
		
		# Resolve wall-reflection bounds once for bouncing hazards (Yin-Yang Orbs)
		var bounces: bool = child is YinYangOrb or "playfield_min_x" in child
		var bounce_min_x: float = DEFAULT_BOUNCE_MIN_X
		var bounce_max_x: float = DEFAULT_BOUNCE_MAX_X
		if bounces:
			if "playfield_min_x" in child:
				bounce_min_x = child.playfield_min_x
			if "playfield_max_x" in child:
				bounce_max_x = child.playfield_max_x
		
		# How close this hazard comes to where we are standing if we did nothing. Ranking on
		# this instead of raw distance keeps the bullet that is far but bearing down ahead of
		# the one that is close but already past - the tracking cap then spends its budget on
		# hazards that can actually reach us, including ones along an escape route.
		var threat_sq: float = dist_sq
		var speed_sq: float = b_vel.length_squared()
		if speed_sq > 0.0001:
			var t_close: float = clampf((p_pos - b_pos).dot(b_vel) / speed_sq, 0.0, lookahead_horizon)
			threat_sq = (b_pos + b_vel * t_close).distance_squared_to(p_pos)
		
		hazards.append({
			"pos": b_pos,
			"vel": b_vel,
			"dy": dy,
			"radius": hazard_radius,
			"kind": HAZARD_BULLET,
			"bounces": bounces,
			"bounce_min_x": bounce_min_x,
			"bounce_max_x": bounce_max_x,
			"dist_sq": dist_sq,
			"threat_sq": threat_sq
		})
	
	# Enemy body collision hazards (fairies, spirits, bosses)
	var entities_layer = playfield.get("entities_layer")
	if entities_layer != null and is_instance_valid(entities_layer):
		for child in entities_layer.get_children():
			if not is_instance_valid(child) or not child.visible or child.is_queued_for_deletion():
				continue
			
			if not (child is Fairy or child is Spirit or child is BossCharacter or child is LilyWhite):
				continue
			
			var e_pos: Vector2 = child.position
			var dy: float = p_pos.y - e_pos.y
			var dist_sq: float = p_pos.distance_squared_to(e_pos)
			var in_band: bool = dy >= -HAZARD_BAND_BELOW and dy <= HAZARD_BAND_ABOVE
			if not in_band and dist_sq > HAZARD_SCAN_RADIUS_SQ:
				continue
			
			var e_radius: float = 40.0 if (child is BossCharacter or child is LilyWhite) else 24.0
			var e_vel: Vector2 = Vector2.ZERO
			var hazard_id: int = child.get_instance_id()
			var has_direct_vel: bool = false
			if "velocity" in child and child.velocity is Vector2:
				e_vel = child.velocity
				has_direct_vel = true
			elif "_velocity" in child and child._velocity is Vector2:
				e_vel = child._velocity
				has_direct_vel = true
			elif delta > 0.0 and _position_samples.has(hazard_id):
				e_vel = (e_pos - _position_samples[hazard_id]) / delta
				has_direct_vel = true
			if delta > 0.0:
				fresh_samples[hazard_id] = e_pos
			
			if not has_direct_vel:
				if child is Fairy and child.speed > 0.0:
					e_vel = Vector2.DOWN * child.speed
			
			var e_threat_sq: float = dist_sq
			var e_speed_sq: float = e_vel.length_squared()
			if e_speed_sq > 0.0001:
				var t_close: float = clampf((p_pos - e_pos).dot(e_vel) / e_speed_sq, 0.0, lookahead_horizon)
				e_threat_sq = (e_pos + e_vel * t_close).distance_squared_to(p_pos)
			
			hazards.append({
				"pos": e_pos,
				"vel": e_vel,
				"dy": dy,
				"radius": e_radius,
				"kind": HAZARD_ENEMY_BODY,
				"bounces": false,
				"bounce_min_x": 0.0,
				"bounce_max_x": 0.0,
				"dist_sq": dist_sq,
				"threat_sq": e_threat_sq
			})
	
	if delta > 0.0:
		_position_samples = fresh_samples
	
	return hazards

## Reads a hazard's real collision shape when it publishes no radius of its own.
func _measure_collision_radius(node: Node) -> float:
	for child in node.get_children():
		if not (child is CollisionShape2D) or child.shape == null:
			continue
		var shape: Shape2D = child.shape
		var radius: float = DEFAULT_HAZARD_RADIUS
		if shape is CircleShape2D or shape is CapsuleShape2D:
			radius = shape.radius
		elif shape is RectangleShape2D:
			# Half the narrow side: what actually has to be cleared sideways
			radius = minf(shape.size.x, shape.size.y) * 0.5
		else:
			continue
		return radius * maxf(absf(node.scale.x), absf(node.scale.y))
	
	return DEFAULT_HAZARD_RADIUS

## Projects a hazard's X position forward to the player's Y level, following wall bounces
func _predict_x_at_player(hazard: Dictionary, dy: float) -> float:
	var pred_x: float = hazard.pos.x
	
	# Only meaningful while the hazard is in front and moving downward at a real pace
	if dy > 0.0 and hazard.vel.y > 10.0:
		var time_to_y: float = dy / hazard.vel.y
		pred_x = hazard.pos.x + hazard.vel.x * time_to_y
		
		if hazard.bounces:
			var span: float = hazard.bounce_max_x - hazard.bounce_min_x
			if span > 10.0:
				var shifted: float = pred_x - hazard.bounce_min_x
				var mod_val: float = fposmod(shifted, span * 2.0)
				if mod_val > span:
					pred_x = hazard.bounce_max_x - (mod_val - span)
				else:
					pred_x = hazard.bounce_min_x + mod_val
	
	return pred_x

## Scans for incoming projectiles in front of the player (above and heading down)
func _scan_front_threats() -> Array[Dictionary]:
	return _filter_front_threats(_collect_hazards())

## Narrows a collected hazard set down to what actually threatens the player's column
func _filter_front_threats(hazards: Array[Dictionary]) -> Array[Dictionary]:
	var threats: Array[Dictionary] = []
	if player == null or not is_instance_valid(player):
		return threats
	
	var p_pos: Vector2 = player.position
	
	for hazard in hazards:
		var kind: int = hazard.kind
		
		if kind == HAZARD_LASER:
			if abs(hazard.pos.x - p_pos.x) < LASER_DANGER_HALF_WIDTH:
				threats.append({
					"pos": hazard.pos,
					"vel": Vector2.ZERO,
					"pred_x": hazard.pos.x,
					"radius": hazard.radius,
					"is_laser": true
				})
			continue
		
		var dy: float = hazard.dy
		# Threat threshold scales with the hazard's actual radius
		var danger_width: float = maxf(DANGER_HALF_WIDTH, hazard.radius + 24.0)
		
		if kind == HAZARD_ENEMY_BODY:
			# Dangerously close vertically (from 140px above down to 50px below)
			if dy < -ENEMY_BODY_SCAN_DEPTH or dy > ENEMY_BODY_SCAN_HEIGHT:
				continue
			if abs(hazard.pos.x - p_pos.x) < danger_width:
				threats.append({
					"pos": hazard.pos,
					"vel": Vector2.ZERO,
					"pred_x": hazard.pos.x,
					"dy": dy,
					"radius": hazard.radius,
					"is_laser": false,
					"is_flank": (dy <= 0.0),
					"is_enemy_body": true
				})
			continue
		
		# Threat window: from FRONT_SCAN_HEIGHT (320px) above down to FLANK_SCAN_DEPTH (40px) below
		if dy < -FLANK_SCAN_DEPTH or dy > FRONT_SCAN_HEIGHT:
			continue
		
		var pred_x: float = _predict_x_at_player(hazard, dy)
		var cur_dx: float = abs(hazard.pos.x - p_pos.x)
		var pred_dx: float = abs(pred_x - p_pos.x)
		
		if cur_dx < danger_width or pred_dx < danger_width:
			threats.append({
				"pos": hazard.pos,
				"vel": hazard.vel,
				"pred_x": pred_x,
				"dy": dy,
				"radius": hazard.radius,
				"is_laser": false,
				"is_flank": (dy <= 0.0)
			})
	
	return threats

# =============================================================================
# Velocity-obstacle movement scorer
#
# Instead of ordering hand-written rules by priority, score every velocity the
# player can actually command this frame and take the best one. "Stay off the
# edge", "line up with enemies" and "stream upward when pinned" stop being rules
# and become weights, so they can trade off against each other honestly.
# =============================================================================

## Candidate headings: the 9 inputs a player can actually hold. Diagonals are
## normalized because the player applies the raw vector straight to its speed.
const MOVE_DIRECTIONS := [
	Vector2.ZERO,
	Vector2(-1.0, 0.0), Vector2(1.0, 0.0),
	Vector2(0.0, -1.0), Vector2(0.0, 1.0),
	Vector2(-0.7071068, -0.7071068), Vector2(0.7071068, -0.7071068),
	Vector2(-0.7071068, 0.7071068), Vector2(0.7071068, 0.7071068)
]

## Evaluates one hazard against one candidate velocity, assuming both hold course.
## Returns (time_to_collision, closest_approach): x is INF when the paths never close,
## y is how much room we would have left at the tightest moment of the window.
## Packed into a Vector2 because this runs 18 times per hazard per decision.
func _evaluate_hazard(hazard: Dictionary, p_pos: Vector2, p_vel: Vector2, combined_radius: float) -> Vector2:
	return _evaluate_hazard_raw(hazard.pos, hazard.vel, hazard.kind, p_pos, p_vel, combined_radius)

## Same test against loose values, so the rollout can reuse it on projected hazard
## positions without allocating a dictionary per hazard per candidate.
func _evaluate_hazard_raw(h_pos: Vector2, h_vel: Vector2, kind: int, p_pos: Vector2, p_vel: Vector2, combined_radius: float) -> Vector2:
	# A vertical laser is a column, not a disc - only horizontal approach matters
	if kind == HAZARD_LASER:
		var dx: float = h_pos.x - p_pos.x
		if abs(dx) <= combined_radius:
			return Vector2(0.0, 0.0)
		
		var rel_vx: float = -p_vel.x
		var dx_end: float = dx + rel_vx * lookahead_horizon
		var gap: float = 0.0
		if signf(dx) == signf(dx_end):
			gap = maxf(minf(abs(dx), abs(dx_end)) - combined_radius, 0.0)
		
		if abs(rel_vx) < 0.0001:
			return Vector2(INF, gap)
		
		var edge: float = combined_radius if dx > 0.0 else -combined_radius
		var t_edge: float = (edge - dx) / rel_vx
		return Vector2(t_edge if t_edge >= 0.0 else INF, gap)
	
	var d: Vector2 = h_pos - p_pos
	var vr: Vector2 = h_vel - p_vel
	var a: float = vr.length_squared()
	
	# How close the two ever get inside the prediction window
	var t_close: float = 0.0
	if a >= 0.0001:
		t_close = clampf(-d.dot(vr) / a, 0.0, lookahead_horizon)
	var clearance: float = maxf((d + vr * t_close).length() - combined_radius, 0.0)
	
	var c: float = d.length_squared() - combined_radius * combined_radius
	if c <= 0.0:
		return Vector2(0.0, 0.0) # already overlapping
	if a < 0.0001:
		return Vector2(INF, clearance)
	
	var b: float = 2.0 * d.dot(vr)
	if b >= 0.0:
		return Vector2(INF, clearance) # separating
	
	var disc: float = b * b - 4.0 * a * c
	if disc < 0.0:
		return Vector2(INF, clearance) # passes by without touching
	
	var t: float = (-b - sqrt(disc)) / (2.0 * a)
	return Vector2(t if t >= 0.0 else INF, clearance)

## Difficulty dial: a weaker bot simply sees fewer of the bullets on screen,
## rather than being handed worse rules.
func _limit_hazards(hazards: Array[Dictionary]) -> Array[Dictionary]:
	if max_tracked_hazards <= 0 or hazards.size() <= max_tracked_hazards:
		return hazards

	var ranked: Array[Dictionary] = hazards.duplicate()
	ranked.sort_custom(
		func(a: Dictionary, b: Dictionary) -> bool:
			return a.threat_sq < b.threat_sq
	)
	return ranked.slice(0, max_tracked_hazards)

## Closest shootable enemy above the player, as an X to line up with.
func _find_alignment_target_x() -> float:
	var target_x: float = PLAYFIELD_CENTER_X
	if playfield == null or not is_instance_valid(playfield) or player == null:
		return target_x

	var entities_layer = playfield.get("entities_layer")
	if entities_layer == null or not is_instance_valid(entities_layer):
		return target_x

	var p_pos: Vector2 = player.position
	var closest_dist: float = 99999.0
	for child in entities_layer.get_children():
		if not is_instance_valid(child) or not child.visible or child.is_queued_for_deletion():
			continue
		if not (child is Fairy or child is Spirit or child is BossCharacter or child is LilyWhite):
			continue
		# Enemies behind/below the player cannot be hit by upward shots
		if child.position.y > p_pos.y - 30.0:
			continue
		var dist: float = p_pos.distance_to(child.position)
		if dist < closest_dist:
			closest_dist = dist
			target_x = child.position.x

	return target_x

## True when an unactivated Spirit is close enough that Focus would scoop it up.
func _wants_spirit_scope_focus() -> bool:
	if playfield == null or not is_instance_valid(playfield) or player == null:
		return false

	var entities_layer = playfield.get("entities_layer")
	if entities_layer == null or not is_instance_valid(entities_layer):
		return false

	var p_pos: Vector2 = player.position
	for child in entities_layer.get_children():
		if not is_instance_valid(child) or not child.visible or child.is_queued_for_deletion():
			continue
		if child is Spirit and not child.get("is_activated"):
			if p_pos.distance_to(child.position) < SPIRIT_SCOPE_RANGE:
				return true

	return false

## The velocity the player can actually sustain from here. Position is clamped to the
## playfield every frame, so a heading that runs into a wall only delivers whatever room
## is left. Without this the scorer rates 'dive into the floor' as a clean escape, and the
## bot sits against the bottom wall holding a direction that moves it nowhere while a
## pellet lands on its head.
func _effective_velocity(p_pos: Vector2, vel: Vector2) -> Vector2:
	var horizon: float = maxf(lookahead_horizon, 0.01)
	var out := vel
	
	if vel.x > 0.0:
		out.x = minf(vel.x, maxf(Player.MAX_X - p_pos.x, 0.0) / horizon)
	elif vel.x < 0.0:
		out.x = -minf(-vel.x, maxf(p_pos.x - Player.MIN_X, 0.0) / horizon)
	
	if vel.y > 0.0:
		out.y = minf(vel.y, maxf(Player.MAX_Y - p_pos.y, 0.0) / horizon)
	elif vel.y < 0.0:
		out.y = -minf(-vel.y, maxf(p_pos.y - Player.MIN_Y, 0.0) / horizon)
	
	return out

## Everything that is not about surviving the next second: where we would rather stand.
func _positional_score(p_pos: Vector2, vel: Vector2, dir: Vector2, target_x: float, focus: bool, wants_scope_focus: bool) -> float:
	# Where this move is taking us. Judged over a fixed planning step rather than a single
	# decision tick: one tick of travel is a handful of pixels, which makes every
	# positional gradient smaller than the flat hysteresis bonus, and the bot then sits
	# still through pressure it can plainly see.
	var step: float = maxf(decision_interval, POSITION_PROJECTION)
	var cand := Vector2(
		clampf(p_pos.x + vel.x * step, Player.MIN_X, Player.MAX_X),
		clampf(p_pos.y + vel.y * step, Player.MIN_Y, Player.MAX_Y)
	)

	var score: float = 0.0

	# Line up with whatever we are shooting at
	score -= abs(cand.x - target_x) * W_ALIGNMENT
	# Engagement band, not an engagement line. The legacy bot was tethered to a single
	# baseline height, which in scoring terms charged ~17 points to stand mid-field -
	# about a third of the whole clearance term - so the bot bought its way out of
	# trouble sideways and never upward. Inside the band height is free; the pull only
	# resumes past the top of it, where fairy spawn paths start to matter.
	if cand.y < ENGAGEMENT_BAND_TOP:
		score -= (ENGAGEMENT_BAND_TOP - cand.y) * W_BASELINE
	elif cand.y > ENGAGEMENT_BAND_BOTTOM:
		score -= (cand.y - ENGAGEMENT_BAND_BOTTOM) * W_BASELINE

	# Soft pressure away from every wall independently. Paying for each wall separately ensures
	# that being near both a side wall and the floor costs more than being against a single flat wall.
	var dist_x: float = minf(cand.x - Player.MIN_X, Player.MAX_X - cand.x)
	var dist_y: float = minf(cand.y - Player.MIN_Y, Player.MAX_Y - cand.y)
	if dist_x < EDGE_SOFT_MARGIN:
		score -= (1.0 - dist_x / EDGE_SOFT_MARGIN) * W_EDGE
	if dist_y < EDGE_SOFT_MARGIN:
		score -= (1.0 - dist_y / EDGE_SOFT_MARGIN) * W_EDGE

	# Corner avoidance: being pinched against two walls simultaneously cuts off up to 75% of
	# available headings. Bottom corners are especially lethal because danmaku descends from above.
	if dist_x < CORNER_MARGIN and dist_y < CORNER_MARGIN:
		var corner_severity: float = (1.0 - dist_x / CORNER_MARGIN) * (1.0 - dist_y / CORNER_MARGIN)
		var is_bottom_corner: bool = cand.y > (Player.MIN_Y + Player.MAX_Y) * 0.5
		var corner_penalty: float = corner_severity * (W_CORNER + (W_BOTTOM_CORNER_EXTRA if is_bottom_corner else 0.0))
		score -= corner_penalty

	# Speed that is not buying safety is not free: drifting at 540 px/s when nothing is
	# pressing is how the bot sails past its escape and into the next bullet. The survival
	# term outweighs this the moment a move actually earns its speed.
	score -= vel.length() * W_EFFORT

	# Prefer holding the current heading so the bot does not vibrate between equal options
	if dir.is_equal_approx(_movement_vector):
		score += W_HYSTERESIS

	if focus:
		score += W_SCOPE_FOCUS if wants_scope_focus else -FOCUS_MOBILITY_PENALTY

	return score

## Second ply: having survived this move, would we still have anywhere to go?
##
## A one-step scorer walks into dead ends. Every move it picks is locally safe, and then
## it arrives somewhere with no safe follow-up at all - which is exactly how a bot gets
## boxed in by a dense barrage. This projects the world forward by one rollout step and
## reports the best survival still reachable from where the move lands, so a move into a
## pocket loses to one that keeps its options open.
##
## Hazards advance in a straight line here; wall bounces are not re-simulated. Follow-ups
## are priced at full speed only, since the bot can always choose not to Focus.
func _escape_potential(tracked: Array[Dictionary], radii: PackedFloat32Array, future_positions: PackedVector2Array, p_pos: Vector2, vel: Vector2, spd_normal: float) -> float:
	var landing := Vector2(
		clampf(p_pos.x + vel.x * rollout_step, Player.MIN_X, Player.MAX_X),
		clampf(p_pos.y + vel.y * rollout_step, Player.MIN_Y, Player.MAX_Y)
	)
	
	var best_1: float = 0.0
	var best_2: float = 0.0
	var best_3: float = 0.0
	for dir in MOVE_DIRECTIONS:
		var follow_vel: Vector2 = _effective_velocity(landing, dir * spd_normal)
		# A heading blocked by a wall yields virtually zero mobility for escaping
		if dir != Vector2.ZERO and follow_vel.length() < spd_normal * 0.25:
			continue
		var survival: float = lookahead_horizon
		for i in tracked.size():
			var ttc: float = _evaluate_hazard_raw(future_positions[i], tracked[i].vel, tracked[i].kind, landing, follow_vel, radii[i]).x
			if ttc < survival:
				survival = ttc
				if survival <= 0.0:
					break
		if survival > best_1:
			best_3 = best_2
			best_2 = best_1
			best_1 = survival
		elif survival > best_2:
			best_3 = best_2
			best_2 = survival
		elif survival > best_3:
			best_3 = survival
	
	# Reward multiple viable escape routes (open-field branching factor).
	# Landing in a corner or narrow pinch with only 1 tight corridor is heavily discounted
	# compared to open field where multiple evasive maneuvers remain open.
	return best_1 * 0.60 + best_2 * 0.25 + best_3 * 0.15

## Greedy velocity-obstacle selection: survival dominates, positioning breaks ties.
func _pick_best_move(hazards: Array[Dictionary]) -> void:
	var p_pos: Vector2 = player.position
	var hurtbox: float = player.hurtbox_radius if "hurtbox_radius" in player else 3.0
	var spd_normal: float = player.normal_speed if "normal_speed" in player else 540.0
	var spd_focus: float = player.focus_speed if "focus_speed" in player else 270.0

	var tracked: Array[Dictionary] = _limit_hazards(hazards)
	var target_x: float = _find_alignment_target_x()
	var wants_scope_focus: bool = _wants_spirit_scope_focus()

	# Combined radii do not depend on the candidate, so resolve them once
	var radii := PackedFloat32Array()
	radii.resize(tracked.size())
	for i in tracked.size():
		radii[i] = tracked[i].radius + hurtbox + SAFETY_MARGIN

	# Mobility override. A greedy one-step scorer only sees the next second, so it will
	# happily Focus in front of a slow wide hazard because the window says it survives.
	# In practice more danmaku is already on its way and half speed is how you get
	# cornered, so a wide hazard takes the Focus candidates off the table entirely.
	var focus_options := [false, true]
	for hazard in tracked:
		if hazard.radius >= WIDE_HAZARD_RADIUS and hazard.dy >= -FLANK_SCAN_DEPTH and hazard.dy <= FRONT_SCAN_HEIGHT:
			focus_options = [false]
			break

	var scored: Array[Dictionary] = []
	var under_threat: bool = false

	for focus in focus_options:
		var speed: float = spd_focus if focus else spd_normal
		for dir in MOVE_DIRECTIONS:
			# What this heading is actually worth from where we are standing
			var vel: Vector2 = _effective_velocity(p_pos, dir * speed)

			var survival: float = lookahead_horizon
			var clearance: float = INF
			for i in tracked.size():
				var hazard_eval := _evaluate_hazard(tracked[i], p_pos, vel, radii[i])
				survival = minf(survival, hazard_eval.x)
				clearance = minf(clearance, hazard_eval.y)
				if survival <= 0.0 and clearance <= 0.0:
					break

			if survival < lookahead_horizon:
				under_threat = true

			# Surviving the window is the point; keeping room to react is what separates two
			# moves that both survive it. Without this the bot happily shaves every bullet.
			# Diminishing returns, so a comfortable move never loses to a wildly distant one.
			var score: float = survival * W_SURVIVAL
			if clearance < INF:
				score += W_CLEARANCE * (1.0 - exp(-clearance / CLEARANCE_SCALE))
			else:
				score += W_CLEARANCE
			score += _positional_score(p_pos, vel, dir, target_x, focus, wants_scope_focus)
			if score_noise > 0.0:
				score += randf_range(-score_noise, score_noise)

			scored.append({"score": score, "dir": dir, "focus": focus, "vel": vel})

	# Second ply, but only when something is actually threatening, and only for the few
	# candidates still in contention. Calm play pays nothing for this; the rest of the time
	# it is what stops the bot taking a safe-looking move into a pocket it cannot leave.
	if under_threat and rollout_candidates > 0 and not tracked.is_empty():
		scored.sort_custom(
			func(a: Dictionary, b: Dictionary) -> bool:
				return a.score > b.score
		)

		# Advance every tracked hazard once; the step is the same for all candidates
		var future_positions := PackedVector2Array()
		future_positions.resize(tracked.size())
		for i in tracked.size():
			future_positions[i] = tracked[i].pos + tracked[i].vel * rollout_step

		var deepened: int = mini(rollout_candidates, scored.size())
		for i in deepened:
			var escape: float = _escape_potential(tracked, radii, future_positions, p_pos, scored[i].vel, spd_normal)
			scored[i].score += escape * W_ESCAPE

	var best: Dictionary = {}
	for candidate in scored:
		if best.is_empty() or candidate.score > best.score:
			best = candidate

	if best.is_empty():
		_movement_vector = Vector2.ZERO
		_is_focus = false
		return

	_movement_vector = best.dir
	_is_focus = best.focus

## Maps a 1-9 skill level onto the bot's perception and reflexes rather than onto
## special-case rules, so difficulty rises smoothly instead of in lumps.
## Level 1 barely looks ahead and reacts slowly; level 9 tracks the 48 nearest hazards at
## 30Hz with a full horizon and no scoring noise.
func configure_difficulty(level: int) -> void:
	var t: float = clampf((float(level) - 1.0) / 8.0, 0.0, 1.0)
	lookahead_horizon = lerpf(0.35, 1.30, t)
	decision_interval = lerpf(1.0 / 8.0, 1.0 / 30.0, t)
	max_tracked_hazards = int(roundf(lerpf(6.0, 48.0, t)))
	score_noise = lerpf(16.0, 0.0, t)

