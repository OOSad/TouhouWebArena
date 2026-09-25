class_name BambooRoad3D
extends Node3D

@export var scroll_speed: float = 4.8
@export var sway_amplitude: float = 0.45
@export var sway_frequency: float = 0.48
@export var roll_amplitude_deg: float = 1.3
@export var camera_height: float = 6.6
@export var camera_pitch_deg: float = -38.5
@export var time_offset: float = 0.0

@export var fog_start: float = 3.5
@export var fog_end: float = 14.0
@export var fog_color: Color = Color(0.1, 0.22, 0.14, 1.0)

@export var tile_count: int = 8
@export var tile_length: float = 8.0
@export var tree_count_per_side: int = 44
@export var tree_x_min: float = 1.9
@export var tree_x_max: float = 5.2
@export var tree_lean_min_deg: float = 6.0
@export var tree_lean_max_deg: float = 13.0

const FLOOR_TEX: Texture2D = preload("res://resources/dat_textures/stage_bamboo_floor.tres")
const STALK_TEX: Texture2D = preload("res://resources/dat_textures/stage_bamboo_stalk.tres")
const LEAVES_A_TEX: Texture2D = preload("res://resources/dat_textures/stage_bamboo_leaves_a.tres")
const LEAVES_B_TEX: Texture2D = preload("res://resources/dat_textures/stage_bamboo_leaves_b.tres")
const FOG_SHADER: Shader = preload("res://scenes/stages/bamboo_road/bamboo_stage_fog.gdshader")
## PoFV's leaf art is ringed by a soft black halo: every partly transparent pixel is near
## black. PoFV blends it in faintly, but a hard cutoff draws it as solid black specks, so
## only the fully solid leaf pixels are kept, minus the near-black shading blotches inside the
## clusters (per the user, who found the dark fringe ugly). The art itself is untouched.
const LEAF_ALPHA_CUTOFF: float = 0.95
const LEAF_DARK_CUTOFF: float = 0.08

const STALK_LOCAL: Transform3D = Transform3D(Basis(), Vector3(0.0, 5.25, 0.0))

class FloorTileData:
	var position: Vector3 = Vector3.ZERO

class TreeData:
	var is_left: bool = true
	var position: Vector3 = Vector3.ZERO
	var rotation: Vector3 = Vector3.ZERO
	var stalk_idx: int = -1
	var _temp_use_a_list: Array[bool] = []
	var leaves_a_indices: Array[int] = []
	var leaves_a_locals: Array[Transform3D] = []
	var leaves_b_indices: Array[int] = []
	var leaves_b_locals: Array[Transform3D] = []

	func get_child_count() -> int:
		return 1 + leaves_a_indices.size() + leaves_b_indices.size()

@onready var camera: Camera3D = get_node_or_null("Camera3D")
@onready var world_container: Node3D = get_node_or_null("WorldContainer")

var _floor_tiles: Array[FloorTileData] = []
var _trees: Array[TreeData] = []
var _time: float = 0.0

var _floor_mat: ShaderMaterial
var _stalk_mat: ShaderMaterial
var _leaves_a_mat: ShaderMaterial
var _leaves_b_mat: ShaderMaterial

var _floor_mesh: PlaneMesh
var _stalk_mesh: QuadMesh
var _leaves_a_mesh: QuadMesh
var _leaves_b_mesh: QuadMesh

var _floor_multimesh: MultiMeshInstance3D = null
var _stalk_multimesh: MultiMeshInstance3D = null
var _leaves_a_multimesh: MultiMeshInstance3D = null
var _leaves_b_multimesh: MultiMeshInstance3D = null

func _ready() -> void:
	if camera == null:
		camera = get_node_or_null("Camera3D")
	if world_container == null:
		world_container = get_node_or_null("WorldContainer")
	if world_container == null:
		world_container = Node3D.new()
		world_container.name = "WorldContainer"
		add_child(world_container)
	
	if camera:
		var we: WorldEnvironment = get_node_or_null("WorldEnvironment")
		if we and we.environment:
			camera.environment = we.environment
		
	_init_materials()
	_init_meshes()
	_build_floor()
	_build_bamboo_groves()
	_update_camera(0.0)

