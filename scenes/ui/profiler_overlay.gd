class_name ProfilerOverlay
extends CanvasLayer

## Unity-style real-time Performance Profiler and Telemetry Overlay for Touhou Web Arena.
## Designed to fit in the right margin without obscuring combat playfields.

var p1_playfield: Playfield = null
var p2_playfield: Playfield = null
var arena: Control = null

var is_open: bool = false
var _frame_history: Array[float] = [] # stores frame times in seconds
const MAX_HISTORY: int = 120

var _update_timer: float = 0.0
const UPDATE_INTERVAL: float = 0.1 # 10 updates per second for text

var _copy_feedback_timer: float = 0.0

# UI Node References
@onready var root_panel: PanelContainer = %RootPanel
@onready var fps_badge: Label = %FPSBadge
@onready var copy_btn: Button = %CopyBtn
@onready var close_btn: Button = %CloseBtn
@onready var graph_control: Control = %GraphControl
@onready var fps_details_label: Label = %FPSDetailsLabel
@onready var cpu_timing_label: Label = %CPUTimingLabel
@onready var render_stats_label: Label = %RenderStatsLabel
@onready var physics_stats_label: Label = %PhysicsStatsLabel
@onready var memory_stats_label: Label = %MemoryStatsLabel
@onready var orphans_label: Label = %OrphansLabel
@onready var p1_gameplay_label: Label = %P1GameplayLabel
@onready var p2_gameplay_label: Label = %P2GameplayLabel
@onready var total_gameplay_label: Label = %TotalGameplayLabel
@onready var status_hint_label: Label = %StatusHintLabel

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	visible = false
	is_open = false
	
	if copy_btn:
		copy_btn.pressed.connect(copy_snapshot_to_clipboard)
	if close_btn:
		close_btn.pressed.connect(close)
	if graph_control:
		graph_control.draw.connect(_on_graph_draw)

func setup(p_arena: Control, p1: Playfield, p2: Playfield) -> void:
	arena = p_arena
	p1_playfield = p1
	p2_playfield = p2

func toggle() -> void:
	if is_open:
		close()
	else:
		open()

func open() -> void:
	is_open = true
	visible = true
	_refresh_readouts()

func close() -> void:
	is_open = false
	visible = false

func _process(delta: float) -> void:
	# Always record frame history so when opened, stats are immediately populated
	var dt: float = delta
	_frame_history.append(dt)
	if _frame_history.size() > MAX_HISTORY:
		_frame_history.pop_front()
	
	if not is_open:
		return
	
	if graph_control:
		graph_control.queue_redraw()
	
	_update_timer += delta
	if _update_timer >= UPDATE_INTERVAL:
		_update_timer = 0.0
		_refresh_readouts()
	
	if _copy_feedback_timer > 0.0:
		_copy_feedback_timer -= delta
		if _copy_feedback_timer <= 0.0 and status_hint_label:
			status_hint_label.text = "[F9] Copy Snapshot  |  [F10] Toggle"
			status_hint_label.add_theme_color_override("font_color", Color(0.65, 0.75, 0.7))

func get_current_fps() -> float:
	return Performance.get_monitor(Performance.TIME_FPS)

func get_average_fps() -> float:
	if _frame_history.is_empty():
		return get_current_fps()
	var total_time: float = 0.0
	for dt in _frame_history:
		total_time += dt
	if total_time <= 0.0:
		return 60.0
	return float(_frame_history.size()) / total_time

func get_1_percent_low_fps() -> float:
	if _frame_history.size() < 10:
		return get_current_fps()
	var sorted_history: Array[float] = _frame_history.duplicate()
	sorted_history.sort() # ascending order (fastest to slowest)
	# The slowest 1% (or at least 2 frames) are at the end of the array
	var sample_count: int = maxi(1, int(sorted_history.size() * 0.05)) # bottom 5% gives solid metric
	var worst_time: float = 0.0
	for i in range(sample_count):
		worst_time += sorted_history[sorted_history.size() - 1 - i]
	worst_time /= float(sample_count)
	if worst_time <= 0.0:
		return 60.0
	return 1.0 / worst_time

func get_min_fps() -> float:
	if _frame_history.is_empty():
		return get_current_fps()
	var max_dt: float = 0.0
	for dt in _frame_history:
		if dt > max_dt:
			max_dt = dt
	return 1.0 / max_dt if max_dt > 0.0 else 60.0

