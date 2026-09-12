using UnityEngine;
using System.Collections;

public class SetSpinFromObjectProperty : MonoBehaviour {

    private void NotifyObjectPropertyChanged(object[] args)
    {
        string name = (string)args[0];
        if (name == "AnimationParamater")
        {
            double angle = ((double)args[1]);
            Animator animator = gameObject.GetComponentInChildren<Animator>();
            animator.Play(0, -1, (float)(angle / 60)/6);
           // Debug.Log("Animation time is "+angle+ ", angle is " +angle / 60);
        }
    }
}
