// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/CustomShader" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,0.5)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Alpha ("Alpha", Range(0,1) ) = 0.5
	}

	SubShader
	{
		Tags {"Queue"="Geometry"  "RenderType"="Opaque"  }
		LOD 200
		Pass
		{
			Lighting On
			 
			Tags {"LightMode" = "ForwardBase"}
			 
			//ZTest On//Lequal//Always//On			
			//ZWrite Off
			//Cull Back
			//AlphaTest Greater 0.3
			//Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#pragma alphatest:0.2
			#pragma multi_compile_fwdbase
			
			//#define SHADOWS_DEPTH
			//#define SPOT
			
			#include "UnityCG.cginc"
			//#include "UnityShaderVariables.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"


			float4 _Color;
			
			//struct vertexInput 
			//{
				//float4 vertex : POSITION;
				//float4 texcoord0 : TEXCOORD0;
			//};*/

			struct fragmentInput
			{
				float4 pos : SV_POSITION;
				float4 texcoord0 : TEXCOORD1;
				float3 normal : TEXCOORD2;
				LIGHTING_COORDS(3,4)
			};

			fragmentInput vert(appdata_tan v)
			{
				fragmentInput o;
				//i.vertex.xyz += float3(2,0,0);
				v.vertex.xyz *= 1.0;//1;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.texcoord0 = v.texcoord;
				//float4x4 normalmat = inverse(_Object2World);
				o.normal = v.normal;//mul( normalmat, float4(v.normal,1) );
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}
				
			uniform sampler2D _MainTex;
			float _Alpha;
			
			float4 frag(fragmentInput i) : COLOR
			{
				float3 lightColor = _LightColor0.rgb;
				float3 lightDir = _WorldSpaceLightPos0;
				float4 colorTex = tex2D(_MainTex, i.texcoord0.xy );
				float atten = LIGHT_ATTENUATION(i);
				//float atten = tex2D(_LightTexture0, dot(i._LightCoord,i._LightCoord).rr).UNITY_ATTEN_CHANNEL * SHADOW_ATTENUATION(i);
				//float3 N = normalize(i.normal);				
				//float NL = saturate(dot(N, lightDir));
    			//float shadow = LIGHT_ATTENUATION(i) * NL;
    			//float4 shadowtex = tex2D( _ShadowMapTexture, i.texcoord0.xy );
				float3 color = colorTex.rgb * lightColor * atten;//shadow;// * NL;
				float4 ret = float4(color, colorTex.a);
				ret.a *= _Alpha;
				return ret;
				//return float4(i.normal,0.5);
					//DIRECTIONAL=1
					//SHADOWS_SCREEN=1				
			}
			ENDCG
		}
	}

	Fallback "Diffuse"
}
