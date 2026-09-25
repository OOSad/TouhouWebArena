class_name ReisenChargeAttack
extends Node2D

## Coordinator for Reisen Udongein Inaba's Level 1 Charge Attack.
##
## Unlike the other characters' charge attacks, which scatter several projectiles, hers
## is a single oversized cartridge fired straight ahead. All the behaviour lives on
## ReisenChargeBullet; this just spawns one into the playfield's bullet layer.

const BULLET_SCENE: PackedScene = preload("res://scenes/attacks/reisen_charge_bullet.tscn")

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

	var bullet: ReisenChargeBullet = BULLET_SCENE.instantiate()
	target_parent.add_child(bullet)
	bullet.setup(player, playfield, Vector2.UP)

	queue_free()
