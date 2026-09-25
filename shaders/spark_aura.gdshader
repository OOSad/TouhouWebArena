shader_type canvas_item;

// Impact burst for Reisen's Level 1 Charge Attack "Mind Explosion", matched against
// reference footage frame by frame.
//
// Lifecycle, from the original (~1.1s, 60fps):
//   * A solid red disc appears and grows steadily the whole time. It is flat and opaque,
//     DARK in the middle and bright red at the rim - not a glow with a hot centre.
//   * FOUR thin white loops hug the disc, each jittering slightly differently. Early on
//     their amplitude is tiny so they overlap into what looks like a single clean circle;
//     as the burst ages they jitter harder and separate into four violent jagged
//     scribbles reaching well outside the disc.
//   * Late on, the red goes translucent and mottled while the scribble persists, then
//     the whole thing fades.
//
// It is all computed per fragment on a single quad, so the cost is one draw call however
// hard it flickers. Building the outline from real geometry via _draw() is what wrecked
// travel_mote.gd (60-150 unbatched calls), so it stays on the GPU.

uniform vec4 core_color : source_color = vec4(0.30, 0.02, 0.10, 1.0);
uniform vec4 edge_color : source_color = vec4(1.0, 0.05, 0.05, 1.0);
uniform vec4 spark_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

// All radii are in UV units, where 1.0 is the quad's half width. Driven from the script.
uniform float disc_radius : hint_range(0.0, 1.0) = 0.42;
uniform float jitter_amount : hint_range(0.0, 0.5) = 0.01;
uniform float disc_alpha : hint_range(0.0, 1.0) = 1.0;
uniform float spark_alpha : hint_range(0.0, 1.0) = 1.0;
// Mottles the disc as it breaks up near the end.
uniform float mottle : hint_range(0.0, 1.0) = 0.0;

uniform float ring_thickness : hint_range(0.001, 0.05) = 0.010;
uniform float control_points = 22.0;
uniform float jitter_hz = 30.0;
uniform float seed = 0.0;

float hash(vec2 v) {
	return fract(sin(dot(v, vec2(127.1, 311.7))) * 43758.5453123);
}

// One jittering closed loop. Control points are interpolated LINEARLY, so the loop has
// hard corners - which is what makes it read as a jagged scribble once the amplitude is
// large, while staying invisibly smooth when the amplitude is tiny.
float loop_ring(float ang01, float d, float base_r, float amp, float t_step, float sd) {
	float idx = ang01 * control_points;
	float i0 = floor(idx);
	float i1 = mod(i0 + 1.0, control_points);
	float f = fract(idx);
	float j = mix(hash(vec2(i0, t_step + sd)), hash(vec2(i1, t_step + sd)), f);
	float r = base_r + (j - 0.5) * 2.0 * amp;
	return 1.0 - smoothstep(0.0, ring_thickness, abs(d - r));
}

void fragment() {
	vec2 p = (UV - vec2(0.5)) * 2.0;
	float d = length(p);

	if (d > 1.0) {
		COLOR = vec4(0.0);
	} else {
		float ang01 = (atan(p.y, p.x) + 3.14159265) / 6.28318531;
		float t_step = floor(TIME * jitter_hz);

		// --- the disc: flat red, dark in the middle, bright at the rim ---
		float inside = 1.0 - smoothstep(disc_radius - 0.010, disc_radius + 0.010, d);
		float radial = clamp(d / max(disc_radius, 0.001), 0.0, 1.0);
		vec3 disc_rgb = mix(core_color.rgb, edge_color.rgb, pow(radial, 0.7));

		// Break the disc up as it dies, so it goes patchy rather than just dimming.
		float grain = hash(vec2(floor(ang01 * 34.0), floor(radial * 9.0) + t_step * 0.5));
		float a_disc = inside * disc_alpha * mix(1.0, 0.35 + 0.65 * grain, mottle);

		// --- the white scribble: FOUR loops, each jittering slightly differently ---
		// They are all live from the start. Early on the amplitude is tiny, so they sit
		// almost on top of one another and read as a single clean outline; as it ramps up
		// they separate into four distinct scribbles. Each gets its own seed and its own
		// step rate so they never flicker in lockstep.
		float sparks = 0.0;
		for (int i = 0; i < 4; i++) {
			float fi = float(i);
			float amp = jitter_amount * (0.76 + 0.21 * fi);
			float hz_step = floor(TIME * jitter_hz * (1.0 + 0.13 * fi));
			sparks = max(sparks, loop_ring(ang01, d, disc_radius, amp, hz_step, seed + fi * 53.0));
		}
		sparks *= spark_alpha;

		vec3 rgb = mix(disc_rgb, spark_color.rgb, clamp(sparks, 0.0, 1.0));
		float alpha = clamp(max(a_disc, sparks), 0.0, 1.0);
		COLOR = vec4(rgb, alpha);
	}
}
