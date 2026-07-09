shader_type canvas_item;
render_mode unshaded, blend_add;

uniform mat4 por;
uniform mat4 por_inverse;

uniform vec4 event;
uniform float frequency;

#include "res://shader/color.gdshaderinc"
// uniform vec4 velocity;
// uniform float radius;
// uniform float brightness;

float get_doppler(vec4 e, vec4 v) {
	//if(e.xy == vec2(0,0))
	//	return 1.0;
	return v.z*(1.0-(v.x*e.x+v.y*e.y)/sqrt(e.x*e.x+e.y*e.y));
}

vec4 draw(vec2 screen_pos) {
	vec2 rel = screen_pos - vec2(0.5);

	// === foo: Event on your past light-cone from your reference frame ===
	float t = -length(rel);
	vec4 foo = vec4(rel, t, 1.0); // (x, y, t, 1)

	// === bar: Transform foo to rest frame ===
	vec4 bar = por * foo;

	// Instead of bothering with a point of reference for the explosion, I'm just going with the event and velocity.

	vec4 offset = bar - event;
	//If it's further than the radius in the future, ignore. I should modify this so it's from the explosion's frame of reference, but it doesn't matter for now.
	// if (offset.z > radius)
	//	discard;
	//if (dot(offset.xy, offset.xy) > offset.z * offset.z)
	//	return vec4(0.5, 0.5, 0.5, 1.0);
		//discard;
	float proper_time_squared = offset.z * offset.z - dot(offset.xy, offset.xy);
	vec4 v = vec4((offset / sqrt(proper_time_squared)).xyz, 0.0);

	vec4 quux = por_inverse * v;

	// === doppler: Red/blue shift ===
	float doppler = get_doppler(foo, quux);

	float amplitude = 1.0;//brightness * doppler * doppler * doppler * doppler / proper_time_squared;

	// === color based on doppler shift ===
	vec4 color = vec4(get_color(doppler * 4.0) * amplitude, 1.0);

	// It's redshifted on the outside and blueshifted on the inside. Why? It should be redshifted everywhere, and just less so on the outside.

	return color;
}

void fragment() {
	COLOR = draw(SCREEN_UV);
	//COLOR = vec4(1.0, 1.0, 1.0, 1.0);
}