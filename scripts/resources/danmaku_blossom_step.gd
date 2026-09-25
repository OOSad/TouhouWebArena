class_name DanmakuBlossomStep
extends DanmakuStep

## Yuuka Kazami's Level 2 and Level 3, Flower Sign "Blossoming of Gensokyo" (pl09.ecl sub0 / sub1).
##
## A point near the top of the victim's field fires small rings in quick succession: every two
## frames, one six-way ring turning one way and a second six-way ring, set half a step between
## the first one's spokes, turning the other way. Each emission is a little faster than the one
## before, so the later rings overtake the earlier ones. The two counter-turning sets cross
## each other as they open out, which is what draws the petals.
##
## Level 2 alternates rice and pellets from one emission to the next. Level 3 fires
## arrowheads and runs the two rings on slightly different speed tracks (0.15 against 0.14
## per emission, swapping every emission), so its petals fray apart as they travel.
##
## ZUN spawns the emitter at (0, 128): dead centre, whatever the victim is doing. The script
## computes Reimu's mirrored -PLAYER_X beside it and never uses it, so neither do we.

@export_group("Bullet Types")
## Fired on even emissions (0, 2, 4, ...), both rings
@export var bullet_data_even: DanmakuBulletData = null
## Fired on odd emissions (1, 3, 5, ...), both rings
@export var bullet_data_odd: DanmakuBulletData = null

@export_group("Shape")
## Bullets in each ring (ZUN: 6)
@export var bullets_per_ring: int = 6
## Each emission turns the first ring this way and the second the opposite way, in degrees
## (ZUN: 0.10471976 rad)
@export var turn_per_emission_deg: float = 6.0
## Emitter height on the victim's field, or -1 to use the caller's origin (ZUN y=128)
@export var spawn_y_override: float = 266.7

@export_group("Timing & Rank Scaling")
## Rank / pairs_rank_divisor + pairs_base emission pairs (ZUN: rank / 2 + 5, integer)
@export var pairs_base: int = 5
@export var pairs_rank_divisor: int = 2
## Frames between emissions, at 60 fps (ZUN: 2)
@export var emission_interval_frames: int = 2

@export_group("Pre-Cast Flower")
## Seconds the 5-point runic flower (FlowerRingEffect, as on Reisen's Lunar Wave) spends
## closing onto the emitter before the first ring. ZUN: effect_particle(Effect25) on spawn,
## then +40 frames before firing. 0 skips it (the boss's rerun opens without one).
@export var pre_cast_delay: float = 0.0
@export var flower_color: Color = Color(1.0, 0.45, 0.85, 1.0)
@export var flower_start_radius: float = 210.0

@export_group("Speed")
## First emission's speed, px/s (ZUN: 1.0)
@export var speed_start: float = 125.0
## Added after every emission, px/s (ZUN: 0.15)
@export var speed_step: float = 18.75
## Level 3's second speed track, px/s per emission (ZUN: 0.14), or -1 for none. When set, the
## first ring rides this track on even emissions and the main one on odd, and the second
## ring does the reverse.
@export var alt_speed_step: float = -1.0

const FLOWER_RING_SCENE: PackedScene = preload("res://scenes/effects/flower_ring_effect.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	# ZUN's RAND_ANGLE, rolled once per wave by SpellcardData so both clients agree.
	var start_angle: float = take_wave_angle()

	if playfield == null or not is_instance_valid(playfield):
		return
	if not playfield.has_method("spawn_danmaku_bullet") or not playfield.is_inside_tree():
		return

	var center: Vector2 = origin
	if spawn_y_override >= 0.0:
		center.y = spawn_y_override

	# The rose closes onto the emitter, the point every ring then leaves from. Effects layer,
	# not bullets: it is decoration and must never read as a hazard. setup() before add_child(),
	# because the effect seeds its shader from its own exports in _ready().
	if pre_cast_delay > 0.0:
		var effects_layer: Node2D = playfield.get("effects_layer")
		if effects_layer == null:
			effects_layer = playfield
		var flower: Node2D = FLOWER_RING_SCENE.instantiate()
		flower.call("setup", center, pre_cast_delay, flower_color, flower_start_radius)
		effects_layer.add_child(flower)
		await playfield.get_tree().create_timer(pre_cast_delay).timeout
		# A round can end while the rose is still closing
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return

	var count: int = maxi(bullets_per_ring, 1)
	var step_angle: float = TAU / float(count)
	var turn: float = deg_to_rad(turn_per_emission_deg)
	var pairs: int = clampi(rank, 1, 16) / maxi(pairs_rank_divisor, 1) + pairs_base
	var emissions: int = pairs * 2
	var interval: float = float(emission_interval_frames) / 60.0
	var alt_step: float = alt_speed_step if alt_speed_step >= 0.0 else speed_step

	var tree: SceneTree = playfield.get_tree()
	var elapsed: float = 0.0
	var k: int = 0

	while k < emissions:
		# Every emission due since the last frame fires now, pushed out by however far it
		# would already have flown, so a dropped frame does not bunch the rings together.
		while k < emissions and float(k) * interval <= elapsed:
			var late: float = elapsed - float(k) * interval
			var data: DanmakuBulletData = bullet_data_even if k % 2 == 0 else bullet_data_odd
			var main_speed: float = speed_start + speed_step * float(k)
			var alt_speed: float = speed_start + alt_step * float(k)
			# sub1: even emissions give the first ring F3 and the second F2, odd the reverse
			var speed_a: float = alt_speed if k % 2 == 0 else main_speed
			var speed_b: float = main_speed if k % 2 == 0 else alt_speed
			var angle_a: float = start_angle + turn * float(k)
			# bullet_offset_circle: half a step round from its own base angle
			var angle_b: float = start_angle - turn * float(k) + step_angle * 0.5

			for i in range(count):
				var dir_a := Vector2.from_angle(angle_a + step_angle * float(i))
				playfield.spawn_danmaku_bullet(data, center + dir_a * speed_a * late, DanmakuBullet.MotionMode.LINEAR, dir_a, speed_a)
				var dir_b := Vector2.from_angle(angle_b + step_angle * float(i))
				playfield.spawn_danmaku_bullet(data, center + dir_b * speed_b * late, DanmakuBullet.MotionMode.LINEAR, dir_b, speed_b)

			# Flag 0x200: a shot sound on every emission. At 30 a second it goes on the
			# single-voice channel, as Reisen's machinegun does, so each cuts off the last.
			AudioService.play_tan_exclusive()
			k += 1

		if k >= emissions:
			break
		await tree.process_frame
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return
		elapsed += playfield.get_process_delta_time()
