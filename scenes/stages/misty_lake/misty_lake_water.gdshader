shader_type spatial;
render_mode cull_disabled, depth_draw_opaque, unshaded;

// Authentic Touhou 09 Misty Lake (霧の湖) Multi-Layer Water & Fog Shader
// Recreates the triple-scrolling surface from world05.anm:
//  - Base Floor: Script 2, vertical flip (scale_y = -1.0), tinted pale ice-blue, gentle drift (vx = +0.00002, vy = +0.0003)
//  - Mid Layer:  Script 0, alpha = 0.22, horizontal drift left (vx = -0.00026, vy = -0.00005)
//  - Top Layer:  Script 1, horizontal flip (scale_x = -1.0), alpha = 0.38, horizontal drift right (vx = +0.0004, vy = +0.00005)
//  - Upper Mist: Dreamy lavender-purple horizon depth fog (#8479bb) smoothly dissolving into the horizon.

uniform sampler2D texture_water : source_color, filter_linear_mipmap, repeat_enable;
uniform vec2 uv_scale = vec2(6.0, 10.0);

// Base layer parameters
uniform vec4 base_tint : source_color = vec4(0.878, 0.878, 1.0, 1.0); // Touhou 09 opcode 9 (224, 224, 255)
uniform vec2 base_scroll = vec2(0.0012, 0.018); // 60fps equivalent of (0.00002, 0.0003)

// Mid layer parameters (flows left)
uniform vec2 mid_scroll = vec2(-0.0156, -0.003); // 60fps equivalent of (-0.00026, -0.00005)
uniform float mid_alpha : hint_range(0.0, 1.0) = 0.22; // Touhou 09 opcode 8 (56/255)

// Top layer parameters (flows right, flipped horizontally)
uniform vec2 top_scroll = vec2(0.024, 0.003); // 60fps equivalent of (0.0004, 0.00005)
uniform float top_alpha : hint_range(0.0, 1.0) = 0.38; // Touhou 09 opcode 8 (96/255)

// Atmospheric Horizon Mist / Depth Fog
uniform vec4 fog_color : source_color = vec4(0.518, 0.475, 0.733, 1.0); // #8479bb Touhou 09 mist
uniform float fog_start : hint_range(0.0, 50.0) = 5.0;
uniform float fog_end : hint_range(0.0, 50.0) = 20.0;

// Forward flight speed across the lake
uniform float forward_speed : hint_range(0.0, 2.0) = 0.105;


// External time parameter for synchronization and P2 desync offset
uniform float time_val = 0.0;

void fragment() {
	float t = time_val > 0.0 ? time_val : TIME;

	// Global forward motion across the water surface
	vec2 scrolled_uv = UV + vec2(0.0, -forward_speed * t);

	// Layer 0: Base water floor (vertically inverted per world05.anm Script 2)
	vec2 uv_base = scrolled_uv * uv_scale * vec2(1.0, -1.0) + base_scroll * t;
	vec4 col_base = texture(texture_water, uv_base) * base_tint;

	// Layer 1: Mid water layer (left drift per world05.anm Script 0)
	vec2 uv_mid = scrolled_uv * uv_scale + mid_scroll * t;
	vec4 col_mid = texture(texture_water, uv_mid);

	// Layer 2: Top water layer (horizontally mirrored & right drift per world05.anm Script 1)
	vec2 uv_top = scrolled_uv * uv_scale * vec2(-1.0, 1.0) + top_scroll * t;
	vec4 col_top = texture(texture_water, uv_top);


	// Multi-layer interference & caustics (combines highlights to create organic shimmering waves)
	vec3 water_rgb = col_base.rgb + col_mid.rgb * (mid_alpha * col_mid.a) + col_top.rgb * (top_alpha * col_top.a);

	// Atmospheric horizon depth fog: in camera view space, -VERTEX.z is forward distance
	float dist = -VERTEX.z;
	float fog_factor = clamp((dist - fog_start) / (fog_end - fog_start), 0.0, 1.0);
	fog_factor = smoothstep(0.0, 1.0, fog_factor);

	ALBEDO = mix(water_rgb, fog_color.rgb, fog_factor);
}

