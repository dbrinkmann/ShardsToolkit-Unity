using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace HighlightingSystem
{	
	public class HighlightingBase : MonoBehaviour
	{
		public float offsetFactor = 0f;
		public float offsetUnits = 0f;

		public int downsampleFactor
		{
			get { return _downsampleFactor; }
			set
			{
				if (_downsampleFactor != value)
				{
					// Is power of two check
					if ((value != 0) && ((value & (value - 1)) == 0))
					{
						_downsampleFactor = value;
					}
					else
					{
						Debug.LogWarning("HighlightingSystem : Prevented attempt to set incorrect downsample factor value.");
					}
				}
			}
		}

		public int iterations
		{
			get { return _iterations; }
			set
			{
				if (_iterations != value)
				{
					_iterations = value;
				}
			}
		}

		public float blurMinSpread
		{
			get { return _blurMinSpread; }
			set
			{
				if (_blurMinSpread != value)
				{
					_blurMinSpread = value;
				}
			}
		}

		public float blurSpread
		{
			get { return _blurSpread; }
			set
			{
				if (_blurSpread != value)
				{
					_blurSpread = value;
				}
			}
		}

		public float blurIntensity
		{
			get { return _blurIntensity; }
			set
			{
				if (_blurIntensity != value)
				{
					_blurIntensity = value;
					if (Application.isPlaying)
					{
						blurMaterial.SetFloat(1, _blurIntensity);
					}
				}
			}
		}

		protected CommandBuffer renderBuffer;

		protected int cachedWidth = -1;
		protected int cachedHeight = -1;
		protected int cachedAA = -1;

		[FormerlySerializedAs("downsampleFactor")] [SerializeField]
		protected int _downsampleFactor = 4;

		[FormerlySerializedAs("iterations")] [SerializeField]
		protected int _iterations = 2;

		[FormerlySerializedAs("blurMinSpread")] [SerializeField]
		protected float _blurMinSpread = 0.65f;

		[FormerlySerializedAs("blurSpread")] [SerializeField]
		protected float _blurSpread = 0.25f;

		[SerializeField] protected float _blurIntensity = 0.3f;

		protected RenderTargetIdentifier highlightingBufferID;

		protected RenderTexture highlightingBuffer = null;

		protected Camera cam = null;

		protected bool isDepthAvailable = true;

		// Dynamic materials
		protected Material blurMaterial;
		protected Material cutMaterial;
		protected Material compMaterial;
	}
}