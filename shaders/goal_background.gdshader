shader_type canvas_item;

uniform vec4 background_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
	// Called for every pixel the material is visible on.
	vec4 text_color = texture(TEXTURE, UV);

	if (text_color.a < 0.1) {
		COLOR = background_color;
	}
}
