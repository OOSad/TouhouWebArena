shader_type spatial;
render_mode cull_disabled, depth_draw_opaque, unshaded;

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap;
uniform vec4 fog_color : source_color = vec4(0.1, 0.22, 0.14, 1.0);
uniform float fog_start : hint_range(0.0, 50.0) = 4.0;
uniform float fog_end : hint_range(0.0, 50.0) = 14.5;
uniform float alpha_scissor_threshold : hint_range(0.0, 1.0) = 0.30;
uniform bool use_alpha_scissor = false;
// Leaves only: drop near-black pixels too (0 keeps everything).
uniform float dark_cutoff : hint_range(0.0, 1.0) = 0.0;

void fragment() {
	vec4 tex_color = texture(texture_albedo, UV);
	
	if (use_alpha_scissor && (tex_color.a < alpha_scissor_threshold || max(tex_color.r, max(tex_color.g, tex_color.b)) < dark_cutoff)) {
		discard;
	}
	
	// In Godot spatial fragment shaders, VERTEX is in view (camera) space.
	// Looking down the camera is negative Z, so -VERTEX.z is positive view distance.
	float dist = -VERTEX.z;
	float fog_factor = clamp((dist - fog_start) / (fog_end - fog_start), 0.0, 1.0);
	fog_factor = smoothstep(0.0, 1.0, fog_factor);
	
	ALBEDO = mix(tex_color.rgb, fog_color.rgb, fog_factor);
}
