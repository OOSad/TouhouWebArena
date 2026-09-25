shader_type spatial;
render_mode cull_disabled, depth_draw_opaque, unshaded;

// The Garden of the Sun's earth (world09.png). One plane that never moves; forward travel
// slides the texture towards the camera instead, so nothing ever recycles or pops.

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
// World units per texture repeat (one 256-unit PoFV tile)
uniform float tile_length = 4.0;
// How far the field has slid towards the camera within the current tile, in world units
uniform float slide = 0.0;
uniform vec4 fog_color : source_color = vec4(0.816, 0.816, 0.502, 1.0);
uniform float fog_start = 9.375;
uniform float fog_end = 12.5;

varying vec3 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 uv = fract(vec2(world_pos.x, world_pos.z - slide) / tile_length);
	// The sprite is the sheet inset by one pixel on every side (world09.anm sprite0)
	uv = (uv * 254.0 + 1.0) / 256.0;
	vec3 col = texture(texture_albedo, uv).rgb;
	// Direct3D vertex fog: linear in view depth
	float fog = clamp((-VERTEX.z - fog_start) / (fog_end - fog_start), 0.0, 1.0);
	ALBEDO = mix(col, fog_color.rgb, fog);
}
