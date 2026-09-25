shader_type canvas_item;

// Dual counter-scrolling shader for Level 4 Spellcard background overlays (Cirno's "Cold Divinity")
// In a 600x960 playfield, uv_scale = vec2(1.125, 1.8) preserves exact isotropic aspect ratio.
// Layer 1 scrolls at +scroll_speed, Layer 2 scrolls at -scroll_speed, blended at counter_layer_alpha (0.5).
uniform vec2 scroll_speed = vec2(0.0, 0.04);
uniform vec2 uv_scale = vec2(1.125, 1.8);
uniform float counter_layer_alpha : hint_range(0.0, 1.0) = 0.5;
uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
	vec2 uv1 = fract(UV * uv_scale + scroll_speed * TIME);
	vec2 uv2 = fract(UV * uv_scale - scroll_speed * TIME);
	vec4 col1 = texture(TEXTURE, uv1);
	vec4 col2 = texture(TEXTURE, uv2);
	vec4 blended = mix(col1, col2, counter_layer_alpha);
	COLOR = blended * tint_color * COLOR;
}

