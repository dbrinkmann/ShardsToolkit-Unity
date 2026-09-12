using UnityEngine;
using System.Collections;

public class PendulumReplayScript : MonoBehaviour {

    private void NotifyObjectPropertyChanged(object[] args)
    {
        string name = (string)args[0];
        if (name == "AnimationParamater")
        {
            //Debug.Log("AnimationParamater is " + args[1]);
            double angle = ((double)args[1]);
            Animator animator = gameObject.GetComponentInChildren<Animator>();
            animator.Play("SwingPendulum", -1, 0f);
           // Debug.Log("Animation time is "+angle+ ", angle is " +angle / 60);
        }
    }
}
