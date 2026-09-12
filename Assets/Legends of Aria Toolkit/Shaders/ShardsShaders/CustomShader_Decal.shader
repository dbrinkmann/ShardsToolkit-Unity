// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/CustomShader_Decal" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,0.5)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Alpha ("Alpha", Range(0,1) ) = 0.5
	}

	SubShader
	{
		Tags {"Queue"="Geometry+2"  "RenderType"="Transparent"  }
		LOD 200
		Pass
		{
			Lighting On
			 
			Tags {"LightMode" = "ForwardBase"}
			 
			ZTest Off//Lequal//Always//On			
			//ZWrite Off
			//Cull Back
			//AlphaTest Greater 0.3
			ColorMask RGBA
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma alphatest:0.2 alpha
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
				//float3 normal : TEXCOORD2;
				//LIGHTING_COORDS(3,4)
			};

			fragmentInput vert(appdata_tan v)
			{
				fragmentInput o;
				//i.vertex.xyz += float3(2,0,0);
				//v.vertex.xyz *= 1.0;//1;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.texcoord0 = v.texcoord;
				//float4x4 normalmat = inverse(_Object2World);
				//o.normal = v.normal;//mul( normalmat, float4(v.normal,1) );
				//TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}
				
			uniform sampler2D _MainTex;
			float _Alpha;
			
			float4 frag(fragmentInput i) : COLOR
			{
				float4 colorTex = tex2D(_MainTex, i.texcoord0.xy );			
				colorTex.a *= _Alpha;
				return colorTex;
			}
			ENDCG
		}
	}

	Fallback "Diffuse"
}
