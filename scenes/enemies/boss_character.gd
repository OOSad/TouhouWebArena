class_name BossCharacter
extends Area2D

## Level 4 Boss Character entity for Touhou Web Arena.
## Swoops into the upper playfield from offscreen upon Level 4 activation,
## performs evasive short hops to disrupt opponent aim,
## absorbs fire (~7s Marisa fire to defeat), leaves naturally after ~12s,
## and can be canceled out by opponent's Level 4 spellcard.

signal hit(damage: float, remaining_health: float)
signal health_changed(current_hp: float, max_hp: float)
signal defeated(death_pos: Vector2, source: String)
signal dispelled()
signal left_screen()
signal attack_started(spell_name: String)
## Emitted once perform_strafe_dash's initial jump-to-start_pos segment lands, before the dash
## segment begins - lets a calling step await just the jump, not the whole two-phase move.
signal strafe_jump_landed()

enum State {
	ENTERING,
	ACTIVE,
	HOPPING,
	LEAVING,
	DISPELLED,
	DEFEATED
}

enum Phase {
	IDLE,
	POST_ENTRANCE,
	CASTING,
	POST_CAST,
	POST_HOP,
}

const DEFEAT_SHOCKWAVE_SCENE: PackedScene = preload("res://scenes/effects/defeat_shockwave.tscn")

@export var boss_data: BossData = null
@export var current_health: float = 180.0
@export var max_health: float = 180.0

var rank: int = 1
var playfield: Node2D = null

## Seeded per boss spawn by Playfield.spawn_boss() (from the match's synced round seed) so
## attack-pattern selection and hop AI roll identically on both netplay clients instead of
## diverging - see the comment on Playfield._round_seed for the full rationale. Individual
## DanmakuStep resources still use plain global randomness for their own bullet-level jitter,
## which is intentionally left unsynced.
var _rng: RandomNumberGenerator = RandomNumberGenerator.new()
var state: State = State.ENTERING
var is_vulnerable: bool = false
var is_dead: bool = false

var _time_alive: float = 0.0
var _hop_timer: float = 0.0
var _attack_timer: float = 0.0
var _phase: Phase = Phase.IDLE
var _phase_timer: float = 0.0
var _current_pattern_idx: int = 0
var _attacks_performed: int = 0
var _target_attack_count: int = 5
var _target_duration: float = 12.0
var _is_casting: bool = false
var _base_position: Vector2 = Vector2.ZERO
var _bob_time: float = 0.0
var _hit_tween: Tween = null
var _move_tween: Tween = null

# Animation parameters
var _frame_timer: float = 0.0
var _anim_frame: int = 0
var _anim_row: int = 0 # 0: Idle, 1: Move / Bank, 2: Attack / Cast
var _frame_duration: float = 0.08
var _frames_per_row: int = 4

@onready var sprite: Sprite2D = get_node_or_null("Sprite2D")
@onready var collision_shape: CollisionShape2D = get_node_or_null("CollisionShape2D")
@onready var magic_circle: BossMagicCircle = get_node_or_null("MagicCircle")
@onready var life_ring: BossLifeRing = get_node_or_null("LifeRing")

func _ready() -> void:
	# Start non-monitorable while offscreen
	monitorable = false
	monitoring = false
	is_vulnerable = false
	
	if boss_data:
		_apply_boss_data()

func setup(p_data: BossData, p_rank: int = 1, p_playfield: Node2D = null, p_seed: int = -1) -> void:
	boss_data = p_data
	rank = p_rank
	playfield = p_playfield
	if p_seed >= 0:
		_rng.seed = p_seed
	else:
		_rng.randomize()

	if boss_data:
		max_health = boss_data.max_health
		current_health = max_health
		_apply_boss_data()
	
	_start_entrance()

func _apply_boss_data() -> void:
	if boss_data == null:
		return
	
	_frames_per_row = boss_data.hframes
	_frame_duration = boss_data.frame_duration
	
	if sprite == null:
		sprite = get_node_or_null("Sprite2D")
	if collision_shape == null:
		collision_shape = get_node_or_null("CollisionShape2D")
	
	if sprite:
		if boss_data.sprite_sheet:
			sprite.texture = boss_data.sprite_sheet
		sprite.hframes = boss_data.hframes
		sprite.vframes = boss_data.vframes
		sprite.scale = boss_data.sprite_scale
		sprite.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	
	if collision_shape:
		var circle := CircleShape2D.new()
		var rad: float = boss_data.collision_radius if ("collision_radius" in boss_data and boss_data.collision_radius > 0.0) else 32.0
		circle.radius = rad
		collision_shape.shape = circle

