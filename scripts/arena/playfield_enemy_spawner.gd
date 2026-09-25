class_name PlayfieldEnemySpawner
extends Node

## Handles enemy spawning, fairy wave scheduling, spirits, midbosses (Lily White),
## stage bosses, and pickup item generation for a Playfield.

const FAIRY_SCENE: PackedScene = preload("res://scenes/enemies/fairy.tscn")
const SPIRIT_SCENE: PackedScene = preload("res://scenes/enemies/spirit.tscn")
const BOSS_CHARACTER_SCENE: PackedScene = preload("res://scenes/enemies/boss_character.tscn")
const LILY_WHITE_SCENE: PackedScene = preload("res://scenes/enemies/lily_white.tscn")
const PICKUP_ITEM_SCENE: PackedScene = preload("res://scenes/items/pickup_item.tscn")
const HEAVY_SHOCKWAVE_SCENE: PackedScene = preload("res://scenes/effects/heavy_shockwave.tscn")

var playfield: Node2D = null
var entities_layer: Node2D = null

var fairy_spawner: FairySpawner = null
var active_boss: BossCharacter = null
var active_lily_white: LilyWhite = null

@export var spirit_spawning_enabled: bool = true
@export var lily_white_spawning_enabled: bool = true

func setup(p_playfield: Node2D, p_entities_layer: Node2D) -> void:
	playfield = p_playfield
	entities_layer = p_entities_layer

func _get_player() -> Player:
	if playfield and "player" in playfield:
		return playfield.player as Player
	return null

func _get_playfield_width() -> float:
	return Playfield.PLAYFIELD_WIDTH if "PLAYFIELD_WIDTH" in Playfield else 600.0

func _get_playfield_height() -> float:
	return Playfield.PLAYFIELD_HEIGHT if "PLAYFIELD_HEIGHT" in Playfield else 960.0

func init_fairy_spawner(seed_val: int = 99991, interval: float = 1.7, enabled: bool = true) -> void:
	if fairy_spawner == null:
		fairy_spawner = FairySpawner.new()
		add_child(fairy_spawner)
		fairy_spawner.fairy_spawn_interval = interval
		fairy_spawner.fairy_spawning_enabled = enabled
	fairy_spawner.setup(seed_val)

func update_fairy_spawner(delta: float, on_defeated: Callable) -> void:
	if fairy_spawner:
		if entities_layer == null and playfield:
			entities_layer = playfield.get_node_or_null("%Entities")
		fairy_spawner.update(delta, entities_layer, on_defeated)

func spawn_fairy_train(on_defeated: Callable) -> void:
	if fairy_spawner:
		if entities_layer == null and playfield:
			entities_layer = playfield.get_node_or_null("%Entities")
		fairy_spawner.spawn_fairy_train(entities_layer, on_defeated)

func spawn_spirit(local_pos: Vector2, color_theme: String = "blue", on_defeated: Callable = Callable(), on_detonated: Callable = Callable()) -> Spirit:
	if not spirit_spawning_enabled:
		return null
	if entities_layer == null and playfield:
		entities_layer = playfield.get_node_or_null("%Entities")
	if entities_layer == null:
		return null
	
	var spirit: Spirit = SPIRIT_SCENE.instantiate()
	spirit.setup(local_pos, color_theme)
	if on_defeated.is_valid():
		spirit.defeated.connect(on_defeated)
	if on_detonated.is_valid():
		spirit.detonated.connect(on_detonated)
	entities_layer.add_child(spirit)
	return spirit

func detonate_spirit(detonation_pos: Vector2, color_theme: String) -> void:
	var target_pos := Vector2(_get_playfield_width() / 2.0, _get_playfield_height() * 0.85)
	var player: Player = _get_player()
	if player and is_instance_valid(player):
		target_pos = player.position
	
	var base_dir := (target_pos - detonation_pos).normalized()
	if base_dir == Vector2.ZERO:
		base_dir = Vector2.DOWN
	
	var bullet_color: Color = Color(0.35, 0.75, 1.0) # blue default
	match color_theme.to_lower():
		"green", "marisa":
			bullet_color = Color(0.35, 0.95, 0.50)
		"red":
			bullet_color = Color(1.0, 0.35, 0.45)
		_:
			bullet_color = Color(0.35, 0.75, 1.0)
	
	var fan_angles: Array[float] = [-deg_to_rad(22.0), 0.0, deg_to_rad(22.0)]
	var bullet_speed: float = 245.0
	
	for angle_offset in fan_angles:
		var dir := base_dir.rotated(angle_offset)
		if playfield and playfield.has_method("spawn_ring_pellet"):
			playfield.spawn_ring_pellet(detonation_pos, bullet_speed, dir, bullet_color)

func dispel_active_boss() -> void:
	if active_boss != null and is_instance_valid(active_boss):
		active_boss.dispel()
		active_boss = null
	if playfield and "spell_bg_overlay" in playfield and playfield.spell_bg_overlay:
		playfield.spell_bg_overlay.deactivate()

