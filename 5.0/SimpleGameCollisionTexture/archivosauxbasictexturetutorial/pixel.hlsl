Texture2D textcolor : register(t0);
SamplerState textsampler : register(s0);

float4 PS(float4 pos : SV_POSITION, float4 color : COLOR, float4 normal : NORMAL, float2 uvcoord : UV) : SV_Target
{
	float4 la = {1.0,1.0,1.0,1.0};
	float4	 ld = { 1.0,1.0,1.0,1.0 };
	float3 ldir = { 1.0,1.0,-1.5 };
	float cl = max(dot(ldir, normal.xyz), 0);
	float4 color1 = textcolor.Sample(textsampler, uvcoord)*color;
	float4 newcolor = la * color1  + float4(cl * (ld.rgb * color1.rgb), ld.a * color1.a);
	return newcolor;
}