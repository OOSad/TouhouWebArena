class_name DanmakuRandomSprayStep
extends DanmakuStep

## Aya Shameimaru's Level 4 boss attack 3 (pl10.ecl sub5).
##
## Every two frames the boss throws a handful of bullets in random directions at random
## speeds (ZUN's bullet_random: three red pellets and two red ring balls, each between the
## slow and fast speed), for `16 + rank` emissions. Where each bullet goes is per-bullet
## jitter, so it is left unsynced.

@export_group("Bullet Types")
@export var bullet_data: DanmakuBulletData = null
@export var bullets_per_emission: int = 3
@export var second_bullet_data: DanmakuBulletData = null
@export var second_bullets_per_emission: int = 2

@export_group("Timing & Rank Scaling")
## Emissions = emissions_base + rank (ZUN: IC2 + 16)
@export var emissions_base: int = 16
## Frames between emissions, at 60 fps (ZUN: 2)
@export var interval_frames: int = 2

@export_group("Speed")
## Fastest bullet: speed_base + speed_per_rank * rank, px/s (ZUN: 5.0 + 0.1 rank)
@export var speed_base: float = 625.0
@export var speed_per_rank: float = 12.5
## Slowest bullet, px/s (ZUN: 1.0)
@export var speed_slow: float = 125.0

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	if not playfield.has_method("spawn_danmaku_bullet") or not playfield.is_inside_tree():
		return

	var r: int = clampi(rank, 1, 16)
	var fast: float = speed_base + speed_per_rank * float(r)
	var emissions: int = emissions_base + r
	var tree: SceneTree = playfield.get_tree()

	for k in range(emissions):
		var center: Vector2 = origin
		var boss: Node2D = playfield.get("active_boss")
		if boss and is_instance_valid(boss):
			center = boss.position
		_spray(playfield, center, bullet_data, bullets_per_emission, fast)
		_spray(playfield, center, second_bullet_data, second_bullets_per_emission, fast)
		# Flag 0x200: a shot sound per emission, on the single-voice channel at 30 a second
		AudioService.play_tan_exclusive()
		if k < emissions - 1:
			await tree.create_timer(float(interval_frames) / 60.0).timeout
			if not is_instance_valid(playfield) or not playfield.is_inside_tree():
				return


func _spray(playfield: Node2D, center: Vector2, data: DanmakuBulletData, count: int, fast: float) -> void:
	if data == null:
		return
	for i in range(count):
		var dir := Vector2.from_angle(randf() * TAU)
		playfield.spawn_danmaku_bullet(data, center, DanmakuBullet.MotionMode.LINEAR, dir, randf_range(speed_slow, fast))
