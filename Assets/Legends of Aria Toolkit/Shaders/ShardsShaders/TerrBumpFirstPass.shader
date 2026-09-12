// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom Nature/Terrain/Bumped Specular" {
Properties {
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.03, 1)) = 0.078125

	// set by terrain engine
	[HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
	[HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
	[HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
	[HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
	[HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
	[HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
	[HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
	[HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
	[HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
	// used in fallback on old cards & base map
	[HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
	[HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
	
	_DecalTexture ("Decal Texture", 2D) = "white" {}
	_DecalPosition ("Decal Position", Vector) = (0,0,0,0)
	_DecalSize ("Decal Size", Vector) = (0,0,0,0)
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


struct appdata_full_custom
{
    float4 vertex : POSITION;
    float4 tangent : TANGENT;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    float4 color : COLOR;
};

void vert (inout appdata_full_custom v)
{
	v.tangent.xyz = cross(v.normal, float3(0,0,1));
	v.tangent.w = -1;
	
	float3 WorldPos = (float3)mul (unity_ObjectToWorld, v.vertex);
	v.color.xyz = WorldPos.xyz;
	v.color.w = 0.0;	
}

struct Input 
{
	float2 uv_Control : TEXCOORD0;
	float2 uv_Splat0 : TEXCOORD1;
	
	float2 uv_Splat1 : TEXCOORD2;
	float2 uv_Splat2 : TEXCOORD3;
	float2 uv_Splat3 : TEXCOORD4;
	
	float4 T0 : TEXCOORD0;
	float4 T1 : TEXCOORD1;
	float4 color : COLOR;//TEXCOORD5;
};

sampler2D _Control;
sampler2D _Splat0,_Splat1,_Splat2,_Splat3;
sampler2D _Normal0,_Normal1,_Normal2,_Normal3;
half _Shininess;

uniform sampler2D _DecalTexture;
uniform float4 _DecalPosition;
uniform float4 _DecalSize;
			
void surf (Input IN, inout SurfaceOutput o) {
	fixed4 splat_control = tex2D (_Control, IN.uv_Control);
	fixed4 col;
	col  = splat_control.r * tex2D (_Splat0, IN.uv_Splat0.xy);
	col += splat_control.g * tex2D (_Splat1, IN.uv_Splat1.xy);
	col += splat_control.b * tex2D (_Splat2, IN.uv_Splat2.xy);
	col += splat_control.a * tex2D (_Splat3, IN.uv_Splat3.xy);
	o.Albedo = col.rgb;

	fixed4 nrm;
	nrm  = splat_control.r * tex2D (_Normal0, IN.uv_Splat0.xy);
	nrm += splat_control.g * tex2D (_Normal1, IN.uv_Splat1.xy);
	nrm += splat_control.b * tex2D (_Normal2, IN.uv_Splat2.xy);
	nrm += splat_control.a * tex2D (_Normal3, IN.uv_Splat3.xy);
	// Sum of our four splat weights might not sum up to 1, in
	// case of more than 4 total splat maps. Need to lerp towards
	// "flat normal" in that case.
	fixed splatSum = dot(splat_control, fixed4(1,1,1,1));
	fixed4 flatNormal = fixed4(0.5,0.5,1,0.5); // this is "flat normal" in both DXT5nm and xyz*2-1 cases
	nrm = lerp(flatNormal, nrm, splatSum);
	o.Normal = UnpackNormal(nrm);

	o.Gloss = col.a * splatSum;
	o.Specular = _Shininess;
	
	//Decals->
	float3 WorldPos = float3(IN.color.xyz);
	
	float3 PosDif = float3( WorldPos.xyz - _DecalPosition.xyz );
	float3 Compare = abs( PosDif );
	
	float2 ProjectTexcoord = float2(0.0,0.0);
	bool IsInDecal = false;
	if (Compare.x < _DecalSize.x/2 && Compare.z < _DecalSize.z/2)
	{
		//PosDif.xz *= float2(1.1,1.1);
		PosDif += _DecalSize.xyz/2;
		PosDif /= _DecalSize.xyz;
		
		ProjectTexcoord = float2(PosDif.x, PosDif.z);
		IsInDecal = true;		
	}
	
	float4 DecalTexel = tex2D(_DecalTexture, ProjectTexcoord );
	o.Alpha = 0.0;
	if (IsInDecal)// && DecalTexel.a > 0.32)
	{
		o.Albedo = DecalTexel.rgb * DecalTexel.a + col.rgb * (1.0 - DecalTexel.a);
		//o.Alpha = DecalTexel.a;
	}
}
ENDCG  
}

//Dependency "AddPassShader" = "Hidden/Nature/Terrain/Bumped Specular AddPass"
//Dependency "BaseMapShader" = "Specular"

//Fallback "Nature/Terrain/Diffuse"
}
