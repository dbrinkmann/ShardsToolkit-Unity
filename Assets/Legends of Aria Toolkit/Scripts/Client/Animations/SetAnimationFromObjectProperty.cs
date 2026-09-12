using UnityEngine;
using System.Collections;

public class SetAnimationFromObjectProperty : MonoBehaviour {

    private void NotifyObjectPropertyChanged(object[] args)
    {
        string name = (string)args[0];
        if (name == "AnimationParamater")
        {
            Animation animComp = gameObject.GetComponentInChildren<Animation>();
            animComp["SwingPendululm"].time = 0;
            animComp.Play("SwingPendululm");
            // Debug.Log("Animation time is "+angle+ ", angle is " +angle / 60);
        }
    }
}
