class_name Fairy
extends Area2D

# Fairy enemy entity that travels along a Curve2D path
enum FairyType {
	SMALL,
	GREAT
}

enum FairyVariant {
	BLUE,
	RED,
	GREEN
}

signal hit(damage: float, remaining_health: float)
signal defeated(type: FairyType, death_pos: Vector2, source: String)

# Per tier (blue, red, green, great), from enemy.ecl: enemy_create hp and enemy_set_hitbox size.
const ECL_HP: Array[float] = [20.0, 25.0, 30.0, 35.0]
const ECL_HITBOX: Array[float] = [24.0, 28.0, 32.0, 36.0]
const HEALTH_PER_ECL_HP: float = 3.0 / 35.0 # keeps PoFV's ratios with the great fairy at 3 health
const ECL_SCALE: float = 600.0 / 288.0

const PLAYFIELD_MIN_X: float = 20.0
const PLAYFIELD_MAX_X: float = 580.0
const PLAYFIELD_MIN_Y: float = 20.0
const PLAYFIELD_MAX_Y: float = 940.0
const ENTRY_INVULNERABILITY_TIME: float = 0.07 # ~23px of flight inside playfield (~4 frames)

@export var fairy_type: FairyType = FairyType.SMALL
@export var fairy_variant: FairyVariant = FairyVariant.BLUE
@export var speed: float = 330.0
@export var max_health: float = 2.0

var current_health: float = 2.0
var curve: Curve2D = null
var distance: float = 0.0
var is_active: bool = false
var is_dead: bool = false
var is_vulnerable: bool = false
var _time_on_screen: float = 0.0
var _hit_tween: Tween = null
var _dying_from_heavy_shockwave: bool = false

# Animation parameters
var _frame_timer: float = 0.0
var _anim_frame: int = 0
var _anim_row: int = 0 # 0: Forward flight/idle, 1: Forward tilt, 2: Banking turn
const FRAME_DURATION: float = 0.10
const FRAMES_PER_ROW: int = 4

@onready var sprite: Sprite2D = %Sprite2D if has_node("%Sprite2D") else get_node_or_null("Sprite2D")
@onready var collision_shape: CollisionShape2D = %CollisionShape2D if has_node("%CollisionShape2D") else get_node_or_null("CollisionShape2D")

const SMALL_FAIRY_BLUE_TEX: Texture2D = preload("res://resources/dat_textures/fairy_blue.tres")
const SMALL_FAIRY_RED_TEX: Texture2D = preload("res://resources/dat_textures/fairy_red.tres")
const SMALL_FAIRY_GREEN_TEX: Texture2D = preload("res://resources/dat_textures/fairy_green.tres")
const GREAT_FAIRY_TEX: Texture2D = preload("res://resources/dat_textures/fairy_great.tres")

func _ensure_nodes() -> void:
	if sprite == null:
		sprite = %Sprite2D if has_node("%Sprite2D") else get_node_or_null("Sprite2D")
	if collision_shape == null:
		collision_shape = %CollisionShape2D if has_node("%CollisionShape2D") else get_node_or_null("CollisionShape2D")

func _ready() -> void:
	_ensure_nodes()
	visible = false
	monitorable = false
	is_vulnerable = false
	_apply_type()

func setup(p_type: FairyType, p_curve: Curve2D, p_start_distance: float, p_speed: float = 330.0, p_variant: FairyVariant = FairyVariant.BLUE) -> void:
	fairy_type = p_type
	fairy_variant = p_variant
	curve = p_curve
	distance = p_start_distance
	speed = p_speed
	
	# Snapped so float error cannot leave a sliver of health that costs an extra hit
	max_health = snappedf(ECL_HP[_tier()] * HEALTH_PER_ECL_HP, 0.001)
	current_health = max_health
	
	_ensure_nodes()
	_apply_type()

func _apply_type() -> void:
	_ensure_nodes()
	if sprite == null:
		return
	
	sprite.hframes = FRAMES_PER_ROW
	sprite.vframes = 3
	
	var circle := CircleShape2D.new()
	if fairy_type == FairyType.GREAT:
		sprite.texture = GREAT_FAIRY_TEX
		sprite.scale = Vector2(1.8, 1.8)
	else:
		match fairy_variant:
			FairyVariant.RED:
				sprite.texture = SMALL_FAIRY_RED_TEX
				sprite.scale = Vector2(2.25, 2.25)
			FairyVariant.GREEN:
				sprite.texture = SMALL_FAIRY_GREEN_TEX
				sprite.scale = Vector2(1.5, 1.5)
			_:
				sprite.texture = SMALL_FAIRY_BLUE_TEX
				sprite.scale = Vector2(2.25, 2.25)

	circle.radius = ECL_HITBOX[_tier()] * 0.5 * ECL_SCALE
	if collision_shape:
		collision_shape.shape = circle

