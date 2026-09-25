shader_type canvas_item;
render_mode blend_mul;

// Multiplies what is under it by the texture, at `strength` (0 leaves it untouched, 1 is a
// full multiply). Godot's multiply blend ignores alpha, so this stands in for LoLK drawing a
// multiplied layer at half alpha (Clownpiece's spell background, "twin_rotate").
uniform float strength : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	COLOR = vec4(mix(vec3(1.0), tex.rgb, strength * tex.a), 1.0);
}
