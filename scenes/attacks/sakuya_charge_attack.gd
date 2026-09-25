class_name SakuyaChargeAttack
extends Node2D

# Coordinator for Sakuya Izayoi's Level 1 Charge Attack: Silver Knife.
# Summons 8 daggers clustered at Sakuya's hitbox, spinning in place before
# launching in a rapid-fire flurry toward the nearest enemy.

const KNIFE_SCENE: PackedScene = preload("res://scenes/attacks/sakuya_knife.tscn")
const SakuyaKnifeClass = preload("res://scenes/attacks/sakuya_knife.gd")

const KNIFE_COUNT: int = 8
const CLUSTER_RADIUS: float = 10.0
const LAUNCH_STAGGER: float = 0.03

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

	for i in range(KNIFE_COUNT):
		var angle: float = TAU * float(i) / float(KNIFE_COUNT)
		var offset: Vector2 = Vector2(cos(angle), sin(angle)) * CLUSTER_RADIUS

		var knife: SakuyaKnifeClass = KNIFE_SCENE.instantiate()
		if target_parent:
			target_parent.add_child(knife)
		else:
			add_child(knife)
		knife.setup(player, playfield, offset, i * LAUNCH_STAGGER)

	queue_free()