func _start_entrance() -> void:
	state = State.ENTERING
	is_vulnerable = false
	monitorable = false
	
	var target_pos: Vector2 = boss_data.entrance_target_pos if boss_data else Vector2(300.0, 150.0)
	var offset: Vector2 = boss_data.entrance_start_offset if boss_data else Vector2(160.0, -220.0)
	var start_pos: Vector2 = target_pos + offset
	
	position = start_pos
	_base_position = target_pos
	
	# Banking as swooping in from offscreen
	_set_anim_state(1, offset.x > 0)
	
	var dur: float = boss_data.entrance_duration if boss_data else 0.8
	
	if _move_tween and _move_tween.is_valid():
		_move_tween.kill()
	
	_move_tween = create_tween()
	_move_tween.tween_property(self, "position", target_pos, dur).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	_move_tween.tween_callback(_on_entrance_completed)

func _on_entrance_completed() -> void:
	if state != State.ENTERING:
		return
	state = State.ACTIVE
	is_vulnerable = true
	monitorable = true
	_set_anim_state(0, false)
	if life_ring:
		life_ring.appear()
	var target_pos: Vector2 = boss_data.entrance_target_pos if boss_data else Vector2(300.0, 150.0)
	position = target_pos
	_base_position = target_pos
	_bob_time = 0.0
	_attacks_performed = 0
	
	var a_min: int = boss_data.attacks_min if (boss_data and "attacks_min" in boss_data) else 5
	var a_max: int = boss_data.attacks_max if (boss_data and "attacks_max" in boss_data) else 9
	var should_randomize: bool = boss_data.randomize_attack_count if (boss_data and "randomize_attack_count" in boss_data) else true
	if should_randomize:
		_target_attack_count = _rng.randi_range(a_min, a_max)
	else:
		var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
		_target_attack_count = int(round(lerpf(float(a_min), float(a_max), t_rank)))

	var should_randomize_duration: bool = boss_data.randomize_duration if (boss_data and "randomize_duration" in boss_data) else false
	if should_randomize_duration:
		_target_duration = _rng.randf_range(boss_data.duration_min, boss_data.duration_max)
	else:
		_target_duration = boss_data.duration if boss_data else 12.0

	var initial_delay: float = boss_data.initial_attack_delay if boss_data else 0.4
	_attack_timer = initial_delay
	_phase = Phase.POST_ENTRANCE
	_phase_timer = initial_delay

func _physics_process(delta: float) -> void:
	# 1. Update sprite frame animation
	_frame_timer += delta
	if _frame_timer >= _frame_duration:
		_frame_timer -= _frame_duration
		if _anim_row == 0:
			# Idle animation loops continuously
			_anim_frame = (_anim_frame + 1) % _frames_per_row
		else:
			# Side-to-side banking or attack poses hold on their finished state (do not loop)
			if _anim_frame < _frames_per_row - 1:
				_anim_frame += 1
		_update_sprite_frame()
	
	# Only execute lifetime and bobbing when active or hopping
	if state != State.ACTIVE and state != State.HOPPING:
		return
	
	# 2. Lifetime tracking (natural expiration after duration or safety fallback)
	_time_alive += delta
	var max_dur: float = boss_data.duration_safety if (boss_data and "duration_safety" in boss_data and boss_data.duration_safety > 0.0) else 24.0
	if boss_data and boss_data.departure_mode == BossData.DepartureMode.DURATION:
		max_dur = _target_duration
	if _time_alive >= max_dur and (state == State.ACTIVE or state == State.HOPPING):
		leave_screen()
		return
	
	# 3. Idle bobbing when ACTIVE
	if state == State.ACTIVE:
		var bob_amp: float = boss_data.bob_amplitude if boss_data else 4.0
		var bob_freq: float = boss_data.bob_frequency if boss_data else 3.0
		_bob_time += delta * bob_freq
		position = _base_position + Vector2(0.0, sin(_bob_time) * bob_amp)
	
	# 4. Cadence progression
	if state == State.ACTIVE and not is_dead and not _is_casting:
		if _phase == Phase.POST_ENTRANCE:
			_phase_timer -= delta
			if _phase_timer <= 0.0:
				_trigger_attack()
		elif _phase == Phase.POST_CAST:
			_phase_timer -= delta
			if _phase_timer <= 0.0:
				_perform_hop()
		elif _phase == Phase.POST_HOP:
			_phase_timer -= delta
			if _phase_timer <= 0.0:
				_trigger_attack()

