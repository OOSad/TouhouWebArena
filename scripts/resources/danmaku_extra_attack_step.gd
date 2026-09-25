class_name DanmakuExtraAttackStep
extends DanmakuStep

## Step in a Danmaku timeline that summons a sequential barrage of Extra Attacks.
## Used by Bosses (e.g. Reimu's Yin-Yang Orbs, Marisa's Earth Light Rays).

enum SpawnLocation {
	FROM_BOSS,
	ACROSS_TOP,
	GROUND_RAYS
}

@export var character_id: String = "reimu"
@export var extra_attack_scene: PackedScene = null

@export_group("Rank Scaling")
@export var count_min: int = 4
@export var count_max: int = 8

@export_group("Timing & Placement")
@export var delay_between_spawns: float = 0.22
@export var spawn_location: SpawnLocation = SpawnLocation.FROM_BOSS
@export var horizontal_spread: float = 120.0
@export var initial_upward_speed: float = 390.0

@export_group("Ray Tilt Settings")
@export var ground_spawn_min_x: float = 75.0
@export var ground_spawn_max_x: float = 525.0
@export var tilt_angle_min_deg: float = -3.25
@export var tilt_angle_max_deg: float = 3.25
@export var alternate_tilt: bool = false

@export_group("ZUN Ground Sweep (pl01.ecl sub5)")
## GROUND_RAYS only. Rays step inward by (64 - 2 x rank) ECL units from a random start,
## alternating between that point and its mirror, 288 / step rays spaced 100 / count frames
## apart, each tilted up to +-2.8125 degrees. Overrides count_*, delay and the tilt range.
@export var zun_sweep: bool = false
## ACROSS_TOP only (pl05.ecl sub4 / sub5): rank / 4 + 6 attacks, 10 frames apart, in even
## columns x = k x 600 / n swept from a synced random side. Overrides count_* and the delay.
@export var zun_top_sweep: bool = false

@export_group("Audio")
@export var sfx: String = ""

