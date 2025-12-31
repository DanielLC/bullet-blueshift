shader_type canvas_item;
render_mode unshaded;

uniform mat4 por;
uniform mat4 por_inverse;
uniform vec2 observer_position;	// pixels
uniform float scale;			// pixels per light second
uniform float size;				// size of the sprite
uniform float spin_speed;
uniform sampler2D texture_sampler : source_color;

const int MAX_SEGMENTS = 64;
uniform int segment_count;
uniform vec4 coevents[MAX_SEGMENTS];
uniform mat4 path_transform[MAX_SEGMENTS];
uniform mat4 path_transform_inverse[MAX_SEGMENTS];
uniform float rads[MAX_SEGMENTS];
uniform float accels[MAX_SEGMENTS];

//to_local_spacetime() needs to return both the event (representing where it was seen) and a point of reference (in order to properly calculate redshift/blueshift if it's rotating).
struct EventAndPOR {
	vec4 event;
	mat3 pointOfReferenceInverse;
};

mat3 to_mat3(mat4 m) {
	return mat3(
	    m[0].xyz,
	    m[1].xyz,
	    m[2].xyz
	);
}

//relativity functions
mat4 event_to_translation(vec4 event) {
    return mat4(
        vec4(1.0, 0.0, 0.0, 0.0),
        vec4(0.0, 1.0, 0.0, 0.0),
        vec4(0.0, 0.0, 1.0, 0.0),
        event
    );
}
mat4 velocity_to_lorentz(vec4 e) {
	float k = 1.0/(e.z+1.0);
	return mat4(
		vec4(1.0+k*e.x*e.x,	k*e.x*e.y,		e.x,	0),
		vec4(k*e.x*e.y,		1.0+k*e.y*e.y,	e.y,	0),
		vec4(e.x,			e.y,			e.z,	0),
		vec4(0,				0,				0,		1)
	);
}
//== These two functions depend on the motion of the object.
//==Rest
vec4 get_velocity_at_rest(vec4 event) {
	return vec4(0,0,1,0);
}

EventAndPOR to_local_spacetime_rest(vec4 event) {
	return EventAndPOR(event, mat3(1.0));
}
//==Hyperbola
vec4 get_velocity_at_hyperbola(vec4 e) {
	if(e.y <= 0.0) {
		discard;
	}
	float speed = e.z/e.y;
	if(abs(speed) > 1.0) {
		discard;
	}
	//Convert velocity to the kind I'm using
	float gamma = 1.0/sqrt(1.0-speed*speed);
	return vec4(0, gamma*speed, gamma, 0);
}

//This one repeats a lot of get_velocity_at(). I should probably find a way to merge them.
EventAndPOR to_local_spacetime_hyperbola(vec4 e, float accel) {
	if(e.y <= 0.0) {
		discard;
	}
	float speed = e.z/e.y;
	if(abs(speed) > 1.0) {
		discard;
	}
	float gamma = 1.0/sqrt(1.0-speed*speed);
	vec4 v = vec4(0, -gamma*speed, gamma, 0);
	mat4 lorentz = velocity_to_lorentz(v);
	vec4 lateral = event_to_translation(vec4(0, -1.0/accel, 0, 1)) * (lorentz * e);
	vec4 position = vec4(lateral.xy, atanh(speed)/accel, 1);
	mat3 inv_lorentz = mat3(
		vec3(lorentz[0].x, -lorentz[0].y, -lorentz[0].z),
		vec3(-lorentz[1].x, lorentz[1].y, -lorentz[1].z),
		vec3(-lorentz[2].x, -lorentz[2].y, lorentz[2].z)
	);
	return EventAndPOR(position, inv_lorentz);
}
//==Compound
//TODO: What if it's a Rest?
vec4 get_velocity_at(int i, vec4 event) {
	if(accels[i] == 0.0)
		return path_transform[i]*get_velocity_at_rest(path_transform_inverse[i]*event);
	else
		return path_transform[i]*get_velocity_at_hyperbola(path_transform_inverse[i]*event);
}

EventAndPOR to_local_spacetime(int i, vec4 event) {
	if(accels[i] == 0.0)
		return to_local_spacetime_rest(path_transform_inverse[i]*event);
	else
		return to_local_spacetime_hyperbola(path_transform_inverse[i]*event, accels[i]);
}

vec4 rotate_position(int component, vec4 event) {
	float total_rads = rads[component] + event.z * spin_speed;
	float s = sin(total_rads);
	float c = cos(total_rads);

	mat2 rot = mat2(vec2(c, s),
	                vec2(-s,  c));
	event.xy = rot * event.xy;
	return event;
}

/*
var k = 1/(t+1);
new Vector4(1+k*x*x,	k*x*y,		x,	0),
new Vector4(k*x*y,		1+k*y*y,	y,	0),
new Vector4(x,			y,			t,	0),
new Vector4(0,			0,			0,	1)
*/

vec4 add_velocity(vec4 v1, vec4 v2) {
	float k = 1.0/(v1.z + 1.0);
	mat3 loretz = mat3(
		vec3(1.0+k*v1.x*v1.x, k*v1.x*v1.y, v1.x),
		vec3(k*v1.x*v1.y, 1.0+k*v1.y*v1.y, v1.y),
		v1.xyz
	);
	return vec4(loretz * v2.xyz, 0);
}

