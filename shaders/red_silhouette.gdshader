shader_type canvas_item;

uniform vec4 silhouette_color : source_color = vec4(1.0, 0.15, 0.15, 1.0);
uniform float silhouette_strength : hint_range(0.0, 1.0) = 1.0;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	COLOR = vec4(mix(tex.rgb, silhouette_color.rgb, silhouette_strength), tex.a * COLOR.a);
}

