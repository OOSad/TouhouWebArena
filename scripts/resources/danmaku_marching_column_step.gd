class_name DanmakuMarchingColumnStep
extends DanmakuStep

## Aya Shameimaru's Level 2 and Level 3 (pl10.ecl sub0 / sub1).
##
## The emitter sits on the top edge of the victim's field and jumps along it, firing one
## column at the victim from each stop: `layers` bullets on the same aimed line, the first
## at the top speed and each next one slower (ZUN's bullet_fan_aimed with 1 way, n layers),
## so the column stretches out as it flies. The stops march `32 - rank` ZUN px at a time.
##
## Level 2 (`mirrored`): stops come in pairs, one on each edge, marching inward from the
## walls and ending before they meet in the middle, every 10 frames; one side fires pellets
## and the other arrowheads, swapping each pair.
## Level 3: one stop at a time, every 8 frames, sweeping from one wall all the way to the
## other; the starting wall is ZUN's RAND_INT % 2, drawn from the sync RNG.

@export_group("Bullet Types")
@export var bullet_data: DanmakuBulletData = null
## Level 2: the other side's bullet, swapping with bullet_data every pair. Null = same.
@export var alternate_bullet_data: DanmakuBulletData = null

@export_group("Shape")
## Both walls at once, marching inward (Level 2), or one wall to the other (Level 3)
@export var mirrored: bool = true
## Bullets per column (ZUN: 8 on Level 2, 5 on Level 3)
@export var layers: int = 8
## Starting distance from the centre, ZUN px (144, the wall)
@export var start_offset: float = 144.0
## March per stop is march_base - rank, ZUN px
@export var march_base: float = 32.0
## Frames between stops, at 60 fps (ZUN: 10 on Level 2, 8 on Level 3)
@export var interval_frames: int = 10
## Emitter height on the field (ZUN y=0, the top edge)
@export var spawn_y: float = 0.0

@export_group("Speed")
## First bullet of a column: speed_base + speed_per_rank * rank, px/s (ZUN: 5.0 + 0.1 rank)
@export var speed_base: float = 625.0
@export var speed_per_rank: float = 12.5
## The speed the layers step down toward, px/s (ZUN: 2.5)
@export var speed_floor: float = 312.5

@export_group("Pre-Cast Flower")
## Seconds the FlowerRingEffect closes onto the emitter's spawn point first (ZUN: Effect25,
## then +40 frames). 0 skips it.
@export var pre_cast_delay: float = 0.6666667
@export var flower_color: Color = Color(1.0, 0.45, 0.85, 1.0)
@export var flower_start_radius: float = 210.0

const ZUN_SCALE: float = 2.0833333
const FLOWER_RING_SCENE: PackedScene = preload("res://scenes/effects/flower_ring_effect.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	var rng: RandomNumberGenerator = take_sync_rng()
	var from_right: bool = (rng.randi() if rng else randi()) % 2 == 0

	if playfield == null or not is_instance_valid(playfield):
		return
	if not playfield.has_method("spawn_danmaku_bullet") or not playfield.is_inside_tree():
		return

	var field_width: float = playfield.PLAYFIELD_WIDTH if "PLAYFIELD_WIDTH" in playfield else 600.0
	var center_x: float = field_width * 0.5
	var tree: SceneTree = playfield.get_tree()

	if pre_cast_delay > 0.0:
		var effects_layer: Node2D = playfield.get("effects_layer")
		if effects_layer == null:
			effects_layer = playfield
		var flower: Node2D = FLOWER_RING_SCENE.instantiate()
		flower.call("setup", Vector2(center_x, spawn_y), pre_cast_delay, flower_color, flower_start_radius)
		effects_layer.add_child(flower)
		await tree.create_timer(pre_cast_delay).timeout
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return

	var r: int = clampi(rank, 1, 16)
	var march: float = march_base - float(r)
	var top_speed: float = speed_base + speed_per_rank * float(r)
	var alt_data: DanmakuBulletData = alternate_bullet_data if alternate_bullet_data else bullet_data
	var offset: float = start_offset
	var stop: int = 0

	while true:
		if mirrored:
			var right_data: DanmakuBulletData = bullet_data if stop % 2 == 0 else alt_data
			var left_data: DanmakuBulletData = alt_data if stop % 2 == 0 else bullet_data
			_fire_column(playfield, Vector2(center_x + offset * ZUN_SCALE, spawn_y), right_data, top_speed)
			_fire_column(playfield, Vector2(center_x - offset * ZUN_SCALE, spawn_y), left_data, top_speed)
		else:
			var x: float = offset if from_right else -offset
			_fire_column(playfield, Vector2(center_x + x * ZUN_SCALE, spawn_y), bullet_data, top_speed)
		# Flag 0x200: a shot sound per stop
		AudioService.play_sfx("se_tan00")

		offset -= march
		if mirrored and offset <= 0.0:
			break
		if not mirrored and offset <= -start_offset:
			break
		stop += 1
		await tree.create_timer(float(interval_frames) / 60.0).timeout
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return


func _fire_column(playfield: Node2D, pos: Vector2, data: DanmakuBulletData, top_speed: float) -> void:
	var aim: Vector2 = Vector2.DOWN
	var player_node: Node2D = playfield.get("player")
	if player_node and is_instance_valid(player_node):
		var to_player: Vector2 = player_node.position - pos
		if to_player.length_squared() > 0.001:
			aim = to_player.normalized()
	var n: int = maxi(layers, 1)
	for k in range(n):
		# ZUN's layered fan: layer k of n at first - (first - floor) * k / n
		var speed: float = top_speed - (top_speed - speed_floor) * float(k) / float(n)
		playfield.spawn_danmaku_bullet(data, pos, DanmakuBullet.MotionMode.LINEAR, aim, speed)