vec4 get_rotational_velocity(vec4 event) {
	vec2 spun = spin_speed * vec2(event.y, -event.x);
	float gamma2 = 1.0 - dot(spun, spun);
	if(gamma2 < 0.0)
		discard;
	return vec4(spun, sqrt(gamma2), 0);
}

int get_component(vec4 event) {
	//This version was for debugging. It's a simpler linear search.
	//for(int i=0; i<segment_count; ++i) {
        //if (dot(coevents[i], event) < 0.0) {
			//if(i<1)
				//discard;
			//return i-1;
		//}
	//}
	//return segment_count-1;

	//Binary search
    int left = -1;
    int right = segment_count;
    int mid = 0;

	//Here's the binary search I intend on actually using.
    while (right - left > 1) {
        mid = (left + right) / 2;

        if (dot(coevents[mid], event) > 0.0) {
            left = mid;
        } else {
            right = mid;
        }
    }
	if(left < 0 || right == segment_count)
		discard;
	return left;
}

//drawing functions
//Originally this just returned if it's there, but I need to have it return brightness.
float check_if_there(vec4 e2) {
	//if(e2.x*e2.x + e2.y*e2.y > size*size)
	//	discard;
	e2 *= sqrt(2);
	if(e2.x < -size || e2.x > size || e2.y < -size || e2.y > size)
		discard;
	vec4 color = texture(texture_sampler, e2.xy/(2.0*size) + vec2(0.5,0.5));
	if(color.x == 0.0 || color.w == 0.0)
		discard;
	return color.x;
}

float get_doppler(vec4 e, vec4 v) {
	//if(e.xy == vec2(0,0))
	//	return 1.0;
	return v.z*(1.0-(v.x*e.x+v.y*e.y)/sqrt(e.x*e.x+e.y*e.y));
}

float fade(float t) {
	//This uses a cosine function to fade from 0 to 1 without suddenly starting and stopping at the end.
	//return (1.0-cos(PI*t))*0.5;
	//This is the square root of the last one (because trig identities), and it's used to undo gamma correction weirdness that otherwise makes non-pure colors darker.
	return sin(PI*t*.5);
}

vec3 get_color(float ratio) {
	float original_wavelength = sqrt(2.0);
	float original_intensity = 1.0;

	float shifted_wavelength = log2(original_wavelength * ratio) * 5.0 - 1.0;
	float shifted_intensity = original_intensity; // divide by ratio if you want intensity shift

	vec3 rgb = vec3(0.0,0.0,0.0);
	float lower = floor(shifted_wavelength);
	float higher = lower + 1.0;
	float t = shifted_wavelength - lower;

	if (0.0 <= lower && lower < 3.0)
		rgb[int(lower)] = fade(1.0 - t) * shifted_intensity;
	if (0.0 <= higher && higher < 3.0)
		rgb[int(higher)] = fade(t) * shifted_intensity;
	else if (int(higher) == 3)
		rgb[0] = fade(2.0 * t) * shifted_intensity / 2.0;

	return rgb;
}

vec4 int_to_color(int value) {
    float r = float((value >> 0) & 3) / 3.0; // bits 0–1
    float g = float((value >> 2) & 3) / 3.0; // bits 2–3
    float b = float((value >> 4) & 3) / 3.0; // bits 4–5
    return vec4(r, g, b, 1.0);
}

vec4 draw(vec2 screen_pos) {
	vec2 rel = screen_pos - vec2(0.5);

	// === foo: Event on your past light-cone from your reference frame ===
	float t = -length(rel);
	vec4 foo = vec4(rel, t, 1.0); // (x, y, t, 1)

	// === bar: Transform foo to rest frame ===
	vec4 bar = por * foo;

	// === component: The index of the component of the componound path this pixel is in ===
	int component = get_component(bar);
	if(isnan(accels[component]))
		discard;
	//return int_to_color(component);

	// === e2: convert bar to object's (non-inertial) frame ===
	// For a hyperbola, e2.event.z is always 0.
	EventAndPOR e2 = to_local_spacetime(component, bar);

	vec4 e3 = rotate_position(component, e2.event);

	// Figure out if it even needs to draw that pixel
	float brightness = check_if_there(e3);
	//return(int_to_color(component));

	// === baz: Velocity at that event ===
	//vec4 baz = get_velocity_at(component, bar);

	// Add in velocity from rotation.
	// DEBUG: The redshift/blueshift is rotating with the object.
	// I need to find the velocity, then convert it back from local_spacetime.
	// That's supposed to be e2.pointOfReference.inverse, isn't it?
	vec4 baz = path_transform[component] * vec4(e2.pointOfReferenceInverse * get_rotational_velocity(e2.event).xyz, 0);

	// === quux: Transform velocity to observer frame ===
	vec4 quux = por_inverse * baz;

	// === doppler: Red/blue shift ===
	float doppler = get_doppler(foo, quux);

	// === color based on doppler shift ===
	vec4 color = vec4(get_color(doppler) * brightness, 1.0);

	return color;
}

void fragment() {
	//Draw the outline (for debugging purposes)
	if(UV.x < 0.01 || UV.x > 0.99 || UV.y < 0.01 || UV.y > 0.99) {
		COLOR = vec4(0,1,0,1);
	} else {
		//COLOR = path_transform[0][3];
		//COLOR.a = 1.0;
		COLOR = draw(SCREEN_UV);
	}
}
