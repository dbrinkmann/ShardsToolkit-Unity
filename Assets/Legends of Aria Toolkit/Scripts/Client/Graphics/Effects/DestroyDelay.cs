using UnityEngine;
using System.Collections;

public class DestroyDelay : MonoBehaviour 
{
    public GameObject TargetObject;
    public float DestroyDelaySecs;

    // Use this for initialization
    void Start()
    {
        if(TargetObject == null)
        {
            TargetObject = this.gameObject;
        }
        StartCoroutine(StartDelay());
    }

    IEnumerator StartDelay()
    {
        yield return new WaitForSeconds(DestroyDelaySecs);

        GameObject.Destroy(TargetObject);
    }
}
