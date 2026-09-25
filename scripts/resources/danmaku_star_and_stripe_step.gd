class_name DanmakuStarAndStripeStep
extends DanmakuStep

## Clownpiece's Hell Sign "Star and Stripe" (TH15 `st05bs.ecl` BossCard2), in place of Inferno
## "Striped Abyss", whose two-sided crossing beams proved too much to port (per the user).
##
## Stripes: twelve long beams slide in from the left wall only, one row at a time from the
## top of the field downward, and every one of them drifts upward at the same rate while it
## holds its row. The gaps between rows therefore keep their size; the player rides one up.
## This is TH15's card mirrored top to bottom, per the user: sinking rows squashed players at
## the bottom of the field, and rising ones carry them up toward Fake Apollo's moon.
## Stars: meanwhile two spawn points, mirrored about the centre, sweep back and forth across
## the top of the field dropping stars straight down at a random slow speed.
##
## Rank also scales the beam count (7 to 11, TH15 fires 12) and the star size, and the beams
## slide in far slower than TH15's at every rank; all three per the user after playtesting.
##
## ECL: `BossCard2_at` fires `ins_700` line lasers (sprite 38 colour 2, speed 12, length 1600)
## from x = -192 at y = 448 - rand * 16 - 64n, 12 of them `ins_23(10)` apart, then waits 90
## frames; the drift is `ins_536([-9981], 0.6, 1.3, 1.8, 1.8)`, read off the footage as
## downward. `BossCard2_at3` (Hard / Lunatic) drops a star (type 23) from x = +-A + rand * 16,
## y = -16 + rand * 64, at speed 1 + rand, every 18 / 11 frames, A stepping 24 px across the
## field and back. Rank runs from Easy's drift (0.6) to Normal's (1.3) and the stars from
## Hard's spacing (18 frames) to Normal's 12; Lunatic is left out. TH15 units convert by field
## height (960 / 448 = 2.143).
##
## The beams are Marisa's Earth Light Ray recoloured red (`earth_light_ray_red.tscn`) in its
## primed mode.

@export var laser_scene: PackedScene = null
@export var star_data: DanmakuBulletData = null

@export_group("Stripes")
## Beams per cast at Rank 1 and Rank 16. TH15 fires 12; playtesting asked for fewer, scaling.
@export var stripes_min_rank: int = 7
@export var stripes_max_rank: int = 11
## 64 TH15 px.
@export var row_spacing: float = 137.14
## Random shift of the first row, 16 TH15 px.
@export var start_jitter: float = 34.29
## 10 frames.
@export var stripe_interval: float = 0.1667
## Wait after the last stripe before the cast ends, 90 frames.
@export var hold_after: float = 1.5
## TH15 slides its beams in at 12 px/frame (1543 px/s), which played as a jumpscare, so every
## rank uses this instead: a beam takes about a second to cross the field (480 read as too slow).
@export var beam_speed: float = 640.0
## Earth Light Ray's own length. TH15's beam is 1600 px long at 12 px/frame, so it holds its
## row ~2.2s; at the slower speed this length holds it about as long.
@export var beam_length: float = 1060.0
## Upward drift at Rank 1 (Easy, 0.6 px/frame) and Rank 16 (Normal, 1.3).
@export var drift_min_rank: float = 77.14
@export var drift_max_rank: float = 167.14
@export var damage: float = 1.0

@export_group("Stars")
## Seconds between star drops at Rank 1 (Hard's 18 frames) and Rank 16 (Normal's 12).
@export var star_interval_min_rank: float = 0.3
@export var star_interval_max_rank: float = 0.2
## 24 TH15 px per drop.
@export var star_sweep_step: float = 51.43
## Random x offset, 16 TH15 px.
@export var star_x_jitter: float = 34.29
## Spawn height: -16 + rand * 64 TH15 px.
@export var star_y_min: float = -34.29
@export var star_y_range: float = 137.14
## 1 + rand px/frame.
@export var star_speed_min: float = 128.57
@export var star_speed_max: float = 257.14
## Star size at Rank 1 and Rank 16, as a multiple of the star bullet (sprite and hitbox). The
## bullet is TH15's big star at TH15 scale; Rank 1 shrinks it to the size of our small star.
@export var star_scale_min_rank: float = 0.64
@export var star_scale_max_rank: float = 1.0

@export_group("Audio")
@export var sfx: String = "se_lazer00"

const FIELD_WIDTH: float = 600.0
const FIELD_HEIGHT: float = 960.0
## Earth Light Ray's beam spans local y -60 to 1000 along its axis.
const BEAM_TAIL: float = -60.0
const BEAM_HEAD: float = 1000.0
const EXIT_MARGIN: float = 40.0

