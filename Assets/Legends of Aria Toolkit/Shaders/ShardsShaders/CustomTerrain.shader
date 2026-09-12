// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/CustomTerrain" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,0.5)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Alpha ("Alpha", Range(0,1) ) = 0.5
		
		_DecalTexture ("Decal Texture", 2D) = "white" {}
		_DecalPosition ("Decal Position", Vector) = (0,0,0,0)
		_DecalSize ("Decal Size", Vector) = (0,0,0,0)
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
			
			
			struct fragmentInput
			{
				float4 pos : SV_POSITION;
				float4 texcoord0 : TEXCOORD1;
				float3 normal : TEXCOORD2;
				float3 WorldPos : TEXCOORD5;
				LIGHTING_COORDS(3,4)
			};

			fragmentInput vert(appdata_tan v)
			{
				fragmentInput o;
				//i.vertex.xyz += float3(2,0,0);
				v.vertex.xyz *= 1.0;//1;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.WorldPos = mul (unity_ObjectToWorld, v.vertex);
				o.texcoord0 = v.texcoord;				
				o.normal = v.normal;//mul( normalmat, float4(v.normal,1) );
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}
				
			uniform sampler2D _MainTex;
			uniform sampler2D _DecalTexture;
			uniform float4 _DecalPosition;
			uniform float4 _DecalSize;
			
			float _Alpha;
			
			float4 frag(fragmentInput i) : COLOR
			{
				float3 lightColor = _LightColor0.rgb;
				float3 lightDir = _WorldSpaceLightPos0;
				float4 colorTex = tex2D(_MainTex, i.texcoord0.xy );				
				float atten = LIGHT_ATTENUATION(i);
				float3 color = colorTex.rgb *  lightColor * atten;
				
				float3 PosDif = float3( i.WorldPos.xyz - _DecalPosition.xyz );
				float3 Compare = abs( PosDif );
				
				float4 ret = float4(colorTex.rgb,0);//float4(color, colorTex.a);
				
				if (Compare.x < _DecalSize.x/2 && Compare.z < _DecalSize.z/2)
				{
					PosDif += _DecalSize.xyz/2;
					PosDif /= _DecalSize.xyz;
					float2 ProjectTexcoord = float2(PosDif.x, PosDif.z); 
					float4 colorTex2 = tex2D(_DecalTexture, ProjectTexcoord );
					
					ret.rgb = colorTex2.rgb * colorTex2.a + ret.rgb * (1.0 - colorTex2.a);
					//ret = float4(colorTex2.rgba,0);
				}
				//ret.a *= _Alpha;
				return ret;
			}
			ENDCG
		}
	}

	Fallback "Diffuse"
}
