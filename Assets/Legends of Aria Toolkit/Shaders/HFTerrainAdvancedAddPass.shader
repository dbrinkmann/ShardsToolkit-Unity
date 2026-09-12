Shader "Hidden/TerrainEngine/Splatmap/Lightmap-AddPass-Advanced" {
Properties {
	_Depth ("Blend Depth", Range(0.001, 1)) = 0.1
	_Control ("Control (RGBA)", 2D) = "black" {}
	_Splat3 ("Layer 3 (A)", 2D) = "white" {}
	_Splat2 ("Layer 2 (B)", 2D) = "white" {}
	_Splat1 ("Layer 1 (G)", 2D) = "white" {}
	_Splat0 ("Layer 0 (R)", 2D) = "white" {}
}
	
SubShader {
	Tags {
		"SplatCount" = "4"
		"Queue" = "Geometry-99"
		"IgnoreProjector"="True"
		"RenderType" = "Opaque"
	}
CGPROGRAM
#pragma surface surf Lambert decal:add
#pragma target 3.0

struct Input {
	float2 uv_Control : TEXCOORD0;
	float2 uv_Splat0 : TEXCOORD1;
	float2 uv_Splat1 : TEXCOORD2;
	float2 uv_Splat2 : TEXCOORD3;
	float2 uv_Splat3 : TEXCOORD4;
};

float _Depth;
sampler2D _Control;
sampler2D _Splat0,_Splat1,_Splat2,_Splat3;

inline float4 normalized(float4 val) {
	return val / (val.r + val.g + val.b + val.a);
}

inline float maximized(float4 val) {
	return max(val[0], max(val[1], max(val[2], val[3])));
}

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 sc = tex2D (_Control, IN.uv_Control);

	fixed4 t0 = tex2D (_Splat0, IN.uv_Splat0);
	fixed4 t1 = tex2D (_Splat1, IN.uv_Splat1);
	fixed4 t2 = tex2D (_Splat2, IN.uv_Splat2);
	fixed4 t3 = tex2D (_Splat3, IN.uv_Splat3);

	float4 blend;
	blend[0] = t0.a + sc.r;
	blend[1] = t1.a + sc.g;
	blend[2] = t2.a + sc.b;
	blend[3] = t3.a + sc.a;

	float a = maximized(blend);
	blend = normalized(max(blend - a + _Depth, 0));

	fixed3 col;
	col  = blend.r * t0;
	col += blend.g * t1;
	col += blend.b * t2;
	col += blend.a * t3;

	o.Albedo = col * (sc.r + sc.g + sc.b + sc.a);
	o.Alpha = 0;
}
ENDCG  
}

Fallback off
}
