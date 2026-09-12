using UnityEngine;
using System.Collections;

public class RtsCamera : MonoBehaviour {

	// Use this for initialization
    protected void Start()
    {
        gameObject.GetComponent<Camera>().gameObject.SetActive(false);
    }
}
