using UnityEngine;
using System.Collections;

public class AutoFaceCamera : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update() {
        var v = Camera.main.transform.forward;
        v.y = 0;
        transform.rotation = Quaternion.LookRotation(v, Vector3.up);
	}
}
