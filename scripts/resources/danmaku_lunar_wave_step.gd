class_name DanmakuLunarWaveStep
extends DanmakuStep

## Reisen Udongein Inaba's Level 2 Spellcard: Wave Sign "Lunar Wave"
## A ring swells outward from a point high in the opponent's playfield, braking until it
## hangs motionless at full size. It holds for a beat, sinks slightly back inward, then
## accelerates outward again - this time turning, so the bullets sweep around with the ring
## as it goes and trail off as a rotating spiral.
##
## The step fires a stack of these rings on the same point. Level 2 stacks four rings of 36
## in a single frame, so they read as one circle and only come apart on release, where each
## ring accelerates harder than the last. Level 3 instead fires many rings of only four
## bullets, each nudged round a fraction of a step (interleave_rings) and each a frame or so
## behind the last (ring_spawn_interval): 14 such rings read as one 56-bullet wheel while
## swelling, then trail out as four long spiral arms once released.
##
## The spellcard uses two of these steps back to back - red icicles first, round pellets a
## beat later but expanding faster, spinning the other way - so the two figures overtake and
## cross each other rather than playing out one after the other.
##
## Every bullet runs the same time-based schedule off its own spawn moment
## (DanmakuBullet.MotionMode.LUNAR_WAVE), so a ring needs no shared state and overlapping
## rings never interfere.

@export_group("Bullet Type")
@export var bullet_data: DanmakuBulletData = null

@export_group("Ring Geometry & Rank Scaling")
## Ring centre, as an offset from the caller's cast origin. The default drops the centre
## well below the usual cast point so the full-size ring sits inside the playfield.
@export var center_offset: Vector2 = Vector2(0.0, 165.0)
## Bullets in each ring. Fixed at every rank in the PoFV original.
@export var bullets_per_ring: int = 36
## Radius, in pixels, the rings brake to a standstill at (Rank 1)
@export var stall_radius_min: float = 140.0
## Radius, in pixels, the rings brake to a standstill at (Rank 16)
@export var stall_radius_max: float = 220.0

@export_group("Ring Stack")
## How many rings this step fires at Rank 1
@export var ring_count_rank1: int = 4
## How many rings this step fires at Rank 16
@export var ring_count_rank16: int = 4
## Seconds between one ring launching and the next. 0 puts the whole stack in one frame,
## which is what makes it read as a single circle. A small value (a frame or two) leaves
## each ring slightly behind the last, giving the stack visible radial thickness and, once
## released, turning it into trailing spiral arms.
@export var ring_spawn_interval: float = 0.0
## Rotates each ring a further 1/ring_count of the gap between neighbouring bullets. With
## only a few bullets per ring this is what knits the rings into what looks like one dense
## ring: 14 rings of 4 read as a 56-bullet wheel.
@export var interleave_rings: bool = false

@export_group("Phase Timing")
## Seconds the stack spends swelling out to stall_radius at Rank 1. Rank buys speed: a
## Rank 16 wave both reaches further and gets there sooner, so it swells visibly harder
## than a Rank 1 one rather than just ending up bigger.
@export var expand_time_rank1: float = 2.60
## Seconds the stack spends swelling out to stall_radius at Rank 16
@export var expand_time_rank16: float = 2.10
## Seconds a ring hangs motionless at full size
@export var hold_time: float = 0.15
## Seconds spent drifting back inward before a ring is released
@export var contract_time: float = 0.35
## Peak inward drift speed during the contraction, in px/s
@export var contract_speed: float = 70.0

@export_group("Release & Spin")
## Radial speed a ring leaves with once released, in px/s
@export var release_speed: float = 30.0
## Outward acceleration of the slowest (first) ring on release, in px/s^2 (Rank 1)
@export var release_accel_min: float = 60.0
## Outward acceleration of the slowest (first) ring on release, in px/s^2 (Rank 16)
@export var release_accel_max: float = 80.0
## How much harder the last ring leaves than the first, as a multiple of the first ring's
## acceleration. At 7.2 the stack spreads evenly from 1.0x up to 8.2x however many rings
## there are, which is the speed spread that unpacks them into chains streaming outward.
@export var release_accel_spread: float = 7.20
## Cap on release speed in px/s (0 = uncapped)
@export var release_max_speed: float = 0.0
## How far each bullet's travel leans away from straight outward once released, in degrees.
## The lean stays fixed for the whole flight, so the bullets keep a constant tilt and the
## slower rings curl further around - that is what turns each chain into a spiral arm.
## Positive leans clockwise on screen, negative counter-clockwise.
@export_range(-80.0, 80.0) var spin_lean_deg: float = 40.0
## Base orientation of the rings in degrees, so the two figures can be interleaved
@export var base_angle_deg: float = 0.0

