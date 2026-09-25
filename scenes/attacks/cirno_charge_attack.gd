class_name CirnoChargeAttack
extends Node2D

## Coordinator for Cirno's Level 1 Charge Attack: Freeze Blade (フリーズブレード)
## Discharges 28 crystalline icicle blades in a 360-degree radial ring around Cirno.

const BLADE_SCENE: PackedScene = preload("res://scenes/attacks/cirno_freeze_blade.tscn")
const CirnoFreezeBladeClass = preload("res://scenes/attacks/cirno_freeze_blade.gd")

const ICICLE_COUNT: int = 28
const SPAWN_RADIUS: float = 24.0

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
	
	var player_pos: Vector2 = player.global_position if is_instance_valid(player) else global_position
	
	for i in range(ICICLE_COUNT):
		var angle: float = (float(i) + 0.5) * (TAU / float(ICICLE_COUNT))
		var dir: Vector2 = Vector2.from_angle(angle)
		var spawn_pos: Vector2 = player_pos + dir * SPAWN_RADIUS
		
		var blade: CirnoFreezeBladeClass = BLADE_SCENE.instantiate()
		if target_parent:
			target_parent.add_child(blade)
		else:
			add_child(blade)
		blade.setup(player, playfield, dir, angle, spawn_pos)
	
	queue_free()