func _init_materials() -> void:
	_floor_mat = ShaderMaterial.new()
	_floor_mat.shader = FOG_SHADER
	_floor_mat.set_shader_parameter("texture_albedo", FLOOR_TEX)
	_floor_mat.set_shader_parameter("fog_color", fog_color)
	_floor_mat.set_shader_parameter("fog_start", fog_start)
	_floor_mat.set_shader_parameter("fog_end", fog_end)
	_floor_mat.set_shader_parameter("use_alpha_scissor", false)

	_stalk_mat = ShaderMaterial.new()
	_stalk_mat.shader = FOG_SHADER
	_stalk_mat.set_shader_parameter("texture_albedo", STALK_TEX)
	_stalk_mat.set_shader_parameter("fog_color", fog_color)
	_stalk_mat.set_shader_parameter("fog_start", fog_start)
	_stalk_mat.set_shader_parameter("fog_end", fog_end)
	_stalk_mat.set_shader_parameter("use_alpha_scissor", true)
	_stalk_mat.set_shader_parameter("alpha_scissor_threshold", 0.30)

	_leaves_a_mat = ShaderMaterial.new()
	_leaves_a_mat.shader = FOG_SHADER
	_leaves_a_mat.set_shader_parameter("texture_albedo", LEAVES_A_TEX)
	_leaves_a_mat.set_shader_parameter("fog_color", fog_color)
	_leaves_a_mat.set_shader_parameter("fog_start", fog_start)
	_leaves_a_mat.set_shader_parameter("fog_end", fog_end)
	_leaves_a_mat.set_shader_parameter("use_alpha_scissor", true)
	_leaves_a_mat.set_shader_parameter("alpha_scissor_threshold", LEAF_ALPHA_CUTOFF)
	_leaves_a_mat.set_shader_parameter("dark_cutoff", LEAF_DARK_CUTOFF)

	_leaves_b_mat = ShaderMaterial.new()
	_leaves_b_mat.shader = FOG_SHADER
	_leaves_b_mat.set_shader_parameter("texture_albedo", LEAVES_B_TEX)
	_leaves_b_mat.set_shader_parameter("fog_color", fog_color)
	_leaves_b_mat.set_shader_parameter("fog_start", fog_start)
	_leaves_b_mat.set_shader_parameter("fog_end", fog_end)
	_leaves_b_mat.set_shader_parameter("use_alpha_scissor", true)
	_leaves_b_mat.set_shader_parameter("alpha_scissor_threshold", LEAF_ALPHA_CUTOFF)
	_leaves_b_mat.set_shader_parameter("dark_cutoff", LEAF_DARK_CUTOFF)

func _init_meshes() -> void:
	_floor_mesh = PlaneMesh.new()
	_floor_mesh.size = Vector2(28.0, tile_length)

	_stalk_mesh = QuadMesh.new()
	# PoFV's stalk is 16x512 edge to edge; 0.33 wide keeps the visible thickness it had.
	_stalk_mesh.size = Vector2(0.33, 10.5)

	# Leaves A is PoFV's 288x254 cluster (aspect ~1.13:1), at the height it had before
	_leaves_a_mesh = QuadMesh.new()
	_leaves_a_mesh.size = Vector2(3.04, 2.68)

	# Leaves B is PoFV's 237x192 cluster (aspect ~1.23:1), at the height it had before
	_leaves_b_mesh = QuadMesh.new()
	_leaves_b_mesh.size = Vector2(2.72, 2.2)

func _build_floor() -> void:
	var floor_mm := MultiMesh.new()
	floor_mm.transform_format = MultiMesh.TRANSFORM_3D
	floor_mm.instance_count = tile_count
	floor_mm.mesh = _floor_mesh

	_floor_multimesh = MultiMeshInstance3D.new()
	_floor_multimesh.name = "FloorMultiMesh"
	_floor_multimesh.multimesh = floor_mm
	_floor_multimesh.material_override = _floor_mat
	world_container.add_child(_floor_multimesh)

	_floor_tiles.clear()
	var start_z: float = 6.0
	for i in range(tile_count):
		var tile := FloorTileData.new()
		tile.position = Vector3(0.0, 0.0, start_z - (i * tile_length))
		_floor_tiles.append(tile)
		floor_mm.set_instance_transform(i, Transform3D(Basis(), tile.position))

