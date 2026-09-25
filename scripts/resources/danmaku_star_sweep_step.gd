class_name DanmakuStarSweepStep
extends DanmakuStep

## Clownpiece's Level 2: the star sweep of her first non-spell (TH15 `st05bs.ecl` Boss1).
##
## Big stars leave the emitter one per frame, each 9 degrees further round, so in about a
## quarter second they fan ~117 degrees centred on the player (TH15: 20 stars, a half
## circle); the next arc sweeps back the other way. Every star is aimed on its own frame, so
## the fan follows a moving player.
##
## ECL (`Boss1_at2`): type 23 (big star), `ins_607` mode 0 (aimed at the player), 20 stars an
## arc from +9.5 steps of 0.15708 rad to -9.5 (each `ins_23(1)` apart), then 20 back; the arc's
## centre is shifted by a random [-9987] x 0.349 rad (up to 20 degrees) each time. 3 px/frame
## (3.8 Hard, 4.3 Lunatic), from 24 units out from the emitter. Easy fires 3 one-way arcs,
## Normal and up 5 there-and-back pairs. Here the arcs alternate direction and rank sets how
## many (2 to 4, per the user), so the card is one short sweep to read, not a sustained one.
## TH15 units convert by field height (960 / 448 = 2.143).

@export var star_data: DanmakuBulletData = preload("res://resources/bullets/clownpiece_big_star_blue.tres")
## Node scale on the star: TH15's big star is large for a sent attack, so it goes out at the
## size Star and Stripe uses at Rank 1 (our small star's size).
@export var star_scale: float = 0.64

@export_group("Sweep")
@export var arcs_min_rank: int = 2
@export var arcs_max_rank: int = 4
## 14 fan ~117 degrees; TH15's 20 fan 171, which swept too far to the sides (per the user).
@export var stars_per_arc: int = 14
## 0.15708 rad a star.
@export var step_deg: float = 9.0
## One frame.
@export var star_interval: float = 0.016667
## Largest random turn of an arc's centre, 0.349 rad.
@export var wobble_deg: float = 20.0
## 3 px/frame.
@export var speed: float = 385.71
## Stars start 24 TH15 units out from the emitter.
@export var spawn_offset: float = 51.43

@export_group("Tell")
## The closing flower before the first arc, as on Yuuka's and Reisen's Level 2s.
@export var pre_cast_delay: float = 0.6666667
@export var flower_color: Color = Color(0.62, 0.3, 0.72, 1.0)
@export var flower_start_radius: float = 210.0

@export_group("Audio")
## Played at the start of each arc.
@export var sfx: String = "se_tan00"

const FLOWER_RING_SCENE: PackedScene = preload("res://scenes/effects/flower_ring_effect.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	# The arcs' wobble is the attack's shape, so it is rolled up front from the synced RNG.
	var rng: RandomNumberGenerator = take_sync_rng()
	if playfield == null or not is_instance_valid(playfield):
		return
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var arcs: int = roundi(lerpf(float(arcs_min_rank), float(arcs_max_rank), t_rank))
	var wobbles: Array[float] = []
	for a in arcs:
		wobbles.append(deg_to_rad(wobble_deg) * ((rng.randf() if rng else randf()) * 2.0 - 1.0))

	if pre_cast_delay > 0.0:
		var effects_layer: Node2D = playfield.get("effects_layer")
		if effects_layer == null:
			effects_layer = playfield
		var flower: Node2D = FLOWER_RING_SCENE.instantiate()
		flower.call("setup", origin, pre_cast_delay, flower_color, flower_start_radius)
		effects_layer.add_child(flower)
		await playfield.get_tree().create_timer(pre_cast_delay).timeout

	var step: float = deg_to_rad(step_deg)
	var half: float = step * (float(stars_per_arc) - 1.0) * 0.5
	for a in arcs:
		# Odd arcs sweep back the way the last one came.
		var sweep_sign: float = -1.0 if a % 2 == 0 else 1.0
		if not sfx.is_empty():
			AudioService.play_sfx(sfx)
		for i in stars_per_arc:
			if not is_instance_valid(playfield) or not playfield.is_inside_tree():
				return
			var aim: float = PI / 2.0
			var victim: Node2D = playfield.get("player")
			if victim and is_instance_valid(victim) and victim.position != origin:
				aim = (victim.position - origin).angle()
			var angle: float = aim + wobbles[a] - sweep_sign * half + sweep_sign * step * float(i)
			var dir := Vector2.from_angle(angle)
			var bullet: DanmakuBullet = playfield.spawn_danmaku_bullet(star_data,
				origin + dir * spawn_offset, DanmakuBullet.MotionMode.LINEAR, dir, speed)
			# Node scale grows sprite and hitbox together; the pool resets it on reuse.
			if bullet:
				bullet.scale = Vector2(star_scale, star_scale)
			await playfield.get_tree().create_timer(star_interval).timeout
