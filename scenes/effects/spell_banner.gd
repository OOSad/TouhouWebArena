class_name SpellBanner
extends Node2D

## Spell Attack Notification Banner for Level 2-4 Spellcard Declarations.
## Displays the authentic sliced character banner scaled 2x with nearest-neighbor filtering.
## Operates under PROCESS_MODE_ALWAYS so animations run during the Action Stop freeze.

@onready var sprite: Sprite2D = $Sprite2D

const TARGET_X: float = 12.0 # (600 - 576) / 2
const DEFAULT_Y: float = 340.0
const SCALE_FACTOR: float = 2.0

var _is_dismissed: bool = false
var _active_tween: Tween = null

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS

func setup(texture: Texture2D, start_mirrored: bool = false) -> void:
	if sprite == null:
		sprite = $Sprite2D
	if sprite:
		sprite.texture = texture
		sprite.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
		sprite.scale = Vector2(SCALE_FACTOR, SCALE_FACTOR)
		sprite.centered = false
		sprite.flip_h = start_mirrored

## Plays the entrance slide, holds for hold_duration (default 0.55s), and smoothly fades/slides out.
func play_banner(hold_duration: float = 0.55) -> void:
	position = Vector2(-600.0, DEFAULT_Y)
	modulate.a = 0.0
	
	if _active_tween:
		_active_tween.kill()
	
	_active_tween = create_tween()
	_active_tween.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	
	# Rapid entrance snap with slight overshoot / ease-out
	_active_tween.set_parallel(true)
	_active_tween.tween_property(self, "position:x", TARGET_X, 0.12).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	_active_tween.tween_property(self, "modulate:a", 1.0, 0.08)
	_active_tween.set_parallel(false)
	
	# Hold on screen during the action stop freeze
	_active_tween.tween_interval(hold_duration)
	
	# Smooth exit fade and slide
	_active_tween.tween_callback(dismiss)

## Dismisses the banner smoothly
func dismiss() -> void:
	if _is_dismissed:
		return
	_is_dismissed = true
	
	if _active_tween:
		_active_tween.kill()
		
	var exit_tw := create_tween()
	exit_tw.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	exit_tw.set_parallel(true)
	exit_tw.tween_property(self, "position:x", TARGET_X + 60.0, 0.20).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	exit_tw.tween_property(self, "modulate:a", 0.0, 0.20)
	exit_tw.set_parallel(false)
	exit_tw.tween_callback(queue_free)

