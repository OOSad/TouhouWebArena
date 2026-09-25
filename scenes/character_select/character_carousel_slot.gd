class_name CharacterCarouselSlot
extends Control

## An interlocking alternating wedge slot in the diagonal character carousel.
## The entire wedge is filled with the character's sumi-e silhouette,
## rendered upright ("facing absolute north") instead of tilted with the track.
## - Even slots: wide base on bottom rail (pointing up), face centered in lower wide area, name at top
## - Odd slots: wide base on top rail (pointing down), face centered in upper wide area, name at bottom

signal slot_clicked(index: int)

@export var character_index: int = 0
@export var is_top_heavy: bool = false

var character_data: CharacterData = null
var is_p1: bool = false
var is_p2: bool = false
var is_cpu_p2: bool = false

var polygon_points: PackedVector2Array = PackedVector2Array()
var accent_color: Color = Color(0.5, 0.6, 0.8, 1.0)
var _wedge_mat: ShaderMaterial = null

@onready var wedge_rect: ColorRect = $WedgeRect
@onready var p1_badge: Label = $P1Badge
@onready var p2_badge: Label = $P2Badge

const W_BASE: float = 240.0
const W_TIP: float = 70.0
const H: float = 220.0
const SHIFT: float = 85.0 # (240 - 70) / 2

# Face calibrations for upright absolute-north sampling of Dairi character art
const FRAMING: Dictionary = {
	"reimu": {
		"offset": Vector2(0.476, 0.171),
		"zoom": 1.25,
		"accent": Color(1.0, 0.28, 0.38, 1.0)
	},
	"marisa": {
		"offset": Vector2(0.477, 0.243),
		"zoom": 1.25,
		"accent": Color(1.0, 0.82, 0.25, 1.0)
	},
	"sakuya": {
		"offset": Vector2(0.538, 0.175),
		"zoom": 1.25,
		"accent": Color(0.45, 0.60, 0.90, 1.0)
	},
	"youmu": {
		"offset": Vector2(0.451, 0.147),
		"zoom": 1.12,
		"accent": Color(0.35, 0.90, 0.62, 1.0)
	},
	"cirno": {
		"offset": Vector2(0.473, 0.217),
		"zoom": 1.25,
		"accent": Color(0.28, 0.82, 1.0, 1.0)
	},
	"reisen": {
		# Offset further right/down in the texture than her face alone would suggest, which
		# pulls the art up and left in the wedge - her ears sit above her head and were
		# otherwise pushing her face into the lower right corner.
		"offset": Vector2(0.545, 0.230),
		"zoom": 1.25,
		"accent": Color(0.82, 0.45, 0.95, 1.0)
	},
	"yuuka": {
		"offset": Vector2(0.460, 0.300),
		"zoom": 1.25,
		"accent": Color(0.25, 0.85, 0.45, 1.0)
	},
	"aya": {
		"offset": Vector2(0.500, 0.130),
		"zoom": 1.25,
		"accent": Color(0.92, 0.25, 0.28, 1.0)
	},
	"clownpiece": {
		"offset": Vector2(0.520, 0.250),
		"zoom": 1.25,
		"accent": Color(0.62, 0.3, 0.72, 1.0)
	},
	"random": {
		"offset": Vector2(0.50, 0.22),
		"zoom": 1.25,
		"accent": Color(0.75, 0.65, 0.95, 1.0)
	}
}

func _ready() -> void:
	custom_minimum_size = Vector2(W_BASE, H)
	size = custom_minimum_size
	mouse_filter = Control.MOUSE_FILTER_PASS
	pivot_offset = size * 0.5
	
	if wedge_rect and wedge_rect.material is ShaderMaterial:
		_wedge_mat = wedge_rect.material.duplicate() as ShaderMaterial
		wedge_rect.material = _wedge_mat
		
	_rebuild_geometry()

