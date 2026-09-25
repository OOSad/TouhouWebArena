shader_type spatial;
render_mode cull_disabled, depth_draw_never, blend_mix, unshaded;

// The Garden of the Sun's sunflower clusters (world09b.png), all of them in one MultiMesh so
// the whole field is a single draw call. Each one always faces the camera (world09.anm
// script 2, __rotate_auto). Instances are laid out far to near, which is the order they
// draw in, so their soft edges blend over each other correctly without sorting.

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_disable;
// Same slide as the ground, so the flowers stay planted in it
uniform float slide = 0.0;
uniform vec4 fog_color : source_color = vec4(0.816, 0.816, 0.502, 1.0);
uniform float fog_start = 9.375;
uniform float fog_end = 12.5;

void vertex() {
	vec3 centre = MODEL_MATRIX[3].xyz + vec3(0.0, 0.0, slide);
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(INV_VIEW_MATRIX[0], INV_VIEW_MATRIX[1], INV_VIEW_MATRIX[2], vec4(centre, 1.0));
}

void fragment() {
	// The sprite is the top 200 rows of the 256-row sheet
	vec4 tex = texture(texture_albedo, vec2(UV.x, UV.y * 200.0 / 256.0));
	float fog = clamp((-VERTEX.z - fog_start) / (fog_end - fog_start), 0.0, 1.0);
	ALBEDO = mix(tex.rgb, fog_color.rgb, fog);
	ALPHA = tex.a;
}
