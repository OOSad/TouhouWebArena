shader_type spatial;
render_mode cull_disabled, depth_draw_never, blend_mix, unshaded;

// A cloud over Aya's mountain pond (world10b.png, sprite2). Over the water they always face
// the camera (world10.anm scripts 4-7, __rotate_auto); their reflections under the water lie
// flat (scripts 8-11).

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_disable;
uniform bool billboard = true;
// The odd scripts draw the cloud mirrored (scale -1, 1)
uniform bool mirror = false;
uniform vec4 fog_color : source_color = vec4(0.627, 0.690, 0.627, 1.0);
uniform float fog_start = 12.5;
uniform float fog_end = 17.1875;

void vertex() {
	if (billboard) {
		MODELVIEW_MATRIX = VIEW_MATRIX * mat4(INV_VIEW_MATRIX[0], INV_VIEW_MATRIX[1], INV_VIEW_MATRIX[2], MODEL_MATRIX[3]);
	}
}

void fragment() {
	vec2 uv = UV;
	if (mirror) {
		uv.x = 1.0 - uv.x;
	}
	vec4 tex = texture(texture_albedo, uv);
	float fog = clamp((-VERTEX.z - fog_start) / (fog_end - fog_start), 0.0, 1.0);
	ALBEDO = mix(tex.rgb, fog_color.rgb, fog);
	ALPHA = tex.a;
}
