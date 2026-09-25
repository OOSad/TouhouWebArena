shader_type spatial;
render_mode cull_disabled, depth_draw_never, blend_mix, unshaded;

// One flat layer of Aya's mountain pond (world10.std object 0): the lily pond itself, or one
// of the three sheets of water light under it. The texture is laid in world space, one repeat
// per PoFV quad, so the plane can be as large as the view needs without seams.

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
// World units per texture repeat (one 800-unit PoFV quad)
uniform float tile_length = 12.5;
// World (x, along) of a quad's corner, and a mirror per axis (+1 / -1), from the anm's scale
uniform vec2 tile_origin = vec2(0.0, 0.0);
uniform vec2 flip = vec2(1.0, 1.0);
// Texture scroll so far, in repeats (the anm's scroll_speed x frames, plus scroll_set)
uniform vec2 scroll = vec2(0.0, 0.0);
uniform vec4 tint : source_color = vec4(1.0);
uniform vec4 fog_color : source_color = vec4(0.627, 0.690, 0.627, 1.0);
uniform float fog_start = 12.5;
uniform float fog_end = 17.1875;

varying vec3 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 along = vec2(world_pos.x, -world_pos.z);
	// No fract(): the sampler repeats, and wrapping by hand breaks the mip level at the seam
	vec2 uv = (along - tile_origin) / tile_length * flip + scroll;
	vec4 tex = texture(texture_albedo, uv) * tint;
	float fog = clamp((-VERTEX.z - fog_start) / (fog_end - fog_start), 0.0, 1.0);
	ALBEDO = mix(tex.rgb, fog_color.rgb, fog);
	ALPHA = tex.a;
}