func _build_bamboo_groves() -> void:
	var total_span: float = tile_count * tile_length
	var z_step: float = total_span / float(tree_count_per_side)
	
	var rng := RandomNumberGenerator.new()
	rng.seed = 90901

	_trees.clear()
	var total_trees: int = tree_count_per_side * 2
	var leaves_a_count: int = 0
	var leaves_b_count: int = 0

	# 1. First pass: build all tree data and tally leaves counts
	# Left trees
	for i in range(tree_count_per_side):
		var base_z: float = 4.0 - (i * z_step) + rng.randf_range(-0.8, 0.8)
		var x: float = rng.randf_range(-tree_x_max, -tree_x_min)
		var tree := _create_tree_data(rng, true, i)
		tree.position = Vector3(x, 0.0, base_z)
		for is_a in tree._temp_use_a_list:
			if is_a:
				tree.leaves_a_indices.append(leaves_a_count)
				leaves_a_count += 1
			else:
				tree.leaves_b_indices.append(leaves_b_count)
				leaves_b_count += 1
		_trees.append(tree)

	# Right trees
	for i in range(tree_count_per_side):
		var base_z: float = 4.0 - (i * z_step) + rng.randf_range(-0.8, 0.8)
		var x: float = rng.randf_range(tree_x_min, tree_x_max)
		var tree := _create_tree_data(rng, false, tree_count_per_side + i)
		tree.position = Vector3(x, 0.0, base_z)
		for is_a in tree._temp_use_a_list:
			if is_a:
				tree.leaves_a_indices.append(leaves_a_count)
				leaves_a_count += 1
			else:
				tree.leaves_b_indices.append(leaves_b_count)
				leaves_b_count += 1
		_trees.append(tree)

	# 2. Instantiate the 3 MultiMeshInstance3Ds for stalks, leaves A, and leaves B
	var stalk_mm := MultiMesh.new()
	stalk_mm.transform_format = MultiMesh.TRANSFORM_3D
	stalk_mm.instance_count = total_trees
	stalk_mm.mesh = _stalk_mesh
	_stalk_multimesh = MultiMeshInstance3D.new()
	_stalk_multimesh.name = "StalkMultiMesh"
	_stalk_multimesh.multimesh = stalk_mm
	_stalk_multimesh.material_override = _stalk_mat
	world_container.add_child(_stalk_multimesh)

	var leaves_a_mm := MultiMesh.new()
	leaves_a_mm.transform_format = MultiMesh.TRANSFORM_3D
	leaves_a_mm.instance_count = leaves_a_count
	leaves_a_mm.mesh = _leaves_a_mesh
	_leaves_a_multimesh = MultiMeshInstance3D.new()
	_leaves_a_multimesh.name = "LeavesAMultiMesh"
	_leaves_a_multimesh.multimesh = leaves_a_mm
	_leaves_a_multimesh.material_override = _leaves_a_mat
	world_container.add_child(_leaves_a_multimesh)

	var leaves_b_mm := MultiMesh.new()
	leaves_b_mm.transform_format = MultiMesh.TRANSFORM_3D
	leaves_b_mm.instance_count = leaves_b_count
	leaves_b_mm.mesh = _leaves_b_mesh
	_leaves_b_multimesh = MultiMeshInstance3D.new()
	_leaves_b_multimesh.name = "LeavesBMultiMesh"
	_leaves_b_multimesh.multimesh = leaves_b_mm
	_leaves_b_multimesh.material_override = _leaves_b_mat
	world_container.add_child(_leaves_b_multimesh)

	# 3. Apply initial transforms to all instances
	for tree in _trees:
		_update_tree_transforms(tree)

func _create_tree_data(rng: RandomNumberGenerator, is_left: bool, stalk_idx: int) -> TreeData:
	var tree := TreeData.new()
	tree.is_left = is_left
	tree.stalk_idx = stalk_idx

	# Outward lean:
	# Positive Z rotation leans left (outward for left trees)
	# Negative Z rotation leans right (outward for right trees)
	var lean_deg: float = rng.randf_range(tree_lean_min_deg, tree_lean_max_deg)
	tree.rotation.z = deg_to_rad(lean_deg if is_left else -lean_deg)
	tree.rotation.x = deg_to_rad(rng.randf_range(-2.5, 2.5))

	# 1. Lower foliage branch (Y = 2.8 - 4.0)
	var use_a1: bool = rng.randf() > 0.5
	var y1: float = rng.randf_range(2.8, 4.0)
	var rot1: float = rng.randf_range(-6.0, 6.0)
	var local1 := _calc_foliage_local(rng, is_left, use_a1, y1, 0.85, rot1)
	tree._temp_use_a_list.append(use_a1)
	if use_a1:
		tree.leaves_a_locals.append(local1)
	else:
		tree.leaves_b_locals.append(local1)

	# 2. Mid foliage branch (Y = 4.4 - 5.8) - eye-level from elevated camera
	var use_a2: bool = rng.randf() > 0.4
	var y2: float = rng.randf_range(4.4, 5.8)
	var rot2: float = rng.randf_range(-7.0, 7.0)
	var local2 := _calc_foliage_local(rng, is_left, use_a2, y2, 1.0, rot2)
	tree._temp_use_a_list.append(use_a2)
	if use_a2:
		tree.leaves_a_locals.append(local2)
	else:
		tree.leaves_b_locals.append(local2)

	# 3. Upper foliage branch (Y = 6.2 - 7.6)
	var use_a3: bool = rng.randf() > 0.3
	var y3: float = rng.randf_range(6.2, 7.6)
	var rot3: float = rng.randf_range(-6.0, 6.0)
	var local3 := _calc_foliage_local(rng, is_left, use_a3, y3, 0.95, rot3)
	tree._temp_use_a_list.append(use_a3)
	if use_a3:
		tree.leaves_a_locals.append(local3)
	else:
		tree.leaves_b_locals.append(local3)

	# 4. Top crown (Y = 8.0 - 9.5)
	if rng.randf() > 0.2:
		var crown_x: float = rng.randf_range(-0.15, 0.15)
		var crown_y: float = rng.randf_range(8.0, 9.5)
		var crown_basis := Basis().scaled(Vector3(0.85, 0.85, 0.85))
		var crown_local := Transform3D(crown_basis, Vector3(crown_x, crown_y, 0.0))
		tree._temp_use_a_list.append(false)
		tree.leaves_b_locals.append(crown_local)

	return tree

