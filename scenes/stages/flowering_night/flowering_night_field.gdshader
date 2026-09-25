shader_type spatial;
render_mode cull_disabled, depth_draw_opaque, unshaded;

// Sakuya Izayoi's "Flowering Night" stage: three stacked poppy-field planes
// extracted from world00.png (quadrants), alpha-composited bottom to top.
//  - Base:  opaque grass layer.
//  - Mid:   red/pink flowers, native per-pixel alpha (~67% max).
//  - Top:   sparse white/gold flower clusters, native per-pixel alpha.

uniform sampler2D texture_base : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D texture_mid : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D texture_top : source_color, filter_linear_mipmap, repeat_enable;
uniform vec2 uv_scale = vec2(6.0, 10.0);

uniform float mid_alpha_mult : hint_range(0.0, 2.0) = 1.0;
uniform float top_alpha_mult : hint_range(0.0, 2.0) = 1.0;

// Atmospheric horizon depth fog
uniform vec4 fog_color : source_color = vec4(0.55, 0.45, 0.4, 1.0);
uniform float fog_start : hint_range(0.0, 50.0) = 5.0;
uniform float fog_end : hint_range(0.0, 50.0) = 20.0;

// Steady forward camera flight across the field
uniform float forward_speed : hint_range(0.0, 2.0) = 0.12;

// External time parameter for synchronization and P2 desync offset
uniform float time_val = 0.0;

void fragment() {
	float t = time_val > 0.0 ? time_val : TIME;

	vec2 scrolled_uv = UV + vec2(0.0, -forward_speed * t);
	vec2 uv = scrolled_uv * uv_scale;

	vec4 col_base = texture(texture_base, uv);
	vec4 col_mid = texture(texture_mid, uv);
	vec4 col_top = texture(texture_top, uv);

	vec3 field_rgb = col_base.rgb;
	field_rgb = mix(field_rgb, col_mid.rgb, clamp(col_mid.a * mid_alpha_mult, 0.0, 1.0));
	field_rgb = mix(field_rgb, col_top.rgb, clamp(col_top.a * top_alpha_mult, 0.0, 1.0));

	// Atmospheric horizon depth fog: in camera view space, -VERTEX.z is forward distance
	float dist = -VERTEX.z;
	float fog_factor = clamp((dist - fog_start) / (fog_end - fog_start), 0.0, 1.0);
	fog_factor = smoothstep(0.0, 1.0, fog_factor);

	ALBEDO = mix(field_rgb, fog_color.rgb, fog_factor);
}
