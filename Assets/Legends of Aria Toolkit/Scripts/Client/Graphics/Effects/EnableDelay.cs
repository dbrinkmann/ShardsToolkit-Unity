using UnityEngine;
using System.Collections;

public class EnableDelay : MonoBehaviour {
    public GameObject TargetObject;
    public float DelaySecs;

	// Use this for initialization
	void Start () {
        StartCoroutine(StartDelay());
	}
	
    IEnumerator StartDelay()
    {
        yield return new WaitForSeconds(DelaySecs);

        TargetObject.SetActive(true);
    }
}
