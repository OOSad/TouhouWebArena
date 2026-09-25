class_name YuukaChargeAttack
extends Node2D

## Coordinator for Yuuka Kazami's Level 1 Charge Attack: Beauty of Nature
## Discharges 30 spinning needle-blossom petals in 6 waves of 5 petals each.
## Authentic parameters extracted directly from pl09.sht:
## - 6 staggered waves fired at 0f, 5f, 10f, 15f, 20f, 25f (5-frame cadence = ~0.083s)
## - Angles alternate between 3 spread tiers:
##     Tier 1 (Waves 0 & 3): -120°, -105°, -90°, -75°, -60° (step 15°)
##     Tier 2 (Waves 1 & 4): -130°, -110°, -90°, -70°, -50° (step 20°)
##     Tier 3 (Waves 2 & 5): -138°, -114°, -90°, -66°, -42° (step 24°)
## - Spawns at player position + Vector2(0, -16)

const PETAL_SCENE: PackedScene = preload("res://scenes/attacks/yuuka_charge_petal.tscn")
const YuukaChargePetalClass = preload("res://scenes/attacks/yuuka_charge_petal.gd")

const WAVE_ANGLES: Array[Array] = [
	[-120.0, -105.0, -90.0, -75.0, -60.0],
	[-130.0, -110.0, -90.0, -70.0, -50.0],
	[-138.0, -114.0, -90.0, -66.0, -42.0],
	[-120.0, -105.0, -90.0, -75.0, -60.0],
	[-130.0, -110.0, -90.0, -70.0, -50.0],
	[-138.0, -114.0, -90.0, -66.0, -42.0],
]

const FRAME_INTERVAL: float = 5.0 / 60.0 # 5 frames per wave (~0.0833s)
const SPAWN_OFFSET_Y: float = -16.0

func setup(player: Node2D, playfield: Node2D) -> void:
	var target_parent: Node = null
	if playfield:
		if playfield.has_node("%Bullets"):
			target_parent = playfield.get_node("%Bullets")
		elif playfield.has_node("Bullets"):
			target_parent = playfield.get_node("Bullets")
		else:
			target_parent = playfield
	elif player:
		target_parent = player.get_parent()
	
	if target_parent == null:
		target_parent = get_parent()

	# The executor never parents coordinators that define setup(), so join the
	# tree ourselves; the staggered waves need get_tree() timers to run.
	if not is_inside_tree() and is_instance_valid(target_parent):
		target_parent.add_child(self)

	# Execute waves asynchronously over the authentic 25-frame timeline
	_spawn_all_waves(player, playfield, target_parent)

func _spawn_all_waves(player: Node2D, playfield: Node2D, target_parent: Node) -> void:
	for wave_idx in range(WAVE_ANGLES.size()):
		if not is_instance_valid(player) or not is_inside_tree():
			break
		
		var angles: Array = WAVE_ANGLES[wave_idx]
		var spawn_origin: Vector2 = player.global_position + Vector2(0.0, SPAWN_OFFSET_Y)
		
		for deg in angles:
			var dir := Vector2.from_angle(deg_to_rad(deg))
			var petal: YuukaChargePetalClass = PETAL_SCENE.instantiate()
			if is_instance_valid(target_parent):
				target_parent.add_child(petal)
			else:
				add_child(petal)
			petal.setup(player, playfield, dir, spawn_origin)
		
		AudioService.play_sfx("se_plst00")
		
		if wave_idx < WAVE_ANGLES.size() - 1:
			await get_tree().create_timer(FRAME_INTERVAL).timeout
	
	queue_free()

