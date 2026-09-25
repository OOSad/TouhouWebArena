class_name MatchTimer
extends PanelContainer

@onready var timer_label: Label = %TimerLabel if has_node("%TimerLabel") else get_node_or_null("TimerLabel")

var elapsed_seconds: float = 0.0
var is_running: bool = false
var is_sudden_death: bool = false
var _pulse_timer: float = 0.0

func _ready() -> void:
	_ensure_nodes()
	update_display()

func _process(delta: float) -> void:
	if is_sudden_death and timer_label:
		_pulse_timer += delta * 5.0
		var alpha: float = 0.70 + 0.30 * (sin(_pulse_timer) * 0.5 + 0.5)
		timer_label.modulate = Color(1.0, 0.25, 0.25, alpha)

func _ensure_nodes() -> void:
	if timer_label == null:
		timer_label = %TimerLabel if has_node("%TimerLabel") else get_node_or_null("TimerLabel")

func set_time(seconds: float) -> void:
	elapsed_seconds = maxf(0.0, seconds)
	update_display()

func set_sudden_death(active: bool) -> void:
	if is_sudden_death == active:
		return
	is_sudden_death = active
	_pulse_timer = 0.0
	_ensure_nodes()
	if timer_label and not is_sudden_death:
		timer_label.modulate = Color.WHITE

func advance(delta: float) -> void:
	if is_running:
		elapsed_seconds += delta
		update_display()

func start() -> void:
	is_running = true

func pause() -> void:
	is_running = false

func stop() -> void:
	pause()

func reset() -> void:
	elapsed_seconds = 0.0
	set_sudden_death(false)
	update_display()

func update_display() -> void:
	_ensure_nodes()
	if timer_label:
		var total_secs: int = int(elapsed_seconds)
		var mins: int = total_secs / 60
		var secs: int = total_secs % 60
		timer_label.text = "%02d:%02d" % [mins, secs]
