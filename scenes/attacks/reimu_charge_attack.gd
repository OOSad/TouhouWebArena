class_name ReimuChargeAttack
extends Node2D

# Coordinator for Reimu Hakurei's Level 1 Charge Attack.
# Spawns 4 Hakurei Amulets in a staggered cluster in front of Reimu.

const AMULET_SCENE: PackedScene = preload("res://scenes/attacks/hakurei_amulet.tscn")
const HakureiAmuletClass = preload("res://scenes/attacks/hakurei_amulet.gd")

const OFFSETS: Array[Vector2] = [
	Vector2(-32.0, -28.0),
	Vector2(-12.0, -48.0),
	Vector2(12.0, -48.0),
	Vector2(32.0, -28.0)
]

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
	
	for offset in OFFSETS:
		var amulet: HakureiAmuletClass = AMULET_SCENE.instantiate()
		if target_parent:
			target_parent.add_child(amulet)
		else:
			add_child(amulet)
		amulet.setup(player, playfield, offset)
	
	# Clean up the coordinator
	queue_free()
