Shader "Custom/Diffuse-Hue-Normal" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_BumpMap ("Bumpmap", 2D) = "bump" {}
	_BumpPower ("Bump Power", Range (3, 0.01)) = 1
	_Mask ("Mask", 2D) = "white" {}
	_HueTable ("Hue Table", 2D) = "white" {}
	_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	_Hue ("Hue Index", Range(0,1024)) = 0
}

SubShader {
	Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
	
	
CGPROGRAM
#pragma surface surf Lambert alphatest:_Cutoff

sampler2D _MainTex;
sampler2D _Mask;
sampler2D _HueTable;
sampler2D _BumpMap;
fixed _BumpPower;
fixed4 _Color;
int _Hue;

struct Input {
	float2 uv_MainTex;
	float2 uv_BumpMap;
};

void surf (Input IN, inout SurfaceOutput o) {	
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);	

	if(_Hue != 0)
	{
		fixed4 m = tex2D(_Mask, IN.uv_MainTex);	
        float lookupColor = pow((c.rgb[0] + c.rgb[1] + c.rgb[2])/3.0f,1/2.2f);
        fixed4 h = tex2D(_HueTable, fixed2(lookupColor,1.0f - ((float)_Hue+0.5f)/1024.0f));
        fixed4 lerpColor = lerp(c,h,m.rgb[0]);
        c.rgb = lerpColor.rgb * _Color;
	}
	else
	{
		c = c * _Color;
	}

	o.Albedo = c.rgb;
	o.Alpha = c.a;
	fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
	normal.z = normal.z / _BumpPower;
	o.Normal = normalize(normal);
}
ENDCG
}

Fallback "Legacy Shaders/Transparent/Cutout/VertexLit"
}
