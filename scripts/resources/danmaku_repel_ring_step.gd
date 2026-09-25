class_name DanmakuRepelRingStep
extends DanmakuStep

## Clownpiece's Hellfire "Infernal Essence of Grazing" (TH15 `st05bs.ecl` BossCard3).
##
## Each ring is a closed circle of flames with no gap anywhere in it: 144 of them, one aimed
## at the player, all at the same speed. What makes it passable is that the flames are
## repelled by the player - any flame inside the repel radius is pushed straight away from
## them - so moving into the ring bends open a narrow gap to slip through. The push is weaker
## than the ring, so a player who stands still is still hit by the flame aimed at them.
##
## ECL (`BossCard3_at`): `ins_606` 144 bullets (64 on Easy), `ins_605` speed 1.5, aim mode 3
## at the player, ex 0x200000 for 120 frames at 2.0 - the repel. TH15 fires three rings,
## `ins_23` 120 frames apart (90 on Lunatic). That held the stage for four seconds and crowded
## out her other cards, so per the user a volley is two rings fired close together: one
## doubled ring, the second trailing the first by about 60 px.
##
## TH15 units convert by field height, 960 / 448 = 2.143, so 1 px/frame is 128.6 px/s here. Not the
## depot's uniform 1.5625: TH15's field is wider than ours in proportion, so width scaling
## leaves every distance ~27% short. Height is also the scale our player sprites are drawn at.
##
## The repel itself is deliberately gentler than TH15's (see the Repel group): the original
## only lets a player through who meets the ring close under the boss, which playtesting
## found unreadable.

@export_group("Bullet Type")
@export var bullet_data: DanmakuBulletData = null

@export_group("Ring")
@export var rings_per_volley: int = 2
@export var bullets_per_ring: int = 144
## 1.5 px/frame.
@export var speed: float = 192.86
## Seconds between the two rings: 0.3s puts the second ~58 px behind the first, inside the
## repel reach, so the gap the player opens bends both. (TH15: 120 frames between three.)
@export var ring_interval: float = 0.3

@export_group("Repel")
## How far the push reaches at Rank 1 and Rank 16; this sets how wide a gap the player can
## open, which is what rank scales. TH15's is ~50 px (measured off the footage, 107 here).
## Rank 16 is the graze radius, playtested as the edge of possible, so the flames bend right
## at the edge of the graze field.
@export var repel_radius_min_rank: float = 90.0
@export var repel_radius_max_rank: float = 64.0
## TH15 pushes at 2.0 px/frame (257 px/s), faster than the ring, so flames stall on the edge.
## Kept under the ring's own speed here: flames bend around the player but still creep in,
## so standing still does not hold the aimed flame off forever. Rank 1, Rank 16.
@export var repel_speed_min_rank: float = 170.0
@export var repel_speed_max_rank: float = 150.0
## Seconds the repel lasts from launch; 0 keeps it for the flame's whole life. TH15 stops it
## after 120 frames, which only rewards meeting the ring close under the boss - something a
## Touhou player's instincts fight, so it runs forever here.
@export var repel_duration: float = 0.0

@export_group("Audio & Visuals")
@export var sfx: String = "se_tan00"
## One flash per ring on the emitter. 0 disables it.
@export var spawn_flash_scale: float = 2.6

const DEFAULT_BULLET: DanmakuBulletData = preload("res://resources/bullets/clownpiece_flame_red.tres")
const SPAWN_FLASH_SCENE: PackedScene = preload("res://scenes/effects/spawn_flash.tscn")

func execute(playfield: Node2D, origin: Vector2, rank: int) -> void:
	if playfield == null or not is_instance_valid(playfield):
		return
	if not playfield.has_method("spawn_danmaku_bullet"):
		return

	var data: DanmakuBulletData = bullet_data if bullet_data else DEFAULT_BULLET
	var count: int = maxi(bullets_per_ring, 3)
	var interval: float = ring_interval
	var t_rank: float = clampf(float(rank - 1) / 15.0, 0.0, 1.0)
	var radius: float = lerpf(repel_radius_min_rank, repel_radius_max_rank, t_rank)
	var push: float = lerpf(repel_speed_min_rank, repel_speed_max_rank, t_rank)
	var step_angle: float = TAU / float(count)

	for ring in range(maxi(rings_per_volley, 1)):
		if not is_instance_valid(playfield):
			return

		var victim: Node2D = playfield.player if "player" in playfield else null
		var aim: float = PI / 2.0
		if victim and is_instance_valid(victim):
			aim = (victim.position - origin).angle()

		if not sfx.is_empty():
			AudioService.play_sfx(sfx)
		_spawn_flash(playfield, origin)

		for i in range(count):
			var bullet: DanmakuBullet = playfield.spawn_danmaku_bullet(
				data,
				origin,
				DanmakuBullet.MotionMode.LINEAR,
				Vector2.from_angle(aim + step_angle * float(i)),
				speed
			)
			if bullet and victim and is_instance_valid(victim):
				bullet.repel_source = victim
				bullet.repel_radius = radius
				bullet.repel_speed = push
				bullet.repel_time_left = repel_duration if repel_duration > 0.0 else INF

		if ring < rings_per_volley - 1 and interval > 0.0:
			if not playfield.is_inside_tree():
				return
			await playfield.get_tree().create_timer(interval).timeout

func _spawn_flash(playfield: Node2D, at: Vector2) -> void:
	if spawn_flash_scale <= 0.0:
		return
	var layer: Node2D = playfield.get("effects_layer")
	if layer == null:
		layer = playfield.get_node_or_null("%Effects")
	if layer == null:
		layer = playfield
	# setup() before add_child(): the flash builds its tween in _ready() off these.
	var flash: Node2D = SPAWN_FLASH_SCENE.instantiate()
	flash.call("setup", at, spawn_flash_scale)
	layer.add_child(flash)
