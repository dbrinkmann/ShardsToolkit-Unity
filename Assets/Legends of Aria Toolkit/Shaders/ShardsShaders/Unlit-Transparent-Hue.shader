// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Unlit-Transparent-Hue"
{
	Properties
	{
		_MainTex ("Base (RGB), Alpha (A)", 2D) = "black" {}
		_Mask ("Mask", 2D) = "white" {}
		_HueTable ("Hue Table", 2D) = "white" {}
		_Hue ("Hue Index", Range(0,1024)) = 0
	}
	
	SubShader
	{
		LOD 200

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		
		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Fog { Mode Off }
			Offset -1, -1
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _Mask;
			sampler2D _HueTable;
			int _Hue;

	
			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
			};
	
			struct v2f
			{
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
			};
	
			v2f o;

			v2f vert (appdata_t v)
			{
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				o.color = v.color;
				return o;
			}
				
			fixed4 frag (v2f IN) : SV_Target
			{
				//return tex2D(_MainTex, IN.texcoord) * IN.color;

				fixed4 c = tex2D(_MainTex, IN.texcoord);	

				if(_Hue != 0)
				{
					fixed4 m = tex2D(_Mask, IN.texcoord);
					if(m.rgb[0] > 0.5f)
					{			
						fixed4 h = tex2D(_HueTable, fixed2(min(0.999,(c.rgb[0] + c.rgb[1] + c.rgb[2])/3.0f),1.0f - ((float)_Hue+0.5f)/1024.0f));
						fixed4 lerpColor = lerp(c,h,m.rgb[0]);
						c.rgb = lerpColor.rgb * IN.color;
					}
				}
				else
				{
					c = c * IN.color;
				}

				return c;
			}
			ENDCG
		}
	}

	SubShader
	{
		LOD 100

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		
		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Fog { Mode Off }
			Offset -1, -1
			//ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMaterial AmbientAndDiffuse
			
			SetTexture [_MainTex]
			{
				Combine Texture * Primary
			}
		}
	}
}
