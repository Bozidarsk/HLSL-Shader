#define WIDTH 1920
#define HEIGHT 1080
#define NUMTHREADS 8

RWStructuredBuffer<float4> _MainTexture;

struct appdata
{
	uint3 id;
};

struct v2f
{
	float2 uv;
	float3 screenPos;
};

v2f vert(appdata v) 
{
	v2f o;
	v.id.y = HEIGHT - v.id.y;
	o.uv = float2(v.id.xy) / float2(WIDTH, HEIGHT);
	o.screenPos = float3(v.id.xy, 0);

	v.id.y = HEIGHT - v.id.y;
	return o;
}

float4 frag(v2f IN) 
{
	// return float4(IN.uv.xy, 0.4, 1);

	float scale = 10;
	float offset = 5;
	float intensity = 5;
	float falloff = 0.7;
	float falloffScale = 5;

	float value = 1 - abs(sin(IN.uv.x * scale) - (IN.uv.y * scale - offset));
	value = pow(saturate(value), intensity);

	value *= saturate(pow(IN.uv.x, falloff) / (falloff / falloffScale));
	value *= saturate(pow(1 - IN.uv.x, falloff) / (falloff / falloffScale));

	return saturate(float4(0, value, 0, 1));
}

[numthreads(NUMTHREADS, NUMTHREADS, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
	uint position = id.y * WIDTH + id.x;

	appdata data;
	data.id = id;

	_MainTexture[position] = frag(vert(data));
}