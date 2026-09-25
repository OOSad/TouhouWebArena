class_name DanmakuDelayStep
extends DanmakuStep

## Pauses the pattern timeline execution for a specified duration in seconds.

@export var delay_seconds: float = 0.15

func execute(playfield: Node2D, _origin: Vector2, _rank: int) -> void:
	if delay_seconds > 0.0 and playfield != null and playfield.is_inside_tree():
		await playfield.get_tree().create_timer(delay_seconds).timeout