func _trigger_attack() -> void:
	if is_dead or state != State.ACTIVE:
		return
	if boss_data == null or boss_data.attack_patterns.is_empty():
		return
	
	var pattern: SpellcardData = null
	if boss_data.selection_mode == BossData.AttackSelectionMode.RANDOM:
		# Array.pick_random() draws from Godot's global (unseeded, per-client) RNG - picked by
		# hand from _rng instead so both netplay clients choose the same attack.
		pattern = boss_data.attack_patterns[_rng.randi_range(0, boss_data.attack_patterns.size() - 1)]
	else:
		pattern = boss_data.attack_patterns[_current_pattern_idx % boss_data.attack_patterns.size()]
		_current_pattern_idx += 1
	
	_attacks_performed += 1
	_is_casting = true
	_phase = Phase.CASTING
	_set_anim_state(2, false) # Row 2: Gohei sweep / cast pose
	attack_started.emit(pattern.spellcard_name if pattern else "")
	
	var rate: float = boss_data.attack_rate if boss_data.attack_rate > 0.0 else 1.6
	_attack_timer = rate
	
	# Drawn here, unconditionally and before the pattern runs, so it keeps a fixed place
	# in _rng's draw order on both clients (same reasoning as the pattern pick above).
	var pattern_seed: int = _rng.randi() & 0x7FFFFFFF
	
	if is_inside_tree():
		_execute_attack_with_pose(pattern, pattern_seed)
	elif playfield and is_instance_valid(playfield) and pattern:
		pattern.execute_sequence(playfield, position, rank, pattern_seed)

func _execute_attack_with_pose(pattern: SpellcardData, pattern_seed: int = -1) -> void:
	var min_timer := get_tree().create_timer(0.55)
	if playfield and is_instance_valid(playfield) and pattern:
		await pattern.execute_sequence(playfield, position, rank, pattern_seed)
	if min_timer.time_left > 0.0:
		await min_timer.timeout
	_on_cast_finished()

func _on_cast_finished() -> void:
	_is_casting = false
	if is_dead or state != State.ACTIVE:
		return
	_set_anim_state(0, false) # Return to idle hover
	
	# Check departure condition after finishing attack
	var should_depart: bool = false
	if boss_data:
		if boss_data.departure_mode == BossData.DepartureMode.ATTACK_COUNT or boss_data.departure_mode == BossData.DepartureMode.HYBRID:
			if _attacks_performed >= _target_attack_count:
				should_depart = true
	
	if should_depart:
		leave_screen()
		return
	
	_phase = Phase.POST_CAST
	_phase_timer = boss_data.post_cast_delay if boss_data else 0.3

func _set_anim_state(row: int, flip: bool = false) -> void:
	if _anim_row != row:
		_anim_row = row
		_anim_frame = 0
		_frame_timer = 0.0
	if sprite:
		sprite.flip_h = flip
	_update_sprite_frame()

func _update_sprite_frame() -> void:
	if sprite:
		sprite.frame = _anim_row * _frames_per_row + _anim_frame

## Performs a hop to the side while in cast pose (e.g. Marisa's Lines of Stars).
func perform_cast_hop(distance: float = 80.0, duration: float = 1.55) -> void:
	if state != State.ACTIVE or is_dead:
		return
	
	var bounds: Rect2 = boss_data.roam_bounds if boss_data else Rect2(140.0, 110.0, 320.0, 100.0)
	var min_x: float = bounds.position.x
	var max_x: float = bounds.end.x
	var min_y: float = bounds.position.y
	var max_y: float = bounds.end.y
	
	var dist_to_left: float = _base_position.x - min_x
	var dist_to_right: float = max_x - _base_position.x
	
	var move_right: bool = false
	if dist_to_left < distance + 10.0:
		move_right = true
	elif dist_to_right < distance + 10.0:
		move_right = false
	else:
		move_right = (_rng.randf() < 0.5)

	var offset_x: float = distance if move_right else -distance
	var target_x: float = clampf(_base_position.x + offset_x, min_x, max_x)
	var offset_y: float = _rng.randf_range(-6.0, 6.0)
	var target_y: float = clampf(_base_position.y + offset_y, min_y, max_y)
	var target_base := Vector2(target_x, target_y)
	
	# Horizontal facing during cast hop
	if sprite:
		if target_x < _base_position.x - 2.0:
			sprite.flip_h = true
		elif target_x > _base_position.x + 2.0:
			sprite.flip_h = false
	
	if _move_tween and _move_tween.is_valid():
		_move_tween.kill()
	
	_move_tween = create_tween()
	_move_tween.tween_property(self, "_base_position", target_base, duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)

