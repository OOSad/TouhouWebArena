class_name LilyWhite
extends Area2D

enum Phase {
	IDLE,
	DESCENDING,
	RESTING,
	RETREATING,
	DEPARTED
}

signal descended
signal retreat_started
signal departed
signal defeated(death_pos: Vector2)

@export var descent_duration: float = 2.0
@export var rest_duration: float = 0.8
@export var retreat_duration: float = 4.0

@export var start_position: Vector2 = Vector2(300.0, -100.0)
@export var rest_position: Vector2 = Vector2(300.0, 500.0)
@export var exit_position: Vector2 = Vector2(300.0, -100.0)

@export_group("Danmaku Barrage")
## Density note: wave count, step interval and rotation step are one setting in three
## parts. Duration is count x interval (3.36s) and the swept arc is count x rotation
## (192 degrees), so those two stay put however they are split - but TOTAL BULLETS is
## count x 12, so raising the count raises density even when duration and arc do not
## move. The 1.5x cadence pass (48 / 0.070 / 4.0) kept both and still pushed output from
## 384 to 576 bullets as a side effect. Change the count only when density is the thing
## being changed.
## Claw geometry note: angle_jitter_deg must stay well under half of claw_spread_deg,
## otherwise neighbouring prongs land in each other's range and the three strands smear
## into one solid band. At the old 13/7 pairing the -13 prong covered [-20, -6] and the
## 0 prong covered [-7, +7], so they overlapped every wave and the barrage came out as a
## wall with no lanes through it - unlike PoFV, where the strands stay clearly separated.
@export var barrage_wave_count: int = 32
@export var barrage_step_interval: float = 0.105
@export var claw_spread_deg: float = 24.0
@export var rotation_step_deg: float = 6.0
@export var blue_start_angle_deg: float = -45.0
@export var red_start_angle_deg: float = -135.0
@export var bullet_speed: float = 220.0
@export var speed_jitter: float = 15.0
@export var angle_jitter_deg: float = 2.0
@export var twin_offset_px: float = 30.0
@export var blue_color: Color = Color(0.4, 0.8, 1.0)
@export var red_color: Color = Color(1.0, 0.35, 0.45)

## 3 seconds of uninterrupted player fire (player fires 2 bullets every 0.075s = 26.67 DPS),
## matching PoFV footage where she dies to ~3.3s of normal fire.
## 80 HP / 26.67 DPS = 3.0s (40 volleys).
@export var max_health: float = 80.0

var current_health: float = 80.0
var current_phase: Phase = Phase.IDLE
var is_active: bool = false
var is_dead: bool = false
var playfield: Playfield = null

var _motion_tween: Tween = null
var _hit_flash_tween: Tween = null
var _barrage_tween: Tween = null

@onready var sprite: Sprite2D = %Sprite2D if has_node("%Sprite2D") else get_node_or_null("Sprite2D")
@onready var collision_shape: CollisionShape2D = %CollisionShape2D if has_node("%CollisionShape2D") else get_node_or_null("CollisionShape2D")

func _ensure_nodes() -> void:
	if sprite == null:
		sprite = %Sprite2D if has_node("%Sprite2D") else get_node_or_null("Sprite2D")
	if collision_shape == null:
		collision_shape = %CollisionShape2D if has_node("%CollisionShape2D") else get_node_or_null("CollisionShape2D")

func _ready() -> void:
	_ensure_nodes()
	current_health = max_health
	position = start_position
	# If spawned and not manually started, start immediately
	if not is_active:
		start_sequence()

func start_sequence() -> void:
	if is_active:
		return
	is_active = true
	current_phase = Phase.DESCENDING
	position = start_position
	
	AudioService.play_lily_warning()
	
	if _motion_tween and _motion_tween.is_valid():
		_motion_tween.kill()
	
	_motion_tween = create_tween()
	
	# 1. Descend from top in 2.0s with ease-out rest
	_motion_tween.tween_property(self, "position", rest_position, descent_duration)\
		.set_trans(Tween.TRANS_QUAD)\
		.set_ease(Tween.EASE_OUT)
	
	# 2. Reached rest point
	_motion_tween.tween_callback(func():
		current_phase = Phase.RESTING
		descended.emit()
	)
	
	# 3. Rest / hover pause
	if rest_duration > 0.0:
		_motion_tween.tween_interval(rest_duration)
	
	# 4. Start retreat
	_motion_tween.tween_callback(func():
		current_phase = Phase.RETREATING
		retreat_started.emit()
		_start_retreat_barrage()
	)
	
	# 5. Ascend back to top in 4.0s
	_motion_tween.tween_property(self, "position", exit_position, retreat_duration)\
		.set_trans(Tween.TRANS_QUAD)\
		.set_ease(Tween.EASE_IN)
	
	# 6. Exit screen and cleanup
	_motion_tween.tween_callback(func():
		current_phase = Phase.DEPARTED
		if _barrage_tween and _barrage_tween.is_valid():
			_barrage_tween.kill()
		departed.emit()
		queue_free()
	)

