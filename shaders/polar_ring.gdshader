shader_type canvas_item;
render_mode blend_add;

uniform sampler2D ribbon_texture : repeat_enable, filter_linear;
uniform float inner_radius_px = 80.0;
uniform float outer_radius_px = 112.0;
uniform float rotation_angle = 0.0;
uniform float repeats = 7.0;
uniform vec4 color_tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
	vec2 center_px = (UV - vec2(0.5)) * (2.0 * outer_radius_px);
	float dist = length(center_px);
	
	if (dist < inner_radius_px - 2.5 || dist > outer_radius_px + 2.5) {
		discard;
	}
	
	float edge_feather = clamp(min(dist - inner_radius_px, outer_radius_px - dist) / 2.5, 0.0, 1.0);
	float u = clamp((dist - inner_radius_px) / max(outer_radius_px - inner_radius_px, 0.001), 0.0, 1.0);
	float angle = atan(center_px.y, center_px.x) + rotation_angle;
	float v = fract((angle / 6.28318530718) * repeats);
	
	vec4 tex = texture(ribbon_texture, vec2(u, v));
	COLOR = tex * color_tint * edge_feather;
}

