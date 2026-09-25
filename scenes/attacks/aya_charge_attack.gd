class_name AyaChargeAttack
extends Node2D

## Coordinator for Aya Shameimaru's Level 1 Charge Attack.
## pl10.sht's second shot list: 20 crescents, one every 3 frames, fired from wherever Aya is
## at that moment, so moving while it streams out sweeps the column. Headings sway one
## degree per shot between -87 and -93 (0 = right, -90 = straight up).

const SHOT_SCENE: PackedScene = preload("res://scenes/attacks/aya_charge_shot.tscn")
const AyaChargeShotClass = preload("res://scenes/attacks/aya_charge_shot.gd")

const HEADINGS: Array[float] = [
	-90.0, -89.0, -88.0, -87.0, -88.0, -89.0, -90.0, -91.0, -92.0, -93.0,
	-92.0, -91.0, -90.0, -89.0, -88.0, -89.0, -90.0, -91.0, -92.0, -91.0,
]
const FRAME_INTERVAL: float = 3.0 / 60.0

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
	# tree ourselves; the stream needs get_tree() timers to run.
	if not is_inside_tree() and is_instance_valid(target_parent):
		target_parent.add_child(self)

	_stream(player, target_parent)

func _stream(player: Node2D, target_parent: Node) -> void:
	for i in HEADINGS.size():
		if not is_instance_valid(player) or not is_inside_tree():
			break
		var shot: AyaChargeShotClass = SHOT_SCENE.instantiate()
		if is_instance_valid(target_parent):
			target_parent.add_child(shot)
		else:
			add_child(shot)
		shot.setup(player.global_position, Vector2.from_angle(deg_to_rad(HEADINGS[i])))
		if i % 2 == 0:
			AudioService.play_sfx("se_plst00")
		if i < HEADINGS.size() - 1:
			await get_tree().create_timer(FRAME_INTERVAL).timeout

	queue_free()