func _find_playfield() -> Playfield:
	var cur: Node = get_parent()
	while cur != null:
		if cur is Playfield:
			return cur as Playfield
		cur = cur.get_parent()
	return null

func _start_retreat_barrage() -> void:
	if _barrage_tween and _barrage_tween.is_valid():
		_barrage_tween.kill()
	
	_barrage_tween = create_tween()
	for i in range(barrage_wave_count):
		_barrage_tween.tween_callback(func():
			_fire_barrage_wave(i)
		)
		if i < barrage_wave_count - 1:
			_barrage_tween.tween_interval(barrage_step_interval)

func _fire_barrage_wave(step_idx: int) -> void:
	if is_dead:
		return
	if playfield == null:
		playfield = _find_playfield()
	if playfield == null:
		return
	
	AudioService.play_danmaku_shot()
	
	var spawn_pos: Vector2 = position
	
	# Right vector (blue): starts up-right (-45 deg), rotates CLOCKWISE (+deg)
	var blue_deg: float = blue_start_angle_deg + (step_idx * rotation_step_deg)
	# Left vector (red): starts up-left (-135 deg), rotates COUNTER-CLOCKWISE (-deg)
	var red_deg: float = red_start_angle_deg - (step_idx * rotation_step_deg)
	
	_fire_claw_combo(spawn_pos, blue_deg, blue_color)
	_fire_claw_combo(spawn_pos, red_deg, red_color)

func _fire_claw_combo(spawn_pos: Vector2, center_deg: float, color: Color) -> void:
	var claw_angles: Array[float] = [
		center_deg - claw_spread_deg,
		center_deg,
		center_deg + claw_spread_deg
	]
	
	for angle_deg in claw_angles:
		var final_angle: float = angle_deg
		if angle_jitter_deg > 0.0:
			final_angle += randf_range(-angle_jitter_deg, angle_jitter_deg)
		
		var final_speed: float = bullet_speed
		if speed_jitter > 0.0:
			final_speed += randf_range(-speed_jitter, speed_jitter)
		
		var rad: float = deg_to_rad(final_angle)
		var dir := Vector2(cos(rad), sin(rad)).normalized()
		
		# Twin pair: same speed and same heading.
		# Solid pellet spawns ahead of the circled ring pellet by twin_offset_px.
		var lead_pos: Vector2 = spawn_pos + dir * twin_offset_px
		var ring_pos: Vector2 = spawn_pos
		
		# Lead bullet: solid pellet in front
		playfield.spawn_directional_pellet(lead_pos, final_speed, dir, color, false)
		
		# Circled ring bullet right behind it
		playfield.spawn_directional_pellet(ring_pos, final_speed, dir, color, true)

func take_damage(amount: float, _source: String = "") -> bool:
	if is_dead or not is_active:
		return false
	
	current_health -= amount
	_play_hit_flash()
	var is_low: bool = (max_health > 0.0 and (current_health / max_health) <= 0.30)
	AudioService.play_damage_hit(is_low)
	
	if current_health <= 0.0:
		current_health = 0.0
		is_dead = true
		AudioService.play_lily_death()
		var death_pos := position
		defeated.emit(death_pos)
		if _motion_tween and _motion_tween.is_valid():
			_motion_tween.kill()
		if _hit_flash_tween and _hit_flash_tween.is_valid():
			_hit_flash_tween.kill()
		if _barrage_tween and _barrage_tween.is_valid():
			_barrage_tween.kill()
		queue_free()
	
	return true

func _play_hit_flash() -> void:
	if sprite == null:
		return
	if _hit_flash_tween and _hit_flash_tween.is_valid():
		_hit_flash_tween.kill()
	
	sprite.modulate = Color(2.5, 2.5, 2.5, 1.0)
	_hit_flash_tween = create_tween()
	_hit_flash_tween.tween_property(sprite, "modulate", Color.WHITE, 0.08)

func stop_and_free() -> void:
	if _motion_tween and _motion_tween.is_valid():
		_motion_tween.kill()
	if _hit_flash_tween and _hit_flash_tween.is_valid():
		_hit_flash_tween.kill()
	if _barrage_tween and _barrage_tween.is_valid():
		_barrage_tween.kill()
	queue_free()
