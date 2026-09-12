Shader "Unlit/UIMaskShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Mask("Mask Texture", 2D) = "white" {}
	}
	SubShader
	{
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			SetTexture[_Mask] {combine texture}
			SetTexture[_MainTex] {combine texture, previous}
		}
	}
}
