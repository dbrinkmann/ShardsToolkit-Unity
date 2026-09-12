Shader "Nature/Terrain/Advanced Diffuse HotFix" {
Properties {
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.03, 1)) = 0.078125
	
	_Depth ("Blend Depth", Range(0.001, 1)) = 0.5
	[HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
	[HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "black" {}
	[HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "black" {}
	[HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "black" {}
	[HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "black" {}
	// used in fallback on old cards & base map
	[HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
	[HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
	[HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
	[HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
	[HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
	[HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
}
	
SubShader {
	Tags {
		"SplatCount" = "4"
		"Queue" = "Geometry-100"
		"RenderType" = "Opaque"
	}
CGPROGRAM
#pragma surface surf BlinnPhong vertex:vert
#pragma target 3.0

void vert (inout appdata_full v)
{
	v.tangent.xyz = cross(v.normal, float3(0,0,1));
	v.tangent.w = -1;
}

struct Input {
	float2 uv_Control : TEXCOORD0;
	float2 uv_Splat0 : TEXCOORD1;
	float2 uv_Splat1 : TEXCOORD2;
	float2 uv_Splat2 : TEXCOORD3;
	float2 uv_Splat3 : TEXCOORD4;
};

float _Depth;
sampler2D _Normal0,_Normal1,_Normal2,_Normal3;
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
	
	fixed4 nrm;
	nrm  = sc.r * tex2D (_Normal0, IN.uv_Splat0);
	nrm += sc.g * tex2D (_Normal1, IN.uv_Splat1);
	nrm += sc.b * tex2D (_Normal2, IN.uv_Splat2);
	nrm += sc.a * tex2D (_Normal3, IN.uv_Splat3);
	// Sum of our four splat weights might not sum up to 1, in
	// case of more than 4 total splat maps. Need to lerp towards
	// "flat normal" in that case.
	fixed splatSum = dot(sc, fixed4(1,1,1,1));
	fixed4 flatNormal = fixed4(0.5,0.5,1,0.5); // this is "flat normal" in both DXT5nm and xyz*2-1 cases
	nrm = lerp(flatNormal, nrm, splatSum);
	o.Normal = UnpackNormal(nrm);
}
ENDCG
}

Dependency "AddPassShader" = "Hidden/Nature/Terrain/Bumped Specular AddPass"
Dependency "BaseMapShader" = "Diffuse"
Dependency "Details0"      = "Hidden/TerrainEngine/Details/Vertexlit"
Dependency "Details1"      = "Hidden/TerrainEngine/Details/WavingDoublePass"
Dependency "Details2"      = "Hidden/TerrainEngine/Details/BillboardWavingDoublePass"
Dependency "Tree0"         = "Hidden/TerrainEngine/BillboardTree"

// Fallback to Default shader
Fallback "Nature/Terrain/Diffuse"
}
