class_name DanmakuFixedLasersStep
extends DanmakuStep

## Step in a Danmaku timeline that summons sequential vertical laser beams at fixed positions.
## Used by Marisa Boss Attack 5 ("Two Fixed Green Lasers").

@export var laser_scene: PackedScene = null

@export_group("Placement")
## X coordinates for the fixed lasers. Default [150.0, 450.0] splits the 600px playfield at 1/4 and 3/4.
@export var laser_x_positions: Array[float] = [150.0, 450.0]
## Ground anchor Y coordinate at the bottom screen border.
@export var ground_y: float = 950.0
## Tilt angle in degrees (strictly 0.0 for vertical beams).
@export var tilt_angle_deg: float = 0.0

@export_group("Timing & Pulses")
## Delay in seconds between the two lasers firing in each pulse wave.
@export var stagger_interval: float = 0.22
## Delay in seconds between successive pulse waves.
@export var pulse_interval: float = 0.65
## Number of pulse waves fired at Rank 1 (default 1 = single volley).
@export var pulses_rank_1: int = 1
## Number of pulse waves fired at Rank 16 (default 3 = repeats 3 times).
@export var pulses_rank_16: int = 3
## If true, alternates the order of positions on each pulse (e.g. Left->Right then Right->Left).
@export var alternate_order: bool = true

@export_group("ZUN Player Bracket (pl01.ecl sub6)")
## Instead of fixed columns, each laser lands bracket_offset px beside the victim's X at
## that moment, alternating sides. rank / 5 + 3 lasers spread evenly over window_frames.
@export var zun_bracket: bool = false
@export var bracket_offset: float = 133.3
@export var window_frames: int = 120

const DEFAULT_LASER_SCENE: PackedScene = preload("res://scenes/attacks/earth_light_ray_green.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	
	var scene_to_spawn: PackedScene = laser_scene if laser_scene else DEFAULT_LASER_SCENE
	if scene_to_spawn == null:
		return
	
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var pulse_count: int = int(round(lerpf(float(pulses_rank_1), float(pulses_rank_16), t_rank)))
	
	var bullets_layer: Node2D = playfield.get("bullets_layer")
	if bullets_layer == null:
		bullets_layer = playfield.get_node_or_null("%Bullets")
	if bullets_layer == null:
		bullets_layer = playfield
	
	if zun_bracket:
		await _execute_bracket(playfield, scene_to_spawn, bullets_layer, rank)
		return
	
	for p in range(pulse_count):
		if not is_instance_valid(playfield):
			return
		
		# Build active positions list for this pulse
		var positions: Array[float] = []
		if alternate_order and (p % 2 == 1):
			for idx in range(laser_x_positions.size() - 1, -1, -1):
				positions.append(laser_x_positions[idx])
		else:
			positions.append_array(laser_x_positions)
		
		for i in range(positions.size()):
			if not is_instance_valid(playfield):
				return
			
			var spawn_x: float = positions[i]
			var laser_node: Node2D = scene_to_spawn.instantiate()
			
			if laser_node.has_method("setup_tilt"):
				laser_node.call("setup_tilt", tilt_angle_deg, spawn_x)
			else:
				laser_node.position = Vector2(spawn_x, ground_y)
				laser_node.rotation = deg_to_rad(tilt_angle_deg)
			
			bullets_layer.add_child(laser_node)
			
			# Delay between the two lasers in this wave
			if i < positions.size() - 1 and stagger_interval > 0.0:
				if is_instance_valid(playfield) and playfield.is_inside_tree():
					await playfield.get_tree().create_timer(stagger_interval).timeout
		
		# Interval between successive pulses
		if p < pulse_count - 1 and pulse_interval > 0.0:
			if is_instance_valid(playfield) and playfield.is_inside_tree():
				await playfield.get_tree().create_timer(pulse_interval).timeout


func _execute_bracket(playfield: Node2D, scene_to_spawn: PackedScene, bullets_layer: Node2D, rank: int) -> void:
	var count: int = clampi(rank, 1, 16) / 5 + 3
	var delay: float = float(window_frames / count) / 60.0
	for i in range(count):
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return
		# ZUN counts down: an even remaining count goes right of the player, odd goes left
		var side: float = 1.0 if (count - i) % 2 == 0 else -1.0
		var player_x: float = 300.0
		var player_node: Node2D = playfield.get("player")
		if player_node and is_instance_valid(player_node):
			player_x = player_node.position.x
		var spawn_x: float = player_x + side * bracket_offset
		var laser_node: Node2D = scene_to_spawn.instantiate()
		if laser_node.has_method("setup_tilt"):
			laser_node.call("setup_tilt", 0.0, spawn_x)
		else:
			laser_node.position = Vector2(spawn_x, ground_y)
		bullets_layer.add_child(laser_node)
		if playfield.has_method("register_custom_hazard"):
			playfield.register_custom_hazard(laser_node)
		if i < count - 1:
			await playfield.get_tree().create_timer(delay).timeout
