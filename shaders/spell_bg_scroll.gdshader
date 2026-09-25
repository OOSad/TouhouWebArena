shader_type canvas_item;

// Tiling and scrolling shader for Level 4 Spellcard background overlays (e.g. Marisa's hex pattern)
// In a 600x960 playfield, uv_scale = vec2(1.125, 1.8) preserves exact 1:1 isotropic aspect ratio (533.33px per UV unit in both X and Y)
uniform vec2 scroll_speed = vec2(0.0, 0.66667);
uniform vec2 uv_scale = vec2(1.125, 1.8);
uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
	vec2 tiled_uv = fract(UV * uv_scale + scroll_speed * TIME);
	vec4 tex = texture(TEXTURE, tiled_uv);
	COLOR = tex * tint_color * COLOR;
}
