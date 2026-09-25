shader_type canvas_item;
render_mode blend_add;

uniform sampler2D ribbon_texture : repeat_enable, filter_linear;
uniform float base_radius = 120.0;
uniform float lobe_depth = 0.32;
uniform float band_width = 144.0;
uniform float rotation_angle = 0.0;
uniform float repeats = 10.0;
uniform vec4 color_tint : source_color = vec4(0.3, 0.85, 1.0, 1.0);
uniform float core_glow_intensity = 0.5;

void fragment() {
	float max_r = base_radius * (1.0 + lobe_depth) + band_width * 0.5 + 8.0;
	vec2 center_px = (UV - vec2(0.5)) * (2.0 * max_r);
	float dist = length(center_px);
	float angle = atan(center_px.y, center_px.x);

	// 5-lobed rose curve formula: authentic Touhou 5-point flower spellcast border
	float r_curve = base_radius * (1.0 + lobe_depth * cos(5.0 * (angle - rotation_angle)));
	float inner_r = r_curve - band_width * 0.5;
	float outer_r = r_curve + band_width * 0.5;

	vec4 col = vec4(0.0);

	// Ribbon band
	if (dist >= inner_r - 2.5 && dist <= outer_r + 2.5) {
		float edge_feather = clamp(min(dist - inner_r, outer_r - dist) / 2.5, 0.0, 1.0);
		float u = clamp((dist - inner_r) / max(band_width, 0.001), 0.0, 1.0);
		float v = fract(((angle - rotation_angle * 1.5) / 6.28318530718) * repeats);
		vec4 tex = texture(ribbon_texture, vec2(u, v));
		col += tex * color_tint * edge_feather;
	}

	// Center soft magical glow / core
	if (dist < base_radius * 0.6) {
		float core_fade = clamp(1.0 - (dist / (base_radius * 0.6)), 0.0, 1.0);
		vec4 core_col = vec4(color_tint.rgb * 1.2, color_tint.a) * (core_fade * core_fade * core_glow_intensity);
		col += core_col;
	}

	if (col.a <= 0.001) {
		discard;
	}

	COLOR = col;
}

