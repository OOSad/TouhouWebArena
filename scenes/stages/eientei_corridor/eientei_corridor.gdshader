shader_type spatial;
render_mode cull_disabled, depth_draw_opaque, unshaded;

// Flat, scrolling surface for Reisen's Eientei corridor. The geometry never
// moves: forward travel is faked by sliding the UVs along the corridor axis,
// so there is no tile recycling and no seam pop however long a round runs.

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform vec2 uv_scale = vec2(1.0, 1.0);
// Tiles travelled so far, times the axis this surface scrolls along.
uniform vec2 uv_offset = vec2(0.0, 0.0);
uniform vec4 tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 fog_color : source_color = vec4(0.04, 0.03, 0.05, 1.0);
uniform float fog_start : hint_range(0.0, 80.0) = 10.0;
uniform float fog_end : hint_range(0.0, 80.0) = 34.0;
// Darkens the surface as it approaches the camera, so the corridor mouth in
// the bottom of frame falls away into shadow instead of ending on a hard edge.
uniform float near_fade_start : hint_range(0.0, 40.0) = 6.0;
uniform float near_fade_amount : hint_range(0.0, 1.0) = 0.0;
// The floor art does not tile on its own, so it is mirrored end to end instead.
uniform bool mirror_u = false;
uniform bool mirror_v = false;

void fragment() {
	vec2 uv = UV * uv_scale + uv_offset;
	if (mirror_u) {
		uv.x = abs(fract(uv.x * 0.5) * 2.0 - 1.0);
	}
	if (mirror_v) {
		uv.y = abs(fract(uv.y * 0.5) * 2.0 - 1.0);
	}
	vec3 tex_color = texture(texture_albedo, uv).rgb * tint.rgb;

	// VERTEX is view space in a spatial fragment shader; the camera looks down -Z.
	float dist = -VERTEX.z;

	float fog_factor = smoothstep(0.0, 1.0, clamp((dist - fog_start) / (fog_end - fog_start), 0.0, 1.0));
	vec3 col = mix(tex_color, fog_color.rgb, fog_factor);

	float near_factor = 1.0 - clamp(dist / max(near_fade_start, 0.001), 0.0, 1.0);
	col = mix(col, fog_color.rgb, near_factor * near_fade_amount);

	ALBEDO = col;
}
