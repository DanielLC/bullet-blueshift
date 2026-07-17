float fade(float t) {
	//This uses a cosine function to fade from 0 to 1 without suddenly starting and stopping at the end.
	//return (1.0-cos(PI*t))*0.5;
	//This is the square root of the last one (because trig identities), and it's used to undo gamma correction weirdness that otherwise makes non-pure colors darker.
	return sin(PI*t*.5);
}

vec3 get_color(float freq) {
	float original_intensity = 1.0;

	float log_frequency = log2(freq) * 4.0 + 1.0;
	float shifted_intensity = original_intensity; // divide by ratio to the fourth if you want proper intensity shift

	vec3 rgb = vec3(0.0,0.0,0.0);
	// Why aren't I making these ints immediately?
	float lower = floor(log_frequency);
	float higher = lower + 1.0;
	float t = log_frequency - lower;

	//if (lower < -1.0)
	//	return vec3(0.5,0.5,0.5);

	if (0.0 <= lower && lower < 3.0)
		rgb[int(lower)] = fade(1.0 - t) * shifted_intensity;
	if (0.0 <= higher && higher < 3.0)
		rgb[int(higher)] = fade(t) * shifted_intensity;
	else if (int(higher) == 3)
		rgb[0] = fade(2.0 * t) * shifted_intensity / 2.0;

	return rgb;
}