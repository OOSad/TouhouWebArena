class_name DanmakuSnakingFanStep
extends DanmakuStep

## Lyrica Prismriver's Level 2 and Level 3, Noise Sign "Soul Noise Flow" (pl06.ecl sub0 / sub1).
##
## An emitter above the middle of the victim's field fires `fan_count` downward fans of
## `ways` bullets, `interval_frames` apart, alternating bullet_data and alternate_bullet_data.
## Every bullet snakes: ZUN chains four bullet_effects slots of 30 frames each, turning one
## way, back, half as far the first way, then back again, after which it flies straight.
## Which way the snake starts is ZUN's RAND_INT % 2, drawn from the sync RNG.
##
## Level 2: every fan snakes the same way. Level 3 (`mirror_alternate`): the alternate fans
## snake the mirrored way, so the two colours weave through each other.

@export_group("Bullet Types")
@export var bullet_data: DanmakuBulletData = null
## Every other fan. Null = same as bullet_data.
@export var alternate_bullet_data: DanmakuBulletData = null
## The alternate fans turn the opposite way (Level 3)
@export var mirror_alternate: bool = false

@export_group("Shape")
## Bullets per fan (ZUN: 7)
@export var ways: int = 7
## Radians between neighbouring bullets of a fan (ZUN: 0.19634955, 11.25 degrees)
@export var spread: float = 0.19634955
## Fans in the whole cast (ZUN: 8 loops of two)
@export var fan_count: int = 16
## Frames between fans, at 60 fps (ZUN: 8)
@export var interval_frames: int = 8
## Emitter height on the field, ZUN px (Level 2: 64, Level 3: 0, the top edge)
@export var spawn_y_zun: float = 64.0

@export_group("Snake")
## Turn rates in rad/s, one after another (ZUN, rad/frame x 60: 0.0262, -0.0262, 0.0131, -0.0262)
@export var turns: PackedFloat32Array = PackedFloat32Array([1.5707964, -1.5707964, 0.7853982, -1.5707964])
## Frames each turn lasts (ZUN: 30)
@export var turn_frames: int = 30

@export_group("Speed")
## speed_base + speed_per_rank * rank, px/s (ZUN: 1.5 + 0.1 rank on Level 2, 1.5 + 0.05 rank on Level 3)
@export var speed_base: float = 187.5
@export var speed_per_rank: float = 12.5

@export_group("Pre-Cast Flower")
## Seconds the FlowerRingEffect closes onto the emitter first (ZUN: Effect25, then +40 frames).
## 0 skips it.
@export var pre_cast_delay: float = 0.6666667
@export var flower_color: Color = Color(1.0, 0.35, 0.4, 1.0)
@export var flower_start_radius: float = 210.0

const ZUN_SCALE: float = 2.0833333
const FLOWER_RING_SCENE: PackedScene = preload("res://scenes/effects/flower_ring_effect.tscn")

func execute(playfield: Node2D, _origin: Vector2, rank: int) -> void:
	var rng: RandomNumberGenerator = take_sync_rng()
	var turn_sign: float = 1.0 if (rng.randi() if rng else randi()) % 2 == 0 else -1.0

	if playfield == null or not is_instance_valid(playfield):
		return
	if not playfield.has_method("spawn_danmaku_bullet_curve_line") or not playfield.is_inside_tree():
		return

	var field_width: float = playfield.PLAYFIELD_WIDTH if "PLAYFIELD_WIDTH" in playfield else 600.0
	var emitter := Vector2(field_width * 0.5, spawn_y_zun * ZUN_SCALE)
	var tree: SceneTree = playfield.get_tree()

	if pre_cast_delay > 0.0:
		var effects_layer: Node2D = playfield.get("effects_layer")
		if effects_layer == null:
			effects_layer = playfield
		var flower: Node2D = FLOWER_RING_SCENE.instantiate()
		flower.call("setup", emitter, pre_cast_delay, flower_color, flower_start_radius)
		effects_layer.add_child(flower)
		await tree.create_timer(pre_cast_delay).timeout
		if not is_instance_valid(playfield) or not playfield.is_inside_tree():
			return

	var r: int = clampi(rank, 1, 16)
	var speed: float = speed_base + speed_per_rank * float(r)
	var alt_data: DanmakuBulletData = alternate_bullet_data if alternate_bullet_data else bullet_data

	for i in range(fan_count):
		var is_alt: bool = i % 2 == 1
		var fan_sign: float = -turn_sign if (is_alt and mirror_alternate) else turn_sign
		_fire_fan(playfield, emitter, alt_data if is_alt else bullet_data, speed, fan_sign)
		# Flag 0x200: a shot sound per fan
		AudioService.play_sfx("se_tan00")
		if i < fan_count - 1:
			await tree.create_timer(float(interval_frames) / 60.0).timeout
			if not is_instance_valid(playfield) or not playfield.is_inside_tree():
				return


func _fire_fan(playfield: Node2D, pos: Vector2, data: DanmakuBulletData, speed: float, fan_sign: float) -> void:
	if turns.is_empty():
		return
	var chain := PackedFloat32Array()
	for k in range(1, turns.size()):
		chain.append(turns[k] * fan_sign)
	var duration: float = float(turn_frames) / 60.0
	var n: int = maxi(ways, 1)
	for k in range(n):
		var angle: float = PI * 0.5 + (float(k) - float(n - 1) * 0.5) * spread
		var b: DanmakuBullet = playfield.spawn_danmaku_bullet_curve_line(
			data, pos, Vector2.from_angle(angle), speed, turns[0] * fan_sign, duration)
		if b:
			b.curve_chain = chain
