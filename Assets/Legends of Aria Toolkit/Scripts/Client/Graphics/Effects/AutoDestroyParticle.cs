using UnityEngine;
using System.Collections;

public class AutoDestroyParticle : MonoBehaviour {
	
	public float Lifetime;
	// percentage of lifetime that emitter should be enabled
	public float EmitterLifetimePct = 0.5f;
	
	private ParticleSystem ps;	
	
}