func _calc_foliage_local(
	rng: RandomNumberGenerator,
	is_left: bool,
	use_a: bool,
	y_pos: float,
	scale_mult: float,
	rot_deg: float
) -> Transform3D:
	var offset_x: float
	if use_a:
		var base_dist := 1.05 * scale_mult
		offset_x = (base_dist if is_left else -base_dist) + rng.randf_range(-0.15, 0.15)
	else:
		var base_dist := 0.55 * scale_mult
		offset_x = (base_dist if is_left else -base_dist) + rng.randf_range(-0.15, 0.15)

	var pos := Vector3(offset_x, y_pos, rng.randf_range(-0.1, 0.1))
	var sx: float = scale_mult if is_left else -scale_mult
	var basis := Basis.from_euler(Vector3(0.0, 0.0, deg_to_rad(rot_deg if is_left else -rot_deg))).scaled(Vector3(sx, scale_mult, scale_mult))
	return Transform3D(basis, pos)

func _update_tree_transforms(tree: TreeData) -> void:
	var tree_basis := Basis.from_euler(Vector3(tree.rotation.x, 0.0, tree.rotation.z))
	var tree_trans := Transform3D(tree_basis, tree.position)

	_stalk_multimesh.multimesh.set_instance_transform(tree.stalk_idx, tree_trans * STALK_LOCAL)

	for k in range(tree.leaves_a_indices.size()):
		_leaves_a_multimesh.multimesh.set_instance_transform(tree.leaves_a_indices[k], tree_trans * tree.leaves_a_locals[k])

	for k in range(tree.leaves_b_indices.size()):
		_leaves_b_multimesh.multimesh.set_instance_transform(tree.leaves_b_indices[k], tree_trans * tree.leaves_b_locals[k])

func _process(delta: float) -> void:
	_time += delta
	_update_camera(_time)
	_scroll_world(delta)

func _update_camera(time: float) -> void:
	if not camera:
		return
	var sway: float = sin((time + time_offset) * sway_frequency)
	camera.position.x = sway * sway_amplitude
	camera.position.y = camera_height
	camera.position.z = 0.0

	camera.rotation.x = deg_to_rad(camera_pitch_deg)
	camera.rotation.y = -sway * deg_to_rad(1.2)
	camera.rotation.z = -sway * deg_to_rad(roll_amplitude_deg)

func _scroll_world(delta: float) -> void:
	var move_dist: float = scroll_speed * delta

	# Scroll floor tiles
	var max_tile_z: float = 6.0 + tile_length
	var min_tile_z: float = 9999.0
	for tile in _floor_tiles:
		if tile.position.z < min_tile_z:
			min_tile_z = tile.position.z

	for i in range(_floor_tiles.size()):
		var tile: FloorTileData = _floor_tiles[i]
		tile.position.z += move_dist
		if tile.position.z > max_tile_z:
			# Recycle tile to far end
			tile.position.z = min_tile_z - tile_length + (tile.position.z - max_tile_z)
			min_tile_z = tile.position.z
		_floor_multimesh.multimesh.set_instance_transform(i, Transform3D(Basis(), tile.position))

	# Scroll bamboo trees
	var max_tree_z: float = 6.0
	var total_span: float = tile_count * tile_length
	for tree in _trees:
		tree.position.z += move_dist
		if tree.position.z > max_tree_z:
			# Recycle tree to back of grove
			tree.position.z -= total_span
			# Randomize x slightly on wrap for organic variation
			if tree.is_left:
				tree.position.x = randf_range(-tree_x_max, -tree_x_min)
			else:
				tree.position.x = randf_range(tree_x_min, tree_x_max)
			var lean_deg: float = randf_range(tree_lean_min_deg, tree_lean_max_deg)
			tree.rotation.z = deg_to_rad(lean_deg if tree.is_left else -lean_deg)
		_update_tree_transforms(tree)