func setup(idx: int, data: CharacterData, portrait_tex: Texture2D) -> void:
	character_index = idx
	character_data = data
	is_top_heavy = (idx % 2 == 1)
	
	_rebuild_geometry()
	
	var char_id: String = data.character_id if data else ""
	var cfg: Dictionary = FRAMING.get(char_id, {
		"offset": Vector2(0.50, 0.20),
		"zoom": 1.25,
		"accent": Color(0.5, 0.6, 0.8, 1.0)
	})
	accent_color = cfg.get("accent", Color(0.5, 0.6, 0.8, 1.0))
	
	# Shift face center towards wide portion of trapezoid
	var face_shift := Vector2(0.0, -25.0) if is_top_heavy else Vector2(0.0, 25.0)
	
	# Calculate exact 1:1 isometric UV scale based on texture dimensions to prevent any warping/stretching
	var tex_w: float = float(portrait_tex.get_width()) if portrait_tex else 1000.0
	var tex_h: float = float(portrait_tex.get_height()) if portrait_tex else 1000.0
	var zoom: float = cfg.get("zoom", 1.25)
	var uv_scale := Vector2(zoom / tex_w, zoom / tex_h)
	
	if wedge_rect and _wedge_mat:
		_wedge_mat.set_shader_parameter("is_top_heavy", is_top_heavy)
		if portrait_tex:
			_wedge_mat.set_shader_parameter("portrait_tex", portrait_tex)
		_wedge_mat.set_shader_parameter("uv_offset", cfg.get("offset", Vector2(0.5, 0.20)))
		_wedge_mat.set_shader_parameter("uv_scale", uv_scale)
		_wedge_mat.set_shader_parameter("face_shift", face_shift)
		_wedge_mat.set_shader_parameter("accent_color", accent_color)
	
	_update_visual_state()

func set_selection(p1_active: bool, p2_active: bool, cpu: bool = false) -> void:
	is_p1 = p1_active
	is_p2 = p2_active
	is_cpu_p2 = cpu
	_update_visual_state()

func _rebuild_geometry() -> void:
	polygon_points.clear()
	
	if is_top_heavy:
		# Wide on top (Y = 0), narrow on bottom (Y = H)
		polygon_points.append(Vector2(0.0, 0.0))
		polygon_points.append(Vector2(W_BASE, 0.0))
		polygon_points.append(Vector2(SHIFT + W_TIP, H))
		polygon_points.append(Vector2(SHIFT, H))
		
		if p1_badge:
			p1_badge.position = Vector2(16.0, 6.0)
		if p2_badge:
			p2_badge.position = Vector2(SHIFT, H - 32.0)
	else:
		# Narrow on top (Y = 0), wide on bottom (Y = H)
		polygon_points.append(Vector2(SHIFT, 0.0))
		polygon_points.append(Vector2(SHIFT + W_TIP, 0.0))
		polygon_points.append(Vector2(W_BASE, H))
		polygon_points.append(Vector2(0.0, H))
		
		if p1_badge:
			p1_badge.position = Vector2(SHIFT - 20.0, 6.0)
		if p2_badge:
			p2_badge.position = Vector2(W_BASE - 84.0, H - 32.0)

func _update_visual_state() -> void:
	if _wedge_mat:
		_wedge_mat.set_shader_parameter("is_selected_p1", is_p1)
		_wedge_mat.set_shader_parameter("is_selected_p2", is_p2)
	
	if p1_badge:
		p1_badge.visible = is_p1
	
	if p2_badge:
		p2_badge.visible = is_p2
		if is_p2:
			p2_badge.text = "[ CPU ]" if is_cpu_p2 else "[ 2P ]"
	
	scale = Vector2(1.05, 1.05) if (is_p1 or is_p2) else Vector2(1.0, 1.0)

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.is_pressed():
		if Geometry2D.is_point_in_polygon(event.position, polygon_points):
			slot_clicked.emit(character_index)
			accept_event()
