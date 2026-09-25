class_name DanmakuFreezeRingStep
extends DanmakuStep

## Cirno's Level 2 & 3 Spellcards: Freeze Sign "Perfect Freeze"
## A ring of bullets emerges from a point near the top of the opponent's playfield and expands
## outward. Once the leading ring reaches a critical size, EVERY bullet currently on the field
## -- including bullets that were not part of this spellcard -- is reassigned a random new
## direction, recolored white, and slowly accelerates outward from rest.
## At Lv2, a single ring alternates icicles and pellets, joined by a second pure-icicle ring at
## higher rank. At Lv3 (second_ring_min_rank = 1), both rings are always present from Rank 1 and
## every bullet is a pure icicle. Since both rings are caught by the same freeze sweep, the
## slower (second) ring is still mid-expansion (smaller) when it happens, producing a natural
## separation between the two with no extra bookkeeping.

@export_group("Bullet Types")
@export var icicle_data: DanmakuBulletData = null
@export var pellet_data: DanmakuBulletData = null

@export_group("Ring Geometry")
## Overrides the default Lv2/3 cast Y position (spawns much higher, near the top border) so
## the ring forms lower on the playfield and actually threatens the player's lane. Set to a
## negative value to fall back to the caller-supplied origin.
@export var spawn_y_override: float = 260.0
## Bullets per ring at Rank 1 (Lv2 default: 80 = constant across all ranks)
@export var bullets_per_ring_min: int = 80
## Bullets per ring at Rank 16 (Lv2 default: 80 = constant across all ranks)
@export var bullets_per_ring_max: int = 80
## Radius, in pixels, at which the leading ring "bursts" and the freeze effect triggers
@export var freeze_trigger_radius: float = 240.0
## Radial expansion speed of the primary (faster) ring, in px/s
@export var radial_speed: float = 250.0
## The second ring expands at this fraction of radial_speed, causing it to lag behind
@export var slow_ring_speed_factor: float = 0.6
## Minimum rank at which the second pure-icicle ring joins (Rank 16 example = 2 rings)
@export var second_ring_min_rank: int = 9
## pl05.ecl sub0 / sub1: ring speed 187.5 + 12.5 x rank px/s, the slow ring at the midpoint
## between that and 125 (ZUN's 2-layer circle), and the freeze firing zun_freeze_after seconds
## after the ring does (60 frames) rather than at a fixed radius
@export var zun_speeds: bool = false
@export var zun_freeze_after: float = 1.0

@export_group("Freeze / Scatter")
## Acceleration applied to every field bullet once frozen (px/s^2)
@export var scatter_acceleration: float = 60.0
## Terminal speed every field bullet accelerates up to once frozen (px/s)
@export var scatter_max_speed: float = 160.0
## Duration (seconds) bullets hang motionless in place before scattering
@export var freeze_duration: float = 1.0

@export_group("Visuals")
## Duration (seconds) of the 5-pointed flower pre-cast visual before the bullet ring bursts out
@export var pre_cast_delay: float = 0.65
@export var flower_color: Color = Color(0.3, 0.85, 1.0, 1.0)

@export_group("Audio")
@export var sfx: String = "se_tan00"

const DEFAULT_ICICLE: DanmakuBulletData = preload("res://resources/bullets/blue_icicle.tres")
const DEFAULT_PELLET: DanmakuBulletData = preload("res://resources/bullets/blue_pellet_small.tres")
const FLOWER_RING_SCENE: PackedScene = preload("res://scenes/effects/flower_ring_effect.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	var wave_angle: float = take_wave_angle()
	if playfield == null or not is_instance_valid(playfield):
		return

	var icicle: DanmakuBulletData = icicle_data if icicle_data else DEFAULT_ICICLE
	var pellet: DanmakuBulletData = pellet_data if pellet_data else DEFAULT_PELLET

	var ring_origin: Vector2 = origin
	if spawn_y_override >= 0.0:
		ring_origin.y = spawn_y_override

	var effects_layer: Node2D = playfield.get("effects_layer")
	if effects_layer == null:
		effects_layer = playfield.get_node_or_null("%Effects")
	if effects_layer == null:
		effects_layer = playfield

	if FLOWER_RING_SCENE and effects_layer:
		var flower: Node2D = FLOWER_RING_SCENE.instantiate()
		if flower.has_method("setup"):
			flower.call("setup", ring_origin, pre_cast_delay, flower_color)
		else:
			flower.position = ring_origin
		effects_layer.add_child(flower)

	if pre_cast_delay > 0.0 and is_instance_valid(playfield) and playfield.is_inside_tree():
		await playfield.get_tree().create_timer(pre_cast_delay).timeout

	if not is_instance_valid(playfield):
		return

	if not sfx.is_empty():
		AudioService.play_sfx(sfx)

	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var bullets_per_ring: int = int(round(lerpf(float(bullets_per_ring_min), float(bullets_per_ring_max), t_rank)))

	var use_two_rings: bool = rank >= second_ring_min_rank
	var fast_speed: float = radial_speed
	var slow_speed: float = radial_speed * slow_ring_speed_factor
	var trigger_radius: float = freeze_trigger_radius
	if zun_speeds:
		fast_speed = 187.5 + 12.5 * float(clampi(rank, 1, 16))
		slow_speed = (fast_speed + 125.0) * 0.5
		trigger_radius = fast_speed * zun_freeze_after
	var step_angle: float = TAU / float(bullets_per_ring)
	var leader: DanmakuBullet = null

	for i in range(bullets_per_ring):
		if not is_instance_valid(playfield):
			return
		var angle: float = wave_angle + float(i) * step_angle

		# Primary ring: pure icicles once the second ring joins, else alternating icicle/pellet
		var primary_data: DanmakuBulletData = icicle if (use_two_rings or i % 2 == 0) else pellet
		var b: DanmakuBullet = playfield.call("spawn_danmaku_bullet_expanding_orbit", primary_data, ring_origin, 0.0, angle, fast_speed, 0.0)
		if leader == null and b != null:
			leader = b

		if use_two_rings:
			playfield.call("spawn_danmaku_bullet_expanding_orbit", icicle, ring_origin, 0.0, angle, slow_speed, 0.0)

	if leader != null:
		leader.freeze_trigger_radius = trigger_radius
		leader.freeze_accel = scatter_acceleration
		leader.freeze_max_speed = scatter_max_speed
		leader.freeze_duration = freeze_duration
		leader.owner_playfield = playfield
