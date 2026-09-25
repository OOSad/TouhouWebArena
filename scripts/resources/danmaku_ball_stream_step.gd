class_name DanmakuBallStreamStep
extends DanmakuStep

## Yuuka Kazami's Level 4 Boss Attack 2 (pl09.ecl sub4).
##
## After a wind-up, the boss takes one aim at the victim and fires a six-way ring of big balls
## every few frames. Each ring sits half a step off that aim (bullet_offset_circle), so the
## victim stands in the gap between two of the six streams rather than on one. The balls are so
## fast and so closely spaced that each stream reads as a solid chain. After the straight part,
## the last few rings turn a little further every time, all six streams the same way, so the
## tails of the chains sweep round and close in.
##
## Nothing here scales with rank: sub4 never reads IC2.

@export_group("Bullet Type")
@export var bullet_data: DanmakuBulletData = null

@export_group("Shape")
## Bullets in each ring (ZUN: 6)
@export var bullets_per_ring: int = 6
## Ball speed, px/s (ZUN: 7.0 px/frame)
@export var speed: float = 875.0

@export_group("Timing")
## Frames between the cast starting and the aim being taken (ZUN: +40)
@export var windup_frames: int = 40
## Frames between rings (ZUN: +4)
@export var emission_interval_frames: int = 4
## Rings fired at the fixed aim (ZUN: IC0 = 30)
@export var straight_emissions: int = 30
## Rings fired while turning (ZUN: IC0 = 10)
@export var turning_emissions: int = 10
## Turn added after each turning ring, degrees; which way is a coin toss per cast
## (ZUN: 0.049087387 rad, pi / 64)
@export var turn_per_emission_deg: float = 2.8125

@export_group("Audio")
## Played as the wind-up starts (ZUN's effect_sound(Sound5); se_power0 per the user)
@export var windup_sfx: String = ""

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	# The turn direction decides where the tails sweep, so it is attack identity: synced.
	var rng: RandomNumberGenerator = take_sync_rng()

	if playfield == null or not is_instance_valid(playfield):
		return
	if not playfield.has_method("spawn_danmaku_bullet") or not playfield.is_inside_tree():
		return

	var turn_sign: float = 1.0
	if (rng.randi() if rng else randi()) % 2 == 0:
		turn_sign = -1.0

	if not windup_sfx.is_empty():
		AudioService.play_sfx(windup_sfx)

	var tree: SceneTree = playfield.get_tree()
	await tree.create_timer(float(windup_frames) / 60.0, false).timeout
	if not is_instance_valid(playfield) or not playfield.is_inside_tree():
		return

	# PLAYER_ANGLE: taken once, from where the boss stands now
	var aim: float = PI * 0.5
	var victim: Node2D = playfield.get("player")
	if victim and is_instance_valid(victim) and victim.position != origin:
		aim = (victim.position - origin).angle()

	var count: int = maxi(bullets_per_ring, 1)
	var step_angle: float = TAU / float(count)
	var turn: float = deg_to_rad(turn_per_emission_deg) * turn_sign
	var emissions: int = straight_emissions + turning_emissions
	var interval: float = float(emission_interval_frames) / 60.0
	var elapsed: float = 0.0
	var k: int = 0

	while k < emissions:
		# Every ring due since the last frame fires now, pushed out by however far it would
		# already have flown, so a dropped frame does not break the chain.
		while k < emissions and float(k) * interval <= elapsed:
			# A ring that forms in place first (spawn_fade_in_holds) has not flown yet, so it
			# starts on the emitter however late it is
			var late: float = 0.0 if bullet_data.spawn_fade_in_holds else elapsed - float(k) * interval
			# The first turning ring still fires at the aim; ZUN turns after firing
			var turned: int = maxi(k - straight_emissions, 0)
			var base: float = aim + step_angle * 0.5 + turn * float(turned)
			for i in range(count):
				var dir := Vector2.from_angle(base + step_angle * float(i))
				playfield.spawn_danmaku_bullet(bullet_data, origin + dir * speed * late, DanmakuBullet.MotionMode.LINEAR, dir, speed)
			# Flag 0x200: a shot sound on every ring, on the single-voice channel
			AudioService.play_tan_exclusive()
			k += 1

		if k >= emissions:
			break
		await tree.process_frame
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return
		elapsed += playfield.get_process_delta_time()
