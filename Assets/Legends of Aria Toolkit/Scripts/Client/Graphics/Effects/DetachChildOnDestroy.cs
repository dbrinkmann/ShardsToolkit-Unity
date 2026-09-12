using UnityEngine;
using System.Collections;

public class DetachChildOnDestroy : MonoBehaviour 
{
    public GameObject targetObject;
    public float destroyDelay;

	void OnDestroy()
    {
        if(targetObject != null)
        {
            targetObject.transform.parent = null;

            if(destroyDelay != 0.0f)
            {	
				targetObject.SendMessage("OnDestroyDetach",SendMessageOptions.DontRequireReceiver);
                DestroyDelay delayComp = targetObject.AddComponent<DestroyDelay>();
                delayComp.DestroyDelaySecs = destroyDelay;
            }
        }
    }
}