## 0-3 = blue, red, green, great, matching FairyPaths.Tier and the ECL tables above.
func _tier() -> int:
	return 3 if fairy_type == FairyType.GREAT else int(fairy_variant)

func _physics_process(delta: float) -> void:
	# Advance animation frame timer
	_frame_timer += delta
	if _frame_timer >= FRAME_DURATION:
		_frame_timer -= FRAME_DURATION
		_anim_frame = (_anim_frame + 1) % FRAMES_PER_ROW

	distance += speed * delta
	
	# While distance < 0, the fairy is queued up off-screen
	if distance < 0.0:
		visible = false
		monitorable = false
		is_vulnerable = false
		return
	
	if not is_active:
		is_active = true
	
	visible = true
	
	if curve == null:
		return
	
	var total_len := curve.get_baked_length()
	if distance <= total_len:
		var current_pos := curve.sample_baked(distance)
		position = current_pos
		
		# Sample slightly ahead along the curve to determine banking direction
		var lookahead: float = minf(distance + 8.0, total_len)
		var forward_pos := curve.sample_baked(lookahead)
		var tangent := forward_pos - current_pos
		if tangent.length_squared() > 0.001:
			var dir := tangent.normalized()
			if absf(dir.x) > 0.35:
				_anim_row = 2 # Banking turn
				if sprite:
					sprite.flip_h = (dir.x < 0.0)
			else:
				_anim_row = 0 # Forward flapping flight
				if sprite:
					sprite.flip_h = false
		else:
			_anim_row = 0
			if sprite:
				sprite.flip_h = false
	else:
		# Reached the end of the path - despawn gracefully
		if _dying_from_heavy_shockwave:
			die("heavy_shockwave")
		else:
			queue_free()
		return
	
	if sprite:
		sprite.frame = _anim_row * FRAMES_PER_ROW + _anim_frame
	
	# Playfield bounds & entry invulnerability check
	if _is_inside_playfield() and not _dying_from_heavy_shockwave:
		_time_on_screen += delta
		if _time_on_screen >= ENTRY_INVULNERABILITY_TIME:
			if not is_vulnerable:
				is_vulnerable = true
				monitorable = true
	elif not _dying_from_heavy_shockwave:
		# Outside playfield bounds
		is_vulnerable = false
		monitorable = false

func _is_inside_playfield() -> bool:
	return (
		position.x >= PLAYFIELD_MIN_X and
		position.x <= PLAYFIELD_MAX_X and
		position.y >= PLAYFIELD_MIN_Y and
		position.y <= PLAYFIELD_MAX_Y
	)

func take_damage(amount: float, source: String = "bullet") -> bool:
	if not is_active or is_dead or _dying_from_heavy_shockwave:
		return false
	if not is_vulnerable and source != "heavy_shockwave":
		return false
	
	current_health = maxf(0.0, current_health - amount)
	_flash_hit()
	AudioService.play_damage_hit(false)
	hit.emit(amount, current_health)
	
	if current_health <= 0.0:
		if source == "heavy_shockwave":
			_start_delayed_heavy_shockwave_death()
		else:
			die(source)
	
	return true

func _start_delayed_heavy_shockwave_death() -> void:
	if _dying_from_heavy_shockwave or is_dead:
		return
	_dying_from_heavy_shockwave = true
	is_vulnerable = false
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	
	# Delay death slightly (~0.12s) while fairy continues travelling,
	# allowing the expanding heavy shockwave to clear surrounding bullets first.
	if is_inside_tree():
		var tree := get_tree()
		if tree:
			var timer := tree.create_timer(0.12)
			timer.timeout.connect(func() -> void:
				if is_instance_valid(self) and not is_queued_for_deletion():
					_dying_from_heavy_shockwave = false
					die("heavy_shockwave")
			)
			return
	die("heavy_shockwave")

func _flash_hit() -> void:
	if sprite:
		if _hit_tween and _hit_tween.is_valid():
			_hit_tween.kill()
		sprite.modulate = Color(1.0, 0.4, 0.4, 1.0)
		_hit_tween = create_tween()
		_hit_tween.tween_property(sprite, "modulate", Color.WHITE, 0.06)

func die(source: String = "bullet") -> void:
	if is_dead:
		return
	is_dead = true
	AudioService.play_enemy_death()
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	if collision_shape:
		collision_shape.set_deferred("disabled", true)
	defeated.emit(fairy_type, position, source)
	queue_free()