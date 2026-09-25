class_name DanmakuBouncingKnifeRingStep
extends DanmakuStep

## Sakuya's Level 4 Boss Attack 2: Reflective Knife Explosion.
## Summons circle_count small circles of daggers_per_circle purple daggers each, one circle at a
## time (delay_between_circles apart), every circle independently spawned at its own randomized
## point within spawn_offset_radius of the boss's position ("in a random area around herself" -
## not all sharing one center or one moment). Every dagger in a circle launches outward radially
## at once. Any dagger that travels bounce_overshoot px past the left, right, or top playfield
## edge ricochets back inward exactly once, turning blue in the process
## (DanmakuBullet.bounce_off_walls/bounce_bullet_data/bounce_overshoot handle the actual
## reflection physics - see danmaku_bullet.gd) and then flies straight from there on. Daggers
## reaching the bottom edge (where the player is) are never reflected - they simply exit like any
## other bullet.
## Fixed at 7 daggers/circle and 10 circles at every rank - only speed scales with rank.

@export_group("Bullet Settings")
## Pre-bounce dagger (purple).
@export var dagger_data: DanmakuBulletData = null
## Post-bounce dagger (blue) - swapped in the one time a dagger ricochets off a wall.
@export var bounced_dagger_data: DanmakuBulletData = null

@export_group("Circle Geometry - Fixed at Every Rank")
@export var daggers_per_circle: int = 7
@export var circle_count: int = 10
## Each circle's own center is independently randomized within this radius of the boss's
## position - rolled fresh per circle, per cast.
@export var spawn_offset_radius: float = 110.0
## Seconds between each circle's emission - circles appear one at a time, not all in one frame.
@export var delay_between_circles: float = 0.1
## pl02.ecl sub3: rank / 2 + 10 circles, int(60 / count) frames apart, overriding the two above
@export var zun_rank_timing: bool = false
## pl02.ecl sub3 offset quirk: x and y each take their own random angle (cos(a) x r, sin(b) x r)
@export var zun_offset: bool = false

@export_group("Kinematics - Rank Scaled")
## Lower than the Pellet-Spewing Dagger's fixed 300 px/s at Rank 1 - the user found the rank
## difference imperceptible at 300->390, so Rank 1 was slowed down to widen the gap.
@export var speed_min: float = 230.0
@export var speed_max: float = 390.0
## How far past the left/right/top playfield edge a dagger travels (briefly offscreen) before
## ricocheting back inward, instead of bouncing exactly at the boundary line.
@export var bounce_overshoot: float = 40.0

@export_group("Sound Effects")
@export var sfx: String = "se_tan00"

const DEFAULT_DAGGER_DATA: DanmakuBulletData = preload("res://resources/bullets/sakuya_knife_purple.tres")
const DEFAULT_BOUNCED_DAGGER_DATA: DanmakuBulletData = preload("res://resources/bullets/sakuya_knife_blue.tres")
const EXPLOSION_BURST_SCENE: PackedScene = preload("res://scenes/effects/knife_explosion_burst.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return

	if daggers_per_circle <= 0 or circle_count <= 0:
		return

	var data: DanmakuBulletData = dagger_data if dagger_data else DEFAULT_DAGGER_DATA
	var b_data: DanmakuBulletData = bounced_dagger_data if bounced_dagger_data else DEFAULT_BOUNCED_DAGGER_DATA
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var speed: float = lerpf(speed_min, speed_max, t_rank)

	var boss_pos: Vector2 = origin
	var active_boss: Node2D = playfield.get("active_boss")
	if active_boss and is_instance_valid(active_boss):
		boss_pos = active_boss.position

	var effects_layer: Node2D = playfield.get("effects_layer")
	if effects_layer == null:
		effects_layer = playfield.get_node_or_null("%Effects")
	if effects_layer == null:
		effects_layer = playfield

	var angle_step: float = TAU / float(daggers_per_circle)
	var circles: int = circle_count
	var circle_delay: float = delay_between_circles
	if zun_rank_timing:
		circles = clampi(rank, 1, 16) / 2 + 10
		circle_delay = float(60 / circles) / 60.0

	for c in range(circles):
		if not is_instance_valid(playfield):
			return

		var circle_center: Vector2 = boss_pos
		if spawn_offset_radius > 0.0:
			var jitter_angle: float = randf_range(0.0, TAU)
			var jitter_radius: float = randf_range(0.0, spawn_offset_radius)
			if zun_offset:
				circle_center += Vector2(cos(jitter_angle), sin(randf_range(0.0, TAU))) * jitter_radius
			else:
				circle_center += Vector2(cos(jitter_angle), sin(jitter_angle)) * jitter_radius

		var start_angle: float = randf_range(0.0, TAU)

		var burst: Node2D = EXPLOSION_BURST_SCENE.instantiate()
		burst.position = circle_center
		effects_layer.add_child(burst)

		for i in range(daggers_per_circle):
			var angle: float = start_angle + (angle_step * float(i))
			var dir := Vector2.from_angle(angle)
			var dagger: DanmakuBullet = playfield.call(
				"spawn_danmaku_bullet",
				data,
				circle_center,
				DanmakuBullet.MotionMode.LINEAR,
				dir,
				speed
			)
			if dagger:
				dagger.bounce_off_walls = true
				dagger.bounce_bullet_data = b_data
				dagger.bounce_overshoot = bounce_overshoot

		if not sfx.is_empty():
			AudioService.play_sfx(sfx)

		if c < circles - 1 and circle_delay > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(circle_delay).timeout
