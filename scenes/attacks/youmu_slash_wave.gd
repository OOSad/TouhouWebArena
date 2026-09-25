class_name YoumuSlashWave
extends Area2D

## Youmu Konpaku's Level 1 Charge Attack: Hesitation-Clearing Slash (断迷剣 - Danmeiken)
## A rapid, massive cutting crescent sword arc that sweeps forward from Youmu.
## - Directional heavy shockwave: Erases ALL bullets outright without generating counter-motes.
## - Dispels physical EX attacks like Reimu's Yin-Yang Orbs (Marisa's lasers are immune).
## - Deals 15.0 cleave damage, about 15 normal shots as measured against Lily White in PoFV footage,
##   and one-shots fairies/spirits (whose death shockwaves still pop nearby pellets as usual).

const DURATION: float = 0.28
const FORWARD_SPEED: float = 620.0
const DAMAGE: float = 15.0

var player: Node2D = null
var playfield: Node2D = null

var _elapsed: float = 0.0
var _hit_enemies: Dictionary = {}
var _velocity: Vector2 = Vector2(0.0, -FORWARD_SPEED)

@onready var sprite: Sprite2D = %Sprite2D
@onready var collision_shape: CollisionShape2D = %CollisionShape2D

func _ready() -> void:
	area_entered.connect(_on_area_entered)

func setup(p_player: Node2D, p_playfield: Node2D) -> void:
	player = p_player
	playfield = p_playfield
	
	AudioService.play_slash()
	
	if is_instance_valid(player):
		# Spawn just above Youmu's position
		global_position = player.global_position + Vector2(0.0, -32.0)

func _process(delta: float) -> void:
	_elapsed += delta
	global_position += _velocity * delta
	
	# Ensure any overlapping entities in rapid motion are processed
	for overlap in get_overlapping_areas():
		_handle_area(overlap)
	
	# Update 4-frame animation across DURATION
	var frame_idx: int = clampi(int((_elapsed / DURATION) * 4.0), 0, 3)
	if sprite:
		sprite.frame = frame_idx
		if frame_idx == 3:
			# Smooth fade out in the final frame
			var t: float = (_elapsed - (DURATION * 0.75)) / (DURATION * 0.25)
			sprite.modulate.a = clampf(1.0 - t, 0.0, 1.0)
	
	if _elapsed >= DURATION:
		queue_free()

func _on_area_entered(area: Area2D) -> void:
	_handle_area(area)

func _handle_area(area: Area2D) -> void:
	if not is_instance_valid(area) or area.is_queued_for_deletion():
		return
	
	# 1. EX Attacks (Reimu's Yin-Yang Orbs cleaved; Marisa's Earth Light Rays are immune)
	if area is YinYangOrb:
		area.queue_free()
		return
	
	if is_instance_valid(area.get_parent()) and area.get_parent() is EarthLightRay:
		# Laser pillars cannot be cut
		return
	
	# 2. Enemy Pellets (regular pellets, big pellets, ring bullets) - erased outright
	if area is EnemyPellet:
		if not area.is_canceled:
			area.is_canceled = true
			area.despawn()
		return
	
	# 3. Danmaku Bullets (pellets, ovals, talismans, stars) - erased outright
	if area is DanmakuBullet:
		if not area.is_canceled:
			area.is_canceled = true
			area.despawn()
		return
	
	# 4. Enemies & Bosses (Fairies, Spirits, Lily White, BossCharacter)
	if ("is_dead" in area) and area.is_dead:
		return
	
	if not _hit_enemies.has(area):
		_hit_enemies[area] = true
		if area.has_method("take_damage"):
			area.take_damage(DAMAGE, "charge_attack")
