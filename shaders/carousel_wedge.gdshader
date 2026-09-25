shader_type canvas_item;

uniform sampler2D portrait_tex : filter_linear;
uniform vec4 bg_color : source_color = vec4(0.03, 0.04, 0.08, 0.92);
uniform vec4 accent_color : source_color = vec4(1.0, 0.35, 0.45, 1.0);
uniform vec4 normal_border_color : source_color = vec4(0.35, 0.45, 0.65, 0.55);
uniform vec4 p1_border_color : source_color = vec4(1.0, 0.28, 0.40, 1.0);
uniform vec4 p2_border_color : source_color = vec4(0.25, 0.85, 1.0, 1.0);

uniform bool is_top_heavy = false;
uniform float track_rotation_rad = -0.512389; // Angle of diagonal track (~ -29.36 deg)
uniform vec2 uv_offset = vec2(0.50, 0.22);
uniform vec2 uv_scale = vec2(0.00165, 0.00165);
uniform vec2 face_shift = vec2(0.0, 0.0);
uniform float silhouette_alpha : hint_range(0.0, 1.0) = 0.95;

uniform bool is_selected_p1 = false;
uniform bool is_selected_p2 = false;
uniform float border_width = 3.0;

const float W_BASE = 240.0;
const float W_TIP = 70.0;
const float H = 220.0;
const float SHIFT = 85.0; // (240 - 70) / 2
const float SEAM_LEN = 235.85; // sqrt(85^2 + 220^2)
const float COS_SLANT = 0.9328; // 220 / 235.85

void fragment() {
	vec2 pixel_pos = UV * vec2(W_BASE, H);
	float x = pixel_pos.x;
	float y = pixel_pos.y;

	// Calculate trapezoid left and right boundaries at height y
	float x_left;
	float x_right;
	if (is_top_heavy) {
		float ratio = clamp(y / H, 0.0, 1.0);
		x_left = SHIFT * ratio;
		x_right = W_BASE - SHIFT * ratio;
	} else {
		float ratio = clamp(1.0 - (y / H), 0.0, 1.0);
		x_left = SHIFT * ratio;
		x_right = W_BASE - SHIFT * ratio;
	}

	// Discard pixels outside the trapezoid
	if (x < x_left || x > x_right || y < 0.0 || y > H) {
		discard;
	}

	// Calculate Euclidean distance to all 4 edges for anti-aliasing and borders
	float d_left = (x - x_left) * COS_SLANT;
	float d_right = (x_right - x) * COS_SLANT;
	float d_top = y;
	float d_bottom = H - y;
	float min_dist = min(min(d_left, d_right), min(d_top, d_bottom));

	if (min_dist < 0.0) {
		discard;
	}

	float aa = smoothstep(0.0, 1.5, min_dist);

	// Transform local pixel coordinates to world/screen space coordinates (unrotated: "absolute north")
	vec2 p_local = pixel_pos - vec2(W_BASE * 0.5, H * 0.5);
	float cos_t = cos(track_rotation_rad);
	float sin_t = sin(track_rotation_rad);
	vec2 p_screen = vec2(
		p_local.x * cos_t - p_local.y * sin_t,
		p_local.x * sin_t + p_local.y * cos_t
	);

	// Sample upright portrait texture
	vec2 tex_uv = uv_offset + (p_screen - face_shift) * uv_scale;
	vec4 sil = vec4(0.0);
	if (tex_uv.x >= 0.0 && tex_uv.x <= 1.0 && tex_uv.y >= 0.0 && tex_uv.y <= 1.0) {
		sil = texture(portrait_tex, tex_uv);
	}

	// Base glass background with accent wash
	bool is_selected = is_selected_p1 || is_selected_p2;
	float tint_strength = is_selected ? 0.32 : 0.15;
	vec4 base_color = mix(bg_color, bg_color + accent_color * 0.45, tint_strength);

	// Composite upright silhouette over background
	float eff_alpha = is_selected ? silhouette_alpha : (silhouette_alpha * 0.70);
	vec4 final_color = mix(base_color, vec4(sil.rgb, 1.0), sil.a * eff_alpha);

	if (is_selected) {
		final_color.rgb += accent_color.rgb * 0.16;
	}

	// Border coloring
	if (min_dist < border_width) {
		vec4 active_border = normal_border_color;
		if (is_selected_p1 && is_selected_p2) {
			active_border = mix(p1_border_color, p2_border_color, clamp(UV.x, 0.0, 1.0));
			active_border.rgb += vec3(0.20);
		} else if (is_selected_p1) {
			active_border = p1_border_color;
			active_border.rgb += vec3(0.20);
		} else if (is_selected_p2) {
			active_border = p2_border_color;
			active_border.rgb += vec3(0.20);
		}

		float b_factor = 1.0 - smoothstep(0.0, border_width, min_dist);
		final_color = mix(final_color, active_border, b_factor * 0.95);
	}

	final_color.a *= aa;
	COLOR = final_color;
}
