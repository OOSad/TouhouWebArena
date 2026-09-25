shader_type canvas_item;

// Track angle and position in screen coordinates
uniform float track_rotation_rad = -0.512389;
uniform vec2 track_center = vec2(960.0, 540.0);
uniform int player_number = 1;
uniform float cutoff_dist = 50.0;
uniform float fade_width = 16.0;

varying vec2 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
	vec4 col = texture(TEXTURE, UV);
	
	vec2 d = world_pos - track_center;
	// Normal vector pointing perpendicular to track towards Player 2 (down-right)
	vec2 n_track = vec2(-sin(track_rotation_rad), cos(track_rotation_rad));
	float perp_dist = dot(d, n_track);
	
	float mask = 1.0;
	if (player_number == 1) {
		// P1 lives in upper-left (perp_dist < 0). Cut off inside the dark track before reaching P2 side.
		mask = 1.0 - smoothstep(cutoff_dist - fade_width, cutoff_dist, perp_dist);
	} else {
		// P2 lives in lower-right (perp_dist > 0). Cut off inside the dark track before reaching P1 side.
		mask = smoothstep(-cutoff_dist, -cutoff_dist + fade_width, perp_dist);
	}
	
	col.a *= mask;
	if (col.a <= 0.0) {
		discard;
	}
	
	COLOR = col * COLOR;
}

