class_name PickupItem
extends Area2D

## Collectible Pickup Item for Touhou Web Arena
## Spawned upon defeating Lily White or Level 4 Illusion Bosses.
## Launches straight up vertically under gravity, falls down floatily,
## and triggers power-up or opponent Danmaku effects when collected.

enum Type {
	G,      ## Fully recharges player's Spell Gauge to MAX
	BULLET, ## Sends dense bullet clusters to opponent's field
	EX,     ## Summons a barrage of Extra Attacks against opponent
	POINT   ## Fires off a Level 4 Spellcard against opponent
}

# PoFV's item sprites in etama.anm, from the player's th09.dat.
const TH09_SPRITES: Dictionary = {
	Type.G: 172,
	Type.BULLET: 173,
	Type.EX: 174,
	Type.POINT: 175,
}

@export var item_type: Type = Type.G:
	set(val):
		item_type = val
		_update_texture()

@export var initial_upward_speed: float = 390.0
@export var gravity_accel: float = 360.0
@export var max_fall_speed: float = 220.0
@export var despawn_y: float = 1040.0

@onready var sprite: Sprite2D = $Sprite2D
@onready var collision_shape: CollisionShape2D = $CollisionShape2D

var velocity: Vector2 = Vector2.ZERO
var is_collected: bool = false
var playfield: Node2D = null

signal collected(item: PickupItem, collector: Player)

func _ready() -> void:
	# Pickups reside on Layer 5 (pickups = 16) and detect Player on Layer 1 (player = 1)
	collision_layer = 16
	collision_mask = 1
	_update_texture()
	
	# Initial toss: strictly straight up in a vertical line
	velocity = Vector2(0.0, -initial_upward_speed)
	
	# Authentic spawn scale-pop
	scale = Vector2(0.2, 0.2)
	var spawn_tween := create_tween()
	spawn_tween.tween_property(self, "scale", Vector2.ONE, 0.18).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)
	
	body_entered.connect(_on_body_entered)
	area_entered.connect(_on_area_entered)

func setup(p_type: Type, spawn_pos: Vector2, p_playfield: Node2D = null, upward_speed: float = 390.0) -> void:
	item_type = p_type
	position = spawn_pos
	playfield = p_playfield
	initial_upward_speed = upward_speed
	velocity = Vector2(0.0, -initial_upward_speed)
	_update_texture()

func _update_texture() -> void:
	if sprite == null or not TH09_SPRITES.has(item_type):
		return
	var game_data := get_node_or_null("/root/GameData") if is_inside_tree() else null
	if game_data:
		sprite.texture = game_data.sprite("th09", "etama.anm", TH09_SPRITES[item_type], 2)

func _physics_process(delta: float) -> void:
	if is_collected:
		return
	
	# Decelerate upward launch with gravity until terminal float fall speed
	velocity.y = minf(velocity.y + gravity_accel * delta, max_fall_speed)
	position += velocity * delta
	
	# Despawn once fallen past playfield bottom
	if position.y >= despawn_y:
		queue_free()

## Magnetically pulls item towards target position (used when player is focusing nearby)
func attract_towards(target_pos: Vector2, pull_speed: float, delta: float) -> void:
	if is_collected:
		return
	var dir := (target_pos - position).normalized()
	position += dir * pull_speed * delta

func collect(collector: Player) -> void:
	if is_collected:
		return
	is_collected = true
	set_deferred("monitoring", false)
	set_deferred("monitorable", false)
	
	collected.emit(self, collector)
	
	# Collection flash pop
	if is_inside_tree():
		var tw := create_tween()
		if tw:
			tw.set_parallel(true)
			tw.tween_property(self, "scale", Vector2(1.5, 1.5), 0.12).set_ease(Tween.EASE_OUT)
			tw.tween_property(self, "modulate:a", 0.0, 0.12)
			tw.chain().tween_callback(queue_free)
		else:
			queue_free()
	else:
		queue_free()

func _on_body_entered(body: Node2D) -> void:
	if body is Player and not is_collected:
		collect(body)

func _on_area_entered(area: Area2D) -> void:
	if is_collected:
		return
	# Also allow pickup via player's ItemCollector Area2D
	var parent_player = area.get_parent()
	if parent_player is Player and not is_collected:
		collect(parent_player)
