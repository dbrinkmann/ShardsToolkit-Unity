using UnityEngine;
using System.Collections;

public class ForceFlat : MonoBehaviour {

	void Start () {
	
	}
	
	void Update () {
        gameObject.transform.localRotation = Quaternion.Euler(0,0,0);
	}
}