func get_max_fps() -> float:
	if _frame_history.is_empty():
		return get_current_fps()
	var min_dt: float = 999.0
	for dt in _frame_history:
		if dt > 0.0 and dt < min_dt:
			min_dt = dt
	return 1.0 / min_dt if min_dt < 999.0 else 60.0

func _refresh_readouts() -> void:
	var cur_fps: float = get_current_fps()
	var avg_fps: float = get_average_fps()
	var low_fps: float = get_1_percent_low_fps()
	var min_fps: float = get_min_fps()
	var max_fps: float = get_max_fps()
	
	var frame_ms: float = (1.0 / cur_fps * 1000.0) if cur_fps > 0.0 else 16.67
	var proc_ms: float = Performance.get_monitor(Performance.TIME_PROCESS) * 1000.0
	var phys_ms: float = Performance.get_monitor(Performance.TIME_PHYSICS_PROCESS) * 1000.0
	
	var draw_calls: int = int(Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME))
	var primitives: int = int(Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME))
	var objects_drawn: int = int(Performance.get_monitor(Performance.RENDER_TOTAL_OBJECTS_IN_FRAME))
	var vram_mb: float = Performance.get_monitor(Performance.RENDER_VIDEO_MEM_USED) / (1024.0 * 1024.0)
	
	var phys_active: int = int(Performance.get_monitor(Performance.PHYSICS_2D_ACTIVE_OBJECTS))
	var phys_pairs: int = int(Performance.get_monitor(Performance.PHYSICS_2D_COLLISION_PAIRS))
	var phys_islands: int = int(Performance.get_monitor(Performance.PHYSICS_2D_ISLAND_COUNT))
	
	var mem_static_mb: float = Performance.get_monitor(Performance.MEMORY_STATIC) / (1024.0 * 1024.0)
	var mem_peak_mb: float = Performance.get_monitor(Performance.MEMORY_STATIC_MAX) / (1024.0 * 1024.0)
	var node_count: int = int(Performance.get_monitor(Performance.OBJECT_NODE_COUNT))
	var obj_count: int = int(Performance.get_monitor(Performance.OBJECT_COUNT))
	var orphan_nodes: int = int(Performance.get_monitor(Performance.OBJECT_ORPHAN_NODE_COUNT))
	
	# FPS Badge
	if fps_badge:
		fps_badge.text = "%.0f FPS" % cur_fps
		if cur_fps >= 55.0:
			fps_badge.add_theme_color_override("font_color", Color(0.3, 1.0, 0.4))
		elif cur_fps >= 35.0:
			fps_badge.add_theme_color_override("font_color", Color(1.0, 0.85, 0.2))
		else:
			fps_badge.add_theme_color_override("font_color", Color(1.0, 0.35, 0.35))
	
	if fps_details_label:
		fps_details_label.text = "FPS: %.1f  (Avg: %.1f | 1%% Low: %.1f)\nRange: [%.0f - %.0f FPS]" % [
			cur_fps, avg_fps, low_fps, min_fps, max_fps
		]
	
	if cpu_timing_label:
		cpu_timing_label.text = "Frame: %4.1f ms\nCPU Proc: %4.1f ms | Phys: %4.1f ms" % [
			frame_ms, proc_ms, phys_ms
		]
	
	if render_stats_label:
		render_stats_label.text = "Draw Calls: %d | Prims: %d\n2D Canvas Items: %d | VRAM: %.1f MB" % [
			draw_calls, primitives, objects_drawn, vram_mb
		]
	
	if physics_stats_label:
		physics_stats_label.text = "Phys2D Active: %d | Pairs: %d\nIslands: %d" % [
			phys_active, phys_pairs, phys_islands
		]
	
	if memory_stats_label:
		memory_stats_label.text = "RAM: %.1f MB (Peak: %.1f MB)\nNodes: %d | Objs: %d" % [
			mem_static_mb, mem_peak_mb, node_count, obj_count
		]
	
	if orphans_label:
		if orphan_nodes > 0:
			orphans_label.text = "⚠️ ORPHAN NODES LEAKING: %d" % orphan_nodes
			orphans_label.add_theme_color_override("font_color", Color(1.0, 0.3, 0.3))
		else:
			orphans_label.text = "Orphan Nodes: 0 (No Leaks)"
			orphans_label.add_theme_color_override("font_color", Color(0.6, 0.85, 0.65))
	
	# Danmaku Telemetry
	var t1: Dictionary = p1_playfield.get_entity_telemetry() if (p1_playfield and p1_playfield.has_method("get_entity_telemetry")) else {}
	var t2: Dictionary = p2_playfield.get_entity_telemetry() if (p2_playfield and p2_playfield.has_method("get_entity_telemetry")) else {}
	
	if p1_gameplay_label and not t1.is_empty():
		var cap_str: String = "%d" % p1_playfield.max_active_bullets if p1_playfield.max_active_bullets > 0 else "OFF"
		p1_gameplay_label.text = "P1: %d/%s Bullets (Pel: %d | Dan: %d)\n   Fairies: %d | Spirits: %d" % [
			t1.total_bullets, cap_str, t1.pellets, t1.danmaku, t1.fairies, t1.spirits
		]
	
	if p2_gameplay_label and not t2.is_empty():
		var cap_str: String = "%d" % p2_playfield.max_active_bullets if p2_playfield.max_active_bullets > 0 else "OFF"
		p2_gameplay_label.text = "P2: %d/%s Bullets (Pel: %d | Dan: %d)\n   Fairies: %d | Spirits: %d" % [
			t2.total_bullets, cap_str, t2.pellets, t2.danmaku, t2.fairies, t2.spirits
		]
	
	if total_gameplay_label and not t1.is_empty() and not t2.is_empty():
		var total_b: int = t1.total_bullets + t2.total_bullets
		var total_e: int = t1.total_entities + t2.total_entities
		var motes: int = arena.mote_pool.get_active_count() if (arena and arena.mote_pool) else 0
		total_gameplay_label.text = "Total Active: %d Bullets | %d Entities\nActive Motes: %d" % [
			total_b, total_e, motes
		]

