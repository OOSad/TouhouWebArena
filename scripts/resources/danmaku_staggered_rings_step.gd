class_name DanmakuStaggeredRingsStep
extends DanmakuStep

## Reisen Udongein Inaba's Level 4 Boss Attack 2, informally "Rings of Red Bullets".
##
## Eight rings of red bullets leave the boss one after another and expand at a constant,
## identical speed. Nothing about a ring's flight sets it apart from its neighbours: the
## rings are separated purely by when they launched, which is what makes them read as one
## steady pulse rolling outward rather than as eight distinct attacks.
##
## The catch is that every ring is rotated by a random amount. Not offset by a pattern, not
## alternated, not fanned - rolled fresh for each ring, anywhere inside the gap between two
## neighbouring bullets. The gaps therefore never line up from one ring to the next, and
## they do not line up in any way the player can anticipate either. A clear radial corridor
## through the first ring tells you nothing about where the second one's will be.
##
## Rank changes one thing only: how fast the rings grow. The count (8), the density (36 per
## ring) and the launch interval all measured the same at Rank 1 and Rank 16. A Rank 16 ring
## covers the field in well under half the time, so the same gaps have to be read and taken
## far sooner.
##
## Measured off reisen_level4_rank1.mp4 (cast at ~20.5s) and reisen_level4_rank16.mp4 (cast
## at ~14.5s), converting the reference playfield's 432.5px width to our 600px.

@export_group("Bullet Type")
@export var bullet_data: DanmakuBulletData = null

@export_group("Ring Geometry")
## Ring centre, as an offset from the caller's cast origin. The rings come straight out of
## the boss in the original, so this stays at zero; every ring shares this one centre even
## if the boss drifts, which is what keeps them concentric.
@export var center_offset: Vector2 = Vector2.ZERO
## How many rings the volley fires. Eight at every rank.
@export var ring_count: int = 8
## Bullets in each ring. Fixed at 36 at every rank in the PoFV original, which puts a
## bullet every 10 degrees.
@export var bullets_per_ring: int = 36
## Added per rank (pl04.ecl sub3: 32 + rank)
@export var bullets_per_rank: int = 0
## Base orientation the random per-ring rotation is rolled around, in degrees.
@export var base_angle_deg: float = 0.0
## Width of the random rotation each ring is given, in degrees, as a multiple of the gap
## between neighbouring bullets. Every ring rolls its own value independently.
##
## 1.0 is the natural setting and the maximum that means anything: rotating a ring by a full
## gap lands every bullet exactly where its neighbour was, so the ring is unchanged, and
## every possible rotation is already reachable within one gap. Lower values pull the rings
## back toward a common alignment - 0.0 stacks them into clean radial corridors, which is
## the thing this attack exists to prevent.
##
## Deliberately not a pattern. Alternating or fanning the offsets covers the gap just as
## evenly, but the eye picks the structure straight back out (four rings leaning one way and
## four the other reads as organised, not messy), and once a player can predict the next
## ring's rotation the corridors are effectively open again.
@export_range(0.0, 1.0) var ring_rotation_spread: float = 1.0

@export_group("Timing & Rank Scaling")
## Seconds between one ring launching and the next. Rank does not touch this - measured at
## ~0.28s in both reference clips - so what a higher rank buys is reach, not rhythm.
@export var ring_interval: float = 0.28
## How fast a ring expands at Rank 1, in px/s
@export var speed_min: float = 130.0
## How fast a ring expands at Rank 16, in px/s. Rank 16 rings grow nearly two and a half
## times as fast, which is the entire difficulty curve of this attack.
@export var speed_max: float = 310.0

@export_group("Audio")
## Played once per ring rather than once per volley, so the eight launches are audible as
## the steady pulse they are.
@export var sfx: String = "se_tan00"

@export_group("Visuals")
## Red flash marking each ring's launch, one per ring rather than one per bullet: all 36 of a
## ring's bullets leave the same point in the same frame, so a flash each would be 36 copies
## stacked on one pixel. 0 disables it.
@export var spawn_flash_scale: float = 2.6

const DEFAULT_BULLET: DanmakuBulletData = preload("res://resources/bullets/reisen_wave_bullet.tres")
const SPAWN_FLASH_SCENE: PackedScene = preload("res://scenes/effects/spawn_flash.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	# Claimed before the first await: the rotations decide where the safe gaps are, so they
	# are part of the attack's identity and have to roll identically on both netplay clients.
	# Held in a local because this resource is shared between the two playfields.
	var rng: RandomNumberGenerator = take_sync_rng()

	if playfield == null or not is_instance_valid(playfield):
		return
	if not playfield.has_method("spawn_danmaku_bullet"):
		return

	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_BULLET
	var count: int = maxi(bullets_per_ring + bullets_per_rank * clampi(rank, 1, 16), 3)
	var rings: int = maxi(ring_count, 1)
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var speed: float = lerpf(speed_min, speed_max, t_rank)

	var center: Vector2 = origin + center_offset
	var step_angle: float = TAU / float(count)
	var base_angle: float = deg_to_rad(base_angle_deg)
	# One gap is the whole space of distinct rotations, so the spread is expressed against it.
	var spread: float = step_angle * clampf(ring_rotation_spread, 0.0, 1.0)

	# Rolled up front, in one unbroken run, rather than a ring at a time between the awaits.
	# Two volleys overlap whenever both playfields have a boss up, and interleaving their
	# draws would desync the sequence even though each is seeded correctly.
	var ring_angles := PackedFloat32Array()
	for ring in range(rings):
		var offset: float = rng.randf_range(0.0, spread) if rng else randf_range(0.0, spread)
		ring_angles.append(base_angle + offset)

	for ring in range(rings):
		if not is_instance_valid(playfield):
			return

		if not sfx.is_empty():
			AudioService.play_sfx(sfx)

		if spawn_flash_scale > 0.0:
			var layer: Node2D = playfield.get("effects_layer")
			if layer == null:
				layer = playfield.get_node_or_null("%Effects")
			if layer == null:
				layer = playfield
			# setup() before add_child(): the flash builds its tween in _ready() off these.
			var flash: Node2D = SPAWN_FLASH_SCENE.instantiate()
			flash.call("setup", center, spawn_flash_scale)
			layer.add_child(flash)

		for i in range(count):
			playfield.spawn_danmaku_bullet(
				data,
				center,
				DanmakuBullet.MotionMode.LINEAR,
				Vector2.from_angle(ring_angles[ring] + step_angle * float(i)),
				speed
			)

		if ring < rings - 1 and ring_interval > 0.0:
			if not playfield.is_inside_tree():
				return
			await playfield.get_tree().create_timer(ring_interval).timeout
