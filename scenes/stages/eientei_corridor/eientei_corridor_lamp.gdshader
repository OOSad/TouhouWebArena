shader_type spatial;
render_mode blend_add, depth_draw_never, cull_disabled, unshaded, shadows_disabled;

// Camera-facing additive lamp glow for the Eientei corridor (world04.anm scripts 3/8:
// additive, auto-rotating to face the view, alpha flickering 0xc0/0xff every frame).
// Fogged with the same curve as the corridor shader so it cannot glow through the fog.

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap;
uniform float flicker = 1.0;
uniform float intensity = 1.0;
uniform float fog_start = 3.0;
uniform float fog_end = 13.0;

void vertex() {
	// Billboard: keep the node's position and scale, drop its rotation for the camera's.
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
		INV_VIEW_MATRIX[0] * length(MODEL_MATRIX[0].xyz),
		INV_VIEW_MATRIX[1] * length(MODEL_MATRIX[1].xyz),
		INV_VIEW_MATRIX[2] * length(MODEL_MATRIX[2].xyz),
		MODEL_MATRIX[3]);
}

void fragment() {
	vec4 tex = texture(texture_albedo, UV);
	float dist = -VERTEX.z;
	float fog_factor = smoothstep(0.0, 1.0, clamp((dist - fog_start) / (fog_end - fog_start), 0.0, 1.0));
	ALBEDO = tex.rgb * intensity;
	ALPHA = tex.a * flicker * (1.0 - fog_factor);
}
