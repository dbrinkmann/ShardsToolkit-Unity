using UnityEngine;
using System.Collections;

public class SparksParticlePlay : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Example ();
	}

	void Example() 
	{
		InvokeRepeating ("PlayParticleEffect", 2.5f, 2.5f);
	}
	
	void PlayParticleEffect () {

		gameObject.GetComponent<ParticleSystem>().Play();
	}

}
