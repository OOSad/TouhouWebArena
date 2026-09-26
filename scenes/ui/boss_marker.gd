class_name BossMarker
extends Sprite2D

## The red smudge under a playfield that follows a Level 4 boss's x, so a player dodging near
## the bottom can see where she is without looking up. LoLK's front.anm sprite 81 (front00.png),
## held steady (script 111's interrupt 10). Its blink rates were tried and dropped:
## steady reads better.
##
## Sits in the arena under the playfield's viewport. Width keeps LoLK's share of the playfield
## (96 of 384); height fits the gap above the spell bar.

const SIZE: Vector2 = Vector2(150.0, 22.0)

## The playfield whose boss this marker follows.
@export var playfield_path: NodePath

var _playfield: Playfield = null
## Where the playfield's left edge is; the scene places the marker there.
var _left: float = 0.0


func _ready() -> void:
	texture = preload("res://resources/dat_textures/boss_marker.tres")
	_playfield = get_node_or_null(playfield_path) as Playfield
	_left = position.x
	visible = false


func _process(_delta: float) -> void:
	var boss: BossCharacter = _playfield.active_boss if _playfield else null
	var showing := boss != null and is_instance_valid(boss) and not boss.is_dead \
		and boss.state in [BossCharacter.State.ENTERING, BossCharacter.State.ACTIVE, BossCharacter.State.HOPPING]
	if not showing:
		visible = false
		return
	visible = true
	if texture.get_width() > 0:
		scale = SIZE / texture.get_size()
	position.x = _left + clampf(boss.position.x, 0.0, Playfield.PLAYFIELD_WIDTH)
