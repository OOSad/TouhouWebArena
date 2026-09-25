class_name NodePool
extends RefCounted

## Generic, reusable Object Pool for Godot 4 nodes.
## Minimizes allocations and garbage collection overhead by recycling instances.

var scene: PackedScene
var parent_node: Node
var _pool: Array[Node] = []
var _active_set: Dictionary = {} # Set of active Node references: Node -> true
var _active_count: int = 0

func _init(p_scene: PackedScene, p_parent: Node, initial_capacity: int = 160) -> void:
	scene = p_scene
	parent_node = p_parent
	
	for i in range(initial_capacity):
		var instance: Node = _create_instance()
		_pool.append(instance)

func _create_instance() -> Node:
	var instance: Node = scene.instantiate()
	if "pool" in instance:
		instance.pool = self
	
	if is_instance_valid(parent_node):
		parent_node.add_child(instance)
	
	_deactivate(instance)
	return instance

## Acquires an active instance from the pool, expanding dynamically if necessary.
func acquire() -> Node:
	var instance: Node
	if _pool.is_empty():
		instance = _create_instance()
	else:
		instance = _pool.pop_back()
	
	_active_set[instance] = true
	_active_count = _active_set.size()
	_activate(instance)
	return instance

## Returns an instance to the pool, resetting and putting it to sleep.
func release(instance: Node) -> void:
	if not is_instance_valid(instance):
		return
	
	# Guard against double-release or releasing an instance not currently acquired
	if not _active_set.has(instance):
		return
	
	_active_set.erase(instance)
	_active_count = _active_set.size()
	_deactivate(instance)
	_pool.append(instance)

func _activate(instance: Node) -> void:
	instance.visible = true
	instance.process_mode = Node.PROCESS_MODE_INHERIT
	
	if instance is CanvasItem:
		instance.modulate.a = 1.0
	
	if instance is CollisionObject2D:
		instance.set_deferred("monitorable", true)
	
	if instance.has_method("on_pool_acquire"):
		instance.on_pool_acquire()

func _deactivate(instance: Node) -> void:
	instance.visible = false
	instance.process_mode = Node.PROCESS_MODE_DISABLED
	
	if instance is CollisionObject2D:
		instance.set_deferred("monitorable", false)
		if instance is Area2D:
			instance.set_deferred("monitoring", false)
	
	if instance is Node2D:
		# Move inactive pooled instances far offscreen so spatial/range queries never match them
		instance.position = Vector2(-9999.0, -9999.0)
	
	if instance.has_method("on_pool_release"):
		instance.on_pool_release()

## Cleans up all pooled instances.
func clear() -> void:
	_active_set.clear()
	_active_count = 0
	for node in _pool:
		if is_instance_valid(node):
			node.queue_free()
	_pool.clear()

## Deactivates and reclaims all active instances belonging to this pool currently in the scene.
func release_all_active() -> void:
	var active_nodes: Array = _active_set.keys()
	for instance in active_nodes:
		if is_instance_valid(instance):
			release(instance)
	_active_set.clear()
	_active_count = 0

## Returns the count of active instances currently acquired from this pool.
func get_active_count() -> int:
	return _active_count

## Returns an array of references to currently active instances in this pool.
func get_active_nodes() -> Array:
	return _active_set.keys()