func spawn_boss(sender_character: String, rank: int = 1, on_defeated: Callable = Callable(), on_dispelled: Callable = Callable()) -> BossCharacter:
	dispel_active_boss()
	
	if entities_layer == null and playfield:
		entities_layer = playfield.get_node_or_null("%Entities")
	if entities_layer == null or playfield == null:
		return null
	
	var sender_data := CharacterData.get_character(sender_character)
	var b_data: BossData = sender_data.boss_data if sender_data else null
	if b_data == null:
		var res_path := "res://resources/bosses/%s_boss.tres" % sender_character.to_lower()
		if ResourceLoader.exists(res_path):
			b_data = load(res_path) as BossData
	
	if b_data == null:
		return null
	
	var r_seed: int = playfield.get("_round_seed") if "_round_seed" in playfield else 99991
	var b_count: int = playfield.get("_boss_spawn_count") if "_boss_spawn_count" in playfield else 0
	var boss_seed: int = r_seed + b_count * 7919
	if "_boss_spawn_count" in playfield:
		playfield.set("_boss_spawn_count", b_count + 1)

	var boss: BossCharacter = BOSS_CHARACTER_SCENE.instantiate() as BossCharacter
	boss.setup(b_data, rank, playfield, boss_seed)
	if on_defeated.is_valid():
		boss.defeated.connect(on_defeated)
	if on_dispelled.is_valid():
		boss.dispelled.connect(on_dispelled)
		boss.left_screen.connect(on_dispelled)
	boss.tree_exited.connect(func():
		if active_boss == boss:
			active_boss = null
	)
	entities_layer.add_child(boss)
	active_boss = boss
	
	var spell_bg_overlay = playfield.get("spell_bg_overlay")
	if spell_bg_overlay == null:
		spell_bg_overlay = playfield.get_node_or_null("%SpellBackgroundOverlay")
	if spell_bg_overlay and sender_data:
		spell_bg_overlay.activate(sender_data)
	
	return boss

func spawn_lily_white(forced: bool = false, on_defeated: Callable = Callable()) -> LilyWhite:
	if not forced and not lily_white_spawning_enabled:
		return null
	if entities_layer == null and playfield:
		entities_layer = playfield.get_node_or_null("%Entities")
	if entities_layer == null:
		return null
	
	if active_lily_white != null and is_instance_valid(active_lily_white):
		active_lily_white.stop_and_free()
		active_lily_white = null
	
	var lily: LilyWhite = LILY_WHITE_SCENE.instantiate() as LilyWhite
	lily.playfield = playfield
	active_lily_white = lily
	lily.tree_exited.connect(func():
		if active_lily_white == lily:
			active_lily_white = null
	)
	if on_defeated.is_valid():
		lily.defeated.connect(func(death_pos: Vector2):
			on_defeated.call(death_pos)
		)
	entities_layer.add_child(lily)
	lily.start_sequence()
	return lily

func spawn_defeat_pickups(center_pos: Vector2, forced_type: int = -1, on_collected: Callable = Callable()) -> PickupItem:
	if entities_layer == null and playfield:
		entities_layer = playfield.get_node_or_null("%Entities")
	if entities_layer == null or PICKUP_ITEM_SCENE == null:
		return null
	
	var chosen_type: PickupItem.Type
	if forced_type >= 0 and forced_type < 4:
		chosen_type = forced_type as PickupItem.Type
	else:
		var pickup_types: Array[PickupItem.Type] = [
			PickupItem.Type.G,
			PickupItem.Type.POINT,
			PickupItem.Type.EX,
			PickupItem.Type.BULLET
		]
		# Seeded like spawn_boss(), not pick_random(): the owning client decides what the item
		# does, and the opponent's copy of this field has to show that same item rising.
		var r_seed: int = playfield.get("_round_seed") if playfield and "_round_seed" in playfield else 99991
		var d_count: int = playfield.get("_pickup_drop_count") if playfield and "_pickup_drop_count" in playfield else 0
		if playfield and "_pickup_drop_count" in playfield:
			playfield.set("_pickup_drop_count", d_count + 1)
		var rng := RandomNumberGenerator.new()
		rng.seed = r_seed + d_count * 104729
		chosen_type = pickup_types[rng.randi_range(0, pickup_types.size() - 1)]
	
	var item: PickupItem = PICKUP_ITEM_SCENE.instantiate() as PickupItem
	var item_pos := Vector2(
		clampf(center_pos.x, 30.0, _get_playfield_width() - 30.0),
		center_pos.y
	)
	item.setup(chosen_type, item_pos, playfield, 390.0)
	if on_collected.is_valid():
		item.collected.connect(on_collected)
	entities_layer.add_child(item)
	return item

func clear_all_enemies() -> void:
	dispel_active_boss()
	if active_lily_white != null and is_instance_valid(active_lily_white):
		active_lily_white.stop_and_free()
		active_lily_white = null
	if entities_layer == null and playfield:
		entities_layer = playfield.get_node_or_null("%Entities")
	if entities_layer:
		var p: Player = _get_player()
		for child in entities_layer.get_children():
			if child != p:
				child.set_process(false)
				child.set_physics_process(false)
				if child is Area2D:
					child.set_deferred("monitoring", false)
					child.set_deferred("monitorable", false)
				child.visible = false
				child.queue_free()

func reset() -> void:
	clear_all_enemies()
	if fairy_spawner:
		fairy_spawner.reset()