## Boss "Knife Strafing": smoothly glides to start_pos (e.g. the left playfield edge) over
## jump_duration, then dashes to end_pos (e.g. the right edge) at constant speed over
## dash_duration - deliberately ignoring the normal roam_bounds clamp that
## perform_cast_hop/_perform_hop respect, since this is meant to be a genuine edge-to-edge
## traversal, not a bounded hop. Stays in State.ACTIVE and tweens _base_position (not position
## directly), same as perform_cast_hop, so idle bobbing keeps applying on top and the calling step
## can sample the live position at each of its own ticks. Both segments run on one chained Tween
## (sequential by default); strafe_jump_landed fires right as the first segment finishes, so a
## calling step can await just the jump before it starts firing during the dash segment.
func perform_strafe_dash(start_pos: Vector2, end_pos: Vector2, jump_duration: float, dash_duration: float, hold_duration: float = 0.0, dash_ease_out: bool = false) -> void:
	if state != State.ACTIVE or is_dead:
		return

	# Switch to the Move/Bank row for the whole glide+dash (it was otherwise left frozen on
	# whatever pose _trigger_attack() set before calling this - the cast pose, held static since
	# non-idle rows don't loop - which read as the boss doing nothing while sliding across the
	# screen). _on_cast_finished() already resets back to idle once the attack's execute_sequence
	# returns, same as every other attack, so no extra reset is needed here.
	_set_anim_state(1, start_pos.x < _base_position.x)

	if _move_tween and _move_tween.is_valid():
		_move_tween.kill()

	_move_tween = create_tween()
	_move_tween.tween_property(self, "_base_position", start_pos, jump_duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	if hold_duration > 0.0:
		_move_tween.tween_interval(hold_duration)
	_move_tween.tween_callback(func():
		strafe_jump_landed.emit()
		_set_anim_state(1, end_pos.x < start_pos.x)
	)
	var dash: PropertyTweener = _move_tween.tween_property(self, "_base_position", end_pos, dash_duration)
	if dash_ease_out:
		dash.set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	else:
		dash.set_trans(Tween.TRANS_LINEAR)

func _perform_hop() -> void:
	if state != State.ACTIVE or is_dead:
		return
	
	state = State.HOPPING
	
	var bounds: Rect2 = boss_data.roam_bounds if boss_data else Rect2(140.0, 110.0, 320.0, 100.0)
	var min_x: float = bounds.position.x
	var max_x: float = bounds.end.x
	var min_y: float = bounds.position.y
	var max_y: float = bounds.end.y
	
	var min_dist: float = boss_data.hop_distance_min if boss_data else 90.0
	var max_dist: float = boss_data.hop_distance_max if boss_data else 160.0
	var hop_dist: float = _rng.randf_range(min_dist, max_dist)
	
	# Decide hop direction:
	# If too close to left bound, hop right.
	# If too close to right bound, hop left.
	# Otherwise, choose direction randomly.
	var dist_to_left: float = _base_position.x - min_x
	var dist_to_right: float = max_x - _base_position.x
	
	var move_right: bool = false
	if dist_to_left < min_dist + 10.0:
		move_right = true
	elif dist_to_right < min_dist + 10.0:
		move_right = false
	else:
		move_right = (_rng.randf() < 0.5)

	var offset_x: float = hop_dist if move_right else -hop_dist
	var target_x: float = clampf(_base_position.x + offset_x, min_x, max_x)

	# Slight vertical variation (+/- 18px)
	var offset_y: float = _rng.randf_range(-18.0, 18.0)
	var target_y: float = clampf(_base_position.y + offset_y, min_y, max_y)
	var target_pos := Vector2(target_x, target_y)
	
	# Directional animation banking
	if target_x < _base_position.x - 3.0:
		_set_anim_state(1, true) # Bank left
	elif target_x > _base_position.x + 3.0:
		_set_anim_state(1, false) # Bank right
	else:
		_set_anim_state(0, false)
	
	var hop_dur: float = boss_data.hop_duration if boss_data else 0.84
	_base_position = target_pos
	
	if _move_tween and _move_tween.is_valid():
		_move_tween.kill()
	
	_move_tween = create_tween()
	_move_tween.tween_property(self, "position", target_pos, hop_dur).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	_move_tween.tween_callback(_on_hop_completed)

func _on_hop_completed() -> void:
	if state != State.HOPPING or is_dead:
		return
	
	state = State.ACTIVE
	position = _base_position
	_set_anim_state(0, false) # Return to idle
	
	_phase = Phase.POST_HOP
	_phase_timer = boss_data.post_hop_delay if boss_data else 0.25

func take_damage(amount: float, source: String = "bullet") -> bool:
	if not is_vulnerable or is_dead or state == State.ENTERING or state == State.LEAVING or state == State.DISPELLED:
		return false
	
	current_health = maxf(0.0, current_health - amount)
	_flash_hit()
	var is_low: bool = (max_health > 0.0 and (current_health / max_health) <= 0.30)
	AudioService.play_damage_hit(is_low)
	hit.emit(amount, current_health)
	health_changed.emit(current_health, max_health)
	if life_ring and max_health > 0.0:
		life_ring.health_fraction = current_health / max_health
	
	if current_health <= 0.0:
		die(source)
	
	return true

func _flash_hit() -> void:
	if sprite:
		if _hit_tween and _hit_tween.is_valid():
			_hit_tween.kill()
		sprite.modulate = Color(1.0, 0.45, 0.45, 1.0)
		_hit_tween = create_tween()
		_hit_tween.tween_property(sprite, "modulate", Color.WHITE, 0.06)

func die(source: String = "bullet") -> void:
	if is_dead:
		return
	is_dead = true
	AudioService.play_boss_defeat()
	state = State.DEFEATED
	is_vulnerable = false
	monitorable = false
	monitoring = false
	
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	if _move_tween and _move_tween.is_valid():
		_move_tween.kill()
	
	_vanish_boss_effects()
	defeated.emit(position, source)
	
	# Defeat fade and burst
	var tw := create_tween()
	if sprite:
		tw.tween_property(sprite, "modulate:a", 0.0, 0.25)
	else:
		tw.tween_property(self, "modulate:a", 0.0, 0.25)
	tw.parallel().tween_property(self, "scale", Vector2(1.3, 1.3), 0.25)
	tw.tween_callback(queue_free)

func _vanish_boss_effects() -> void:
	if magic_circle:
		magic_circle.vanish()
	if life_ring:
		life_ring.vanish()

func _spawn_red_shockwave() -> void:
	if playfield and playfield.has_method("spawn_defeat_shockwave"):
		playfield.spawn_defeat_shockwave(position)
	elif DEFEAT_SHOCKWAVE_SCENE:
		var wave = DEFEAT_SHOCKWAVE_SCENE.instantiate()
		wave.setup(position)
		if get_parent():
			get_parent().add_child(wave)

## Called when the duration elapses naturally without being killed
func leave_screen() -> void:
	if (state != State.ACTIVE and state != State.HOPPING) or is_dead:
		return
	
	state = State.LEAVING
	is_vulnerable = false
	monitorable = false
	monitoring = false
	
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	if _move_tween and _move_tween.is_valid():
		_move_tween.kill()
	
	# Explode into a red shockwave upon timeout (no flying offscreen)
	_spawn_red_shockwave()
	AudioService.play_boss_defeat()
	_vanish_boss_effects()
	left_screen.emit()
	
	var tw := create_tween()
	if sprite:
		tw.tween_property(sprite, "modulate:a", 0.0, 0.18).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		tw.parallel().tween_property(sprite, "scale", sprite.scale * 1.35, 0.18).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	else:
		tw.tween_property(self, "modulate:a", 0.0, 0.18)
	tw.tween_callback(queue_free)

## Cancels out / dispels the boss when opponent casts their own Level 4 Spellcard
func dispel() -> void:
	if is_dead or state == State.DISPELLED:
		return
	state = State.DISPELLED
	is_vulnerable = false
	monitorable = false
	monitoring = false
	
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	if _move_tween and _move_tween.is_valid():
		_move_tween.kill()
	
	AudioService.play_boss_defeat()
	_vanish_boss_effects()
	dispelled.emit()
	
	var tw := create_tween()
	tw.tween_property(self, "modulate:a", 0.0, 0.2)
	tw.parallel().tween_property(self, "scale", Vector2(0.2, 0.2), 0.2)
	tw.tween_callback(queue_free)
