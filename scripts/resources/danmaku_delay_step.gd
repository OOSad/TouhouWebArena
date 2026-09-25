class_name DanmakuDelayStep
extends DanmakuStep

## Pauses the pattern timeline execution for a specified duration in seconds.

@export var delay_seconds: float = 0.15
## Played as the pause starts, e.g. a wind-up cue (ZUN's effect_sound before a +40)
@export var sfx: String = ""

func execute(playfield: Node2D, _origin: Vector2, _rank: int) -> void:
	if not sfx.is_empty():
		AudioService.play_sfx(sfx)
	if delay_seconds > 0.0 and playfield != null and playfield.is_inside_tree():
		await playfield.get_tree().create_timer(delay_seconds).timeout
