class_name FairySpawner
extends Node

## Handles fairy wave scheduling, curve selection, and spawning for a Playfield.

const FAIRY_SCENE: PackedScene = preload("res://scenes/enemies/fairy.tscn")

@export var fairy_spawning_enabled: bool = true
@export var fairy_spawn_interval: float = 1.7
@export var initial_spawn_delay: float = 2.0

var _fairy_spawn_timer: float = 2.0
var _formations: Array[FairyPaths.Formation] = []
var _fairy_rng: RandomNumberGenerator = RandomNumberGenerator.new()

func setup(seed_val: int = 99991) -> void:
	if _formations.is_empty():
		_formations = FairyPaths.get_all_formations()
	_fairy_rng.seed = seed_val

func reset() -> void:
	_fairy_spawn_timer = initial_spawn_delay

func update(delta: float, entities_layer: Node2D, on_defeated_callable: Callable) -> void:
	if not fairy_spawning_enabled:
		return
	_fairy_spawn_timer -= delta
	if _fairy_spawn_timer <= 0.0:
		_fairy_spawn_timer = fairy_spawn_interval
		spawn_fairy_train(entities_layer, on_defeated_callable)

func spawn_fairy_train(entities_layer: Node2D, on_defeated_callable: Callable) -> void:
	if _formations.is_empty() or entities_layer == null:
		return

	# Pick a random formation deterministically; its length and fairy lineup are fixed by the ECL
	var formation: FairyPaths.Formation = _formations[_fairy_rng.randi_range(0, _formations.size() - 1)]

	for i in formation.lineup.size():
		var fairy: Fairy = FAIRY_SCENE.instantiate()
		var fairy_type: Fairy.FairyType = Fairy.FairyType.SMALL
		var fairy_variant: Fairy.FairyVariant = Fairy.FairyVariant.BLUE
		match formation.lineup[i]:
			FairyPaths.Tier.RED:
				fairy_variant = Fairy.FairyVariant.RED
			FairyPaths.Tier.GREEN:
				fairy_variant = Fairy.FairyVariant.GREEN
			FairyPaths.Tier.GREAT:
				fairy_type = Fairy.FairyType.GREAT

		var start_dist: float = -float(i) * formation.spacing
		fairy.setup(fairy_type, formation.curve, start_dist, formation.speed, fairy_variant)
		if on_defeated_callable.is_valid():
			fairy.defeated.connect(on_defeated_callable)
		entities_layer.add_child(fairy)
