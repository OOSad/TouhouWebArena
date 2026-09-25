class_name DanmakuDispatcher
extends Node

## Handles bullet pooling, hazard registration, and all danmaku projectile spawning for a Playfield.

const ENEMY_PELLET_SCENE: PackedScene = preload("res://scenes/bullets/enemy_pellet.tscn")
const DANMAKU_BULLET_SCENE: PackedScene = preload("res://scenes/bullets/danmaku_bullet.tscn")

var playfield: Node2D = null
var bullets_layer: Node2D = null
var pellet_pool: NodePool = null
var danmaku_bullet_pool: NodePool = null
var _custom_hazards: Array[Node] = []

@export var pellet_spawning_enabled: bool = true
@export var max_active_bullets: int = 350

func setup(p_playfield: Node2D, p_bullets_layer: Node2D) -> void:
	playfield = p_playfield
	bullets_layer = p_bullets_layer
	
	if bullets_layer:
		bullets_layer.child_entered_tree.connect(func(node: Node):
			if node.get("pool") == null:
				register_custom_hazard(node)
		)
		bullets_layer.child_exiting_tree.connect(func(node: Node):
			unregister_custom_hazard(node)
		)
		pellet_pool = NodePool.new(ENEMY_PELLET_SCENE, bullets_layer, 500)
		danmaku_bullet_pool = NodePool.new(DANMAKU_BULLET_SCENE, bullets_layer, 1200)

func _get_player() -> Player:
	if playfield and "player" in playfield:
		return playfield.player as Player
	return null

func register_custom_hazard(node: Node) -> void:
	if is_instance_valid(node) and not _custom_hazards.has(node):
		_custom_hazards.append(node)

func unregister_custom_hazard(node: Node) -> void:
	_custom_hazards.erase(node)

func get_active_bullet_count() -> int:
	var count: int = 0
	if pellet_pool:
		count += pellet_pool.get_active_count()
	if danmaku_bullet_pool:
		count += danmaku_bullet_pool.get_active_count()
	return count

## Returns an array of only active bullets (pellets + danmaku + non-pooled hazards), avoiding 1,700-node pool scans.
func get_active_bullets() -> Array:
	var result: Array = []
	if pellet_pool:
		result.append_array(pellet_pool.get_active_nodes())
	if danmaku_bullet_pool:
		result.append_array(danmaku_bullet_pool.get_active_nodes())
	if not _custom_hazards.is_empty():
		for h in _custom_hazards:
			if is_instance_valid(h):
				result.append(h)
	return result

func clear_all_bullets() -> void:
	if pellet_pool:
		pellet_pool.release_all_active()
	if danmaku_bullet_pool:
		danmaku_bullet_pool.release_all_active()
	if bullets_layer:
		for child in bullets_layer.get_children():
			if child is EnemyPellet or child is DanmakuBullet:
				continue
			child.queue_free()
	for h in _custom_hazards.duplicate():
		if is_instance_valid(h):
			h.queue_free()
	_custom_hazards.clear()

func spawn_pellet(local_pos: Vector2, speed: float = -1.0, is_big: bool = false, bounce_count: int = 0, p_color: Color = Color.WHITE) -> void:
	if not pellet_spawning_enabled:
		return
	if max_active_bullets > 0 and get_active_bullet_count() >= max_active_bullets:
		return
	if pellet_pool == null and bullets_layer != null:
		pellet_pool = NodePool.new(ENEMY_PELLET_SCENE, bullets_layer, 500)
	
	if pellet_pool:
		var pellet: EnemyPellet = pellet_pool.acquire() as EnemyPellet
		if pellet:
			pellet.setup(local_pos, speed, Vector2.ZERO, is_big, false, p_color, bounce_count)

func spawn_ring_pellet(local_pos: Vector2, speed: float, direction: Vector2, color: Color) -> void:
	if not pellet_spawning_enabled:
		return
	if max_active_bullets > 0 and get_active_bullet_count() >= max_active_bullets:
		return
	if pellet_pool == null and bullets_layer != null:
		pellet_pool = NodePool.new(ENEMY_PELLET_SCENE, bullets_layer, 500)
	
	if pellet_pool:
		var pellet: EnemyPellet = pellet_pool.acquire() as EnemyPellet
		if pellet:
			pellet.setup(local_pos, speed, direction, false, true, color)

func spawn_directional_pellet(local_pos: Vector2, speed: float, direction: Vector2, color: Color, is_ring_pellet: bool = false) -> void:
	if not pellet_spawning_enabled:
		return
	if max_active_bullets > 0 and get_active_bullet_count() >= max_active_bullets:
		return
	if pellet_pool == null and bullets_layer != null:
		pellet_pool = NodePool.new(ENEMY_PELLET_SCENE, bullets_layer, 500)
	
	if pellet_pool:
		var pellet: EnemyPellet = pellet_pool.acquire() as EnemyPellet
		if pellet:
			pellet.setup(local_pos, speed, direction, false, is_ring_pellet, color)

func spawn_danmaku_bullet(p_data: DanmakuBulletData, spawn_pos: Vector2, p_mode: DanmakuBullet.MotionMode, p_dir: Vector2, p_speed: float, p_accel: float = 0.0, p_max_speed: float = 0.0) -> DanmakuBullet:
	if danmaku_bullet_pool == null and bullets_layer != null:
		danmaku_bullet_pool = NodePool.new(DANMAKU_BULLET_SCENE, bullets_layer, 1200)
	if danmaku_bullet_pool:
		var b := danmaku_bullet_pool.acquire() as DanmakuBullet
		if b:
			b.setup(p_data, spawn_pos, p_mode, p_dir, p_speed, _get_player(), p_accel, p_max_speed)
			return b
	return null

