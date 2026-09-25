class_name MarisaChargeAttack
extends Node2D

# Coordinator for Marisa Kirisame's Level 1 Charge Attack.
# Spawns an Illusion Laser anchored to Marisa.

const LASER_SCENE: PackedScene = preload("res://scenes/attacks/illusion_laser.tscn")
const IllusionLaserClass = preload("res://scenes/attacks/illusion_laser.gd")

func setup(player: Player, playfield: Node2D) -> void:
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
	
	var laser: IllusionLaserClass = LASER_SCENE.instantiate()
	if target_parent:
		target_parent.add_child(laser)
	else:
		add_child(laser)
	
	laser.setup(player, playfield)
	
	queue_free()

