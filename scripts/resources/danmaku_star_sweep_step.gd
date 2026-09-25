class_name DanmakuStarSweepStep
extends DanmakuStep

## Clownpiece's "Starry Illusion", a Level 4 boss attack: the star sweep of her first
## non-spell (TH15 `st05bs.ecl` Boss1), which the user named. It was her Level 2 first, but
## stars appearing from nowhere lost the charm of her spraying the field back and forth, so
## it moved to the boss, where they come from her.
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
## many (Easy's 3 to 5), so the card stays a short spray, not a sustained one.
## TH15 units convert by field height (960 / 448 = 2.143).

## LoLK's pink-red big star (type 23 colour 1), as in the footage.
@export var star_data: DanmakuBulletData = preload("res://resources/bullets/clownpiece_big_star_red.tres")
## Node scale on the star at Rank 1 and Rank 16. 0.64 is our small star's size (Star and
## Stripe's Rank 1); 1.0 is TH15's big star.
@export var star_scale_min_rank: float = 0.8
@export var star_scale_max_rank: float = 1.0

@export_group("Sweep")
@export var arcs_min_rank: int = 3
@export var arcs_max_rank: int = 5
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
## A closing flower before the first arc, as on Yuuka's and Reisen's Level 2s, for a sent
## version. 0 skips it: the boss has her own cast animation.
@export var pre_cast_delay: float = 0.0
@export var flower_color: Color = Color(0.62, 0.3, 0.72, 1.0)
@export var flower_start_radius: float = 210.0

@export_group("Flanking Beams")
## LoLK's version flings white orbs up from her in pairs that drop a curtain of blue beams
## down each side of the spray (`Boss1_at`, 5 or 6 a side). Per the user, one beam a side:
## Earth Light Ray's blue beam, with its rune warning, dropped from the top of the field.
## null leaves them out.
@export var flank_beam_scene: PackedScene = preload("res://scenes/attacks/earth_light_ray.tscn")
## Distance from her to each beam, clamped inside the field.
@export var flank_offset: float = 150.0
## How long each beam burns at Rank 1 and Rank 16, after its warning.
@export var flank_fire_min_rank: float = 0.8
@export var flank_fire_max_rank: float = 1.2
@export var flank_damage: float = 1.0

@export_group("Audio")
## Played at the start of each arc.
@export var sfx: String = "se_tan00"

const FLOWER_RING_SCENE: PackedScene = preload("res://scenes/effects/flower_ring_effect.tscn")
const FIELD_WIDTH: float = 600.0
const BEAM_MARGIN: float = 30.0

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	# The arcs' wobble is the attack's shape, so it is rolled up front from the synced RNG.
	var rng: RandomNumberGenerator = take_sync_rng()
	if playfield == null or not is_instance_valid(playfield):
		return
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var arcs: int = roundi(lerpf(float(arcs_min_rank), float(arcs_max_rank), t_rank))
	var star_scale: float = lerpf(star_scale_min_rank, star_scale_max_rank, t_rank)
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

	if flank_beam_scene:
		_drop_flank_beams(playfield, origin, lerpf(flank_fire_min_rank, flank_fire_max_rank, t_rank))

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


## One beam each side of her, falling from the top of the field.
func _drop_flank_beams(playfield: Node2D, origin: Vector2, fire_time: float) -> void:
	var layer: Node2D = playfield.get("bullets_layer")
	if layer == null:
		layer = playfield.get_node_or_null("%Bullets")
	if layer == null:
		layer = playfield
	for side in [-1.0, 1.0]:
		var beam: EarthLightRay = flank_beam_scene.instantiate() as EarthLightRay
		if beam == null:
			return
		beam.position = Vector2(clampf(origin.x + side * flank_offset, BEAM_MARGIN, FIELD_WIDTH - BEAM_MARGIN), 0.0)
		beam.fire_duration = fire_time
		beam.damage = flank_damage
		layer.add_child(beam)
