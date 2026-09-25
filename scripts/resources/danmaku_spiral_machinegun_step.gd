class_name DanmakuSpiralMachinegunStep
extends DanmakuStep

## Reisen Udongein Inaba's Level 4 Boss Attack 3, informally "Purple Bullet Spiral
## Machinegun".
##
## One barrel, pointed out of the boss's hitbox, firing as fast as it can while it turns
## clockwise. Nothing else happens: the bullets travel straight out along whatever direction
## the barrel had when they left it, so the whole figure is just a record of where the barrel
## has been pointing. What the player sees is a single arm of purple bullets winding outward.
##
## It is the simplest attack in her set and the fastest. The bullets are quicker than anything
## else she throws, and because the stream is continuous there is no wave to wait out: the only
## safe move is to cross the arm between two bullets, and the arm sweeps a full turn roughly
## every 1.2 seconds, so the crossing has to be taken again and again.
##
## Rank does not change the spin, and barely changes the spacing. What it changes is speed:
## Rank 16 bullets travel 1.6x faster, which stretches the spiral to nearly twice the pitch,
## so fewer turns sit on the field at once but each one crosses it in well under a second.
## More bullets reach the player per second at Rank 16 even though the arm itself is no
## denser, which is what makes it read as a thicker stream.
##
## Measured off reisen_level4_rank1.mp4 (cast at ~14.6s) and reisen_level4_rank16.mp4 (cast at
## ~2.2s), by polar-unwrapping each frame about the boss and reading the arm's angle against
## time. See DANMAKU_CATALOG.md for what that method could and could not settle.

@export_group("Bullet Type")
@export var bullet_data: DanmakuBulletData = null

@export_group("Barrel")
## Where the barrel sits relative to the cast origin. The bullets come out of the boss's
## hitbox in the original, so this stays at zero.
@export var muzzle_offset: Vector2 = Vector2.ZERO
## How fast the barrel turns, in degrees per second. Positive is clockwise on screen, which
## is the direction the original turns. Measured at both ranks by tracking the arm's angle at
## a fixed radius: 1.18s per revolution in the Rank 1 clip and 1.18s in the Rank 16 one, so
## this is deliberately not rank-scaled.
@export var spin_rate_deg: float = 300.0
## Seconds the barrel fires for. About 1.8 turns at the default spin.
@export var duration: float = 2.20

@export_group("Rate & Rank Scaling")
## Seconds between shots at Rank 1. With the default spin that puts a bullet every 5.7°,
## which is what the reference measures at a matched radius.
@export var fire_interval_min: float = 0.019
## Seconds between shots at Rank 16. Only slightly shorter, and deliberately so. The measured
## Rank 16 spacing is 5.4°, near enough to Rank 1 to be inside the reading error, so the arm
## is barely denser; this leans on the tight side of that measurement rather than past it.
##
## The floor on this is geometric, and it depends on range. The gap between two neighbouring
## bullets is the arc `deg_to_rad(spin_rate_deg * interval) * distance`, minus the bullet's
## own 13px width. At the range this card is normally met, with the player near the bottom
## and the boss at the top of her band (~550px), 4.8° leaves 33px of daylight and there is
## room to spare. Up close it runs out fast: at 200px the same gap is 4px, which a 6px
## hurtbox cannot pass. Anything below about 0.014 closes the near field entirely and turns
## flying at the boss from risky into impossible.
@export var fire_interval_max: float = 0.016
## Bullet speed at Rank 1, px/s. The fastest thing in her set.
@export var speed_min: float = 490.0
## Bullet speed at Rank 16, px/s. Read off the spiral's pitch, which is 1.6x wider at Rank 16
## against an identical spin.
@export var speed_max: float = 790.0

@export_group("Visuals")
## Red flash at the muzzle, one per bullet. Kept small and short on purpose: at ~53 shots a
## second even a 0.18s flash keeps about ten alive at once, and they are additive fill sitting
## on top of each other at the barrel. 0 disables it.
@export var spawn_flash_scale: float = 1.7

const DEFAULT_BULLET: DanmakuBulletData = preload("res://resources/bullets/reisen_spiral_bullet.tres")
const SPAWN_FLASH_SCENE: PackedScene = preload("res://scenes/effects/spawn_flash.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	# Claimed before the first await: which way the barrel starts pointing decides where the
	# stream is, so it is attack identity and must match on both netplay clients.
	var rng: RandomNumberGenerator = take_sync_rng()

	if playfield == null or not is_instance_valid(playfield):
		return
	if not playfield.has_method("spawn_danmaku_bullet"):
		return
	if not playfield.is_inside_tree():
		return

	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_BULLET
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var interval: float = maxf(lerpf(fire_interval_min, fire_interval_max, t_rank), 0.001)
	var speed: float = lerpf(speed_min, speed_max, t_rank)
	var spin: float = deg_to_rad(spin_rate_deg)

	var muzzle: Vector2 = origin + muzzle_offset
	var start_angle: float = rng.randf_range(0.0, TAU) if rng else randf_range(0.0, TAU)

	# Resolved once for the whole cast rather than per shot: this runs ~115 times at Rank 1.
	var effects_layer: Node2D = null
	if spawn_flash_scale > 0.0:
		effects_layer = playfield.get("effects_layer")
		if effects_layer == null:
			effects_layer = playfield.get_node_or_null("%Effects")
		if effects_layer == null:
			effects_layer = playfield

	var tree: SceneTree = playfield.get_tree()
	var elapsed: float = 0.0
	var shot: int = 0

	while elapsed < duration:
		if not is_instance_valid(playfield):
			return

		# Every shot whose turn has come since the last frame is fired now. At these rates
		# that is two or three per frame, and firing them all at the frame's angle would
		# staircase the spiral into visible clumps. Each one is instead placed at the angle
		# the barrel held at its own moment, and pushed out along that angle by however far
		# it would already have flown, so the arm comes out smooth no matter the frame rate.
		while float(shot) * interval <= elapsed:
			var shot_time: float = float(shot) * interval
			var dir := Vector2.from_angle(start_angle + spin * shot_time)
			var spawn_pos: Vector2 = muzzle + dir * (speed * (elapsed - shot_time))
			playfield.spawn_danmaku_bullet(
				data,
				spawn_pos,
				DanmakuBullet.MotionMode.LINEAR,
				dir,
				speed
			)

			# On the bullet's own spawn point rather than on the muzzle. The two are only a
			# few pixels apart - the sub-frame push is at most speed * interval, about 9px -
			# but that is enough to smear the flashes along the barrel's current heading
			# instead of stacking every one of them on a single pixel.
			if effects_layer != null and is_instance_valid(effects_layer):
				var flash: Node2D = SPAWN_FLASH_SCENE.instantiate()
				flash.call("setup", spawn_pos, spawn_flash_scale)
				effects_layer.add_child(flash)
			# One se_tan00 per bullet, on the dedicated single-voice channel: each shot cuts
			# off the one before it rather than stacking, which is what makes ~53 rounds a
			# second read as a machinegun instead of a wall of mush. Same channel and the
			# same reason as Sakuya's Time Stop Fan dagger formation.
			#
			# It has to be this rather than the general pool: se_tan00 is throttled at 30ms
			# there, so two thirds of these would simply be dropped, and the ones that did
			# play would pile up across twelve round-robin voices.
			AudioService.play_tan_exclusive()
			shot += 1

		await tree.process_frame
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return
		# Node delta rather than wall clock, so the barrel slows with everything else when
		# the game's time scale is pulled about (action stop, pause).
		elapsed += playfield.get_process_delta_time()
