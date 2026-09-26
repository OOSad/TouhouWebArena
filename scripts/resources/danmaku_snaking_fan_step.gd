class_name DanmakuSnakingFanStep
extends DanmakuStep

## Lyrica Prismriver's snaking bullets: her Level 2 and Level 3, Noise Sign "Soul Noise Flow"
## (pl06.ecl sub0 / sub1), and two of her Level 4 boss attacks (sub4 / sub5 and sub6).
##
## An emitter fires `fan_count` fans of `ways` bullets, `interval_frames` apart, alternating
## bullet_data and alternate_bullet_data. Every bullet snakes: ZUN chains four bullet_effects
## slots of 30 frames each (`turns`), after which it flies straight. Which way the snake
## starts is ZUN's RAND_INT % 2 (or the boss's pick of sub4 / sub5), drawn from the sync RNG.
##
## Level 2: downward fans from above the middle, every fan snaking the same way. Level 3
## (`mirror_alternate`): the alternate fans snake the mirrored way, so the colours weave.
## Boss (`from_origin`, `aim_at_player`): fans aimed at the victim from the boss, the aim
## fixed when the first fan fires; sub6 fires whole rings (`ring`) with a wilder chain.

@export_group("Bullet Types")
@export var bullet_data: DanmakuBulletData = null
## Every other fan. Null = same as bullet_data.
@export var alternate_bullet_data: DanmakuBulletData = null
## The alternate fans turn the opposite way (Level 3)
@export var mirror_alternate: bool = false

@export_group("Shape")
## Bullets per fan (ZUN: 7 on Level 2 / 3, 3 for the boss's fans, 16 for its rings)
@export var ways: int = 7
## Radians between neighbouring bullets of a fan (ZUN: 0.19634955, 11.25 degrees)
@export var spread: float = 0.19634955
## Whole rings of `ways` bullets instead of fans (ZUN bullet_circle, boss sub6)
@export var ring: bool = false
## Fans in the whole cast (ZUN: 8 loops of two, or 16 of one)
@export var fan_count: int = 16
## Frames between fans, at 60 fps (ZUN: 8)
@export var interval_frames: int = 8
## Emitter height on the field, ZUN px (Level 2: 64, Level 3: 0, the top edge)
@export var spawn_y_zun: float = 64.0
## Fire from the caller's origin (the boss) instead of the fixed point above the middle
@export var from_origin: bool = false
## Centre the fans on the victim (ZUN PLAYER_ANGLE) instead of straight down
@export var aim_at_player: bool = false

@export_group("Snake")
## (turn rate rad/s, acceleration px/s^2), one after another. ZUN, rad/frame x 60:
## Level 2 / 3 and the boss's fans 0.0262, -0.0262, 0.0131, -0.0262; the boss's rings
## 0.0524, 0.1047, 0.0131, -0.0524 with 0.0333 px/frame^2 on the last.
@export var turns: PackedVector2Array = PackedVector2Array([
	Vector2(1.5707964, 0.0), Vector2(-1.5707964, 0.0), Vector2(0.7853982, 0.0), Vector2(-1.5707964, 0.0)])
## Frames each turn lasts (ZUN: 30)
@export var turn_frames: int = 30

@export_group("Speed")
## speed_base + speed_per_rank * rank, px/s (ZUN: 1.5 + 0.1 rank on Level 2, 1.5 + 0.05 rank
## on Level 3, 1.4 + 0.1 rank for the boss's fans, 1.5 + 0.1 rank for its rings)
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

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	var rng: RandomNumberGenerator = take_sync_rng()
	var turn_sign: float = 1.0 if (rng.randi() if rng else randi()) % 2 == 0 else -1.0

	if playfield == null or not is_instance_valid(playfield):
		return
	if not playfield.has_method("spawn_danmaku_bullet_curve_line") or not playfield.is_inside_tree():
		return

	var field_width: float = playfield.PLAYFIELD_WIDTH if "PLAYFIELD_WIDTH" in playfield else 600.0
	var emitter := origin if from_origin else Vector2(field_width * 0.5, spawn_y_zun * ZUN_SCALE)
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
	var aim: float = PI * 0.5
	if aim_at_player:
		var victim: Node2D = playfield.get("player")
		if victim and is_instance_valid(victim) and victim.position.distance_squared_to(emitter) > 0.001:
			aim = (victim.position - emitter).angle()

	for i in range(fan_count):
		var is_alt: bool = i % 2 == 1
		var fan_sign: float = -turn_sign if (is_alt and mirror_alternate) else turn_sign
		_fire_fan(playfield, emitter, alt_data if is_alt else bullet_data, speed, aim, fan_sign)
		# Flag 0x200: a shot sound per fan
		AudioService.play_sfx("se_tan00")
		if i < fan_count - 1:
			await tree.create_timer(float(interval_frames) / 60.0).timeout
			if not is_instance_valid(playfield) or not playfield.is_inside_tree():
				return


func _fire_fan(playfield: Node2D, pos: Vector2, data: DanmakuBulletData, speed: float, aim: float, fan_sign: float) -> void:
	if turns.is_empty():
		return
	var chain := PackedVector2Array()
	for k in range(1, turns.size()):
		chain.append(Vector2(turns[k].x * fan_sign, turns[k].y))
	var duration: float = float(turn_frames) / 60.0
	var n: int = maxi(ways, 1)
	for k in range(n):
		var angle: float = aim + (TAU * float(k) / float(n) if ring else (float(k) - float(n - 1) * 0.5) * spread)
		var b: DanmakuBullet = playfield.spawn_danmaku_bullet_curve_line(
			data, pos, Vector2.from_angle(angle), speed, turns[0].x * fan_sign, duration, turns[0].y)
		if b:
			b.curve_chain = chain