@export_group("Pre-Cast Flower")
## Seconds the 5-point runic flower spends shrinking onto the ring's centre before any bullet
## exists. 0 skips it, and is the default so that the second figure of a card does not open
## with a second one: both cards gather once.
##
## This is awaited, so it delays the step, and that is why only the **first** figure may carry
## it. `SpellcardData.execute_sequence` awaits each step in turn, so setting it on both would
## push the two figures 1.80s apart instead of the 0.50s the crossing effect is built around.
@export var pre_cast_delay: float = 0.0
## Where it shrinks to is not configurable on purpose: it is the ring centre, the exact point
## the bullets then come out of. That is the whole read of the effect.
@export var flower_color: Color = Color(1.0, 0.45, 0.85, 1.0)
## Radius the flower opens at, in pixels, before closing onto the centre. Wider than Cirno's
## 150 because Lunar Wave's ring stalls wider too (140-220px against her fixed target).
@export var flower_start_radius: float = 210.0

@export_group("ZUN Lunar Wave (pl04.ecl sub0 / sub1)")
## Fires rank + zun_circles_base four-way circles in one frame, circle k turned k x
## zun_arc_deg / count from the wave's shared random angle, all at zun_speed (125 + 6.25 x rank
## px/s). Each brakes to a stop over zun_brake_time (ZUN's AimRel), turns zun_turn_deg from its
## own heading and relaunches at 2 x speed - k x (2 x speed / count - 6.25): the drop in
## relaunch speed from circle to circle is what draws the spiral arms.
@export var zun_mode: bool = false
@export var zun_circles_base: int = 14
@export var zun_arc_deg: float = 360.0
@export var zun_turn_deg: float = 120.0
@export var zun_brake_time: float = 2.0

@export_group("Audio")
## Played once as the figure is cast. One per step rather than one per ring: a stack launched
## in a single frame is one circle as far as the player is concerned, and se_tan00's 30ms
## throttle would collapse the repeats anyway.
@export var sfx: String = "se_tan00"
## Played once at the moment the stack stops being a ring and starts unpacking - the release,
## where the bullets accelerate outward and the fixed lean curls them into spiral arms. Same
## sound and the same non-blocking timer as `DanmakuInterweavingIciclesStep`'s breakout.
@export var release_sfx: String = "se_kira00"

