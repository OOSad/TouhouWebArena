class_name MenuRowStyle
extends RefCounted

## The small buttons and labels the main menu builds into its list rows (lobby, replays).

const FONT: Font = preload("res://Cirno.ttf")

static func label(font_size: int) -> Label:
	var l := Label.new()
	l.add_theme_font_override("font", FONT)
	l.add_theme_font_size_override("font_size", font_size)
	return l

## A compact bordered button. with_hover lightens it on hover and focus.
static func button(text: String, min_size: Vector2, font_size: int, bg: Color, border: Color,
		outline: int = 0, with_hover: bool = true) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = min_size
	btn.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	btn.add_theme_font_override("font", FONT)
	btn.add_theme_font_size_override("font_size", font_size)
	if outline > 0:
		btn.add_theme_color_override("font_outline_color", Color(0, 0, 0, 1.0))
		btn.add_theme_constant_override("outline_size", outline)

	var style := StyleBoxFlat.new()
	style.bg_color = bg
	style.border_color = border
	style.set_border_width_all(1)
	style.set_corner_radius_all(4)
	style.content_margin_left = 6.0
	style.content_margin_right = 6.0
	style.content_margin_top = 2.0
	style.content_margin_bottom = 2.0
	btn.add_theme_stylebox_override("normal", style)
	if with_hover:
		var hover := style.duplicate() as StyleBoxFlat
		hover.bg_color = bg.lightened(0.2)
		btn.add_theme_stylebox_override("hover", hover)
		btn.add_theme_stylebox_override("focus", hover)
	return btn
