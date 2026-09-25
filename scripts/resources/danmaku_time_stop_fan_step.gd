class_name DanmakuTimeStopFanStep
extends DanmakuStep

## Sakuya Izayoi's Level 2 & 3 Spellcard: Time Sign "Private Square"
## After a brief beat where nothing seems to happen, time freezes for everything on
## the target's field - bullets, fairies, the boss/Lily White, and the victim's own
## character - while daggers form in an arc above the victim, one group at a time.
## When time resumes, every dagger gently accelerates from rest along its own heading.
##
## Two interchangeable geometries share all of the above (freeze/dim/timing/audio):
## - Lv2 default (`stacked_cross_mode = false`): a single dagger (paired with a small red
##   pellet near its grip) per spoke, all pointing inward, converging on the victim and
##   continuing straight through to the far side.
## - Lv3 (`stacked_cross_mode = true`): a 4-dagger stack per spoke, all spawned on top of
##   each other, then peeling apart along that spoke's own local axes - one outward, one
##   inward (the one that actually threatens the victim), and two tangential (clockwise /
##   counter-clockwise along the circle). Packed together at rest, the cross shape - and
##   the way neighboring spokes' tangential daggers interleave - only becomes visible once
##   they've picked up speed. No pellets in this mode.
##
## Authentic "bug" preserved on purpose: only the target field's own player character
## is ever frozen here. If the opponent is also playing Sakuya, they are immune to the
## time-stop exactly like the caster herself is - a mirror-match quirk straight from 09.

@export_group("Bullet Types")
@export var knife_data: DanmakuBulletData = null
@export var pellet_data: DanmakuBulletData = null

@export_group("Fan Geometry")
@export var dagger_count: int = 10
## ZUN pl02.ecl step: 0.22439948 rad (12.857 deg) * 9 gaps = 115.71 deg total span
@export var arc_span_deg: float = 115.7
## Scaled to 600x960 playfield (2.083x): ZUN F3 = 64.0 * 2.0833 = ~133.3 px
@export var knife_radius: float = 133.0
## Scaled to 600x960 playfield (2.083x): ZUN F6 = 80.0 * 2.0833 = ~166.7 px
@export var pellet_radius: float = 167.0
## Lv3 geometry switch - see class comment above. dagger_count becomes the number of
## 4-dagger stacks (spoke positions), not the total dagger count.
@export var stacked_cross_mode: bool = false
## Lv2 (pl02.ecl sub0): centre the arc on the direction from the victim toward this field
## point (ZUN's helper sits at (0, 224) ECL) and start it half the spoke count back, so the
## spokes run -64.3 to +51.4 degrees around that direction. Off = symmetric about straight up.
@export var aim_arc_at_point: bool = false
@export var arc_aim_point: Vector2 = Vector2(300.0, 466.7)

@export_group("Timing")
## Brief beat where nothing visibly happens before the freeze engages
@export var pre_freeze_delay: float = 0.15
## Delay between each dagger+pellet pair appearing as the fan forms (ZUN: wait(4) = 4 frames = 0.0667s)
@export var delay_between_daggers: float = 4.0 / 60.0
## Lv2 (pl02.ecl sub0): spoke delay is int(40 / (rank x 3 / 4 + 6)) frames instead
@export var zun_rank_delay: bool = false
## How long the target field stays frozen after all 10 spokes finish forming before launching (ZUN: +20 = 20 frames = 0.333s)
## Total freeze duration from first dagger to launch is exactly 60 frames = 1.0 second.
@export var freeze_duration: float = 20.0 / 60.0

@export_group("Kinematics")
## Daggers start at rest and gently accelerate (ZUN F4: (Rank * 0.1 + 1.0) / 60.0 px/frame^2)
## Scaled by 2.083x playfield ratio: 125.0 px/s^2 at Rank 1, up to 325.0 px/s^2 at Rank 16.
@export var accel_r1: float = 125.0
@export var accel_r16: float = 325.0
@export var max_speed_r1: float = 260.0
@export var max_speed_r16: float = 340.0
## When > 0, daggers only accelerate for this long (ZUN's bullet_effects duration, 40 frames),
## so top speed is accel x accel_duration and max_speed_r1 / r16 are ignored
@export var accel_duration: float = 0.0

@export_group("Audio")
@export var freeze_sfx: String = "se_timestop0"
@export var launch_sfx: String = "se_tan00"

@export_group("Visuals")
## Both playfields (not just the victim's) dim a bit while the fan is forming, matching the
## source spellcard - authentic to 09, this is visible on the caster's own side too. Snaps
## on/off instantly (no fade), right at the first dagger's spawn and right as they launch.
@export var dim_alpha: float = 0.35

