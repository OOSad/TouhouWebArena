class_name ClownpieceChargeAttack
extends Node2D

## Clownpiece's Level 1 Charge Attack: a star spray, a small Starry Illusion of her own aimed
## up her field at the fairies (the user's pick of three ideas; she has no PoFV original).
## Stars leave her one per frame, each STEP_DEG further round, fanning across the field above
## her and back again, as her boss's spray sweeps back and forth.

const STAR_SCENE: PackedScene = preload("res://scenes/attacks/clownpiece_charge_star.tscn")

## Half the fan's width either side of straight up.
const HALF_FAN_DEG: float = 60.0
const STEP_DEG: float = 10.0
## Across and back.
const ARCS: int = 2
const STAR_INTERVAL: float = 1.0 / 60.0
const SPAWN_OFFSET_Y: float = -16.0

func setup(player: Node2D, playfield: Node2D) -> void:
	var target_parent: Node = null
	if playfield:
		target_parent = playfield.get_node_or_null("%Bullets")
		if target_parent == null:
			target_parent = playfield
	elif player:
		target_parent = player.get_parent()
	# The executor never parents coordinators that define setup(), so join the tree
	# ourselves; the stars are staggered on timers.
	if not is_inside_tree() and is_instance_valid(target_parent):
		target_parent.add_child(self)
	_spray(player, target_parent)

func _spray(player: Node2D, target_parent: Node) -> void:
	var per_arc: int = int(HALF_FAN_DEG * 2.0 / STEP_DEG) + 1
	for arc in ARCS:
		AudioService.play_sfx("se_plst00")
		# Even arcs sweep left to right, odd ones back.
		var sweep: float = 1.0 if arc % 2 == 0 else -1.0
		for i in per_arc:
			if not is_instance_valid(player) or not is_inside_tree():
				queue_free()
				return
			var deg: float = -90.0 + sweep * (-HALF_FAN_DEG + STEP_DEG * float(i))
			var star: ClownpieceChargeStar = STAR_SCENE.instantiate()
			target_parent.add_child(star)
			star.setup(Vector2.from_angle(deg_to_rad(deg)), player.global_position + Vector2(0.0, SPAWN_OFFSET_Y))
			await get_tree().create_timer(STAR_INTERVAL).timeout
	queue_free()
