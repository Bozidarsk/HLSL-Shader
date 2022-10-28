#define WIDTH 1920
#define HEIGHT 1080
#define NUMTHREADS 8

RWStructuredBuffer<float4> _MainTex;

[numthreads(NUMTHREADS, NUMTHREADS, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
	uint position = id.y * WIDTH + id.x;
	float4 color = _MainTex[position];
	float2 uv = float2(id.x, HEIGHT - id.y) / float2(WIDTH, HEIGHT);
	float2 screenPos = float3(id.x, HEIGHT - id.y, 0);

	float scale = 10;
	float offset = 5;
	float intensity = 10;
	float falloff = 0.7;
	float falloffScale = 5;

	float value = 1 - abs(sin(uv.x * scale) - (uv.y * scale - offset));
	value = pow(saturate(value), intensity);

	value *= saturate(pow(uv.x, falloff) / (falloff / falloffScale));
	value *= saturate(pow(1 - uv.x, falloff) / (falloff / falloffScale));
	color = float4(0, value, 0, 1);

	_MainTex[position] = saturate(color);
}