const DEFAULT_KNIFE: DanmakuBulletData = preload("res://resources/bullets/sakuya_knife.tres")
const DEFAULT_PELLET: DanmakuBulletData = preload("res://resources/bullets/red_pellet.tres")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return

	var player: Player = playfield.get("player")
	var center: Vector2 = player.position if (player and is_instance_valid(player)) else origin

	if pre_freeze_delay > 0.0 and playfield.is_inside_tree():
		await playfield.get_tree().create_timer(pre_freeze_delay).timeout

	if not is_instance_valid(playfield):
		return

	# Re-read the victim's position after the beat - the fan forms around wherever
	# they actually are when the freeze catches them, not where they started.
	player = playfield.get("player")
	if player and is_instance_valid(player):
		center = player.position
	var is_victim_sakuya: bool = player != null and is_instance_valid(player) and player.character_id == "sakuya"

	if not freeze_sfx.is_empty():
		AudioService.play_sfx(freeze_sfx)

	var sibling: Node2D = playfield.get("sibling_playfield")
	playfield.call("set_dim", dim_alpha, 0.0)
	if sibling and is_instance_valid(sibling):
		sibling.call("set_dim", dim_alpha, 0.0)

	var entities_layer: Node2D = playfield.get("entities_layer")
	var bullets_layer: Node2D = playfield.get("bullets_layer")

	playfield.set("fairy_spawning_enabled", false)
	if entities_layer:
		entities_layer.process_mode = Node.PROCESS_MODE_DISABLED
	if player and is_instance_valid(player):
		# Exempt the player node itself from the field-wide freeze so its own
		# is_action_stopped flag (below) is what actually governs it - this is
		# what lets a Sakuya victim keep moving through her own sister's spell.
		player.process_mode = Node.PROCESS_MODE_ALWAYS
		if not is_victim_sakuya:
			player.set_action_stop(true)
	if bullets_layer:
		bullets_layer.process_mode = Node.PROCESS_MODE_DISABLED

	var knife: DanmakuBulletData = knife_data if knife_data else DEFAULT_KNIFE
	var pellet: DanmakuBulletData = pellet_data if pellet_data else DEFAULT_PELLET

	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var max_speed: float = lerpf(max_speed_r1, max_speed_r16, t_rank)
	var current_accel: float = lerpf(accel_r1, accel_r16, t_rank)
	if accel_duration > 0.0:
		max_speed = current_accel * accel_duration
	var dagger_delay: float = delay_between_daggers
	if zun_rank_delay:
		dagger_delay = float(40 / (clampi(rank, 1, 16) * 3 / 4 + 6)) / 60.0

	var span_rad: float = deg_to_rad(arc_span_deg)
	var half_span: float = span_rad * 0.5
	var base_angle: float = -PI / 2.0 - half_span
	var step_angle: float = span_rad / float(max(dagger_count - 1, 1))
	if aim_arc_at_point and arc_aim_point != center:
		base_angle = (arc_aim_point - center).angle() - step_angle * float(dagger_count) * 0.5

	# Held motionless in place by the field-wide freeze above (bullets_layer is disabled,
	# so nothing moves or accelerates until it's re-enabled below) - so daggers can safely
	# spawn staggered one pair at a time here without any of them getting a head start.
	for i in range(dagger_count):
		if not is_instance_valid(playfield):
			return
		var angle: float = base_angle + step_angle * float(i)
		var outward: Vector2 = Vector2(cos(angle), sin(angle))
		var inward: Vector2 = -outward
		if stacked_cross_mode:
			# All 4 daggers of this stack spawn on top of each other at the same spoke
			# point, forming a 4-dagger cross that peels apart along its own local axes.
			var stack_spawn: Vector2 = center + outward * knife_radius
			var tangent: Vector2 = outward.orthogonal()
			var directions: Array[Vector2] = [outward, inward, tangent, -tangent]
			for dir in directions:
				playfield.call("spawn_danmaku_bullet", knife, stack_spawn, DanmakuBullet.MotionMode.LINEAR, dir, 0.0, current_accel, max_speed)
		else:
			var knife_spawn: Vector2 = center + outward * knife_radius
			var pellet_spawn: Vector2 = center + outward * pellet_radius
			playfield.call("spawn_danmaku_bullet", knife, knife_spawn, DanmakuBullet.MotionMode.LINEAR, inward, 0.0, current_accel, max_speed)
			playfield.call("spawn_danmaku_bullet", pellet, pellet_spawn, DanmakuBullet.MotionMode.LINEAR, inward, 0.0, current_accel, max_speed)

		AudioService.play_tan_exclusive()

		if i < dagger_count - 1 and dagger_delay > 0.0 and playfield.is_inside_tree():
			await playfield.get_tree().create_timer(dagger_delay).timeout

	if freeze_duration > 0.0 and is_instance_valid(playfield) and playfield.is_inside_tree():
		await playfield.get_tree().create_timer(freeze_duration).timeout

	if not is_instance_valid(playfield):
		return

	playfield.call("set_dim", 0.0, 0.0)
	if sibling and is_instance_valid(sibling):
		sibling.call("set_dim", 0.0, 0.0)

	if entities_layer:
		entities_layer.process_mode = Node.PROCESS_MODE_INHERIT
	if bullets_layer:
		bullets_layer.process_mode = Node.PROCESS_MODE_INHERIT
	playfield.set("fairy_spawning_enabled", true)
	if player and is_instance_valid(player):
		player.process_mode = Node.PROCESS_MODE_INHERIT
		if not is_victim_sakuya:
			player.set_action_stop(false)

	if not launch_sfx.is_empty():
		AudioService.play_sfx(launch_sfx)