const DEFAULT_LASER_SCENE: PackedScene = preload("res://scenes/attacks/earth_light_ray_red.tscn")
const DEFAULT_STAR: DanmakuBulletData = preload("res://resources/bullets/clownpiece_big_star_blue.tres")

func execute(playfield: Node2D, _origin: Vector2, rank: int) -> void:
	# Claimed before the first await; every random draw below is rolled up front in one run
	# so both netplay clients (and two overlapping casts) see the same pattern.
	var rng: RandomNumberGenerator = take_sync_rng()
	if playfield == null or not is_instance_valid(playfield):
		return

	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var drift: float = lerpf(drift_min_rank, drift_max_rank, t_rank)
	var stripes: int = roundi(lerpf(float(stripes_min_rank), float(stripes_max_rank), t_rank))
	var star_scale: float = lerpf(star_scale_min_rank, star_scale_max_rank, t_rank)
	var star_interval: float = lerpf(star_interval_min_rank, star_interval_max_rank, t_rank)
	var stripe_time: float = stripe_interval * float(maxi(stripes - 1, 0))
	var cast_time: float = stripe_time + hold_after
	var star_count: int = maxi(int(cast_time / star_interval), 0)

	var first_row: float = _roll(rng) * start_jitter
	var stars := []
	var sweep_x: float = 0.0
	var sweeping_out: bool = true
	for i in range(star_count):
		stars.append({
			"left_x": sweep_x + _roll(rng) * star_x_jitter,
			"left_y": star_y_min + _roll(rng) * star_y_range,
			"left_speed": lerpf(star_speed_min, star_speed_max, _roll(rng)),
			"right_x": FIELD_WIDTH - sweep_x + _roll(rng) * star_x_jitter,
			"right_y": star_y_min + _roll(rng) * star_y_range,
			"right_speed": lerpf(star_speed_min, star_speed_max, _roll(rng)),
		})
		# The two points start at opposite walls and cross; each turns back at the far wall.
		if sweeping_out:
			if sweep_x < FIELD_WIDTH:
				sweep_x += star_sweep_step
			else:
				sweeping_out = false
		elif sweep_x > 0.0:
			sweep_x -= star_sweep_step
		else:
			sweeping_out = true

	_drop_stars(playfield, stars, star_interval, star_scale)

	for n in range(stripes):
		if not is_instance_valid(playfield):
			return
		_fire(playfield, first_row + row_spacing * float(n), -drift)
		if n < stripes - 1:
			if not playfield.is_inside_tree():
				return
			await playfield.get_tree().create_timer(stripe_interval).timeout
	if hold_after > 0.0 and playfield.is_inside_tree():
		await playfield.get_tree().create_timer(hold_after).timeout

func _roll(rng: RandomNumberGenerator) -> float:
	return rng.randf() if rng else randf()

func _drop_stars(playfield: Node2D, stars: Array, interval: float, star_scale: float) -> void:
	var data: DanmakuBulletData = star_data if star_data else DEFAULT_STAR
	for star in stars:
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return
		for side in ["left", "right"]:
			var bullet: DanmakuBullet = playfield.spawn_danmaku_bullet(
				data,
				Vector2(star[side + "_x"], star[side + "_y"]),
				DanmakuBullet.MotionMode.LINEAR,
				Vector2.DOWN,
				star[side + "_speed"]
			)
			# Node scale grows sprite and hitbox together; the pool resets it on reuse.
			if bullet:
				bullet.scale = Vector2(star_scale, star_scale)
		await playfield.get_tree().create_timer(interval).timeout

## One beam from the left wall at height y, head first, stretched to `beam_length`.
func _fire(playfield: Node2D, y: float, drift: float) -> void:
	var layer: Node2D = playfield.get("bullets_layer")
	if layer == null:
		layer = playfield.get_node_or_null("%Bullets")
	if layer == null:
		layer = playfield
	var scene: PackedScene = laser_scene if laser_scene else DEFAULT_LASER_SCENE
	var beam: EarthLightRay = scene.instantiate() as EarthLightRay
	if beam == null:
		return
	var stretch: float = beam_length / (BEAM_HEAD - BEAM_TAIL)
	# Rotated so the beam lies along +x with its head leading, then stretched along its axis;
	# the head starts at the wall, so the rest of the beam is still off screen behind it.
	beam.rotation = -PI / 2.0
	beam.scale = Vector2(1.0, stretch)
	beam.position = Vector2(-BEAM_HEAD * stretch, y)
	beam.velocity = Vector2(beam_speed, drift)
	beam.primed = true
	beam.fire_duration = (FIELD_WIDTH + (BEAM_HEAD - BEAM_TAIL) * stretch + EXIT_MARGIN) / beam_speed
	beam.damage = damage
	layer.add_child(beam)
	if not sfx.is_empty():
		AudioService.play_sfx(sfx)
