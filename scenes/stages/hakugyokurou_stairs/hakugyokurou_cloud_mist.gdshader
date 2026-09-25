shader_type spatial;
render_mode blend_add, depth_draw_never, depth_test_disabled, cull_disabled, unshaded;

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap;
uniform vec4 light_modulate : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float mist_opacity : hint_range(0.0, 3.0) = 0.6;
uniform float near_fade_start : hint_range(0.0, 20.0) = 3.6;
uniform float near_fade_end : hint_range(0.0, 20.0) = 4.8;
uniform float far_fade_start : hint_range(0.0, 50.0) = 14.0;
uniform float far_fade_end : hint_range(0.0, 50.0) = 20.0;

void fragment() {
	vec4 tex = texture(texture_albedo, UV);
	
	// Distance fade:
	// dist is camera-space view depth (-VERTEX.z)
	float dist = -VERTEX.z;
	
	// Near fade: smoothly fade out as clouds approach camera / bottom screen edge
	float near_fade = smoothstep(near_fade_start, near_fade_end, dist);
	
	// Far fade: smoothly fade into distant fog
	float far_fade = 1.0 - smoothstep(far_fade_start, far_fade_end, dist);
	
	float fade = near_fade * far_fade;
	
	// Safety edge falloff around quad boundaries
	float border_fade = smoothstep(0.0, 0.05, UV.x) * smoothstep(1.0, 0.95, UV.x) *
	                    smoothstep(0.0, 0.05, UV.y) * smoothstep(1.0, 0.95, UV.y);
	
	// Luminous additive mist:
	// Background pixels have tex.a == 0 and tex.rgb == 0, adding nothing to the framebuffer.
	// Cloud body adds a soft mauve ethereal glow across the steps.
	ALBEDO = tex.rgb * (tex.a * mist_opacity * fade * border_fade) * light_modulate.rgb;
}

