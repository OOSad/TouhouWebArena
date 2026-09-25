class_name DanmakuFakeApolloStep
extends DanmakuStep

## Clownpiece's "Fake Apollo" (TH15 `st05bs.ecl` BossCard5), reshaped for a versus match
## per the user. TH15's three moons circling her for the whole card is an endurance test;
## here one moon makes a single sweep: it fades in at one side of her, swings a half circle
## underneath her to the other side while shedding aimed rings of orbs, and fades out. The
## side it starts on is rolled per cast. See `ClownpieceMoon` for the moon and the ECL.
##
## Rank scales the orbs per ring (Easy's 18 to Normal's 23) and the gap between rings (Easy's
## 60 frames to Normal's 30).

@export var moon_scene: PackedScene = preload("res://scenes/attacks/clownpiece_moon.tscn")
@export var ring_data: DanmakuBulletData = preload("res://resources/bullets/clownpiece_glow_ball_purple.tres")

@export_group("Swing")
## Distance from Clownpiece, 140 TH15 units (`ins_411`).
@export var orbit_radius: float = 300.0
@export var fade_in: float = 0.5
## One half circle, per the user.
@export var swing_duration: float = 4.0
@export var fade_out: float = 0.5

@export_group("Rings")
@export var ring_count_min_rank: int = 18
@export var ring_count_max_rank: int = 23
## Seconds between rings at Rank 1 (Easy, 60 frames) and Rank 16 (Normal, 30).
@export var ring_interval_min_rank: float = 1.0
@export var ring_interval_max_rank: float = 0.5
## 0.8 px/frame.
@export var ring_speed: float = 102.86
## Rings form 80 TH15 units out from the moon's centre, just past its edge (`ins_627`).
@export var ring_offset: float = 171.43
## How far the ring is turned off the player (`ins_604`, 10 degrees).
@export var ring_turn_deg: float = 10.0

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	# Which side the moon sets off from is the attack's identity, so it comes from the
	# synced RNG; both clients see it swing the same way.
	var rng: RandomNumberGenerator = take_sync_rng()
	if playfield == null or not is_instance_valid(playfield):
		return
	var from_left: bool = (rng.randf() if rng else randf()) < 0.5

	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var count: int = roundi(lerpf(float(ring_count_min_rank), float(ring_count_max_rank), t_rank))
	var interval: float = lerpf(ring_interval_min_rank, ring_interval_max_rank, t_rank)

	var layer: Node2D = playfield.get("bullets_layer")
	if layer == null:
		layer = playfield.get_node_or_null("%Bullets")
	if layer == null:
		layer = playfield
	var moon: ClownpieceMoon = moon_scene.instantiate() as ClownpieceMoon
	if moon == null:
		return
	# Angles: PI is her left, 0 her right; going between them through PI/2 passes below her.
	var from_angle: float = PI if from_left else 0.0
	var to_angle: float = 0.0 if from_left else PI
	moon.setup(playfield, origin, orbit_radius, from_angle, to_angle, fade_in, swing_duration,
		fade_out, ring_data, count, interval, ring_speed, ring_offset, ring_turn_deg)
	layer.add_child(moon)
	# Held for the moon's whole life, as Star and Stripe holds for its beams, so she doesn't
	# start another attack while it is still out.
	if playfield.is_inside_tree():
		await playfield.get_tree().create_timer(fade_in + swing_duration + fade_out).timeout