const YIN_YANG_ORB_SCENE: PackedScene = preload("res://scenes/attacks/yin_yang_orb.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	# Taken before anything can await. An Extra Attack is big, slow and telegraphed,
	# so where each one lands is attack identity and has to match on both clients.
	var rng: RandomNumberGenerator = take_sync_rng()
	
	if playfield == null or not is_instance_valid(playfield):
		return
	
	var scene_to_spawn: PackedScene = extra_attack_scene
	if scene_to_spawn == null:
		var char_data := CharacterData.get_character(character_id)
		if char_data and char_data.extra_attack_scene:
			scene_to_spawn = char_data.extra_attack_scene
		else:
			scene_to_spawn = YIN_YANG_ORB_SCENE
	
	var t: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var count: int = int(round(lerpf(float(count_min), float(count_max), t)))
	var spawn_delay: float = delay_between_spawns
	var sweep_step: float = 0.0
	var sweep_x: float = 0.0
	if zun_sweep:
		sweep_step = 64.0 - 2.0 * float(clampi(rank, 1, 16))
		count = 288 / int(sweep_step)
		spawn_delay = float(100 / count) / 60.0
		sweep_x = (rng.randf() if rng else randf()) * sweep_step - 144.0
	
	var sweep_from_right: bool = false
	if zun_top_sweep:
		count = clampi(rank, 1, 16) / 4 + 6
		spawn_delay = 10.0 / 60.0
		sweep_from_right = (rng.randi() if rng else randi()) % 2 == 1
	
	var bullets_layer: Node2D = playfield.get("bullets_layer")
	if bullets_layer == null:
		bullets_layer = playfield.get_node_or_null("%Bullets")
	if bullets_layer == null:
		bullets_layer = playfield
	
	var last_ray_x: float = -999.0
	for i in range(count):
		if not is_instance_valid(playfield):
			return
		
		var spawn_pos: Vector2
		match spawn_location:
			SpawnLocation.FROM_BOSS:
				var t_spread: float = (float(i) / maxf(1.0, float(count - 1))) - 0.5 if count > 1 else 0.0
				var offset_x: float = t_spread * horizontal_spread + (rng.randf_range(-15.0, 15.0) if rng else randf_range(-15.0, 15.0))
				var offset_y: float = rng.randf_range(-10.0, 10.0) if rng else randf_range(-10.0, 10.0)
				spawn_pos = origin + Vector2(offset_x, offset_y)
				spawn_pos.x = clampf(spawn_pos.x, 70.0, 530.0)
			SpawnLocation.ACROSS_TOP:
				var t_spread: float = float(i) / maxf(1.0, float(count - 1)) if count > 1 else 0.5
				var base_x: float = lerpf(100.0, 500.0, t_spread) + (rng.randf_range(-30.0, 30.0) if rng else randf_range(-30.0, 30.0))
				var top_y: float = rng.randf_range(70.0, 120.0) if rng else randf_range(70.0, 120.0)
				spawn_pos = Vector2(clampf(base_x, 70.0, 530.0), top_y)
				if zun_top_sweep:
					var column: float = float(i) * 600.0 / float(count)
					spawn_pos.x = 600.0 - column if sweep_from_right else column
			SpawnLocation.GROUND_RAYS:
				if zun_sweep:
					# ZUN counts down, so the mirrored side is the one where the remaining count is even
					var ecl_x: float = -sweep_x if (count - i) % 2 == 0 else sweep_x
					sweep_x += sweep_step
					spawn_pos = Vector2(300.0 + ecl_x * 600.0 / 288.0, 950.0)
				else:
					# Random position across the bottom of the opponent's playfield
					var rand_x: float = rng.randf_range(ground_spawn_min_x, ground_spawn_max_x) if rng else randf_range(ground_spawn_min_x, ground_spawn_max_x)
					for _attempt in range(5):
						if absf(rand_x - last_ray_x) >= 45.0:
							break
						# The retry draws too, so a seeded run has to repeat them to stay in step.
						rand_x = rng.randf_range(ground_spawn_min_x, ground_spawn_max_x) if rng else randf_range(ground_spawn_min_x, ground_spawn_max_x)
					last_ray_x = rand_x
					spawn_pos = Vector2(rand_x, 950.0)
		
		var attack_node: Node2D = scene_to_spawn.instantiate()
		
		if spawn_location == SpawnLocation.GROUND_RAYS:
			var tilt_angle: float = 0.0
			if zun_sweep:
				tilt_angle = (rng.randf_range(-1.0, 1.0) if rng else randf_range(-1.0, 1.0)) * 2.8125
			elif alternate_tilt:
				var sign_val: float = -1.0 if (i % 2 == 0) else 1.0
				tilt_angle = sign_val * (rng.randf_range(0.0, tilt_angle_max_deg) if rng else randf_range(0.0, tilt_angle_max_deg))
			else:
				tilt_angle = rng.randf_range(tilt_angle_min_deg, tilt_angle_max_deg) if rng else randf_range(tilt_angle_min_deg, tilt_angle_max_deg)
			
			if attack_node.has_method("setup_tilt"):
				attack_node.call("setup_tilt", tilt_angle, spawn_pos.x)
			else:
				attack_node.position = spawn_pos
				attack_node.rotation = deg_to_rad(tilt_angle)
		else:
			attack_node.position = spawn_pos
			# An attack picks its own toss in _ready(), which runs on add_child() below, so
			# there is nothing useful to set on velocity from out here - a removed
			# alternate_toss_direction flag tried to and was silently inert for it.
			if initial_upward_speed > 0.0 and "initial_upward_speed" in attack_node:
				attack_node.initial_upward_speed = initial_upward_speed
		
		if attack_node.has_method("setup_rank"):
			attack_node.call("setup_rank", rank)
		
		# An attack that rolls its own trajectory (the Yin-Yang Orb's opening toss) would
		# otherwise still diverge, even with its spawn point matched.
		if attack_node.has_method("setup_variation"):
			var variation: int = rng.randi() if rng else randi()
			attack_node.call("setup_variation", variation)
		
		if not sfx.is_empty() and "spawn_sfx" in attack_node:
			attack_node.spawn_sfx = sfx
		
		bullets_layer.add_child(attack_node)
		if playfield.has_method("register_custom_hazard"):
			playfield.register_custom_hazard(attack_node)
		if not sfx.is_empty() and not ("spawn_sfx" in attack_node):
			AudioService.play_sfx(sfx)
		
		if i < count - 1 and spawn_delay > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(spawn_delay).timeout
