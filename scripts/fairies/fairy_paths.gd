class_name FairyPaths
extends RefCounted

## Fairy train formations, built from PoFV's enemy.ecl (th09_depot/ecl/enemy.ecl.txt, timeline0-7).
## A fairy flies straight, turns at a constant PI/160 rad per frame, then flies straight again.
## Versus matches use each timeline both as written and mirrored left-right, so both are built.
## Checked against match footage: every wave in a 2-minute recording fit one of these 16.

const ECL_SCALE: float = 600.0 / 288.0 # px per ECL unit, same ratio the danmaku steps use
const ECL_FPS: float = 60.0
const TURN_RATE: float = PI / 160.0 # rad per ECL frame
const PLAYFIELD_SIZE: Vector2 = Vector2(600.0, 960.0)
const EXIT_MARGIN: float = 64.0 # keep the curve going until the fairy is fully off-screen
const MAX_FRAMES: int = 1200

## Fairy tiers, smallest to largest. The ECL picks them per fairy with anm_set_poses_ex(0/5/10/15),
## which in match footage are the blue, red and green small fairies and the sunflower great fairy.
enum Tier { BLUE, RED, GREEN, GREAT }

class Formation:
	var curve: Curve2D
	var speed: float # px/s along the curve
	var spacing: float # px between neighbouring fairies along the curve
	var lineup: Array[Tier] # one entry per fairy, head of the train first

const TRAIN_EDGES: Array[Tier] = [Tier.GREAT, Tier.GREEN, Tier.RED, Tier.BLUE, Tier.BLUE, Tier.BLUE, Tier.BLUE, Tier.RED, Tier.GREEN, Tier.GREAT]
const TRAIN_HOOK: Array[Tier] = [Tier.BLUE, Tier.BLUE, Tier.RED, Tier.RED, Tier.GREEN, Tier.GREAT]
const TRAIN_RISE: Array[Tier] = [Tier.RED, Tier.BLUE, Tier.RED, Tier.BLUE, Tier.RED, Tier.BLUE]
const TRAIN_SWEEP: Array[Tier] = [Tier.BLUE, Tier.RED, Tier.BLUE, Tier.RED, Tier.GREEN, Tier.GREAT]

# ECL spawn x, spawn y, heading (rad), speed (units/frame), frames between fairies,
# turn starts at frame, turn lasts frames, turn direction (+1 clockwise on screen), lineup.
const TIMELINES: Array = [
	[-64.0, -16.0, PI / 2.0, 2.0, 20, 60, 120, -1.0, TRAIN_HOOK], # timeline0: down, hook up and out
	[-128.0, -16.0, PI / 2.0, 2.0, 20, 60, 120, -1.0, TRAIN_HOOK], # timeline1: same, nearer the wall
	[-64.0, -16.0, PI / 4.0, 2.5, 15, 60, 120, 1.0, TRAIN_EDGES], # timeline2: diagonal, curl back across the bottom
	[-32.0, -16.0, PI / 4.0, 2.5, 15, 60, 120, 1.0, TRAIN_EDGES], # timeline3: same, from nearer the centre
	[-128.0, 464.0, -PI / 2.0, 2.0, 15, 60, 120, 1.0, TRAIN_RISE], # timeline4: up from below, arc over and dive out
	[-160.0, 128.0, 0.0, 2.0, 15, 0, 0, 0.0, TRAIN_SWEEP], # timeline5: straight across, high
	[-160.0, 192.0, 0.0, 2.0, 15, 0, 0, 0.0, TRAIN_SWEEP], # timeline6: straight across, middle
	[-160.0, 256.0, 0.0, 2.0, 15, 0, 0, 0.0, TRAIN_SWEEP], # timeline7: straight across, low
]

static var _cached: Array[Formation] = []

static func get_all_formations() -> Array[Formation]:
	if _cached.is_empty():
		for timeline: Array in TIMELINES:
			_cached.append(_build(timeline, false))
			_cached.append(_build(timeline, true))
	return _cached

static func _build(timeline: Array, mirrored: bool) -> Formation:
	var x: float = timeline[0]
	var y: float = timeline[1]
	var heading: float = timeline[2]
	var step: float = timeline[3]
	var turn_start: int = timeline[5]
	var turn_end: int = turn_start + int(timeline[6])
	var turn_dir: float = timeline[7]
	if mirrored:
		x = -x
		heading = PI - heading
		turn_dir = -turn_dir

	var bounds := Rect2(Vector2.ZERO, PLAYFIELD_SIZE)
	var exit_bounds := bounds.grow(EXIT_MARGIN)
	var curve := Curve2D.new()
	var has_entered: bool = false
	for frame in MAX_FRAMES:
		var pos := Vector2(PLAYFIELD_SIZE.x * 0.5 + x * ECL_SCALE, y * ECL_SCALE)
		curve.add_point(pos)
		if bounds.has_point(pos):
			has_entered = true
		elif has_entered and not exit_bounds.has_point(pos):
			break
		if frame >= turn_start and frame < turn_end:
			heading += TURN_RATE * turn_dir
		x += cos(heading) * step
		y += sin(heading) * step

	var formation := Formation.new()
	formation.curve = curve
	formation.speed = step * ECL_FPS * ECL_SCALE
	formation.spacing = step * float(timeline[4]) * ECL_SCALE
	formation.lineup = timeline[8]
	return formation
