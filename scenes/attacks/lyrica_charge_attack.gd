class_name LyricaChargeAttack
extends Node2D

## Coordinator for Lyrica Prismriver's Level 1 Charge Attack.
## pl06.sht's third shot list: 25 notes straight up, one every 3 frames with a beat skipped
## at frame 60, each fired from wherever Lyrica is at that moment, so moving sweeps the column.
## Meanwhile she plays a keyboard (pl06.anm script 7): it fades in over 2 frames while
## squashing from 0.5x8 to 1x1 over 8, runs its eight frames 3 frames apart three times over,
## then fades out over 15 frames.

const NOTE_SCENE: PackedScene = preload("res://scenes/attacks/lyrica_charge_note.tscn")
const KEYBOARD_TEXTURE: Texture2D = preload("res://resources/dat_textures/lyrica_keyboard.tres")

const NOTE_FRAMES: Array[int] = [
	0, 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48, 51, 54, 57,
	63, 66, 69, 72, 75,
]
const KEYBOARD_SCALE: float = 2.0
const KEYBOARD_FRAMES: int = 8
const KEYBOARD_FRAME_TICKS: int = 3
const KEYBOARD_LOOPS: int = 3
const KEYBOARD_FADE_IN: float = 2.0
const KEYBOARD_SQUASH: float = 8.0
const KEYBOARD_FADE_OUT: float = 15.0

var _player: Node2D = null
var _target_parent: Node = null
var _bullet_texture: Texture2D = null
var _keyboard: Sprite2D = null
var _ticks: float = 0.0
var _next_note: int = 0

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

	_player = player
	_target_parent = target_parent
	if "character_data" in player and player.character_data:
		_bullet_texture = player.character_data.bullet_texture

	_keyboard = Sprite2D.new()
	_keyboard.texture = KEYBOARD_TEXTURE
	_keyboard.hframes = KEYBOARD_FRAMES
	_keyboard.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	# In front of her, as PoFV draws it: the player sits at z 30, her hitbox dot at 35.
	_keyboard.z_as_relative = false
	_keyboard.z_index = 31
	add_child(_keyboard)
	_update_keyboard()

	# The executor never parents coordinators that define setup(), so join the tree ourselves.
	if not is_inside_tree() and is_instance_valid(target_parent):
		target_parent.add_child(self)

func _physics_process(delta: float) -> void:
	if not is_instance_valid(_player):
		queue_free()
		return
	while _next_note < NOTE_FRAMES.size() and _ticks >= NOTE_FRAMES[_next_note]:
		_fire_note()
		_next_note += 1
	_update_keyboard()
	_ticks += delta * 60.0
	if _ticks >= _keyboard_life():
		queue_free()

func _fire_note() -> void:
	var note: LyricaChargeNote = NOTE_SCENE.instantiate()
	if is_instance_valid(_target_parent):
		_target_parent.add_child(note)
	else:
		add_child(note)
	note.setup(_player.global_position, _bullet_texture)
	if _next_note % 2 == 0:
		AudioService.play_sfx("se_plst00")

func _keyboard_life() -> float:
	return float(KEYBOARD_FRAMES * KEYBOARD_FRAME_TICKS * KEYBOARD_LOOPS) + KEYBOARD_FADE_OUT

func _update_keyboard() -> void:
	if _keyboard == null or not is_instance_valid(_player):
		return
	_keyboard.global_position = _player.global_position
	var play_ticks := float(KEYBOARD_FRAMES * KEYBOARD_FRAME_TICKS * KEYBOARD_LOOPS)
	_keyboard.frame = int(minf(_ticks, play_ticks - 1.0) / KEYBOARD_FRAME_TICKS) % KEYBOARD_FRAMES
	var squash := clampf(_ticks / KEYBOARD_SQUASH, 0.0, 1.0)
	_keyboard.scale = Vector2(lerpf(0.5, 1.0, squash), lerpf(8.0, 1.0, squash)) * KEYBOARD_SCALE
	var alpha := clampf(_ticks / KEYBOARD_FADE_IN, 0.0, 1.0)
	if _ticks > play_ticks:
		alpha = clampf(1.0 - (_ticks - play_ticks) / KEYBOARD_FADE_OUT, 0.0, 1.0)
	_keyboard.modulate.a = alpha