func _on_graph_draw() -> void:
	if graph_control == null:
		return
	
	var w: float = graph_control.size.x
	var h: float = graph_control.size.y
	
	# Background
	graph_control.draw_rect(Rect2(0, 0, w, h), Color(0.04, 0.07, 0.05, 0.95))
	graph_control.draw_rect(Rect2(0, 0, w, h), Color(0.2, 0.35, 0.25, 0.8), false, 1.0)
	
	# Reference line: 16.6ms (60 FPS)
	var max_ms: float = 40.0 # Graph vertical scale: 0 to 40ms
	var y_60fps: float = h - (16.67 / max_ms) * h
	graph_control.draw_line(Vector2(0, y_60fps), Vector2(w, y_60fps), Color(0.2, 0.8, 0.5, 0.45), 1.0)
	
	# Reference line: 33.3ms (30 FPS)
	var y_30fps: float = h - (33.33 / max_ms) * h
	graph_control.draw_line(Vector2(0, y_30fps), Vector2(w, y_30fps), Color(0.9, 0.7, 0.2, 0.45), 1.0)
	
	if _frame_history.is_empty():
		return
	
	var step_x: float = w / float(MAX_HISTORY)
	var points: PackedVector2Array = []
	var start_idx: int = MAX_HISTORY - _frame_history.size()
	
	for i in range(_frame_history.size()):
		var dt_ms: float = _frame_history[i] * 1000.0
		var x: float = (start_idx + i) * step_x
		var y: float = clampf(h - (dt_ms / max_ms) * h, 2.0, h - 2.0)
		points.append(Vector2(x, y))
		
		# Draw spike column if frame exceeded 33.3ms
		if dt_ms > 33.33:
			graph_control.draw_line(Vector2(x, h), Vector2(x, y), Color(1.0, 0.3, 0.3, 0.7), 1.5)
	
	if points.size() >= 2:
		graph_control.draw_polyline(points, Color(0.35, 0.9, 0.6, 0.9), 1.5)

