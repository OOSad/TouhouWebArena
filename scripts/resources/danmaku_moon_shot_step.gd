class_name DanmakuMoonShotStep
extends DanmakuStep

## Reisen Udongein Inaba's Triple Moon Shot.
##
## Three large light motes leave the boss and sink to just below her, one for each of the
## three pale moons they become. The moons drift down the field on a narrow fan, each
## carrying a proximity fuse.
##
## The tell is not decoration: for its whole 1.2s nothing exists that can hurt anybody, and
## each moon is born where its mote came to rest rather than at the boss herself. The fuse is set wider than the crater it leaves, so a mote can never reach
## the player: it always bursts short, and what it leaves behind is a stationary no-go zone.
## When all three go off they overlap into a wall across most of the field for about a second,
## and the kill comes from flying into one afterwards rather than from the shot itself.
##
## The fuse is the only trigger. A moon that never closes on anybody does not burst on a timer
## or at a depth - it simply leaves the screen and is culled - so a badly placed volley can
## leave two craters, one, or none at all.
##
## The fan points straight down, not at the player. Rank 16 footage settles this: the boss
## fired from well right of centre and the fan still came down vertically rather than leaning
## toward the player parked at the bottom middle.
##
## Rank does not touch this attack. Speed, fan angle, fuse distance and crater size all
## measured within noise of each other between Rank 1 and Rank 16, so nothing here scales and
## the difficulty comes from what the boss is doing alongside it.

@export_group("Scenes")
@export var mote_scene: PackedScene = preload("res://scenes/attacks/reisen_moon_mote.tscn")
@export var blast_scene: PackedScene = preload("res://scenes/attacks/reisen_moon_blast.tscn")
@export var charge_scene: PackedScene = preload("res://scenes/effects/reisen_moon_charge.tscn")

@export_group("Charge")
## The tell: one light mote per moon leaves the boss and sinks to that moon's spot before
## any moon exists. Measured at ~1.2s in the reference - the motes appear at 2.40s and the
## moons have resolved out of them by 3.60s. Set to 0 to have the moons appear instantly.
@export var charge_duration: float = 1.20

@export_group("Fan")
## Moons per volley. Three in the original, hence the name.
@export var mote_count: int = 3
## Half-angle of the fan off vertical. The original measures ~17 degrees each side.
@export var fan_half_angle_deg: float = 17.0
@export var mote_speed: float = 250.0
## Gap between neighbouring moons at the moment they resolve. They are not born from a
## point: the reference shows them appearing in a row already touching, one diameter apart,
## and only then diverging. Reading the fan as converging to a point puts the spawn nearly
## 120px too high and throws off every fuse timing downstream.
@export var initial_spacing: float = 40.0
## Where the moons resolve, relative to the cast origin. They form below her feet rather
## than on her: in the reference the flare sinks about a sprite height before condensing.
@export var spawn_offset: Vector2 = Vector2(0.0, 130.0)

@export_group("Fuse")
## Distance from the player at which a moon bursts: the original's 133 reference px, scaled.
## Must stay comfortably above the crater radius, or the attack stops being survivable by
## standing still, which is the point of it. At 181 against a 126px crater the blast edge
## lands ~55px clear, matching the reference.
##
## How many moons connect depends on where the boss is standing, and that is the attack's
## texture rather than a problem to engineer away. From low in her roam band a centred cast
## lands all three; from the top of it only the middle one does, because the fan has longer
## to spread and the outer two pass the player too wide. Any off-centre cast lands two, since
## one moon always runs away from the player. Clearance holds at 51-55px throughout.
##
## Her roam band is set from the reference rather than shared with the other bosses. See
## `reisen_boss.tres`.
@export var fuse_radius: float = 181.0

@export_group("Crater")
## Radius the crater opens at and tops out at, ~0.04 and ~0.21 field-widths in the original.
@export var blast_radius_start: float = 24.0
@export var blast_radius_end: float = 126.0
@export var blast_duration: float = 1.05

@export_group("Audio")
@export var sfx: String = ""

func execute(playfield: Node2D, origin: Vector2, _rank: int) -> void:
	# Nothing here is rolled at random, so the sync RNG is taken only to clear it for the
	# next step in the timeline.
	take_sync_rng()

	if playfield == null or not is_instance_valid(playfield):
		return
	if mote_scene == null:
		return

	var layer: Node2D = playfield.get("bullets_layer")
	if layer == null:
		layer = playfield.get_node_or_null("%Bullets")
	if layer == null:
		layer = playfield

	if not sfx.is_empty():
		AudioService.play_sfx(sfx)

	var spawn_pos: Vector2 = origin + spawn_offset
	var mid: float = float(mote_count - 1) * 0.5

	# Each moon gets its own light mote, which leaves the boss and drifts to exactly where
	# that moon will appear. The motes are cast where she is standing and then travel
	# independently, so she is free to hop away while they sink.
	if charge_duration > 0.0 and charge_scene:
		for i in range(mote_count):
			var charge: Node2D = charge_scene.instantiate()
			layer.add_child(charge)
			charge.setup(origin, (spawn_pos + _lateral(i, mid)) - origin, charge_duration)
		await playfield.get_tree().create_timer(charge_duration).timeout
		# A round can end, or the field be torn down, while the tell is still playing.
		if not is_instance_valid(playfield) or not is_instance_valid(layer):
			return

	for i in range(mote_count):
		var mote: Node2D = mote_scene.instantiate()
		mote.position = spawn_pos + _lateral(i, mid)
		mote.direction = Vector2.DOWN.rotated(deg_to_rad(_fan_angle_deg(i)))
		mote.speed = mote_speed
		mote.fuse_radius = fuse_radius
		mote.blast_scene = blast_scene
		mote.blast_radius_start = blast_radius_start
		mote.blast_radius_end = blast_radius_end
		mote.blast_duration = blast_duration
		mote.playfield = playfield
		layer.add_child(mote)

## Where moon `index` sits at the moment it resolves, relative to the middle of the volley.
##
## Taken from the moon's own fan direction rather than from its index, so a moon always
## starts on the side it is heading for. Deriving the two separately once had them disagree
## in sign, which sent each outer moon across the middle instead of away from it: the fan
## visibly crossed over itself and the outer fuses fired near-together.
func _lateral(index: int, mid: float) -> Vector2:
	var dir := Vector2.DOWN.rotated(deg_to_rad(_fan_angle_deg(index)))
	return Vector2(signf(dir.x) * absf(float(index) - mid) * initial_spacing, 0.0)

## Spreads the volley evenly across the fan, centred on vertical. A single moon goes straight
## down; three give -17, 0, +17.
func _fan_angle_deg(index: int) -> float:
	if mote_count <= 1:
		return 0.0
	var t: float = float(index) / float(mote_count - 1)
	return lerpf(-fan_half_angle_deg, fan_half_angle_deg, t)
