class_name YoumuChargeAttack
extends Node2D

## Coordinator for Youmu Konpaku's Level 1 Charge Attack.
## Spawns Youmu's signature forward cutting sword arc (Danmeiken).

const SLASH_SCENE: PackedScene = preload("res://scenes/attacks/youmu_slash_wave.tscn")
const YoumuSlashWaveClass = preload("res://scenes/attacks/youmu_slash_wave.gd")

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
	
	var slash: YoumuSlashWaveClass = SLASH_SCENE.instantiate()
	if target_parent:
		target_parent.add_child(slash)
	else:
		add_child(slash)
	
	slash.setup(player, playfield)
	
	queue_free()