func generate_snapshot_text() -> String:
	var cur_fps: float = get_current_fps()
	var avg_fps: float = get_average_fps()
	var low_fps: float = get_1_percent_low_fps()
	var min_fps: float = get_min_fps()
	var max_fps: float = get_max_fps()
	
	var frame_ms: float = (1.0 / cur_fps * 1000.0) if cur_fps > 0.0 else 16.67
	var proc_ms: float = Performance.get_monitor(Performance.TIME_PROCESS) * 1000.0
	var phys_ms: float = Performance.get_monitor(Performance.TIME_PHYSICS_PROCESS) * 1000.0
	
	var draw_calls: int = int(Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME))
	var primitives: int = int(Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME))
	var objects_drawn: int = int(Performance.get_monitor(Performance.RENDER_TOTAL_OBJECTS_IN_FRAME))
	var vram_mb: float = Performance.get_monitor(Performance.RENDER_VIDEO_MEM_USED) / (1024.0 * 1024.0)
	
	var phys_active: int = int(Performance.get_monitor(Performance.PHYSICS_2D_ACTIVE_OBJECTS))
	var phys_pairs: int = int(Performance.get_monitor(Performance.PHYSICS_2D_COLLISION_PAIRS))
	var phys_islands: int = int(Performance.get_monitor(Performance.PHYSICS_2D_ISLAND_COUNT))
	
	var mem_static_mb: float = Performance.get_monitor(Performance.MEMORY_STATIC) / (1024.0 * 1024.0)
	var mem_peak_mb: float = Performance.get_monitor(Performance.MEMORY_STATIC_MAX) / (1024.0 * 1024.0)
	var node_count: int = int(Performance.get_monitor(Performance.OBJECT_NODE_COUNT))
	var obj_count: int = int(Performance.get_monitor(Performance.OBJECT_COUNT))
	var orphan_nodes: int = int(Performance.get_monitor(Performance.OBJECT_ORPHAN_NODE_COUNT))
	
	var platform_str: String = OS.get_name()
	if OS.has_feature("web"):
		platform_str = "Web (HTML5 / WebAssembly)"
	
	var t1: Dictionary = p1_playfield.get_entity_telemetry() if (p1_playfield and p1_playfield.has_method("get_entity_telemetry")) else {}
	var t2: Dictionary = p2_playfield.get_entity_telemetry() if (p2_playfield and p2_playfield.has_method("get_entity_telemetry")) else {}
	
	var p1_str: String = "P1: %d (Pellets: %d, Danmaku: %d) | F: %d, S: %d" % [
		t1.get("total_bullets", 0), t1.get("pellets", 0), t1.get("danmaku", 0), t1.get("fairies", 0), t1.get("spirits", 0)
	] if not t1.is_empty() else "P1: N/A"
	
	var p2_str: String = "P2: %d (Pellets: %d, Danmaku: %d) | F: %d, S: %d" % [
		t2.get("total_bullets", 0), t2.get("pellets", 0), t2.get("danmaku", 0), t2.get("fairies", 0), t2.get("spirits", 0)
	] if not t2.is_empty() else "P2: N/A"
	
	var total_b: int = t1.get("total_bullets", 0) + t2.get("total_bullets", 0)
	var total_e: int = t1.get("total_entities", 0) + t2.get("total_entities", 0)
	var motes: int = arena.mote_pool.get_active_count() if (arena and arena.mote_pool) else 0
	
	var s: String = ""
	s += "=== TOUHOU WEB ARENA PROFILER SNAPSHOT ===\n"
	s += "Platform: %s\n" % platform_str
	s += "FPS: %.1f (Avg: %.1f | 1%% Low: %.1f | Min: %.1f | Max: %.1f)\n" % [cur_fps, avg_fps, low_fps, min_fps, max_fps]
	s += "Frame Time: %.2f ms | Process: %.2f ms | Physics: %.2f ms\n" % [frame_ms, proc_ms, phys_ms]
	s += "Rendering: Draw Calls: %d | Primitives: %d | 2D Items: %d | VRAM: %.1f MB\n" % [draw_calls, primitives, objects_drawn, vram_mb]
	s += "Physics 2D: Active Objects: %d | Collision Pairs: %d | Islands: %d\n" % [phys_active, phys_pairs, phys_islands]
	s += "Memory: Static: %.1f MB (Peak: %.1f MB) | Nodes: %d | Objects: %d | Orphans: %d\n" % [mem_static_mb, mem_peak_mb, node_count, obj_count, orphan_nodes]
	s += "Danmaku:\n"
	s += "  %s\n" % p1_str
	s += "  %s\n" % p2_str
	s += "  Total Bullets: %d | Total Entities: %d | Active Motes: %d\n" % [total_b, total_e, motes]
	s += "=========================================\n"
	return s

func copy_snapshot_to_clipboard() -> String:
	var snapshot: String = generate_snapshot_text()
	DisplayServer.clipboard_set(snapshot)
	print("\n" + snapshot)
	
	_copy_feedback_timer = 2.0
	if status_hint_label:
		status_hint_label.text = "✓ COPIED SNAPSHOT TO CLIPBOARD!"
		status_hint_label.add_theme_color_override("font_color", Color(0.3, 1.0, 0.4))
	return snapshot

