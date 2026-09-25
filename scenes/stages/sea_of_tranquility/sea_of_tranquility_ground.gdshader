shader_type spatial;
render_mode cull_disabled, depth_draw_opaque, unshaded;

// The Sea of Tranquility's cratered ground (LoLK stage05a.png). One plane that never moves;
// forward travel slides the texture towards the camera instead, as Garden of the Sun does.

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
// World units per texture repeat (512 LoLK units)
uniform float tile_length = 8.0;
// How far the ground has slid towards the camera within the current tile, in world units
uniform float slide = 0.0;
uniform vec4 fog_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float fog_start = 12.5;
uniform float fog_end = 20.3;

varying vec3 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	// Left unwrapped: the sampler repeats it. fract() here would jump the UV by a whole tile at
	// each repeat, and the GPU would read that jump as extreme minification and draw a line
	// of the smallest mipmap along every tile edge.
	vec2 uv = vec2(world_pos.x, world_pos.z - slide) / tile_length;
	vec3 col = texture(texture_albedo, uv).rgb;
	// Direct3D vertex fog: linear in view depth
	float fog = clamp((-VERTEX.z - fog_start) / (fog_end - fog_start), 0.0, 1.0);
	ALBEDO = mix(col, fog_color.rgb, fog);
}