func spawn_danmaku_bullet_decel_home(
	p_data: DanmakuBulletData,
	spawn_pos: Vector2,
	p_dir: Vector2,
	p_init_speed: float,
	p_decel: float,
	p_pause: float,
	p_launch_speed: float,
	p_stages: int = 1,
	p_stage1_duration: float = 0.5,
	p_homing_sfx: String = ""
) -> DanmakuBullet:
	if danmaku_bullet_pool == null and bullets_layer != null:
		danmaku_bullet_pool = NodePool.new(DANMAKU_BULLET_SCENE, bullets_layer, 1200)
	if danmaku_bullet_pool:
		var b := danmaku_bullet_pool.acquire() as DanmakuBullet
		if b:
			b.setup_decel_home(p_data, spawn_pos, p_dir, p_init_speed, p_decel, p_pause, p_launch_speed, _get_player(), p_stages, p_stage1_duration, p_homing_sfx)
			return b
	return null

func spawn_danmaku_bullet_expanding_orbit(
	p_data: DanmakuBulletData,
	p_center: Vector2,
	p_initial_radius: float,
	p_initial_angle: float,
	p_radial_speed: float,
	p_angular_speed: float,
	p_freeze_time: float = -1.0,
	p_breakout_time: float = -1.0,
	p_breakout_angle_offset: float = 0.0,
	p_breakout_accel: float = 0.0,
	p_breakout_max_speed: float = 0.0,
	p_breakout_initial_speed: float = 0.0
) -> DanmakuBullet:
	if danmaku_bullet_pool == null and bullets_layer != null:
		danmaku_bullet_pool = NodePool.new(DANMAKU_BULLET_SCENE, bullets_layer, 1200)
	if danmaku_bullet_pool:
		var b := danmaku_bullet_pool.acquire() as DanmakuBullet
		if b:
			b.setup_expanding_orbit(p_data, p_center, p_initial_radius, p_initial_angle, p_radial_speed, p_angular_speed, p_freeze_time, p_breakout_time, p_breakout_angle_offset, p_breakout_accel, p_breakout_max_speed, p_breakout_initial_speed)
			return b
	return null

func spawn_danmaku_bullet_lunar_wave(
	p_data: DanmakuBulletData,
	p_center: Vector2,
	p_initial_angle: float,
	p_expand_speed: float,
	p_expand_time: float,
	p_hold_time: float,
	p_contract_speed: float,
	p_contract_time: float,
	p_release_speed: float,
	p_release_accel: float,
	p_release_max_speed: float,
	p_spin_lean: float
) -> DanmakuBullet:
	if danmaku_bullet_pool == null and bullets_layer != null:
		danmaku_bullet_pool = NodePool.new(DANMAKU_BULLET_SCENE, bullets_layer, 1200)
	if danmaku_bullet_pool:
		var b := danmaku_bullet_pool.acquire() as DanmakuBullet
		if b:
			b.setup_lunar_wave(p_data, p_center, p_initial_angle, p_expand_speed, p_expand_time, p_hold_time, p_contract_speed, p_contract_time, p_release_speed, p_release_accel, p_release_max_speed, p_spin_lean)
			return b
	return null


func spawn_danmaku_bullet_curve_line(
	p_data: DanmakuBulletData,
	spawn_pos: Vector2,
	p_dir: Vector2,
	p_speed: float,
	p_curve_angular_speed: float,
	p_curve_duration: float,
	p_accel: float = 0.0,
	p_max_speed: float = 0.0,
	p_spin_speed: float = 0.0
) -> DanmakuBullet:
	if danmaku_bullet_pool == null and bullets_layer != null:
		danmaku_bullet_pool = NodePool.new(DANMAKU_BULLET_SCENE, bullets_layer, 1200)
	if danmaku_bullet_pool:
		var b := danmaku_bullet_pool.acquire() as DanmakuBullet
		if b:
			b.setup_curve_line(p_data, spawn_pos, p_dir, p_speed, p_curve_angular_speed, p_curve_duration, p_accel, p_max_speed, p_spin_speed)
			return b
	return null

func spawn_danmaku_bullet_ellipse_then_line(
	p_data: DanmakuBulletData,
	p_center: Vector2,
	p_radius: Vector2,
	p_start_angle: float,
	p_angular_speed: float,
	p_target_angle: float,
	p_fan_direction: Vector2,
	p_fan_speed: float,
	p_accel: float = 0.0,
	p_max_speed: float = 0.0
) -> DanmakuBullet:
	if danmaku_bullet_pool == null and bullets_layer != null:
		danmaku_bullet_pool = NodePool.new(DANMAKU_BULLET_SCENE, bullets_layer, 1200)
	if danmaku_bullet_pool:
		var b := danmaku_bullet_pool.acquire() as DanmakuBullet
		if b:
			b.setup_ellipse_then_line(p_data, p_center, p_radius, p_start_angle, p_angular_speed, p_target_angle, p_fan_direction, p_fan_speed, p_accel, p_max_speed)
			return b
	return null

