using UnityEngine;

[ExecuteInEditMode]
[RequireComponent (typeof(Camera))]
[AddComponentMenu("Image Effects/Rendering/CustomCombine")]
public class CustomCombine : MonoBehaviour
{	
	public Shader UsedShader;
	private Material UsedMaterial;
	public RenderTexture InputRT;
	public Color MatColor = Color.red;

	private static Material CreateMaterial (Shader shader)
	{
		if (!shader)
			return null;
		Material m = new Material (shader);
		m.hideFlags = HideFlags.HideAndDontSave;//.HideAndDontSave;

		return m;
	}
	private static void DestroyMaterial (Material mat)
	{
		if (mat)
		{
			DestroyImmediate (mat);
			mat = null;
		}
	}
	
	
	void OnDisable()
	{
		DestroyMaterial ( UsedMaterial );
	}
	
	void Start()
	{
		if (!SystemInfo.supportsImageEffects)
		{
			enabled = false;
			return;
		}
		
		CreateMaterials ();
	}
	
	void OnEnable () 
	{
		//camera.depthTextureMode |= DepthTextureMode.DepthNormals;
	}

	private void CreateMaterials ()
	{
		if (!UsedMaterial)// && m_SSAOShader.isSupported)
		{
			UsedMaterial = CreateMaterial ( UsedShader);
			//UsedMaterial.SetTexture ("_RandomTexture", m_RandomTexture);
		}
	}
	
	[ImageEffectOpaque]
	public void OnRenderImage (RenderTexture source, RenderTexture destination)
	{
		if (!UsedShader.isSupported)
		{
			enabled = false;
			return;
		}
		CreateMaterials ();

		//Debug.Log(" OnRenderImage ");

		RenderTexture TempRT = RenderTexture.GetTemporary (source.width , source.height , 0, 
		                                                   RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1 );

		UsedMaterial.SetColor("_Color", MatColor );
		UsedMaterial.SetFloat("_Alpha", 1.0f );

		if( InputRT != null)
		{
			UsedMaterial.SetTexture("_MainTex", InputRT );
			UsedMaterial.SetTexture("_SecondTex", source );
			Graphics.Blit( InputRT , destination, UsedMaterial, 0);
		}
		else
		{
			Graphics.Blit(source, destination, UsedMaterial, -1);
		}
		RenderTexture.ReleaseTemporary ( TempRT );
	}
}
