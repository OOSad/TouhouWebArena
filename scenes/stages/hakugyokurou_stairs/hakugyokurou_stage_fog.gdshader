shader_type spatial;
render_mode cull_disabled, depth_draw_opaque, unshaded;

uniform sampler2D texture_albedo : source_color, filter_linear_mipmap;
uniform vec2 uv_scale = vec2(1.0, 1.0);
uniform vec2 uv_offset = vec2(0.0, 0.0);
uniform float uv_rotation = 0.0; // degrees
uniform vec4 fog_color : source_color = vec4(0.66, 0.53, 0.57, 1.0);
uniform float fog_start : hint_range(0.0, 60.0) = 4.0;
uniform float fog_end : hint_range(0.0, 60.0) = 22.0;
uniform vec4 light_modulate : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform bool use_alpha = false;
// Multimesh instances can pick a band of a stacked texture: INSTANCE_CUSTOM.r is added to the
// V coordinate (the stair risers, each showing the next band of the relief).
uniform bool use_instance_band = false;
// Fixed brightness on top of light_modulate (PoFV draws the stair treads at color 0x40).
uniform vec3 tint = vec3(1.0, 1.0, 1.0);

varying float instance_band;

void vertex() {
	instance_band = use_instance_band ? INSTANCE_CUSTOM.r : 0.0;
}

void fragment() {
	vec2 uv = UV;
	if (uv_rotation != 0.0) {
		float rad = radians(uv_rotation);
		float c = cos(rad);
		float s = sin(rad);
		mat2 rot_mat = mat2(vec2(c, -s), vec2(s, c));
		uv = rot_mat * (uv - 0.5) + 0.5;
	}
	uv = uv * uv_scale + uv_offset + vec2(0.0, instance_band);
	vec4 tex_color = texture(texture_albedo, uv);
	
	if (use_alpha) {
		ALPHA = tex_color.a;
		if (ALPHA < 0.01) {
			discard;
		}
	}
	
	// Apply dynamic lighting/mood modulate (dark intro vs bright dawn)
	vec3 base_rgb = tex_color.rgb * light_modulate.rgb * tint;
	
	// In Godot spatial fragment shaders, VERTEX is in view (camera) space.
	// Looking down camera is negative Z, so -VERTEX.z is positive view distance.
	float dist = -VERTEX.z;
	float fog_factor = clamp((dist - fog_start) / (fog_end - fog_start), 0.0, 1.0);
	fog_factor = smoothstep(0.0, 1.0, fog_factor);
	
	ALBEDO = mix(base_rgb, fog_color.rgb, fog_factor);
}

