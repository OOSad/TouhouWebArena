class_name DanmakuFakeApolloStep
extends DanmakuStep

## Clownpiece's "Fake Apollo" (TH15 `st05bs.ecl` BossCard5), her Level 2 and Level 3, reshaped
## for a versus match per the user. TH15's three moons circling her for the whole card is an
## endurance test; here the moons make a single half turn round the orbit's centre while
## shedding aimed rings of orbs, then fade out. The way they turn is rolled per cast. See
## `ClownpieceMoon` for the moon and the ECL.
##
## Level 2 is one moon swinging from one side of the field to the other underneath the
## emitter. Level 3 is TH15's three moons, 120 degrees apart, turning as a wheel centred
## lower in the field (a 140-unit orbit round the top of the field would put two of them off
## screen). Each level's resource sets its own Rank 1 and Rank 16 values.

@export var moon_scene: PackedScene = preload("res://scenes/attacks/clownpiece_moon.tscn")
@export var ring_data: DanmakuBulletData = preload("res://resources/bullets/clownpiece_glow_ball_purple.tres")

@export_group("Moons")
## 1 or TH15's 3, spaced evenly round the orbit.
@export var moon_count: int = 1
## The orbit's centre, relative to the emitter.
@export var orbit_offset: Vector2 = Vector2.ZERO
## 140 TH15 units (`ins_411`) for one moon.
@export var orbit_radius: float = 300.0
@export var fade_in: float = 0.5
## One half turn, per the user.
@export var swing_duration: float = 4.0
@export var fade_out: float = 0.5

@export_group("Rings")
@export var ring_count_min_rank: int = 18
@export var ring_count_max_rank: int = 23
## Seconds between rings at Rank 1 (Easy, 60 frames) and Rank 16 (Normal, 30).
@export var ring_interval_min_rank: float = 1.0
@export var ring_interval_max_rank: float = 0.5
## 0.8 px/frame (Easy / Normal); Hard's 1.2 is 154 px/s.
@export var ring_speed_min_rank: float = 102.86
@export var ring_speed_max_rank: float = 102.86
## How far out from the moon's centre rings form: just past its ~120 px edge. TH15's 80 units
## (`ins_627`, 171 px) read as too far from the moon (per the user).
@export var ring_offset: float = 130.0
## How far the ring is turned off the player (`ins_604`, 10 degrees).
@export var ring_turn_deg: float = 10.0

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	# Which way the moons turn is the attack's identity, so it comes from the synced RNG;
	# both clients see them swing the same way.
	var rng: RandomNumberGenerator = take_sync_rng()
	if playfield == null or not is_instance_valid(playfield):
		return
	var from_left: bool = (rng.randf() if rng else randf()) < 0.5

	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var count: int = roundi(lerpf(float(ring_count_min_rank), float(ring_count_max_rank), t_rank))
	var interval: float = lerpf(ring_interval_min_rank, ring_interval_max_rank, t_rank)
	var speed: float = lerpf(ring_speed_min_rank, ring_speed_max_rank, t_rank)

	var layer: Node2D = playfield.get("bullets_layer")
	if layer == null:
		layer = playfield.get_node_or_null("%Bullets")
	if layer == null:
		layer = playfield
	# Angles: PI is left of the centre, 0 right of it; the first moon starts on one side and
	# turns through PI/2 (below the centre) to the other. The rest keep their spacing.
	var start: float = PI if from_left else 0.0
	var turn: float = -PI if from_left else PI
	for i in maxi(moon_count, 1):
		var moon: ClownpieceMoon = moon_scene.instantiate() as ClownpieceMoon
		if moon == null:
			return
		var from_angle: float = start + TAU * float(i) / float(maxi(moon_count, 1))
		moon.setup(playfield, origin + orbit_offset, orbit_radius, from_angle, from_angle + turn,
			fade_in, swing_duration, fade_out, ring_data, count, interval, speed, ring_offset,
			ring_turn_deg)
		layer.add_child(moon)
	# Held for the moons' whole life, so a caster doesn't start anything else meanwhile.
	if playfield.is_inside_tree():
		await playfield.get_tree().create_timer(fade_in + swing_duration + fade_out).timeout
