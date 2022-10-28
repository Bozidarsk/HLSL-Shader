#pragma fragment frag

// Texture2D MainTex : register(t0);
// sampler _MainTex = sampler_state { Texture = (MainTex); };

// float WIDTH : register(c0);
// float HEIGHT : register(c1);

float4 frag(float2 uv : TEXCOORD0) : SV_Target
{
	return float4(uv.xy, 0, 1);
}





/*

Texture2D shaderTexture : register(t0);
SamplerState SampleType : register(s0);

cbuffer MatrixBuffer : register (b0)
{
	matrix worldMatrix;
	matrix viewMatrix;
	matrix projectionMatrix;
};

struct appdata
{
	float4 position : POSITION;
	float2 uv : TEXCOORD0;
};

struct v2f
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

v2f vert(appdata v)
{
	v2f o;
	v.position.w = 1;
	o.position = mul(v.position, worldMatrix);
	o.position = mul(o.position, viewMatrix);
	o.position = mul(o.position, projectionMatrix);
	o.uv = v.uv;

	return o;
}

float4 frag(v2f IN) : SV_Target
{
	return float4(0, 0.5, 0.5, 1);
}

*/