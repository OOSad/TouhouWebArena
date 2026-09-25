class_name YinYangOrb
extends Area2D

# Reimu Hakurei's Extra Attack: Yin-Yang Orb (陰陽玉)
# Spawns near the top of the playfield, tossed upward in an arc, falls under
# gravity, spins continuously, bounces elastically off side walls, AND bounces off
# other Yin-Yang orbs!

@export var initial_upward_speed: float = 390.0
@export var gravity_accel: float = 400.0
@export var max_fall_speed: float = 340.0
@export var horizontal_speed: float = 130.0
@export var spin_speed: float = 4.5
@export var radius: float = 60.0

@export var playfield_min_x: float = 65.0
@export var playfield_max_x: float = 535.0
@export var despawn_y: float = 1080.0

@onready var sprite: Sprite2D = $Sprite2D
@onready var collision_shape: CollisionShape2D = $CollisionShape2D

var velocity: Vector2 = Vector2.ZERO

## Set from the attack's synced landing spot before the orb enters the tree.
## Which way the orb is first tossed decides every wall bounce it will ever make,
## so the toss is part of the attack identity and has to match on both clients.
var _toss_rng: RandomNumberGenerator = null

func setup_variation(seed_value: int) -> void:
	_toss_rng = RandomNumberGenerator.new()
	_toss_rng.seed = seed_value

func _ready() -> void:
	collision_layer = 2 # 2 (enemy_bullets) - hazards player, ignored by shockwaves and enemy-targeting shots
	collision_mask = 1  # 1 (player)
	body_entered.connect(_on_body_entered)
	
	# Initial toss: random horizontal direction, upward launch
	var dir_roll: float = _toss_rng.randf() if _toss_rng else randf()
	var speed_scale: float = _toss_rng.randf_range(0.7, 1.3) if _toss_rng else randf_range(0.7, 1.3)
	var dir_x: float = -1.0 if dir_roll < 0.5 else 1.0
	velocity = Vector2(dir_x * horizontal_speed * speed_scale, -initial_upward_speed)
	
	# Spawn scale pop
	scale = Vector2(0.2, 0.2)
	var spawn_tween := create_tween()
	spawn_tween.tween_property(self, "scale", Vector2.ONE, 0.18).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)

func _physics_process(delta: float) -> void:
	# Apply gravity
	velocity.y = minf(velocity.y + gravity_accel * delta, max_fall_speed)
	
	# Move position
	position += velocity * delta
	
	# Continuous rotation
	sprite.rotation += spin_speed * delta
	
	# Elastic bounce off left and right playfield walls
	if position.x <= playfield_min_x and velocity.x < 0.0:
		position.x = playfield_min_x
		velocity.x = -velocity.x
	elif position.x >= playfield_max_x and velocity.x > 0.0:
		position.x = playfield_max_x
		velocity.x = -velocity.x
	
	# Despawn only when falling past the bottom of the screen
	if position.y >= despawn_y:
		queue_free()

func take_damage(_amount: float, source: String = "bullet") -> bool:
	if source == "heavy_shockwave":
		queue_free()
		return true
	# Indestructible danmaku obstacle: ignore all damage from shots or fairy shockwaves
	return false

func _on_body_entered(body: Node2D) -> void:
	if body is Player:
		if body.has_method("take_damage"):
			body.take_damage(1.5, self)
		elif body.has_method("hit"):
			body.hit()
