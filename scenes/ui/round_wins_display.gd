class_name RoundWinsDisplay
extends HBoxContainer

const SAKURA_TEXTURE: Texture2D = preload("res://resources/dat_textures/round_win_sakura.tres")
const ROTATION_SPEED: float = 1.8 # Radians per second

@onready var slot0: TextureRect = $Slot0
@onready var slot1: TextureRect = $Slot1

var current_wins: int = 0

func _ready() -> void:
	if slot0 == null: slot0 = get_node_or_null("Slot0")
	if slot1 == null: slot1 = get_node_or_null("Slot1")
	
	_configure_slot(slot0)
	_configure_slot(slot1)
	update_display(current_wins, false)

func _configure_slot(slot: TextureRect) -> void:
	if slot == null:
		return
	slot.texture = SAKURA_TEXTURE
	slot.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	slot.custom_minimum_size = Vector2(36, 36)
	slot.pivot_offset = Vector2(18, 18)

func _process(delta: float) -> void:
	# Spin earned flowers continuously
	if current_wins >= 1 and slot0:
		slot0.rotation += delta * ROTATION_SPEED
	if current_wins >= 2 and slot1:
		slot1.rotation += delta * ROTATION_SPEED

func set_wins(wins: int, animate_new: bool = true) -> void:
	var old_wins := current_wins
	current_wins = clamp(wins, 0, 2)
	update_display(current_wins, animate_new and (current_wins > old_wins))

func update_display(wins: int, should_animate: bool = false) -> void:
	current_wins = clamp(wins, 0, 2)
	_apply_slot_state(slot0, current_wins >= 1, should_animate and current_wins == 1)
	_apply_slot_state(slot1, current_wins >= 2, should_animate and current_wins == 2)

func _apply_slot_state(slot: TextureRect, is_active: bool, animate: bool) -> void:
	if slot == null:
		return
	
	if is_active:
		slot.modulate = Color.WHITE
		if animate:
			slot.scale = Vector2(0.2, 0.2)
			var tw := create_tween()
			tw.tween_property(slot, "scale", Vector2.ONE, 0.4).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		else:
			slot.scale = Vector2.ONE
	else:
		# Unearned: subtle translucent silhouette
		slot.modulate = Color(1.0, 1.0, 1.0, 0.18)
		slot.scale = Vector2.ONE
		slot.rotation = 0.0