const DEFAULT_BULLET: DanmakuBulletData = preload("res://resources/bullets/red_oval.tres")
const FLOWER_RING_SCENE: PackedScene = preload("res://scenes/effects/flower_ring_effect.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	var wave_angle: float = take_wave_angle()
	if playfield == null or not is_instance_valid(playfield):
		return

	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_BULLET
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var count: int = maxi(bullets_per_ring, 3)
	var stall_radius: float = lerpf(stall_radius_min, stall_radius_max, t_rank)
	var base_accel: float = lerpf(release_accel_min, release_accel_max, t_rank)
	var rings: int = maxi(int(round(lerpf(float(ring_count_rank1), float(ring_count_rank16), t_rank))), 1)

	var center: Vector2 = origin + center_offset
	var step_angle: float = TAU / float(count)
	var base_angle: float = deg_to_rad(base_angle_deg)
	var derived_lean: float = deg_to_rad(spin_lean_deg)
	# The ease-out brake (v = v0 * (1 - (t/T)^2)) covers two thirds of the ground a constant
	# speed would, so the launch speed is scaled up to land exactly on stall_radius.
	var expand_time_safe: float = maxf(lerpf(expand_time_rank1, expand_time_rank16, t_rank), 0.30)
	var expand_speed: float = 1.5 * stall_radius / expand_time_safe

	# The rose closes onto `center`, which is the point every bullet is then born at, so the
	# gather and the burst share one spot. It goes on the effects layer rather than the bullet
	# layer: it is decoration and must never be mistaken for a hazard.
	if pre_cast_delay > 0.0:
		var effects_layer: Node2D = playfield.get("effects_layer")
		if effects_layer == null:
			effects_layer = playfield.get_node_or_null("%Effects")
		if effects_layer == null:
			effects_layer = playfield
		# setup() before add_child(), as the freeze ring's call site also does: the effect
		# reads its own exports in _ready() to seed the shader and size its quad, so anything
		# handed over afterwards would be a frame late and the petal count never applied.
		var flower: Node2D = FLOWER_RING_SCENE.instantiate()
		flower.call("setup", center, pre_cast_delay, flower_color, flower_start_radius)
		effects_layer.add_child(flower)

		if not playfield.is_inside_tree():
			return
		await playfield.get_tree().create_timer(pre_cast_delay).timeout
		# A round can end while the rose is still closing.
		if not is_instance_valid(playfield):
			return

	if not sfx.is_empty():
		AudioService.play_sfx(sfx)

	if zun_mode:
		_fire_zun(playfield, center, rank, data, wave_angle)
		return

	# The release is where the figure stops being a ring: past this point every bullet is
	# accelerating outward with the lean on, which is what draws the spiral arms. The bullets
	# run that schedule off their own spawn moment, so this mirrors the boundary
	# `DanmakuBullet` uses (`contract_end`) rather than being told about it.
	#
	# Scheduled rather than awaited on purpose. `execute_sequence` awaits each step in turn,
	# so blocking here for the full three seconds would push the second figure of the card
	# that far back and take the two-figures-crossing effect apart.
	var release_time: float = expand_time_safe + hold_time + contract_time
	if not release_sfx.is_empty() and playfield.is_inside_tree():
		playfield.get_tree().create_timer(release_time).timeout.connect(func():
			if is_instance_valid(playfield):
				AudioService.play_sfx(release_sfx)
		)

	for ring in range(rings):
		if not is_instance_valid(playfield):
			return

		# Identical swell for every ring, so a stack launched in one frame travels and
		# stalls as one apparent circle. What pulls them apart on the way out is the
		# release acceleration, and - where ring_spawn_interval is set - the head start
		# each earlier ring already has.
		var ring_accel: float = base_accel * (1.0 + release_accel_spread * float(ring) / float(maxi(rings - 1, 1)))
		# Interleaving nudges each ring round by a fraction of the gap between neighbouring
		# bullets, so rings of only a few bullets knit together into one dense wheel.
		var ring_angle: float = base_angle
		if interleave_rings:
			ring_angle += step_angle * float(ring) / float(rings)

		for i in range(count):
			playfield.call(
				"spawn_danmaku_bullet_lunar_wave",
				data,
				center,
				ring_angle + float(i) * step_angle,
				expand_speed,
				expand_time_safe,
				hold_time,
				contract_speed,
				contract_time,
				release_speed,
				ring_accel,
				release_max_speed,
				derived_lean
			)

		if ring < rings - 1 and ring_spawn_interval > 0.0:
			if not playfield.is_inside_tree():
				return
			await playfield.get_tree().create_timer(ring_spawn_interval).timeout

func _fire_zun(playfield: Node2D, center: Vector2, rank: int, data: DanmakuBulletData, base_angle: float) -> void:
	var r: int = clampi(rank, 1, 16)
	var circles: int = r + zun_circles_base
	var speed: float = 125.0 + 6.25 * float(r)
	var circle_step: float = deg_to_rad(zun_arc_deg) / float(circles)
	var relaunch_drop: float = 2.0 * speed / float(circles) - 6.25
	var turn: float = deg_to_rad(zun_turn_deg)
	for k in range(circles):
		var relaunch: float = 2.0 * speed - float(k) * relaunch_drop
		for j in range(4):
			var dir: Vector2 = Vector2.from_angle(base_angle + float(k) * circle_step + float(j) * PI / 2.0)
			var b: DanmakuBullet = playfield.call("spawn_danmaku_bullet_decel_home", data, center, dir, speed, zun_brake_time, 0.0, relaunch)
			if b:
				b.relaunch_relative = true
				b.relaunch_turn = turn
	if not release_sfx.is_empty() and playfield.is_inside_tree():
		playfield.get_tree().create_timer(zun_brake_time).timeout.connect(func():
			if is_instance_valid(playfield):
				AudioService.play_sfx(release_sfx)
		)
